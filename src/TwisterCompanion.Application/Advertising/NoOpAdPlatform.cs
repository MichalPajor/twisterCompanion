using Microsoft.Extensions.Logging;

namespace TwisterCompanion.Application.Advertising;

/// <summary>
/// Reklamy nieobecne — implementacja domyślna.
/// </summary>
/// <remarks>
/// Obowiązuje wszędzie, gdzie nie ma integracji z zestawem SDK reklam: w testach, na
/// platformach bez wsparcia i w buildach deweloperskich. Jest domyślna świadomie — dzięki
/// temu wyłączenie modułu reklam nie wymaga zmiany w żadnym ekranie ani w żadnej regule.
/// </remarks>
internal sealed class NoOpAdPlatform(ILogger<NoOpAdPlatform> logger) : IAdPlatform
{
    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public Task<bool> PrepareAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    /// <inheritdoc />
    public Task<bool> ShowInterstitialAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Reklama pełnoekranowa pominięta — w tym wydaniu nie ma reklam.");

        return Task.FromResult(false);
    }
}
