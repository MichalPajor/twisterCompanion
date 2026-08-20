using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Voice;

namespace TwisterCompanion.Application.Feedback;

/// <summary>
/// Domyślna odpowiedź aplikacji na zdarzenia gry: dźwięk plus wibracja.
/// </summary>
/// <remarks>
/// Zasady są tutaj, a nie w kodzie platformowym, bo są regułami aplikacji i mają testy —
/// „nie graj w trakcie mowy" albo „przy zerowej głośności nie zawracaj głowy odtwarzaczowi"
/// to decyzje projektowe, a nie szczegół Androida.
/// </remarks>
internal sealed class GameFeedback : IGameFeedback
{
    private readonly ISoundService _sounds;
    private readonly IHapticService _haptics;
    private readonly ISettingsService _settings;
    private readonly IAnnouncementSpeaker _speaker;
    private readonly ILogger<GameFeedback> _logger;

    /// <summary>Tworzy serwis reakcji na zdarzenia gry.</summary>
    /// <param name="sounds">Port odtwarzania efektów.</param>
    /// <param name="haptics">Port wibracji.</param>
    /// <param name="settings">Ustawienia aplikacji.</param>
    /// <param name="speaker">Odczyt głosowy — źródło informacji, czy aplikacja mówi.</param>
    /// <param name="logger">Logger.</param>
    public GameFeedback(
        ISoundService sounds,
        IHapticService haptics,
        ISettingsService settings,
        IAnnouncementSpeaker speaker,
        ILogger<GameFeedback> logger)
    {
        ArgumentNullException.ThrowIfNull(sounds);
        ArgumentNullException.ThrowIfNull(haptics);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(logger);

        _sounds = sounds;
        _haptics = haptics;
        _settings = settings;
        _speaker = speaker;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PreloadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _sounds.PreloadAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Nie udało się wczytać efektów dźwiękowych.");
        }
    }

    /// <inheritdoc />
    public void Play(FeedbackMoment moment)
    {
        AppSettings settings = _settings.Current;

        PlaySound(moment, settings);
        Vibrate(moment, settings);
    }

    /// <summary>
    /// Odtwarza dźwięk, jeśli wolno.
    /// </summary>
    /// <remarks>
    /// Trzy warunki muszą zajść naraz: dźwięki włączone, głośność niezerowa i cisza w mowie.
    /// Ostatni jest najważniejszy — polecenie „Anna, prawa ręka, czerwony" ma być zrozumiane,
    /// a nie przykryte fanfarą.
    /// </remarks>
    private void PlaySound(FeedbackMoment moment, AppSettings settings)
    {
        if (!settings.AreSoundsEnabled || settings.SoundVolume <= 0.0 || _speaker.IsSpeaking)
        {
            return;
        }

        try
        {
            _sounds.Play(ToSoundEffect(moment), settings.SoundVolume);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Nie udało się odtworzyć efektu dla {Moment}.", moment);
        }
    }

    /// <summary>
    /// Wywołuje wibrację, jeśli dane zdarzenie ją ma.
    /// </summary>
    /// <remarks>
    /// Wibracja <b>nie milczy w trakcie mowy</b> i nie zależy od włącznika dźwięków: ma własny
    /// przełącznik, bo jest jedyną informacją, która dochodzi do graczy przy wyciszonym
    /// telefonie. Nie każde zdarzenie ją dostaje — wibracja przy każdym ruchu zamieniłaby się
    /// w tło, którego nikt już nie zauważa.
    /// </remarks>
    private void Vibrate(FeedbackMoment moment, AppSettings settings)
    {
        if (!settings.AreHapticsEnabled || ToHapticIntensity(moment) is not { } intensity)
        {
            return;
        }

        try
        {
            _haptics.Vibrate(intensity);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Nie udało się zawibrować dla {Moment}.", moment);
        }
    }

    /// <summary>Dobiera próbkę do zdarzenia.</summary>
    private static SoundEffect ToSoundEffect(FeedbackMoment moment) => moment switch
    {
        FeedbackMoment.MoveRevealed => SoundEffect.MoveRevealed,
        FeedbackMoment.EventAnnounced => SoundEffect.EventTriggered,
        FeedbackMoment.PlayerEliminated => SoundEffect.PlayerEliminated,
        FeedbackMoment.GameStarted => SoundEffect.GameStarted,
        FeedbackMoment.GameFinished => SoundEffect.GameFinished,
        _ => SoundEffect.ButtonTap,
    };

    /// <summary>
    /// Dobiera wibrację do zdarzenia albo jej brak.
    /// </summary>
    /// <remarks>
    /// Wibrują trzy rzeczy: naciśnięcie przycisku (krótko, jako potwierdzenie) oraz wydarzenie
    /// i odpadnięcie gracza (mocniej, bo to zmiana w partii). Zwykły ruch i granice partii
    /// wibracji nie mają: ruch pada co turę, a start i koniec i tak są zapowiadane głosem.
    /// </remarks>
    private static HapticIntensity? ToHapticIntensity(FeedbackMoment moment) => moment switch
    {
        FeedbackMoment.ButtonTap => HapticIntensity.Light,
        FeedbackMoment.EventAnnounced => HapticIntensity.Strong,
        FeedbackMoment.PlayerEliminated => HapticIntensity.Strong,
        _ => null,
    };
}
