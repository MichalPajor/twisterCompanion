using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Domain.MoveSelection;

/// <summary>
/// Okno przesuwne ostatnich ruchów, o ustalonej długości.
/// </summary>
/// <remarks>
/// Silnik gry (Etap 5) trzyma jedną taką historię i podaje jej zawartość algorytmowi
/// losowania. Typ istnieje, żeby logika „pamiętaj ostatnie N i zapomnij resztę" była
/// w jednym miejscu i nie musiała być powtarzana przez każdego, kto tej historii
/// potrzebuje.
/// <para>
/// <b>Nie jest bezpieczny wątkowo</b> — właścicielem jest silnik gry, który operuje
/// na nim z jednego wątku.
/// </para>
/// </remarks>
public sealed class MoveHistory
{
    private readonly LinkedList<Move> _moves = new();

    /// <summary>Tworzy historię o podanej długości.</summary>
    /// <param name="capacity">Ile ostatnich ruchów pamiętać.</param>
    /// <exception cref="ArgumentOutOfRangeException">Gdy długość jest mniejsza od jednego.</exception>
    public MoveHistory(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        Capacity = capacity;
    }

    /// <summary>Maksymalna liczba pamiętanych ruchów.</summary>
    public int Capacity { get; }

    /// <summary>Liczba aktualnie pamiętanych ruchów.</summary>
    public int Count => _moves.Count;

    /// <summary>Zapamiętuje ruch, wypychając najstarszy po przekroczeniu długości.</summary>
    /// <param name="move">Wykonany ruch.</param>
    public void Add(Move move)
    {
        _moves.AddFirst(move);

        if (_moves.Count > Capacity)
        {
            _moves.RemoveLast();
        }
    }

    /// <summary>Czyści historię — na przykład przy rozpoczęciu nowej partii.</summary>
    public void Clear() => _moves.Clear();

    /// <summary>
    /// Zwraca migawkę historii, od najnowszego ruchu do najstarszego.
    /// </summary>
    /// <remarks>
    /// Kopia, a nie widok na wewnętrzną kolekcję — algorytm losowania nie może zobaczyć
    /// historii zmieniającej się pod nim w trakcie obliczeń.
    /// </remarks>
    public IReadOnlyList<Move> Snapshot() => [.. _moves];
}
