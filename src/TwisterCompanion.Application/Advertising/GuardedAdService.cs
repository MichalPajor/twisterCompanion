using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Application.VoiceControl;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Advertising;

/// <summary>
/// Reguły reklam pilnowane przed każdym pokazaniem.
/// </summary>
/// <remarks>
/// Nakładka na <see cref="IAdPlatform"/>, przez którą przechodzi każde żądanie reklamy —
/// i to jest cały jej sens. Reguły dają się wtedy sprawdzić testem, a nie tylko przeczytać
/// w komentarzu, i nie da się ich pominąć przez pomyłkę w nowym miejscu kodu.
/// <para>
/// Zakazy, nie zalecenia:
/// <list type="bullet">
/// <item>reklama pełnoekranowa tylko przy partii zakończonej — nigdy w trakcie losowania,
/// odczytu ani wykonywania ruchu;</item>
/// <item>nigdy, kiedy aplikacja mówi — reklama przerwałaby komunikat i zabrałaby dźwięk;</item>
/// <item>nigdy przy otwartym nasłuchu komend — mikrofon i reklama walczyłyby o dźwięk,
/// a gracz o uwagę.</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class GuardedAdService(
    IAdPlatform platform,
    IGameEngine engine,
    IAnnouncementSpeaker speaker,
    IVoiceControlService voiceControl,
    ILogger<GuardedAdService> logger)
    : IAdService
{
    /// <inheritdoc />
    public bool IsAvailable => platform.IsAvailable;

    /// <inheritdoc />
    public Task<bool> PrepareAsync(CancellationToken cancellationToken = default) =>
        platform.PrepareAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ShowInterstitialAsync(CancellationToken cancellationToken = default)
    {
        if (!platform.IsAvailable)
        {
            return false;
        }

        if (engine.State != GameState.Finished)
        {
            logger.LogInformation(
                "Reklama pełnoekranowa odrzucona — partia jest w stanie {State}, a wolno ją"
                + " pokazać wyłącznie po zakończonej partii.",
                engine.State);

            return false;
        }

        if (speaker.IsSpeaking)
        {
            logger.LogInformation("Reklama pełnoekranowa odrzucona — trwa odczyt komunikatu.");

            return false;
        }

        if (voiceControl.State == VoiceControlState.Listening)
        {
            logger.LogInformation("Reklama pełnoekranowa odrzucona — trwa nasłuch komend.");

            return false;
        }

        return await platform.ShowInterstitialAsync(cancellationToken);
    }
}
