namespace TwisterCompanion.Infrastructure.Persistence.Dto;

/// <summary>
/// Postać zapamiętanej listy graczy zapisywana w pliku JSON.
/// </summary>
internal sealed class PlayerRosterDto
{
    /// <summary>Wersja schematu dokumentu.</summary>
    public int SchemaVersion { get; set; } = PersistenceSchema.CurrentVersion;

    /// <summary>Zapamiętani gracze.</summary>
    public List<PlayerDto> Players { get; set; } = [];
}

/// <summary>
/// Postać gracza zapisywana w pliku JSON.
/// </summary>
/// <remarks>
/// Bez informacji o eliminacji — zapamiętujemy skład, a nie przebieg rozgrywki.
/// </remarks>
internal sealed class PlayerDto
{
    /// <summary>Identyfikator gracza.</summary>
    public Guid Id { get; set; }

    /// <summary>Nazwa gracza.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Pozycja w kolejce.</summary>
    public int Order { get; set; }
}
