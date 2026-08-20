using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Infrastructure.Persistence.Dto;

namespace TwisterCompanion.Infrastructure.Persistence.Mapping;

/// <summary>
/// Przekłada listę graczy między postacią zapisaną a modelem domenowym.
/// </summary>
internal static class PlayerMapper
{
    /// <summary>Buduje listę graczy z odczytanego dokumentu, pomijając wpisy niepoprawne.</summary>
    /// <param name="dto">Odczytany dokument.</param>
    public static IReadOnlyList<Player> ToDomain(PlayerRosterDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        List<Player> players = [];

        foreach (PlayerDto playerDto in dto.Players)
        {
            if (playerDto.Id == Guid.Empty
                || string.IsNullOrWhiteSpace(playerDto.Name)
                || playerDto.Order < 0)
            {
                continue;
            }

            players.Add(new Player
            {
                Id = playerDto.Id,
                Name = playerDto.Name,
                Order = playerDto.Order,
            });
        }

        return [.. players.OrderBy(player => player.Order)];
    }

    /// <summary>Buduje dokument do zapisu na podstawie listy graczy.</summary>
    /// <param name="players">Gracze do zapamiętania.</param>
    public static PlayerRosterDto ToDto(IReadOnlyList<Player> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        return new PlayerRosterDto
        {
            SchemaVersion = PersistenceSchema.CurrentVersion,
            Players =
            [
                .. players.Select(player => new PlayerDto
                {
                    Id = player.Id,
                    Name = player.Name,
                    Order = player.Order,
                }),
            ],
        };
    }
}
