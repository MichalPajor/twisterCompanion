using TwisterCompanion.App.Services;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.App.Views;

/// <summary>
/// Wspólna podstawa wszystkich ekranów: przy pierwszym pokazaniu uruchamia
/// inicjalizację ViewModelu, dokłada przejście wejściowe i pilnuje szerokości treści.
/// </summary>
/// <remarks>
/// Inicjalizacja odpala się dokładnie raz, a nie przy każdym powrocie na ekran —
/// inaczej wracanie z ekranu potomnego przeładowywałoby dane bez potrzeby.
/// </remarks>
public abstract class ContentPageBase : ContentPage, IQueryAttributable
{
    /// <summary>Czas trwania przejścia wejściowego.</summary>
    private const uint EntryDurationMs = 160;

    /// <summary>O ile jednostek treść wjeżdża w górę przy pokazaniu ekranu.</summary>
    private const double EntryOffset = 12;

    /// <summary>
    /// Powyżej tej szerokości treść przestaje się rozciągać.
    /// </summary>
    /// <remarks>
    /// Wiersz tekstu szeroki na cały tablet czyta się źle — oko traci początek następnej
    /// linii. Ograniczenie dotyczy wszystkich ekranów, bo wszystkie są listami i formularzami.
    /// </remarks>
    private const double MaxContentWidth = 720;

    private bool _initialized;

    /// <summary>Tworzy ekran bez systemowego paska tytułu.</summary>
    /// <param name="animations">Zasada animacji — decyduje o przejściu wejściowym.</param>
    /// <remarks>
    /// Pasek Shella zabierał u góry kilkadziesiąt jednostek na tytuł, który powtarzał
    /// nagłówek widoczny już w treści, i na strzałkę powrotu, którą każdy ekran ma jako
    /// własny <see cref="PageHeader"/> — czyli na dwie kopie tej samej informacji. Ustawienie
    /// jest tutaj, a nie w każdym pliku XAML, żeby nowy ekran dostawał je bez pamiętania o tym.
    /// </remarks>
    protected ContentPageBase(IAnimationPolicy animations)
    {
        ArgumentNullException.ThrowIfNull(animations);

        Animations = animations;

        Shell.SetNavBarIsVisible(this, false);
    }

    /// <summary>Zasada animacji, dostępna dla ekranów z własnymi efektami.</summary>
    protected IAnimationPolicy Animations { get; }

    /// <summary>
    /// Czy ekran ma być pokazywany z przejściem wejściowym.
    /// </summary>
    /// <remarks>
    /// Ekran z własną animacją wejścia wyłącza wspólne przejście, żeby treść nie pojawiała
    /// się dwa razy — raz przez wspólne rozjaśnienie, raz przez własny efekt.
    /// </remarks>
    protected virtual bool UsesEntryTransition => true;

    /// <inheritdoc />
    /// <remarks>
    /// Wywoływane przez Shella przed pokazaniem ekranu, więc parametry są na miejscu, zanim
    /// ruszy inicjalizacja ViewModelu. Ekrany bez parametrów nic tu nie robią.
    /// </remarks>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is INavigationParameterReceiver receiver)
        {
            receiver.ApplyParameters(new Dictionary<string, object>(query));
        }
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        RunEntryTransition();

        if (BindingContext is not ViewModelBase viewModel)
        {
            return;
        }

        // Subskrypcje zakładamy przy każdym pokazaniu ekranu i zwalniamy przy ukryciu —
        // patrz komentarz w ViewModelBase.OnAppearing.
        viewModel.OnAppearing();

        if (_initialized)
        {
            return;
        }

        _initialized = true;

        // InitializeAsync ma wbudowaną obsługę błędów, więc nie rzuca wyjątków.
        // To istotne w async void — wyjątek nie miałby gdzie zostać przechwycony.
        await viewModel.InitializeAsync();
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is ViewModelBase viewModel)
        {
            viewModel.OnDisappearing();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Ograniczenie szerokości jest liczone tutaj, a nie w każdym pliku XAML: zależy od
    /// rozmiaru okna, a nie od zawartości ekranu. Na telefonie w pionie warunek nigdy nie
    /// zachodzi, więc układ zostaje dokładnie taki, jak był.
    /// </remarks>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (Content is not View content)
        {
            return;
        }

        bool wide = width > MaxContentWidth;

        content.MaximumWidthRequest = wide ? MaxContentWidth : double.PositiveInfinity;
        content.HorizontalOptions = wide ? LayoutOptions.Center : LayoutOptions.Fill;
    }

    /// <summary>
    /// Rozjaśnia treść ekranu przy wejściu.
    /// </summary>
    /// <remarks>
    /// Krótkie przejście zamiast przeskoku — 160 ms, czyli mniej niż czas reakcji na
    /// naciśnięcie, więc nawigacja nie robi się od niego ospała. Przy wyłączonych animacjach
    /// treść jest po prostu ustawiana w miejscu docelowym; nie wolno jej zostawić
    /// przezroczystej.
    /// </remarks>
    private void RunEntryTransition()
    {
        if (Content is not View content)
        {
            return;
        }

        if (!UsesEntryTransition || !Animations.AreAnimationsEnabled)
        {
            content.Opacity = 1;
            content.TranslationY = 0;

            return;
        }

        content.Opacity = 0;
        content.TranslationY = EntryOffset;

        _ = AnimateEntryAsync(content);
    }

    private static async Task AnimateEntryAsync(View content)
    {
        await Task.WhenAll(
            content.FadeToAsync(1, EntryDurationMs, Easing.CubicOut),
            content.TranslateToAsync(0, 0, EntryDurationMs, Easing.CubicOut));
    }
}
