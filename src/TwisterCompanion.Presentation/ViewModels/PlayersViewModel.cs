using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Presentation.Abstractions;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Ekran zarządzania graczami — dodawanie, zmiana imienia, kolejność, usuwanie.
/// </summary>
/// <remarks>
/// Skład jest zapisywany po każdej zmianie, a nie przy wyjściu z ekranu. Powód: system może
/// zamknąć aplikację w tle w dowolnej chwili, a wpisywanie imion od nowa jest dokładnie tym
/// rodzajem straty, którego użytkownik nie wybaczy.
/// <para>
/// <b>Kolejność na liście jest kolejnością tur</b>, więc jej zmiana jest częścią ustawiania
/// rozgrywki, a nie kosmetyką. Przenoszenie odbywa się przyciskami, nie przeciąganiem:
/// przeciąganie na liście z polami tekstowymi myli się z przewijaniem, a telefon w tej grze
/// bywa obsługiwany jedną ręką w niewygodnej pozycji.
/// </para>
/// </remarks>
public partial class PlayersViewModel : NavigableViewModelBase
{
    private readonly IPlayerRosterRepository _playerRoster;

    /// <summary>Tworzy ViewModel ekranu graczy.</summary>
    /// <param name="navigation">Serwis nawigacji.</param>
    /// <param name="playerRoster">Repozytorium listy graczy.</param>
    /// <param name="logger">Logger tego ViewModelu.</param>
    /// <param name="dialogService">Serwis komunikatów dla użytkownika.</param>
    /// <param name="localization">Serwis tłumaczeń.</param>
    public PlayersViewModel(
        INavigationService navigation,
        IPlayerRosterRepository playerRoster,
        ILogger<PlayersViewModel> logger,
        IDialogService dialogService,
        ILocalizationService localization)
        : base(navigation, logger, dialogService, localization)
    {
        ArgumentNullException.ThrowIfNull(playerRoster);

        _playerRoster = playerRoster;
    }

    /// <summary>Uczestnicy w kolejności rozgrywki.</summary>
    public ObservableCollection<PlayerRowItem> Players { get; } = [];

    /// <summary>Nazwa wpisywana w polu dodawania gracza.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPlayerCommand))]
    private string _newPlayerName = string.Empty;

    /// <summary>Czy lista graczy jest pusta.</summary>
    public bool HasNoPlayers => Players.Count == 0;

    /// <summary>Czy jest kogo pokazać na liście.</summary>
    public bool HasPlayers => Players.Count > 0;

    /// <inheritdoc />
    protected override async Task OnInitializeAsync()
    {
        IReadOnlyList<Player> saved = await _playerRoster.GetAsync();

        Players.Clear();

        foreach (Player player in saved)
        {
            Players.Add(new PlayerRowItem(player));
        }

        RefreshPositions();
    }

    /// <summary>
    /// Dodaje gracza o wpisanej nazwie.
    /// </summary>
    /// <remarks>
    /// Imię powtórzone jest odrzucane: aplikacja woła graczy po imieniu, a dwie „Anny"
    /// w składzie zamieniają polecenie „Anna, prawa ręka" w zagadkę, do kogo należy tura.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanAddPlayer))]
    private Task AddPlayerAsync() => ExecuteSafeAsync(async () =>
    {
        string name = NewPlayerName.Trim();

        if (FindDuplicate(name) is not null)
        {
            await ShowDuplicateAsync(name);

            return;
        }

        Players.Add(new PlayerRowItem(Player.Create(name, Players.Count)));
        NewPlayerName = string.Empty;

        RefreshPositions();

        await SaveAsync();
    });

    private bool CanAddPlayer() => !string.IsNullOrWhiteSpace(NewPlayerName);

    /// <summary>Usuwa gracza z listy po potwierdzeniu.</summary>
    /// <param name="player">Gracz do usunięcia.</param>
    /// <remarks>
    /// Pytanie o potwierdzenie, bo usunięcie jest nieodwracalne, a przycisk stoi w wierszu
    /// obok przycisków przenoszenia — o jeden palec za blisko, żeby polegać na uwadze.
    /// </remarks>
    [RelayCommand]
    private Task RemovePlayerAsync(PlayerRowItem player) => ExecuteSafeAsync(async () =>
    {
        ArgumentNullException.ThrowIfNull(player);

        bool confirmed = await Dialogs.ConfirmAsync(
            Localization[StringKeys.Players.DeleteConfirmTitle],
            Localization.GetFormattedString(
                StringKeys.Players.DeleteConfirmMessage,
                StringCatalog.Ui,
                player.Name),
            Localization[StringKeys.Players.ButtonRemove],
            Localization[StringKeys.Common.ButtonCancel]);

        if (!confirmed)
        {
            return;
        }

        Players.Remove(player);
        RenumberPlayers();
        RefreshPositions();

        await SaveAsync();
    });

