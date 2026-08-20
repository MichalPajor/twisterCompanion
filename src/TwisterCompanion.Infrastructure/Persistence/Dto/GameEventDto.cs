using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Infrastructure.Persistence.Dto;

/// <summary>
/// Postać wydarzenia zapisywana w pliku JSON.
/// </summary>
/// <remarks>
/// DTO jest celowo „głupie" — same właściwości, zero walidacji. Walidacja należy do
/// modelu domenowego i dzieje się przy mapowaniu. Gdyby DTO walidowało, uszkodzony
/// plik wywalałby deserializację, a chcemy móc taki plik rozpoznać i obsłużyć.
/// </remarks>
internal sealed class GameEventDto
{
    /// <summary>Identyfikator wydarzenia.</summary>
    public Guid Id { get; set; }

    /// <summary>Klucz zasobu z nazwą — dla wydarzeń wbudowanych.</summary>
    public string? NameKey { get; set; }

    /// <summary>Nazwa wpisana przez użytkownika.</summary>
    public string? CustomName { get; set; }

    /// <summary>Szansa wystąpienia w procentach.</summary>
    public double ChancePercent { get; set; }

    /// <summary>Czy wydarzenie bierze udział w losowaniu.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Kogo dotyczy wydarzenie.</summary>
    public EventScope Scope { get; set; } = EventScope.CurrentPlayer;

    /// <summary>Czy wydarzenie może wystąpić tylko raz na partię.</summary>
    public bool IsOneShot { get; set; }

    /// <summary>Ile tur musi minąć, zanim to wydarzenie może paść ponownie.</summary>
    public int CooldownTurns { get; set; }
}
