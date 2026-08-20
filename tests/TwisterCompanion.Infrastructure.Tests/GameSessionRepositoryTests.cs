using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.MoveSelection;
using TwisterCompanion.Infrastructure.Tests.Fixtures;

namespace TwisterCompanion.Infrastructure.Tests;

/// <summary>
/// Testy zapisu przerwanej partii — realizacja kryterium „gra przetrwała minimalizację
/// aplikacji" po stronie persystencji.
/// </summary>
public class GameSessionRepositoryTests
{
    [Fact]
    public async Task LoadAsync_GdyBrakZapisu_ZwracaNull()
    {
        using TemporaryStorage storage = new();

        Assert.Null(await storage.GameSessions.LoadAsync());
    }

    [Fact]
    public async Task SaveAsync_NastepnieLoadAsync_ZwracaTeSameDane()
    {
        using TemporaryStorage storage = new();
        GameSessionSnapshot snapshot = CreateSnapshot();

        await storage.GameSessions.SaveAsync(snapshot);
        GameSessionSnapshot? odczytany = await storage.GameSessions.LoadAsync();

        Assert.NotNull(odczytany);
        Assert.Equal(snapshot.State, odczytany.State);
        Assert.Equal(snapshot.TurnNumber, odczytany.TurnNumber);
        Assert.Equal(snapshot.EventCount, odczytany.EventCount);
        Assert.Equal(snapshot.CurrentPlayerId, odczytany.CurrentPlayerId);
        Assert.Equal(snapshot.StartedAt, odczytany.StartedAt);
        Assert.Equal(snapshot.TurnAdvanceMode, odczytany.TurnAdvanceMode);
        Assert.Equal(snapshot.MoveTime, odczytany.MoveTime);
        Assert.Equal(snapshot.TaskTime, odczytany.TaskTime);
    }

    [Fact]
    public async Task SaveAsync_ZachowujeSkladWrazZEliminacjami()
    {
        using TemporaryStorage storage = new();
        GameSessionSnapshot snapshot = CreateSnapshot();

        await storage.GameSessions.SaveAsync(snapshot);
        GameSessionSnapshot odczytany = (await storage.GameSessions.LoadAsync())!;

        Assert.Equal(3, odczytany.Players.Count);
        Assert.Single(odczytany.Players, player => player.IsEliminated);
        Assert.Equal(snapshot.EliminationOrder, odczytany.EliminationOrder);
    }

    [Fact]
    public async Task SaveAsync_ZachowujeKolejnoscHistoriiRuchow()
    {
        // Kolejność jest pamięcią algorytmu losowania — odwrócona oznaczałaby, że po
        // wznowieniu gry najstarszy ruch uchodzi za najświeższy.
        using TemporaryStorage storage = new();
        GameSessionSnapshot snapshot = CreateSnapshot();

        await storage.GameSessions.SaveAsync(snapshot);
        GameSessionSnapshot odczytany = (await storage.GameSessions.LoadAsync())!;

        Assert.Equal(snapshot.RecentMoves, odczytany.RecentMoves);
    }

    [Fact]
    public async Task SaveAsync_ZachowujePozycjeKonczynGraczy()
    {
        using TemporaryStorage storage = new();
        GameSessionSnapshot snapshot = CreateSnapshot();
        Guid playerId = snapshot.Players[0].Id;

        await storage.GameSessions.SaveAsync(snapshot);
        GameSessionSnapshot odczytany = (await storage.GameSessions.LoadAsync())!;

        Assert.Equal(
            snapshot.LimbPositions[playerId],
            odczytany.LimbPositions[playerId]);
    }

