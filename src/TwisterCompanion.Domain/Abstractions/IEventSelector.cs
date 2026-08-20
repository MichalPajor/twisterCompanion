using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.EventSelection;

namespace TwisterCompanion.Domain.Abstractions;

/// <summary>
/// Decyduje, czy w danej turze wystąpi wydarzenie, i które.
/// </summary>
/// <remarks>
/// Osobna abstrakcja od losowania ruchów, bo to inna decyzja i inne reguły: ruch pada
/// w każdej turze, wydarzenie tylko czasem i z ograniczeniami częstotliwości.
/// <para>
/// Implementacje muszą być bezstanowe — cały kontekst przychodzi w
/// <see cref="EventSelectionContext"/>.
/// </para>
/// </remarks>
public interface IEventSelector
{
    /// <summary>Losuje wydarzenie dla rozgrywanej tury.</summary>
    /// <param name="context">Stan partii i parametry losowania.</param>
    /// <returns>Wylosowane wydarzenie albo <see langword="null"/>, gdy żadne nie pada.</returns>
    GameEvent? SelectNext(EventSelectionContext context);
}
