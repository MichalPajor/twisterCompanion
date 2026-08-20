namespace TwisterCompanion.Domain.Enums;

/// <summary>
/// Zasada odpadania graczy w trybie gry.
/// </summary>
/// <remarks>
/// Rozstrzyga, co dzieje się po upadku gracza — i jest jedyną regułą trybu, która zmienia
/// przebieg partii, a nie tylko jej parametry losowania.
/// </remarks>
public enum EliminationRule
{
    /// <summary>
    /// Gracze sami decydują, kto odpadł — przyciskiem albo komendą głosową.
    /// </summary>
    /// <remarks>
    /// Zasada klasyczna: aplikacja nie ma jak zobaczyć upadku, więc zgłaszają go gracze.
    /// </remarks>
    Manual,

    /// <summary>
    /// Nikt nie odpada, partia toczy się dalej.
    /// </summary>
    /// <remarks>
    /// Dla trybu dla dzieci i zabawy bez rywalizacji: upadek nie kończy udziału, a zgłoszenie
    /// odpadnięcia jest pomijane. Bez tego najmłodszy gracz wypada z gry po minucie i siedzi
    /// obok do końca partii.
    /// </remarks>
    NoElimination,
}
