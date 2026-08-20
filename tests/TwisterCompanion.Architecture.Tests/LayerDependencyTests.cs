using System.Reflection;
using System.Xml.Linq;
using TwisterCompanion.Tests.Shared;

namespace TwisterCompanion.Architecture.Tests;

/// <summary>
/// Testy pilnujące kierunku zależności między warstwami.
/// </summary>
/// <remarks>
/// Zadanie 4 Etapu 14. Plan wymieniał bibliotekę NetArchTest — nie została użyta i to jest
/// świadoma decyzja, nie przeoczenie. Reguły tego projektu są tak proste, że wyrażają się
/// dwiema pętlami, a najważniejszą z nich („żadna warstwa poza aplikacją nie zna MAUI")
/// biblioteka reguł na zestawach i tak by przepuściła: nieużywana referencja do paczki
/// NuGet nie zostawia śladu w metadanych zestawu, więc trzeba czytać plik projektu.
/// Dlatego sprawdzamy oba poziomy — deklaracje w plikach projektów i faktyczne referencje
/// skompilowanych zestawów.
/// <para>
/// Po co to w ogóle: architektura tego projektu opiera się na jednym założeniu — rdzeń
/// (Domain, Application, Presentation) nie zna platformy. Na tym stoi możliwość testowania
/// bez urządzenia i przyszły port na iOS. Złamanie tej zasady jest jedną linijką
/// <c>using</c> i nie widać go w żadnym przeglądzie kodu.
/// </para>
/// </remarks>
public class LayerDependencyTests
{
    /// <summary>Dozwolone referencje do innych projektów — z warstwy do warstw.</summary>
    private static readonly Dictionary<string, string[]> DozwoloneProjekty = new()
    {
        ["TwisterCompanion.Domain"] = [],
        ["TwisterCompanion.Application"] = ["TwisterCompanion.Domain"],
        ["TwisterCompanion.Presentation"] = ["TwisterCompanion.Application"],
        ["TwisterCompanion.Infrastructure"] = ["TwisterCompanion.Application"],

        // Projekt aplikacji jest miejscem, w którym warstwy się spotykają — i jedynym.
        ["TwisterCompanion.App"] =
        [
            "TwisterCompanion.Application",
            "TwisterCompanion.Domain",
            "TwisterCompanion.Infrastructure",
            "TwisterCompanion.Presentation",
        ],
    };

    /// <summary>Warstwy, którym nie wolno znać żadnego API platformy.</summary>
    private static readonly string[] WarstwyBezPlatformy =
    [
        "TwisterCompanion.Domain",
        "TwisterCompanion.Application",
        "TwisterCompanion.Presentation",
        "TwisterCompanion.Infrastructure",
    ];

    [Fact]
    public void ZadnaWarstwa_NieReferujeWarstwyWyzszej()
    {
        List<string> naruszenia = [];

        foreach ((string warstwa, XDocument projekt) in WczytajProjekty())
        {
            string[] dozwolone = DozwoloneProjekty[warstwa];

            foreach (string referencja in Referencje(projekt, "ProjectReference"))
            {
                string nazwa = Path.GetFileNameWithoutExtension(referencja.Replace('\\', '/'));

                if (!dozwolone.Contains(nazwa))
                {
                    naruszenia.Add($"{warstwa} → {nazwa}");
                }
            }
        }

        Assert.True(
            naruszenia.Count == 0,
            "Referencje łamiące kierunek zależności:\n" + string.Join("\n", naruszenia));
    }

    [Fact]
    public void RdzenAplikacji_NieZnaMaui()
    {
        List<string> naruszenia = [];

        foreach ((string warstwa, XDocument projekt) in WczytajProjekty())
        {
            if (!WarstwyBezPlatformy.Contains(warstwa))
            {
                continue;
            }

            foreach (string paczka in Referencje(projekt, "PackageReference"))
            {
                // CommunityToolkit.Mvvm jest zwykłą biblioteką MVVM bez API platformy i wolno
                // jej być w Presentation. CommunityToolkit.Maui już nie — stąd ten warunek
                // patrzy na „Maui" w nazwie, a nie na przedrostek producenta.
                if (paczka.Contains("Maui", StringComparison.OrdinalIgnoreCase))
                {
                    naruszenia.Add($"{warstwa} → {paczka}");
                }
            }

            foreach (string cel in projekt.Descendants("TargetFramework")
                         .Concat(projekt.Descendants("TargetFrameworks"))
                         .Select(element => element.Value))
            {
                // Warstwa z celem platformowym (np. net10.0-android) prędzej czy później
                // zacznie z tej platformy korzystać. Rdzeń zostaje na czystym net10.0.
                if (cel.Contains('-', StringComparison.Ordinal))
                {
                    naruszenia.Add($"{warstwa} celuje w {cel}");
                }
            }
        }

        Assert.True(
            naruszenia.Count == 0,
            "Rdzeń aplikacji nie może zależeć od platformy:\n" + string.Join("\n", naruszenia));
    }

