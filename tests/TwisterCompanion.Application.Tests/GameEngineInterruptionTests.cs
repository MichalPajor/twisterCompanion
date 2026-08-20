using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy przerywania tury w trakcie odczytu: zakończenie partii, wstrzymanie i zejście
/// z ekranu rozgrywki.
/// </summary>
/// <remarks>
/// Tura nie jest pojedynczą operacją, tylko sekwencją rozciągniętą w czasie: wywołanie
/// gracza, przerwa, wydarzenie, czas na jego wykonanie, polecenie ruchu. Trwa kilkanaście
/// sekund i przez ten czas gracze mogą zrobić wszystko — wyjść z ekranu, zakończyć partię,
/// wstrzymać ją. Zestaw pilnuje jednej zasady: <b>po przerwaniu partii aplikacja milknie</b>,
/// zamiast dokańczać sekwencję, która straciła sens.
/// <para>
/// Zgłoszone z urządzenia: „komunikaty głosowe wybrzmiewały mimo wyjścia z ekranu rozgrywki
/// albo zakończenia partii".
/// </para>
/// </remarks>
public class GameEngineInterruptionTests
{
    [Fact]
    public async Task ZakonczeniePartiiWTrakcieOdczytu_UciszaResztęTury()
    {
        using GameTestHarness harness = new(useResourceLocalization: true);
        List<(string Text, GameState State)> zgloszone = [];

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        harness.Engine.AnnouncementRaised += (_, announcement) =>
            zgloszone.Add((announcement.Text, harness.Engine.State));

        // Wypowiedź zatrzymana w połowie: tura stoi na pierwszym komunikacie, tak jak na
        // urządzeniu stoi na wywołaniu gracza.
        TaskCompletionSource brama = new();
        harness.TextToSpeech.Gate = brama;

        Task tura = harness.Engine.NextTurnAsync();
        Task zakonczenie = harness.Engine.EndAsync();

        brama.SetResult();
        harness.TextToSpeech.Gate = null;

        await tura;
        await zakonczenie;

        Assert.Equal(GameState.Finished, harness.Engine.State);

        // Po zakończeniu partii wolno paść wyłącznie zapowiedzi samego zakończenia —
        // nic z przerwanej tury, w szczególności nie polecenie ruchu.
        Assert.All(
            zgloszone.Where(wpis => wpis.State == GameState.Finished),
            wpis => Assert.Equal(TekstZapowiedzi(harness, "Voice_Announce_GameEnd"), wpis.Text));
    }

    [Fact]
    public async Task WstrzymaniePartiiWTrakcieOdczytu_UciszaResztęTury()
    {
        // Ta sama sekwencja co przy zakończeniu, ale wstrzymanie jest częstsze: to ono
        // dzieje się przy zejściu z ekranu rozgrywki.
        using GameTestHarness harness = new(useResourceLocalization: true);
        List<(string Text, GameState State)> zgloszone = [];

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        harness.Engine.AnnouncementRaised += (_, announcement) =>
            zgloszone.Add((announcement.Text, harness.Engine.State));

        TaskCompletionSource brama = new();
        harness.TextToSpeech.Gate = brama;

        Task tura = harness.Engine.NextTurnAsync();
        Task wstrzymanie = harness.Engine.PauseAsync();

        brama.SetResult();
        harness.TextToSpeech.Gate = null;

        await tura;
        await wstrzymanie;

        Assert.Equal(GameState.Paused, harness.Engine.State);

        // Na pauzie wolno paść wyłącznie zapowiedzi samej pauzy — nic z przerwanej tury.
        Assert.All(
            zgloszone.Where(wpis => wpis.State == GameState.Paused),
            wpis => Assert.Equal(TekstZapowiedzi(harness, "Voice_Announce_Paused"), wpis.Text));
    }

    [Fact]
    public async Task ZejscieZEkranu_WstrzymujePartieBezZapowiedzi()
    {
        // Zejście z ekranu wstrzymuje partię, ale nie ma jej ogłaszać: ekranu już nie ma,
        // a gracz właśnie świadomie poszedł gdzie indziej. Zapowiedź „Pauza" ma sens tylko
        // wtedy, gdy pauzę wywołano głosem albo przyciskiem — czyli patrząc na ten ekran.
        using GameTestHarness harness = new(useResourceLocalization: true);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.NextTurnAsync();

        int przedWyjsciem = harness.TextToSpeech.Spoken.Count;

        await harness.Engine.PauseAsync(announce: false);

        Assert.Equal(GameState.Paused, harness.Engine.State);
        Assert.Equal(przedWyjsciem, harness.TextToSpeech.Spoken.Count);
    }

    [Fact]
    public async Task ZakonczeniePartiiWTrakcieOdliczaniaZadania_PrzerywaOdliczanie()
    {
        // Odliczanie zadania z wydarzenia trwa kilkanaście sekund i jest częścią tej samej
        // sekwencji co odczyt. Po zakończeniu partii nie ma czego odmierzać — a odliczanie
        // widoczne na podsumowaniu gry byłoby zegarem bez gry.
        using GameTestHarness harness = new(useResourceLocalization: true);
        List<TurnCountdown?> odliczania = [];

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2) with
        {
            TaskTime = TimeSpan.FromSeconds(10),
        });

        harness.Engine.CountdownChanged += (_, countdown) => odliczania.Add(countdown);

        TaskCompletionSource brama = new();
        harness.TextToSpeech.Gate = brama;

        Task tura = harness.Engine.NextTurnAsync();
        Task zakonczenie = harness.Engine.EndAsync();

        brama.SetResult();
        harness.TextToSpeech.Gate = null;

        await tura;
        await zakonczenie;

        Assert.Equal(GameState.Finished, harness.Engine.State);
        Assert.Null(harness.Engine.Countdown);
    }

    /// <summary>Treść zapowiedzi w bieżącym języku — komunikaty porównujemy po tekście.</summary>
    private static string TekstZapowiedzi(GameTestHarness harness, string klucz) =>
        harness.Localization.GetString(klucz, StringCatalog.Voice);
}
