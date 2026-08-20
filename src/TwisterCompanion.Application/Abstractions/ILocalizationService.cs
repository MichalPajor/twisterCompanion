using System.Globalization;

namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Dostęp do przetłumaczonych tekstów i zmiana języka aplikacji.
/// </summary>
/// <remarks>
/// Zmiana języka działa <b>bez restartu aplikacji</b>: serwis zgłasza
/// <see cref="CultureChanged"/>, a warstwa widoku odświeża wszystkie powiązania.
/// </remarks>
public interface ILocalizationService
{
    /// <summary>Aktualnie używany język.</summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>Języki, dla których aplikacja ma tłumaczenia.</summary>
    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    /// <summary>Zgłaszane po każdej zmianie języka.</summary>
    event EventHandler<CultureInfo>? CultureChanged;

    /// <summary>Zwraca tekst interfejsu dla podanego klucza.</summary>
    /// <param name="key">Klucz zasobu.</param>
    string this[string key] { get; }

    /// <summary>Zwraca tekst z wybranego zbioru.</summary>
    /// <param name="key">Klucz zasobu.</param>
    /// <param name="catalog">Zbiór, z którego pochodzi tekst.</param>
    /// <returns>
    /// Przetłumaczony tekst, a gdy klucza nie ma — sam klucz w nawiasach kwadratowych.
    /// </returns>
    /// <remarks>
    /// Brakujący klucz nie jest błędem zatrzymującym aplikację, ale ma być
    /// <b>natychmiast widoczny</b> podczas testów — stąd nawiasy zamiast pustego napisu.
    /// Puste miejsce w interfejsie potrafi umknąć, <c>[Home_Button_Play]</c> nie umknie.
    /// </remarks>
    string GetString(string key, StringCatalog catalog = StringCatalog.Ui);

    /// <summary>Zwraca tekst z podstawionymi argumentami.</summary>
    /// <param name="key">Klucz zasobu zawierającego wzorzec formatowania.</param>
    /// <param name="catalog">Zbiór, z którego pochodzi tekst.</param>
    /// <param name="arguments">Argumenty wzorca.</param>
    string GetFormattedString(string key, StringCatalog catalog, params object?[] arguments);

    /// <summary>Ustawia język aplikacji.</summary>
    /// <param name="culture">Język do ustawienia.</param>
    void SetCulture(CultureInfo culture);

    /// <summary>Ustawia język na podstawie kodu języka.</summary>
    /// <param name="languageCode">
    /// Kod języka, na przykład <c>pl</c>. <see langword="null"/>, wartość pusta albo język
    /// nieobsługiwany oznaczają „idź za językiem systemu", z awaryjnym zejściem na angielski.
    /// </param>
    void SetLanguage(string? languageCode);
}
