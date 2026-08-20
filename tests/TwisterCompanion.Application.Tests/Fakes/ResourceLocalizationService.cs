using System.Globalization;
using System.Resources;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Tłumaczenia czytane z prawdziwych plików zasobów warstwy aplikacji.
/// </summary>
/// <remarks>
/// Testy komend głosowych muszą chodzić po <b>rzeczywistych</b> frazach, a nie po
/// podstawionych: połowa ryzyka rozpoznawania siedzi w tym, jakie synonimy wpisaliśmy
/// do <c>.resx</c>. Ten serwis sięga po te same zasoby, które trafiają do aplikacji,
/// pomijając jedynie warstwę platformową.
/// </remarks>
internal sealed class ResourceLocalizationService : ILocalizationService
{
    private static readonly ResourceManager UiResources = new(
        "TwisterCompanion.Application.Resources.Strings.AppResources",
        typeof(ILocalizationService).Assembly);

    private static readonly ResourceManager VoiceResources = new(
        "TwisterCompanion.Application.Resources.Strings.VoiceResources",
        typeof(ILocalizationService).Assembly);

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
    public string GetString(string key, StringCatalog catalog = StringCatalog.Ui)
    {
        ResourceManager manager = catalog == StringCatalog.Voice ? VoiceResources : UiResources;

        return manager.GetString(key, CurrentCulture) ?? key;
    }

    /// <inheritdoc />
    public string GetFormattedString(string key, StringCatalog catalog, params object?[] arguments) =>
        string.Format(CurrentCulture, GetString(key, catalog), arguments);

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
