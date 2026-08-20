using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Localization;
using TwisterCompanion.Application.Voice;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Presentation.Abstractions;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Ekran paczek Custom Events: lista paczek, wybór aktywnej, edycja zawartości,
/// kopiowanie i przenoszenie.
/// </summary>
/// <remarks>
/// Paczki wbudowane są tylko do odczytu. Zmiana ich zawartości odbywa się przez
/// skopiowanie i edycję kopii — ekran podpowiada to, gdy użytkownik próbuje coś zmienić.
/// <para>
/// Docelowy wygląd dokłada Etap 10 — tutaj jest sam mechanizm.
/// </para>
/// </remarks>
public partial class EventPacksViewModel : NavigableViewModelBase
{
    private const int DefaultNewEventChance = 10;

    private readonly IEventPackService _eventPacks;
    private readonly IAnnouncementBuilder _announcementBuilder;

    /// <summary>Paczka, dla której zbudowano aktualną listę wydarzeń.</summary>
    private Guid? _eventsBuiltForPackId;

    /// <summary>Tworzy ViewModel ekranu paczek wydarzeń.</summary>
    /// <param name="navigation">Serwis nawigacji.</param>
    /// <param name="eventPacks">Operacje na paczkach wydarzeń.</param>
    /// <param name="announcementBuilder">Tłumaczenie nazw wydarzeń.</param>
    /// <param name="logger">Logger tego ViewModelu.</param>
    /// <param name="dialogService">Serwis komunikatów dla użytkownika.</param>
    /// <param name="localization">Serwis tłumaczeń.</param>
    public EventPacksViewModel(
        INavigationService navigation,
        IEventPackService eventPacks,
        IAnnouncementBuilder announcementBuilder,
        ILogger<EventPacksViewModel> logger,
        IDialogService dialogService,
        ILocalizationService localization)
        : base(navigation, logger, dialogService, localization)
    {
        ArgumentNullException.ThrowIfNull(eventPacks);
        ArgumentNullException.ThrowIfNull(announcementBuilder);

        _eventPacks = eventPacks;
        _announcementBuilder = announcementBuilder;
    }

    /// <summary>Wszystkie paczki — wbudowane i użytkownika.</summary>
    public ObservableCollection<PackListItem> Packs { get; } = [];

    /// <summary>Wydarzenia wybranej paczki.</summary>
    public ObservableCollection<EventListItem> Events { get; } = [];

    /// <summary>Zaznaczona pozycja listy paczek.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPackSelected))]
    [NotifyPropertyChangedFor(nameof(IsSelectedPackEditable))]
    [NotifyPropertyChangedFor(nameof(IsSelectedPackActive))]
    private PackListItem? _selectedPackItem;

    /// <summary>Nazwa nowej paczki wpisywana przez użytkownika.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreatePackCommand))]
    private string _newPackName = string.Empty;

    /// <summary>Nazwa nowego wydarzenia wpisywana przez użytkownika.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddEventCommand))]
    private string _newEventName = string.Empty;

    /// <summary>
    /// Szansa nowego wydarzenia — wpisywana ręcznie, w procentach.
    /// </summary>
    /// <remarks>
    /// Tekst, a nie liczba, żeby dało się wpisać dowolną wartość z klawiatury.
    /// Niepoprawny wpis jest traktowany jako zero przy dodawaniu wydarzenia.
    /// </remarks>
    [ObservableProperty]
    private string _newEventChanceText = PercentText.Format(DefaultNewEventChance);

    /// <summary>Podsumowanie sumy szans wybranej paczki.</summary>
    [ObservableProperty]
    private string _totalChanceText = string.Empty;

    /// <summary>Suma szans włączonych wydarzeń wybranej paczki.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChanceWarning))]
    private double _totalChancePercent;

    /// <summary>Wybrana paczka w postaci domenowej.</summary>
    public EventPack? SelectedPack => SelectedPackItem?.Model;

    /// <summary>Czy jakakolwiek paczka jest wybrana.</summary>
    public bool IsPackSelected => SelectedPackItem is not null;

