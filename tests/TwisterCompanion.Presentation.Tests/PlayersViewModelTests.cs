using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Tests.Fakes;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.Presentation.Tests;

/// <summary>
/// Testy ekranu graczy.
/// </summary>
public class PlayersViewModelTests
{
    private readonly IPlayerRosterRepository _roster = Substitute.For<IPlayerRosterRepository>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    public PlayersViewModelTests()
    {
        _roster.GetAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<Player>>([]));

        // Domyślnie potwierdzamy usunięcie — testy odmowy ustawiają to same.
        _dialogs.ConfirmAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(Task.FromResult(true));
    }

    [Fact]
    public async Task InitializeAsync_WczytujeZapamietanySklad()
    {
        _roster.GetAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<Player>>(
            [Player.Create("Kuba", 0), Player.Create("Anna", 1)]));

        PlayersViewModel viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        Assert.Equal(["Kuba", "Anna"], viewModel.Players.Select(player => player.Name));
        Assert.False(viewModel.HasNoPlayers);
    }

    [Fact]
    public async Task AddPlayerCommand_DodajeGraczaIZapisujeSklad()
    {
        PlayersViewModel viewModel = CreateViewModel();
        viewModel.NewPlayerName = "Kuba";

        await viewModel.AddPlayerCommand.ExecuteAsync(parameter: null);

        Assert.Equal("Kuba", Assert.Single(viewModel.Players).Name);
        Assert.Empty(viewModel.NewPlayerName ?? string.Empty);
        await _roster.Received(1).SaveAsync(
            Arg.Is<IReadOnlyList<Player>>(players => players != null && players.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AddPlayerCommand_PrzyPustejNazwie_JestNieaktywna()
    {
        PlayersViewModel viewModel = CreateViewModel();

        viewModel.NewPlayerName = "   ";

        Assert.False(viewModel.AddPlayerCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddPlayerCommand_NadajeKolejnePozycje()
    {
        PlayersViewModel viewModel = await CreateWithPlayersAsync("Kuba", "Anna", "Marek");

        Assert.Equal([0, 1, 2], viewModel.Players.Select(player => player.Model.Order));
    }

    [Fact]
    public async Task PowtorzoneImie_NieWchodziNaListe()
    {
        // Aplikacja woła graczy po imieniu — dwie „Anny" zamieniają polecenie „Anna, prawa
        // ręka" w zagadkę, do kogo należy tura.
        PlayersViewModel viewModel = await CreateWithPlayersAsync("Anna");

        viewModel.NewPlayerName = "  anna ";
        await viewModel.AddPlayerCommand.ExecuteAsync(parameter: null);

        Assert.Single(viewModel.Players);

        await _dialogs.Received(1).AlertAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task ZmianaImienia_ZapisujeSkladIZachowujeIdentyfikator()
    {
        // Identyfikator jest tym, po czym partia rozpoznaje gracza — zmiana imienia nie może
        // zrobić z niego kogoś innego.
        PlayersViewModel viewModel = await CreateWithPlayersAsync("Kuba");
        PlayerRowItem row = viewModel.Players[0];
        Guid id = row.Id;

        viewModel.BeginEditCommand.Execute(row);
        row.EditedName = "Kubuś";
        await viewModel.CommitEditCommand.ExecuteAsync(row);

        Assert.Equal("Kubuś", row.Name);
        Assert.Equal(id, row.Id);
        Assert.False(row.IsEditing);
        await _roster.Received(2).SaveAsync(
            Arg.Any<IReadOnlyList<Player>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ZmianaImieniaNaPowtorzone_NieZostajeZapisana()
    {
        PlayersViewModel viewModel = await CreateWithPlayersAsync("Kuba", "Anna");
        PlayerRowItem row = viewModel.Players[1];

        viewModel.BeginEditCommand.Execute(row);
        row.EditedName = "Kuba";
        await viewModel.CommitEditCommand.ExecuteAsync(row);

        Assert.Equal("Anna", row.Name);
        Assert.True(row.IsEditing);
    }

    [Fact]
    public async Task PusteImie_KonczyEdycjeBezZmiany()
    {
        // Model odrzuciłby pustą nazwę wyjątkiem, a komunikat o błędzie za skasowanie tekstu
        // byłby karą za nic.
        PlayersViewModel viewModel = await CreateWithPlayersAsync("Kuba");
        PlayerRowItem row = viewModel.Players[0];

        viewModel.BeginEditCommand.Execute(row);
        row.EditedName = "   ";
        await viewModel.CommitEditCommand.ExecuteAsync(row);

        Assert.Equal("Kuba", row.Name);
        Assert.Equal("Kuba", row.EditedName);
        Assert.False(row.IsEditing);
    }

    [Fact]
    public async Task RozpoczecieEdycji_ZamykaEdycjeInnegoWiersza()
    {
        // Dwa otwarte pola to dwa niezapisane stany na ekranie.
        PlayersViewModel viewModel = await CreateWithPlayersAsync("Kuba", "Anna");

        viewModel.BeginEditCommand.Execute(viewModel.Players[0]);
        viewModel.BeginEditCommand.Execute(viewModel.Players[1]);

        Assert.False(viewModel.Players[0].IsEditing);
        Assert.True(viewModel.Players[1].IsEditing);
    }

    [Fact]
    public async Task PrzeniesienieWyzej_ZmieniaKolejnoscTur()
    {
        PlayersViewModel viewModel = await CreateWithPlayersAsync("Kuba", "Anna", "Marek");

        await viewModel.MoveUpCommand.ExecuteAsync(viewModel.Players[2]);

        Assert.Equal(["Kuba", "Marek", "Anna"], viewModel.Players.Select(player => player.Name));
        Assert.Equal([0, 1, 2], viewModel.Players.Select(player => player.Model.Order));
    }

    [Fact]
    public async Task PrzeniesienieNizej_ZmieniaKolejnoscTur()
    {
        PlayersViewModel viewModel = await CreateWithPlayersAsync("Kuba", "Anna");

        await viewModel.MoveDownCommand.ExecuteAsync(viewModel.Players[0]);

        Assert.Equal(["Anna", "Kuba"], viewModel.Players.Select(player => player.Name));
    }

    [Fact]
    public async Task SkrajneWiersze_NieMajaDokadSiePrzeniesc()
    {
        PlayersViewModel viewModel = await CreateWithPlayersAsync("Kuba", "Anna");

        Assert.False(viewModel.Players[0].CanMoveUp);
        Assert.True(viewModel.Players[0].CanMoveDown);
        Assert.True(viewModel.Players[1].CanMoveUp);
        Assert.False(viewModel.Players[1].CanMoveDown);
    }

    [Fact]
    public async Task RemovePlayerCommand_UsuwaGraczaIPrzenumerowujePozostalych()
    {
        // Partia jest rozgrywana w kolejności wynikającej z numerów, więc luka
        // po usuniętym graczu nie może zostać.
        PlayersViewModel viewModel = await CreateWithPlayersAsync("Kuba", "Anna", "Marek");

        PlayerRowItem anna = viewModel.Players.Single(player => player.Name == "Anna");
        await viewModel.RemovePlayerCommand.ExecuteAsync(anna);

        Assert.Equal(["Kuba", "Marek"], viewModel.Players.Select(player => player.Name));
        Assert.Equal([0, 1], viewModel.Players.Select(player => player.Model.Order));
    }

    [Fact]
    public async Task OdmowaPotwierdzenia_ZostawiaGraczaNaLiscie()
    {
        // Przycisk usuwania stoi obok przycisków przenoszenia — o jeden palec za blisko,
        // żeby polegać na uwadze.
        _dialogs.ConfirmAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(Task.FromResult(false));

        PlayersViewModel viewModel = await CreateWithPlayersAsync("Kuba");

        await viewModel.RemovePlayerCommand.ExecuteAsync(viewModel.Players[0]);

        Assert.Single(viewModel.Players);
    }

    [Fact]
    public async Task HasNoPlayers_ZmieniaSiePoDodaniuIUsunieciu()
    {
        PlayersViewModel viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        Assert.True(viewModel.HasNoPlayers);

        viewModel.NewPlayerName = "Kuba";
        await viewModel.AddPlayerCommand.ExecuteAsync(parameter: null);

        Assert.False(viewModel.HasNoPlayers);

        await viewModel.RemovePlayerCommand.ExecuteAsync(viewModel.Players[0]);

        Assert.True(viewModel.HasNoPlayers);
    }

    [Fact]
    public async Task ZmianaSkladu_JestZapisywanaNatychmiast()
    {
        // System może usunąć proces aplikacji w tle w dowolnej chwili — wpisywanie imion
        // od nowa jest stratą, której użytkownik nie wybaczy.
        PlayersViewModel viewModel = CreateViewModel();

        viewModel.NewPlayerName = "Kuba";
        await viewModel.AddPlayerCommand.ExecuteAsync(parameter: null);
        await viewModel.RemovePlayerCommand.ExecuteAsync(viewModel.Players[0]);

        await _roster.Received(2).SaveAsync(
            Arg.Any<IReadOnlyList<Player>>(),
            Arg.Any<CancellationToken>());
    }

    private async Task<PlayersViewModel> CreateWithPlayersAsync(params string[] names)
    {
        PlayersViewModel viewModel = CreateViewModel();

        foreach (string name in names)
        {
            viewModel.NewPlayerName = name;
            await viewModel.AddPlayerCommand.ExecuteAsync(parameter: null);
        }

        return viewModel;
    }

    private PlayersViewModel CreateViewModel() => new(
        Substitute.For<INavigationService>(),
        _roster,
        NullLogger<PlayersViewModel>.Instance,
        _dialogs,
        new FakeLocalizationService());
}
