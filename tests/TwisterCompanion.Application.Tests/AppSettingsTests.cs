using TwisterCompanion.Application.Settings;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy niezmienników ustawień aplikacji.
/// </summary>
/// <remarks>
/// Zakresy są pilnowane w akcesorach <c>init</c>, więc muszą działać także dla wyrażenia
/// <c>with</c> — a właśnie tą drogą ustawienia są zmieniane w całej aplikacji.
/// </remarks>
public class AppSettingsTests
{
    [Fact]
    public void Default_MaSensowneWartosciPoczatkowe()
    {
        AppSettings settings = AppSettings.Default;

        Assert.Null(settings.LanguageCode);
        Assert.True(settings.IsTextToSpeechEnabled);
        Assert.True(settings.AreSoundsEnabled);

        // Sterowanie głosem jest domyślnie WYŁĄCZONE, w przeciwieństwie do pozostałych
        // udogodnień: wymaga zgody na mikrofon, a pytanie o nią przy pierwszym uruchomieniu,
        // zanim gracz wie po co, jest złym pierwszym wrażeniem.
        Assert.False(settings.IsVoiceControlEnabled);
        Assert.Equal(TurnAdvanceMode.Manual, settings.TurnAdvanceMode);
        Assert.Equal(1.0f, settings.SpeechRate);
        Assert.Equal(1.0f, settings.SpeechPitch);
        Assert.False(string.IsNullOrWhiteSpace(settings.GameModeKey));
        Assert.Null(settings.ActiveEventPackId);
    }

    [Fact]
    public void Default_MieściSieWeWlasnychZakresach()
    {
        AppSettings settings = AppSettings.Default;

        Assert.InRange(settings.SpeechRate, AppSettings.MinSpeechRate, AppSettings.MaxSpeechRate);
        Assert.InRange(settings.SpeechPitch, AppSettings.MinSpeechPitch, AppSettings.MaxSpeechPitch);
        Assert.InRange(settings.SoundVolume, 0.0, 1.0);
        Assert.InRange(
            settings.VoiceListeningDelay,
            AppSettings.MinVoiceListeningDelay,
            AppSettings.MaxVoiceListeningDelay);
        Assert.InRange(settings.MoveTime, AppSettings.MinMoveTime, AppSettings.MaxMoveTime);
        Assert.InRange(settings.TaskTime, AppSettings.MinTaskTime, AppSettings.MaxTaskTime);
    }

    [Theory]
    [InlineData(0.1f)]
    [InlineData(5.0f)]
    public void SpeechRate_PozaZakresem_RzucaWyjatek(float tempo) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AppSettings.Default with { SpeechRate = tempo });

    [Theory]
    [InlineData(0.1f)]
    [InlineData(5.0f)]
    public void SpeechPitch_PozaZakresem_RzucaWyjatek(float wysokosc) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AppSettings.Default with { SpeechPitch = wysokosc });

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void SoundVolume_PozaZakresem_RzucaWyjatek(double glosnosc) =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AppSettings.Default with { SoundVolume = glosnosc });

    [Fact]
    public void MoveTime_ZaDlugi_RzucaWyjatek() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AppSettings.Default with { MoveTime = TimeSpan.FromHours(1) });

    [Fact]
    public void TaskTime_ZaDlugi_RzucaWyjatek() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AppSettings.Default with { TaskTime = TimeSpan.FromHours(1) });

    [Fact]
    public void CzasyZakresowe_SaPrzyjmowane()
    {
        // Czas na zadanie jest domyślnie dłuższy niż na ruch: „zaśpiewaj refren" trwa
        // dłużej niż postawienie ręki.
        AppSettings settings = AppSettings.Default with
        {
            MoveTime = TimeSpan.FromSeconds(20),
            TaskTime = TimeSpan.FromSeconds(30),
        };

        Assert.Equal(TimeSpan.FromSeconds(20), settings.MoveTime);
        Assert.Equal(TimeSpan.FromSeconds(30), settings.TaskTime);
        Assert.True(AppSettings.Default.TaskTime > AppSettings.Default.MoveTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GameModeKey_Pusty_RzucaWyjatek(string klucz) =>
        Assert.Throws<ArgumentException>(() => AppSettings.Default with { GameModeKey = klucz });

    [Fact]
    public void GameModeKey_JestObcinanyZBialychZnakow() =>
        Assert.Equal("hardcore", (AppSettings.Default with { GameModeKey = "  hardcore  " }).GameModeKey);

    [Fact]
    public void Zmiana_NieModyfikujeInstancjiZrodlowej()
    {
        AppSettings original = AppSettings.Default;

        AppSettings zmienione = original with { AreSoundsEnabled = false };

        Assert.True(original.AreSoundsEnabled);
        Assert.False(zmienione.AreSoundsEnabled);
    }

    [Fact]
    public void Rownosc_DzialaPoWartosciach() =>
        Assert.Equal(AppSettings.Default, AppSettings.Default with { });
}
