namespace TwisterCompanion.Presentation.Abstractions;

/// <summary>
/// Otwieranie adresów poza aplikacją.
/// </summary>
/// <remarks>
/// Za interfejsem z tego samego powodu co <see cref="IDialogService"/>: ViewModel nie może
/// sięgnąć po <c>Browser.Default</c>, bo to typ z MAUI, a warstwa widoku nie zna platformy.
/// W testach podstawia się atrapę i sprawdza, że ekran poprosił o właściwy adres — bez
/// otwierania czegokolwiek.
/// </remarks>
public interface IExternalBrowser
{
    /// <summary>Otwiera adres w przeglądarce systemowej.</summary>
    /// <param name="url">Adres do otwarcia.</param>
    /// <returns><see langword="true"/>, jeśli udało się przekazać adres systemowi.</returns>
    Task<bool> OpenAsync(Uri url);
}
