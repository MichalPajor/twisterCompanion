using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Application.VoiceControl;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy rytmu nasłuchu: kiedy okno się otwiera, kiedy zamyka i co robią komendy.
/// </summary>
/// <remarks>
/// Ten zestaw pilnuje przebiegu tury ustalonego przy testach na urządzeniu: odczyt komunikatu
/// przy zamkniętym mikrofonie, czas na wykonanie ruchu, dopiero potem nasłuch.
/// </remarks>
public class VoiceControlCoordinatorTests
{
    private static readonly VoiceControlOptions FastOptions = new()
    {
        SessionRestartDelay = TimeSpan.FromMilliseconds(20),
        CueGap = TimeSpan.FromMilliseconds(10),
        ThrottleBackoff = TimeSpan.FromMilliseconds(20),
    };

    [Fact]
    public async Task WTrakcieOdczytuKomunikatu_MikrofonMilczy()
    {
        // Najważniejsza reguła całego etapu: nasłuch nie może pracować, kiedy mówi aplikacja.
        using GameTestHarness harness = await CreateActivatedHarnessAsync();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        // Partia właśnie ogłosiła turę i czeka na graczy — okno otworzy się dopiero po
        // czasie na wykonanie ruchu, a nie natychmiast.
        Assert.Empty(harness.Recognition.StartedSessions);
    }

    [Fact]
    public async Task PoCzasieNaRuch_OknoNasluchuSieOtwiera()
    {
        using GameTestHarness harness = await CreateActivatedHarnessAsync(
            voiceListeningDelay: TimeSpan.FromSeconds(3));

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        Assert.Empty(harness.Recognition.StartedSessions);

        await WaitUntilAsync(harness, () => harness.Recognition.StartedSessions.Count > 0);
    }

    [Fact]
    public async Task KomendaDalej_RozgrywaNastepnaTure()
    {
        using GameTestHarness harness = await CreateActivatedHarnessAsync(
            voiceListeningDelay: TimeSpan.FromSeconds(3));

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        await WaitUntilAsync(harness, () => harness.Recognition.IsListening);

        int tura = harness.Engine.Session!.TurnNumber;
        harness.Recognition.RaisePartial("dalej");

        await WaitUntilAsync(harness, () => harness.Engine.Session!.TurnNumber > tura);

        Assert.Equal(tura + 1, harness.Engine.Session.TurnNumber);
    }

    [Fact]
    public async Task ZgloszenieOdpadniecia_NieJestKomendaGlosowa()
    {
        // Komenda „gracz odpadł" została usunięta: nie mówi, KTÓRY gracz odpadł, a przy
        // kilku osobach na macie to jedyna informacja, która się liczy. Zostaje przycisk
        // obok imienia.
        using GameTestHarness harness = await CreateActivatedHarnessAsync(
            voiceListeningDelay: TimeSpan.FromSeconds(3));

        await harness.Engine.StartAsync(GameTestHarness.Configuration(3));

        await WaitUntilAsync(harness, () => harness.Recognition.IsListening);

        harness.Recognition.RaisePartial("gracz odpadł");

        // Chwila na wykonanie się ewentualnej komendy — nic nie powinno się stać.
        await Task.Delay(100);

        Assert.Empty(harness.Engine.Session!.EliminationOrder);
    }

    [Fact]
    public async Task WTrybieAutomatycznym_NasluchSieNieWlacza()
    {
        // Nie ma czym sterować: tury same następują po sobie, a odpadnięcie idzie
        // z przycisku. Otwarty mikrofon byłby wyłącznie zużyciem baterii.
        using GameTestHarness harness = new(
            useResourceLocalization: true,
            voiceControlOptions: FastOptions);

        await harness.SettingsService.UpdateAsync(settings => settings with
        {
            IsVoiceControlEnabled = true,
            TurnAdvanceMode = TurnAdvanceMode.Automatic,
        });

        Assert.False(await harness.VoiceCoordinator.ActivateAsync());
        Assert.Empty(harness.Recognition.StartedSessions);
    }

    [Fact]
    public async Task KomendaPauza_WstrzymujeRozgrywke()
    {
        using GameTestHarness harness = await CreateActivatedHarnessAsync(
            voiceListeningDelay: TimeSpan.FromSeconds(3));

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        await WaitUntilAsync(harness, () => harness.Recognition.IsListening);

        harness.Recognition.RaisePartial("pauza");

        await WaitUntilAsync(harness, () => harness.Engine.State == GameState.Paused);

        Assert.Equal(GameState.Paused, harness.Engine.State);
    }

