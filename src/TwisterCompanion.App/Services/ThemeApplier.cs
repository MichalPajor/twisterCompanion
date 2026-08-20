using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Settings;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Stosuje wybrany motyw kolorystyczny do aplikacji.
/// </summary>
/// <remarks>
/// Odpowiednik <c>ResxLocalizationService</c> dla wyglądu: ekran ustawień zapisuje wybór,
/// a ta klasa nasłuchuje zmian i przestawia motyw. Dzięki temu nie da się zmienić motywu
/// bez zapamiętania go ani zapamiętać bez zastosowania — a warstwa prezentacji nie musi
/// znać typów platformy.
/// </remarks>
internal sealed class ThemeApplier : IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ThemeApplier> _logger;

    private bool _disposed;

    /// <summary>Tworzy usługę stosującą motyw.</summary>
    /// <param name="settingsService">Ustawienia aplikacji.</param>
    /// <param name="logger">Logger.</param>
    public ThemeApplier(ISettingsService settingsService, ILogger<ThemeApplier> logger)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(logger);

        _settingsService = settingsService;
        _logger = logger;

        _settingsService.Changed += OnSettingsChanged;
    }

    /// <summary>Stosuje motyw zapisany w ustawieniach.</summary>
    public void Apply() => Apply(_settingsService.Current.ThemePreference);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _settingsService.Changed -= OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, AppSettings settings) =>
        Apply(settings.ThemePreference);

    private void Apply(AppThemePreference preference)
    {
        // Motyw wolno zmieniać tylko z wątku interfejsu, a zmiana ustawień może przyjść
        // z dowolnego — zapis pliku dzieje się poza wątkiem interfejsu.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                MauiControlsApplication app = MauiControlsApplication.Current
                    ?? throw new InvalidOperationException("Aplikacja nie jest jeszcze gotowa.");

                app.UserAppTheme = preference switch
                {
                    AppThemePreference.Light => AppTheme.Light,
                    AppThemePreference.Dark => AppTheme.Dark,

                    // „Jak w systemie" to w MAUI brak własnego wyboru — wtedy obowiązuje
                    // motyw urządzenia i zmienia się razem z nim, bez restartu aplikacji.
                    _ => AppTheme.Unspecified,
                };
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Nie udało się zastosować motywu {Theme}.", preference);
            }
        });
    }
}
