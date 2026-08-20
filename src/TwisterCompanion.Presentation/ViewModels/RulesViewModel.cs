using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.GameModes;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Domain.GameModes;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Navigation;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Ekran zasad trybu gry.
/// </summary>
/// <remarks>
/// Tryb przychodzi parametrem nawigacji, a nie z ustawień: gracz czyta zasady, żeby
/// <b>zdecydować</b> o wyborze trybu, więc ekran musi umieć pokazać także tryb niewybrany.
/// Bez parametru pokazuje tryb obowiązujący.
/// </remarks>
public partial class RulesViewModel : NavigableViewModelBase, INavigationParameterReceiver
{
    private readonly IGameModeService _gameModes;

    private string? _requestedModeKey;

    /// <summary>Tworzy ViewModel ekranu.</summary>
    /// <param name="navigation">Serwis nawigacji.</param>
    /// <param name="gameModes">Serwis trybów gry.</param>
    /// <param name="logger">Logger tego ViewModelu.</param>
    /// <param name="dialogService">Serwis komunikatów dla użytkownika.</param>
    /// <param name="localization">Serwis tłumaczeń.</param>
    public RulesViewModel(
        INavigationService navigation,
        IGameModeService gameModes,
        ILogger<RulesViewModel> logger,
        IDialogService dialogService,
        ILocalizationService localization)
        : base(navigation, logger, dialogService, localization)
    {
        ArgumentNullException.ThrowIfNull(gameModes);

        _gameModes = gameModes;
    }

    /// <summary>Nazwa trybu, którego zasady są pokazane.</summary>
    [ObservableProperty]
    private string _modeName = string.Empty;

    /// <summary>Opis zasad trybu.</summary>
    [ObservableProperty]
    private string _rulesText = string.Empty;

    /// <inheritdoc />
    public void ApplyParameters(IReadOnlyDictionary<string, object> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        _requestedModeKey = parameters.TryGetValue(Routes.Parameters.GameModeKey, out object? value)
            ? value as string
            : null;
    }

    /// <inheritdoc />
    protected override async Task OnInitializeAsync()
    {
        GameModeDefinition mode = (_requestedModeKey is null ? null : _gameModes.Find(_requestedModeKey))
            ?? await _gameModes.GetActiveAsync();

        ModeName = Localization[mode.NameKey];

        RulesText = mode.RulesKey is null
            ? Localization[StringKeys.Rules.LabelMissing]
            : Localization[mode.RulesKey];
    }
}
