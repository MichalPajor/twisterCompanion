using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Domain.Tests;

/// <summary>
/// Testy reguł partii: kolejki graczy, przejść stanu, eliminacji i warunku końca gry.
/// </summary>
public class GameSessionTests
{
    private static readonly Move AnyMove = new(BodyPart.RightHand, SpinColor.Red);

    [Fact]
    public void NowaPartia_JestWStanieIdle()
    {
        GameSession session = CreateSession(2);

        Assert.Equal(GameState.Idle, session.State);
        Assert.Equal(0, session.TurnNumber);
        Assert.Null(session.CurrentPlayer);
        Assert.False(session.IsRunning);
    }

    [Fact]
    public void Konstruktor_BezGraczy_RzucaWyjatek() =>
        Assert.Throws<ArgumentException>(() => new GameSession([], 12));

    [Fact]
    public void Konstruktor_ZPowtorzonymIdentyfikatorem_RzucaWyjatek()
    {
        Player player = Player.Create("Kuba", 0);

        Assert.Throws<ArgumentException>(() => new GameSession([player, player with { Order = 1 }], 12));
    }

    [Fact]
    public void Gracze_SaUporzadkowaniPoKolejnosci()
    {
        GameSession session = new(
            [Player.Create("Trzeci", 2), Player.Create("Pierwszy", 0), Player.Create("Drugi", 1)],
            12);

        Assert.Equal(["Pierwszy", "Drugi", "Trzeci"], session.Players.Select(player => player.Name));
    }

    [Fact]
    public void Start_ZmieniaStanNaStarting()
    {
        GameSession session = CreateSession(2);

        session.Start();

        Assert.Equal(GameState.Starting, session.State);
        Assert.True(session.IsRunning);
    }

    [Fact]
    public void Start_DwaRazy_RzucaWyjatek()
    {
        GameSession session = CreateSession(2);
        session.Start();

        Assert.Throws<InvalidOperationException>(session.Start);
    }

    [Fact]
    public void SelectNextPlayer_ObracaSieWKolejnosci()
    {
        GameSession session = CreateSession(3);
        session.Start();

        string[] kolejnosc =
        [
            session.SelectNextPlayer().Name,
            session.SelectNextPlayer().Name,
            session.SelectNextPlayer().Name,
            session.SelectNextPlayer().Name,
        ];

        Assert.Equal(["Gracz 1", "Gracz 2", "Gracz 3", "Gracz 1"], kolejnosc);
    }

    [Fact]
    public void SelectNextPlayer_PomijaGraczyKtorzyOdpadli()
    {
        GameSession session = CreateSession(3);
        session.Start();
        session.SelectNextPlayer();          // Gracz 1
        session.SelectNextPlayer();          // Gracz 2
        session.EliminateCurrentPlayer();    // Gracz 2 odpada

        Assert.Equal("Gracz 3", session.SelectNextPlayer().Name);
        Assert.Equal("Gracz 1", session.SelectNextPlayer().Name);
    }

    [Fact]
    public void BeginTurn_NumerujeTuryOdJednego()
    {
        GameSession session = StartedSession(2);

        Turn pierwsza = session.BeginTurn(AnyMove);
        session.CompleteAnnouncement();
        session.SelectNextPlayer();
        Turn druga = session.BeginTurn(new Move(BodyPart.LeftFoot, SpinColor.Blue));

        Assert.Equal(1, pierwsza.Number);
        Assert.Equal(2, druga.Number);
        Assert.Equal(2, session.TurnNumber);
    }

    [Fact]
    public void BeginTurn_ZapisujeRuchWHistorii()
    {
        GameSession session = StartedSession(1);

        session.BeginTurn(AnyMove);

        Assert.Equal([AnyMove], session.MoveHistory.Snapshot());
    }

    [Fact]
    public void BeginTurn_AktualizujePozycjeKonczynGracza()
    {
        // To domyka mechanizm z Etapu 4: algorytm losowania karze ruch, który niczego
        // nie zmienia, i te właśnie pozycje są mu do tego potrzebne.
        GameSession session = StartedSession(1);
        Player player = session.CurrentPlayer!;

        session.BeginTurn(new Move(BodyPart.RightHand, SpinColor.Red));

        IReadOnlyDictionary<BodyPart, SpinColor> pozycje = session.GetLimbPositions(player.Id);

        Assert.Equal(SpinColor.Red, pozycje[BodyPart.RightHand]);
    }

