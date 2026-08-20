using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Domain.Tests;

/// <summary>
/// Testy wydarzeń — rozróżnienia nazwy własnej od klucza zasobu i walidacji szansy.
/// </summary>
public class GameEventTests
{
    [Fact]
    public void CreateCustom_UstawiaNazweUzytkownikaBezKluczaZasobu()
    {
        GameEvent gameEvent = GameEvent.CreateCustom("Zamiana miejsc", 5, EventScope.AllPlayers);

        Assert.Equal("Zamiana miejsc", gameEvent.CustomName);
        Assert.Null(gameEvent.NameKey);
        Assert.True(gameEvent.HasCustomName);
        Assert.Equal(5, gameEvent.Chance.Percent);
        Assert.Equal(EventScope.AllPlayers, gameEvent.Scope);
        Assert.True(gameEvent.IsEnabled);
    }

    [Fact]
    public void CreateBuiltIn_UstawiaKluczZasobuBezNazwyUzytkownika()
    {
        GameEvent gameEvent = GameEvent.CreateBuiltIn("Event_SwapPlaces", 5);

        Assert.Equal("Event_SwapPlaces", gameEvent.NameKey);
        Assert.Null(gameEvent.CustomName);
        Assert.False(gameEvent.HasCustomName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCustom_PustaNazwa_RzucaWyjatek(string nazwa) =>
        Assert.Throws<ArgumentException>(() => GameEvent.CreateCustom(nazwa, 5));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateBuiltIn_PustyKlucz_RzucaWyjatek(string klucz) =>
        Assert.Throws<ArgumentException>(() => GameEvent.CreateBuiltIn(klucz, 5));

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void CreateCustom_SzansaPozaZakresem_RzucaWyjatek(int procent) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => GameEvent.CreateCustom("Test", procent));

    [Fact]
    public void Nazwa_JestObcinanaZBialychZnakow() =>
        Assert.Equal("Zamiana miejsc", GameEvent.CreateCustom("  Zamiana miejsc  ", 5).CustomName);

    [Fact]
    public void PustaNazwaUzytkownika_JestTraktowanaJakoBrak()
    {
        // Deserializacja pliku może dostarczyć pusty łańcuch zamiast wartości null.
        // Oba przypadki muszą znaczyć to samo, inaczej HasCustomName kłamie.
        GameEvent gameEvent = GameEvent.CreateBuiltIn("Event_SwapPlaces", 5) with { CustomName = "   " };

        Assert.Null(gameEvent.CustomName);
        Assert.False(gameEvent.HasCustomName);
    }

    [Fact]
    public void DomyslnaSzansa_ToZero() =>
        Assert.True(new GameEvent { Id = Guid.NewGuid(), NameKey = "Event_Test" }.Chance.IsNever);
}
