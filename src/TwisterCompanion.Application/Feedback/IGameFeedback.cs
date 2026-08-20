namespace TwisterCompanion.Application.Feedback;

/// <summary>
/// Jedno miejsce, które decyduje, czy aplikacja ma teraz zabrzmieć i zawibrować.
/// </summary>
/// <remarks>
/// Warstwa pośrednia między tym, co się stało w grze, a portami dźwięku i wibracji —
/// dokładnie tak, jak <c>IAnnouncementSpeaker</c> stoi między silnikiem a syntezatorem mowy.
/// Odpowiada za cztery rzeczy, których nie robi żaden z portów:
/// <list type="bullet">
/// <item>sprawdza w ustawieniach, czy dźwięki i wibracje są włączone;</item>
/// <item>podaje głośność z ustawień, a przy zerowej nie odtwarza wcale;</item>
/// <item><b>milczy w trakcie mowy</b> — efekt nachodzący na polecenie zabiera z niego słowa,
/// a polecenie jest w tej grze ważniejsze od ozdoby;</item>
/// <item>pochłania awarie, bo brak dźwięku nie może przerwać partii.</item>
/// </list>
/// <para>
/// Wywołuje go warstwa prezentacji, a nie silnik gry: silnik nie wie nic o dźwiękach i nie ma
/// powodu wiedzieć, a ekran rozgrywki i tak już nasłuchuje wszystkich zdarzeń partii.
/// </para>
/// </remarks>
public interface IGameFeedback
{
    /// <summary>Wczytuje próbki dźwiękowe do pamięci.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task PreloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Zgłasza zdarzenie z gry, na które aplikacja może odpowiedzieć dźwiękiem i wibracją.
    /// </summary>
    /// <param name="moment">Co się stało.</param>
    /// <remarks>Nigdy nie rzuca wyjątku.</remarks>
    void Play(FeedbackMoment moment);
}

/// <summary>
/// Chwila w grze, na którą aplikacja odpowiada dźwiękiem.
/// </summary>
/// <remarks>
/// Osobne od <c>SoundEffect</c>: tam jest nazwa <b>próbki</b>, tu nazwa <b>zdarzenia</b>.
/// Rozdział pozwala jednym zdarzeniom dawać wibrację, innym nie, a w przyszłości podłożyć
/// pod dwa zdarzenia ten sam dźwięk bez mylącej nazwy.
/// </remarks>
public enum FeedbackMoment
{
    /// <summary>Na ekranie pojawił się wylosowany ruch.</summary>
    MoveRevealed,

    /// <summary>W turze padło wydarzenie.</summary>
    EventAnnounced,

    /// <summary>Gracz odpadł z gry.</summary>
    PlayerEliminated,

    /// <summary>Partia się rozpoczęła.</summary>
    GameStarted,

    /// <summary>Partia się skończyła.</summary>
    GameFinished,

    /// <summary>Gracz nacisnął przycisk.</summary>
    ButtonTap,
}
