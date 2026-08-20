using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Voice;

namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Okno nasłuchu złożone z kolejnych sesji rozpoznawania mowy.
/// </summary>
/// <remarks>
/// Przebieg jednego obiegu: sygnał otwarcia → odstęp → mikrofon → sesja → mikrofon zamknięty
/// → odstęp → sygnał zamknięcia → przerwa → od nowa. Pętla kończy się dopiero po rozpoznaniu
/// komendy albo po zamknięciu okna z zewnątrz.
/// <para>
/// <b>Komenda działa na wyniku częściowym.</b> Sesja zamyka się dopiero wtedy, gdy rozpoznawacz
/// uzna, że mówiący skończył, i po odpowiedzi z serwera — czekanie na wynik finalny dodałoby
/// do każdej komendy około sekundy. Wynik częściowy pasujący do komendy jest równie pewny,
/// bo frazy są krótkie i zamknięte.
/// </para>
/// <para>
/// <b>Mikrofon milczy w trakcie mowy aplikacji.</b> Bez tego rozpoznawanie usłyszałoby własny
/// komunikat i potraktowało go jako komendę — a komunikat zawiera słowa z tego samego języka.
/// </para>
/// </remarks>
internal sealed class VoiceControlService : IVoiceControlService, IDisposable, IAsyncDisposable
{
    private readonly ISpeechRecognitionService _recognition;
    private readonly IAudioCueService _audioCues;
    private readonly IVoiceCommandParser _parser;
    private readonly ISettingsService _settings;
    private readonly ILocalizationService _localization;
    private readonly IAnnouncementSpeaker _speaker;
    private readonly TimeProvider _timeProvider;
    private readonly VoiceControlOptions _options;
    private readonly ILogger<VoiceControlService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private CancellationTokenSource? _windowCancellation;
    private Task? _windowLoop;
    private TaskCompletionSource<SessionResult>? _session;
    private SpeechRecognitionMode? _mode;
    private VoiceCommandType? _lastCommand;
    private long _lastCommandAt;
    private bool _disposed;

    /// <summary>Tworzy warstwę nasłuchu komend.</summary>
    /// <param name="recognition">Rozpoznawanie mowy.</param>
    /// <param name="audioCues">Sygnały dźwiękowe stanu mikrofonu.</param>
    /// <param name="parser">Dopasowanie rozpoznanego tekstu do komend.</param>
    /// <param name="settings">Ustawienia aplikacji.</param>
    /// <param name="localization">Serwis tłumaczeń — źródło języka rozpoznawania.</param>
    /// <param name="speaker">Odczyt komunikatów — źródło informacji, kiedy aplikacja mówi.</param>
    /// <param name="timeProvider">Źródło czasu.</param>
    /// <param name="options">Parametry nasłuchu.</param>
    /// <param name="logger">Logger.</param>
    public VoiceControlService(
        ISpeechRecognitionService recognition,
        IAudioCueService audioCues,
        IVoiceCommandParser parser,
        ISettingsService settings,
        ILocalizationService localization,
        IAnnouncementSpeaker speaker,
        TimeProvider timeProvider,
        VoiceControlOptions options,
        ILogger<VoiceControlService> logger)
    {
        ArgumentNullException.ThrowIfNull(recognition);
        ArgumentNullException.ThrowIfNull(audioCues);
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _recognition = recognition;
        _audioCues = audioCues;
        _parser = parser;
        _settings = settings;
        _localization = localization;
        _speaker = speaker;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;

        _recognition.PartialRecognized += OnPartialRecognized;
        _recognition.SessionCompleted += OnSessionCompleted;
        _speaker.SpeakingChanged += OnSpeakingChanged;
    }

    /// <inheritdoc />
    public VoiceControlState State { get; private set; } = VoiceControlState.Disabled;

    /// <inheritdoc />
    public event EventHandler<VoiceCommandType>? CommandRecognized;

    /// <inheritdoc />
    public event EventHandler<VoiceControlState>? StateChanged;

