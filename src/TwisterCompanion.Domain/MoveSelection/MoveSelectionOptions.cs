namespace TwisterCompanion.Domain.MoveSelection;

/// <summary>
/// Parametry algorytmu inteligentnego losowania.
/// </summary>
/// <remarks>
/// Wydzielone z algorytmu, żeby każdy tryb gry mógł mieć własne nastawy (Etap 9) bez
/// mnożenia klas strategii. Wartości domyślne są dobrane pod tryb Classic z włączoną
/// inteligencją — Hardcore będzie chciał ostrzejszych kar, Kids łagodniejszych.
/// <para>
/// Wszystkie mnożniki są z zakresu 0–1 i działają jak kary: 1,0 oznacza brak kary,
/// 0,0 oznacza całkowite wykluczenie ruchu.
/// </para>
/// </remarks>
public sealed record MoveSelectionOptions
{
    private readonly int _tabooWindowSize = 3;
    private readonly double _tabooWeightMultiplier = 0.05;
    private readonly double _recencyDecay = 0.6;
    private readonly int _maxSameBodyPartStreak = 2;
    private readonly double _sameBodyPartStreakMultiplier = 0.15;
    private readonly int _maxSameColorStreak = 2;
    private readonly double _sameColorStreakMultiplier = 0.3;
    private readonly double _redundantMoveMultiplier = 0.1;
    private readonly int _historyLength = 12;

    /// <summary>
    /// Liczba ostatnich ruchów objętych oknem tabu.
    /// </summary>
    /// <remarks>
    /// Ruch powtórzony w tym oknie dostaje karę <see cref="TabooWeightMultiplier"/>.
    /// Powtórzenie natychmiastowe (odległość 1) jest zakazane niezależnie od tej wartości.
    /// </remarks>
    public int TabooWindowSize
    {
        get => _tabooWindowSize;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _tabooWindowSize = value;
        }
    }

    /// <summary>Kara dla ruchu powtórzonego w oknie tabu.</summary>
    public double TabooWeightMultiplier
    {
        get => _tabooWeightMultiplier;
        init => _tabooWeightMultiplier = ValidateMultiplier(value);
    }

    /// <summary>
    /// Współczynnik wygasania kary za świeżość, stosowany poza oknem tabu.
    /// </summary>
    /// <remarks>
    /// Waga ruchu użytego <c>d</c> losowań temu to <c>1 - decay^d</c>. Im dawniej ruch
    /// wystąpił, tym kara mniejsza — przy odległości rosnącej do nieskończoności zbiega
    /// do braku kary. Wartość 0,0 wyłącza ten mechanizm.
    /// </remarks>
    public double RecencyDecay
    {
        get => _recencyDecay;
        init => _recencyDecay = ValidateMultiplier(value);
    }

    /// <summary>
    /// Ile razy pod rząd ta sama część ciała może wystąpić, zanim zostanie ukarana.
    /// </summary>
    public int MaxSameBodyPartStreak
    {
        get => _maxSameBodyPartStreak;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxSameBodyPartStreak = value;
        }
    }

    /// <summary>Kara za przekroczenie serii tej samej części ciała.</summary>
    public double SameBodyPartStreakMultiplier
    {
        get => _sameBodyPartStreakMultiplier;
        init => _sameBodyPartStreakMultiplier = ValidateMultiplier(value);
    }

    /// <summary>Ile razy pod rząd ten sam kolor może wystąpić, zanim zostanie ukarany.</summary>
    public int MaxSameColorStreak
    {
        get => _maxSameColorStreak;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxSameColorStreak = value;
        }
    }

    /// <summary>Kara za przekroczenie serii tego samego koloru.</summary>
    public double SameColorStreakMultiplier
    {
        get => _sameColorStreakMultiplier;
        init => _sameColorStreakMultiplier = ValidateMultiplier(value);
    }

    /// <summary>
    /// Kara dla ruchu, który niczego nie zmienia — kończyna gracza już stoi na tym kolorze.
    /// </summary>
    /// <remarks>
    /// Bez tego „prawa ręka, czerwony" mogłoby paść graczowi, którego prawa ręka już jest
    /// na czerwonym. Formalnie poprawne, w praktyce zmarnowana tura.
    /// </remarks>
    public double RedundantMoveMultiplier
    {
        get => _redundantMoveMultiplier;
        init => _redundantMoveMultiplier = ValidateMultiplier(value);
    }

    /// <summary>Ile ostatnich ruchów silnik gry ma przechowywać dla algorytmu.</summary>
    public int HistoryLength
    {
        get => _historyLength;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _historyLength = value;
        }
    }

    /// <summary>Nastawy domyślne.</summary>
    public static MoveSelectionOptions Default { get; } = new();

    private static double ValidateMultiplier(double value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0);

        return value;
    }
}
