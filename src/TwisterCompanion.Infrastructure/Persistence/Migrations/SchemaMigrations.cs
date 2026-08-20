using System.Text.Json.Nodes;

namespace TwisterCompanion.Infrastructure.Persistence.Migrations;

/// <summary>
/// Rejestr migracji schematu, stosowanych po kolei aż do wersji aktualnej.
/// </summary>
/// <remarks>
/// Podnosząc <see cref="PersistenceSchema.CurrentVersion"/>, dopisz tutaj krok
/// z odpowiednim <see cref="ISchemaMigration.FromVersion"/>. Brak kroku dla napotkanej
/// wersji jest traktowany jako dokument nieczytelny — aplikacja wróci do wartości
/// domyślnych, zamiast wywalić się przy starcie.
/// </remarks>
internal static class SchemaMigrations
{
    /// <summary>Nazwa właściwości z wersją schematu w zapisanym dokumencie.</summary>
    public const string VersionPropertyName = "schemaVersion";

    /// <summary>Wszystkie znane migracje.</summary>
    public static IReadOnlyList<ISchemaMigration> All { get; } =
    [
        new StampSchemaVersionMigration(),
    ];

    /// <summary>
    /// Odczytuje wersję dokumentu. Brak pola oznacza dokument nieoznaczony wersją.
    /// </summary>
    /// <param name="document">Korzeń dokumentu JSON.</param>
    public static int ReadVersion(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.TryGetPropertyValue(VersionPropertyName, out JsonNode? versionNode)
            || versionNode is null)
        {
            return PersistenceSchema.UnversionedDocumentVersion;
        }

        return versionNode.GetValueKind() == System.Text.Json.JsonValueKind.Number
            ? versionNode.GetValue<int>()
            : PersistenceSchema.UnversionedDocumentVersion;
    }
}

/// <summary>
/// Podnosi dokument bez oznaczenia wersji do wersji 1.
/// </summary>
/// <remarks>
/// Dokumenty bez pola <c>schemaVersion</c> mogą pochodzić z buildów sprzed wprowadzenia
/// wersjonowania. Ich układ jest zgodny z wersją 1, więc wystarczy je oznaczyć — cała
/// reszta danych zostaje bez zmian.
/// </remarks>
internal sealed class StampSchemaVersionMigration : ISchemaMigration
{
    /// <inheritdoc />
    public int FromVersion => PersistenceSchema.UnversionedDocumentVersion;

    /// <inheritdoc />
    public int ToVersion => 1;

    /// <inheritdoc />
    public void Apply(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document[SchemaMigrations.VersionPropertyName] = ToVersion;
    }
}
