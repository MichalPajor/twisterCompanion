using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.GameModes;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.GameModes;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Navigation;
using TwisterCompanion.Presentation.Tests.Fakes;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.Presentation.Tests;

/// <summary>
/// Testy ekranu wyboru trybu gry i ekranu zasad.
/// </summary>
public class GameModesViewModelTests
{
    private static readonly GameModeDefinition Classic = new()
    {
        Key = "classic",
        NameKey = "GameMode_Classic_Name",
        DescriptionKey = "GameMode_Classic_Description",
        RulesKey = "GameMode_Classic_Rules",
    };

    private static readonly GameModeDefinition Kids = new()
    {
        Key = "kids",
        NameKey = "GameMode_Kids_Name",
        DescriptionKey = "GameMode_Kids_Description",
        RulesKey = "GameMode_Kids_Rules",
        EliminationRule = EliminationRule.NoElimination,
    };

    private readonly IGameModeService _gameModes = Substitute.For<IGameModeService>();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();

    public GameModesViewModelTests()
    {
        _gameModes.GetAvailable().Returns([Classic, Kids]);
        _gameModes.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Classic));
        _gameModes.Find("kids").Returns(Kids);
        _gameModes.Find("classic").Returns(Classic);
    }

    [Fact]
    public async Task Inicjalizacja_PokazujeTrybyIZaznaczaObowiazujacy()
    {
        GameModesViewModel viewModel = CreateModesViewModel();

        await viewModel.InitializeAsync();

        Assert.Equal(2, viewModel.Modes.Count);
        Assert.False(viewModel.IsEmpty);

        GameModeListItem classic = viewModel.Modes.Single(mode => mode.Key == "classic");

        Assert.True(classic.IsActive);
        Assert.False(classic.IsNotActive);
        Assert.False(viewModel.Modes.Single(mode => mode.Key == "kids").IsActive);
    }

    [Fact]
    public async Task PustyKatalog_PokazujeKomunikat()
    {
        _gameModes.GetAvailable().Returns([]);

        GameModesViewModel viewModel = CreateModesViewModel();

        await viewModel.InitializeAsync();

        Assert.True(viewModel.IsEmpty);
        Assert.Empty(viewModel.Modes);
    }

    [Fact]
    public async Task WyborTrybu_ZapisujeGoIPrzestawiaZaznaczenie()
    {
        GameModesViewModel viewModel = CreateModesViewModel();
        await viewModel.InitializeAsync();

        GameModeListItem kids = viewModel.Modes.Single(mode => mode.Key == "kids");

        await viewModel.SelectModeCommand.ExecuteAsync(kids);

        await _gameModes.Received(1).SetActiveAsync("kids", Arg.Any<CancellationToken>());
        Assert.True(kids.IsActive);
        Assert.False(viewModel.Modes.Single(mode => mode.Key == "classic").IsActive);
    }

    [Fact]
    public async Task ZasadyOtwierajaSieDlaWskazanegoTrybu_NieDlaWybranego()
    {
        // Gracz czyta zasady, żeby zdecydować o wyborze — ekran musi umieć pokazać także
        // tryb, którego jeszcze nie wybrał.
        GameModesViewModel viewModel = CreateModesViewModel();
        await viewModel.InitializeAsync();

        await viewModel.GoToRulesCommand.ExecuteAsync(
            viewModel.Modes.Single(mode => mode.Key == "kids"));

        // Parametry sprawdzamy po przechwyceniu, a nie w wyrażeniu dopasowania: drzewo
        // wyrażeń nie przyjmuje deklaracji zmiennej wyjściowej, a indeksowanie słownika
        // wprost zwracałoby ostrzeżenie o możliwej wartości nullowalnej.
        IReadOnlyDictionary<string, object> przekazane = (IReadOnlyDictionary<string, object>)
            _navigation.ReceivedCalls()
                .Single(call => call.GetMethodInfo().GetParameters().Length == 2)
                .GetArguments()[1]!;

        Assert.Equal("kids", przekazane[Routes.Parameters.GameModeKey]);
    }

    [Fact]
    public async Task EkranZasad_PokazujeZasadyTrybuZParametru()
    {
        RulesViewModel viewModel = CreateRulesViewModel();

        viewModel.ApplyParameters(new Dictionary<string, object>
        {
            [Routes.Parameters.GameModeKey] = "kids",
        });

        await viewModel.InitializeAsync();

        Assert.Equal(Kids.NameKey, viewModel.ModeName);
        Assert.Equal(Kids.RulesKey, viewModel.RulesText);
    }

    [Fact]
    public async Task EkranZasadBezParametru_PokazujeTrybObowiazujacy()
    {
        RulesViewModel viewModel = CreateRulesViewModel();

        await viewModel.InitializeAsync();

        Assert.Equal(Classic.NameKey, viewModel.ModeName);
        Assert.Equal(Classic.RulesKey, viewModel.RulesText);
    }

    [Fact]
    public async Task EkranZasad_ZNieznanymTrybem_PokazujeTrybObowiazujacy()
    {
        RulesViewModel viewModel = CreateRulesViewModel();

        viewModel.ApplyParameters(new Dictionary<string, object>
        {
            [Routes.Parameters.GameModeKey] = "nie-ma-takiego",
        });

        await viewModel.InitializeAsync();

        Assert.Equal(Classic.NameKey, viewModel.ModeName);
    }

    private GameModesViewModel CreateModesViewModel() => new(
        _navigation,
        _gameModes,
        NullLogger<GameModesViewModel>.Instance,
        Substitute.For<IDialogService>(),
        new FakeLocalizationService());

    private RulesViewModel CreateRulesViewModel() => new(
        _navigation,
        _gameModes,
        NullLogger<RulesViewModel>.Instance,
        Substitute.For<IDialogService>(),
        new FakeLocalizationService());
}
