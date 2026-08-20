using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy silnika rozgrywki — realizacja kryterium „pełna partia przechodzi bez UI".
/// </summary>
public class GameEngineTests
{
    [Fact]
    public async Task StartAsync_RozgrywaPierwszaTure()
    {
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        Assert.Equal(GameState.AwaitingPlayerAction, harness.Engine.State);
        Assert.Equal(1, harness.Engine.Session!.TurnNumber);
        Assert.NotNull(harness.Engine.Session.CurrentTurn);
        Assert.NotNull(harness.Engine.LastAnnouncement);
    }

    [Fact]
    public async Task StartAsync_ZglaszaKomunikatRozpoczeciaPrzedPierwszaTura()
    {
        using GameTestHarness harness = new();
        List<AnnouncementKind> kolejnosc = [];
        harness.Engine.AnnouncementRaised += (_, announcement) => kolejnosc.Add(announcement.Kind);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        // Tura zaczyna się wywołaniem gracza, dopiero potem pada polecenie ruchu.
        Assert.Equal(
            [AnnouncementKind.GameStart, AnnouncementKind.PlayerTurn, AnnouncementKind.Move],
            kolejnosc);
    }

    [Fact]
    public async Task NextTurnAsync_RozdajeTuryKolejnymGraczom()
    {
        using GameTestHarness harness = new();
        List<string> gracze = [];
        harness.Engine.TurnPlayed += (_, turn) => gracze.Add(turn.Player.Name);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(3));
        await harness.Engine.NextTurnAsync();
        await harness.Engine.NextTurnAsync();
        await harness.Engine.NextTurnAsync();

