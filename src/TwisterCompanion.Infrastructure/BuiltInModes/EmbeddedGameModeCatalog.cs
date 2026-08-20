using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.EventSelection;
using TwisterCompanion.Domain.GameModes;
using TwisterCompanion.Domain.MoveSelection;
using TwisterCompanion.Infrastructure.Persistence.Dto;
using TwisterCompanion.Infrastructure.Persistence.Json;

namespace TwisterCompanion.Infrastructure.BuiltInModes;

/// <summary>
/// Katalog trybów gry czytany z pliku osadzonego w bibliotece.
/// </summary>
/// <remarks>
/// Definicje są danymi, więc dołożenie trybu nie wymaga rekompilacji logiki — wystarczy wpis
/// w <c>modes.json</c> i dwa klucze tłumaczeń. Plik jest osadzony, a nie kopiowany do katalogu
/// użytkownika: tryb bez odpowiadających mu tekstów nie miałby czym się przedstawić, więc
/// definicje i tłumaczenia muszą jechać razem z wersją aplikacji.
/// <para>
/// <b>Uszkodzony albo brakujący plik nie zatrzymuje aplikacji.</b> Zostaje tryb awaryjny
/// zbudowany w kodzie — gra bez trybów nie miałaby jak się rozpocząć, a to gorsze niż gra
/// z jednym trybem.
/// </para>
/// </remarks>
internal sealed class EmbeddedGameModeCatalog : IGameModeCatalog
{
    private const string ResourceName = "TwisterCompanion.Infrastructure.Resources.GameModes.modes.json";

    /// <summary>Klucz trybu klasycznego — jego brak w pliku byłby błędem definicji.</summary>
    private const string ClassicKey = "classic";

    private readonly ILogger<EmbeddedGameModeCatalog> _logger;
    private readonly Lock _guard = new();

    private IReadOnlyList<GameModeDefinition>? _modes;

