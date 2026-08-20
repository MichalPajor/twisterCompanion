using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Game;

/// <summary>
/// Podsumowanie zakończonej partii.
/// </summary>
/// <param name="PlayerCount">Liczba uczestników.</param>
/// <param name="TurnCount">Liczba rozegranych tur.</param>
/// <param name="EventCount">Liczba tur, w których wystąpiło wydarzenie.</param>
/// <param name="Duration">Czas trwania partii.</param>
/// <param name="EliminationOrder">Gracze w kolejności odpadania.</param>
/// <param name="Winner">Zwycięzca, jeśli został wyłoniony.</param>
public sealed record GameSummary(
    int PlayerCount,
    int TurnCount,
    int EventCount,
    TimeSpan Duration,
    IReadOnlyList<Player> EliminationOrder,
    Player? Winner);
