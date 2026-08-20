using TwisterCompanion.App.Services;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.App.Views;

/// <summary>Ekran ustawień.</summary>
public partial class SettingsPage : ContentPageBase
{
    /// <summary>Tworzy ekran.</summary>
    /// <param name="viewModel">ViewModel ekranu, wstrzykiwany przez kontener.</param>
    /// <param name="animations">Zasada animacji — przejście wejściowe ekranu.</param>
    public SettingsPage(SettingsViewModel viewModel, IAnimationPolicy animations)
        : base(animations)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
