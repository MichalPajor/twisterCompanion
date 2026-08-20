using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.MoveSelection;
using TwisterCompanion.Domain.Randomness;

namespace TwisterCompanion.Domain.Tests.MoveSelection;

/// <summary>
/// Testy zachowania inteligentnego losowania na konkretnych, ustalonych kontekstach.
/// </summary>
public class SmartMoveSelectionStrategyTests
{
    [Fact]
    public void Konstruktor_BezZrodlaLosowosci_RzucaArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new SmartMoveSelectionStrategy(null!));

    [Fact]
    public void SelectNext_BezKontekstu_RzucaArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => CreateStrategy().SelectNext(null!));

    [Fact]
    public void SelectNext_PrzyPustejHistorii_ZwracaPoprawnyRuch()
    {
        Move move = CreateStrategy().SelectNext(MoveSelectionContext.Initial());

        Assert.Contains(move, Move.All);
    }

    [Fact]
    public void SelectNext_NigdyNieZwracaRuchuZPoprzedniejTury()
    {
        // Twarda reguła algorytmu, niezależna od nastaw.
        Move previous = new(BodyPart.RightHand, SpinColor.Red);
        IMoveSelectionStrategy strategy = CreateStrategy();

        for (int i = 0; i < 500; i++)
        {
            Move move = strategy.SelectNext(new MoveSelectionContext { RecentMoves = [previous] });

            Assert.NotEqual(previous, move);
        }
    }

    [Fact]
    public void SelectNext_RuchBezczynny_WypadaZnaczniedRzadziej()
    {
        // Gracz ma już prawą rękę na czerwonym — powtórzenie tego polecenia byłoby
        // formalnie poprawne, ale zmarnowałoby turę.
        const int draws = 20_000;
        Move redundant = new(BodyPart.RightHand, SpinColor.Red);
        Dictionary<BodyPart, SpinColor> limbs = new() { [BodyPart.RightHand] = SpinColor.Red };
        IMoveSelectionStrategy strategy = CreateStrategy();

        int redundantCount = 0;

        for (int i = 0; i < draws; i++)
        {
            MoveSelectionContext context = new() { CurrentLimbPositions = limbs };

            if (strategy.SelectNext(context) == redundant)
            {
                redundantCount++;
            }
        }

        double share = (double)redundantCount / draws;
        const double uniformShare = 1.0 / Move.TotalCombinations;

        Assert.True(share < uniformShare / 2, $"Ruch bezczynny wypadł w {share:P2} losowań.");
    }

    [Fact]
    public void SelectNext_PoSeriiTejSamejKonczyny_PreferujeInneKonczyny()
    {
        const int draws = 5_000;
        Move[] history =
        [
            new(BodyPart.RightHand, SpinColor.Red),
            new(BodyPart.RightHand, SpinColor.Blue),
            new(BodyPart.RightHand, SpinColor.Green),
        ];
        IMoveSelectionStrategy strategy = CreateStrategy();

        int sameParts = 0;

        for (int i = 0; i < draws; i++)
        {
            if (strategy.SelectNext(new MoveSelectionContext { RecentMoves = history }).Part
                == BodyPart.RightHand)
            {
                sameParts++;
            }
        }

        double share = (double)sameParts / draws;

        Assert.True(share < 0.10, $"Ta sama kończyna po trzykrotnej serii wypadła w {share:P2} losowań.");
    }

    [Fact]
    public void SelectNext_TeSamoZiarno_DajeIdentycznaSekwencje()
    {
        IReadOnlyList<Move> pierwsza = MoveSequenceSimulator.Run(
            new SmartMoveSelectionStrategy(new SeededRandomProvider(seed: 555)),
            200);

        IReadOnlyList<Move> druga = MoveSequenceSimulator.Run(
            new SmartMoveSelectionStrategy(new SeededRandomProvider(seed: 555)),
            200);

        Assert.Equal(pierwsza, druga);
    }

    [Fact]
    public void SelectNext_InneZiarno_DajeInnaSekwencje()
    {
        IReadOnlyList<Move> pierwsza = MoveSequenceSimulator.Run(
            new SmartMoveSelectionStrategy(new SeededRandomProvider(seed: 1)),
            200);

        IReadOnlyList<Move> druga = MoveSequenceSimulator.Run(
            new SmartMoveSelectionStrategy(new SeededRandomProvider(seed: 2)),
            200);

        Assert.NotEqual(pierwsza, druga);
    }

    [Fact]
    public void SelectNext_GdyNastawyWykluczylybyWszystkieRuchy_NadalDzialaINiePowtarza()
    {
        // Skrajne nastawy: okno tabu obejmuje całą przestrzeń losowania, a kara wyklucza
        // ruch całkowicie. Po 16 losowaniach każdy ruch ma wagę zero. Algorytm musi wtedy
        // przejść w tryb awaryjny — gra nie może się zatrzymać.
        MoveSelectionOptions options = new()
        {
            TabooWindowSize = Move.TotalCombinations,
            TabooWeightMultiplier = 0,
            HistoryLength = Move.TotalCombinations,
        };

        IReadOnlyList<Move> moves = MoveSequenceSimulator.Run(CreateStrategy(), 60, options);

        Assert.Equal(60, moves.Count);
        Assert.Equal(0, MoveSequenceSimulator.CountImmediateRepeats(moves));
    }

    [Fact]
    public void SelectNext_ZWylaczonymiKarami_NadalNiePowtarzaPodRzad()
    {
        MoveSelectionOptions options = new()
        {
            TabooWindowSize = 0,
            RecencyDecay = 0,
            SameBodyPartStreakMultiplier = 1.0,
            SameColorStreakMultiplier = 1.0,
            RedundantMoveMultiplier = 1.0,
        };

        IReadOnlyList<Move> moves = MoveSequenceSimulator.Run(CreateStrategy(), 5_000, options);

        Assert.Equal(0, MoveSequenceSimulator.CountImmediateRepeats(moves));
    }

    private static IMoveSelectionStrategy CreateStrategy() =>
        new SmartMoveSelectionStrategy(new SeededRandomProvider(seed: 42));
}
