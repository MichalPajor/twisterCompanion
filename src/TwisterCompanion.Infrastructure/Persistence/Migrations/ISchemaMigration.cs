using System.Text.Json.Nodes;

namespace TwisterCompanion.Infrastructure.Persistence.Migrations;

/// <summary>
/// Pojedynczy krok podnoszący dokument JSON z jednej wersji schematu do następnej.
/// </summary>
/// <remarks>
/// Migracja działa na drzewie JSON, a nie na obiektach DTO. To istotne: DTO opisuje
/// <i>aktualny</i> format, więc starego dokumentu często nie da się w nie wczytać.
/// Poprawianie surowego JSON-a przed deserializacją pozwala czytać dane zapisane przez
/// każdą poprzednią wersję aplikacji.
/// </remarks>
internal interface ISchemaMigration
{
    /// <summary>Wersja dokumentu, którą ta migracja przyjmuje.</summary>
    int FromVersion { get; }

    /// <summary>Wersja dokumentu, którą ta migracja produkuje.</summary>
    int ToVersion { get; }

    /// <summary>Przekształca dokument w miejscu.</summary>
    /// <param name="document">Korzeń dokumentu JSON.</param>
    void Apply(JsonObject document);
}
