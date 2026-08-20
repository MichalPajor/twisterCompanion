using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.App.Services;

/// <summary>
/// Łączy systemowe ograniczenie animacji z przełącznikiem w ustawieniach aplikacji.
/// </summary>
/// <remarks>
/// System wygrywa zawsze: wyłączenie animacji w ustawieniach dostępności Androida ustawia
/// mnożnik czasu animacji na zero i dla części osób decyduje o tym, czy aplikacja jest
/// w ogóle używalna. Przełącznik aplikacji może animacje tylko <b>dodatkowo</b> wyłączyć.
/// <para>
/// Wartość jest czytana przy każdym pytaniu, a nie zapamiętywana: zmiana ustawienia ma
/// działać od razu, bez ponownego wejścia na ekran.
/// </para>
/// </remarks>
internal sealed class AnimationPolicy : IAnimationPolicy
{
    private readonly ISettingsService _settings;

    /// <summary>Tworzy zasadę animacji.</summary>
    /// <param name="settings">Ustawienia aplikacji.</param>
    public AnimationPolicy(ISettingsService settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
    }

    /// <inheritdoc />
    public bool AreAnimationsEnabled => _settings.Current.AreAnimationsEnabled && SystemAllowsAnimations;

    /// <summary>Czy system pozwala na animacje.</summary>
    private static bool SystemAllowsAnimations
    {
        get
        {
#if ANDROID
            float scale = Android.Provider.Settings.Global.GetFloat(
                Android.App.Application.Context.ContentResolver,
                Android.Provider.Settings.Global.AnimatorDurationScale,
                1f);

            return scale > 0f;
#else
            return true;
#endif
        }
    }
}
