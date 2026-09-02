using System.Globalization;

namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Rozpoznawanie mowy urządzenia.
/// </summary>
/// <remarks>
/// Port do platformy. Implementacja żyje w projekcie hosta, bo rozpoznawanie mowy jest
/// częścią MAUI Community Toolkit, a warstwy niższe pozostają platformowo neutralne.
/// <para>
/// Interfejs obsługuje <b>jedną sesję</b> rozpoznawania i nic więcej. Android kończy sesję,
/// gdy uzna, że mówiący skończył, i <b>nie wznawia jej sam</b> — cykliczne odtwarzanie
/// nasłuchu, opóźnienia po błędach i wyciszanie mikrofonu na czas mowy aplikacji należą do
/// warstwy aplikacji, gdzie da się je przetestować bez urządzenia.
/// </para>
/// <para>
/// Tu podłączy się też inny silnik rozpoznawania w przyszłości — wymaga to jednej nowej
/// klasy i zmiany rejestracji, bez dotykania silnika gry.
/// </para>
/// </remarks>
public interface ISpeechRecognitionService
{
    /// <summary>Czy sesja rozpoznawania trwa.</summary>
    bool IsListening { get; }

    /// <summary>Zgłaszane przy każdym częściowym rozpoznaniu, jeszcze w trakcie sesji.</summary>
    /// <remarks>
    /// Wyniki częściowe są dla nas ważniejsze od finalnych: komendy mają jedno lub dwa słowa,
    /// więc pełną frazę znamy na długo przed zamknięciem sesji przez rozpoznawacz.
    /// </remarks>
    event EventHandler<string>? PartialRecognized;

    /// <summary>Zgłaszane przy zamknięciu sesji — z wynikiem albo z błędem.</summary>
    event EventHandler<SpeechRecognitionOutcome>? SessionCompleted;

    /// <summary>Sprawdza, co urządzenie potrafi.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task<SpeechRecognitionCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Prosi o zgodę na dostęp do mikrofonu.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><see langword="true"/>, gdy zgoda jest przyznana.</returns>
    Task<bool> RequestPermissionAsync(CancellationToken cancellationToken = default);

