using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Game;

/// <summary>
/// Prowadzi rozgrywkę: rozdaje tury, pilnuje stanu i zgłasza, co się dzieje.
/// </summary>
/// <remarks>
/// Silnik jest jedynym miejscem, które zmienia stan partii. Warstwa prezentacji wywołuje
/// jego operacje i słucha zdarzeń — nie sięga do <see cref="GameSession"/> po to, żeby
/// cokolwiek w niej zmienić.
/// <para>
/// Operacje są <b>szeregowane</b>: równoległe wywołanie „Dalej" z przycisku i z komendy
/// głosowej (Etap 8) nie rozegra dwóch tur.
/// </para>
/// </remarks>
public interface IGameEngine
{
    /// <summary>Aktualny stan rozgrywki.</summary>
    GameState State { get; }

    /// <summary>Trwająca partia albo <see langword="null"/>, gdy żadna nie została rozpoczęta.</summary>
    GameSession? Session { get; }

    /// <summary>Ostatni komunikat przekazany graczom.</summary>
    Announcement? LastAnnouncement { get; }

    /// <summary>
    /// Trwające odliczanie albo <see langword="null"/>, gdy nic nie jest odmierzane.
    /// </summary>
    /// <remarks>
    /// Odmierzany jest czas na wykonanie zadania z wydarzenia (w obu trybach) oraz czas
    /// na wykonanie ruchu (tylko w trybie automatycznym).
    /// </remarks>
    TurnCountdown? Countdown { get; }

    /// <summary>Zgłaszane przy rozpoczęciu i zakończeniu odliczania.</summary>
    event EventHandler<TurnCountdown?>? CountdownChanged;

    /// <summary>
    /// Czy w trwającej partii da się zgłosić odpadnięcie gracza.
    /// </summary>
    /// <remarks>
    /// Wynika z trybu gry. Ekran rozgrywki ukrywa na tej podstawie przycisk — pokazywanie
    /// przycisku, który nic nie robi, byłoby gorsze od jego braku.
    /// </remarks>
    bool IsEliminationEnabled { get; }

    /// <summary>Zgłaszane po każdej zmianie stanu rozgrywki.</summary>
    event EventHandler<GameState>? StateChanged;

    /// <summary>Zgłaszane po rozegraniu tury.</summary>
    event EventHandler<Turn>? TurnPlayed;

    /// <summary>
    /// Zgłaszane dla każdego komunikatu do graczy.
    /// </summary>
    /// <remarks>
    /// Etap 7 podłączy się tutaj z odczytem głosowym. Dziś komunikaty pojawiają się
    /// wyłącznie na ekranie.
    /// </remarks>
    event EventHandler<Announcement>? AnnouncementRaised;

    /// <summary>Zgłaszane po zakończeniu partii.</summary>
    event EventHandler<GameSummary>? GameFinished;

    /// <summary>Rozpoczyna nową partię i rozgrywa pierwszą turę.</summary>
    /// <param name="configuration">Parametry partii.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task StartAsync(GameConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>Rozgrywa następną turę.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Wywołanie w stanie, który tego nie pozwala — na przykład w trakcie ogłaszania ruchu
    /// albo na pauzie — jest <b>ignorowane</b>, a nie kończy się błędem. Komenda głosowa
    /// może przyjść w dowolnym momencie i nie powinna wywalać rozgrywki.
    /// </remarks>
    Task NextTurnAsync(CancellationToken cancellationToken = default);

