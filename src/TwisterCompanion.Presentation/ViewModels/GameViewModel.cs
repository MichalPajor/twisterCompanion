using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Advertising;
using TwisterCompanion.Application.Feedback;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.GameModes;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Application.VoiceControl;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.GameModes;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Navigation;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Ekran rozgrywki.
/// </summary>
/// <remarks>
/// ViewModel jest <b>widokiem na silnik gry</b>, a nie właścicielem stanu. Nie trzyma
/// niczego, czego nie dałoby się odczytać z <see cref="IGameEngine"/> — dzięki temu powrót
/// na ekran po wyjściu z niego pokazuje aktualny stan partii, a nie stan sprzed wyjścia.
/// <para>
/// Subskrypcje zdarzeń silnika są zakładane w <see cref="OnAppearing"/> i zwalniane
/// w <see cref="OnDisappearing"/>. Silnik jest singletonem, a ten ViewModel powstaje na
/// każde wejście na ekran — subskrypcja bez zwolnienia trzymałaby w pamięci każdą
/// dotychczasową instancję.
/// </para>
/// <para>
/// Docelowy wygląd ekranu dokłada Etap 10.
/// </para>
/// </remarks>
public partial class GameViewModel : NavigableViewModelBase
{
    private readonly IGameEngine _engine;
    private readonly IPlayerRosterRepository _playerRoster;
    private readonly ISettingsService _settingsService;
    private readonly IEventPackService _eventPacks;
    private readonly IGameModeService _gameModes;
    private readonly IVoiceControlService _voiceControl;
    private readonly IVoiceControlCoordinator _voiceCoordinator;
    private readonly IAdCoordinator _ads;
    private readonly IUiDispatcher _dispatcher;
    private readonly IAudioCueService _audioCues;
    private readonly IGameFeedback _feedback;
    private readonly TimeProvider _timeProvider;

    /// <summary>Od ilu sekund odliczanie jest pokazywane jako pilne.</summary>
    private const int UrgentCountdownSeconds = 5;

    private TurnCountdown? _countdown;
    private ITimer? _countdownTimer;
    private bool _isSubscribed;

