using System.ComponentModel;
using TwisterCompanion.App.Services;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.App.Views;

/// <summary>
/// Ekran rozgrywki.
/// </summary>
/// <remarks>
/// Ekran nie gaśnie w trakcie partii. Powód jest praktyczny: telefon leży na podłodze,
/// nikt go nie dotyka po kilkanaście sekund, a zgaśnięcie ekranu zabrałoby graczom
/// jedyny widoczny stan gry — i przy sterowaniu głosem nie byłoby jak go przywrócić
/// bez podniesienia telefonu.
/// <para>
/// Animacje są <b>oszczędne i krótkie</b>: pojawienie się nowego koloru i pulsowanie
/// odliczania w ostatnich sekundach. Animacja losowania („ruletka" kolorów) została
/// świadomie odrzucona — opóźniałaby pokazanie wyniku, a odczyt głosowy podaje go
/// natychmiast, więc obraz spóźniałby się za dźwiękiem.
/// </para>
/// </remarks>
public partial class GamePage : ContentPageBase
{
    private const uint RevealDurationMs = 180;
    private const uint PulseDurationMs = 120;

    /// <summary>Największa i najmniejsza sensowna średnica koła z kolorem.</summary>
    /// <remarks>
    /// Górna granica wzrosła z 200 do 280, gdy całe polecenie ruchu przeniosło się na kółko:
    /// nazwa kończyny przestała zajmować własny wiersz pod kołem, więc zwolnione miejsce koło
    /// może wziąć dla siebie. Dolna granica jest granicą czytelności trzech wierszy napisu
    /// w środku — poniżej niej tekst zaczyna się zawijać na siłę.
    /// </remarks>
    private const double CircleDiameterMax = 280;
    private const double CircleDiameterMin = 150;

    /// <summary>
    /// Rozmiary pisma na kółku jako część jego średnicy.
    /// </summary>
    /// <remarks>
    /// Proporcje, nie stałe wartości: napis ma mieścić się w kole niezależnie od tego, ile
    /// miejsca zostało na ekranie. Wcześniej rozmiary były dwiema wartościami przełączanymi
    /// progiem, a przy zmiennej średnicy próg zawsze wypada gdzieś nie tak.
    /// <para>
    /// Po usunięciu nazwy koloru zostały na kółku dwa wiersze zamiast trzech, więc oba mogły
    /// urosnąć. Nazwa kończyny jest teraz największa: to ona niesie treść polecenia, a znak
    /// pod nią ją potwierdza.
    /// </para>
    /// </remarks>
    private const double SymbolFontRatio = 0.16;
    private const double BodyPartFontRatio = 0.15;

    /// <summary>Odstęp napisu od krawędzi koła, jako część średnicy.</summary>
    /// <remarks>
    /// Koło zwęża się ku górze i ku dołowi, więc napis dochodzący do jego krawędzi wyglądałby
    /// na wypchnięty. Margines liczony od średnicy trzyma tę samą proporcję przy każdej
    /// wielkości koła.
    /// </remarks>
    private const double TextMarginRatio = 0.1;

    /// <summary>Jaką część średnicy wolno zająć napisowi w pionie.</summary>
    /// <remarks>
    /// Nie całą: koło jest kołem, więc przy górnej i dolnej krawędzi miejsca na tekst prawie
    /// nie ma. Siedemdziesiąt dwa procent to wysokość pasa, w którym szerokość koła nie spada
    /// poniżej dwóch trzecich średnicy.
    /// </remarks>
    private const double TextHeightRatio = 0.72;

    /// <summary>Najmniejsze zmniejszenie pisma, na jakie pozwalamy przy dopasowaniu.</summary>
    private const double MinimumTextScale = 0.6;

