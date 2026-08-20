using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.Presentation.Tests.Fakes;

/// <summary>
/// Minimalny ViewModel udostępniający chronione mechanizmy <see cref="ViewModelBase"/>
/// do testów.
/// </summary>
internal sealed class TestViewModel(
    ILogger logger,
    IDialogService dialogService,
    ILocalizationService localization)
    : ViewModelBase(logger, dialogService, localization)
{
    /// <summary>Operacja wykonywana przy inicjalizacji ekranu.</summary>
    public Func<Task>? InitializeBehavior { get; set; }

    /// <summary>Udostępnia <c>ExecuteSafeAsync</c> testom.</summary>
    /// <param name="operation">Operacja do wykonania.</param>
    /// <param name="errorTitleKey">Klucz tytułu komunikatu o błędzie.</param>
    public Task RunAsync(Func<Task> operation, string? errorTitleKey = null) =>
        ExecuteSafeAsync(operation, errorTitleKey);

    /// <inheritdoc />
    protected override Task OnInitializeAsync() =>
        InitializeBehavior?.Invoke() ?? Task.CompletedTask;
}
