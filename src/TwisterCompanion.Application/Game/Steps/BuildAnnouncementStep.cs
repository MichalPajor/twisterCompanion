using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Game.Steps;

/// <summary>
/// Buduje tekst komunikatu dla rozegranej tury.
/// </summary>
/// <remarks>
/// Ten sam tekst pojawia się na ekranie i — od Etapu 7 — zostanie odczytany na głos.
/// Krok odczytu dołoży się do potoku po tym kroku i skorzysta z gotowego komunikatu.
/// </remarks>
internal sealed class BuildAnnouncementStep(IAnnouncementBuilder announcementBuilder) : ITurnPipelineStep
{
    private readonly IAnnouncementBuilder _announcementBuilder =
        announcementBuilder ?? throw new ArgumentNullException(nameof(announcementBuilder));

    /// <inheritdoc />
    public Task ExecuteAsync(TurnContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Turn turn = context.Turn
            ?? throw new InvalidOperationException(
                $"{nameof(BuildAnnouncementStep)} wymaga zapisanej tury. "
                + $"Sprawdź kolejność kroków — {nameof(RecordTurnStep)} musi być wcześniej.");

        context.Announcement = _announcementBuilder.BuildMove(turn);

        if (turn.Event is not null)
        {
            context.EventAnnouncement = _announcementBuilder.BuildEvent(turn.Event);
        }

        return Task.CompletedTask;
    }
}
