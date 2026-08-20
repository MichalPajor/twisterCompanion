using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Domain.Entities;

/// <summary>
/// Stan jednej partii i reguły, które go pilnują.
/// </summary>
/// <remarks>
/// Jedyny <b>zmienny</b> typ w warstwie domeny. Pozostałe (<see cref="Player"/>,
/// <see cref="Move"/>, <see cref="GameEvent"/>) są niezmienne, bo opisują wartości.
/// Partia natomiast jest z natury czymś, co się zmienia w czasie, a próba modelowania jej
/// niezmiennie oznaczałaby przepisywanie całego stanu przy każdej turze i przenoszenie
/// pilnowania reguł na zewnątrz.
/// <para>
/// Klasa odpowiada za <b>reguły</b>: kto jest następny, kiedy gra się kończy, jakie
/// przejścia stanu są dopuszczalne. Nie zna losowania, komunikatów ani czasu — to należy
/// do silnika gry w warstwie aplikacji.
/// </para>
/// <para>
/// <b>Nie jest bezpieczna wątkowo.</b> Właścicielem jest silnik gry, który operuje na niej
/// z jednego wątku.
/// </para>
/// </remarks>
public sealed class GameSession
{
    private readonly List<Player> _players;
    private readonly Dictionary<Guid, Dictionary<BodyPart, SpinColor>> _limbPositions = [];
    private readonly List<Guid> _eliminationOrder = [];
    private readonly Dictionary<Guid, int> _lastEventTurns = [];

    private int _currentPlayerIndex = -1;

    /// <summary>Tworzy nową partię.</summary>
    /// <param name="players">Uczestnicy. Kolejność wynika z <see cref="Player.Order"/>.</param>
    /// <param name="moveHistoryLength">Ile ostatnich ruchów pamiętać dla algorytmu losowania.</param>
    /// <exception cref="ArgumentException">Gdy lista jest pusta albo zawiera powtórzone identyfikatory.</exception>
    public GameSession(IReadOnlyList<Player> players, int moveHistoryLength)
    {
        ArgumentNullException.ThrowIfNull(players);

        if (players.Count == 0)
        {
            throw new ArgumentException("Partia wymaga co najmniej jednego gracza.", nameof(players));
        }

        if (players.Select(player => player.Id).Distinct().Count() != players.Count)
        {
            throw new ArgumentException("Identyfikatory graczy muszą być unikalne.", nameof(players));
        }

        _players = [.. players.OrderBy(player => player.Order)];
        MoveHistory = new MoveHistory(moveHistoryLength);
    }

    /// <summary>Aktualny stan rozgrywki.</summary>
    public GameState State { get; private set; } = GameState.Idle;

    /// <summary>Numer ostatniej rozegranej tury. Zero oznacza, że żadna jeszcze nie padła.</summary>
    public int TurnNumber { get; private set; }

    /// <summary>Wszyscy uczestnicy, także ci którzy odpadli.</summary>
    public IReadOnlyList<Player> Players => _players;

    /// <summary>Uczestnicy nadal w grze.</summary>
    public IReadOnlyList<Player> ActivePlayers => [.. _players.Where(player => !player.IsEliminated)];

    /// <summary>Gracz, którego jest tura.</summary>
    public Player? CurrentPlayer =>
        _currentPlayerIndex >= 0 && _currentPlayerIndex < _players.Count
            ? _players[_currentPlayerIndex]
            : null;

    /// <summary>Ostatnio rozegrana tura.</summary>
    public Turn? CurrentTurn { get; private set; }

    /// <summary>Okno ostatnich ruchów, używane przez algorytm losowania.</summary>
    public MoveHistory MoveHistory { get; }

    /// <summary>Identyfikatory graczy w kolejności odpadania.</summary>
    public IReadOnlyList<Guid> EliminationOrder => _eliminationOrder;

    /// <summary>Liczba tur, w których wystąpiło wydarzenie.</summary>
    public int EventCount { get; private set; }

    /// <summary>
    /// Numer tury, w której padło poprzednie wydarzenie. <see langword="null"/>, gdy
    /// jeszcze żadne nie padło.
    /// </summary>
    public int? LastEventTurn { get; private set; }

    /// <summary>
    /// Numery tur, w których padły poszczególne wydarzenia.
    /// </summary>
    /// <remarks>
    /// Podstawa dwóch ograniczeń naraz: własnego odstępu wydarzenia oraz wydarzeń
    /// jednorazowych, dla których sama obecność wpisu oznacza, że już wystąpiły.
    /// </remarks>
    public IReadOnlyDictionary<Guid, int> LastEventTurns => _lastEventTurns;

    /// <summary>Czy gra jest w toku — rozpoczęta i niezakończona.</summary>
    public bool IsRunning => State is GameState.Starting
        or GameState.AnnouncingTurn
        or GameState.AwaitingPlayerAction
        or GameState.Paused;

