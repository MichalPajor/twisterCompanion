using System.Globalization;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Zapis i odczyt procentów wpisywanych przez gracza.
/// </summary>
/// <remarks>
/// Jedno miejsce dla obu ekranów wydarzeń — wcześniej ta sama logika stała w dwóch i przy
/// pierwszej poprawce rozjechałaby się.
/// <para>
/// Odczyt przyjmuje <b>oba separatory dziesiętne</b>, bo gracz wpisze ten, który zna,
/// a klawiatura numeryczna podaje ten, który ma system: przy polskich ustawieniach „0.5"
/// nie jest błędem użytkownika, a przy angielskich „0,5" też nie. Normalizacja na kropkę
/// i odczyt kulturą niezmienną obsługują oba przypadki jednym przebiegiem.
/// </para>
/// <para>
/// Zapis idzie już kulturą bieżącą, bo to gracz czyta wynik — po polsku ma zobaczyć „0,5".
/// Format „0.#" ucina zbędne zero po przecinku, więc pełne procenty zostają bez ogonka.
/// </para>
/// </remarks>
internal static class PercentText
{
    /// <summary>Zapisuje procent tak, jak gracz go zobaczy.</summary>
    /// <param name="percent">Wartość procentowa.</param>
    public static string Format(double percent) =>
        percent.ToString("0.#", CultureInfo.CurrentCulture);

    /// <summary>Odczytuje procent wpisany przez gracza.</summary>
    /// <param name="text">Wpisany tekst.</param>
    /// <param name="percent">Odczytana wartość.</param>
    /// <returns><see langword="false"/>, gdy tekst nie jest liczbą — na przykład jest w trakcie
    /// pisania albo pusty.</returns>
    public static bool TryParse(string? text, out double percent)
    {
        percent = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return double.TryParse(
            text.Trim().Replace(',', '.'),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out percent);
    }
}
