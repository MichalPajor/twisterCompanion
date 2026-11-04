using System.Text.RegularExpressions;
using TwisterCompanion.Tests.Shared;

namespace TwisterCompanion.Architecture.Tests;

/// <summary>
/// Strażnik limitu znaków w polach tekstowych.
/// </summary>
/// <remarks>
/// Android układa na nowo cały tekst pola przy każdym wpisanym znaku. W polu bez limitu
/// wklejony bardzo długi tekst zajmował wątek interfejsu na ponad pięć sekund i system
/// zgłaszał ANR — tak przyszło zgłoszenie z Google Play dla wersji 1.0.3 (stos kończył się
/// w <c>DynamicLayout.reflow</c>). Nowe pole bez <c>MaxLength</c> wróciłoby do tego samego,
/// a na emulatorze nic tego nie zdradza.
/// </remarks>
public class PolaTekstoweTests
{
    [Fact]
    public void KazdePoleTekstowe_MaLimitZnakow()
    {
        string widoki = KatalogRepozytorium.Plik("src", "TwisterCompanion.App", "Views");
        List<string> bezLimitu = [];

        foreach (string plik in Directory.EnumerateFiles(widoki, "*.xaml"))
        {
            string xaml = File.ReadAllText(plik);

            foreach (Match pole in Regex.Matches(xaml, @"<(?:Entry|Editor)\b[^>]*>", RegexOptions.Singleline))
            {
                if (!pole.Value.Contains("MaxLength=", StringComparison.Ordinal))
                {
                    int linia = xaml[..pole.Index].Count(znak => znak == '\n') + 1;
                    bezLimitu.Add($"{Path.GetFileName(plik)}:{linia}");
                }
            }
        }

        Assert.True(bezLimitu.Count == 0, "Pola tekstowe bez MaxLength: " + string.Join(", ", bezLimitu));
    }
}
