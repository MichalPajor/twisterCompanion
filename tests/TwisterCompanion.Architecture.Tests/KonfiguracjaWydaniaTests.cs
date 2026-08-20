using System.Text.RegularExpressions;
using System.Xml.Linq;
using TwisterCompanion.Tests.Shared;

namespace TwisterCompanion.Architecture.Tests;

/// <summary>
/// Testy spójności konfiguracji wydania — wersji, nazwy paczki i identyfikatorów reklam.
/// </summary>
/// <remarks>
/// Zadania 1 i 2 Etapu 16. Wszystkie te wartości mają wspólną cechę: rozjeżdżają się cicho,
/// a skutek widać dopiero po wgraniu pakietu do sklepu — czyli wtedy, kiedy naprawa jest
/// najdroższa albo niemożliwa. Nazwy paczki nie da się zmienić po pierwszym wgraniu,
/// notatki wydania Play po prostu ucina, a pomylony identyfikator reklamy liczy wyświetlenia
/// na cudze konto.
/// <para>
/// Testy czytają <b>pliki źródłowe</b>, nie skompilowane zestawy: część tych wartości nie
/// istnieje w żadnym zestawie (manifest Androida, plik historii zmian), a projekt aplikacji
/// celuje w Androida, więc nie da się go tu zreferować.
/// </para>
/// </remarks>
public class KonfiguracjaWydaniaTests
{
    /// <summary>Numer wydawcy w testowych identyfikatorach reklam Google.</summary>
    private const string WydawcaTestowy = "3940256099942544";

    /// <summary>Limit długości notatek wydania w Google Play, na język.</summary>
    private const int LimitNotatekPlay = 500;

    [Fact]
    public void WersjaWPliku_ZgadzaSieZNajnowszymWpisemHistorii()
    {
        // Dwa miejsca, jedna prawda. Rozjechanie się ich znaczy albo wydanie z numerem,
        // którego nie ma w historii, albo wpis w historii, który nigdy nie wyszedł.
        string wersja = WersjaProjektu();
        string zHistorii = NajnowszaWersjaHistorii();

        Assert.Equal(zHistorii, wersja);
    }

