using TwisterCompanion.Domain.GameModes;

namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Katalog trybów gry dołączonych do aplikacji.
/// </summary>
/// <remarks>
/// Tryby są danymi, nie kodem — katalog tylko je wczytuje. Implementacja żyje w warstwie
/// infrastruktury, bo definicje leżą w pliku osadzonym w bibliotece.
/// </remarks>
public interface IGameModeCatalog
{
    /// <summary>Zwraca tryby dostępne do wyboru.</summary>
    /// <remarks>Tryby wyłączone są pomijane.</remarks>
    IReadOnlyList<GameModeDefinition> GetAvailable();

    /// <summary>Zwraca tryb o podanym kluczu albo <see langword="null"/>.</summary>
    /// <param name="key">Klucz trybu.</param>
    /// <remarks>
    /// Szuka także wśród trybów wyłączonych: zapisany w ustawieniach tryb, który został
    /// wyłączony w nowej wersji aplikacji, ma zostać rozpoznany, a nie zniknąć bez śladu.
    /// </remarks>
    GameModeDefinition? Find(string key);

    /// <summary>Tryb używany, gdy żaden nie został wybrany albo wybrany nie istnieje.</summary>
    GameModeDefinition Default { get; }
}
