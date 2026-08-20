using System.Globalization;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Presentation.Tests.Fakes;

/// <summary>
/// Tłumaczenia zastępcze — zwracają sam klucz jako tekst.
/// </summary>
/// <remarks>
/// Dzięki temu test może sprawdzić, <b>który klucz</b> ViewModel wykorzystał, zamiast
/// porównywać przetłumaczone napisy. Testy warstwy prezentacji nie mają weryfikować
/// treści tłumaczeń — tym zajmują się testy zasobów.
/// </remarks>
internal sealed class FakeLocalizationService : ILocalizationService
{
    /// <inheritdoc />
    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo("pl");

    /// <inheritdoc />
    public IReadOnlyList<CultureInfo> SupportedCultures { get; } =
    [
        CultureInfo.GetCultureInfo("en"),
        CultureInfo.GetCultureInfo("pl"),
    ];

    /// <inheritdoc />
    public event EventHandler<CultureInfo>? CultureChanged;

    /// <inheritdoc />
    public string this[string key] => GetString(key);

    /// <inheritdoc />
    public string GetString(string key, StringCatalog catalog = StringCatalog.Ui) => key;

    /// <inheritdoc />
    public string GetFormattedString(string key, StringCatalog catalog, params object?[] arguments) =>
        string.Format(CurrentCulture, key, arguments);

    /// <inheritdoc />
    public void SetCulture(CultureInfo culture)
    {
        CurrentCulture = culture;
        CultureChanged?.Invoke(this, culture);
    }

    /// <inheritdoc />
    public void SetLanguage(string? languageCode) =>
        SetCulture(CultureInfo.GetCultureInfo(languageCode ?? "en"));
}
