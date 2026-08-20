using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Presentation.Abstractions;
using TwisterCompanion.Presentation.Tests.Fakes;
using TwisterCompanion.Presentation.ViewModels;

namespace TwisterCompanion.Presentation.Tests;

/// <summary>
/// Testy ekranu paczek wydarzeń: lista, wybór aktywnej paczki, edycja zawartości,
/// kopiowanie i usuwanie.
/// </summary>
/// <remarks>
/// Ekran powstał w Etapie 6 i do Etapu 14 nie miał ani jednego testu — pomiar pokrycia
/// pokazał 202 niepokryte linie z 215, największą dziurę w całej warstwie prezentacji.
/// Testy skupiają się na regule, która trzyma ten ekran w kupie: <b>paczki wbudowane są
/// tylko do odczytu</b>, a każda próba ich zmiany ma kończyć się podpowiedzią „skopiuj
/// i zmieniaj kopię", nie zapisem.
/// </remarks>
public class EventPacksViewModelTests
{
    private static readonly EventPack Wbudowana = new()
    {
        Id = Guid.NewGuid(),
        Name = "Party",
        NameKey = "EventPack_Party_Name",
        IsBuiltIn = true,
        Events = [GameEvent.CreateBuiltIn("Event_SingChorus", 20)],
    };

    private readonly IEventPackService _packs = Substitute.For<IEventPackService>();
    private readonly IAnnouncementBuilder _announcements = Substitute.For<IAnnouncementBuilder>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private EventPack _wlasna = EventPack.Create(
        "Moja paczka",
        [GameEvent.CreateCustom("Zrób pajacyka", 30)]);

    public EventPacksViewModelTests()
    {
        _packs.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<EventPack>>([Wbudowana, _wlasna]));

