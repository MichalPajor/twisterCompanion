using System.Globalization;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Presentation.Tests.Fakes;

/// <summary>
/// Rozpoznawanie mowy zastępcze — sesje kończy test, nie urządzenie.
/// </summary>
internal sealed class FakeSpeechRecognitionService : ISpeechRecognitionService
{
    private readonly List<SpeechRecognitionRequest> _startedSessions = [];

    /// <summary>Sesje, o które poproszono, w kolejności.</summary>
    public IReadOnlyList<SpeechRecognitionRequest> StartedSessions => _startedSessions;

    /// <summary>Ile razy zamknięto nasłuch.</summary>
    public int StopCount { get; private set; }

    /// <summary>Czy zgoda na mikrofon ma być przyznana.</summary>
    public bool IsPermissionGranted { get; set; } = true;

    /// <summary>Możliwości zgłaszane przez „urządzenie".</summary>
    public SpeechRecognitionCapabilities Capabilities { get; set; } =
        new(IsSystemRecognitionAvailable: true, IsOnDeviceRecognitionAvailable: true, "Test");

    /// <inheritdoc />
    public bool IsListening { get; private set; }

    /// <inheritdoc />
    public event EventHandler<string>? PartialRecognized;

    /// <inheritdoc />
    public event EventHandler<SpeechRecognitionOutcome>? SessionCompleted;

    /// <inheritdoc />
    public Task<SpeechRecognitionCapabilities> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default) => Task.FromResult(Capabilities);

    /// <inheritdoc />
    public Task<bool> RequestPermissionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(IsPermissionGranted);

    /// <inheritdoc />
    public Task StartAsync(
        SpeechRecognitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _startedSessions.Add(request);
        IsListening = true;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        StopCount++;
        IsListening = false;

        return Task.CompletedTask;
    }

    /// <summary>Udaje częściowe rozpoznanie w trwającej sesji.</summary>
    /// <param name="text">Rozpoznany fragment.</param>
    public void RaisePartial(string text) => PartialRecognized?.Invoke(this, text);

    /// <summary>Zamyka sesję rozpoznaniem.</summary>
    /// <param name="text">Rozpoznany tekst.</param>
    public void CompleteWith(string text)
    {
        IsListening = false;
        SessionCompleted?.Invoke(this, new SpeechRecognitionOutcome(text));
    }

    /// <summary>Zamyka sesję błędem.</summary>
    /// <param name="error">Rodzaj błędu.</param>
    /// <param name="details">Komunikat platformy.</param>
    public void CompleteWithError(SpeechRecognitionError error, string? details = null)
    {
        IsListening = false;
        SessionCompleted?.Invoke(this, new SpeechRecognitionOutcome(null, error, details));
    }

    /// <summary>Kultura używana w testach.</summary>
    public static CultureInfo TestCulture { get; } = CultureInfo.GetCultureInfo("pl");
}
