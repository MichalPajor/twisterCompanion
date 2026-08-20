using TwisterCompanion.Application.VoiceControl;

namespace TwisterCompanion.Presentation.Tests.Fakes;

/// <summary>
/// Nasłuch komend zastępczy — stan i komendy podaje test.
/// </summary>
internal sealed class FakeVoiceControlService : IVoiceControlService
{
    /// <summary>Co ma zwrócić przygotowanie nasłuchu.</summary>
    public bool CanPrepare { get; set; } = true;

    /// <summary>Stan ustawiany po nieudanym przygotowaniu.</summary>
    public VoiceControlState StateAfterFailedPrepare { get; set; } = VoiceControlState.Disabled;

    /// <summary>Ile razy otwarto okno nasłuchu.</summary>
    public int OpenCount { get; private set; }

    /// <summary>Ile razy zamknięto okno nasłuchu.</summary>
    public int CloseCount { get; private set; }

    /// <inheritdoc />
    public VoiceControlState State { get; private set; } = VoiceControlState.Disabled;

    /// <inheritdoc />
    public event EventHandler<VoiceCommandType>? CommandRecognized;

    /// <inheritdoc />
    public event EventHandler<VoiceControlState>? StateChanged;

    /// <inheritdoc />
    public Task<bool> PrepareAsync(CancellationToken cancellationToken = default)
    {
        SetState(CanPrepare ? VoiceControlState.Idle : StateAfterFailedPrepare);

        return Task.FromResult(CanPrepare);
    }

    /// <inheritdoc />
    public Task OpenWindowAsync(CancellationToken cancellationToken = default)
    {
        OpenCount++;
        SetState(VoiceControlState.Listening);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CloseWindowAsync()
    {
        CloseCount++;
        SetState(VoiceControlState.Idle);

        return Task.CompletedTask;
    }

    /// <summary>Udaje rozpoznanie komendy.</summary>
    /// <param name="command">Rozpoznana komenda.</param>
    public void RaiseCommand(VoiceCommandType command) => CommandRecognized?.Invoke(this, command);

    /// <summary>Ustawia stan nasłuchu.</summary>
    /// <param name="state">Nowy stan.</param>
    public void SetState(VoiceControlState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke(this, state);
    }
}
