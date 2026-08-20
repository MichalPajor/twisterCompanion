using TwisterCompanion.Application.Settings;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.EventSelection;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Application.Game;

/// <summary>
/// Pełny stan partii w postaci nadającej się do zapisania.
/// </summary>
/// <remarks>
/// Istnieje po to, żeby zminimalizowanie aplikacji przez system nie kosztowało graczy
/// rozgrywki. Android może w każdej chwili usunąć proces aplikacji działającej w tle —
/// bez zapisu partia przepadałaby razem z nim.
/// <para>
/// Zapisujemy wszystko, co jest potrzebne do wiernego odtworzenia, w tym historię ruchów
/// i pozycje kończyn. Bez nich algorytm losowania po wznowieniu zachowywałby się jak na
/// początku partii i mógłby powtórzyć dopiero co wykonany ruch.
/// </para>
/// </remarks>
public sealed record GameSessionSnapshot
{
    /// <summary>Stan rozgrywki w chwili zapisu.</summary>
    public required GameState State { get; init; }

    /// <summary>Numer ostatniej rozegranej tury.</summary>
    public required int TurnNumber { get; init; }

    /// <summary>Liczba tur, w których wystąpiło wydarzenie.</summary>
    public required int EventCount { get; init; }

    /// <summary>Uczestnicy partii wraz z informacją, kto odpadł.</summary>
    public required IReadOnlyList<Player> Players { get; init; }

    /// <summary>Gracz, którego była tura.</summary>
    public Guid? CurrentPlayerId { get; init; }

    /// <summary>Identyfikatory graczy w kolejności odpadania.</summary>
    public required IReadOnlyList<Guid> EliminationOrder { get; init; }

    /// <summary>Ostatnie ruchy, od najnowszego — pamięć algorytmu losowania.</summary>
    public required IReadOnlyList<Move> RecentMoves { get; init; }

    /// <summary>Pozycje kończyn poszczególnych graczy.</summary>
    public required IReadOnlyDictionary<Guid, IReadOnlyDictionary<BodyPart, SpinColor>> LimbPositions { get; init; }

    /// <summary>Numer tury, w której padło poprzednie wydarzenie.</summary>
    public int? LastEventTurn { get; init; }

    /// <summary>Numery tur, w których padły poszczególne wydarzenia.</summary>
    public IReadOnlyDictionary<Guid, int> LastEventTurns { get; init; } = new Dictionary<Guid, int>();

    /// <summary>
    /// Paczka wydarzeń obowiązująca w tej partii.
    /// </summary>
    /// <remarks>
    /// Zapisywana w całości, a nie jako identyfikator do wczytania z repozytorium. Powód:
    /// użytkownik mógł w czasie przerwy zmienić albo usunąć paczkę, a wznowiona partia ma
    /// toczyć się na tych zasadach, na jakich się zaczęła.
    /// </remarks>
    public EventPack? EventPack { get; init; }

    /// <summary>Parametry losowania wydarzeń obowiązujące w tej partii.</summary>
    public EventSelectionOptions EventSelectionOptions { get; init; } = EventSelectionOptions.Default;

    /// <summary>Chwila rozpoczęcia partii — potrzebna do policzenia czasu trwania.</summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>Parametry algorytmu losowania obowiązujące w tej partii.</summary>
    public MoveSelectionOptions MoveSelectionOptions { get; init; } = MoveSelectionOptions.Default;

    /// <summary>Klucz trybu gry, w którym toczy się partia.</summary>
    public string GameModeKey { get; init; } = "classic";

    /// <summary>
    /// Zasada odpadania graczy obowiązująca w tej partii.
    /// </summary>
    /// <remarks>
    /// Zapisywana razem z partią, z tego samego powodu co paczka wydarzeń: wznowiona partia
    /// ma toczyć się na zasadach, na jakich się zaczęła, nawet jeśli w przerwie ktoś zmienił
    /// tryb gry.
    /// </remarks>
    public EliminationRule EliminationRule { get; init; } = EliminationRule.Manual;

    /// <summary>Sposób przechodzenia do następnej tury.</summary>
    public TurnAdvanceMode TurnAdvanceMode { get; init; } = TurnAdvanceMode.Manual;

    /// <summary>
    /// Przerwa między wywołaniem gracza a dalszą częścią komunikatu.
    /// </summary>
    /// <remarks>
    /// Zapisywana razem z resztą rytmu tury: wznowiona partia ma brzmieć tak samo, jak przed
    /// przerwą. Bez tego pola odtworzona konfiguracja brałaby wartość domyślną i partia
    /// wznowiona w trybie o innym tempie zmieniłaby rytm w połowie gry.
    /// </remarks>
    public TimeSpan NameAnnouncementPause { get; init; } = TimeSpan.FromMilliseconds(400);

    /// <summary>Czas na wykonanie ruchu, przeliczony już mnożnikiem trybu.</summary>
    public TimeSpan MoveTime { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Czas na wykonanie zadania z wydarzenia, przeliczony już mnożnikiem trybu.</summary>
    public TimeSpan TaskTime { get; init; } = TimeSpan.FromSeconds(15);
}
