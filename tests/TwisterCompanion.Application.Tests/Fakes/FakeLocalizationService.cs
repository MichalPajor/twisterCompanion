using System.Globalization;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Tłumaczenia zastępcze o przewidywalnym zachowaniu.
/// </summary>
/// <remarks>
/// Wzorce z podstawieniami są tu odwzorowane wprost, bo testy komunikatów sprawdzają
/// <b>kolejność i kompletność członów</b>, a nie treść tłumaczeń — tym zajmują się testy
/// zasobów w tym samym projekcie.
/// </remarks>
internal sealed class FakeLocalizationService : ILocalizationService
{
    private static readonly Dictionary<string, string> Templates = new(StringComparer.Ordinal)
    {
        ["Voice_Announce_PlayerTurn"] = "{0}.",
        ["Voice_Announce_Move"] = "{0} — {1}.",
        ["Voice_Announce_Event"] = "Wydarzenie: {0}.",
        ["Voice_Announce_PlayerEliminated"] = "{0} odpada.",
        ["Voice_Announce_Winner"] = "Wygrywa {0}.",
        ["Game_Label_Turn"] = "Tura {0}",
        ["Game_Summary_Winner"] = "Wygrywa {0} po {1} turach.",
        ["Game_Summary_NoWinner"] = "Koniec gry po {0} turach.",
    };

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
    public string GetString(string key, StringCatalog catalog = StringCatalog.Ui) =>
        Templates.TryGetValue(key, out string? template) ? template : key;

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
