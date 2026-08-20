using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Settings;
using TwisterCompanion.Application.Tests.Fakes;
using TwisterCompanion.Application.Voice;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy warstwy odczytu komunikatów: ustawienia, przerywanie i odporność na awarie.
/// </summary>
public class AnnouncementSpeakerTests
{
    private static readonly Announcement Ruch =
        new("Kuba, prawa ręka — czerwony.", AnnouncementKind.Move);

    [Fact]
    public async Task PrzygotowanieSyntezatora_IdzieDoUrzadzenia()
    {
        // Silnik mowy Androida budzi się przy pierwszym użyciu i pierwsza wypowiedź potrafi
        // spóźnić się o kilka sekund. Start aplikacji budzi go więc wcześniej — zgłoszone
        // z urządzenia jako „ponad pięć sekund ciszy przed początkiem gry".
        using GameTestHarness harness = new();

        await harness.Speaker.PrepareAsync();

        Assert.Equal(1, harness.TextToSpeech.PrepareCalls);
    }

    [Fact]
    public async Task PrzygotowanieSyntezatora_NieWywracaSiePrzyAwarii()
    {
        // Brak silnika mowy nie może przeszkodzić w uruchomieniu aplikacji — a to wywołanie
        // idzie ze startu, gdzie nikt na nie nie czeka i nikt go nie obserwuje.
        using GameTestHarness harness = new();

        harness.TextToSpeech.FailWith = new InvalidOperationException("brak silnika mowy");

        await harness.Speaker.PrepareAsync();
    }

    [Fact]
    public async Task WlaczonyOdczyt_PrzekazujeTekstDoSyntezatora()
    {
        using GameTestHarness harness = new();

        await harness.Speaker.SpeakAsync(Ruch);

        Assert.Equal(["Kuba, prawa ręka — czerwony."], harness.TextToSpeech.Spoken);
    }

    [Fact]
    public async Task WylaczonyOdczyt_NieMowiNic()
    {
        using GameTestHarness harness = new();

        await harness.SettingsService.UpdateAsync(settings => settings with
        {
            IsTextToSpeechEnabled = false,
        });

        await harness.Speaker.SpeakAsync(Ruch);

        Assert.Empty(harness.TextToSpeech.Spoken);
    }

    [Fact]
    public async Task Wypowiedz_UzywaGlosuTempaIWysokosciZUstawien()
    {
        using GameTestHarness harness = new();

        await harness.SettingsService.UpdateAsync(settings => settings with
        {
            PreferredVoiceId = "pl||Polski",
            SpeechRate = 1.25f,
            SpeechPitch = 0.75f,
        });

        await harness.Speaker.SpeakAsync(Ruch);

        SpeechRequest request = Assert.IsType<SpeechRequest>(harness.TextToSpeech.LastRequest);

        Assert.Equal("pl||Polski", request.VoiceId);
        Assert.Equal(1.25f, request.Rate);
        Assert.Equal(0.75f, request.Pitch);
    }

    [Fact]
    public async Task AwariaSyntezatora_NieZatrzymujeRozgrywki()
    {
        // Brak mowy pogarsza doświadczenie, ale tekst jest widoczny na ekranie —
        // wyjątek z syntezatora nie może przerwać tury.
        using GameTestHarness harness = new();
        harness.TextToSpeech.FailWith = new InvalidOperationException("Brak silnika mowy.");

        await harness.Speaker.SpeakAsync(Ruch);

        Assert.False(harness.Speaker.IsSpeaking);
    }

    [Fact]
    public async Task NowaWypowiedz_PrzerywaTrwajaca()
    {
        // Zasada „ostatnie polecenie wygrywa": „Powtórz" ma zadziałać od razu, a nie
        // po dokończeniu zdania, które właśnie chcemy powtórzyć.
        using GameTestHarness harness = new();
        TaskCompletionSource brama = new();
        harness.TextToSpeech.Gate = brama;

        Task pierwsza = harness.Speaker.SpeakAsync(Ruch);

        harness.TextToSpeech.Gate = null;

        await harness.Speaker.SpeakAsync(new Announcement("Anna, lewa noga — zielony.", AnnouncementKind.Move));
        await pierwsza;

        // Przerwana wypowiedź nigdy nie doszła do syntezatora — wypowiedziane jest
        // tylko to, co przerwało.
        Assert.Equal(["Anna, lewa noga — zielony."], harness.TextToSpeech.Spoken);
        Assert.True(harness.TextToSpeech.StopCount > 0);
    }

    [Fact]
    public async Task Wypowiedz_ZglaszaZmianeStanuMowienia()
    {
        // Etap 8 wycisza na tej podstawie mikrofon — inaczej rozpoznawanie mowy
        // usłyszy własny głos aplikacji i „rozpozna" w nim komendę.
        using GameTestHarness harness = new();
        List<bool> zmiany = [];

        harness.Speaker.SpeakingChanged += (_, mowi) => zmiany.Add(mowi);

        await harness.Speaker.SpeakAsync(Ruch);

        Assert.Equal([true, false], zmiany);
        Assert.False(harness.Speaker.IsSpeaking);
    }

    [Fact]
    public async Task Cisza_PrzerywaWypowiedzWSyntezatorze()
    {
        using GameTestHarness harness = new();

        await harness.Speaker.SilenceAsync();

        Assert.Equal(1, harness.TextToSpeech.StopCount);
    }
}
