using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Domain.EventSelection;

/// <summary>
/// Losowanie wydarzeń: najpierw rzut, czy wydarzenie w ogóle pada, potem wybór ważony
/// szansami poszczególnych wydarzeń.
/// </summary>
/// <remarks>
/// Decyzja jest rozdzielona na dwa kroki celowo. Suma szans wszystkich włączonych wydarzeń
/// mówi, <i>jak często</i> cokolwiek się dzieje, a proporcje między nimi — <i>co</i> się
/// wtedy dzieje. Jeden rzut nie dałby tego rozdziału: dodanie kolejnego wydarzenia
/// zmieniałoby częstotliwość i proporcje jednocześnie.
/// <para>
/// Suma szans powyżej 100% oznacza pewne wystąpienie wydarzenia — użytkownik ma prawo
/// ustawić dowolne wartości, a ekran paczek ostrzega, kiedy sumy przekraczają 100%.
/// </para>
/// <para>
/// Ograniczenia częstotliwości są <b>wyłącznie per wydarzenie</b>:
/// <list type="bullet">
/// <item><b>własny odstęp wydarzenia</b> — ile tur musi minąć od <i>tego</i> wydarzenia;</item>
/// <item><b>wydarzenie jednorazowe</b> — może paść tylko raz na partię.</item>
/// </list>
/// </para>
/// <para>
/// <b>Globalnego odstępu między wydarzeniami celowo nie ma.</b> Wcześniej istniał jako
/// „ochrona" przed lawiną wydarzeń, ale przy dwóch graczach działał rażąco niesprawiedliwie:
/// wydarzenia padały co drugą turę, więc trafiały wciąż tego samego gracza, a drugi nie
/// dostawał ich wcale. Częstotliwość jest wyborem gracza — sumą szans w zestawie i mnożnikiem
/// trybu — i aplikacja nie ma prawa jej po cichu nadpisywać.
/// </para>
/// </remarks>
public sealed class WeightedEventSelector(IRandomProvider randomProvider) : IEventSelector
{
    private readonly IRandomProvider _randomProvider =
        randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));

    /// <inheritdoc />
    public GameEvent? SelectNext(EventSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Pack is null || context.Options.ChanceMultiplier <= 0)
        {
            return null;
        }

        List<GameEvent> eligible = [.. context.Pack.EnabledEvents.Where(candidate => IsEligible(candidate, context))];

        if (eligible.Count == 0)
        {
            return null;
        }

        if (!RollWhetherEventOccurs(eligible, context))
        {
            return null;
        }

        return PickWeighted(eligible);
    }

    /// <summary>Czy konkretne wydarzenie może w tej turze wystąpić.</summary>
    private static bool IsEligible(GameEvent candidate, EventSelectionContext context)
    {
        if (candidate.Chance.IsNever)
        {
            return false;
        }

        if (!context.LastEventTurns.TryGetValue(candidate.Id, out int lastTurn))
        {
            return true;
        }

        // Wpis w historii oznacza, że wydarzenie już padło — dla jednorazowego to koniec.
        return !candidate.IsOneShot
            && context.TurnNumber - lastTurn >= candidate.CooldownTurns;
    }

    /// <summary>
    /// Rzut decydujący, czy w tej turze pada jakiekolwiek wydarzenie.
    /// </summary>
    private bool RollWhetherEventOccurs(List<GameEvent> eligible, EventSelectionContext context)
    {
        double totalChance = eligible.Sum(candidate => candidate.Chance.AsFraction)
                             * context.Options.ChanceMultiplier;

        // Ograniczenie do 1.0: suma powyżej 100% to pewne wystąpienie, a nie błąd.
        totalChance = Math.Min(1.0, totalChance);

        return _randomProvider.NextDouble() < totalChance;
    }

    /// <summary>Wybiera wydarzenie proporcjonalnie do jego szansy.</summary>
    private GameEvent PickWeighted(List<GameEvent> eligible)
    {
        double totalWeight = eligible.Sum(candidate => candidate.Chance.Percent);

        if (totalWeight <= 0)
        {
            return eligible[_randomProvider.Next(eligible.Count)];
        }

        double roll = _randomProvider.NextDouble() * totalWeight;
        double cumulative = 0;

        foreach (GameEvent candidate in eligible)
        {
            cumulative += candidate.Chance.Percent;

            if (roll < cumulative)
            {
                return candidate;
            }
        }

        // Domknięcie na wypadek błędu zaokrągleń przy sumowaniu.
        return eligible[^1];
    }
}
