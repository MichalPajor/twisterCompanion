using Google.Android.Gms.Ads;
using Google.Android.Gms.Ads.Interstitial;
using Microsoft.Extensions.Logging;
using TwisterCompanion.App.Services;
using TwisterCompanion.Application.Advertising;
using Xamarin.Google.UserMesssagingPlatform;

namespace TwisterCompanion.App.Platforms.Android;

/// <summary>
/// Reklamy AdMob na Androidzie.
/// </summary>
/// <remarks>
/// Cały plik jest androidowy — leży w katalogu platformy, więc nie kompiluje się nigdzie
/// indziej. Warstwy rdzenia widzą wyłącznie port <see cref="IAdPlatform"/> i nie wiedzą,
/// że AdMob istnieje; przy porcie na iOS (Etap 17) dojdzie druga implementacja tego samego
/// portu i nic poza nią się nie zmieni.
/// <para>
/// Reklama pełnoekranowa jest wczytywana <b>na żądanie</b>, a nie trzymana w zapasie. Pada
/// najwyżej co trzecią partię, więc zapas przeleżałby kilkanaście minut i zdążyłby się
/// przedawnić — AdMob unieważnia wczytane reklamy po około godzinie, a wczytanie zajmuje
/// ułamek sekundy w tle, podczas gdy gracze czytają podsumowanie.
/// </para>
/// <para>
/// Zgoda na personalizację (UMP) jest częścią przygotowania, nie osobnym krokiem: bez niej
/// w EEA nie wolno żądać reklam w ogóle, więc pytanie o nią i inicjalizacja zestawu SDK to
/// jedna operacja z jednym wynikiem — „wolno żądać reklam" albo „nie wolno".
/// </para>
/// </remarks>
internal sealed class AdMobService(ILogger<AdMobService> logger) : IAdPlatform
{
    private readonly SemaphoreSlim _prepareGate = new(1, 1);

