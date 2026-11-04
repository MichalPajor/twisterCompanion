using TwisterCompanion.App.Navigation;
using TwisterCompanion.App.Views;
using TwisterCompanion.Presentation.Navigation;

namespace TwisterCompanion.App;

/// <summary>
/// Powłoka nawigacyjna aplikacji — ustawia ekran startowy i przypisuje strony do tras.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>Tworzy powłokę nawigacyjną.</summary>
    /// <param name="homePage">Ekran startowy, wstrzykiwany przez kontener.</param>
    public AppShell(HomePage homePage)
    {
        ArgumentNullException.ThrowIfNull(homePage);

        InitializeComponent();

        Items.Add(new ShellContent
        {
            Route = Routes.HomeContent,
            Content = homePage,
        });
    }

    /// <summary>
    /// Przypisuje strony do tras z <see cref="Routes"/>. Wołane <b>raz na proces</b>,
    /// z <c>MauiProgram</c>.
    /// </summary>
    /// <param name="services">Kontener, z którego tworzone są strony tras.</param>
    /// <remarks>
    /// Rejestr tras MAUI jest statyczny — jeden na proces — a powłoka powstaje na każde
    /// okno. Android tworzy nowe okno w żyjącym procesie: po powrocie do aktywności
    /// usuniętej w tle, po wyjściu przyciskiem Wstecz na Androidzie 11 i starszych, przy
    /// części zmian konfiguracji. Rejestracja w konstruktorze powłoki rzucała wtedy
    /// „Duplicated Route" i aplikacja padała przy otwieraniu (zgłoszenie z Google Play
    /// dla 1.0.3).
    /// <para>
    /// Nowy ekran wymaga wpisu tutaj oraz rejestracji strony i ViewModelu w kontenerze.
    /// Fabryka <see cref="ServiceProviderRouteFactory{TPage}"/> gwarantuje, że strona
    /// powstanie z kontenera, a nie przez <c>Activator.CreateInstance</c>.
    /// </para>
    /// </remarks>
    internal static void RegisterRoutes(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Routing.RegisterRoute(Routes.Game, new ServiceProviderRouteFactory<GamePage>(services));
        Routing.RegisterRoute(Routes.Players, new ServiceProviderRouteFactory<PlayersPage>(services));
        Routing.RegisterRoute(Routes.GameModes, new ServiceProviderRouteFactory<GameModesPage>(services));
        Routing.RegisterRoute(Routes.EventPacks, new ServiceProviderRouteFactory<EventPacksPage>(services));
        Routing.RegisterRoute(Routes.Settings, new ServiceProviderRouteFactory<SettingsPage>(services));
        Routing.RegisterRoute(Routes.Rules, new ServiceProviderRouteFactory<RulesPage>(services));
        Routing.RegisterRoute(Routes.Onboarding, new ServiceProviderRouteFactory<OnboardingPage>(services));
    }
}
