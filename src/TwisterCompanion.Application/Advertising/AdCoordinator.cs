using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Advertising;

/// <summary>
/// Rytm reklam: baner tylko na ekranie rozgrywki, pełnoekranowa co N zakończoną partię.
/// </summary>
/// <remarks>
/// Reklama pełnoekranowa nie pada w chwili zakończenia partii, choć technicznie mogłaby:
/// silnik zgłasza wtedy koniec gry i <b>zaczyna czytać</b> komunikat o wyniku. Reklama weszłaby
/// mu w słowo i zabrała dźwięk. Koordynator czeka więc na koniec zapowiedzi i dopiero wtedy
/// pyta o reklamę — a jeśli w tym czasie gracze zaczną kolejną partię albo zejdą z ekranu,
/// rezygnuje.
/// <para>
/// Licznik zakończonych partii jest w ustawieniach, więc przeżywa restart aplikacji. Inaczej
/// zamknięcie aplikacji zerowałoby odliczanie i reklama mogłaby wracać po każdej partii —
/// dokładnie to, czego użytkownik nie chce.
/// </para>
/// </remarks>
internal sealed class AdCoordinator : IAdCoordinator, IDisposable
{
    private readonly IAdService _ads;
    private readonly IGameEngine _engine;
    private readonly IAnnouncementSpeaker _speaker;
    private readonly ISettingsService _settings;
    private readonly AdOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AdCoordinator> _logger;

    private bool _isActive;
    private bool _bannerAllowed;
    private bool _disposed;

    /// <summary>Tworzy koordynator reklam.</summary>
    /// <param name="ads">Reklamy z regułami.</param>
    /// <param name="engine">Silnik rozgrywki — źródło informacji o zakończeniu partii.</param>
    /// <param name="speaker">Odczyt komunikatów — reklama czeka na jego koniec.</param>
    /// <param name="settings">Ustawienia — trwały licznik zakończonych partii.</param>
    /// <param name="options">Parametry reklam.</param>
    /// <param name="timeProvider">Źródło czasu.</param>
    /// <param name="logger">Logger.</param>
    public AdCoordinator(
        IAdService ads,
        IGameEngine engine,
        IAnnouncementSpeaker speaker,
        ISettingsService settings,
        AdOptions options,
        TimeProvider timeProvider,
        ILogger<AdCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(ads);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _ads = ads;
        _engine = engine;
        _speaker = speaker;
        _settings = settings;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsBannerAllowed => _bannerAllowed;

    /// <inheritdoc />
    public event EventHandler<bool>? BannerAllowedChanged;

    /// <inheritdoc />
    public async Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        if (_isActive || _disposed)
        {
            return;
        }

        _isActive = true;
        _engine.GameFinished += OnGameFinished;

        // Baner pojawia się dopiero po przygotowaniu zestawu SDK i zgody — inaczej ekran
        // trzymałby puste miejsce po reklamie, której nie wolno pokazać.
        bool allowed = await _ads.PrepareAsync(cancellationToken);

        SetBannerAllowed(allowed && _isActive);
    }

    /// <inheritdoc />
    public Task DeactivateAsync()
    {
        if (!_isActive)
        {
            return Task.CompletedTask;
        }

        _isActive = false;
        _engine.GameFinished -= OnGameFinished;

        SetBannerAllowed(false);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _engine.GameFinished -= OnGameFinished;
    }

    private void OnGameFinished(object? sender, GameSummary summary) => _ = HandleFinishedGameAsync();

    private async Task HandleFinishedGameAsync()
    {
        try
        {
            int finishedGames = await CountFinishedGameAsync();

            if (finishedGames % _options.InterstitialEveryNthGame != 0)
            {
                _logger.LogDebug(
                    "Reklama pełnoekranowa pominięta — partia {Count} z {Every}.",
                    finishedGames % _options.InterstitialEveryNthGame,
                    _options.InterstitialEveryNthGame);

                return;
            }

            if (!await WaitForSilenceAsync())
            {
                return;
            }

            // Warunki sprawdzamy jeszcze raz: przez czas zapowiedzi gracze mogli zejść
            // z ekranu albo rozpocząć kolejną partię.
            if (!_isActive || _engine.State != GameState.Finished)
            {
                return;
            }

            await _ads.ShowInterstitialAsync();
        }
        catch (Exception exception)
        {
            // Reklama nie może zepsuć końca partii — podsumowanie jest ważniejsze.
            _logger.LogError(exception, "Nie udało się obsłużyć reklamy po zakończeniu partii.");
        }
    }

    /// <summary>Zwiększa trwały licznik zakończonych partii i zwraca nową wartość.</summary>
    private async Task<int> CountFinishedGameAsync()
    {
        int finishedGames = _settings.Current.FinishedGamesCount + 1;

        await _settings.UpdateAsync(settings => settings with { FinishedGamesCount = finishedGames });

        return finishedGames;
    }

    /// <summary>
    /// Czeka na koniec zapowiedzi głosowej.
    /// </summary>
    /// <returns><see langword="false"/>, gdy zapowiedź nie zamilkła w wyznaczonym czasie.</returns>
    private async Task<bool> WaitForSilenceAsync()
    {
        if (!_speaker.IsSpeaking)
        {
            return true;
        }

        TaskCompletionSource silence = new(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnSpeakingChanged(object? sender, bool isSpeaking)
        {
            if (!isSpeaking)
            {
                silence.TrySetResult();
            }
        }

        _speaker.SpeakingChanged += OnSpeakingChanged;

        try
        {
            if (!_speaker.IsSpeaking)
            {
                return true;
            }

            // Limit czasu odmierza sterowane źródło czasu, a nie zegar systemowy — inaczej
            // test tej reguły musiałby naprawdę czekać piętnaście sekund.
            using CancellationTokenSource timeout = new(_options.SpeechWaitTimeout, _timeProvider);

            await silence.Task.WaitAsync(timeout.Token);

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Reklama pełnoekranowa pominięta — zapowiedź końca partii nie zamilkła w ciągu"
                + " {Seconds} s.",
                _options.SpeechWaitTimeout.TotalSeconds);

            return false;
        }
        finally
        {
            _speaker.SpeakingChanged -= OnSpeakingChanged;
        }
    }

    private void SetBannerAllowed(bool allowed)
    {
        if (_bannerAllowed == allowed)
        {
            return;
        }

        _bannerAllowed = allowed;
        BannerAllowedChanged?.Invoke(this, allowed);
    }
}
