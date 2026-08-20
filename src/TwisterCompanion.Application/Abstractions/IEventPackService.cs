using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Operacje na paczkach wydarzeń widziane z perspektywy ekranu.
/// </summary>
/// <remarks>
/// Serwis istnieje, bo „aktywna paczka" to informacja rozpięta między dwoma miejscami:
/// lista paczek leży w repozytorium, a wybór aktywnej w ustawieniach. Bez tego serwisu
/// ViewModel musiałby je łączyć samodzielnie i pilnować zgodności — na przykład tego, że
/// usunięcie aktywnej paczki musi też wyczyścić wybór w ustawieniach.
/// </remarks>
public interface IEventPackService
{
    /// <summary>Zwraca wszystkie paczki — wbudowane i użytkownika.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task<IReadOnlyList<EventPack>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Zwraca aktywną paczkę albo <see langword="null"/>, gdy gramy bez wydarzeń.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task<EventPack?> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Ustawia aktywną paczkę.</summary>
    /// <param name="packId">Identyfikator paczki albo <see langword="null"/>, by grać bez wydarzeń.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task SetActiveAsync(Guid? packId, CancellationToken cancellationToken = default);

    /// <summary>Tworzy nową, pustą paczkę użytkownika.</summary>
    /// <param name="name">Nazwa paczki.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task<EventPack> CreateAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Zapisuje zmienioną paczkę użytkownika.</summary>
    /// <param name="pack">Paczka do zapisania.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task SaveAsync(EventPack pack, CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa paczkę użytkownika i — jeśli była aktywna — czyści wybór w ustawieniach.
    /// </summary>
    /// <param name="packId">Identyfikator paczki.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><see langword="true"/>, jeśli paczka istniała i została usunięta.</returns>
    Task<bool> DeleteAsync(Guid packId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tworzy edytowalną kopię paczki i od razu ją zapisuje.
    /// </summary>
    /// <param name="pack">Paczka do skopiowania.</param>
    /// <param name="newName">Nazwa kopii.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// To jedyny sposób zmiany zawartości paczki wbudowanej — kopiuje się ją i zmienia kopię.
    /// </remarks>
    Task<EventPack> DuplicateAsync(
        EventPack pack,
        string newName,
        CancellationToken cancellationToken = default);

}
