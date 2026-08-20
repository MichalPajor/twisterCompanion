using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Navigation;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Implementacja nawigacji oparta na <see cref="Shell"/>.
/// </summary>
/// <remarks>
/// Jedyne miejsce w aplikacji, które zna <see cref="Shell"/>. Każde wywołanie jest
/// kierowane na wątek UI, bo komendy mogą zostać wywołane z wątku tła — na przykład
/// z rozpoznawania mowy (Etap 8).
/// </remarks>
internal sealed class ShellNavigationService : INavigationService
{
    /// <inheritdoc />
    public Task GoToAsync(string route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route);

        return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route));
    }

    /// <inheritdoc />
    public Task GoToAsync(string route, IReadOnlyDictionary<string, object> parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route);
        ArgumentNullException.ThrowIfNull(parameters);

        Dictionary<string, object> shellParameters = new(parameters);

        return MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route, shellParameters));
    }

    /// <inheritdoc />
    public Task GoBackAsync() =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(".."));

    /// <inheritdoc />
    public Task GoToRootAsync() =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(Routes.Home));
}
