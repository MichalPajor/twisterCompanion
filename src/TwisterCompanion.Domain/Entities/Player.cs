namespace TwisterCompanion.Domain.Entities;

/// <summary>
/// Gracz biorący udział w rozgrywce.
/// </summary>
/// <remarks>
/// Walidacja siedzi w akcesorach <c>init</c>, a nie w konstruktorze. To celowe:
/// wyrażenie <c>with</c> omija konstruktor, więc walidacja w nim dałaby się obejść
/// jednym <c>player with { Name = "" }</c>.
/// </remarks>
public sealed record Player
{
    private readonly string _name = string.Empty;
    private readonly int _order;

    /// <summary>Identyfikator gracza, stały przez całą rozgrywkę.</summary>
    public required Guid Id { get; init; }

    /// <summary>Nazwa gracza — czytana na głos przy jego turze.</summary>
    /// <exception cref="ArgumentException">Gdy nazwa jest pusta lub sama z białych znaków.</exception>
    public required string Name
    {
        get => _name;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _name = value.Trim();
        }
    }

    /// <summary>Pozycja w kolejce graczy, liczona od zera.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Gdy pozycja jest ujemna.</exception>
    public int Order
    {
        get => _order;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _order = value;
        }
    }

    /// <summary>Czy gracz odpadł z rozgrywki.</summary>
    public bool IsEliminated { get; init; }

    /// <summary>Tworzy nowego gracza z wygenerowanym identyfikatorem.</summary>
    /// <param name="name">Nazwa gracza.</param>
    /// <param name="order">Pozycja w kolejce.</param>
    /// <returns>Nowy, aktywny gracz.</returns>
    public static Player Create(string name, int order) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Order = order,
    };

    /// <inheritdoc />
    public override string ToString() => IsEliminated ? $"{Name} (odpadł)" : Name;
}
