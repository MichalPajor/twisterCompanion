using TwisterCompanion.Presentation.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Przeniesienie na wątek interfejsu przez mechanizm MAUI.
/// </summary>
internal sealed class MauiUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (MainThread.IsMainThread)
        {
            action();

            return;
        }

        MainThread.BeginInvokeOnMainThread(action);
    }
}
