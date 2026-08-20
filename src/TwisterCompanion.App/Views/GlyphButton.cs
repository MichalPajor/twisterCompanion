using System.Windows.Input;

namespace TwisterCompanion.App.Views;

/// <summary>
/// Przycisk z znakiem obrazkowym przed podpisem.
/// </summary>
/// <remarks>
/// Istnieje z jednego powodu: znak nie może wejść do plików tłumaczeń. Emotka jest taka sama
/// w każdym języku, więc wpisana do zasobów oznaczałaby dwie kopie tej samej wartości i dwa
/// miejsca do rozjechania się przy pierwszej poprawce. Tutaj znak podaje XAML, a podpis
/// przychodzi z tłumaczeń — i dopiero ten typ składa jedno z drugim.
/// <para>
/// Zbudowany w kodzie, nie w XAML: całą treść stanowi jeden <see cref="Button"/>, a powiązania
/// „do samego siebie" w pliku XAML kosztowałyby więcej linii niż te trzy przypisania.
/// </para>
/// <para>
/// Nazwa akcji trafia do <see cref="SemanticProperties.DescriptionProperty"/> bez znaku —
/// czytnik ekranu ma przeczytać „Pauza", a nie „znak pauzy Pauza".
/// </para>
/// </remarks>
public sealed class GlyphButton : ContentView
{
    /// <summary>Znak obrazkowy pokazywany przed podpisem.</summary>
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(
        nameof(Glyph),
        typeof(string),
        typeof(GlyphButton),
        string.Empty,
        propertyChanged: OnContentChanged);

    /// <summary>Podpis przycisku.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text),
        typeof(string),
        typeof(GlyphButton),
        string.Empty,
        propertyChanged: OnContentChanged);

    /// <summary>Komenda wywoływana naciśnięciem.</summary>
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command),
        typeof(ICommand),
        typeof(GlyphButton),
        propertyChanged: (bindable, _, value) =>
            ((GlyphButton)bindable)._button.Command = (ICommand?)value);

    /// <summary>Parametr komendy.</summary>
    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter),
        typeof(object),
        typeof(GlyphButton),
        propertyChanged: (bindable, _, value) =>
            ((GlyphButton)bindable)._button.CommandParameter = value);

    /// <summary>
    /// Rozmiar czcionki przycisku — bez podania obowiązuje rozmiar ze stylu.
    /// </summary>
    /// <remarks>
    /// Znaki nie rysują się w jednakowej skali: trójkąt „▶" wypełnia cały kwadrat czcionki,
    /// a strzałka w okręgu „⟳" zajmuje jego środek i przy tym samym rozmiarze wygląda o klasę
    /// mniej. Ta właściwość pozwala nadrobić różnicę w miejscu, gdzie widać ją najbardziej,
    /// bez ruszania stylu wspólnego dla wszystkich przycisków.
    /// </remarks>
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize),
        typeof(double),
        typeof(GlyphButton),
        0.0,
        propertyChanged: (bindable, _, value) =>
        {
            if (value is double size && size > 0)
            {
                ((GlyphButton)bindable)._button.FontSize = size;
            }
        });

    /// <summary>Styl przycisku — bez podania obowiązuje styl domyślny aplikacji.</summary>
    public static readonly BindableProperty ButtonStyleProperty = BindableProperty.Create(
        nameof(ButtonStyle),
        typeof(Style),
        typeof(GlyphButton),
        propertyChanged: (bindable, _, value) =>
            ((GlyphButton)bindable)._button.Style = (Style?)value);

    private readonly Button _button = new() { HorizontalOptions = LayoutOptions.Fill };

    /// <summary>Tworzy przycisk ze znakiem.</summary>
    public GlyphButton() => Content = _button;

    /// <inheritdoc cref="GlyphProperty" />
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <inheritdoc cref="TextProperty" />
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <inheritdoc cref="CommandProperty" />
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <inheritdoc cref="CommandParameterProperty" />
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <inheritdoc cref="FontSizeProperty" />
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <inheritdoc cref="ButtonStyleProperty" />
    public Style? ButtonStyle
    {
        get => (Style?)GetValue(ButtonStyleProperty);
        set => SetValue(ButtonStyleProperty, value);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Wyłączenie przenosimy na przycisk wprost. Sam <see cref="ContentView"/> wyłączony
    /// blokuje naciśnięcie, ale nie przełącza stanu wizualnego przycisku — wyglądałby na
    /// czynny, a nie reagował.
    /// </remarks>
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsEnabledProperty.PropertyName)
        {
            _button.IsEnabled = IsEnabled;
        }
    }

    private static void OnContentChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((GlyphButton)bindable).RefreshContent();

    private void RefreshContent()
    {
        _button.Text = string.IsNullOrEmpty(Glyph) ? Text : $"{Glyph}  {Text}";

        SemanticProperties.SetDescription(_button, Text);
    }
}
