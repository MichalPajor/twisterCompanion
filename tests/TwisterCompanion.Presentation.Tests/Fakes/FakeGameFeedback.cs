using TwisterCompanion.Application.Feedback;

namespace TwisterCompanion.Presentation.Tests.Fakes;

/// <summary>
/// Reakcje dźwiękowe zastępcze — zapamiętują, o co je poproszono.
/// </summary>
/// <remarks>
/// Test warstwy prezentacji sprawdza, czy ekran <b>zgłasza właściwe zdarzenie</b>, a nie czy
/// coś zabrzmiało: o tym, czy wolno zagrać, decyduje serwis w warstwie aplikacji i on ma
/// własne testy.
/// </remarks>
internal sealed class FakeGameFeedback : IGameFeedback
{
    /// <summary>Zgłoszone zdarzenia, w kolejności zgłoszenia.</summary>
    public List<FeedbackMoment> Moments { get; } = [];

    /// <summary>Ile razy poproszono o wczytanie próbek.</summary>
    public int PreloadCount { get; private set; }

    /// <inheritdoc />
    public Task PreloadAsync(CancellationToken cancellationToken = default)
    {
        PreloadCount++;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Play(FeedbackMoment moment) => Moments.Add(moment);
}
