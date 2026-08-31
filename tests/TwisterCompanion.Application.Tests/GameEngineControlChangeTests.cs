using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy zmiany sposobu prowadzenia tury na trwającej partii.
/// </summary>
/// <remarks>
/// Tryb był zamrażany w chwili startu: pole konfiguracji dostawało wartość raz i nie miało
/// jak jej zmienić, bo silnik nie zna ustawień. Przełącznik na ekranie rozgrywki to zmienia,
/// a te testy pilnują tego, co przy takiej zmianie najłatwiej zepsuć — <b>odliczania</b>.
/// </remarks>
public class GameEngineControlChangeTests
{
    [Fact]
    public async Task ZmianaWTrakcieOdliczaniaRuchu_RestartujeJeNaNowymCzasie()
    {
        // Sedno wybranej reguły: zmiana obowiązuje natychmiast. Gracz sięga po przełącznik,
        // gdy bieżący sposób zawodzi — gdyby zmiana czekała na następną turę, przycisk
        // wyglądałby jak zepsuty.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(
            GameTestHarness.Configuration(2, TurnAdvanceMode.Manual, TimeSpan.FromSeconds(8)));
        await harness.Engine.NextTurnAsync();

        Assert.Equal(TurnCountdownKind.Move, harness.Engine.Countdown?.Kind);
        Assert.Equal(TimeSpan.FromSeconds(8), harness.Engine.Countdown?.Total);

        harness.TimeProvider.Advance(TimeSpan.FromSeconds(3));

        await harness.Engine.ChangeTurnControlAsync(TurnAdvanceMode.Automatic, TimeSpan.FromSeconds(20));

        TurnCountdown? odliczanie = harness.Engine.Countdown;

        Assert.Equal(TurnCountdownKind.Move, odliczanie?.Kind);
        Assert.Equal(TimeSpan.FromSeconds(20), odliczanie?.Total);

        // Liczy od zera, a nie od pozostałych pięciu sekund — inaczej liczba na ekranie
        // znaczyłaby co innego niż moment, w którym coś się wydarzy.
        Assert.Equal(harness.TimeProvider.GetTimestamp(), odliczanie?.StartedAt);
    }

    [Fact]
    public async Task ZmianaNaAutomatyczny_SprawiaZeTuraPrzechodziSama()
    {
        // Nie wystarczy, żeby zmieniła się liczba na ekranie — musi zmienić się skutek
        // dojścia jej do zera.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(
            GameTestHarness.Configuration(2, TurnAdvanceMode.Manual, TimeSpan.FromSeconds(5)));
        await harness.Engine.NextTurnAsync();

        int turaPrzed = harness.Engine.Session!.TurnNumber;

        await harness.Engine.ChangeTurnControlAsync(TurnAdvanceMode.Automatic, TimeSpan.FromSeconds(5));
        harness.TimeProvider.Advance(TimeSpan.FromSeconds(6));

        await WaitForAsync(() => harness.Engine.Session!.TurnNumber > turaPrzed);

        Assert.True(harness.Engine.Session!.TurnNumber > turaPrzed);
    }

    [Fact]
    public async Task ZmianaPrzedStartemPartii_NieRobiNic()
    {
        // Przełącznik jest widoczny także przed rozpoczęciem partii. Wtedy zmienia same
        // ustawienia, a silnik nie ma czego zmieniać — i nie ma prawa się o to wywrócić.
        using GameTestHarness harness = new();

        await harness.Engine.ChangeTurnControlAsync(TurnAdvanceMode.Automatic, TimeSpan.FromSeconds(20));

        Assert.Equal(GameState.Idle, harness.Engine.State);
        Assert.Null(harness.Engine.Countdown);
    }

    [Fact]
    public async Task ZmianaNaTenSamTryb_NieRestartujeOdliczania()
    {
        // Bez tego warunku każde dotknięcie przycisku, także przypadkowe podwójne, dawałoby
        // graczowi pełen czas od nowa.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(
            GameTestHarness.Configuration(2, TurnAdvanceMode.Manual, TimeSpan.FromSeconds(8)));
        await harness.Engine.NextTurnAsync();

        long poczatek = harness.Engine.Countdown!.StartedAt;
        harness.TimeProvider.Advance(TimeSpan.FromSeconds(3));

        await harness.Engine.ChangeTurnControlAsync(TurnAdvanceMode.Manual, TimeSpan.FromSeconds(8));

        Assert.Equal(poczatek, harness.Engine.Countdown!.StartedAt);
    }

    private static async Task WaitForAsync(Func<bool> warunek)
    {
        for (int proba = 0; proba < 100 && !warunek(); proba++)
        {
            await Task.Delay(10);
        }
    }
}
