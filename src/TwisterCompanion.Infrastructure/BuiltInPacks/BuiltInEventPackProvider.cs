using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Infrastructure.Persistence.Dto;
using TwisterCompanion.Infrastructure.Persistence.Json;
using TwisterCompanion.Infrastructure.Persistence.Mapping;

namespace TwisterCompanion.Infrastructure.BuiltInPacks;

/// <summary>
/// Dostarcza paczki wydarzeń dołączone do aplikacji.
/// </summary>
/// <remarks>
/// Paczki są osadzone w bibliotece jako zasoby JSON, a nie kopiowane do katalogu
/// użytkownika przy pierwszym uruchomieniu. Trzy powody:
/// <list type="bullet">
/// <item>aktualizacja aplikacji od razu przynosi poprawione paczki — kopia w katalogu
/// użytkownika zostałaby przy starej wersji;</item>
/// <item>użytkownik nie może ich przypadkiem uszkodzić ani usunąć, a mimo to może
/// zrobić własną, edytowalną kopię (<see cref="EventPack.Duplicate"/>);</item>
/// <item>zasób osadzony czyta się przez <see cref="Assembly"/>, więc ta warstwa nie
/// potrzebuje dostępu do plików pakietu aplikacji i pozostaje testowalna.</item>
/// </list>
/// Identyfikatory paczek i wydarzeń są w plikach zapisane na stałe — ustawienia
/// odwołują się do aktywnej paczki po identyfikatorze, więc nie może się on zmieniać
/// przy każdym uruchomieniu.
/// </remarks>
internal sealed class BuiltInEventPackProvider(ILogger<BuiltInEventPackProvider> logger)
{
    private const string ResourceFolder = ".Resources.EventPacks.";

    private IReadOnlyList<EventPack>? _packs;

    /// <summary>Zwraca wszystkie paczki wbudowane, posortowane po nazwie.</summary>
    /// <remarks>Wynik jest zapamiętywany — zasoby nie zmieniają się w czasie działania.</remarks>
    public IReadOnlyList<EventPack> GetAll() => _packs ??= Load();

    /// <summary>Czy podany identyfikator należy do paczki wbudowanej.</summary>
    /// <param name="id">Identyfikator paczki.</param>
    public bool IsBuiltIn(Guid id) => GetAll().Any(pack => pack.Id == id);

    private IReadOnlyList<EventPack> Load()
    {
        Assembly assembly = typeof(BuiltInEventPackProvider).Assembly;
        string prefix = assembly.GetName().Name + ResourceFolder;

        List<EventPack> packs = [];

        foreach (string resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(prefix, StringComparison.Ordinal)
                || !resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            EventPack? pack = LoadPack(assembly, resourceName);

            if (pack is not null)
            {
                packs.Add(pack);
            }
        }

        if (packs.Count == 0)
        {
            logger.LogWarning("Nie znaleziono żadnej paczki wbudowanej wśród zasobów aplikacji.");
        }

        return [.. packs.OrderBy(pack => pack.Name, StringComparer.OrdinalIgnoreCase)];
    }

    private EventPack? LoadPack(Assembly assembly, string resourceName)
    {
        try
        {
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                logger.LogWarning("Nie udało się otworzyć zasobu {Resource}.", resourceName);
                return null;
            }

            EventPackDto? dto = JsonSerializer.Deserialize(
                stream,
                PersistenceJsonContext.Default.EventPackDto);

            if (dto is null)
            {
                logger.LogWarning("Zasób {Resource} jest pusty.", resourceName);
                return null;
            }

            EventPack? pack = EventPackMapper.ToDomain(dto, isBuiltIn: true);

            if (pack is null)
            {
                logger.LogWarning("Zasób {Resource} nie zawiera poprawnej paczki.", resourceName);
            }

            return pack;
        }
        catch (JsonException exception)
        {
            // Zasób jest częścią aplikacji, więc błąd tutaj oznacza pomyłkę w kodzie,
            // a nie uszkodzone dane użytkownika. Logujemy i pomijamy paczkę, żeby
            // jedna literówka w JSON-ie nie blokowała startu całej aplikacji.
            logger.LogError(exception, "Zasób {Resource} ma niepoprawny format JSON.", resourceName);
            return null;
        }
    }
}
