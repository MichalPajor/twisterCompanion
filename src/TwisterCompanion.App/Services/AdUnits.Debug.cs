namespace TwisterCompanion.App.Services;

/// <summary>
/// Przykładowe jednostki testowe Google — kompilowane wyłącznie w Debug.
/// </summary>
/// <remarks>
/// Zawsze zwracają reklamę testową i nigdy prawdziwą, więc nie da się nimi ani naruszyć zasad,
/// ani zarobić. To te same identyfikatory, które podaje dokumentacja AdMob w rozdziale o
/// włączaniu reklam testowych. Test <c>JednostkiDebug_SaWylacznieTestowe</c> pilnuje, żeby nikt
/// nie wkleił tu przez pomyłkę identyfikatorów produkcyjnych.
/// </remarks>
internal static partial class AdUnits
{
    /// <summary>Przykładowy baner testowy Google.</summary>
    public const string Banner = "ca-app-pub-3940256099942544/6300978111";

    /// <summary>Przykładowa reklama pełnoekranowa testowa Google.</summary>
    public const string Interstitial = "ca-app-pub-3940256099942544/1033173712";
}
