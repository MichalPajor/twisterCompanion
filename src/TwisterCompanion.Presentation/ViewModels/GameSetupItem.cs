namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Jeden wiersz podsumowania zasad przed rozpoczęciem partii.
/// </summary>
/// <param name="Glyph">Znak jednobarwny wiersza, na przykład <c>☰</c>.</param>
/// <param name="Label">Nazwa parametru w aktualnym języku.</param>
/// <param name="Value">Wartość parametru w aktualnym języku.</param>
/// <remarks>
/// Wiersze powstają w ViewModelu, a nie w XAML, bo ich <b>liczba</b> zależy od stanu: partia
/// bez wydarzeń nie ma czego pokazać w wierszu o paczce, a tryb bez odpadania inaczej opisuje
/// koniec gracza. Lista wierszy pozwala też dołożyć parametr bez zmiany układu ekranu.
/// <para>
/// Znak jest <b>jednobarwny</b> i bierze kolor tekstu, jak wszystkie znaki w tej aplikacji:
/// kolorowe emotki dokładały tu barw, które nic nie znaczyły, a jedna z nich świeciła
/// pomarańczem obok grafitowych napisów.
/// </para>
/// </remarks>
public sealed record GameSetupItem(string Glyph, string Label, string Value);
