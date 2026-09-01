using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy zapominania zakończonej partii.
/// </summary>
/// <remarks>
/// Zgłoszone z fazy testów: po zakończeniu gry, wyjściu do menu i powrocie na ekran
/// rozgrywki nadal widniało podsumowanie poprzedniej partii zamiast zasad nowej. Zapis na
/// dysku był w porządku — zakończonej partii nigdy się nie zapisuje — ale silnik żyje tyle,
/// co aplikacja, więc trzymał ją w pamięci.
/// </remarks>
public class GameEngineResetTests
{
    [Fact]
    public async Task ZapomnienieZakonczonejPartii_WracaDoStanuSprzedGry()
    {
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.NextTurnAsync();
        await harness.Engine.EndAsync();

        Assert.Equal(GameState.Finished, harness.Engine.State);

        await harness.Engine.ResetAsync();

        Assert.Equal(GameState.Idle, harness.Engine.State);
        Assert.Null(harness.Engine.Session);
        Assert.Null(harness.Engine.Countdown);
        Assert.Null(harness.Engine.LastAnnouncement);
    }

    [Fact]
    public async Task ZapomnienieZakonczonejPartii_ZglaszaZmianeStanu()
    {
        // Ekran rozgrywki przerysowuje się na zdarzeniu zmiany stanu. Bez zgłoszenia
        // podsumowanie zostałoby na ekranie mimo pustej pamięci silnika.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.EndAsync();

        List<GameState> zgloszone = [];
        harness.Engine.StateChanged += (_, state) => zgloszone.Add(state);

        await harness.Engine.ResetAsync();

        Assert.Contains(GameState.Idle, zgloszone);
    }

    [Fact]
    public async Task ZapomnienieBezPartii_NieRobiNic()
    {
        using GameTestHarness harness = new();

        await harness.Engine.ResetAsync();

        Assert.Equal(GameState.Idle, harness.Engine.State);
    }

    [Fact]
    public async Task PoZapomnieniu_DaSieRozpoczacNowaPartie()
    {
        // Sedno: gracz wraca na ekran rozgrywki i ma zobaczyć zasady nowej gry, a nie
        // wynik poprzedniej.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.EndAsync();
        await harness.Engine.ResetAsync();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(3));

        Assert.Equal(GameState.AwaitingPlayerAction, harness.Engine.State);
        Assert.Equal(3, harness.Engine.Session?.Players.Count);
    }
}
