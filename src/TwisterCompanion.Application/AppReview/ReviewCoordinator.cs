using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Advertising;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.AppReview;

/// <summary>
/// Rytm prośby o ocenę: po liczącej się partii, gdy reguły pozwalają, i nigdy razem z reklamą.
/// </summary>
/// <remarks>
/// Koordynator nie słucha końca partii bezpośrednio, tylko decyzji o reklamie
/// (<see cref="IAdCoordinator.PostGameAdResolved"/>). Słuchając obu zdarzeń niezależnie,
/// ścigałby się z reklamą o to samo przejście. Tak kolejność jest pewna: najpierw reklama,
/// potem — jeśli jej nie było — prośba o ocenę. Gdy reklama była, prośba czeka na następną
/// liczącą się partię; licznik w ustawieniach rośnie dalej, więc nic nie przepada.
/// <para>
/// Jak reklama, prośba czeka na koniec zapowiedzi wyniku i rezygnuje, gdy w tym czasie
/// gracze zejdą z ekranu albo zaczną kolejną partię.
/// </para>
/// </remarks>
internal sealed class ReviewCoordinator : IReviewCoordinator, IDisposable
{
    private readonly IReviewPlatform _platform;
    private readonly IAdCoordinator _ads;
    private readonly IGameEngine _engine;
    private readonly IAnnouncementSpeaker _speaker;
    private readonly ISettingsService _settings;
    private readonly ReviewOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReviewCoordinator> _logger;

    private bool _isActive;
    private bool _disposed;

    /// <summary>Tworzy koordynator prośby o ocenę.</summary>
    /// <param name="platform">Okienko oceny ze sklepu.</param>
    /// <param name="ads">Koordynator reklam — źródło informacji o zakończonej partii.</param>
    /// <param name="engine">Silnik rozgrywki — stan partii po zapowiedzi.</param>
    /// <param name="speaker">Odczyt komunikatów — prośba czeka na jego koniec.</param>
    /// <param name="settings">Ustawienia — trwały stan prośby.</param>
    /// <param name="options">Parametry prośby.</param>
    /// <param name="timeProvider">Źródło czasu.</param>
    /// <param name="logger">Logger.</param>
    public ReviewCoordinator(
        IReviewPlatform platform,
        IAdCoordinator ads,
        IGameEngine engine,
        IAnnouncementSpeaker speaker,
        ISettingsService settings,
        ReviewOptions options,
        TimeProvider timeProvider,
        ILogger<ReviewCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(ads);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _platform = platform;
        _ads = ads;
        _engine = engine;
        _speaker = speaker;
        _settings = settings;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ActivateAsync()
    {
        if (_isActive || _disposed)
        {
            return Task.CompletedTask;
        }

        _isActive = true;
        _ads.PostGameAdResolved += OnPostGameAdResolved;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeactivateAsync()
    {
        if (!_isActive)
        {
            return Task.CompletedTask;
        }

        _isActive = false;
        _ads.PostGameAdResolved -= OnPostGameAdResolved;

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
        _ads.PostGameAdResolved -= OnPostGameAdResolved;
    }

    private void OnPostGameAdResolved(object? sender, PostGameAdOutcome outcome) =>
        _ = HandleFinishedGameAsync(outcome);

    private async Task HandleFinishedGameAsync(PostGameAdOutcome outcome)
    {
        try
        {
            if (!ReviewPolicy.CountsTowardsReview(outcome.Summary, _options))
            {
                return;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();

            await _settings.UpdateAsync(settings => ReviewPolicy.RecordQualifyingGame(settings, now));

            if (!ReviewPolicy.IsRequestDue(_settings.Current, now, _options))
            {
                return;
            }

            if (outcome.InterstitialShown)
            {
                _logger.LogInformation("Prośba o ocenę odłożona — po tej partii była reklama.");

                return;
            }

            if (!_platform.IsAvailable)
            {
                return;
            }

            if (!await _speaker.WaitForSilenceAsync(_options.SpeechWaitTimeout, _timeProvider))
            {
                _logger.LogWarning(
                    "Prośba o ocenę odłożona — zapowiedź końca partii nie zamilkła w ciągu {Seconds} s.",
                    _options.SpeechWaitTimeout.TotalSeconds);

                return;
            }

            // Przez czas zapowiedzi gracze mogli zejść z ekranu albo zacząć kolejną partię.
            if (!_isActive || _engine.State != GameState.Finished)
            {
                return;
            }

            await _platform.RequestReviewAsync();

            // Zapis po wywołaniu, nie przed: gdy wywołanie się wywróci, prośba wróci przy
            // następnej partii, zamiast przepaść na trzydzieści dni.
            await _settings.UpdateAsync(settings => ReviewPolicy.RecordRequest(settings, now));

            _logger.LogInformation("Poproszono sklep o okienko oceny.");
        }
        catch (Exception exception)
        {
            // Prośba o ocenę nie może zepsuć końca partii.
            _logger.LogError(exception, "Nie udało się obsłużyć prośby o ocenę po zakończeniu partii.");
        }
    }
}
