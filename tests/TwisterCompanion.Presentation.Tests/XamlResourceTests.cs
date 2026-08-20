using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Presentation.Tests;

/// <summary>
/// Testy spójności słowników zasobów XAML.
/// </summary>
/// <remarks>
/// Powstały po awarii, którą kompilator przepuścił, a urządzenie zgłosiło dopiero przy starcie:
/// <c>StaticResource not found for key CardBorder</c>. Styl odwoływał się przez
/// <c>BasedOn</c> do klucza zdefiniowanego <b>niżej</b> w tym samym pliku, a słownik zasobów
/// rozwiązuje <c>StaticResource</c> w kolejności wpisów. Kompilacja przechodzi, aplikacja nie
/// wstaje — czyli najgorszy możliwy rodzaj błędu: niewidoczny do momentu uruchomienia.
/// <para>
/// Testy czytają <b>pliki źródłowe</b> z dysku, bo warstwa widoku nie ma własnego projektu
/// testowego: jest projektem Androida, a projekt testowy <c>net10.0</c> nie może go referować.
/// Katalog repozytorium bierzemy z <see cref="CallerFilePathAttribute"/>, bo katalog wyjściowy
/// buildu leży poza repozytorium.
/// </para>
/// <para>
/// Sprawdzamy tylko <c>StaticResource</c>. <c>DynamicResource</c> jest rozwiązywane przy
/// każdym użyciu, więc odwołanie „w przód" jest tam poprawne — ale sam klucz musi istnieć,
/// i to też jest tu sprawdzane.
/// </para>
/// </remarks>
public class XamlResourceTests
{
    private static readonly Regex StaticResourceUse = new(
        @"\{StaticResource\s+([A-Za-z0-9_]+)\}",
        RegexOptions.None);

    private static readonly Regex KeyDefinition = new(@"x:Key=""([^""]+)""", RegexOptions.None);

    private static readonly Regex MergedDictionary = new(
        @"<ResourceDictionary\s+Source=""([^""]+)""",
        RegexOptions.None);

    private static readonly Regex TranslateUse = new(
        @"\{loc:Translate\s+([A-Za-z0-9_]+)\}",
        RegexOptions.None);

    private static readonly Regex DynamicResourceInCode = new(
        @"SetDynamicResource\(\s*StyleProperty\s*,\s*""([^""]+)""\s*\)",
        RegexOptions.None);

