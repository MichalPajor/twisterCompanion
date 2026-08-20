using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Randomness;

namespace TwisterCompanion.Domain.Tests;

/// <summary>
/// Testy źródeł losowości. Powtarzalność wersji z ziarnem jest fundamentem, na którym
/// oprą się testy algorytmu losowania ruchów (Etap 4).
/// </summary>
public class RandomProviderTests
{
    [Fact]
    public void SeededRandomProvider_TeSamoZiarno_DajeIdentycznaSekwencje()
    {
        int[] pierwsza = Generate(new SeededRandomProvider(seed: 1234));
        int[] druga = Generate(new SeededRandomProvider(seed: 1234));

        Assert.Equal(pierwsza, druga);
    }

    [Fact]
    public void SeededRandomProvider_InneZiarno_DajeInnaSekwencje()
    {
        int[] pierwsza = Generate(new SeededRandomProvider(seed: 1));
        int[] druga = Generate(new SeededRandomProvider(seed: 2));

        Assert.NotEqual(pierwsza, druga);
    }

    [Fact]
    public void SeededRandomProvider_UdostepniaSwojeZiarno() =>
        Assert.Equal(4321, new SeededRandomProvider(seed: 4321).Seed);

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(16)]
    public void Next_ZGranicaGorna_TrzymaSieZakresu(int granica)
    {
        IRandomProvider provider = new SeededRandomProvider(seed: 77);

        for (int i = 0; i < 1_000; i++)
        {
            int wartosc = provider.Next(granica);

            Assert.InRange(wartosc, 0, granica - 1);
        }
    }

    [Fact]
    public void Next_ZDwomaGranicami_TrzymaSieZakresu()
    {
        IRandomProvider provider = new SeededRandomProvider(seed: 88);

        for (int i = 0; i < 1_000; i++)
        {
            int wartosc = provider.Next(10, 20);

            Assert.InRange(wartosc, 10, 19);
        }
    }

    [Fact]
    public void NextDouble_TrzymaSieZakresuZeroDoJeden()
    {
        IRandomProvider provider = new SeededRandomProvider(seed: 99);

        for (int i = 0; i < 1_000; i++)
        {
            double wartosc = provider.NextDouble();

            Assert.True(wartosc >= 0.0 && wartosc < 1.0, $"Wartość {wartosc} poza zakresem.");
        }
    }

    [Fact]
    public void SystemRandomProvider_TrzymaSieZakresow()
    {
        IRandomProvider provider = new SystemRandomProvider();

        for (int i = 0; i < 1_000; i++)
        {
            Assert.InRange(provider.Next(16), 0, 15);
            Assert.InRange(provider.Next(5, 9), 5, 8);
            Assert.True(provider.NextDouble() is >= 0.0 and < 1.0);
        }
    }

    [Fact]
    public void SystemRandomProvider_ZwracaRozneWartosci()
    {
        // Zabezpieczenie przed implementacją, która zawsze zwraca to samo.
        IRandomProvider provider = new SystemRandomProvider();

        HashSet<int> wartosci = [.. Enumerable.Range(0, 200).Select(_ => provider.Next(16))];

        Assert.True(wartosci.Count > 1, "Losowanie zwróciło stale tę samą wartość.");
    }

    private static int[] Generate(IRandomProvider provider) =>
        [.. Enumerable.Range(0, 50).Select(_ => provider.Next(16))];
}
