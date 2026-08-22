using TwisterCompanion.App.Services;
using TwisterCompanion.App.Views;
using TwisterCompanion.Presentation.Abstractions;
using IAdPlatform = TwisterCompanion.Application.Advertising.IAdPlatform;
using IAudioCueService = TwisterCompanion.Application.Abstractions.IAudioCueService;
using IHapticService = TwisterCompanion.Application.Abstractions.IHapticService;
using ISoundService = TwisterCompanion.Application.Abstractions.ISoundService;
using ISpeechRecognitionService = TwisterCompanion.Application.Abstractions.ISpeechRecognitionService;
using IStoragePathProvider = TwisterCompanion.Application.Abstractions.IStoragePathProvider;
using ITextToSpeechService = TwisterCompanion.Application.Abstractions.ITextToSpeechService;

namespace TwisterCompanion.App.DependencyInjection;

/// <summary>
/// Rejestracja elementów należących do hosta MAUI: implementacji portów prezentacji,
/// Shella i stron.
/// </summary>
internal static class AppServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje implementacje interfejsów, które warstwa prezentacji tylko deklaruje.
    /// </summary>
    /// <param name="services">Kolekcja usług.</param>
    /// <returns>Ta sama kolekcja, dla łańcuchowania wywołań.</returns>
    /// <remarks>
    /// <c>Singleton</c>, bo oba serwisy są bezstanowe — sięgają po aktualny
    /// <c>Shell</c> lub aktywną stronę w momencie wywołania.
    /// </remarks>
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<INavigationService, ShellNavigationService>();
        services.AddSingleton<IDialogService, MauiDialogService>();
        services.AddSingleton<IExternalBrowser, MauiExternalBrowser>();

        // Odliczanie czasu tury tyka na wątku puli, a widok wolno zmieniać tylko z wątku
        // interfejsu — ten port jest jedynym miejscem, które o tym wie.
        services.AddSingleton<IUiDispatcher, MauiUiDispatcher>();

        // Jedyna zależność platformowa persystencji: gdzie leży katalog danych aplikacji.
        // Sam zapis i odczyt plików w warstwie Infrastructure jest już przenośny.
        services.AddSingleton<IStoragePathProvider, MauiStoragePathProvider>();

        // Syntezator mowy urządzenia — offline, bez żadnego serwisu zewnętrznego.
        services.AddSingleton<ITextToSpeechService, MauiTextToSpeechService>();

        // Rozpoznawanie mowy z MAUI Community Toolkit. Singleton, bo obie implementacje
        // toolkitu są singletonami, a adapter pilnuje, żeby w danej chwili nasłuchiwała
        // tylko jedna z nich.
        services.AddSingleton<ISpeechRecognitionService, ToolkitSpeechRecognitionService>();

        // Sygnały stanu mikrofonu. Singleton, bo generator tonów jest zasobem systemowym
        // i nie ma sensu tworzyć go co turę.
        services.AddSingleton<IAudioCueService, AudioCueService>();

        // Motyw kolorystyczny: nasłuchuje ustawień i przestawia wygląd aplikacji.
        services.AddSingleton<ThemeApplier>();

        // Czy wolno animować — systemowe ograniczenie animacji plus przełącznik w ustawieniach.
        services.AddSingleton<IAnimationPolicy, AnimationPolicy>();

        // Efekty dźwiękowe rozgrywki. Singleton, bo pula dźwięków trzyma rozpakowane próbki
        // w pamięci — tworzenie jej na każde odtworzenie byłoby dokładnie tym, czego pula
        // ma unikać.
        services.AddSingleton<ISoundService, SoundEffectService>();

        // Wibracje: bezstanowe, całą pracę wykonuje system.
        services.AddSingleton<IHapticService, HapticService>();

#if ANDROID
        // Reklamy (Etap 15). Rejestracja jest TUTAJ, a nie w warstwie aplikacji, i musi
        // nastąpić PRZED AddApplication(): tamta rejestruje wersję nieobecną przez TryAdd,
        // więc pierwszy zarejestrowany wygrywa. Dzięki temu build bez tej linii — albo inna
        // platforma — po prostu nie ma reklam i nie wymaga żadnej innej zmiany.
        services.AddSingleton<IAdPlatform, Platforms.Android.AdMobService>();
#endif

        return services;
    }

    /// <summary>Rejestruje Shella i wszystkie strony aplikacji.</summary>
    /// <param name="services">Kolekcja usług.</param>
    /// <returns>Ta sama kolekcja, dla łańcuchowania wywołań.</returns>
    /// <remarks>
    /// Wszystko jako <c>Transient</c>: strona trzyma stan UI, więc każde wejście
    /// na ekran dostaje świeżą instancję razem ze świeżym ViewModelem.
    /// Nowy ekran dopisujemy tutaj oraz w <c>AppShell.RegisterRoutes</c>.
    /// </remarks>
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<AppShell>();

        services.AddTransient<HomePage>();
        services.AddTransient<GamePage>();
        services.AddTransient<PlayersPage>();
        services.AddTransient<GameModesPage>();
        services.AddTransient<EventPacksPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<RulesPage>();
        services.AddTransient<OnboardingPage>();

        return services;
    }
}
