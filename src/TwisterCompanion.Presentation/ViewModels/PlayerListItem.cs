using CommunityToolkit.Mvvm.ComponentModel;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Gracz na liście uczestników partii.
/// </summary>
/// <remarks>
/// <see cref="Player"/> jest niezmienny, a wiersz na ekranie musi wiedzieć nie tylko, kto
/// odpadł, ale też czy w tym trybie <b>wolno</b> zgłosić odpadnięcie. To druga informacja,
/// której nie ma w modelu gracza, bo nie jest jego cechą — wynika z trybu gry.
/// </remarks>
public partial class PlayerListItem : ObservableObject
{
    /// <summary>Tworzy wiersz gracza.</summary>
    /// <param name="model">Gracz w postaci domenowej.</param>
    /// <param name="canEliminate">Czy wolno zgłosić odpadnięcie tego gracza.</param>
    public PlayerListItem(Player model, bool canEliminate)
    {
        ArgumentNullException.ThrowIfNull(model);

        Model = model;
        _canEliminate = canEliminate;
    }

    /// <summary>Gracz w postaci domenowej.</summary>
    public Player Model { get; }

    /// <summary>Identyfikator gracza.</summary>
    public Guid Id => Model.Id;

    /// <summary>Imię gracza.</summary>
    public string Name => Model.Name;

    /// <summary>Czy gracz już odpadł.</summary>
    public bool IsEliminated => Model.IsEliminated;

    /// <summary>Czy przy tym graczu ma być przycisk zgłoszenia odpadnięcia.</summary>
    [ObservableProperty]
    private bool _canEliminate;
}
