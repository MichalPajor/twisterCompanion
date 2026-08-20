using System.Runtime.CompilerServices;

namespace TwisterCompanion.Tests.Shared;

/// <summary>
/// Odnajduje katalog repozytorium na podstawie położenia pliku źródłowego testu.
/// </summary>
/// <remarks>
/// Część testów czyta <b>pliki źródłowe</b>, a nie skompilowane zestawy: użycia kluczy są
/// rozsiane po kodzie i po ekranach XAML, katalogi tłumaczeń mają być odkrywane, a wersja
/// i identyfikatory reklam żyją w plikach projektu i w manifeście. Katalog wyjściowy buildu
/// leży poza repozytorium (artefakty są przekierowane), więc ścieżki liczymy od pliku źródła.
/// <para>
/// Plik jest <b>dołączany linkiem</b> do kilku projektów testowych, zamiast być skopiowany
/// do każdego z nich. Kopii było już trzy i każda następna rozjeżdżałaby się po cichu.
/// </para>
/// </remarks>
internal static class KatalogRepozytorium
{
    /// <summary>Zwraca katalog repozytorium — pierwszy nadrzędny, w którym jest „src".</summary>
    /// <param name="plikWywolujacy">Wypełniane przez kompilator.</param>
    public static string Znajdz([CallerFilePath] string plikWywolujacy = "")
    {
        DirectoryInfo? katalog = new FileInfo(plikWywolujacy).Directory;

        while (katalog is not null && !Directory.Exists(Path.Combine(katalog.FullName, "src")))
        {
            katalog = katalog.Parent;
        }

        return katalog?.FullName
               ?? throw new DirectoryNotFoundException("Nie znalazłem katalogu repozytorium.");
    }

    /// <summary>Zwraca ścieżkę do pliku w repozytorium.</summary>
    /// <param name="segmenty">Kolejne segmenty ścieżki względem katalogu repozytorium.</param>
    public static string Plik(params string[] segmenty) =>
        Path.Combine([Znajdz(), .. segmenty]);

    /// <summary>Zwraca katalog z plikami tłumaczeń.</summary>
    public static string Tlumaczenia() =>
        Plik("src", "TwisterCompanion.Application", "Resources", "Strings");

    /// <summary>
    /// Zwraca kody języków, dla których istnieje katalog tłumaczeń.
    /// </summary>
    /// <remarks>
    /// Odkrywane z plików, a nie wypisane w teście: dzięki temu dołożenie języka od razu
    /// wchodzi pod wszystkie testy spójności, zamiast wymagać dopisania go w kilku miejscach —
    /// czyli zamiast czekać, aż ktoś o tym zapomni.
    /// </remarks>
    public static IReadOnlyList<string> PrzetlumaczoneJezyki() =>
    [
        .. Directory.EnumerateFiles(Tlumaczenia(), "AppResources.*.resx")
            .Select(plik => Path.GetFileNameWithoutExtension(plik).Split('.')[^1])
            .Order(),
    ];
}
