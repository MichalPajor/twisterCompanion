using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Domain.Tests.MoveSelection;

/// <summary>
/// Testy okna przesuwnego historii ruchów.
/// </summary>
public class MoveHistoryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Konstruktor_DlugoscMniejszaNizJeden_RzucaWyjatek(int capacity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new MoveHistory(capacity));

    [Fact]
    public void NowaHistoria_JestPusta()
    {
        MoveHistory history = new(5);

        Assert.Equal(0, history.Count);
        Assert.Empty(history.Snapshot());
    }

    [Fact]
    public void Snapshot_ZwracaRuchyOdNajnowszegoDoNajstarszego()
    {
        // Kolejność jest częścią kontraktu: indeks powiększony o jeden to odległość
        // ruchu w przeszłość, na której opierają się kary algorytmu losowania.
        MoveHistory history = new(5);
        Move pierwszy = new(BodyPart.RightHand, SpinColor.Red);
        Move drugi = new(BodyPart.LeftFoot, SpinColor.Blue);

        history.Add(pierwszy);
        history.Add(drugi);

        Assert.Equal([drugi, pierwszy], history.Snapshot());
    }

    [Fact]
    public void Add_PoPrzekroczeniuDlugosci_WypychaNajstarszyRuch()
    {
        MoveHistory history = new(2);
        Move najstarszy = new(BodyPart.RightHand, SpinColor.Red);
        Move sredni = new(BodyPart.LeftHand, SpinColor.Blue);
        Move najnowszy = new(BodyPart.RightFoot, SpinColor.Green);

        history.Add(najstarszy);
        history.Add(sredni);
        history.Add(najnowszy);

        Assert.Equal(2, history.Count);
        Assert.Equal([najnowszy, sredni], history.Snapshot());
    }

    [Fact]
    public void Clear_UsuwaCalaHistorie()
    {
        MoveHistory history = new(3);
        history.Add(new Move(BodyPart.RightHand, SpinColor.Red));

        history.Clear();

        Assert.Equal(0, history.Count);
        Assert.Empty(history.Snapshot());
    }

    [Fact]
    public void Snapshot_JestKopiaNiepodatnaNaPozniejszeZmiany()
    {
        // Algorytm losowania nie może zobaczyć historii zmieniającej się pod nim
        // w trakcie obliczeń.
        MoveHistory history = new(3);
        history.Add(new Move(BodyPart.RightHand, SpinColor.Red));

        IReadOnlyList<Move> snapshot = history.Snapshot();
        history.Add(new Move(BodyPart.LeftHand, SpinColor.Blue));

        Assert.Single(snapshot);
    }
}