    /// <summary>Czy wybraną paczkę wolno zmieniać.</summary>
    public bool IsSelectedPackEditable => SelectedPackItem is { IsBuiltIn: false };

    /// <summary>Czy wybrana paczka jest używana w rozgrywce.</summary>
    public bool IsSelectedPackActive => SelectedPackItem?.IsActive ?? false;

    /// <summary>
    /// Czy suma szans przekracza 100% — wtedy wydarzenie pada w każdej dozwolonej turze.
    /// </summary>
    /// <remarks>
    /// Nie blokujemy takiej konfiguracji: użytkownik ma prawo ustawić dowolne wartości,
    /// a silnik traktuje sumę powyżej 100% jako pewne wystąpienie. Ostrzegamy, bo to
    /// zwykle nie jest to, o co chodziło.
    /// </remarks>
    public bool HasChanceWarning => TotalChancePercent > 100;

    /// <inheritdoc />
    protected override Task OnInitializeAsync() => ReloadAsync();

    /// <summary>Zaznacza paczkę dotkniętą na liście.</summary>
    /// <param name="item">Dotknięty wiersz.</param>
    /// <remarks>
    /// Zaznaczenie idzie przez polecenie, a nie przez tryb zaznaczania listy: tryb listy
    /// maluje wiersz kolorem systemu — na Androidzie pomarańczowym, spoza palety aplikacji —
    /// i ten kolor nie da się pogodzić z wyróżnieniem paczki używanej w rozgrywce, bo oba
    /// walczyłyby o tło i obrys tej samej karty.
    /// </remarks>
    [RelayCommand]
    private void SelectPack(PackListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        SelectedPackItem = item;
    }

    /// <summary>Ustawia wybraną paczkę jako używaną w rozgrywce.</summary>
    [RelayCommand]
    private Task SetActivePackAsync() => ExecuteSafeAsync(async () =>
    {
        if (SelectedPack is null)
        {
            return;
        }

        await _eventPacks.SetActiveAsync(SelectedPack.Id);
        await ReloadAsync();
    });

    /// <summary>Wyłącza wydarzenia w rozgrywce.</summary>
    [RelayCommand]
    private Task ClearActivePackAsync() => ExecuteSafeAsync(async () =>
    {
        await _eventPacks.SetActiveAsync(null);
        await ReloadAsync();
    });

    /// <summary>Tworzy nową, pustą paczkę użytkownika.</summary>
    [RelayCommand(CanExecute = nameof(CanCreatePack))]
    private Task CreatePackAsync() => ExecuteSafeAsync(async () =>
    {
        EventPack created = await _eventPacks.CreateAsync(NewPackName.Trim());

        NewPackName = string.Empty;

        await ReloadAsync(selectPackId: created.Id);
    });

    private bool CanCreatePack() => !string.IsNullOrWhiteSpace(NewPackName);

    /// <summary>Tworzy edytowalną kopię wybranej paczki.</summary>
    [RelayCommand]
    private Task DuplicatePackAsync() => ExecuteSafeAsync(async () =>
    {
        if (SelectedPackItem is null)
        {
            return;
        }

        string copyName = Localization.GetFormattedString(
            StringKeys.EventPacks.CopyNameFormat,
            StringCatalog.Ui,
            SelectedPackItem.DisplayName);

        EventPack copy = await _eventPacks.DuplicateAsync(SelectedPackItem.Model, copyName);

        await ReloadAsync(selectPackId: copy.Id);
    });

    /// <summary>Usuwa wybraną paczkę po potwierdzeniu.</summary>
    [RelayCommand]
    private Task DeletePackAsync() => ExecuteSafeAsync(async () =>
    {
        if (SelectedPackItem is null)
        {
            return;
        }

        if (SelectedPackItem.IsBuiltIn)
        {
            await ShowInfoAsync(StringKeys.EventPacks.BuiltInReadOnly);

            return;
        }

        bool confirmed = await Dialogs.ConfirmAsync(
            Localization[StringKeys.EventPacks.DeleteConfirmTitle],
            Localization.GetFormattedString(
                StringKeys.EventPacks.DeleteConfirmMessage,
                StringCatalog.Ui,
                SelectedPackItem.DisplayName),
            Localization[StringKeys.EventPacks.ButtonDelete],
            Localization[StringKeys.Common.ButtonCancel]);

        if (!confirmed)
        {
            return;
        }

        await _eventPacks.DeleteAsync(SelectedPackItem.Model.Id);

        await ReloadAsync();
    });

