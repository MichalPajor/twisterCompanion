using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Application.VoiceControl;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy okna nasłuchu: kolejność sygnałów, reakcja na komendę i zachowanie przy odmowach.
/// </summary>
/// <remarks>
/// Pętla nasłuchu chodzi w tle, więc testy czekają na skutek przez odpytywanie, a nie na
/// zakończenie metody. Czas płynie prawdziwy — sterowany zegar nie pomoże, bo pętla czeka
/// jednocześnie na zdarzenie z platformy i na upływ czasu.
/// </remarks>
public class VoiceControlServiceTests
{
    /// <summary>Parametry przyspieszone, żeby testy nie odmierzały realnych sekund.</summary>
    private static readonly VoiceControlOptions FastOptions = new()
    {
        SessionRestartDelay = TimeSpan.FromMilliseconds(20),
        CueGap = TimeSpan.FromMilliseconds(10),
        ThrottleBackoff = TimeSpan.FromMilliseconds(20),
        DebounceWindow = TimeSpan.FromMilliseconds(300),
    };

    [Fact]
    public async Task WylaczoneWUstawieniach_NieNasluchuje()
    {
        using GameTestHarness harness = CreateHarness();

        Assert.False(await harness.VoiceControl.PrepareAsync());
        Assert.Equal(VoiceControlState.Disabled, harness.VoiceControl.State);

        await harness.VoiceControl.OpenWindowAsync();

        Assert.Empty(harness.Recognition.StartedSessions);
    }

    [Fact]
    public async Task BrakZgodyNaMikrofon_NieWlaczaNasluchu()
    {
        using GameTestHarness harness = CreateHarness();
        await EnableVoiceControlAsync(harness);
        harness.Recognition.IsPermissionGranted = false;

        Assert.False(await harness.VoiceControl.PrepareAsync());
        Assert.Equal(VoiceControlState.Disabled, harness.VoiceControl.State);
    }

    [Fact]
    public async Task BrakRozpoznawaniaNaUrzadzeniu_ZglaszaNiedostepnosc()
    {
        using GameTestHarness harness = CreateHarness();
        await EnableVoiceControlAsync(harness);
        harness.Recognition.Capabilities = new SpeechRecognitionCapabilities(
            IsSystemRecognitionAvailable: false,
            IsOnDeviceRecognitionAvailable: false,
            PlatformDescription: "Test bez rozpoznawania");

        Assert.False(await harness.VoiceControl.PrepareAsync());
        Assert.Equal(VoiceControlState.Unavailable, harness.VoiceControl.State);
    }

    [Fact]
    public async Task TrybNaUrzadzeniu_WygrywaGdyJestDostepny()
    {
        // Brak limitów usługi, brak zależności od sieci, głos nie opuszcza telefonu —
        // przy dostępnym trybie lokalnym nie ma powodu wybierać systemowego.
        using GameTestHarness harness = CreateHarness();
        await EnableVoiceControlAsync(harness);

        await harness.VoiceControl.PrepareAsync();
        await harness.VoiceControl.OpenWindowAsync();
        await WaitUntilAsync(() => harness.Recognition.StartedSessions.Count > 0);

        Assert.Equal(SpeechRecognitionMode.OnDevice, harness.Recognition.StartedSessions[0].Mode);

        await harness.VoiceControl.CloseWindowAsync();
    }

    [Fact]
    public async Task TrybSystemowy_GdyLokalnegoNieMa()
    {
        using GameTestHarness harness = CreateHarness();
        await EnableVoiceControlAsync(harness);
        harness.Recognition.Capabilities = new SpeechRecognitionCapabilities(
            IsSystemRecognitionAvailable: true,
            IsOnDeviceRecognitionAvailable: false,
            PlatformDescription: "Android 12");

        await harness.VoiceControl.PrepareAsync();
        await harness.VoiceControl.OpenWindowAsync();
        await WaitUntilAsync(() => harness.Recognition.StartedSessions.Count > 0);

        Assert.Equal(SpeechRecognitionMode.System, harness.Recognition.StartedSessions[0].Mode);

        await harness.VoiceControl.CloseWindowAsync();
    }

