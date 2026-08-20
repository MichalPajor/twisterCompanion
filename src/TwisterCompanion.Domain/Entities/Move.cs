using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Domain.Entities;

/// <summary>
/// Pojedynczy wylosowany ruch: która część ciała i na jaki kolor.
/// </summary>
/// <param name="Part">Część ciała.</param>
/// <param name="Color">Kolor pola.</param>
/// <remarks>
/// Typ wartościowy, bo ruch jest porównywany po wartości — algorytm losowania
/// (Etap 4) trzyma okno ostatnich ruchów i sprawdza, czy nowy się w nim nie powtarza.
/// </remarks>
public readonly record struct Move(BodyPart Part, SpinColor Color)
{
    /// <summary>Liczba wszystkich możliwych ruchów: 4 części ciała × 4 kolory.</summary>
    public const int TotalCombinations = 16;

    /// <summary>Wszystkie możliwe ruchy — pełna przestrzeń losowania.</summary>
    /// <remarks>
    /// Wyliczone raz i zapamiętane. Algorytmy losowania (Etap 4) przechodzą tę listę
    /// przy każdym losowaniu, więc nie ma sensu budować jej za każdym razem.
    /// </remarks>
    public static IReadOnlyList<Move> All { get; } =
    [
        .. from part in Enum.GetValues<BodyPart>()
           from color in Enum.GetValues<SpinColor>()
           select new Move(part, color),
    ];

    /// <inheritdoc />
    public override string ToString() => $"{Part}/{Color}";
}
