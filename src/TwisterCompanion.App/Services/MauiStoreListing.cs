using Microsoft.Extensions.Logging;
using TwisterCompanion.Presentation.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Karta aplikacji w Sklepie Play, na MAUI.
/// </summary>
/// <remarks>
/// Najpierw adres <c>market://</c>, który otwiera aplikację Sklep Play — tam ocenę wystawia
/// się jednym dotknięciem. Gdy na urządzeniu nie ma sklepu (emulator bez usług Google,
/// telefony bez Google Play), zostaje strona karty w przeglądarce. Identyfikator pakietu
/// pochodzi z systemu, więc zgadza się z tym, co faktycznie zainstalowano.
/// </remarks>
internal sealed class MauiStoreListing(ILogger<MauiStoreListing> logger) : IStoreListing
{
    /// <inheritdoc />
    public async Task<bool> OpenAsync()
    {
        string packageName = AppInfo.Current.PackageName;

        try
        {
            if (await Launcher.Default.OpenAsync(new Uri($"market://details?id={packageName}")))
            {
                return true;
            }
        }
        catch (Exception exception)
        {
            logger.LogInformation(exception, "Brak aplikacji sklepu — otwieram kartę w przeglądarce.");
        }

        return await Browser.Default.OpenAsync(
            new Uri($"https://play.google.com/store/apps/details?id={packageName}"),
            BrowserLaunchMode.External);
    }
}
