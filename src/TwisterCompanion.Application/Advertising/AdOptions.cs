namespace TwisterCompanion.Application.Advertising;

/// <summary>
/// Parametry reklam.
/// </summary>
/// <remarks>
/// Wartości wynikają z ustaleń z użytkownikiem, nie z możliwości zestawu SDK: reklama
/// pełnoekranowa wyłącznie po zakończonej partii i nie częściej niż co trzecią.
/// </remarks>
public sealed record AdOptions
{
    /// <summary>Wartości domyślne.</summary>
    public static AdOptions Default { get; } = new();

    /// <summary>
    /// Co która zakończona partia kończy się reklamą pełnoekranową.
    /// </summary>
    /// <remarks>
    /// Trzy, nie jedna: reklama po każdej partii zniechęca do kolejnej, a ta gra jest
    /// z założenia rozgrywana seriami. Licznik przeżywa restart aplikacji, bo inaczej
    /// wystarczyłoby ją zamknąć, żeby zaczynać odliczanie od zera.
    /// </remarks>
    public int InterstitialEveryNthGame { get; init; } = 3;

    /// <summary>
    /// Ile najwyżej czekamy na koniec zapowiedzi głosowej, zanim pokażemy reklamę.
    /// </summary>
    /// <remarks>
    /// Reklama nie może wejść na odczyt komunikatu o zakończeniu partii — ale nie może też
    /// czekać w nieskończoność, gdyby zapowiedź nigdy się nie zakończyła (awaria syntezatora,
    /// wyciszone urządzenie). Po tym czasie po prostu rezygnujemy z reklamy.
    /// </remarks>
    public TimeSpan SpeechWaitTimeout { get; init; } = TimeSpan.FromSeconds(15);
}
