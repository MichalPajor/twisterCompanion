using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Presentation.Abstractions;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Wprowadzenie „Jak grać" — trzy kroki o tym, czego nie widać po samym ekranie.
/// </summary>
/// <remarks>
/// Kroki są <b>przełączane, nie przewijane w karuzeli</b>. Powód jest ten sam, który wywrócił
/// aplikację przy wierszu pigułek: układy MAUI wypełniane wiązaniem bywają zawodne, a tutaj
/// wystarczy pokazać jeden z trzech tekstów i zmienić liczbę. Przesunięcie palcem nadal działa
/// — jest zwykłym gestem na treści, a nie osobnym układem.
/// <para>
/// Wprowadzenie jest pokazywane raz, przy pierwszym uruchomieniu, i zawsze dostępne
/// z ekranu startowego pod „Jak grać". Jedno i drugie kończy się tym samym: zapisaniem
/// w ustawieniach, że gracz je widział.
/// </para>
/// </remarks>
public partial class OnboardingViewModel : NavigableViewModelBase
{
    private readonly ISettingsService _settingsService;

    /// <summary>Tworzy ViewModel wprowadzenia.</summary>
    /// <param name="navigation">Serwis nawigacji.</param>
    /// <param name="settingsService">Ustawienia — zapis informacji, że wprowadzenie widziano.</param>
    /// <param name="logger">Logger tego ViewModelu.</param>
    /// <param name="dialogService">Serwis komunikatów dla użytkownika.</param>
    /// <param name="localization">Serwis tłumaczeń.</param>
    public OnboardingViewModel(
        INavigationService navigation,
        ISettingsService settingsService,
        ILogger<OnboardingViewModel> logger,
        IDialogService dialogService,
        ILocalizationService localization)
        : base(navigation, logger, dialogService, localization)
    {
        ArgumentNullException.ThrowIfNull(settingsService);

        _settingsService = settingsService;

        Steps =
        [
            // Znaki idą za treścią kroków: powitanie ma znak tarczy losującej (to nią jest ta
            // aplikacja), przygotowanie — listy do przejścia, a rozgrywka — trójkąta startu.
            new OnboardingStep(
                "◉",
                localization[StringKeys.Onboarding.Step1Title],
                localization[StringKeys.Onboarding.Step1Body]),
            new OnboardingStep(
                "☰",
                localization[StringKeys.Onboarding.Step2Title],
                localization[StringKeys.Onboarding.Step2Body]),
            new OnboardingStep(
                "▶",
                localization[StringKeys.Onboarding.Step3Title],
                localization[StringKeys.Onboarding.Step3Body]),
        ];
    }

    /// <summary>Kroki wprowadzenia, w kolejności.</summary>
    public IReadOnlyList<OnboardingStep> Steps { get; }

    /// <summary>Numer pokazywanego kroku, liczony od zera.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Current))]
    [NotifyPropertyChangedFor(nameof(IsFirstStep))]
    [NotifyPropertyChangedFor(nameof(IsNotFirstStep))]
    [NotifyPropertyChangedFor(nameof(IsLastStep))]
    [NotifyPropertyChangedFor(nameof(IsNotLastStep))]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private int _stepIndex;

    /// <summary>Pokazywany krok.</summary>
    public OnboardingStep Current => Steps[StepIndex];

    /// <summary>Czy to pierwszy krok.</summary>
    public bool IsFirstStep => StepIndex == 0;

    /// <summary>Czy jest do czego wracać.</summary>
    public bool IsNotFirstStep => !IsFirstStep;

    /// <summary>Czy to ostatni krok.</summary>
    public bool IsLastStep => StepIndex == Steps.Count - 1;

    /// <summary>Czy są jeszcze kroki przed nami.</summary>
    public bool IsNotLastStep => !IsLastStep;

    /// <summary>Informacja „krok z ilu" dla czytnika ekranu i dla gracza.</summary>
    public string ProgressText => Localization.GetFormattedString(
        StringKeys.Onboarding.ProgressFormat,
        StringCatalog.Ui,
        StepIndex + 1,
        Steps.Count);

    /// <summary>Przechodzi do następnego kroku, a z ostatniego kończy wprowadzenie.</summary>
    [RelayCommand]
    private Task NextAsync()
    {
        if (IsLastStep)
        {
            return FinishAsync();
        }

        StepIndex++;

        return Task.CompletedTask;
    }

    /// <summary>Wraca do poprzedniego kroku.</summary>
    [RelayCommand]
    private void Back()
    {
        if (!IsFirstStep)
        {
            StepIndex--;
        }
    }

    /// <summary>
    /// Kończy wprowadzenie i wraca na ekran startowy.
    /// </summary>
    /// <remarks>
    /// Zapis „widziano" idzie <b>przed</b> nawigacją i jest osobno zabezpieczony: gdyby zapis
    /// zawiódł, gracz i tak ma wyjść z ekranu — inaczej zostałby w nim uwięziony przez awarię
    /// pliku ustawień.
    /// </remarks>
    [RelayCommand]
    private Task FinishAsync() => ExecuteSafeAsync(async () =>
    {
        try
        {
            await _settingsService.UpdateAsync(settings => settings with { HasSeenOnboarding = true });
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Nie udało się zapisać informacji o pokazanym wprowadzeniu.");
        }

        await Navigation.GoBackAsync();
    });
}
