using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.Entities;
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

    [Fact]
    public async Task Rozgrywka_PrzyPustymSkladzie_PytaZamiastPrzechodzic()
    {
        // Ślepy zaułek zgłoszony z urządzenia: wejście w „Rozgrywkę" bez graczy dawało ekran
        // z nieaktywnym przyciskiem startu i żadnej wskazówki, czego brakuje.
        _playerRoster.GetAsync().Returns(Task.FromResult<IReadOnlyList<Player>>([]));
        _dialogs.ConfirmAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        HomeViewModel viewModel = CreateViewModel();

        await viewModel.GoToGameCommand.ExecuteAsync(null);

        await _navigation.DidNotReceive().GoToAsync(Routes.Game);
        await _navigation.DidNotReceive().GoToAsync(Routes.Players);
    }

    [Fact]
    public async Task Rozgrywka_PrzyPustymSkladzieIZgodzie_ProwadziDoGraczy()
    {
        _playerRoster.GetAsync().Returns(Task.FromResult<IReadOnlyList<Player>>([]));
        _dialogs.ConfirmAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);

        HomeViewModel viewModel = CreateViewModel();

        await viewModel.GoToGameCommand.ExecuteAsync(null);

        await _navigation.Received(1).GoToAsync(Routes.Players);
        await _navigation.DidNotReceive().GoToAsync(Routes.Game);
    }

    [Fact]
    public async Task Rozgrywka_GdySaGracze_PrzechodziBezPytania()
    {
        // Jeden gracz wystarczy: silnik startuje partię solo, tylko bez zwycięzcy. Pytanie
        // przy jednym graczu odbierałoby coś, co dziś działa.
        _playerRoster.GetAsync().Returns(Task.FromResult<IReadOnlyList<Player>>(
            [Player.Create("Kuba", 0)]));

        HomeViewModel viewModel = CreateViewModel();

        await viewModel.GoToGameCommand.ExecuteAsync(null);

        await _navigation.Received(1).GoToAsync(Routes.Game);
        await _dialogs.DidNotReceive().ConfirmAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    private readonly IPlayerRosterRepository _playerRoster = Substitute.For<IPlayerRosterRepository>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private HomeViewModel CreateViewModel() => new(
        _navigation,
        _settings,
        _playerRoster,
        NullLogger<HomeViewModel>.Instance,
        _dialogs,
        new FakeLocalizationService());
}