        Assert.Equal(["Gracz 1", "Gracz 2", "Gracz 3", "Gracz 1"], gracze);
    }

    [Fact]
    public async Task NextTurnAsync_NigdyNiePowtarzaTegoSamegoRuchuPodRzad()
    {
        // Sprawdzenie, że algorytm z Etapu 4 faktycznie dostaje historię partii.
        using GameTestHarness harness = new();
        List<Move> ruchy = [];
        harness.Engine.TurnPlayed += (_, turn) => ruchy.Add(turn.Move);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        for (int i = 0; i < 60; i++)
        {
            await harness.Engine.NextTurnAsync();
        }

        for (int i = 1; i < ruchy.Count; i++)
        {
            Assert.NotEqual(ruchy[i - 1], ruchy[i]);
        }
    }

    [Fact]
    public async Task NextTurnAsync_BezRozpoczetejPartii_JestIgnorowane()
    {
        using GameTestHarness harness = new();

        await harness.Engine.NextTurnAsync();

        Assert.Equal(GameState.Idle, harness.Engine.State);
    }

    [Fact]
    public async Task NextTurnAsync_NaPauzie_JestIgnorowane()
    {
        // Komenda głosowa może przyjść w dowolnym momencie i nie może wywalić rozgrywki
        // ani przeskoczyć tury w trakcie pauzy.
        using GameTestHarness harness = new();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.PauseAsync();

        int turaPrzed = harness.Engine.Session!.TurnNumber;
        await harness.Engine.NextTurnAsync();

        Assert.Equal(turaPrzed, harness.Engine.Session.TurnNumber);
        Assert.Equal(GameState.Paused, harness.Engine.State);
    }

    [Fact]
    public async Task RepeatAsync_ZglaszaPonownieTenSamKomunikat()
    {
        using GameTestHarness harness = new();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        Announcement pierwszy = harness.Engine.LastAnnouncement!;
        List<Announcement> powtorzone = [];
        harness.Engine.AnnouncementRaised += (_, announcement) => powtorzone.Add(announcement);

        await harness.Engine.RepeatAsync();

        Assert.Equal(pierwszy, Assert.Single(powtorzone));
        Assert.Equal(1, harness.Engine.Session!.TurnNumber);
    }

    [Fact]
    public async Task PauseAsync_NastepnieResumeAsync_PrzywracaMozliwoscGry()
    {
        using GameTestHarness harness = new();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        await harness.Engine.PauseAsync();
        Assert.Equal(GameState.Paused, harness.Engine.State);

        await harness.Engine.ResumeAsync();
        Assert.Equal(GameState.AwaitingPlayerAction, harness.Engine.State);

        await harness.Engine.NextTurnAsync();
        Assert.Equal(2, harness.Engine.Session!.TurnNumber);
    }

    [Fact]
    public async Task EliminateCurrentPlayerAsync_PrzyDwochGraczach_KonczyPartie()
    {
        using GameTestHarness harness = new();
        GameSummary? summary = null;
        harness.Engine.GameFinished += (_, value) => summary = value;

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.EliminateCurrentPlayerAsync();

        Assert.Equal(GameState.Finished, harness.Engine.State);
        Assert.NotNull(summary);
        Assert.NotNull(summary.Winner);
        Assert.Equal("Gracz 2", summary.Winner.Name);
        Assert.Single(summary.EliminationOrder);
    }

    [Fact]
    public async Task PelnaPartiaCzterechGraczy_PrzechodziBezInterfejsu()
    {
        // Kryterium ukończenia Etapu 5: pełna partia daje się rozegrać w testach,
        // bez uruchamiania aplikacji.
        using GameTestHarness harness = new();
        GameSummary? summary = null;
        harness.Engine.GameFinished += (_, value) => summary = value;

        await harness.Engine.StartAsync(GameTestHarness.Configuration(4));

        while (harness.Engine.State != GameState.Finished)
        {
            await harness.Engine.NextTurnAsync();
            await harness.Engine.EliminateCurrentPlayerAsync();
        }

        Assert.NotNull(summary);
        Assert.Equal(4, summary.PlayerCount);
        Assert.Equal(3, summary.EliminationOrder.Count);
        Assert.NotNull(summary.Winner);
        Assert.DoesNotContain(summary.Winner, summary.EliminationOrder);
    }

    [Fact]
    public async Task EndAsync_LiczyCzasTrwaniaPartii()
    {
        using GameTestHarness harness = new();
        GameSummary? summary = null;
        harness.Engine.GameFinished += (_, value) => summary = value;

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        harness.TimeProvider.Advance(TimeSpan.FromMinutes(7));
        await harness.Engine.EndAsync();

        Assert.NotNull(summary);
        Assert.Equal(TimeSpan.FromMinutes(7), summary.Duration);
    }

    [Fact]
    public async Task EndAsync_UsuwaZapisPartii()
    {
        // Zakończona partia nie ma czego wznawiać.
        using GameTestHarness harness = new();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.SaveSnapshotAsync();

        await harness.Engine.EndAsync();

        Assert.Null(harness.SessionRepository.Snapshot);
        Assert.True(harness.SessionRepository.ClearCount > 0);
    }

    [Fact]
    public async Task TrybAutomatyczny_PoUplywieOdstepu_RozgrywaNastepnaTure()
    {
        using GameTestHarness harness = new();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(
            2,
            TurnAdvanceMode.Automatic,
            TimeSpan.FromSeconds(5)));

        Assert.Equal(1, harness.Engine.Session!.TurnNumber);

        harness.TimeProvider.Advance(TimeSpan.FromSeconds(5));
        await WaitForTurnAsync(harness, expectedTurn: 2);

        Assert.Equal(2, harness.Engine.Session.TurnNumber);
    }

    [Fact]
    public async Task TrybAutomatyczny_PrzedUplywemOdstepu_NieRozgrywaTury()
    {
        using GameTestHarness harness = new();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(
            2,
            TurnAdvanceMode.Automatic,
            TimeSpan.FromSeconds(5)));

        harness.TimeProvider.Advance(TimeSpan.FromSeconds(4));

        Assert.Equal(1, harness.Engine.Session!.TurnNumber);
    }

    [Fact]
    public async Task TrybAutomatyczny_NaPauzie_TimerNieRozgrywaTur()
    {
        // Pauza musi wstrzymać także automatyczne przechodzenie tur.
        using GameTestHarness harness = new();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(
            2,
            TurnAdvanceMode.Automatic,
            TimeSpan.FromSeconds(5)));

        await harness.Engine.PauseAsync();
        harness.TimeProvider.Advance(TimeSpan.FromMinutes(1));

        Assert.Equal(1, harness.Engine.Session!.TurnNumber);
        Assert.Equal(GameState.Paused, harness.Engine.State);
    }

    [Fact]
    public async Task TrybReczny_UplywCzasu_NieRozgrywaTur()
    {
        using GameTestHarness harness = new();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(2, TurnAdvanceMode.Manual));

        harness.TimeProvider.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(1, harness.Engine.Session!.TurnNumber);
    }

    [Fact]
    public async Task SaveSnapshotAsync_PoZakonczeniuPartii_NieZapisujeNiczego()
    {
        using GameTestHarness harness = new();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.EndAsync();

        int zapisyPrzed = harness.SessionRepository.SaveCount;
        await harness.Engine.SaveSnapshotAsync();

        Assert.Equal(zapisyPrzed, harness.SessionRepository.SaveCount);
    }

    [Fact]
    public async Task TryRestoreAsync_BezZapisu_ZwracaFalse()
    {
        using GameTestHarness harness = new();

        Assert.False(await harness.Engine.TryRestoreAsync());
    }

    [Fact]
    public async Task GraPrzezywaMinimalizacjeAplikacji()
    {
        // Kryterium ukończenia Etapu 5. Drugi obiekt silnika odpowiada aplikacji
        // uruchomionej ponownie po tym, jak system usunął jej proces w tle.
        using GameTestHarness pierwszeUruchomienie = new();
        await pierwszeUruchomienie.Engine.StartAsync(GameTestHarness.Configuration(3));
        await pierwszeUruchomienie.Engine.NextTurnAsync();
        await pierwszeUruchomienie.Engine.EliminateCurrentPlayerAsync();
        await pierwszeUruchomienie.Engine.NextTurnAsync();
        await pierwszeUruchomienie.Engine.SaveSnapshotAsync();

        GameSessionSnapshot zapis = pierwszeUruchomienie.SessionRepository.Snapshot!;
        int turyPrzed = pierwszeUruchomienie.Engine.Session!.TurnNumber;
        int aktywniPrzed = pierwszeUruchomienie.Engine.Session.ActivePlayers.Count;

        using GameTestHarness poWznowieniu = new();
        await poWznowieniu.SessionRepository.SaveAsync(zapis);

        Assert.True(await poWznowieniu.Engine.TryRestoreAsync());

        GameSession wznowiona = poWznowieniu.Engine.Session!;

        Assert.Equal(turyPrzed, wznowiona.TurnNumber);
        Assert.Equal(aktywniPrzed, wznowiona.ActivePlayers.Count);
        Assert.Equal(GameState.Paused, wznowiona.State);
    }

    [Fact]
    public async Task WznowionaPartia_DajeSieKontynuowac()
    {
        using GameTestHarness harness = new();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(3));
        await harness.Engine.NextTurnAsync();
        await harness.Engine.SaveSnapshotAsync();

        GameSessionSnapshot zapis = harness.SessionRepository.Snapshot!;

        using GameTestHarness poWznowieniu = new();
        await poWznowieniu.SessionRepository.SaveAsync(zapis);
        await poWznowieniu.Engine.TryRestoreAsync();

        await poWznowieniu.Engine.ResumeAsync();
        await poWznowieniu.Engine.NextTurnAsync();

        Assert.Equal(zapis.TurnNumber + 1, poWznowieniu.Engine.Session!.TurnNumber);
    }

    [Fact]
    public async Task WznowionaPartia_ZachowujePamiecAlgorytmuLosowania()
    {
        // Bez historii ruchów algorytm po wznowieniu mógłby powtórzyć dopiero co
        // wykonany ruch — a to jedyna reguła, której nigdy nie łamie.
        using GameTestHarness harness = new();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.NextTurnAsync();
        await harness.Engine.SaveSnapshotAsync();

        GameSessionSnapshot zapis = harness.SessionRepository.Snapshot!;
        Move ostatniRuch = harness.Engine.Session!.CurrentTurn!.Move;

        using GameTestHarness poWznowieniu = new();
        await poWznowieniu.SessionRepository.SaveAsync(zapis);
        await poWznowieniu.Engine.TryRestoreAsync();

        Assert.Equal(ostatniRuch, poWznowieniu.Engine.Session!.MoveHistory.Snapshot()[0]);

        await poWznowieniu.Engine.ResumeAsync();
        await poWznowieniu.Engine.NextTurnAsync();

        Assert.NotEqual(ostatniRuch, poWznowieniu.Engine.Session.CurrentTurn!.Move);
    }


    [Fact]
    public async Task ZAktywnaPaczka_WydarzeniaPojawiajaSieWTurach()
    {
        // Integracja Etapu 6 z silnikiem: krok losowania wydarzeń dołożony do potoku
        // bez zmiany pozostałych kroków.
        using GameTestHarness harness = new();
        EventPack pack = EventPack.Create("Pewniak", [GameEvent.CreateCustom("Zamiana miejsc", 100)]);

        GameConfiguration configuration = GameTestHarness.Configuration(2) with { EventPack = pack };

        await harness.Engine.StartAsync(configuration);

        for (int i = 0; i < 10; i++)
        {
            await harness.Engine.NextTurnAsync();
        }

        Assert.True(harness.Engine.Session!.EventCount > 0, "Żadne wydarzenie nie padło.");
    }

    [Fact]
    public async Task ZAktywnaPaczka_ZglaszaOsobnyKomunikatWydarzenia()
    {
        using GameTestHarness harness = new();
        EventPack pack = EventPack.Create("Pewniak", [GameEvent.CreateCustom("Zamiana miejsc", 100)]);
        List<AnnouncementKind> rodzaje = [];

        harness.Engine.AnnouncementRaised += (_, announcement) => rodzaje.Add(announcement.Kind);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2) with { EventPack = pack });

        Assert.Contains(AnnouncementKind.Event, rodzaje);

        // Wydarzenie idzie przed ruchem: dotyczy tej tury i zmienia sposób jej wykonania
        // („tę rundę robisz z zamkniętymi oczami"), więc gracz musi je usłyszeć, zanim
        // pozna polecenie ruchu. Odwrotna kolejność kazałaby mu poprawiać już rozpoczęty ruch.
        int indeksRuchu = rodzaje.IndexOf(AnnouncementKind.Move);
        int indeksWydarzenia = rodzaje.IndexOf(AnnouncementKind.Event);

        Assert.True(indeksWydarzenia < indeksRuchu);
    }

    [Fact]
    public async Task BezPaczki_ZadneWydarzenieNiePada()
    {
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        for (int i = 0; i < 20; i++)
        {
            await harness.Engine.NextTurnAsync();
        }

        Assert.Equal(0, harness.Engine.Session!.EventCount);
    }

    [Fact]
    public async Task ZapisPartii_ZachowujePaczkeIHistorieWydarzen()
    {
        // Wznowiona partia toczy się na zasadach, na jakich się zaczęła — nawet jeśli
        // użytkownik w czasie przerwy zmienił aktywną paczkę.
        using GameTestHarness harness = new();
        EventPack pack = EventPack.Create("Pewniak", [GameEvent.CreateCustom("Zamiana miejsc", 100)]);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(3) with { EventPack = pack });
        await harness.Engine.NextTurnAsync();
        await harness.Engine.NextTurnAsync();
        await harness.Engine.SaveSnapshotAsync();

        GameSessionSnapshot zapis = harness.SessionRepository.Snapshot!;

        Assert.NotNull(zapis.EventPack);
        Assert.Equal("Pewniak", zapis.EventPack.Name);
        Assert.NotNull(zapis.LastEventTurn);
        Assert.NotEmpty(zapis.LastEventTurns);

        using GameTestHarness poWznowieniu = new();
        await poWznowieniu.SessionRepository.SaveAsync(zapis);
        await poWznowieniu.Engine.TryRestoreAsync();

        Assert.Equal(zapis.LastEventTurn, poWznowieniu.Engine.Session!.LastEventTurn);
        Assert.Equal(zapis.EventCount, poWznowieniu.Engine.Session.EventCount);
    }

    [Fact]
    public async Task Tura_CzytaWszystkoCoPokazuje_WTejSamejKolejnosci()
    {
        // Ekran i głos to dwa kanały tego samego przekazu. Rozjazd między nimi byłby
        // dla graczy myszący: słyszą jedno, widzą drugie.
        using GameTestHarness harness = new();
        EventPack pack = EventPack.Create("Pewniak", [GameEvent.CreateCustom("Zamiana miejsc", 100)]);
        List<Announcement> pokazane = [];

        harness.Engine.AnnouncementRaised += (_, announcement) => pokazane.Add(announcement);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2) with { EventPack = pack });

        Assert.Equal(pokazane.Select(announcement => announcement.Text), harness.TextToSpeech.Spoken);

        List<AnnouncementKind> rodzaje = [.. pokazane.Select(announcement => announcement.Kind)];

        Assert.True(rodzaje.IndexOf(AnnouncementKind.Event) < rodzaje.IndexOf(AnnouncementKind.Move));
    }

    [Fact]
    public async Task MiedzyWydarzeniemARuchem_JestCzasNaZadanie()
    {
        // Bez przerwy oba zdania zlewają się w jedno i gracze nie wiedzą, gdzie kończy
        // się wydarzenie, a zaczyna polecenie ruchu.
        using GameTestHarness harness = new();
        EventPack pack = EventPack.Create("Pewniak", [GameEvent.CreateCustom("Zamiana miejsc", 100)]);
        Announcement? wydarzenie = null;
        Announcement? ruch = null;

        harness.Engine.AnnouncementRaised += (_, announcement) =>
        {
            if (announcement.Kind == AnnouncementKind.Event)
            {
                wydarzenie = announcement;
            }
            else if (announcement.Kind == AnnouncementKind.Move)
            {
                ruch = announcement;
            }
        };

        GameConfiguration configuration = GameTestHarness.Configuration(2) with
        {
            EventPack = pack,
            TaskTime = TimeSpan.FromSeconds(2),
        };

        Task partia = harness.Engine.StartAsync(configuration);

        await WaitUntilAsync(() =>
            wydarzenie is not null && harness.TextToSpeech.Spoken.Contains(wydarzenie.Text));

        // Przerwa trwa: wydarzenie już wypowiedziane, ruch jeszcze nie.
        Assert.Null(ruch);

        // Sterowany zegar przesuwa tylko oczekiwania już zarejestrowane, a przerwa
        // rejestruje się po zakończeniu wypowiedzi — przesuwamy więc do skutku.
        await WaitUntilAsync(() =>
        {
            harness.TimeProvider.Advance(TimeSpan.FromSeconds(2));

            return ruch is not null;
        });

        await partia;

        Assert.NotNull(ruch);
        Assert.Contains(ruch!.Text, harness.TextToSpeech.Spoken);
    }

    [Fact]
    public async Task WTrakcieOdczytu_DalejJestIgnorowane()
    {
        // Sedno flow: gracz nie może przeskoczyć tury, której jeszcze nie usłyszał.
        // Dotyczy to zarówno przycisku, jak i komendy głosowej z Etapu 8 — obie prowadzą
        // do tej samej metody.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        TaskCompletionSource brama = new();
        harness.TextToSpeech.Gate = brama;

        Task druga = harness.Engine.NextTurnAsync();

        await WaitUntilAsync(() => harness.Engine.State == GameState.AnnouncingTurn);

        // „Dalej" w trakcie odczytu — musi zostać pominięte.
        await harness.Engine.NextTurnAsync();

        Assert.Equal(2, harness.Engine.Session!.TurnNumber);

        harness.TextToSpeech.Gate = null;
        brama.SetResult();

        await druga;

        Assert.Equal(GameState.AwaitingPlayerAction, harness.Engine.State);
        Assert.Equal(2, harness.Engine.Session.TurnNumber);
    }

    [Fact]
    public async Task Tura_ZaczynaSieWywolaniemGraczaPoImieniu()
    {
        // Gracz ma wiedzieć, że to jego kolej, ZANIM usłyszy polecenie — inaczej orientuje
        // się w połowie komunikatu, którego początku już nie usłyszał.
        using GameTestHarness harness = new();
        List<Announcement> pokazane = [];

        harness.Engine.AnnouncementRaised += (_, announcement) => pokazane.Add(announcement);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        Announcement wywolanie = pokazane.First(a => a.Kind == AnnouncementKind.PlayerTurn);
        Announcement ruch = pokazane.First(a => a.Kind == AnnouncementKind.Move);

        Assert.True(pokazane.IndexOf(wywolanie) < pokazane.IndexOf(ruch));
        Assert.Contains(harness.Engine.Session!.CurrentPlayer!.Name, wywolanie.Text, StringComparison.Ordinal);

        // Polecenie ruchu nie powtarza imienia.
        Assert.DoesNotContain(
            harness.Engine.Session.CurrentPlayer.Name,
            ruch.Text,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task CzasNaZadanie_JestZglaszanyJakoOdliczanie()
    {
        // Ekran ma pokazać, ile czasu zostało na wykonanie zadania z wydarzenia.
        using GameTestHarness harness = new();
        EventPack pack = EventPack.Create("Pewniak", [GameEvent.CreateCustom("Zamiana miejsc", 100)]);
        List<TurnCountdown?> odliczania = [];

        harness.Engine.CountdownChanged += (_, countdown) => odliczania.Add(countdown);

        GameConfiguration configuration = GameTestHarness.Configuration(2) with
        {
            EventPack = pack,
            TaskTime = TimeSpan.FromSeconds(5),
        };

        Task partia = harness.Engine.StartAsync(configuration);

        await WaitUntilAsync(() => odliczania.Count > 0);

        TurnCountdown? rozpoczete = odliczania[0];

        Assert.NotNull(rozpoczete);
        Assert.Equal(TurnCountdownKind.Task, rozpoczete.Kind);
        Assert.Equal(TimeSpan.FromSeconds(5), rozpoczete.Total);

        await WaitUntilAsync(() =>
        {
            harness.TimeProvider.Advance(TimeSpan.FromSeconds(1));

            return odliczania.Count > 1;
        });

        await partia;

        // Odliczanie zadania kończy się jawnym zerowaniem, żeby ekran zdjął liczbę.
        // Po nim rusza odliczanie czasu na ruch, więc sprawdzamy sam moment zamknięcia.
        int indeksZadania = odliczania.FindIndex(c => c?.Kind == TurnCountdownKind.Task);

        Assert.True(indeksZadania >= 0, "Odliczanie zadania nie zostało zgłoszone.");
        Assert.Null(odliczania[indeksZadania + 1]);
    }

    [Fact]
    public async Task TrybAutomatyczny_ZglaszaOdliczanieCzasuNaRuch()
    {
        using GameTestHarness harness = new();
        List<TurnCountdown?> odliczania = [];

        harness.Engine.CountdownChanged += (_, countdown) => odliczania.Add(countdown);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(
            2,
            Settings.TurnAdvanceMode.Automatic,
            TimeSpan.FromSeconds(8)));

        TurnCountdown? ruch = odliczania.LastOrDefault(countdown => countdown is not null);

        Assert.NotNull(ruch);
        Assert.Equal(TurnCountdownKind.Move, ruch.Kind);
        Assert.Equal(TimeSpan.FromSeconds(8), ruch.Total);
    }

    [Fact]
    public async Task TrybReczny_TezZglaszaOdliczanieCzasuNaRuch()
    {
        // Gracze chcą wiedzieć, ile mają czasu, także wtedy, gdy sami zatwierdzają turę.
        using GameTestHarness harness = new();
        List<TurnCountdown?> odliczania = [];

        harness.Engine.CountdownChanged += (_, countdown) => odliczania.Add(countdown);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        Assert.Contains(odliczania, countdown => countdown?.Kind == TurnCountdownKind.Move);
    }

    [Fact]
    public async Task TrybReczny_PoUplywieCzasuNaRuch_NieRozgrywaTury()
    {
        // Zegar odmierza w tym trybie SUGEROWANE tempo, a nie termin: dojście do zera kończy
        // odliczanie, ale partia dalej czeka na potwierdzenie od graczy.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(
            2,
            Settings.TurnAdvanceMode.Manual,
            TimeSpan.FromSeconds(4)));

        int tura = harness.Engine.Session!.TurnNumber;

        harness.TimeProvider.Advance(TimeSpan.FromSeconds(10));
        await Task.Delay(50);

        Assert.Equal(tura, harness.Engine.Session.TurnNumber);
        Assert.Null(harness.Engine.Countdown);
    }

    [Fact]
    public async Task ZgloszenieOdpadniecia_DotyczyWskazanegoGracza()
    {
        // Upadek zdarza się także wtedy, gdy ruch wykonuje ktoś inny — zgłoszenie musi
        // wskazywać gracza, a nie brać tego, którego jest tura.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(3));

        Player aktualny = harness.Engine.Session!.CurrentPlayer!;
        Player inny = harness.Engine.Session.Players.First(player => player.Id != aktualny.Id);

        await harness.Engine.EliminatePlayerAsync(inny.Id);

        Assert.Equal([inny.Id], harness.Engine.Session.EliminationOrder);
        Assert.False(harness.Engine.Session.Players.Single(p => p.Id == aktualny.Id).IsEliminated);
    }

    /// <summary>
    /// Czeka na spełnienie warunku, odpytując go co dziesięć milisekund.
    /// </summary>
    /// <remarks>
    /// Odczyt głosowy i przerwa między komunikatami dzieją się poza sekcją krytyczną
    /// silnika, więc test musi poczekać na stan, a nie na zakończenie metody.
    /// </remarks>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.Fail("Warunek nie został spełniony w wyznaczonym czasie.");
    }

    /// <summary>
    /// Czeka na rozegranie tury uruchomionej przez timer.
    /// </summary>
    /// <remarks>
    /// Timer uruchamia turę bez czekania, więc test musi dać jej chwilę na wykonanie.
    /// Odpytywanie zamiast stałego opóźnienia — test kończy się od razu po skutku.
    /// </remarks>
    private static async Task WaitForTurnAsync(GameTestHarness harness, int expectedTurn)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            if (harness.Engine.Session?.TurnNumber >= expectedTurn)
            {
                return;
            }

            await Task.Delay(10);
        }
    }
}
