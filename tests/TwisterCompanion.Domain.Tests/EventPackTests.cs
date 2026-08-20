using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Domain.Tests;

/// <summary>
/// Testy paczek wydarzeń — sumowania szans i kopiowania paczek wbudowanych.
/// </summary>
public class EventPackTests
{
    [Fact]
    public void Create_TworzyPaczkeEdytowalnaZWlasnymIdentyfikatorem()
    {
        EventPack pack = EventPack.Create("Moja paczka");

        Assert.NotEqual(Guid.Empty, pack.Id);
        Assert.Equal("Moja paczka", pack.Name);
        Assert.False(pack.IsBuiltIn);
        Assert.Empty(pack.Events);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Nazwa_Pusta_RzucaWyjatek(string nazwa) =>
        Assert.Throws<ArgumentException>(() => EventPack.Create(nazwa));

    [Fact]
    public void EnabledEvents_PomijaWydarzeniaWylaczone()
    {
        EventPack pack = EventPack.Create("Test",
        [
            GameEvent.CreateCustom("Włączone", 10),
            GameEvent.CreateCustom("Wyłączone", 10) with { IsEnabled = false },
        ]);

        Assert.Equal("Włączone", Assert.Single(pack.EnabledEvents).CustomName);
    }

    [Fact]
    public void TotalEnabledChancePercent_SumujeTylkoWydarzeniaWlaczone()
    {
        EventPack pack = EventPack.Create("Test",
        [
            GameEvent.CreateCustom("A", 10),
            GameEvent.CreateCustom("B", 15),
            GameEvent.CreateCustom("C", 50) with { IsEnabled = false },
        ]);

        Assert.Equal(25, pack.TotalEnabledChancePercent);
    }

    [Fact]
    public void TotalEnabledChancePercent_MozePrzekroczycSto()
    {
        // Użytkownik ma prawo ustawić dowolne wartości. Ekran paczek (Etap 6) ostrzega,
        // a silnik wydarzeń traktuje taką sumę jako pewne wystąpienie któregoś wydarzenia.
        EventPack pack = EventPack.Create("Test",
        [
            GameEvent.CreateCustom("A", 80),
            GameEvent.CreateCustom("B", 70),
        ]);

        Assert.Equal(150, pack.TotalEnabledChancePercent);
    }

    [Fact]
    public void Duplicate_TworzyEdytowalnaKopieZNowymiIdentyfikatorami()
    {
        EventPack builtIn = new()
        {
            Id = Guid.NewGuid(),
            Name = "Party",
            NameKey = "EventPack_Party_Name",
            IsBuiltIn = true,
            Events = [GameEvent.CreateBuiltIn("Event_SwapPlaces", 5, EventScope.AllPlayers)],
        };

        EventPack kopia = builtIn.Duplicate("Party (kopia)");

        Assert.NotEqual(builtIn.Id, kopia.Id);
        Assert.Equal("Party (kopia)", kopia.Name);
        Assert.False(kopia.IsBuiltIn);
        Assert.Null(kopia.NameKey);
    }

    [Fact]
    public void Duplicate_NadajeNoweIdentyfikatoryTakzeWydarzeniom()
    {
        // Bez tego edycja kopii mogłaby kolidować z oryginałem przy zapisie.
        EventPack original = EventPack.Create("Oryginał",
        [
            GameEvent.CreateCustom("A", 5),
            GameEvent.CreateCustom("B", 5),
        ]);

        EventPack kopia = original.Duplicate("Kopia");

        Assert.Equal(original.Events.Count, kopia.Events.Count);
        Assert.Empty(kopia.Events.Select(e => e.Id).Intersect(original.Events.Select(e => e.Id)));
    }

    [Fact]
    public void Duplicate_ZachowujeUstawieniaWydarzen()
    {
        EventPack original = EventPack.Create("Oryginał",
        [
            GameEvent.CreateCustom("Wyłączone", 42, EventScope.Round) with { IsEnabled = false },
        ]);

        GameEvent skopiowane = Assert.Single(original.Duplicate("Kopia").Events);

        Assert.Equal("Wyłączone", skopiowane.CustomName);
        Assert.Equal(42, skopiowane.Chance.Percent);
        Assert.Equal(EventScope.Round, skopiowane.Scope);
        Assert.False(skopiowane.IsEnabled);
    }
}
