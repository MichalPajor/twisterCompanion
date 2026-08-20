using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.GameModes;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Domain.GameModes;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Navigation;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Ekran wyboru trybu gry.
/// </summary>
/// <remarks>
/// Wybór trybu zapisuje się od razu, bez przycisku zatwierdzania: tryb obowiązuje od
/// następnej partii, a partia właśnie trwająca zachowuje swoje zasady do końca.
/// </remarks>
public partial class GameModesViewModel : NavigableViewModelBase
{
    private readonly IGameModeService _gameModes;

    /// <summary>Tworzy ViewModel ekranu trybów gry.</summary>
    /// <param name="navigation">Serwis nawigacji.</param>
    /// <param name="gameModes">Serwis trybów gry.</param>
    /// <param name="logger">Logger tego ViewModelu.</param>
    /// <param name="dialogService">Serwis komunikatów dla użytkownika.</param>
    /// <param name="localization">Serwis tłumaczeń.</param>
    public GameModesViewModel(
        INavigationService navigation,
        IGameModeService gameModes,
        ILogger<GameModesViewModel> logger,
        IDialogService dialogService,
        ILocalizationService localization)
        : base(navigation, logger, dialogService, localization)
    {
        ArgumentNullException.ThrowIfNull(gameModes);

        _gameModes = gameModes;
    }

    /// <summary>Tryby dostępne do wyboru.</summary>
    public ObservableCollection<GameModeListItem> Modes { get; } = [];

    /// <summary>Czy katalog trybów jest pusty.</summary>
    [ObservableProperty]
    private bool _isEmpty;

    /// <inheritdoc />
    /// <remarks>
    /// Wczytanie <b>bez</b> <c>ExecuteSafeAsync</c>: inicjalizacja ekranu jest już nim
    /// otoczona, a wspólna flaga zajętości sprawiłaby, że zagnieżdżone wywołanie zostałoby
    /// pominięte i lista zostałaby pusta.
    /// </remarks>
    protected override async Task OnInitializeAsync()
    {
        GameModeDefinition active = await _gameModes.GetActiveAsync();

        Modes.Clear();

        foreach (GameModeDefinition mode in _gameModes.GetAvailable())
        {
            Modes.Add(new GameModeListItem(
                mode,
                Localization[mode.NameKey],
                mode.DescriptionKey is null ? string.Empty : Localization[mode.DescriptionKey],
                mode.Key == active.Key));
        }

        IsEmpty = Modes.Count == 0;
    }

    /// <summary>Ustawia tryb jako obowiązujący.</summary>
    /// <param name="item">Wybrana karta trybu.</param>
    [RelayCommand]
    private Task SelectModeAsync(GameModeListItem item) => ExecuteSafeAsync(async () =>
    {
        ArgumentNullException.ThrowIfNull(item);

        await _gameModes.SetActiveAsync(item.Key);

        // Zaznaczenie przestawiamy na miejscu, zamiast przebudowywać listę: karty mają
        // zostać na swoich pozycjach, żeby wybór nie przeskoczył pod palcem.
        foreach (GameModeListItem mode in Modes)
        {
            mode.IsActive = mode.Key == item.Key;
        }
    });

    /// <summary>Przechodzi do opisu zasad wybranego trybu.</summary>
    /// <param name="item">Karta trybu, którego zasady mają być pokazane.</param>
    [RelayCommand]
    private Task GoToRulesAsync(GameModeListItem item) => ExecuteSafeAsync(() =>
    {
        ArgumentNullException.ThrowIfNull(item);

        // Zasady dotyczą trybu wskazanego na ekranie, a nie trybu wybranego: gracz ma prawo
        // przeczytać, czym jest Hardcore, przed decyzją o grze w nim.
        return Navigation.GoToAsync(
            Routes.Rules,
            new Dictionary<string, object> { [Routes.Parameters.GameModeKey] = item.Key });
    });
}
