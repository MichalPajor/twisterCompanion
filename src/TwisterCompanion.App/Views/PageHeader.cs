using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace TwisterCompanion.App.Views;

/// <summary>
/// Pasek górny ekranu: strzałka powrotu w lewym górnym rogu i tytuł obok niej.
/// </summary>
/// <remarks>
/// Powrót jest <b>zawsze w tym samym miejscu</b> na każdym ekranie i zawsze wygląda tak samo.
/// Wcześniej każdy ekran miał własny przycisk „Wróć" na dole — raz grafitowy, raz z obrysem,
/// raz na całą szerokość — więc gracz musiał go za każdym razem szukać, a na ekranie
/// rozgrywki wyglądał identycznie jak „Zakończ grę", od którego różni się skutkiem.
/// <para>
/// Tytuł jest częścią paska, a nie osobnym napisem w treści: pasek Shella jest wyłączony,
/// więc tytuł i powrót muszą tworzyć jeden rząd, tak jak w każdej innej aplikacji na tym
/// systemie.
/// </para>
/// </remarks>
public sealed class PageHeader : ContentView
{
    /// <summary>Tytuł ekranu — pusty schowa napis, zostawiając samą strzałkę.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title),
        typeof(string),
        typeof(PageHeader),
        string.Empty,
        propertyChanged: (bindable, _, _) => ((PageHeader)bindable).ApplyText());

    /// <summary>
    /// Tekst dopisany po prawej stronie tytułu — bez podania pasek wygląda jak dotąd.
    /// </summary>
    /// <remarks>
    /// Powstał dla ekranu rozgrywki, żeby imię gracza mieściło się w pasku razem z numerem
    /// tury, zamiast zajmować własny wiersz nad kołem. Wiersz kosztował trzydzieści kilka
    /// jednostek wysokości, a pasek i tak stał pusty.
    /// <para>
    /// Tekst jest <b>dopisywany do tytułu</b>, a nie stawiany w osobnej kolumnie — i to jest
    /// poprawka po uwadze z urządzenia. Dwa osobne napisy w dwóch kolumnach dały się ustawić
    /// tylko od lewej krawędzi: wyśrodkować można jeden element, a nie parę stojącą obok
    /// siebie. Jeden napis wraca do reguły obowiązującej w całej aplikacji — tytuł jest
    /// bezwzględnie na środku ekranu — i przy okazji urywa się kropkami w jednym miejscu,
    /// czyli na końcu imienia, bo to ono stoi na końcu linii.
    /// </para>
    /// </remarks>
    public static readonly BindableProperty TrailingTextProperty = BindableProperty.Create(
        nameof(TrailingText),
        typeof(string),
        typeof(PageHeader),
        string.Empty,
        propertyChanged: (bindable, _, _) => ((PageHeader)bindable).ApplyText());

    /// <summary>Komenda powrotu.</summary>
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command),
        typeof(ICommand),
        typeof(PageHeader),
        propertyChanged: (bindable, _, value) =>
            ((PageHeader)bindable)._back.Command = (ICommand?)value);

    /// <summary>Nazwa akcji powrotu dla czytnika ekranu.</summary>
    public static readonly BindableProperty BackDescriptionProperty = BindableProperty.Create(
        nameof(BackDescription),
        typeof(string),
        typeof(PageHeader),
        string.Empty,
        propertyChanged: (bindable, _, value) =>
            SemanticProperties.SetDescription(((PageHeader)bindable)._back, (string?)value));

    /// <summary>Znak akcji po prawej stronie paska — bez podania miejsce zostaje puste.</summary>
    public static readonly BindableProperty ActionGlyphProperty = BindableProperty.Create(
        nameof(ActionGlyph),
        typeof(string),
        typeof(PageHeader),
        string.Empty,
        propertyChanged: (bindable, _, value) =>
        {
            PageHeader header = (PageHeader)bindable;
            string glyph = (string?)value ?? string.Empty;

            header._action.Text = glyph;
            header._action.IsVisible = glyph.Length > 0;
        });

    /// <summary>Komenda akcji po prawej stronie paska.</summary>
    public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
        nameof(ActionCommand),
        typeof(ICommand),
        typeof(PageHeader),
        propertyChanged: (bindable, _, value) =>
            ((PageHeader)bindable)._action.Command = (ICommand?)value);

    /// <summary>Nazwa akcji po prawej stronie dla czytnika ekranu.</summary>
    public static readonly BindableProperty ActionDescriptionProperty = BindableProperty.Create(
        nameof(ActionDescription),
        typeof(string),
        typeof(PageHeader),
        string.Empty,
        propertyChanged: (bindable, _, value) =>
            SemanticProperties.SetDescription(((PageHeader)bindable)._action, (string?)value));

    /// <summary>Czy akcja po prawej stronie jest dostępna.</summary>
    public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
        nameof(IsActionEnabled),
        typeof(bool),
        typeof(PageHeader),
        true,
        propertyChanged: (bindable, _, value) =>
            ((PageHeader)bindable)._action.IsEnabled = (bool)value);

    /// <summary>
    /// Strzałka powrotu jako figura, a nie znak z czcionki.
    /// </summary>
    /// <remarks>
    /// Znak „←" wychodził mały i nie na środku przycisku, bo jego wielkość i położenie zależą
    /// od metryk czcionki systemowej, na które nie mamy wpływu. Figura ma dokładnie tę
    /// wielkość, którą tu podamy, i jest wyśrodkowana co do jednostki.
    /// </remarks>
    private const string BackArrowGeometry = "M 25,14 L 5,14 M 13,5 L 5,14 L 13,23";

    /// <summary>Szerokość przycisku powrotu, powtórzona jako puste miejsce po prawej.</summary>
    private const double TouchTargetSize = 48;

    private readonly Button _back = new();
    private readonly Path _arrow = new()
    {
        Data = new PathGeometryConverter().ConvertFromInvariantString(BackArrowGeometry) as Geometry,
        StrokeThickness = 2.4,
        StrokeLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center,

        // Figura leży na przycisku, więc nie może przechwytywać dotknięć — inaczej
        // naciśnięcie samej strzałki nie trafiałoby w przycisk pod nią.
        InputTransparent = true,
    };

    /// <summary>
    /// Miejsce na jedną akcję ekranu po prawej stronie paska.
    /// </summary>
    /// <remarks>
    /// Puste miejsce tej szerokości i tak tam stało — trzymało tytuł na środku ekranu. Akcja
    /// zajmuje je bez kosztu, a zabiera przycisk z treści: „Zakończ grę" nie musi stać na
    /// ekranie rozgrywki, gdzie jest używany raz na partię, a zajmuje wiersz w każdej turze.
    /// Bez znaku miejsce zostaje puste i pasek zachowuje się jak wcześniej.
    /// </remarks>
    private readonly Button _action = new() { IsVisible = false, WidthRequest = TouchTargetSize };

    /// <summary>
    /// Tytuł ekranu, a na ekranie rozgrywki tytuł razem z imieniem gracza.
    /// </summary>
    /// <remarks>
    /// Jedna linia z wykropkowaniem, nigdy dwie: pasek, który raz jest jednowierszowy, a raz
    /// dwuwierszowy, przesuwałby cały ekran w dół co turę — a imiona bywają długie.
    /// </remarks>
    private readonly Label _title = new()
    {
        VerticalOptions = LayoutOptions.Center,
        HorizontalTextAlignment = TextAlignment.Center,
        LineBreakMode = LineBreakMode.TailTruncation,
        MaxLines = 1,
        IsVisible = false,
    };

    /// <summary>Tworzy pasek górny ekranu.</summary>
    public PageHeader()
    {
        _back.SetDynamicResource(StyleProperty, "AppBarButton");
        _action.SetDynamicResource(StyleProperty, "AppBarButton");
        _title.SetDynamicResource(StyleProperty, "TitleLabel");
        _arrow.SetAppTheme(
            Shape.StrokeProperty,
            ThemeBrush("OnSurfaceLight", Colors.Black),
            ThemeBrush("OnSurfaceDark", Colors.White));

        // Przycisk i strzałka leżą w tej samej komórce: przycisk daje cel dotknięcia
        // i reakcję systemu na naciśnięcie, figura — sam znak.
        Grid target = new();
        target.Add(_back);
        target.Add(_arrow);

        // Trzy kolumny, a boczne o STAŁEJ szerokości — nie „Auto". To była przyczyna tego, że
        // tytuł odbijał w prawo: przy schowanej akcji kolumna z prawej zwijała się do zera,
        // więc środek kolumny środkowej przesuwał się o pół szerokości przycisku powrotu.
        // Stała szerokość po obu stronach trzyma tytuł na środku ekranu niezależnie od tego,
        // czy ekran ma akcję po prawej.
        Grid row = new()
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(TouchTargetSize)),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(TouchTargetSize)),
            ],
        };

        row.Add(target);
        row.Add(_title);
        Grid.SetColumn(_title, 1);

        row.Add(_action);
        Grid.SetColumn(_action, 2);

        Content = row;
        Margin = new Thickness(0, 0, 0, 4);
    }

    /// <inheritdoc cref="TitleProperty" />
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <inheritdoc cref="TrailingTextProperty" />
    public string TrailingText
    {
        get => (string)GetValue(TrailingTextProperty);
        set => SetValue(TrailingTextProperty, value);
    }

    /// <inheritdoc cref="CommandProperty" />
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <inheritdoc cref="BackDescriptionProperty" />
    public string BackDescription
    {
        get => (string)GetValue(BackDescriptionProperty);
        set => SetValue(BackDescriptionProperty, value);
    }

    /// <inheritdoc cref="ActionGlyphProperty" />
    public string ActionGlyph
    {
        get => (string)GetValue(ActionGlyphProperty);
        set => SetValue(ActionGlyphProperty, value);
    }

    /// <inheritdoc cref="ActionCommandProperty" />
    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    /// <inheritdoc cref="ActionDescriptionProperty" />
    public string ActionDescription
    {
        get => (string)GetValue(ActionDescriptionProperty);
        set => SetValue(ActionDescriptionProperty, value);
    }

    /// <inheritdoc cref="IsActionEnabledProperty" />
    public bool IsActionEnabled
    {
        get => (bool)GetValue(IsActionEnabledProperty);
        set => SetValue(IsActionEnabledProperty, value);
    }

    /// <summary>Składa napis paska z tytułu i tekstu dopisywanego po prawej.</summary>
    private void ApplyText()
    {
        string title = Title ?? string.Empty;
        string trailing = TrailingText ?? string.Empty;

        _title.Text = trailing.Length > 0 ? $"{title} {trailing}".Trim() : title;
        _title.IsVisible = _title.Text.Length > 0;
    }

    /// <summary>
    /// Zwraca pędzel w barwie z palety aplikacji.
    /// </summary>
    /// <param name="key">Klucz barwy w słowniku zasobów.</param>
    /// <param name="fallback">Barwa awaryjna, gdyby słownik nie był jeszcze dostępny.</param>
    /// <remarks>
    /// Barwa jest czytana z palety, a nie wpisana w kodzie: dwie kopie tej samej wartości
    /// rozjeżdżają się przy pierwszej zmianie motywu. Wartość awaryjna istnieje tylko po to,
    /// żeby brak słownika nie znaczył „strzałka bez koloru", czyli strzałka niewidoczna.
    /// </remarks>
    private static Brush ThemeBrush(string key, Color fallback)
    {
        if (Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(key, out object? value) == true
            && value is Color color)
        {
            return new SolidColorBrush(color);
        }

        return new SolidColorBrush(fallback);
    }
}
