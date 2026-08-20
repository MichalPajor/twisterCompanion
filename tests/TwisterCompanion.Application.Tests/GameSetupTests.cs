using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.EventSelection;
using TwisterCompanion.Domain.GameModes;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy podsumowania zasad pokazywanego przed rozpoczęciem partii.
/// </summary>
/// <remarks>
/// Podsumowanie ma <b>obiecywać dokładnie to</b>, co silnik potem rozegra. Dlatego oprócz
/// samych reguł test pilnuje zgodności z <see cref="GameConfiguration"/> — dwa miejsca
/// liczące czasy rozjechałyby się przy pierwszej zmianie mnożnika trybu.
/// </remarks>
public class GameSetupTests
{
    [Fact]
    public void PrzySterowaniuGlosem_CzasNaRuchToCzasPrzedNasluchem()
    {
        // Zero na ekranie i sygnał „mów teraz" opisują ten sam moment.
        GameSetup setup = GameSetup.FromSettings(
            AppSettings.Default with
            {
                TurnAdvanceMode = TurnAdvanceMode.Manual,
                IsVoiceControlEnabled = true,
                MoveTime = TimeSpan.FromSeconds(10),
                VoiceListeningDelay = TimeSpan.FromSeconds(5),
            },
            Mode(moveMultiplier: 0.5));

        Assert.Equal(TimeSpan.FromSeconds(5), setup.MoveTime);
        Assert.True(setup.IsVoiceControlEnabled);
    }

    [Fact]
    public void WTrybieAutomatycznym_SterowanieGlosemNieDziala()
    {
        // Ustawienie może zostać włączone, ale w trybie automatycznym nasłuch się nie
        // uruchamia — podsumowanie musi mówić o partii, a nie o zawartości ustawień.
        GameSetup setup = GameSetup.FromSettings(
            AppSettings.Default with
            {
                TurnAdvanceMode = TurnAdvanceMode.Automatic,
                IsVoiceControlEnabled = true,
                MoveTime = TimeSpan.FromSeconds(10),
            },
            Mode(moveMultiplier: 0.5));

        Assert.False(setup.IsVoiceControlEnabled);
        Assert.Equal(TimeSpan.FromSeconds(5), setup.MoveTime);
    }

    [Fact]
    public void TrybBezWydarzen_ZglaszaBrakWydarzenNawetZWybranaPaczka()
    {
        EventPack pack = EventPack.Create("Próba", [GameEvent.CreateCustom("Zadanie", 50)]);

        GameSetup setup = GameSetup.FromSettings(
            AppSettings.Default,
            new GameModeDefinition
            {
                Key = "test",
                NameKey = "GameMode_Classic_Name",
                EventSelectionOptions = EventSelectionOptions.Disabled,
            },
            pack);

        Assert.False(setup.AreEventsEnabled);
        Assert.NotNull(setup.EventPack);
    }

    [Fact]
    public void BezPaczki_ZglaszaBrakWydarzen()
    {
        GameSetup setup = GameSetup.FromSettings(AppSettings.Default, Mode());

        Assert.False(setup.AreEventsEnabled);
        Assert.Null(setup.EventPack);
    }

    [Fact]
    public void ZWybranaPaczka_ZglaszaWydarzenia()
    {
        EventPack pack = EventPack.Create("Próba", [GameEvent.CreateCustom("Zadanie", 50)]);

        GameSetup setup = GameSetup.FromSettings(AppSettings.Default, Mode(), pack);

        Assert.True(setup.AreEventsEnabled);
        Assert.Same(pack, setup.EventPack);
    }

    [Fact]
    public void TrybBezOdpadania_TrafiaDoPodsumowania()
    {
        GameSetup setup = GameSetup.FromSettings(
            AppSettings.Default,
            new GameModeDefinition
            {
                Key = "kids",
                NameKey = "GameMode_Kids_Name",
                EliminationRule = EliminationRule.NoElimination,
            });

        Assert.Equal(EliminationRule.NoElimination, setup.EliminationRule);
    }

    [Fact]
    public void PodsumowanieIKonfiguracja_PodajaTeSameZasady()
    {
        // Jedno źródło wyliczeń: ekran przed grą nie może obiecać innych czasów niż te,
        // z jakimi ruszy silnik.
        AppSettings settings = AppSettings.Default with
        {
            MoveTime = TimeSpan.FromSeconds(12),
            TaskTime = TimeSpan.FromSeconds(18),
            TurnAdvanceMode = TurnAdvanceMode.Automatic,
        };

        GameModeDefinition mode = Mode(moveMultiplier: 0.5, taskMultiplier: 1.5);
        EventPack pack = EventPack.Create("Próba", [GameEvent.CreateCustom("Zadanie", 50)]);

        GameSetup setup = GameSetup.FromSettings(settings, mode, pack);

        GameConfiguration configuration = GameConfiguration.FromSettings(
            GameTestHarness.CreatePlayers(2),
            settings,
            mode,
            pack);

        Assert.Equal(configuration.MoveTime, setup.MoveTime);
        Assert.Equal(configuration.TaskTime, setup.TaskTime);
        Assert.Equal(configuration.TurnAdvanceMode, setup.TurnAdvanceMode);
        Assert.Equal(configuration.EliminationRule, setup.EliminationRule);
        Assert.Equal(configuration.GameModeKey, setup.GameModeKey);
        Assert.Same(configuration.EventPack, setup.EventPack);
    }

    private static GameModeDefinition Mode(
        double moveMultiplier = 1.0,
        double taskMultiplier = 1.0) => new()
        {
            Key = "test",
            NameKey = "GameMode_Classic_Name",
            MoveTimeMultiplier = moveMultiplier,
            TaskTimeMultiplier = taskMultiplier,
        };
}
