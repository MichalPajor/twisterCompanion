using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.EventSelection;

namespace TwisterCompanion.Application.Game.Steps;

/// <summary>
/// Losuje wydarzenie dla rozgrywanej tury.
/// </summary>
/// <remarks>
/// Krok musi być <b>po</b> losowaniu ruchu i <b>przed</b> zapisem tury: wydarzenie jest
/// częścią zapisywanej tury i wpływa na licznik wydarzeń oraz na historię potrzebną do
/// pilnowania odstępów.
/// </remarks>
internal sealed class RollEventStep(IEventSelector eventSelector) : ITurnPipelineStep
{
    private readonly IEventSelector _eventSelector =
        eventSelector ?? throw new ArgumentNullException(nameof(eventSelector));

    /// <inheritdoc />
    public Task ExecuteAsync(TurnContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        EventSelectionContext selectionContext = new()
        {
            Pack = context.EventPack,

            // Tura, która właśnie się rozgrywa, jeszcze nie została zapisana — jej numer
            // to kolejny po ostatnim. Bez tego przesunięcia odstępy między wydarzeniami
            // liczyłyby się o jedną turę za krótko.
            TurnNumber = context.Session.TurnNumber + 1,
            LastEventTurn = context.Session.LastEventTurn,
            LastEventTurns = context.Session.LastEventTurns,
            Options = context.EventSelectionOptions,
        };

        context.Event = _eventSelector.SelectNext(selectionContext);

        return Task.CompletedTask;
    }
}
