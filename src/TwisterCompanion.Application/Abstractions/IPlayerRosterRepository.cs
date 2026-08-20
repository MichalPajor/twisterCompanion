using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Zapamiętuje listę graczy między uruchomieniami aplikacji.
/// </summary>
/// <remarks>
/// Przechowywana jest sama lista uczestników, bez stanu rozgrywki — kolejna gra zaczyna
/// się z pełnym składem, bez informacji o tym, kto poprzednio odpadł.
/// </remarks>
public interface IPlayerRosterRepository
{
    /// <summary>Zwraca zapamiętaną listę graczy. Pusta lista oznacza brak zapisu.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task<IReadOnlyList<Player>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Zapisuje listę graczy.</summary>
    /// <param name="players">Gracze do zapamiętania.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task SaveAsync(IReadOnlyList<Player> players, CancellationToken cancellationToken = default);

    /// <summary>Czyści zapamiętaną listę graczy.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
