namespace TwisterCompanion.Application.Voice;

/// <summary>
/// Odczytuje komunikaty na głos, pilnując, żeby wypowiedzi się nie nakładały.
/// </summary>
/// <remarks>
/// Warstwa pośrednia między silnikiem gry a syntezatorem mowy. Odpowiada za trzy rzeczy,
/// których nie robi sam syntezator:
/// <list type="bullet">
/// <item>sprawdza, czy odczyt głosowy jest w ustawieniach włączony;</item>
/// <item>przerywa trwającą wypowiedź, gdy przychodzi nowa — „Powtórz" ma zadziałać od razu,
/// a nie po dokończeniu poprzedniego zdania;</item>
/// <item>pochłania awarie syntezatora, żeby brak mowy nie zatrzymał rozgrywki.</item>
/// </list>
/// <para>
/// <see cref="IsSpeaking"/> i <see cref="SpeakingChanged"/> będą podstawą wzajemnego
/// wykluczenia z rozpoznawaniem mowy w Etapie 8: mikrofon musi milczeć, kiedy aplikacja
/// mówi, inaczej usłyszy własny głos i „rozpozna" w nim komendę.
/// </para>
/// </remarks>
public interface IAnnouncementSpeaker
{
    /// <summary>Czy trwa wypowiedź.</summary>
    bool IsSpeaking { get; }

    /// <summary>Zgłaszane przy rozpoczęciu i zakończeniu wypowiedzi.</summary>
    event EventHandler<bool>? SpeakingChanged;

    /// <summary>
    /// Wypowiada komunikat, przerywając wypowiedź trwającą.
    /// </summary>
    /// <param name="announcement">Komunikat do odczytania.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Metoda kończy się po zakończeniu wypowiedzi. Gdy odczyt jest wyłączony w ustawieniach
    /// albo syntezator zawiedzie, wraca natychmiast — <b>nigdy nie rzuca wyjątku</b>.
    /// </remarks>
    Task SpeakAsync(Announcement announcement, CancellationToken cancellationToken = default);

    /// <summary>Przerywa trwającą wypowiedź.</summary>
    Task SilenceAsync();

    /// <summary>
    /// Przygotowuje syntezator, żeby pierwsza wypowiedź nie czekała na obudzenie silnika.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Wywoływane przy starcie aplikacji. Nie mówi i nie zgłasza wyjątków.
    /// </remarks>
    Task PrepareAsync(CancellationToken cancellationToken = default);
}
