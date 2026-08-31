namespace TwisterCompanion.Application.Settings;

/// <summary>
/// Sposób prowadzenia rozgrywki, widziany przez gracza jako jeden wybór z trzech.
/// </summary>
/// <remarks>
/// W ustawieniach te trzy stany są zapisane <b>dwoma</b> przełącznikami: trybem zmiany tury
/// i włącznikiem sterowania głosem. Para wyklucza się wzajemnie — przy turach automatycznych
/// nie ma czym sterować głosem — więc z czterech kombinacji sensowne są trzy. Ten typ nazywa
/// je wprost, żeby ekran rozgrywki nie musiał odtwarzać tej reguły po raz kolejny.
/// </remarks>
public enum GameControlMode
{
    /// <summary>Turę zatwierdzają gracze przyciskiem.</summary>
    Manual,

    /// <summary>Tura zmienia się sama po upływie czasu.</summary>
    Automatic,

    /// <summary>Turą sterują komendy głosowe.</summary>
    Voice,
}

/// <summary>
/// Przejścia między parą przełączników w ustawieniach a jednym wyborem z trzech.
/// </summary>
/// <remarks>
/// Jedno miejsce na regułę wykluczania. Wcześniej mieszkała w dwóch: w ekranie ustawień,
/// który przy włączeniu jednego przełącznika gasił drugi, i w <c>GameSetup</c>, który przy
/// turach automatycznych ignorował sterowanie głosem. Trzecia kopia w przycisku na ekranie
/// rozgrywki byłaby o dwie za dużo.
/// </remarks>
public static class GameControlModes
{
    /// <summary>Wszystkie tryby w kolejności przełączania.</summary>
    private static readonly GameControlMode[] Kolejnosc =
        [GameControlMode.Manual, GameControlMode.Automatic, GameControlMode.Voice];

    /// <summary>Wszystkie tryby w kolejności przełączania.</summary>
    public static IReadOnlyList<GameControlMode> All => Kolejnosc;

    /// <summary>Odczytuje tryb z ustawień.</summary>
    /// <param name="settings">Ustawienia aplikacji.</param>
    public static GameControlMode From(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.TurnAdvanceMode == TurnAdvanceMode.Automatic)
        {
            return GameControlMode.Automatic;
        }

        return settings.IsVoiceControlEnabled ? GameControlMode.Voice : GameControlMode.Manual;
    }

    /// <summary>Zapisuje tryb w ustawieniach, ustawiając oba przełączniki naraz.</summary>
    /// <param name="settings">Ustawienia do zmiany.</param>
    /// <param name="mode">Wybrany tryb.</param>
    public static AppSettings Apply(AppSettings settings, GameControlMode mode)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings with
        {
            TurnAdvanceMode = mode == GameControlMode.Automatic
                ? TurnAdvanceMode.Automatic
                : TurnAdvanceMode.Manual,
            IsVoiceControlEnabled = mode == GameControlMode.Voice,
        };
    }

    /// <summary>Zwraca tryb następny w kolejności, zawijając po ostatnim.</summary>
    /// <param name="mode">Tryb bieżący.</param>
    public static GameControlMode Next(GameControlMode mode) =>
        Kolejnosc[(Array.IndexOf(Kolejnosc, mode) + 1) % Kolejnosc.Length];
}
