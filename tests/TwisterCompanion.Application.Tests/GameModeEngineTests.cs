using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.EventSelection;
using TwisterCompanion.Domain.GameModes;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy wpływu trybu gry na przebieg partii.
/// </summary>
/// <remarks>
/// Realizacja kryterium z planu: wybór trybu ma <b>realnie</b> zmieniać zachowanie silnika,
/// a nie tylko podpis na ekranie. Testy statystyczne używają tego samego ziarna losowości
/// dla obu trybów, więc różnica w wynikach pochodzi wyłącznie z nastaw trybu.
/// </remarks>
public class GameModeEngineTests
{
    /// <summary>Ile tur rozgrywać w próbach statystycznych.</summary>
    private const int Turns = 200;

    [Fact]
    public async Task Hardcore_DajeWiecejWydarzenNizParty()
    {
        // Ten sam plik paczki, to samo ziarno, ta sama liczba tur — różni się tylko tryb.
        int hardcore = await CountEventsAsync(BuildMode(
            "hardcore",
            new EventSelectionOptions { ChanceMultiplier = 2.5 }));

        int party = await CountEventsAsync(BuildMode(
            "party",
            new EventSelectionOptions { ChanceMultiplier = 1.8 }));

        Assert.True(
            hardcore > party,
            $"Hardcore dał {hardcore} wydarzeń, Party {party} — tryb nie zmienia losowania.");
    }

    [Fact]
    public async Task Classic_NieDajeZadnychWydarzen()
    {
        // Mnożnik zerowy wyłącza wydarzenia całkowicie, także przy paczce z wysokimi szansami.
        int events = await CountEventsAsync(BuildMode("classic", EventSelectionOptions.Disabled));

        Assert.Equal(0, events);
    }

    [Fact]
    public async Task TrybBezOdpadania_IgnorujeZgloszenieOdpadniecia()
    {
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(3) with
        {
            EliminationRule = EliminationRule.NoElimination,
        });

        await harness.Engine.EliminateCurrentPlayerAsync();