    [Fact]
    public void BeginTurn_KolejnyRuchTaSamaKonczyna_NadpisujePozycje()
    {
        GameSession session = StartedSession(1);
        Player player = session.CurrentPlayer!;

        session.BeginTurn(new Move(BodyPart.RightHand, SpinColor.Red));
        session.CompleteAnnouncement();
        session.BeginTurn(new Move(BodyPart.RightHand, SpinColor.Green));

        Assert.Equal(SpinColor.Green, session.GetLimbPositions(player.Id)[BodyPart.RightHand]);
    }

    [Fact]
    public void BeginTurn_ZWydarzeniem_ZwiekszaLicznikWydarzen()
    {
        GameSession session = StartedSession(1);

        session.BeginTurn(AnyMove, GameEvent.CreateCustom("Zamiana miejsc", 5));

        Assert.Equal(1, session.EventCount);
        Assert.True(session.CurrentTurn!.HasEvent);
    }

    [Fact]
    public void BeginTurn_BezWskazanegoGracza_RzucaWyjatek()
    {
        GameSession session = CreateSession(2);
        session.Start();

        Assert.Throws<InvalidOperationException>(() => session.BeginTurn(AnyMove));
    }

    [Fact]
    public void Pause_NastepnieResume_WracaDoOczekiwaniaNaGraczy()
    {
        GameSession session = StartedSession(2);
        session.BeginTurn(AnyMove);
        session.CompleteAnnouncement();

        session.Pause();
        Assert.Equal(GameState.Paused, session.State);

        session.Resume();
        Assert.Equal(GameState.AwaitingPlayerAction, session.State);
    }

    [Fact]
    public void Resume_PrzedPierwszaTura_WracaDoStanuStarting()
    {
        GameSession session = StartedSession(2);
        session.Pause();

        session.Resume();

        Assert.Equal(GameState.Starting, session.State);
    }

    [Fact]
    public void Resume_PauzaWTrakcieOglaszania_NieWracaDoOglaszania()
    {
        // Powtórne odczytanie ruchu to zadanie komendy „Powtórz", a nie wznowienia gry.
        GameSession session = StartedSession(2);
        session.BeginTurn(AnyMove);

        session.Pause();
        session.Resume();

        Assert.Equal(GameState.AwaitingPlayerAction, session.State);
    }

    [Fact]
    public void Resume_GdyGraNieJestWstrzymana_RzucaWyjatek()
    {
        GameSession session = StartedSession(2);

        Assert.Throws<InvalidOperationException>(session.Resume);
    }

    [Fact]
    public void EliminateCurrentPlayer_OznaczaGraczaIZapisujeKolejnosc()
    {
        GameSession session = StartedSession(3);
        Player odpadajacy = session.CurrentPlayer!;

        Player eliminated = session.EliminateCurrentPlayer();

        Assert.True(eliminated.IsEliminated);
        Assert.Equal(odpadajacy.Id, eliminated.Id);
        Assert.Equal([odpadajacy.Id], session.EliminationOrder);
        Assert.Equal(2, session.ActivePlayers.Count);
    }

    [Fact]
    public void EliminateCurrentPlayer_DwaRazyTegoSamego_RzucaWyjatek()
    {
        GameSession session = StartedSession(3);
        session.EliminateCurrentPlayer();

        Assert.Throws<InvalidOperationException>(() => session.EliminateCurrentPlayer());
    }

    [Fact]
    public void IsGameOver_PrzyDwochGraczach_PoJednejEliminacji()
    {
        GameSession session = StartedSession(2);

        Assert.False(session.IsGameOver);

        session.EliminateCurrentPlayer();

        Assert.True(session.IsGameOver);
        Assert.NotNull(session.Winner);
    }

