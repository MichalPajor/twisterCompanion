using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.Presentation.Tests;

/// <summary>
/// Testy edycji wydarzenia na ekranie — w szczególności ręcznego wpisywania procentów.
/// </summary>
public class EventListItemTests
{
    [Theory]
    [InlineData("0,5")]
    [InlineData("0.5")]
    public void SzansaWpisanaRecznie_PrzyjmujeObaSeparatoryDziesietne(string wpisana)
    {
        // Klawiatura numeryczna podaje ten separator, który ma system, a gracz i tak wpisze
        // ten, który zna. Wpisane „0.5" przy polskich ustawieniach nie jest błędem
        // użytkownika, tylko skutkiem tego, na której klawiaturze akurat pisze.
        GameEvent zmienione = null!;
        EventListItem wiersz = new(
            GameEvent.CreateCustom("Wydarzenie", 10),
            "Wydarzenie",
            isEditable: true,
            item => zmienione = item.Model);

        wiersz.ChanceText = wpisana;

        Assert.Equal(0.5, wiersz.ChancePercent);
        Assert.NotNull(zmienione);
        Assert.Equal(0.5, zmienione.Chance.Percent);
    }

    [Fact]
    public void Utworzenie_PrzenosiWartosciZModelu()
    {
        GameEvent model = GameEvent.CreateCustom("Zamiana miejsc", 37);

        EventListItem item = new(model, "Zamiana miejsc", isEditable: true);

        Assert.Equal(37, item.ChancePercent);
        Assert.Equal("37", item.ChanceText);
        Assert.True(item.IsEnabled);
    }

    [Fact]
    public void Utworzenie_NieZglaszaZmiany()
    {
        // Inaczej samo wypełnienie listy wywołałoby zapis wszystkich wydarzeń.
        int zgloszenia = 0;

        _ = new EventListItem(
            GameEvent.CreateCustom("Test", 10),
            "Test",
            isEditable: true,
            _ => zgloszenia++);

        Assert.Equal(0, zgloszenia);
    }

    [Fact]
    public void WpisanieProcentow_UstawiaDowolnaWartosc()
    {
        // Wcześniej wartość dawała się zmieniać tylko skokowo co 5 punktów.
        EventListItem item = CreateItem(10, out _);

        item.ChanceText = "37";

        Assert.Equal(37, item.ChancePercent);
        Assert.Equal(37, item.Model.Chance.Percent);
    }

    [Fact]
    public void WpisanieProcentow_ZglaszaZmiane()
    {
        EventListItem item = CreateItem(10, out List<EventListItem> zgloszone);

        item.ChanceText = "42";

        Assert.Same(item, Assert.Single(zgloszone));
    }

    [Theory]
    [InlineData("150", 100)]
    [InlineData("999", 100)]
    [InlineData("-20", 0)]
    public void WpisanieProcentow_PozaZakresem_JestPrzycinane(string wpis, int oczekiwany)
    {
        EventListItem item = CreateItem(10, out _);

        item.ChanceText = wpis;

        Assert.Equal(oczekiwany, item.ChancePercent);

        // Pole jest poprawiane, żeby użytkownik zobaczył, co naprawdę zostało ustawione.
        Assert.Equal(oczekiwany.ToString(), item.ChanceText);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void WpisanieProcentow_TekstNiebedacyLiczba_JestIgnorowany(string wpis)
    {
        // Użytkownik jest w środku edycji — kasuje cyfry albo dopiero zaczyna wpisywać.
        EventListItem item = CreateItem(25, out List<EventListItem> zgloszone);

        item.ChanceText = wpis;

        Assert.Equal(25, item.ChancePercent);
        Assert.Empty(zgloszone);
    }

    [Fact]
    public void WpisanieTejSamejWartosci_NieZglaszaZmiany()
    {
        EventListItem item = CreateItem(25, out List<EventListItem> zgloszone);

        item.ChanceText = "25";

        Assert.Empty(zgloszone);
    }

    [Fact]
    public void PrzelacznikWlaczenia_ZglaszaZmianeIAktualizujeModel()
    {
        EventListItem item = CreateItem(10, out List<EventListItem> zgloszone);

        item.IsEnabled = false;

        Assert.False(item.Model.IsEnabled);
        Assert.Same(item, Assert.Single(zgloszone));
    }

    [Fact]
    public void WydarzenieTylkoDoOdczytu_IgnorujeZmiany()
    {
        // Paczki wbudowane są nietykalne — reguła obowiązuje także tutaj, a nie tylko
        // przez wyszarzenie kontrolek na ekranie.
        GameEvent model = GameEvent.CreateBuiltIn("Event_SwapPlaces", 5);
        List<EventListItem> zgloszone = [];
        EventListItem item = new(model, "Zamiana miejsc", isEditable: false, zgloszone.Add);

        item.ChanceText = "80";
        item.IsEnabled = false;

        Assert.Equal(5, item.Model.Chance.Percent);
        Assert.True(item.Model.IsEnabled);
        Assert.Empty(zgloszone);
    }

    private static EventListItem CreateItem(int chancePercent, out List<EventListItem> raised)
    {
        List<EventListItem> zgloszone = [];
        raised = zgloszone;

        return new EventListItem(
            GameEvent.CreateCustom("Test", chancePercent),
            "Test",
            isEditable: true,
            zgloszone.Add);
    }
}
