using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.EventSelection;
using TwisterCompanion.Domain.MoveSelection;

namespace TwisterCompanion.Application.Game;

/// <summary>
/// Stan jednej tury przechodzący przez kolejne kroki potoku.
/// </summary>
/// <remarks>
/// Kontekst jest zmienny — każdy krok dokłada swój wynik. To celowe: alternatywą byłoby
/// przekazywanie coraz szerszych krotek albo tworzenie nowego obiektu na każdym kroku,
/// co przy potoku rozszerzanym w Etapach 6 i 7 skończyłoby się przepisywaniem wszystkich
/// sygnatur przy dodaniu jednego kroku.
/// </remarks>
public sealed class TurnContext
{
    /// <summary>Partia, w ramach której rozgrywana jest tura.</summary>
    public required GameSession Session { get; init; }

    /// <summary>Parametry algorytmu losowania ruchów.</summary>
    public required MoveSelectionOptions MoveSelectionOptions { get; init; }

    /// <summary>Paczka wydarzeń obowiązująca w partii.</summary>
    public EventPack? EventPack { get; init; }

    /// <summary>Parametry losowania wydarzeń.</summary>
    public EventSelectionOptions EventSelectionOptions { get; init; } = EventSelectionOptions.Default;

    /// <summary>Gracz, którego jest tura — ustawiane przez krok wyboru gracza.</summary>
    public Player? Player { get; set; }

    /// <summary>Wylosowany ruch — ustawiane przez krok losowania.</summary>
    public Move? Move { get; set; }

    /// <summary>
    /// Wylosowane wydarzenie — ustawiane przez krok losowania wydarzeń (Etap 6).
    /// </summary>
    public GameEvent? Event { get; set; }

    /// <summary>Zapisana tura — ustawiane przez krok zapisu.</summary>
    public Turn? Turn { get; set; }

    /// <summary>Komunikat do przekazania graczom — ustawiane przez krok budowy komunikatu.</summary>
    public Announcement? Announcement { get; set; }

    /// <summary>
    /// Osobny komunikat zapowiadający wydarzenie, gdy w tej turze jakieś padło.
    /// </summary>
    /// <remarks>
    /// Oddzielony od komunikatu o ruchu, bo w Etapie 7 obie wypowiedzi będą osobnymi
    /// zdaniami z pauzą między nimi — sklejenie ich w jeden łańcuch brzmiałoby jak
    /// jedno długie zdanie.
    /// </remarks>
    public Announcement? EventAnnouncement { get; set; }
}