    /// <summary>Tworzy katalog trybów.</summary>
    /// <param name="logger">Logger.</param>
    public EmbeddedGameModeCatalog(ILogger<EmbeddedGameModeCatalog> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    /// <inheritdoc />
    public GameModeDefinition Default =>
        Find(ClassicKey) ?? All.FirstOrDefault(mode => mode.IsEnabled) ?? Fallback;

    /// <inheritdoc />
    public IReadOnlyList<GameModeDefinition> GetAvailable() => [.. All.Where(mode => mode.IsEnabled)];

    /// <inheritdoc />
    public GameModeDefinition? Find(string key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : All.FirstOrDefault(mode => string.Equals(mode.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Wszystkie tryby, także wyłączone. Wynik jest zapamiętywany.</summary>
    private IReadOnlyList<GameModeDefinition> All
    {
        get
        {
            lock (_guard)
            {
                return _modes ??= Load();
            }
        }
    }

    /// <summary>
    /// Tryb awaryjny, gdy definicji nie da się wczytać.
    /// </summary>
    /// <remarks>
    /// Odpowiada trybowi klasycznemu: nastawy domyślne, bez wydarzeń, ręczne zgłaszanie
    /// odpadnięcia. Klucze tekstów są te same, więc jeśli zawiódł tylko plik definicji,
    /// gracz nawet tego nie zauważy.
    /// </remarks>
    private static GameModeDefinition Fallback { get; } = new()
    {
        Key = ClassicKey,
        NameKey = "GameMode_Classic_Name",
        DescriptionKey = "GameMode_Classic_Description",
        RulesKey = "GameMode_Classic_Rules",
        EventSelectionOptions = EventSelectionOptions.Disabled,
    };

    private IReadOnlyList<GameModeDefinition> Load()
    {
        try
        {
            using Stream? stream = typeof(EmbeddedGameModeCatalog).Assembly
                .GetManifestResourceStream(ResourceName);

            if (stream is null)
            {
                _logger.LogError("Brak zasobu {Resource} z definicjami trybów gry.", ResourceName);

                return [Fallback];
            }

            GameModeCatalogDto? catalog = JsonSerializer.Deserialize(
                stream,
                PersistenceJsonContext.Default.GameModeCatalogDto);

            List<GameModeDefinition> modes = [];

            foreach (GameModeDto dto in catalog?.Modes ?? [])
            {
                GameModeDefinition? mode = Map(dto);

                if (mode is not null)
                {
                    modes.Add(mode);
                }
            }

            if (modes.Count == 0)
            {
                _logger.LogError("Plik definicji trybów gry nie zawiera żadnego poprawnego trybu.");

                return [Fallback];
            }

            _logger.LogInformation("Wczytano {Count} trybów gry.", modes.Count);

            return modes;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Nie udało się wczytać definicji trybów gry.");

            return [Fallback];
        }
    }

    /// <summary>
    /// Przekłada wpis z pliku na definicję trybu.
    /// </summary>
    /// <remarks>
    /// Wpis bez klucza albo bez nazwy jest pomijany, a nie zgłaszany wyjątkiem: jeden zepsuty
    /// tryb nie może zabrać graczom pozostałych.
    /// </remarks>
    private GameModeDefinition? Map(GameModeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Key) || string.IsNullOrWhiteSpace(dto.NameKey))
        {
            _logger.LogWarning("Pominięty tryb gry bez klucza albo nazwy: {Key}.", dto.Key);

            return null;
        }

        try
        {
            return new GameModeDefinition
            {
                Key = dto.Key,
                NameKey = dto.NameKey,
                DescriptionKey = dto.DescriptionKey,
                RulesKey = dto.RulesKey,
                IsEnabled = dto.IsEnabled,
                EliminationRule = ParseEliminationRule(dto.EliminationRule),
                DefaultEventPackNameKey = dto.DefaultEventPackNameKey,
                MoveTimeMultiplier = dto.MoveTimeMultiplier ?? 1.0,
                TaskTimeMultiplier = dto.TaskTimeMultiplier ?? 1.0,
                EventSelectionOptions = MapEventSelection(dto),
                MoveSelectionOptions = MapMoveSelection(dto.MoveSelection),
            };
        }
        catch (ArgumentException exception)
        {
            // Wartość spoza dopuszczalnego zakresu w pliku definicji — pomijamy tryb,
            // zamiast pozwolić mu zepsuć losowanie.
            _logger.LogWarning(exception, "Pominięty tryb gry {Key} z niepoprawnymi nastawami.", dto.Key);

            return null;
        }
    }

    private EliminationRule ParseEliminationRule(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EliminationRule.Manual;
        }

        if (Enum.TryParse(value, ignoreCase: true, out EliminationRule rule))
        {
            return rule;
        }

        _logger.LogWarning("Nieznana zasada odpadania „{Rule}” — obowiązuje ręczna.", value);

        return EliminationRule.Manual;
    }

    private static EventSelectionOptions MapEventSelection(GameModeDto dto)
    {
        EventSelectionOptions defaults = EventSelectionOptions.Default;

        return new EventSelectionOptions
        {
            ChanceMultiplier = dto.EventChanceMultiplier ?? defaults.ChanceMultiplier,
        };
    }

    private static MoveSelectionOptions MapMoveSelection(MoveSelectionDto? dto)
    {
        MoveSelectionOptions defaults = MoveSelectionOptions.Default;

        if (dto is null)
        {
            return defaults;
        }

        return new MoveSelectionOptions
        {
            TabooWindowSize = dto.TabooWindowSize ?? defaults.TabooWindowSize,
            TabooWeightMultiplier = dto.TabooWeightMultiplier ?? defaults.TabooWeightMultiplier,
            RecencyDecay = dto.RecencyDecay ?? defaults.RecencyDecay,
            MaxSameBodyPartStreak = dto.MaxSameBodyPartStreak ?? defaults.MaxSameBodyPartStreak,
            SameBodyPartStreakMultiplier =
                dto.SameBodyPartStreakMultiplier ?? defaults.SameBodyPartStreakMultiplier,
            MaxSameColorStreak = dto.MaxSameColorStreak ?? defaults.MaxSameColorStreak,
            SameColorStreakMultiplier = dto.SameColorStreakMultiplier ?? defaults.SameColorStreakMultiplier,
            RedundantMoveMultiplier = dto.RedundantMoveMultiplier ?? defaults.RedundantMoveMultiplier,
            HistoryLength = dto.HistoryLength ?? defaults.HistoryLength,
        };
    }
}
