using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Advertising;
using TwisterCompanion.Application.Feedback;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Voice;

namespace TwisterCompanion.App;

/// <summary>
/// Punkt wejścia aplikacji MAUI.
/// </summary>
/// <remarks>
/// Klasa bazowa jest podana przez alias <c>MauiControlsApplication</c> — patrz
/// <c>GlobalUsings.cs</c> po wyjaśnienie kolizji nazw z warstwą aplikacji.
/// </remarks>
public partial class App : MauiControlsApplication
{
    private readonly IServiceProvider _services;
    private readonly ILogger<App> _logger;

    /// <summary>Tworzy aplikację.</summary>
    /// <param name="services">Kontener zależności aplikacji.</param>
    /// <param name="logger">Logger aplikacji.</param>
    public App(IServiceProvider services, ILogger<App> logger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        InitializeComponent();

        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Zadanie wczytywania stanu aplikacji z dysku.
    /// </summary>
    /// <remarks>
    /// Wystawione jako właściwość, a nie uruchomione „w tle i zapomniane" — kod, który
    /// potrzebuje wczytanych ustawień, może na nie poczekać, zamiast zgadywać, czy już
    /// są gotowe. Zadanie nigdy nie kończy się błędem: awaria odczytu oznacza wartości
    /// domyślne, bo aplikacja musi dać się uruchomić.
    /// </remarks>
    public Task InitializationTask { get; private set; } = Task.CompletedTask;

    /// <inheritdoc />
    /// <remarks>
    /// Powłoka jest rozwiązywana z kontenera przy każdym utworzeniu okna, a nie
    /// wstrzykiwana raz do konstruktora — okno może zostać odtworzone i wtedy
    /// współdzielenie jednej instancji Shella powodowałoby problemy.
    /// </remarks>
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(_services.GetRequiredService<AppShell>());

    /// <inheritdoc />
    protected override void OnStart()
    {
        base.OnStart();

        InitializationTask = LoadPersistedStateAsync();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Android może usunąć proces aplikacji działającej w tle w dowolnej chwili i bez
    /// ostrzeżenia. To jedyny moment, w którym da się zapisać trwającą partię — dlatego
    /// zapis idzie tutaj, a nie przy zamykaniu aplikacji.
    /// </remarks>
    protected override void OnSleep()
    {
        base.OnSleep();

        _ = SaveGameStateAsync();
    }

    private async Task SaveGameStateAsync()
    {
        try
        {
            await _services.GetRequiredService<IGameEngine>().SaveSnapshotAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Nie udało się zapisać stanu partii przy przejściu w tło.");
        }
    }

    /// <summary>
    /// Budzi silnik mowy i przygotowuje reklamy, kiedy nikt na nie nie czeka.
    /// </summary>
    /// <remarks>
    /// Oba kroki same pochłaniają swoje awarie, ale całość jest jeszcze raz otoczona
    /// przechwytywaniem: to zadanie nikt nie obserwuje, więc wyjątek z niego byłby
    /// nieprzechwycony i zabiłby proces.
    /// </remarks>
    private async Task WarmUpAsync()
    {
        try
        {
            await _services.GetRequiredService<IAnnouncementSpeaker>().PrepareAsync();
            await _services.GetRequiredService<IAdService>().PrepareAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Rozgrzewanie usług przy starcie się nie udało.");
        }
    }

    private async Task LoadPersistedStateAsync()
    {
        try
        {
            ISettingsService settingsService = _services.GetRequiredService<ISettingsService>();

            // Wczytanie zgłasza zdarzenie zmiany, więc język i wygląd stosują się same —
            // subskrybenci są utworzeni przy budowaniu kontenera, patrz MauiProgram.
            // Nie ma tu żadnego „zastosuj X po wczytaniu": każde takie wywołanie byłoby
            // drugą ścieżką do tego samego, a o jednej z nich dałoby się zapomnieć.
            await settingsService.LoadAsync();

            _logger.LogInformation(
                "Ustawienia wczytane. Język: {Culture}. Wygląd: {Theme}.",
                _services.GetRequiredService<ILocalizationService>().CurrentCulture.Name,
                settingsService.Current.ThemePreference);

            // Próbki dźwiękowe wczytujemy po ustawieniach, a nie przed: gracz mógł dźwięki
            // wyłączyć, a wtedy nie ma po co zajmować nimi pamięci. Wczytanie nie zgłasza
            // wyjątków, więc nie psuje startu aplikacji.
            if (settingsService.Current.AreSoundsEnabled)
            {
                await _services.GetRequiredService<IGameFeedback>().PreloadAsync();
            }

            // Dwie rzeczy, które budzą się długo i których nikt nie powinien czekać: silnik
            // mowy urządzenia i zestaw SDK reklam. Bez tego oba koszty spadały na pierwsze
            // wejście do rozgrywki — zgłoszone z urządzenia jako „ponad pięć sekund ciszy,
            // zanim usłyszę początek gry". Tutaj płacimy je, kiedy gracz patrzy na ekran
            // startowy.
            //
            // Bez czekania na wynik: to przyspieszenie, nie warunek. Jeśli gracz wejdzie do
            // rozgrywki, zanim się skończą, pierwsza wypowiedź poczeka tyle, ile poczekałaby
            // wcześniej — a nie dłużej.
            _ = WarmUpAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Nie udało się wczytać stanu aplikacji. Używam wartości domyślnych.");
        }
    }
}
