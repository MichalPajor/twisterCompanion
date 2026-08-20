using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy składania komunikatów dla graczy.
/// </summary>
public class AnnouncementBuilderTests
{
    [Fact]
    public void BuildPlayerTurn_WywolujeGraczaPoImieniu()
    {
        // Imię pada osobno, przed poleceniem: gracz ma wiedzieć, że to jego kolej, zanim
        // usłyszy, co ma zrobić.
        using GameTestHarness harness = new();

        Announcement announcement = harness.AnnouncementBuilder.BuildPlayerTurn(Player.Create("Kuba", 0));

        Assert.Equal(AnnouncementKind.PlayerTurn, announcement.Kind);
        Assert.Equal("Kuba.", announcement.Text);
    }

    [Fact]
    public void BuildMove_SkladaKomunikatZCzesciCialaIKoloru()
    {
        using GameTestHarness harness = new();
        Turn turn = new()
        {
            Number = 1,
            Player = Player.Create("Kuba", 0),
            Move = new Move(BodyPart.RightHand, SpinColor.Red),
        };

        Announcement announcement = harness.AnnouncementBuilder.BuildMove(turn);

        Assert.Equal(AnnouncementKind.Move, announcement.Kind);

        // Bez imienia — podaje je osobny komunikat.
        Assert.Equal("Voice_BodyPart_RightHand — Voice_Color_Red.", announcement.Text);
        Assert.DoesNotContain("Kuba", announcement.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMove_UzywaKluczyZbudowanychZNazwWyliczen()
    {
        // Dodanie koloru albo części ciała nie wymaga zmiany kodu — wystarczy nowa wartość
        // wyliczenia i wpis w zasobach. Ten test pilnuje, że klucz powstaje z nazwy.
        using GameTestHarness harness = new();
        Turn turn = new()
        {
            Number = 1,
            Player = Player.Create("Anna", 0),
            Move = new Move(BodyPart.LeftFoot, SpinColor.Green),
        };

        string text = harness.AnnouncementBuilder.BuildMove(turn).Text;

        Assert.Contains("Voice_BodyPart_LeftFoot", text, StringComparison.Ordinal);
        Assert.Contains("Voice_Color_Green", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GetEventName_WydarzenieWlasne_ZwracaNazweUzytkownikaBezTlumaczenia()
    {
        using GameTestHarness harness = new();
        GameEvent gameEvent = GameEvent.CreateCustom("Zamiana miejsc", 5);

        Assert.Equal("Zamiana miejsc", harness.AnnouncementBuilder.GetEventName(gameEvent));
    }

    [Fact]
    public void GetEventName_WydarzenieWbudowane_ZwracaTlumaczenieKlucza()
    {
        using GameTestHarness harness = new();
        GameEvent gameEvent = GameEvent.CreateBuiltIn("Event_SwapPlaces", 5);

        Assert.Equal("Event_SwapPlaces", harness.AnnouncementBuilder.GetEventName(gameEvent));
    }

    [Fact]
    public void BuildEvent_ZapowiadaNazweWydarzenia()
    {
        using GameTestHarness harness = new();

        Announcement announcement = harness.AnnouncementBuilder.BuildEvent(
            GameEvent.CreateCustom("Zamiana miejsc", 5));

        Assert.Equal(AnnouncementKind.Event, announcement.Kind);
        Assert.Equal("Wydarzenie: Zamiana miejsc.", announcement.Text);
    }

    [Fact]
    public void BuildGameEnd_ZeZwyciezca_OglaszaWygranego()
    {
        using GameTestHarness harness = new();

        Announcement announcement = harness.AnnouncementBuilder.BuildGameEnd(Player.Create("Kuba", 0));

        Assert.Equal(AnnouncementKind.GameEnd, announcement.Kind);
        Assert.Equal("Wygrywa Kuba.", announcement.Text);
    }

    [Fact]
    public void BuildGameEnd_BezZwyciezcy_MowiTylkoOZakonczeniu()
    {
        // Tryb treningowy z jednym graczem nie ma wygranego.
        using GameTestHarness harness = new();

        Announcement announcement = harness.AnnouncementBuilder.BuildGameEnd(winner: null);

        Assert.Equal("Voice_Announce_GameEnd", announcement.Text);
    }

    [Fact]
    public void BuildPlayerEliminated_PodajeNazweGracza()
    {
        using GameTestHarness harness = new();

        Announcement announcement = harness.AnnouncementBuilder.BuildPlayerEliminated(
            Player.Create("Marek", 1));

        Assert.Equal(AnnouncementKind.PlayerEliminated, announcement.Kind);
        Assert.Equal("Marek odpada.", announcement.Text);
    }

    [Fact]
    public void BuildPausedIResumed_MajaWlasneRodzaje()
    {
        using GameTestHarness harness = new();

        Assert.Equal(AnnouncementKind.Paused, harness.AnnouncementBuilder.BuildPaused().Kind);
        Assert.Equal(AnnouncementKind.Resumed, harness.AnnouncementBuilder.BuildResumed().Kind);
    }
}
