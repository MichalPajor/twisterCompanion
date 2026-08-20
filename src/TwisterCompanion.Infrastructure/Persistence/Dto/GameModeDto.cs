namespace TwisterCompanion.Infrastructure.Persistence.Dto;

/// <summary>
/// Postać definicji trybów gry zapisana w pliku JSON.
/// </summary>
internal sealed class GameModeCatalogDto
{
    /// <summary>Wersja schematu dokumentu.</summary>
    public int SchemaVersion { get; set; } = PersistenceSchema.CurrentVersion;

    /// <summary>Definicje trybów.</summary>
    public List<GameModeDto> Modes { get; set; } = [];
}

/// <summary>
/// Postać jednego trybu gry zapisana w pliku JSON.
/// </summary>
/// <remarks>
/// Wszystkie parametry poza kluczem i nazwą są opcjonalne — tryb, który nie ma zdania
/// w danej sprawie, po prostu pomija wpis, a wtedy obowiązuje wartość domyślna. Dzięki temu
/// definicja trybu w pliku pokazuje <b>czym się różni</b>, a nie powtarza wszystkiego.
/// <para>
/// Czasy są mnożnikami, nie sekundami: użytkownik ustawia jeden czas, który mu pasuje,
/// a tryb go skaluje.
/// </para>
/// </remarks>
internal sealed class GameModeDto
{
    /// <summary>Identyfikator trybu.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Klucz zasobu z nazwą trybu.</summary>
    public string NameKey { get; set; } = string.Empty;

    /// <summary>Klucz zasobu z krótkim opisem.</summary>
    public string? DescriptionKey { get; set; }

    /// <summary>Klucz zasobu z opisem zasad.</summary>
    public string? RulesKey { get; set; }

    /// <summary>Zasada odpadania graczy.</summary>
    public string? EliminationRule { get; set; }

    /// <summary>Mnożnik szans wydarzeń.</summary>
    public double? EventChanceMultiplier { get; set; }

    /// <summary>Klucz nazwy paczki wydarzeń proponowanej przez tryb.</summary>
    public string? DefaultEventPackNameKey { get; set; }

    /// <summary>Mnożnik czasu na wykonanie ruchu.</summary>
    public double? MoveTimeMultiplier { get; set; }

    /// <summary>Mnożnik czasu na wykonanie zadania z wydarzenia.</summary>
    public double? TaskTimeMultiplier { get; set; }

    /// <summary>Czy tryb jest dostępny do wyboru.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Nastawy algorytmu losowania ruchów.</summary>
    public MoveSelectionDto? MoveSelection { get; set; }
}

/// <summary>
/// Postać nastaw algorytmu losowania ruchów zapisana w pliku JSON.
/// </summary>
internal sealed class MoveSelectionDto
{
    /// <summary>Liczba ostatnich ruchów objętych oknem tabu.</summary>
    public int? TabooWindowSize { get; set; }

    /// <summary>Kara dla ruchu powtórzonego w oknie tabu.</summary>
    public double? TabooWeightMultiplier { get; set; }

    /// <summary>Współczynnik wygasania kary za świeżość.</summary>
    public double? RecencyDecay { get; set; }

    /// <summary>Dopuszczalna seria tej samej części ciała.</summary>
    public int? MaxSameBodyPartStreak { get; set; }

    /// <summary>Kara za przekroczenie serii tej samej części ciała.</summary>
    public double? SameBodyPartStreakMultiplier { get; set; }

    /// <summary>Dopuszczalna seria tego samego koloru.</summary>
    public int? MaxSameColorStreak { get; set; }

    /// <summary>Kara za przekroczenie serii tego samego koloru.</summary>
    public double? SameColorStreakMultiplier { get; set; }

    /// <summary>Kara dla ruchu, który niczego nie zmienia.</summary>
    public double? RedundantMoveMultiplier { get; set; }

    /// <summary>Ile ostatnich ruchów przechowywać dla algorytmu.</summary>
    public int? HistoryLength { get; set; }
}
