using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Infrastructure.Persistence.Dto;

/// <summary>
/// Postać paczki wydarzeń zapisywana w pliku JSON.
/// </summary>
/// <remarks>
/// Znacznik „paczka wbudowana" nie jest zapisywany. Wynika ze źródła: paczki wbudowane
/// są czytane z zasobów osadzonych w aplikacji, a wszystko z katalogu danych użytkownika
/// jest edytowalne. Dzięki temu nie da się ręczną edycją pliku podszyć pod paczkę
/// wbudowaną i zablokować sobie jej usunięcia.
/// </remarks>
internal sealed class EventPackDto
{
    /// <summary>Wersja schematu dokumentu.</summary>
    public int SchemaVersion { get; set; } = PersistenceSchema.CurrentVersion;

    /// <summary>Identyfikator paczki.</summary>
    public Guid Id { get; set; }

    /// <summary>Nazwa paczki.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Klucz zasobu z nazwą — dla paczek wbudowanych.</summary>
    public string? NameKey { get; set; }

    /// <summary>Klasyfikacja wiekowa zawartości.</summary>
    public EventPackAgeRating AgeRating { get; set; } = EventPackAgeRating.Everyone;

    /// <summary>Wydarzenia należące do paczki.</summary>
    public List<GameEventDto> Events { get; set; } = [];
}
