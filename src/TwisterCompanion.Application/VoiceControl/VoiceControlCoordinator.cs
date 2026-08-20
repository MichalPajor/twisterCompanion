using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Decyduje, kiedy nasłuchiwać, i wykonuje rozpoznane komendy.
/// </summary>
/// <remarks>
/// Rytm nasłuchu wynika z rytmu gry: po odczytaniu komunikatu gracze mają czas na wykonanie
/// ruchu i dopiero potem otwiera się okno nasłuchu. Nasłuch <b>nie</b> startuje zaraz po
/// odczycie, choć technicznie byłby możliwy — mikrofon otwarty w trakcie układania ręki na
/// macie zbierałby wyłącznie sapanie i śmiech, zużywając sesje rozpoznawania na nic.
/// <para>
/// Na pauzie okno otwiera się od razu, bez odczekiwania: gracze nie wykonują wtedy żadnego
/// ruchu, a jedyne, co mogą chcieć zrobić, to wznowić grę.
/// </para>
/// <para>
/// W trybie automatycznym sterowanie głosem w ogóle się nie włącza — decyzję podejmuje
/// <see cref="IVoiceControlService.PrepareAsync"/>, bo to ona zna ustawienia.
/// </para>
/// </remarks>
internal sealed class VoiceControlCoordinator : IVoiceControlCoordinator, IDisposable
{
    private readonly IVoiceControlService _voiceControl;
    private readonly IGameEngine _engine;
    private readonly IAnnouncementSpeaker _speaker;
    private readonly VoiceControlOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<VoiceControlCoordinator> _logger;

    private CancellationTokenSource? _pendingWindow;
    private bool _disposed;

    /// <summary>Tworzy koordynator sterowania głosem.</summary>
    /// <param name="voiceControl">Nasłuch komend.</param>
    /// <param name="engine">Silnik rozgrywki — odbiorca komend.</param>
    /// <param name="speaker">Odczyt komunikatów — źródło informacji, kiedy aplikacja mówi.</param>
    /// <param name="options">Parametry nasłuchu.</param>
    /// <param name="timeProvider">Źródło czasu — odmierza czas na wykonanie ruchu.</param>
    /// <param name="logger">Logger.</param>
    public VoiceControlCoordinator(
        IVoiceControlService voiceControl,
        IGameEngine engine,
        IAnnouncementSpeaker speaker,
        VoiceControlOptions options,
        TimeProvider timeProvider,
        ILogger<VoiceControlCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(voiceControl);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _voiceControl = voiceControl;
        _engine = engine;
        _speaker = speaker;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsActive { get; private set; }

    /// <inheritdoc />
    public async Task<bool> ActivateAsync(CancellationToken cancellationToken = default)
    {
        if (IsActive)
        {
            return true;
        }

        if (!await _voiceControl.PrepareAsync(cancellationToken))
        {
            return false;
        }

        _engine.StateChanged += OnEngineStateChanged;
        _speaker.SpeakingChanged += OnSpeakingChanged;
        _voiceControl.CommandRecognized += OnCommandRecognized;
        IsActive = true;

        // Ekran rozgrywki bywa otwierany w trakcie trwającej partii — stan trzeba odczytać
        // od razu, a nie czekać na jego następną zmianę.
        ScheduleForState(_engine.State);

        return true;
    }

    /// <inheritdoc />
    public async Task DeactivateAsync()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        _engine.StateChanged -= OnEngineStateChanged;
        _speaker.SpeakingChanged -= OnSpeakingChanged;
        _voiceControl.CommandRecognized -= OnCommandRecognized;

        CancelPendingWindow();

        await _voiceControl.CloseWindowAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _engine.StateChanged -= OnEngineStateChanged;
        _speaker.SpeakingChanged -= OnSpeakingChanged;
        _voiceControl.CommandRecognized -= OnCommandRecognized;

        CancelPendingWindow();
        _pendingWindow?.Dispose();
    }

    private void OnEngineStateChanged(object? sender, GameState state) => ScheduleForState(state);

    /// <summary>
    /// Wstrzymuje nasłuch na czas mowy aplikacji i wznawia go po jej zakończeniu.
    /// </summary>
    /// <remarks>
    /// Bez wznowienia po zakończeniu mowy zapowiedź stanu („Pauza", „Wznawiamy") byłaby
    /// końcem sterowania głosem: mikrofon zamknąłby się na czas zapowiedzi i nic już nie
    /// otworzyłoby go z powrotem, bo stan rozgrywki się nie zmienia.
    /// </remarks>
    private void OnSpeakingChanged(object? sender, bool isSpeaking)
    {
        if (isSpeaking)
        {
            CancelPendingWindow();

            return;
        }

        ScheduleForState(_engine.State);
    }

