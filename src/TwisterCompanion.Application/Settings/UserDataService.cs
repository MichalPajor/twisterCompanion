using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Application.Settings;

/// <summary>
/// Kasowanie i przywracanie danych użytkownika.
/// </summary>
/// <remarks>
/// Kolejność kasowania nie jest przypadkowa: najpierw idzie zapis partii, potem skład i paczki,
/// a ustawienia na końcu. Gdyby ustawienia poszły pierwsze, wskazywałyby przez chwilę na paczkę
/// wydarzeń, której już nie ma — a aplikacja przerwana w tym momencie (system może zabić proces
/// w tle w dowolnej chwili) wstałaby z niespójnym stanem.
/// <para>
/// Każdy krok jest osobno pochłaniany: nieudane skasowanie jednej rzeczy nie może zatrzymać
/// kasowania pozostałych. Użytkownik poprosił o wyczyszczenie wszystkiego, więc dostaje tyle,
/// ile da się wyczyścić.
/// </para>
/// </remarks>
internal sealed class UserDataService : IUserDataService
{
    private readonly ISettingsService _settings;
    private readonly IPlayerRosterRepository _roster;
    private readonly IEventPackService _eventPacks;
    private readonly IGameSessionRepository _sessions;
    private readonly ILogger<UserDataService> _logger;

    /// <summary>Tworzy serwis danych użytkownika.</summary>
    /// <param name="settings">Ustawienia aplikacji.</param>
    /// <param name="roster">Skład graczy.</param>
    /// <param name="eventPacks">Paczki wydarzeń.</param>
    /// <param name="sessions">Zapis przerwanej partii.</param>
    /// <param name="logger">Logger.</param>
    public UserDataService(
        ISettingsService settings,
        IPlayerRosterRepository roster,
        IEventPackService eventPacks,
        IGameSessionRepository sessions,
        ILogger<UserDataService> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentNullException.ThrowIfNull(eventPacks);
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(logger);

        _settings = settings;
        _roster = roster;
        _eventPacks = eventPacks;
        _sessions = sessions;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task ResetSettingsAsync(CancellationToken cancellationToken = default) =>
        _settings.ResetAsync(cancellationToken);

    /// <inheritdoc />
    public async Task EraseAsync(CancellationToken cancellationToken = default)
    {
        await EraseStepAsync("zapis partii", () => _sessions.ClearAsync(cancellationToken));
        await EraseStepAsync("skład graczy", () => _roster.ClearAsync(cancellationToken));
        await EraseStepAsync("własne paczki wydarzeń", () => EraseCustomPacksAsync(cancellationToken));
        await EraseStepAsync("ustawienia", () => _settings.ResetAsync(cancellationToken));
    }

    /// <summary>
    /// Usuwa paczki utworzone przez użytkownika.
    /// </summary>
    /// <remarks>
    /// Paczki wbudowane zostają: to zawartość aplikacji, a nie dane użytkownika — tak samo jak
    /// teksty interfejsu, których „usuń moje dane" też nie kasuje.
    /// </remarks>
    private async Task EraseCustomPacksAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<EventPack> packs = await _eventPacks.GetAllAsync(cancellationToken);

        foreach (EventPack pack in packs.Where(pack => !pack.IsBuiltIn))
        {
            await _eventPacks.DeleteAsync(pack.Id, cancellationToken);
        }
    }

    private async Task EraseStepAsync(string description, Func<Task> step)
    {
        try
        {
            await step();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Nie udało się usunąć: {Description}.", description);
        }
    }
}
