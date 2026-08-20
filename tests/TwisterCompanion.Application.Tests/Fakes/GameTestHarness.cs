using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Advertising;
using TwisterCompanion.Application.DependencyInjection;
using TwisterCompanion.Application.Feedback;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Application.VoiceControl;
using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Randomness;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Środowisko testowe silnika gry: kontener z podstawionym czasem, losowością i zapisem.
/// </summary>
/// <remarks>
/// Silnik i budowanie komunikatów są <c>internal</c> i celowo nie odsłaniamy ich testom.
/// Rozwiązanie ich z kontenera sprawdza przy okazji, czy rejestracja jest poprawna —
/// gdyby test konstruował je bezpośrednio, ta warstwa weryfikacji zniknęłaby.
/// </remarks>
internal sealed class GameTestHarness : IDisposable
{
    private readonly ServiceProvider _services;

    /// <summary>Tworzy środowisko testowe.</summary>
    /// <param name="randomSeed">Ziarno losowości.</param>
    /// <param name="useResourceLocalization">
    /// Czy tłumaczenia mają pochodzić z prawdziwych plików zasobów.
    /// </param>
    /// <param name="voiceControlOptions">
    /// Parametry nasłuchu komend; domyślne odmierzają sekundy, więc testy podają własne.
    /// </param>
    /// <param name="useRealTime">
    /// Czy czas ma płynąć naprawdę, zamiast być sterowany z testu.
    /// </param>
    /// <remarks>
    /// Domyślnie działają tłumaczenia zastępcze: testy komunikatów sprawdzają kolejność
    /// i kompletność członów, więc przewidywalne wzorce są wygodniejsze od rzeczywistych
    /// tekstów. Testy komend głosowych potrzebują odwrotnie — <b>prawdziwych fraz</b>,
    /// bo to one decydują o tym, co zostanie rozpoznane.
    /// </remarks>
    public GameTestHarness(
        int randomSeed = 12345,
        bool useResourceLocalization = false,
        VoiceControlOptions? voiceControlOptions = null,
        bool useRealTime = false)
    {
        ServiceCollection services = new();

        Localization = useResourceLocalization
            ? new ResourceLocalizationService()
            : new FakeLocalizationService();

        services.AddLogging();
        services.AddSingleton(Localization);
        services.AddSingleton<IAudioCueService>(AudioCues);
        services.AddSingleton<ISpeechRecognitionService>(Recognition);
        services.AddSingleton<IGameSessionRepository>(SessionRepository);
        services.AddSingleton<IRandomProvider>(new SeededRandomProvider(randomSeed));
        services.AddSingleton<ISettingsService>(SettingsService);
        services.AddSingleton<ITextToSpeechService>(TextToSpeech);
        services.AddSingleton<ISoundService>(Sounds);
        services.AddSingleton<IPlayerRosterRepository>(PlayerRoster);
        services.AddSingleton<IHapticService>(Haptics);

        // Reklamy PRZED AddApplication: tamta rejestruje wersję nieobecną przez TryAdd, więc
        // pierwszy zarejestrowany wygrywa — dokładnie tak, jak robi to projekt aplikacji.
        services.AddSingleton<IAdPlatform>(Ads);

        services.AddApplication();

        if (voiceControlOptions is not null)
        {
            services.AddSingleton(voiceControlOptions);
        }

        // Rejestracja po AddApplication nadpisuje TimeProvider.System — ostatnia wygrywa.
        // Bez tego test trybu automatycznego musiałby realnie czekać osiem sekund na turę.
        //
        // Wyjątkiem są testy nasłuchu komend: ich pętla czeka jednocześnie na upływ czasu
        // i na zdarzenie z platformy, więc sterowany zegar musiałby być przesuwany z innego
        // wątku w nieokreślonym momencie. Tam czas płynie naprawdę, a odstępy są skrócone
        // do dziesiątek milisekund.
        services.AddSingleton(useRealTime ? System.TimeProvider.System : TimeProvider);

        // Paczki wydarzeń rejestrujemy PO AddApplication: prawdziwy serwis paczek sięga do
        // repozytorium z warstwy infrastruktury, którego w testach warstwy aplikacji nie ma.
        services.AddSingleton<IEventPackService>(EventPacks);

        _services = services.BuildServiceProvider();
    }

