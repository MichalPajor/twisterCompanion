namespace TwisterCompanion.Application.Advertising;

/// <summary>
/// Decyduje, kiedy reklamy się pojawiają i czy ekran rozgrywki ma trzymać miejsce na baner.
/// </summary>
/// <remarks>
/// Odpowiednik <c>IVoiceControlCoordinator</c> dla reklam: warstwa prezentacji zgłasza tylko
/// wejście na ekran rozgrywki i zejście z niego, a cała wiedza o tym, co z tego wynika,
/// zostaje tutaj.
/// </remarks>
public interface IAdCoordinator
{
    /// <summary>Czy ekran rozgrywki ma trzymać w układzie miejsce na baner.</summary>
    bool IsBannerAllowed { get; }

    /// <summary>Zgłaszane, gdy zmieni się odpowiedź na pytanie o baner.</summary>
    /// <remarks>
    /// Przygotowanie zestawu SDK i pytanie o zgodę trwają, więc odpowiedź poznajemy dopiero
    /// po chwili od wejścia na ekran. Bez zdarzenia ekran musiałby o nią odpytywać.
    /// </remarks>
    event EventHandler<bool>? BannerAllowedChanged;

    /// <summary>Wejście na ekran rozgrywki.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>Zejście z ekranu rozgrywki.</summary>
    Task DeactivateAsync();
}
