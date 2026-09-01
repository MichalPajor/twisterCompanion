using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.EventSelection;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Application.Game;

/// <summary>
/// Silnik rozgrywki.
/// </summary>
/// <remarks>
/// Klasa orkiestruje: prowadzi partię przez potok kroków, pilnuje stanu i rozgłasza
/// zdarzenia. Reguły gry siedzą w <see cref="GameSession"/>, losowanie w strategii,
/// składanie komunikatów w <see cref="IAnnouncementBuilder"/> — silnik ich nie duplikuje.
/// <para>
/// <b>Szeregowanie operacji.</b> Wszystkie operacje przechodzą przez semafor. Powód nie
/// jest teoretyczny: od Etapu 8 „Dalej" można wywołać przyciskiem i głosem jednocześnie,
/// a w trybie automatycznym dochodzi jeszcze timer. Bez szeregowania dwa równoległe
/// wywołania rozegrałyby dwie tury i pominęły gracza.
/// </para>
/// <para>
/// <b>Zdarzenia są zgłaszane po zwolnieniu semafora.</b> Subskrybent może w reakcji
/// wywołać kolejną operację silnika — gdyby zdarzenie leciało w sekcji krytycznej,
/// czekałby na semafor, który sam trzyma.
/// </para>
/// <para>
/// <b>Czas przez <see cref="TimeProvider"/></b>, a nie <c>DateTimeOffset.UtcNow</c> i
/// <c>System.Timers</c>. Tylko tak da się przetestować tryb automatyczny bez czekania
/// w teście ośmiu sekund na turę.
/// </para>
/// </remarks>
internal sealed class GameEngine : IGameEngine, IDisposable
{
    private readonly IReadOnlyList<ITurnPipelineStep> _steps;
    private readonly IAnnouncementBuilder _announcementBuilder;
    private readonly IAnnouncementSpeaker _speaker;
    private readonly IGameSessionRepository _sessionRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GameEngine> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private GameConfiguration? _configuration;
    private DateTimeOffset _startedAt;
    private ITimer? _moveTimer;

    /// <summary>Przerwanie sekwencji trwającej tury — odczytów i odliczań.</summary>
    /// <remarks>
    /// Tura nie jest pojedynczą operacją, tylko sekwencją rozciągniętą w czasie: wywołanie
    /// gracza, przerwa, wydarzenie, czas na jego wykonanie, polecenie ruchu. Trwa kilkanaście
    /// sekund i biegnie <b>poza</b> sekcją krytyczną, żeby przyciski i komendy głosowe działały
    /// w jej trakcie. Skoro tak, to musi też dać się przerwać: bez tego wstrzymanie albo
    /// zakończenie partii milkło tylko na chwilę, a zaraz potem aplikacja dokańczała sekwencję,
    /// która straciła sens — zgłoszone z urządzenia jako komunikaty odzywające się po wyjściu
    /// z ekranu rozgrywki.
    /// </remarks>
    private CancellationTokenSource? _turnSequence;

    private bool _disposed;

    /// <summary>Tworzy silnik rozgrywki.</summary>
    /// <param name="steps">Kroki potoku tury, w kolejności rejestracji w kontenerze.</param>
    /// <param name="announcementBuilder">Budowanie komunikatów dla graczy.</param>
    /// <param name="speaker">Odczyt komunikatów na głos.</param>
    /// <param name="sessionRepository">Zapis stanu partii.</param>
    /// <param name="timeProvider">Źródło czasu.</param>
    /// <param name="logger">Logger silnika.</param>
    public GameEngine(
        IEnumerable<ITurnPipelineStep> steps,
        IAnnouncementBuilder announcementBuilder,
        IAnnouncementSpeaker speaker,
        IGameSessionRepository sessionRepository,
        TimeProvider timeProvider,
        ILogger<GameEngine> logger)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(announcementBuilder);
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(sessionRepository);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _steps = [.. steps];
        _announcementBuilder = announcementBuilder;
        _speaker = speaker;
        _sessionRepository = sessionRepository;
        _timeProvider = timeProvider;
        _logger = logger;

