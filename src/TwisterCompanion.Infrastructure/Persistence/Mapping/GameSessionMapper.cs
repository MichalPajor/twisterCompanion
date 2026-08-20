using TwisterCompanion.Application.Game;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.EventSelection;
using TwisterCompanion.Domain.MoveSelection;
using TwisterCompanion.Infrastructure.Persistence.Dto;

namespace TwisterCompanion.Infrastructure.Persistence.Mapping;

/// <summary>
/// Przekłada zapis partii między postacią plikową a modelem aplikacji.
/// </summary>
/// <remarks>
/// Jak przy pozostałych mapowaniach: dane z dysku są traktowane jak dane z zewnątrz.
/// Wpis bez nazwy gracza albo z nieznaną wartością wyliczeniową unieważnia cały zapis —
/// tutaj, w odróżnieniu od paczek wydarzeń, częściowe odtworzenie nie ma sensu.
/// Partia z brakującym graczem albo bez wskazania, kto ma turę, jest gorsza niż brak zapisu.
/// </remarks>
internal static class GameSessionMapper
{
    /// <summary>Buduje zapis do odtworzenia partii.</summary>
    /// <param name="dto">Odczytany dokument.</param>
    /// <returns>Zapis albo <see langword="null"/>, gdy danych nie da się użyć.</returns>
    public static GameSessionSnapshot? ToDomain(GameSessionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Players.Count == 0)
        {
            return null;
        }

        List<Player> players = [];

        foreach (SessionPlayerDto playerDto in dto.Players)
        {
            if (playerDto.Id == Guid.Empty
                || string.IsNullOrWhiteSpace(playerDto.Name)
                || playerDto.Order < 0)
            {
                return null;
            }

            players.Add(new Player
            {
                Id = playerDto.Id,
                Name = playerDto.Name,
                Order = playerDto.Order,
                IsEliminated = playerDto.IsEliminated,
            });
        }

        if (players.Select(player => player.Id).Distinct().Count() != players.Count)
        {
            return null;
        }

        if (!Enum.IsDefined(dto.State) || !Enum.IsDefined(dto.TurnAdvanceMode) || dto.TurnNumber < 0)
        {
            return null;
        }

        if (!TryMapMoves(dto.RecentMoves, out List<Move> recentMoves))
        {
            return null;
        }

        Dictionary<Guid, IReadOnlyDictionary<BodyPart, SpinColor>> limbPositions = [];

        foreach (PlayerLimbPositionsDto entry in dto.LimbPositions)
        {
            if (!TryMapMoves(entry.Positions, out List<Move> positions))
            {
                return null;
            }

            limbPositions[entry.PlayerId] = positions.ToDictionary(
                move => move.Part,
                move => move.Color);
        }

