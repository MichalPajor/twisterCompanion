using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Navigation;
using TwisterCompanion.Presentation.Tests.Fakes;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.Presentation.Tests;

/// <summary>
/// Testy nawigacji z ekranu startowego — sprawdzają, że komendy prowadzą
/// pod właściwe trasy.
/// </summary>
public class HomeViewModelTests
{
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly FakeSettingsService _settings = new();

    [Fact]
    public async Task GoToGameCommand_NawigujeNaEkranRozgrywki()
    {
        HomeViewModel viewModel = CreateViewModel();

        await viewModel.GoToGameCommand.ExecuteAsync(parameter: null);

        await _navigation.Received(1).GoToAsync(Routes.Game);
    }

    [Fact]
    public async Task GoToPlayersCommand_NawigujeNaEkranGraczy()
    {
        HomeViewModel viewModel = CreateViewModel();

        await viewModel.GoToPlayersCommand.ExecuteAsync(parameter: null);

        await _navigation.Received(1).GoToAsync(Routes.Players);
    }

    [Fact]
    public async Task GoToGameModesCommand_NawigujeNaEkranTrybow()
    {
        HomeViewModel viewModel = CreateViewModel();

        await viewModel.GoToGameModesCommand.ExecuteAsync(parameter: null);

        await _navigation.Received(1).GoToAsync(Routes.GameModes);
    }

    [Fact]
    public async Task GoToEventPacksCommand_NawigujeNaEkranPaczekWydarzen()
    {
        HomeViewModel viewModel = CreateViewModel();

        await viewModel.GoToEventPacksCommand.ExecuteAsync(parameter: null);

        await _navigation.Received(1).GoToAsync(Routes.EventPacks);
    }

    [Fact]
    public async Task GoToSettingsCommand_NawigujeNaEkranUstawien()
    {
        HomeViewModel viewModel = CreateViewModel();

        await viewModel.GoToSettingsCommand.ExecuteAsync(parameter: null);

        await _navigation.Received(1).GoToAsync(Routes.Settings);
    }

    [Fact]
    public async Task GoToHowToPlayCommand_NawigujeNaWprowadzenie()
    {
        HomeViewModel viewModel = CreateViewModel();

        await viewModel.GoToHowToPlayCommand.ExecuteAsync(parameter: null);

        await _navigation.Received(1).GoToAsync(Routes.Onboarding);
    }

    [Fact]
    public async Task PierwszeUruchomienie_ProwadziDoWprowadzenia()
    {
        // Wprowadzenie ma pokazać się samo, bez szukania w menu — ale tylko raz.
        HomeViewModel viewModel = CreateViewModel();

        await viewModel.InitializeAsync();

        await _navigation.Received(1).GoToAsync(Routes.Onboarding);
    }

    [Fact]
    public async Task KolejneUruchomienie_NieProwadziDoWprowadzenia()
    {
        await _settings.UpdateAsync(settings => settings with { HasSeenOnboarding = true });

        HomeViewModel viewModel = CreateViewModel();

        await viewModel.InitializeAsync();

        await _navigation.DidNotReceive().GoToAsync(Routes.Onboarding);
    }

    private HomeViewModel CreateViewModel() => new(
        _navigation,
        _settings,
        NullLogger<HomeViewModel>.Instance,
        Substitute.For<IDialogService>(),
        new FakeLocalizationService());
}
