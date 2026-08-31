using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Advertising;
using TwisterCompanion.Application.Feedback;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.GameModes;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Application.VoiceControl;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.GameModes;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Tests.Fakes;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.Presentation.Tests;

/// <summary>
/// Testy ekranu rozgrywki — w szczególności rozdziału komunikatu o ruchu
/// od zapowiedzi wydarzenia.
/// </summary>
public class GameViewModelTests
{
    private readonly IGameEngine _engine = Substitute.For<IGameEngine>();
    private readonly IPlayerRosterRepository _roster = Substitute.For<IPlayerRosterRepository>();
    private readonly IGameModeService _gameModes = Substitute.For<IGameModeService>();
    private readonly IEventPackService _eventPacks = Substitute.For<IEventPackService>();
    private readonly FakeVoiceControlService _voiceControl = new();
    private readonly FakeAudioCueService _audioCues = new();
    private readonly FakeGameFeedback _feedback = new();
    private readonly IVoiceControlCoordinator _voiceCoordinator = Substitute.For<IVoiceControlCoordinator>();
    private readonly IAdCoordinator _ads = Substitute.For<IAdCoordinator>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    public GameViewModelTests()
    {
        _roster.GetAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Player>>([Player.Create("Kuba", 0)]));

        _engine.State.Returns(GameState.AwaitingPlayerAction);

