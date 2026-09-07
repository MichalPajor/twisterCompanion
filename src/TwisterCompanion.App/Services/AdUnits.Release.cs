namespace TwisterCompanion.App.Services;

/// <summary>
/// Jednostki reklamowe produkcyjne — kompilowane wyłącznie w Release.
/// </summary>
/// <remarks>
/// Dlaczego osobny plik, a nie <c>#if</c> w jednym: testy konfiguracji wydania i kontrola
/// w przebiegu wydania czytają <b>pliki źródłowe</b>. Przy dwóch definicjach tej samej stałej
/// w jednym pliku musiałyby rozumieć preprocesor; przy dwóch plikach wystarczy wskazać właściwy.
/// </remarks>
internal static partial class AdUnits
{
    /// <summary>Jednostka banera na ekranie rozgrywki.</summary>
    public const string Banner = "ca-app-pub-6431615298155605/9488771883";

    /// <summary>Jednostka reklamy pełnoekranowej po zakończonej partii.</summary>
    public const string Interstitial = "ca-app-pub-6431615298155605/8620106447";
}