    [Fact]
    public void WersjaWPliku_JestPoprawnymSemVerem()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+$", WersjaProjektu());
    }

    [Fact]
    public void NotatkiWydania_MieszczaSieWLimicieGooglePlay()
    {
        // Play nie ostrzega przed przekroczeniem limitu — ucina tekst w połowie zdania.
        string historia = File.ReadAllText(KatalogRepozytorium.Plik("CHANGELOG.md"));

        MatchCollection bloki = Regex.Matches(
            historia,
            @"### (?<tytul>(?:Co nowego|What's new)[^\n]*)\n\n```\n(?<tresc>.*?)\n```",
            RegexOptions.Singleline);

        Assert.NotEmpty(bloki);

        string[] zaDlugie =
        [
            .. bloki
                .Where(blok => blok.Groups["tresc"].Value.Length > LimitNotatekPlay)
                .Select(blok =>
                    $"{blok.Groups["tytul"].Value}: {blok.Groups["tresc"].Value.Length}/{LimitNotatekPlay}"),
        ];

        Assert.Empty(zaDlugie);
    }

    [Fact]
    public void NazwaPaczki_JestPoprawnymIdentyfikatoremAndroida()
    {
        // Po pierwszym wgraniu pakietu nazwa paczki jest nieodwracalna. Literówki w niej
        // nie wyłapie ani kompilator, ani sklep — wyłapie ją użytkownik, na zawsze.
        string nazwa = NazwaPaczki();

        Assert.Matches(@"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$", nazwa);
    }

    [Fact]
    public void NazwaPaczki_NieZawieraCudzegoZnakuTowarowego()
    {
        // Tytuł w sklepie używa znaku „Twister" i to jest świadome ryzyko — tytuł da się
        // zmienić. Nazwy paczki nie, więc ona musi zostać czysta: to jedyna droga wyjścia,
        // jeśli kiedyś przyjdzie zgłoszenie od właściciela znaku.
        Assert.DoesNotContain("twister", NazwaPaczki(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IdentyfikatorAplikacjiWAdMob_ZgadzaSieMiedzyManifestemIKodem()
    {
        // Ta sama wartość musi być w dwóch plikach: zestaw SDK reklam czyta ją z manifestu
        // w swoim inicjalizatorze, a jej brak albo pomyłka nie daje ostrzeżenia przy
        // budowaniu — wywala aplikację przy starcie. Wcześniej pilnował tego komentarz.
        Assert.Equal(IdentyfikatorZManifestu(), IdentyfikatoryZKodu()["ApplicationId"]);
    }

    [Fact]
    public void IdentyfikatoryReklam_PochodzaZJednegoKonta()
    {
        // Wymieszanie kont to najbardziej podstępny błąd przy zamianie identyfikatorów
        // testowych na prawdziwe: aplikacja działa, reklamy się pokazują, a przychód idzie
        // gdzie indziej. Numer wydawcy jest w każdym identyfikatorze i musi być ten sam.
        Dictionary<string, string> zKodu = IdentyfikatoryZKodu();

        string[] wydawcy =
        [
            .. zKodu.Values.Append(IdentyfikatorZManifestu()).Select(Wydawca).Distinct(),
        ];

        Assert.Single(wydawcy);
    }

    [Fact]
    public void IdentyfikatoryReklam_SaJeszczeTestowe()
    {
        // Test-świadek, nie test-strażnik. Dopóki nie ma konta AdMob, w kodzie są testowe
        // identyfikatory Google i to jest w porządku. W dniu, w którym wejdą prawdziwe,
        // ten test się wywali — i to jest jego sens: przypomni o dwóch rzeczach, które
        // trzeba wtedy zrobić razem, czyli o zdjęciu blokady w przebiegu wydania i o wpisie
        // w historii zmian.
        Assert.Equal(WydawcaTestowy, Wydawca(IdentyfikatorZManifestu()));
    }

    private static string WersjaProjektu()
    {
        XDocument wlasciwosci = XDocument.Load(KatalogRepozytorium.Plik("Directory.Build.props"));

        string? wersja = wlasciwosci.Descendants("VersionPrefix").FirstOrDefault()?.Value;

        Assert.False(string.IsNullOrWhiteSpace(wersja), "Brak VersionPrefix w Directory.Build.props.");

        return wersja!.Trim();
    }

    private static string NajnowszaWersjaHistorii()
    {
        string historia = File.ReadAllText(KatalogRepozytorium.Plik("CHANGELOG.md"));

        Match wpis = Regex.Match(historia, @"^## \[(?<wersja>\d+\.\d+\.\d+)\]", RegexOptions.Multiline);

        Assert.True(wpis.Success, "Brak wpisu wersji w CHANGELOG.md.");

        return wpis.Groups["wersja"].Value;
    }

    private static string NazwaPaczki()
    {
        XDocument projekt = XDocument.Load(
            KatalogRepozytorium.Plik("src", "TwisterCompanion.App", "TwisterCompanion.App.csproj"));

        string? nazwa = projekt.Descendants("ApplicationId").FirstOrDefault()?.Value;

        Assert.False(string.IsNullOrWhiteSpace(nazwa), "Brak ApplicationId w projekcie aplikacji.");

        return nazwa!.Trim();
    }

    private static string IdentyfikatorZManifestu()
    {
        XDocument manifest = XDocument.Load(KatalogRepozytorium.Plik(
            "src", "TwisterCompanion.App", "Platforms", "Android", "AndroidManifest.xml"));

        XNamespace android = "http://schemas.android.com/apk/res/android";

        string? wartosc = manifest.Descendants("meta-data")
            .Where(element =>
                element.Attribute(android + "name")?.Value == "com.google.android.gms.ads.APPLICATION_ID")
            .Select(element => element.Attribute(android + "value")?.Value)
            .FirstOrDefault();

        Assert.False(string.IsNullOrWhiteSpace(wartosc), "Brak identyfikatora AdMob w manifeście.");

        return wartosc!;
    }

    private static Dictionary<string, string> IdentyfikatoryZKodu()
    {
        string kod = File.ReadAllText(
            KatalogRepozytorium.Plik("src", "TwisterCompanion.App", "Services", "AdUnits.cs"));

        Dictionary<string, string> znalezione = Regex
            .Matches(kod, @"public const string (?<nazwa>\w+) = ""(?<wartosc>ca-app-pub-[^""]+)"";")
            .ToDictionary(wpis => wpis.Groups["nazwa"].Value, wpis => wpis.Groups["wartosc"].Value);

        Assert.Equal(3, znalezione.Count);

        return znalezione;
    }

    /// <summary>Wyciąga numer wydawcy z identyfikatora <c>ca-app-pub-NUMER~JEDNOSTKA</c>.</summary>
    private static string Wydawca(string identyfikator)
    {
        Match numer = Regex.Match(identyfikator, @"^ca-app-pub-(?<wydawca>\d+)");

        Assert.True(numer.Success, $"Niepoprawny identyfikator reklamy: {identyfikator}");

        return numer.Groups["wydawca"].Value;
    }
}
