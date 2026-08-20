using TwisterCompanion.Presentation.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Implementacja komunikatów oparta na oknach dialogowych MAUI.
/// </summary>
/// <remarks>
/// Jedyne miejsce w aplikacji, które sięga po aktywną stronę. Wywołania są kierowane
/// na wątek UI, bo mogą przyjść z wątku tła.
/// </remarks>
internal sealed class MauiDialogService : IDialogService
{
    /// <inheritdoc />
    public Task AlertAsync(string title, string message, string cancel = "OK") =>
        MainThread.InvokeOnMainThreadAsync(() => CurrentPage.DisplayAlertAsync(title, message, cancel));

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        MainThread.InvokeOnMainThreadAsync(() => CurrentPage.DisplayAlertAsync(title, message, accept, cancel));

    /// <inheritdoc />
    public Task<string?> PromptAsync(
        string title,
        string message,
        string accept,
        string cancel,
        string? placeholder = null,
        string? initialValue = null) =>
        MainThread.InvokeOnMainThreadAsync(() => CurrentPage.DisplayPromptAsync(
            title,
            message,
            accept,
            cancel,
            placeholder,
            initialValue: initialValue ?? string.Empty))!;

    /// <summary>Strona aktywnego okna — właścicielka okien dialogowych.</summary>
    private static Page CurrentPage =>
        MauiControlsApplication.Current?.Windows.FirstOrDefault()?.Page
        ?? throw new InvalidOperationException(
            "Brak aktywnej strony — nie ma gdzie pokazać okna dialogowego.");
}
