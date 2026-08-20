namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Syntezator mowy urządzenia.
/// </summary>
/// <remarks>
/// Port do platformy. Implementacja żyje w projekcie hosta, bo API mowy jest częścią MAUI,
/// a warstwy niższe pozostają platformowo neutralne.
/// <para>
/// Interfejs jest celowo „głupi": mówi podany tekst z podanymi parametrami i nic więcej.
/// Kolejkowanie, przerywanie i decyzja, czy w ogóle mówić, należą do
/// <see cref="Voice.IAnnouncementSpeaker"/>. Dzięki temu cała logika odczytu jest
/// testowalna bez urządzenia.
/// </para>
/// <para>
/// Tu podłączy się też ewentualny inny silnik mowy w przyszłości — wymaga to jednej nowej
/// klasy i zmiany rejestracji, bez dotykania silnika gry.
/// </para>
/// </remarks>
public interface ITextToSpeechService
{
    /// <summary>
    /// Przygotowuje syntezator do mówienia, zanim będzie potrzebny.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Silnik mowy Androida budzi się przy pierwszym użyciu i to widać: pierwsza wypowiedź
    /// w uruchomieniu aplikacji potrafi spóźnić się o kilka sekund, bo zanim padnie, system
    /// wiąże usługę i wczytuje głos. Wywołanie tej metody przy starcie aplikacji przenosi ten
    /// koszt na moment, w którym nikt nie czeka — a nie na pierwsze „Zaczynamy".
    /// <para>
    /// Nigdy nie zgłasza wyjątku i nigdy nie mówi: brak silnika mowy nie może przeszkodzić
    /// w uruchomieniu aplikacji.
    /// </para>
    /// </remarks>
    Task PrepareAsync(CancellationToken cancellationToken = default);

    /// <summary>Zwraca głosy dostępne w systemie.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task<IReadOnlyList<SpeechVoice>> GetVoicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Wypowiada tekst i kończy się, gdy wypowiedź się zakończy.</summary>
    /// <param name="text">Tekst do wypowiedzenia.</param>
    /// <param name="request">Parametry wypowiedzi.</param>
    /// <param name="cancellationToken">
    /// Anulowanie przerywa wypowiedź w trakcie — używane przez komendę „Powtórz".
    /// </param>
    Task SpeakAsync(string text, SpeechRequest request, CancellationToken cancellationToken = default);

    /// <summary>Przerywa trwającą wypowiedź.</summary>
    Task StopAsync();
}

/// <summary>
/// Parametry pojedynczej wypowiedzi.
/// </summary>
/// <param name="VoiceId">Identyfikator głosu; <see langword="null"/> oznacza głos domyślny.</param>
/// <param name="Rate">Tempo mowy, gdzie 1,0 to tempo domyślne.</param>
/// <param name="Pitch">Wysokość głosu, gdzie 1,0 to wysokość domyślna.</param>
public sealed record SpeechRequest(string? VoiceId, float Rate, float Pitch);

/// <summary>
/// Głos dostępny w systemie.
/// </summary>
/// <param name="Id">Identyfikator używany przy wyborze głosu.</param>
/// <param name="Name">Nazwa do pokazania użytkownikowi.</param>
/// <param name="Language">Kod języka głosu, na przykład <c>pl-PL</c>.</param>
public sealed record SpeechVoice(string Id, string Name, string Language);
