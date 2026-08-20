using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Wibracje zastępcze — zapisują, o jaką siłę poproszono.
/// </summary>
internal sealed class FakeHapticService : IHapticService
{
    private readonly List<HapticIntensity> _vibrations = [];

    /// <summary>Wywołane wibracje, w kolejności.</summary>
    public IReadOnlyList<HapticIntensity> Vibrations => _vibrations;

    /// <inheritdoc />
    public void Vibrate(HapticIntensity intensity) => _vibrations.Add(intensity);
}
