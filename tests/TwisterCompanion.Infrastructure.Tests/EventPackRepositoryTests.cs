using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Infrastructure.Tests.Fixtures;

namespace TwisterCompanion.Infrastructure.Tests;

/// <summary>
/// Testy przechowywania paczek wydarzeń — zapis, odczyt, ochrona paczek wbudowanych
/// oraz zachowanie przy uszkodzonych danych.
/// </summary>
public class EventPackRepositoryTests
{
    [Fact]
    public async Task PaczkaRodzinna_MaWszystkieWydarzeniaPoPolProcenta()
    {
        // Kontrakt zawartości, nie tylko formatu: paczka rodzinna jest największa i to na niej
        // najłatwiej popsuć coś przy dopisywaniu wydarzeń — literówka w kluczu, powtórzony
        // identyfikator albo szansa wpisana jako 5 zamiast 0,5.
        using TemporaryStorage storage = new();

        EventPack rodzinna = (await storage.EventPacks.GetAllAsync())
            .Single(pack => pack.NameKey == "EventPack_Family_Name");

        Assert.Equal(63, rodzinna.Events.Count);
        Assert.All(rodzinna.Events, gameEvent => Assert.Equal(0.5, gameEvent.Chance.Percent));

        // Suma szans decyduje o tym, jak często pada jakiekolwiek wydarzenie. 31,5% to około
        // co trzecia tura — przy jednym procencie na wydarzenie byłoby to 63%.
        Assert.Equal(31.5, rodzinna.TotalEnabledChancePercent);

        Assert.Equal(
            rodzinna.Events.Count,
            rodzinna.Events.Select(gameEvent => gameEvent.Id).Distinct().Count());
    }

    [Fact]
    public async Task PaczkaDzieciece_MaWszystkieWydarzeniaPoPolProcenta()
    {
        using TemporaryStorage storage = new();

        EventPack dziecieca = (await storage.EventPacks.GetAllAsync())
            .Single(pack => pack.NameKey == "EventPack_Kids_Name");

        Assert.Equal(53, dziecieca.Events.Count);
        Assert.All(dziecieca.Events, gameEvent => Assert.Equal(0.5, gameEvent.Chance.Percent));
        Assert.Equal(26.5, dziecieca.TotalEnabledChancePercent);
    }

    [Fact]
    public async Task WydarzeniaWszystkichPaczek_MajaRozneIdentyfikatory()
    {
        // Identyfikatory są wpisane w plikach ręcznie, więc powtórzenie jest kwestią czasu.
        // Zderzenie dwóch wydarzeń o tym samym identyfikatorze psułoby historię wydarzeń
        // w partii: silnik pamięta, co już padło, właśnie po identyfikatorze.
        using TemporaryStorage storage = new();

        IReadOnlyList<EventPack> paczki = await storage.EventPacks.GetAllAsync();

        Guid[] identyfikatory = [.. paczki.SelectMany(pack => pack.Events).Select(gameEvent => gameEvent.Id)];

        Assert.Equal(identyfikatory.Length, identyfikatory.Distinct().Count());
    }

    [Fact]
    public async Task GetAllAsync_NaCzystymKatalogu_ZwracaTylkoPaczkiWbudowane()
    {
        using TemporaryStorage storage = new();

        IReadOnlyList<EventPack> packs = await storage.EventPacks.GetAllAsync();

        Assert.NotEmpty(packs);
        Assert.All(packs, pack => Assert.True(pack.IsBuiltIn));
    }

    [Fact]
    public async Task GetAllAsync_ZwracaTrzyZapowiedzianePaczkiWbudowane()
    {
        // Trzy, nie cztery: paczka „Śpiewane" została usunięta decyzją użytkownika, a jej
        // jedyne wydarzenie, które miało sens poza nią — refren ulubionej piosenki — przeszło
        // do paczki imprezowej.
        using TemporaryStorage storage = new();

        IReadOnlyList<EventPack> packs = await storage.EventPacks.GetAllAsync();
        string[] nazwy = [.. packs.Select(pack => pack.Name)];

        Assert.Equal(3, packs.Count);
        Assert.Contains("Party", nazwy);
        Assert.Contains("Kids", nazwy);
        Assert.Contains("Family", nazwy);
    }

    [Fact]
    public async Task PaczkaImprezowa_MaWszystkieWydarzeniaPoPolProcenta()
    {
        using TemporaryStorage storage = new();

        EventPack imprezowa = (await storage.EventPacks.GetAllAsync())
            .Single(pack => pack.NameKey == "EventPack_Party_Name");

        Assert.Equal(43, imprezowa.Events.Count);
        Assert.All(imprezowa.Events, gameEvent => Assert.Equal(0.5, gameEvent.Chance.Percent));
        Assert.Equal(21.5, imprezowa.TotalEnabledChancePercent);
    }