    /// <summary>Rozpoczyna jedną sesję rozpoznawania.</summary>
    /// <param name="request">Parametry sesji.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Metoda wraca po uruchomieniu nasłuchu, a nie po jego zakończeniu. Wynik przychodzi
    /// przez <see cref="SessionCompleted"/>.
    /// </remarks>
    Task StartAsync(SpeechRecognitionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Zamyka trwającą sesję.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Który silnik rozpoznawania ma obsłużyć sesję.
/// </summary>
public enum SpeechRecognitionMode
{
    /// <summary>
    /// Rozpoznawanie systemowe. Na Androidzie zwykle Google, zwykle przez sieć.
    /// </summary>
    /// <remarks>
    /// Nie wymaga od nas żadnej integracji, konta ani klucza — korzysta z usługi
    /// rozpoznawania zainstalowanej w systemie. Głos opuszcza jednak urządzenie, więc
    /// tryb musi być wybrany świadomie.
    /// </remarks>
    System,

    /// <summary>
    /// Rozpoznawanie na urządzeniu, bez sieci.
    /// </summary>
    /// <remarks>
    /// Na Androidzie wymaga wersji 13 lub nowszej oraz pobranego pakietu językowego.
    /// Bez pakietu sesja kończy się błędem <see cref="SpeechRecognitionError.LanguageUnavailable"/>.
    /// </remarks>
    OnDevice,
}

/// <summary>
/// Parametry jednej sesji rozpoznawania.
/// </summary>
/// <param name="Culture">Język, w którym mówi gracz.</param>
/// <param name="Mode">Silnik rozpoznawania.</param>
/// <param name="ReportPartialResults">Czy zgłaszać rozpoznania w trakcie mówienia.</param>
/// <param name="AutoStopSilenceTimeout">
/// Po jakim czasie ciszy zamknąć sesję. <see langword="null"/> zostawia decyzję
/// rozpoznawaczowi.
/// </param>
/// <remarks>
/// Wartość <paramref name="AutoStopSilenceTimeout"/> jest dla systemu <b>sugestią</b>:
/// dokumentacja Androida dla odpowiadających jej parametrów mówi wprost, że zależnie od
/// implementacji rozpoznawacza mogą nie mieć żadnego efektu. Dlatego długość sesji nigdy
/// nie jest zakładana, tylko mierzona.
/// </remarks>
public sealed record SpeechRecognitionRequest(
    CultureInfo Culture,
    SpeechRecognitionMode Mode = SpeechRecognitionMode.System,
    bool ReportPartialResults = true,
    TimeSpan? AutoStopSilenceTimeout = null);

/// <summary>
/// Co urządzenie potrafi w zakresie rozpoznawania mowy.
/// </summary>
/// <param name="IsSystemRecognitionAvailable">Czy działa rozpoznawanie systemowe.</param>
/// <param name="IsOnDeviceRecognitionAvailable">Czy działa rozpoznawanie na urządzeniu.</param>
/// <param name="PlatformDescription">
/// Opis platformy do wpisania w log i na ekran diagnostyczny — wersja systemu i nazwa
/// urządzenia.
/// </param>
/// <param name="IsMicrophoneBlockedBySystem">
/// Czy mikrofon jest odcięty <b>przełącznikiem systemowym</b>, mimo przyznanej zgody.
/// </param>
/// <remarks>
/// Ostatnia wartość opisuje coś innego niż uprawnienie aplikacji. Android od wersji 12 ma
/// globalny przełącznik prywatności — ten w szybkich ustawieniach, obok latarki. Gdy jest
/// wyłączony, aplikacja <b>nadal ma zgodę</b> na mikrofon, a system podaje jej ciszę.
/// Sprawdzenie samego uprawnienia przechodzi wtedy pomyślnie i nic nie zapowiada, że
/// rozpoznawanie nie usłyszy ani słowa.
/// </remarks>
public sealed record SpeechRecognitionCapabilities(
    bool IsSystemRecognitionAvailable,
    bool IsOnDeviceRecognitionAvailable,
    string PlatformDescription,
    bool IsMicrophoneBlockedBySystem = false);

/// <summary>
/// Zakończenie sesji rozpoznawania.
/// </summary>
/// <param name="Text">Rozpoznany tekst albo <see langword="null"/> przy błędzie.</param>
/// <param name="Error">Rodzaj błędu; <see cref="SpeechRecognitionError.None"/> przy sukcesie.</param>
/// <param name="Details">Oryginalny komunikat platformy — do logu i diagnostyki.</param>
public sealed record SpeechRecognitionOutcome(
    string? Text,
    SpeechRecognitionError Error = SpeechRecognitionError.None,
    string? Details = null)
{
    /// <summary>Czy sesja zakończyła się rozpoznaniem.</summary>
    public bool IsSuccessful => Error == SpeechRecognitionError.None;
}

/// <summary>
/// Rodzaj błędu sesji, przełożony na pojęcia niezależne od platformy.
/// </summary>
/// <remarks>
/// Podział nie jest ozdobny — decyduje o zachowaniu nasłuchu ciągłego. Błędy „spodziewane"
/// (<see cref="NoMatch"/>, <see cref="SpeechTimeout"/>) w grze zdarzają się cały czas, bo
/// nikt nie mówi bez przerwy, i muszą prowadzić do natychmiastowego wznowienia nasłuchu.
/// Błędy przeciążenia (<see cref="TooManyRequests"/>, <see cref="RecognizerBusy"/>) wymagają
/// odczekania, bo są skutkiem zbyt częstych restartów.
/// </remarks>
public enum SpeechRecognitionError
{
    /// <summary>Brak błędu.</summary>
    None,

    /// <summary>Nic nie rozpoznano — normalne, gdy w pokoju panuje cisza.</summary>
    NoMatch,

    /// <summary>Nikt nie zaczął mówić w wyznaczonym czasie.</summary>
    SpeechTimeout,

    /// <summary>Rozpoznawanie wymaga sieci, a jej nie ma.</summary>
    Network,

    /// <summary>Rozpoznawacz jest zajęty poprzednią sesją.</summary>
    RecognizerBusy,

    /// <summary>Zbyt wiele żądań w krótkim czasie — limit usługi rozpoznawania.</summary>
    TooManyRequests,

    /// <summary>Brak modelu dla wybranego języka; przy trybie na urządzeniu — brak pakietu.</summary>
    LanguageUnavailable,

    /// <summary>Brak zgody na mikrofon.</summary>
    InsufficientPermissions,

    /// <summary>Pozostałe awarie.</summary>
    Other,
}
