using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Syntezator mowy zastępczy — zapisuje wypowiedzi, zamiast je wypowiadać.
/// </summary>
internal sealed class FakeTextToSpeechService : ITextToSpeechService
{
    private readonly List<string> _spoken = [];
    private readonly Lock _guard = new();

    /// <summary>Teksty przekazane do wypowiedzenia, w kolejności.</summary>
    /// <remarks>
    /// Zwraca kopię pod blokadą: testy przepływu odpytują tę listę z wątku testu, kiedy
    /// wypowiedź trwa jeszcze na innym, a odczyt kolekcji w trakcie dopisywania
    /// jest niezdefiniowany.
    /// </remarks>
    public IReadOnlyList<string> Spoken
    {
        get
        {
            lock (_guard)
            {
                return [.. _spoken];
            }
        }
    }

    /// <summary>Parametry ostatniej wypowiedzi.</summary>
    public SpeechRequest? LastRequest { get; private set; }

    /// <summary>Ile razy przerwano wypowiedź.</summary>
    public int StopCount { get; private set; }

    /// <summary>Wyjątek zgłaszany przy próbie wypowiedzenia — do testów awarii.</summary>
    public Exception? FailWith { get; set; }

    /// <summary>Zadanie, na które czeka wypowiedź — pozwala testowi ją „zatrzymać".</summary>
    public TaskCompletionSource? Gate { get; set; }

    /// <summary>Ile razy proszono o przygotowanie syntezatora.</summary>
    public int PrepareCalls { get; private set; }

    /// <inheritdoc />
    public Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        PrepareCalls++;

        return FailWith is not null ? Task.FromException(FailWith) : Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SpeechVoice>> GetVoicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpeechVoice>>(
        [
            new SpeechVoice("pl||Polski", "Polski", "pl"),
            new SpeechVoice("en||English", "English", "en"),
        ]);

    /// <inheritdoc />
    public async Task SpeakAsync(
        string text,
        SpeechRequest request,
        CancellationToken cancellationToken = default)
    {
        LastRequest = request;

        if (FailWith is not null)
        {
            throw FailWith;
        }

        if (Gate is not null)
        {
            await Gate.Task.WaitAsync(cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_guard)
        {
            _spoken.Add(text);
        }
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        StopCount++;

        return Task.CompletedTask;
    }
}
