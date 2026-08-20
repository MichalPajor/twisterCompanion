using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Feedback;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Application.Voice;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy reguł decydujących, kiedy aplikacja wydaje dźwięk i wibruje.
/// </summary>
/// <remarks>
/// Realizacja kryteriów ukończenia Etapu 11. Dwa z nich są regułami, które da się sprawdzić
/// bez urządzenia i dlatego <b>muszą</b> siedzieć w warstwie aplikacji, a nie w kodzie
/// platformowym: „dźwięki nie nakładają się na mowę" i „wyłączenie dźwięków wycisza wszystko
/// poza mową". Trzecie kryterium (brak wycieków przy 200 odtworzeniach) sprawdza pula dźwięków
/// systemu i test manualny — tego z linii poleceń nie zmierzę.
/// </remarks>
public class GameFeedbackTests
{
    [Fact]
    public void WlaczoneDzwieki_OdtwarzajaEfektZGlosnosciaZUstawien()
    {
        using GameTestHarness harness = new();

        harness.Feedback.Play(FeedbackMoment.MoveRevealed);

        Assert.Equal(
            [(SoundEffect.MoveRevealed, AppSettings.Default.SoundVolume)],
            harness.Sounds.Played);
    }

    [Fact]
    public async Task WylaczoneDzwieki_NieOdtwarzajaNiczego()
    {
        using GameTestHarness harness = new();
        await harness.SettingsService.UpdateAsync(settings => settings with { AreSoundsEnabled = false });

        harness.Feedback.Play(FeedbackMoment.GameStarted);

        Assert.Empty(harness.Sounds.Played);
    }

    [Fact]
    public async Task ZerowaGlosnosc_NieZawracaGlowyOdtwarzaczowi()
    {
        using GameTestHarness harness = new();
        await harness.SettingsService.UpdateAsync(settings => settings with { SoundVolume = 0.0 });

        harness.Feedback.Play(FeedbackMoment.EventAnnounced);

        Assert.Empty(harness.Sounds.Played);
    }

    [Fact]
    public async Task WTrakcieMowy_EfektMilczy()
    {
        // Kryterium z planu: dźwięki nie nakładają się na odczyt. Polecenie „Anna, prawa ręka,
        // czerwony" ma zostać zrozumiane, a nie przykryte fanfarą.
        using GameTestHarness harness = new();

        harness.TextToSpeech.Gate = new TaskCompletionSource();

        Task speaking = harness.Speaker.SpeakAsync(
            new Announcement("Kuba, prawa ręka — czerwony.", AnnouncementKind.Move));

        await WaitUntilAsync(() => harness.Speaker.IsSpeaking);

        harness.Feedback.Play(FeedbackMoment.MoveRevealed);

        Assert.Empty(harness.Sounds.Played);

        harness.TextToSpeech.Gate.SetResult();
        await speaking;
    }

    [Fact]
    public async Task PoZakonczeniuMowy_EfektZnowuGra()
    {
        using GameTestHarness harness = new();

        await harness.Speaker.SpeakAsync(new Announcement("Koniec gry.", AnnouncementKind.GameEnd));

        harness.Feedback.Play(FeedbackMoment.GameFinished);

        Assert.Single(harness.Sounds.Played);
    }

    [Fact]
    public async Task WTrakcieMowy_WibracjaNadalDziala()
    {
        // Wibracja nie wchodzi w słowo, bo nie jest dźwiękiem — a przy wyciszonym telefonie
        // jest jedyną informacją, która dochodzi do graczy.
        using GameTestHarness harness = new();

        harness.TextToSpeech.Gate = new TaskCompletionSource();

        Task speaking = harness.Speaker.SpeakAsync(
            new Announcement("Kuba, prawa ręka — czerwony.", AnnouncementKind.Move));

        await WaitUntilAsync(() => harness.Speaker.IsSpeaking);

        harness.Feedback.Play(FeedbackMoment.PlayerEliminated);

        Assert.Equal([HapticIntensity.Strong], harness.Haptics.Vibrations);

        harness.TextToSpeech.Gate.SetResult();
        await speaking;
    }

    [Fact]
    public async Task WylaczoneDzwieki_NieWylaczajaWibracji()
    {
        // Osobne przełączniki, bo służą do różnych rzeczy: dźwięk informuje wszystkich
        // w pokoju, wibracja działa przy wyciszonym telefonie.
        using GameTestHarness harness = new();
        await harness.SettingsService.UpdateAsync(settings => settings with { AreSoundsEnabled = false });

        harness.Feedback.Play(FeedbackMoment.EventAnnounced);

        Assert.Equal([HapticIntensity.Strong], harness.Haptics.Vibrations);
    }

    [Fact]
    public async Task WylaczoneWibracje_NieWylaczajaDzwiekow()
    {
        using GameTestHarness harness = new();
        await harness.SettingsService.UpdateAsync(settings => settings with { AreHapticsEnabled = false });

        harness.Feedback.Play(FeedbackMoment.EventAnnounced);

        Assert.Single(harness.Sounds.Played);
        Assert.Empty(harness.Haptics.Vibrations);
    }

    [Fact]
    public void ZwyklyRuch_NieWibruje()
    {
        // Wibracja przy każdej turze zamieniłaby się w tło, którego nikt już nie zauważa.
        using GameTestHarness harness = new();

        harness.Feedback.Play(FeedbackMoment.MoveRevealed);

        Assert.Empty(harness.Haptics.Vibrations);
    }

    [Fact]
    public void NacisniecePrzycisku_WibrujeKrotko()
    {
        using GameTestHarness harness = new();

        harness.Feedback.Play(FeedbackMoment.ButtonTap);

        Assert.Equal([HapticIntensity.Light], harness.Haptics.Vibrations);
    }

    [Theory]
    [InlineData(FeedbackMoment.MoveRevealed, SoundEffect.MoveRevealed)]
    [InlineData(FeedbackMoment.EventAnnounced, SoundEffect.EventTriggered)]
    [InlineData(FeedbackMoment.PlayerEliminated, SoundEffect.PlayerEliminated)]
    [InlineData(FeedbackMoment.GameStarted, SoundEffect.GameStarted)]
    [InlineData(FeedbackMoment.GameFinished, SoundEffect.GameFinished)]
    [InlineData(FeedbackMoment.ButtonTap, SoundEffect.ButtonTap)]
    public void KazdeZdarzenie_MaWlasnyEfekt(FeedbackMoment moment, SoundEffect expected)
    {
        // Podpięcie dwóch zdarzeń pod jedną próbkę jest dopuszczalne, ale ma być decyzją —
        // ten test pokazuje, co pod co jest podpięte, i wywali się przy pomyłce.
        using GameTestHarness harness = new();

        harness.Feedback.Play(moment);

        Assert.Equal(expected, Assert.Single(harness.Sounds.Played).Effect);
    }

    [Fact]
    public void AwariaOdtwarzacza_NiePrzerywaPartii()
    {
        // Brak dźwięku pogarsza wrażenie, ale nie może wywalić gry.
        using GameTestHarness harness = new();

        harness.Sounds.FailWith = new InvalidOperationException("odtwarzacz padł");

        harness.Feedback.Play(FeedbackMoment.GameFinished);
    }

    [Fact]
    public async Task AwariaWczytywania_NiePrzerywaStartuAplikacji()
    {
        using GameTestHarness harness = new();

        harness.Sounds.FailPreloadWith = new InvalidOperationException("brak plików");

        await harness.Feedback.PreloadAsync();

        Assert.Equal(1, harness.Sounds.PreloadCount);
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
}
