using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Tests.Fakes;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy rytmu prośby o ocenę na prawdziwym silniku i koordynatorze reklam (Etap 18).
/// </summary>
/// <remarks>
/// Najważniejsza reguła: prośba o ocenę <b>nigdy</b> nie pada w tym samym przejściu co reklama
/// pełnoekranowa. Reklamy zostają bez zmian (decyzja użytkownika), więc przy kolizji ustępuje
/// prośba — i musi wrócić przy następnej partii, a nie przepaść.
/// </remarks>
public class ReviewCoordinatorTests
{
    [Fact]
    public async Task PoDwochWygranychPartiachWOdstepieDwochDni_ProsiOOcene()
    {
        using GameTestHarness harness = new();
        await ActivateAsync(harness);

        await PlayWonGameAsync(harness);
        await WaitUntilAsync(() => harness.SettingsService.Current.ReviewQualifyingGamesCount == 1);

        harness.TimeProvider.Advance(TimeSpan.FromDays(2));

        await PlayWonGameAsync(harness);
        await WaitUntilAsync(() => harness.Reviews.Requests == 1);

        AppSettings settings = harness.SettingsService.Current;
        Assert.Equal(0, settings.ReviewQualifyingGamesCount);
        Assert.Equal(harness.TimeProvider.GetUtcNow(), settings.LastReviewRequestAt);
    }

    [Fact]
    public async Task DwiePartieJednegoWieczoru_NieWystarczaja()
    {
        using GameTestHarness harness = new();
        await ActivateAsync(harness);

        await PlayWonGameAsync(harness);
        await PlayWonGameAsync(harness);
        await WaitUntilAsync(() => harness.SettingsService.Current.ReviewQualifyingGamesCount == 2);

        Assert.Equal(0, harness.Reviews.Requests);
    }

    [Fact]
    public async Task KrotkaPartiaPrzerwanaZakoncz_NieLiczySie()
    {
        using GameTestHarness harness = new();
        await ActivateAsync(harness);

        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.EndAsync();

        // Koordynator reklam liczy każdą partię — gdy jego licznik drgnie, decyzja o ocenie
        // też już zapadła.
        await WaitUntilAsync(() => harness.SettingsService.Current.FinishedGamesCount == 1);
        await Task.Delay(50);

        Assert.Equal(0, harness.SettingsService.Current.ReviewQualifyingGamesCount);
    }

    [Fact]
    public async Task GdyPoPartiiJestReklama_ProsbaCzekaNaNastepnaPartie()
    {
        using GameTestHarness harness = new();
        await DueAfterNextGameAsync(harness, finishedGames: 2);
        await ActivateAsync(harness);

        // Trzecia partia od instalacji — reklama pełnoekranowa. Dwa okna jedno po drugim
        // to przepis na złą ocenę, więc prośba ustępuje.
        await PlayWonGameAsync(harness);
        await WaitUntilAsync(() => harness.Ads.InterstitialRequests == 1);
        await WaitUntilAsync(() => harness.SettingsService.Current.ReviewQualifyingGamesCount == 2);

        Assert.Equal(0, harness.Reviews.Requests);

        // Czwarta partia — bez reklamy, więc prośba wraca.
        await PlayWonGameAsync(harness);
        await WaitUntilAsync(() => harness.Reviews.Requests == 1);

        Assert.Equal(1, harness.Ads.InterstitialRequests);
    }

    [Fact]
    public async Task GdyReklamaWypadlaAleSieNiePokazala_ProsbaPada()
    {
        // Brak reklamy do wyświetlenia (sieć, brak wypełnienia) to nie powód, żeby odkładać
        // prośbę — okna i tak nie było.
        using GameTestHarness harness = new();
        harness.Ads.ShowSucceeds = false;
        await DueAfterNextGameAsync(harness, finishedGames: 2);
        await ActivateAsync(harness);

        await PlayWonGameAsync(harness);
        await WaitUntilAsync(() => harness.Reviews.Requests == 1);

        Assert.Equal(1, harness.Ads.InterstitialRequests);
    }