    /// <summary>Sterowany zegar — pozwala „przesunąć czas" bez czekania.</summary>
    public FakeTimeProvider TimeProvider { get; } = new(new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Zapis partii trzymany w pamięci.</summary>
    public InMemoryGameSessionRepository SessionRepository { get; } = new();

    /// <summary>Reklamy zastępcze — zapisują żądania.</summary>
    public FakeAdPlatform Ads { get; } = new();

    /// <summary>Syntezator mowy zastępczy — zapisuje wypowiedzi.</summary>
    public FakeTextToSpeechService TextToSpeech { get; } = new();

    /// <summary>Tłumaczenia — zastępcze albo z prawdziwych plików zasobów.</summary>
    public ILocalizationService Localization { get; }

    /// <summary>Sygnały dźwiękowe zastępcze.</summary>
    public FakeAudioCueService AudioCues { get; } = new();

    /// <summary>Odtwarzacz efektów dźwiękowych zastępczy.</summary>
    public FakeSoundService Sounds { get; } = new();

    /// <summary>Wibracje zastępcze.</summary>
    public FakeHapticService Haptics { get; } = new();

    /// <summary>Skład graczy trzymany w pamięci.</summary>
    public InMemoryPlayerRoster PlayerRoster { get; } = new();

    /// <summary>Paczki wydarzeń trzymane w pamięci.</summary>
    public InMemoryEventPackService EventPacks { get; } = new();

    /// <summary>Rozpoznawanie mowy zastępcze.</summary>
    public FakeSpeechRecognitionService Recognition { get; } = new();

    /// <summary>Ustawienia trzymane w pamięci.</summary>
    /// <remarks>
    /// Nazwa <c>Settings</c> jest zajęta przez przestrzeń nazw
    /// <c>TwisterCompanion.Application.Settings</c>, do której odwołuje się sygnatura
    /// <see cref="Configuration"/> — właściwość o tej nazwie przysłoniłaby ją.
    /// </remarks>
    public InMemorySettingsService SettingsService { get; } = new();

    /// <summary>Reklamy z regułami — jedyna droga do platformy reklam.</summary>
    public IAdService AdService => _services.GetRequiredService<IAdService>();

    /// <summary>Koordynator reklam.</summary>
    public IAdCoordinator AdCoordinator => _services.GetRequiredService<IAdCoordinator>();

    /// <summary>Silnik gry z podstawionymi zależnościami.</summary>
    public IGameEngine Engine => _services.GetRequiredService<IGameEngine>();

    /// <summary>Budowanie komunikatów rozwiązane z kontenera.</summary>
    public IAnnouncementBuilder AnnouncementBuilder => _services.GetRequiredService<IAnnouncementBuilder>();

    /// <summary>Warstwa odczytu komunikatów rozwiązana z kontenera.</summary>
    public IAnnouncementSpeaker Speaker => _services.GetRequiredService<IAnnouncementSpeaker>();

    /// <summary>Reakcje dźwiękowe i wibracje rozwiązane z kontenera.</summary>
    public IGameFeedback Feedback => _services.GetRequiredService<IGameFeedback>();

    /// <summary>Operacje na danych użytkownika rozwiązane z kontenera.</summary>
    public IUserDataService UserData => _services.GetRequiredService<IUserDataService>();

    /// <summary>Nasłuch komend głosowych rozwiązany z kontenera.</summary>
    public IVoiceControlService VoiceControl => _services.GetRequiredService<IVoiceControlService>();

    /// <summary>Koordynator sterowania głosem rozwiązany z kontenera.</summary>
    public IVoiceControlCoordinator VoiceCoordinator =>
        _services.GetRequiredService<IVoiceControlCoordinator>();

    /// <summary>
    /// Parser komend głosowych rozwiązany z kontenera.
    /// </summary>
    /// <remarks>
    /// Chodzi po prawdziwych frazach z plików zasobów — patrz
    /// <see cref="ResourceLocalizationService"/>.
    /// </remarks>
    public IVoiceCommandParser VoiceCommandParser =>
        _services.GetRequiredService<IVoiceCommandParser>();

    /// <summary>Tworzy konfigurację partii z podaną liczbą graczy.</summary>
    /// <param name="playerCount">Liczba graczy.</param>
    /// <param name="advanceMode">Sposób przechodzenia do następnej tury.</param>
    /// <param name="moveTime">Czas na wykonanie ruchu w trybie automatycznym.</param>
    public static GameConfiguration Configuration(
        int playerCount,
        Settings.TurnAdvanceMode advanceMode = Settings.TurnAdvanceMode.Manual,
        TimeSpan? moveTime = null) => new()
        {
            Players = CreatePlayers(playerCount),
            TurnAdvanceMode = advanceMode,
            MoveTime = moveTime ?? TimeSpan.FromSeconds(8),

            // Testy nie odmierzają realnych sekund. Przerwy i czas na zadanie sprawdzają
            // osobne testy, ustawiając je jawnie.
            NameAnnouncementPause = TimeSpan.Zero,
            TaskTime = TimeSpan.Zero,
        };

    /// <summary>Tworzy listę graczy o przewidywalnych nazwach.</summary>
    /// <param name="count">Liczba graczy.</param>
    public static IReadOnlyList<Player> CreatePlayers(int count) =>
    [
        .. Enumerable.Range(0, count).Select(index =>
            Player.Create(string.Create(CultureInfo.InvariantCulture, $"Gracz {index + 1}"), index)),
    ];

    public void Dispose() => _services.Dispose();
}