        return new GameSessionSnapshot
        {
            State = dto.State,
            TurnNumber = dto.TurnNumber,
            EventCount = Math.Max(0, dto.EventCount),
            Players = players,
            CurrentPlayerId = dto.CurrentPlayerId == Guid.Empty ? null : dto.CurrentPlayerId,
            EliminationOrder = [.. dto.EliminationOrder],
            RecentMoves = recentMoves,
            LimbPositions = limbPositions,
            LastEventTurn = dto.LastEventTurn,
            LastEventTurns = dto.LastEventTurns.ToDictionary(entry => entry.EventId, entry => entry.Turn),
            EventPack = dto.EventPack is null
                ? null
                : EventPackMapper.ToDomain(dto.EventPack, isBuiltIn: false),
            EventSelectionOptions = ToDomain(dto.EventSelectionOptions),
            StartedAt = dto.StartedAt,
            MoveSelectionOptions = ToDomain(dto.MoveSelectionOptions),
            TurnAdvanceMode = dto.TurnAdvanceMode,
            GameModeKey = string.IsNullOrWhiteSpace(dto.GameModeKey) ? "classic" : dto.GameModeKey.Trim(),
            EliminationRule = Enum.IsDefined(dto.EliminationRule) ? dto.EliminationRule : EliminationRule.Manual,
            NameAnnouncementPause =
                TimeSpan.FromMilliseconds(Math.Clamp(dto.NameAnnouncementPauseMilliseconds, 0, 10_000)),
            MoveTime = TimeSpan.FromSeconds(Math.Clamp(dto.MoveTimeSeconds, 1, 600)),
            TaskTime = TimeSpan.FromSeconds(Math.Clamp(dto.TaskTimeSeconds, 1, 600)),
        };
    }

    /// <summary>Buduje dokument do zapisu.</summary>
    /// <param name="snapshot">Stan partii.</param>
    public static GameSessionDto ToDto(GameSessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new GameSessionDto
        {
            SchemaVersion = PersistenceSchema.CurrentVersion,
            State = snapshot.State,
            TurnNumber = snapshot.TurnNumber,
            EventCount = snapshot.EventCount,
            Players =
            [
                .. snapshot.Players.Select(player => new SessionPlayerDto
                {
                    Id = player.Id,
                    Name = player.Name,
                    Order = player.Order,
                    IsEliminated = player.IsEliminated,
                }),
            ],
            CurrentPlayerId = snapshot.CurrentPlayerId,
            EliminationOrder = [.. snapshot.EliminationOrder],
            RecentMoves = [.. snapshot.RecentMoves.Select(ToDto)],
            LimbPositions =
            [
                .. snapshot.LimbPositions.Select(entry => new PlayerLimbPositionsDto
                {
                    PlayerId = entry.Key,
                    Positions = [.. entry.Value.Select(position => new MoveDto
                    {
                        Part = position.Key,
                        Color = position.Value,
                    })],
                }),
            ],
            LastEventTurn = snapshot.LastEventTurn,
            LastEventTurns =
            [
                .. snapshot.LastEventTurns.Select(entry => new EventTurnDto
                {
                    EventId = entry.Key,
                    Turn = entry.Value,
                }),
            ],
            EventPack = snapshot.EventPack is null ? null : EventPackMapper.ToDto(snapshot.EventPack),
            EventSelectionOptions = ToDto(snapshot.EventSelectionOptions),
            StartedAt = snapshot.StartedAt,
            MoveSelectionOptions = ToDto(snapshot.MoveSelectionOptions),
            TurnAdvanceMode = snapshot.TurnAdvanceMode,
            GameModeKey = snapshot.GameModeKey,
            EliminationRule = snapshot.EliminationRule,
            NameAnnouncementPauseMilliseconds = (int)snapshot.NameAnnouncementPause.TotalMilliseconds,
            MoveTimeSeconds = (int)snapshot.MoveTime.TotalSeconds,
            TaskTimeSeconds = (int)snapshot.TaskTime.TotalSeconds,
        };
    }

    private static bool TryMapMoves(List<MoveDto> source, out List<Move> result)
    {
        result = [];

        foreach (MoveDto moveDto in source)
        {
            if (!Enum.IsDefined(moveDto.Part) || !Enum.IsDefined(moveDto.Color))
            {
                return false;
            }

            result.Add(new Move(moveDto.Part, moveDto.Color));
        }

        return true;
    }

    private static MoveDto ToDto(Move move) => new() { Part = move.Part, Color = move.Color };

    private static MoveSelectionOptions ToDomain(MoveSelectionOptionsDto dto) => new()
    {
        TabooWindowSize = Math.Max(0, dto.TabooWindowSize),
        TabooWeightMultiplier = Math.Clamp(dto.TabooWeightMultiplier, 0.0, 1.0),
        RecencyDecay = Math.Clamp(dto.RecencyDecay, 0.0, 1.0),
        MaxSameBodyPartStreak = Math.Max(1, dto.MaxSameBodyPartStreak),
        SameBodyPartStreakMultiplier = Math.Clamp(dto.SameBodyPartStreakMultiplier, 0.0, 1.0),
        MaxSameColorStreak = Math.Max(1, dto.MaxSameColorStreak),
        SameColorStreakMultiplier = Math.Clamp(dto.SameColorStreakMultiplier, 0.0, 1.0),
        RedundantMoveMultiplier = Math.Clamp(dto.RedundantMoveMultiplier, 0.0, 1.0),
        HistoryLength = Math.Max(1, dto.HistoryLength),
    };

    private static EventSelectionOptions ToDomain(EventSelectionOptionsDto dto) => new()
    {
        ChanceMultiplier = Math.Clamp(dto.ChanceMultiplier, 0.0, 10.0),
    };

    private static EventSelectionOptionsDto ToDto(EventSelectionOptions options) => new()
    {
        ChanceMultiplier = options.ChanceMultiplier,
    };

    private static MoveSelectionOptionsDto ToDto(MoveSelectionOptions options) => new()
    {
        TabooWindowSize = options.TabooWindowSize,
        TabooWeightMultiplier = options.TabooWeightMultiplier,
        RecencyDecay = options.RecencyDecay,
        MaxSameBodyPartStreak = options.MaxSameBodyPartStreak,
        SameBodyPartStreakMultiplier = options.SameBodyPartStreakMultiplier,
        MaxSameColorStreak = options.MaxSameColorStreak,
        SameColorStreakMultiplier = options.SameColorStreakMultiplier,
        RedundantMoveMultiplier = options.RedundantMoveMultiplier,
        HistoryLength = options.HistoryLength,
    };
}
