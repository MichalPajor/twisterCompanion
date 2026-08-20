using System.Globalization;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Application.VoiceControl;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy dopasowania rozpoznanego tekstu do komend.
/// </summary>
/// <remarks>
/// Testy chodzą po prawdziwych frazach z plików zasobów, a nie po podstawionych — połowa
/// ryzyka siedzi właśnie w tym, jakie synonimy tam wpisaliśmy.
/// </remarks>
public class VoiceCommandParserTests
{
    [Theory]
    [InlineData("dalej")]
    [InlineData("Dalej")]
    [InlineData("dalej.")]
    [InlineData("następny")]
    [InlineData("nastepny")]
    [InlineData("dawaj")]
    public void FrazyKomendyDalej_SaRozpoznawane(string wypowiedz)
    {
        using GameTestHarness harness = new(useResourceLocalization: true);

        Assert.True(harness.VoiceCommandParser.TryParse(wypowiedz, out VoiceCommandType command));
        Assert.Equal(VoiceCommandType.Next, command);
    }

    [Theory]
    [InlineData("powtórz", VoiceCommandType.Repeat)]
    [InlineData("jeszcze raz", VoiceCommandType.Repeat)]
    [InlineData("pauza", VoiceCommandType.Pause)]
    [InlineData("wznów", VoiceCommandType.Resume)]
    public void PozostaleKomendy_SaRozpoznawane(string wypowiedz, VoiceCommandType oczekiwana)
    {
        using GameTestHarness harness = new(useResourceLocalization: true);

        Assert.True(harness.VoiceCommandParser.TryParse(wypowiedz, out VoiceCommandType command));
        Assert.Equal(oczekiwana, command);
    }

    [Theory]
    [InlineData("no dalej")]
    [InlineData("okej dalej proszę")]
    [InlineData("dalej dalej")]
    public void KomendaWSrodkuZdania_JestRozpoznawana(string wypowiedz)
    {
        // Rozpoznawanie mowy zwraca całe zdania, nie pojedyncze słowa. Porównanie całości
        // zamiast szukania frazy w środku nie zadziałałoby ani razu w prawdziwej grze.
        using GameTestHarness harness = new(useResourceLocalization: true);

        Assert.True(harness.VoiceCommandParser.TryParse(wypowiedz, out VoiceCommandType command));
        Assert.Equal(VoiceCommandType.Next, command);
    }

    [Theory]
    [InlineData("powtoz")]
    [InlineData("powtorzy")]
    [InlineData("nastepnyy")]
    public void PrzekreconaFraza_JestRozpoznawanaZTolerancja(string wypowiedz)
    {
        // Rozpoznawanie gubi końcówki i pojedyncze dźwięki. Tolerancja rośnie z długością
        // frazy, bo przy dłuższym słowie jedna litera nie zmienia znaczenia.
        using GameTestHarness harness = new(useResourceLocalization: true);

        Assert.True(harness.VoiceCommandParser.TryParse(wypowiedz, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("prawa ręka czerwony")]
    [InlineData("kuba wygrał tę partię")]
    [InlineData("gracz odpadł")]
    [InlineData("nie wiem co dalej robić z tym")]
    public void TekstBezKomendy_NieJestDopasowywany(string wypowiedz)
    {
        // „nie wiem co dalej robić" zawiera słowo „dalej" — i ma zadziałać, bo gracz
        // je wypowiedział. Test pilnuje pozostałych przypadków, w tym „gracz odpadł":
        // ta komenda została usunięta, bo nie mówi, KTÓRY gracz odpadł, a przy kilku
        // osobach na macie to jedyna informacja, która się liczy.
        using GameTestHarness harness = new(useResourceLocalization: true);

        bool rozpoznano = harness.VoiceCommandParser.TryParse(wypowiedz, out _);

        Assert.Equal(wypowiedz.Contains("dalej", StringComparison.OrdinalIgnoreCase), rozpoznano);
    }

    [Theory]
    [InlineData("stoi")]
    [InlineData("stop")]
    public void KrotkieSlowa_WymagajaDokladnegoTrafienia(string wypowiedz)
    {
        // „stop" jest frazą pauzy, „stoi" różni się od niej dwiema literami — przy czterech
        // literach każda pomyłka to już inne słowo, więc tolerancja jest zerowa.
        using GameTestHarness harness = new(useResourceLocalization: true);

        bool rozpoznano = harness.VoiceCommandParser.TryParse(wypowiedz, out VoiceCommandType command);

        if (wypowiedz == "stop")
        {
            Assert.True(rozpoznano);
            Assert.Equal(VoiceCommandType.Pause, command);
        }
        else
        {
            Assert.False(rozpoznano);
        }
    }

    [Theory]
    [InlineData("Powtórz!", VoiceCommandType.Repeat)]
    [InlineData("powtorz", VoiceCommandType.Repeat)]
    [InlineData("Pauza!", VoiceCommandType.Pause)]
    [InlineData("wznow", VoiceCommandType.Resume)]
    [InlineData("NASTĘPNY", VoiceCommandType.Next)]
    public void OgonkiWielkoscLiterIInterpunkcja_NieMajaZnaczenia(
        string wypowiedz,
        VoiceCommandType oczekiwana)
    {
        // Rozpoznawanie zwraca „powtórz" albo „powtorz", z kropką albo bez, a polskie „ł"
        // bywa oddawane jako „l" — dla nas to musi być ten sam tekst.
        using GameTestHarness harness = new(useResourceLocalization: true);

        Assert.True(harness.VoiceCommandParser.TryParse(wypowiedz, out VoiceCommandType command));
        Assert.Equal(oczekiwana, command);
    }

    [Fact]
    public void ZmianaJezykaAplikacji_ZmieniaRozpoznawaneFrazy()
    {
        // Gracze mówią w tym samym języku, w którym aplikacja mówi do nich, więc zmiana
        // języka musi przełączyć także frazy komend — i unieważnić ich zapamiętaną postać.
        using GameTestHarness harness = new(useResourceLocalization: true);

        Assert.False(harness.VoiceCommandParser.TryParse("next", out _));

        harness.Localization.SetCulture(CultureInfo.GetCultureInfo("en"));

        Assert.True(harness.VoiceCommandParser.TryParse("next", out VoiceCommandType command));
        Assert.Equal(VoiceCommandType.Next, command);
        Assert.False(harness.VoiceCommandParser.TryParse("dalej", out _));
    }
}
