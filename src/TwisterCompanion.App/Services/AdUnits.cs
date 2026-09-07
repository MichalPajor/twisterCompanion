namespace TwisterCompanion.App.Services;

/// <summary>
/// Identyfikatory reklam — <b>produkcyjne</b>, z konta AdMob wydawcy aplikacji.
/// </summary>
/// <remarks>
/// Nie są sekretem: trafiają do pakietu i da się je z niego odczytać, więc ukrywanie ich
/// w repozytorium nic by nie dało. Realne ryzyko jest inne — cudzy identyfikator da się
/// wkleić do własnej aplikacji i generować na nim sztuczny ruch, żeby AdMob zawiesił konto.
/// Standardową obroną jest plik <c>app-ads.txt</c> na stronie dewelopera; do zrobienia po
/// publikacji, gdy karta sklepu będzie publiczna.
/// <para>
/// Identyfikator aplikacji jest też w <c>AndroidManifest.xml</c> i musi się z tym plikiem
/// zgadzać — pilnuje tego test <c>IdentyfikatorAplikacjiWAdMob_ZgadzaSieMiedzyManifestemIKodem</c>.
/// Wspólnego wydawcy wszystkich trzech pilnuje <c>IdentyfikatoryReklam_PochodzaZJednegoKonta</c>:
/// wymieszanie kont to aplikacja, która działa, pokazuje reklamy i przekazuje przychód
/// gdzie indziej.
/// </para>
/// <para>
/// Do testów na własnym urządzeniu <b>nie wraca się</b> do identyfikatorów testowych Google —
/// urządzenie rejestruje się w konsoli AdMob jako testowe i wtedy na nim nadal lecą reklamy
/// testowe. Każde wyświetlenie prawdziwej reklamy na telefonie dewelopera to nieprawidłowy
/// ruch, a przebieg wydania odmawia zbudowania pakietu ze znacznika z identyfikatorami
/// testowymi właśnie po to, żeby przez pomyłkę nie wyszła wersja, która nic nie zarabia.
/// </para>
/// </remarks>
internal static class AdUnits
{
    /// <summary>Identyfikator aplikacji w AdMob.</summary>
    public const string ApplicationId = "ca-app-pub-6431615298155605~3559351453";

    /// <summary>Jednostka banera na ekranie rozgrywki.</summary>
    public const string Banner = "ca-app-pub-6431615298155605/9488771883";

    /// <summary>Jednostka reklamy pełnoekranowej po zakończonej partii.</summary>
    public const string Interstitial = "ca-app-pub-6431615298155605/8620106447";
}
