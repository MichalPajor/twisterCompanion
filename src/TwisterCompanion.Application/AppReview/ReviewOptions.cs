namespace TwisterCompanion.Application.AppReview;

/// <summary>
/// Parametry prośby o ocenę.
/// </summary>
/// <remarks>
/// Wartości ustalone z użytkownikiem (Etap 18), nie wymuszone przez Google Play — sklep ma
/// własny, nieujawniany limit i i tak nie pokaże okienka częściej, niż uzna za stosowne.
/// Nasze progi chronią graczy przed prośbą po pierwszej, przypadkowej partii.
/// </remarks>
public sealed record ReviewOptions
{
    /// <summary>Wartości domyślne.</summary>
    public static ReviewOptions Default { get; } = new();

    /// <summary>
    /// Ile liczących się partii musi minąć przed prośbą — pierwszą i każdą kolejną.
    /// </summary>
    public int QualifyingGamesBeforeRequest { get; init; } = 2;

    /// <summary>
    /// Od ilu tur partia bez zwycięzcy liczy się do prośby o ocenę.
    /// </summary>
    /// <remarks>
    /// Partia ze zwycięzcą liczy się zawsze. Bez zwycięzcy kończą się partie przerwane
    /// przyciskiem „Zakończ" — i <b>wszystkie</b> partie w trybie dla dzieci, gdzie nikt nie
    /// odpada. Próg tur odróżnia prawdziwą rozgrywkę od próby zakończonej po minucie.
    /// </remarks>
    public int MinimumTurnsWithoutWinner { get; init; } = 10;

    /// <summary>
    /// Ile czasu musi minąć od pierwszej liczącej się partii do pierwszej prośby.
    /// </summary>
    /// <remarks>
    /// Liczone od pierwszej partii, a nie od instalacji: aplikacja nie zapisywała daty
    /// instalacji, a dla osób, które zaktualizowały ją z wersji 1.0.3, data „pierwszego
    /// uruchomienia" byłaby dniem aktualizacji. Pierwsza partia mierzy to samo — że ktoś
    /// wrócił do gry innego dnia — i nie zależy od tego, skąd przyszedł.
    /// </remarks>
    public TimeSpan MinimumTimeSinceFirstGame { get; init; } = TimeSpan.FromDays(2);

    /// <summary>Najkrótszy odstęp między kolejnymi prośbami.</summary>
    public TimeSpan MinimumTimeBetweenRequests { get; init; } = TimeSpan.FromDays(30);
}