    [Fact]
    public async Task OtwarcieOkna_NajpierwSygnalPotemMikrofon()
    {
        // Sygnał musi wybrzmieć przed otwarciem mikrofonu, inaczej rozpoznawanie usłyszy
        // własne piknięcie i zmarnuje na nie sesję.
        using GameTestHarness harness = CreateHarness();
        await PrepareAsync(harness);

        await harness.VoiceControl.OpenWindowAsync();
        await WaitUntilAsync(() => harness.Recognition.StartedSessions.Count > 0);

        Assert.Equal(AudioCue.ListeningStarted, harness.AudioCues.Played[0]);

        await harness.VoiceControl.CloseWindowAsync();
    }

    [Fact]
    public async Task AnulowanieOknaZZewnatrz_ZwalniaMikrofon()
    {
        // Token okna nasłuchu jest powiązany z tokenem koordynatora, więc każda zmiana stanu
        // partii przerywa pętlę w środku sesji — i to jest zwykła droga, nie awaria. Mikrofon
        // musi wtedy zostać zwolniony tak samo jak przy zamknięciu okna. Bez tego
        // rozpoznawanie zostawało włączone, a urządzenie odzywało się własnymi sygnałami
        // początku i końca nasłuchu w chwili, gdy aplikacja uważała mikrofon za zamknięty.
        using GameTestHarness harness = CreateHarness();
        await PrepareAsync(harness);

        using CancellationTokenSource okno = new();

        await harness.VoiceControl.OpenWindowAsync(okno.Token);
        await WaitUntilAsync(() => harness.Recognition.IsListening);

        await okno.CancelAsync();

        await WaitUntilAsync(() => !harness.Recognition.IsListening);

        Assert.Equal(VoiceControlState.Idle, harness.VoiceControl.State);
    }

    [Fact]
    public async Task KomendaZWynikuCzastkowego_ZamykaOknoIPotwierdza()
    {
        // Sedno szybkości: nie czekamy na wynik finalny, bo ten przychodzi dopiero wtedy,
        // gdy rozpoznawacz uzna, że mówiący skończył.
        using GameTestHarness harness = CreateHarness();
        await PrepareAsync(harness);

        List<VoiceCommandType> komendy = [];
        harness.VoiceControl.CommandRecognized += (_, command) => komendy.Add(command);

        await harness.VoiceControl.OpenWindowAsync();
        await WaitUntilAsync(() => harness.Recognition.IsListening);

        harness.Recognition.RaisePartial("no dalej");

        await WaitUntilAsync(() => komendy.Count > 0);

        Assert.Equal([VoiceCommandType.Next], komendy);
        Assert.Contains(AudioCue.CommandAccepted, harness.AudioCues.Played);
        Assert.DoesNotContain(AudioCue.ListeningStopped, harness.AudioCues.Played);
        Assert.True(harness.Recognition.StopCount > 0);
    }

    [Fact]
    public async Task KomendaZWynikuFinalnego_TezDziala()
    {
        // Na wolniejszych urządzeniach wyniki częściowe potrafią wcale nie przyjść.
        using GameTestHarness harness = CreateHarness();
        await PrepareAsync(harness);

        List<VoiceCommandType> komendy = [];
        harness.VoiceControl.CommandRecognized += (_, command) => komendy.Add(command);

        await harness.VoiceControl.OpenWindowAsync();
        await WaitUntilAsync(() => harness.Recognition.IsListening);

        harness.Recognition.CompleteWith("powtórz");

        await WaitUntilAsync(() => komendy.Count > 0);

        Assert.Equal([VoiceCommandType.Repeat], komendy);
    }

