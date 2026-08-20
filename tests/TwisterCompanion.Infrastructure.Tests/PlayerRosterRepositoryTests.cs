using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Infrastructure.Tests.Fixtures;

namespace TwisterCompanion.Infrastructure.Tests;

/// <summary>
/// Testy zapamiętywania listy graczy między uruchomieniami.
/// </summary>
public class PlayerRosterRepositoryTests
{
    [Fact]
    public async Task GetAsync_GdyBrakZapisu_ZwracaPustaListe()
    {
        using TemporaryStorage storage = new();

        Assert.Empty(await storage.PlayerRoster.GetAsync());
    }

    [Fact]
    public async Task SaveAsync_NastepnieGetAsync_ZwracaGraczyWKolejnosci()
    {
        using TemporaryStorage storage = new();
        Player[] gracze =
        [
            Player.Create("Kuba", 0),
            Player.Create("Anna", 1),
            Player.Create("Marek", 2),
        ];

        await storage.PlayerRoster.SaveAsync(gracze);
        IReadOnlyList<Player> odczytani = await storage.PlayerRoster.GetAsync();

        Assert.Equal(["Kuba", "Anna", "Marek"], odczytani.Select(player => player.Name));
    }

    [Fact]
    public async Task SaveAsync_PoRestarcieAplikacji_ZwracaTeSameDane()
    {
        // Zadanie 2 Etapu 14 — kontrakt repozytorium sprawdzany na dysku, nie w pamięci
        // jednego kontenera: dopiero drugi kontener na tym samym katalogu dowodzi, że dane
        // przeszły przez plik.
        using TemporaryStorage pierwszeUruchomienie = new();
        await pierwszeUruchomienie.PlayerRoster.SaveAsync(
            [Player.Create("Kuba", 0), Player.Create("Anna", 1)]);

        using TemporaryStorage poRestarcie = new(pierwszeUruchomienie.Root);

        Assert.Equal(
            ["Kuba", "Anna"],
            (await poRestarcie.PlayerRoster.GetAsync()).Select(player => player.Name));
    }

    [Fact]
    public async Task GetAsync_GdyPlikJestUszkodzony_ZwracaPustaListe()
    {
        // Lista graczy jest wygodą, nie danymi, których utrata boli — dlatego uszkodzony
        // plik nie jest błędem. Gracz zobaczy pusty ekran składu i wpisze imiona jeszcze raz,
        // zamiast dostać komunikat o awarii przy wejściu na ekran.
        using TemporaryStorage storage = new();
        storage.WriteRawRosterFile("{ to nie jest JSON");

        Assert.Empty(await storage.PlayerRoster.GetAsync());
    }

    [Fact]
    public async Task GetAsync_GdyPlikMaNowszaWersjeSchematu_ZwracaPustaListe()
    {
        using TemporaryStorage storage = new();
        storage.WriteRawRosterFile("""{ "schemaVersion": 999, "players": [] }""");

        Assert.Empty(await storage.PlayerRoster.GetAsync());
    }

    [Fact]
    public async Task GetAsync_PomijaWpisyBezImieniaAlboBezIdentyfikatora()
    {
        // Pojedynczy uszkodzony wpis nie może zabrać całej listy — gracz straciłby skład
        // przez jedno pole.
        using TemporaryStorage storage = new();
        storage.WriteRawRosterFile("""
            {
              "schemaVersion": 1,
              "players": [
                { "id": "11111111-1111-1111-1111-111111111111", "name": "Kuba", "order": 0 },
                { "id": "22222222-2222-2222-2222-222222222222", "name": "   ", "order": 1 },
                { "id": "00000000-0000-0000-0000-000000000000", "name": "Bez identyfikatora", "order": 2 },
                { "id": "33333333-3333-3333-3333-333333333333", "name": "Anna", "order": 3 }
              ]
            }
            """);

        Assert.Equal(
            ["Kuba", "Anna"],
            (await storage.PlayerRoster.GetAsync()).Select(player => player.Name));
    }

    [Fact]
    public async Task GetAsync_SortujeGraczyPoKolejnosciNiezaleznieOdZapisu()
    {
        using TemporaryStorage storage = new();
        Player[] gracze =
        [
            Player.Create("Trzeci", 2),
            Player.Create("Pierwszy", 0),
            Player.Create("Drugi", 1),
        ];

        await storage.PlayerRoster.SaveAsync(gracze);
        IReadOnlyList<Player> odczytani = await storage.PlayerRoster.GetAsync();

        Assert.Equal(["Pierwszy", "Drugi", "Trzeci"], odczytani.Select(player => player.Name));
    }

    [Fact]
    public async Task GetAsync_NieZapamietujeInformacjiOEliminacji()
    {
        // Zapamiętujemy skład, a nie przebieg rozgrywki — kolejna gra startuje z pełną stawką.
        using TemporaryStorage storage = new();
        Player odpadly = Player.Create("Kuba", 0) with { IsEliminated = true };

        await storage.PlayerRoster.SaveAsync([odpadly]);
        IReadOnlyList<Player> odczytani = await storage.PlayerRoster.GetAsync();

        Assert.False(Assert.Single(odczytani).IsEliminated);
    }

    [Fact]
    public async Task ClearAsync_UsuwaZapamietanaListe()
    {
        using TemporaryStorage storage = new();
        await storage.PlayerRoster.SaveAsync([Player.Create("Kuba", 0)]);

        await storage.PlayerRoster.ClearAsync();

        Assert.Empty(await storage.PlayerRoster.GetAsync());
    }

    [Fact]
    public async Task SaveAsync_PustaLista_JestPoprawnaOperacja()
    {
        using TemporaryStorage storage = new();
        await storage.PlayerRoster.SaveAsync([Player.Create("Kuba", 0)]);

        await storage.PlayerRoster.SaveAsync([]);

        Assert.Empty(await storage.PlayerRoster.GetAsync());
    }
}
