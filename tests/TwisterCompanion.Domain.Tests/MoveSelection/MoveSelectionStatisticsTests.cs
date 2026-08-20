using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.MoveSelection;
using TwisterCompanion.Domain.Randomness;

namespace TwisterCompanion.Domain.Tests.MoveSelection;

/// <summary>
/// Testy statystyczne obu algorytmów losowania — realizacja mierzalnych kryteriów
/// ukończenia Etapu 4.
/// </summary>
/// <remarks>
/// Sekwencje są liczone raz i współdzielone przez testy: 100 000 losowań dla każdego
/// algorytmu wystarcza do wniosków, a nie ma powodu powtarzać tej pracy w każdym teście.
/// Wszystkie sekwencje pochodzą z <see cref="SeededRandomProvider"/>, więc test, który
/// kiedyś nie przejdzie, będzie się dawał odtworzyć.
/// </remarks>
public class MoveSelectionStatisticsTests
{
    private const int SampleSize = 100_000;
    private const double MaxDeviationFromUniform = 0.03;

    private static readonly Lazy<IReadOnlyList<Move>> SmartSequence = new(() =>
        MoveSequenceSimulator.Run(
            new SmartMoveSelectionStrategy(new SeededRandomProvider(seed: 20260729)),
            SampleSize));

    private static readonly Lazy<IReadOnlyList<Move>> ClassicSequence = new(() =>
        MoveSequenceSimulator.Run(
            new ClassicMoveSelectionStrategy(new SeededRandomProvider(seed: 20260729)),
            SampleSize));

    [Fact]
    public void Inteligentne_NigdyNiePowtarzaTegoSamegoRuchuPodRzad() =>
        Assert.Equal(0, MoveSequenceSimulator.CountImmediateRepeats(SmartSequence.Value));

    [Fact]
    public void Klasyczne_PowtarzaRuchyPodRzad_BoTakDzialaPrawdziwySpinner()
    {
        // Nie jest to wada, a cel tej strategii. Test pilnuje, żeby ktoś przez pomyłkę
        // nie „poprawił" trybu Classic, odbierając mu charakter prawdziwego spinnera.
        int repeats = MoveSequenceSimulator.CountImmediateRepeats(ClassicSequence.Value);

        Assert.True(repeats > 0, "Losowanie klasyczne nie powtórzyło żadnego ruchu pod rząd.");
    }

    [Fact]
    public void Inteligentne_RozkladCzesciCialaJestRownomierny() =>
        AssertUniformShare(SmartSequence.Value, move => move.Part);

    [Fact]
    public void Inteligentne_RozkladKolorowJestRownomierny() =>
        AssertUniformShare(SmartSequence.Value, move => move.Color);

    [Fact]
    public void Klasyczne_RozkladCzesciCialaJestRownomierny() =>
        AssertUniformShare(ClassicSequence.Value, move => move.Part);

    [Fact]
    public void Klasyczne_RozkladKolorowJestRownomierny() =>
        AssertUniformShare(ClassicSequence.Value, move => move.Color);

    [Fact]
    public void Inteligentne_WykorzystujeWszystkieSzesnascieRuchow()
    {
        // Zabezpieczenie przed „martwymi polami": kary nie mogą wykluczyć żadnego ruchu
        // na stałe, bo część maty przestałaby być używana.
        Assert.Equal(Move.TotalCombinations, SmartSequence.Value.Distinct().Count());
    }

    [Fact]
    public void Inteligentne_PowtarzaRuchWOknieTabuZnaczniedRzadziejNizKlasyczne()
    {
        int window = MoveSelectionOptions.Default.TabooWindowSize;

        double smart = MoveSequenceSimulator.MeasureRepeatRateWithinWindow(SmartSequence.Value, window);
        double classic = MoveSequenceSimulator.MeasureRepeatRateWithinWindow(ClassicSequence.Value, window);

        Assert.True(smart < 0.03, $"Powtórzenia w oknie tabu: {smart:P2} — oczekiwano poniżej 3%.");
        Assert.True(classic > 0.10, $"Losowanie klasyczne powtarza tylko {classic:P2} — test odniesienia stracił sens.");
        Assert.True(smart < classic / 3, $"Inteligentne {smart:P2} vs klasyczne {classic:P2} — za mała różnica.");
    }

    [Fact]
    public void Inteligentne_DajeWiecejRoznychRuchowWKrotkimOknie()
    {
        // Właściwa miara „większej różnorodności" z założeń projektu.
        const int window = 8;

        double smart = MoveSequenceSimulator.MeasureAverageDistinctMovesInWindow(SmartSequence.Value, window);
        double classic = MoveSequenceSimulator.MeasureAverageDistinctMovesInWindow(ClassicSequence.Value, window);

        Assert.True(
            smart > classic + 0.5,
            $"Różnych ruchów w oknie {window}: inteligentne {smart:F2}, klasyczne {classic:F2}.");
    }

    [Fact]
    public void Inteligentne_OgraniczaSerieTejSamejCzesciCiala()
    {
        int smart = MoveSequenceSimulator.MeasureLongestStreak(SmartSequence.Value, move => move.Part);
        int classic = MoveSequenceSimulator.MeasureLongestStreak(ClassicSequence.Value, move => move.Part);

        Assert.True(smart <= 6, $"Najdłuższa seria tej samej kończyny: {smart}.");
        Assert.True(smart < classic, $"Inteligentne {smart} vs klasyczne {classic} — brak poprawy.");
    }

    [Fact]
    public void Inteligentne_OgraniczaSerieTegoSamegoKoloru()
    {
        int smart = MoveSequenceSimulator.MeasureLongestStreak(SmartSequence.Value, move => move.Color);
        int classic = MoveSequenceSimulator.MeasureLongestStreak(ClassicSequence.Value, move => move.Color);

        Assert.True(smart <= 8, $"Najdłuższa seria tego samego koloru: {smart}.");
        Assert.True(smart < classic, $"Inteligentne {smart} vs klasyczne {classic} — brak poprawy.");
    }

    private static void AssertUniformShare<TKey>(IReadOnlyList<Move> moves, Func<Move, TKey> selector)
        where TKey : notnull
    {
        Dictionary<TKey, double> shares = MoveSequenceSimulator.MeasureShare(moves, selector);
        double expected = 1.0 / shares.Count;

        Assert.Equal(4, shares.Count);

        foreach ((TKey key, double share) in shares)
        {
            Assert.True(
                Math.Abs(share - expected) <= MaxDeviationFromUniform,
                $"{key}: {share:P2}, oczekiwano {expected:P2} z tolerancją {MaxDeviationFromUniform:P0}.");
        }
    }
}
