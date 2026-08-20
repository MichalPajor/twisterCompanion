namespace TwisterCompanion.Domain.Enums;

/// <summary>
/// Stan rozgrywki.
/// </summary>
/// <remarks>
/// Rozdzielenie <see cref="AnnouncingTurn"/> i <see cref="AwaitingPlayerAction"/> jest
/// istotne dla odczytu głosowego (Etap 7): w trakcie ogłaszania ruchu komenda „Dalej"
/// musi być ignorowana, bo gracze jeszcze nie usłyszeli, co mają zrobić. Bez tego
/// rozdziału szybkie kliknięcie albo przypadkowe rozpoznanie mowy przeskakiwałoby turę.
/// </remarks>
public enum GameState
{
    /// <summary>Gra nie została rozpoczęta.</summary>
    Idle,

    /// <summary>Gra rozpoczęta, pierwszy ruch jeszcze nie wylosowany.</summary>
    Starting,

    /// <summary>Ruch wylosowany, trwa ogłaszanie go graczom.</summary>
    AnnouncingTurn,

    /// <summary>Gracze wykonują ruch — czekamy na przejście do następnej tury.</summary>
    AwaitingPlayerAction,

    /// <summary>Gra wstrzymana.</summary>
    Paused,

    /// <summary>Gra zakończona.</summary>
    Finished,
}
