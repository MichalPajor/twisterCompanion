using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Tests.Shared;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy spójności plików zasobów.
/// </summary>
/// <remarks>
/// Realizacja kryterium ukończenia Etapu 2: „test sprawdzający, że każdy klucz z pliku
/// neutralnego ma odpowiednik w polskim". Rozszerzony o dwie rzeczy, które w praktyce
/// psują się najczęściej: puste tłumaczenia i utracone wzorce podstawień.
/// </remarks>
public class LocalizationResourceTests
{
    private const string ResourceNamespace = "TwisterCompanion.Application.Resources.Strings.";

    private static readonly Assembly ResourceAssembly = typeof(ILocalizationService).Assembly;

    public static TheoryData<string> Catalogs => new("AppResources", "VoiceResources");

    /// <summary>
    /// Każdy katalog razy każdy przetłumaczony język.
    /// </summary>
    /// <remarks>
    /// Języki są <b>odkrywane z plików</b>, nie wypisane tutaj: dołożenie kolejnego wchodzi
    /// wtedy pod wszystkie testy spójności samo. Wcześniej testy znały tylko polski, więc
    /// hiszpański przeszedłby bez sprawdzenia parzystości kluczy i wzorców podstawień.
    /// </remarks>
    public static TheoryData<string, string> CatalogsAndCultures
    {
        get
        {
            TheoryData<string, string> dane = new();

            foreach (string catalog in new[] { "AppResources", "VoiceResources" })
            {
                foreach (string culture in KatalogRepozytorium.PrzetlumaczoneJezyki())
                {
                    dane.Add(catalog, culture);
                }
            }

            return dane;
        }
    }

    /// <summary>Wszystkie języki aplikacji — angielski z pliku neutralnego i tłumaczenia.</summary>
    public static TheoryData<string> WszystkieJezyki
    {
        get
        {
            TheoryData<string> dane = new("en");

            foreach (string culture in KatalogRepozytorium.PrzetlumaczoneJezyki())
            {
                dane.Add(culture);
            }

            return dane;
        }
    }

    [Theory]
    [MemberData(nameof(CatalogsAndCultures))]
    public void KazdyKluczNeutralny_MaOdpowiednikWKazdymJezyku(string catalog, string culture)
    {
        IReadOnlySet<string> neutralne = ReadKeys(catalog, CultureInfo.InvariantCulture);
        IReadOnlySet<string> przetlumaczone = ReadKeys(catalog, CultureInfo.GetCultureInfo(culture));

        string[] brakujace = [.. neutralne.Except(przetlumaczone).Order()];

        Assert.Empty(brakujace);
    }

    [Theory]
    [MemberData(nameof(CatalogsAndCultures))]
    public void KazdyKluczPrzetlumaczony_MaOdpowiednikNeutralny(string catalog, string culture)
    {
        // Klucz istniejący tylko w jednym języku to zwykle literówka albo pozostałość po
        // zmianie nazwy. Bez tego testu nikt by go nie znalazł, bo aplikacja w tym języku
        // działałaby poprawnie.
        IReadOnlySet<string> neutralne = ReadKeys(catalog, CultureInfo.InvariantCulture);
        IReadOnlySet<string> przetlumaczone = ReadKeys(catalog, CultureInfo.GetCultureInfo(culture));

        string[] osierocone = [.. przetlumaczone.Except(neutralne).Order()];

        Assert.Empty(osierocone);
    }

    [Theory]
    [MemberData(nameof(CatalogsAndCultures))]
    public void ZadneTlumaczenieNieJestPuste(string catalog, string culture)
    {
        Dictionary<string, string> neutralne = ReadEntries(catalog, CultureInfo.InvariantCulture);
        Dictionary<string, string> przetlumaczone =
            ReadEntries(catalog, CultureInfo.GetCultureInfo(culture));

        string[] puste =
        [
            .. neutralne.Where(entry => string.IsNullOrWhiteSpace(entry.Value)).Select(entry => $"en:{entry.Key}"),
            .. przetlumaczone.Where(entry => string.IsNullOrWhiteSpace(entry.Value))
                .Select(entry => $"{culture}:{entry.Key}"),
        ];

        Assert.Empty(puste);
    }

