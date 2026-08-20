using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Domain.Abstractions;

/// <summary>
/// Algorytm wybierający następny ruch.
/// </summary>
/// <remarks>
/// Wzorzec strategii. Silnik gry (Etap 5) zna wyłącznie ten interfejs, więc podmiana
/// algorytmu — czy to na klasyczny spinner dla trybu Classic, czy na coś zupełnie nowego
/// w przyszłości — sprowadza się do zmiany rejestracji w kontenerze.
/// <para>
/// Implementacje muszą być <b>bezstanowe</b>: cały kontekst przychodzi w
/// <see cref="MoveSelectionContext"/>. Dzięki temu mogą być singletonami i dają się
/// wywołać z dowolnego wątku — losowanie może zostać wywołane z UI albo z rozpoznawania
/// mowy (Etap 8).
/// </para>
/// </remarks>
public interface IMoveSelectionStrategy
{
    /// <summary>Wybiera następny ruch.</summary>
    /// <param name="context">Historia rozgrywki i parametry algorytmu.</param>
    /// <returns>Wylosowany ruch. Metoda nigdy nie zwraca wartości nieokreślonej.</returns>
    Move SelectNext(MoveSelectionContext context);
}
