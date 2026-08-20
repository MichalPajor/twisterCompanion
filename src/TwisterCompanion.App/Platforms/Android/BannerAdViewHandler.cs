using Google.Android.Gms.Ads;
using Microsoft.Maui.Handlers;
using TwisterCompanion.App.Services;
using TwisterCompanion.App.Views;

namespace TwisterCompanion.App.Platforms.Android;

/// <summary>
/// Zamienia <see cref="BannerAdView"/> na natywny widok banera AdMob.
/// </summary>
/// <remarks>
/// Rozmiar jest <b>stały</b> (320×50), zgodnie z ustaleniem z użytkownikiem. Baner
/// adaptacyjny dopasowałby wysokość do ekranu, ale wtedy miejsce zarezerwowane w układzie
/// musiałoby zależeć od urządzenia — a ekran rozgrywki jest policzony co do jednostki
/// i przeskok wysokości byłby tam widoczny od razu.
/// <para>
/// Reklama jest wczytywana raz, przy utworzeniu widoku. Baner odświeża się potem sam po
/// stronie zestawu SDK; wymuszanie kolejnych żądań przy każdym wejściu na ekran zużywałoby
/// limity i — przy krótkich wejściach — nie dawałoby reklamie czasu na wyświetlenie.
/// </para>
/// </remarks>
internal sealed class BannerAdViewHandler : ViewHandler<BannerAdView, AdView>
{
    /// <summary>Baner nie ma własnych właściwości do mapowania — cała treść jest natywna.</summary>
    public static IPropertyMapper<BannerAdView, BannerAdViewHandler> BannerMapper { get; } =
        new PropertyMapper<BannerAdView, BannerAdViewHandler>(ViewMapper);

    /// <summary>Tworzy uchwyt.</summary>
    public BannerAdViewHandler()
        : base(BannerMapper)
    {
    }

    /// <inheritdoc />
    protected override AdView CreatePlatformView()
    {
        AdView banner = new(Context)
        {
            AdUnitId = AdUnits.Banner,
            AdSize = AdSize.Banner,
        };

        banner.LoadAd(new AdRequest.Builder().Build());

        return banner;
    }

    /// <inheritdoc />
    protected override void DisconnectHandler(AdView platformView)
    {
        ArgumentNullException.ThrowIfNull(platformView);

        // Baner trzyma połączenie sieciowe i cykl odświeżania — bez zwolnienia zostaje po nim
        // wyciek przy każdym wejściu na ekran rozgrywki.
        platformView.Destroy();

        base.DisconnectHandler(platformView);
    }
}
