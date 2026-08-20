namespace TwisterCompanion.App.Views;

/// <summary>
/// Nagłówek sekcji ze znakiem: „♪ Głos", „◉ Sterowanie głosem".
/// </summary>
/// <remarks>
/// Ekran ustawień to jedna długa lista pól. Bez znaków wszystkie nagłówki wyglądały tak samo
/// i sekcje zlewały się w ciąg tekstu — znak daje oku punkt zaczepienia przy przewijaniu.
/// <para>
/// Znak podaje XAML, a podpis przychodzi z tłumaczeń, z tego samego powodu co
/// w <see cref="GlyphButton"/>: znak jest identyczny w każdym języku.
/// </para>
/// <para>
/// Znaki są <b>jednobarwne</b> i biorą kolor tekstu. Kolorowe emotki wnosiły tu barwy,
/// które nic nie znaczyły — a paleta tej aplikacji ma cztery kolory z maty i jeden kolor
/// akcji, więc pomarańczowa emotka obok grafitowego napisu była szóstym, przypadkowym.
/// </para>
/// </remarks>
public sealed class SectionHeader : ContentView
{
    /// <summary>Znak obrazkowy sekcji.</summary>
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(
        nameof(Glyph),
        typeof(string),
        typeof(SectionHeader),
        string.Empty,
        propertyChanged: (bindable, _, value) =>
            ((SectionHeader)bindable)._glyph.Text = (string?)value);

    /// <summary>Tytuł sekcji.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text),
        typeof(string),
        typeof(SectionHeader),
        string.Empty,
        propertyChanged: (bindable, _, value) =>
            ((SectionHeader)bindable)._text.Text = (string?)value);

    /// <summary>
    /// Rozmiar znaku sekcji.
    /// </summary>
    /// <remarks>
    /// Większy niż tytuł obok (22), bo znak jednobarwny rysuje się w środku kwadratu czcionki
    /// i przy równym rozmiarze wygląda mniejszy od pisma. Przy 20 był po prostu za mały.
    /// </remarks>
    private const double GlyphFontSize = 26;

    private readonly Label _glyph = new() { FontSize = GlyphFontSize, VerticalOptions = LayoutOptions.Center };
    private readonly Label _text = new() { VerticalOptions = LayoutOptions.Center };

    /// <summary>Tworzy nagłówek sekcji.</summary>
    public SectionHeader()
    {
        _text.SetDynamicResource(StyleProperty, "TitleLabel");

        Content = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { _glyph, _text },
        };

        Margin = new Thickness(0, 16, 0, 0);
    }

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
}
