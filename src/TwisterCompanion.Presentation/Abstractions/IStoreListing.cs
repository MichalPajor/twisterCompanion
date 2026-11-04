namespace TwisterCompanion.Presentation.Abstractions;

/// <summary>
/// Karta tej aplikacji w sklepie.
/// </summary>
/// <remarks>
/// Osobny port, a nie adres dla <see cref="IExternalBrowser"/>: karta ma się otworzyć
/// w aplikacji sklepu, gdzie od razu można wystawić ocenę, a nie na stronie WWW sklepu
/// w przeglądarce. Jak to zrobić, wie tylko platforma — i ona zna identyfikator pakietu,
/// więc nie trzeba go powtarzać w kodzie.
/// </remarks>
public interface IStoreListing
{
    /// <summary>Otwiera kartę aplikacji w sklepie.</summary>
    /// <returns><see langword="true"/>, jeśli udało się ją otworzyć.</returns>
    Task<bool> OpenAsync();
}
