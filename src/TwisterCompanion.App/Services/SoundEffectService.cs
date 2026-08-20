using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Efekty dźwiękowe rozgrywki odtwarzane z plików aplikacji.
/// </summary>
/// <remarks>
/// Na Androidzie używa <c>SoundPool</c>, a nie <c>MediaPlayer</c>, i to jest cała różnica
/// między dźwiękiem na czas a dźwiękiem spóźnionym: <c>SoundPool</c> trzyma rozpakowane
/// próbki w pamięci i odtwarza je bez przygotowywania odtwarzacza, a <c>MediaPlayer</c>
/// zawiązuje się przy każdym użyciu. Przy próbkach krótszych od sekundy to jedyny sensowny
/// wybór — i on też odpowiada za <b>pulę</b> z zadania planu: jeden zbiornik obsługuje kilka
/// dźwięków naraz i sam zwalnia najstarszy strumień.
/// <para>
/// Cisza systemowa jest respektowana wprost: przy dzwonku ustawionym na cichy albo na same
/// wibracje efekty nie idą. Strumień multimediów technicznie by je przepuścił, ale telefon
/// przełączony na cichy w towarzystwie ma być cichy — a wibracje zostają, bo mają własny
/// przełącznik i to jest właśnie ich rola.
/// </para>
/// <para>
/// Awarie są pochłaniane z logiem: brak dźwięku pogarsza wrażenie, ale nie może przerwać
/// partii.
/// </para>
/// </remarks>
internal sealed class SoundEffectService : ISoundService, IDisposable
{
    /// <summary>
    /// Ile dźwięków może brzmieć jednocześnie.
    /// </summary>
    /// <remarks>
    /// Cztery, bo tyle najwyżej może się w tej grze nałożyć: tło poprzedniego efektu,
    /// nowy efekt, stuknięcie przycisku i zapas. Większa pula to tylko więcej zajętej pamięci.
    /// </remarks>
    private const int MaxStreams = 4;

    /// <summary>Nazwy plików próbek — jedna na każdy efekt.</summary>
    private static readonly IReadOnlyDictionary<SoundEffect, string> FileNames =
        new Dictionary<SoundEffect, string>
        {
            [SoundEffect.MoveRevealed] = "sound_move.wav",
            [SoundEffect.EventTriggered] = "sound_event.wav",
            [SoundEffect.PlayerEliminated] = "sound_eliminated.wav",
            [SoundEffect.GameStarted] = "sound_start.wav",
            [SoundEffect.GameFinished] = "sound_finish.wav",
            [SoundEffect.ButtonTap] = "sound_tap.wav",
        };

    private readonly ILogger<SoundEffectService> _logger;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

#if ANDROID
    private readonly Dictionary<SoundEffect, int> _soundIds = [];
    private Android.Media.SoundPool? _pool;
#endif

    private bool _loaded;
    private bool _disposed;

    /// <summary>Tworzy serwis efektów dźwiękowych.</summary>
    /// <param name="logger">Logger.</param>
    public SoundEffectService(ILogger<SoundEffectService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PreloadAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded || _disposed)
        {
            return;
        }

        // Brama, bo wczytanie może zostać wywołane ze startu aplikacji i z pierwszego użycia
        // jednocześnie — a dwa równoległe wczytania dałyby dwie pule i podwójną pamięć.
        await _loadGate.WaitAsync(cancellationToken);

        try
        {
            if (_loaded || _disposed)
            {
                return;
            }

            await LoadAsync(cancellationToken);

            _loaded = true;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Nie udało się wczytać próbek dźwiękowych.");
        }
        finally
        {
            _loadGate.Release();
        }
    }

    /// <inheritdoc />
    public void Play(SoundEffect effect, double volume)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            PlayCore(effect, (float)Math.Clamp(volume, 0.0, 1.0));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Nie udało się odtworzyć efektu {Effect}.", effect);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _loadGate.Dispose();

#if ANDROID
        _pool?.Release();
        _pool?.Dispose();
        _pool = null;
        _soundIds.Clear();
#endif
    }

#if ANDROID
    /// <summary>Tworzy pulę i wczytuje wszystkie próbki z zasobów aplikacji.</summary>
    /// <remarks>
    /// Wczytanie jest w <c>SoundPool</c> asynchroniczne, więc czekamy na zgłoszenia
    /// zakończenia. Bez tego pierwsze odtworzenie po starcie aplikacji byłoby ciszą —
    /// próbka nie byłaby jeszcze gotowa.
    /// </remarks>
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Android.Media.AudioAttributes attributes = new Android.Media.AudioAttributes.Builder()!
            .SetUsage(Android.Media.AudioUsageKind.Game)!
            .SetContentType(Android.Media.AudioContentType.Sonification)!
            .Build()!;

        Android.Media.SoundPool pool = new Android.Media.SoundPool.Builder()
            .SetMaxStreams(MaxStreams)!
            .SetAudioAttributes(attributes)!
            .Build()!;

        TaskCompletionSource<bool> allLoaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int remaining = FileNames.Count;

        pool.LoadComplete += (_, arguments) =>
        {
            if (arguments.Status != 0)
            {
                _logger.LogWarning(
                    "Próbka {SoundId} nie została wczytana (status {Status}).",
                    arguments.SampleId,
                    arguments.Status);
            }

            if (Interlocked.Decrement(ref remaining) == 0)
            {
                allLoaded.TrySetResult(true);
            }
        };

        Android.Content.Res.AssetManager assets = Android.App.Application.Context.Assets
            ?? throw new InvalidOperationException("Brak dostępu do zasobów aplikacji.");

        foreach ((SoundEffect effect, string fileName) in FileNames)
        {
            using Android.Content.Res.AssetFileDescriptor descriptor = assets.OpenFd(fileName)
                ?? throw new InvalidOperationException($"Nie znaleziono próbki {fileName}.");

            _soundIds[effect] = pool.Load(descriptor, priority: 1);
        }

        _pool = pool;

        // Znikoma szansa, że zgłoszenie nie przyjdzie — wtedy i tak ruszamy dalej, a brakująca
        // próbka po prostu nie zabrzmi. Czekanie w nieskończoność zablokowałoby start aplikacji.
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await allLoaded.Task.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Wczytywanie próbek dźwiękowych nie zdążyło się zgłosić.");
        }
    }

    /// <summary>Odtwarza próbkę, jeśli jest wczytana i urządzenie nie jest wyciszone.</summary>
    private void PlayCore(SoundEffect effect, float volume)
    {
        if (_pool is null || !_soundIds.TryGetValue(effect, out int soundId))
        {
            return;
        }

        if (IsDeviceSilenced())
        {
            return;
        }

        _pool.Play(soundId, volume, volume, priority: 1, loop: 0, rate: 1.0f);
    }

    /// <summary>Czy dzwonek urządzenia jest ustawiony na cichy albo na same wibracje.</summary>
    private static bool IsDeviceSilenced()
    {
        using Android.Media.AudioManager? audio = Android.App.Application.Context
            .GetSystemService(Android.Content.Context.AudioService) as Android.Media.AudioManager;

        return audio?.RingerMode is Android.Media.RingerMode.Silent or Android.Media.RingerMode.Vibrate;
    }
#else
    private Task LoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void PlayCore(SoundEffect effect, float volume)
    {
    }
#endif
}
