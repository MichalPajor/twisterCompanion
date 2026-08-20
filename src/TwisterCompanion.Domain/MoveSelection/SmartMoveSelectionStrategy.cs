using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Domain.MoveSelection;

/// <summary>
/// Losowanie inteligentne — ważone, z pamięcią ostatnich ruchów.
/// </summary>
/// <remarks>
/// Każdemu z 16 możliwych ruchów przypisywana jest waga startowa 1,0, następnie mnożona
/// przez kary wynikające z historii. Ruch jest wybierany losowo proporcjonalnie do wagi.
/// <para>
/// Kary, w kolejności stosowania:
/// <list type="number">
/// <item><b>Powtórzenie natychmiastowe</b> — waga 0. Ten sam ruch dwa razy pod rząd nie
/// wystąpi nigdy, niezależnie od nastaw.</item>
/// <item><b>Okno tabu</b> — ruch powtórzony w ostatnich <c>K</c> losowaniach dostaje
/// bardzo niską wagę. Możliwy, ale rzadki.</item>
/// <item><b>Kara za świeżość</b> — poza oknem tabu waga rośnie wraz z odległością:
/// <c>1 - decay^d</c>. Im dawniej ruch wystąpił, tym chętniej wraca.</item>
/// <item><b>Seria tej samej kończyny</b> — po kilku ruchach tą samą kończyną pod rząd
/// pozostałe stają się wyraźnie bardziej prawdopodobne.</item>
/// <item><b>Seria tego samego koloru</b> — jak wyżej, osobno dla kolorów.</item>
/// <item><b>Ruch bezczynny</b> — kończyna już stoi na tym kolorze, więc polecenie
/// niczego by nie zmieniło.</item>
/// </list>
/// </para>
/// <para>
/// Kary są symetryczne względem części ciała i kolorów, więc rozkład brzegowy pozostaje
/// równomierny — algorytm zwiększa różnorodność sekwencji, a nie faworyzuje wybranych pól.
/// Potwierdzają to testy statystyczne na 100 000 losowaniach.
/// </para>
/// </remarks>
public sealed class SmartMoveSelectionStrategy(IRandomProvider randomProvider) : IMoveSelectionStrategy
{
    private readonly IRandomProvider _randomProvider =
        randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));

    /// <inheritdoc />
    public Move SelectNext(MoveSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<Move> candidates = Move.All;
        double[] weights = new double[candidates.Count];
        double total = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            weights[i] = CalculateWeight(candidates[i], context);
            total += weights[i];
        }

        if (total <= 0)
        {
            // Zabezpieczenie na wypadek nastaw, które wykluczyłyby wszystko.
            // Losowanie musi zawsze coś zwrócić — gra nie może się zatrzymać.
            return SelectAnyExceptPrevious(context);
        }

        double roll = _randomProvider.NextDouble() * total;
        double cumulative = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += weights[i];

            if (roll < cumulative && weights[i] > 0)
            {
                return candidates[i];
            }
        }

        // Domknięcie na wypadek błędu zaokrągleń przy sumowaniu wag.
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            if (weights[i] > 0)
            {
                return candidates[i];
            }
        }

        return SelectAnyExceptPrevious(context);
    }

    private static double CalculateWeight(Move candidate, MoveSelectionContext context)
    {
        MoveSelectionOptions options = context.Options;
        IReadOnlyList<Move> recent = context.RecentMoves;

        int? distance = FindDistance(recent, candidate);

        if (distance == 1)
        {
            return 0;
        }

        double weight = 1.0;

        if (distance is int found)
        {
            weight *= found <= options.TabooWindowSize
                ? options.TabooWeightMultiplier
                : 1.0 - Math.Pow(options.RecencyDecay, found);
        }

        if (CountLeadingStreak(recent, move => move.Part == candidate.Part)
            >= options.MaxSameBodyPartStreak)
        {
            weight *= options.SameBodyPartStreakMultiplier;
        }

        if (CountLeadingStreak(recent, move => move.Color == candidate.Color)
            >= options.MaxSameColorStreak)
        {
            weight *= options.SameColorStreakMultiplier;
        }

        if (context.CurrentLimbPositions.TryGetValue(candidate.Part, out SpinColor currentColor)
            && currentColor == candidate.Color)
        {
            weight *= options.RedundantMoveMultiplier;
        }

        return weight;
    }

    /// <summary>
    /// Zwraca odległość ruchu w przeszłość, licząc od jednego, albo <see langword="null"/>,
    /// gdy ruch nie wystąpił w pamiętanej historii.
    /// </summary>
    private static int? FindDistance(IReadOnlyList<Move> recent, Move candidate)
    {
        for (int i = 0; i < recent.Count; i++)
        {
            if (recent[i] == candidate)
            {
                return i + 1;
            }
        }

        return null;
    }

    /// <summary>
    /// Liczy, ile najnowszych ruchów pod rząd spełnia warunek.
    /// </summary>
    private static int CountLeadingStreak(IReadOnlyList<Move> recent, Func<Move, bool> predicate)
    {
        int streak = 0;

        foreach (Move move in recent)
        {
            if (!predicate(move))
            {
                break;
            }

            streak++;
        }

        return streak;
    }

    private Move SelectAnyExceptPrevious(MoveSelectionContext context)
    {
        Move? previous = context.PreviousMove;

        if (previous is null)
        {
            return Move.All[_randomProvider.Next(Move.All.Count)];
        }

        // Ta sama para część ciała i kolor nie może wypaść dwa razy pod rząd nawet
        // w trybie awaryjnym — to jedyna twarda reguła tego algorytmu.
        Move[] allowed = [.. Move.All.Where(move => move != previous.Value)];

        return allowed[_randomProvider.Next(allowed.Length)];
    }
}
