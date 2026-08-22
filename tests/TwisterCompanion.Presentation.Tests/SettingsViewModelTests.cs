using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TwisterCompanion.Application.Feedback;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Application.VoiceControl;
using TwisterCompanion.Presentation;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Tests.Fakes;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.Presentation.Tests;

/// <summary>
/// Testy ekranu ustawień — wyboru języka i parametrów odczytu głosowego.
/// </summary>
public class SettingsViewModelTests
{
    private readonly FakeSettingsService _settings = new();
    private readonly FakeLocalizationService _localization = new();
    private readonly FakeTextToSpeechService _textToSpeech = new();
    private readonly IAnnouncementSpeaker _speaker = Substitute.For<IAnnouncementSpeaker>();
    private readonly IAnnouncementBuilder _announcements = Substitute.For<IAnnouncementBuilder>();
    private readonly FakeVoiceControlService _voiceControl = new();

    [Fact]
    public void AvailableLanguages_ZawieraWszystkieObslugiwaneJezyki()
    {
        SettingsViewModel viewModel = CreateViewModel();

        Assert.Equal(
            _localization.SupportedCultures.Count,
            viewModel.AvailableLanguages.Count);
        Assert.Contains(viewModel.AvailableLanguages, option => option.LanguageCode == "pl");
        Assert.Contains(viewModel.AvailableLanguages, option => option.LanguageCode == "en");
    }

    [Fact]
    public void AvailableLanguages_MajaNazwyWeWlasnychJezykach()
    {
        // Użytkownik szukający swojego języka rozpozna go po własnej nazwie,
        // a nie po tłumaczeniu na język, którego może nie znać.
        SettingsViewModel viewModel = CreateViewModel();

        Assert.Equal("Polski", viewModel.AvailableLanguages.Single(o => o.LanguageCode == "pl").DisplayName);
        Assert.Equal("English", viewModel.AvailableLanguages.Single(o => o.LanguageCode == "en").DisplayName);
    }

    [Fact]
    public void SelectedLanguage_PoUtworzeniu_WskazujeAktualnyJezyk()
    {
        SettingsViewModel viewModel = CreateViewModel();

        Assert.NotNull(viewModel.SelectedLanguage);
        Assert.Equal("pl", viewModel.SelectedLanguage.LanguageCode);
    }

    [Fact]
    public async Task ChangeLanguageCommand_ZapisujeJezykWUstawieniach()
    {
        SettingsViewModel viewModel = CreateViewModel();
        LanguageOption english = viewModel.AvailableLanguages.Single(o => o.LanguageCode == "en");

        await viewModel.ChangeLanguageCommand.ExecuteAsync(english);

        Assert.Equal("en", _settings.Current.LanguageCode);
    }

    [Fact]
    public void ZmianaWyboruNaLiscie_ZapisujeJezykWUstawieniach()
    {
        // Tak wygląda ścieżka użytkownika: wybór z listy na ekranie.
        SettingsViewModel viewModel = CreateViewModel();
        LanguageOption english = viewModel.AvailableLanguages.Single(o => o.LanguageCode == "en");

        viewModel.SelectedLanguage = english;

        Assert.Equal("en", _settings.Current.LanguageCode);
        Assert.Equal(1, _settings.UpdateCount);
    }

    [Fact]
    public void ZmianaJezyka_NieUstawiaGoBezposrednioWSerwisieTlumaczen()
    {
        // Istotny szczegół projektowy: ViewModel zapisuje wyłącznie ustawienia.
        // Język stosuje serwis tłumaczeń, nasłuchując ich zmiany. Dzięki temu nie da się
        // zmienić języka bez zapamiętania go ani zapamiętać bez zastosowania.
        SettingsViewModel viewModel = CreateViewModel();
        LanguageOption english = viewModel.AvailableLanguages.Single(o => o.LanguageCode == "en");

        viewModel.SelectedLanguage = english;

        Assert.Equal("pl", _localization.CurrentCulture.TwoLetterISOLanguageName);
    }

    [Fact]
    public async Task AvailableVoices_ZaczynaSieOdGlosuDomyslnegoSystemu()
    {
        // Bez tej pozycji nie da się wrócić do głosu, który system dobiera sam.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.Null(viewModel.AvailableVoices[0].Id);
        Assert.True(viewModel.HasVoices);
        Assert.False(viewModel.HasNoVoices);
    }

