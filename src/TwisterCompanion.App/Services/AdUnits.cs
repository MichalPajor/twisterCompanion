namespace TwisterCompanion.App.Services;

/// <summary>
/// Identyfikatory reklam.
/// </summary>
/// <remarks>
/// Klasa jest podzielona na trzy pliki według tego, <b>co się zmienia między konfiguracjami</b>:
/// <list type="bullet">
/// <item><description>ten plik — identyfikator aplikacji, wspólny dla Debug i Release;</description></item>
/// <item><description><c>AdUnits.Release.cs</c> — jednostki produkcyjne z konta AdMob wydawcy,
/// kompilowane wyłącznie w Release;</description></item>
/// <item><description><c>AdUnits.Debug.cs</c> — przykładowe jednostki testowe Google,
/// kompilowane wyłącznie w Debug.</description></item>
/// </list>
/// Wybór pliku robi projekt (<c>Compile Remove</c> pod warunkiem konfiguracji), więc build Debug
/// <b>zawsze</b> pokazuje reklamy testowe — na każdym emulatorze i każdym telefonie, bez rejestrowania
/// czegokolwiek. Chodzi o bezpieczeństwo konta: wyświetlenie produkcyjnej reklamy na urządzeniu
/// dewelopera to dla AdMob nieprawidłowy ruch, a to najczęstszy powód zawieszenia konta.
/// <para>
/// Identyfikator aplikacji nie przełącza się, bo nie musi: Google dokumentuje, że przykładowe
/// jednostki testowe wolno łączyć z własnym identyfikatorem aplikacji. Dzięki temu manifest jest
/// jeden, a test zgodności manifestu z kodem nie zależy od konfiguracji.
/// </para>
/// <para>
/// Identyfikatory nie są sekretem — trafiają do pakietu i da się je z niego odczytać. Realne
/// ryzyko to podszycie się cudzą aplikacją pod nasze jednostki; obroną jest <c>app-ads.txt</c>
/// na stronie dewelopera, do zrobienia po publikacji. Zgodności manifestu z kodem, wspólnego
/// wydawcy i tego, że w Release nie ma identyfikatorów testowych, pilnują testy
/// w <c>KonfiguracjaWydaniaTests</c>, a przebieg wydania odmawia zbudowania pakietu ze
/// znacznika, gdyby testowe trafiły do plików kompilowanych w Release.
/// </para>
/// </remarks>
internal static partial class AdUnits
{
    /// <summary>Identyfikator aplikacji w AdMob — ten sam w Debug i Release.</summary>
    public const string ApplicationId = "ca-app-pub-6431615298155605~3559351453";
}