    [Fact]
    public async Task PaczkiWbudowane_MajaWydarzeniaZKluczamiZasobow()
    {
        // Nazwy paczek wbudowanych muszą dać się przetłumaczyć (Etap 2), więc każde
        // wydarzenie musi mieć klucz zasobu, a nie nazwę wpisaną na twardo.
        using TemporaryStorage storage = new();

        IReadOnlyList<EventPack> packs = await storage.EventPacks.GetAllAsync();

        Assert.All(packs, pack =>
        {
            Assert.NotNull(pack.NameKey);
            Assert.NotEmpty(pack.Events);
            Assert.All(pack.Events, gameEvent =>
            {
                Assert.NotNull(gameEvent.NameKey);
                Assert.Null(gameEvent.CustomName);
            });
        });
    }

    [Fact]
    public async Task SaveAsync_NastepnieGetByIdAsync_ZwracaTeSameDane()
    {
        using TemporaryStorage storage = new();
        EventPack pack = EventPack.Create("Moja paczka",
        [
            GameEvent.CreateCustom("Zamiana miejsc", 7, EventScope.AllPlayers),
            GameEvent.CreateCustom("Zaśpiewaj refren", 12),
        ]);

        await storage.EventPacks.SaveAsync(pack);
        EventPack? odczytana = await storage.EventPacks.GetByIdAsync(pack.Id);

        Assert.NotNull(odczytana);
        Assert.Equal(pack.Id, odczytana.Id);
        Assert.Equal("Moja paczka", odczytana.Name);
        Assert.False(odczytana.IsBuiltIn);
        Assert.Equal(2, odczytana.Events.Count);

        GameEvent pierwsze = odczytana.Events.Single(e => e.CustomName == "Zamiana miejsc");
        Assert.Equal(7, pierwsze.Chance.Percent);
        Assert.Equal(EventScope.AllPlayers, pierwsze.Scope);
        Assert.True(pierwsze.IsEnabled);
    }

    [Fact]
    public async Task SaveAsync_PoRestarcieAplikacji_DaneSaNadalDostepne()
    {
        // Osobny kontener na tym samym katalogu odpowiada ponownemu uruchomieniu aplikacji.
        using TemporaryStorage storage = new();
        EventPack pack = EventPack.Create("Trwała paczka", [GameEvent.CreateCustom("Test", 5)]);
        await storage.EventPacks.SaveAsync(pack);

        IReadOnlyList<EventPack> packs = await storage.EventPacks.GetAllAsync();

        Assert.Contains(packs, candidate => candidate.Id == pack.Id);
    }

