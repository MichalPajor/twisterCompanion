namespace TwisterCompanion.Application.Game;

/// <summary>
/// Trwające odliczanie w turze.
/// </summary>
/// <param name="Kind">Czego dotyczy odliczanie.</param>
/// <param name="Total">Ile czasu przewidziano.</param>
/// <param name="StartedAt">Znacznik czasu rozpoczęcia, ze źródła czasu silnika.</param>
/// <remarks>
/// Silnik podaje <b>fakty</b> — co i od kiedy jest odmierzane — a nie liczbę pozostałych
/// sekund. Odświeżanie napisu na ekranie należy do warstwy prezentacji, bo tylko ona wie,
/// jak często ekran ma się przerysowywać.
/// </remarks>
public sealed record TurnCountdown(TurnCountdownKind Kind, TimeSpan Total, long StartedAt);

/// <summary>
/// Czego dotyczy odliczanie w turze.
/// </summary>
public enum TurnCountdownKind
{
    /// <summary>Czas na wykonanie zadania z wydarzenia.</summary>
    Task,

    /// <summary>Czas na wykonanie ruchu — tylko w trybie automatycznym.</summary>
    Move,
}
