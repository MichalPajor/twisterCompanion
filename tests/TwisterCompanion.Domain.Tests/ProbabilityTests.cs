using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.ValueObjects;

namespace TwisterCompanion.Domain.Tests;

/// <summary>
/// Testy typu opisującego szansę wystąpienia.
/// </summary>
public class ProbabilityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Konstruktor_WartoscWZakresie_JestPrzyjmowana(int procent) =>
        Assert.Equal(procent, new Probability(procent).Percent);

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Konstruktor_WartoscPozaZakresem_RzucaWyjatek(int procent) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Probability(procent));

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(25, 0.25)]
    [InlineData(100, 1.0)]
    public void AsFraction_PrzeliczaProcentyNaUlamek(int procent, double oczekiwany) =>
        Assert.Equal(oczekiwany, new Probability(procent).AsFraction);

    [Fact]
    public void Szansa_PrzyjmujeCzesciDziesiatneProcenta()
    {
        // Powód istnienia drobniejszej podziałki: w paczce suma szans decyduje o tym, jak
        // często cokolwiek się dzieje, więc przy sześćdziesięciu kilku wydarzeniach jeden
        // procent na każde dawał wydarzenie prawie w każdej turze.
        Probability szansa = new(0.5);

        Assert.Equal(0.5, szansa.Percent);
        Assert.False(szansa.IsNever);
    }

    [Theory]
    [InlineData(0.44, 0.4)]
    [InlineData(0.45, 0.5)]
    [InlineData(0.449999, 0.4)]
    [InlineData(33.333333, 33.3)]
    public void Szansa_ZaokraglaDoJednegoMiejscaPoPrzecinku(double podana, double oczekiwana)
    {
        // Zaokrąglenie jest w konstruktorze, żeby w modelu nie powstała wartość w rodzaju
        // 0,4999999, której nie dałoby się ani pokazać, ani zapisać bez straty.
        Assert.Equal(oczekiwana, new Probability(podana).Percent);
    }

    [Fact]
    public void SumaSzansPaczki_LiczySieZDokladnosciaDoJednejDziesiatej()
    {
        // Sześćdziesiąt trzy wydarzenia po 0,5% to 31,5%, a nie 31,499999999 — sumowanie
        // liczb rzeczywistych bez zaokrąglenia dałoby to drugie i ekran pokazałby bzdurę.
        EventPack paczka = EventPack.Create(
            "Duża",
            [.. Enumerable.Range(0, 63).Select(numer =>
                GameEvent.CreateCustom($"Wydarzenie {numer}", 0.5))]);

        Assert.Equal(31.5, paczka.TotalEnabledChancePercent);
    }

    [Fact]
    public void Never_OznaczaZeroProcent()
    {
        Assert.Equal(0, Probability.Never.Percent);
        Assert.True(Probability.Never.IsNever);
    }

    [Fact]
    public void Always_OznaczaStoProcent()
    {
        Assert.Equal(100, Probability.Always.Percent);
        Assert.False(Probability.Always.IsNever);
    }

    [Fact]
    public void WartoscDomyslna_JestPoprawnaIOznaczaZero()
    {
        // default(Probability) musi być sensowny, bo typ jest strukturą.
        Probability domyslna = default;

        Assert.Equal(0, domyslna.Percent);
        Assert.True(domyslna.IsNever);
    }

    [Fact]
    public void Rownosc_DzialaPoWartosci() =>
        Assert.Equal(new Probability(30), new Probability(30));
}
