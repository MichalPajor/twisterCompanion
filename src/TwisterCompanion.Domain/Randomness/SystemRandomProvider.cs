using TwisterCompanion.Domain.Abstractions;

namespace TwisterCompanion.Domain.Randomness;

/// <summary>
/// Losowość produkcyjna, oparta na <see cref="Random.Shared"/>.
/// </summary>
/// <remarks>
/// <see cref="Random.Shared"/> jest bezpieczny wątkowo, więc ta implementacja może być
/// zarejestrowana jako singleton. To istotne, bo losowanie może zostać wywołane
/// z wątku UI albo z wątku obsługującego rozpoznawanie mowy.
/// </remarks>
public sealed class SystemRandomProvider : IRandomProvider
{
    /// <inheritdoc />
    public int Next(int exclusiveMaximum) => Random.Shared.Next(exclusiveMaximum);

    /// <inheritdoc />
    public int Next(int inclusiveMinimum, int exclusiveMaximum) =>
        Random.Shared.Next(inclusiveMinimum, exclusiveMaximum);

    /// <inheritdoc />
    public double NextDouble() => Random.Shared.NextDouble();
}
