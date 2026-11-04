namespace TwisterCompanion.Application.AppReview;

/// <summary>
/// Prośba o ocenę widziana od strony platformy: okienko oceny ze sklepu.
/// </summary>
/// <remarks>
/// Port, nie serwis aplikacji — jak <see cref="Advertising.IAdPlatform"/>. O tym, <b>kiedy</b>
/// warto poprosić, decyduje <see cref="ReviewPolicy"/>; tutaj zostaje samo „poproś".
/// <para>
/// Metoda celowo nic nie zwraca. Google Play nie zdradza, czy okienko się pokazało ani czy
/// użytkownik ocenił — sam decyduje, czy je pokazać (nie pokaże, gdy ocena już jest albo
/// wyczerpał się jego limit). Wynik, którego nie da się poznać, w sygnaturze tylko by kłamał.
/// Z tego samego powodu prośba nie może być podpięta pod przycisk: kliknięcie mogłoby
/// nie dać żadnego efektu.
/// </para>
/// </remarks>
public interface IReviewPlatform
{
    /// <summary>Czy w tym wydaniu aplikacji okienko oceny w ogóle istnieje.</summary>
    /// <remarks>
    /// <see langword="false"/> dla implementacji zastępczej — buildy deweloperskie i platformy
    /// bez integracji.
    /// </remarks>
    bool IsAvailable { get; }

    /// <summary>Prosi sklep o pokazanie okienka oceny.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task RequestReviewAsync(CancellationToken cancellationToken = default);
}
