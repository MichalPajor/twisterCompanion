using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Syntezator mowy wbudowany w urządzenie.
/// </summary>
/// <remarks>
/// Cienki adapter na API mowy z MAUI. Cała logika odczytu — kiedy mówić, co przerwać,
/// co zrobić z awarią — jest w warstwie aplikacji i tam też jest przetestowana. Tutaj
/// zostaje wyłącznie tłumaczenie naszych parametrów na parametry platformy.
/// <para>
/// Działa <b>offline</b>, bez żadnego serwisu zewnętrznego, zgodnie z założeniami projektu.
/// </para>
/// </remarks>
internal sealed class MauiTextToSpeechService : ITextToSpeechService
{
    /// <summary>
    /// Raz odczytana lista głosów urządzenia.
    /// </summary>
    /// <remarks>
    /// Buforowana, bo odczyt <b>budzi silnik mowy</b> i przy zapisanym wyborze głosu leciał
    /// przed każdą wypowiedzią — czyli kilka razy w każdej turze. Lista głosów urządzenia nie
    /// zmienia się w trakcie działania aplikacji, więc jeden odczyt wystarcza.
    /// <para>
    /// Trzymane jako zadanie, a nie jako gotowa lista: dzięki temu dwa równoległe pytania
    /// o głosy czekają na ten sam odczyt, zamiast wywoływać dwa.
    /// </para>
    /// </remarks>
    private Task<IEnumerable<Locale>>? _locales;

    /// <inheritdoc />
    public async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        // Odczyt listy głosów jest najtańszą rzeczą, która wiąże usługę mowy i wczytuje
        // silnik — a to jest cały koszt, który chcemy ponieść przed pierwszym „Zaczynamy".
        await LoadLocalesAsync();

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpeechVoice>> GetVoicesAsync(
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Locale> locales = await LoadLocalesAsync();

        cancellationToken.ThrowIfCancellationRequested();

        return
        [
            .. locales.Select(locale => new SpeechVoice(
                BuildVoiceId(locale),
                BuildVoiceName(locale),
                locale.Language)),
        ];
    }

    /// <inheritdoc />
    public async Task SpeakAsync(
        string text,
        SpeechRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        SpeechOptions options = new()
        {
            Pitch = request.Pitch,
            Rate = request.Rate,
            Volume = 1.0f,
            Locale = await FindLocaleAsync(request.VoiceId),
        };

        await TextToSpeech.Default.SpeakAsync(text, options, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Przerwanie odbywa się przez anulowanie tokenu przekazanego do wypowiedzi —
    /// API mowy w MAUI nie ma osobnej metody zatrzymania.
    /// </remarks>
    public Task StopAsync() => Task.CompletedTask;

    /// <summary>Znajduje ustawienia regionalne odpowiadające wybranemu głosowi.</summary>
    private async Task<Locale?> FindLocaleAsync(string? voiceId)
    {
        if (string.IsNullOrWhiteSpace(voiceId))
        {
            return null;
        }

        IEnumerable<Locale> locales = await LoadLocalesAsync();

        return locales.FirstOrDefault(locale => BuildVoiceId(locale) == voiceId);
    }

    /// <summary>Odczytuje listę głosów urządzenia raz na uruchomienie aplikacji.</summary>
    private Task<IEnumerable<Locale>> LoadLocalesAsync() =>
        _locales ??= TextToSpeech.Default.GetLocalesAsync();

    /// <summary>
    /// Buduje stabilny identyfikator głosu.
    /// </summary>
    /// <remarks>
    /// <see cref="Locale"/> nie ma własnego identyfikatora, a wybór głosu jest zapisywany
    /// w ustawieniach — potrzebna jest wartość, która nie zmieni się między uruchomieniami.
    /// </remarks>
    private static string BuildVoiceId(Locale locale) =>
        $"{locale.Language}|{locale.Country}|{locale.Name}";

    private static string BuildVoiceName(Locale locale) =>
        string.IsNullOrWhiteSpace(locale.Name)
            ? $"{locale.Language}-{locale.Country}"
            : locale.Name;
}
