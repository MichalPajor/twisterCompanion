using Microsoft.Extensions.DependencyInjection;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.Presentation.DependencyInjection;

/// <summary>
/// Rejestracja warstwy prezentacji w kontenerze zależności.
/// </summary>
/// <remarks>
/// Rejestracja jest tutaj, a nie w <c>MauiProgram</c>, z jednego powodu: ten kod jest
/// platformowo neutralny, więc daje się przetestować. Test „smoke DI" w projekcie
/// <c>TwisterCompanion.Presentation.Tests</c> sprawdza, czy każdy ViewModel z
/// <see cref="ViewModelTypes"/> faktycznie daje się rozwiązać z kontenera — bez
/// uruchamiania aplikacji.
/// </remarks>
public static class PresentationServiceCollectionExtensions
{
    /// <summary>
    /// Wszystkie ViewModele aplikacji — jedno źródło prawdy dla rejestracji i dla testu.
    /// </summary>
    /// <remarks>
    /// Nowy ekran dopisujemy tutaj. Test DI wyłapie brak zależności od razu,
    /// zamiast pozwolić aplikacji wywalić się przy wejściu na ekran.
    /// </remarks>
    public static IReadOnlyList<Type> ViewModelTypes { get; } =
    [
        typeof(HomeViewModel),
        typeof(GameViewModel),
        typeof(PlayersViewModel),
        typeof(GameModesViewModel),
        typeof(EventPacksViewModel),
        typeof(SettingsViewModel),
        typeof(OnboardingViewModel),
        typeof(RulesViewModel),

    ];

    /// <summary>
    /// Rejestruje ViewModele warstwy prezentacji jako <c>Transient</c>.
    /// </summary>
    /// <param name="services">Kolekcja usług.</param>
    /// <returns>Ta sama kolekcja, dla łańcuchowania wywołań.</returns>
    /// <remarks>
    /// <c>Transient</c> celowo: ViewModel trzyma stan ekranu, więc każde wejście na ekran
    /// dostaje świeżą instancję. Serwisy stanowe (ustawienia, silnik gry) rejestrowane są
    /// jako <c>Singleton</c> osobno.
    /// </remarks>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (Type viewModelType in ViewModelTypes)
        {
            services.AddTransient(viewModelType);
        }

        return services;
    }
}
