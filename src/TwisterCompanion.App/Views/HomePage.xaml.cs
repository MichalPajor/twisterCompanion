using TwisterCompanion.App.Services;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.App.Views;

/// <summary>
/// Ekran startowy aplikacji.
/// </summary>
/// <remarks>
/// Ma własne wejście: znak aplikacji „wskakuje" ze zmniejszenia, a wiersze menu pojawiają się
/// jeden po drugim. To jedyne miejsce w aplikacji, gdzie animacja jest ozdobą, a nie
/// informacją — i jedyne, w którym można sobie na to pozwolić, bo nikt tu na nic nie czeka.
/// <para>
/// Wejście odtwarza się <b>raz na uruchomienie</b>, a nie przy każdym powrocie na ekran.
/// Powrót z ustawień do menu nie jest wejściem do aplikacji, a powtarzana animacja zmieniłaby
/// się z miłego akcentu w opóźnienie.
/// </para>
/// </remarks>
public partial class HomePage : ContentPageBase
{
    private const uint MarkDurationMs = 260;
    private const uint RowDurationMs = 180;

    /// <summary>Opóźnienie między kolejnymi wierszami menu.</summary>
    private const int RowStaggerMs = 45;

    private bool _introPlayed;

    /// <summary>Tworzy ekran startowy.</summary>
    /// <param name="viewModel">ViewModel ekranu, wstrzykiwany przez kontener.</param>
    /// <param name="animations">Zasada animacji — decyduje o animacji wejścia.</param>
    public HomePage(HomeViewModel viewModel, IAnimationPolicy animations)
        : base(animations)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Wspólne przejście wejściowe jest tu wyłączone, bo ekran ma własne — dwa efekty naraz
    /// dałyby treść pojawiającą się dwukrotnie.
    /// </remarks>
    protected override bool UsesEntryTransition => false;

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_introPlayed)
        {
            return;
        }

        _introPlayed = true;

        if (!Animations.AreAnimationsEnabled)
        {
            return;
        }

        _ = PlayIntroAsync();
    }

    /// <summary>
    /// Odtwarza wejście: znak aplikacji, nazwa, kreska i wiersze menu.
    /// </summary>
    /// <remarks>
    /// Stan początkowy jest ustawiany tutaj, a nie w XAML: przy wyłączonych animacjach
    /// i przy powrocie na ekran wszystko musi być od razu widoczne, a niewidoczne elementy
    /// w pliku układu zostawiłyby puste miejsce, gdyby animacja nie ruszyła.
    /// </remarks>
    private async Task PlayIntroAsync()
    {
        LogoMark.Opacity = 0;
        LogoMark.Scale = 0.8;
        AppNameLabel.Opacity = 0;
        Divider.Opacity = 0;

        List<View> rows = [.. MenuStack.Children.OfType<View>()];

        foreach (View row in rows)
        {
            row.Opacity = 0;
            row.TranslationY = 16;
        }

        await Task.WhenAll(
            LogoMark.FadeToAsync(1, MarkDurationMs, Easing.CubicOut),
            LogoMark.ScaleToAsync(1.0, MarkDurationMs, Easing.SpringOut));

        await Task.WhenAll(
            AppNameLabel.FadeToAsync(1, RowDurationMs, Easing.CubicOut),
            Divider.FadeToAsync(1, RowDurationMs, Easing.CubicOut));

        foreach (View row in rows)
        {
            _ = Task.WhenAll(
                row.FadeToAsync(1, RowDurationMs, Easing.CubicOut),
                row.TranslateToAsync(0, 0, RowDurationMs, Easing.CubicOut));

            await Task.Delay(RowStaggerMs);
        }
    }
}
