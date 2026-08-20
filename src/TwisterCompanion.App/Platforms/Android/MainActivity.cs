using Android.App;
using Android.Content.PM;
using Android.OS;

namespace TwisterCompanion.App;

/// <summary>
/// Aktywność główna aplikacji.
/// </summary>
/// <remarks>
/// Tło okna jest tu ustawiane <b>ponownie</b>, już po utworzeniu aktywności, i to jest naprawa
/// zgłoszonego błędu „szary ekran bez obrazka". MAUI startuje aktywność w motywie powitalnym,
/// ale w <c>OnCreate</c> przestawia ją na motyw główny — a jego tłem jest domyślna szarość
/// biblioteki Material, nie nasz grafit ze znakiem. Systemowy ekran powitalny znika wtedy
/// natychmiast, a przez cały czas przygotowania aplikacji (wczytanie ustawień, próbek
/// dźwiękowych i pierwszego ekranu) widać właśnie tę szarość.
/// <para>
/// Ustawienie tła na rysunek ekranu powitalnego sprawia, że ta przerwa wygląda jak dalsza część
/// ekranu powitalnego, a nie jak zawieszona aplikacja. Nie zastępuje systemowego ekranu
/// powitalnego — działa po nim.
/// </para>
/// </remarks>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        KeepSplashBackground();
    }

    /// <summary>
    /// Zostawia na oknie tło ekranu powitalnego, dopóki nie pojawi się pierwszy ekran.
    /// </summary>
    /// <remarks>
    /// Identyfikator zasobu jest pobierany po nazwie, a nie przez klasę wygenerowaną z zasobów:
    /// rysunek pochodzi z biblioteki MAUI, więc odwołanie po nazwie nie zależy od tego, jak
    /// wersja narzędzi generuje klasę zasobów. Brak zasobu nie jest błędem — wtedy zostaje
    /// zachowanie domyślne, czyli tło motywu.
    /// </remarks>
    private void KeepSplashBackground()
    {
        try
        {
            int splash = Resources?.GetIdentifier("maui_splash", "drawable", PackageName) ?? 0;

            if (splash != 0)
            {
                Window?.SetBackgroundDrawableResource(splash);
            }
        }
        catch (Exception)
        {
            // Tło okna to kosmetyka startu — jego brak nie może przeszkodzić w uruchomieniu.
        }
    }
}
