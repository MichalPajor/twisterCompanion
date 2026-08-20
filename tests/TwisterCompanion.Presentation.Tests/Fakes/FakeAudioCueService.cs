using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Presentation.Tests.Fakes;

/// <summary>
/// Sygnały dźwiękowe zastępcze — zapisują, co i w jakiej kolejności zabrzmiało.
/// </summary>
internal sealed class FakeAudioCueService : IAudioCueService
{
    private readonly List<AudioCue> _played = [];

    /// <summary>Odtworzone sygnały, w kolejności.</summary>
    public IReadOnlyList<AudioCue> Played => _played;

    /// <inheritdoc />
    public Task PlayAsync(AudioCue cue, CancellationToken cancellationToken = default)
    {
        _played.Add(cue);

        return Task.CompletedTask;
    }
}
