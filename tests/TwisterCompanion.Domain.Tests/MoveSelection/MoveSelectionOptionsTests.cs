using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Domain.Tests.MoveSelection;

/// <summary>
/// Testy nastaw algorytmu losowania.
/// </summary>
public class MoveSelectionOptionsTests
{
    [Fact]
    public void Default_MaSensowneWartosciPoczatkowe()
    {
        MoveSelectionOptions options = MoveSelectionOptions.Default;

        Assert.True(options.TabooWindowSize > 0);
        Assert.True(options.HistoryLength >= options.TabooWindowSize);
        Assert.InRange(options.TabooWeightMultiplier, 0.0, 1.0);
        Assert.InRange(options.RecencyDecay, 0.0, 1.0);
        Assert.InRange(options.RedundantMoveMultiplier, 0.0, 1.0);
        Assert.True(options.MaxSameBodyPartStreak >= 1);
        Assert.True(options.MaxSameColorStreak >= 1);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void TabooWeightMultiplier_PozaZakresem_RzucaWyjatek(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MoveSelectionOptions.Default with { TabooWeightMultiplier = value });

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void RecencyDecay_PozaZakresem_RzucaWyjatek(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MoveSelectionOptions.Default with { RecencyDecay = value });

    [Theory]
    [InlineData(-2.0)]
    [InlineData(3.0)]
    public void RedundantMoveMultiplier_PozaZakresem_RzucaWyjatek(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MoveSelectionOptions.Default with { RedundantMoveMultiplier = value });

    [Fact]
    public void TabooWindowSize_Ujemny_RzucaWyjatek() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MoveSelectionOptions.Default with { TabooWindowSize = -1 });

    [Fact]
    public void MaxSameBodyPartStreak_MniejszyNizJeden_RzucaWyjatek() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MoveSelectionOptions.Default with { MaxSameBodyPartStreak = 0 });

    [Fact]
    public void MaxSameColorStreak_MniejszyNizJeden_RzucaWyjatek() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MoveSelectionOptions.Default with { MaxSameColorStreak = 0 });

    [Fact]
    public void HistoryLength_MniejszyNizJeden_RzucaWyjatek() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MoveSelectionOptions.Default with { HistoryLength = 0 });

    [Fact]
    public void Zmiana_NieModyfikujeInstancjiZrodlowej()
    {
        MoveSelectionOptions original = MoveSelectionOptions.Default;

        MoveSelectionOptions zmienione = original with { TabooWindowSize = 7 };

        Assert.Equal(3, original.TabooWindowSize);
        Assert.Equal(7, zmienione.TabooWindowSize);
    }
}
