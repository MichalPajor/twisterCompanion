using System.ComponentModel;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.App.Localization;

/// <summary>
/// Źródło powiązań dla tekstów w XAML — zgłasza zmianę wszystkich tekstów po przełączeniu
/// języka.
/// </summary>
/// <remarks>
/// To ten element sprawia, że <b>zmiana języka nie wymaga restartu aplikacji</b>. Powiązania
/// utworzone przez <see cref="TranslateExtension"/> wskazują na indekser tej klasy;
/// zgłoszenie zmiany właściwości odświeża je wszystkie naraz.
/// <para>
/// Dostęp statyczny jest tu wyjątkiem od reguły „wszystko przez kontener" i jedynym w całej
/// solucji. Powód jest techniczny: rozszerzenia znaczników XAML tworzy parser, który nie zna
/// kontenera zależności i nie da się w nie nic wstrzyknąć. Wywołanie
/// <see cref="Initialize"/> w <c>MauiProgram</c> musi nastąpić przed pierwszym wczytaniem
/// XAML — i tak się dzieje, bo instancja aplikacji powstaje po zbudowaniu kontenera.
/// </para>
/// </remarks>
public sealed class LocalizationResourceManager : INotifyPropertyChanged
{
    private static LocalizationResourceManager? _instance;

    private readonly ILocalizationService _localization;

    private LocalizationResourceManager(ILocalizationService localization)
    {
        _localization = localization;

        // Menedżer żyje tak długo jak aplikacja, więc subskrypcja bez zwalniania
        // nie tworzy wycieku.
        _localization.CultureChanged += OnCultureChanged;
    }

    /// <summary>Jedyna instancja menedżera.</summary>
    /// <exception cref="InvalidOperationException">
    /// Gdy <see cref="Initialize"/> nie zostało jeszcze wywołane.
    /// </exception>
    public static LocalizationResourceManager Instance =>
        _instance ?? throw new InvalidOperationException(
            $"{nameof(LocalizationResourceManager)} nie został zainicjalizowany. "
            + $"Wywołaj {nameof(Initialize)} w MauiProgram przed wczytaniem XAML.");

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Zwraca przetłumaczony tekst interfejsu.</summary>
    /// <param name="key">Klucz zasobu.</param>
    public string this[string key] => _localization[key];

    /// <summary>Tworzy instancję menedżera na podstawie serwisu z kontenera.</summary>
    /// <param name="localization">Serwis tłumaczeń.</param>
    public static void Initialize(ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(localization);

        _instance = new LocalizationResourceManager(localization);
    }

    private void OnCultureChanged(object? sender, System.Globalization.CultureInfo culture)
    {
        // Zgłoszenie z wartością null oznacza „zmieniło się wszystko" i odświeża każde
        // powiązanie. Nazwa "Item[]" to konwencja dla powiązań przez indekser — zgłaszamy
        // oba warianty, żeby nie zależeć od szczegółu implementacji powiązań.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName: null));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
