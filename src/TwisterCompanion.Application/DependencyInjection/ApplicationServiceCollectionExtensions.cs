using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Advertising;
using TwisterCompanion.Application.Events;
using TwisterCompanion.Application.Feedback;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Game.Steps;
using TwisterCompanion.Application.GameModes;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Application.VoiceControl;
using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.EventSelection;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Application.DependencyInjection;

/// <summary>
/// Rejestracja warstwy aplikacji i usług domenowych w kontenerze zależności.
/// </summary>
/// <remarks>
/// Rejestracja typów domenowych jest tutaj, a nie w warstwie <c>Domain</c>, bo Domain
/// celowo nie ma żadnej zależności zewnętrznej — także od kontenera. Warstwa aplikacji
/// jest naturalnym miejscem składania usług domenowych.
/// </remarks>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Rejestruje silnik gry, potok tury, komunikaty i algorytm losowania.</summary>
    /// <param name="services">Kolekcja usług.</param>
    /// <returns>Ta sama kolekcja, dla łańcuchowania wywołań.</returns>
    /// <remarks>
    /// Domyślnie działa <see cref="SmartMoveSelectionStrategy"/>. Podmiana na klasyczny
    /// spinner to jedna linia w tym miejscu — silnik gry zna wyłącznie interfejs
    /// <see cref="IMoveSelectionStrategy"/> i nie wymaga żadnej zmiany.
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Strategie losowania są bezstanowe, więc mogą być singletonami.
        services.AddSingleton<IMoveSelectionStrategy, SmartMoveSelectionStrategy>();
        services.AddSingleton<IEventSelector, WeightedEventSelector>();

        services.AddSingleton<IAnnouncementBuilder, AnnouncementBuilder>();

        // Singleton, bo trzyma stan trwającej wypowiedzi i rozgłasza jego zmiany.
        // Etap 8 podłączy się tutaj, żeby wyciszyć mikrofon na czas mowy aplikacji.
        services.AddSingleton<IAnnouncementSpeaker, AnnouncementSpeaker>();

        // Dźwięk i wibracja jako odpowiedź na zdarzenia gry. Singleton, bo trzyma tylko
        // zależności — a przez niego przechodzi każda decyzja „czy teraz zabrzmieć".
        services.AddSingleton<IGameFeedback, GameFeedback>();

        // Kasowanie danych użytkownika w jednym miejscu: rodzajów danych jest cztery i przy
        // każdym kolejnym ekran ustawień musiałby o nim pamiętać.
        services.AddSingleton<IUserDataService, UserDataService>();
        services.AddSingleton<IEventPackService, EventPackService>();
        services.AddSingleton<IGameModeService, GameModeService>();

        AddVoiceControl(services);
        AddAdvertising(services);

        // Źródło czasu: pozwala testować tryb automatyczny bez czekania w teście
        // ośmiu sekund na turę.
        services.AddSingleton(TimeProvider.System);

        AddTurnPipeline(services);

        // Silnik trzyma stan trwającej partii, więc musi być singletonem — ekran rozgrywki
        // dostaje ten sam obiekt po każdym powrocie na ekran.
        services.AddSingleton<IGameEngine, GameEngine>();

        return services;
    }

    /// <summary>
    /// Rejestruje sterowanie głosem.
    /// </summary>
    /// <remarks>
    /// Wszystkie trzy elementy są singletonami z różnych powodów: rejestr i parser trzymają
    /// wczytane frazy, nasłuch trzyma stan mikrofonu, a koordynator subskrypcję silnika gry.
    /// <para>
    /// Parametry nasłuchu są rejestrowane jako obiekt, a nie wpisane w klasę: pochodzą
    /// z pomiarów na urządzeniu i będą się zmieniać razem z kolejnymi pomiarami.
    /// </para>
    /// </remarks>
    private static void AddVoiceControl(IServiceCollection services)
    {
        services.AddSingleton(VoiceControlOptions.Default);
        services.AddSingleton<IVoiceCommandRegistry, VoiceCommandRegistry>();
        services.AddSingleton<IVoiceCommandParser, VoiceCommandParser>();
        services.AddSingleton<IVoiceControlService, VoiceControlService>();
        services.AddSingleton<IVoiceControlCoordinator, VoiceControlCoordinator>();
    }

    /// <summary>
    /// Rejestruje kroki potoku tury.
    /// </summary>
    /// <remarks>
    /// <b>Kolejność rejestracji jest kolejnością wykonania</b> — to jedyne miejsce, które
    /// o niej decyduje. Kolejne etapy wstawiają tu swoje kroki:
    /// <list type="bullet">
    /// <item>Etap 6 — losowanie wydarzeń, między wyborem ruchu a zapisem tury
    /// (<see cref="RollEventStep"/>, dołożone bez zmiany pozostałych kroków);</item>
    /// <item>Etap 7 — odczyt głosowy, po zbudowaniu komunikatu.</item>
    /// </list>
    /// </remarks>
    /// <summary>Rejestruje reklamy: port platformy, reguły i koordynator.</summary>
    /// <remarks>
    /// Port platformy idzie przez <c>TryAdd</c>, więc projekt aplikacji może podstawić
    /// implementację AdMob <b>przed</b> tą rejestracją, a bez niej obowiązuje wersja
    /// nieobecna. Dzięki temu testy i buildy deweloperskie nie widzą żadnych reklam bez
    /// jednej linii konfiguracji.
    /// <para>
    /// Reguły są osobnym typem od portu i to one są rejestrowane jako <c>IAdService</c>:
    /// każde żądanie reklamy przechodzi przez nie, bo innej drogi do platformy nie ma.
    /// </para>
    /// </remarks>
    private static void AddAdvertising(IServiceCollection services)
    {
        services.AddSingleton(AdOptions.Default);
        services.TryAddSingleton<IAdPlatform, NoOpAdPlatform>();
        services.AddSingleton<IAdService, GuardedAdService>();
        services.AddSingleton<IAdCoordinator, AdCoordinator>();
    }

    private static void AddTurnPipeline(IServiceCollection services)
    {
        services.AddSingleton<ITurnPipelineStep, SelectPlayerStep>();
        services.AddSingleton<ITurnPipelineStep, SelectMoveStep>();
        services.AddSingleton<ITurnPipelineStep, RollEventStep>();
        services.AddSingleton<ITurnPipelineStep, RecordTurnStep>();
        services.AddSingleton<ITurnPipelineStep, BuildAnnouncementStep>();
    }
}
