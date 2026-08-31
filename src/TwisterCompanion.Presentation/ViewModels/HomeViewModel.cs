using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Navigation;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Ekran startowy — punkt wejścia do wszystkich pozostałych ekranów.
/// </summary>
/// <remarks>
/// Przy pierwszym uruchomieniu prowadzi do wprowadzenia „Jak grać". Decyzja pada tutaj, a nie
/// przy tworzeniu okna aplikacji: ustawienia są wczytywane z dysku asynchronicznie, więc
/// w momencie budowania okna nie wiadomo jeszcze, czy gracz wprowadzenie widział.
/// </remarks>
public partial class HomeViewModel : NavigableViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IPlayerRosterRepository _playerRoster;

    /// <summary>Tworzy ViewModel ekranu startowego.</summary>
    /// <param name="navigation">Serwis nawigacji.</param>
    /// <param name="settingsService">Ustawienia — źródło informacji o pokazanym wprowadzeniu.</param>
    /// <param name="playerRoster">Skład graczy — sprawdzany przed wejściem na ekran rozgrywki.</param>
    /// <param name="logger">Logger tego ViewModelu.</param>
    /// <param name="dialogService">Serwis komunikatów dla użytkownika.</param>
    /// <param name="localization">Serwis tłumaczeń.</param>
    public HomeViewModel(
        INavigationService navigation,
        ISettingsService settingsService,
        IPlayerRosterRepository playerRoster,
        ILogger<HomeViewModel> logger,
        IDialogService dialogService,
        ILocalizationService localization)
        : base(navigation, logger, dialogService, localization)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(playerRoster);

        _settingsService = settingsService;
        _playerRoster = playerRoster;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Ustawienia są tu wczytywane <b>jeszcze raz</b>, choć robi to już start aplikacji. Powód
    /// jest wyścigiem: inicjalizacja tego ekranu może wyprzedzić odczyt pliku, a wtedy
    /// „nie widział wprowadzenia" byłoby wartością domyślną, nie zapisaną — i gracz dostawałby
    /// wprowadzenie przy każdym uruchomieniu. Odczyt jest tani, a pewność co do tej jednej
    /// wartości warta jest jednego wejścia na dysk.
    /// </remarks>
    protected override async Task OnInitializeAsync()
    {
        // Awarie są tu pochłaniane z logiem, a nie zgłaszane komunikatem: to jest pierwsza
        // sekunda działania aplikacji, a komunikat o błędzie zamiast ekranu startowego byłby
        // najgorszym możliwym pierwszym wrażeniem. Bez wprowadzenia gra działa normalnie.
        try
        {
            await _settingsService.LoadAsync();

            if (_settingsService.Current.HasSeenOnboarding)
            {
                return;
            }

            await Navigation.GoToAsync(Routes.Onboarding);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Nie udało się pokazać wprowadzenia przy pierwszym uruchomieniu.");
        }
    }

    /// <summary>Przechodzi do ekranu rozgrywki.</summary>
    /// <summary>
    /// Wchodzi na ekran rozgrywki, a przy pustym składzie najpierw pyta o dodanie graczy.
    /// </summary>
    /// <remarks>
    /// Bez tego pytania pierwsze uruchomienie kończyło się ślepym zaułkiem: gracz wchodził
    /// w „Rozgrywkę", widział nieaktywny przycisk startu i nie miał żadnej wskazówki, czego
    /// brakuje ani gdzie to uzupełnić. Pytanie pada <b>przed</b> przejściem, bo dopiero wtedy
    /// odpowiedź „tak" może zaprowadzić wprost tam, gdzie trzeba.
    /// <para>
    /// Sprawdzany jest skład <b>pusty</b>, a nie mniejszy niż dwa. Silnik startuje partię
    /// z jednym graczem — to sensowny trening solo, tylko bez zwycięzcy — więc blokowanie
    /// wejścia przy jednym graczu odbierałoby coś, co dziś działa.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private Task GoToGameAsync() => ExecuteSafeAsync(async () =>
    {
        IReadOnlyList<Player> players = await _playerRoster.GetAsync();

        if (players.Count > 0)
        {
            await Navigation.GoToAsync(Routes.Game);

            return;
        }

        bool dodajemy = await Dialogs.ConfirmAsync(
            Localization[StringKeys.Home.NoPlayersTitle],
            Localization[StringKeys.Home.NoPlayersMessage],
            Localization[StringKeys.Common.ButtonYes],
            Localization[StringKeys.Common.ButtonNo]);

        if (dodajemy)
        {
            await Navigation.GoToAsync(Routes.Players);
        }
    });

    /// <summary>Przechodzi do zarządzania graczami.</summary>
    [RelayCommand]
    private Task GoToPlayersAsync() => ExecuteSafeAsync(() => Navigation.GoToAsync(Routes.Players));

    /// <summary>Przechodzi do wyboru trybu gry.</summary>
    [RelayCommand]
    private Task GoToGameModesAsync() => ExecuteSafeAsync(() => Navigation.GoToAsync(Routes.GameModes));

    /// <summary>Przechodzi do paczek Custom Events.</summary>
    [RelayCommand]
    private Task GoToEventPacksAsync() => ExecuteSafeAsync(() => Navigation.GoToAsync(Routes.EventPacks));

    /// <summary>Przechodzi do ustawień aplikacji.</summary>
    [RelayCommand]
    private Task GoToSettingsAsync() => ExecuteSafeAsync(() => Navigation.GoToAsync(Routes.Settings));

    /// <summary>
    /// Przechodzi do wprowadzenia „Jak grać".
    /// </summary>
    /// <remarks>
    /// Wprowadzenie jest dostępne zawsze, nie tylko przy pierwszym uruchomieniu: gra bywa
    /// pokazywana komuś nowemu, a tłumaczenie zasad z pamięci wychodzi gorzej niż trzy ekrany,
    /// które i tak są w aplikacji.
    /// </remarks>
    [RelayCommand]
    private Task GoToHowToPlayAsync() => ExecuteSafeAsync(() => Navigation.GoToAsync(Routes.Onboarding));
}
