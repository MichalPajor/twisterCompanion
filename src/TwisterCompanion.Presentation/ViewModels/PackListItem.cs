using CommunityToolkit.Mvvm.ComponentModel;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Paczka wydarzeń w postaci gotowej do pokazania na liście.
/// </summary>
/// <remarks>
/// Typ istnieje, żeby nazwa paczki wbudowanej mogła być przetłumaczona. Model domenowy
/// trzyma <see cref="EventPack.NameKey"/>, a nie gotowy napis — rozwiązanie klucza wymaga
/// serwisu tłumaczeń, do którego widok nie ma dostępu.
/// <para>
/// Wiersz zna <b>dwa</b> stany i to nie jest to samo: paczka <see cref="IsActive"/> jest
/// używana w rozgrywce, a paczka <see cref="IsSelected"/> to ta, na której działają przyciski
/// pod listą. Ekran maluje je różnie, bo pomylenie ich to pomylenie „gram tym" z „patrzę na to".
/// </para>
/// <para>
/// Obserwowalny obiekt, a nie rekord: zmiana zawartości paczki albo jej stanu odświeża wiersz
/// w miejscu. Wcześniej wiersz był podmieniany na nowy, a podmiana zaznaczonego wiersza
/// pociągała za sobą przebudowę listy wydarzeń — czyli kontrolkę podmienianą pod palcem.
/// </para>
/// </remarks>
public partial class PackListItem : ObservableObject
{
    /// <summary>Tworzy wiersz paczki.</summary>
    /// <param name="model">Paczka w postaci domenowej.</param>
    /// <param name="displayName">Nazwa w aktualnym języku.</param>
    /// <param name="isActive">Czy paczka jest używana w rozgrywce.</param>
    public PackListItem(EventPack model, string displayName, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(model);

        _model = model;
        DisplayName = displayName;
        _isActive = isActive;
    }

    /// <summary>Nazwa w aktualnym języku.</summary>
    public string DisplayName { get; }

    /// <summary>Paczka w postaci domenowej.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBuiltIn))]
    [NotifyPropertyChangedFor(nameof(EventCount))]
    private EventPack _model;

    /// <summary>Czy paczka jest używana w rozgrywce.</summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>Czy to na tej paczce działają przyciski pod listą.</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Czy paczka pochodzi z aplikacji i jest tylko do odczytu.</summary>
    public bool IsBuiltIn => Model.IsBuiltIn;

    /// <summary>Liczba wydarzeń w paczce.</summary>
    public int EventCount => Model.Events.Count;

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}
