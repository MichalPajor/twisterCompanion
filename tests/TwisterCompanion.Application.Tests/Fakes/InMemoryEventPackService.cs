using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Paczki wydarzeń trzymane w pamięci, razem z jedną paczką „wbudowaną".
/// </summary>
/// <remarks>
/// Paczka wbudowana jest tu po to, żeby dało się sprawdzić najważniejszą regułę kasowania
/// danych: własne paczki znikają, wbudowane zostają.
/// </remarks>
internal sealed class InMemoryEventPackService : IEventPackService
{
    private readonly List<EventPack> _packs =
    [
        EventPack.Create("Wbudowana", [GameEvent.CreateCustom("Zadanie", 10)]) with { IsBuiltIn = true },
    ];

    private Guid? _activeId;

    /// <inheritdoc />
    public Task<IReadOnlyList<EventPack>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EventPack>>([.. _packs]);

    /// <inheritdoc />
    public Task<EventPack?> GetActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_packs.FirstOrDefault(pack => pack.Id == _activeId));

    /// <inheritdoc />
    public Task SetActiveAsync(Guid? packId, CancellationToken cancellationToken = default)
    {
        _activeId = packId;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<EventPack> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        EventPack created = EventPack.Create(name);

        _packs.Add(created);

        return Task.FromResult(created);
    }

    /// <inheritdoc />
    public Task SaveAsync(EventPack pack, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pack);

        _packs.RemoveAll(existing => existing.Id == pack.Id);
        _packs.Add(pack);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid packId, CancellationToken cancellationToken = default)
    {
        bool removed = _packs.RemoveAll(pack => pack.Id == packId) > 0;

        if (removed && _activeId == packId)
        {
            _activeId = null;
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<EventPack> DuplicateAsync(
        EventPack pack,
        string newName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pack);

        EventPack copy = EventPack.Create(newName, pack.Events);

        _packs.Add(copy);

        return Task.FromResult(copy);
    }
}