    [Theory]
    [MemberData(nameof(CatalogsAndCultures))]
    public void WzorcePodstawien_ZgadzajaSieMiedzyJezykami(string catalog, string culture)
    {
        // Tłumacz, który zgubi {0} w komunikacie „Następne wydarzenie: {0}.", nie wywali
        // aplikacji — po prostu nazwa wydarzenia przestanie być czytana. Taki błąd bardzo
        // trudno zauważyć w działającej aplikacji, więc pilnuje go test.
        Dictionary<string, string> neutralne = ReadEntries(catalog, CultureInfo.InvariantCulture);
        Dictionary<string, string> przetlumaczone =
            ReadEntries(catalog, CultureInfo.GetCultureInfo(culture));

        List<string> rozbieznosci = [];

        foreach ((string key, string neutralValue) in neutralne)
        {
            if (!przetlumaczone.TryGetValue(key, out string? translatedValue))
            {
                continue;
            }

            IReadOnlySet<string> oczekiwane = ExtractPlaceholders(neutralValue);
            IReadOnlySet<string> otrzymane = ExtractPlaceholders(translatedValue);

            if (!oczekiwane.SetEquals(otrzymane))
            {
                rozbieznosci.Add(
                    $"{key}: en={string.Join(",", oczekiwane.Order())}"
                    + $" {culture}={string.Join(",", otrzymane.Order())}");
            }
        }

        Assert.Empty(rozbieznosci);
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public void KluczeTrzymajaSieKonwencjiNazewniczej(string catalog)
    {
        // Konwencja: Ekran_Element_Przeznaczenie — co najmniej dwa segmenty w PascalCase.
        Regex konwencja = new(@"^[A-Z][A-Za-z0-9]*(_[A-Z][A-Za-z0-9]*)+$", RegexOptions.None);

        string[] niezgodne =
        [
            .. ReadKeys(catalog, CultureInfo.InvariantCulture)
                .Where(key => !konwencja.IsMatch(key))
                .Order(),
        ];

        Assert.Empty(niezgodne);
    }

    [Fact]
    public void KatalogGlosowy_ZawieraWszystkieCzesciCialaIKolory()
    {
        // Komunikat o ruchu składa się z tych właśnie fragmentów. Brak choćby jednego
        // oznaczałby wypowiedź z nawiasem kwadratowym w środku.
        IReadOnlySet<string> klucze = ReadKeys("VoiceResources", CultureInfo.InvariantCulture);

        string[] wymagane =
        [
            "Voice_BodyPart_RightHand",
            "Voice_BodyPart_LeftHand",
            "Voice_BodyPart_RightFoot",
            "Voice_BodyPart_LeftFoot",
            "Voice_Color_Red",
            "Voice_Color_Yellow",
            "Voice_Color_Blue",
            "Voice_Color_Green",
            "Voice_Announce_Move",
        ];

        Assert.All(wymagane, key => Assert.Contains(key, klucze));
    }

    [Theory]
    [MemberData(nameof(WszystkieJezyki))]
    public void FrazyKomend_NieMieszajaSieMiedzyKomendami(string culture)
    {
        // Parser szuka frazy wewnątrz rozpoznanego zdania i bierze pierwsze trafienie.
        // Jeśli fraza jednej komendy zawiera się w słowach frazy innej, ta druga nigdy nie
        // wygra — po rosyjsku „дальше" jako „dalej" i „поехали дальше" jako „wznów" dałyby
        // wznawianie, które zawsze przechodzi w następną turę. Takiej kolizji nie widać
        // w pliku zasobów, widać ją dopiero na macie.
        Dictionary<string, string[][]> frazy = [];

        foreach ((string key, string value) in ReadEntries("VoiceResources", KulturaZKodu(culture)))
        {
            if (!key.StartsWith("Voice_Command_", StringComparison.Ordinal))
            {
                continue;
            }

            frazy[key] =
            [
                .. value
                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(fraza => fraza.ToLowerInvariant().Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
            ];
        }

        Assert.NotEmpty(frazy);
        Assert.All(frazy, wpis => Assert.NotEmpty(wpis.Value));

        List<string> kolizje = [];

        foreach ((string pierwszaKomenda, string[][] pierwszeFrazy) in frazy)
        {
            foreach ((string drugaKomenda, string[][] drugieFrazy) in frazy)
            {
                if (string.Equals(pierwszaKomenda, drugaKomenda, StringComparison.Ordinal))
                {
                    continue;
                }

                kolizje.AddRange(
                    from pierwsza in pierwszeFrazy
                    from druga in drugieFrazy
                    where ZawieraCiag(druga, pierwsza)
                    select $"{pierwszaKomenda} [{string.Join(' ', pierwsza)}] "
                           + $"w {drugaKomenda} [{string.Join(' ', druga)}]");
            }
        }

        Assert.Empty(kolizje);
    }

    /// <summary>Czy ciąg słów <paramref name="szukany"/> występuje w <paramref name="calosc"/>.</summary>
    private static bool ZawieraCiag(string[] calosc, string[] szukany)
    {
        for (int start = 0; start + szukany.Length <= calosc.Length; start++)
        {
            if (calosc.Skip(start).Take(szukany.Length).SequenceEqual(szukany, StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Zamienia kod języka na kulturę do czytania zasobów.
    /// </summary>
    /// <remarks>
    /// Angielski siedzi w pliku neutralnym, więc nie ma zestawu satelickiego — z kulturą „en"
    /// i <c>tryParents: false</c> wyszłoby zero wpisów.
    /// </remarks>
    private static CultureInfo KulturaZKodu(string culture) =>
        culture == "en" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture);

    private static IReadOnlySet<string> ReadKeys(string catalog, CultureInfo culture) =>
        ReadEntries(catalog, culture).Keys.ToHashSet(StringComparer.Ordinal);

    private static Dictionary<string, string> ReadEntries(string catalog, CultureInfo culture)
    {
        ResourceManager manager = new(ResourceNamespace + catalog, ResourceAssembly);

        // tryParents: false — dla polskiego chcemy wyłącznie wpisy z pliku polskiego,
        // bez cichego dziedziczenia z pliku neutralnego. Inaczej test parzystości
        // zawsze by przechodził.
        using ResourceSet? set = manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);

        Dictionary<string, string> entries = new(StringComparer.Ordinal);

        if (set is null)
        {
            return entries;
        }

        foreach (DictionaryEntry entry in set)
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                entries[key] = value;
            }
        }

        return entries;
    }

    private static IReadOnlySet<string> ExtractPlaceholders(string value) =>
        Regex.Matches(value, @"\{\d+\}")
            .Select(match => match.Value)
            .ToHashSet(StringComparer.Ordinal);
}
