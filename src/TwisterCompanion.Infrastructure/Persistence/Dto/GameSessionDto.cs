using TwisterCompanion.Application.Settings;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Infrastructure.Persistence.Dto;

/// <summary>
/// Postać zapisu przerwanej partii.
/// </summary>
/// <remarks>
/// Słowniki są zapisywane jako listy par, a nie jako obiekty JSON o kluczach nietekstowych.
/// Powód praktyczny: generowany kontekst serializacji obsługuje listy bez żadnych
/// zastrzeżeń, a słowniki o kluczach <see cref="Guid"/> albo wyliczeniowych wymagają
/// dodatkowych konwerterów i zachowują się różnie w kolejnych wersjach biblioteki.
/// </remarks>
internal sealed class GameSessionDto
{
    /// <summary>Wersja schematu dokumentu.</summary>
    public int SchemaVersion { get; set; } = PersistenceSchema.CurrentVersion;

    /// <summary>Stan rozgrywki w chwili zapisu.</summary>
    public GameState State { get; set; } = GameState.Idle;

    /// <summary>Numer ostatniej rozegranej tury.</summary>
    public int TurnNumber { get; set; }

    /// <summary>Liczba tur z wydarzeniem.</summary>
    public int EventCount { get; set; }

    /// <summary>Uczestnicy partii.</summary>
    public List<SessionPlayerDto> Players { get; set; } = [];

    /// <summary>Gracz, którego była tura.</summary>
    public Guid? CurrentPlayerId { get; set; }

    /// <summary>Identyfikatory graczy w kolejności odpadania.</summary>
    public List<Guid> EliminationOrder { get; set; } = [];

    /// <summary>Ostatnie ruchy, od najnowszego.</summary>
    public List<MoveDto> RecentMoves { get; set; } = [];

    /// <summary>Pozycje kończyn poszczególnych graczy.</summary>
    public List<PlayerLimbPositionsDto> LimbPositions { get; set; } = [];

    /// <summary>Numer tury, w której padło poprzednie wydarzenie.</summary>
    public int? LastEventTurn { get; set; }

    /// <summary>Numery tur, w których padły poszczególne wydarzenia.</summary>
    public List<EventTurnDto> LastEventTurns { get; set; } = [];

    /// <summary>Paczka wydarzeń obowiązująca w tej partii.</summary>
    public EventPackDto? EventPack { get; set; }

    /// <summary>Parametry losowania wydarzeń.</summary>
    public EventSelectionOptionsDto EventSelectionOptions { get; set; } = new();

    /// <summary>Chwila rozpoczęcia partii.</summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>Parametry algorytmu losowania obowiązujące w tej partii.</summary>
    public MoveSelectionOptionsDto MoveSelectionOptions { get; set; } = new();

    /// <summary>Sposób przechodzenia do następnej tury.</summary>
    public TurnAdvanceMode TurnAdvanceMode { get; set; } = TurnAdvanceMode.Manual;

    /// <summary>Klucz trybu gry, w którym toczy się partia.</summary>
    public string GameModeKey { get; set; } = "classic";

    /// <summary>Zasada odpadania graczy obowiązująca w tej partii.</summary>
    public EliminationRule EliminationRule { get; set; } = EliminationRule.Manual;

    /// <summary>Przerwa po wywołaniu gracza, w milisekundach.</summary>
    /// <remarks>
    /// W milisekundach, bo jest krótsza od sekundy w testach i może być krótsza w trybie,
    /// który zechce szybszego rytmu.
    /// </remarks>
    public int NameAnnouncementPauseMilliseconds { get; set; } = 400;

    /// <summary>Czas na wykonanie ruchu, w sekundach.</summary>
    public int MoveTimeSeconds { get; set; } = 10;

    /// <summary>Czas na wykonanie zadania z wydarzenia, w sekundach.</summary>
    public int TaskTimeSeconds { get; set; } = 15;
}

/// <summary>Postać gracza w zapisie partii — z informacją o eliminacji.</summary>
internal sealed class SessionPlayerDto
{
    /// <summary>Identyfikator gracza.</summary>
    public Guid Id { get; set; }

    /// <summary>Nazwa gracza.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Pozycja w kolejce.</summary>
    public int Order { get; set; }

    /// <summary>Czy gracz odpadł.</summary>
    public bool IsEliminated { get; set; }
}

/// <summary>Numer tury, w której padło konkretne wydarzenie.</summary>
internal sealed class EventTurnDto
{
    /// <summary>Identyfikator wydarzenia.</summary>
    public Guid EventId { get; set; }

    /// <summary>Numer tury.</summary>
    public int Turn { get; set; }
}

/// <summary>Postać parametrów losowania wydarzeń w zapisie partii.</summary>
internal sealed class EventSelectionOptionsDto
{

    /// <summary>Mnożnik szans wszystkich wydarzeń.</summary>
    public double ChanceMultiplier { get; set; } = 1.0;
}

/// <summary>Postać ruchu w zapisie partii.</summary>
internal sealed class MoveDto
{
    /// <summary>Część ciała.</summary>
    public BodyPart Part { get; set; }

    /// <summary>Kolor pola.</summary>
    public SpinColor Color { get; set; }
}

/// <summary>Pozycje kończyn jednego gracza.</summary>
internal sealed class PlayerLimbPositionsDto
{
    /// <summary>Identyfikator gracza.</summary>
    public Guid PlayerId { get; set; }

    /// <summary>Kończyny wraz z kolorami, na których stoją.</summary>
    public List<MoveDto> Positions { get; set; } = [];
}

/// <summary>Postać parametrów algorytmu losowania w zapisie partii.</summary>
/// <remarks>
/// Parametry są zapisywane w całości, a nie odtwarzane z domyślnych. Inaczej wznowienie
/// partii w trybie Hardcore (Etap 9) cofnęłoby losowanie do nastaw domyślnych, co gracze
/// odczuliby jako zmianę zasad w połowie gry.
/// </remarks>
internal sealed class MoveSelectionOptionsDto
{
    /// <summary>Długość okna tabu.</summary>
    public int TabooWindowSize { get; set; } = 3;

    /// <summary>Kara dla ruchu z okna tabu.</summary>
    public double TabooWeightMultiplier { get; set; } = 0.05;

    /// <summary>Współczynnik wygasania kary za świeżość.</summary>
    public double RecencyDecay { get; set; } = 0.6;

    /// <summary>Dopuszczalna seria tej samej części ciała.</summary>
    public int MaxSameBodyPartStreak { get; set; } = 2;

    /// <summary>Kara za przekroczenie serii części ciała.</summary>
    public double SameBodyPartStreakMultiplier { get; set; } = 0.15;

    /// <summary>Dopuszczalna seria tego samego koloru.</summary>
    public int MaxSameColorStreak { get; set; } = 2;

    /// <summary>Kara za przekroczenie serii koloru.</summary>
    public double SameColorStreakMultiplier { get; set; } = 0.3;

    /// <summary>Kara dla ruchu, który niczego nie zmienia.</summary>
    public double RedundantMoveMultiplier { get; set; } = 0.1;

    /// <summary>Długość pamiętanej historii ruchów.</summary>
    public int HistoryLength { get; set; } = 12;
}
