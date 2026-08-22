namespace TwisterCompanion.Presentation;

/// <summary>
/// Adresy zewnętrzne, do których odsyła aplikacja.
/// </summary>
/// <remarks>
/// Jedno miejsce, bo ten sam adres polityki prywatności jest podany w karcie Google Play
/// i musi się z nią zgadzać — rozjechanie się tych dwóch jest naruszeniem zasad sklepu,
/// a nie usterką kosmetyczną.
/// </remarks>
public static class AppLinks
{
    /// <summary>
    /// Polityka prywatności — ta sama, którą wskazuje karta aplikacji w Google Play.
    /// </summary>
    /// <remarks>
    /// Adres prowadzi do wersji angielskiej, która na górze ma odnośnik do polskiej.
    /// Aplikacja mówi w dziesięciu językach, a polityka istnieje w dwóch, więc wybieranie
    /// wersji po języku interfejsu i tak nie trafiłoby dla większości użytkowników.
    /// </remarks>
    public static Uri PrivacyPolicy { get; } = new("https://michalpajor.github.io/twisterCompanion/");
}
