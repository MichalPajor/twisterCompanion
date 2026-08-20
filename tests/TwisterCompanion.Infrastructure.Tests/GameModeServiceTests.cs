using TwisterCompanion.Domain.GameModes;
using TwisterCompanion.Infrastructure.Tests.Fixtures;

namespace TwisterCompanion.Infrastructure.Tests;

/// <summary>
/// Testy wyboru trybu gry zapamiętywanego w ustawieniach.
/// </summary>
public class GameModeServiceTests : IDisposable
{
    private readonly TemporaryStorage _storage = new();

    [Fact]
    public async Task BezWyboru_ObowiazujeTrybKlasyczny()
    {
        GameModeDefinition mode = await _storage.GameModes.GetActiveAsync();

        Assert.Equal("classic", mode.Key);
    }

    [Fact]
    public async Task WybranyTryb_JestZapamietywanyMiedzyUruchomieniami()
    {
        await _storage.GameModes.SetActiveAsync("hardcore");

        // Drugi kontener na tym samym katalogu danych to odpowiednik ponownego uruchomienia.
        using TemporaryStorage poRestarcie = new(_storage.Root);
        await poRestarcie.Settings.LoadAsync();

        GameModeDefinition mode = await poRestarcie.GameModes.GetActiveAsync();

        Assert.Equal("hardcore", mode.Key);
    }

    [Fact]
    public async Task NieistniejacyTryb_JestOdrzucany()
    {
        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(
            () => _storage.GameModes.SetActiveAsync("nie-ma-takiego"));

        Assert.Equal("key", error.ParamName);
    }

    [Fact]
    public async Task TrybWylaczonyWUstawieniach_WracaNaDomyslnyIPoprawiaZapis()
    {
        // Odpowiada sytuacji, w której nowa wersja aplikacji wyłączyła tryb zapisany
        // przez użytkownika. Zapis poprawiamy od razu, żeby ekran wyboru pokazywał to,
        // co faktycznie obowiązuje w grze.
        await _storage.Settings.UpdateAsync(settings => settings with { GameModeKey = "drinking" });

        GameModeDefinition mode = await _storage.GameModes.GetActiveAsync();

        Assert.Equal("classic", mode.Key);
        Assert.Equal("classic", _storage.Settings.Current.GameModeKey);
    }

    [Fact]
    public void DostepneTryby_NieZawierajaWylaczonych()
    {
        Assert.DoesNotContain("drinking", _storage.GameModes.GetAvailable().Select(mode => mode.Key));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _storage.Dispose();
        GC.SuppressFinalize(this);
    }
}
