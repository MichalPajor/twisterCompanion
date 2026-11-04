using Microsoft.Extensions.Logging;

namespace TwisterCompanion.Application.AppReview;

/// <summary>
/// Okienko oceny nieobecne — implementacja domyślna.
/// </summary>
/// <remarks>
/// Obowiązuje w testach, w buildach deweloperskich i na platformach bez integracji ze
/// sklepem. Okienko Google Play i tak nie pokazuje się w aplikacji uruchomionej spoza sklepu,
/// więc wersja zastępcza nie ukrywa niczego, co dałoby się tam zobaczyć.
/// </remarks>
internal sealed class NoOpReviewPlatform(ILogger<NoOpReviewPlatform> logger) : IReviewPlatform
{
    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public Task RequestReviewAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Prośba o ocenę pominięta — brak integracji ze sklepem w tym wydaniu.");

        return Task.CompletedTask;
    }
}
