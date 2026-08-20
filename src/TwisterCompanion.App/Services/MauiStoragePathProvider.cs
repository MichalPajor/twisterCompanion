using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Wskazuje katalog danych aplikacji przy użyciu API platformy.
/// </summary>
/// <remarks>
/// Jedyny element persystencji zależny od platformy. Katalog jest prywatny dla aplikacji
/// i usuwany wraz z jej odinstalowaniem — dokładnie tego chcemy dla paczek wydarzeń,
/// listy graczy i ustawień.
/// </remarks>
internal sealed class MauiStoragePathProvider : IStoragePathProvider
{
    /// <inheritdoc />
    public string AppDataDirectory => FileSystem.AppDataDirectory;
}
