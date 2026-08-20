using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Domain.EventSelection;

/// <summary>
/// Wszystko, co algorytm losowania wydarzeń wie o stanie partii.
/// </summary>
public sealed record EventSelectionContext
{
    private readonly IReadOnlyDictionary<Guid, int> _lastEventTurns = new Dictionary<Guid, int>();

    /// <summary>Aktywna paczka wydarzeń albo <see langword="null"/>, gdy żadnej nie wybrano.</summary>
    public EventPack? Pack { get; init; }

    /// <summary>Numer rozgrywanej tury, liczony od jednego.</summary>
    public required int TurnNumber { get; init; }

    /// <summary>
    /// Numer tury, w której padło poprzednie wydarzenie. <see langword="null"/>, gdy
    /// jeszcze żadne nie padło.
    /// </summary>
    public int? LastEventTurn { get; init; }

    /// <summary>
    /// Numery tur, w których padły poszczególne wydarzenia — klucz to identyfikator
    /// wydarzenia.
    /// </summary>
    /// <remarks>
    /// Obsługuje dwie rzeczy naraz: własny odstęp konkretnego wydarzenia oraz wydarzenia
    /// jednorazowe, dla których sama obecność wpisu oznacza, że już wystąpiły.
    /// </remarks>
    public IReadOnlyDictionary<Guid, int> LastEventTurns
    {
        get => _lastEventTurns;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _lastEventTurns = value;
        }
    }

    /// <summary>Parametry losowania wydarzeń.</summary>
    public EventSelectionOptions Options { get; init; } = EventSelectionOptions.Default;
}
