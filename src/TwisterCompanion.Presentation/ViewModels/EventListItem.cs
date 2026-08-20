using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.ValueObjects;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Wydarzenie w postaci nadającej się do edycji na ekranie.
/// </summary>
/// <remarks>
/// <see cref="GameEvent"/> jest niezmienny, a przełącznik i pole procentów na ekranie
/// potrzebują właściwości zapisywalnych z powiadamianiem o zmianie. Ten typ jest tym
/// pomostem: zbiera zmiany z interfejsu i zgłasza je jedną akcją, a model domenowy
/// pozostaje niezmienny.
/// <para>
/// Szansa jest wystawiona jako <b>tekst</b>, żeby dało się ją wpisać z klawiatury.
/// Wartość jest przy tym pilnowana: tekst niebędący liczbą jest ignorowany, a liczba
/// poza zakresem przycinana do 0–100 i poprawiana w polu, żeby użytkownik zobaczył,
/// co faktycznie zostało ustawione.
/// </para>
/// </remarks>
public partial class EventListItem : ObservableObject
{
    private readonly Action<EventListItem>? _onChanged;
    private bool _suppressNotifications;

    /// <summary>Tworzy element listy dla wydarzenia.</summary>
    /// <param name="model">Wydarzenie w postaci domenowej.</param>
    /// <param name="displayName">Nazwa w aktualnym języku.</param>
    /// <param name="isEditable">Czy wydarzenie wolno zmieniać.</param>
    /// <param name="onChanged">Wywoływane po zmianie przez użytkownika.</param>
    public EventListItem(
        GameEvent model,
        string displayName,
        bool isEditable,
        Action<EventListItem>? onChanged = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        Model = model;
        DisplayName = displayName;
        IsEditable = isEditable;
        _onChanged = onChanged;

        _suppressNotifications = true;
        IsEnabled = model.IsEnabled;
        ChancePercent = model.Chance.Percent;
        ChanceText = FormatPercent(model.Chance.Percent);
        _suppressNotifications = false;
    }

    /// <summary>Wydarzenie w postaci domenowej, z uwzględnieniem zmian z ekranu.</summary>
    public GameEvent Model { get; private set; }

    /// <summary>Nazwa wydarzenia w aktualnym języku.</summary>
    public string DisplayName { get; }

    /// <summary>Czy wydarzenie wolno zmieniać — paczki wbudowane są tylko do odczytu.</summary>
    public bool IsEditable { get; }

    /// <summary>Czy wydarzenie bierze udział w losowaniu.</summary>
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>Szansa wystąpienia w procentach, z dokładnością do jednej dziesiątej.</summary>
    [ObservableProperty]
    private double _chancePercent;

    /// <summary>Szansa w postaci tekstu — wpisywana ręcznie przez użytkownika.</summary>
    [ObservableProperty]
    private string _chanceText = string.Empty;

    /// <summary>Krok zmiany szansy przyciskami.</summary>
    public const int ChanceStep = 5;

    /// <summary>Zwiększa szansę o krok.</summary>
    /// <remarks>
    /// Przyciski i ręczne wpisywanie działają obok siebie: przyciski są szybsze przy
    /// typowych korektach, wpisywanie pozwala ustawić dowolną wartość.
    /// </remarks>
    [RelayCommand]
    private void IncreaseChance() => ShiftChance(ChanceStep);

    /// <summary>Zmniejsza szansę o krok.</summary>
    [RelayCommand]
    private void DecreaseChance() => ShiftChance(-ChanceStep);

    /// <summary>
    /// Przesuwa szansę o podaną liczbę punktów, zaokrąglając do wielokrotności kroku.
    /// </summary>
    /// <remarks>
    /// Zaokrąglenie jest celowe: po ręcznym wpisaniu 37 pierwszy plus daje 40, a nie 42.
    /// Przyciski mają prowadzić do „okrągłych" wartości.
    /// </remarks>
    private void ShiftChance(int delta)
    {
        if (!IsEditable)
        {
            return;
        }

        // Zaokrąglenie idzie do wielokrotności kroku, więc wartość wpisana ręcznie — na
        // przykład 0,5 — po naciśnięciu plusa wskakuje na 5. Tak ma być: przyciski są od
        // szybkich, okrągłych korekt, a drobne wartości ustawia się wpisaniem.
        double rounded = delta > 0
            ? (Math.Floor(ChancePercent / ChanceStep) * ChanceStep) + ChanceStep
            : (Math.Ceiling(ChancePercent / ChanceStep) * ChanceStep) - ChanceStep;

        SetChanceTextQuietly(Math.Clamp(rounded, Probability.MinPercent, Probability.MaxPercent));
        OnChanceTextChanged(ChanceText);
    }

    partial void OnIsEnabledChanged(bool value) =>
        ApplyChange(current => current with { IsEnabled = value });

    partial void OnChanceTextChanged(string value)
    {
        if (_suppressNotifications)
        {
            return;
        }

        // Wpis niebędący liczbą — na przykład pusty, w trakcie kasowania — zostawiamy
        // bez reakcji. Użytkownik jest w środku edycji.
        if (!PercentText.TryParse(value, out double parsed))
        {
            return;
        }

        double clamped = Math.Clamp(parsed, Probability.MinPercent, Probability.MaxPercent);

        if (clamped != parsed)
        {
            // Poprawiamy pole, żeby użytkownik zobaczył, co naprawdę zostało ustawione.
            SetChanceTextQuietly(clamped);
        }

        if (Math.Abs(ChancePercent - clamped) < Probability.Step / 2)
        {
            return;
        }

        ChancePercent = clamped;
        ApplyChange(current => current with { Chance = new Probability(clamped) });
    }

    /// <summary>
    /// Przenosi zmianę z ekranu do modelu i zgłasza ją właścicielowi listy.
    /// </summary>
    /// <remarks>
    /// Zmiany wprowadzone przy tworzeniu obiektu są pomijane — inaczej samo wypełnienie
    /// listy wywołałoby zapis wszystkich wydarzeń.
    /// </remarks>
    private void ApplyChange(Func<GameEvent, GameEvent> change)
    {
        if (_suppressNotifications || !IsEditable)
        {
            return;
        }

        Model = change(Model);
        _onChanged?.Invoke(this);
    }

    private void SetChanceTextQuietly(double percent)
    {
        _suppressNotifications = true;
        ChanceText = FormatPercent(percent);
        _suppressNotifications = false;
    }

    private static string FormatPercent(double percent) => PercentText.Format(percent);
}
