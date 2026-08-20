using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Infrastructure.BuiltInPacks;
using TwisterCompanion.Infrastructure.Persistence.Dto;
using TwisterCompanion.Infrastructure.Persistence.Json;
using TwisterCompanion.Infrastructure.Persistence.Mapping;

namespace TwisterCompanion.Infrastructure.Persistence;

/// <summary>
/// Paczki wydarzeń przechowywane jako pojedyncze pliki JSON, po jednym na paczkę.
/// </summary>
/// <remarks>
/// Jeden plik na paczkę, a nie jeden wspólny plik z listą. Powody: uszkodzenie dotyka
/// wtedy jednej paczki zamiast wszystkich, zapis nie przepisuje danych, których nie
/// zmieniamy, a import i eksport paczki (Etap 6) sprowadza się do skopiowania pliku.
/// </remarks>
internal sealed class JsonEventPackRepository(
    IStoragePathProvider pathProvider,
    JsonDocumentStore documentStore,
    BuiltInEventPackProvider builtInPacks,
    ILogger<JsonEventPackRepository> logger)
    : IEventPackRepository
{
    private string PacksDirectory =>
        Path.Combine(pathProvider.AppDataDirectory, PersistenceSchema.PacksDirectoryName);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventPack>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        List<EventPack> packs = [.. builtInPacks.GetAll()];
        packs.AddRange(await ReadUserPacksAsync(cancellationToken));

        return packs;
    }

    /// <inheritdoc />
    public async Task<EventPack?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EventPack? builtIn = builtInPacks.GetAll().FirstOrDefault(pack => pack.Id == id);

        if (builtIn is not null)
        {
            return builtIn;
        }

        EventPackDto? dto = await documentStore.ReadAsync(
            GetPackPath(id),
            PersistenceJsonContext.Default.EventPackDto,
            cancellationToken);

        return dto is null ? null : EventPackMapper.ToDomain(dto, isBuiltIn: false);
    }

    /// <inheritdoc />
    public Task SaveAsync(EventPack pack, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pack);
        EnsureNotBuiltIn(pack.Id, pack.IsBuiltIn, "zapisać");

        return documentStore.WriteAsync(
            GetPackPath(pack.Id),
            EventPackMapper.ToDto(pack),
            PersistenceJsonContext.Default.EventPackDto,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureNotBuiltIn(id, builtInPacks.IsBuiltIn(id), "usunąć");

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(documentStore.Delete(GetPackPath(id)));
    }

    private async Task<List<EventPack>> ReadUserPacksAsync(CancellationToken cancellationToken)
    {
        List<EventPack> packs = [];

        if (!Directory.Exists(PacksDirectory))
        {
            return packs;
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(PacksDirectory, "*" + PersistenceSchema.FileExtension);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Nie udało się odczytać katalogu {Directory}.", PacksDirectory);
            return packs;
        }

        foreach (string file in files)
        {
            EventPackDto? dto = await documentStore.ReadAsync(
                file,
                PersistenceJsonContext.Default.EventPackDto,
                cancellationToken);

            if (dto is null)
            {
                continue;
            }

            EventPack? pack = EventPackMapper.ToDomain(dto, isBuiltIn: false);

            if (pack is null)
            {
                logger.LogWarning("Plik {File} nie zawiera poprawnej paczki. Pominięty.", file);
                continue;
            }

            packs.Add(pack);
        }

        return [.. packs.OrderBy(pack => pack.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    private string GetPackPath(Guid id) =>
        Path.Combine(PacksDirectory, id.ToString("D") + PersistenceSchema.FileExtension);

    private static void EnsureNotBuiltIn(Guid id, bool isBuiltIn, string operation)
    {
        if (isBuiltIn)
        {
            throw new InvalidOperationException(
                $"Nie można {operation} paczki wbudowanej ({id}). Zrób jej kopię i zmieniaj kopię.");
        }
    }
}
