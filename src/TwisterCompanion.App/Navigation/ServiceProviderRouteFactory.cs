namespace TwisterCompanion.App.Navigation;

/// <summary>
/// Fabryka tras, która tworzy strony przez kontener zależności.
/// </summary>
/// <typeparam name="TPage">Typ strony przypisanej do trasy.</typeparam>
/// <remarks>
/// Celowo zamiast <c>Routing.RegisterRoute(route, typeof(TPage))</c>. Domyślna fabryka
/// tras potrafi sięgnąć po <c>Activator.CreateInstance</c>, a nasze strony mają
/// konstruktory z wstrzykiwanym ViewModelem — brak bezparametrowego konstruktora
/// wywaliłby aplikację przy pierwszej nawigacji. Jawna fabryka usuwa tę zależność
/// od szczegółu implementacyjnego frameworka.
/// </remarks>
/// <param name="services">Kontener, z którego rozwiązywane są strony.</param>
internal sealed class ServiceProviderRouteFactory<TPage>(IServiceProvider services) : RouteFactory
    where TPage : Page
{
    /// <inheritdoc />
    public override Element GetOrCreate() => services.GetRequiredService<TPage>();

    /// <inheritdoc />
    public override Element GetOrCreate(IServiceProvider serviceProvider) =>
        (serviceProvider ?? services).GetRequiredService<TPage>();
}
