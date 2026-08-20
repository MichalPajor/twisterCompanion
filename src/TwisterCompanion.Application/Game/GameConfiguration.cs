using TwisterCompanion.Application.Settings;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.EventSelection;
using TwisterCompanion.Domain.GameModes;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Application.Game;

/// <summary>
/// Parametry, z jakimi rozpoczyna się partia.
/// </summary>
/// <remarks>
/// Silnik gry nie sięga po ustawienia samodzielnie — dostaje gotową konfigurację.
/// Dzięki temu test rozgrywa partię bez udawania całego serwisu ustawień, a tryb gry
/// (Etap 9) może nadpisać parametry losowania bez zmiany silnika.
/// </remarks>
public sealed record GameConfiguration
{
    private readonly IReadOnlyList<Player> _players = [];

    /// <summary>Uczestnicy partii.</summary>
    /// <exception cref="ArgumentException">Gdy lista jest pusta.</exception>
    public required IReadOnlyList<Player> Players
    {
        get => _players;
        init
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Count == 0)
            {
                throw new ArgumentException("Partia wymaga co najmniej jednego gracza.", nameof(value));
            }

            _players = value;
        }
    }

    /// <summary>Parametry algorytmu losowania ruchów.</summary>
    public MoveSelectionOptions MoveSelectionOptions { get; init; } = MoveSelectionOptions.Default;

    /// <summary>
    /// Paczka wydarzeń obowiązująca w tej partii albo <see langword="null"/>, gdy gramy
    /// bez wydarzeń.
    /// </summary>
    /// <remarks>
    /// Paczka jest wczytywana raz, przy rozpoczęciu partii, i zostaje z nią do końca.
    /// Zmiana aktywnej paczki w trakcie gry nie zmienia zasad rozpoczętej rozgrywki —
    /// gracze nie powinni odczuć, że reguły zmieniły się w połowie partii.
    /// </remarks>
    public EventPack? EventPack { get; init; }

    /// <summary>Parametry losowania wydarzeń.</summary>
    public EventSelectionOptions EventSelectionOptions { get; init; } = EventSelectionOptions.Default;

    /// <summary>
    /// Przerwa między wywołaniem gracza a dalszą częścią komunikatu.
    /// </summary>
    /// <remarks>
    /// Tura zaczyna się od nazwy gracza, żeby wiedział, że to jego kolej, <b>zanim</b> usłyszy
    /// polecenie. Bez przerwy imię zlewa się z resztą zdania i gracz orientuje się w połowie
    /// komunikatu, którego początku już nie usłyszał.
    /// <para>
    /// Czterysta milisekund, nie sekunda: sekunda była dobrana bez próby na urządzeniu i po
    /// niej cała tura wlokła się bardziej, niż trzeba (uwaga użytkownika). Do tej przerwy
    /// dochodzi jeszcze przerwa własna syntezatora między wypowiedziami, więc słyszalny odstęp
    /// jest dłuższy od tej liczby.
    /// </para>
    /// </remarks>
    public TimeSpan NameAnnouncementPause { get; init; } = TimeSpan.FromMilliseconds(400);

    /// <summary>
    /// Ile czasu gracze mają na wykonanie zadania z wylosowanego wydarzenia.
    /// </summary>
    /// <remarks>
    /// Odmierzany w obu trybach: po odczytaniu wydarzenia aplikacja czeka, aż gracze je
    /// wykonają, i dopiero potem czyta polecenie ruchu.
    /// </remarks>
    public TimeSpan TaskTime { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Ile czasu gracze mają na wykonanie ruchu — tyle trwa odliczanie po odczytaniu polecenia.
    /// </summary>
    /// <remarks>
    /// Skutek dojścia do zera zależy od trybu: automatyczny rozgrywa następną turę, ręczny
    /// tylko kończy odliczanie i dalej czeka na graczy.
    /// <para>
    /// <b>Źródło wartości też zależy od trybu</b>, i to jest tu najważniejsze. Przy sterowaniu
    /// głosem odliczanie musi kończyć się dokładnie w chwili otwarcia nasłuchu, bo zero na
    /// ekranie i sygnał „mów teraz" opisują ten sam moment — dwie różne liczby oznaczałyby,
    /// że jedna z nich kłamie. Dlatego wtedy wartość pochodzi z czasu przed nasłuchem,
    /// a nie z czasu tury automatycznej.
    /// </para>
    /// </remarks>
    public TimeSpan MoveTime { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Klucz trybu gry, w którym toczy się partia.</summary>
    public string GameModeKey { get; init; } = "classic";

    /// <summary>
    /// Zasada odpadania graczy obowiązująca w tej partii.
    /// </summary>
    /// <remarks>
    /// Jedyna reguła trybu, która zmienia przebieg partii, a nie tylko parametry losowania —
    /// dlatego trafia do konfiguracji, a nie zostaje w definicji trybu.
    /// </remarks>
    public EliminationRule EliminationRule { get; init; } = EliminationRule.Manual;

    /// <summary>Sposób przechodzenia do następnej tury.</summary>
    public TurnAdvanceMode TurnAdvanceMode { get; init; } = TurnAdvanceMode.Manual;


    /// <summary>Buduje konfigurację z ustawień aplikacji i wybranego trybu gry.</summary>
    /// <param name="players">Uczestnicy partii.</param>
    /// <param name="settings">Ustawienia aplikacji.</param>
    /// <param name="mode">Wybrany tryb gry albo <see langword="null"/> dla nastaw domyślnych.</param>
    /// <param name="eventPack">Aktywna paczka wydarzeń, jeśli jakaś jest wybrana.</param>
    /// <remarks>
    /// Podział wpływów jest tu rozstrzygnięty raz dla całej aplikacji:
    /// <list type="bullet">
    /// <item>parametry losowania ruchów i wydarzeń należą <b>wyłącznie</b> do trybu — one
    /// właśnie <i>są</i> trybem, a użytkownik nie ma do nich dostępu;</item>
    /// <item>czasy na ruch i na zadanie ustawia użytkownik, a tryb je <b>skaluje</b> swoim
    /// mnożnikiem — dzięki temu przestawienie czasu działa we wszystkich trybach naraz,
    /// a proporcje między trybami zostają zachowane;</item>
    /// <item>przy sterowaniu głosem odliczanie ruchu bierze <b>czas przed nasłuchem</b>
    /// i nie skaluje go mnożnikiem trybu: zero na ekranie musi wypaść równo z sygnałem
    /// otwarcia mikrofonu, więc obie wartości muszą pochodzić z tego samego ustawienia;</item>
    /// <item>sposób przechodzenia do następnej tury należy zawsze do użytkownika — to
    /// preferencja obsługi, a nie reguła gry.</item>
    /// </list>
    /// <para>
    /// Same wartości wylicza <see cref="GameSetup.FromSettings"/>, a nie ta metoda. Ekran
    /// przed grą pokazuje te zasady, zanim będzie znany skład, więc musiałby je liczyć drugi
    /// raz — a dwa miejsca liczące to samo rozjeżdżają się przy pierwszej zmianie reguły.
    /// </para>
    /// </remarks>
    public static GameConfiguration FromSettings(
        IReadOnlyList<Player> players,
        AppSettings settings,
        GameModeDefinition? mode = null,
        EventPack? eventPack = null)
    {
        GameSetup setup = GameSetup.FromSettings(settings, mode, eventPack);

        return new GameConfiguration
        {
            Players = players,
            TurnAdvanceMode = setup.TurnAdvanceMode,
            MoveTime = setup.MoveTime,
            TaskTime = setup.TaskTime,
            EventPack = setup.EventPack,
            GameModeKey = setup.GameModeKey,
            EliminationRule = setup.EliminationRule,
            MoveSelectionOptions = mode?.MoveSelectionOptions ?? MoveSelectionOptions.Default,
            EventSelectionOptions = mode?.EventSelectionOptions ?? EventSelectionOptions.Default,
        };
    }
}
