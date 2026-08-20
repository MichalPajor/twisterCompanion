namespace TwisterCompanion.Domain.Abstractions;

/// <summary>
/// Źródło losowości używane przez algorytmy losowania ruchów i wydarzeń.
/// </summary>
/// <remarks>
/// Interfejs istnieje wyłącznie dla testowalności. Losowanie jest sercem tej aplikacji
/// i musi dać się sprawdzić deterministycznie — bez tej abstrakcji test „nigdy nie
/// powtarza tego samego ruchu dwa razy pod rząd" byłby testem losowym, a nie testem.
/// </remarks>
public interface IRandomProvider
{
    /// <summary>Zwraca losową liczbę z zakresu od zera (włącznie) do podanej granicy (wyłącznie).</summary>
    /// <param name="exclusiveMaximum">Górna granica, wyłączna. Musi być większa od zera.</param>
    int Next(int exclusiveMaximum);

    /// <summary>Zwraca losową liczbę z podanego zakresu.</summary>
    /// <param name="inclusiveMinimum">Dolna granica, włączna.</param>
    /// <param name="exclusiveMaximum">Górna granica, wyłączna.</param>
    int Next(int inclusiveMinimum, int exclusiveMaximum);

    /// <summary>Zwraca losową liczbę zmiennoprzecinkową z zakresu 0,0 (włącznie) do 1,0 (wyłącznie).</summary>
    double NextDouble();
}
