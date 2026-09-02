using Microsoft.Extensions.Logging;
using TwisterCompanion.Presentation.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Otwieranie ekranów ustawień systemu na Androidzie.
/// </summary>
internal sealed class MauiSystemSettings(ILogger<MauiSystemSettings> logger) : ISystemSettings
{
    private readonly ILogger<MauiSystemSettings> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public Task<bool> OpenPrivacySettingsAsync()
    {
#if ANDROID
        try
        {
            // NewTask jest wymagane: intencja startuje z kontekstu aplikacji, a nie z aktywności.
            using Android.Content.Intent intencja = new(
                Android.Provider.Settings.ActionPrivacySettings);

            intencja.AddFlags(Android.Content.ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intencja);

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            // Producenci przenoszą i usuwają ekrany ustawień. Nieudane otwarcie nie może
            // wywalić gry — gracz ma w komunikacie drugą drogę, przez szybkie ustawienia.
            _logger.LogWarning(exception, "Nie udało się otworzyć ustawień prywatności.");

            return Task.FromResult(false);
        }
#else
        return Task.FromResult(false);
#endif
    }
}
