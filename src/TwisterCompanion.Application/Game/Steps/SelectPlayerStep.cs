namespace TwisterCompanion.Application.Game.Steps;

/// <summary>
/// Wskazuje gracza, którego jest tura.
/// </summary>
internal sealed class SelectPlayerStep : ITurnPipelineStep
{
    /// <inheritdoc />
    public Task ExecuteAsync(TurnContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        context.Player = context.Session.SelectNextPlayer();

        return Task.CompletedTask;
    }
}
