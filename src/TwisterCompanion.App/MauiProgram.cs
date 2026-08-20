using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using TwisterCompanion.App.DependencyInjection;
using TwisterCompanion.App.Diagnostics;
using TwisterCompanion.App.Localization;
using TwisterCompanion.App.Services;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.DependencyInjection;
using TwisterCompanion.Application.Feedback;
using TwisterCompanion.Infrastructure.DependencyInjection;
using TwisterCompanion.Presentation.DependencyInjection;

namespace TwisterCompanion.App;

/// <summary>
/// Złożenie aplikacji: rejestracja bibliotek, czcionek, serwisów, ViewModeli i stron.
/// </summary>
/// <remarks>
/// Jedyne miejsce w solucji, które zna wszystkie warstwy jednocześnie. Rejestracje są
/// pogrupowane w metody rozszerzające per warstwa, żeby ten plik nie rósł razem z
/// aplikacją i żeby część platformowo neutralna dała się przetestować.
/// </remarks>
public static class MauiProgram
{
    /// <summary>Buduje i konfiguruje aplikację MAUI.</summary>
    /// <returns>Gotowa do uruchomienia aplikacja.</returns>
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if ANDROID
        // Baner reklamowy jako kontrolka układu (Etap 15). Uchwyt musi być zarejestrowany
        // przed pierwszym wczytaniem XAML z tą kontrolką — bez rejestracji MAUI potraktuje
        // ją jako widok bez postaci platformowej i baner będzie pustym miejscem.
        builder.ConfigureMauiHandlers(handlers =>
            handlers.AddHandler<Views.BannerAdView, Platforms.Android.BannerAdViewHandler>());
#endif

        builder.Services
            .AddPlatformServices()   // implementacje portów: nawigacja, dialogi, ścieżki
            .AddInfrastructure()     // persystencja, losowość, paczki wbudowane
            .AddApplication()        // algorytm losowania i usługi domenowe
            .AddPresentation()       // ViewModele (projekt Presentation)
            .AddViews();             // Shell i strony

#if ANDROID
        // Systemowa kreska pod polami tekstowymi i polami wyboru: nasze pola mają własną
        // ramkę, więc kreska w jej środku jest drugą krawędzią. Zdejmowana raz, dla całej
        // aplikacji — patrz AndroidInputStyling.
        AndroidInputStyling.RemoveInputUnderline();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        MauiApp app = builder.Build();

        GlobalExceptionHandler.Register(app.Services.GetRequiredService<ILogger<App>>());

        // Musi nastąpić przed pierwszym wczytaniem XAML — rozszerzenie {loc:Translate}
        // sięga po tę instancję statycznie, bo parser XAML nie zna kontenera zależności.
        // Instancja aplikacji powstaje po zbudowaniu kontenera, więc kolejność jest pewna.
        LocalizationResourceManager.Initialize(app.Services.GetRequiredService<ILocalizationService>());

        // Wygląd. To wywołanie ma dwa zadania i drugie jest ważniejsze od pierwszego:
        // stosuje stan znany przed odczytem pliku, ale przede wszystkim TWORZY singleton,
        // a to jego konstruktor zapisuje się na zdarzenie zmiany ustawień. Bez utworzenia
        // tutaj nikt by nie nasłuchiwał wczytania ustawień i zapisany wygląd nie wszedłby
        // w życie. Ta sama rola przypada instrukcji powyżej dla serwisu tłumaczeń.
        app.Services.GetRequiredService<ThemeApplier>().Apply();

        // Dźwięk naciśnięcia dla wszystkich przycisków naraz. Tutaj, bo dopiero po zbudowaniu
        // kontenera da się rozwiązać serwis, a mapowanie uchwytów jest statyczne i musi zostać
        // ustawione przed pierwszym utworzeniem przycisku.
        ButtonSoundHook.Install(app.Services.GetRequiredService<IGameFeedback>());

        return app;
    }
}
