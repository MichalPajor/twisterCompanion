using TwisterCompanion.Application.Settings;
using TwisterCompanion.Infrastructure.Tests.Fixtures;

namespace TwisterCompanion.Infrastructure.Tests;

/// <summary>
/// Testy przechowywania ustawień — wartości domyślne, trwałość, powiadamianie o zmianach
/// i odporność na uszkodzony plik.
/// </summary>
public class SettingsServiceTests
{
    [Fact]
    public async Task LoadAsync_GdyBrakPliku_UstawiaWartosciDomyslne()
    {
        using TemporaryStorage storage = new();

        await storage.Settings.LoadAsync();

        Assert.Equal(AppSettings.Default, storage.Settings.Current);
    }

    [Fact]
    public async Task LoadAsync_ZglaszaZdarzenieZWczytanymiUstawieniami()
    {
        // Regresja zgłoszona z urządzenia: wybrany ciemny wygląd wracał po restarcie do
        // jasnego. Przyczyną był brak tego zdarzenia — wygląd stosował się raz, przy starcie,
        // i trafiał na wartości domyślne, bo plik był jeszcze nieodczytany.
        using TemporaryStorage pierwszeUruchomienie = new();
        await pierwszeUruchomienie.Settings.LoadAsync();
        await pierwszeUruchomienie.Settings.UpdateAsync(settings => settings with
        {
            ThemePreference = AppThemePreference.Dark,
        });

        using TemporaryStorage poRestarcie = new(pierwszeUruchomienie.Root);
        AppSettings? zgloszone = null;
        poRestarcie.Settings.Changed += (_, settings) => zgloszone = settings;

        await poRestarcie.Settings.LoadAsync();

        Assert.NotNull(zgloszone);
        Assert.Equal(AppThemePreference.Dark, zgloszone.ThemePreference);
    }

    [Fact]
    public async Task LoadAsync_GdyBrakPliku_ZglaszaZdarzenieZWartosciamiDomyslnymi()
    {
        // Także wtedy, bo subskrybent nie ma innego sposobu dowiedzieć się, że odczyt się
        // zakończył. Bez zdarzenia zostałby z wyglądem sprzed odczytu — czyli z niczym.
        using TemporaryStorage storage = new();
        AppSettings? zgloszone = null;
        storage.Settings.Changed += (_, settings) => zgloszone = settings;

        await storage.Settings.LoadAsync();

        Assert.Equal(AppSettings.Default, zgloszone);
    }

    [Fact]
    public async Task UpdateAsync_ZapisujeZmianeIWczytujeJaPonownie()
    {
        using TemporaryStorage storage = new();
        await storage.Settings.LoadAsync();

        await storage.Settings.UpdateAsync(settings => settings with
        {
            LanguageCode = "en",
            IsTextToSpeechEnabled = false,
            SpeechRate = 1.5f,
            TurnAdvanceMode = TurnAdvanceMode.Automatic,
            MoveTime = TimeSpan.FromSeconds(15),
            AreAnimationsEnabled = false,
            AreSoundsEnabled = false,
            SoundVolume = 0.35,
            AreHapticsEnabled = false,
            HasSeenOnboarding = true,
            FinishedGamesCount = 7,
            GameModeKey = "hardcore",
            ActiveEventPackId = new Guid("11111111-2222-3333-4444-555555555555"),
        });

        await storage.Settings.LoadAsync();

        Assert.Equal("en", storage.Settings.Current.LanguageCode);
        Assert.False(storage.Settings.Current.IsTextToSpeechEnabled);
        Assert.Equal(1.5f, storage.Settings.Current.SpeechRate);
        Assert.Equal(TurnAdvanceMode.Automatic, storage.Settings.Current.TurnAdvanceMode);
        Assert.Equal(TimeSpan.FromSeconds(15), storage.Settings.Current.MoveTime);
        Assert.False(storage.Settings.Current.AreAnimationsEnabled);
        Assert.False(storage.Settings.Current.AreSoundsEnabled);
        Assert.Equal(0.35, storage.Settings.Current.SoundVolume);
        Assert.False(storage.Settings.Current.AreHapticsEnabled);
        Assert.True(storage.Settings.Current.HasSeenOnboarding);

        // Etap 15: licznik zakończonych partii musi przeżyć restart, bo na nim stoi reguła
        // „reklama pełnoekranowa co trzecią partię". Gdyby żył w pamięci, wystarczyłoby
        // zamknąć aplikację, żeby reklama wracała częściej.
        Assert.Equal(7, storage.Settings.Current.FinishedGamesCount);

        // Zadanie 2 Etapu 12: wybrany tryb i aktywna paczka wydarzeń przeżywają restart.
        Assert.Equal("hardcore", storage.Settings.Current.GameModeKey);
        Assert.Equal(new Guid("11111111-2222-3333-4444-555555555555"), storage.Settings.Current.ActiveEventPackId);
    }

