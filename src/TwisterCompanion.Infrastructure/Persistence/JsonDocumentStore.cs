using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Infrastructure.Persistence.Migrations;

namespace TwisterCompanion.Infrastructure.Persistence;

/// <summary>
/// Odczyt i zapis dokumentów JSON: wersjonowanie schematu, migracje i zapis atomowy.
/// </summary>
/// <remarks>
/// Wspólna warstwa dla wszystkich repozytoriów, żeby logika „co zrobić z uszkodzonym
/// plikiem" istniała w jednym miejscu, a nie w każdym repozytorium osobno.
/// <para>
/// Zasada odczytu: <b>żaden uszkodzony plik nie może zablokować startu aplikacji</b>.
/// Nieczytelny dokument to <see langword="null"/> i wpis w logu, a decyzję o wartościach
/// zastępczych podejmuje repozytorium.
/// </para>
/// </remarks>
internal sealed class JsonDocumentStore(ILogger<JsonDocumentStore> logger)
{
    /// <summary>
    /// Wczytuje dokument, w razie potrzeby migrując go do aktualnego schematu.
    /// </summary>
    /// <typeparam name="TDocument">Typ dokumentu.</typeparam>
    /// <param name="path">Pełna ścieżka pliku.</param>
    /// <param name="typeInfo">Metadane serializacji dokumentu.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns>
    /// Wczytany dokument albo <see langword="null"/>, gdy plik nie istnieje, jest
    /// uszkodzony, pochodzi z nowszej wersji aplikacji lub nie ma dla niego migracji.
    /// </returns>
    public async Task<TDocument?> ReadAsync<TDocument>(
        string path,
        JsonTypeInfo<TDocument> typeInfo,
        CancellationToken cancellationToken = default)
        where TDocument : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(typeInfo);

        if (!File.Exists(path))
        {
            return null;
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Nie udało się odczytać pliku {Path}.", path);
            return null;
        }

        JsonObject? document;
        try
        {
            document = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Plik {Path} nie jest poprawnym dokumentem JSON.", path);
            return null;
        }

        if (document is null)
        {
            logger.LogWarning("Plik {Path} nie zawiera obiektu JSON.", path);
            return null;
        }

        if (!TryMigrate(document, path))
        {
            return null;
        }

        try
        {
            return document.Deserialize(typeInfo);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Nie udało się zinterpretować zawartości pliku {Path}.", path);
            return null;
        }
    }

    /// <summary>Zapisuje dokument, zastępując poprzednią zawartość.</summary>
    /// <typeparam name="TDocument">Typ dokumentu.</typeparam>
    /// <param name="path">Pełna ścieżka pliku.</param>
    /// <param name="document">Dokument do zapisania.</param>
    /// <param name="typeInfo">Metadane serializacji dokumentu.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Zapis jest atomowy: najpierw plik tymczasowy, potem podmiana. Bez tego przerwanie
    /// zapisu — na przykład zamknięciem aplikacji przez system — zostawiłoby obciętą
    /// paczkę wydarzeń, czyli utratę danych użytkownika.
    /// </remarks>
    public async Task WriteAsync<TDocument>(
        string path,
        TDocument document,
        JsonTypeInfo<TDocument> typeInfo,
        CancellationToken cancellationToken = default)
        where TDocument : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(typeInfo);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = path + PersistenceSchema.TemporaryFileExtension;

        try
        {
            string json = JsonSerializer.Serialize(document, typeInfo);
            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw;
        }
    }

    /// <summary>Usuwa plik, jeśli istnieje.</summary>
    /// <param name="path">Pełna ścieżka pliku.</param>
    /// <returns><see langword="true"/>, jeśli plik istniał i został usunięty.</returns>
    public bool Delete(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Nie udało się usunąć pliku {Path}.", path);
            return false;
        }
    }

    /// <summary>Podnosi dokument do aktualnej wersji schematu.</summary>
    /// <returns><see langword="false"/>, gdy dokumentu nie da się doprowadzić do aktualnej wersji.</returns>
    private bool TryMigrate(JsonObject document, string path)
    {
        int version = SchemaMigrations.ReadVersion(document);

        if (version > PersistenceSchema.CurrentVersion)
        {
            // Plik zapisany przez nowszą wersję aplikacji. Próba odczytu mogłaby
            // zinterpretować dane błędnie i nadpisać je przy najbliższym zapisie.
            logger.LogWarning(
                "Plik {Path} ma wersję schematu {FileVersion}, a aplikacja obsługuje {SupportedVersion}. Pominięty.",
                path,
                version,
                PersistenceSchema.CurrentVersion);

            return false;
        }

        while (version < PersistenceSchema.CurrentVersion)
        {
            ISchemaMigration? migration = SchemaMigrations.All
                .FirstOrDefault(candidate => candidate.FromVersion == version);

            if (migration is null)
            {
                logger.LogError(
                    "Brak migracji z wersji {FileVersion} dla pliku {Path}. Dokument pominięty.",
                    version,
                    path);

                return false;
            }

            migration.Apply(document);
            version = migration.ToVersion;

            logger.LogInformation(
                "Plik {Path} zmigrowany do wersji schematu {Version}.",
                path,
                version);
        }

        return true;
    }

    private void TryDeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(
                exception,
                "Nie udało się usunąć pliku tymczasowego {Path}.",
                temporaryPath);
        }
    }
}
