using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Infrastructure.Persistence.Dto;
using TwisterCompanion.Infrastructure.Persistence.Json;
using TwisterCompanion.Infrastructure.Persistence.Mapping;

namespace TwisterCompanion.Infrastructure.Persistence;

/// <summary>
/// Lista graczy zapamiętywana w jednym pliku JSON.
/// </summary>
internal sealed class JsonPlayerRosterRepository(
    IStoragePathProvider pathProvider,
    JsonDocumentStore documentStore)
    : IPlayerRosterRepository
{
    private string RosterPath =>
        Path.Combine(pathProvider.AppDataDirectory, PersistenceSchema.PlayerRosterFileName);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Player>> GetAsync(CancellationToken cancellationToken = default)
    {
        PlayerRosterDto? dto = await documentStore.ReadAsync(
            RosterPath,
            PersistenceJsonContext.Default.PlayerRosterDto,
            cancellationToken);

        return dto is null ? [] : PlayerMapper.ToDomain(dto);
    }

    /// <inheritdoc />
    public Task SaveAsync(IReadOnlyList<Player> players, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(players);

        return documentStore.WriteAsync(
            RosterPath,
            PlayerMapper.ToDto(players),
            PersistenceJsonContext.Default.PlayerRosterDto,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        documentStore.Delete(RosterPath);

        return Task.CompletedTask;
    }
}
