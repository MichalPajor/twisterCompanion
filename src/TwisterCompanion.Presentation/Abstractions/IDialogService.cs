namespace TwisterCompanion.Presentation.Abstractions;

/// <summary>
/// Komunikaty i pytania kierowane do użytkownika.
/// </summary>
/// <remarks>
/// Za interfejsem, bo ViewModel nie może odwoływać się do <c>Page.DisplayAlert</c>.
/// W testach podstawia się atrapę i sprawdza, czy ekran faktycznie o coś zapytał
/// albo coś zgłosił — bez uruchamiania UI.
/// </remarks>
public interface IDialogService
{
    /// <summary>Pokazuje komunikat z jednym przyciskiem zamykającym.</summary>
    /// <param name="title">Tytuł okna.</param>
    /// <param name="message">Treść komunikatu.</param>
    /// <param name="cancel">Etykieta przycisku zamykającego.</param>
    Task AlertAsync(string title, string message, string cancel = "OK");

    /// <summary>Zadaje pytanie zamknięte.</summary>
    /// <param name="title">Tytuł okna.</param>
    /// <param name="message">Treść pytania.</param>
    /// <param name="accept">Etykieta przycisku potwierdzenia.</param>
    /// <param name="cancel">Etykieta przycisku odrzucenia.</param>
    /// <returns><see langword="true"/>, jeśli użytkownik potwierdził.</returns>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);

    /// <summary>Prosi użytkownika o wpisanie tekstu.</summary>
    /// <param name="title">Tytuł okna.</param>
    /// <param name="message">Treść prośby.</param>
    /// <param name="accept">Etykieta przycisku potwierdzenia.</param>
    /// <param name="cancel">Etykieta przycisku anulowania.</param>
    /// <param name="placeholder">Podpowiedź w pustym polu.</param>
    /// <param name="initialValue">Wartość początkowa pola.</param>
    /// <returns>Wpisany tekst albo <see langword="null"/>, jeśli użytkownik anulował.</returns>
    Task<string?> PromptAsync(
        string title,
        string message,
        string accept,
        string cancel,
        string? placeholder = null,
        string? initialValue = null);
}