    [Fact]
    public async Task TaSamaKomendaDwaRazy_JestWykonanaRaz()
    {
        // Ta sama fraza przychodzi zwykle dwa razy: jako wynik częściowy i jako finalny.
        // Bez wyciszenia „Dalej" rozegrałoby dwie tury i pominęło gracza.
        using GameTestHarness harness = CreateHarness();
        await PrepareAsync(harness);

        List<VoiceCommandType> komendy = [];
        harness.VoiceControl.CommandRecognized += (_, command) => komendy.Add(command);

        await harness.VoiceControl.OpenWindowAsync();
        await WaitUntilAsync(() => harness.Recognition.IsListening);

        harness.Recognition.RaisePartial("dalej");
        harness.Recognition.CompleteWith("dalej");

        await WaitUntilAsync(() => komendy.Count > 0);
        await Task.Delay(100);

        Assert.Equal([VoiceCommandType.Next], komendy);
    }

    [Fact]
    public async Task BrakKomendy_ZamykaSesjeSygnalemIWznawiaNasluch()
    {
        // Przebieg wymagany przez scenariusz: sesja bez komendy → sygnał zamknięcia →
        // przerwa → sygnał otwarcia → nowa sesja.
        using GameTestHarness harness = CreateHarness();
        await PrepareAsync(harness);

        await harness.VoiceControl.OpenWindowAsync();
        await WaitUntilAsync(() => harness.Recognition.IsListening);

        harness.Recognition.CompleteWithError(SpeechRecognitionError.NoMatch);

        await WaitUntilAsync(() => harness.Recognition.StartedSessions.Count >= 2);

        Assert.Equal(
            [AudioCue.ListeningStarted, AudioCue.ListeningStopped, AudioCue.ListeningStarted],
            harness.AudioCues.Played.Take(3));

        await harness.VoiceControl.CloseWindowAsync();
    }

    [Fact]
    public async Task OdmowyUslugi_WstrzymujaSterowanieGlosem()
    {
        // Trzy odmowy z rzędu oznaczają, że dalsze próby tylko pogarszają sprawę.
        using GameTestHarness harness = CreateHarness();
        await PrepareAsync(harness);

        await harness.VoiceControl.OpenWindowAsync();

        for (int attempt = 0; attempt < 3; attempt++)
        {
            int expectedSessions = attempt + 1;
            await WaitUntilAsync(() => harness.Recognition.StartedSessions.Count >= expectedSessions);
            harness.Recognition.CompleteWithError(SpeechRecognitionError.TooManyRequests);
        }

        await WaitUntilAsync(() => harness.VoiceControl.State == VoiceControlState.Unavailable);

        Assert.Equal(VoiceControlState.Unavailable, harness.VoiceControl.State);
    }

    [Fact]
    public async Task MowaAplikacji_ZamykaMikrofon()
    {
        // Komunikat „Kuba, prawa ręka — czerwony" zawiera słowa z tego samego języka,
        // więc otwarty mikrofon usłyszałby aplikację i potraktował ją jako gracza.
        using GameTestHarness harness = CreateHarness();
        await PrepareAsync(harness);

        await harness.VoiceControl.OpenWindowAsync();
        await WaitUntilAsync(() => harness.Recognition.IsListening);

        await harness.Speaker.SpeakAsync(harness.AnnouncementBuilder.BuildVoiceSample());

        await WaitUntilAsync(() => !harness.Recognition.IsListening);

        Assert.False(harness.Recognition.IsListening);
    }

    private static GameTestHarness CreateHarness() =>
        new(useResourceLocalization: true, voiceControlOptions: FastOptions, useRealTime: true);

    private static async Task EnableVoiceControlAsync(GameTestHarness harness) =>
        await harness.SettingsService.UpdateAsync(settings => settings with
        {
            IsVoiceControlEnabled = true,
        });

    private static async Task PrepareAsync(GameTestHarness harness)
    {
        await EnableVoiceControlAsync(harness);

        Assert.True(await harness.VoiceControl.PrepareAsync());
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 300; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.Fail("Warunek nie został spełniony w wyznaczonym czasie.");
    }
}