        Assert.Empty(harness.Engine.Session!.EliminationOrder);
        Assert.False(harness.Engine.IsEliminationEnabled);
    }

    [Fact]
    public async Task TrybZOdpadaniem_WyklucaGracza()
    {
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(3) with
        {
            EliminationRule = EliminationRule.Manual,
        });

        await harness.Engine.EliminateCurrentPlayerAsync();

        Assert.Single(harness.Engine.Session!.EliminationOrder);
        Assert.True(harness.Engine.IsEliminationEnabled);
    }

    [Fact]
    public async Task ZasadyTrybu_PrzezywajaWznowieniePartii()
    {
        // Wznowiona partia toczy się na zasadach, na jakich się zaczęła — nawet jeśli
        // w przerwie ktoś przestawił tryb gry.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(3) with
        {
            GameModeKey = "kids",
            EliminationRule = EliminationRule.NoElimination,
        });

        await harness.Engine.SaveSnapshotAsync();

        using GameTestHarness poWznowieniu = new();
        await poWznowieniu.SessionRepository.SaveAsync(
            (await harness.SessionRepository.LoadAsync())!);

        Assert.True(await poWznowieniu.Engine.TryRestoreAsync());
        Assert.False(poWznowieniu.Engine.IsEliminationEnabled);

        await poWznowieniu.Engine.ResumeAsync();
        await poWznowieniu.Engine.EliminateCurrentPlayerAsync();

        Assert.Empty(poWznowieniu.Engine.Session!.EliminationOrder);
    }

    [Fact]
    public void KonfiguracjaZTrybu_BierzeParametryLosowaniaWylacznieZTrybu()
    {
        // Rozdział wpływów: nastawy losowania należą do trybu, sposób przechodzenia tur
        // do użytkownika.
        MoveSelectionOptions moveOptions = new() { MaxSameColorStreak = 4 };

        GameModeDefinition mode = new()
        {
            Key = "test",
            NameKey = "GameMode_Classic_Name",
            MoveSelectionOptions = moveOptions,
            EventSelectionOptions = new EventSelectionOptions { ChanceMultiplier = 1.5 },
            MoveTimeMultiplier = 1.5,
            TaskTimeMultiplier = 2.0,
            EliminationRule = EliminationRule.NoElimination,
        };

        AppSettings settings = AppSettings.Default with
        {
            TurnAdvanceMode = TurnAdvanceMode.Automatic,
            MoveTime = TimeSpan.FromSeconds(10),
            TaskTime = TimeSpan.FromSeconds(10),
        };

        GameConfiguration configuration = GameConfiguration.FromSettings(
            GameTestHarness.CreatePlayers(2),
            settings,
            mode);

        Assert.Equal(4, configuration.MoveSelectionOptions.MaxSameColorStreak);
        Assert.Equal(1.5, configuration.EventSelectionOptions.ChanceMultiplier);
        Assert.Equal(EliminationRule.NoElimination, configuration.EliminationRule);
        Assert.Equal("test", configuration.GameModeKey);

        // Czasy z ustawień przeskalowane mnożnikami trybu.
        Assert.Equal(TimeSpan.FromSeconds(15), configuration.MoveTime);
        Assert.Equal(TimeSpan.FromSeconds(20), configuration.TaskTime);

        // Sposób przechodzenia tur zostaje przy użytkowniku.
        Assert.Equal(TurnAdvanceMode.Automatic, configuration.TurnAdvanceMode);
    }

    [Fact]
    public void TrybBezWlasnychMnoznikow_ZostawiaCzasyUstawione()
    {
        GameModeDefinition mode = new()
        {
            Key = "classic",
            NameKey = "GameMode_Classic_Name",
        };

        AppSettings settings = AppSettings.Default with
        {
            MoveTime = TimeSpan.FromSeconds(12),
            TaskTime = TimeSpan.FromSeconds(18),
        };

        GameConfiguration configuration = GameConfiguration.FromSettings(
            GameTestHarness.CreatePlayers(2),
            settings,
            mode);

        Assert.Equal(TimeSpan.FromSeconds(12), configuration.MoveTime);
        Assert.Equal(TimeSpan.FromSeconds(18), configuration.TaskTime);
    }

    [Fact]
    public void MnoznikTrybu_NieSchodziPonizejSekundy()
    {
        // Czas zerowy oznaczałby turę, która kończy się przed odczytaniem polecenia.
        GameModeDefinition mode = new()
        {
            Key = "test",
            NameKey = "GameMode_Classic_Name",
            MoveTimeMultiplier = GameModeDefinition.MinTimeMultiplier,
        };

        GameConfiguration configuration = GameConfiguration.FromSettings(
            GameTestHarness.CreatePlayers(2),
            AppSettings.Default with { MoveTime = AppSettings.MinMoveTime },
            mode);

        Assert.True(configuration.MoveTime >= TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void PrzySterowaniuGlosem_OdliczanieBierzeCzasPrzedNasluchem()
    {
        // Zero na ekranie i sygnał „mów teraz" opisują ten sam moment, więc muszą pochodzić
        // z tej samej wartości. Wcześniej odliczanie brało czas tury automatycznej i pokazywało
        // 10 sekund tam, gdzie mikrofon otwierał się po 5.
        AppSettings settings = AppSettings.Default with
        {
            TurnAdvanceMode = TurnAdvanceMode.Manual,
            IsVoiceControlEnabled = true,
            MoveTime = TimeSpan.FromSeconds(10),
            VoiceListeningDelay = TimeSpan.FromSeconds(5),
        };

        GameConfiguration configuration = GameConfiguration.FromSettings(
            GameTestHarness.CreatePlayers(2),
            settings,
            new GameModeDefinition
            {
                Key = "hardcore",
                NameKey = "GameMode_Hardcore_Name",
                MoveTimeMultiplier = 0.5,
            });

        // Bez mnożnika trybu: liczba musi zgadzać się z chwilą otwarcia mikrofonu co do sekundy.
        Assert.Equal(TimeSpan.FromSeconds(5), configuration.MoveTime);
    }

    [Fact]
    public void BezSterowaniaGlosem_OdliczanieBierzeCzasNaRuchZeSkalowaniem()
    {
        AppSettings settings = AppSettings.Default with
        {
            TurnAdvanceMode = TurnAdvanceMode.Manual,
            IsVoiceControlEnabled = false,
            MoveTime = TimeSpan.FromSeconds(10),
            VoiceListeningDelay = TimeSpan.FromSeconds(5),
        };

        GameConfiguration configuration = GameConfiguration.FromSettings(
            GameTestHarness.CreatePlayers(2),
            settings,
            new GameModeDefinition
            {
                Key = "hardcore",
                NameKey = "GameMode_Hardcore_Name",
                MoveTimeMultiplier = 0.5,
            });

        Assert.Equal(TimeSpan.FromSeconds(5), configuration.MoveTime);
    }

    [Fact]
    public async Task WydarzeniaMogaPadacWKolejnychTurach()
    {
        // Przy dwóch graczach globalny odstęp między wydarzeniami trafiałby wciąż tego samego
        // gracza. Zestaw z pewnym wydarzeniem musi dawać je w każdej turze.
        using GameTestHarness harness = new();
        EventPack pack = EventPack.Create("Pewniak", [GameEvent.CreateCustom("Zadanie", 100)]);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2) with { EventPack = pack });

        for (int turn = 0; turn < 5; turn++)
        {
            await harness.Engine.NextTurnAsync();
        }

        // Sześć tur, w każdej wydarzenie.
        Assert.Equal(6, harness.Engine.Session!.EventCount);
    }

    private static GameModeDefinition BuildMode(string key, EventSelectionOptions events) => new()
    {
        Key = key,
        NameKey = "GameMode_Classic_Name",
        EventSelectionOptions = events,
    };

    /// <summary>Rozgrywa ustaloną liczbę tur i zwraca liczbę wylosowanych wydarzeń.</summary>
    private static async Task<int> CountEventsAsync(GameModeDefinition mode)
    {
        using GameTestHarness harness = new(randomSeed: 4242);

        EventPack pack = EventPack.Create(
            "Próba",
            [
                GameEvent.CreateCustom("Zamiana miejsc", 20),
                GameEvent.CreateCustom("Obrót", 20),
            ]);

        GameConfiguration configuration = GameConfiguration.FromSettings(
            GameTestHarness.CreatePlayers(2),
            AppSettings.Default,
            mode,
            pack) with
        {
            NameAnnouncementPause = TimeSpan.Zero,
            TaskTime = TimeSpan.Zero,
        };

        await harness.Engine.StartAsync(configuration);

        for (int turn = 0; turn < Turns; turn++)
        {
            await harness.Engine.NextTurnAsync();
        }

        return harness.Engine.Session!.EventCount;
    }
}
