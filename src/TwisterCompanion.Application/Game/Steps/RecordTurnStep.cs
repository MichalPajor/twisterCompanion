using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Game.Steps;

/// <summary>
/// Zapisuje turę w partii: numeruje ją, dopisuje do historii ruchów i aktualizuje
/// pozycje kończyn gracza.
/// </summary>
/// <remarks>
/// Krok musi następować <b>po</b> losowaniu wydarzeń (Etap 6), bo wydarzenie jest częścią
/// zapisywanej tury i wpływa na licznik wydarzeń w statystykach.
/// </remarks>
internal sealed class RecordTurnStep : ITurnPipelineStep
{
    /// <inheritdoc />
    public Task ExecuteAsync(TurnContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Move move = context.Move
            ?? throw new InvalidOperationException(
                $"{nameof(RecordTurnStep)} wymaga wylosowanego ruchu. "
                + $"Sprawdź kolejność kroków — {nameof(SelectMoveStep)} musi być wcześniej.");

        context.Turn = context.Session.BeginTurn(move, context.Event);

        return Task.CompletedTask;
    }
}
