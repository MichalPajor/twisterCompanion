using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Presentation.Abstractions;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Podstawa ViewModeli ekranów, do których się nawiguje — dokłada dostęp do nawigacji
/// i komendę powrotu.
/// </summary>
/// <remarks>
/// Istnieje, żeby nie powtarzać tej samej komendy powrotu w każdym ekranie.
/// </remarks>
public abstract partial class NavigableViewModelBase : ViewModelBase
{
    /// <summary>Tworzy ViewModel ekranu z dostępem do nawigacji.</summary>
    /// <param name="navigation">Serwis nawigacji.</param>
    /// <param name="logger">Logger konkretnego ViewModelu.</param>
    /// <param name="dialogService">Serwis komunikatów dla użytkownika.</param>
    /// <param name="localization">Serwis tłumaczeń.</param>
    protected NavigableViewModelBase(
        INavigationService navigation,
        ILogger logger,
        IDialogService dialogService,
        ILocalizationService localization)
        : base(logger, dialogService, localization)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        Navigation = navigation;
    }

    /// <summary>Nawigacja dostępna dla klas pochodnych.</summary>
    protected INavigationService Navigation { get; }

    /// <summary>Wraca do poprzedniego ekranu.</summary>
    [RelayCommand]
    private Task GoBackAsync() => ExecuteSafeAsync(Navigation.GoBackAsync);
}
