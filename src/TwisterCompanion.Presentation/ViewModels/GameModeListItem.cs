using CommunityToolkit.Mvvm.ComponentModel;
using TwisterCompanion.Domain.GameModes;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Tryb gry w postaci nadającej się na kartę na ekranie wyboru.
/// </summary>
/// <remarks>
/// <see cref="GameModeDefinition"/> zna tylko klucze zasobów — nazwa i opis są tu już
/// przetłumaczone, bo powiązania XAML nie mają dostępu do serwisu tłumaczeń.
/// </remarks>
public partial class GameModeListItem : ObservableObject
{
    /// <summary>Tworzy kartę trybu.</summary>
    /// <param name="model">Tryb w postaci domenowej.</param>
    /// <param name="name">Nazwa w aktualnym języku.</param>
    /// <param name="description">Krótki opis w aktualnym języku.</param>
    /// <param name="isActive">Czy to tryb aktualnie wybrany.</param>
    public GameModeListItem(GameModeDefinition model, string name, string description, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(model);

        Model = model;
        Name = name;
        Description = description;
        _isActive = isActive;
    }

    /// <summary>Tryb w postaci domenowej.</summary>
    public GameModeDefinition Model { get; }

    /// <summary>Klucz trybu.</summary>
    public string Key => Model.Key;

    /// <summary>Nazwa trybu w aktualnym języku.</summary>
    public string Name { get; }

    /// <summary>Krótki opis trybu w aktualnym języku.</summary>
    public string Description { get; }

    /// <summary>Czy to tryb aktualnie wybrany.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotActive))]
    private bool _isActive;

    /// <summary>
    /// Czy tryb nie jest wybrany.
    /// </summary>
    /// <remarks>
    /// Zaprzeczenie jako właściwość, a nie konwerter w XAML: aplikacja nie używa żadnego
    /// konwertera, a chodzi o wyłączenie jednego przycisku.
    /// </remarks>
    public bool IsNotActive => !IsActive;
}
