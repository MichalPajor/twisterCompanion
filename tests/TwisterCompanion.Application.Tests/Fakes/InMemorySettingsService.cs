using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Settings;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Ustawienia trzymane w pamięci, bez zapisu na dysk.
/// </summary>
internal sealed class InMemorySettingsService : ISettingsService
{
    /// <inheritdoc />
    public AppSettings Current { get; private set; } = AppSettings.Default;

    /// <inheritdoc />
    public event EventHandler<AppSettings>? Changed;

    /// <inheritdoc />
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task UpdateAsync(
        Func<AppSettings, AppSettings> change,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);

        Current = change(Current);
        Changed?.Invoke(this, Current);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) =>
        UpdateAsync(_ => AppSettings.Default, cancellationToken);
}