    /// <summary>Powtarza komunikat ostatniej tury.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task RepeatAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Zmienia sposób prowadzenia tury na <b>trwającej</b> partii.
    /// </summary>
    /// <param name="turnAdvanceMode">Nowy tryb zmiany tury.</param>
    /// <param name="moveTime">Czas, jaki ma odmierzać odliczanie ruchu w nowym trybie.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Tryb był dotąd zamrażany w chwili startu partii, bo zmiana ustawień w trakcie gry nie
    /// miała jak do silnika dotrzeć. Przycisk na ekranie rozgrywki to zmienia.
    /// <para>
    /// <b>Czas na ruch jest osobnym parametrem, a nie wyliczeniem z trybu</b>, bo znaczy co
    /// innego w każdym z nich: przy sterowaniu głosem odmierza chwilę otwarcia mikrofonu,
    /// w pozostałych czas na wykonanie ruchu, przeskalowany mnożnikiem trybu gry. Silnik nie
    /// zna ani ustawień, ani trybów gry — wartość podaje wywołujący.
    /// </para>
    /// <para>
    /// Jeśli w chwili wywołania biegnie odliczanie ruchu, <b>startuje od nowa</b> pod nową
    /// wartością. Gracz sięga po ten przełącznik, gdy bieżący sposób nie działa, więc zmiana
    /// od następnej tury wyglądałaby jak przycisk, który nic nie robi.
    /// </para>
    /// </remarks>
    Task ChangeTurnControlAsync(
        TurnAdvanceMode turnAdvanceMode,
        TimeSpan moveTime,
        CancellationToken cancellationToken = default);

    /// <summary>Wstrzymuje rozgrywkę, przerywając trwający odczyt i odliczanie.</summary>
    /// <param name="announce">Czy ogłosić wstrzymanie na głos.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Zapowiedź jest wyborem wołającego, bo wstrzymanie ma dwa różne powody. Pauza
    /// z przycisku albo z komendy głosowej dotyczy graczy leżących na macie — ci patrzą
    /// wtedy w sufit, nie w ekran, więc muszą usłyszeć, że gra stoi. Wstrzymanie przy zejściu
    /// z ekranu rozgrywki jest czynnością porządkową: ekranu już nie ma, a gracz świadomie
    /// poszedł gdzie indziej, więc mówiąca do niego aplikacja byłaby tylko hałasem.
    /// </remarks>
    Task PauseAsync(bool announce = true, CancellationToken cancellationToken = default);

    /// <summary>Wznawia wstrzymaną rozgrywkę.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task ResumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Oznacza aktualnego gracza jako odpadniętego i przechodzi dalej.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Jeśli po eliminacji zostaje najwyżej jeden gracz, partia kończy się samoczynnie.
    /// </remarks>
    Task EliminateCurrentPlayerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Oznacza wskazanego gracza jako odpadniętego.
    /// </summary>
    /// <param name="playerId">Gracz, który odpadł.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Odpada <b>ten</b> gracz, a nie ten, którego jest tura: upadek zdarza się także wtedy,
    /// gdy ruch wykonuje ktoś inny — ktoś traci równowagę, gdy sąsiad przeciska się nad jego
    /// ręką. Zgłoszenie idzie z przycisku obok imienia.
    /// </remarks>
    Task EliminatePlayerAsync(Guid playerId, CancellationToken cancellationToken = default);

    /// <summary>Kończy partię przed czasem.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task EndAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Zapomina zakończoną partię i wraca do stanu sprzed rozpoczęcia gry.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Silnik żyje tyle, co aplikacja, więc zakończona partia zostawała w jego pamięci
    /// i po powrocie na ekran rozgrywki gracz zamiast zasad nowej gry widział podsumowanie
    /// poprzedniej. Zapis na dysku był akurat w porządku — zakończonej partii nigdy się nie
    /// zapisuje — ale pamięć trzeba wyczyścić osobno.
    /// <para>
    /// Nie dotyczy partii wstrzymanej: tę wolno wznowić i właśnie po to jest wstrzymywana
    /// przy zejściu z ekranu.
    /// </para>
    /// </remarks>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>Zapisuje stan partii, żeby dało się ją wznowić.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task SaveSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Próbuje wznowić partię z zapisu.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><see langword="true"/>, jeśli partia została wznowiona.</returns>
    Task<bool> TryRestoreAsync(CancellationToken cancellationToken = default);
}
