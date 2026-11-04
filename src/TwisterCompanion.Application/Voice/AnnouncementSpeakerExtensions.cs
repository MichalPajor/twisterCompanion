namespace TwisterCompanion.Application.Voice;

/// <summary>
/// Czekanie na koniec zapowiedzi — wspólne dla wszystkiego, co pojawia się po partii.
/// </summary>
/// <remarks>
/// Reklama i prośba o ocenę mają tę samą regułę: nie wchodzą w słowo komunikatowi o wyniku
/// partii. Jedna implementacja, żeby obie nie rozjechały się przy pierwszej poprawce.
/// </remarks>
internal static class AnnouncementSpeakerExtensions
{
    /// <summary>Czeka, aż aplikacja przestanie mówić.</summary>
    /// <param name="speaker">Odczyt komunikatów.</param>
    /// <param name="timeout">Ile najwyżej czekać.</param>
    /// <param name="timeProvider">
    /// Źródło czasu dla limitu — sterowane w testach, żeby nie czekały naprawdę.
    /// </param>
    /// <returns><see langword="false"/>, gdy zapowiedź nie zamilkła w wyznaczonym czasie.</returns>
    public static async Task<bool> WaitForSilenceAsync(
        this IAnnouncementSpeaker speaker,
        TimeSpan timeout,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (!speaker.IsSpeaking)
        {
            return true;
        }

        TaskCompletionSource silence = new(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnSpeakingChanged(object? sender, bool isSpeaking)
        {
            if (!isSpeaking)
            {
                silence.TrySetResult();
            }
        }

        speaker.SpeakingChanged += OnSpeakingChanged;

        try
        {
            // Zapowiedź mogła zamilknąć między pierwszym sprawdzeniem a subskrypcją.
            if (!speaker.IsSpeaking)
            {
                return true;
            }

            using CancellationTokenSource limit = new(timeout, timeProvider);

            await silence.Task.WaitAsync(limit.Token);

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            speaker.SpeakingChanged -= OnSpeakingChanged;
        }
    }
}
