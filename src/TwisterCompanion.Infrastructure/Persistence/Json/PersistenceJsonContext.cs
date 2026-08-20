using System.Text.Json.Serialization;
using TwisterCompanion.Infrastructure.Persistence.Dto;

namespace TwisterCompanion.Infrastructure.Persistence.Json;

/// <summary>
/// Kontekst serializacji generowany w czasie kompilacji.
/// </summary>
/// <remarks>
/// Źródłowo generowany kontekst zamiast domyślnej serializacji przez refleksję.
/// Powód praktyczny: aplikacja mobilna jest budowana z przycinaniem kodu (trimming),
/// a serializacja refleksyjna wywala się wtedy w czasie działania, na urządzeniu,
/// w sposób trudny do odtworzenia. Kontekst generowany rozwiązuje to na etapie kompilacji.
/// <para>
/// Enumy zapisujemy jako tekst, a nazwy właściwości w <c>camelCase</c> — plik ma być
/// czytelny, bo użytkownik może chcieć podejrzeć albo poprawić paczkę wydarzeń ręcznie
/// (import i eksport paczek to Etap 6).
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(EventPackDto))]
[JsonSerializable(typeof(PlayerRosterDto))]
[JsonSerializable(typeof(AppSettingsDto))]
[JsonSerializable(typeof(GameSessionDto))]
[JsonSerializable(typeof(GameModeCatalogDto))]
internal sealed partial class PersistenceJsonContext : JsonSerializerContext;
