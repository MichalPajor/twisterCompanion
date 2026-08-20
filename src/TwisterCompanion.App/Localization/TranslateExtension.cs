using System.Globalization;

namespace TwisterCompanion.App.Localization;

/// <summary>
/// Rozszerzenie znaczników XAML podstawiające przetłumaczony tekst.
/// </summary>
/// <remarks>
/// Użycie: <c>Text="{loc:Translate Home_Button_Game}"</c>.
/// <para>
/// Zwraca <b>powiązanie</b>, a nie gotowy napis. To celowe: gdyby zwracało napis,
/// tekst zostałby ustalony w chwili wczytania ekranu i zmiana języka wymagałaby restartu
/// aplikacji. Powiązanie wskazuje na <see cref="LocalizationResourceManager"/>, który po
/// zmianie języka zgłasza zmianę i odświeża wszystkie teksty naraz.
/// </para>
/// </remarks>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public sealed class TranslateExtension : IMarkupExtension<BindingBase>
{
    /// <summary>Klucz zasobu do przetłumaczenia.</summary>
    public string Key { get; set; } = string.Empty;

    /// <inheritdoc />
    public BindingBase ProvideValue(IServiceProvider serviceProvider) => new Binding
    {
        Path = string.Create(CultureInfo.InvariantCulture, $"[{Key}]"),
        Mode = BindingMode.OneWay,
        Source = LocalizationResourceManager.Instance,
    };

    /// <inheritdoc />
    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) =>
        ProvideValue(serviceProvider);
}
