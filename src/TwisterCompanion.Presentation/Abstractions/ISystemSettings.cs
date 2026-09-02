namespace TwisterCompanion.Presentation.Abstractions;

/// <summary>
/// Otwieranie ekranów ustawień systemu.
/// </summary>
/// <remarks>
/// Za interfejsem z tego samego powodu co <see cref="IDialogService"/>: ViewModel nie zna
/// platformy, a intencje Androida są jej częścią. Osobno od <see cref="IExternalBrowser"/>,
/// bo to nie jest adres do otwarcia — aplikacja prosi system o pokazanie własnego ekranu.
/// </remarks>
public interface ISystemSettings
{
    /// <summary>
    /// Otwiera ustawienia prywatności, gdzie mieszka przełącznik dostępu do mikrofonu.
    /// </summary>
    /// <returns><see langword="true"/>, jeśli system pokazał ekran.</returns>
    /// <remarks>
    /// Prowadzi do sekcji prywatności, a nie wprost do przełącznika — Android nie udostępnia
    /// intencji celującej w sam przełącznik. To i tak o kilka kroków bliżej niż „poszukaj
    /// gdzieś w ustawieniach", a przełącznik jest też w szybkich ustawieniach, o czym mówi
    /// treść komunikatu.
    /// </remarks>
    Task<bool> OpenPrivacySettingsAsync();
}