    private bool _isPrepared;
    private bool _canRequestAds;

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public async Task<bool> PrepareAsync(CancellationToken cancellationToken = default)
    {
        if (_isPrepared)
        {
            return _canRequestAds;
        }

        await _prepareGate.WaitAsync(cancellationToken);

        try
        {
            if (_isPrepared)
            {
                return _canRequestAds;
            }

            global::Android.App.Activity? activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

            // Brak aktywności NIE jest odpowiedzią „nie wolno reklam" i nie wolno go zapamiętać:
            // przygotowanie idzie teraz ze startu aplikacji, a wtedy okno może jeszcze nie
            // istnieć. Zapamiętana odmowa wyłączyłaby reklamy na całe uruchomienie, choć
            // wystarczyłoby spróbować chwilę później — przy wejściu na ekran rozgrywki.
            if (activity is null)
            {
                logger.LogInformation(
                    "Przygotowanie reklam odłożone — okno aplikacji jeszcze nie istnieje.");

                return false;
            }

            _canRequestAds = await RequestConsentAsync(activity, cancellationToken);

            if (_canRequestAds)
            {
                // Inicjalizacja jest długa (kilkaset milisekund) i synchroniczna wewnątrz,
                // więc idzie poza wątek interfejsu — inaczej wejście na ekran rozgrywki
                // zacinałoby się przy pierwszym uruchomieniu.
                await Task.Run(
                    () => MobileAds.Initialize(global::Android.App.Application.Context),
                    cancellationToken);
            }

            _isPrepared = true;

            logger.LogInformation(
                "Reklamy przygotowane. Wolno żądać reklam: {CanRequestAds}.",
                _canRequestAds);

            return _canRequestAds;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Nie udało się przygotować reklam.");

            _isPrepared = true;
            _canRequestAds = false;

            return false;
        }
        finally
        {
            _prepareGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ShowInterstitialAsync(CancellationToken cancellationToken = default)
    {
        if (!_canRequestAds)
        {
            return false;
        }

        try
        {
            InterstitialAd? ad = await LoadInterstitialAsync(cancellationToken);

            if (ad is null)
            {
                return false;
            }

            global::Android.App.Activity? activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

            if (activity is null)
            {
                logger.LogWarning("Reklama pełnoekranowa pominięta — brak aktywnej aktywności.");

                return false;
            }

            TaskCompletionSource dismissed = new(TaskCreationOptions.RunContinuationsAsynchronously);

            ad.FullScreenContentCallback = new DismissalCallback(dismissed, logger);

            await MainThread.InvokeOnMainThreadAsync(() => ad.Show(activity));

            // Czekamy na zamknięcie reklamy, żeby wywołujący wiedział, kiedy ekran wraca
            // do gry — bez tego podsumowanie mogłoby zmienić się pod reklamą.
            await dismissed.Task.WaitAsync(cancellationToken);

            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Nie udało się pokazać reklamy pełnoekranowej.");

            return false;
        }
    }

    /// <summary>Wczytuje reklamę pełnoekranową.</summary>
    private async Task<InterstitialAd?> LoadInterstitialAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource<InterstitialAd?> loaded =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        await MainThread.InvokeOnMainThreadAsync(() => InterstitialAd.Load(
            global::Android.App.Application.Context,
            AdUnits.Interstitial,
            new AdRequest.Builder().Build(),
            new LoadCallback(loaded, logger)));

        return await loaded.Task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Pyta o zgodę na personalizację reklam i zwraca odpowiedź na pytanie, czy wolno żądać
    /// reklam.
    /// </summary>
    /// <param name="activity">Okno, w którym może pokazać się formularz zgody.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Formularz pokazuje się tylko wtedy, gdy jest wymagany — o tym decyduje zestaw SDK na
    /// podstawie regionu. Poza EEA cała ta droga przechodzi bez pokazania czegokolwiek.
    /// </remarks>
    private async Task<bool> RequestConsentAsync(
        global::Android.App.Activity activity,
        CancellationToken cancellationToken)
    {
        IConsentInformation consent = UserMessagingPlatform.GetConsentInformation(activity);
        TaskCompletionSource updated = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await MainThread.InvokeOnMainThreadAsync(() => consent.RequestConsentInfoUpdate(
            activity,
            new ConsentRequestParameters.Builder().Build(),
            new ConsentUpdateSuccessListener(updated),
            new ConsentUpdateFailureListener(updated, logger)));

        await updated.Task.WaitAsync(cancellationToken);

        TaskCompletionSource formClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await MainThread.InvokeOnMainThreadAsync(() =>
            UserMessagingPlatform.LoadAndShowConsentFormIfRequired(
                activity,
                new ConsentFormDismissedListener(formClosed, logger)));

        await formClosed.Task.WaitAsync(cancellationToken);

        return consent.CanRequestAds();
    }

    /// <summary>Odbiera wynik wczytywania reklamy pełnoekranowej.</summary>
    private sealed class LoadCallback(
        TaskCompletionSource<InterstitialAd?> result,
        ILogger logger)
        : InterstitialAdLoadCallback
    {
        // Przeciążenie z konkretnym typem, a nie z Java.Lang.Object: wiązanie ma oba, ale
        // w Javie oba mają tę samą sygnaturę po wymazaniu typów i nadpisanie ogólnego kończy
        // się błędem generatora („name clash").
        public override void OnAdLoaded(InterstitialAd ad) => result.TrySetResult(ad);

        public override void OnAdFailedToLoad(LoadAdError error)
        {
            ArgumentNullException.ThrowIfNull(error);

            logger.LogWarning(
                "Nie udało się wczytać reklamy pełnoekranowej: {Code} {Message}.",
                error.Code,
                error.Message);

            result.TrySetResult(null);
        }
    }

    /// <summary>Czeka na zamknięcie reklamy pełnoekranowej przez użytkownika.</summary>
    private sealed class DismissalCallback(TaskCompletionSource dismissed, ILogger logger)
        : FullScreenContentCallback
    {
        public override void OnAdDismissedFullScreenContent() => dismissed.TrySetResult();

        public override void OnAdFailedToShowFullScreenContent(AdError error)
        {
            ArgumentNullException.ThrowIfNull(error);

            logger.LogWarning("Reklama pełnoekranowa nie pokazała się: {Message}.", error.Message);

            dismissed.TrySetResult();
        }
    }

    private sealed class ConsentUpdateSuccessListener(TaskCompletionSource updated)
        : Java.Lang.Object, IConsentInformationOnConsentInfoUpdateSuccessListener
    {
        public void OnConsentInfoUpdateSuccess() => updated.TrySetResult();
    }

    private sealed class ConsentUpdateFailureListener(TaskCompletionSource updated, ILogger logger)
        : Java.Lang.Object, IConsentInformationOnConsentInfoUpdateFailureListener
    {
        public void OnConsentInfoUpdateFailure(FormError error)
        {
            logger.LogWarning("Nie udało się odświeżyć stanu zgody: {Message}.", error?.Message);

            updated.TrySetResult();
        }
    }

    private sealed class ConsentFormDismissedListener(TaskCompletionSource closed, ILogger logger)
        : Java.Lang.Object, IConsentFormOnConsentFormDismissedListener
    {
        public void OnConsentFormDismissed(FormError? error)
        {
            if (error is not null)
            {
                logger.LogWarning("Formularz zgody zamknięty błędem: {Message}.", error.Message);
            }

            closed.TrySetResult();
        }
    }
}