    /// <summary>Ustawia nasłuch odpowiednio do stanu rozgrywki.</summary>
    private void ScheduleForState(GameState state)
    {
        CancelPendingWindow();

        switch (state)
        {
            case GameState.AwaitingPlayerAction:
            case GameState.Paused:
                _pendingWindow = new CancellationTokenSource();
                _ = OpenWindowAsync(GetDelay(state), _pendingWindow.Token);

                break;

            default:
                // Odczyt komunikatu, rozpoczęcie i koniec partii — mikrofon musi milczeć.
                _ = _voiceControl.CloseWindowAsync();

                break;
        }
    }

    /// <summary>Po jakim czasie otworzyć okno nasłuchu w danym stanie.</summary>
    /// <remarks>
    /// Odpowiedź daje <b>odliczanie silnika</b>, a nie własny licznik koordynatora: trwające
    /// odliczanie ruchu znaczy dokładnie tyle, że gracze układają teraz ręce na macie. Mikrofon
    /// otwiera się więc w tej samej chwili, w której liczba na ekranie dobiega zera.
    /// <para>
    /// Wcześniej stała tu flaga ustawiana przy nowo rozegranej turze i to był błąd zgłoszony
    /// z urządzenia: <b>wznowienie partii uruchamia odliczanie od nowa</b>, ale nową turą nie
    /// jest, więc flaga pozostawała opuszczona i nasłuch ruszał od razu — z sygnałami
    /// dźwiękowymi w trakcie czasu przeznaczonego na ruch. Odliczanie zna wszystkie przypadki
    /// naraz: nową turę, wznowienie i pauzę, na której odliczania nie ma.
    /// </para>
    /// <para>
    /// Po komendzie „Powtórz" czekania nadal nie ma — powtórzenie nie dotyka odliczania,
    /// a skoro nasłuch był otwarty, to znaczy, że czas na ruch już minął.
    /// </para>
    /// </remarks>
    private TimeSpan GetDelay(GameState state)
    {
        if (state != GameState.AwaitingPlayerAction
            || _engine.Countdown is not { Kind: TurnCountdownKind.Move } countdown)
        {
            return _options.WindowSettleDelay;
        }

        TimeSpan remaining = countdown.Total - _timeProvider.GetElapsedTime(countdown.StartedAt);

        return remaining > _options.WindowSettleDelay ? remaining : _options.WindowSettleDelay;
    }

    /// <summary>Otwiera okno nasłuchu po zadanym czasie.</summary>
    private async Task OpenWindowAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, _timeProvider, cancellationToken);

            await _voiceControl.OpenWindowAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Stan rozgrywki albo mowa aplikacji zmieniły sytuację przed otwarciem okna.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Nie udało się otworzyć okna nasłuchu.");
        }
    }

    /// <summary>
    /// Wykonuje rozpoznaną komendę.
    /// </summary>
    /// <remarks>
    /// Każda komenda prowadzi do tej samej metody silnika, co odpowiadający jej przycisk —
    /// dlatego szeregowanie operacji, pomijanie ich w niewłaściwym stanie i zapis partii
    /// działają identycznie niezależnie od tego, czy gracz kliknął, czy powiedział.
    /// </remarks>
    private void OnCommandRecognized(object? sender, VoiceCommandType command) =>
        _ = ExecuteCommandAsync(command);

    private async Task ExecuteCommandAsync(VoiceCommandType command)
    {
        try
        {
            Task operation = command switch
            {
                VoiceCommandType.Next => _engine.NextTurnAsync(),
                VoiceCommandType.Repeat => _engine.RepeatAsync(),
                VoiceCommandType.Pause => _engine.PauseAsync(),
                VoiceCommandType.Resume => _engine.ResumeAsync(),
                _ => Task.CompletedTask,
            };

            await operation;
        }
        catch (Exception exception)
        {
            // Nieudana komenda nie może przerwać partii — zostaje sterowanie przyciskami.
            _logger.LogError(exception, "Wykonanie komendy {Command} nie udało się.", command);
        }
    }

    private void CancelPendingWindow()
    {
        if (_pendingWindow is null)
        {
            return;
        }

        _pendingWindow.Cancel();
        _pendingWindow.Dispose();
        _pendingWindow = null;
    }
}
