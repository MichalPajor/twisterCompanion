using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.ValueObjects;
using TwisterCompanion.Infrastructure.Persistence.Dto;

namespace TwisterCompanion.Infrastructure.Persistence.Mapping;

/// <summary>
/// Przekłada paczki wydarzeń między postacią zapisaną a modelem domenowym.
/// </summary>
/// <remarks>
/// Mapowanie jest miejscem, w którym dane z dysku stają się modelem — i dlatego tutaj
/// odbywa się obrona przed wartościami niemożliwymi. Plik może zostać ręcznie
/// zmodyfikowany albo pochodzić z importu (Etap 6), więc traktujemy go jak dane
/// z zewnątrz: szansa poza zakresem jest przycinana, wpis bez żadnej nazwy pomijany.
/// Aplikacja ma się uruchomić i pokazać to, co da się odczytać.
/// </remarks>
internal static class EventPackMapper
{
    /// <summary>Buduje model paczki na podstawie danych z pliku.</summary>
    /// <param name="dto">Odczytany dokument.</param>
    /// <param name="isBuiltIn">Czy paczka pochodzi z zasobów aplikacji.</param>
    /// <returns>Paczka gotowa do użycia albo <see langword="null"/>, gdy danych nie da się użyć.</returns>
    public static EventPack? ToDomain(EventPackDto dto, bool isBuiltIn)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Id == Guid.Empty || string.IsNullOrWhiteSpace(dto.Name))
        {
            return null;
        }

        List<GameEvent> events = [];

        foreach (GameEventDto eventDto in dto.Events)
        {
            GameEvent? gameEvent = ToDomain(eventDto);

            if (gameEvent is not null)
            {
                events.Add(gameEvent);
            }
        }

        return new EventPack
        {
            Id = dto.Id,
            Name = dto.Name,
            NameKey = dto.NameKey,
            IsBuiltIn = isBuiltIn,
            AgeRating = Enum.IsDefined(dto.AgeRating) ? dto.AgeRating : EventPackAgeRating.Everyone,
            Events = events,
        };
    }

    /// <summary>Buduje dokument do zapisu na podstawie modelu.</summary>
    /// <param name="pack">Paczka do zapisania.</param>
    public static EventPackDto ToDto(EventPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        return new EventPackDto
        {
            SchemaVersion = PersistenceSchema.CurrentVersion,
            Id = pack.Id,
            Name = pack.Name,
            NameKey = pack.NameKey,
            AgeRating = pack.AgeRating,
            Events = [.. pack.Events.Select(ToDto)],
        };
    }

    private static GameEvent? ToDomain(GameEventDto dto)
    {
        bool hasName = !string.IsNullOrWhiteSpace(dto.NameKey)
                       || !string.IsNullOrWhiteSpace(dto.CustomName);

        if (dto.Id == Guid.Empty || !hasName)
        {
            return null;
        }

        double chancePercent = Math.Clamp(
            dto.ChancePercent,
            Probability.MinPercent,
            Probability.MaxPercent);

        return new GameEvent
        {
            Id = dto.Id,
            NameKey = dto.NameKey,
            CustomName = dto.CustomName,
            Chance = new Probability(chancePercent),
            IsEnabled = dto.IsEnabled,
            Scope = Enum.IsDefined(dto.Scope) ? dto.Scope : EventScope.CurrentPlayer,
            IsOneShot = dto.IsOneShot,
            CooldownTurns = Math.Max(0, dto.CooldownTurns),
        };
    }

    private static GameEventDto ToDto(GameEvent gameEvent) => new()
    {
        Id = gameEvent.Id,
        NameKey = gameEvent.NameKey,
        CustomName = gameEvent.CustomName,
        ChancePercent = gameEvent.Chance.Percent,
        IsEnabled = gameEvent.IsEnabled,
        Scope = gameEvent.Scope,
        IsOneShot = gameEvent.IsOneShot,
        CooldownTurns = gameEvent.CooldownTurns,
    };
}
