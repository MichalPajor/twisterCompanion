using System.Collections;
using System.Globalization;
using System.Resources;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Infrastructure.Tests.Fixtures;
using TwisterCompanion.Tests.Shared;

namespace TwisterCompanion.Infrastructure.Tests;

/// <summary>
/// Testy serwisu tłumaczeń — przełączania języka, zachowania przy brakujących kluczach
/// i powiązania z ustawieniami.
/// </summary>
public class LocalizationServiceTests
{
    [Fact]
    public void SetCulture_ZmieniaJezykTekstow()
    {
        using TemporaryStorage storage = new();
        ILocalizationService localization = storage.Localization;

        localization.SetLanguage("en");
        string angielski = localization["Home_Button_Game"];

        localization.SetLanguage("pl");
        string polski = localization["Home_Button_Game"];

        Assert.Equal("Play", angielski);
        Assert.Equal("Rozgrywka", polski);
    }

    [Fact]
    public void SetCulture_ZglaszaZdarzenieZmiany()
    {
        using TemporaryStorage storage = new();
        ILocalizationService localization = storage.Localization;
        localization.SetLanguage("en");

        CultureInfo? zgloszona = null;
        localization.CultureChanged += (_, culture) => zgloszona = culture;

        localization.SetLanguage("pl");

        Assert.NotNull(zgloszona);
        Assert.Equal("pl", zgloszona.TwoLetterISOLanguageName);
    }

    [Fact]
    public void SetCulture_TenSamJezyk_NieZglaszaZdarzenia()
    {
        using TemporaryStorage storage = new();
        ILocalizationService localization = storage.Localization;
        localization.SetLanguage("pl");

        int wywolania = 0;
        localization.CultureChanged += (_, _) => wywolania++;

        localization.SetLanguage("pl");

        Assert.Equal(0, wywolania);
    }

    [Fact]
    public void SetLanguage_NieznanyJezyk_SchodziNaJezykSystemuAlboAngielski()
    {
        using TemporaryStorage storage = new();
        ILocalizationService localization = storage.Localization;

        localization.SetLanguage("ja");

        Assert.Contains(
            localization.CurrentCulture.TwoLetterISOLanguageName,
            localization.SupportedCultures.Select(culture => culture.TwoLetterISOLanguageName));
    }

    [Fact]
    public void SetLanguage_KodZRegionem_JestRozpoznawany()
    {
        using TemporaryStorage storage = new();
        ILocalizationService localization = storage.Localization;

        localization.SetLanguage("pl-PL");

        Assert.Equal("pl", localization.CurrentCulture.TwoLetterISOLanguageName);
    }

    [Fact]
    public void BrakujacyKlucz_ZwracaKluczWNawiasach()
    {
        // Brak tłumaczenia ma być natychmiast widoczny na ekranie. Pusty napis
        // potrafi umknąć, [Nieistniejacy_Klucz] nie umknie.
        using TemporaryStorage storage = new();

        Assert.Equal("[Nieistniejacy_Klucz]", storage.Localization["Nieistniejacy_Klucz"]);
    }

    [Fact]
    public void GetString_ZKatalogGlosowy_CzytaZOsobnegoPliku()
    {
        using TemporaryStorage storage = new();
        ILocalizationService localization = storage.Localization;
        localization.SetLanguage("pl");

        string czescCiala = localization.GetString("Voice_BodyPart_RightHand", StringCatalog.Voice);

        Assert.Equal("prawa ręka", czescCiala);
    }

    [Fact]
    public void GetFormattedString_PodstawiaArgumenty()
    {
        using TemporaryStorage storage = new();
        ILocalizationService localization = storage.Localization;
        localization.SetLanguage("pl");

        string wywolanie = localization.GetFormattedString(
            "Voice_Announce_PlayerTurn",
            StringCatalog.Voice,
            "Kuba");

        string komunikat = localization.GetFormattedString(
            "Voice_Announce_Move",
            StringCatalog.Voice,
            "prawa ręka",
            "czerwony");

        // Imię pada osobnym komunikatem, przed poleceniem ruchu.
        Assert.Equal("Kuba.", wywolanie);
        Assert.Equal("prawa ręka — czerwony.", komunikat);
    }