    /// <summary>Włącza edycję imienia w wierszu.</summary>
    /// <param name="player">Wiersz do edycji.</param>
    /// <remarks>
    /// Edycja w miejscu, a nie osobne okno: zmiana imienia to zwykle poprawa literówki albo
    /// zdrobnienie, a nie wypełnianie formularza. Naraz edytowany jest jeden wiersz — dwa
    /// otwarte pola oznaczałyby dwa niezapisane stany na ekranie.
    /// </remarks>
    [RelayCommand]
    private void BeginEdit(PlayerRowItem player)
    {
        ArgumentNullException.ThrowIfNull(player);

        foreach (PlayerRowItem other in Players)
        {
            if (!ReferenceEquals(other, player) && other.IsEditing)
            {
                other.ResetEdit();
            }
        }

        player.EditedName = player.Name;
        player.IsEditing = true;
    }

    /// <summary>Zapisuje zmienione imię gracza.</summary>
    /// <param name="player">Edytowany wiersz.</param>
    /// <remarks>
    /// Puste imię i imię niezmienione kończą edycję bez zapisu — model odrzuciłby pustą
    /// nazwę wyjątkiem, a komunikat o błędzie za skasowanie tekstu byłby karą za nic.
    /// </remarks>
    [RelayCommand]
    private Task CommitEditAsync(PlayerRowItem player) => ExecuteSafeAsync(async () =>
    {
        ArgumentNullException.ThrowIfNull(player);

        string name = player.EditedName.Trim();

        if (name.Length == 0 || string.Equals(name, player.Name, StringComparison.Ordinal))
        {
            player.ResetEdit();

            return;
        }

        if (FindDuplicate(name, except: player) is not null)
        {
            await ShowDuplicateAsync(name);

            return;
        }

        player.Apply(player.Model with { Name = name });
        player.IsEditing = false;

        await SaveAsync();
    });

    /// <summary>Porzuca zmianę imienia.</summary>
    /// <param name="player">Edytowany wiersz.</param>
    [RelayCommand]
    private static void CancelEdit(PlayerRowItem player)
    {
        ArgumentNullException.ThrowIfNull(player);

        player.ResetEdit();
    }

    /// <summary>Przenosi gracza o jedno miejsce wyżej w kolejce.</summary>
    /// <param name="player">Gracz do przeniesienia.</param>
    [RelayCommand]
    private Task MoveUpAsync(PlayerRowItem player) => MoveAsync(player, offset: -1);

    /// <summary>Przenosi gracza o jedno miejsce niżej w kolejce.</summary>
    /// <param name="player">Gracz do przeniesienia.</param>
    [RelayCommand]
    private Task MoveDownAsync(PlayerRowItem player) => MoveAsync(player, offset: 1);

    private Task MoveAsync(PlayerRowItem player, int offset) => ExecuteSafeAsync(async () =>
    {
        ArgumentNullException.ThrowIfNull(player);

        int index = Players.IndexOf(player);
        int target = index + offset;

        if (index < 0 || target < 0 || target >= Players.Count)
        {
            return;
        }

        Players.Move(index, target);
        RenumberPlayers();
        RefreshPositions();

        await SaveAsync();
    });

    /// <summary>Znajduje gracza o tym samym imieniu, pomijając wskazany wiersz.</summary>
    /// <remarks>
    /// Porównanie bez rozróżniania wielkości liter i z uwzględnieniem języka: „anna" i „Anna"
    /// brzmią identycznie, a to brzmienie jest tu istotne — imiona są czytane na głos.
    /// </remarks>
    private PlayerRowItem? FindDuplicate(string name, PlayerRowItem? except = null) =>
        Players.FirstOrDefault(item =>
            !ReferenceEquals(item, except)
            && string.Equals(item.Name, name, StringComparison.CurrentCultureIgnoreCase));

    private Task ShowDuplicateAsync(string name) => Dialogs.AlertAsync(
        Localization[StringKeys.Common.InfoTitle],
        Localization.GetFormattedString(StringKeys.Players.DuplicateName, StringCatalog.Ui, name),
        Localization[StringKeys.Common.ButtonOk]);

    /// <summary>
    /// Nadaje graczom kolejne pozycje po usunięciu albo przeniesieniu któregoś z nich.
    /// </summary>
    /// <remarks>
    /// Bez tego po usunięciu gracza ze środka listy pozostałyby luki w numeracji,
    /// a partia jest rozgrywana w kolejności wynikającej właśnie z tych numerów.
    /// </remarks>
    private void RenumberPlayers()
    {
        for (int index = 0; index < Players.Count; index++)
        {
            if (Players[index].Model.Order != index)
            {
                Players[index].Apply(Players[index].Model with { Order = index });
            }
        }
    }

    /// <summary>Odświeża dostępność przycisków przenoszenia i stan pustej listy.</summary>
    private void RefreshPositions()
    {
        for (int index = 0; index < Players.Count; index++)
        {
            Players[index].CanMoveUp = index > 0;
            Players[index].CanMoveDown = index < Players.Count - 1;
        }

        OnPropertyChanged(nameof(HasNoPlayers));
        OnPropertyChanged(nameof(HasPlayers));
    }

    private Task SaveAsync() => _playerRoster.SaveAsync([.. Players.Select(item => item.Model)]);
}
