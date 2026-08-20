using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Infrastructure.Persistence.Dto;
using TwisterCompanion.Infrastructure.Persistence.Json;
using TwisterCompanion.Infrastructure.Persistence.Mapping;

namespace TwisterCompanion.Infrastructure.Persistence;

/// <summary>
/// Zapis przerwanej partii w pliku JSON.
/// </summary>
internal sealed class JsonGameSessionRepository(
    IStoragePathProvider pathProvider,
    JsonDocumentStore documentStore)
    : IGameSessionRepository
{
    private string SessionPath =>
        Path.Combine(pathProvider.AppDataDirectory, PersistenceSchema.GameSessionFileName);

    /// <inheritdoc />
    public Task SaveAsync(GameSessionSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return documentStore.WriteAsync(
            SessionPath,
            GameSessionMapper.ToDto(snapshot),
            PersistenceJsonContext.Default.GameSessionDto,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GameSessionSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
    {
        GameSessionDto? dto = await documentStore.ReadAsync(
            SessionPath,
            PersistenceJsonContext.Default.GameSessionDto,
            cancellationToken);

        return dto is null ? null : GameSessionMapper.ToDomain(dto);
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        documentStore.Delete(SessionPath);

        return Task.CompletedTask;
    }
}
