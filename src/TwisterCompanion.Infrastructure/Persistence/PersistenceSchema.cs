namespace TwisterCompanion.Infrastructure.Persistence;

/// <summary>
/// Stałe opisujące format i rozmieszczenie danych na dysku.
/// </summary>
internal static class PersistenceSchema
{
    /// <summary>
    /// Aktualna wersja schematu zapisywanych dokumentów.
    /// </summary>
    /// <remarks>
    /// Podnosimy przy każdej zmianie formatu, która nie jest zgodna wstecz, i dopisujemy
    /// migrację do <c>SchemaMigrations</c>. Dokument bez pola z wersją traktujemy jako
    /// wersję 0.
    /// </remarks>
    public const int CurrentVersion = 1;

    /// <summary>Wersja przypisywana dokumentom, które nie mają pola z wersją.</summary>
    public const int UnversionedDocumentVersion = 0;

    /// <summary>Katalog z paczkami wydarzeń użytkownika.</summary>
    public const string PacksDirectoryName = "packs";

    /// <summary>Plik z zapamiętaną listą graczy.</summary>
    public const string PlayerRosterFileName = "players.json";

    /// <summary>Plik z ustawieniami aplikacji.</summary>
    public const string SettingsFileName = "settings.json";

    /// <summary>Plik z zapisem przerwanej partii.</summary>
    public const string GameSessionFileName = "session.json";

    /// <summary>Rozszerzenie plików z danymi.</summary>
    public const string FileExtension = ".json";

    /// <summary>
    /// Rozszerzenie pliku tymczasowego używanego przy zapisie atomowym.
    /// </summary>
    public const string TemporaryFileExtension = ".tmp";
}
