using System.Globalization;
using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Settings;

namespace TwisterCompanion.Infrastructure.Localization;

/// <summary>
/// Tłumaczenia oparte na plikach zasobów <c>.resx</c> z warstwy aplikacji.
/// </summary>
/// <remarks>
/// Serwis nasłuchuje zmian ustawień i sam stosuje wybrany język. Dzięki temu istnieje
/// dokładnie jedna droga zmiany języka — zapis do ustawień — i nie da się zmienić języka
/// tak, żeby nie został zapamiętany, ani zapamiętać go bez zastosowania.
/// <para>
/// Ustawiane są obie kultury wątku: <see cref="CultureInfo.CurrentUICulture"/> wybiera
/// zestaw zasobów, a <see cref="CultureInfo.CurrentCulture"/> odpowiada za formatowanie
/// liczb i dat. Zmiana języka bez drugiej z nich dawałaby polski interfejs z angielskim
/// formatowaniem.
/// </para>
/// </remarks>
internal sealed class ResxLocalizationService : ILocalizationService, IDisposable
{
    private const string ResourceNamespace = "TwisterCompanion.Application.Resources.Strings.";
    private const string UiResourceName = ResourceNamespace + "AppResources";
    private const string VoiceResourceName = ResourceNamespace + "VoiceResources";
    private const string FallbackLanguageCode = "en";

    private readonly ResourceManager _uiResources;
    private readonly ResourceManager _voiceResources;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ResxLocalizationService> _logger;
    private readonly HashSet<string> _reportedMissingKeys = [];

    /// <summary>Tworzy serwis tłumaczeń.</summary>
    /// <param name="settingsService">Ustawienia, z których pochodzi wybrany język.</param>
    /// <param name="logger">Logger serwisu.</param>
    public ResxLocalizationService(
        ISettingsService settingsService,
        ILogger<ResxLocalizationService> logger)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(logger);

        _settingsService = settingsService;
        _logger = logger;

        Assembly resourceAssembly = typeof(ILocalizationService).Assembly;
        _uiResources = new ResourceManager(UiResourceName, resourceAssembly);
        _voiceResources = new ResourceManager(VoiceResourceName, resourceAssembly);

        CurrentCulture = ResolveCulture(null);
        ApplyToCurrentThread(CurrentCulture);

        _settingsService.Changed += OnSettingsChanged;
    }

    /// <inheritdoc />
    public CultureInfo CurrentCulture { get; private set; }

    /// <summary>
    /// Języki, dla których istnieją tłumaczenia.
    /// </summary>
    /// <remarks>
    /// Lista jest zadeklarowana, a nie odkrywana automatycznie — wykrywanie satelickich
    /// zestawów zasobów w czasie działania zachowuje się różnie w zależności od tego, jak
    /// aplikacja została spakowana i przycięta. Dodanie języka to nowy plik <c>.resx</c>
    /// oraz jeden wpis tutaj.
    /// </remarks>
    public IReadOnlyList<CultureInfo> SupportedCultures { get; } =
    [
        CultureInfo.GetCultureInfo("de"),
        CultureInfo.GetCultureInfo("en"),
        CultureInfo.GetCultureInfo("es"),
        CultureInfo.GetCultureInfo("fr"),
        CultureInfo.GetCultureInfo("it"),
        CultureInfo.GetCultureInfo("pl"),
        CultureInfo.GetCultureInfo("pt-BR"),
        CultureInfo.GetCultureInfo("ru"),
        CultureInfo.GetCultureInfo("tr"),
        CultureInfo.GetCultureInfo("uk"),
    ];

    /// <inheritdoc />
    public event EventHandler<CultureInfo>? CultureChanged;

    /// <inheritdoc />
    public string this[string key] => GetString(key);

    /// <inheritdoc />
    public string GetString(string key, StringCatalog catalog = StringCatalog.Ui)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        ResourceManager resources = catalog == StringCatalog.Voice ? _voiceResources : _uiResources;

        string? value = null;
        try
        {
            value = resources.GetString(key, CurrentCulture);
        }
        catch (MissingManifestResourceException exception)
        {
            _logger.LogError(exception, "Brak zestawu zasobów {Catalog}.", catalog);
        }

        if (value is not null)
        {
            return value;
        }

        ReportMissingKey(key, catalog);

        // Klucz w nawiasach zamiast pustego napisu — brak tłumaczenia ma być widoczny
        // od razu na ekranie, a nie objawiać się pustym miejscem.
        return $"[{key}]";
    }

    /// <inheritdoc />
    public string GetFormattedString(string key, StringCatalog catalog, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        string template = GetString(key, catalog);

        try
        {
            return string.Format(CurrentCulture, template, arguments);
        }
        catch (FormatException exception)
        {
            _logger.LogError(
                exception,
                "Wzorzec {Key} nie zgadza się z liczbą argumentów ({Count}).",
                key,
                arguments.Length);

            return template;
        }
    }

    /// <inheritdoc />
    public void SetCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (CurrentCulture.Name == culture.Name)
        {
            return;
        }

        CurrentCulture = culture;
        ApplyToCurrentThread(culture);

        _logger.LogInformation("Język zmieniony na {Culture}.", culture.Name);

        CultureChanged?.Invoke(this, culture);
    }

    /// <inheritdoc />
    public void SetLanguage(string? languageCode) => SetCulture(ResolveCulture(languageCode));

    /// <inheritdoc />
    public void Dispose() => _settingsService.Changed -= OnSettingsChanged;

    /// <summary>
    /// Wybiera język: żądany, a gdy nieobsługiwany — systemowy, a gdy i ten nieobsługiwany
    /// — angielski.
    /// </summary>
    private CultureInfo ResolveCulture(string? languageCode)
    {
        if (!string.IsNullOrWhiteSpace(languageCode)
            && TryFindSupported(languageCode, out CultureInfo? requested))
        {
            return requested;
        }

        string systemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return TryFindSupported(systemLanguage, out CultureInfo? system)
            ? system
            : CultureInfo.GetCultureInfo(FallbackLanguageCode);
    }

    private bool TryFindSupported(string languageCode, out CultureInfo culture)
    {
        string twoLetter = languageCode.Trim().Split('-')[0];

        CultureInfo? match = SupportedCultures.FirstOrDefault(candidate =>
            string.Equals(candidate.TwoLetterISOLanguageName, twoLetter, StringComparison.OrdinalIgnoreCase));

        culture = match ?? CultureInfo.InvariantCulture;

        return match is not null;
    }

    private static void ApplyToCurrentThread(CultureInfo culture)
    {
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;

        // Wątki utworzone później też muszą widzieć wybrany język — na przykład wątek
        // obsługujący rozpoznawanie mowy (Etap 8).
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
    }

    private void OnSettingsChanged(object? sender, AppSettings settings) =>
        SetLanguage(settings.LanguageCode);

    private void ReportMissingKey(string key, StringCatalog catalog)
    {
        // Powiązania w interfejsie odpytują ten sam klucz wielokrotnie, więc zgłaszamy
        // każdy brak jeden raz — inaczej log stałby się bezużyteczny.
        if (!_reportedMissingKeys.Add($"{catalog}:{key}:{CurrentCulture.Name}"))
        {
            return;
        }

        _logger.LogWarning(
            "Brak tłumaczenia klucza {Key} w zbiorze {Catalog} dla języka {Culture}.",
            key,
            catalog,
            CurrentCulture.Name);
    }
}
