namespace TwisterCompanion.App.Views;

/// <summary>
/// Miejsce na baner reklamowy w układzie strony.
/// </summary>
/// <remarks>
/// Baner jest <b>kontrolką</b>, a nie czymś, co serwis dokłada do okna — i to jest cała
/// przyczyna istnienia tego typu. Widok natywny wstawiany z boku, poza układem MAUI, trzeba
/// by pozycjonować ręcznie i poprawiać przy każdej zmianie orientacji; kontrolka w siatce
/// robi to sama.
/// <para>
/// Klasa jest pusta świadomie: całą treść daje uchwyt platformowy
/// (<c>Platforms/Android/BannerAdViewHandler.cs</c>), a na platformach bez reklam ten sam
/// widok jest po prostu niewidoczny i nie zajmuje miejsca.
/// </para>
/// </remarks>
public sealed class BannerAdView : View
{
}
