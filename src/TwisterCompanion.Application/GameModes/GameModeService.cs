using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.GameModes;

namespace TwisterCompanion.Application.GameModes;

/// <summary>
/// Wybór trybu gry zapamiętywany w ustawieniach.
/// </summary>
internal sealed class GameModeService(
    IGameModeCatalog catalog,
    ISettingsService settingsService,
    ILogger<GameModeService> logger)
    : IGameModeService
{
    /// <inheritdoc />
    public IReadOnlyList<GameModeDefinition> GetAvailable() => catalog.GetAvailable();

    /// <inheritdoc />
    public GameModeDefinition? Find(string key) => catalog.Find(key);

    /// <inheritdoc />
    public async Task<GameModeDefinition> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        string key = settingsService.Current.GameModeKey;
        GameModeDefinition? mode = catalog.Find(key);

        if (mode is { IsEnabled: true })
        {
            return mode;
        }

        // Tryb mógł zostać wyłączony albo usunięty w nowej wersji aplikacji. Zapisany wybór
        // poprawiamy od razu, żeby nie szukać go przy każdej partii i żeby ekran wyboru
        // pokazywał to samo, co obowiązuje w grze.
        GameModeDefinition fallback = catalog.Default;

        logger.LogWarning(
            "Zapisany tryb gry „{Key}” jest niedostępny. Obowiązuje „{Fallback}”.",
            key,
            fallback.Key);

        await SaveAsync(fallback.Key, cancellationToken);

        return fallback;
    }

    /// <inheritdoc />
    public Task SetActiveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        GameModeDefinition mode = catalog.Find(key)
            ?? throw new ArgumentException($"Tryb gry „{key}” nie istnieje.", nameof(key));

        return SaveAsync(mode.Key, cancellationToken);
    }

    private Task SaveAsync(string key, CancellationToken cancellationToken) =>
        settingsService.UpdateAsync(
            settings => settings with { GameModeKey = key },
            cancellationToken);
}