    [Fact]
    public async Task PoZejsciuZEkranu_ProsbaNiePada()
    {
        using GameTestHarness harness = new();
        await DueAfterNextGameAsync(harness, finishedGames: 0);
        await ActivateAsync(harness);
        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));

        await harness.ReviewCoordinator.DeactivateAsync();
        await harness.AdCoordinator.DeactivateAsync();
        await harness.Engine.EliminateCurrentPlayerAsync();
        await Task.Delay(50);

        Assert.Equal(0, harness.Reviews.Requests);
    }

    [Fact]
    public async Task GdyOkienkaNieMaWTymWydaniu_PartieLiczaSieDalejBezProsby()
    {
        using GameTestHarness harness = new();
        harness.Reviews.IsAvailable = false;
        await DueAfterNextGameAsync(harness, finishedGames: 0);
        await ActivateAsync(harness);

        await PlayWonGameAsync(harness);
        await WaitUntilAsync(() => harness.SettingsService.Current.ReviewQualifyingGamesCount == 2);

        Assert.Equal(0, harness.Reviews.Requests);
        Assert.Null(harness.SettingsService.Current.LastReviewRequestAt);
    }

    [Fact]
    public async Task GdyWywolanieSieWywrocilo_ProsbaNieJestZapisanaIWracaPrzyNastepnejPartii()
    {
        // Zapis prośby po wywołaniu, nie przed — inaczej jedna awaria blokowałaby prośbę
        // na trzydzieści dni.
        using GameTestHarness harness = new();
        harness.Reviews.Throws = true;
        await DueAfterNextGameAsync(harness, finishedGames: 0);
        await ActivateAsync(harness);

        await PlayWonGameAsync(harness);
        await WaitUntilAsync(() => harness.SettingsService.Current.ReviewQualifyingGamesCount == 2);
        await Task.Delay(50);

        Assert.Null(harness.SettingsService.Current.LastReviewRequestAt);

        harness.Reviews.Throws = false;
        await PlayWonGameAsync(harness);
        await WaitUntilAsync(() => harness.Reviews.Requests == 1);
    }

    [Fact]
    public async Task ReklamyPoWprowadzeniuOcen_NadalCoTrzeciaPartie()
    {
        // Decyzja użytkownika: prośba o ocenę nie zabiera żadnej reklamy.
        using GameTestHarness harness = new();
        await ActivateAsync(harness);

        for (int partia = 1; partia <= 6; partia++)
        {
            harness.TimeProvider.Advance(TimeSpan.FromDays(3));
            await PlayWonGameAsync(harness);
            await WaitUntilAsync(() => harness.SettingsService.Current.FinishedGamesCount == partia);
        }

        await WaitUntilAsync(() => harness.Ads.InterstitialRequests == 2);

        Assert.Equal(2, harness.Ads.InterstitialRequests);
    }

    private static async Task ActivateAsync(GameTestHarness harness)
    {
        await harness.ReviewCoordinator.ActivateAsync();
        await harness.AdCoordinator.ActivateAsync();
    }

    /// <summary>Ustawia stan, w którym następna licząca się partia uruchamia prośbę.</summary>
    private static Task DueAfterNextGameAsync(GameTestHarness harness, int finishedGames) =>
        harness.SettingsService.UpdateAsync(settings => settings with
        {
            FinishedGamesCount = finishedGames,
            ReviewQualifyingGamesCount = 1,
            FirstQualifyingGameAt = harness.TimeProvider.GetUtcNow().AddDays(-3),
        });

    /// <summary>Rozgrywa partię dwóch graczy zakończoną odpadnięciem — ze zwycięzcą.</summary>
    private static async Task PlayWonGameAsync(GameTestHarness harness)
    {
        await harness.Engine.StartAsync(GameTestHarness.Configuration(2));
        await harness.Engine.EliminateCurrentPlayerAsync();
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
