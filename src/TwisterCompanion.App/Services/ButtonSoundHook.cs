using Microsoft.Maui.Handlers;
using TwisterCompanion.Application.Feedback;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Dokłada stuknięcie do <b>każdego</b> przycisku w aplikacji.
/// </summary>
/// <remarks>
/// Podpięcie idzie przez mapowanie właściwości uchwytu, a nie przez pojedyncze ekrany, i to
/// jest tu istotne: dźwięk naciśnięcia ma być przy każdym przycisku, także przy przyciskach
/// ze znakiem w wierszach list, a takich miejsc jest kilkadziesiąt. Wpisywanie tego w każdy
/// przycisk oznaczałoby, że nowy przycisk „zapomni" o dźwięku — i nikt tego nie zauważy.
/// <para>
/// Nasłuch jest najpierw odłączany, potem podłączany. Mapowanie wykonuje się przy każdej
/// zmianie właściwości przycisku, więc bez tego jeden przycisk zbierałby kilka nasłuchów
/// i stukałby kilka razy na dotknięcie.
/// </para>
/// </remarks>
internal static class ButtonSoundHook
{
    /// <summary>Klucz wpisu w mapowaniu — musi być niepowtarzalny w obrębie kontrolki.</summary>
    private const string MappingKey = "TwisterCompanion.StuknieciePrzycisku";

    private static IGameFeedback? _feedback;

    /// <summary>Podłącza dźwięk naciśnięcia do wszystkich przycisków.</summary>
    /// <param name="feedback">Serwis decydujący, czy wolno teraz zabrzmieć.</param>
    public static void Install(IGameFeedback feedback)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        _feedback = feedback;

        ButtonHandler.Mapper.AppendToMapping(MappingKey, (_, button) =>
        {
            if (button is not Button control)
            {
                return;
            }

            control.Clicked -= OnClicked;
            control.Clicked += OnClicked;
        });
    }

    private static void OnClicked(object? sender, EventArgs e) =>
        _feedback?.Play(FeedbackMoment.ButtonTap);
}
