using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Feedback;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Application.VoiceControl;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Navigation;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Ekran ustawień — język, odczyt głosowy i sterowanie głosem.
/// </summary>
/// <remarks>
/// Pozostałe ustawienia (dźwięki, wibracje) dokłada Etap 12.
/// </remarks>
public partial class SettingsViewModel : NavigableViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ITextToSpeechService _textToSpeech;
    private readonly IAnnouncementSpeaker _speaker;
    private readonly IAnnouncementBuilder _announcementBuilder;
    private readonly IVoiceControlService _voiceControl;
    private readonly IGameFeedback _feedback;
    private readonly IUserDataService _userData;
    private readonly IExternalBrowser _browser;

    /// <summary>
    /// Blokuje zapis ustawień w czasie wczytywania stanu do formularza.
    /// </summary>
    /// <remarks>
    /// Ustawienie właściwości powiązanej z suwakiem czy przełącznikiem wywołuje metodę
    /// <c>On…Changed</c>, która normalnie zapisuje zmianę. Przy wczytywaniu wartości
    /// <b>pochodzą</b> z ustawień, więc zapis byłby zapisem tego samego.
    /// </remarks>
    private bool _isLoading;
    private bool _isSubscribed;

    /// <summary>Tworzy ViewModel ekranu ustawień.</summary>
    /// <param name="navigation">Serwis nawigacji.</param>
    /// <param name="settingsService">Ustawienia aplikacji.</param>
    /// <param name="textToSpeech">Syntezator mowy — źródło listy dostępnych głosów.</param>
    /// <param name="speaker">Odczyt komunikatów — odtwarza próbkę głosu.</param>
    /// <param name="announcementBuilder">Budowanie komunikatów — źródło tekstu próbki.</param>
    /// <param name="voiceControl">Sterowanie głosem — sprawdza zgodę i możliwości urządzenia.</param>
    /// <param name="feedback">Efekty dźwiękowe — odtwarza próbkę przy sprawdzaniu dźwięku.</param>
    /// <param name="userData">Dane użytkownika — przywracanie ustawień i kasowanie danych.</param>
    /// <param name="localization">Serwis tłumaczeń — źródło listy dostępnych języków.</param>
    /// <param name="logger">Logger tego ViewModelu.</param>
    /// <param name="dialogService">Serwis komunikatów dla użytkownika.</param>
    /// <param name="browser">Przeglądarka systemowa — otwiera politykę prywatności.</param>
    public SettingsViewModel(
        INavigationService navigation,
        ISettingsService settingsService,
        ITextToSpeechService textToSpeech,
        IAnnouncementSpeaker speaker,
        IAnnouncementBuilder announcementBuilder,
        IVoiceControlService voiceControl,
        IGameFeedback feedback,
        IUserDataService userData,
        ILocalizationService localization,
        ILogger<SettingsViewModel> logger,
        IDialogService dialogService,
        IExternalBrowser browser)
        : base(navigation, logger, dialogService, localization)
    {
        ArgumentNullException.ThrowIfNull(browser);

        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(textToSpeech);
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(announcementBuilder);
        ArgumentNullException.ThrowIfNull(voiceControl);
        ArgumentNullException.ThrowIfNull(feedback);
        ArgumentNullException.ThrowIfNull(userData);

        _voiceControl = voiceControl;
        _feedback = feedback;
        _userData = userData;
        _settingsService = settingsService;
        _textToSpeech = textToSpeech;
        _speaker = speaker;
        _announcementBuilder = announcementBuilder;
        _browser = browser;

        MoveTime = new SecondsSetting(
            AppSettings.MinMoveTime,
            AppSettings.MaxMoveTime,
            value => SaveTimes(settings => settings with { MoveTime = value }));

        TaskTime = new SecondsSetting(
            AppSettings.MinTaskTime,
            AppSettings.MaxTaskTime,
            value => SaveTimes(settings => settings with { TaskTime = value }));

        VoiceListeningDelay = new SecondsSetting(
            AppSettings.MinVoiceListeningDelay,
            AppSettings.MaxVoiceListeningDelay,
            value => SaveTimes(settings => settings with { VoiceListeningDelay = value }));

        AvailableLanguages = [.. localization.SupportedCultures.Select(LanguageOption.From)];

        AvailableThemes =
        [
            .. Enum.GetValues<AppThemePreference>()
                .Select(preference => new ThemeOption(
                    preference,
                    localization[StringKeys.Settings.ThemePrefix + preference])),
        ];

        _selectedLanguage = AvailableLanguages.FirstOrDefault(option =>
            option.LanguageCode == localization.CurrentCulture.TwoLetterISOLanguageName);
    }

    /// <summary>Języki, w których dostępna jest aplikacja.</summary>
    public IReadOnlyList<LanguageOption> AvailableLanguages { get; }

    /// <summary>
    /// Dostępne motywy kolorystyczne.
    /// </summary>
    /// <remarks>
    /// Lista powstaje z wartości wyliczenia, więc dołożenie motywu nie wymaga zmiany tego
    /// kodu — wystarczy nowa wartość i klucz tłumaczenia.
    /// </remarks>
    public IReadOnlyList<ThemeOption> AvailableThemes { get; }

    /// <summary>Głosy zgłoszone przez syntezator, z pozycją „domyślny systemowy" na czele.</summary>
    public ObservableCollection<VoiceOption> AvailableVoices { get; } = [];

    /// <summary>Aktualnie wybrany język.</summary>
    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    /// <summary>Aktualnie wybrany motyw.</summary>
    [ObservableProperty]
    private ThemeOption? _selectedTheme;

    /// <summary>Czy aplikacja czyta komunikaty na głos.</summary>
    [ObservableProperty]
    private bool _isTextToSpeechEnabled = true;

    /// <summary>Wybrany głos.</summary>
    [ObservableProperty]
    private VoiceOption? _selectedVoice;

    /// <summary>Tempo mowy.</summary>
    /// <remarks>
    /// Typ <see cref="double"/>, bo suwak operuje na <see cref="double"/>, a nieudana
    /// konwersja w powiązaniu XAML nie zgłasza błędu — po prostu cicho nie działa.
    /// </remarks>
    [ObservableProperty]
    private double _speechRate = AppSettings.Default.SpeechRate;

    /// <summary>Wysokość głosu.</summary>
    [ObservableProperty]
    private double _speechPitch = AppSettings.Default.SpeechPitch;

    /// <summary>Czy sterowanie głosem jest włączone.</summary>
    [ObservableProperty]
    private bool _isVoiceControlEnabled;

    /// <summary>Czy tury następują automatycznie.</summary>
    [ObservableProperty]
    private bool _isAutomaticTurnAdvance;

    /// <summary>Czy efekty dźwiękowe są włączone.</summary>
    [ObservableProperty]
    private bool _areSoundsEnabled = true;

    /// <summary>
    /// Głośność efektów dźwiękowych.
    /// </summary>
    /// <remarks>
    /// Typ <see cref="double"/>, bo suwak operuje na <see cref="double"/> — nieudana konwersja
    /// w powiązaniu XAML nie zgłasza błędu, tylko cicho nie działa.
    /// </remarks>
    [ObservableProperty]
    private double _soundVolume = AppSettings.Default.SoundVolume;

    /// <summary>Czy wibracje są włączone.</summary>
    [ObservableProperty]
    private bool _areHapticsEnabled = true;

    /// <summary>Czy animacje interfejsu są włączone.</summary>
    /// <remarks>
    /// Osobno od systemowego ograniczenia animacji: wyłączenie animacji w ustawieniach
    /// dostępności Androida wyłącza je i tak, a ten przełącznik pozwala uspokoić sam ekran
    /// gry bez zmiany zachowania całego telefonu.
    /// </remarks>
    [ObservableProperty]
    private bool _areAnimationsEnabled = true;

    /// <summary>Czy urządzenie zgłosiło choć jeden głos poza domyślnym systemowym.</summary>
    public bool HasVoices => AvailableVoices.Count > 1;

    /// <summary>Czas na wykonanie ruchu.</summary>
    /// <remarks>
    /// W trybie automatycznym po tym czasie rusza następna tura; w ręcznym wartość nie jest
    /// używana. Tryb gry skaluje ją swoim mnożnikiem — Hardcore o połowę w dół, tryb dla
    /// dzieci o połowę w górę.
    /// </remarks>
    public SecondsSetting MoveTime { get; }

    /// <summary>Czas na wykonanie zadania z wydarzenia.</summary>
    public SecondsSetting TaskTime { get; }

    /// <summary>Czas na wykonanie ruchu, po którym otwiera się nasłuch komend.</summary>
    public SecondsSetting VoiceListeningDelay { get; }

    /// <summary>
    /// Czy urządzenie nie zgłosiło żadnego głosu poza domyślnym systemowym.
    /// </summary>
    /// <remarks>
    /// Zaprzeczenie jako osobna właściwość, a nie konwerter w XAML: aplikacja nie używa
    /// żadnego konwertera, a dołożenie jednego dla jednej etykiety byłoby większym
    /// kosztem niż ta linia.
    /// </remarks>
    public bool HasNoVoices => !HasVoices;

    /// <summary>Dolna granica tempa mowy.</summary>
    /// <remarks>
    /// Granice suwaków pochodzą z <see cref="AppSettings"/>, a nie z liczb wpisanych
    /// w XAML: wartość spoza zakresu jest odrzucana przy zapisie ustawień, więc suwak
    /// nie może pozwolić jej ustawić.
    /// </remarks>
    public double MinSpeechRate => AppSettings.MinSpeechRate;

    /// <summary>Górna granica tempa mowy.</summary>
    public double MaxSpeechRate => AppSettings.MaxSpeechRate;

    /// <summary>Dolna granica wysokości głosu.</summary>
    public double MinSpeechPitch => AppSettings.MinSpeechPitch;

    /// <summary>Górna granica wysokości głosu.</summary>
    public double MaxSpeechPitch => AppSettings.MaxSpeechPitch;

    /// <inheritdoc />
    protected override async Task OnInitializeAsync()
    {
        await LoadVoicesAsync();

        LoadFromSettings();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Lista głosów zależy od języka, a język zmienia się na tym samym ekranie — bez
    /// nasłuchu użytkownik po przełączeniu na angielski widziałby polskie głosy.
    /// </remarks>
    public override void OnAppearing()
    {
        if (_isSubscribed)
        {
            return;
        }

        Localization.CultureChanged += OnCultureChanged;
        _isSubscribed = true;
    }

    /// <inheritdoc />
    public override void OnDisappearing()
    {
        if (!_isSubscribed)
        {
            return;
        }

        Localization.CultureChanged -= OnCultureChanged;
        _isSubscribed = false;
    }

    /// <summary>
    /// Zapisuje wybrany język w ustawieniach.
    /// </summary>
    /// <param name="option">Wybrany język.</param>
    /// <remarks>
    /// Zapis do ustawień jest jedyną czynnością — serwis tłumaczeń nasłuchuje ich zmian
    /// i sam przełącza język, a interfejs odświeża się przez powiązania. Dzięki temu nie
    /// da się zmienić języka bez zapamiętania go ani zapamiętać bez zastosowania.
    /// </remarks>
    [RelayCommand]
    private Task ChangeLanguageAsync(LanguageOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        return ExecuteSafeAsync(() => _settingsService.UpdateAsync(
            settings => settings with { LanguageCode = option.LanguageCode }));
    }

    /// <summary>
    /// Zapisuje tempo i wysokość głosu.
    /// </summary>
    /// <remarks>
    /// Wywoływane po puszczeniu suwaka, a nie przy każdej zmianie wartości: przeciągnięcie
    /// palcem po suwaku daje kilkadziesiąt zmian, a każda z nich to zapis pliku ustawień.
    /// </remarks>
    [RelayCommand]
    private Task SaveSpeechParametersAsync() => ExecuteSafeAsync(() =>
        _settingsService.UpdateAsync(settings => settings with
        {
            SpeechRate = (float)SpeechRate,
            SpeechPitch = (float)SpeechPitch,
        }));

    /// <summary>
    /// Odczytuje próbkę wybranym głosem.
    /// </summary>
    /// <remarks>
    /// Parametry są najpierw zapisywane, bo odczyt bierze je z ustawień — inaczej próbka
    /// brzmiałaby inaczej niż to, co użytkownik właśnie ustawił suwakami.
    /// </remarks>
    [RelayCommand]
    private Task TestVoiceAsync() => ExecuteSafeAsync(async () =>
    {
        await _settingsService.UpdateAsync(settings => settings with
        {
            SpeechRate = (float)SpeechRate,
            SpeechPitch = (float)SpeechPitch,
            PreferredVoiceId = SelectedVoice?.Id,
        });

        await _speaker.SpeakAsync(_announcementBuilder.BuildVoiceSample());
    });

    /// <summary>
    /// Reaguje na wybór z listy — generowane przez CommunityToolkit.Mvvm.
    /// </summary>
    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (_isLoading || value is null)
        {
            return;
        }

        ChangeLanguageCommand.Execute(value);
    }

    /// <summary>
    /// Zapisuje wybrany motyw.
    /// </summary>
    /// <remarks>
    /// Jak przy języku: ViewModel zapisuje wyłącznie ustawienie, a warstwa hosta nasłuchuje
    /// jego zmian i przestawia motyw aplikacji. Nie da się więc zmienić motywu bez
    /// zapamiętania go ani zapamiętać bez zastosowania.
    /// </remarks>
    partial void OnSelectedThemeChanged(ThemeOption? value)
    {
        if (_isLoading || value is null)
        {
            return;
        }

        _ = SaveSafelyAsync(settings => settings with { ThemePreference = value.Preference });
    }

    /// <summary>
    /// Zapisuje włączenie albo wyłączenie dźwięków.
    /// </summary>
    /// <remarks>
    /// Włączenie od razu wczytuje próbki: pierwszy efekt po włączeniu ma zabrzmieć na czas,
    /// a nie spóźnić się o wczytywanie pliku. Przy starcie aplikacji z wyłączonymi dźwiękami
    /// próbki nie są wczytywane wcale.
    /// </remarks>
    partial void OnAreSoundsEnabledChanged(bool value)
    {
        if (_isLoading)
        {
            return;
        }

        _ = SaveSafelyAsync(settings => settings with { AreSoundsEnabled = value });

        if (value)
        {
            _ = _feedback.PreloadAsync();
        }
    }

    /// <summary>Zapisuje włączenie albo wyłączenie wibracji.</summary>
    partial void OnAreHapticsEnabledChanged(bool value)
    {
        if (_isLoading)
        {
            return;
        }

        _ = SaveSafelyAsync(settings => settings with { AreHapticsEnabled = value });
    }

    /// <summary>
    /// Zapisuje głośność efektów i od razu jej próbuje.
    /// </summary>
    /// <remarks>
    /// Zapis po puszczeniu suwaka, tak jak przy tempie mowy: przeciągnięcie palcem daje
    /// kilkadziesiąt zmian wartości. Próbka po zapisie, bo głośność bez usłyszenia jej to
    /// liczba bez znaczenia.
    /// </remarks>
    [RelayCommand]
    private Task SaveSoundVolumeAsync() => ExecuteSafeAsync(async () =>
    {
        await _settingsService.UpdateAsync(settings => settings with { SoundVolume = SoundVolume });

        _feedback.Play(FeedbackMoment.MoveRevealed);
    });

    /// <summary>Odtwarza próbkę efektu, żeby dało się ocenić głośność.</summary>
    [RelayCommand]
    private void TestSound() => _feedback.Play(FeedbackMoment.EventAnnounced);

    /// <summary>Zapisuje włączenie albo wyłączenie animacji.</summary>
    partial void OnAreAnimationsEnabledChanged(bool value)
    {
        if (_isLoading)
        {
            return;
        }

        _ = SaveSafelyAsync(settings => settings with { AreAnimationsEnabled = value });
    }

    /// <summary>Zapisuje włączenie albo wyłączenie odczytu głosowego.</summary>
    partial void OnIsTextToSpeechEnabledChanged(bool value)
    {
        if (_isLoading)
        {
            return;
        }

        _ = ExecuteSafeAsync(() => _settingsService.UpdateAsync(
            settings => settings with { IsTextToSpeechEnabled = value }));
    }

    /// <summary>
    /// Włącza albo wyłącza sterowanie głosem.
    /// </summary>
    /// <remarks>
    /// Przy włączaniu pytamy o zgodę na mikrofon <b>tutaj</b>, a nie przy pierwszej partii:
    /// gracz właśnie sam poprosił o tę funkcję, więc rozumie, po co jest pytanie. Odmowa
    /// cofa przełącznik, żeby stan na ekranie nie kłamał — z odmówionym mikrofonem
    /// sterowanie głosem nie zadziała.
    /// </remarks>
    partial void OnIsVoiceControlEnabledChanged(bool value)
    {
        if (_isLoading)
        {
            return;
        }

        _ = ChangeVoiceControlAsync(value);
    }

    private async Task ChangeVoiceControlAsync(bool enabled)
    {
        await _settingsService.UpdateAsync(settings => settings with
        {
            IsVoiceControlEnabled = enabled,
        });

        if (!enabled)
        {
            return;
        }

        // Oba przełączniki nie mogą być włączone naraz: w trybie automatycznym nie ma czym
        // sterować. Wyłączamy ten drugi i mówimy o tym wprost, zamiast zostawić na ekranie
        // dwa zaznaczone przełączniki, z których jeden nic nie robi.
        if (IsAutomaticTurnAdvance)
        {
            await SetTurnAdvanceAsync(automatic: false);
            await ShowInfoAsync(StringKeys.Settings.AutomaticTurnsDisabledByVoice);
        }

        if (await _voiceControl.PrepareAsync())
        {
            return;
        }

        await _settingsService.UpdateAsync(settings => settings with
        {
            IsVoiceControlEnabled = false,
        });

        SetWithoutSaving(() => IsVoiceControlEnabled = false);

        await ShowInfoAsync(_voiceControl.State == VoiceControlState.Unavailable
            ? StringKeys.Settings.VoiceControlUnavailable
            : StringKeys.Settings.MicrophoneDenied);
    }

    /// <summary>
    /// Zapisuje zmieniony czas.
    /// </summary>
    /// <remarks>
    /// Bez <c>ExecuteSafeAsync</c>: zapis leci z pola tekstowego przy każdej poprawce, a flaga
    /// zajętości blokowałaby wtedy resztę ekranu. Awaria zapisu ustawień nie ma tu czego
    /// przerwać, więc wystarczy log.
    /// </remarks>
    private void SaveTimes(Func<AppSettings, AppSettings> change)
    {
        if (_isLoading)
        {
            return;
        }

        _ = SaveSafelyAsync(change);
    }

    private async Task SaveSafelyAsync(Func<AppSettings, AppSettings> change)
    {
        try
        {
            await _settingsService.UpdateAsync(change);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Nie udało się zapisać ustawień czasu.");
        }
    }

    /// <summary>
    /// Zapisuje sposób przechodzenia do następnej tury.
    /// </summary>
    /// <remarks>
    /// Włączenie trybu automatycznego wyłącza sterowanie głosem — nie w ustawieniach, ale
    /// w działaniu: nasłuch po prostu się nie uruchamia, bo nie ma czym sterować. Przełącznik
    /// sterowania głosem zostaje włączony, żeby powrót do trybu ręcznego go przywrócił.
    /// </remarks>
    partial void OnIsAutomaticTurnAdvanceChanged(bool value)
    {
        if (_isLoading)
        {
            return;
        }

        _ = ChangeTurnAdvanceAsync(value);
    }

    private async Task ChangeTurnAdvanceAsync(bool automatic)
    {
        await SaveSafelyAsync(settings => settings with
        {
            TurnAdvanceMode = automatic ? TurnAdvanceMode.Automatic : TurnAdvanceMode.Manual,
        });

        if (!automatic || !IsVoiceControlEnabled)
        {
            return;
        }

        // Druga strona tego samego wykluczenia: włączenie tur automatycznych wyłącza
        // sterowanie głosem.
        await SaveSafelyAsync(settings => settings with { IsVoiceControlEnabled = false });

        SetWithoutSaving(() => IsVoiceControlEnabled = false);

        await ShowInfoAsync(StringKeys.Settings.VoiceControlDisabledByAutomaticTurns);
    }

    /// <summary>Zapisuje sposób przechodzenia tur i odzwierciedla go na przełączniku.</summary>
    private async Task SetTurnAdvanceAsync(bool automatic)
    {
        await SaveSafelyAsync(settings => settings with
        {
            TurnAdvanceMode = automatic ? TurnAdvanceMode.Automatic : TurnAdvanceMode.Manual,
        });

        SetWithoutSaving(() => IsAutomaticTurnAdvance = automatic);
    }

    /// <summary>
    /// Zmienia właściwość bez wywoływania zapisu.
    /// </summary>
    /// <remarks>
    /// Przestawienie przełącznika z kodu uruchamia metodę <c>On…Changed</c>, która normalnie
    /// zapisuje ustawienie. Tutaj zapis już się odbył i powtórzenie go zapętliłoby oba
    /// wykluczające się przełączniki.
    /// </remarks>
    private void SetWithoutSaving(Action change)
    {
        _isLoading = true;
        try
        {
            change();
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Zapisuje wybrany głos.</summary>
    partial void OnSelectedVoiceChanged(VoiceOption? value)
    {
        if (_isLoading)
        {
            return;
        }

        _ = ExecuteSafeAsync(() => _settingsService.UpdateAsync(
            settings => settings with { PreferredVoiceId = value?.Id }));
    }

    /// <summary>
    /// Przywraca ustawienia do wartości domyślnych po potwierdzeniu.
    /// </summary>
    /// <remarks>
    /// Dotyczy <b>tylko ustawień</b>: skład graczy, paczki wydarzeń i zapisana partia zostają.
    /// Rozdział jest celowy — „coś mi się rozjechało w ustawieniach" i „chcę wyczyścić telefon"
    /// to dwie różne potrzeby, a druga jest nieodwracalna.
    /// </remarks>
    [RelayCommand]
    private Task ResetSettingsAsync() => ExecuteSafeAsync(async () =>
    {
        bool confirmed = await Dialogs.ConfirmAsync(
            Localization[StringKeys.Settings.ResetConfirmTitle],
            Localization[StringKeys.Settings.ResetConfirmMessage],
            Localization[StringKeys.Settings.ButtonReset],
            Localization[StringKeys.Common.ButtonCancel]);

        if (!confirmed)
        {
            return;
        }

        await _userData.ResetSettingsAsync();

        // Formularz czyta z ustawień, więc po ich przywróceniu musi się przeładować — inaczej
        // pokazywałby wartości, których już nie ma.
        LoadFromSettings();
    });

    /// <summary>Usuwa wszystkie dane użytkownika po potwierdzeniu.</summary>
    [RelayCommand]
    private Task EraseDataAsync() => ExecuteSafeAsync(async () =>
    {
        bool confirmed = await Dialogs.ConfirmAsync(
            Localization[StringKeys.Settings.EraseConfirmTitle],
            Localization[StringKeys.Settings.EraseConfirmMessage],
            Localization[StringKeys.Settings.ButtonErase],
            Localization[StringKeys.Common.ButtonCancel]);

        if (!confirmed)
        {
            return;
        }

        await _userData.EraseAsync();

        LoadFromSettings();

        await ShowInfoAsync(StringKeys.Settings.EraseDone);
    });

    /// <summary>Pokazuje wprowadzenie „Jak grać".</summary>
    /// <remarks>
    /// Ta sama droga co z ekranu startowego. W ustawieniach jest, bo tam szuka się rzeczy
    /// „do przeczytania o aplikacji" — a wprowadzenie jest dokładnie tym.
    /// </remarks>
    [RelayCommand]
    private Task GoToHowToPlayAsync() => ExecuteSafeAsync(() => Navigation.GoToAsync(Routes.Onboarding));

    /// <summary>Otwiera politykę prywatności w przeglądarce systemowej.</summary>
    /// <remarks>
    /// Google Play wymaga dostępu do polityki <b>z wnętrza aplikacji</b>, nie tylko z karty
    /// sklepu. Odnośnik, a nie ekran z pełnym tekstem: dokument prawny trzeba by wtedy
    /// utrzymywać w dziesięciu językach w plikach zasobów i pilnować, żeby nie rozjechał się
    /// z wersją opublikowaną, którą sklep i tak wskazuje.
    /// </remarks>
    [RelayCommand]
    private Task OpenPrivacyPolicyAsync() =>
        ExecuteSafeAsync(() => _browser.OpenAsync(AppLinks.PrivacyPolicy));

    /// <summary>
    /// Wczytuje listę głosów z syntezatora, zawężoną do języka aplikacji.
    /// </summary>
    /// <remarks>
    /// Lista głosów na urządzeniu bywa długa — kilkadziesiąt języków. Głos z innego języka
    /// przeczyta polskie polecenie z obcą fonetyką, więc pokazywanie wszystkich pozycji
    /// tylko utrudnia wybór.
    /// <para>
    /// Awaria pobierania jest pochłaniana: bez listy zostaje głos domyślny systemu, co jest
    /// gorsze od pełnej listy, ale nie zamyka ekranu ustawień.
    /// </para>
    /// </remarks>
    private async Task LoadVoicesAsync()
    {
        IReadOnlyList<SpeechVoice> voices = [];

        try
        {
            voices = await _textToSpeech.GetVoicesAsync();
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Nie udało się pobrać listy głosów syntezatora.");
        }

        string language = Localization.CurrentCulture.TwoLetterISOLanguageName;
        List<SpeechVoice> matching = [.. voices.Where(voice => MatchesLanguage(voice, language))];

        // Urządzenie może nie mieć żadnego głosu w języku aplikacji. Wtedy lepiej pokazać
        // wszystkie niż puste pole wyboru — użytkownik sam zdecyduje, co brzmi znośnie.
        IEnumerable<SpeechVoice> shown = matching.Count > 0 ? matching : voices;

        // Zapisany głos zostaje na liście, nawet gdy nie należy do języka aplikacji:
        // to nim aplikacja w tej chwili mówi i użytkownik musi to widzieć.
        string? saved = _settingsService.Current.PreferredVoiceId;

        if (saved is not null && !shown.Any(voice => voice.Id == saved))
        {
            shown = shown.Concat(voices.Where(voice => voice.Id == saved));
        }

        AvailableVoices.Clear();

        // Pozycja „domyślny systemowy" jest zawsze pierwsza i ma pusty identyfikator —
        // syntezator dobiera wtedy głos sam, zgodnie z ustawieniami urządzenia.
        AvailableVoices.Add(new VoiceOption(
            null,
            Localization[StringKeys.Settings.LabelSystemVoice]));

        IEnumerable<SpeechVoice> sorted = shown
            .OrderBy(voice => voice.Language, StringComparer.Ordinal)
            .ThenBy(voice => voice.Name, StringComparer.CurrentCulture);

        foreach (SpeechVoice voice in sorted)
        {
            AvailableVoices.Add(VoiceOption.From(voice));
        }

        OnPropertyChanged(nameof(HasVoices));
        OnPropertyChanged(nameof(HasNoVoices));
    }

    /// <summary>
    /// Sprawdza, czy głos należy do podanego języka.
    /// </summary>
    /// <remarks>
    /// Systemy podają język głosu w różnych postaciach — <c>pl</c>, <c>pl-PL</c>, a Android
    /// czasem trzyliterowo (<c>pol</c>). Porównanie przez <see cref="CultureInfo"/> obsługuje
    /// wszystkie trzy; nieznana nazwa nie jest błędem, tylko brakiem dopasowania.
    /// </remarks>
    private static bool MatchesLanguage(SpeechVoice voice, string twoLetterLanguage)
    {
        if (string.IsNullOrWhiteSpace(voice.Language))
        {
            return false;
        }

        if (voice.Language.StartsWith(twoLetterLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            return string.Equals(
                CultureInfo.GetCultureInfo(voice.Language).TwoLetterISOLanguageName,
                twoLetterLanguage,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Przebudowuje listę głosów po zmianie języka.
    /// </summary>
    /// <remarks>
    /// Bez <c>ExecuteSafeAsync</c>: zdarzenie przychodzi w trakcie zapisu języka, kiedy
    /// ekran jest już zajęty, a zajętość blokuje kolejną operację. Pobranie listy głosów
    /// samo pochłania awarie, więc nie ma tu czego dodatkowo zabezpieczać.
    /// </remarks>
    private void OnCultureChanged(object? sender, CultureInfo culture) => _ = ReloadVoicesAsync();

    private async Task ReloadVoicesAsync()
    {
        await LoadVoicesAsync();

        LoadFromSettings();
    }

    /// <summary>Przenosi zapisane ustawienia do formularza.</summary>
    private void LoadFromSettings()
    {
        AppSettings settings = _settingsService.Current;

        _isLoading = true;
        try
        {
            IsTextToSpeechEnabled = settings.IsTextToSpeechEnabled;
            SpeechRate = settings.SpeechRate;
            SpeechPitch = settings.SpeechPitch;
            SelectedTheme = AvailableThemes.FirstOrDefault(option =>
                option.Preference == settings.ThemePreference);
            AreAnimationsEnabled = settings.AreAnimationsEnabled;
            AreSoundsEnabled = settings.AreSoundsEnabled;
            SoundVolume = settings.SoundVolume;
            AreHapticsEnabled = settings.AreHapticsEnabled;

            IsVoiceControlEnabled = settings.IsVoiceControlEnabled;
            IsAutomaticTurnAdvance = settings.TurnAdvanceMode == TurnAdvanceMode.Automatic;

            MoveTime.Load(settings.MoveTime);
            TaskTime.Load(settings.TaskTime);
            VoiceListeningDelay.Load(settings.VoiceListeningDelay);

            // Zapisany głos mógł zostać odinstalowany — wtedy wracamy na domyślny systemowy,
            // zamiast pokazywać puste pole wyboru.
            SelectedVoice = AvailableVoices.FirstOrDefault(option => option.Id == settings.PreferredVoiceId)
                ?? AvailableVoices.FirstOrDefault();
        }
        finally
        {
            _isLoading = false;
        }
    }
}
