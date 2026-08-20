namespace TwisterCompanion.Application.Settings;

/// <summary>
/// Wybór motywu kolorystycznego aplikacji.
/// </summary>
/// <remarks>
/// Trzy wartości, nie dwie: „jak w systemie" jest osobnym wyborem, a nie brakiem wyboru.
/// Telefon przełączający się na ciemny motyw po zachodzie słońca ma pociągnąć za sobą
/// aplikację — chyba że gracz zażyczył sobie konkretnego motywu na stałe.
/// </remarks>
public enum AppThemePreference
{
    /// <summary>Motyw zgodny z ustawieniem systemu.</summary>
    System,

    /// <summary>Motyw jasny, niezależnie od systemu.</summary>
    Light,

    /// <summary>Motyw ciemny, niezależnie od systemu.</summary>
    Dark,
}
