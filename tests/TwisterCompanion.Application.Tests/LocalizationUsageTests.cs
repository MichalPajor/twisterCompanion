using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy użycia kluczy tłumaczeń: żaden klucz w plikach zasobów nie jest martwy i żadna
/// stała <c>StringKeys</c> nie wisi bez odbiorcy.
/// </summary>
/// <remarks>
/// Druga połowa kryterium Etapu 14 zadania 3. Pierwsza — „każdy klucz w każdym języku" —
/// jest w <see cref="LocalizationResourceTests"/> i pilnuje braków. Ten plik pilnuje
/// nadmiaru, a nadmiar w katalogu tłumaczeń kosztuje realnie: każdy nieużywany klucz to
/// tekst, który tłumacz musi przełożyć na oba języki, a którego nikt nigdy nie zobaczy.
/// <para>
/// Testy czytają pliki źródłowe z dysku, bo użycia są rozsiane po kodzie i po ekranach XAML,
/// a ekrany nie mają własnego projektu testowego. Katalog repozytorium bierzemy
/// z <see cref="CallerFilePathAttribute"/> — tak samo jak testy słowników zasobów XAML.
/// </para>
/// <para>
/// Klucz uznajemy za używany na cztery sposoby, bo tyle jest w tym projekcie legalnych dróg
/// dojścia do tłumaczenia: wprost w ekranie (<c>{loc:Translate Klucz}</c>), wprost w kodzie
/// (napis w cudzysłowie), przez stałą <c>StringKeys</c>, albo przez stałą-przedrostek —
/// nazwy części ciała i kolorów są składane w czasie działania, więc pełny klucz nie
/// występuje w kodzie nigdzie.
/// </para>
/// </remarks>
public class LocalizationUsageTests
{
    private const string ResourceNamespace = "TwisterCompanion.Application.Resources.Strings.";

    private static readonly Assembly ResourceAssembly = typeof(ILocalizationService).Assembly;

    /// <summary>Deklaracja stałej: <c>public const string ButtonOk = "Common_Button_Ok";</c></summary>
    private static readonly Regex ConstantDeclaration = new(
        @"public\s+const\s+string\s+([A-Za-z0-9_]+)\s*=\s*""([^""]+)""",
        RegexOptions.None);

    /// <summary>Otwarcie klasy zagnieżdżonej, czyli grupy kluczy.</summary>
    private static readonly Regex NestedClass = new(
        @"public\s+static\s+class\s+([A-Za-z0-9_]+)",
        RegexOptions.None);

    private static readonly Regex TranslateUse = new(
        @"\{loc:Translate\s+([A-Za-z0-9_]+)\}",
        RegexOptions.None);

    [Fact]
    public void KazdyKluczZasobow_JestGdziesUzywany()
    {
        Uzycia uzycia = Uzycia.Zbierz();
        List<string> martwe = [];

        foreach (string catalog in new[] { "AppResources", "VoiceResources" })
        {
            foreach (string klucz in CollectKeys(catalog))
            {
                if (!uzycia.JestUzywany(klucz))
                {
                    martwe.Add($"{catalog}: {klucz}");
                }
            }
        }

        Assert.True(
            martwe.Count == 0,
            "Klucze tłumaczeń bez ani jednego użycia — usuń je z obu plików zasobów albo "
                + "zacznij ich używać:\n" + string.Join("\n", martwe));
    }

    [Fact]
    public void KazdaStalaStringKeys_JestUzywanaPozaWlasnymPlikiem()
    {
        Uzycia uzycia = Uzycia.Zbierz();

        List<string> nieuzywane =
        [
            .. uzycia.Stale
                .Where(stala => !uzycia.StalaJestUzywana(stala.Key))
                .Select(stala => $"{stala.Key} (= \"{stala.Value}\")"),
        ];

        Assert.True(
            nieuzywane.Count == 0,
            "Stałe StringKeys, po które nikt nie sięga:\n" + string.Join("\n", nieuzywane));
    }

    private static IEnumerable<string> CollectKeys(string catalog)
    {
        ResourceManager resources = new(ResourceNamespace + catalog, ResourceAssembly);
        using ResourceSet? set = resources.GetResourceSet(CultureInfo.InvariantCulture, true, false);

        Assert.NotNull(set);

        foreach (DictionaryEntry entry in set)
        {
            yield return (string)entry.Key;
        }
    }

