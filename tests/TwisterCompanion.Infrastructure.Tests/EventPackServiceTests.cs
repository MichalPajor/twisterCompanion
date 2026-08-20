using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Infrastructure.Tests.Fixtures;

namespace TwisterCompanion.Infrastructure.Tests;

/// <summary>
/// Testy operacji na paczkach wydarzeń — realizacja kryterium ukończenia Etapu 6:
/// utworzenie paczki, dodanie wydarzeń, ustawienie szans, aktywacja i trwałość po restarcie.
/// </summary>
public class EventPackServiceTests
{
    [Fact]
    public async Task PelnyScenariuszUzytkownika_PrzezywaRestartAplikacji()
    {
        // Kryterium ukończenia Etapu 6 w całości: paczka, pięć wydarzeń, szanse,
        // aktywacja, a potem ponowne uruchomienie aplikacji.
        using TemporaryStorage storage = new();

        EventPack pack = await storage.EventPackService.CreateAsync("Moja impreza");

        for (int index = 1; index <= 5; index++)
        {
            pack = pack.WithEvent(GameEvent.CreateCustom($"Wydarzenie {index}", index * 4));
            await storage.EventPackService.SaveAsync(pack);
        }

        await storage.EventPackService.SetActiveAsync(pack.Id);

        // Osobny kontener na tym samym katalogu odpowiada ponownemu uruchomieniu aplikacji.
        using TemporaryStorage poRestarcie = new(storage.Root);
        await poRestarcie.Settings.LoadAsync();

        EventPack? aktywna = await poRestarcie.EventPackService.GetActiveAsync();

        Assert.NotNull(aktywna);
        Assert.Equal("Moja impreza", aktywna.Name);
        Assert.Equal(5, aktywna.Events.Count);
        Assert.Equal([4, 8, 12, 16, 20], aktywna.Events.Select(e => e.Chance.Percent).Order());
        Assert.Equal(60, aktywna.TotalEnabledChancePercent);
    }

    [Fact]
    public async Task GetActiveAsync_BezWyboru_ZwracaNull()
    {
        using TemporaryStorage storage = new();
        await storage.Settings.LoadAsync();

        Assert.Null(await storage.EventPackService.GetActiveAsync());
    }

    [Fact]
    public async Task SetActiveAsync_MoznaAktywowacPaczkeWbudowana()
    {
        using TemporaryStorage storage = new();
        await storage.Settings.LoadAsync();
        EventPack builtIn = (await storage.EventPackService.GetAllAsync()).First(pack => pack.IsBuiltIn);

        await storage.EventPackService.SetActiveAsync(builtIn.Id);

        EventPack? aktywna = await storage.EventPackService.GetActiveAsync();

        Assert.NotNull(aktywna);
        Assert.Equal(builtIn.Id, aktywna.Id);
        Assert.True(aktywna.IsBuiltIn);
    }

    [Fact]
    public async Task DeleteAsync_UsuwajacAktywnaPaczke_CzysciWyborWUstawieniach()
    {
        // Inaczej ustawienia wskazywałyby na paczkę, której już nie ma.
        using TemporaryStorage storage = new();
        await storage.Settings.LoadAsync();

        EventPack pack = await storage.EventPackService.CreateAsync("Do usunięcia");
        await storage.EventPackService.SetActiveAsync(pack.Id);

        await storage.EventPackService.DeleteAsync(pack.Id);

        Assert.Null(storage.Settings.Current.ActiveEventPackId);
        Assert.Null(await storage.EventPackService.GetActiveAsync());
    }

    [Fact]
    public async Task GetActiveAsync_GdyAktywnaPaczkaZniknela_CzysciWybor()
    {
        using TemporaryStorage storage = new();
        await storage.Settings.LoadAsync();
        await storage.EventPackService.SetActiveAsync(Guid.NewGuid());

        Assert.Null(await storage.EventPackService.GetActiveAsync());
        Assert.Null(storage.Settings.Current.ActiveEventPackId);
    }

    [Fact]
    public async Task DuplicateAsync_PaczkiWbudowanej_DajeEdytowalnaKopie()
    {
        // Jedyny sposób zmiany zawartości paczki wbudowanej.
        using TemporaryStorage storage = new();
        await storage.Settings.LoadAsync();
        EventPack builtIn = (await storage.EventPackService.GetAllAsync()).First(pack => pack.IsBuiltIn);

        EventPack kopia = await storage.EventPackService.DuplicateAsync(builtIn, "Moja kopia");
        EventPack zmieniona = kopia.WithEvent(GameEvent.CreateCustom("Dodane", 15));
        await storage.EventPackService.SaveAsync(zmieniona);

        IReadOnlyList<EventPack> wszystkie = await storage.EventPackService.GetAllAsync();
        EventPack odczytana = wszystkie.Single(pack => pack.Id == kopia.Id);

        Assert.False(odczytana.IsBuiltIn);
        Assert.Equal(builtIn.Events.Count + 1, odczytana.Events.Count);
    }

    [Fact]
    public async Task SaveAsync_PaczkiWbudowanej_RzucaWyjatek()
    {
        using TemporaryStorage storage = new();
        EventPack builtIn = (await storage.EventPackService.GetAllAsync()).First(pack => pack.IsBuiltIn);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => storage.EventPackService.SaveAsync(builtIn));
    }

}
