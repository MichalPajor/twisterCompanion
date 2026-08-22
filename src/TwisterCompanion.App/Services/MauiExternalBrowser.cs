using TwisterCompanion.Presentation.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Otwieranie adresów przeglądarką systemową, na MAUI.
/// </summary>
/// <remarks>
/// <see cref="BrowserLaunchMode.SystemPreferred"/>, a nie <c>External</c>: na Androidzie
/// otwiera kartę wewnątrz aplikacji (Custom Tab), więc użytkownik wraca do gry przyciskiem
/// wstecz, zamiast lądować w osobnej przeglądarce i szukać drogi powrotnej.
/// </remarks>
internal sealed class MauiExternalBrowser : IExternalBrowser
{
    /// <inheritdoc />
    public async Task<bool> OpenAsync(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);

        return await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
    }
}
