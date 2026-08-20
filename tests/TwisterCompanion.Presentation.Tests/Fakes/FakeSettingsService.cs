using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Settings;

namespace TwisterCompanion.Presentation.Tests.Fakes;

/// <summary>
/// Ustawienia trzymane w pamięci, bez zapisu na dysk.
/// </summary>
internal sealed class FakeSettingsService : ISettingsService
{
    /// <inheritdoc />
    public AppSettings Current { get; private set; } = AppSettings.Default;

    /// <inheritdoc />
    public event EventHandler<AppSettings>? Changed;

    /// <summary>Ile razy zapisano zmianę ustawień.</summary>
    public int UpdateCount { get; private set; }

    /// <inheritdoc />
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task UpdateAsync(
        Func<AppSettings, AppSettings> change,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);

        Current = change(Current);
        UpdateCount++;
        Changed?.Invoke(this, Current);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) =>
        UpdateAsync(_ => AppSettings.Default, cancellationToken);
}
