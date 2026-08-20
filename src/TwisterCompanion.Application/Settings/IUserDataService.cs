namespace TwisterCompanion.Application.Settings;

/// <summary>
/// Operacje na wszystkich danych, które aplikacja trzyma o użytkowniku.
/// </summary>
/// <remarks>
/// Istnieje po to, żeby „usuń moje dane" było <b>jednym wywołaniem</b>, a nie listą czynności
/// do odtworzenia w ekranie ustawień. Danych jest cztery rodzaje w trzech różnych miejscach
/// (ustawienia, skład graczy, własne paczki wydarzeń, zapis przerwanej partii) i przy każdym
/// kolejnym rodzaju ekran musiałby o nim pamiętać — a zapomniany rodzaj danych to obietnica
/// złamana wobec użytkownika, który właśnie poprosił o wyczyszczenie telefonu.
/// <para>
/// Paczki wbudowane nie są danymi użytkownika i zostają — to zawartość aplikacji, jak teksty
/// interfejsu.
/// </para>
/// </remarks>
public interface IUserDataService
{
    /// <summary>Przywraca ustawienia do wartości domyślnych, nie ruszając pozostałych danych.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task ResetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa wszystkie dane użytkownika: ustawienia, skład graczy, własne paczki wydarzeń
    /// i zapis przerwanej partii.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task EraseAsync(CancellationToken cancellationToken = default);
}
