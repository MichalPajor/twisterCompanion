using System.Collections;
using System.Globalization;
using System.Resources;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.GameModes;
using TwisterCompanion.Infrastructure.Tests.Fixtures;

namespace TwisterCompanion.Infrastructure.Tests;

/// <summary>
/// Testy katalogu trybów gry czytanego z pliku definicji.
/// </summary>
/// <remarks>
/// Katalog jest <c>internal</c> i rozwiązywany z kontenera — tak jak w aplikacji. Dzięki temu
/// testy sprawdzają przy okazji rejestrację, a nie tylko samą klasę.
/// </remarks>
public class GameModeCatalogTests : IDisposable
{
    private readonly TemporaryStorage _storage = new();

    private IGameModeCatalog Catalog => _storage.GameModeCatalog;

    [Fact]
    public async Task DomyslnePaczkiTrybow_WskazujaIstniejacePaczki()
    {
        // Tryb bez wybranej paczki bierze swoją domyślną — a wskazanie paczki, której nie ma,
        // nie jest błędem: gra po prostu idzie bez wydarzeń. Cicho, więc trudno to zauważyć.
        // Ten test złapałby usunięcie paczki „Śpiewane" bez poprawienia trybu o tej nazwie,
        // co zdarzyło się naprawdę.
        using TemporaryStorage storage = new();

        string[] nazwyPaczek = [.. (await storage.EventPacks.GetAllAsync()).Select(pack => pack.NameKey!)];

        string[] brakujace =
        [
            .. storage.GameModeCatalog.GetAvailable()
                .Select(mode => mode.DefaultEventPackNameKey)
                .Where(klucz => klucz is not null && !nazwyPaczek.Contains(klucz))
                .Select(klucz => klucz!),
        ];

        Assert.Empty(brakujace);
    }

    [Fact]
    public void Katalog_ZawieraTrybyStartowe()
    {
        string[] klucze = [.. Catalog.GetAvailable().Select(mode => mode.Key)];

        Assert.Contains("classic", klucze);
        Assert.Contains("hardcore", klucze);
        Assert.Contains("kids", klucze);
        Assert.Contains("party", klucze);
        Assert.Contains("singing", klucze);
    }

    [Fact]
    public void TrybWylaczony_NieJestDostepnyDoWyboru_AleDaSieGoZnalezc()
    {
        // Tryb dla dorosłych jest przygotowany, ale nieudostępniony. Zapisany w ustawieniach
        // musi być rozpoznawalny, żeby aplikacja wiedziała, czym go zastąpić.
        Assert.DoesNotContain("drinking", Catalog.GetAvailable().Select(mode => mode.Key));

        GameModeDefinition? drinking = Catalog.Find("drinking");

        Assert.NotNull(drinking);
        Assert.False(drinking.IsEnabled);
    }

    [Fact]
    public void Domyslny_ToTrybKlasyczny()
    {
        Assert.Equal("classic", Catalog.Default.Key);
        Assert.True(Catalog.Default.IsEnabled);
    }

    [Fact]
    public void Hardcore_MaWyzszeSzanseWydarzenOdParty_ATaWyzszeOdKids()
    {
        // Kolejność trybów po „ostrości" jest częścią ich tożsamości — gdyby ktoś przestawił
        // liczby w pliku definicji, ten test to wychwyci.
        double hardcore = Catalog.Find("hardcore")!.EventSelectionOptions.ChanceMultiplier;
        double party = Catalog.Find("party")!.EventSelectionOptions.ChanceMultiplier;
        double kids = Catalog.Find("kids")!.EventSelectionOptions.ChanceMultiplier;

        Assert.True(hardcore > party, $"Hardcore {hardcore} nie jest ostrzejszy od Party {party}.");
        Assert.True(party > kids, $"Party {party} nie jest ostrzejsze od trybu dla dzieci {kids}.");
    }

    [Fact]
    public void Kids_NieWyklucaGraczyIDajeWiecejCzasu()
    {
        GameModeDefinition kids = Catalog.Find("kids")!;

        Assert.Equal(EliminationRule.NoElimination, kids.EliminationRule);
        Assert.True(kids.MoveTimeMultiplier > 1.0, "Tryb dla dzieci ma wydłużać czas na ruch.");
        Assert.True(kids.TaskTimeMultiplier > 1.0, "Tryb dla dzieci ma wydłużać czas na zadanie.");
    }

