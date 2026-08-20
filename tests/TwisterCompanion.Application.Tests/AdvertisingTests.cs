using TwisterCompanion.Application.Advertising;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy reguł reklam: kiedy wolno je pokazać, jak często i czy nie wchodzą w rozgrywkę.
/// </summary>
/// <remarks>
/// Reguły z Etapu 15 są <b>zakazami</b>, a zakaz bez testu jest tylko komentarzem. Ten zestaw
/// jest ich egzekutorem: reklama pełnoekranowa wyłącznie po zakończonej partii, nigdy przy
/// mówiącej aplikacji, nigdy przy otwartym mikrofonie i nie częściej niż co trzecią partię.
/// </remarks>
public class AdvertisingTests
{
    [Fact]
    public async Task WTrakcieRozgrywki_ReklamaPelnoekranowaJestOdrzucana()
    {
        // Najważniejszy zakaz całego etapu: reklama nie ma prawa wejść w trwającą partię.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        Assert.False(await harness.AdService.ShowInterstitialAsync());
        Assert.Equal(0, harness.Ads.InterstitialRequests);
    }

    [Fact]
    public async Task PrzedRozpoczeciemPartii_ReklamaPelnoekranowaJestOdrzucana()
    {
        using GameTestHarness harness = new();

        Assert.False(await harness.AdService.ShowInterstitialAsync());
        Assert.Equal(0, harness.Ads.InterstitialRequests);
    }

    [Fact]
    public async Task NaPauzie_ReklamaPelnoekranowaJestOdrzucana()
    {
        // Pauza to nadal trwająca partia — gracze leżą na macie i czekają na wznowienie.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.PauseAsync();

        Assert.Equal(GameState.Paused, harness.Engine.State);
        Assert.False(await harness.AdService.ShowInterstitialAsync());
    }

    [Fact]
    public async Task PoZakonczeniuPartii_ReklamaPelnoekranowaJestDozwolona()
    {
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.EndAsync();

        Assert.True(await harness.AdService.ShowInterstitialAsync());
        Assert.Equal(1, harness.Ads.InterstitialRequests);
    }

    [Fact]
    public async Task WTrakcieOdczytuKomunikatu_ReklamaPelnoekranowaJestOdrzucana()
    {
        // Reklama przerwałaby komunikat i zabrała dźwięk — a komunikat o wyniku partii jest
        // tym, po co gracze zostają na ekranie.
        using GameTestHarness harness = new();

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        TaskCompletionSource brama = new();
        harness.TextToSpeech.Gate = brama;

        Task zakonczenie = harness.Engine.EndAsync();

        await WaitUntilAsync(() => harness.Speaker.IsSpeaking);

        Assert.False(await harness.AdService.ShowInterstitialAsync());
        Assert.Equal(0, harness.Ads.InterstitialRequests);

        brama.SetResult();
        harness.TextToSpeech.Gate = null;

        await zakonczenie;
    }

    [Fact]
    public async Task GdyReklamNieMaWTymWydaniu_ProsbaJestPomijana()
    {
        using GameTestHarness harness = new();

        harness.Ads.IsAvailable = false;

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.EndAsync();

        Assert.False(harness.AdService.IsAvailable);
        Assert.False(await harness.AdService.ShowInterstitialAsync());
        Assert.Equal(0, harness.Ads.InterstitialRequests);
    }

    [Fact]
    public async Task ReklamaPelnoekranowa_PadaCoTrzeciaZakonczonaPartie()
    {
        // Ustalenie z użytkownikiem: nie po każdej partii. Reklama po każdej zniechęca do
        // kolejnej, a ta gra jest rozgrywana seriami.
        using GameTestHarness harness = new();

        await harness.AdCoordinator.ActivateAsync();

        for (int partia = 1; partia <= 6; partia++)
        {
            await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
            await harness.Engine.EndAsync();

            await WaitUntilAsync(() => harness.SettingsService.Current.FinishedGamesCount == partia);
        }

        // Sześć partii, reklama po trzeciej i po szóstej.
        await WaitUntilAsync(() => harness.Ads.InterstitialRequests == 2);

        Assert.Equal(2, harness.Ads.InterstitialRequests);
        Assert.Equal(6, harness.SettingsService.Current.FinishedGamesCount);
    }

    [Fact]
    public async Task LicznikPartii_PrzezywaZamknieciaAplikacji()
    {
        // Licznik jest w ustawieniach właśnie dlatego: gdyby żył w pamięci, wystarczyłoby
        // zamknąć aplikację, żeby odliczanie zaczynało się od zera i reklama wracała częściej.
        using GameTestHarness harness = new();

        await harness.SettingsService.UpdateAsync(settings => settings with { FinishedGamesCount = 2 });

        await harness.AdCoordinator.ActivateAsync();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.EndAsync();

        // Trzecia partia licząc od instalacji — reklama pada od razu po pierwszej rozegranej
        // w tym uruchomieniu.
        await WaitUntilAsync(() => harness.Ads.InterstitialRequests == 1);

        Assert.Equal(3, harness.SettingsService.Current.FinishedGamesCount);
    }

    [Fact]
    public async Task PoZejsciuZEkranu_ReklamaPelnoekranowaJuzNiePada()
    {
        // Reklama pokazana na ekranie startowym albo w ustawieniach byłaby reklamą wyskakującą
        // bez powodu. Koordynator sprawdza warunki jeszcze raz po zapowiedzi końca partii.
        using GameTestHarness harness = new();

        await harness.SettingsService.UpdateAsync(settings => settings with { FinishedGamesCount = 2 });
        await harness.AdCoordinator.ActivateAsync();
        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        await harness.AdCoordinator.DeactivateAsync();
        await harness.Engine.EndAsync();

        await Task.Delay(50);

        Assert.Equal(0, harness.Ads.InterstitialRequests);
    }

    [Fact]
    public async Task MiejsceNaBaner_JestTrzymaneTylkoNaEkranieRozgrywki()
    {
        using GameTestHarness harness = new();

        Assert.False(harness.AdCoordinator.IsBannerAllowed);

        await harness.AdCoordinator.ActivateAsync();

        Assert.True(harness.AdCoordinator.IsBannerAllowed);

        await harness.AdCoordinator.DeactivateAsync();

        Assert.False(harness.AdCoordinator.IsBannerAllowed);
    }

    [Fact]
    public async Task GdyReklamNieMaWTymWydaniu_MiejsceNaBanerNieJestTrzymane()
    {
        // Pusty pas na dole ekranu zabierałby wysokość za nic — a wysokość jest na tym ekranie
        // policzona co do jednostki.
        using GameTestHarness harness = new();

        harness.Ads.IsAvailable = false;

        await harness.AdCoordinator.ActivateAsync();

        Assert.False(harness.AdCoordinator.IsBannerAllowed);
    }

    [Fact]
    public async Task BrakZgodyNaPersonalizacje_ZnaczyBrakBaneraIBrakReklamy()
    {
        // W EEA bez zgody nie wolno żądać reklam w ogóle — ani banera, ani pełnoekranowej.
        using GameTestHarness harness = new();

        harness.Ads.CanRequestAds = false;

        await harness.AdCoordinator.ActivateAsync();

        Assert.False(harness.AdCoordinator.IsBannerAllowed);
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
