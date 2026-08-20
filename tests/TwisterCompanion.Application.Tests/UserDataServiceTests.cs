using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy kasowania i przywracania danych użytkownika.
/// </summary>
/// <remarks>
/// Realizacja zadania 3 z Etapu 12. „Usuń moje dane" jest obietnicą wobec użytkownika i wobec
/// polityki prywatności, więc pilnowany jest tu <b>zakres</b>: co ma zniknąć i co ma zostać.
/// Zapomniany rodzaj danych byłby złamaniem tej obietnicy, a zauważyłby go dopiero ktoś, kto
/// sprawdzi pliki na telefonie.
/// </remarks>
public class UserDataServiceTests
{
    [Fact]
    public async Task PrzywrocenieUstawien_NieRuszaPozostalychDanych()
    {
        // Rozdział jest celowy: „coś mi się rozjechało w ustawieniach" i „chcę wyczyścić
        // telefon" to dwie różne potrzeby.
        using GameTestHarness harness = new();

        await harness.SettingsService.UpdateAsync(settings => settings with { LanguageCode = "en" });
        await harness.PlayerRoster.SaveAsync([Player.Create("Kuba", 0)]);

        await harness.UserData.ResetSettingsAsync();

        Assert.Null(harness.SettingsService.Current.LanguageCode);
        Assert.Single(await harness.PlayerRoster.GetAsync());
    }

    [Fact]
    public async Task UsuniecieDanych_KasujeSkladGraczy()
    {
        using GameTestHarness harness = new();

        await harness.PlayerRoster.SaveAsync([Player.Create("Kuba", 0), Player.Create("Anna", 1)]);

        await harness.UserData.EraseAsync();

        Assert.Empty(await harness.PlayerRoster.GetAsync());
    }

    [Fact]
    public async Task UsuniecieDanych_PrzywracaUstawieniaDomyslne()
    {
        using GameTestHarness harness = new();

        await harness.SettingsService.UpdateAsync(settings => settings with
        {
            LanguageCode = "en",
            AreSoundsEnabled = false,
            HasSeenOnboarding = true,
        });

        await harness.UserData.EraseAsync();

        Assert.Equal(AppSettings.Default, harness.SettingsService.Current);
    }

    [Fact]
    public async Task UsuniecieDanych_ZerujeInformacjeOWprowadzeniu()
    {
        // Po wyczyszczeniu aplikacja wita nowego właściciela tak, jak przy pierwszym
        // uruchomieniu — wprowadzenie pokazuje się znowu.
        using GameTestHarness harness = new();

        await harness.SettingsService.UpdateAsync(settings => settings with { HasSeenOnboarding = true });

        await harness.UserData.EraseAsync();

        Assert.False(harness.SettingsService.Current.HasSeenOnboarding);
    }

    [Fact]
    public async Task UsuniecieDanych_KasujeZapisPrzerwanejPartii()
    {
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.SaveSnapshotAsync();

        Assert.NotNull(await harness.SessionRepository.LoadAsync());

        await harness.UserData.EraseAsync();

        Assert.Null(await harness.SessionRepository.LoadAsync());
    }

    [Fact]
    public async Task UsuniecieDanych_KasujeWlasnePaczkiIZostawiaWbudowane()
    {
        // Paczki wbudowane to zawartość aplikacji, a nie dane użytkownika — tak samo jak
        // teksty interfejsu, których „usuń moje dane" też nie kasuje.
        using GameTestHarness harness = new();

        await harness.EventPacks.CreateAsync("Moja paczka");

        IReadOnlyList<EventPack> before = await harness.EventPacks.GetAllAsync();

        Assert.Contains(before, pack => !pack.IsBuiltIn);

        await harness.UserData.EraseAsync();

        IReadOnlyList<EventPack> after = await harness.EventPacks.GetAllAsync();

        Assert.DoesNotContain(after, pack => !pack.IsBuiltIn);
        Assert.Contains(after, pack => pack.IsBuiltIn);
    }
}
