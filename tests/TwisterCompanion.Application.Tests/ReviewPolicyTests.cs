using TwisterCompanion.Application.AppReview;
using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy reguł prośby o ocenę (Etap 18): która partia się liczy i kiedy wolno poprosić.
/// </summary>
/// <remarks>
/// Okienko oceny pojawia się tylko w aplikacji zainstalowanej ze sklepu, więc na urządzeniu
/// z Visual Studio tych reguł nie da się zobaczyć w działaniu. Ten zestaw jest jedynym
/// miejscem, w którym są sprawdzane przed wydaniem.
/// </remarks>
public class ReviewPolicyTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 4, 18, 0, 0, TimeSpan.FromHours(1));
    private static readonly ReviewOptions Options = ReviewOptions.Default;

    [Fact]
    public void Default_ZgodneZUstaleniamiZUzytkownikiem()
    {
        Assert.Equal(2, Options.QualifyingGamesBeforeRequest);
        Assert.Equal(10, Options.MinimumTurnsWithoutWinner);
        Assert.Equal(TimeSpan.FromDays(2), Options.MinimumTimeSinceFirstGame);
        Assert.Equal(TimeSpan.FromDays(30), Options.MinimumTimeBetweenRequests);
    }

    [Fact]
    public void CountsTowardsReview_PartiaZeZwyciezcaLiczySieNawetKrotka()
    {
        Assert.True(ReviewPolicy.CountsTowardsReview(Summary(turns: 3, withWinner: true), Options));
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(40, true)]
    public void CountsTowardsReview_PartiaBezZwyciezcyLiczySieOdDziesieciuTur(int turns, bool expected)
    {
        // Bez zwycięzcy kończą się partie przerwane „Zakończ" i wszystkie partie trybu dla
        // dzieci, gdzie nikt nie odpada. Próg tur nie może ich wykluczać w całości.
        Assert.Equal(expected, ReviewPolicy.CountsTowardsReview(Summary(turns, withWinner: false), Options));
    }

    [Fact]
    public void RecordQualifyingGame_ZapamietujeDatePierwszejPartiiIJejNieNadpisuje()
    {
        AppSettings settings = ReviewPolicy.RecordQualifyingGame(AppSettings.Default, Start);
        settings = ReviewPolicy.RecordQualifyingGame(settings, Start.AddDays(5));

        Assert.Equal(2, settings.ReviewQualifyingGamesCount);
        Assert.Equal(Start, settings.FirstQualifyingGameAt);
    }

    [Fact]
    public void IsRequestDue_PoJednejPartiiNie()
    {
        AppSettings settings = ReviewPolicy.RecordQualifyingGame(AppSettings.Default, Start);

        Assert.False(ReviewPolicy.IsRequestDue(settings, Start.AddDays(10), Options));
    }

    [Fact]
    public void IsRequestDue_DwiePartieTegoSamegoDniaToZaWczesnie()
    {
        // Dwie partie jednego wieczoru to jeszcze nie znak, że ktoś do gry wraca.
        AppSettings settings = Played(AppSettings.Default, Start, Start.AddHours(1));

        Assert.False(ReviewPolicy.IsRequestDue(settings, Start.AddHours(1), Options));
    }

    [Fact]
    public void IsRequestDue_DwiePartieIDwaDniOdPierwszej()
    {
        AppSettings settings = Played(AppSettings.Default, Start, Start.AddDays(2));

        Assert.True(ReviewPolicy.IsRequestDue(settings, Start.AddDays(2), Options));
    }

    [Fact]
    public void IsRequestDue_PoProsbieTrzebaZnowDwochPartiiITrzydziestuDni()
    {
        AppSettings settings = Played(AppSettings.Default, Start, Start.AddDays(2));
        DateTimeOffset request = Start.AddDays(2);
        settings = ReviewPolicy.RecordRequest(settings, request);

        // Same partie nie wystarczą — nie minęło 30 dni.
        settings = Played(settings, request.AddDays(1), request.AddDays(2));
        Assert.False(ReviewPolicy.IsRequestDue(settings, request.AddDays(2), Options));

        // Sam czas też nie: po powrocie po miesiącu pierwsza partia to za mało.
        AppSettings poMiesiacu = ReviewPolicy.RecordRequest(settings, request);
        poMiesiacu = ReviewPolicy.RecordQualifyingGame(poMiesiacu, request.AddDays(31));
        Assert.False(ReviewPolicy.IsRequestDue(poMiesiacu, request.AddDays(31), Options));

        poMiesiacu = ReviewPolicy.RecordQualifyingGame(poMiesiacu, request.AddDays(31));
        Assert.True(ReviewPolicy.IsRequestDue(poMiesiacu, request.AddDays(31), Options));
    }

    [Fact]
    public void RecordRequest_ZerujeLicznikIZapamietujeDate()
    {
        AppSettings settings = Played(AppSettings.Default, Start, Start.AddDays(2));

        settings = ReviewPolicy.RecordRequest(settings, Start.AddDays(2));

        Assert.Equal(0, settings.ReviewQualifyingGamesCount);
        Assert.Equal(Start.AddDays(2), settings.LastReviewRequestAt);
        Assert.Equal(Start, settings.FirstQualifyingGameAt);
    }

    [Fact]
    public void IsRequestDue_ProsbaOdlozonaZPowoduReklamyCzekaNaNastepnaPartie()
    {
        // Gdy prośba wypada w przejściu z reklamą, wygrywa reklama, a prośby nie zapisujemy.
        // Licznik rośnie dalej, więc następna licząca się partia wciąż ją uruchamia.
        AppSettings settings = Played(AppSettings.Default, Start, Start.AddDays(2));
        Assert.True(ReviewPolicy.IsRequestDue(settings, Start.AddDays(2), Options));

        settings = ReviewPolicy.RecordQualifyingGame(settings, Start.AddDays(2));

        Assert.True(ReviewPolicy.IsRequestDue(settings, Start.AddDays(2), Options));
    }

    [Fact]
    public void IsRequestDue_DataZPrzyszlosciNieBlokujeProsbyNaZawsze()
    {
        // Zegar telefonu przestawiony w przód przy zapisie, potem cofnięty.
        AppSettings settings = AppSettings.Default with
        {
            ReviewQualifyingGamesCount = 2,
            FirstQualifyingGameAt = Start.AddYears(1),
            LastReviewRequestAt = Start.AddYears(1),
        };

        Assert.True(ReviewPolicy.IsRequestDue(settings, Start, Options));
    }

    [Fact]
    public void AppSettings_UjemnyLicznikPartiiDoOcenyJestPrzycinanyDoZera()
    {
        AppSettings settings = AppSettings.Default with { ReviewQualifyingGamesCount = -3 };

        Assert.Equal(0, settings.ReviewQualifyingGamesCount);
    }

    private static AppSettings Played(AppSettings settings, params DateTimeOffset[] games)
    {
        foreach (DateTimeOffset game in games)
        {
            settings = ReviewPolicy.RecordQualifyingGame(settings, game);
        }

        return settings;
    }

    private static GameSummary Summary(int turns, bool withWinner)
    {
        Player winner = Player.Create("Kuba", 0);

        return new GameSummary(
            PlayerCount: 2,
            TurnCount: turns,
            EventCount: 0,
            Duration: TimeSpan.FromMinutes(turns),
            EliminationOrder: [Player.Create("Anna", 1)],
            Winner: withWinner ? winner : null);
    }
}
