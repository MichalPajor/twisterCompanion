namespace TwisterCompanion.Application.Settings;

/// <summary>
/// Ustawienia aplikacji zapamiętywane między uruchomieniami.
/// </summary>
/// <remarks>
/// Typ jest niezmienny — zmiana ustawienia powstaje przez <c>with</c>, a serwis ustawień
/// zapisuje i rozgłasza nowy stan. Dzięki temu nie ma sytuacji, w której część aplikacji
/// widzi już nową wartość, a część jeszcze starą.
/// <para>
/// Zakresy są pilnowane w akcesorach <c>init</c>, więc niepoprawna wartość nie przejdzie
/// ani przez konstruktor, ani przez <c>with</c>, ani przez odczyt uszkodzonego pliku.
/// </para>
/// </remarks>
public sealed record AppSettings
{
    /// <summary>Najmniejsze dopuszczalne tempo mowy.</summary>
    public const float MinSpeechRate = 0.5f;

    /// <summary>Największe dopuszczalne tempo mowy.</summary>
    public const float MaxSpeechRate = 2.0f;

    /// <summary>Najmniejsza dopuszczalna wysokość głosu.</summary>
    public const float MinSpeechPitch = 0.5f;

    /// <summary>Największa dopuszczalna wysokość głosu.</summary>
    public const float MaxSpeechPitch = 2.0f;

    /// <summary>Najkrótszy dopuszczalny czas na ruch przed otwarciem nasłuchu.</summary>
    public static readonly TimeSpan MinVoiceListeningDelay = TimeSpan.FromSeconds(1);

    /// <summary>Najdłuższy dopuszczalny czas na ruch przed otwarciem nasłuchu.</summary>
    public static readonly TimeSpan MaxVoiceListeningDelay = TimeSpan.FromSeconds(60);

    /// <summary>Najkrótszy dopuszczalny czas na wykonanie ruchu.</summary>
    public static readonly TimeSpan MinMoveTime = TimeSpan.FromSeconds(1);

    /// <summary>Najdłuższy dopuszczalny czas na wykonanie ruchu.</summary>
    public static readonly TimeSpan MaxMoveTime = TimeSpan.FromMinutes(2);

    /// <summary>Najkrótszy dopuszczalny czas na wykonanie zadania z wydarzenia.</summary>
    public static readonly TimeSpan MinTaskTime = TimeSpan.FromSeconds(1);

    /// <summary>Najdłuższy dopuszczalny czas na wykonanie zadania z wydarzenia.</summary>
    public static readonly TimeSpan MaxTaskTime = TimeSpan.FromMinutes(3);

    private readonly float _speechRate = 1.0f;
    private readonly float _speechPitch = 1.0f;
    private readonly double _soundVolume = 0.8;
    private readonly TimeSpan _moveTime = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _taskTime = TimeSpan.FromSeconds(15);
    private readonly TimeSpan _voiceListeningDelay = TimeSpan.FromSeconds(10);
    private readonly string _gameModeKey = "classic";
    private readonly int _finishedGamesCount;

    /// <summary>
    /// Kod języka interfejsu, na przykład <c>pl</c> albo <c>en</c>.
    /// <see langword="null"/> oznacza „idź za językiem systemu".
    /// </summary>
    public string? LanguageCode { get; init; }

    /// <summary>
    /// Wybrany motyw kolorystyczny.
    /// </summary>
    /// <remarks>
    /// Domyślnie zgodny z systemem: telefon w trybie ciemnym po zachodzie słońca ma pociągnąć
    /// za sobą aplikację, dopóki gracz nie zażyczy sobie inaczej.
    /// </remarks>
    public AppThemePreference ThemePreference { get; init; } = AppThemePreference.System;

    /// <summary>Czy aplikacja odczytuje komunikaty na głos.</summary>
    public bool IsTextToSpeechEnabled { get; init; } = true;

    /// <summary>
    /// Identyfikator wybranego głosu systemowego. <see langword="null"/> oznacza
    /// głos domyślny dla języka.
    /// </summary>
    public string? PreferredVoiceId { get; init; }

