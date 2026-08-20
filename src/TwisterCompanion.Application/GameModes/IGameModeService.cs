using TwisterCompanion.Domain.GameModes;

namespace TwisterCompanion.Application.GameModes;

/// <summary>
/// Wybór trybu gry i dostęp do jego nastaw.
/// </summary>
/// <remarks>
/// Rozdzielone od <see cref="Abstractions.IGameModeCatalog"/>: katalog wie, jakie tryby
/// istnieją, a ten serwis — który z nich jest wybrany i co z tego wynika dla partii.
/// </remarks>
public interface IGameModeService
{
    /// <summary>Zwraca tryby dostępne do wyboru.</summary>
    IReadOnlyList<GameModeDefinition> GetAvailable();

    /// <summary>Zwraca wybrany tryb.</summary>
    /// <remarks>
    /// Nigdy nie zwraca <see langword="null"/>: gdy zapisany tryb zniknął z aplikacji,
    /// wraca tryb domyślny, a wybór zostaje poprawiony w ustawieniach.
    /// </remarks>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task<GameModeDefinition> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Zapisuje wybrany tryb.</summary>
    /// <param name="key">Klucz trybu.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <exception cref="ArgumentException">Gdy tryb o takim kluczu nie istnieje.</exception>
    Task SetActiveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Zwraca tryb o podanym kluczu albo <see langword="null"/>.</summary>
    /// <param name="key">Klucz trybu.</param>
    GameModeDefinition? Find(string key);
}
