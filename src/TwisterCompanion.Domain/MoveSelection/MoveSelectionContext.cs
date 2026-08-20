using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Domain.MoveSelection;

/// <summary>
/// Wszystko, co algorytm losowania wie o stanie rozgrywki.
/// </summary>
/// <remarks>
/// Kontekst jest jedynym wejściem strategii — dzięki temu strategie są bezstanowe
/// i dają się testować pojedynczym wywołaniem, bez rozgrywania całej partii.
/// </remarks>
public sealed record MoveSelectionContext
{
    private readonly IReadOnlyList<Move> _recentMoves = [];
    private readonly IReadOnlyDictionary<BodyPart, SpinColor> _currentLimbPositions =
        new Dictionary<BodyPart, SpinColor>();

    /// <summary>
    /// Ostatnie ruchy, <b>od najnowszego do najstarszego</b>.
    /// </summary>
    /// <remarks>
    /// Kolejność „najnowszy pierwszy" jest tu istotna: indeks elementu, powiększony o jeden,
    /// jest wprost odległością ruchu w przeszłość, na której opierają się kary za świeżość
    /// i okno tabu. Pusta lista oznacza pierwsze losowanie w partii.
    /// </remarks>
    public IReadOnlyList<Move> RecentMoves
    {
        get => _recentMoves;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _recentMoves = value;
        }
    }

    /// <summary>
    /// Kolory, na których stoją poszczególne kończyny gracza wykonującego ruch.
    /// </summary>
    /// <remarks>
    /// Aplikacja zna te pozycje, bo sama je wcześniej ogłosiła. Brak wpisu dla kończyny
    /// oznacza, że gracz nie dostał jeszcze polecenia jej ustawienia.
    /// </remarks>
    public IReadOnlyDictionary<BodyPart, SpinColor> CurrentLimbPositions
    {
        get => _currentLimbPositions;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _currentLimbPositions = value;
        }
    }

    /// <summary>Parametry algorytmu, zależne od trybu gry.</summary>
    public MoveSelectionOptions Options { get; init; } = MoveSelectionOptions.Default;

    /// <summary>Ruch wykonany bezpośrednio przed obecnym losowaniem.</summary>
    public Move? PreviousMove => RecentMoves.Count > 0 ? RecentMoves[0] : null;

    /// <summary>Kontekst pierwszego losowania w partii.</summary>
    /// <param name="options">Parametry algorytmu.</param>
    public static MoveSelectionContext Initial(MoveSelectionOptions? options = null) => new()
    {
        Options = options ?? MoveSelectionOptions.Default,
    };
}
