using Android.Runtime;
using Google.Android.Play.Core.Review;
using Microsoft.Extensions.Logging;
using TwisterCompanion.Application.AppReview;
using GmsTask = Android.Gms.Tasks.Task;
using IOnCompleteListener = Android.Gms.Tasks.IOnCompleteListener;
using Task = System.Threading.Tasks.Task;

namespace TwisterCompanion.App.Platforms.Android;

/// <summary>
/// Okienko oceny Google Play (In-App Review API) na Androidzie.
/// </summary>
/// <remarks>
/// Dwa kroki, jak w dokumentacji Google: najpierw <c>requestReviewFlow</c> po bilet
/// (<see cref="ReviewInfo"/>), potem <c>launchReviewFlow</c> z bieżącą aktywnością.
/// Koniec drugiego kroku <b>nie mówi</b>, czy okienko się pokazało ani czy użytkownik ocenił —
/// Google świadomie to ukrywa i sam decyduje o pokazaniu (limit, istniejąca ocena).
/// <para>
/// W aplikacji zainstalowanej spoza Sklepu Play — także z Visual Studio — oba kroki kończą
/// się poprawnie, a okienka nie ma. To zachowanie biblioteki, nie błąd tej klasy; pokazanie
/// sprawdza się na wersji ze ścieżki testów wewnętrznych.
/// </para>
/// </remarks>
internal sealed class PlayReviewService(ILogger<PlayReviewService> logger) : IReviewPlatform
{
    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public async Task RequestReviewAsync(CancellationToken cancellationToken = default)
    {
        global::Android.App.Activity? activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

        if (activity is null)
        {
            logger.LogWarning("Prośba o ocenę pominięta — brak aktywnej aktywności.");

            return;
        }

        IReviewManager manager = ReviewManagerFactory.Create(activity);

        Java.Lang.Object? ticket = await CompleteAsync(manager.RequestReviewFlow(), cancellationToken);
        ReviewInfo? reviewInfo = ticket?.JavaCast<ReviewInfo>();

        if (reviewInfo is null)
        {
            logger.LogWarning("Prośba o ocenę pominięta — sklep nie wydał biletu.");

            return;
        }

        GmsTask flow = await MainThread.InvokeOnMainThreadAsync(
            () => manager.LaunchReviewFlow(activity, reviewInfo));

        await CompleteAsync(flow, cancellationToken);
    }

    /// <summary>Przekłada zadanie usług Google na zadanie .NET.</summary>
    private static Task<Java.Lang.Object?> CompleteAsync(GmsTask task, CancellationToken cancellationToken)
    {
        TaskCompletionSource<Java.Lang.Object?> completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        task.AddOnCompleteListener(new CompletionListener(completed));

        return completed.Task.WaitAsync(cancellationToken);
    }

    private sealed class CompletionListener(TaskCompletionSource<Java.Lang.Object?> completed)
        : Java.Lang.Object, IOnCompleteListener
    {
        public void OnComplete(GmsTask task)
        {
            ArgumentNullException.ThrowIfNull(task);

            if (task.IsSuccessful)
            {
                completed.TrySetResult(task.Result);
            }
            else
            {
                // Wiązanie deklaruje wyjątek jako zawsze obecny, ale anulowane zadanie Google
                // go nie ma — bez tego sprawdzenia oczekujący czekałby w nieskończoność.
                Exception? error = task.Exception;

                completed.TrySetException(
                    error is null ? new InvalidOperationException("Sklep odrzucił prośbę o ocenę.") : error);
            }
        }
    }
}
