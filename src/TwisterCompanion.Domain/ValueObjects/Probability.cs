using System.Globalization;

namespace TwisterCompanion.Domain.ValueObjects;

/// <summary>
/// Szansa wystąpienia wyrażona w procentach z dokładnością do jednej dziesiątej.
/// </summary>
/// <remarks>
/// Typ istnieje, żeby niepoprawna wartość nie mogła powstać. Gdyby szansa była zwykłym
/// <see cref="double"/>, każde miejsce ją przyjmujące musiałoby walidować zakres — a któreś
/// z nich w końcu by o tym zapomniało.
/// <para>
/// Wartości były najpierw <b>całkowite</b> i to okazało się za grubym sitem. W paczce
/// wydarzeń suma szans decyduje o tym, jak często cokolwiek się dzieje, więc przy
/// siedemdziesięciu wydarzeniach najmniejsza niezerowa wartość — jeden procent — dawała
/// siedemdziesiąt procent szans na wydarzenie w każdej turze. Duże zestawy potrzebują
/// drobniejszej podziałki, stąd jedna dziesiąta procenta.
/// </para>
/// <para>
/// Podziałka jest wymuszona w konstruktorze zaokrągleniem: dzięki temu w modelu nie powstaje
/// wartość w rodzaju 0,4999999, której nie dałoby się ani pokazać, ani zapisać bez straty.
/// </para>
/// </remarks>
public readonly record struct Probability
{
    /// <summary>Najmniejsza dopuszczalna wartość procentowa.</summary>
    public const double MinPercent = 0.0;

    /// <summary>Największa dopuszczalna wartość procentowa.</summary>
    public const double MaxPercent = 100.0;

    /// <summary>Najmniejszy krok podziałki — jedna dziesiąta procenta.</summary>
    public const double Step = 0.1;

    /// <summary>Tworzy szansę o podanej wartości procentowej.</summary>
    /// <param name="percent">Wartość z zakresu 0–100.</param>
    /// <exception cref="ArgumentOutOfRangeException">Gdy wartość jest poza zakresem.</exception>
    public Probability(double percent)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(percent, MinPercent);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percent, MaxPercent);

        Percent = Math.Round(percent, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>Wartość procentowa.</summary>
    public double Percent { get; }

    /// <summary>Ta sama wartość jako ułamek z zakresu 0,0–1,0.</summary>
    public double AsFraction => Percent / 100d;

    /// <summary>Zdarzenie nigdy nie wystąpi.</summary>
    public static Probability Never => new(MinPercent);

    /// <summary>Zdarzenie wystąpi zawsze.</summary>
    public static Probability Always => new(MaxPercent);

    /// <summary>Czy szansa jest zerowa.</summary>
    public bool IsNever => Percent <= MinPercent;

    /// <inheritdoc />
    /// <remarks>
    /// Zapis niezmienny kulturą, bo to tekst diagnostyczny: warstwa domeny nie wie, w jakim
    /// języku pracuje aplikacja, a wpis w logu ma być czytelny niezależnie od tego.
    /// </remarks>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Percent:0.#}%");
}
