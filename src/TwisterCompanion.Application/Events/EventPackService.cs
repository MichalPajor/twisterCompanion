using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Events;

/// <summary>
/// Operacje na paczkach wydarzeń: lista, wybór aktywnej, edycja, kopiowanie, przenoszenie.
/// </summary>
internal sealed class EventPackService(
    IEventPackRepository repository,
    ISettingsService settingsService,
    ILogger<EventPackService> logger)
    : IEventPackService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<EventPack>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<EventPack?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        Guid? activeId = settingsService.Current.ActiveEventPackId;

        if (activeId is null)
        {
            return null;
        }

        EventPack? pack = await repository.GetByIdAsync(activeId.Value, cancellationToken);

        if (pack is null)
        {
            // Paczka mogła zostać usunięta poza ekranem paczek albo plik uszkodzony.
            // Czyścimy wybór, żeby aplikacja nie próbowała jej szukać przy każdej partii.
            logger.LogWarning("Aktywna paczka {PackId} nie istnieje. Czyszczę wybór.", activeId);

            await SetActiveAsync(null, cancellationToken);
        }

        return pack;
    }

    /// <inheritdoc />
    public Task SetActiveAsync(Guid? packId, CancellationToken cancellationToken = default) =>
        settingsService.UpdateAsync(
            settings => settings with { ActiveEventPackId = packId },
            cancellationToken);

    /// <inheritdoc />
    public async Task<EventPack> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        EventPack pack = EventPack.Create(name);

        await repository.SaveAsync(pack, cancellationToken);

        return pack;
    }

    /// <inheritdoc />
    public Task SaveAsync(EventPack pack, CancellationToken cancellationToken = default) =>
        repository.SaveAsync(pack, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid packId, CancellationToken cancellationToken = default)
    {
        bool deleted = await repository.DeleteAsync(packId, cancellationToken);

        // Usunięcie aktywnej paczki musi wyczyścić wybór — inaczej ustawienia wskazywałyby
        // na coś, czego już nie ma.
        if (deleted && settingsService.Current.ActiveEventPackId == packId)
        {
            await SetActiveAsync(null, cancellationToken);
        }

        return deleted;
    }

    /// <inheritdoc />
    public async Task<EventPack> DuplicateAsync(
        EventPack pack,
        string newName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pack);

        EventPack copy = pack.Duplicate(newName);

        await repository.SaveAsync(copy, cancellationToken);

        return copy;
    }

}
