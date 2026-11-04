namespace TwisterCompanion.Application.AppReview;

/// <summary>
/// Decyduje, kiedy po partii poprosić o ocenę aplikacji.
/// </summary>
/// <remarks>
/// Odpowiednik <see cref="Advertising.IAdCoordinator"/>: warstwa prezentacji zgłasza tylko
/// wejście na ekran rozgrywki i zejście z niego. Prośba pada wyłącznie na tym ekranie, po
/// zakończonej partii — nigdy na ekranie startowym ani w ustawieniach.
/// </remarks>
public interface IReviewCoordinator
{
    /// <summary>Wejście na ekran rozgrywki.</summary>
    Task ActivateAsync();

    /// <summary>Zejście z ekranu rozgrywki.</summary>
    Task DeactivateAsync();
}