    [Fact]
    public void SlownikiZasobow_OdwolujaSieTylkoDoKluczyZdefiniowanychWczesniej()
    {
        HashSet<string> defined = new(StringComparer.Ordinal);
        List<string> problems = [];

        foreach (string dictionary in MergedDictionaries())
        {
            HashSet<string> localKeys = new(StringComparer.Ordinal);
            string[] lines = File.ReadAllLines(dictionary);

            for (int index = 0; index < lines.Length; index++)
            {
                foreach (Match match in StaticResourceUse.Matches(lines[index]))
                {
                    string key = match.Groups[1].Value;

                    if (!defined.Contains(key) && !localKeys.Contains(key))
                    {
                        problems.Add($"{Path.GetFileName(dictionary)}:{index + 1} → {key}");
                    }
                }

                // Klucze z tej samej linii dopisujemy PO sprawdzeniu, bo styl nie może
                // odwołać się do samego siebie.
                foreach (Match match in KeyDefinition.Matches(lines[index]))
                {
                    localKeys.Add(match.Groups[1].Value);
                }
            }

            defined.UnionWith(localKeys);
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void EkranyUzywajaTylkoIstniejacychKluczy()
    {
        // Literówka w kluczu na ekranie nie jest błędem kompilacji — jest wyjątkiem przy
        // wejściu na ten jeden ekran, czyli awarią, na którą trafia użytkownik, a nie build.
        IReadOnlySet<string> defined = DefinedKeys();
        List<string> problems = [];

        foreach (string page in Directory.GetFiles(ViewsDirectory(), "*.xaml"))
        {
            string content = File.ReadAllText(page);

            // Ekran może zdefiniować własny zasób — te klucze też są dozwolone.
            HashSet<string> local = new(
                KeyDefinition.Matches(content).Select(match => match.Groups[1].Value),
                StringComparer.Ordinal);

            foreach (Match match in StaticResourceUse.Matches(content))
            {
                string key = match.Groups[1].Value;

                if (!defined.Contains(key) && !local.Contains(key))
                {
                    problems.Add($"{Path.GetFileName(page)} → {key}");
                }
            }
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void KontrolkiZKoduUzywajaIstniejacychStylow()
    {
        // Kontrolki budowane w kodzie (PageHeader, SectionHeader) sięgają po styl przez
        // DynamicResource. Nieistniejący klucz nie wywala aplikacji — po prostu kontrolka
        // zostaje bez stylu, co widać dopiero na ekranie.
        IReadOnlySet<string> defined = DefinedKeys();
        List<string> problems = [];

        foreach (string file in Directory.GetFiles(ViewsDirectory(), "*.cs"))
        {
            foreach (Match match in DynamicResourceInCode.Matches(File.ReadAllText(file)))
            {
                string key = match.Groups[1].Value;

                if (!defined.Contains(key))
                {
                    problems.Add($"{Path.GetFileName(file)} → {key}");
                }
            }
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void EkranyTlumaczaTylkoIstniejaceKlucze()
    {
        // Klucz wpisany w XAML nie przechodzi przez stałe z StringKeys, więc literówka nie
        // jest błędem kompilacji — na ekranie pojawia się [Nazwa_Klucza_W_Nawiasach].
        // Widać ją tylko wtedy, gdy ktoś wejdzie na ten ekran i przeczyta napis.
        ResourceManager resources = new(
            "TwisterCompanion.Application.Resources.Strings.AppResources",
            typeof(ILocalizationService).Assembly);

        List<string> problems = [];

        foreach (string page in Directory.GetFiles(ViewsDirectory(), "*.xaml"))
        {
            foreach (Match match in TranslateUse.Matches(File.ReadAllText(page)))
            {
                string key = match.Groups[1].Value;

                if (resources.GetString(key, CultureInfo.InvariantCulture) is null)
                {
                    problems.Add($"{Path.GetFileName(page)} → {key}");
                }
            }
        }

        Assert.Empty(problems);
    }

    /// <summary>Zwraca wszystkie klucze zdefiniowane w słownikach aplikacji.</summary>
    private static IReadOnlySet<string> DefinedKeys()
    {
        HashSet<string> keys = new(StringComparer.Ordinal);

        foreach (string dictionary in MergedDictionaries())
        {
            foreach (Match match in KeyDefinition.Matches(File.ReadAllText(dictionary)))
            {
                keys.Add(match.Groups[1].Value);
            }
        }

        return keys;
    }

    /// <summary>
    /// Zwraca słowniki zasobów w kolejności scalania.
    /// </summary>
    /// <remarks>
    /// Kolejność pochodzi z <c>App.xaml</c>, a nie z listy wpisanej w teście: dołożenie
    /// słownika ma być objęte tym sprawdzeniem bez zmiany testu.
    /// </remarks>
    private static IEnumerable<string> MergedDictionaries()
    {
        string appDirectory = Path.Combine(RepositoryRoot(), "src", "TwisterCompanion.App");
        string appXaml = Path.Combine(appDirectory, "App.xaml");

        Assert.True(File.Exists(appXaml), $"Nie znaleziono {appXaml}.");

        foreach (Match match in MergedDictionary.Matches(File.ReadAllText(appXaml)))
        {
            string relative = match.Groups[1].Value.Replace('/', Path.DirectorySeparatorChar);
            string full = Path.Combine(appDirectory, relative);

            Assert.True(File.Exists(full), $"Nie znaleziono słownika {full}.");

            yield return full;
        }
    }

    private static string ViewsDirectory() =>
        Path.Combine(RepositoryRoot(), "src", "TwisterCompanion.App", "Views");

    /// <summary>
    /// Zwraca katalog repozytorium.
    /// </summary>
    /// <remarks>
    /// Ścieżka jest wyliczana z położenia tego pliku źródłowego, bo katalog wyjściowy buildu
    /// leży poza repozytorium (patrz <c>Directory.Build.props</c>), więc szukanie w górę od
    /// katalogu binariów nic by nie znalazło.
    /// </remarks>
    private static string RepositoryRoot([CallerFilePath] string? callerFilePath = null)
    {
        string testDirectory = Path.GetDirectoryName(callerFilePath)
            ?? throw new InvalidOperationException("Nie udało się ustalić katalogu testów.");

        return Path.GetFullPath(Path.Combine(testDirectory, "..", ".."));
    }
}
