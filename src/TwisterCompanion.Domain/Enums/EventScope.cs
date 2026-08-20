namespace TwisterCompanion.Domain.Enums;

/// <summary>
/// Zasięg wydarzenia — kogo dotyczy, gdy zostanie wylosowane.
/// </summary>
public enum EventScope
{
    /// <summary>Dotyczy gracza, którego jest tura.</summary>
    CurrentPlayer,

    /// <summary>Dotyczy wszystkich graczy jednocześnie (np. „Zamiana miejsc").</summary>
    AllPlayers,

    /// <summary>Zmienia zasady na czas całej rundy (np. „Runda w zwolnionym tempie").</summary>
    Round,
}
