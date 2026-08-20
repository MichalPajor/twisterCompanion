using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Wibracja urządzenia przez systemowe API MAUI.
/// </summary>
/// <remarks>
/// Dwie siły, nie pięć: MAUI odsłania krótkie stuknięcie i długie przytrzymanie, a więcej
/// stopni i tak nie dałoby się rozróżnić przez telefon leżący na podłodze.
/// <para>
/// Awarie są pochłaniane: część urządzeń nie ma silnika wibracji, a część odmawia bez
/// dodatkowej zgody. Ani jedno, ani drugie nie może przerwać partii.
/// </para>
/// </remarks>
internal sealed class HapticService : IHapticService
{
    private readonly ILogger<HapticService> _logger;

    /// <summary>Tworzy serwis wibracji.</summary>
    /// <param name="logger">Logger.</param>
    public HapticService(ILogger<HapticService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    /// <inheritdoc />
    public void Vibrate(HapticIntensity intensity)
    {
        try
        {
            HapticFeedback.Default.Perform(intensity == HapticIntensity.Strong
                ? HapticFeedbackType.LongPress
                : HapticFeedbackType.Click);
        }
        catch (Exception exception)
        {
            // FeatureNotSupportedException na urządzeniach bez wibracji — to nie jest błąd
            // aplikacji, tylko cecha sprzętu.
            _logger.LogDebug(exception, "Wibracja {Intensity} nie jest dostępna.", intensity);
        }
    }
}