    [Fact]
    public void SkompilowaneZestawy_MajaTylkoDozwoloneReferencje()
    {
        // Drugi poziom sprawdzenia: plik projektu mówi o zamiarze, metadane zestawu o fakcie.
        // Tu wyszłaby na przykład referencja odziedziczona tranzytywnie przez cudzą paczkę.
        Assembly[] zestawy =
        [
            typeof(TwisterCompanion.Domain.Entities.Player).Assembly,
            typeof(TwisterCompanion.Application.Abstractions.ISettingsService).Assembly,
            typeof(TwisterCompanion.Presentation.ViewModels.GameViewModel).Assembly,
            typeof(TwisterCompanion.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions).Assembly,
        ];

        List<string> naruszenia = [];

        foreach (Assembly zestaw in zestawy)
        {
            string warstwa = zestaw.GetName().Name!;

            // Na poziomie zestawów liczy się domknięcie: Presentation referuje tylko
            // Application, ale używa typów z Domain, więc kompilator zapisuje referencję
            // także do Domain. To nie jest złamanie warstw — Domain leży niżej. Złamaniem
            // byłoby Presentation → Infrastructure, i tego to domknięcie nie przepuszcza.
            HashSet<string> dozwolone = DozwoloneTranzytywnie(warstwa);

            foreach (AssemblyName referencja in zestaw.GetReferencedAssemblies())
            {
                string nazwa = referencja.Name ?? string.Empty;

                if (nazwa.StartsWith("TwisterCompanion.", StringComparison.Ordinal)
                    && !dozwolone.Contains(nazwa))
                {
                    naruszenia.Add($"{warstwa} → {nazwa}");
                }

                if (nazwa.StartsWith("Microsoft.Maui", StringComparison.Ordinal)
                    || nazwa.StartsWith("Mono.Android", StringComparison.Ordinal))
                {
                    naruszenia.Add($"{warstwa} → {nazwa}");
                }
            }
        }

        Assert.True(
            naruszenia.Count == 0,
            "Zestawy z niedozwolonymi referencjami:\n" + string.Join("\n", naruszenia));
    }

    [Fact]
    public void KazdaWarstwa_MaZadeklarowaneReguly()
    {
        // Bez tego testu nowa warstwa dodana do src/ byłaby po prostu pomijana przez
        // pozostałe testy — i żaden z nich by nie zaprotestował.
        string[] znalezione = [.. WczytajProjekty().Select(projekt => projekt.Warstwa).Order()];

        Assert.Equal([.. DozwoloneProjekty.Keys.Order()], znalezione);
    }

    /// <summary>Warstwy, które dana warstwa może znać bezpośrednio albo przez inną warstwę.</summary>
    private static HashSet<string> DozwoloneTranzytywnie(string warstwa)
    {
        HashSet<string> zebrane = [];
        Queue<string> doOdwiedzenia = new(DozwoloneProjekty[warstwa]);

        while (doOdwiedzenia.TryDequeue(out string? nastepna))
        {
            if (!zebrane.Add(nastepna))
            {
                continue;
            }

            foreach (string kolejna in DozwoloneProjekty[nastepna])
            {
                doOdwiedzenia.Enqueue(kolejna);
            }
        }

        return zebrane;
    }

    private static IEnumerable<(string Warstwa, XDocument Projekt)> WczytajProjekty()
    {
        foreach (string plik in Directory
                     .EnumerateFiles(KatalogRepozytorium.Plik("src"), "*.csproj", SearchOption.AllDirectories)
                     .Order())
        {
            yield return (Path.GetFileNameWithoutExtension(plik), XDocument.Load(plik));
        }
    }

    private static IEnumerable<string> Referencje(XDocument projekt, string rodzaj) =>
        projekt.Descendants(rodzaj)
            .Select(element => element.Attribute("Include")?.Value)
            .Where(wartosc => !string.IsNullOrWhiteSpace(wartosc))
            .Select(wartosc => wartosc!);
}
