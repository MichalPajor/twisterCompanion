using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Application.Voice;

/// <summary>
/// Odczyt komunikatów z zasadą „nowa wypowiedź przerywa poprzednią".
/// </summary>
/// <remarks>
/// Zasada jest świadomym wyborem zamiast kolejkowania. Gdyby wypowiedzi stały w kolejce,
/// komenda „Powtórz" czekałaby na dokończenie zdania, które właśnie chcemy powtórzyć —
/// a przy szybkim klikaniu „Dalej" aplikacja odczytywałaby ruchy z opóźnieniem rosnącym
/// z każdym kliknięciem. Ostatnie polecenie jest zawsze tym, które słyszą gracze.
/// <para>
/// Awarie syntezatora są pochłaniane i zapisywane w logu. Brak mowy pogarsza doświadczenie,
/// ale <b>nie może zatrzymać rozgrywki</b> — tekst jest widoczny na ekranie niezależnie
/// od tego, czy udało się go wypowiedzieć.
/// </para>
/// </remarks>
internal sealed class AnnouncementSpeaker : IAnnouncementSpeaker, IDisposable
{
    private readonly ITextToSpeechService _textToSpeech;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AnnouncementSpeaker> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private CancellationTokenSource? _currentUtterance;
    private bool _disposed;

    /// <summary>Tworzy warstwę odczytu komunikatów.</summary>
    /// <param name="textToSpeech">Syntezator mowy urządzenia.</param>
    /// <param name="settingsService">Ustawienia — źródło głosu, tempa i wysokości.</param>
    /// <param name="logger">Logger.</param>
    public AnnouncementSpeaker(
        ITextToSpeechService textToSpeech,
        ISettingsService settingsService,
        ILogger<AnnouncementSpeaker> logger)
    {
        ArgumentNullException.ThrowIfNull(textToSpeech);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(logger);

        _textToSpeech = textToSpeech;
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsSpeaking { get; private set; }

    /// <inheritdoc />
    public event EventHandler<bool>? SpeakingChanged;

    /// <inheritdoc />
    public async Task SpeakAsync(
        Announcement announcement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(announcement);

        if (!_settingsService.Current.IsTextToSpeechEnabled)
        {
            return;
        }

        await SilenceAsync();

        CancellationTokenSource utterance =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await _gate.WaitAsync(cancellationToken);

        try
        {
            _currentUtterance = utterance;
            SetSpeaking(true);

            await _textToSpeech.SpeakAsync(announcement.Text, BuildRequest(), utterance.Token);
        }
        catch (OperationCanceledException)
        {
            // Przerwane celowo — nowa wypowiedź albo cisza na żądanie.
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Nie udało się odczytać komunikatu. Rozgrywka toczy się dalej bez głosu.");
        }
        finally
        {
            _currentUtterance = null;
            SetSpeaking(false);
            utterance.Dispose();
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _textToSpeech.PrepareAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // Rozgrzanie silnika mowy jest przyspieszeniem, nie warunkiem działania.
            _logger.LogWarning(exception, "Nie udało się przygotować syntezatora mowy.");
        }
    }

    /// <inheritdoc />
    public async Task SilenceAsync()
    {
        _currentUtterance?.Cancel();

        try
        {
            await _textToSpeech.StopAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Nie udało się przerwać wypowiedzi.");
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

        _currentUtterance?.Dispose();
        _gate.Dispose();
    }

    private SpeechRequest BuildRequest()
    {
        Settings.AppSettings settings = _settingsService.Current;

        return new SpeechRequest(settings.PreferredVoiceId, settings.SpeechRate, settings.SpeechPitch);
    }

    private void SetSpeaking(bool value)
    {
        if (IsSpeaking == value)
        {
            return;
        }

        IsSpeaking = value;
        SpeakingChanged?.Invoke(this, value);
    }
}
