using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.ValueObjects;

namespace TwisterCompanion.Domain.Entities;

/// <summary>
/// Custom Event — wydarzenie, które może wystąpić w trakcie tury zamiast
/// albo obok zwykłego ruchu.
/// </summary>
/// <remarks>
/// Nazwa jest przechowywana na dwa sposoby. Wydarzenia z paczek wbudowanych mają
/// <see cref="NameKey"/> — klucz zasobu, dzięki czemu tłumaczą się na język aplikacji.
/// Wydarzenia dodane przez użytkownika mają <see cref="CustomName"/>, którego nie
/// tłumaczymy, bo użytkownik wpisał je w swoim języku.
/// </remarks>
public sealed record GameEvent
{
    private readonly string? _nameKey;
    private readonly string? _customName;
    private readonly int _cooldownTurns;

    /// <summary>Identyfikator wydarzenia, stały w obrębie paczki.</summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Klucz zasobu z nazwą wydarzenia — używany przez paczki wbudowane.
    /// </summary>
    public string? NameKey
    {
        get => _nameKey;
        init => _nameKey = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Nazwa wpisana przez użytkownika — używana przez wydarzenia własne.
    /// Ma pierwszeństwo nad <see cref="NameKey"/>.
    /// </summary>
    public string? CustomName
    {
        get => _customName;
        init => _customName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Szansa wystąpienia w pojedynczej turze.</summary>
    public Probability Chance { get; init; } = Probability.Never;

    /// <summary>Czy wydarzenie bierze udział w losowaniu.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Kogo dotyczy wydarzenie.</summary>
    public EventScope Scope { get; init; } = EventScope.CurrentPlayer;

    /// <summary>
    /// Czy wydarzenie może wystąpić tylko raz na partię.
    /// </summary>
    /// <remarks>
    /// Przydatne dla wydarzeń zmieniających zasady na stałe albo takich, których powtórzenie
    /// przestaje być zabawne.
    /// </remarks>
    public bool IsOneShot { get; init; }

    /// <summary>
    /// Ile tur musi minąć, zanim to konkretne wydarzenie może paść ponownie.
    /// </summary>
    /// <remarks>
    /// Niezależne od globalnego odstępu między wydarzeniami. Pozwala rzadkiemu, mocnemu
    /// wydarzeniu nie wracać zaraz po sobie, gdy inne mogą.
    /// </remarks>
    public int CooldownTurns
    {
        get => _cooldownTurns;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _cooldownTurns = value;
        }
    }

    /// <summary>
    /// Czy wydarzenie ma nazwę nadaną przez użytkownika, a nie klucz zasobu.
    /// </summary>
    public bool HasCustomName => CustomName is not null;

    /// <summary>Tworzy wydarzenie własne użytkownika.</summary>
    /// <param name="name">Nazwa wpisana przez użytkownika.</param>
    /// <param name="chancePercent">Szansa wystąpienia w procentach.</param>
    /// <param name="scope">Kogo dotyczy wydarzenie.</param>
    /// <returns>Nowe, włączone wydarzenie.</returns>
    /// <exception cref="ArgumentException">Gdy nazwa jest pusta.</exception>
    public static GameEvent CreateCustom(
        string name,
        double chancePercent,
        EventScope scope = EventScope.CurrentPlayer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new GameEvent
        {
            Id = Guid.NewGuid(),
            CustomName = name,
            Chance = new Probability(chancePercent),
            Scope = scope,
        };
    }

    /// <summary>Tworzy wydarzenie paczki wbudowanej, opisane kluczem zasobu.</summary>
    /// <param name="nameKey">Klucz zasobu z nazwą.</param>
    /// <param name="chancePercent">Szansa wystąpienia w procentach.</param>
    /// <param name="scope">Kogo dotyczy wydarzenie.</param>
    /// <returns>Nowe, włączone wydarzenie.</returns>
    /// <exception cref="ArgumentException">Gdy klucz jest pusty.</exception>
    public static GameEvent CreateBuiltIn(
        string nameKey,
        double chancePercent,
        EventScope scope = EventScope.CurrentPlayer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameKey);

        return new GameEvent
        {
            Id = Guid.NewGuid(),
            NameKey = nameKey,
            Chance = new Probability(chancePercent),
            Scope = scope,
        };
    }

    /// <inheritdoc />
    public override string ToString() => $"{CustomName ?? NameKey} ({Chance})";
}
