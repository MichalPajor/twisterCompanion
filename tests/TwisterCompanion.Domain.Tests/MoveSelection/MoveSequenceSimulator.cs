using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Domain.Tests.MoveSelection;

/// <summary>
/// Rozgrywa serię losowań, prowadząc historię tak jak zrobi to silnik gry (Etap 5).
/// </summary>
/// <remarks>
/// Testy algorytmu muszą sprawdzać zachowanie <i>sekwencji</i>, a nie pojedynczego
/// wywołania — cała wartość inteligentnego losowania leży w tym, co dzieje się między
/// kolejnymi ruchami.
/// </remarks>
internal static class MoveSequenceSimulator
{
    /// <summary>Rozgrywa podaną liczbę losowań.</summary>
    /// <param name="strategy">Badany algorytm.</param>
    /// <param name="count">Liczba losowań.</param>
    /// <param name="options">Nastawy algorytmu.</param>
    /// <param name="limbPositions">Pozycje kończyn gracza, jeśli mają być brane pod uwagę.</param>
    public static IReadOnlyList<Move> Run(
        IMoveSelectionStrategy strategy,
        int count,
        MoveSelectionOptions? options = null,
        IReadOnlyDictionary<BodyPart, SpinColor>? limbPositions = null)
    {
        MoveSelectionOptions effectiveOptions = options ?? MoveSelectionOptions.Default;
        MoveHistory history = new(effectiveOptions.HistoryLength);
        List<Move> moves = new(count);

        for (int i = 0; i < count; i++)
        {
            MoveSelectionContext context = new()
            {
                RecentMoves = history.Snapshot(),
                Options = effectiveOptions,
                CurrentLimbPositions = limbPositions ?? new Dictionary<BodyPart, SpinColor>(),
            };

            Move move = strategy.SelectNext(context);

            moves.Add(move);
            history.Add(move);
        }

        return moves;
    }

    /// <summary>Liczy powtórzenia ruchu bezpośrednio po sobie.</summary>
    public static int CountImmediateRepeats(IReadOnlyList<Move> moves)
    {
        int repeats = 0;

        for (int i = 1; i < moves.Count; i++)
        {
            if (moves[i] == moves[i - 1])
            {
                repeats++;
            }
        }

        return repeats;
    }

    /// <summary>
    /// Zwraca udział losowań, w których ruch powtórzył się w oknie podanej długości.
    /// </summary>
    public static double MeasureRepeatRateWithinWindow(IReadOnlyList<Move> moves, int windowSize)
    {
        int repeats = 0;
        int considered = 0;

        for (int i = windowSize; i < moves.Count; i++)
        {
            considered++;

            for (int back = 1; back <= windowSize; back++)
            {
                if (moves[i] == moves[i - back])
                {
                    repeats++;
                    break;
                }
            }
        }

        return considered == 0 ? 0 : (double)repeats / considered;
    }

    /// <summary>
    /// Zwraca średnią liczbę różnych ruchów w przesuwnym oknie podanej długości.
    /// </summary>
    /// <remarks>
    /// To miara różnorodności, na której opiera się kryterium ukończenia Etapu 4.
    /// Zastępuje pierwotnie zapisane w planie kryterium „wyższej entropii", które było
    /// błędne: rozkład równomierny ma entropię maksymalną, więc żadne ograniczenie jej
    /// nie podniesie. Realnym celem jest to, żeby w krótkim okienku pojawiało się więcej
    /// różnych ruchów — i to jest tutaj mierzone.
    /// </remarks>
    public static double MeasureAverageDistinctMovesInWindow(IReadOnlyList<Move> moves, int windowSize)
    {
        if (moves.Count < windowSize)
        {
            return moves.Distinct().Count();
        }

        long total = 0;
        int windows = 0;

        for (int i = 0; i + windowSize <= moves.Count; i++)
        {
            HashSet<Move> distinct = [];

            for (int offset = 0; offset < windowSize; offset++)
            {
                distinct.Add(moves[i + offset]);
            }

            total += distinct.Count;
            windows++;
        }

        return (double)total / windows;
    }

    /// <summary>Zwraca najdłuższą serię ruchów spełniających ten sam warunek pod rząd.</summary>
    public static int MeasureLongestStreak<TKey>(IReadOnlyList<Move> moves, Func<Move, TKey> selector)
    {
        int longest = 0;
        int current = 0;
        TKey? previous = default;

        foreach (Move move in moves)
        {
            TKey key = selector(move);

            current = current > 0 && EqualityComparer<TKey>.Default.Equals(key, previous)
                ? current + 1
                : 1;

            previous = key;
            longest = Math.Max(longest, current);
        }

        return longest;
    }

    /// <summary>
    /// Zwraca udział procentowy poszczególnych wartości cechy w całej sekwencji.
    /// </summary>
    public static Dictionary<TKey, double> MeasureShare<TKey>(
        IReadOnlyList<Move> moves,
        Func<Move, TKey> selector)
        where TKey : notnull =>
        moves
            .GroupBy(selector)
            .ToDictionary(group => group.Key, group => (double)group.Count() / moves.Count);
}
