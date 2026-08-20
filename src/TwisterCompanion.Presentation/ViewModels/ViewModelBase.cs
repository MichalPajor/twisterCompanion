using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Presentation.Abstractions;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Wspólna podstawa wszystkich ViewModeli: stan zajętości, tytuł ekranu
/// i jednolita obsługa błędów.
/// </summary>
/// <remarks>
/// Klasa celowo nie zna żadnego typu MAUI — cała komunikacja z UI idzie przez
/// <see cref="IDialogService"/>. To warunek testowalności warstwy prezentacji.
/// </remarks>
public abstract partial class ViewModelBase : ObservableObject
{
    private readonly ILogger _logger;
    private readonly IDialogService _dialogService;

    /// <summary>Tworzy ViewModel z zależnościami wymaganymi do obsługi błędów.</summary>
    /// <param name="logger">Logger konkretnego ViewModelu.</param>
    /// <param name="dialogService">Serwis komunikatów dla użytkownika.</param>
    /// <param name="localization">Serwis tłumaczeń.</param>
    protected ViewModelBase(
        ILogger logger,
        IDialogService dialogService,
        ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(localization);

        _logger = logger;
        _dialogService = dialogService;
        Localization = localization;
    }

    /// <summary>Tłumaczenia dostępne dla klas pochodnych.</summary>
    protected ILocalizationService Localization { get; }

    /// <summary>Logger tego ekranu.</summary>
    /// <remarks>
    /// Odsłonięty dla ekranów, które pochłaniają awarie zamiast je zgłaszać — na przykład
    /// nieudane pobranie listy głosów syntezatora nie może zamknąć ekranu ustawień, ale
    /// musi zostawić ślad w logu.
    /// </remarks>
    protected ILogger Logger => _logger;

    /// <summary>Komunikaty dla użytkownika dostępne dla klas pochodnych.</summary>
    protected IDialogService Dialogs => _dialogService;

    /// <summary>Pokazuje komunikat informacyjny na podstawie klucza zasobu.</summary>
    /// <param name="messageKey">Klucz zasobu z treścią komunikatu.</param>
    protected Task ShowInfoAsync(string messageKey) => _dialogService.AlertAsync(
        Localization[StringKeys.Common.InfoTitle],
        Localization[messageKey],
        Localization[StringKeys.Common.ButtonOk]);

    /// <summary>
    /// Tytuł ekranu, gdy zależy od danych — na przykład nazwa wybranego trybu gry.
    /// </summary>
    /// <remarks>
    /// Ekrany o stałym tytule <b>nie używają tej właściwości</b>. Ich tytuły są w XAML
    /// przez <c>{loc:Translate}</c>, dzięki czemu odświeżają się po zmianie języka bez
    /// żadnego kodu w ViewModelu.
    /// </remarks>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>Czy trwa operacja blokująca interakcję z ekranem.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    /// <summary>Zaprzeczenie <see cref="IsBusy"/> — wygodne do wiązania z <c>IsEnabled</c>.</summary>
    public bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Wywoływane raz, przy pierwszym pokazaniu ekranu. Błędy są już obsłużone,
    /// więc metoda nigdy nie rzuca wyjątku.
    /// </summary>
    public Task InitializeAsync() => ExecuteSafeAsync(OnInitializeAsync);

    /// <summary>
    /// Miejsce na inicjalizację konkretnego ekranu — wczytanie danych.
    /// </summary>
    protected virtual Task OnInitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Wywoływane przy każdym pokazaniu ekranu — także po powrocie z ekranu potomnego.
    /// </summary>
    /// <remarks>
    /// Tutaj podłącza się subskrypcje zdarzeń serwisów o długim życiu. ViewModele są
    /// tworzone na każde wejście na ekran, a serwisy takie jak silnik gry żyją tyle, co
    /// aplikacja — subskrypcja założona raz i nigdy nie zwolniona trzymałaby w pamięci
    /// każdy ViewModel, jaki kiedykolwiek powstał.
    /// <para>
    /// Para <see cref="OnAppearing"/> i <see cref="OnDisappearing"/> jest symetryczna, więc
    /// subskrypcja żyje dokładnie tyle, ile widoczny ekran. Stan odczytuje się przy każdym
    /// pokazaniu z serwisu, bo to on jest jego źródłem.
    /// </para>
    /// </remarks>
    public virtual void OnAppearing()
    {
    }

    /// <summary>Wywoływane przy każdym ukryciu ekranu — tutaj zwalnia się subskrypcje.</summary>
    public virtual void OnDisappearing()
    {
    }

    /// <summary>
    /// Uruchamia operację, pilnując <see cref="IsBusy"/> i przechwytując wyjątki.
    /// </summary>
    /// <param name="operation">Operacja do wykonania.</param>
    /// <param name="errorTitleKey">
    /// Klucz zasobu z tytułem komunikatu o błędzie. Domyślnie tytuł ogólny.
    /// </param>
    /// <remarks>
    /// Ponowne wywołanie w trakcie trwającej operacji jest ignorowane — chroni przed
    /// dwukrotnym kliknięciem przycisku i przed równoległym uruchomieniem tej samej akcji
    /// z przycisku oraz komendy głosowej (Etap 8).
    /// </remarks>
    protected async Task ExecuteSafeAsync(Func<Task> operation, string? errorTitleKey = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await operation();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Nieobsłużony błąd w {ViewModel}.", GetType().Name);

            await _dialogService.AlertAsync(
                Localization[errorTitleKey ?? StringKeys.Common.ErrorTitle],
                exception.Message,
                Localization[StringKeys.Common.ButtonOk]);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