        if (_steps.Count == 0)
        {
            throw new ArgumentException("Potok tury nie zawiera żadnego kroku.", nameof(steps));
        }
    }

    /// <inheritdoc />
    public GameState State => Session?.State ?? GameState.Idle;

    /// <inheritdoc />
    public GameSession? Session { get; private set; }

    /// <inheritdoc />
    public Announcement? LastAnnouncement { get; private set; }

    /// <inheritdoc />
    public TurnCountdown? Countdown { get; private set; }

    /// <inheritdoc />
    public bool IsEliminationEnabled =>
        _configuration?.EliminationRule != EliminationRule.NoElimination;

    /// <inheritdoc />
    public event EventHandler<GameState>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<Turn>? TurnPlayed;

    /// <inheritdoc />
    public event EventHandler<Announcement>? AnnouncementRaised;

    /// <inheritdoc />
    public event EventHandler<TurnCountdown?>? CountdownChanged;

    /// <inheritdoc />
    public event EventHandler<GameSummary>? GameFinished;

    /// <inheritdoc />
    public async Task StartAsync(
        GameConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        await InterruptTurnSequenceAsync();

        Announcement startAnnouncement;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            StopMoveCountdown();

            _configuration = configuration;
            _startedAt = _timeProvider.GetUtcNow();

            Session = new GameSession(
                configuration.Players,
                configuration.MoveSelectionOptions.HistoryLength);

            Session.Start();

            startAnnouncement = _announcementBuilder.BuildGameStart();
            LastAnnouncement = startAnnouncement;

            _logger.LogInformation(
                "Rozpoczęta partia: {PlayerCount} graczy, tryb postępu {Mode}.",
                configuration.Players.Count,
                configuration.TurnAdvanceMode);
        }
        finally
        {
            _gate.Release();
        }

        RaiseStateChanged();
        await RaiseAndSpeakAsync(startAnnouncement, cancellationToken);

        await NextTurnAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task NextTurnAsync(CancellationToken cancellationToken = default)
    {
        Turn? turn = null;
        Announcement? announcement = null;
        Announcement? eventAnnouncement = null;
        Announcement? playerAnnouncement = null;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Session is null || _configuration is null)
            {
                return;
            }

            // Ignorujemy, a nie zgłaszamy błąd: komenda głosowa albo timer mogą przyjść
            // w trakcie ogłaszania ruchu lub na pauzie, i nie jest to sytuacja wyjątkowa.
            if (Session.State is not (GameState.Starting or GameState.AwaitingPlayerAction))
            {
                _logger.LogDebug("Pominięte przejście do następnej tury w stanie {State}.", Session.State);

                return;
            }

            StopMoveCountdown();

            TurnContext context = new()
            {
                Session = Session,
                MoveSelectionOptions = _configuration.MoveSelectionOptions,
                EventPack = _configuration.EventPack,
                EventSelectionOptions = _configuration.EventSelectionOptions,
            };

            foreach (ITurnPipelineStep step in _steps)
            {
                await step.ExecuteAsync(context, cancellationToken);
            }

            turn = context.Turn;
            announcement = context.Announcement;
            eventAnnouncement = context.EventAnnouncement;

            if (turn is not null)
            {
                playerAnnouncement = _announcementBuilder.BuildPlayerTurn(turn.Player);
            }

            if (announcement is not null)
            {
                LastAnnouncement = announcement;
            }

        }
        finally
        {
            _gate.Release();
        }

        if (turn is not null)
        {
            TurnPlayed?.Invoke(this, turn);
        }

        // Publikowanie tury odbywa się POZA sekcją krytyczną, bo obejmuje odczyt głosowy
        // i odliczanie zadania, które razem trwają kilkanaście sekund. Stan partii pozostaje
        // przez ten czas na AnnouncingTurn, więc „Dalej" — z przycisku albo z komendy
        // głosowej — jest ignorowane, dopóki gracze nie usłyszą, co mają zrobić.
        //
        // Przebieg: wywołanie gracza → przerwa → wydarzenie → czas na jego wykonanie → ruch.
        // Gracz musi wiedzieć, że to jego kolej, zanim usłyszy polecenie, a wydarzenie zmienia
        // sposób wykonania tury, więc pada przed ruchem i dostaje własny czas.
        //
        // Cała sekwencja jedzie na własnym tokenie, żeby wstrzymanie i zakończenie partii
        // mogły ją przerwać w dowolnym miejscu — patrz komentarz przy _turnSequence.
        CancellationTokenSource sequence =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        Interlocked.Exchange(ref _turnSequence, sequence)?.Dispose();

        try
        {
            await SpeakStepAsync(playerAnnouncement, sequence.Token);

            if (playerAnnouncement is not null)
            {
                await DelayAsync(_configuration?.NameAnnouncementPause, sequence.Token);
            }

            if (eventAnnouncement is not null)
            {
                await SpeakStepAsync(eventAnnouncement, sequence.Token);
                await RunCountdownAsync(TurnCountdownKind.Task, _configuration?.TaskTime, sequence.Token);
            }

            await SpeakStepAsync(announcement, sequence.Token);

            await CompleteTurnAsync(sequence.Token);

            RaiseStateChanged();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Partia została w trakcie tury wstrzymana albo zakończona. Reszta sekwencji
            // straciła sens — i to jest zwykła droga wyjścia, nie awaria.
            _logger.LogDebug("Sekwencja tury przerwana zmianą stanu partii.");
        }
        finally
        {
            Interlocked.CompareExchange(ref _turnSequence, null, sequence);
            sequence.Dispose();
        }
    }

    /// <summary>Czeka podany czas, jeśli jest dodatni.</summary>
    private async Task DelayAsync(TimeSpan? delay, CancellationToken cancellationToken)
    {
        if (delay is { Ticks: > 0 })
        {
            await Task.Delay(delay.Value, _timeProvider, cancellationToken);
        }
    }

    /// <summary>
    /// Odmierza czas, pokazując graczom, ile go zostało.
    /// </summary>
    /// <remarks>
    /// Odliczanie jest zgłaszane zdarzeniem, a nie odpytywane — ekran ma pokazać liczbę
    /// od pierwszej sekundy, a nie od pierwszego odświeżenia.
    /// </remarks>
    private async Task RunCountdownAsync(
        TurnCountdownKind kind,
        TimeSpan? total,
        CancellationToken cancellationToken)
    {
        if (total is not { Ticks: > 0 })
        {
            return;
        }

        SetCountdown(new TurnCountdown(kind, total.Value, _timeProvider.GetTimestamp()));

        try
        {
            await Task.Delay(total.Value, _timeProvider, cancellationToken);
        }
        finally
        {
            SetCountdown(null);
        }
    }

    private void SetCountdown(TurnCountdown? countdown)
    {
        Countdown = countdown;
        CountdownChanged?.Invoke(this, countdown);
    }

    /// <summary>
    /// Domyka turę po zakończeniu odczytu — przechodzi do oczekiwania na graczy.
    /// </summary>
    /// <remarks>
    /// Stan jest sprawdzany, bo w trakcie odczytu gracze mogli wstrzymać albo zakończyć
    /// partię. Wtedy nie ma czego domykać.
    /// </remarks>
    private async Task CompleteTurnAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Session?.State != GameState.AnnouncingTurn)
            {
                return;
            }

            Session.CompleteAnnouncement();
            ScheduleMoveCountdown();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RepeatAsync(CancellationToken cancellationToken = default)
    {
        Announcement? announcement;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            announcement = LastAnnouncement;
        }
        finally
        {
            _gate.Release();
        }

        if (announcement is not null)
        {
            // Przerwanie trwającej wypowiedzi jest tu istotne: „Powtórz" ma zadziałać od
            // razu, a nie po dokończeniu zdania, które właśnie chcemy powtórzyć.
            await _speaker.SilenceAsync();
            await RaiseAndSpeakAsync(announcement, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ChangeTurnControlAsync(
        TurnAdvanceMode turnAdvanceMode,
        TimeSpan moveTime,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_configuration is null)
            {
                return;
            }

            if (_configuration.TurnAdvanceMode == turnAdvanceMode && _configuration.MoveTime == moveTime)
            {
                return;
            }

            // Odliczanie ruchu restartujemy tylko wtedy, gdy to ono właśnie biegnie. Gdy trwa
            // odliczanie zadania z wydarzenia, tryb wchodzi w życie po jego zakończeniu —
            // ScheduleMoveCountdown weźmie już nową wartość. Przerwanie zadania w połowie
            // odebrałoby graczowi czas, który dostał na jego wykonanie.
            bool restartuj = Countdown?.Kind == TurnCountdownKind.Move;

            _configuration = _configuration with
            {
                TurnAdvanceMode = turnAdvanceMode,
                MoveTime = moveTime,
            };

            _logger.LogInformation(
                "Sposób prowadzenia tury zmieniony na {Mode}, czas na ruch {MoveTime}. Restart odliczania: {Restart}.",
                turnAdvanceMode,
                moveTime,
                restartuj);

            if (restartuj)
            {
                StopMoveCountdown();
                ScheduleMoveCountdown();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task PauseAsync(bool announce = true, CancellationToken cancellationToken = default)
    {
        Announcement? announcement = null;

        // Najpierw cisza, potem stan: gracze mają usłyszeć skutek natychmiast, a nie po
        // dokończeniu zdania, które właśnie przestało obowiązywać.
        await InterruptTurnSequenceAsync();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Session is null || Session.State is GameState.Paused or GameState.Finished or GameState.Idle)
            {
                return;
            }

            StopMoveCountdown();
            Session.Pause();

            announcement = announce ? _announcementBuilder.BuildPaused() : null;
        }
        finally
        {
            _gate.Release();
        }

        RaiseStateChanged();
        await RaiseAndSpeakAsync(announcement, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        Announcement? announcement = null;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Session is null || Session.State != GameState.Paused)
            {
                return;
            }

            Session.Resume();
            ScheduleMoveCountdown();

            announcement = _announcementBuilder.BuildResumed();
        }
        finally
        {
            _gate.Release();
        }

        RaiseStateChanged();
        await RaiseAndSpeakAsync(announcement, cancellationToken);
    }

    /// <inheritdoc />
    public Task EliminateCurrentPlayerAsync(CancellationToken cancellationToken = default) =>
        EliminatePlayerAsync(playerId: null, cancellationToken);

    /// <inheritdoc />
    public Task EliminatePlayerAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        EliminatePlayerAsync((Guid?)playerId, cancellationToken);

    /// <summary>
    /// Zgłasza odpadnięcie gracza.
    /// </summary>
    /// <param name="playerId">
    /// Gracz, który odpadł, albo <see langword="null"/> dla gracza, którego jest tura.
    /// </param>
    /// <param name="cancellationToken">Token anulowania.</param>
    private async Task EliminatePlayerAsync(Guid? playerId, CancellationToken cancellationToken)
    {
        Announcement? announcement = null;
        bool gameOver = false;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Session is null || !Session.IsRunning)
            {
                return;
            }

            if (playerId is null && Session.CurrentPlayer is null)
            {
                return;
            }

            // Tryb bez odpadania pomija zgłoszenie, zamiast zgłaszać błąd: komenda głosowa
            // „Gracz odpadł" jest zawsze dostępna, a w trybie dla dzieci ma po prostu
            // nic nie robić.
            if (_configuration?.EliminationRule == EliminationRule.NoElimination)
            {
                _logger.LogDebug("Zgłoszenie odpadnięcia pominięte — tryb gry go nie przewiduje.");

                return;
            }

            Player eliminated = playerId is { } id
                ? Session.EliminatePlayer(id)
                : Session.EliminateCurrentPlayer();

            announcement = _announcementBuilder.BuildPlayerEliminated(eliminated);
            gameOver = Session.IsGameOver;

            _logger.LogInformation("Gracz {Player} odpadł w turze {Turn}.", eliminated.Name, Session.TurnNumber);
        }
        finally
        {
            _gate.Release();
        }

        await RaiseAndSpeakAsync(announcement, cancellationToken);

        if (gameOver)
        {
            await EndAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task EndAsync(CancellationToken cancellationToken = default)
    {
        await InterruptTurnSequenceAsync();

        GameSummary? summary = null;
        Announcement? announcement = null;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Session is null || Session.State == GameState.Finished)
            {
                return;
            }

            StopMoveCountdown();

            Player? winner = Session.Winner;
            Session.Finish();

            summary = BuildSummary(Session, winner);
            announcement = _announcementBuilder.BuildGameEnd(winner);
            LastAnnouncement = announcement;

            _logger.LogInformation(
                "Partia zakończona po {Turns} turach. Zwycięzca: {Winner}.",
                summary.TurnCount,
                winner?.Name ?? "brak");
        }
        finally
        {
            _gate.Release();
        }

        // Zakończona partia nie ma czego wznawiać — zapis znika, żeby przy następnym
        // uruchomieniu aplikacja nie proponowała powrotu do skończonej gry.
        await ClearSnapshotSafelyAsync(cancellationToken);

        RaiseStateChanged();
        await RaiseAndSpeakAsync(announcement, cancellationToken);

        if (summary is not null)
        {
            GameFinished?.Invoke(this, summary);
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await InterruptTurnSequenceAsync();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Session is null)
            {
                return;
            }

            StopMoveCountdown();
            SetCountdown(null);

            Session = null;
            _configuration = null;
            LastAnnouncement = null;
        }
        finally
        {
            _gate.Release();
        }

        await ClearSnapshotSafelyAsync(cancellationToken);

        _logger.LogInformation("Stan partii wyczyszczony — ekran rozgrywki zacznie od zasad.");

        RaiseStateChanged();
    }

    /// <inheritdoc />
    public async Task SaveSnapshotAsync(CancellationToken cancellationToken = default)
    {
        GameSessionSnapshot? snapshot;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            snapshot = Session is null || Session.State is GameState.Idle or GameState.Finished
                ? null
                : BuildSnapshot(Session);
        }
        finally
        {
            _gate.Release();
        }

        if (snapshot is null)
        {
            return;
        }

        await _sessionRepository.SaveAsync(snapshot, cancellationToken);

        _logger.LogInformation("Zapisano stan partii po turze {Turn}.", snapshot.TurnNumber);
    }

    /// <inheritdoc />
    public async Task<bool> TryRestoreAsync(CancellationToken cancellationToken = default)
    {
        GameSessionSnapshot? snapshot = await _sessionRepository.LoadAsync(cancellationToken);

        if (snapshot is null)
        {
            return false;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            _configuration = new GameConfiguration
            {
                Players = snapshot.Players,
                MoveSelectionOptions = snapshot.MoveSelectionOptions,
                TurnAdvanceMode = snapshot.TurnAdvanceMode,
                NameAnnouncementPause = snapshot.NameAnnouncementPause,
                MoveTime = snapshot.MoveTime,
                TaskTime = snapshot.TaskTime,
                EventPack = snapshot.EventPack,
                EventSelectionOptions = snapshot.EventSelectionOptions,
                GameModeKey = snapshot.GameModeKey,
                EliminationRule = snapshot.EliminationRule,
            };

            _startedAt = snapshot.StartedAt;

            Session = new GameSession(
                snapshot.Players,
                snapshot.MoveSelectionOptions.HistoryLength);

            // Wznowiona partia zawsze staje na pauzie, niezależnie od stanu w chwili zapisu.
            // Gracze wrócili do aplikacji po przerwie i muszą zobaczyć, gdzie są, zanim
            // ruszy timer albo posypią się kolejne tury.
            Session.RestoreFrom(new GameSessionRestorePoint
            {
                State = GameState.Paused,
                TurnNumber = snapshot.TurnNumber,
                CurrentPlayerId = snapshot.CurrentPlayerId,
                EventCount = snapshot.EventCount,
                LastEventTurn = snapshot.LastEventTurn,
                LastEventTurns = snapshot.LastEventTurns,
                EliminationOrder = snapshot.EliminationOrder,
                RecentMoves = snapshot.RecentMoves,
                LimbPositions = snapshot.LimbPositions,
            });

            _logger.LogInformation("Wznowiono partię z zapisu, tura {Turn}.", snapshot.TurnNumber);
        }
        finally
        {
            _gate.Release();
        }

        RaiseStateChanged();

        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _moveTimer?.Dispose();
        _gate.Dispose();
    }

    private GameSummary BuildSummary(GameSession session, Player? winner)
    {
        Dictionary<Guid, Player> byId = session.Players.ToDictionary(player => player.Id);

        IReadOnlyList<Player> eliminationOrder =
        [
            .. session.EliminationOrder
                .Where(byId.ContainsKey)
                .Select(id => byId[id]),
        ];

        return new GameSummary(
            session.Players.Count,
            session.TurnNumber,
            session.EventCount,
            _timeProvider.GetUtcNow() - _startedAt,
            eliminationOrder,
            winner);
    }

    private GameSessionSnapshot BuildSnapshot(GameSession session) => new()
    {
        State = session.State,
        TurnNumber = session.TurnNumber,
        EventCount = session.EventCount,
        Players = session.Players,
        CurrentPlayerId = session.CurrentPlayer?.Id,
        EliminationOrder = session.EliminationOrder,
        RecentMoves = session.MoveHistory.Snapshot(),
        LimbPositions = session.Players.ToDictionary(
            player => player.Id,
            player => session.GetLimbPositions(player.Id)),
        LastEventTurn = session.LastEventTurn,
        LastEventTurns = session.LastEventTurns,
        StartedAt = _startedAt,
        MoveSelectionOptions = _configuration?.MoveSelectionOptions ?? MoveSelectionOptions.Default,
        EventPack = _configuration?.EventPack,
        EventSelectionOptions = _configuration?.EventSelectionOptions ?? EventSelectionOptions.Default,
        GameModeKey = _configuration?.GameModeKey ?? "classic",
        EliminationRule = _configuration?.EliminationRule ?? EliminationRule.Manual,
        TurnAdvanceMode = _configuration?.TurnAdvanceMode ?? TurnAdvanceMode.Manual,
        NameAnnouncementPause = _configuration?.NameAnnouncementPause ?? TimeSpan.FromMilliseconds(400),
        MoveTime = _configuration?.MoveTime ?? TimeSpan.FromSeconds(10),
        TaskTime = _configuration?.TaskTime ?? TimeSpan.FromSeconds(15),
    };

    private async Task ClearSnapshotSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _sessionRepository.ClearAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // Nieudane usunięcie zapisu nie może przeszkodzić w zakończeniu partii —
            // najwyżej aplikacja zaproponuje wznowienie czegoś, co się już skończyło.
            _logger.LogWarning(exception, "Nie udało się usunąć zapisu zakończonej partii.");
        }
    }

    /// <summary>
    /// Uruchamia odliczanie czasu na wykonanie ruchu.
    /// </summary>
    /// <remarks>
    /// Odliczanie leci w <b>obu</b> trybach, bo gracze chcą wiedzieć, ile mają czasu, także
    /// wtedy, gdy sami zatwierdzają turę. Różni się skutek dojścia do zera: w trybie
    /// automatycznym rusza następna tura, w ręcznym odliczanie po prostu się kończy, a partia
    /// dalej czeka na potwierdzenie. Zegar odmierza więc <i>sugerowane</i> tempo, a nie
    /// termin — dopisanie mu automatycznego przejścia w trybie ręcznym odebrałoby graczom
    /// kontrolę, o którą właśnie ten tryb chodzi.
    /// </remarks>
    private void ScheduleMoveCountdown()
    {
        if (_configuration is null || Session?.State != GameState.AwaitingPlayerAction)
        {
            return;
        }

        _moveTimer ??= _timeProvider.CreateTimer(
            OnMoveTimeElapsed,
            state: null,
            dueTime: Timeout.InfiniteTimeSpan,
            period: Timeout.InfiniteTimeSpan);

        _moveTimer.Change(_configuration.MoveTime, Timeout.InfiniteTimeSpan);

        // Odliczanie zaczyna się razem z timerem, żeby liczba na ekranie zgadzała się
        // z chwilą, w której faktycznie ruszy następna tura.
        SetCountdown(new TurnCountdown(
            TurnCountdownKind.Move,
            _configuration.MoveTime,
            _timeProvider.GetTimestamp()));
    }

    private void StopMoveCountdown()
    {
        _moveTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        if (Countdown?.Kind == TurnCountdownKind.Move)
        {
            SetCountdown(null);
        }
    }

    /// <summary>
    /// Reaguje na upływ czasu na ruch.
    /// </summary>
    /// <remarks>
    /// W trybie ręcznym kończy tylko odliczanie: gracze zatwierdzają turę sami, a zegar był
    /// dla nich wskazówką tempa.
    /// </remarks>
    private void OnMoveTimeElapsed(object? state)
    {
        if (_configuration?.TurnAdvanceMode != TurnAdvanceMode.Automatic)
        {
            SetCountdown(null);

            return;
        }

        // Wywołanie zwrotne timera jest synchroniczne, więc uruchamiamy turę bez czekania.
        // Wyjątek musi zostać przechwycony tutaj — nie ma go gdzie przekazać wyżej.
        _ = Task.Run(async () =>
        {
            try
            {
                await NextTurnAsync();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Automatyczne przejście do następnej tury nie udało się.");
            }
        });
    }

    /// <summary>Przerywa sekwencję trwającej tury i ucisza aplikację.</summary>
    /// <remarks>
    /// Anulowanie tokenu zatrzymuje odliczania i kolejne kroki sekwencji, ale nie przerywa
    /// wypowiedzi, która już trwa — tę zatrzymuje dopiero cisza na żądanie. Potrzebne są więc
    /// obie rzeczy naraz.
    /// </remarks>
    private async Task InterruptTurnSequenceAsync()
    {
        CancellationTokenSource? sequence = Interlocked.Exchange(ref _turnSequence, null);

        if (sequence is not null)
        {
            try
            {
                await sequence.CancelAsync();
            }
            catch (ObjectDisposedException)
            {
                // Sekwencja zdążyła się skończyć sama — nie ma czego przerywać.
            }
        }

        await _speaker.SilenceAsync();
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, State);

    /// <summary>
    /// Pokazuje komunikat i odczytuje go na głos.
    /// </summary>
    /// <remarks>
    /// Jedno miejsce dla obu kanałów przekazu. Zdarzenie leci pierwsze, więc tekst pojawia
    /// się na ekranie zanim zacznie się wypowiedź — gracze widzą i słyszą to samo,
    /// a przy wyłączonym odczycie nadal widzą.
    /// </remarks>
    /// <summary>
    /// Odczytuje kolejny komunikat sekwencji tury, przerywając ją, gdy partia zmieniła stan.
    /// </summary>
    /// <remarks>
    /// Token sprawdzamy tu wprost, przed odczytem i po nim, bo <see cref="IAnnouncementSpeaker"/>
    /// z założenia <b>nie zgłasza wyjątków</b> — przerwana wypowiedź wraca tak samo jak
    /// dokończona. Bez tych sprawdzeń anulowanie ucinałoby jedno zdanie, a sekwencja spokojnie
    /// przechodziłaby do następnego.
    /// </remarks>
    private async Task SpeakStepAsync(Announcement? announcement, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await RaiseAndSpeakAsync(announcement, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task RaiseAndSpeakAsync(Announcement? announcement, CancellationToken cancellationToken)
    {
        if (announcement is null)
        {
            return;
        }

        AnnouncementRaised?.Invoke(this, announcement);

        await _speaker.SpeakAsync(announcement, cancellationToken);
    }
}
