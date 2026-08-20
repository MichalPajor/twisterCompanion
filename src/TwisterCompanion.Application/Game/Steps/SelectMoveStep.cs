using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Application.Game.Steps;

/// <summary>
/// Losuje ruch dla wskazanego gracza.
/// </summary>
/// <remarks>
/// Krok buduje kontekst losowania z historii partii <b>oraz z pozycji kończyn tego
/// konkretnego gracza</b>. To drugie domyka mechanizm z Etapu 4: algorytm umiał już karać
/// ruch, który niczego nie zmienia, ale nie miał skąd wziąć informacji o tym, gdzie stoją
/// kończyny. Teraz ją ma, bo aplikacja sama wcześniej ogłosiła każdy ruch.
/// </remarks>
internal sealed class SelectMoveStep(IMoveSelectionStrategy strategy) : ITurnPipelineStep
{
    private readonly IMoveSelectionStrategy _strategy =
        strategy ?? throw new ArgumentNullException(nameof(strategy));

    /// <inheritdoc />
    public Task ExecuteAsync(TurnContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Player player = context.Player
            ?? throw new InvalidOperationException(
                $"{nameof(SelectMoveStep)} wymaga wskazanego gracza. "
                + $"Sprawdź kolejność kroków — {nameof(SelectPlayerStep)} musi być wcześniej.");

        MoveSelectionContext selectionContext = new()
        {
            RecentMoves = context.Session.MoveHistory.Snapshot(),
            CurrentLimbPositions = context.Session.GetLimbPositions(player.Id),
            Options = context.MoveSelectionOptions,
        };

        context.Move = _strategy.SelectNext(selectionContext);

        return Task.CompletedTask;
    }
}
