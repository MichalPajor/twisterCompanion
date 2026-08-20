using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Domain.Entities;

/// <summary>
/// Paczka Custom Events — zestaw wydarzeń, który użytkownik może włączyć w rozgrywce.
/// </summary>
public sealed record EventPack
{
    private readonly string _name = string.Empty;
    private readonly IReadOnlyList<GameEvent> _events = [];

    /// <summary>Identyfikator paczki.</summary>
    public required Guid Id { get; init; }

    /// <summary>Nazwa paczki widoczna na liście.</summary>
    /// <exception cref="ArgumentException">Gdy nazwa jest pusta.</exception>
    public required string Name
    {
        get => _name;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _name = value.Trim();
        }
    }

    /// <summary>
    /// Klucz zasobu z nazwą paczki — ustawiany dla paczek wbudowanych, żeby ich nazwy
    /// tłumaczyły się na język aplikacji.
    /// </summary>
    public string? NameKey { get; init; }

    /// <summary>
    /// Czy paczka pochodzi z aplikacji. Paczek wbudowanych nie można usunąć ani edytować,
    /// ale można je skopiować i zmieniać kopię.
    /// </summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>
    /// Klasyfikacja wiekowa zawartości.
    /// </summary>
    /// <remarks>
    /// Wszystkie paczki dołączone do aplikacji są oznaczone jako odpowiednie dla każdego.
    /// Pole istnieje, żeby dodanie paczek dla dorosłych w przyszłości było kwestią danych
    /// i filtra, a nie przebudowy modelu.
    /// </remarks>
    public EventPackAgeRating AgeRating { get; init; } = EventPackAgeRating.Everyone;

    /// <summary>Wydarzenia należące do paczki.</summary>
    public IReadOnlyList<GameEvent> Events
    {
        get => _events;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _events = value;
        }
    }

    /// <summary>Wydarzenia biorące udział w losowaniu.</summary>
    public IEnumerable<GameEvent> EnabledEvents => Events.Where(gameEvent => gameEvent.IsEnabled);

    /// <summary>
    /// Suma szans wszystkich włączonych wydarzeń, w procentach.
    /// </summary>
    /// <remarks>
    /// Może przekroczyć 100 — użytkownik ma prawo ustawić dowolne wartości. Ekran paczek
    /// (Etap 6) ostrzega w takiej sytuacji, a silnik wydarzeń traktuje sumę powyżej 100
    /// jako pewne wystąpienie któregoś z wydarzeń.
    /// </remarks>
    public double TotalEnabledChancePercent =>
        Math.Round(EnabledEvents.Sum(gameEvent => gameEvent.Chance.Percent), 1);

    /// <summary>Tworzy nową paczkę użytkownika.</summary>
    /// <param name="name">Nazwa paczki.</param>
    /// <param name="events">Wydarzenia początkowe.</param>
    /// <returns>Nowa paczka edytowalna.</returns>
    public static EventPack Create(string name, IReadOnlyList<GameEvent>? events = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Events = events ?? [],
    };

    /// <summary>
    /// Tworzy edytowalną kopię paczki — z nowymi identyfikatorami i bez znacznika
    /// paczki wbudowanej.
    /// </summary>
    /// <param name="newName">Nazwa kopii.</param>
    /// <returns>Kopia, którą użytkownik może dowolnie zmieniać.</returns>
    /// <remarks>
    /// Identyfikatory wydarzeń też są nowe. Bez tego edycja kopii mogłaby kolidować
    /// z oryginałem przy zapisie.
    /// </remarks>
    public EventPack Duplicate(string newName) => new()
    {
        Id = Guid.NewGuid(),
        Name = newName,
        NameKey = null,
        IsBuiltIn = false,
        Events = [.. Events.Select(gameEvent => gameEvent with { Id = Guid.NewGuid() })],
    };

    /// <summary>Zwraca paczkę z dołożonym wydarzeniem.</summary>
    /// <param name="gameEvent">Wydarzenie do dodania.</param>
    /// <exception cref="InvalidOperationException">Gdy paczka jest wbudowana.</exception>
    public EventPack WithEvent(GameEvent gameEvent)
    {
        ArgumentNullException.ThrowIfNull(gameEvent);
        EnsureEditable();

        return this with { Events = [.. Events, gameEvent] };
    }

    /// <summary>Zwraca paczkę bez wskazanego wydarzenia.</summary>
    /// <param name="eventId">Identyfikator wydarzenia.</param>
    /// <exception cref="InvalidOperationException">Gdy paczka jest wbudowana.</exception>
    public EventPack WithoutEvent(Guid eventId)
    {
        EnsureEditable();

        return this with { Events = [.. Events.Where(candidate => candidate.Id != eventId)] };
    }

    /// <summary>Zwraca paczkę z podmienionym wydarzeniem.</summary>
    /// <param name="gameEvent">Wydarzenie w nowej postaci; dopasowywane po identyfikatorze.</param>
    /// <exception cref="InvalidOperationException">Gdy paczka jest wbudowana.</exception>
    public EventPack WithUpdatedEvent(GameEvent gameEvent)
    {
        ArgumentNullException.ThrowIfNull(gameEvent);
        EnsureEditable();

        return this with
        {
            Events = [.. Events.Select(candidate => candidate.Id == gameEvent.Id ? gameEvent : candidate)],
        };
    }

    /// <summary>
    /// Sprawdza, że paczkę wolno zmieniać.
    /// </summary>
    /// <remarks>
    /// Reguła jest pilnowana w modelu, a nie tylko w interfejsie: paczki wbudowane są
    /// czytane z zasobów aplikacji, więc zmiana i tak nie miałaby gdzie zostać zapisana.
    /// Lepiej, żeby próba skończyła się jasnym wyjątkiem niż cichym brakiem efektu.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Gdy paczka jest wbudowana.</exception>
    private void EnsureEditable()
    {
        if (IsBuiltIn)
        {
            throw new InvalidOperationException(
                $"Paczka wbudowana „{Name}” nie podlega edycji. Zrób jej kopię i zmieniaj kopię.");
        }
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"{Name} ({Events.Count} wydarzeń{(IsBuiltIn ? ", wbudowana" : string.Empty)})";
}