    /// <summary>Dodaje wydarzenie do wybranej paczki.</summary>
    [RelayCommand(CanExecute = nameof(CanAddEvent))]
    private Task AddEventAsync() => ExecuteSafeAsync(async () =>
    {
        if (SelectedPack is null)
        {
            return;
        }

        if (SelectedPack.IsBuiltIn)
        {
            await ShowInfoAsync(StringKeys.EventPacks.BuiltInReadOnly);

            return;
        }

        GameEvent added = GameEvent.CreateCustom(NewEventName.Trim(), ParseNewEventChance());

        await SavePackAsync(SelectedPack.WithEvent(added));

        NewEventName = string.Empty;
        NewEventChanceText = PercentText.Format(DefaultNewEventChance);
    });

    private bool CanAddEvent() => !string.IsNullOrWhiteSpace(NewEventName);

    /// <summary>Zwiększa szansę nowego wydarzenia o krok.</summary>
    [RelayCommand]
    private void IncreaseNewEventChance() => ShiftNewEventChance(EventListItem.ChanceStep);

    /// <summary>Zmniejsza szansę nowego wydarzenia o krok.</summary>
    [RelayCommand]
    private void DecreaseNewEventChance() => ShiftNewEventChance(-EventListItem.ChanceStep);

    private void ShiftNewEventChance(int delta)
    {
        double current = ParseNewEventChance();
        double step = EventListItem.ChanceStep;

        double rounded = delta > 0
            ? (Math.Floor(current / step) * step) + step
            : (Math.Ceiling(current / step) * step) - step;

        NewEventChanceText = PercentText.Format(Math.Clamp(rounded, 0, 100));
    }

    /// <summary>Odczytuje wpisaną szansę, przycinając ją do dopuszczalnego zakresu.</summary>
    private double ParseNewEventChance() =>
        PercentText.TryParse(NewEventChanceText, out double value)
            ? Math.Clamp(value, 0, 100)
            : 0;

    /// <summary>Usuwa wydarzenie z wybranej paczki.</summary>
    /// <param name="item">Wydarzenie do usunięcia.</param>
    [RelayCommand]
    private Task RemoveEventAsync(EventListItem item) => ExecuteSafeAsync(async () =>
    {
        ArgumentNullException.ThrowIfNull(item);

        if (SelectedPack is null || SelectedPack.IsBuiltIn)
        {
            return;
        }

        await SavePackAsync(SelectedPack.WithoutEvent(item.Model.Id));
    });

    /// <summary>
    /// Zapisuje zmianę wprowadzoną przełącznikiem albo suwakiem wydarzenia.
    /// </summary>
    /// <param name="item">Zmienione wydarzenie.</param>
    [RelayCommand]
    private Task UpdateEventAsync(EventListItem item) => ExecuteSafeAsync(async () =>
    {
        ArgumentNullException.ThrowIfNull(item);

        if (SelectedPack is null || SelectedPack.IsBuiltIn)
        {
            return;
        }

        EventPack updated = SelectedPack.WithUpdatedEvent(item.Model);

        await _eventPacks.SaveAsync(updated);

        // Lista wydarzeń nie jest przebudowywana — użytkownik trzyma palec na suwaku.
        // Odświeżamy tylko paczkę i podsumowanie sum.
        ReplacePackInList(updated);
        RefreshTotals();
    });

