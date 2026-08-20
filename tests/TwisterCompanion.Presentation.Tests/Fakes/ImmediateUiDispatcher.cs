using TwisterCompanion.Presentation.Abstractions;

namespace TwisterCompanion.Presentation.Tests.Fakes;

/// <summary>
/// Wykonuje działania od razu, w wątku wywołującego.
/// </summary>
/// <remarks>
/// W testach nie ma wątku interfejsu, a przenoszenie wykonania utrudniałoby sprawdzanie
/// skutków — wszystko dzieje się synchronicznie.
/// </remarks>
internal sealed class ImmediateUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        action();
    }
}