    [Fact]
    public async Task NaPauzie_NasluchOtwieraSieBezOdczekiwania()
    {
        // Na pauzie nikt nie wykonuje ruchu, a jedyne, co gracze mogą chcieć zrobić,
        // to wznowić grę — odczekiwanie kilkunastu sekund byłoby tu bezcelowe.
        using GameTestHarness harness = await CreateActivatedHarnessAsync(
            voiceListeningDelay: TimeSpan.FromSeconds(30));

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.PauseAsync();

        await WaitUntilAsync(harness, () => harness.Recognition.StartedSessions.Count > 0);
    }

    [Fact]
    public async Task PoWznowieniu_MikrofonMilczyPrzezPonowionyCzasNaRuch()
    {
        // Zgłoszone z urządzenia: po wstrzymaniu i wznowieniu partii głosem sygnały włączenia
        // i wyłączenia nasłuchu odzywały się w trakcie odliczania czasu na ruch. Wznowienie
        // uruchamia odliczanie od nowa (silnik woła ScheduleMoveCountdown), więc nasłuch ma
        // czekać dokładnie tak samo, jak po nowo rozegranej turze — inaczej mikrofon pracuje
        // wtedy, gdy gracze układają ręce na macie, a ekran pokazuje, że mają na to czas.
        using GameTestHarness harness = await CreateActivatedHarnessAsync(
            voiceListeningDelay: TimeSpan.FromSeconds(30));

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.PauseAsync();

        await WaitUntilAsync(harness, () => harness.Recognition.StartedSessions.Count > 0);

        int sesjeNaPauzie = harness.Recognition.StartedSessions.Count;

        await harness.Engine.ResumeAsync();

        // Pięć sekund po wznowieniu odliczanie trwa — mikrofon ma milczeć.
        for (int krok = 0; krok < 100; krok++)
        {
            harness.TimeProvider.Advance(TimeSpan.FromMilliseconds(50));

            await Task.Delay(1);
        }

        Assert.Equal(sesjeNaPauzie, harness.Recognition.StartedSessions.Count);
        Assert.False(harness.Recognition.IsListening);

        // Po upływie całego czasu na ruch nasłuch wraca sam.
        await WaitUntilAsync(harness, () => harness.Recognition.StartedSessions.Count > sesjeNaPauzie);
    }

    [Fact]
    public async Task WylaczenieSterowania_ZamykaMikrofon()
    {
        using GameTestHarness harness = await CreateActivatedHarnessAsync(
            voiceListeningDelay: TimeSpan.FromSeconds(3));

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        await WaitUntilAsync(harness, () => harness.Recognition.IsListening);

        await harness.VoiceCoordinator.DeactivateAsync();

        Assert.False(harness.Recognition.IsListening);
        Assert.False(harness.VoiceCoordinator.IsActive);
    }

    [Fact]
    public async Task WylaczoneWUstawieniach_NieAktywujeSie()
    {
        using GameTestHarness harness = new(
            useResourceLocalization: true,
            voiceControlOptions: FastOptions);

        Assert.False(await harness.VoiceCoordinator.ActivateAsync());
        Assert.False(harness.VoiceCoordinator.IsActive);
    }

    private static async Task<GameTestHarness> CreateActivatedHarnessAsync(
        TimeSpan? voiceListeningDelay = null)
    {
        GameTestHarness harness = new(
            useResourceLocalization: true,
            voiceControlOptions: FastOptions);

        await harness.SettingsService.UpdateAsync(settings => settings with
        {
            IsVoiceControlEnabled = true,
            VoiceListeningDelay = voiceListeningDelay ?? AppSettings.Default.VoiceListeningDelay,
        });

        Assert.True(await harness.VoiceCoordinator.ActivateAsync());

        return harness;
    }

    /// <summary>
    /// Czeka na warunek, przesuwając w tym czasie sterowany zegar.
    /// </summary>
    /// <remarks>
    /// Czas na wykonanie ruchu to sekundy, a odstępy w pętli nasłuchu — milisekundy, i jedno
    /// czeka na drugie. Przesuwanie zegara małymi krokami w rytm odpytywania załatwia oba
    /// naraz: test kończy się w setkach milisekund realnego czasu, zamiast odmierzać sekundy
    /// przeznaczone na ruch gracza.
    /// </remarks>
    private static async Task WaitUntilAsync(GameTestHarness harness, Func<bool> condition)
    {
        for (int attempt = 0; attempt < 400; attempt++)
        {
            if (condition())
            {
                return;
            }

            harness.TimeProvider.Advance(TimeSpan.FromMilliseconds(50));

            await Task.Delay(5);
        }

        Assert.Fail("Warunek nie został spełniony w wyznaczonym czasie.");
    }
}
