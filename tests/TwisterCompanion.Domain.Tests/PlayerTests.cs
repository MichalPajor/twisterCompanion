using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Domain.Tests;

/// <summary>
/// Testy niezmienników gracza.
/// </summary>
public class PlayerTests
{
    [Fact]
    public void Create_UstawiaNazweKolejnoscIAktywnosc()
    {
        Player player = Player.Create("Kuba", 2);

        Assert.Equal("Kuba", player.Name);
        Assert.Equal(2, player.Order);
        Assert.False(player.IsEliminated);
        Assert.NotEqual(Guid.Empty, player.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Nazwa_PustaLubZBialychZnakow_RzucaWyjatek(string nazwa) =>
        Assert.Throws<ArgumentException>(() => Player.Create(nazwa, 0));

    [Fact]
    public void Nazwa_JestObcinanaZBialychZnakow()
    {
        // Nazwa jest czytana na głos, a wiodące spacje potrafią zmienić brzmienie
        // wypowiedzi w niektórych silnikach mowy.
        Player player = Player.Create("  Anna  ", 0);

        Assert.Equal("Anna", player.Name);
    }

    [Fact]
    public void Kolejnosc_Ujemna_RzucaWyjatek() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Player.Create("Kuba", -1));

    [Fact]
    public void WyrazenieWith_ZPustaNazwa_RowniezRzucaWyjatek()
    {
        // Tu jest cała wartość walidacji w akcesorach init: konstruktor nie wystarcza,
        // bo "with" go omija i tworzy kopię bezpośrednio.
        Player player = Player.Create("Kuba", 0);

        Assert.Throws<ArgumentException>(() => player with { Name = "  " });
    }

    [Fact]
    public void WyrazenieWith_ZUjemnaKolejnoscia_RowniezRzucaWyjatek()
    {
        Player player = Player.Create("Kuba", 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => player with { Order = -5 });
    }

    [Fact]
    public void Rownosc_DzialaPoWartosciach()
    {
        Player player = Player.Create("Kuba", 0);
        Player kopia = player with { };

        Assert.Equal(player, kopia);
    }

    [Fact]
    public void Eliminacja_TworzyNowegoGraczaBezZmianyOryginalu()
    {
        Player player = Player.Create("Kuba", 0);

        Player odpadly = player with { IsEliminated = true };

        Assert.True(odpadly.IsEliminated);
        Assert.False(player.IsEliminated);
        Assert.Equal(player.Id, odpadly.Id);
    }
}
