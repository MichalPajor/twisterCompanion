namespace TwisterCompanion.Domain.Entities;

/// <summary>
/// Pojedyncza tura rozgrywki: kto, jaki ruch i czy wystąpiło wydarzenie.
/// </summary>
public sealed record Turn
{
    private readonly int _number;

    /// <summary>Numer tury, liczony od jednego.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Gdy numer jest mniejszy od jednego.</exception>
    public required int Number
    {
        get => _number;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _number = value;
        }
    }

    /// <summary>Gracz, którego jest tura.</summary>
    public required Player Player { get; init; }

    /// <summary>Wylosowany ruch.</summary>
    public required Move Move { get; init; }

    /// <summary>Wydarzenie, jeśli w tej turze wystąpiło.</summary>
    public GameEvent? Event { get; init; }

    /// <summary>Czy w tej turze wystąpiło wydarzenie.</summary>
    public bool HasEvent => Event is not null;

    /// <inheritdoc />
    public override string ToString() =>
        HasEvent
            ? $"#{Number} {Player.Name}: {Move} + {Event}"
            : $"#{Number} {Player.Name}: {Move}";
}