        _packs.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<EventPack?>(_wlasna));

        _announcements.GetEventName(Arg.Any<GameEvent>()).Returns(call =>
        {
            GameEvent? gameEvent = call.Arg<GameEvent>();

            return gameEvent?.CustomName ?? gameEvent?.NameKey ?? string.Empty;
        });
    }

    [Fact]
    public async Task Inicjalizacja_PokazujePaczkiIZaznaczaAktywna()
    {
        EventPacksViewModel viewModel = CreateViewModel();

        await viewModel.InitializeAsync();

        Assert.Equal(2, viewModel.Packs.Count);
        Assert.True(viewModel.Packs.Single(pack => pack.Model.Id == _wlasna.Id).IsActive);
        Assert.False(viewModel.Packs.Single(pack => pack.Model.Id == Wbudowana.Id).IsActive);

        // Zaznaczenie po wczytaniu jest puste: aktywna paczka to nie to samo co zaznaczona
        // na liście. Gracz wchodzi na ekran, żeby wybrać, a nie żeby zobaczyć zaznaczone.
        Assert.False(viewModel.IsPackSelected);
    }

    [Fact]
    public async Task NazwaPaczkiWbudowanej_IdzieProzezTlumaczenie_WlasnaZostajeDoslowna()
    {
        EventPacksViewModel viewModel = CreateViewModel();

        await viewModel.InitializeAsync();

        // Atrapa tłumaczeń zwraca sam klucz, więc równość z kluczem dowodzi, że nazwa
        // paczki wbudowanej przeszła przez tłumaczenie.
        Assert.Equal(
            "EventPack_Party_Name",
            viewModel.Packs.Single(pack => pack.Model.Id == Wbudowana.Id).DisplayName);

        Assert.Equal(
            "Moja paczka",
            viewModel.Packs.Single(pack => pack.Model.Id == _wlasna.Id).DisplayName);
    }

    [Fact]
    public async Task WybranieePaczki_PokazujeJejWydarzeniaISumeSzans()
    {
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);

        Assert.True(viewModel.IsPackSelected);
        Assert.True(viewModel.IsSelectedPackEditable);
        Assert.True(viewModel.IsSelectedPackActive);
        Assert.Equal("Zrób pajacyka", Assert.Single(viewModel.Events).DisplayName);
        Assert.Equal(30, viewModel.TotalChancePercent);
        Assert.False(viewModel.HasChanceWarning);
    }

    [Fact]
    public async Task Zaznaczenie_ZapalaZnacznikNaJednejKarcieIGasiNaPoprzedniej()
    {
        // Ekran maluje dwa stany karty i muszą być rozdzielone: paczka używana w rozgrywce
        // ma pełne wyróżnienie, a zaznaczona tylko pasek przy krawędzi. Znacznik zaznaczenia
        // siedzi na wierszu, bo lista nie ma własnego trybu zaznaczania — jej tryb malowałby
        // wiersz kolorem systemu i kolidował z wyróżnieniem paczki używanej.
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);

        PackListItem wlasna = viewModel.Packs.Single(pack => pack.Model.Id == _wlasna.Id);
        PackListItem wbudowana = viewModel.Packs.Single(pack => pack.Model.Id == Wbudowana.Id);

        Assert.True(wlasna.IsSelected);
        Assert.False(wbudowana.IsSelected);

        viewModel.SelectPackCommand.Execute(wbudowana);

        Assert.False(wlasna.IsSelected);
        Assert.True(wbudowana.IsSelected);
    }

    [Fact]
    public async Task Zaznaczenie_JestNiezalezneOdTego_KtoraPaczkaJestUzywana()
    {
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(Wbudowana.Id);

        PackListItem wlasna = viewModel.Packs.Single(pack => pack.Model.Id == _wlasna.Id);
        PackListItem wbudowana = viewModel.Packs.Single(pack => pack.Model.Id == Wbudowana.Id);

        // Zaznaczona jest wbudowana, ale w rozgrywce używana jest własna — dwa stany na dwóch
        // różnych kartach naraz i każdy ma swój znacznik.
        Assert.True(wbudowana.IsSelected);
        Assert.False(wbudowana.IsActive);
        Assert.False(wlasna.IsSelected);
        Assert.True(wlasna.IsActive);
    }

    [Fact]
    public async Task DodanieWydarzenia_OdswiezaWierszPaczkiWMiejscu()
    {
        // Wiersz nie jest podmieniany na nowy: podmiana zaznaczonego wiersza wyglądała jak
        // zmiana zaznaczenia i przebudowywała listę wydarzeń pod palcem użytkownika.
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);
        PackListItem wiersz = viewModel.Packs.Single(pack => pack.Model.Id == _wlasna.Id);

        Assert.Equal(1, wiersz.EventCount);

        viewModel.NewEventName = "Drugie";
        await viewModel.AddEventCommand.ExecuteAsync(parameter: null);

        Assert.Same(wiersz, viewModel.Packs.Single(pack => pack.Model.Id == _wlasna.Id));
        Assert.Equal(2, wiersz.EventCount);
        Assert.True(wiersz.IsSelected);
    }

    [Fact]
    public async Task PaczkaWbudowana_NieJestEdytowalna()
    {
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(Wbudowana.Id);

        Assert.True(viewModel.IsPackSelected);
        Assert.False(viewModel.IsSelectedPackEditable);
        Assert.False(Assert.Single(viewModel.Events).IsEditable);
    }

    [Fact]
    public async Task DodanieWydarzeniaDoPaczkiWbudowanej_TylkoInformujeINieZapisuje()
    {
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(Wbudowana.Id);
        viewModel.NewEventName = "Nie wejdzie";

        await viewModel.AddEventCommand.ExecuteAsync(parameter: null);

        await _packs.DidNotReceive().SaveAsync(Arg.Any<EventPack>(), Arg.Any<CancellationToken>());
        await _dialogs.Received(1).AlertAsync(
            Arg.Any<string>(),
            "EventPacks_Label_BuiltInReadOnly",
            Arg.Any<string>());
    }

    [Fact]
    public async Task UsuniecieePaczkiWbudowanej_TylkoInformujeINieUsuwa()
    {
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(Wbudowana.Id);

        await viewModel.DeletePackCommand.ExecuteAsync(parameter: null);

        await _packs.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _dialogs.DidNotReceive().ConfirmAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task DodanieWydarzenia_ZapisujePaczkeICzysciPola()
    {
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);
        viewModel.NewEventName = "  Zaśpiewaj refren  ";
        viewModel.NewEventChanceText = "45";

        await viewModel.AddEventCommand.ExecuteAsync(parameter: null);

        EventPack zapisana = ZapisanaPaczka();
        GameEvent dodane = zapisana.Events.Single(gameEvent => gameEvent.CustomName is not null
            && gameEvent.CustomName.StartsWith("Zaśpiewaj", StringComparison.Ordinal));

        // Nazwa jest przycinana z białych znaków — inaczej lista wyglądałaby na przesuniętą.
        Assert.Equal("Zaśpiewaj refren", dodane.CustomName);
        Assert.Equal(45, dodane.Chance.Percent);

        Assert.Equal(string.Empty, viewModel.NewEventName);
        Assert.Equal("10", viewModel.NewEventChanceText);
    }

    [Fact]
    public async Task DodanieWydarzenia_JestNiemozliweBezNazwy()
    {
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);

        Assert.False(viewModel.AddEventCommand.CanExecute(parameter: null));

        viewModel.NewEventName = "   ";

        Assert.False(viewModel.AddEventCommand.CanExecute(parameter: null));

        viewModel.NewEventName = "Coś";

        Assert.True(viewModel.AddEventCommand.CanExecute(parameter: null));
    }

    [Theory]
    [InlineData("nie liczba", 0)]
    [InlineData("-5", 0)]
    [InlineData("250", 100)]
    public async Task SzansaNowegoWydarzenia_JestPrzycinanaDoZakresu(string wpisane, int oczekiwane)
    {
        // Pole jest tekstowe, żeby dało się wpisać cokolwiek z klawiatury — więc musi
        // znosić cokolwiek. Zero dla śmieci jest wyborem świadomym: wydarzenie i tak
        // powstanie, tylko nigdy nie padnie, i widać to na liście.
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);
        viewModel.NewEventName = "Test";
        viewModel.NewEventChanceText = wpisane;

        await viewModel.AddEventCommand.ExecuteAsync(parameter: null);

        GameEvent dodane = ZapisanaPaczka().Events.Single(gameEvent => gameEvent.CustomName == "Test");

        Assert.Equal(oczekiwane, dodane.Chance.Percent);
    }

    [Theory]
    [InlineData("12", true, "15")]
    [InlineData("12", false, "10")]
    [InlineData("100", true, "100")]
    [InlineData("0", false, "0")]
    public async Task PrzyciskiSzansy_ZaokraglajaDoKrokuIPilnujaGranic(
        string poczatkowa,
        bool wZwyz,
        string oczekiwana)
    {
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);
        viewModel.NewEventChanceText = poczatkowa;

        if (wZwyz)
        {
            viewModel.IncreaseNewEventChanceCommand.Execute(parameter: null);
        }
        else
        {
            viewModel.DecreaseNewEventChanceCommand.Execute(parameter: null);
        }

        Assert.Equal(oczekiwana, viewModel.NewEventChanceText);
    }

    [Fact]
    public async Task UtworzeniePaczki_CzysciPoleIZaznaczaNowaPaczke()
    {
        EventPack nowa = EventPack.Create("Świeża");

        _packs.CreateAsync("Świeża", Arg.Any<CancellationToken>()).Returns(Task.FromResult(nowa));
        _packs.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<EventPack>>([Wbudowana, _wlasna, nowa]));

        EventPacksViewModel viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        viewModel.NewPackName = "  Świeża  ";

        Assert.True(viewModel.CreatePackCommand.CanExecute(parameter: null));

        await viewModel.CreatePackCommand.ExecuteAsync(parameter: null);

        Assert.Equal(string.Empty, viewModel.NewPackName);
        Assert.Equal(nowa.Id, viewModel.SelectedPack?.Id);
        Assert.False(viewModel.CreatePackCommand.CanExecute(parameter: null));
    }

    [Fact]
    public async Task KopiowaniePaczki_TworzyKopieOSformatowanejNazwieIZaznaczaJa()
    {
        EventPack kopia = EventPack.Create("EventPacks_Label_CopyNameFormat");

        _packs.DuplicateAsync(Arg.Any<EventPack>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(kopia));
        _packs.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<EventPack>>([Wbudowana, _wlasna, kopia]));

        EventPacksViewModel viewModel = await CreateWithSelectedAsync(Wbudowana.Id);

        await viewModel.DuplicatePackCommand.ExecuteAsync(parameter: null);

        // Kopiowanie jest jedyną drogą do zmiany paczki wbudowanej, więc musi działać
        // właśnie na niej — i od razu przestawiać zaznaczenie na kopię, bo to ją gracz
        // będzie zmieniał.
        await _packs.Received(1).DuplicateAsync(
            Arg.Is<EventPack>(pack => pack != null && pack.Id == Wbudowana.Id),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        Assert.Equal(kopia.Id, viewModel.SelectedPack?.Id);
    }

    [Fact]
    public async Task UsuniecieePaczki_PytaOPotwierdzenie_OdmowaNiczegoNieZmienia()
    {
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);

        await viewModel.DeletePackCommand.ExecuteAsync(parameter: null);

        await _packs.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        Assert.Equal(_wlasna.Id, viewModel.SelectedPack?.Id);
    }

    [Fact]
    public async Task UsuniecieePaczki_PoPotwierdzeniuUsuwaIOdznacza()
    {
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);
        Guid usuwana = _wlasna.Id;

        _packs.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<EventPack>>([Wbudowana]));

        await viewModel.DeletePackCommand.ExecuteAsync(parameter: null);

        await _packs.Received(1).DeleteAsync(usuwana, Arg.Any<CancellationToken>());
        Assert.False(viewModel.IsPackSelected);
        Assert.Empty(viewModel.Events);
    }

    [Fact]
    public async Task UsuniecieWydarzenia_ZapisujePaczkeBezNiego()
    {
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);
        EventListItem wydarzenie = Assert.Single(viewModel.Events);

        await viewModel.RemoveEventCommand.ExecuteAsync(wydarzenie);

        Assert.Empty(ZapisanaPaczka().Events);
        Assert.Empty(viewModel.Events);
        Assert.Equal(0, viewModel.TotalChancePercent);
    }

    [Fact]
    public async Task ZmianaSzansyWydarzenia_ZapisujeBezPrzebudowaniaListy()
    {
        // Sedno: szansa zmienia się w trakcie wpisywania i klikania plusem. Gdyby zapis
        // przebudował listę, kontrolka pod palcem zostałaby podmieniona i edycja by się urwała.
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);
        EventListItem wydarzenie = Assert.Single(viewModel.Events);

        // Zmiana idzie przez pole tekstowe, bo to ono jest źródłem prawdy dla szansy —
        // przyciski też tylko przepisują do niego wartość.
        wydarzenie.ChanceText = "80";

        Assert.Same(wydarzenie, Assert.Single(viewModel.Events));
        Assert.Equal(80, ZapisanaPaczka().Events.Single().Chance.Percent);
        Assert.Equal(80, viewModel.TotalChancePercent);
    }

    [Fact]
    public async Task SumaSzansPowyzejStu_WlaczaOstrzezenie()
    {
        _wlasna = EventPack.Create(
            "Za dużo",
            [GameEvent.CreateCustom("Raz", 70), GameEvent.CreateCustom("Dwa", 60)]);

        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);

        Assert.Equal(130, viewModel.TotalChancePercent);
        Assert.True(viewModel.HasChanceWarning);
    }

    [Fact]
    public async Task GraBezWydarzen_CzysciAktywnaPaczke()
    {
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(_wlasna.Id);

        _packs.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<EventPack?>(null));

        await viewModel.ClearActivePackCommand.ExecuteAsync(parameter: null);

        await _packs.Received(1).SetActiveAsync(null, Arg.Any<CancellationToken>());
        Assert.DoesNotContain(viewModel.Packs, pack => pack.IsActive);
        Assert.False(viewModel.IsSelectedPackActive);
    }

    [Fact]
    public async Task WybranieAktywnej_ZapisujeWyborIOznaczaJaNaLiscie()
    {
        EventPacksViewModel viewModel = await CreateWithSelectedAsync(Wbudowana.Id);

        _packs.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventPack?>(Wbudowana));

        await viewModel.SetActivePackCommand.ExecuteAsync(parameter: null);

        await _packs.Received(1).SetActiveAsync(Wbudowana.Id, Arg.Any<CancellationToken>());
        Assert.True(viewModel.IsSelectedPackActive);
        Assert.True(viewModel.Packs.Single(pack => pack.Model.Id == Wbudowana.Id).IsActive);
    }

    private EventPack ZapisanaPaczka() =>
        (EventPack)_packs.ReceivedCalls()
            .Last(call => call.GetMethodInfo().Name == nameof(IEventPackService.SaveAsync))
            .GetArguments()[0]!;

    private async Task<EventPacksViewModel> CreateWithSelectedAsync(Guid packId)
    {
        EventPacksViewModel viewModel = CreateViewModel();

        await viewModel.InitializeAsync();

        viewModel.SelectedPackItem = viewModel.Packs.Single(pack => pack.Model.Id == packId);

        return viewModel;
    }

    private EventPacksViewModel CreateViewModel() => new(
        Substitute.For<INavigationService>(),
        _packs,
        _announcements,
        NullLogger<EventPacksViewModel>.Instance,
        _dialogs,
        new FakeLocalizationService());
}
