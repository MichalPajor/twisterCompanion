namespace TwisterCompanion.Application.Advertising;

/// <summary>
/// Reklamy widziane od strony platformy: przygotowanie zestawu SDK i pokazanie reklamy
/// pełnoekranowej.
/// </summary>
/// <remarks>
/// Port, nie serwis aplikacji. Nie zna reguł gry i nie ma prawa ich znać — o tym, <b>czy</b>
/// wolno teraz pokazać reklamę, decyduje <see cref="IAdService"/>, a o tym, <b>kiedy</b>
/// warto — <see cref="IAdCoordinator"/>. Tutaj zostaje samo „pokaż".
/// <para>
/// Plan Etapu 15 wymieniał w jednym interfejsie także <c>ShowBanner</c> i <c>HideBanner</c>.
/// Baner ich nie dostał i to jest świadoma decyzja: w MAUI baner jest <b>kontrolką w układzie
/// strony</b>, a nie czymś, co serwis dokłada do okna. Doklejanie natywnego widoku z serwisu
/// wymagałoby sięgnięcia po prywatne wnętrzności powłoki i psułoby układ ekranu przy każdej
/// zmianie orientacji. Zamiast tego baner jest widokiem (<c>BannerAdView</c>) i pokazuje się
/// przez zwykłe wiązanie widoczności — a o tym, czy wolno go w ogóle trzymać w układzie,
/// mówi <see cref="IAdCoordinator.IsBannerAllowed"/>.
/// </para>
/// </remarks>
public interface IAdPlatform
{
    /// <summary>Czy w tym wydaniu aplikacji reklamy w ogóle istnieją.</summary>
    /// <remarks>
    /// <see langword="false"/> dla implementacji zastępczej — buildy deweloperskie i platformy
    /// bez integracji. Cały interfejs użytkownika pyta o tę wartość, zamiast zakładać, że
    /// reklamy są: ekran rozgrywki nie może rezerwować miejsca na baner, którego nie będzie.
    /// </remarks>
    bool IsAvailable { get; }

    /// <summary>
    /// Przygotowuje zestaw SDK i — jeśli trzeba — pyta użytkownika o zgodę na personalizację.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><see langword="true"/>, gdy wolno żądać reklam.</returns>
    /// <remarks>
    /// Wywołanie jest bezpieczne wielokrotnie — kolejne wywołania nic nie robią.
    /// </remarks>
    Task<bool> PrepareAsync(CancellationToken cancellationToken = default);

    /// <summary>Pokazuje reklamę pełnoekranową.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><see langword="true"/>, gdy reklama została pokazana.</returns>
    Task<bool> ShowInterstitialAsync(CancellationToken cancellationToken = default);
}
