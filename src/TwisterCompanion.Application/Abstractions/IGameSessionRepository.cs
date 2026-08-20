using TwisterCompanion.Application.Game;

namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Przechowuje zapis przerwanej partii.
/// </summary>
/// <remarks>
/// W repozytorium leży najwyżej jeden zapis — ten z ostatniej niezakończonej partii.
/// Zakończenie gry go usuwa, żeby przy następnym uruchomieniu aplikacja nie proponowała
/// wznowienia czegoś, co już się skończyło.
/// </remarks>
public interface IGameSessionRepository
{
    /// <summary>Zapisuje stan partii, nadpisując poprzedni zapis.</summary>
    /// <param name="snapshot">Stan do zapisania.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task SaveAsync(GameSessionSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>Odczytuje zapis przerwanej partii.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>Zapisany stan albo <see langword="null"/>, gdy nie ma czego wznawiać.</returns>
    Task<GameSessionSnapshot?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Usuwa zapis.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
