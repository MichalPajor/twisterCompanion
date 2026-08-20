using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Skład graczy trzymany w pamięci.
/// </summary>
internal sealed class InMemoryPlayerRoster : IPlayerRosterRepository
{
    private IReadOnlyList<Player> _players = [];

    /// <inheritdoc />
    public Task<IReadOnlyList<Player>> GetAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_players);

    /// <inheritdoc />
    public Task SaveAsync(IReadOnlyList<Player> players, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(players);

        _players = [.. players];

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _players = [];

        return Task.CompletedTask;
    }
}
