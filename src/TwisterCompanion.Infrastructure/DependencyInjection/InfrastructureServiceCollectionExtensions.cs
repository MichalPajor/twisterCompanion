using Microsoft.Extensions.DependencyInjection;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Randomness;
using TwisterCompanion.Infrastructure.BuiltInModes;
using TwisterCompanion.Infrastructure.BuiltInPacks;
using TwisterCompanion.Infrastructure.Localization;
using TwisterCompanion.Infrastructure.Persistence;

namespace TwisterCompanion.Infrastructure.DependencyInjection;

/// <summary>
/// Rejestracja warstwy infrastruktury w kontenerze zależności.
/// </summary>
/// <remarks>
/// Rejestracja jest tutaj, a nie w <c>MauiProgram</c>, żeby host nie musiał znać
/// wewnętrznych typów tej warstwy — wszystkie implementacje są <c>internal</c>.
/// Host podaje jedynie <see cref="IStoragePathProvider"/>, bo tylko on wie, gdzie na
/// danej platformie leży katalog danych aplikacji.
/// </remarks>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Rejestruje persystencję i pozostałe usługi infrastruktury.</summary>
    /// <param name="services">Kolekcja usług.</param>
    /// <returns>Ta sama kolekcja, dla łańcuchowania wywołań.</returns>
    /// <remarks>
    /// Serwis ustawień jest singletonem, bo trzyma aktualny stan i rozgłasza zmiany.
    /// Repozytoria są bezstanowe, ale też jako singletony — nie ma powodu tworzyć ich
    /// przy każdym użyciu.
    /// </remarks>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IRandomProvider, SystemRandomProvider>();

        services.AddSingleton<JsonDocumentStore>();
        services.AddSingleton<BuiltInEventPackProvider>();

        services.AddSingleton<IEventPackRepository, JsonEventPackRepository>();
        services.AddSingleton<IPlayerRosterRepository, JsonPlayerRosterRepository>();
        services.AddSingleton<IGameSessionRepository, JsonGameSessionRepository>();
        services.AddSingleton<ISettingsService, JsonSettingsService>();

        // Definicje trybów gry z pliku osadzonego w bibliotece. Singleton, bo wynik odczytu
        // jest zapamiętywany — plik nie zmienia się w czasie działania aplikacji.
        services.AddSingleton<IGameModeCatalog, EmbeddedGameModeCatalog>();

        // Singleton, bo trzyma aktualny język i rozgłasza jego zmiany. Zależy od
        // ISettingsService, więc musi być rejestrowany po nim.
        services.AddSingleton<ILocalizationService, ResxLocalizationService>();

        return services;
    }
}
