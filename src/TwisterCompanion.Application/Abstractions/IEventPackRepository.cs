using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Trwałe przechowywanie paczek Custom Events.
/// </summary>
public interface IEventPackRepository
{
    /// <summary>Zwraca wszystkie paczki — wbudowane i użytkownika.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task<IReadOnlyList<EventPack>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Zwraca paczkę o podanym identyfikatorze albo <see langword="null"/>.</summary>
    /// <param name="id">Identyfikator paczki.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task<EventPack?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Zapisuje paczkę — nową albo zmienioną.</summary>
    /// <param name="pack">Paczka do zapisania.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <exception cref="InvalidOperationException">Gdy paczka jest wbudowana.</exception>
    Task SaveAsync(EventPack pack, CancellationToken cancellationToken = default);

    /// <summary>Usuwa paczkę użytkownika.</summary>
    /// <param name="id">Identyfikator paczki.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><see langword="true"/>, jeśli paczka istniała i została usunięta.</returns>
    /// <exception cref="InvalidOperationException">Gdy paczka jest wbudowana.</exception>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