    [Fact]
    public async Task UpdateAsync_ZglaszaZdarzenieZeZmienionymiUstawieniami()
    {
        using TemporaryStorage storage = new();
        await storage.Settings.LoadAsync();
        AppSettings? zgloszone = null;
        storage.Settings.Changed += (_, settings) => zgloszone = settings;

        await storage.Settings.UpdateAsync(settings => settings with { AreSoundsEnabled = false });

        Assert.NotNull(zgloszone);
        Assert.False(zgloszone.AreSoundsEnabled);
    }

    [Fact]
    public async Task ResetAsync_PrzywracaWartosciDomyslneNaDysku()
    {
        using TemporaryStorage storage = new();
        await storage.Settings.LoadAsync();
        await storage.Settings.UpdateAsync(settings => settings with { LanguageCode = "en" });

        await storage.Settings.ResetAsync();
        await storage.Settings.LoadAsync();

        Assert.Equal(AppSettings.Default, storage.Settings.Current);
    }

    [Fact]
    public async Task LoadAsync_GdyPlikJestUszkodzony_WracaDoWartosciDomyslnych()
    {
        // Uszkodzony plik ustawień nie może zablokować startu aplikacji.
        using TemporaryStorage storage = new();
        storage.WriteRawSettingsFile("{ to nie jest JSON");

        await storage.Settings.LoadAsync();

        Assert.Equal(AppSettings.Default, storage.Settings.Current);
    }

    [Fact]
    public async Task LoadAsync_GdyWartosciSaPozaZakresem_PrzycinaJeZamiastRzucacWyjatkiem()
    {
        // Model AppSettings odrzuca wartości niemożliwe wyjątkiem. Plik mógł zostać
        // zmodyfikowany ręcznie, więc mapowanie musi je przyciąć — inaczej aplikacja
        // nie wystartowałaby wcale.
        using TemporaryStorage storage = new();
        storage.WriteRawSettingsFile(
            """
            {
              "schemaVersion": 1,
              "speechRate": 99.0,
              "speechPitch": -5.0,
              "soundVolume": 4.2,
              "moveTimeSeconds": 100000
            }
            """);

        await storage.Settings.LoadAsync();

        Assert.Equal(AppSettings.MaxSpeechRate, storage.Settings.Current.SpeechRate);
        Assert.Equal(AppSettings.MinSpeechPitch, storage.Settings.Current.SpeechPitch);
        Assert.Equal(1.0, storage.Settings.Current.SoundVolume);
        Assert.Equal(AppSettings.MaxMoveTime, storage.Settings.Current.MoveTime);
    }

    [Fact]
    public async Task LoadAsync_ZeStarszegoPliku_PrzenosiOdstepMiedzyTuramiNaCzasRuchu()
    {
        // Plik zapisany przed rozdzieleniem czasu na ruch i czasu na zadanie miał jeden
        // „odstęp między turami". Użytkownik nie ma powodu tracić swojego ustawienia przy
        // aktualizacji aplikacji.
        using TemporaryStorage storage = new();
        storage.WriteRawSettingsFile(
            """
            {
              "schemaVersion": 1,
              "autoAdvanceIntervalSeconds": 25
            }
            """);

        await storage.Settings.LoadAsync();

        Assert.Equal(TimeSpan.FromSeconds(25), storage.Settings.Current.MoveTime);
        Assert.Equal(AppSettings.Default.TaskTime, storage.Settings.Current.TaskTime);
    }

    [Fact]
    public async Task LoadAsync_GdyPlikMaNowszaWersjeSchematu_WracaDoWartosciDomyslnych()
    {
        using TemporaryStorage storage = new();
        storage.WriteRawSettingsFile("""{ "schemaVersion": 999, "languageCode": "de" }""");

        await storage.Settings.LoadAsync();

        Assert.Equal(AppSettings.Default, storage.Settings.Current);
    }

    [Fact]
    public async Task UpdateAsync_WieleZmianPodRzad_ZapisujeOstatniStan()
    {
        // Odpowiednik przeciągania suwaka głośności: seria zmian szybsza niż zapis pliku.
        using TemporaryStorage storage = new();
        await storage.Settings.LoadAsync();

        for (int i = 1; i <= 10; i++)
        {
            double volume = i / 10.0;
            await storage.Settings.UpdateAsync(settings => settings with { SoundVolume = volume });
        }

        await storage.Settings.LoadAsync();

        Assert.Equal(1.0, storage.Settings.Current.SoundVolume);
    }
}
