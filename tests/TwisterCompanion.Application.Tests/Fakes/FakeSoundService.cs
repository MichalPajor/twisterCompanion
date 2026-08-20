using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Odtwarzacz efektów zastępczy — zapisuje, co i jak głośno miało zabrzmieć.
/// </summary>
internal sealed class FakeSoundService : ISoundService
{
    private readonly List<(SoundEffect Effect, double Volume)> _played = [];

    /// <summary>Odtworzone efekty razem z głośnością, w kolejności.</summary>
    public IReadOnlyList<(SoundEffect Effect, double Volume)> Played => _played;

    /// <summary>Ile razy wczytywano próbki.</summary>
    public int PreloadCount { get; private set; }

    /// <summary>Wyjątek zgłaszany przy odtwarzaniu — do testów awarii.</summary>
    public Exception? FailWith { get; set; }

    /// <summary>Wyjątek zgłaszany przy wczytywaniu — do testów awarii.</summary>
    public Exception? FailPreloadWith { get; set; }

    /// <inheritdoc />
    public Task PreloadAsync(CancellationToken cancellationToken = default)
    {
        PreloadCount++;

        return FailPreloadWith is null ? Task.CompletedTask : Task.FromException(FailPreloadWith);
    }

    /// <inheritdoc />
    public void Play(SoundEffect effect, double volume)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        _played.Add((effect, volume));
    }
}
