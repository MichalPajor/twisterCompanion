using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Presentation.Tests.Fakes;

/// <summary>
/// Syntezator mowy zastępczy — zwraca ustaloną listę głosów.
/// </summary>
internal sealed class FakeTextToSpeechService : ITextToSpeechService
{
    /// <summary>Głosy, jakie zgłasza „urządzenie".</summary>
    public List<SpeechVoice> Voices { get; } =
    [
        new SpeechVoice("pl|PL|Zofia", "Zofia", "pl"),
        new SpeechVoice("en|US|Aria", "Aria", "en"),
    ];

    /// <summary>Wyjątek zgłaszany przy pobieraniu listy głosów — do testów awarii.</summary>
    public Exception? FailWith { get; set; }

    /// <summary>Ile razy proszono o przygotowanie syntezatora.</summary>
    public int PrepareCalls { get; private set; }

    /// <inheritdoc />
    public Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        PrepareCalls++;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SpeechVoice>> GetVoicesAsync(CancellationToken cancellationToken = default) =>
        FailWith is not null
            ? Task.FromException<IReadOnlyList<SpeechVoice>>(FailWith)
            : Task.FromResult<IReadOnlyList<SpeechVoice>>([.. Voices]);

    /// <inheritdoc />
    public Task SpeakAsync(
        string text,
        SpeechRequest request,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAsync() => Task.CompletedTask;
}