    [Fact]
    public async Task SaveAsync_PaczkiWbudowanej_RzucaWyjatek()
    {
        using TemporaryStorage storage = new();
        EventPack builtIn = (await storage.EventPacks.GetAllAsync()).First(pack => pack.IsBuiltIn);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => storage.EventPacks.SaveAsync(builtIn));
    }

    [Fact]
    public async Task DeleteAsync_PaczkiWbudowanej_RzucaWyjatek()
    {
        using TemporaryStorage storage = new();
        EventPack builtIn = (await storage.EventPacks.GetAllAsync()).First(pack => pack.IsBuiltIn);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => storage.EventPacks.DeleteAsync(builtIn.Id));
    }

    [Fact]
    public async Task DeleteAsync_PaczkiUzytkownika_UsuwaJa()
    {
        using TemporaryStorage storage = new();
        EventPack pack = EventPack.Create("Do usunięcia", [GameEvent.CreateCustom("Test", 5)]);
        await storage.EventPacks.SaveAsync(pack);

        bool usunieta = await storage.EventPacks.DeleteAsync(pack.Id);

        Assert.True(usunieta);
        Assert.Null(await storage.EventPacks.GetByIdAsync(pack.Id));
    }

    [Fact]
    public async Task DeleteAsync_NieistniejacejPaczki_ZwracaFalse()
    {
        using TemporaryStorage storage = new();

        Assert.False(await storage.EventPacks.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Duplicate_PaczkiWbudowanej_DajeSieZapisac()
    {
        // Sposób, w jaki użytkownik „edytuje" paczkę wbudowaną: kopiuje i zmienia kopię.
        using TemporaryStorage storage = new();
        EventPack builtIn = (await storage.EventPacks.GetAllAsync()).First(pack => pack.IsBuiltIn);

        EventPack kopia = builtIn.Duplicate(builtIn.Name + " (kopia)");
        await storage.EventPacks.SaveAsync(kopia);

        EventPack? odczytana = await storage.EventPacks.GetByIdAsync(kopia.Id);

        Assert.NotNull(odczytana);
        Assert.False(odczytana.IsBuiltIn);
        Assert.NotEqual(builtIn.Id, odczytana.Id);
        Assert.Equal(builtIn.Events.Count, odczytana.Events.Count);
        Assert.Empty(odczytana.Events.Select(e => e.Id).Intersect(builtIn.Events.Select(e => e.Id)));
    }

    [Fact]
    public async Task GetAllAsync_GdyPlikJestUszkodzony_PomijaGoIZwracaPozostale()
    {
        using TemporaryStorage storage = new();
        EventPack poprawna = EventPack.Create("Poprawna", [GameEvent.CreateCustom("Test", 5)]);
        await storage.EventPacks.SaveAsync(poprawna);
        storage.WriteRawPackFile("uszkodzona.json", "{ to nie jest poprawny JSON");

        IReadOnlyList<EventPack> packs = await storage.EventPacks.GetAllAsync();

        Assert.Contains(packs, pack => pack.Id == poprawna.Id);
    }

    [Fact]
    public async Task GetAllAsync_GdyPlikMaNowszaWersjeSchematu_PomijaGo()
    {
        // Plik z przyszłej wersji aplikacji mógłby zostać zinterpretowany błędnie,
        // a przy najbliższym zapisie — nadpisany i bezpowrotnie uszkodzony.
        using TemporaryStorage storage = new();
        Guid id = Guid.NewGuid();
        storage.WriteRawPackFile(
            $"{id:D}.json",
            $$"""
            {
              "schemaVersion": 999,
              "id": "{{id:D}}",
              "name": "Z przyszłości",
              "events": []
            }
            """);

        IReadOnlyList<EventPack> packs = await storage.EventPacks.GetAllAsync();

        Assert.DoesNotContain(packs, pack => pack.Id == id);
    }

    [Fact]
    public async Task GetAllAsync_GdyPlikNieMaWersjiSchematu_MigrujeGoIWczytuje()
    {
        // Dokument bez pola schemaVersion traktujemy jako wersję 0 i podnosimy migracją.
        using TemporaryStorage storage = new();
        Guid id = Guid.NewGuid();
        storage.WriteRawPackFile(
            $"{id:D}.json",
            $$"""
            {
              "id": "{{id:D}}",
              "name": "Bez wersji",
              "events": [
                {
                  "id": "11111111-1111-4111-8111-111111111111",
                  "customName": "Stare wydarzenie",
                  "chancePercent": 10,
                  "isEnabled": true,
                  "scope": "CurrentPlayer"
                }
              ]
            }
            """);

        EventPack? odczytana = await storage.EventPacks.GetByIdAsync(id);

        Assert.NotNull(odczytana);
        Assert.Equal("Bez wersji", odczytana.Name);
        Assert.Equal("Stare wydarzenie", Assert.Single(odczytana.Events).CustomName);
    }

    [Fact]
    public async Task GetAllAsync_PomijaWydarzeniaBezJakiejkolwiekNazwy()
    {
        using TemporaryStorage storage = new();
        Guid id = Guid.NewGuid();
        storage.WriteRawPackFile(
            $"{id:D}.json",
            $$"""
            {
              "schemaVersion": 1,
              "id": "{{id:D}}",
              "name": "Częściowo uszkodzona",
              "events": [
                { "id": "22222222-2222-4222-8222-222222222222", "chancePercent": 10 },
                { "id": "33333333-3333-4333-8333-333333333333", "customName": "Dobre", "chancePercent": 10 }
              ]
            }
            """);

        EventPack? odczytana = await storage.EventPacks.GetByIdAsync(id);

        Assert.NotNull(odczytana);
        Assert.Equal("Dobre", Assert.Single(odczytana.Events).CustomName);
    }

    [Fact]
    public async Task GetAllAsync_PrzycinaSzanseWyjscePozaZakres()
    {
        using TemporaryStorage storage = new();
        Guid id = Guid.NewGuid();
        storage.WriteRawPackFile(
            $"{id:D}.json",
            $$"""
            {
              "schemaVersion": 1,
              "id": "{{id:D}}",
              "name": "Poza zakresem",
              "events": [
                { "id": "44444444-4444-4444-8444-444444444444", "customName": "Za duża", "chancePercent": 500 },
                { "id": "55555555-5555-4555-8555-555555555555", "customName": "Ujemna", "chancePercent": -20 }
              ]
            }
            """);

        EventPack? odczytana = await storage.EventPacks.GetByIdAsync(id);

        Assert.NotNull(odczytana);
        Assert.Equal(100, odczytana.Events.Single(e => e.CustomName == "Za duża").Chance.Percent);
        Assert.Equal(0, odczytana.Events.Single(e => e.CustomName == "Ujemna").Chance.Percent);
    }

    [Fact]
    public async Task GetAllAsync_IgnorujePlikiTymczasowePozostaloscPoPrzerwanymZapisie()
    {
        using TemporaryStorage storage = new();
        storage.WriteRawPackFile("cokolwiek.json.tmp", "{ obcięty zapis");

        IReadOnlyList<EventPack> packs = await storage.EventPacks.GetAllAsync();

        Assert.All(packs, pack => Assert.True(pack.IsBuiltIn));
    }
}