    [Fact]
    public void IsGameOver_PrzyJednymGraczu_DopieroGdyOdpadnie()
    {
        // Jeden gracz to tryb treningowy — gra nie kończy się sama.
        GameSession session = StartedSession(1);

        Assert.False(session.IsGameOver);

        session.EliminateCurrentPlayer();

        Assert.True(session.IsGameOver);
        Assert.Null(session.Winner);
    }

    [Fact]
    public void PelnaPartiaCzterechGraczy_KonczySieJednymZwyciezca()
    {
        GameSession session = StartedSession(4);

        for (int eliminacja = 0; eliminacja < 3; eliminacja++)
        {
            session.BeginTurn(AnyMove);
            session.CompleteAnnouncement();
            session.EliminateCurrentPlayer();

            if (!session.IsGameOver)
            {
                session.SelectNextPlayer();
            }
        }

        Assert.True(session.IsGameOver);
        Assert.Single(session.ActivePlayers);
        Assert.Equal("Gracz 4", session.Winner!.Name);
        Assert.Equal(3, session.EliminationOrder.Count);
    }

    [Fact]
    public void Finish_UstawiaStanFinished()
    {
        GameSession session = StartedSession(2);

        session.Finish();

        Assert.Equal(GameState.Finished, session.State);
        Assert.False(session.IsRunning);
    }

    [Fact]
    public void RestoreFrom_OdtwarzaStanPartii()
    {
        GameSession original = StartedSession(3);
        original.BeginTurn(new Move(BodyPart.LeftHand, SpinColor.Blue));
        original.CompleteAnnouncement();
        original.EliminateCurrentPlayer();
        Player currentPlayer = original.SelectNextPlayer();
        original.BeginTurn(new Move(BodyPart.RightFoot, SpinColor.Green));
        original.CompleteAnnouncement();

        GameSession restored = new(original.Players, 12);
        restored.RestoreFrom(new GameSessionRestorePoint
        {
            State = GameState.Paused,
            TurnNumber = original.TurnNumber,
            CurrentPlayerId = currentPlayer.Id,
            EventCount = original.EventCount,
            EliminationOrder = original.EliminationOrder,
            RecentMoves = original.MoveHistory.Snapshot(),
            LimbPositions = original.Players.ToDictionary(
                player => player.Id,
                player => original.GetLimbPositions(player.Id)),
        });

        Assert.Equal(GameState.Paused, restored.State);
        Assert.Equal(original.TurnNumber, restored.TurnNumber);
        Assert.Equal(currentPlayer.Id, restored.CurrentPlayer!.Id);
        Assert.Equal(original.EliminationOrder, restored.EliminationOrder);
        Assert.Equal(original.ActivePlayers.Count, restored.ActivePlayers.Count);
    }

    [Fact]
    public void RestoreFrom_ZachowujeKolejnoscHistoriiRuchow()
    {
        // Kolejność jest istotna: algorytm losowania liczy z niej odległość ruchu
        // w przeszłość. Odwrócona historia oznaczałaby, że po wznowieniu gry algorytm
        // uznaje najstarszy ruch za najświeższy.
        GameSession original = StartedSession(1);
        Move pierwszy = new(BodyPart.RightHand, SpinColor.Red);
        Move drugi = new(BodyPart.LeftFoot, SpinColor.Green);

        original.BeginTurn(pierwszy);
        original.CompleteAnnouncement();
        original.BeginTurn(drugi);

        GameSession restored = new(original.Players, 12);
        restored.RestoreFrom(new GameSessionRestorePoint
        {
            State = GameState.Paused,
            TurnNumber = original.TurnNumber,
            CurrentPlayerId = original.CurrentPlayer!.Id,
            RecentMoves = original.MoveHistory.Snapshot(),
        });

        Assert.Equal([drugi, pierwszy], restored.MoveHistory.Snapshot());
    }

    private static GameSession CreateSession(int playerCount) => new(
        [.. Enumerable.Range(0, playerCount).Select(index => Player.Create($"Gracz {index + 1}", index))],
        12);

    private static GameSession StartedSession(int playerCount)
    {
        GameSession session = CreateSession(playerCount);
        session.Start();
        session.SelectNextPlayer();

        return session;
    }
}
