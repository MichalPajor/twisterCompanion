namespace TwisterCompanion.App.Services;

/// <summary>
/// Identyfikatory reklam.
/// </summary>
/// <remarks>
/// Wpisane są <b>testowe</b> identyfikatory Google — te same, które podaje dokumentacja
/// AdMob. Zawsze zwracają reklamę testową i nigdy prawdziwą, więc nie da się nimi naruszyć
/// zasad ani zarobić. Do sprawdzenia całej integracji na urządzeniu nie trzeba przy nich
/// żadnego konta.
/// <para>
/// Prawdziwe identyfikatory wchodzą tu przy publikacji (Etap 16), razem z założeniem konta
/// AdMob. Nie są sekretem — trafiają do pakietu i da się je z niego odczytać — ale zamiana
/// musi być <b>jedną zmianą w jednym miejscu</b>, bo pomyłka oznacza reklamy liczone na cudze
/// konto albo aplikację bez reklam.
/// </para>
/// <para>
/// Identyfikator aplikacji jest też w <c>AndroidManifest.xml</c> i musi się z tym plikiem
/// zgadzać. Jego brak w manifeście nie daje ostrzeżenia — aplikacja wywala się przy starcie.
/// </para>
/// </remarks>
internal static class AdUnits
{
    /// <summary>Identyfikator aplikacji w AdMob (wersja testowa Google).</summary>
    public const string ApplicationId = "ca-app-pub-3940256099942544~3347511713";

    /// <summary>Jednostka banera (wersja testowa Google).</summary>
    public const string Banner = "ca-app-pub-3940256099942544/6300978111";

    /// <summary>Jednostka reklamy pełnoekranowej (wersja testowa Google).</summary>
    public const string Interstitial = "ca-app-pub-3940256099942544/1033173712";
}
