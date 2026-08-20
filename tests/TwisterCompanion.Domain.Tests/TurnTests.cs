using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Domain.Tests;

/// <summary>
/// Testy tury rozgrywki.
/// </summary>
public class TurnTests
{
    [Fact]
    public void Tura_ZawieraGraczaIRuch()
    {
        Player player = Player.Create("Kuba", 0);
        Move move = new(BodyPart.RightHand, SpinColor.Red);

        Turn turn = new() { Number = 1, Player = player, Move = move };

        Assert.Equal(1, turn.Number);
        Assert.Equal(player, turn.Player);
        Assert.Equal(move, turn.Move);
        Assert.False(turn.HasEvent);
        Assert.Null(turn.Event);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NumerTury_MniejszyNizJeden_RzucaWyjatek(int numer) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Turn
        {
            Number = numer,
            Player = Player.Create("Kuba", 0),
            Move = new Move(BodyPart.LeftFoot, SpinColor.Blue),
        });

    [Fact]
    public void HasEvent_JestPrawdaGdyWydarzenieWystapilo()
    {
        Turn turn = new()
        {
            Number = 3,
            Player = Player.Create("Anna", 1),
            Move = new Move(BodyPart.LeftHand, SpinColor.Green),
            Event = GameEvent.CreateCustom("Zamiana miejsc", 5),
        };

        Assert.True(turn.HasEvent);
    }

    [Fact]
    public void Ruch_JestPorownywanyPoWartosci()
    {
        // Istotne dla algorytmu losowania (Etap 4), który trzyma okno ostatnich ruchów
        // i sprawdza, czy nowy ruch się w nim nie powtarza.
        Move pierwszy = new(BodyPart.RightHand, SpinColor.Red);
        Move drugi = new(BodyPart.RightHand, SpinColor.Red);

        Assert.Equal(pierwszy, drugi);
        Assert.Equal(pierwszy.GetHashCode(), drugi.GetHashCode());
    }

    [Fact]
    public void LiczbaKombinacjiRuchow_ZgadzaSieZLiczbaWartosciEnumow()
    {
        int czesciCiala = Enum.GetValues<BodyPart>().Length;
        int kolory = Enum.GetValues<SpinColor>().Length;

        Assert.Equal(Move.TotalCombinations, czesciCiala * kolory);
    }
}
