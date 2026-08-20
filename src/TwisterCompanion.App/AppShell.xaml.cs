using TwisterCompanion.App.Navigation;
using TwisterCompanion.App.Views;
using TwisterCompanion.Presentation.Navigation;

namespace TwisterCompanion.App;

/// <summary>
/// Powłoka nawigacyjna aplikacji — ustawia ekran startowy i rejestruje trasy.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>Tworzy powłokę nawigacyjną.</summary>
    /// <param name="homePage">Ekran startowy, wstrzykiwany przez kontener.</param>
    /// <param name="services">Kontener, z którego tworzone są strony tras.</param>
    public AppShell(HomePage homePage, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(homePage);
        ArgumentNullException.ThrowIfNull(services);

        InitializeComponent();

        Items.Add(new ShellContent
        {
            Route = Routes.HomeContent,
            Content = homePage,
        });

        RegisterRoutes(services);
    }

    /// <summary>
    /// Przypisuje strony do tras z <see cref="Routes"/>.
    /// </summary>
    /// <remarks>
    /// Nowy ekran wymaga wpisu tutaj oraz rejestracji strony i ViewModelu w kontenerze.
    /// Fabryka <see cref="ServiceProviderRouteFactory{TPage}"/> gwarantuje, że strona
    /// powstanie z kontenera, a nie przez <c>Activator.CreateInstance</c>.
    /// </remarks>
    private static void RegisterRoutes(IServiceProvider services)
    {
        Routing.RegisterRoute(Routes.Game, new ServiceProviderRouteFactory<GamePage>(services));
        Routing.RegisterRoute(Routes.Players, new ServiceProviderRouteFactory<PlayersPage>(services));
        Routing.RegisterRoute(Routes.GameModes, new ServiceProviderRouteFactory<GameModesPage>(services));
        Routing.RegisterRoute(Routes.EventPacks, new ServiceProviderRouteFactory<EventPacksPage>(services));
        Routing.RegisterRoute(Routes.Settings, new ServiceProviderRouteFactory<SettingsPage>(services));
        Routing.RegisterRoute(Routes.Rules, new ServiceProviderRouteFactory<RulesPage>(services));
        Routing.RegisterRoute(Routes.Onboarding, new ServiceProviderRouteFactory<OnboardingPage>(services));
    }
}