    [Fact]
    public async Task AvailableVoices_ZawieraTylkoGlosyWJezykuAplikacji()
    {
        // Głos z innego języka przeczyta polskie polecenie obcą fonetyką, a lista głosów
        // na urządzeniu ma kilkadziesiąt pozycji — pokazywanie wszystkich utrudnia wybór.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.Equal(["Zofia"], viewModel.AvailableVoices.Skip(1).Select(option => option.DisplayName));
    }

    [Fact]
    public async Task BrakGlosuWJezykuAplikacji_PokazujeWszystkieGlosy()
    {
        // Puste pole wyboru nie dałoby użytkownikowi żadnego wyjścia — niech sam zdecyduje,
        // co brzmi znośnie.
        _textToSpeech.Voices.RemoveAll(voice => voice.Language == "pl");

        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.Contains(viewModel.AvailableVoices, option => option.Id == "en|US|Aria");
    }

    [Fact]
    public async Task ZapisanyGlosZInnegoJezyka_ZostajeNaLiscie()
    {
        // Tym głosem aplikacja właśnie mówi — ukrycie go pokazywałoby wybór niezgodny
        // z tym, co użytkownik słyszy.
        await _settings.UpdateAsync(settings => settings with { PreferredVoiceId = "en|US|Aria" });

        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.Contains(viewModel.AvailableVoices, option => option.Id == "en|US|Aria");
        Assert.Equal("en|US|Aria", viewModel.SelectedVoice?.Id);
    }

    [Fact]
    public async Task ZmianaJezyka_PrzebudowujeListeGlosow()
    {
        // Język zmienia się na tym samym ekranie, na którym wybiera się głos.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();
        viewModel.OnAppearing();

        _localization.SetCulture(CultureInfo.GetCultureInfo("en"));

        Assert.Equal(["Aria"], viewModel.AvailableVoices.Skip(1).Select(option => option.DisplayName));
    }

    [Fact]
    public async Task BrakGlosowWUrzadzeniu_ZostawiaTylkoDomyslnySystemowy()
    {
        _textToSpeech.Voices.Clear();

        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.Single(viewModel.AvailableVoices);
        Assert.False(viewModel.HasVoices);
        Assert.True(viewModel.HasNoVoices);
    }

    [Fact]
    public async Task AwariaSyntezatora_NieZamykaEkranuUstawien()
    {
        // Bez listy głosów zostaje głos domyślny systemu — reszta ustawień działa dalej.
        _textToSpeech.FailWith = new InvalidOperationException("Brak silnika mowy.");

        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.Single(viewModel.AvailableVoices);
        Assert.NotNull(viewModel.SelectedVoice);
    }

    [Fact]
    public async Task Inicjalizacja_WczytujeZapisaneUstawieniaOdczytu()
    {
        await _settings.UpdateAsync(settings => settings with
        {
            IsTextToSpeechEnabled = false,
            SpeechRate = 1.4f,
            SpeechPitch = 0.8f,
            PreferredVoiceId = "pl|PL|Zofia",
        });

        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.False(viewModel.IsTextToSpeechEnabled);
        Assert.Equal(1.4, viewModel.SpeechRate, 3);
        Assert.Equal(0.8, viewModel.SpeechPitch, 3);
        Assert.Equal("pl|PL|Zofia", viewModel.SelectedVoice?.Id);
    }

    [Fact]
    public async Task Inicjalizacja_NieZapisujeWczytanychUstawien()
    {
        // Wczytanie wartości do formularza ustawia właściwości, a te normalnie zapisują
        // zmianę. Zapis tego, co właśnie przeczytaliśmy, byłby zapisem bez zmiany.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.Equal(0, _settings.UpdateCount);
        Assert.NotNull(viewModel.SelectedVoice);
    }

    [Fact]
    public async Task ZapisanyGlosOdinstalowany_WracaNaDomyslnySystemowy()
    {
        // Głos można odinstalować w ustawieniach systemu — puste pole wyboru
        // nie mówiłoby użytkownikowi, czym aplikacja teraz mówi.
        await _settings.UpdateAsync(settings => settings with { PreferredVoiceId = "de|DE|Nieistniejacy" });

        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.Null(viewModel.SelectedVoice?.Id);
    }

    [Fact]
    public async Task WylaczenieOdczytu_ZapisujeUstawienie()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.IsTextToSpeechEnabled = false;

