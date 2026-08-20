using Microsoft.Extensions.DependencyInjection;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.DependencyInjection;
using TwisterCompanion.Application.GameModes;
using TwisterCompanion.Infrastructure.DependencyInjection;

namespace TwisterCompanion.Infrastructure.Tests.Fixtures;

/// <summary>
/// Katalog tymczasowy udający katalog danych aplikacji, wraz z gotowym kontenerem.
/// </summary>
/// <remarks>
/// Testy sięgają wyłącznie po publiczne interfejsy rozwiązane z kontenera — dokładnie
/// tak, jak robi to aplikacja. Implementacje w warstwie infrastruktury są <c>internal</c>
/// i celowo nie odsłaniamy ich testom: gdyby test konstruował je bezpośrednio, przestałby
/// sprawdzać, czy rejestracja w kontenerze jest poprawna.
/// </remarks>
internal sealed class TemporaryStorage : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly bool _ownsDirectory;

    /// <summary>Tworzy nowy katalog tymczasowy i kontener.</summary>
    public TemporaryStorage()
        : this(CreateTemporaryDirectory(), ownsDirectory: true)
    {
    }

    /// <summary>
    /// Tworzy kontener na istniejącym katalogu — odpowiednik ponownego uruchomienia
    /// aplikacji na tych samych danych.
    /// </summary>
    /// <param name="root">Katalog danych.</param>
    public TemporaryStorage(string root)
        : this(root, ownsDirectory: false)
    {
    }

    private TemporaryStorage(string root, bool ownsDirectory)
    {
        Root = root;
        _ownsDirectory = ownsDirectory;

        Directory.CreateDirectory(Root);

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IStoragePathProvider>(new FixedStoragePathProvider(Root));
        services.AddInfrastructure();
        services.AddApplication();

        _services = services.BuildServiceProvider();
    }

    /// <summary>Katalog danych używany w teście.</summary>
    public string Root { get; }

    /// <summary>Katalog paczek wydarzeń użytkownika.</summary>
    public string PacksDirectory => Path.Combine(Root, "packs");

    /// <summary>Repozytorium paczek wydarzeń.</summary>
    public IEventPackRepository EventPacks => _services.GetRequiredService<IEventPackRepository>();

    /// <summary>Operacje na paczkach wydarzeń.</summary>
    public IEventPackService EventPackService => _services.GetRequiredService<IEventPackService>();

    /// <summary>Repozytorium listy graczy.</summary>
    public IPlayerRosterRepository PlayerRoster => _services.GetRequiredService<IPlayerRosterRepository>();

    /// <summary>Serwis ustawień.</summary>
    public ISettingsService Settings => _services.GetRequiredService<ISettingsService>();

    /// <summary>Katalog trybów gry.</summary>
    public IGameModeCatalog GameModeCatalog => _services.GetRequiredService<IGameModeCatalog>();

    /// <summary>Serwis trybów gry.</summary>
    public IGameModeService GameModes => _services.GetRequiredService<IGameModeService>();

    /// <summary>Serwis tłumaczeń.</summary>
    public ILocalizationService Localization => _services.GetRequiredService<ILocalizationService>();

    /// <summary>Repozytorium zapisu przerwanej partii.</summary>
    public IGameSessionRepository GameSessions => _services.GetRequiredService<IGameSessionRepository>();

    /// <summary>Zapisuje surową treść do pliku paczki — do testów uszkodzonych danych.</summary>
    /// <param name="fileName">Nazwa pliku wraz z rozszerzeniem.</param>
    /// <param name="content">Treść pliku.</param>
    public void WriteRawPackFile(string fileName, string content)
    {
        Directory.CreateDirectory(PacksDirectory);
        File.WriteAllText(Path.Combine(PacksDirectory, fileName), content);
    }

    /// <summary>Zapisuje surową treść do pliku ustawień.</summary>
    /// <param name="content">Treść pliku.</param>
    public void WriteRawSettingsFile(string content) =>
        File.WriteAllText(Path.Combine(Root, "settings.json"), content);

    /// <summary>Zapisuje surową treść do pliku listy graczy.</summary>
    /// <param name="content">Treść pliku.</param>
    public void WriteRawRosterFile(string content) =>
        File.WriteAllText(Path.Combine(Root, "players.json"), content);

    /// <summary>Zapisuje surową treść do pliku zapisu partii.</summary>
    /// <param name="content">Treść pliku.</param>
    public void WriteRawSessionFile(string content) =>
        File.WriteAllText(Path.Combine(Root, "session.json"), content);

    public void Dispose()
    {
        _services.Dispose();

        if (!_ownsDirectory)
        {
            return;
        }

        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Pozostawiony katalog tymczasowy nie jest powodem do wywalenia testu.
        }
    }

    private static string CreateTemporaryDirectory() =>
        Path.Combine(Path.GetTempPath(), "twister-companion-tests", Guid.NewGuid().ToString("N"));

    private sealed class FixedStoragePathProvider(string root) : IStoragePathProvider
    {
        public string AppDataDirectory => root;
    }
}