    [Fact]
    public async Task SaveAsync_ZachowujeParametryLosowania()
    {
        // Bez tego wznowienie partii w trybie Hardcore (Etap 9) cofnęłoby losowanie
        // do nastaw domyślnych, co gracze odczuliby jako zmianę zasad w połowie gry.
        using TemporaryStorage storage = new();
        GameSessionSnapshot snapshot = CreateSnapshot() with
        {
            MoveSelectionOptions = MoveSelectionOptions.Default with
            {
                TabooWindowSize = 7,
                RedundantMoveMultiplier = 0.02,
                HistoryLength = 20,
            },
        };

        await storage.GameSessions.SaveAsync(snapshot);
        GameSessionSnapshot odczytany = (await storage.GameSessions.LoadAsync())!;

        Assert.Equal(7, odczytany.MoveSelectionOptions.TabooWindowSize);
        Assert.Equal(0.02, odczytany.MoveSelectionOptions.RedundantMoveMultiplier);
        Assert.Equal(20, odczytany.MoveSelectionOptions.HistoryLength);
    }

    [Fact]
    public async Task ClearAsync_UsuwaZapis()
    {
        using TemporaryStorage storage = new();
        await storage.GameSessions.SaveAsync(CreateSnapshot());

        await storage.GameSessions.ClearAsync();

        Assert.Null(await storage.GameSessions.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_GdyPlikJestUszkodzony_ZwracaNull()
    {
        using TemporaryStorage storage = new();
        storage.WriteRawSessionFile("{ to nie jest JSON");

        Assert.Null(await storage.GameSessions.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_GdyPlikMaNowszaWersjeSchematu_ZwracaNull()
    {
        using TemporaryStorage storage = new();
        storage.WriteRawSessionFile("""{ "schemaVersion": 999, "turnNumber": 5 }""");

        Assert.Null(await storage.GameSessions.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_GdyZapisNieMaGraczy_ZwracaNull()
    {
        // Partia bez składu jest gorsza niż brak zapisu — nie da się jej wznowić.
        using TemporaryStorage storage = new();
        storage.WriteRawSessionFile("""{ "schemaVersion": 1, "turnNumber": 5, "players": [] }""");

        Assert.Null(await storage.GameSessions.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_GdyGraczNieMaNazwy_ZwracaNull()
    {
        using TemporaryStorage storage = new();
        storage.WriteRawSessionFile(
            """
            {
              "schemaVersion": 1,
              "turnNumber": 3,
              "players": [ { "id": "11111111-1111-4111-8111-111111111111", "name": "", "order": 0 } ]
            }
            """);

        Assert.Null(await storage.GameSessions.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_GdyRuchMaNieznanaWartosc_ZwracaNull()
    {
        using TemporaryStorage storage = new();
        storage.WriteRawSessionFile(
            """
            {
              "schemaVersion": 1,
              "turnNumber": 3,
              "players": [ { "id": "11111111-1111-4111-8111-111111111111", "name": "Kuba", "order": 0 } ],
              "recentMoves": [ { "part": "RightHand", "color": "Fioletowy" } ]
            }
            """);

        Assert.Null(await storage.GameSessions.LoadAsync());
    }

    private static GameSessionSnapshot CreateSnapshot()
    {
        Player[] players =
        [
            Player.Create("Kuba", 0) with { IsEliminated = true },
            Player.Create("Anna", 1),
            Player.Create("Marek", 2),
        ];

        return new GameSessionSnapshot
        {
            State = GameState.AwaitingPlayerAction,
            TurnNumber = 12,
            EventCount = 3,
            Players = players,
            CurrentPlayerId = players[1].Id,
            EliminationOrder = [players[0].Id],
            RecentMoves =
            [
                new Move(BodyPart.LeftFoot, SpinColor.Green),
                new Move(BodyPart.RightHand, SpinColor.Red),
            ],
            LimbPositions = new Dictionary<Guid, IReadOnlyDictionary<BodyPart, SpinColor>>
            {
                [players[0].Id] = new Dictionary<BodyPart, SpinColor>
                {
                    [BodyPart.RightHand] = SpinColor.Red,
                    [BodyPart.LeftFoot] = SpinColor.Blue,
                },
            },
            StartedAt = new DateTimeOffset(2026, 7, 30, 18, 30, 0, TimeSpan.Zero),
            TurnAdvanceMode = TurnAdvanceMode.Automatic,
            MoveTime = TimeSpan.FromSeconds(15),
            TaskTime = TimeSpan.FromSeconds(25),
        };
    }
}