    [Fact]
    public void GetFormattedString_ZaMaloArgumentow_ZwracaWzorzecBezWyjatku()
    {
        using TemporaryStorage storage = new();

        string wynik = storage.Localization.GetFormattedString(
            "Voice_Announce_Move",
            StringCatalog.Voice,
            "Kuba");

        Assert.Contains("{1}", wynik, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ZapisJezykaWUstawieniach_PrzelaczaJezykBezDodatkowegoWywolania()
    {
        // Kluczowa właściwość projektu: istnieje jedna droga zmiany języka. Serwis
        // tłumaczeń nasłuchuje ustawień, więc nie da się zmienić języka bez zapamiętania
        // go ani zapamiętać bez zastosowania.
        using TemporaryStorage storage = new();
        await storage.Settings.LoadAsync();
        storage.Localization.SetLanguage("pl");

        await storage.Settings.UpdateAsync(settings => settings with { LanguageCode = "en" });

        Assert.Equal("en", storage.Localization.CurrentCulture.TwoLetterISOLanguageName);
        Assert.Equal("Play", storage.Localization["Home_Button_Game"]);
    }

    [Fact]
    public async Task WczytanieUstawien_StosujeZapisanyJezykBezDodatkowegoWywolania()
    {
        // Do niedawna język po restarcie ustawiał start aplikacji osobnym wywołaniem, a
        // wygląd takiego wywołania nie miał i po prostu nie działał. Teraz obie rzeczy jadą
        // na zdarzeniu wczytania ustawień — ten test pilnuje właśnie tego zdarzenia.
        using TemporaryStorage pierwszeUruchomienie = new();
        await pierwszeUruchomienie.Settings.LoadAsync();
        await pierwszeUruchomienie.Settings.UpdateAsync(settings => settings with { LanguageCode = "en" });

        using TemporaryStorage poRestarcie = new(pierwszeUruchomienie.Root);

        // Serwis tłumaczeń rozwiązany PRZED wczytaniem — tak samo robi to MauiProgram,
        // bo dopiero utworzenie serwisu zapisuje go na zdarzenie zmiany ustawień.
        ILocalizationService tlumaczenia = poRestarcie.Localization;
        tlumaczenia.SetLanguage("pl");

        await poRestarcie.Settings.LoadAsync();

        Assert.Equal("en", tlumaczenia.CurrentCulture.TwoLetterISOLanguageName);
    }

    [Fact]
    public void KazdyObslugiwanyJezyk_MaPelnyKatalogTlumaczen()
    {
        // Deklaracja bez katalogu to język, który po wybraniu pokazuje angielski — działa,
        // więc nikt tego nie zauważy poza użytkownikiem, który wybrał ten język. Test liczy
        // klucze w katalogu każdego zadeklarowanego języka i porównuje z katalogiem neutralnym.
        using TemporaryStorage storage = new();

        ResourceManager teksty = new(
            "TwisterCompanion.Application.Resources.Strings.AppResources",
            typeof(ILocalizationService).Assembly);

        int neutralne = LiczKlucze(teksty, CultureInfo.InvariantCulture);

        Assert.True(neutralne > 0);

        string[] niekompletne =
        [
            .. storage.Localization.SupportedCultures
                .Where(culture => culture.TwoLetterISOLanguageName != "en")
                .Select(culture => (culture, klucze: LiczKlucze(teksty, culture)))
                .Where(wpis => wpis.klucze != neutralne)
                .Select(wpis => $"{wpis.culture.Name}: {wpis.klucze}/{neutralne}"),
        ];

        Assert.Empty(niekompletne);
    }

    [Fact]
    public void SupportedCultures_OdpowiadaPlikomTlumaczen()
    {
        // Lista języków jest wypisana w kodzie (wykrywanie satelickich zasobów w czasie
        // działania zależy od sposobu pakowania aplikacji), więc łatwo dołożyć plik .resx
        // i zapomnieć o wpisie — albo odwrotnie. Test porównuje jedno z drugim.
        using TemporaryStorage storage = new();

        string[] zadeklarowane =
        [
            .. storage.Localization.SupportedCultures
                .Select(culture => culture.Name)
                .Where(kod => kod != "en")
                .Order(),
        ];

        string[] zPlikow =
        [
            .. Directory.EnumerateFiles(KatalogRepozytorium.Tlumaczenia(), "AppResources.*.resx")
                .Select(plik => Path.GetFileNameWithoutExtension(plik).Split('.')[^1])
                .Order(),
        ];

        Assert.Equal(zPlikow, zadeklarowane);
        Assert.Contains("en", storage.Localization.SupportedCultures.Select(culture => culture.Name));
    }

    [Fact]
    public void NazwyKonczyn_WKazdymJezyku_DajaTeSameWielkieLiteryNiezaleznieOdKultury()
    {
        // Napis na kółku ruchu jest zamieniany na wielkie litery przez TextTransform,
        // a ten w MAUI działa niezależnie od kultury. Po turecku „i" ma dużą literę „İ",
        // nie „I", więc gdyby nazwa kończyny zawierała „i", napis byłby błędny. Dziś nie
        // zawiera — test pilnuje, żeby tłumaczenie tego nie zmieniło niezauważenie.
        string[] klucze =
        [
            "Voice_BodyPart_RightHand",
            "Voice_BodyPart_LeftHand",
            "Voice_BodyPart_RightFoot",
            "Voice_BodyPart_LeftFoot",
        ];

        using TemporaryStorage storage = new();
        ILocalizationService localization = storage.Localization;

        List<string> rozbieznosci = [];

        foreach (CultureInfo culture in localization.SupportedCultures)
        {
            localization.SetLanguage(culture.Name);

            foreach (string klucz in klucze)
            {
                string nazwa = localization.GetString(klucz, StringCatalog.Voice);

                if (nazwa.ToUpperInvariant() != nazwa.ToUpper(culture))
                {
                    rozbieznosci.Add($"{culture.Name}/{klucz}: {nazwa}");
                }
            }
        }

        Assert.Empty(rozbieznosci);
    }

    /// <summary>Liczy klucze w katalogu danego języka, bez sięgania do języka nadrzędnego.</summary>
    /// <remarks>
    /// <c>tryParents: false</c> jest tu istotne: z dziedziczeniem po języku nadrzędnym każdy
    /// język miałby pełny zestaw kluczy — angielskich.
    /// </remarks>
    private static int LiczKlucze(ResourceManager teksty, CultureInfo culture)
    {
        using ResourceSet? zestaw = teksty.GetResourceSet(culture, createIfNotExists: true, tryParents: false);

        return zestaw?.Cast<DictionaryEntry>().Count() ?? 0;
    }

    [Fact]
    public void SetCulture_UstawiaKultureWatkuDlaFormatowania()
    {
        // Bez tego interfejs byłby polski, ale liczby i daty formatowałyby się po angielsku.
        using TemporaryStorage storage = new();

        storage.Localization.SetLanguage("pl");

        Assert.Equal("pl", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        Assert.Equal("pl", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
    }
}
