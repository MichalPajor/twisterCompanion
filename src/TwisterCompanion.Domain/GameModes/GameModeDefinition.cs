using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.EventSelection;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Domain.GameModes;

/// <summary>
/// Tryb gry: zestaw nastaw, które zmieniają zachowanie silnika bez zmiany jego kodu.
/// </summary>
/// <remarks>
/// Tryb jest <b>danymi</b>, nie klasą. Dołożenie trybu to wpis w pliku definicji i dwa klucze
/// tłumaczeń — bez rekompilacji logiki, bez nowej gałęzi w silniku i bez nowej strategii
/// losowania. Wszystko, co tryb potrafi zmienić, jest wymienione w tym typie.
/// <para>
/// Teksty są kluczami zasobów, a nie treścią: nazwa, opis i zasady muszą być dostępne
/// w każdym języku aplikacji.
/// </para>
/// </remarks>
public sealed record GameModeDefinition
{
    /// <summary>Najmniejszy dopuszczalny mnożnik czasu.</summary>
    public const double MinTimeMultiplier = 0.1;

    /// <summary>Największy dopuszczalny mnożnik czasu.</summary>
    public const double MaxTimeMultiplier = 3.0;

    private readonly string _key = string.Empty;
    private readonly string _nameKey = string.Empty;
    private readonly double _moveTimeMultiplier = 1.0;
    private readonly double _taskTimeMultiplier = 1.0;

    /// <summary>Identyfikator trybu, zapisywany w ustawieniach.</summary>
    /// <exception cref="ArgumentException">Gdy klucz jest pusty.</exception>
    public required string Key
    {
        get => _key;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _key = value.Trim();
        }
    }

    /// <summary>Klucz zasobu z nazwą trybu.</summary>
    /// <exception cref="ArgumentException">Gdy klucz jest pusty.</exception>
    public required string NameKey
    {
        get => _nameKey;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _nameKey = value.Trim();
        }
    }

    /// <summary>Klucz zasobu z krótkim opisem trybu — jedno, dwa zdania na karcie wyboru.</summary>
    public string? DescriptionKey { get; init; }

    /// <summary>Klucz zasobu z pełnym opisem zasad.</summary>
    public string? RulesKey { get; init; }

    /// <summary>Parametry losowania ruchów.</summary>
    public MoveSelectionOptions MoveSelectionOptions { get; init; } = MoveSelectionOptions.Default;

    /// <summary>Parametry losowania wydarzeń.</summary>
    public EventSelectionOptions EventSelectionOptions { get; init; } = EventSelectionOptions.Default;

    /// <summary>
    /// Klucz nazwy paczki wydarzeń proponowanej przez ten tryb.
    /// </summary>
    /// <remarks>
    /// Propozycja, nie przymus: własny wybór gracza zawsze wygrywa. Tryb podpowiada paczkę
    /// tylko wtedy, gdy nikt nie wybrał żadnej — inaczej wejście w tryb Party kasowałoby
    /// paczkę, którą gracz właśnie sobie ułożył.
    /// </remarks>
    public string? DefaultEventPackNameKey { get; init; }

    /// <summary>
    /// Mnożnik czasu na wykonanie ruchu.
    /// </summary>
    /// <remarks>
    /// Mnożnik, a nie własna liczba sekund: użytkownik ustawia jeden czas, który mu pasuje,
    /// a tryb tylko go skaluje. Dzięki temu przestawienie czasu w ustawieniach działa
    /// we wszystkich trybach naraz, a proporcje między trybami zostają zachowane.
    /// <para>
    /// Hardcore skraca czas o połowę, tryb dla dzieci wydłuża go o połowę.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Gdy mnożnik jest poza zakresem 0,1–3,0.</exception>
    public double MoveTimeMultiplier
    {
        get => _moveTimeMultiplier;
        init => _moveTimeMultiplier = ValidateMultiplier(value);
    }

    /// <summary>Mnożnik czasu na wykonanie zadania z wydarzenia.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Gdy mnożnik jest poza zakresem 0,1–3,0.</exception>
    public double TaskTimeMultiplier
    {
        get => _taskTimeMultiplier;
        init => _taskTimeMultiplier = ValidateMultiplier(value);
    }

    /// <summary>Zasada odpadania graczy.</summary>
    public EliminationRule EliminationRule { get; init; } = EliminationRule.Manual;

    /// <summary>
    /// Czy tryb jest dostępny do wyboru.
    /// </summary>
    /// <remarks>
    /// Tryb wyłączony zostaje w definicjach, ale nie pokazuje się graczom. Powstało dla
    /// trybu imprezowego dla dorosłych: struktura jest gotowa, a decyzja o udostępnieniu
    /// treści należy do publikacji, nie do kodu.
    /// </remarks>
    public bool IsEnabled { get; init; } = true;

    /// <inheritdoc />
    public override string ToString() => Key;

    private static double ValidateMultiplier(double value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, MinTimeMultiplier);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxTimeMultiplier);

        return value;
    }
}
