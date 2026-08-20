using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Domain.Entities;

/// <summary>
/// Stan partii w postaci pozwalającej ją odtworzyć.
/// </summary>
/// <remarks>
/// Obiekt zamiast długiej listy parametrów <see cref="GameSession.RestoreFrom"/>: pola
/// stanu partii dochodzą z kolejnymi etapami, a rozszerzanie sygnatury metody wymuszałoby
/// zmianę każdego wywołania i każdego testu.
/// <para>
/// Opisuje wyłącznie stan <b>partii</b>. Rzeczy należące do silnika — chwila rozpoczęcia,
/// nastawy losowania, tryb postępu tur — są częścią zapisu w warstwie aplikacji, a nie tego
/// typu.
/// </para>
/// </remarks>
public sealed record GameSessionRestorePoint
{
    /// <summary>Stan rozgrywki.</summary>
    public required GameState State { get; init; }

    /// <summary>Numer ostatniej rozegranej tury.</summary>
    public required int TurnNumber { get; init; }

    /// <summary>Gracz, którego była tura.</summary>
    public Guid? CurrentPlayerId { get; init; }

    /// <summary>Liczba tur, w których wystąpiło wydarzenie.</summary>
    public int EventCount { get; init; }

    /// <summary>Numer tury, w której padło poprzednie wydarzenie.</summary>
    public int? LastEventTurn { get; init; }

    /// <summary>Numery tur, w których padły poszczególne wydarzenia.</summary>
    public IReadOnlyDictionary<Guid, int> LastEventTurns { get; init; } = new Dictionary<Guid, int>();

    /// <summary>Identyfikatory graczy w kolejności odpadania.</summary>
    public IReadOnlyList<Guid> EliminationOrder { get; init; } = [];

    /// <summary>Ostatnie ruchy, od najnowszego.</summary>
    public IReadOnlyList<Move> RecentMoves { get; init; } = [];

    /// <summary>Pozycje kończyn poszczególnych graczy.</summary>
    public IReadOnlyDictionary<Guid, IReadOnlyDictionary<BodyPart, SpinColor>> LimbPositions { get; init; } =
        new Dictionary<Guid, IReadOnlyDictionary<BodyPart, SpinColor>>();
}
