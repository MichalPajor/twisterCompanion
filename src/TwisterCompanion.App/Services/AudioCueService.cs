using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Sygnały stanu mikrofonu odtwarzane generatorem tonów urządzenia.
/// </summary>
/// <remarks>
/// Generator tonów Androida wystarcza do trzech krótkich sygnałów i <b>nie wymaga żadnego
/// pliku dźwiękowego ani odtwarzacza</b> — sygnał jest generowany przez system. Efekty
/// dźwiękowe rozgrywki z Etapu 11 dostaną osobną implementację z własnymi plikami; te trzy
/// sygnały mają zostać maksymalnie tanie, bo w trakcie partii odtwarzają się co turę.
/// <para>
/// Awarie są pochłaniane: brak sygnału pogarsza wygodę, ale nie może przerwać rozgrywki.
/// </para>
/// </remarks>
internal sealed class AudioCueService : IAudioCueService, IDisposable
{
    private readonly ILogger<AudioCueService> _logger;

#if ANDROID
    private Android.Media.ToneGenerator? _toneGenerator;
#endif

    private bool _disposed;

    /// <summary>Tworzy serwis sygnałów dźwiękowych.</summary>
    /// <param name="logger">Logger.</param>
    public AudioCueService(ILogger<AudioCueService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PlayAsync(AudioCue cue, CancellationToken cancellationToken = default)
    {
        TimeSpan duration = GetDuration(cue);

        try
        {
            Play(cue);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Nie udało się odtworzyć sygnału {Cue}.", cue);

            return;
        }

        // Czekamy na wybrzmienie, bo wywołujący otwiera mikrofon zaraz po powrocie z tej
        // metody — bez tego rozpoznawanie usłyszałoby sam sygnał.
        await Task.Delay(duration, cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

#if ANDROID
        _toneGenerator?.Release();
        _toneGenerator?.Dispose();
        _toneGenerator = null;
#endif
    }

    /// <summary>Jak długo brzmi dany sygnał.</summary>
    private static TimeSpan GetDuration(AudioCue cue) => cue switch
    {
        AudioCue.ListeningStopped => TimeSpan.FromMilliseconds(220),

        // Tyknięcie musi być krótkie: odtwarza się co sekundę, więc dłuższy dźwięk
        // zamieniłby odliczanie w ciągły pisk.
        AudioCue.CountdownTick => TimeSpan.FromMilliseconds(45),
        _ => TimeSpan.FromMilliseconds(140),
    };

    private void Play(AudioCue cue)
    {
#if ANDROID
        // Strumień multimediów, ten sam, którym idzie odczyt komunikatów: gracz ustawia
        // głośność raz, przyciskami telefonu, i dotyczy ona całej aplikacji.
        _toneGenerator ??= new Android.Media.ToneGenerator(Android.Media.Stream.Music, 70);

        Android.Media.Tone tone = cue switch
        {
            // Zamknięcie nasłuchu ma brzmieć wyraźnie inaczej niż otwarcie, a nie tylko
            // „podobnie, ale dwa razy": dwa krótkie piknięcia okazały się na urządzeniu
            // nieodróżnialne od jednego. Ton opadający („nie teraz") jest słyszalnie inny
            // od piknięcia otwarcia i od potwierdzenia komendy.
            AudioCue.ListeningStopped => Android.Media.Tone.PropNack,
            AudioCue.CommandAccepted => Android.Media.Tone.PropAck,

            // Krótki pip w roli tykania zegara — inny od piknięcia otwarcia nasłuchu,
            // więc gracz nie pomyli odliczania z zaproszeniem do wydania komendy.
            AudioCue.CountdownTick => Android.Media.Tone.SupPip,
            _ => Android.Media.Tone.PropBeep,
        };

        _toneGenerator.StartTone(tone, (int)GetDuration(cue).TotalMilliseconds);
#else
        // Pozostałe platformy dostaną własną implementację razem z Etapem 16 (iOS).
        _logger.LogDebug("Sygnał {Cue} pominięty — platforma bez generatora tonów.", cue);
#endif
    }
}
