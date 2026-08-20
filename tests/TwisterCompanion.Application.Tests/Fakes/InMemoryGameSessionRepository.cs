using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Game;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Zapis partii trzymany w pamięci.
/// </summary>
internal sealed class InMemoryGameSessionRepository : IGameSessionRepository
{
    /// <summary>Aktualnie przechowywany zapis.</summary>
    public GameSessionSnapshot? Snapshot { get; private set; }

    /// <summary>Ile razy zapisano stan partii.</summary>
    public int SaveCount { get; private set; }

    /// <summary>Ile razy usunięto zapis.</summary>
    public int ClearCount { get; private set; }

    /// <inheritdoc />
    public Task SaveAsync(GameSessionSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        Snapshot = snapshot;
        SaveCount++;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<GameSessionSnapshot?> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Snapshot);

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        Snapshot = null;
        ClearCount++;

        return Task.CompletedTask;
    }
}
