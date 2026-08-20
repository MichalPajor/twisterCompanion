using TwisterCompanion.Application.Advertising;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Reklamy zastępcze — zapisują, o co je poproszono, zamiast cokolwiek pokazywać.
/// </summary>
internal sealed class FakeAdPlatform : IAdPlatform
{
    /// <summary>Ile razy poproszono o reklamę pełnoekranową.</summary>
    public int InterstitialRequests { get; private set; }

    /// <summary>Ile razy przygotowywano reklamy.</summary>
    public int PrepareCalls { get; private set; }

    /// <inheritdoc />
    public bool IsAvailable { get; set; } = true;

    /// <summary>Czy przygotowanie ma zakończyć się zgodą na żądanie reklam.</summary>
    public bool CanRequestAds { get; set; } = true;

    /// <summary>Czy pokazanie reklamy ma się udać.</summary>
    public bool ShowSucceeds { get; set; } = true;

    /// <inheritdoc />
    public Task<bool> PrepareAsync(CancellationToken cancellationToken = default)
    {
        PrepareCalls++;

        return Task.FromResult(IsAvailable && CanRequestAds);
    }

    /// <inheritdoc />
    public Task<bool> ShowInterstitialAsync(CancellationToken cancellationToken = default)
    {
        InterstitialRequests++;

        return Task.FromResult(ShowSucceeds);
    }
}