    /// <summary>
    /// Czy partia powinna się zakończyć.
    /// </summary>
    /// <remarks>
    /// Przy jednym uczestniku gra nie kończy się sama — to tryb treningowy, który przerywa
    /// użytkownik. Kończy się dopiero, gdy ten jedyny gracz odpadnie.
    /// </remarks>
    public bool IsGameOver =>
        ActivePlayers.Count == 0 || (_players.Count > 1 && ActivePlayers.Count == 1);

    /// <summary>Zwycięzca, jeśli został wyłoniony.</summary>
    public Player? Winner => ActivePlayers.Count == 1 && _players.Count > 1 ? ActivePlayers[0] : null;

    /// <summary>Rozpoczyna partię.</summary>
    /// <exception cref="InvalidOperationException">Gdy partia już się rozpoczęła.</exception>
    public void Start()
    {
        RequireState(GameState.Idle);

        State = GameState.Starting;
    }

    /// <summary>
    /// Wskazuje następnego gracza w kolejce, pomijając tych, którzy odpadli.
    /// </summary>
    /// <returns>Gracz, którego jest tura.</returns>
    /// <exception cref="InvalidOperationException">Gdy nie ma już aktywnych graczy.</exception>
    public Player SelectNextPlayer()
    {
        if (ActivePlayers.Count == 0)
        {
            throw new InvalidOperationException("Nie ma już aktywnych graczy.");
        }

        // Pełny obrót w najgorszym przypadku — pętla zawsze się kończy, bo wiemy,
        // że istnieje co najmniej jeden aktywny gracz.
        for (int step = 1; step <= _players.Count; step++)
        {
            int candidate = (_currentPlayerIndex + step + _players.Count) % _players.Count;

            if (!_players[candidate].IsEliminated)
            {
                _currentPlayerIndex = candidate;

                return _players[candidate];
            }
        }

        throw new InvalidOperationException("Nie udało się wskazać następnego gracza.");
    }

    /// <summary>
    /// Zapisuje rozegraną turę i przechodzi do ogłaszania ruchu.
    /// </summary>
    /// <param name="move">Wylosowany ruch.</param>
    /// <param name="gameEvent">Wydarzenie, jeśli wystąpiło.</param>
    /// <exception cref="InvalidOperationException">Gdy stan nie pozwala rozegrać tury.</exception>
    public Turn BeginTurn(Move move, GameEvent? gameEvent = null)
    {
        RequireState(GameState.Starting, GameState.AwaitingPlayerAction, GameState.AnnouncingTurn);

        Player player = CurrentPlayer
            ?? throw new InvalidOperationException("Nie wskazano gracza dla tej tury.");

        TurnNumber++;

        Turn turn = new()
        {
            Number = TurnNumber,
            Player = player,
            Move = move,
            Event = gameEvent,
        };

        CurrentTurn = turn;
        MoveHistory.Add(move);
        SetLimbPosition(player.Id, move);

        if (gameEvent is not null)
        {
            EventCount++;
            LastEventTurn = TurnNumber;
            _lastEventTurns[gameEvent.Id] = TurnNumber;
        }

        State = GameState.AnnouncingTurn;

        return turn;
    }

    /// <summary>Kończy ogłaszanie ruchu i przechodzi do oczekiwania na graczy.</summary>
    /// <exception cref="InvalidOperationException">Gdy nie trwa ogłaszanie.</exception>
    public void CompleteAnnouncement()
    {
        RequireState(GameState.AnnouncingTurn);

        State = GameState.AwaitingPlayerAction;
    }

    /// <summary>Wstrzymuje partię.</summary>
    /// <exception cref="InvalidOperationException">Gdy partia nie jest w toku.</exception>
    public void Pause()
    {
        RequireState(GameState.Starting, GameState.AnnouncingTurn, GameState.AwaitingPlayerAction);

        State = GameState.Paused;
    }

    /// <summary>
    /// Wznawia wstrzymaną partię.
    /// </summary>
    /// <remarks>
    /// Wraca do oczekiwania na graczy, a nie do ogłaszania — nawet jeśli pauza padła
    /// w trakcie ogłaszania. Powtórne odczytanie ruchu należy do komendy „Powtórz",
    /// a nie do wznowienia.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Gdy partia nie jest wstrzymana.</exception>
    public void Resume()
    {
        RequireState(GameState.Paused);

        State = TurnNumber == 0 ? GameState.Starting : GameState.AwaitingPlayerAction;
    }

    /// <summary>
    /// Oznacza aktualnego gracza jako odpadniętego.
    /// </summary>
    /// <returns>Gracz, który odpadł.</returns>
    /// <exception cref="InvalidOperationException">Gdy nie ma aktualnego gracza albo partia nie trwa.</exception>
    public Player EliminateCurrentPlayer()
    {
        RequireState(GameState.Starting, GameState.AnnouncingTurn, GameState.AwaitingPlayerAction, GameState.Paused);

        if (_currentPlayerIndex < 0)
        {
            throw new InvalidOperationException("Nie wskazano gracza, który mógłby odpaść.");
        }

        return Eliminate(_currentPlayerIndex);
    }

