namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Nasłuch komend głosowych w oknie oczekiwania na reakcję graczy.
/// </summary>
/// <remarks>
/// Rozpoznawanie mowy na Androidzie działa sesjami i samo się nie wznawia, więc ta warstwa
/// zamienia ciąg sesji technicznych w jedno <b>okno nasłuchu</b>, które gracz widzi i słyszy
/// jako całość. Odpowiada też za sygnały dźwiękowe, pomijanie powtórzeń tej samej komendy
/// i odstąpienie od nasłuchu, gdy usługa rozpoznawania odmawia obsługi.
/// <para>
/// Nie zna silnika gry — komendę wystawia jako zdarzenie. Kto ją wykona, decyduje
/// <see cref="IVoiceControlCoordinator"/>.
/// </para>
/// </remarks>
public interface IVoiceControlService
{
    /// <summary>Aktualny stan nasłuchu.</summary>
    VoiceControlState State { get; }

    /// <summary>
    /// Czy mikrofon jest odcięty <b>przełącznikiem systemowym</b>, mimo przyznanej zgody.
    /// </summary>
    /// <remarks>
    /// Odczytane przy ostatnim przygotowaniu nasłuchu. Osobno od stanu, bo to nie jest stan
    /// nasłuchu, tylko powód, dla którego nasłuch nic nie usłyszy: uprawnienie jest
    /// przyznane, sesje startują, a system podaje ciszę. Ekran rozgrywki pyta o to, żeby
    /// powiedzieć graczowi, gdzie szukać, zamiast pozwolić mu czekać na reakcję.
    /// </remarks>
    bool IsMicrophoneBlockedBySystem { get; }

    /// <summary>Zgłaszane po rozpoznaniu komendy.</summary>
    event EventHandler<VoiceCommandType>? CommandRecognized;

    /// <summary>
    /// Zgłaszane, gdy kilka sesji nasłuchu z rzędu nie usłyszało <b>nic</b>.
    /// </summary>
    /// <remarks>
    /// Cisza sesja po sesji ma zwykle przyczynę poza aplikacją: globalny przełącznik
    /// mikrofonu w szybkich ustawieniach Androida, wyciszony zestaw słuchawkowy, zepsuty
    /// mikrofon. Aplikacja nie potrafi ich rozróżnić i nie musi — wystarczy, że przestanie
    /// milczeć razem z mikrofonem i podpowie, gdzie szukać.
    /// <para>
    /// Zgłaszane <b>raz</b> na aktywację, żeby podpowiedź nie wracała co kilka sekund.
    /// </para>
    /// </remarks>
    event EventHandler? SilenceDetected;

    /// <summary>Zgłaszane przy każdej zmianie stanu nasłuchu.</summary>
    event EventHandler<VoiceControlState>? StateChanged;

    /// <summary>Sprawdza zgodę na mikrofon i możliwości urządzenia.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><see langword="true"/>, gdy sterowanie głosem jest możliwe.</returns>
    Task<bool> PrepareAsync(CancellationToken cancellationToken = default);

    /// <summary>Otwiera okno nasłuchu i trzyma je otwarte do rozpoznania komendy.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task OpenWindowAsync(CancellationToken cancellationToken = default);

    /// <summary>Zamyka okno nasłuchu.</summary>
    Task CloseWindowAsync();
}