    /// <summary>Tworzy ViewModel ekranu rozgrywki.</summary>
    /// <param name="navigation">Serwis nawigacji.</param>
    /// <param name="engine">Silnik rozgrywki.</param>
    /// <param name="playerRoster">Repozytorium listy graczy.</param>
    /// <param name="settingsService">Ustawienia aplikacji.</param>
    /// <param name="eventPacks">Paczki wydarzeń — źródło aktywnej paczki.</param>
    /// <param name="gameModes">Tryby gry — źródło zasad rozpoczynanej partii.</param>
    /// <param name="voiceControl">Nasłuch komend — źródło stanu mikrofonu.</param>
    /// <param name="voiceCoordinator">Sterowanie głosem włączane na czas rozgrywki.</param>
    /// <param name="ads">Reklamy — baner na czas rozgrywki i reklama po zakończeniu partii.</param>
    /// <param name="dispatcher">Przeniesienie odliczania na wątek interfejsu.</param>
    /// <param name="audioCues">Sygnały dźwiękowe — tykanie odliczania.</param>
    /// <param name="feedback">Efekty dźwiękowe i wibracje zdarzeń partii.</param>
    /// <param name="timeProvider">Źródło czasu — odmierza pozostałe sekundy tury.</param>
    /// <param name="logger">Logger tego ViewModelu.</param>
    /// <param name="dialogService">Serwis komunikatów dla użytkownika.</param>
    /// <param name="localization">Serwis tłumaczeń.</param>
    public GameViewModel(
        INavigationService navigation,
        IGameEngine engine,
        IPlayerRosterRepository playerRoster,
        ISettingsService settingsService,
        IEventPackService eventPacks,
        IGameModeService gameModes,
        IVoiceControlService voiceControl,
        IVoiceControlCoordinator voiceCoordinator,
        IAdCoordinator ads,
        IUiDispatcher dispatcher,
        IAudioCueService audioCues,
        IGameFeedback feedback,
        TimeProvider timeProvider,
        ILogger<GameViewModel> logger,
        IDialogService dialogService,
        ILocalizationService localization)
        : base(navigation, logger, dialogService, localization)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(playerRoster);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(eventPacks);
        ArgumentNullException.ThrowIfNull(gameModes);
        ArgumentNullException.ThrowIfNull(voiceControl);
        ArgumentNullException.ThrowIfNull(voiceCoordinator);
        ArgumentNullException.ThrowIfNull(ads);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(audioCues);
        ArgumentNullException.ThrowIfNull(feedback);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _engine = engine;
        _playerRoster = playerRoster;
        _settingsService = settingsService;
        _eventPacks = eventPacks;
        _gameModes = gameModes;
        _voiceControl = voiceControl;
        _voiceCoordinator = voiceCoordinator;
        _ads = ads;
        _dispatcher = dispatcher;
        _audioCues = audioCues;
        _feedback = feedback;
        _timeProvider = timeProvider;
    }

    /// <summary>Skład partii wraz z informacją, kto odpadł.</summary>
    public ObservableCollection<PlayerListItem> Players { get; } = [];

    /// <summary>
    /// Zasady, na jakich ruszy partia — pokazywane przed jej rozpoczęciem.
    /// </summary>
    /// <remarks>
    /// Ekran przed grą odpowiada na pytania „w co gramy" i „na jakich zasadach", zamiast
    /// wypisywać sam skład: tryb, zestaw wydarzeń, czasy i sposób przechodzenia tur są
    /// rozsiane po trzech innych ekranach, a sprawdzać je trzeba właśnie teraz.
    /// </remarks>
    public ObservableCollection<GameSetupItem> SetupItems { get; } = [];

    /// <summary>
    /// Statystyki zakończonej partii.
    /// </summary>
    /// <remarks>
    /// Silnik liczy je i tak (liczba tur, liczba wydarzeń, czas, kolejność odpadania), więc
    /// zatrzymywanie ich w środku byłoby marnowaniem gotowej informacji. Wiersze mają tę samą
    /// postać co podsumowanie przed grą — ekran zamyka się tym samym układem, którym się
    /// otworzył.
    /// </remarks>
    public ObservableCollection<GameSetupItem> SummaryItems { get; } = [];

    /// <summary>Ostatni komunikat — polecenie ruchu albo informacja o zdarzeniu.</summary>
    /// <summary>Sposób prowadzenia rozgrywki, przełączany przyciskiem przy kole ruchu.</summary>
    [ObservableProperty]
    private GameControlMode _controlMode = GameControlMode.Manual;

    /// <summary>Nazwa bieżącego sposobu sterowania — dla czytnika ekranu i testów.</summary>
    [ObservableProperty]
    private string _controlModeText = string.Empty;

    /// <summary>Znak na przycisku sterowania.</summary>
    [ObservableProperty]
    private string _controlModeGlyph = string.Empty;

    /// <summary>Opis przycisku sterowania dla czytnika ekranu.</summary>
    [ObservableProperty]
    private string _controlModeDescription = string.Empty;

    [ObservableProperty]
    private string _announcementText = string.Empty;

    /// <summary>
    /// Zapowiedź wydarzenia, jeśli w tej turze jakieś padło.
    /// </summary>
    /// <remarks>
    /// Osobne pole, a nie wspólne z komunikatem o ruchu. Powód wyszedł w testach: oba
    /// komunikaty lecą jeden po drugim, a odświeżenie stanu po nich czyta z silnika
    /// ostatni komunikat o RUCHU — zapowiedź wydarzenia zdążyła się pojawić i zniknąć
    /// w tej samej chwili. Rozdzielone pola pokazują jedno i drugie.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEventText))]
    private string _eventText = string.Empty;

    /// <summary>Informacja o numerze tury albo o wstrzymaniu gry.</summary>
    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>
    /// Tytuł paska górnego.
    /// </summary>
    /// <remarks>
    /// Przed partią jest nazwą ekranu („Szczegóły gry"), a w trakcie — informacją o turze.
    /// Jeden napis w jednym miejscu, bo wcześniej pasek stał pusty, a numer tury zajmował
    /// osobny wiersz niżej: dwie linie na jedną informację i cały widok zsunięty w dół.
    /// </remarks>
    [ObservableProperty]
    private string _headerTitle = string.Empty;

    /// <summary>
    /// Stan mikrofonu opisany słowami.
    /// </summary>
    /// <remarks>
    /// Gracze potrzebują sygnału dźwiękowego, bo nie patrzą w ekran — ale ekran musi
    /// pokazywać to samo, kiedy ktoś jednak spojrzy albo gdy sygnały są wyciszone.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoiceLineText))]
    private string _voiceStatusText = string.Empty;

    /// <summary>Potwierdzenie ostatnio rozpoznanej komendy.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVoiceCommandText))]
    [NotifyPropertyChangedFor(nameof(VoiceLineText))]
    [NotifyPropertyChangedFor(nameof(IsVoiceLineVisible))]
    private string _voiceCommandText = string.Empty;

    /// <summary>Czy mikrofon w tej chwili słucha.</summary>
    [ObservableProperty]
    private bool _isListening;

    /// <summary>
    /// Czy pokazywać stan mikrofonu.
    /// </summary>
    /// <remarks>
    /// Przy wyłączonym sterowaniu głosem napis „Sterowanie głosem wyłączone" nie informuje
    /// o niczym, czego gracz by nie wiedział — sam je wyłączył — a zajmuje wiersz na ekranie
    /// czytanym z dwóch metrów. Zostaje tylko wtedy, gdy mówi coś nieoczekiwanego: że
    /// mikrofon słucha, czeka albo że urządzenie nie ma rozpoznawania mowy.
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVoiceLineVisible))]
    private bool _isVoiceStatusVisible;

    /// <summary>
    /// Czy w tej partii mogą pojawić się wydarzenia.
    /// </summary>
    /// <remarks>
    /// Ekran trzyma stałe miejsce na zapowiedź wydarzenia, żeby nic nie skakało, gdy ta się
    /// pojawia i znika. Przy grze bez wydarzeń to miejsce nigdy nie zostanie użyte, więc
    /// trzymanie go byłoby pustym pasem na ekranie, który i tak jest ciasny.
    /// <para>
    /// Domyślnie <see langword="true"/>: partia wznowiona po zamknięciu aplikacji nie przechodzi
    /// przez ekran przed grą, a wtedy lepiej zarezerwować miejsce niepotrzebnie niż pozwolić,
    /// żeby wydarzenie wepchnęło się w układ.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEventSlotVisible))]
    private bool _canShowEvents = true;

    /// <summary>Imię gracza, którego jest tura.</summary>
    [ObservableProperty]
    private string _currentPlayerName = string.Empty;

    /// <summary>Nazwa części ciała z wylosowanego ruchu.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMove))]
    private string _moveBodyPartText = string.Empty;

    /// <summary>Znak obrazkowy części ciała — ręka albo stopa ze strzałką strony.</summary>
    [ObservableProperty]
    private string _moveBodyPartSymbol = string.Empty;

    /// <summary>Nazwa koloru z wylosowanego ruchu.</summary>
    [ObservableProperty]
    private string _moveColorText = string.Empty;

    /// <summary>
    /// Nazwa wartości wyliczeniowej koloru, na przykład <c>Red</c>.
    /// </summary>
    /// <remarks>
    /// Warstwa prezentacji nie zna typów graficznych platformy, więc nie może podać gotowego
    /// koloru. Podaje jego <b>nazwę</b>, a ekran dobiera do niej odcień z palety — dzięki temu
    /// motyw jasny i ciemny mają własne wartości, a tu zostaje sama informacja o tym, co padło.
    /// </remarks>
    [ObservableProperty]
    private string _moveColorName = string.Empty;

    /// <summary>Ile sekund zostało w trwającym odliczaniu.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCountdown))]
    [NotifyPropertyChangedFor(nameof(IsCountdownUrgent))]
    private int _countdownSeconds;

    /// <summary>Opis tego, co jest odmierzane — czas na zadanie albo na ruch.</summary>
    [ObservableProperty]
    private string _countdownText = string.Empty;

    /// <summary>Podsumowanie zakończonej partii.</summary>
    [ObservableProperty]
    private string _summaryText = string.Empty;

    /// <summary>Czy partia jest rozpoczęta i niezakończona.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPlayTurn))]
    [NotifyPropertyChangedFor(nameof(IsNotRunning))]
    [NotifyPropertyChangedFor(nameof(IsBeforeGame))]
    [NotifyPropertyChangedFor(nameof(IsEventSlotVisible))]
    private bool _isRunning;

    /// <summary>Czy partia jest wstrzymana.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPlayTurn))]
    private bool _isPaused;

    /// <summary>
    /// Czy tryb gry przewiduje odpadanie graczy.
    /// </summary>
    /// <remarks>
    /// Widoczność przycisków należy do wierszy graczy (<see cref="PlayerListItem.CanEliminate"/>),
    /// bo przycisk jest przy każdym z nich. Tutaj trzymamy samą regułę, żeby przebudowa listy
    /// wiedziała, co ustawić.
    /// </remarks>
    [ObservableProperty]
    private bool _isEliminationEnabled = true;

    /// <summary>Czy partia się zakończyła.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBeforeGame))]
    private bool _isFinished;

    /// <summary>Czy da się rozpocząć partię — jest przynajmniej jeden gracz.</summary>
    [ObservableProperty]
    private bool _canStart;

    /// <summary>Czy skład jest pusty i nie ma z kim grać.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlayers))]
    private bool _hasNoPlayers;

    /// <summary>Czy jest kogo pokazać na liście graczy.</summary>
    public bool HasPlayers => !HasNoPlayers;

    /// <summary>Czy trzymać na ekranie miejsce na zapowiedź wydarzenia.</summary>
    public bool IsEventSlotVisible => IsRunning && CanShowEvents;

    /// <summary>
    /// Czy ekran ma trzymać miejsce na baner reklamowy.
    /// </summary>
    /// <remarks>
    /// Wysokość banera jest stała i zarezerwowana, ale tylko wtedy, gdy reklamy w tym wydaniu
    /// istnieją i wolno je pokazać — pusty pas na dole ekranu w buildzie bez reklam zabierałby
    /// miejsce za nic. Miejsce jest rezerwowane <b>przed</b> wczytaniem reklamy, bo baner
    /// wchodzący w gotowy układ przesuwałby przyciski pod palcem gracza; to jest zarazem
    /// wymóg polityki Google, nie tylko wygoda.
    /// </remarks>
    [ObservableProperty]
    private bool _isBannerVisible;

    /// <summary>Czy widoczne mają być przyciski sterowania turą.</summary>
    public bool CanPlayTurn => IsRunning && !IsPaused;

    /// <summary>
    /// Czy ekran pokazuje treść inną niż trwająca partia.
    /// </summary>
    /// <remarks>
    /// Ekran ma dwie części zamienne: trwająca partia ma układ <b>bez przewijania</b>, bo
    /// wszystko musi być widoczne naraz, a stan przed partią i podsumowanie mogą się przewijać,
    /// bo bywają dłuższe od ekranu. Ta właściwość przełącza jedną na drugą.
    /// </remarks>
    public bool IsNotRunning => !IsRunning;

    /// <summary>
    /// Czy ekran pokazuje stan przed partią.
    /// </summary>
    /// <remarks>
    /// Zakończona partia <b>nie</b> jest stanem przed partią, choć nic się w niej już nie
    /// dzieje: po ostatniej turze na ekranie zostaje podsumowanie z „Zagraj ponownie",
    /// a drugi przycisk rozpoczynający grę byłby tym samym wyjściem podanym dwa razy.
    /// </remarks>
    public bool IsBeforeGame => !IsRunning && !IsFinished;


    /// <summary>Czy w tej turze padło wydarzenie.</summary>
    public bool HasEventText => !string.IsNullOrEmpty(EventText);

    /// <summary>Czy jest co pokazać w potwierdzeniu komendy.</summary>
    public bool HasVoiceCommandText => !string.IsNullOrEmpty(VoiceCommandText);

    /// <summary>
    /// Jedna linia o głosie: rozpoznana komenda, a gdy jej nie ma — stan mikrofonu.
    /// </summary>
    /// <remarks>
    /// Dwa osobne wiersze zabierały na ekranie rozgrywki tyle miejsca, ile cały rząd przycisków,
    /// a nigdy nie były potrzebne jednocześnie: potwierdzenie komendy pojawia się dokładnie
    /// wtedy, gdy mikrofon przestał słuchać.
    /// </remarks>
    public string VoiceLineText => HasVoiceCommandText ? VoiceCommandText : VoiceStatusText;

    /// <summary>Czy linia o głosie ma być widoczna.</summary>
    public bool IsVoiceLineVisible => IsVoiceStatusVisible || HasVoiceCommandText;

    /// <summary>Czy trwa odliczanie.</summary>
    public bool HasCountdown => CountdownSeconds > 0;

    /// <summary>
    /// Czy odliczanie wchodzi w ostatnie sekundy.
    /// </summary>
    /// <remarks>
    /// Pięć sekund, bo tyle wystarcza na dokończenie ruchu, a mniej nie dałoby się już
    /// zauważyć. Ekran zmienia wtedy kolor liczby — to jedyny moment, w którym warto
    /// odciągnąć wzrok od koła z kolorem.
    /// </remarks>
    public bool IsCountdownUrgent => CountdownSeconds is > 0 and <= UrgentCountdownSeconds;

    /// <summary>Czy jest wylosowany ruch do pokazania.</summary>
    public bool HasMove => !string.IsNullOrEmpty(MoveBodyPartText);

    /// <inheritdoc />
    protected override async Task OnInitializeAsync()
    {
        // Wznowienie przerwanej partii ma pierwszeństwo nad wczytaniem składu:
        // jeśli jest co wznawiać, gracze pochodzą z zapisu partii, a nie z listy.
        if (await _engine.TryRestoreAsync())
        {
            RefreshFromEngine();

            return;
        }

        await LoadRosterAsync();
    }

    /// <inheritdoc />
    public override void OnAppearing()
    {
        if (_isSubscribed)
        {
            return;
        }

        _engine.StateChanged += OnEngineStateChanged;
        _engine.TurnPlayed += OnTurnPlayed;
        _engine.CountdownChanged += OnCountdownChanged;
        _engine.AnnouncementRaised += OnAnnouncementRaised;
        _engine.GameFinished += OnGameFinished;
        _voiceControl.StateChanged += OnVoiceStateChanged;
        _voiceControl.SilenceDetected += OnSilenceDetected;
        _voiceControl.CommandRecognized += OnVoiceCommandRecognized;
        _isSubscribed = true;

        RefreshFromEngine();
        RefreshVoiceStatus(_voiceControl.State);
        RefreshControlMode();

        // Skład i zasady wczytujemy przy każdym wejściu na ekran, a nie raz przy pierwszym:
        // gracze idą stąd do ustawień, trybów i wydarzeń właśnie po to, żeby coś zmienić,
        // i wracają sprawdzić, czy zmiana weszła.
        _ = LoadSetupAsync();

        // Sterowanie głosem działa wyłącznie na ekranie rozgrywki — mikrofon nie ma prawa
        // słuchać, kiedy gracze przeglądają ustawienia albo paczki wydarzeń.
        _ = ActivateVoiceControlAsync();

        // Baner też jest wyłącznie tutaj — na ekranie startowym i w ustawieniach reklama
        // niczego nie wnosi, a odbiera miejsce.
        _ads.BannerAllowedChanged += OnBannerAllowedChanged;
        IsBannerVisible = _ads.IsBannerAllowed;
        _ = ActivateAdsAsync();
    }

    /// <inheritdoc />
    public override void OnDisappearing()
    {
        if (!_isSubscribed)
        {
            return;
        }

        _engine.StateChanged -= OnEngineStateChanged;
        _engine.TurnPlayed -= OnTurnPlayed;
        _engine.CountdownChanged -= OnCountdownChanged;
        StopCountdownTimer();
        _engine.AnnouncementRaised -= OnAnnouncementRaised;
        _engine.GameFinished -= OnGameFinished;
        _voiceControl.StateChanged -= OnVoiceStateChanged;
        _voiceControl.SilenceDetected -= OnSilenceDetected;
        _voiceControl.CommandRecognized -= OnVoiceCommandRecognized;
        _ads.BannerAllowedChanged -= OnBannerAllowedChanged;
        IsBannerVisible = false;
        _isSubscribed = false;

        _ = LeaveGameAsync();
    }

    private void OnBannerAllowedChanged(object? sender, bool allowed) =>
        _dispatcher.Post(() => IsBannerVisible = allowed);

    /// <summary>Włącza reklamy na czas pobytu na ekranie rozgrywki.</summary>
    /// <remarks>
    /// Awarie są pochłaniane z logiem: reklama jest dodatkiem do gry, a nie jej częścią, więc
    /// nieudane przygotowanie zestawu SDK nie może przeszkodzić w rozgrywce.
    /// </remarks>
    private async Task ActivateAdsAsync()
    {
        try
        {
            await _ads.ActivateAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Nie udało się włączyć reklam na ekranie rozgrywki.");
        }
    }

    /// <summary>
    /// Zamyka mikrofon i wstrzymuje partię przy zejściu z ekranu.
    /// </summary>
    /// <remarks>
    /// Partia idąca dalej za plecami graczy to błąd, nie funkcja: nikt nie widzi komunikatu,
    /// a w trybie automatycznym tury lecą jedna po drugiej w opustoszałym pokoju. Zejście
    /// z ekranu — przyciskiem „Wróć", przejściem do ustawień albo minimalizacją aplikacji —
    /// oznacza to samo: gracze przestali grać.
    /// <para>
    /// Kolejność jest istotna. Najpierw <b>czekamy</b> na zamknięcie nasłuchu, dopiero potem
    /// wstrzymujemy partię: wstrzymanie zgłasza zmianę stanu, na którą koordynator odpowiada
    /// otwarciem okna nasłuchu, więc odwrotna kolejność dałaby parę sygnałów dźwiękowych już
    /// po wyjściu z ekranu.
    /// </para>
    /// <para>
    /// Wznowienie zostaje świadomą decyzją graczy — po powrocie partia czeka na „Wznów".
    /// </para>
    /// </remarks>
    private async Task LeaveGameAsync()
    {
        try
        {
            await _voiceCoordinator.DeactivateAsync();
            await _ads.DeactivateAsync();

            // Partia zakończona nie ma czego wznawiać, więc znika z pamięci silnika.
            // Bez tego powrót na ekran rozgrywki pokazywał podsumowanie poprzedniej gry
            // zamiast zasad nowej — silnik żyje tyle, co aplikacja, a zapisu zakończonej
            // partii nigdy nie było, bo takich się nie zapisuje.
            if (_engine.State == GameState.Finished)
            {
                await _engine.ResetAsync();

                return;
            }

            // Silnik sam pomija wstrzymanie, gdy nie ma czego wstrzymywać (partia nierozpoczęta
            // albo już wstrzymana), więc nie powtarzamy tu tego warunku.
            //
            // Bez zapowiedzi: ekranu już nie ma, a gracz właśnie świadomie poszedł gdzie
            // indziej — „Pauza" wypowiedziana w ustawieniach albo po powrocie na ekran
            // startowy jest samym hałasem. Trwający odczyt tury silnik i tak przerywa.
            await _engine.PauseAsync(announce: false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Nie udało się wstrzymać partii przy zejściu z ekranu.");
        }
    }

    /// <summary>Rozpoczyna nową partię ze składem z ekranu graczy.</summary>
    [RelayCommand]
    private Task StartGameAsync() => ExecuteSafeAsync(async () =>
    {
        IReadOnlyList<Player> players = await _playerRoster.GetAsync();

        if (players.Count == 0)
        {
            await ShowInfoAsync(StringKeys.Game.LabelAddPlayersFirst);

            return;
        }

        SummaryText = string.Empty;
        EventText = string.Empty;

        // Tryb gry i paczka są wczytywane raz, przy rozpoczęciu partii, i zostają z nią
        // do końca. Zmiana trybu albo paczki w trakcie gry nie zmienia zasad rozpoczętej
        // rozgrywki — gracze nie powinni odczuć, że reguły zmieniły się w połowie partii.
        GameModeDefinition mode = await _gameModes.GetActiveAsync();
        EventPack? activePack = await ResolveEventPackAsync(mode);

        await _engine.StartAsync(
            GameConfiguration.FromSettings(players, _settingsService.Current, mode, activePack));

        _feedback.Play(FeedbackMoment.GameStarted);
    });

    /// <summary>Przechodzi do następnej tury.</summary>
    [RelayCommand]
    private Task NextTurnAsync() => ExecuteSafeAsync(() => _engine.NextTurnAsync());

    /// <summary>Powtarza ostatni komunikat.</summary>
    [RelayCommand]
    private Task RepeatAsync() => ExecuteSafeAsync(() => _engine.RepeatAsync());

    /// <summary>Wstrzymuje albo wznawia rozgrywkę.</summary>
    [RelayCommand]
    private Task TogglePauseAsync() => ExecuteSafeAsync(() =>
        IsPaused ? _engine.ResumeAsync() : _engine.PauseAsync());

    /// <summary>
    /// Oznacza wskazanego gracza jako odpadniętego.
    /// </summary>
    /// <param name="player">Gracz, który odpadł.</param>
    /// <remarks>
    /// Zgłoszenie idzie z przycisku obok imienia, a nie z jednego przycisku na ekranie:
    /// przy kilku osobach na macie „kto odpadł" jest jedyną informacją, która się liczy.
    /// </remarks>
    [RelayCommand]
    private Task EliminatePlayerAsync(PlayerListItem player) => ExecuteSafeAsync(() =>
    {
        ArgumentNullException.ThrowIfNull(player);

        // Cała pigułka gracza jest celem dotknięcia, więc trafia w nią także dotknięcie
        // kogoś, kto już odpadł, i dotknięcie w trybie bez odpadania. Zgłoszenie musi
        // wtedy nie zrobić nic — a nie polecieć do silnika z nieważnym żądaniem.
        if (!player.CanEliminate)
        {
            return Task.CompletedTask;
        }

        _feedback.Play(FeedbackMoment.PlayerEliminated);

        return _engine.EliminatePlayerAsync(player.Id);
    });

    /// <summary>
    /// Rozpoczyna kolejną partię tym samym składem.
    /// </summary>
    /// <remarks>
    /// Po zakończeniu gry najczęstszą reakcją jest „jeszcze raz" — droga przez ekran startowy
    /// i listę graczy byłaby wtedy trzema dotknięciami za dużo.
    /// </remarks>
    [RelayCommand]
    private Task PlayAgainAsync()
    {
        SummaryText = string.Empty;

        return StartGameAsync();
    }

    /// <summary>
    /// Kończy partię przed czasem, po potwierdzeniu.
    /// </summary>
    /// <remarks>
    /// Pytanie jest tu od chwili, gdy przycisk przeniósł się do paska górnego: w narożniku
    /// ekranu trafia się w niego przypadkiem, a partia nie ma jak wrócić do stanu z przed
    /// zakończenia.
    /// </remarks>
    [RelayCommand]
    private Task EndGameAsync() => ExecuteSafeAsync(async () =>
    {
        bool confirmed = await Dialogs.ConfirmAsync(
            Localization[StringKeys.Game.EndConfirmTitle],
            Localization[StringKeys.Game.EndConfirmMessage],
            Localization[StringKeys.Game.ButtonEnd],
            Localization[StringKeys.Common.ButtonCancel]);

        if (!confirmed)
        {
            return;
        }

        await _engine.EndAsync();
    });

    /// <summary>
    /// Ustala paczkę wydarzeń dla partii.
    /// </summary>
    /// <remarks>
    /// Własny wybór gracza zawsze wygrywa. Tryb podpowiada paczkę tylko wtedy, gdy nikt nie
    /// wybrał żadnej — inaczej wejście w tryb Impreza kasowałoby paczkę, którą gracz właśnie
    /// sobie ułożył.
    /// </remarks>
    private async Task<EventPack?> ResolveEventPackAsync(GameModeDefinition mode)
    {
        EventPack? chosen = await _eventPacks.GetActiveAsync();

        if (chosen is not null || mode.DefaultEventPackNameKey is null)
        {
            return chosen;
        }

        IReadOnlyList<EventPack> packs = await _eventPacks.GetAllAsync();

        return packs.FirstOrDefault(pack =>
            string.Equals(pack.NameKey, mode.DefaultEventPackNameKey, StringComparison.Ordinal));
    }

    private async Task LoadRosterAsync()
    {
        IReadOnlyList<Player> players = await _playerRoster.GetAsync();

        ReplacePlayers(players);
    }

    /// <summary>
    /// Wczytuje skład i zasady rozpoczynanej partii.
    /// </summary>
    /// <remarks>
    /// Awarie są tu pochłaniane z logiem, a nie zgłaszane przez <c>ExecuteSafeAsync</c>:
    /// wywołanie leci z wejścia na ekran, bez czekania na wynik, więc komunikat o błędzie
    /// wyskakiwałby graczom w chwili, w której nic o nic nie pytali. Brak podsumowania nie
    /// przeszkadza w grze — przycisk rozpoczęcia działa dalej.
    /// </remarks>
    private async Task LoadSetupAsync()
    {
        try
        {
            if (_engine.Session?.IsRunning == true)
            {
                return;
            }

            await LoadRosterAsync();

            GameModeDefinition mode = await _gameModes.GetActiveAsync();
            EventPack? pack = await ResolveEventPackAsync(mode);

            GameSetup setup = GameSetup.FromSettings(_settingsService.Current, mode, pack);

            CanShowEvents = setup.AreEventsEnabled;

            BuildSetupItems(setup, mode);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Nie udało się wczytać zasad rozpoczynanej partii.");
        }
    }

    /// <summary>Składa wiersze podsumowania zasad z gotowych nastaw partii.</summary>
    private void BuildSetupItems(GameSetup setup, GameModeDefinition mode)
    {
        SetupItems.Clear();

        SetupItems.Add(new GameSetupItem(
            "☰",
            Localization[StringKeys.Game.SetupMode],
            Localization[mode.NameKey]));

        SetupItems.Add(new GameSetupItem(
            "⚄",
            Localization[StringKeys.Game.SetupEvents],
            DescribeEvents(setup)));

        SetupItems.Add(new GameSetupItem(
            "◷",
            Localization[StringKeys.Game.CountdownMove],
            DescribeSeconds(setup.MoveTime)));

        SetupItems.Add(new GameSetupItem(
            "◷",
            Localization[StringKeys.Game.CountdownTask],
            DescribeSeconds(setup.TaskTime)));

        SetupItems.Add(new GameSetupItem(
            "⟳",
            Localization[StringKeys.Game.SetupTurnAdvance],
            Localization[setup.TurnAdvanceMode == TurnAdvanceMode.Automatic
                ? StringKeys.Game.SetupTurnAutomatic
                : StringKeys.Game.SetupTurnManual]));

        SetupItems.Add(new GameSetupItem(
            "◉",
            Localization[StringKeys.Settings.LabelVoiceControl],
            Localization[setup.IsVoiceControlEnabled
                ? StringKeys.Common.LabelOn
                : StringKeys.Common.LabelOff]));

        SetupItems.Add(new GameSetupItem(
            "✕",
            Localization[StringKeys.Game.SetupElimination],
            Localization[setup.EliminationRule == EliminationRule.NoElimination
                ? StringKeys.Game.SetupEliminationNone
                : StringKeys.Game.SetupEliminationManual]));
    }

    /// <summary>
    /// Opisuje wydarzenia rozpoczynanej partii.
    /// </summary>
    /// <remarks>
    /// Nazwa paczki wbudowanej jest kluczem zasobu, a paczki użytkownika własnym napisem,
    /// którego nie tłumaczymy.
    /// </remarks>
    private string DescribeEvents(GameSetup setup)
    {
        if (!setup.AreEventsEnabled || setup.EventPack is null)
        {
            return Localization[StringKeys.Game.SetupNoEvents];
        }

        EventPack pack = setup.EventPack;

        string name = pack.NameKey is null ? pack.Name : Localization[pack.NameKey];

        return Localization.GetFormattedString(
            StringKeys.Game.SetupEventPackFormat,
            StringCatalog.Ui,
            name,
            pack.Events.Count);
    }

    private string DescribeSeconds(TimeSpan value) => Localization.GetFormattedString(
        StringKeys.Game.SetupSecondsFormat,
        StringCatalog.Ui,
        (int)Math.Round(value.TotalSeconds));

    /// <summary>Odczytuje cały widoczny stan z silnika.</summary>
    private void RefreshFromEngine()
    {
        GameSession? session = _engine.Session;

        IsRunning = session?.IsRunning ?? false;
        IsPaused = _engine.State == GameState.Paused;
        IsEliminationEnabled = _engine.IsEliminationEnabled;
        IsFinished = _engine.State == GameState.Finished;

        if (session is null)
        {
            StatusText = string.Empty;
            HeaderTitle = Localization[StringKeys.Game.SetupTitle];

            return;
        }

        ReplacePlayers(session.Players);

        // Powrót na ekran w trakcie partii musi pokazać aktualną turę, a nie puste miejsce
        // po komunikacie, który padł, kiedy ekranu nie było.
        if (session.CurrentTurn is { } currentTurn)
        {
            ShowMove(currentTurn);
        }
        else
        {
            ClearMove();
        }

        AnnouncementText = _engine.LastAnnouncement?.Text ?? string.Empty;

        StatusText = IsPaused
            ? Localization[StringKeys.Game.LabelPaused]
            : Localization.GetFormattedString(
                StringKeys.Game.LabelTurn,
                StringCatalog.Ui,
                session.TurnNumber);

        HeaderTitle = IsBeforeGame ? Localization[StringKeys.Game.SetupTitle] : StatusText;
    }

    /// <summary>
    /// Przebudowuje listę graczy.
    /// </summary>
    /// <remarks>
    /// Przycisk odpadnięcia jest przy każdym grającym uczestniku, a nie tylko przy tym,
    /// którego jest tura: upadek zdarza się także wtedy, gdy ruch wykonuje ktoś inny.
    /// </remarks>
    private void ReplacePlayers(IReadOnlyList<Player> players)
    {
        // Przebudowa dopiero wtedy, gdy skład naprawdę się zmienił. Ta metoda jest wołana przy
        // każdym komunikacie, czyli kilka razy na turę, a wyczyszczenie i ponowne wypełnienie
        // listy każe widokowi przerysować wszystkie wiersze — na ekranie widać mignięcie
        // składu, a układ przelicza się bez powodu.
        if (!HasRosterChanged(players))
        {
            return;
        }

        Players.Clear();

        foreach (Player player in players)
        {
            Players.Add(new PlayerListItem(
                player,
                canEliminate: IsRunning && IsEliminationEnabled && !player.IsEliminated));
        }

        CanStart = players.Count > 0;
        HasNoPlayers = players.Count == 0;
    }

    /// <summary>Sprawdza, czy skład różni się od tego, który jest już na ekranie.</summary>
    private bool HasRosterChanged(IReadOnlyList<Player> players)
    {
        if (players.Count != Players.Count)
        {
            return true;
        }

        bool eliminationAllowed = IsRunning && IsEliminationEnabled;

        for (int index = 0; index < players.Count; index++)
        {
            Player player = players[index];
            PlayerListItem shown = Players[index];

            if (shown.Id != player.Id
                || !string.Equals(shown.Name, player.Name, StringComparison.Ordinal)
                || shown.IsEliminated != player.IsEliminated
                || shown.CanEliminate != (eliminationAllowed && !player.IsEliminated))
            {
                return true;
            }
        }

        return false;
    }

    private void OnEngineStateChanged(object? sender, GameState state) => RefreshFromEngine();

    /// <summary>
    /// Czyści zapowiedź wydarzenia na początku każdej tury.
    /// </summary>
    /// <remarks>
    /// Wydarzenie dotyczy jednej tury, więc nie może zostać na ekranie w następnej.
    /// Kasujemy je na zdarzeniu rozegranej tury, a nie przy komunikacie o ruchu: silnik
    /// czyta najpierw wydarzenie, a dopiero po przerwie ruch, więc kasowanie przy ruchu
    /// zdejmowałoby z ekranu wydarzenie tej samej tury, chwilę po jego pokazaniu.
    /// </remarks>
    private void OnTurnPlayed(object? sender, Turn turn)
    {
        EventText = string.Empty;
        VoiceCommandText = string.Empty;

        ShowMove(turn);

        // Efekt pada tu, a nie przy komunikacie o ruchu: silnik zgłasza rozegraną turę przed
        // odczytaniem czegokolwiek, więc dźwięk zdąży wybrzmieć i zabrzmi jak akcent przed
        // poleceniem. Sam serwis pilnuje, żeby nie wejść w słowo mowie.
        _feedback.Play(FeedbackMoment.MoveRevealed);
    }

    /// <summary>
    /// Pokazuje wylosowany ruch w rozbiciu na gracza, część ciała i kolor.
    /// </summary>
    /// <remarks>
    /// Nazwy części ciała i kolorów pochodzą z <b>katalogu głosowego</b>, a nie z osobnych
    /// kluczy dla ekranu. Powód jest jeden: gracz ma widzieć dokładnie te słowa, które słyszy.
    /// Dwa zestawy tłumaczeń tego samego rozjechałyby się przy pierwszej poprawce.
    /// </remarks>
    private void ShowMove(Turn turn)
    {
        CurrentPlayerName = turn.Player.Name;
        MoveColorName = turn.Move.Color.ToString();

        MoveBodyPartText = Localization.GetString(
            StringKeys.Voice.BodyPartPrefix + turn.Move.Part,
            StringCatalog.Voice);

        MoveColorText = Localization.GetString(
            StringKeys.Voice.ColorPrefix + turn.Move.Color,
            StringCatalog.Voice);

        MoveBodyPartSymbol = GetBodyPartSymbol(turn.Move.Part);
    }

    /// <summary>
    /// Dobiera znak obrazkowy do części ciała.
    /// </summary>
    /// <remarks>
    /// Ręka i stopa to osobne znaki, a stronę pokazuje strzałka — samych emotek dłoni nie da
    /// się rozróżnić z dwóch metrów, a lewej i prawej stopy nie ma w ogóle. Strzałka jest
    /// jednoznaczna i czytelna z odległości, na której napis dopiero się domyśla.
    /// <para>
    /// Znaki są w kodzie, a nie w plikach zasobów, bo są identyczne w każdym języku —
    /// tłumaczenie ich oznaczałoby dwa razy tę samą wartość i dwa miejsca do rozjechania się.
    /// </para>
    /// <para>
    /// Po każdej strzałce stoi <b>selektor wariantu</b> (U+FE0F), i to nie ozdoba: bez niego
    /// system sam wybiera krój, a wybierał różne — strzałka w prawo wychodziła grubą,
    /// czarną emotką, a ta w lewo cienkim znakiem tekstowym. Dwie strzałki o różnej grubości
    /// znaczą to samo, ale wyglądają na dwie różne informacje. Selektor wymusza wariant
    /// obrazkowy dla obu.
    /// </para>
    /// </remarks>
    private static string GetBodyPartSymbol(BodyPart part) => part switch
    {
        BodyPart.RightHand => "✋ ➡️",
        BodyPart.LeftHand => "✋ ⬅️",
        BodyPart.RightFoot => "🦶 ➡️",
        BodyPart.LeftFoot => "🦶 ⬅️",
        _ => string.Empty,
    };

    private void ClearMove()
    {
        CurrentPlayerName = string.Empty;
        MoveBodyPartText = string.Empty;
        MoveColorText = string.Empty;
        MoveColorName = string.Empty;
        MoveBodyPartSymbol = string.Empty;
    }

    private void OnAnnouncementRaised(object? sender, Announcement announcement)
    {
        if (announcement.Kind == AnnouncementKind.Event)
        {
            EventText = announcement.Text;

            _feedback.Play(FeedbackMoment.EventAnnounced);
        }
        else
        {
            AnnouncementText = announcement.Text;

            // Koniec partii zdejmuje z ekranu wydarzenie ostatniej tury — nie ma już tury,
            // której mogłoby dotyczyć.
            if (announcement.Kind == AnnouncementKind.GameEnd)
            {
                EventText = string.Empty;
            }
        }

        // Skład odświeżamy przy każdym komunikacie, bo eliminacja gracza zmienia listę,
        // a nie zmienia stanu rozgrywki.
        if (_engine.Session is not null)
        {
            ReplacePlayers(_engine.Session.Players);
        }
    }

    /// <summary>
    /// Przełącza sposób sterowania na następny w kolejności: ręczny, automatyczny, głosowy.
    /// </summary>
    /// <remarks>
    /// Przełącznik, nie lista wyboru. Okno z trzema opcjami zasłoniłoby koło ruchu dokładnie
    /// w chwili, w której gracz na nie patrzy — a sięga po ten przycisk właśnie wtedy, gdy
    /// bieżący sposób zawodzi. Trzy stany oznaczają najwyżej dwa dotknięcia do dowolnego
    /// z nich, a ikona mówi, gdzie się jest.
    /// </remarks>
    [RelayCommand]
    private Task CycleControlModeAsync() =>
        ExecuteSafeAsync(() => ApplyControlModeAsync(GameControlModes.Next(ControlMode)));

    /// <summary>
    /// Zapisuje wybrany sposób sterowania i stosuje go do trwającej partii.
    /// </summary>
    /// <param name="mode">Nowy sposób sterowania.</param>
    /// <remarks>
    /// Kolejność jest istotna. Najpierw ustawienia, bo one są jedynym źródłem prawdy i to
    /// z nich ekran ustawień odczyta ten sam stan. Potem silnik, żeby odliczanie ruszyło od
    /// nowa pod nową wartością. Na końcu mikrofon, bo dopiero wtedy wiadomo, czy w ogóle da
    /// się go otworzyć.
    /// <para>
    /// Gdy mikrofon odmówi — brak zgody albo urządzenie bez rozpoznawania mowy — tryb
    /// <b>wraca na ręczny</b>. Zostawienie napisu „głosowo" nad grą, której nikt nie słucha,
    /// byłoby gorsze niż samo niepowodzenie: gracz czekałby na reakcję, która nie nadejdzie.
    /// </para>
    /// </remarks>
    private async Task ApplyControlModeAsync(GameControlMode mode)
    {
        await _settingsService.UpdateAsync(settings => GameControlModes.Apply(settings, mode));

        AppSettings settingsPoZmianie = _settingsService.Current;
        GameModeDefinition tryb = await _gameModes.GetActiveAsync();

        await _engine.ChangeTurnControlAsync(
            settingsPoZmianie.TurnAdvanceMode,
            GameSetup.MoveTimeFor(settingsPoZmianie, tryb));

        if (mode == GameControlMode.Voice)
        {
            await ActivateVoiceControlAsync();

            if (_voiceControl.State is VoiceControlState.Unavailable or VoiceControlState.Disabled)
            {
                Logger.LogInformation(
                    "Sterowanie głosem niedostępne ({State}) — wracam na sterowanie ręczne.",
                    _voiceControl.State);

                await ApplyControlModeAsync(GameControlMode.Manual);

                return;
            }
        }
        else
        {
            await _voiceCoordinator.DeactivateAsync();
            RefreshVoiceStatus(_voiceControl.State);
        }

        RefreshControlMode();

        // Komunikat pada na końcu, a nie od razu po dotknięciu: tryb głosowy potrafi wrócić
        // na ręczny, gdy mikrofon odmówi, a wtedy gracz musi zobaczyć stan faktyczny,
        // nie zamierzony.
        await Dialogs.ShowToastAsync(Localization.GetFormattedString(
            StringKeys.Game.ControlModeChanged,
            StringCatalog.Ui,
            ControlModeText));
    }

    /// <summary>Odczytuje sposób sterowania z ustawień i odświeża napisy na przycisku.</summary>
    private void RefreshControlMode()
    {
        ControlMode = GameControlModes.From(_settingsService.Current);

        ControlModeText = Localization[ControlMode switch
        {
            GameControlMode.Automatic => StringKeys.Game.ControlAutomatic,
            GameControlMode.Voice => StringKeys.Game.ControlVoice,
            _ => StringKeys.Game.ControlManual,
        }];

        // Znaki podaje warstwa prezentacji, nie pliki tłumaczeń — są identyczne w każdym
        // języku, więc w zasobach byłyby dziesięcioma kopiami tej samej wartości.
        //
        // Jednobarwne, nie emotki. Kolorowa emotka bierze się z osobnej czcionki systemowej
        // i świeci własnym kolorem na tle, które ma własną paletę — reszta symboli
        // w interfejsie jest jednobarwna, bo znak tekstowy przyjmuje kolor tekstu.
        //
        // Dwa z tych trzech znaków to <b>własne słownictwo tej aplikacji</b>: ◉ oznacza
        // sterowanie głosem w ustawieniach i w podsumowaniu zasad partii, ◷ oznacza czas.
        // Przełącznik mówi więc tym samym językiem, co ekrany, z których gracz już te
        // symbole zna. Dłoń jest nowa, bo na „ręcznie" aplikacja nie miała dotąd znaku.
        ControlModeGlyph = ControlMode switch
        {
            GameControlMode.Automatic => "◷",
            GameControlMode.Voice => "◉",
            _ => "☛",
        };

        ControlModeDescription = Localization.GetFormattedString(
            StringKeys.Game.ControlModeDescription,
            StringCatalog.Ui,
            ControlModeText);
    }

    /// <summary>
    /// Włącza sterowanie głosem, jeśli jest dozwolone w ustawieniach.
    /// </summary>
    /// <remarks>
    /// Brak zgody na mikrofon i brak rozpoznawania na urządzeniu nie są tu błędem —
    /// gra działa na przyciskach, a stan mikrofonu widać na ekranie.
    /// </remarks>
    private async Task ActivateVoiceControlAsync()
    {
        try
        {
            await _voiceCoordinator.ActivateAsync();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Nie udało się włączyć sterowania głosem.");
        }

        RefreshVoiceStatus(_voiceControl.State);
    }

    /// <summary>
    /// Uruchamia albo zatrzymuje odliczanie na ekranie.
    /// </summary>
    /// <remarks>
    /// Silnik podaje tylko, co i od kiedy odmierza. Tykanie sekundnika należy tutaj, bo to
    /// warstwa prezentacji wie, jak często ekran ma się przerysowywać — a widok wolno ruszać
    /// wyłącznie z wątku interfejsu, stąd <see cref="IUiDispatcher"/>.
    /// </remarks>
    private void OnCountdownChanged(object? sender, TurnCountdown? countdown)
    {
        _dispatcher.Post(() =>
        {
            _countdown = countdown;

            if (countdown is null)
            {
                StopCountdownTimer();
                CountdownSeconds = 0;
                CountdownText = string.Empty;

                return;
            }

            CountdownText = Localization[countdown.Kind == TurnCountdownKind.Task
                ? StringKeys.Game.CountdownTask
                : StringKeys.Game.CountdownMove];

            // Zerujemy licznik przed pierwszym przeliczeniem: inaczej odliczanie
            // rozpoczynające się od tej samej liczby, na której skończyło się poprzednie,
            // nie wykryłoby zmiany sekundy i nie tyknęłoby ani razu.
            CountdownSeconds = 0;
            UpdateCountdownSeconds();

            _countdownTimer ??= _timeProvider.CreateTimer(
                _ => _dispatcher.Post(UpdateCountdownSeconds),
                state: null,
                dueTime: TimeSpan.FromSeconds(1),
                period: TimeSpan.FromSeconds(1));
        });
    }

    /// <summary>
    /// Przelicza pozostały czas na sekundy do pokazania i tyka jak zegar.
    /// </summary>
    /// <remarks>
    /// Tykanie jest odtwarzane <b>tylko przy zmianie sekundy</b>, a nie przy każdym
    /// przeliczeniu — inaczej pierwsze i ostatnie odświeżenie dawałoby podwójny dźwięk.
    /// <para>
    /// Milczy w trakcie nasłuchu: tyknięcie wpadające do otwartego mikrofonu zmarnowałoby
    /// sesję rozpoznawania na dźwięk, który sami wydaliśmy.
    /// </para>
    /// </remarks>
    private void UpdateCountdownSeconds()
    {
        if (_countdown is null)
        {
            return;
        }

        TimeSpan remaining = _countdown.Total - _timeProvider.GetElapsedTime(_countdown.StartedAt);
        int seconds = remaining > TimeSpan.Zero ? (int)Math.Ceiling(remaining.TotalSeconds) : 0;

        if (seconds == CountdownSeconds)
        {
            return;
        }

        CountdownSeconds = seconds;

        // Tykanie idzie osobnym portem (generator tonów, nie próbka), ale jest dźwiękiem gry
        // jak każdy inny — więc słucha tego samego włącznika. Gracz, który wyciszył aplikację,
        // nie spodziewa się, że zegar dalej tyka.
        if (seconds > 0 && !IsListening && _settingsService.Current.AreSoundsEnabled)
        {
            _ = PlayTickSafelyAsync();
        }
    }

    private async Task PlayTickSafelyAsync()
    {
        try
        {
            await _audioCues.PlayAsync(AudioCue.CountdownTick);
        }
        catch (Exception exception)
        {
            // Brak tykania nie może przerwać partii — odliczanie widać na ekranie.
            Logger.LogWarning(exception, "Nie udało się odtworzyć tyknięcia odliczania.");
        }
    }

    private void StopCountdownTimer()
    {
        _countdownTimer?.Dispose();
        _countdownTimer = null;
    }

    private void OnVoiceStateChanged(object? sender, VoiceControlState state) =>
        RefreshVoiceStatus(state);

    private void OnVoiceCommandRecognized(object? sender, VoiceCommandType command) =>
        VoiceCommandText = Localization.GetFormattedString(
            StringKeys.Game.VoiceCommandHeard,
            StringCatalog.Ui,
            Localization[StringKeys.VoiceCommands.NamePrefix + command]);

    /// <summary>Przekłada stan nasłuchu na tekst dla graczy.</summary>
    private void RefreshVoiceStatus(VoiceControlState state)
    {
        IsListening = state == VoiceControlState.Listening;

        // Pasek stanu mikrofonu należy do trybu głosowego, więc pyta o tryb, a nie tylko
        // o stan serwisu. Po wyłączeniu sterowania głosem serwis schodzi na „bezczynny",
        // a nie „wyłączony" — i pasek zostawał na ekranie z napisem o wyłączonym mikrofonie
        // w partii, w której mikrofon nie ma już nic do roboty.
        //
        // Źródłem jest tu ustawienie, a nie właściwość ControlMode, bo ta bywa odświeżana
        // po tej metodzie — kolejność wywołań nie może decydować o tym, co widać.
        IsVoiceStatusVisible =
            GameControlModes.From(_settingsService.Current) == GameControlMode.Voice
            && state != VoiceControlState.Disabled;

        VoiceStatusText = Localization[state switch
        {
            VoiceControlState.Listening => StringKeys.Game.VoiceListening,
            VoiceControlState.Waiting => StringKeys.Game.VoiceWaiting,
            VoiceControlState.Idle => StringKeys.Game.VoiceIdle,
            VoiceControlState.Unavailable => StringKeys.Game.VoiceUnavailable,
            _ => StringKeys.Game.VoiceDisabled,
        }];
    }

    /// <summary>
    /// Podpowiada, gdzie szukać, gdy mikrofon milczy sesja po sesji.
    /// </summary>
    /// <remarks>
    /// Toast, nie okno: gracz leży na macie i nie ma czym odklikać komunikatu, a partia ma
    /// iść dalej. Podpowiedź nie zatrzymuje nasłuchu — najczęstszą przyczyną jest globalny
    /// przełącznik mikrofonu w szybkich ustawieniach Androida, który gracz może włączyć bez
    /// wychodzenia z gry, i wtedy komendy zaczynają działać same.
    /// </remarks>
    private void OnSilenceDetected(object? sender, EventArgs e) =>
        _dispatcher.Post(async () =>
            await Dialogs.ShowToastAsync(Localization[StringKeys.Game.MicrophoneSilent]));

    private void OnGameFinished(object? sender, GameSummary summary)
    {
        SummaryText = summary.Winner is null
            ? Localization.GetFormattedString(
                StringKeys.Game.SummaryNoWinner,
                StringCatalog.Ui,
                summary.TurnCount)
            : Localization.GetFormattedString(
                StringKeys.Game.SummaryWinner,
                StringCatalog.Ui,
                summary.Winner.Name,
                summary.TurnCount);

        BuildSummaryItems(summary);

        _feedback.Play(FeedbackMoment.GameFinished);
    }

    /// <summary>Składa wiersze statystyk zakończonej partii.</summary>
    /// <remarks>
    /// Kolejność odpadania pojawia się tylko wtedy, gdy ktoś odpadł: w trybie dla dzieci
    /// nikt nie odpada, a puste miejsce po wierszu wyglądałoby na brakującą informację.
    /// </remarks>
    private void BuildSummaryItems(GameSummary summary)
    {
        SummaryItems.Clear();

        SummaryItems.Add(new GameSetupItem(
            "♟",
            Localization[StringKeys.Players.Title],
            summary.PlayerCount.ToString(CultureInfo.CurrentCulture)));

        SummaryItems.Add(new GameSetupItem(
            "⟳",
            Localization[StringKeys.Game.SummaryTurns],
            summary.TurnCount.ToString(CultureInfo.CurrentCulture)));

        SummaryItems.Add(new GameSetupItem(
            "⚄",
            Localization[StringKeys.Game.SetupEvents],
            summary.EventCount.ToString(CultureInfo.CurrentCulture)));

        SummaryItems.Add(new GameSetupItem(
            "◷",
            Localization[StringKeys.Game.SummaryDuration],
            Localization.GetFormattedString(
                StringKeys.Game.SummaryDurationFormat,
                StringCatalog.Ui,
                (int)summary.Duration.TotalMinutes,
                summary.Duration.Seconds)));

        if (summary.EliminationOrder.Count == 0)
        {
            return;
        }

        SummaryItems.Add(new GameSetupItem(
            "✕",
            Localization[StringKeys.Game.SummaryEliminated],
            string.Join(", ", summary.EliminationOrder.Select(player => player.Name))));
    }

    /// <summary>
    /// Przechodzi do ustawień z ekranu podsumowania.
    /// </summary>
    /// <remarks>
    /// Po zakończeniu partii druga najczęstsza reakcja po „jeszcze raz" to „następnym razem
    /// dajmy więcej czasu" — a ustawienia były wtedy dwa ekrany dalej, przez ekran startowy.
    /// </remarks>
    [RelayCommand]
    private Task GoToSettingsAsync() => ExecuteSafeAsync(() => Navigation.GoToAsync(Routes.Settings));

}
