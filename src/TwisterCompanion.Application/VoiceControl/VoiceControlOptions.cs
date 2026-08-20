namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Parametry nasłuchu komend głosowych.
/// </summary>
/// <remarks>
/// Wartości domyślne nie są zgadnięte — pochodzą z pomiarów na fizycznym urządzeniu
/// (Android 12, rozpoznawanie systemowe) wykonanych w zadaniu 0 tego etapu:
/// sesja rozpoznawania trwała od 2,1 do 11,9 s (średnio 5,0 s), a 12 sesji na minutę
/// nie wywołało żadnej odmowy usługi.
/// </remarks>
public sealed record VoiceControlOptions
{
    /// <summary>Wartości domyślne.</summary>
    public static VoiceControlOptions Default { get; } = new();

    /// <summary>
    /// Przerwa między zamknięciem jednej sesji nasłuchu a otwarciem następnej.
    /// </summary>
    /// <remarks>
    /// Dwie sekundy, a nie kilkaset milisekund: przy krótkiej przerwie sygnał zamknięcia
    /// nasłuchu bywał niesłyszalny, bo nakładał się na start kolejnej sesji. Przerwa daje
    /// też graczom moment na zorientowanie się, że trzeba poczekać.
    /// </remarks>
    public TimeSpan SessionRestartDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Odstęp między sygnałem dźwiękowym a otwarciem mikrofonu — i między jego zamknięciem
    /// a sygnałem końca.
    /// </summary>
    /// <remarks>
    /// Bez odstępu rozpoznawanie łapie własne piknięcie, a sygnał końca ginie w chwili,
    /// gdy urządzenie zwalnia mikrofon.
    /// </remarks>
    public TimeSpan CueGap { get; init; } = TimeSpan.FromMilliseconds(300);

    /// <summary>Jak długo ta sama komenda jest ignorowana po wykonaniu.</summary>
    /// <remarks>
    /// Ta sama fraza przychodzi zwykle dwa razy: jako wynik częściowy i jako finalny.
    /// Bez tego okna „Dalej" rozegrałoby dwie tury i pominęło gracza.
    /// </remarks>
    public TimeSpan DebounceWindow { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Sugerowany dla rozpoznawacza czas ciszy kończący sesję.</summary>
    /// <remarks>
    /// Sugerowany, bo dokumentacja Androida mówi wprost, że implementacja może go pominąć —
    /// i pomiary to potwierdziły. Zostaje, bo na urządzeniach, które go respektują, skraca
    /// sesję z pięciu sekund do dwóch.
    /// </remarks>
    public TimeSpan SilenceTimeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Krótkie odczekanie przed otwarciem okna nasłuchu.
    /// </summary>
    /// <remarks>
    /// Silnik gry zgłasza zmianę stanu <b>przed</b> odczytaniem zapowiedzi („Pauza",
    /// „Wznawiamy"), więc bez tego odczekania nasłuch otwierałby się na moment tylko po to,
    /// żeby zamknąć się przy pierwszym słowie aplikacji — z parą sygnałów dźwiękowych bez
    /// żadnego sensu dla graczy.
    /// </remarks>
    public TimeSpan WindowSettleDelay { get; init; } = TimeSpan.FromMilliseconds(400);

    /// <summary>Podstawa rosnącego opóźnienia po odmowie usługi rozpoznawania.</summary>
    public TimeSpan ThrottleBackoff { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Po ilu odmowach z rzędu przestać próbować w tej turze.</summary>
    public int MaxThrottleStrikes { get; init; } = 3;
}