    /// <summary>Tworzy ekran.</summary>
    /// <param name="viewModel">ViewModel ekranu, wstrzykiwany przez kontener.</param>
    /// <param name="animations">Zasada animacji — systemowa i z ustawień aplikacji.</param>
    public GamePage(GameViewModel viewModel, IAnimationPolicy animations)
        : base(animations)
    {
        InitializeComponent();
        BindingContext = viewModel;

        CircleArea.SizeChanged += OnCircleAreaSizeChanged;
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();

        DeviceDisplay.Current.KeepScreenOn = true;

        if (BindingContext is INotifyPropertyChanged viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        if (BindingContext is INotifyPropertyChanged viewModel)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        // Zwolnienie jest równie ważne jak włączenie: bez tego ekran nie gasłby także
        // po wyjściu z rozgrywki, aż do zamknięcia aplikacji.
        DeviceDisplay.Current.KeepScreenOn = false;

        base.OnDisappearing();
    }

    /// <summary>
    /// Dopasowuje koło z kolorem do miejsca, które po nim zostało.
    /// </summary>
    /// <remarks>
    /// Wcześniej średnica brała się z <b>progu wysokości okna</b> (dwie wartości: 200 albo 150)
    /// i to rozwiązanie miało wadę, która wyszła przy dokładaniu banera reklamowego: próg nic
    /// nie wiedział o tym, ile miejsca zabrały elementy pod kołem. Wystarczyło dołożyć pas
    /// wysokości pięćdziesięciu jednostek, żeby ekran zaczął wymagać przewijania — a przewijany
    /// ekran rozgrywki oznacza skład partii schowany pod krawędzią.
    /// <para>
    /// Teraz średnica wynika z <b>faktycznej</b> wysokości wiersza, w którym koło stoi. Wiersz
    /// jest elastyczny („*"), więc dostaje resztę po pasku górnym, imieniu gracza,
    /// zarezerwowanych miejscach i całym dolnym pasie. Każda przyszła zmiana układu — kolejny
    /// pas, wyższy przycisk, inny baner — dopasuje się sama.
    /// <para>
    /// Razem ze średnicą skalują się rozmiary pisma <b>w środku</b> koła, bo od przeniesienia
    /// polecenia ruchu na kółko to one muszą się w nim zmieścić.
    /// </para>
    /// </para>
    /// </remarks>
    private void OnCircleAreaSizeChanged(object? sender, EventArgs e)
    {
        double available = Math.Min(CircleArea.Height, CircleArea.Width);

        if (available <= 0)
        {
            return;
        }

        double diameter = Math.Clamp(available, CircleDiameterMin, CircleDiameterMax);

        // Bez tego warunku zmiana rozmiaru wywoływałaby kolejny pomiar i kolejne zdarzenie.
        if (Math.Abs(ColorCircle.HeightRequest - diameter) < 0.5)
        {
            return;
        }

        ColorCircle.HeightRequest = diameter;
        ColorCircle.WidthRequest = diameter;
        CircleShape.CornerRadius = diameter / 2;

        MoveTextStack.Margin = new Thickness(diameter * TextMarginRatio, 0);

        FitMoveTextToCircle(diameter);
    }

    /// <summary>
    /// Dobiera rozmiary pisma tak, żeby całe polecenie ruchu zmieściło się w kole.
    /// </summary>
    /// <remarks>
    /// Proporcje same nie wystarczają i to jest rzecz policzona, nie przewidziana: „PRAWA
    /// STOPA" przy piśmie 0,125 średnicy potrzebuje więcej szerokości, niż koło daje w tym
    /// miejscu, więc zawija się na dwa wiersze i rośnie w pionie. Do tego dochodzi systemowe
    /// powiększenie czcionki, którego nie znamy z góry, i dłuższe nazwy w innych językach.
    /// <para>
    /// MAUI nie umie zmniejszać pisma samo — <c>Label</c> przy zbyt małym miejscu po prostu
    /// przycina tekst. Mierzymy więc napis i, jeśli nie mieści się w wyznaczonym pasie,
    /// zmniejszamy pismo proporcjonalnie. Kilka podejść wystarcza, bo każde jest bliżej celu;
    /// dolna granica pilnuje, żeby zamiast obciętego napisu nie wyszedł napis nieczytelny.
    /// </para>
    /// </remarks>
    private void FitMoveTextToCircle(double diameter)
    {
        double usableWidth = diameter * (1 - (2 * TextMarginRatio));
        double usableHeight = diameter * TextHeightRatio;
        double scale = 1.0;

        for (int attempt = 0; attempt < 4; attempt++)
        {
            BodyPartSymbol.FontSize = diameter * SymbolFontRatio * scale;
            BodyPartName.FontSize = diameter * BodyPartFontRatio * scale;

            Size measured = ((IView)MoveTextStack).Measure(usableWidth, double.PositiveInfinity);

            // Zero znaczy „jeszcze nie ma czego mierzyć" — pomiar wróci przy następnym
            // przeliczeniu układu, a do tego czasu obowiązują proporcje.
            if (measured.Height <= 0 || measured.Height <= usableHeight || scale <= MinimumTextScale)
            {
                return;
            }

            scale = Math.Max(MinimumTextScale, scale * (usableHeight / measured.Height));
        }
    }

    /// <summary>
    /// Uruchamia krótkie animacje w odpowiedzi na zmiany stanu ekranu.
    /// </summary>
    /// <remarks>
    /// Animacje są tutaj, a nie w ViewModelu, bo dotyczą wyłącznie widoku i nie mają nic
    /// wspólnego z regułami gry. Nasłuch zmian właściwości jest tańszy od wprowadzania
    /// zdarzeń „animuj" do warstwy prezentacji, która o animacjach wiedzieć nie powinna.
    /// </remarks>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not GameViewModel viewModel || !Animations.AreAnimationsEnabled)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(GameViewModel.MoveColorName) when viewModel.HasMove:
                _ = RevealColorAsync();

                break;

            case nameof(GameViewModel.CountdownSeconds) when viewModel.IsCountdownUrgent:
                _ = PulseCountdownAsync();

                break;
        }
    }

    /// <summary>Pokazuje nowy kolor krótkim powiększeniem.</summary>
    private async Task RevealColorAsync()
    {
        ColorCircle.Scale = 0.85;

        await ColorCircle.ScaleToAsync(1.0, RevealDurationMs, Easing.SpringOut);
    }

    /// <summary>Podbija liczbę odliczania w ostatnich sekundach.</summary>
    private async Task PulseCountdownAsync()
    {
        await CountdownValue.ScaleToAsync(1.15, PulseDurationMs, Easing.CubicOut);
        await CountdownValue.ScaleToAsync(1.0, PulseDurationMs, Easing.CubicIn);
    }
}
