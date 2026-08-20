namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Krótka wibracja urządzenia.
/// </summary>
/// <remarks>
/// Port do platformy. Wibracja jest w tej grze <b>uzupełnieniem dźwięku, nie jego
/// zamiennikiem</b>: telefon leży na podłodze, więc nikt jej nie czuje w ręce — ale słychać
/// ją jako stuknięcie o podłogę, i to wystarcza, gdy dźwięki są wyciszone.
/// </remarks>
public interface IHapticService
{
    /// <summary>
    /// Wywołuje wibrację o podanej sile.
    /// </summary>
    /// <param name="intensity">Siła wibracji.</param>
    /// <remarks>Nigdy nie rzuca wyjątku — urządzenie może nie mieć wibracji.</remarks>
    void Vibrate(HapticIntensity intensity);
}

/// <summary>Siła wibracji.</summary>
public enum HapticIntensity
{
    /// <summary>Stuknięcie — potwierdzenie naciśnięcia.</summary>
    Light,

    /// <summary>Dłuższa wibracja — coś ważnego w partii.</summary>
    Strong,
}
