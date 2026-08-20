namespace TwisterCompanion.Domain.EventSelection;

/// <summary>
/// Parametry losowania wydarzeń.
/// </summary>
/// <remarks>
/// Wydzielone z algorytmu, żeby każdy tryb gry mógł mieć własne nastawy: Hardcore podnosi
/// mnożnik szans, tryb dla dzieci go obniża — bez mnożenia klas selektora.
/// <para>
/// Nie ma tu globalnego odstępu między wydarzeniami. Istniał, ale przy dwóch graczach
/// wydarzenia padały co drugą turę i trafiały wciąż tego samego gracza — ograniczenie
/// „chroniące" rozgrywkę psuło ją bardziej niż lawina wydarzeń, przed którą chroniło.
/// </para>
/// </remarks>
public sealed record EventSelectionOptions
{
    private readonly double _chanceMultiplier = 1.0;

    /// <summary>
    /// Mnożnik szans wszystkich wydarzeń — pozwala trybowi gry wzmocnić albo osłabić
    /// wydarzenia bez zmiany paczki.
    /// </summary>
    /// <remarks>Zero wyłącza wydarzenia całkowicie, na przykład w trybie Classic.</remarks>
    public double ChanceMultiplier
    {
        get => _chanceMultiplier;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 10.0);
            _chanceMultiplier = value;
        }
    }

    /// <summary>Nastawy domyślne.</summary>
    public static EventSelectionOptions Default { get; } = new();

    /// <summary>Nastawy wyłączające wydarzenia — tryb Classic.</summary>
    public static EventSelectionOptions Disabled { get; } = new() { ChanceMultiplier = 0.0 };
}
