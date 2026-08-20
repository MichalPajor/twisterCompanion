using TwisterCompanion.Application.Settings;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.GameModes;

namespace TwisterCompanion.Application.Game;

/// <summary>
/// Zasady, na jakich rozpocznie się partia — bez składu graczy.
/// </summary>
/// <remarks>
/// Powstało dla ekranu przed grą: gracze muszą <b>przed</b> rozpoczęciem wiedzieć, w jakim
/// trybie zagrają, z jakim zestawem wydarzeń i z jakimi czasami. Wcześniej ekran startowy
/// wypisywał tylko imiona, więc nie odpowiadał na żadne z tych pytań.
/// <para>
/// Typ jest osobny od <see cref="GameConfiguration"/> z jednego powodu: konfiguracja wymaga
/// co najmniej jednego gracza, a podsumowanie ma się pokazać także wtedy, gdy skład jest
/// jeszcze pusty. <see cref="GameConfiguration.FromSettings"/> buduje się z tego samego
/// podsumowania, więc ekran nie może pokazać innych zasad niż te, na jakich partia ruszy —
/// jest jedno miejsce, które je wylicza.
/// </para>
/// </remarks>
public sealed record GameSetup
{
    /// <summary>Klucz trybu gry.</summary>
    public required string GameModeKey { get; init; }

    /// <summary>Czas na wykonanie ruchu po przeskalowaniu mnożnikiem trybu.</summary>
    public required TimeSpan MoveTime { get; init; }

    /// <summary>Czas na zadanie z wydarzenia po przeskalowaniu mnożnikiem trybu.</summary>
    public required TimeSpan TaskTime { get; init; }

    /// <summary>Sposób przechodzenia do następnej tury.</summary>
    public required TurnAdvanceMode TurnAdvanceMode { get; init; }

    /// <summary>
    /// Czy sterowanie głosem będzie działać w tej partii.
    /// </summary>
    /// <remarks>
    /// Nie to samo co ustawienie: w trybie automatycznym nasłuch się nie uruchamia, bo nie
    /// ma czym sterować. Tutaj jest odpowiedź na pytanie „czy w tej partii będę mówił", a nie
    /// „co jest zaznaczone w ustawieniach".
    /// </remarks>
    public required bool IsVoiceControlEnabled { get; init; }

    /// <summary>Zasada odpadania graczy.</summary>
    public required EliminationRule EliminationRule { get; init; }

    /// <summary>
    /// Paczka wydarzeń obowiązująca w partii albo <see langword="null"/>, gdy gramy
    /// bez wydarzeń.
    /// </summary>
    public EventPack? EventPack { get; init; }

    /// <summary>
    /// Czy w partii mogą pojawić się wydarzenia.
    /// </summary>
    /// <remarks>
    /// Sama paczka nie wystarcza: tryb z zerowym mnożnikiem szans wyłącza wydarzenia
    /// niezależnie od tego, co gracz wybrał.
    /// </remarks>
    public bool AreEventsEnabled { get; init; }

    /// <summary>Wylicza zasady partii z ustawień i wybranego trybu gry.</summary>
    /// <param name="settings">Ustawienia aplikacji.</param>
    /// <param name="mode">Wybrany tryb gry albo <see langword="null"/> dla nastaw domyślnych.</param>
    /// <param name="eventPack">Paczka wydarzeń, jeśli jakaś obowiązuje.</param>
    /// <remarks>
    /// Podział wpływów między trybem a ustawieniami opisuje
    /// <see cref="GameConfiguration.FromSettings"/> — tutaj jest jego wykonanie.
    /// </remarks>
    public static GameSetup FromSettings(
        AppSettings settings,
        GameModeDefinition? mode = null,
        EventPack? eventPack = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        double eventChance = mode?.EventSelectionOptions.ChanceMultiplier ?? 1.0;

        return new GameSetup
        {
            GameModeKey = mode?.Key ?? settings.GameModeKey,
            TurnAdvanceMode = settings.TurnAdvanceMode,
            MoveTime = ResolveMoveTime(settings, mode),
            TaskTime = Scale(settings.TaskTime, mode?.TaskTimeMultiplier ?? 1.0),
            IsVoiceControlEnabled = settings.IsVoiceControlEnabled
                && settings.TurnAdvanceMode == TurnAdvanceMode.Manual,
            EliminationRule = mode?.EliminationRule ?? EliminationRule.Manual,
            EventPack = eventPack,
            AreEventsEnabled = eventPack is not null && eventChance > 0.0,
        };
    }

    /// <summary>
    /// Ustala, co odmierza odliczanie po odczytaniu polecenia ruchu.
    /// </summary>
    /// <remarks>
    /// Przy sterowaniu głosem w trybie ręcznym liczy się jedna chwila: otwarcie nasłuchu.
    /// Odliczanie musi na niej kończyć się co do sekundy, więc bierze tę samą wartość
    /// i bez mnożnika trybu — inaczej liczba na ekranie rozjeżdżałaby się z sygnałem
    /// dźwiękowym i przestałaby cokolwiek znaczyć.
    /// </remarks>
    private static TimeSpan ResolveMoveTime(AppSettings settings, GameModeDefinition? mode) =>
        settings.TurnAdvanceMode == TurnAdvanceMode.Manual && settings.IsVoiceControlEnabled
            ? settings.VoiceListeningDelay
            : Scale(settings.MoveTime, mode?.MoveTimeMultiplier ?? 1.0);

    /// <summary>
    /// Skaluje czas mnożnikiem trybu, nie schodząc poniżej sekundy.
    /// </summary>
    /// <remarks>
    /// Dolna granica jest po to, żeby mnożnik nie mógł dać zera: czas zerowy oznaczałby
    /// turę, która kończy się przed odczytaniem polecenia.
    /// </remarks>
    private static TimeSpan Scale(TimeSpan value, double multiplier)
    {
        TimeSpan scaled = value * multiplier;

        return scaled < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : scaled;
    }
}
