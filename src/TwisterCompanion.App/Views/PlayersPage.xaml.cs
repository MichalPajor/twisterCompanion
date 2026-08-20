using TwisterCompanion.App.Services;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.App.Views;

/// <summary>Ekran składu graczy: dodawanie, zmiana imienia w miejscu, kolejność tur, usuwanie.</summary>
public partial class PlayersPage : ContentPageBase
{
    /// <summary>Tworzy ekran.</summary>
    /// <param name="viewModel">ViewModel ekranu, wstrzykiwany przez kontener.</param>
    /// <param name="animations">Zasada animacji — przejście wejściowe ekranu.</param>
    public PlayersPage(PlayersViewModel viewModel, IAnimationPolicy animations)
        : base(animations)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
