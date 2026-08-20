using TwisterCompanion.App.Services;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.App.Views;

/// <summary>
/// Wprowadzenie „Jak grać".
/// </summary>
/// <remarks>
/// Systemowy przycisk cofania jest przechwycony i kończy wprowadzenie tak samo jak strzałka
/// w pasku i „Pomiń". Bez tego gest cofnięcia zamykał ekran bez zapisania, że wprowadzenie
/// zostało pokazane — i wracało przy każdym uruchomieniu do kogoś, kto właśnie je zamknął.
/// </remarks>
public partial class OnboardingPage : ContentPageBase
{
    /// <summary>Tworzy ekran.</summary>
    /// <param name="viewModel">ViewModel ekranu, wstrzykiwany przez kontener.</param>
    /// <param name="animations">Zasada animacji — przejście wejściowe ekranu.</param>
    public OnboardingPage(OnboardingViewModel viewModel, IAnimationPolicy animations)
        : base(animations)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc />
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is not OnboardingViewModel viewModel
            || !viewModel.FinishCommand.CanExecute(parameter: null))
        {
            return base.OnBackButtonPressed();
        }

        // Komenda sama wraca na poprzedni ekran, więc domyślne zamknięcie musi zostać
        // wstrzymane — inaczej ekran zniknąłby dwa razy.
        viewModel.FinishCommand.Execute(parameter: null);

        return true;
    }
}
