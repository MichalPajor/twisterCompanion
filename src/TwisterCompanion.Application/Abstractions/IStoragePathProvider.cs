namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Wskazuje katalog, w którym aplikacja przechowuje swoje dane.
/// </summary>
/// <remarks>
/// Jedyna rzecz w persystencji, która zależy od platformy. Sam zapis i odczyt plików
/// odbywa się przez <c>System.IO</c>, które działa wszędzie — problemem jest tylko
/// <i>gdzie</i> te pliki umieścić, bo to wie wyłącznie MAUI
/// (<c>FileSystem.AppDataDirectory</c>).
/// <para>
/// Dzięki tej abstrakcji cała warstwa <c>Infrastructure</c> pozostaje platformowo
/// neutralna i daje się testować na zwykłym katalogu tymczasowym — a testy zapisu,
/// odczytu i migracji są wymogiem Etapu 3.
/// </para>
/// </remarks>
public interface IStoragePathProvider
{
    /// <summary>
    /// Katalog danych aplikacji. Musi istnieć albo dać się utworzyć.
    /// </summary>
    string AppDataDirectory { get; }
}