    [Fact]
    public void Hardcore_SkracaObaCzasyODpolowy()
    {
        GameModeDefinition hardcore = Catalog.Find("hardcore")!;

        Assert.Equal(0.5, hardcore.MoveTimeMultiplier);
        Assert.Equal(0.5, hardcore.TaskTimeMultiplier);
    }

    [Fact]
    public void Singing_WydluzaTylkoCzasNaZadanie()
    {
        // Zaśpiewanie refrenu trwa dłużej niż postawienie ręki, ale sam ruch jest zwykły.
        GameModeDefinition singing = Catalog.Find("singing")!;

        Assert.Equal(1.0, singing.MoveTimeMultiplier);
        Assert.Equal(1.5, singing.TaskTimeMultiplier);
    }

    [Fact]
    public void Classic_MaWydarzeniaZWybranegoZestawu()
    {
        // Tryb klasyczny nie wyłącza już wydarzeń: jeśli gracz wybrał zestaw, wydarzenia
        // padają. Bez wybranego zestawu i tak nie ma czego losować, więc gra jest klasyczna.
        GameModeDefinition classic = Catalog.Find("classic")!;

        Assert.Equal(1.0, classic.EventSelectionOptions.ChanceMultiplier);
        Assert.Null(classic.DefaultEventPackNameKey);
    }

    [Fact]
    public void Kids_MaLagodniejszeLosowanieOdHardcore()
    {
        GameModeDefinition kids = Catalog.Find("kids")!;
        GameModeDefinition hardcore = Catalog.Find("hardcore")!;

        // Łagodniej znaczy: dłuższe dopuszczalne serie tej samej kończyny i koloru oraz
        // mniejsza kara za ruch, który niczego nie zmienia.
        Assert.True(kids.MoveSelectionOptions.MaxSameBodyPartStreak
            > hardcore.MoveSelectionOptions.MaxSameBodyPartStreak);
        Assert.True(kids.MoveSelectionOptions.MaxSameColorStreak
            > hardcore.MoveSelectionOptions.MaxSameColorStreak);
        Assert.True(kids.MoveSelectionOptions.RedundantMoveMultiplier
            > hardcore.MoveSelectionOptions.RedundantMoveMultiplier);
    }

    [Fact]
    public void KazdyTryb_MaNazweOpisIZasadyWObuJezykach()
    {
        // Tryb bez tekstów nie miałby czym się przedstawić. Test pilnuje warunku z planu:
        // dołożenie trybu to wpis w JSON + klucze tłumaczeń — i wychwytuje pominięcie drugiego.
        IReadOnlySet<string> neutralne = ReadKeys(CultureInfo.InvariantCulture);
        IReadOnlySet<string> polskie = ReadKeys(CultureInfo.GetCultureInfo("pl"));

        List<string> brakujace = [];

        foreach (GameModeDefinition mode in AllModes())
        {
            foreach (string? key in new[] { mode.NameKey, mode.DescriptionKey, mode.RulesKey })
            {
                if (key is null)
                {
                    brakujace.Add($"{mode.Key}: brak klucza");

                    continue;
                }

                if (!neutralne.Contains(key))
                {
                    brakujace.Add("neutralny:" + key);
                }

                if (!polskie.Contains(key))
                {
                    brakujace.Add("pl:" + key);
                }
            }
        }

        Assert.Empty(brakujace);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _storage.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Wszystkie tryby, także wyłączone.</summary>
    private IEnumerable<GameModeDefinition> AllModes() =>
        new[] { "classic", "hardcore", "kids", "party", "singing", "drinking" }
            .Select(Catalog.Find)
            .OfType<GameModeDefinition>();

    private static IReadOnlySet<string> ReadKeys(CultureInfo culture)
    {
        ResourceManager manager = new(
            "TwisterCompanion.Application.Resources.Strings.AppResources",
            typeof(ILocalizationService).Assembly);

        using ResourceSet? set = manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);

        HashSet<string> keys = new(StringComparer.Ordinal);

        if (set is null)
        {
            return keys;
        }

        foreach (DictionaryEntry entry in set)
        {
            if (entry.Key is string key)
            {
                keys.Add(key);
            }
        }

        return keys;
    }
}