        Assert.False(_settings.Current.IsTextToSpeechEnabled);
    }

    [Fact]
    public async Task WyborGlosu_ZapisujeUstawienie()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.SelectedVoice = viewModel.AvailableVoices.Single(option => option.Id == "pl|PL|Zofia");

        Assert.Equal("pl|PL|Zofia", _settings.Current.PreferredVoiceId);
    }

    [Fact]
    public async Task ZapisParametrowMowy_PrzenosiTempoIWysokoscDoUstawien()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.SpeechRate = 1.75;
        viewModel.SpeechPitch = 1.25;

        // Suwak zapisuje po puszczeniu, a nie przy każdej zmianie wartości.
        Assert.Equal(AppSettings.Default.SpeechRate, _settings.Current.SpeechRate);

        await viewModel.SaveSpeechParametersCommand.ExecuteAsync(null);

        Assert.Equal(1.75f, _settings.Current.SpeechRate);
        Assert.Equal(1.25f, _settings.Current.SpeechPitch);
    }

    [Fact]
    public async Task SprawdzenieGlosu_ZapisujeParametryIOdczytujeProbke()
    {
        // Próbka bierze parametry z ustawień, więc muszą tam trafić przed odczytem —
        // inaczej użytkownik usłyszałby coś innego, niż właśnie ustawił.
        Announcement probka = new("Prawa ręka, czerwony.", AnnouncementKind.VoiceSample);
        _announcements.BuildVoiceSample().Returns(probka);

        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.SpeechRate = 1.5;
        viewModel.SelectedVoice = viewModel.AvailableVoices.Single(option => option.Id == "pl|PL|Zofia");

        await viewModel.TestVoiceCommand.ExecuteAsync(null);

        Assert.Equal(1.5f, _settings.Current.SpeechRate);
        Assert.Equal("pl|PL|Zofia", _settings.Current.PreferredVoiceId);

        await _speaker.Received(1).SpeakAsync(probka, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GraniceSuwakow_PochodzaZWalidacjiUstawien()
    {
        // Wartość spoza zakresu jest odrzucana przy zapisie, więc suwak nie może
        // pozwolić jej ustawić.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.Equal(AppSettings.MinSpeechRate, viewModel.MinSpeechRate);
        Assert.Equal(AppSettings.MaxSpeechRate, viewModel.MaxSpeechRate);
        Assert.Equal(AppSettings.MinSpeechPitch, viewModel.MinSpeechPitch);
        Assert.Equal(AppSettings.MaxSpeechPitch, viewModel.MaxSpeechPitch);
    }

    [Fact]
    public async Task WlaczenieSterowaniaGlosem_ZapisujeUstawienie()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.IsVoiceControlEnabled = true;

        Assert.True(_settings.Current.IsVoiceControlEnabled);
    }

    [Fact]
    public async Task OdmowaZgodyNaMikrofon_CofaPrzelacznik()
    {
        // Zostawienie włączonego przełącznika przy odmówionym mikrofonie kłamałoby:
        // sterowanie głosem i tak by nie zadziałało.
        _voiceControl.CanPrepare = false;

        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.IsVoiceControlEnabled = true;

        await WaitUntilAsync(() => !viewModel.IsVoiceControlEnabled);

        Assert.False(viewModel.IsVoiceControlEnabled);
        Assert.False(_settings.Current.IsVoiceControlEnabled);
    }

    [Fact]
    public async Task CzasyWpisanePolem_ZapisujaSieOdRazu()
    {
        // Pole zamiast suwaka: suwak nie pokazywał, ile ustawia, a przy sekundach to
        // jedyna informacja, która się liczy.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.MoveTime.Text = "20";
        viewModel.TaskTime.Text = "40";
        viewModel.VoiceListeningDelay.Text = "7";

        await WaitUntilAsync(() => _settings.Current.MoveTime == TimeSpan.FromSeconds(20));

        Assert.Equal(TimeSpan.FromSeconds(20), _settings.Current.MoveTime);
        Assert.Equal(TimeSpan.FromSeconds(40), _settings.Current.TaskTime);
        Assert.Equal(TimeSpan.FromSeconds(7), _settings.Current.VoiceListeningDelay);
    }

    [Fact]
    public async Task PrzyciskiZmieniajaCzasOPiecSekund()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();
        int start = viewModel.MoveTime.Seconds;

        viewModel.MoveTime.IncreaseCommand.Execute(null);

        Assert.Equal(start + SecondsSetting.Step, viewModel.MoveTime.Seconds);

        viewModel.MoveTime.DecreaseCommand.Execute(null);

        Assert.Equal(start, viewModel.MoveTime.Seconds);
    }

    [Fact]
    public async Task WartoscPozaZakresem_JestPrzycinanaIPoprawianaWPolu()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.MoveTime.Text = "999";

        Assert.Equal((int)AppSettings.MaxMoveTime.TotalSeconds, viewModel.MoveTime.Seconds);
        Assert.Equal(viewModel.MoveTime.Seconds.ToString(CultureInfo.CurrentCulture), viewModel.MoveTime.Text);
    }

    [Fact]
    public async Task TekstNiebedacyLiczba_NieZmieniaWartosci()
    {
        // Użytkownik jest w trakcie wpisywania — kasowanie mu znaku pod palcem byłoby
        // walką z klawiaturą.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();
        int start = viewModel.MoveTime.Seconds;

        viewModel.MoveTime.Text = "abc";

        Assert.Equal(start, viewModel.MoveTime.Seconds);
    }

    [Fact]
    public async Task TrybAutomatyczny_ZapisujeSposobPrzechodzeniaTur()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.IsAutomaticTurnAdvance = true;

        await WaitUntilAsync(() => _settings.Current.TurnAdvanceMode == TurnAdvanceMode.Automatic);

        Assert.Equal(TurnAdvanceMode.Automatic, _settings.Current.TurnAdvanceMode);
    }

    [Fact]
    public async Task Motywy_ZawierajaWszystkieWartosciWyboru()
    {
        // Lista powstaje z wartości wyliczenia, więc dołożenie motywu nie wymaga zmiany kodu
        // ekranu — ten test pilnuje, że nikt nie wpisał ich na sztywno.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.Equal(
            Enum.GetValues<AppThemePreference>(),
            viewModel.AvailableThemes.Select(option => option.Preference));
    }

    [Fact]
    public async Task WylaczenieAnimacji_ZapisujeUstawienie()
    {
        // Przełącznik jest dodatkiem do systemowego ograniczenia animacji, więc musi mieć
        // własną, trwałą wartość.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.AreAnimationsEnabled = false;

        await WaitUntilAsync(() => !_settings.Current.AreAnimationsEnabled);

        Assert.False(_settings.Current.AreAnimationsEnabled);
    }

    [Fact]
    public async Task Inicjalizacja_WczytujeUstawienieAnimacji()
    {
        await _settings.UpdateAsync(settings => settings with { AreAnimationsEnabled = false });

        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.False(viewModel.AreAnimationsEnabled);
    }

    [Fact]
    public async Task Inicjalizacja_ZaznaczaZapisanyMotyw()
    {
        await _settings.UpdateAsync(settings => settings with
        {
            ThemePreference = AppThemePreference.Dark,
        });

        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.Equal(AppThemePreference.Dark, viewModel.SelectedTheme?.Preference);
    }

    [Fact]
    public async Task WyborMotywu_ZapisujeUstawienie()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.SelectedTheme = viewModel.AvailableThemes
            .Single(option => option.Preference == AppThemePreference.Light);

        await WaitUntilAsync(() => _settings.Current.ThemePreference == AppThemePreference.Light);

        Assert.Equal(AppThemePreference.Light, _settings.Current.ThemePreference);
    }

    [Fact]
    public async Task WlaczenieSterowaniaGlosem_WylaczaTuryAutomatyczne()
    {
        // Oba przełączniki naraz zostawiłyby na ekranie zaznaczoną opcję, która nic nie robi.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();
        viewModel.IsAutomaticTurnAdvance = true;

        await WaitUntilAsync(() => _settings.Current.TurnAdvanceMode == TurnAdvanceMode.Automatic);

        viewModel.IsVoiceControlEnabled = true;

        await WaitUntilAsync(() => !viewModel.IsAutomaticTurnAdvance);

        Assert.False(viewModel.IsAutomaticTurnAdvance);
        Assert.Equal(TurnAdvanceMode.Manual, _settings.Current.TurnAdvanceMode);
        Assert.True(_settings.Current.IsVoiceControlEnabled);
    }

    [Fact]
    public async Task WlaczenieTurAutomatycznych_WylaczaSterowanieGlosem()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();
        viewModel.IsVoiceControlEnabled = true;

        await WaitUntilAsync(() => _settings.Current.IsVoiceControlEnabled);

        viewModel.IsAutomaticTurnAdvance = true;

        await WaitUntilAsync(() => !viewModel.IsVoiceControlEnabled);

        Assert.False(viewModel.IsVoiceControlEnabled);
        Assert.False(_settings.Current.IsVoiceControlEnabled);
        Assert.Equal(TurnAdvanceMode.Automatic, _settings.Current.TurnAdvanceMode);
    }

    [Fact]
    public async Task WylaczenieJednegoPrzelacznika_NieRuszaDrugiego()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();
        viewModel.IsAutomaticTurnAdvance = true;

        await WaitUntilAsync(() => _settings.Current.TurnAdvanceMode == TurnAdvanceMode.Automatic);

        viewModel.IsAutomaticTurnAdvance = false;

        await WaitUntilAsync(() => _settings.Current.TurnAdvanceMode == TurnAdvanceMode.Manual);

        Assert.False(viewModel.IsVoiceControlEnabled);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(5);
        }

        Assert.Fail("Warunek nie został spełniony w wyznaczonym czasie.");
    }

    [Fact]
    public async Task WylaczenieDzwiekow_ZapisujeUstawienie()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.AreSoundsEnabled = false;

        await WaitUntilAsync(() => !_settings.Current.AreSoundsEnabled);

        Assert.False(_settings.Current.AreSoundsEnabled);
    }

    [Fact]
    public async Task WlaczenieDzwiekow_WczytujeProbki()
    {
        // Pierwszy efekt po włączeniu ma zabrzmieć na czas, a nie spóźnić się o wczytanie
        // pliku — przy starcie z wyłączonymi dźwiękami próbki nie są wczytywane wcale.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.AreSoundsEnabled = false;
        viewModel.AreSoundsEnabled = true;

        await WaitUntilAsync(() => _feedback.PreloadCount > 0);

        Assert.True(_feedback.PreloadCount > 0);
    }

    [Fact]
    public async Task ZapisGlosnosci_OdtwarzaProbke()
    {
        // Głośność bez usłyszenia jej to liczba bez znaczenia.
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.SoundVolume = 0.4;
        await viewModel.SaveSoundVolumeCommand.ExecuteAsync(parameter: null);

        Assert.Equal(0.4, _settings.Current.SoundVolume);
        Assert.NotEmpty(_feedback.Moments);
    }

    [Fact]
    public async Task WylaczenieWibracji_ZapisujeUstawienie()
    {
        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        viewModel.AreHapticsEnabled = false;

        await WaitUntilAsync(() => !_settings.Current.AreHapticsEnabled);

        Assert.False(_settings.Current.AreHapticsEnabled);
    }

    [Fact]
    public async Task Inicjalizacja_WczytujeUstawieniaDzwiekow()
    {
        await _settings.UpdateAsync(settings => settings with
        {
            AreSoundsEnabled = false,
            SoundVolume = 0.25,
            AreHapticsEnabled = false,
        });

        SettingsViewModel viewModel = await CreateInitializedViewModelAsync();

        Assert.False(viewModel.AreSoundsEnabled);
        Assert.Equal(0.25, viewModel.SoundVolume);
        Assert.False(viewModel.AreHapticsEnabled);
    }

    private async Task<SettingsViewModel> CreateInitializedViewModelAsync()
    {
        SettingsViewModel viewModel = CreateViewModel();

        await viewModel.InitializeAsync();

        return viewModel;
    }

    private readonly FakeGameFeedback _feedback = new();
    private readonly IUserDataService _userData = Substitute.For<IUserDataService>();

    [Fact]
    public async Task PolitykaPrywatnosci_OtwieraAdresWskazanyWKarcieSklepu()
    {
        // Google Play wymaga dostępu do polityki z wnętrza aplikacji, a adres musi być ten
        // sam, który podano w karcie sklepu — rozjechanie się ich jest naruszeniem zasad,
        // nie usterką kosmetyczną. Test pilnuje, że przycisk prowadzi dokładnie tam.
        SettingsViewModel viewModel = CreateViewModel();

        await viewModel.OpenPrivacyPolicyCommand.ExecuteAsync(null);

        await _browser.Received(1).OpenAsync(AppLinks.PrivacyPolicy);
    }

    private readonly IExternalBrowser _browser = Substitute.For<IExternalBrowser>();

    private SettingsViewModel CreateViewModel() => new(
        Substitute.For<INavigationService>(),
        _settings,
        _textToSpeech,
        _speaker,
        _announcements,
        _voiceControl,
        _feedback,
        _userData,
        _localization,
        NullLogger<SettingsViewModel>.Instance,
        Substitute.For<IDialogService>(),
        _browser);
}