    /// <summary>Reaguje na zmianę zaznaczenia na liście paczek.</summary>
    /// <remarks>
    /// Znacznik zaznaczenia siedzi na wierszu, a nie w stanach wizualnych listy, więc trzeba
    /// zgasić go na poprzednim wierszu — stąd wersja z dwoma argumentami.
    /// <para>
    /// Lista wydarzeń jest przebudowywana tylko przy zmianie na <b>inną</b> paczkę. Wejście
    /// na tę samą (na przykład po odświeżeniu listy) zostawia wiersze wydarzeń w spokoju:
    /// przebudowa podmieniałaby kontrolkę pod palcem użytkownika w trakcie zmiany szansy.
    /// Ścieżki, które naprawdę zmieniają zawartość paczki, wołają <c>RefreshEvents</c> wprost.
    /// </para>
    /// </remarks>
    partial void OnSelectedPackItemChanged(PackListItem? oldValue, PackListItem? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsSelected = false;
        }

        if (newValue is not null)
        {
            newValue.IsSelected = true;
        }

        if (newValue?.Model.Id == _eventsBuiltForPackId)
        {
            RefreshTotals();

            return;
        }

        RefreshEvents();
    }

    private async Task ReloadAsync(Guid? selectPackId = null)
    {
        Guid? targetId = selectPackId ?? SelectedPackItem?.Model.Id;

        IReadOnlyList<EventPack> packs = await _eventPacks.GetAllAsync();
        Guid? activeId = (await _eventPacks.GetActiveAsync())?.Id;

        Packs.Clear();

        foreach (EventPack pack in packs)
        {
            Packs.Add(new PackListItem(pack, GetPackDisplayName(pack), pack.Id == activeId));
        }

        SelectedPackItem = targetId is null
            ? null
            : Packs.FirstOrDefault(item => item.Model.Id == targetId);
    }

    private async Task SavePackAsync(EventPack updated)
    {
        await _eventPacks.SaveAsync(updated);

        ReplacePackInList(updated);
        RefreshEvents();
    }

    /// <summary>
    /// Wstawia nową postać paczki do jej wiersza na liście.
    /// </summary>
    /// <remarks>
    /// Wiersz zostaje ten sam — podmieniana jest tylko paczka w środku, a wiersz zgłasza
    /// zmianę liczby wydarzeń sam. Wcześniej wiersz był wymieniany na nowy i to właśnie
    /// wywracało edycję szansy: wymiana zaznaczonego wiersza wyglądała jak zmiana
    /// zaznaczenia, a ta przebudowywała listę wydarzeń pod palcem użytkownika.
    /// </remarks>
    private void ReplacePackInList(EventPack updated)
    {
        foreach (PackListItem item in Packs)
        {
            if (item.Model.Id != updated.Id)
            {
                continue;
            }

            item.Model = updated;

            return;
        }
    }

    private void RefreshEvents()
    {
        Events.Clear();
        _eventsBuiltForPackId = SelectedPack?.Id;

        if (SelectedPack is not null)
        {
            bool editable = !SelectedPack.IsBuiltIn;

            foreach (GameEvent gameEvent in SelectedPack.Events)
            {
                Events.Add(new EventListItem(
                    gameEvent,
                    _announcementBuilder.GetEventName(gameEvent),
                    editable,
                    item => UpdateEventCommand.Execute(item)));
            }
        }

        RefreshTotals();
    }

    private void RefreshTotals()
    {
        TotalChancePercent = SelectedPack?.TotalEnabledChancePercent ?? 0;

        TotalChanceText = SelectedPack is null
            ? string.Empty
            : Localization.GetFormattedString(
                StringKeys.EventPacks.TotalChanceFormat,
                StringCatalog.Ui,
                TotalChancePercent);
    }

    /// <summary>
    /// Zwraca nazwę paczki w aktualnym języku.
    /// </summary>
    /// <remarks>
    /// Paczki wbudowane mają klucz zasobu, paczki użytkownika własną nazwę, której
    /// nie tłumaczymy.
    /// </remarks>
    private string GetPackDisplayName(EventPack pack) =>
        pack.NameKey is null ? pack.Name : Localization[pack.NameKey];
}
