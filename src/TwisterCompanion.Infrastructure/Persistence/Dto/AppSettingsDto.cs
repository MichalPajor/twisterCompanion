using TwisterCompanion.Application.Settings;

namespace TwisterCompanion.Infrastructure.Persistence.Dto;

/// <summary>
/// Postać ustawień zapisywana w pliku JSON.
/// </summary>
/// <remarks>
/// Odstęp między turami jest trzymany w sekundach, a nie jako <see cref="TimeSpan"/> —
/// plik ma być czytelny dla człowieka, a domyślny zapis <see cref="TimeSpan"/>
/// (<c>"00:00:08"</c>) tego nie ułatwia.
/// </remarks>
internal sealed class AppSettingsDto
{
    /// <summary>Wersja schematu dokumentu.</summary>
    public int SchemaVersion { get; set; } = PersistenceSchema.CurrentVersion;

    /// <summary>Kod języka interfejsu; <see langword="null"/> oznacza język systemu.</summary>
    public string? LanguageCode { get; set; }

    /// <summary>Wybrany motyw kolorystyczny.</summary>
    public AppThemePreference ThemePreference { get; set; } = AppThemePreference.System;

    /// <summary>Czy odczyt głosowy jest włączony.</summary>
    public bool IsTextToSpeechEnabled { get; set; } = true;

    /// <summary>Identyfikator wybranego głosu.</summary>
    public string? PreferredVoiceId { get; set; }

    /// <summary>Tempo mowy.</summary>
    public float SpeechRate { get; set; } = 1.0f;

    /// <summary>Wysokość głosu.</summary>
    public float SpeechPitch { get; set; } = 1.0f;

    /// <summary>Czy efekty dźwiękowe są włączone.</summary>
    public bool AreSoundsEnabled { get; set; } = true;

    /// <summary>Głośność efektów z zakresu 0,0–1,0.</summary>
    public double SoundVolume { get; set; } = 0.8;

    /// <summary>Czy wibracje są włączone.</summary>
    public bool AreHapticsEnabled { get; set; } = true;

    /// <summary>Czy wprowadzenie „Jak grać" zostało już pokazane.</summary>
    public bool HasSeenOnboarding { get; set; }

    /// <summary>Ile partii zakończono od zainstalowania aplikacji.</summary>
    public int FinishedGamesCount { get; set; }

    /// <summary>Czy animacje interfejsu są włączone.</summary>
    public bool AreAnimationsEnabled { get; set; } = true;

    /// <summary>Czy sterowanie głosem jest włączone.</summary>
    public bool IsVoiceControlEnabled { get; set; }

    /// <summary>Czas na wykonanie ruchu przed otwarciem nasłuchu, w sekundach.</summary>
    public int VoiceListeningDelaySeconds { get; set; } = 10;

    /// <summary>Sposób przechodzenia do następnej tury.</summary>
    public TurnAdvanceMode TurnAdvanceMode { get; set; } = TurnAdvanceMode.Manual;

    /// <summary>
    /// Czas na wykonanie ruchu, w sekundach.
    /// </summary>
    /// <remarks>
    /// Typ opcjonalny, żeby dało się odróżnić <b>brak wpisu</b> od wartości równej domyślnej.
    /// Bez tego starszy plik, w którym pola jeszcze nie było, byłby nieodróżnialny od pliku
    /// z wpisaną wartością domyślną — i wartość użytkownika z poprzedniej wersji przepadłaby.
    /// </remarks>
    public int? MoveTimeSeconds { get; set; }

    /// <summary>Czas na wykonanie zadania z wydarzenia, w sekundach.</summary>
    public int? TaskTimeSeconds { get; set; }

    /// <summary>
    /// Poprzednia nazwa czasu na ruch, czytana dla zgodności ze starszym zapisem.
    /// </summary>
    /// <remarks>
    /// Pole istnieje wyłącznie po to, żeby plik zapisany przed rozdzieleniem czasu na ruch
    /// i czasu na zadanie nie tracił ustawienia użytkownika. Nowe zapisy go nie używają.
    /// </remarks>
    public int? AutoAdvanceIntervalSeconds { get; set; }

    /// <summary>Klucz wybranego trybu gry.</summary>
    public string GameModeKey { get; set; } = "classic";

    /// <summary>Identyfikator aktywnej paczki wydarzeń.</summary>
    public Guid? ActiveEventPackId { get; set; }
}
