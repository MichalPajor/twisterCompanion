using TwisterCompanion.Application.Settings;
using TwisterCompanion.Infrastructure.Persistence.Dto;

namespace TwisterCompanion.Infrastructure.Persistence.Mapping;

/// <summary>
/// Przekłada ustawienia między postacią zapisaną a modelem aplikacji.
/// </summary>
/// <remarks>
/// Wszystkie wartości liczbowe są przycinane do dopuszczalnych zakresów. Powód: model
/// <see cref="AppSettings"/> odrzuca wartości niemożliwe wyjątkiem, a ustawienia są
/// czytane przy starcie aplikacji — plik z ręcznie wpisaną głośnością 5,0 nie może
/// oznaczać, że aplikacja się nie uruchomi.
/// </remarks>
internal static class AppSettingsMapper
{
    /// <summary>Buduje ustawienia z odczytanego dokumentu, przycinając wartości do zakresów.</summary>
    /// <param name="dto">Odczytany dokument.</param>
    public static AppSettings ToDomain(AppSettingsDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // Starszy zapis miał jeden „odstęp między turami". Jeśli plik go zawiera, a nowego
        // pola nie, przenosimy wartość na czas ruchu — użytkownik nie ma powodu tracić
        // swojego ustawienia przy aktualizacji aplikacji.
        int moveSeconds = dto.MoveTimeSeconds
            ?? dto.AutoAdvanceIntervalSeconds
            ?? (int)AppSettings.Default.MoveTime.TotalSeconds;

        return new AppSettings
        {
            LanguageCode = string.IsNullOrWhiteSpace(dto.LanguageCode) ? null : dto.LanguageCode.Trim(),
            ThemePreference = Enum.IsDefined(dto.ThemePreference)
                ? dto.ThemePreference
                : AppThemePreference.System,
            IsTextToSpeechEnabled = dto.IsTextToSpeechEnabled,
            PreferredVoiceId = string.IsNullOrWhiteSpace(dto.PreferredVoiceId) ? null : dto.PreferredVoiceId,
            SpeechRate = Math.Clamp(dto.SpeechRate, AppSettings.MinSpeechRate, AppSettings.MaxSpeechRate),
            SpeechPitch = Math.Clamp(dto.SpeechPitch, AppSettings.MinSpeechPitch, AppSettings.MaxSpeechPitch),
            AreSoundsEnabled = dto.AreSoundsEnabled,
            SoundVolume = Math.Clamp(dto.SoundVolume, 0.0, 1.0),
            AreHapticsEnabled = dto.AreHapticsEnabled,
            AreAnimationsEnabled = dto.AreAnimationsEnabled,
            HasSeenOnboarding = dto.HasSeenOnboarding,
            FinishedGamesCount = dto.FinishedGamesCount,
            IsVoiceControlEnabled = dto.IsVoiceControlEnabled,
            VoiceListeningDelay = Clamp(
                TimeSpan.FromSeconds(dto.VoiceListeningDelaySeconds),
                AppSettings.MinVoiceListeningDelay,
                AppSettings.MaxVoiceListeningDelay),
            TurnAdvanceMode = dto.TurnAdvanceMode,
            MoveTime = Clamp(
                TimeSpan.FromSeconds(moveSeconds),
                AppSettings.MinMoveTime,
                AppSettings.MaxMoveTime),
            TaskTime = Clamp(
                TimeSpan.FromSeconds(dto.TaskTimeSeconds ?? (int)AppSettings.Default.TaskTime.TotalSeconds),
                AppSettings.MinTaskTime,
                AppSettings.MaxTaskTime),
            GameModeKey = string.IsNullOrWhiteSpace(dto.GameModeKey)
                ? AppSettings.Default.GameModeKey
                : dto.GameModeKey.Trim(),
            ActiveEventPackId = dto.ActiveEventPackId == Guid.Empty ? null : dto.ActiveEventPackId,
        };
    }

    /// <summary>Buduje dokument do zapisu na podstawie ustawień.</summary>
    /// <param name="settings">Ustawienia do zapisania.</param>
    public static AppSettingsDto ToDto(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new AppSettingsDto
        {
            SchemaVersion = PersistenceSchema.CurrentVersion,
            LanguageCode = settings.LanguageCode,
            ThemePreference = settings.ThemePreference,
            IsTextToSpeechEnabled = settings.IsTextToSpeechEnabled,
            PreferredVoiceId = settings.PreferredVoiceId,
            SpeechRate = settings.SpeechRate,
            SpeechPitch = settings.SpeechPitch,
            AreSoundsEnabled = settings.AreSoundsEnabled,
            SoundVolume = settings.SoundVolume,
            AreHapticsEnabled = settings.AreHapticsEnabled,
            AreAnimationsEnabled = settings.AreAnimationsEnabled,
            HasSeenOnboarding = settings.HasSeenOnboarding,
            FinishedGamesCount = settings.FinishedGamesCount,
            IsVoiceControlEnabled = settings.IsVoiceControlEnabled,
            VoiceListeningDelaySeconds = (int)settings.VoiceListeningDelay.TotalSeconds,
            TurnAdvanceMode = settings.TurnAdvanceMode,
            MoveTimeSeconds = (int)settings.MoveTime.TotalSeconds,
            TaskTimeSeconds = (int)settings.TaskTime.TotalSeconds,
            GameModeKey = settings.GameModeKey,
            ActiveEventPackId = settings.ActiveEventPackId,
        };
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan minimum, TimeSpan maximum) =>
        value < minimum ? minimum : value > maximum ? maximum : value;
}