        // Wejście na ekran wczytuje zasady rozpoczynanej partii, więc tryb i paczka muszą
        // odpowiadać także w testach, które o nie nie pytają.
        _gameModes.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(
            new GameModeDefinition { Key = "classic", NameKey = "GameMode_Classic_Name" }));

        _eventPacks.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventPack?>(null));

        // Zakończenie partii pyta o potwierdzenie — domyślnie potwierdzamy, a testy odmowy
        // ustawiają to same.
        _dialogs.ConfirmAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(Task.FromResult(true));
    }

    [Fact]
    public void KomunikatORuchu_TrafiaDoWlasnegoPola()
    {
        GameViewModel viewModel = CreateSubscribedViewModel();

        RaiseAnnouncement(new Announcement("Kuba, prawa ręka — czerwony.", AnnouncementKind.Move));

        Assert.Equal("Kuba, prawa ręka — czerwony.", viewModel.AnnouncementText);
        Assert.Empty(viewModel.EventText);
        Assert.False(viewModel.HasEventText);
    }

    [Fact]
    public void ZapowiedzWydarzenia_NieNadpisujeKomunikatuORuchu()
    {
        // Regresja: oba komunikaty lecą jeden po drugim, a odświeżenie stanu po nich
        // czyta z silnika ostatni komunikat o RUCHU. Przy jednym wspólnym polu zapowiedź
        // wydarzenia pojawiała się i znikała w tej samej chwili — punkt scenariusza
        // „wydarzenia w rozgrywce" nie działał, choć zapowiedź powstawała poprawnie.
        GameViewModel viewModel = CreateSubscribedViewModel();

        RaiseAnnouncement(new Announcement("Wydarzenie: Zamiana miejsc.", AnnouncementKind.Event));
        RaiseAnnouncement(new Announcement("Kuba, prawa ręka — czerwony.", AnnouncementKind.Move));

        Assert.Equal("Kuba, prawa ręka — czerwony.", viewModel.AnnouncementText);
        Assert.Equal("Wydarzenie: Zamiana miejsc.", viewModel.EventText);
        Assert.True(viewModel.HasEventText);
    }

    [Fact]
    public void ZapowiedzWydarzenia_PrzezywaOdswiezenieStanu()
    {
        // Drugie zabezpieczenie tej samej regresji: zmiana stanu silnika odświeża ekran
        // z jego danych i nie może wyczyścić zapowiedzi wydarzenia.
        GameViewModel viewModel = CreateSubscribedViewModel();
        _engine.LastAnnouncement.Returns(new Announcement("Kuba, prawa ręka — czerwony.", AnnouncementKind.Move));

        RaiseAnnouncement(new Announcement("Wydarzenie: Zamiana miejsc.", AnnouncementKind.Event));
        RaiseAnnouncement(new Announcement("Kuba, prawa ręka — czerwony.", AnnouncementKind.Move));
        _engine.StateChanged += Raise.Event<EventHandler<GameState>>(_engine, GameState.AwaitingPlayerAction);

        Assert.Equal("Wydarzenie: Zamiana miejsc.", viewModel.EventText);
    }

    [Fact]
    public void NowaTura_ZamykaZapowiedzWydarzeniaPoprzedniejTury()
    {
        // Wydarzenie dotyczy jednej tury — w następnej nie może zostać na ekranie.
        // Sprzątanie jest przywiązane do rozegranej tury, a nie do komunikatu o ruchu:
        // silnik czyta najpierw wydarzenie, a ruch dopiero po przerwie, więc kasowanie
        // przy ruchu zdejmowałoby wydarzenie tej samej tury.
        GameViewModel viewModel = CreateSubscribedViewModel();

        RaiseAnnouncement(new Announcement("Wydarzenie: Zamiana miejsc.", AnnouncementKind.Event));
        RaiseAnnouncement(new Announcement("Anna, lewa noga — zielony.", AnnouncementKind.Move));

        Assert.Equal("Wydarzenie: Zamiana miejsc.", viewModel.EventText);

        RaiseTurnPlayed();

        Assert.Empty(viewModel.EventText);
        Assert.False(viewModel.HasEventText);
    }

    [Fact]
    public void KoniecPartii_ZamykaZapowiedzWydarzenia()
    {
        // Po zakończeniu partii nie ma tury, której wydarzenie mogłoby dotyczyć.
        GameViewModel viewModel = CreateSubscribedViewModel();

        RaiseAnnouncement(new Announcement("Wydarzenie: Zamiana miejsc.", AnnouncementKind.Event));
        RaiseAnnouncement(new Announcement("Koniec gry.", AnnouncementKind.GameEnd));

        Assert.Empty(viewModel.EventText);
    }

    [Fact]
    public void WejscieNaEkran_WlaczaSterowanieGlosem()
    {
        // Sterowanie głosem działa wyłącznie w trakcie rozgrywki — mikrofon nie ma prawa
        // słuchać, kiedy gracze przeglądają ustawienia albo paczki wydarzeń.
        GameViewModel viewModel = CreateSubscribedViewModel();

        _voiceCoordinator.Received(1).ActivateAsync(Arg.Any<CancellationToken>());
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void ZejscieZEkranu_WylaczaSterowanieGlosem()
    {
        GameViewModel viewModel = CreateSubscribedViewModel();

        viewModel.OnDisappearing();

        _voiceCoordinator.Received(1).DeactivateAsync();
    }

    [Fact]
    public async Task ZejscieZEkranu_WstrzymujePartie()
    {
        // Partia idąca dalej za plecami graczy to błąd: nikt nie widzi komunikatu, a w trybie
        // automatycznym tury lecą jedna po drugiej w opustoszałym pokoju.
        GameViewModel viewModel = CreateSubscribedViewModel();

        viewModel.OnDisappearing();

        await WaitUntilAsync(() => _engine.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "PauseAsync"));

        // Bez zapowiedzi: ekranu już nie ma, więc „Pauza" wypowiedziana w ustawieniach
        // byłaby samym hałasem.
        await _engine.Received(1).PauseAsync(false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ZejscieZEkranu_NajpierwZamykaMikrofonPotemWstrzymuje()
    {
        // Wstrzymanie zgłasza zmianę stanu, na którą koordynator odpowiada otwarciem okna
        // nasłuchu — odwrotna kolejność dałaby parę sygnałów już po wyjściu z ekranu.
        List<string> kolejnosc = [];

        _voiceCoordinator.DeactivateAsync().Returns(_ =>
        {
            kolejnosc.Add("mikrofon");

            return Task.CompletedTask;
        });

        _engine.PauseAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            kolejnosc.Add("pauza");

            return Task.CompletedTask;
        });

        GameViewModel viewModel = CreateSubscribedViewModel();

        viewModel.OnDisappearing();

        await WaitUntilAsync(() => kolejnosc.Count == 2);

        Assert.Equal(["mikrofon", "pauza"], kolejnosc);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(5);
        }

        Assert.Fail("Warunek nie został spełniony w wyznaczonym czasie.");
    }

    [Fact]
    public void StanNasluchu_TrafiaNaEkran()
    {
        // Sygnał dźwiękowy jest dla graczy ważniejszy, ale ekran musi pokazywać to samo,
        // gdy ktoś jednak spojrzy.
        GameViewModel viewModel = CreateSubscribedViewModel();

        _voiceControl.SetState(VoiceControlState.Listening);

        Assert.True(viewModel.IsListening);
        Assert.Equal(StringKeys.Game.VoiceListening, viewModel.VoiceStatusText);

        _voiceControl.SetState(VoiceControlState.Waiting);

        Assert.False(viewModel.IsListening);
        Assert.Equal(StringKeys.Game.VoiceWaiting, viewModel.VoiceStatusText);
    }

    [Fact]
    public void RozpoznanaKomenda_JestPotwierdzanaNaEkranie()
    {
        GameViewModel viewModel = CreateSubscribedViewModel();

        _voiceControl.RaiseCommand(VoiceCommandType.Next);

        Assert.True(viewModel.HasVoiceCommandText);
        Assert.Contains(StringKeys.Game.VoiceCommandHeard, viewModel.VoiceCommandText);
    }

    [Fact]
    public void NowaTura_ZamykaPotwierdzenieKomendy()
    {
        // Potwierdzenie dotyczy komendy, która właśnie coś zrobiła — po rozegraniu tury
        // nie ma już czego potwierdzać.
        GameViewModel viewModel = CreateSubscribedViewModel();
        _voiceControl.RaiseCommand(VoiceCommandType.Next);

        RaiseTurnPlayed();

        Assert.False(viewModel.HasVoiceCommandText);
    }

    [Fact]
    public void Odliczanie_TrafiaNaEkranZOpisemIliczbaSekund()
    {
        // Silnik podaje tylko, co i od kiedy odmierza — liczba sekund powstaje tutaj.
        GameViewModel viewModel = CreateSubscribedViewModel();

        RaiseCountdown(new TurnCountdown(
            TurnCountdownKind.Task,
            TimeSpan.FromSeconds(15),
            TimeProvider.System.GetTimestamp()));

        Assert.True(viewModel.HasCountdown);
        Assert.Equal(StringKeys.Game.CountdownTask, viewModel.CountdownText);
        Assert.InRange(viewModel.CountdownSeconds, 14, 15);
    }

    [Fact]
    public void RozegranaTura_PokazujeGraczaKolorICzescCiala()
    {
        // Ekran rozbija komunikat na trzy części, bo każda ma inną wagę i inny rozmiar:
        // kto (imię), jaki kolor (koło) i jaka kończyna (wielki napis).
        GameViewModel viewModel = CreateSubscribedViewModel();

        RaiseTurnPlayed();

        Assert.Equal("Anna", viewModel.CurrentPlayerName);
        Assert.Equal("Green", viewModel.MoveColorName);
        Assert.True(viewModel.HasMove);

        // Nazwy pochodzą z katalogu głosowego, żeby gracz widział te same słowa, które słyszy.
        Assert.Equal(StringKeys.Voice.BodyPartPrefix + BodyPart.LeftFoot, viewModel.MoveBodyPartText);
        Assert.Equal(StringKeys.Voice.ColorPrefix + SpinColor.Green, viewModel.MoveColorText);

        // Znak obrazkowy: stopa i strzałka strony. Samych emotek dłoni nie da się rozróżnić
        // z dwóch metrów, a lewej i prawej stopy nie ma w zestawie znaków w ogóle.
        Assert.Contains("🦶", viewModel.MoveBodyPartSymbol, StringComparison.Ordinal);
        Assert.Contains("⬅", viewModel.MoveBodyPartSymbol, StringComparison.Ordinal);
    }

    [Fact]
    public void OstatnieSekundyOdliczania_SaOznaczoneJakoPilne()
    {
        GameViewModel viewModel = CreateSubscribedViewModel();

        RaiseCountdown(new TurnCountdown(
            TurnCountdownKind.Move,
            TimeSpan.FromSeconds(20),
            TimeProvider.System.GetTimestamp()));

        Assert.False(viewModel.IsCountdownUrgent);

        RaiseCountdown(new TurnCountdown(
            TurnCountdownKind.Move,
            TimeSpan.FromSeconds(4),
            TimeProvider.System.GetTimestamp()));

        Assert.True(viewModel.IsCountdownUrgent);
    }

    [Fact]
    public void Odliczanie_TykaCoSekunde()
    {
        // Gracz stoi nad matą i nie patrzy na ekran — tykanie jest jedynym sposobem, żeby
        // wiedział, ile czasu zostało.
        GameViewModel viewModel = CreateSubscribedViewModel();

        RaiseCountdown(new TurnCountdown(
            TurnCountdownKind.Move,
            TimeSpan.FromSeconds(10),
            TimeProvider.System.GetTimestamp()));

        Assert.Contains(AudioCue.CountdownTick, _audioCues.Played);
        Assert.NotNull(viewModel.CountdownText);
    }

    [Fact]
    public void Odliczanie_MilczyWTrakcieNasluchu()
    {
        // Tyknięcie wpadające do otwartego mikrofonu zmarnowałoby sesję rozpoznawania
        // na dźwięk, który sami wydaliśmy.
        GameViewModel viewModel = CreateSubscribedViewModel();
        _voiceControl.SetState(VoiceControlState.Listening);

        RaiseCountdown(new TurnCountdown(
            TurnCountdownKind.Move,
            TimeSpan.FromSeconds(10),
            TimeProvider.System.GetTimestamp()));

        Assert.DoesNotContain(AudioCue.CountdownTick, _audioCues.Played);
        Assert.True(viewModel.HasCountdown);
    }

    [Fact]
    public void ZakonczoneOdliczanie_ZdejmujeLiczbeZEkranu()
    {
        GameViewModel viewModel = CreateSubscribedViewModel();
        RaiseCountdown(new TurnCountdown(
            TurnCountdownKind.Move,
            TimeSpan.FromSeconds(10),
            TimeProvider.System.GetTimestamp()));

        RaiseCountdown(null);

        Assert.False(viewModel.HasCountdown);
        Assert.Empty(viewModel.CountdownText);
    }

    [Fact]
    public void PrzyciskOdpadniecia_JestPrzyKazdymGrajacym()
    {
        // Upadek zdarza się także wtedy, gdy ruch wykonuje ktoś inny, więc przycisk nie może
        // być jeden na ekranie ani tylko przy graczu, którego jest tura.
        GameSession session = new([Player.Create("Kuba", 0), Player.Create("Anna", 1)], 12);
        session.Start();

        _engine.Session.Returns(session);
        _engine.IsEliminationEnabled.Returns(true);

        GameViewModel viewModel = CreateSubscribedViewModel();

        Assert.Equal(2, viewModel.Players.Count);
        Assert.All(viewModel.Players, player => Assert.True(player.CanEliminate));
    }

    [Fact]
    public async Task ZgloszenieOdpadniecia_PrzekazujeSilnikowiWskazanegoGracza()
    {
        GameSession session = new([Player.Create("Kuba", 0), Player.Create("Anna", 1)], 12);
        session.Start();

        _engine.Session.Returns(session);
        _engine.IsEliminationEnabled.Returns(true);

        GameViewModel viewModel = CreateSubscribedViewModel();
        PlayerListItem anna = viewModel.Players.Single(player => player.Name == "Anna");

        await viewModel.EliminatePlayerCommand.ExecuteAsync(anna);

        await _engine.Received(1).EliminatePlayerAsync(anna.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void TrybBezOdpadania_NieDajePrzyciskuPrzyGraczach()
    {
        GameSession session = new([Player.Create("Kuba", 0), Player.Create("Anna", 1)], 12);
        session.Start();

        _engine.Session.Returns(session);
        _engine.IsEliminationEnabled.Returns(false);

        GameViewModel viewModel = CreateSubscribedViewModel();

        Assert.All(viewModel.Players, player => Assert.False(player.CanEliminate));
    }

    [Fact]
    public async Task PrzedPartia_PokazujeZasadyRozpoczynanejGry()
    {
        // Ekran przed grą odpowiada na pytania „w co gramy" i „na jakich zasadach" — wybory
        // z trzech innych ekranów muszą być widoczne, zanim ruszy pierwsza tura.
        GameViewModel viewModel = CreateSubscribedViewModel();

        await WaitUntilAsync(() => viewModel.SetupItems.Count > 0);

        Assert.True(viewModel.IsBeforeGame);
        Assert.Contains(viewModel.SetupItems, item => item.Value == "GameMode_Classic_Name");
        Assert.Contains(viewModel.SetupItems, item => item.Value == StringKeys.Game.SetupNoEvents);
        Assert.Contains(viewModel.SetupItems, item => item.Label == StringKeys.Game.SetupElimination);
    }

    [Fact]
    public async Task PrzedPartia_PokazujeWybranaPaczkeWydarzen()
    {
        EventPack pack = EventPack.Create("Moja paczka", [GameEvent.CreateCustom("Zadanie", 50)]);

        _eventPacks.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<EventPack?>(pack));

        GameViewModel viewModel = CreateSubscribedViewModel();

        await WaitUntilAsync(() => viewModel.SetupItems.Count > 0);

        // Tłumaczenia zastępcze zwracają sam klucz, więc test sprawdza, że wiersz wydarzeń
        // idzie ścieżką opisu paczki, a nie „bez wydarzeń". Treść napisu należy do zasobów.
        GameSetupItem events = viewModel.SetupItems.Single(item =>
            item.Label == StringKeys.Game.SetupEvents);

        Assert.Equal(StringKeys.Game.SetupEventPackFormat, events.Value);
    }

    [Fact]
    public void ZakonczonaPartia_NieJestStanemPrzedGra()
    {
        // Po ostatniej turze na ekranie zostaje podsumowanie z „Zagraj ponownie" — drugi
        // przycisk rozpoczynający grę byłby tym samym wyjściem podanym dwa razy.
        _engine.State.Returns(GameState.Finished);

        GameViewModel viewModel = CreateSubscribedViewModel();

        Assert.True(viewModel.IsFinished);
        Assert.False(viewModel.IsBeforeGame);
    }

    [Fact]
    public void KoniecPartii_PokazujeStatystykiZPodsumowania()
    {
        // Silnik liczy te wartości i tak — zatrzymanie ich w środku byłoby marnowaniem
        // gotowej informacji, o którą gracze pytają zaraz po ostatniej turze.
        GameViewModel viewModel = CreateSubscribedViewModel();

        Player kuba = Player.Create("Kuba", 0);
        Player anna = Player.Create("Anna", 1);

        _engine.GameFinished += Raise.Event<EventHandler<GameSummary>>(
            _engine,
            new GameSummary(
                PlayerCount: 2,
                TurnCount: 23,
                EventCount: 5,
                Duration: TimeSpan.FromSeconds(150),
                EliminationOrder: [anna],
                Winner: kuba));

        Assert.Equal("23", viewModel.SummaryItems
            .Single(item => item.Label == StringKeys.Game.SummaryTurns).Value);

        Assert.Equal("5", viewModel.SummaryItems
            .Single(item => item.Label == StringKeys.Game.SetupEvents).Value);

        Assert.Equal("Anna", viewModel.SummaryItems
            .Single(item => item.Label == StringKeys.Game.SummaryEliminated).Value);
    }

    [Fact]
    public void KoniecPartiiBezOdpadniec_NiePokazujePustegoWiersza()
    {
        // W trybie dla dzieci nikt nie odpada, a puste miejsce po wierszu wyglądałoby
        // na brakującą informację.
        GameViewModel viewModel = CreateSubscribedViewModel();

        _engine.GameFinished += Raise.Event<EventHandler<GameSummary>>(
            _engine,
            new GameSummary(
                PlayerCount: 2,
                TurnCount: 8,
                EventCount: 0,
                Duration: TimeSpan.FromSeconds(60),
                EliminationOrder: [],
                Winner: null));

        Assert.DoesNotContain(
            viewModel.SummaryItems,
            item => item.Label == StringKeys.Game.SummaryEliminated);
    }

    [Fact]
    public async Task DotknieciePigulkiGraczaKtoryOdpadl_NieIdzieDoSilnika()
    {
        // Cała pigułka gracza jest celem dotknięcia, więc trafia w nią także dotknięcie
        // kogoś, kto już odpadł — zgłoszenie musi wtedy nie zrobić nic.
        GameSession session = new(
            [Player.Create("Kuba", 0), Player.Create("Anna", 1) with { IsEliminated = true }],
            12);

        session.Start();

        _engine.Session.Returns(session);
        _engine.IsEliminationEnabled.Returns(true);

        GameViewModel viewModel = CreateSubscribedViewModel();
        PlayerListItem odpadl = viewModel.Players.Single(player => player.IsEliminated);

        await viewModel.EliminatePlayerCommand.ExecuteAsync(odpadl);

        await _engine.DidNotReceive().EliminatePlayerAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ZakonczeniePartii_PytaOPotwierdzenie()
    {
        // Przycisk stoi w narożniku paska górnego, gdzie trafia się w niego przypadkiem,
        // a partia nie ma jak wrócić do stanu sprzed zakończenia.
        _dialogs.ConfirmAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(Task.FromResult(false));

        GameViewModel viewModel = CreateSubscribedViewModel();

        await viewModel.EndGameCommand.ExecuteAsync(parameter: null);

        await _engine.DidNotReceive().EndAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PotwierdzoneZakonczenie_KonczyPartie()
    {
        GameViewModel viewModel = CreateSubscribedViewModel();

        await viewModel.EndGameCommand.ExecuteAsync(parameter: null);

        await _engine.Received(1).EndAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PartiaBezWydarzen_NieTrzymaMiejscaNaZapowiedz()
    {
        // Zarezerwowane miejsce chroni układ przed skakaniem, ale przy grze bez wydarzeń
        // nigdy nie zostanie użyte — a ekran rozgrywki i tak jest ciasny.
        GameViewModel viewModel = CreateSubscribedViewModel();

        await WaitUntilAsync(() => viewModel.SetupItems.Count > 0);

        Assert.False(viewModel.CanShowEvents);
        Assert.False(viewModel.IsEventSlotVisible);
    }

    [Fact]
    public void LiniaOGlosie_PokazujeKomendeZamiastStanuMikrofonu()
    {
        // Dwa osobne wiersze zabierały tyle miejsca, ile cały rząd przycisków, a nigdy nie były
        // potrzebne jednocześnie: komenda pada wtedy, gdy mikrofon przestał słuchać.
        GameViewModel viewModel = CreateSubscribedViewModel();

        _voiceControl.SetState(VoiceControlState.Waiting);

        Assert.Equal(viewModel.VoiceStatusText, viewModel.VoiceLineText);

        _voiceControl.RaiseCommand(VoiceCommandType.Next);

        Assert.Equal(viewModel.VoiceCommandText, viewModel.VoiceLineText);
        Assert.True(viewModel.IsVoiceLineVisible);
    }

    [Fact]
    public void WylosowanyRuch_ZglaszaEfektDzwiekowy()
    {
        // Zdarzenie pada przy rozegranej turze, a nie przy komunikacie o ruchu: silnik zgłasza
        // turę przed odczytaniem czegokolwiek, więc dźwięk zdąży wybrzmieć przed poleceniem.
        GameViewModel viewModel = CreateSubscribedViewModel();

        RaiseTurnPlayed();

        Assert.Contains(FeedbackMoment.MoveRevealed, _feedback.Moments);
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void ZapowiedzWydarzenia_ZglaszaWlasnyEfekt()
    {
        GameViewModel viewModel = CreateSubscribedViewModel();

        RaiseAnnouncement(new Announcement("Wydarzenie: Zamiana miejsc.", AnnouncementKind.Event));

        Assert.Contains(FeedbackMoment.EventAnnounced, _feedback.Moments);
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void KomunikatORuchu_NieZglaszaEfektuWydarzenia()
    {
        // Efekt wydarzenia ma padać raz, przy wydarzeniu — nie przy każdym komunikacie.
        GameViewModel viewModel = CreateSubscribedViewModel();

        RaiseAnnouncement(new Announcement("Kuba, prawa ręka — czerwony.", AnnouncementKind.Move));

        Assert.DoesNotContain(FeedbackMoment.EventAnnounced, _feedback.Moments);
        Assert.NotNull(viewModel);
    }

    [Fact]
    public async Task ZgloszenieOdpadniecia_ZglaszaEfektOdpadniecia()
    {
        GameSession session = new([Player.Create("Kuba", 0), Player.Create("Anna", 1)], 12);
        session.Start();

        _engine.Session.Returns(session);
        _engine.IsEliminationEnabled.Returns(true);

        GameViewModel viewModel = CreateSubscribedViewModel();

        await viewModel.EliminatePlayerCommand.ExecuteAsync(viewModel.Players[0]);

        Assert.Contains(FeedbackMoment.PlayerEliminated, _feedback.Moments);
    }

    [Fact]
    public async Task DotknieciePigulkiGraczaKtoryOdpadl_NieZglaszaEfektu()
    {
        // Pigułka gracza, który już odpadł, nie robi nic — także nie brzmi.
        GameSession session = new(
            [Player.Create("Kuba", 0), Player.Create("Anna", 1) with { IsEliminated = true }],
            12);

        session.Start();

        _engine.Session.Returns(session);
        _engine.IsEliminationEnabled.Returns(true);

        GameViewModel viewModel = CreateSubscribedViewModel();
        PlayerListItem odpadl = viewModel.Players.Single(player => player.IsEliminated);

        await viewModel.EliminatePlayerCommand.ExecuteAsync(odpadl);

        Assert.Empty(_feedback.Moments);
    }

    [Fact]
    public void KoniecPartii_ZglaszaEfektKonca()
    {
        GameViewModel viewModel = CreateSubscribedViewModel();

        _engine.GameFinished += Raise.Event<EventHandler<GameSummary>>(
            _engine,
            new GameSummary(2, 8, 0, TimeSpan.FromSeconds(60), [], null));

        Assert.Contains(FeedbackMoment.GameFinished, _feedback.Moments);
        Assert.NotNull(viewModel);
    }

    [Fact]
    public async Task WylaczoneDzwieki_ZatrzymujaTykanieOdliczania()
    {
        // Tykanie idzie osobnym portem niż efekty, ale jest dźwiękiem gry jak każdy inny —
        // gracz, który wyciszył aplikację, nie spodziewa się, że zegar dalej tyka.
        FakeSettingsService settings = new();
        await settings.UpdateAsync(current => current with { AreSoundsEnabled = false });

        GameViewModel viewModel = CreateSubscribedViewModel(settings);

        RaiseCountdown(new TurnCountdown(
            TurnCountdownKind.Move,
            TimeSpan.FromSeconds(10),
            TimeProvider.System.GetTimestamp()));

        Assert.Empty(_audioCues.Played);
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void OnDisappearing_ZwalniaSubskrypcjeSilnika()
    {
        // Silnik jest singletonem, a ViewModel powstaje na każde wejście na ekran.
        // Subskrypcja bez zwolnienia trzymałaby w pamięci każdą dotychczasową instancję.
        GameViewModel viewModel = CreateSubscribedViewModel();

        viewModel.OnDisappearing();
        RaiseAnnouncement(new Announcement("Nowy komunikat", AnnouncementKind.Move));

        Assert.NotEqual("Nowy komunikat", viewModel.AnnouncementText);
    }

    private void RaiseAnnouncement(Announcement announcement) =>
        _engine.AnnouncementRaised += Raise.Event<EventHandler<Announcement>>(_engine, announcement);

    /// <summary>
    /// Zgłasza zmianę odliczania.
    /// </summary>
    /// <remarks>
    /// Argumenty są przekazywane jako tablica z jawnym typem: <c>null</c> w roli argumentu
    /// zdarzenia jest poprawną wartością (koniec odliczania), ale wprost trafia na sprawdzanie
    /// wartości nullowalnych w NSubstitute.
    /// </remarks>
    private void RaiseCountdown(TurnCountdown? countdown) =>
        _engine.CountdownChanged += Raise.Event<EventHandler<TurnCountdown?>>(
            new object?[] { _engine, countdown }!);

    private void RaiseTurnPlayed() =>
        _engine.TurnPlayed += Raise.Event<EventHandler<Turn>>(
            _engine,
            new Turn
            {
                Number = 2,
                Player = Player.Create("Anna", 1),
                Move = new Move(BodyPart.LeftFoot, SpinColor.Green),
            });

    [Fact]
    public void WejscieNaEkran_WlaczaReklamyIRezerwujeMiejsceNaBaner()
    {
        // Miejsce na baner jest rezerwowane od wejścia na ekran, a nie po wczytaniu reklamy:
        // baner wchodzący w gotowy układ przesuwałby przyciski pod palcem gracza.
        _ads.IsBannerAllowed.Returns(true);

        GameViewModel viewModel = CreateSubscribedViewModel();

        Assert.True(viewModel.IsBannerVisible);
        _ = _ads.Received(1).ActivateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ZejscieZEkranu_ChowaBanerIWylaczaReklamy()
    {
        // Baner jest wyłącznie na ekranie rozgrywki — na startowym i w ustawieniach nie ma
        // żadnego (ustalenie z użytkownikiem).
        _ads.IsBannerAllowed.Returns(true);

        GameViewModel viewModel = CreateSubscribedViewModel();

        viewModel.OnDisappearing();

        Assert.False(viewModel.IsBannerVisible);
        _ = _ads.Received(1).DeactivateAsync();
    }

    [Fact]
    public void GdyReklamNieMaWTymWydaniu_MiejsceNaBanerNieJestTrzymane()
    {
        _ads.IsBannerAllowed.Returns(false);

        GameViewModel viewModel = CreateSubscribedViewModel();

        Assert.False(viewModel.IsBannerVisible);
    }

    [Fact]
    public void ZgodaUzyskanaPoWejsciuNaEkran_PokazujeMiejsceNaBaner()
    {
        // Przygotowanie zestawu SDK i pytanie o zgodę trwają, więc odpowiedź przychodzi
        // po chwili od wejścia na ekran — zdarzeniem, nie przy odpytaniu.
        _ads.IsBannerAllowed.Returns(false);

        GameViewModel viewModel = CreateSubscribedViewModel();

        Assert.False(viewModel.IsBannerVisible);

        _ads.BannerAllowedChanged += Raise.Event<EventHandler<bool>>(_ads, true);

        Assert.True(viewModel.IsBannerVisible);
    }

    [Fact]
    public async Task PrzelacznikSterowania_ZapisujeTrybWUstawieniachIStosujeGoDoPartii()
    {
        // Ustawienia są jedynym źródłem prawdy — ekran ustawień musi pokazać to samo, co
        // przycisk na ekranie rozgrywki, bo inaczej gracz zobaczy dwie różne odpowiedzi
        // na to samo pytanie.
        FakeSettingsService ustawienia = new();
        GameViewModel viewModel = CreateSubscribedViewModel(ustawienia);

        Assert.Equal(GameControlMode.Manual, viewModel.ControlMode);

        await viewModel.CycleControlModeCommand.ExecuteAsync(null);

        Assert.Equal(GameControlMode.Automatic, viewModel.ControlMode);
        Assert.Equal(TurnAdvanceMode.Automatic, ustawienia.Current.TurnAdvanceMode);

        await _engine.Received(1).ChangeTurnControlAsync(
            TurnAdvanceMode.Automatic,
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PrzelacznikSterowania_GdyMikrofonNiedostepny_WracaNaReczny()
    {
        // Napis „głosowo" nad grą, której nikt nie słucha, jest gorszy niż samo
        // niepowodzenie: gracz czeka wtedy na reakcję, która nie nadejdzie.
        _voiceControl.CanPrepare = false;
        _voiceControl.StateAfterFailedPrepare = VoiceControlState.Unavailable;

        FakeSettingsService ustawienia = new();
        GameViewModel viewModel = CreateSubscribedViewModel(ustawienia);

        // Ręczny -> automatyczny -> głosowy, czyli dwa dotknięcia.
        await viewModel.CycleControlModeCommand.ExecuteAsync(null);
        await viewModel.CycleControlModeCommand.ExecuteAsync(null);

        Assert.Equal(GameControlMode.Manual, viewModel.ControlMode);
        Assert.False(ustawienia.Current.IsVoiceControlEnabled);
        Assert.Equal(TurnAdvanceMode.Manual, ustawienia.Current.TurnAdvanceMode);
    }

    private GameViewModel CreateSubscribedViewModel(FakeSettingsService? settings = null)
    {
        GameViewModel viewModel = new(
            Substitute.For<INavigationService>(),
            _engine,
            _roster,
            settings ?? new FakeSettingsService(),
            _eventPacks,
            _gameModes,
            _voiceControl,
            _voiceCoordinator,
            _ads,
            new ImmediateUiDispatcher(),
            _audioCues,
            _feedback,
            TimeProvider.System,
            NullLogger<GameViewModel>.Instance,
            _dialogs,
            new FakeLocalizationService());

        viewModel.OnAppearing();

        return viewModel;
    }
}
