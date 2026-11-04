using TwisterCompanion.Application.Game;
using TwisterCompanion.Application.Settings;

namespace TwisterCompanion.Application.AppReview;

/// <summary>
/// Reguły prośby o ocenę: która partia się liczy i kiedy wolno poprosić.
/// </summary>
/// <remarks>
/// Czyste funkcje na <see cref="AppSettings"/> i bieżącym czasie — bez zegara, bez dysku,
/// bez platformy. Dzięki temu każdą regułę da się sprawdzić testem w milisekundę, a nie
/// czekaniem dwóch dni albo instalacją ze sklepu.
/// <para>
/// Stan trzymają ustawienia, bo musi przeżyć restart aplikacji — z tego samego powodu co
/// licznik partii dla reklam.
/// </para>
/// </remarks>
public static class ReviewPolicy
{
    /// <summary>Czy zakończona partia liczy się do prośby o ocenę.</summary>
    /// <param name="summary">Podsumowanie partii.</param>
    /// <param name="options">Parametry prośby.</param>
    public static bool CountsTowardsReview(GameSummary summary, ReviewOptions options)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(options);

        return summary.Winner is not null || summary.TurnCount >= options.MinimumTurnsWithoutWinner;
    }

    /// <summary>Zapisuje w stanie kolejną liczącą się partię.</summary>
    /// <param name="settings">Aktualne ustawienia.</param>
    /// <param name="now">Bieżący czas.</param>
    /// <returns>Ustawienia ze zwiększonym licznikiem i — przy pierwszej partii — jej datą.</returns>
    public static AppSettings RecordQualifyingGame(AppSettings settings, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings with
        {
            ReviewQualifyingGamesCount = settings.ReviewQualifyingGamesCount + 1,
            FirstQualifyingGameAt = settings.FirstQualifyingGameAt ?? now,
        };
    }

    /// <summary>Czy w tej chwili wolno poprosić o ocenę.</summary>
    /// <param name="settings">Aktualne ustawienia — po zapisaniu bieżącej partii.</param>
    /// <param name="now">Bieżący czas.</param>
    /// <param name="options">Parametry prośby.</param>
    /// <remarks>
    /// Data z przyszłości (zegar telefonu cofnięty po zapisie) traktowana jest jak data,
    /// od której czas już minął. Inaczej jedno przestawienie zegara blokowałoby prośbę
    /// na zawsze; w drugą stronę ryzyko jest małe, bo licznik partii i tak musi dobić progu,
    /// a sklep ma własny limit.
    /// </remarks>
    public static bool IsRequestDue(AppSettings settings, DateTimeOffset now, ReviewOptions options)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(options);

        if (settings.ReviewQualifyingGamesCount < options.QualifyingGamesBeforeRequest
            || settings.FirstQualifyingGameAt is not { } firstGame)
        {
            return false;
        }

        if (!HasElapsed(firstGame, now, options.MinimumTimeSinceFirstGame))
        {
            return false;
        }

        return settings.LastReviewRequestAt is not { } lastRequest
            || HasElapsed(lastRequest, now, options.MinimumTimeBetweenRequests);
    }

    /// <summary>Zapisuje w stanie wysłaną prośbę o ocenę.</summary>
    /// <param name="settings">Aktualne ustawienia.</param>
    /// <param name="now">Bieżący czas.</param>
    /// <returns>Ustawienia z wyzerowanym licznikiem partii i datą prośby.</returns>
    /// <remarks>
    /// Licznik się zeruje, bo kolejna prośba też wymaga nowych partii — nie tylko upływu
    /// czasu. Ktoś, kto nie grał przez miesiąc, nie powinien dostać prośby zaraz po powrocie,
    /// przy pierwszej partii.
    /// </remarks>
    public static AppSettings RecordRequest(AppSettings settings, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings with
        {
            ReviewQualifyingGamesCount = 0,
            LastReviewRequestAt = now,
        };
    }

    private static bool HasElapsed(DateTimeOffset since, DateTimeOffset now, TimeSpan period) =>
        since > now || now - since >= period;
}