    /// <inheritdoc />
    public async Task<bool> PrepareAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Current.IsVoiceControlEnabled)
        {
            SetState(VoiceControlState.Disabled);

            return false;
        }

        // W trybie automatycznym nie ma czym sterować: tury same następują po sobie, a jedyne
        // zgłoszenie od graczy — odpadnięcie — idzie z przycisku obok imienia. Otwarty mikrofon
        // byłby tam wyłącznie zużyciem baterii i sesji rozpoznawania.
        if (_settings.Current.TurnAdvanceMode == Settings.TurnAdvanceMode.Automatic)
        {
            _logger.LogInformation("Sterowanie głosem nieaktywne — tury następują automatycznie.");

            SetState(VoiceControlState.Disabled);

            return false;
        }

        SpeechRecognitionCapabilities capabilities =
            await _recognition.GetCapabilitiesAsync(cancellationToken);

        if (!capabilities.IsSystemRecognitionAvailable && !capabilities.IsOnDeviceRecognitionAvailable)
        {
            _logger.LogWarning(
                "Urządzenie nie obsługuje rozpoznawania mowy: {Platform}.",
                capabilities.PlatformDescription);

            SetState(VoiceControlState.Unavailable);

            return false;
        }

        if (!await _recognition.RequestPermissionAsync(cancellationToken))
        {
            SetState(VoiceControlState.Disabled);

            return false;
        }

        // Tryb na urządzeniu wygrywa, gdy jest dostępny: nie ma limitów usługi, nie zależy
        // od sieci i nie wysyła głosu poza telefon. Wybór zapada raz, bo w trakcie partii
        // nie ma jak się zmienić.
        _mode = capabilities.IsOnDeviceRecognitionAvailable
            ? SpeechRecognitionMode.OnDevice
            : SpeechRecognitionMode.System;

        _logger.LogInformation(
            "Sterowanie głosem gotowe. Tryb: {Mode}. Urządzenie: {Platform}.",
            _mode,
            capabilities.PlatformDescription);

        SetState(VoiceControlState.Idle);

        return true;
    }

    /// <inheritdoc />
    public async Task OpenWindowAsync(CancellationToken cancellationToken = default)
    {
        if (State is VoiceControlState.Disabled or VoiceControlState.Unavailable)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_windowLoop is { IsCompleted: false })
            {
                return;
            }

            _windowCancellation?.Dispose();
            _windowCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _windowLoop = RunWindowAsync(_windowCancellation.Token);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task CloseWindowAsync()
    {
        Task? loop;

        await _gate.WaitAsync();
        try
        {
            if (_windowCancellation is null)
            {
                return;
            }

            await _windowCancellation.CancelAsync();
            loop = _windowLoop;
            _windowLoop = null;
        }
        finally
        {
            _gate.Release();
        }

        // Czekamy na zakończenie pętli, żeby mikrofon był zamknięty, gdy metoda wraca.
        // Bez tego zaraz po zamknięciu okna mogłaby jeszcze wystartować kolejna sesja.
        if (loop is not null)
        {
            try
            {
                await loop;
            }
            catch (OperationCanceledException)
            {
                // Zamknięcie okna jest zwykłą drogą wyjścia z pętli.
            }
        }

        await _recognition.StopAsync();

        if (State is not (VoiceControlState.Disabled or VoiceControlState.Unavailable))
        {
            SetState(VoiceControlState.Idle);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Droga łagodna: czeka na zamknięcie pętli, więc po powrocie mikrofon jest pewnie wolny.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        Unsubscribe();

        await CloseWindowAsync();

        ReleaseResources();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Kontener zależności w MAUI zwalnia usługi <b>synchronicznie</b> przy zamykaniu
    /// aplikacji, a typ z samym <see cref="IAsyncDisposable"/> zgłasza tam wyjątek. Ta droga
    /// przerywa pętlę nasłuchu bez czekania na jej zakończenie — proces i tak się kończy.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Unsubscribe();

        _windowCancellation?.Cancel();

        ReleaseResources();
    }

    private void Unsubscribe()
    {
        _disposed = true;

        _recognition.PartialRecognized -= OnPartialRecognized;
        _recognition.SessionCompleted -= OnSessionCompleted;
        _speaker.SpeakingChanged -= OnSpeakingChanged;
    }

    private void ReleaseResources()
    {
        _windowCancellation?.Dispose();
        _windowCancellation = null;
        _gate.Dispose();
    }

    /// <summary>Pętla okna nasłuchu.</summary>
    private async Task RunWindowAsync(CancellationToken cancellationToken)
    {
        int throttleStrikes = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Sygnał, potem odstęp, dopiero mikrofon — inaczej rozpoznawanie usłyszy
                // własne piknięcie i zmarnuje na nie całą sesję.
                await _audioCues.PlayAsync(AudioCue.ListeningStarted, cancellationToken);
                await Task.Delay(_options.CueGap, _timeProvider, cancellationToken);

                SetState(VoiceControlState.Listening);

                SessionResult result = await ListenOnceAsync(cancellationToken);

                await _recognition.StopAsync(cancellationToken);
                await Task.Delay(_options.CueGap, _timeProvider, cancellationToken);

                if (result.Command is { } command)
                {
                    await _audioCues.PlayAsync(AudioCue.CommandAccepted, cancellationToken);

                    SetState(VoiceControlState.Idle);
                    CommandRecognized?.Invoke(this, command);

                    return;
                }

                await _audioCues.PlayAsync(AudioCue.ListeningStopped, cancellationToken);
                SetState(VoiceControlState.Waiting);

                bool throttled = result.Error
                    is SpeechRecognitionError.TooManyRequests
                    or SpeechRecognitionError.RecognizerBusy;

                throttleStrikes = throttled ? throttleStrikes + 1 : 0;

                if (throttleStrikes >= _options.MaxThrottleStrikes)
                {
                    _logger.LogWarning(
                        "Usługa rozpoznawania odmawia obsługi ({Error}) — sterowanie głosem"
                        + " wstrzymane do następnej tury.",
                        result.Error);

                    SetState(VoiceControlState.Unavailable);

                    return;
                }

                await Task.Delay(
                    throttled
                        ? _options.ThrottleBackoff * throttleStrikes
                        : _options.SessionRestartDelay,
                    _timeProvider,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Zamknięcie okna — zwykła droga wyjścia.
        }
        catch (Exception exception)
        {
            // Awaria nasłuchu nie może przerwać partii: zostaje sterowanie przyciskami.
            _logger.LogError(exception, "Nasłuch komend głosowych przerwany błędem.");
            SetState(VoiceControlState.Unavailable);
        }
        finally
        {
            // Mikrofon musi zostać zwolniony także wtedy, gdy pętla ginie przez anulowanie —
            // a ginie tak za każdym razem, gdy zmieni się stan partii, bo token okna jest
            // powiązany z tokenem koordynatora. Bez tego rozpoznawanie zostawało włączone:
            // dalej zbierało dźwięk, a urządzenie odzywało się własnymi sygnałami początku
            // i końca nasłuchu w chwili, w której aplikacja uważała mikrofon za zamknięty.
            // Zamknięcie idzie z własnym tokenem, bo tamten jest już anulowany.
            await StopRecognitionQuietlyAsync();

            if (State is VoiceControlState.Listening or VoiceControlState.Waiting)
            {
                SetState(VoiceControlState.Idle);
            }
        }
    }

    /// <summary>Zamyka rozpoznawanie, nie pozwalając awarii wyjść poza pętlę.</summary>
    private async Task StopRecognitionQuietlyAsync()
    {
        try
        {
            await _recognition.StopAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Nie udało się zamknąć sesji rozpoznawania mowy.");
        }
    }

    /// <summary>Przeprowadza jedną sesję rozpoznawania.</summary>
    private async Task<SessionResult> ListenOnceAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource<SessionResult> session =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        _session = session;

        try
        {
            await _recognition.StartAsync(
                new SpeechRecognitionRequest(
                    _localization.CurrentCulture,
                    _mode ?? SpeechRecognitionMode.System,
                    ReportPartialResults: true,
                    AutoStopSilenceTimeout: _options.SilenceTimeout),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _session = null;
            _logger.LogWarning(exception, "Nie udało się otworzyć sesji rozpoznawania mowy.");

            return new SessionResult(null, SpeechRecognitionError.Other);
        }

        return await session.Task.WaitAsync(cancellationToken);
    }

    private void OnPartialRecognized(object? sender, string text)
    {
        if (_session is null || !TryMatchCommand(text, out VoiceCommandType command))
        {
            return;
        }

        _logger.LogInformation("Komenda „{Command}” rozpoznana w tekście: {Text}", command, text);

        // Wynik częściowy wystarcza — nie czekamy na zamknięcie sesji przez rozpoznawacz.
        _session.TrySetResult(new SessionResult(command, SpeechRecognitionError.None));
    }

    private void OnSessionCompleted(object? sender, SpeechRecognitionOutcome outcome)
    {
        TaskCompletionSource<SessionResult>? session = _session;

        if (session is null)
        {
            return;
        }

        // Wynik finalny bierzemy pod uwagę tylko wtedy, gdy żaden częściowy nie zawierał
        // komendy: na wolniejszych urządzeniach wyniki częściowe potrafią wcale nie przyjść.
        if (outcome.IsSuccessful && TryMatchCommand(outcome.Text, out VoiceCommandType command))
        {
            session.TrySetResult(new SessionResult(command, SpeechRecognitionError.None));

            return;
        }

        session.TrySetResult(new SessionResult(null, outcome.Error));
    }

    /// <summary>
    /// Zamyka mikrofon, gdy aplikacja zaczyna mówić.
    /// </summary>
    /// <remarks>
    /// Wyciszenie mikrofonu na czas mowy jest wymogiem, nie optymalizacją: komunikat „Kuba,
    /// prawa ręka — czerwony" zawiera słowa, które rozpoznawanie potrafi dopasować do komend.
    /// </remarks>
    private void OnSpeakingChanged(object? sender, bool isSpeaking)
    {
        if (isSpeaking && _windowLoop is { IsCompleted: false })
        {
            _ = CloseWindowAsync();
        }
    }

    private bool TryMatchCommand(string? text, out VoiceCommandType command)
    {
        command = default;

        if (!_parser.TryParse(text, out VoiceCommandType parsed))
        {
            return false;
        }

        if (IsRepeatWithinDebounceWindow(parsed))
        {
            _logger.LogDebug("Komenda {Command} pominięta — powtórzenie w oknie wyciszenia.", parsed);

            return false;
        }

        _lastCommand = parsed;
        _lastCommandAt = _timeProvider.GetTimestamp();
        command = parsed;

        return true;
    }

    /// <summary>Czy ta sama komenda padła przed chwilą.</summary>
    private bool IsRepeatWithinDebounceWindow(VoiceCommandType command) =>
        _lastCommand == command
        && _timeProvider.GetElapsedTime(_lastCommandAt) < _options.DebounceWindow;

    private void SetState(VoiceControlState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke(this, state);
    }

    /// <summary>Wynik jednej sesji rozpoznawania.</summary>
    private sealed record SessionResult(VoiceCommandType? Command, SpeechRecognitionError Error);
}
