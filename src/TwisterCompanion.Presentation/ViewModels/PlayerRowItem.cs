using CommunityToolkit.Mvvm.ComponentModel;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Wiersz gracza na ekranie składu — razem ze stanem edycji imienia.
/// </summary>
/// <remarks>
/// Stan „ten wiersz jest właśnie edytowany" należy do wiersza, a nie do ekranu: edycja
/// dotyczy jednego gracza, a nie całej listy, więc trzymanie jej w ViewModelu ekranu
/// wymagałoby porównywania identyfikatorów w każdym powiązaniu.
/// <para>
/// Sam <see cref="Player"/> jest niezmienny, więc zmiana imienia powstaje jako nowy egzemplarz
/// z tym samym identyfikatorem — dzięki temu partia w toku i historia odpadnięć nadal wskazują
/// tego samego gracza.
/// </para>
/// </remarks>
public partial class PlayerRowItem : ObservableObject
{
    /// <summary>Tworzy wiersz na podstawie gracza.</summary>
    /// <param name="model">Gracz w postaci domenowej.</param>
    public PlayerRowItem(Player model)
    {
        ArgumentNullException.ThrowIfNull(model);

        Model = model;
        _editedName = model.Name;
    }

    /// <summary>Gracz w postaci domenowej.</summary>
    public Player Model { get; private set; }

    /// <summary>Identyfikator gracza.</summary>
    public Guid Id => Model.Id;

    /// <summary>Imię gracza.</summary>
    public string Name => Model.Name;

    /// <summary>Czy wiersz jest w trybie edycji imienia.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEditing))]
    private bool _isEditing;

    /// <summary>Imię wpisywane w trakcie edycji.</summary>
    [ObservableProperty]
    private string _editedName;

    /// <summary>Czy wiersz da się przenieść wyżej.</summary>
    [ObservableProperty]
    private bool _canMoveUp;

    /// <summary>Czy wiersz da się przenieść niżej.</summary>
    [ObservableProperty]
    private bool _canMoveDown;

    /// <summary>
    /// Czy wiersz pokazuje imię, a nie pole edycji.
    /// </summary>
    /// <remarks>
    /// Zaprzeczenie jako właściwość, a nie konwerter w XAML: aplikacja nie używa żadnego
    /// konwertera, a chodzi o widoczność dwóch elementów.
    /// </remarks>
    public bool IsNotEditing => !IsEditing;

    /// <summary>Podmienia gracza po zapisanej zmianie.</summary>
    /// <param name="model">Nowa postać gracza.</param>
    internal void Apply(Player model)
    {
        ArgumentNullException.ThrowIfNull(model);

        Model = model;
        EditedName = model.Name;

        OnPropertyChanged(nameof(Name));
    }

    /// <summary>Przywraca imię z modelu i wychodzi z edycji.</summary>
    internal void ResetEdit()
    {
        EditedName = Name;
        IsEditing = false;
    }
}