    /// <summary>
    /// Oznacza wskazanego gracza jako odpadniętego.
    /// </summary>
    /// <param name="playerId">Identyfikator gracza.</param>
    /// <returns>Gracz, który odpadł.</returns>
    /// <remarks>
    /// Odpada <b>ten</b> gracz, a nie ten, którego jest tura. Upadek zdarza się przy każdym
    /// ruchu, także cudzym: ktoś traci równowagę, gdy sąsiad przeciska się nad jego ręką.
    /// Zgłoszenie „odpadł aktualny gracz" byłoby w takiej sytuacji nieprawdą.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Gdy gracza nie ma w partii, już odpadł albo partia nie trwa.
    /// </exception>
    public Player EliminatePlayer(Guid playerId)
    {
        RequireState(GameState.Starting, GameState.AnnouncingTurn, GameState.AwaitingPlayerAction, GameState.Paused);

        int index = _players.FindIndex(player => player.Id == playerId);

        if (index < 0)
        {
            throw new InvalidOperationException($"Gracz {playerId} nie bierze udziału w tej partii.");
        }

        return Eliminate(index);
    }

    private Player Eliminate(int index)
    {
        Player eliminated = _players[index];

        if (eliminated.IsEliminated)
        {
            throw new InvalidOperationException($"Gracz {eliminated.Name} już odpadł.");
        }

        _players[index] = eliminated with { IsEliminated = true };
        _eliminationOrder.Add(eliminated.Id);

        return _players[index];
    }

    /// <summary>Kończy partię.</summary>
    public void Finish() => State = GameState.Finished;

    /// <summary>
    /// Zwraca kolory, na których stoją kończyny gracza.
    /// </summary>
    /// <param name="playerId">Identyfikator gracza.</param>
    /// <remarks>
    /// Aplikacja zna te pozycje, bo sama je wcześniej ogłosiła. Algorytm losowania używa ich,
    /// żeby nie wydać polecenia, które niczego nie zmienia.
    /// </remarks>
    public IReadOnlyDictionary<BodyPart, SpinColor> GetLimbPositions(Guid playerId) =>
        _limbPositions.TryGetValue(playerId, out Dictionary<BodyPart, SpinColor>? positions)
            ? positions
            : new Dictionary<BodyPart, SpinColor>();

    /// <summary>
    /// Odtwarza stan partii z zapisu — używane przy wznowieniu aplikacji.
    /// </summary>
    /// <param name="restorePoint">Zapisany stan partii.</param>
    /// <remarks>
    /// Metoda celowo nie waliduje przejść stanu — odtwarza zapis, a nie rozgrywa partię.
    /// Walidacją danych z dysku zajmuje się warstwa persystencji.
    /// </remarks>
    public void RestoreFrom(GameSessionRestorePoint restorePoint)
    {
        ArgumentNullException.ThrowIfNull(restorePoint);

        IReadOnlyList<Guid> eliminationOrder = restorePoint.EliminationOrder;
        IReadOnlyList<Move> recentMoves = restorePoint.RecentMoves;
        IReadOnlyDictionary<Guid, IReadOnlyDictionary<BodyPart, SpinColor>> limbPositions =
            restorePoint.LimbPositions;

        State = restorePoint.State;
        TurnNumber = restorePoint.TurnNumber;
        EventCount = restorePoint.EventCount;
        LastEventTurn = restorePoint.LastEventTurn;

        _lastEventTurns.Clear();

        foreach ((Guid eventId, int turn) in restorePoint.LastEventTurns)
        {
            _lastEventTurns[eventId] = turn;
        }

        _currentPlayerIndex = restorePoint.CurrentPlayerId is null
            ? -1
            : _players.FindIndex(player => player.Id == restorePoint.CurrentPlayerId.Value);

        _eliminationOrder.Clear();
        _eliminationOrder.AddRange(eliminationOrder);

        for (int index = 0; index < _players.Count; index++)
        {
            if (_eliminationOrder.Contains(_players[index].Id))
            {
                _players[index] = _players[index] with { IsEliminated = true };
            }
        }

        MoveHistory.Clear();

        // Historia jest zapisana od najnowszego ruchu, a Add wstawia na początek —
        // dodajemy więc od końca, żeby zachować kolejność.
        for (int index = recentMoves.Count - 1; index >= 0; index--)
        {
            MoveHistory.Add(recentMoves[index]);
        }

        _limbPositions.Clear();

        foreach ((Guid playerId, IReadOnlyDictionary<BodyPart, SpinColor> positions) in limbPositions)
        {
            _limbPositions[playerId] = new Dictionary<BodyPart, SpinColor>(positions);
        }
    }

    private void SetLimbPosition(Guid playerId, Move move)
    {
        if (!_limbPositions.TryGetValue(playerId, out Dictionary<BodyPart, SpinColor>? positions))
        {
            positions = [];
            _limbPositions[playerId] = positions;
        }

        positions[move.Part] = move.Color;
    }

    private void RequireState(params GameState[] allowed)
    {
        if (!allowed.Contains(State))
        {
            throw new InvalidOperationException(
                $"Operacja niedozwolona w stanie {State}. Dopuszczalne stany: {string.Join(", ", allowed)}.");
        }
    }
}
