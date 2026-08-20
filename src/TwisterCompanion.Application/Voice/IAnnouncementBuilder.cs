using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Voice;

/// <summary>
/// Buduje komunikaty dla graczy z przetłumaczonych fragmentów.
/// </summary>
/// <remarks>
/// Składanie tekstu jest wydzielone z silnika gry, bo to osobna odpowiedzialność i osobne
/// ryzyko: kolejność członów oraz interpunkcja zależą od języka. Silnik wie <i>co</i>
/// zaszło, ten typ wie <i>jak to powiedzieć</i>.
/// </remarks>
public interface IAnnouncementBuilder
{
    /// <summary>Buduje wywołanie gracza, którego jest tura.</summary>
    /// <param name="player">Gracz, którego jest tura.</param>
    /// <remarks>
    /// Osobny komunikat, bo pada przed wydarzeniem i przed poleceniem ruchu — gracz ma
    /// wiedzieć, że to jego kolej, zanim usłyszy, co ma zrobić.
    /// </remarks>
    Announcement BuildPlayerTurn(Player player);

    /// <summary>Buduje polecenie ruchu, na przykład „prawa ręka — czerwony.".</summary>
    /// <param name="turn">Rozegrana tura.</param>
    /// <remarks>Bez imienia gracza — podaje je <see cref="BuildPlayerTurn"/>.</remarks>
    Announcement BuildMove(Turn turn);

    /// <summary>Buduje zapowiedź wydarzenia.</summary>
    /// <param name="gameEvent">Wylosowane wydarzenie.</param>
    Announcement BuildEvent(GameEvent gameEvent);

    /// <summary>Buduje komunikat o rozpoczęciu gry.</summary>
    Announcement BuildGameStart();

    /// <summary>Buduje komunikat o zakończeniu gry.</summary>
    /// <param name="winner">Zwycięzca, jeśli został wyłoniony.</param>
    Announcement BuildGameEnd(Player? winner);

    /// <summary>Buduje komunikat o odpadnięciu gracza.</summary>
    /// <param name="player">Gracz, który odpadł.</param>
    Announcement BuildPlayerEliminated(Player player);

    /// <summary>Buduje komunikat o wstrzymaniu gry.</summary>
    Announcement BuildPaused();

    /// <summary>Buduje komunikat o wznowieniu gry.</summary>
    Announcement BuildResumed();

    /// <summary>Zwraca nazwę wydarzenia w aktualnym języku.</summary>
    /// <param name="gameEvent">Wydarzenie.</param>
    /// <remarks>
    /// Wydarzenia własne użytkownika mają nazwę wpisaną ręcznie i nie podlegają tłumaczeniu.
    /// Wydarzenia z paczek wbudowanych mają klucz zasobu.
    /// </remarks>
    string GetEventName(GameEvent gameEvent);

    /// <summary>Buduje próbkę głosu dla ekranu ustawień.</summary>
    /// <remarks>
    /// Próbka jest zdaniem w formie polecenia ruchu, żeby użytkownik ocenił głos w takiej
    /// formie, w jakiej usłyszy go w grze.
    /// </remarks>
    Announcement BuildVoiceSample();
}
