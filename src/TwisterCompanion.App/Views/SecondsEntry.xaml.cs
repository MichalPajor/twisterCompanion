namespace TwisterCompanion.App.Views;

/// <summary>
/// Pole ustawienia czasu w sekundach z przyciskami zmiany.
/// </summary>
/// <remarks>
/// Trzy ustawienia czasu wyglądają identycznie, więc widok jest jeden, a różni je tylko
/// podłączony <c>SecondsSetting</c>.
/// </remarks>
public partial class SecondsEntry : Grid
{
    /// <summary>Tworzy pole.</summary>
    public SecondsEntry() => InitializeComponent();
}
