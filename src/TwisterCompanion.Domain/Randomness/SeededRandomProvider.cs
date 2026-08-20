using TwisterCompanion.Domain.Abstractions;

namespace TwisterCompanion.Domain.Randomness;

/// <summary>
/// Losowość powtarzalna — ten sam ziarno daje zawsze tę samą sekwencję.
/// </summary>
/// <remarks>
/// Podstawowe zastosowanie to testy algorytmów losowania: dzięki temu test sprawdza
/// zachowanie algorytmu, a nie szczęście. Przydaje się też przy diagnozowaniu zgłoszeń —
/// można odtworzyć dokładnie tę samą rozgrywkę.
/// <para>
/// <b>Nie jest bezpieczny wątkowo</b> — <see cref="Random"/> z ziarnem nie jest.
/// Nie rejestruj tej implementacji jako singletona w aplikacji.
/// </para>
/// </remarks>
public sealed class SeededRandomProvider : IRandomProvider
{
    private readonly Random _random;

    /// <summary>Tworzy źródło losowości o podanym ziarnie.</summary>
    /// <param name="seed">Ziarno wyznaczające sekwencję.</param>
    public SeededRandomProvider(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    /// <summary>Ziarno, z którego powstała sekwencja.</summary>
    public int Seed { get; }

    /// <inheritdoc />
    public int Next(int exclusiveMaximum) => _random.Next(exclusiveMaximum);

    /// <inheritdoc />
    public int Next(int inclusiveMinimum, int exclusiveMaximum) =>
        _random.Next(inclusiveMinimum, exclusiveMaximum);

    /// <inheritdoc />
    public double NextDouble() => _random.NextDouble();
}
