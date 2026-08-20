using Microsoft.Extensions.Logging;

namespace TwisterCompanion.Presentation.Tests.Fakes;

/// <summary>
/// Logger, który zapamiętuje wpisy, żeby test mógł sprawdzić, co zostało zalogowane.
/// </summary>
/// <remarks>
/// Napisany ręcznie zamiast atrapy NSubstitute, bo <c>LogError</c> i pokrewne są
/// metodami rozszerzającymi — nie da się ich przechwycić atrapą, a weryfikacja
/// surowego <c>Log(...)</c> jest nieczytelna.
/// </remarks>
/// <typeparam name="T">Kategoria loggera.</typeparam>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _entries = [];

    /// <summary>Zapisane wpisy, w kolejności zgłoszenia.</summary>
    public IReadOnlyList<LogEntry> Entries => _entries;

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => NoOpScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        _entries.Add(new LogEntry(logLevel, exception, formatter(state, exception)));
    }

    /// <summary>Pojedynczy zapisany wpis.</summary>
    /// <param name="Level">Poziom zgłoszenia.</param>
    /// <param name="Exception">Wyjątek dołączony do wpisu, jeśli był.</param>
    /// <param name="Message">Sformatowana treść.</param>
    internal sealed record LogEntry(LogLevel Level, Exception? Exception, string Message);

    private sealed class NoOpScope : IDisposable
    {
        public static NoOpScope Instance { get; } = new();

        public void Dispose()
        {
            // Zakresy logowania nie są w testach potrzebne.
        }
    }
}