    /// <summary>
    /// Zebrane z repozytorium: treść plików źródłowych, klucze użyte w ekranach i stałe
    /// <c>StringKeys</c> wraz z ich wartościami.
    /// </summary>
    private sealed class Uzycia
    {
        private readonly HashSet<string> _kluczeWEkranach = [];
        private readonly List<string> _kodPozaStringKeys = [];
        private readonly List<string> _daneWbudowane = [];

        private Uzycia()
        {
        }

        /// <summary>Nazwa stałej z grupą (np. <c>Common.ButtonOk</c>) → wartość klucza.</summary>
        public Dictionary<string, string> Stale { get; } = [];

        public static Uzycia Zbierz([CallerFilePath] string thisFile = "")
        {
            string repozytorium = ZnajdzKatalogRepozytorium(thisFile);
            string kod = Path.Combine(repozytorium, "src");
            Uzycia uzycia = new();

            foreach (string plik in Directory.EnumerateFiles(kod, "*.xaml", SearchOption.AllDirectories))
            {
                foreach (Match dopasowanie in TranslateUse.Matches(File.ReadAllText(plik)))
                {
                    uzycia._kluczeWEkranach.Add(dopasowanie.Groups[1].Value);
                }
            }

            // Wbudowane paczki wydarzeń i tryby gry są plikami JSON i podają klucze tłumaczeń
            // zamiast gotowych tekstów — inaczej nie dałoby się ich pokazać w dwóch językach.
            // Bez tego przeglądu połowa katalogu wyglądałaby na martwą.
            foreach (string plik in Directory.EnumerateFiles(kod, "*.json", SearchOption.AllDirectories))
            {
                if (plik.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                uzycia._daneWbudowane.Add(File.ReadAllText(plik));
            }

            foreach (string plik in Directory.EnumerateFiles(kod, "*.cs", SearchOption.AllDirectories))
            {
                // Katalogi wyjściowe buildu bywają wewnątrz projektu na maszynach bez
                // przekierowania artefaktów — kod generowany nie jest użyciem.
                if (plik.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                string tresc = File.ReadAllText(plik);

                if (Path.GetFileName(plik) == "StringKeys.cs")
                {
                    uzycia.WczytajStale(tresc);
                    continue;
                }

                uzycia._kodPozaStringKeys.Add(tresc);
            }

            Assert.NotEmpty(uzycia.Stale);
            Assert.NotEmpty(uzycia._kodPozaStringKeys);
            Assert.NotEmpty(uzycia._kluczeWEkranach);
            Assert.NotEmpty(uzycia._daneWbudowane);

            return uzycia;
        }

        public bool JestUzywany(string klucz)
        {
            if (_kluczeWEkranach.Contains(klucz))
            {
                return true;
            }

            if (_kodPozaStringKeys.Any(tresc => tresc.Contains($"\"{klucz}\"", StringComparison.Ordinal)))
            {
                return true;
            }

            if (_daneWbudowane.Any(tresc => tresc.Contains($"\"{klucz}\"", StringComparison.Ordinal)))
            {
                return true;
            }

            foreach ((string nazwa, string wartosc) in Stale)
            {
                if (wartosc == klucz && StalaJestUzywana(nazwa))
                {
                    return true;
                }

                // Stała-przedrostek: klucze rodziny są składane w czasie działania, więc
                // pełny klucz nie występuje w kodzie. Wystarczy, że przedrostek jest w użyciu.
                if (wartosc.EndsWith('_')
                    && klucz.StartsWith(wartosc, StringComparison.Ordinal)
                    && StalaJestUzywana(nazwa))
                {
                    return true;
                }
            }

            return false;
        }

        public bool StalaJestUzywana(string nazwaZGrupa) =>
            _kodPozaStringKeys.Any(tresc => tresc.Contains(nazwaZGrupa, StringComparison.Ordinal));

        private void WczytajStale(string tresc)
        {
            string grupa = string.Empty;

            foreach (string linia in tresc.Split('\n'))
            {
                Match klasa = NestedClass.Match(linia);

                if (klasa.Success)
                {
                    grupa = klasa.Groups[1].Value;
                    continue;
                }

                Match stala = ConstantDeclaration.Match(linia);

                if (stala.Success && grupa.Length > 0)
                {
                    Stale[$"{grupa}.{stala.Groups[1].Value}"] = stala.Groups[2].Value;
                }
            }
        }

        private static string ZnajdzKatalogRepozytorium(string plikTestu)
        {
            DirectoryInfo? katalog = new FileInfo(plikTestu).Directory;

            while (katalog is not null && !Directory.Exists(Path.Combine(katalog.FullName, "src")))
            {
                katalog = katalog.Parent;
            }

            Assert.NotNull(katalog);

            return katalog.FullName;
        }
    }
}
