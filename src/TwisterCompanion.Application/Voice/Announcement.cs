namespace TwisterCompanion.Application.Voice;

/// <summary>
/// Komunikat kierowany do graczy.
/// </summary>
/// <param name="Text">Gotowy tekst w aktualnym języku.</param>
/// <param name="Kind">Rodzaj komunikatu.</param>
/// <remarks>
/// Ten sam komunikat trafia na ekran i — od Etapu 7 — do odczytu głosowego. Rodzaj jest
/// potrzebny właśnie tam: wypowiedź o ruchu można przerwać komendą „Powtórz", a informacji
/// o zakończeniu gry już nie.
/// </remarks>
public sealed record Announcement(string Text, AnnouncementKind Kind);

/// <summary>
/// Rodzaj komunikatu.
/// </summary>
public enum AnnouncementKind
{
    /// <summary>Wywołanie gracza, którego jest tura.</summary>
    /// <remarks>
    /// Osobno od polecenia ruchu: gracz ma usłyszeć swoje imię, zanim padnie polecenie,
    /// żeby nie orientował się w połowie komunikatu, którego początku już nie usłyszał.
    /// </remarks>
    PlayerTurn,

    /// <summary>Polecenie ruchu dla gracza.</summary>
    Move,

    /// <summary>Wystąpiło wydarzenie.</summary>
    Event,

    /// <summary>Gra się rozpoczęła.</summary>
    GameStart,

    /// <summary>Gra się zakończyła.</summary>
    GameEnd,

    /// <summary>Gracz odpadł.</summary>
    PlayerEliminated,

    /// <summary>Gra wstrzymana.</summary>
    Paused,

    /// <summary>Gra wznowiona.</summary>
    Resumed,

    /// <summary>Próbka głosu z ekranu ustawień.</summary>
    /// <remarks>
    /// Nie pochodzi z rozgrywki i nigdy nie przechodzi przez silnik gry — ekran ustawień
    /// przekazuje ją wprost do odczytu. Osobny rodzaj pozwala ekranowi rozgrywki pominąć
    /// ją, gdyby kiedyś zaczęła lecieć tą samą drogą.
    /// </remarks>
    VoiceSample,
}
