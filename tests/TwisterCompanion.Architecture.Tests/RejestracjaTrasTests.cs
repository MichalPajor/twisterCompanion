using System.Text.RegularExpressions;
using TwisterCompanion.Tests.Shared;

namespace TwisterCompanion.Architecture.Tests;

/// <summary>
/// Strażnik rejestracji tras nawigacji raz na proces.
/// </summary>
/// <remarks>
/// Rejestr tras MAUI jest statyczny, a powłoka powstaje na każde okno. Rejestracja
/// w konstruktorze powłoki rzucała „Duplicated Route", gdy Android tworzył drugie okno
/// w żyjącym procesie — zgłoszenie z Google Play dla 1.0.3. Projekt aplikacji nie ma testów
/// jednostkowych (wymaga urządzenia), więc reguła jest pilnowana na plikach źródłowych.
/// </remarks>
public class RejestracjaTrasTests
{
    private static readonly string KatalogAplikacji = KatalogRepozytorium.Plik("src", "TwisterCompanion.App");

    [Fact]
    public void KonstruktorPowloki_NieRejestrujeTras()
    {
        string powloka = File.ReadAllText(Path.Combine(KatalogAplikacji, "AppShell.xaml.cs"));

        Match konstruktor = Regex.Match(powloka, @"public AppShell\([^)]*\)\s*\{(?<cialo>.*?)\n    \}", RegexOptions.Singleline);

        Assert.True(konstruktor.Success, "Nie znalazłem konstruktora AppShell.");
        Assert.DoesNotContain("RegisterRoute", konstruktor.Groups["cialo"].Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Trasy_SaRejestrowaneRazPrzyStarcieAplikacji()
    {
        string program = File.ReadAllText(Path.Combine(KatalogAplikacji, "MauiProgram.cs"));

        Assert.Single(Regex.Matches(program, @"AppShell\.RegisterRoutes\("));
    }
}