    /// <summary>Tempo mowy, gdzie 1,0 to tempo domyślne.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Gdy wartość jest poza dopuszczalnym zakresem.</exception>
    public float SpeechRate
    {
        get => _speechRate;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinSpeechRate);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxSpeechRate);
            _speechRate = value;
        }
    }

    /// <summary>Wysokość głosu, gdzie 1,0 to wysokość domyślna.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Gdy wartość jest poza dopuszczalnym zakresem.</exception>
    public float SpeechPitch
    {
        get => _speechPitch;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinSpeechPitch);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxSpeechPitch);
            _speechPitch = value;
        }
    }

    /// <summary>Czy efekty dźwiękowe są włączone.</summary>
    public bool AreSoundsEnabled { get; init; } = true;

    /// <summary>Głośność efektów dźwiękowych z zakresu 0,0–1,0.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Gdy wartość jest poza zakresem.</exception>
    public double SoundVolume
    {
        get => _soundVolume;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0);
            _soundVolume = value;
        }
    }

    /// <summary>Czy wibracje są włączone.</summary>
    public bool AreHapticsEnabled { get; init; } = true;

    /// <summary>
    /// Czy gracz widział już wprowadzenie „Jak grać".
    /// </summary>
    /// <remarks>
    /// Trzymane w ustawieniach, a nie w osobnym pliku „pierwsze uruchomienie": to jest
    /// preferencja użytkownika jak każda inna, a przy okazji <b>kasowanie danych ją zeruje</b>,
    /// więc po wyczyszczeniu aplikacja wita nowego właściciela tak jak przy pierwszym
    /// uruchomieniu.
    /// </remarks>
    public bool HasSeenOnboarding { get; init; }

    /// <summary>
    /// Ile partii zakończono od zainstalowania aplikacji.
    /// </summary>
    /// <remarks>
    /// Nie jest to preferencja, a jednak leży wśród ustawień, i to jest wybór, nie
    /// niedopatrzenie: licznik ma <b>przeżywać restart aplikacji</b>, bo służy do decyzji
    /// „reklama pełnoekranowa co trzecią partię". Trzymany w pamięci zerowałby się przy każdym
    /// zamknięciu i reklama mogłaby wracać po każdej partii. Osobny plik dla jednej liczby
    /// byłby drugim mechanizmem zapisu do utrzymania — a przy okazji kasowanie danych
    /// użytkownika zeruje licznik razem z resztą, co jest zachowaniem oczekiwanym.
    /// </remarks>
    public int FinishedGamesCount
    {
        get => _finishedGamesCount;
        init => _finishedGamesCount = value < 0 ? 0 : value;
    }

    /// <summary>
    /// Czy animacje interfejsu są włączone.
    /// </summary>
    /// <remarks>
    /// Wyłączenie animacji w ustawieniach dostępności systemu jest respektowane niezależnie
    /// od tej wartości — to ustawienie jest <b>dodatkowym</b> wyłącznikiem dla osób, które
    /// chcą spokojnego ekranu tylko w tej jednej aplikacji, bez zmiany zachowania całego
    /// telefonu.
    /// </remarks>
    public bool AreAnimationsEnabled { get; init; } = true;

    /// <summary>Czy sterowanie głosem jest włączone w trakcie rozgrywki.</summary>
    /// <remarks>
    /// Domyślnie wyłączone: wymaga zgody na mikrofon, a pytanie o nią przy pierwszym
    /// uruchomieniu gry, zanim gracz w ogóle wie, po co, jest złym pierwszym wrażeniem.
    /// Włączenie jest świadomą decyzją na ekranie ustawień.
    /// </remarks>
    public bool IsVoiceControlEnabled { get; init; }

    /// <summary>
    /// Ile czasu gracze mają na wykonanie ruchu, zanim otworzy się nasłuch komend.
    /// </summary>
    /// <remarks>
    /// Nasłuch nie startuje zaraz po odczytaniu komunikatu, choć technicznie mógłby.
    /// Mikrofon otwarty w trakcie układania ręki na macie zbierałby wyłącznie sapanie
    /// i śmiech, zużywając na to sesje rozpoznawania.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Gdy wartość jest poza dopuszczalnym zakresem.</exception>
    public TimeSpan VoiceListeningDelay
    {
        get => _voiceListeningDelay;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinVoiceListeningDelay);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxVoiceListeningDelay);
            _voiceListeningDelay = value;
        }
    }

    /// <summary>
    /// Sposób przechodzenia do następnej tury.
    /// </summary>
    /// <remarks>
    /// W trybie automatycznym <b>sterowanie głosem jest wyłączone</b> — nie ma czym sterować,
    /// bo tury same następują po sobie. Zgłoszenie odpadnięcia zostaje przy przycisku obok
    /// nazwy gracza, dostępnym w obu trybach.
    /// </remarks>
    public TurnAdvanceMode TurnAdvanceMode { get; init; } = TurnAdvanceMode.Manual;

    /// <summary>
    /// Ile czasu gracze mają na wykonanie ruchu.
    /// </summary>
    /// <remarks>
    /// W trybie automatycznym to po tym czasie rusza następna tura, a na ekranie leci
    /// odliczanie. W trybie ręcznym wartość nie jest używana — tam czeka się na gracza.
    /// <para>
    /// Tryb gry przelicza tę wartość własnym mnożnikiem: Hardcore daje połowę czasu,
    /// tryb dla dzieci półtora raza więcej.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Gdy wartość jest poza dopuszczalnym zakresem.</exception>
    public TimeSpan MoveTime
    {
        get => _moveTime;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinMoveTime);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxMoveTime);
            _moveTime = value;
        }
    }

    /// <summary>
    /// Ile czasu gracze mają na wykonanie zadania z wylosowanego wydarzenia.
    /// </summary>
    /// <remarks>
    /// Odmierzany <b>w obu trybach</b>: po odczytaniu wydarzenia aplikacja czeka, aż gracze
    /// je wykonają, i dopiero potem czyta polecenie ruchu. Zadanie („zaśpiewaj refren")
    /// trwa dłużej niż postawienie ręki, dlatego jest to osobna wartość, a nie ta sama
    /// co czas na ruch.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Gdy wartość jest poza dopuszczalnym zakresem.</exception>
    public TimeSpan TaskTime
    {
        get => _taskTime;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, MinTaskTime);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxTaskTime);
            _taskTime = value;
        }
    }

    /// <summary>Klucz wybranego trybu gry.</summary>
    /// <exception cref="ArgumentException">Gdy klucz jest pusty.</exception>
    public string GameModeKey
    {
        get => _gameModeKey;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _gameModeKey = value.Trim();
        }
    }

    /// <summary>
    /// Identyfikator aktywnej paczki wydarzeń. <see langword="null"/> oznacza brak
    /// wybranej paczki, czyli rozgrywkę bez wydarzeń.
    /// </summary>
    public Guid? ActiveEventPackId { get; init; }

    /// <summary>Ustawienia domyślne — stan po pierwszym uruchomieniu i po resecie.</summary>
    public static AppSettings Default { get; } = new();
}
