using Android.Content.Res;
using Microsoft.Maui.Handlers;

namespace TwisterCompanion.App;

/// <summary>
/// Zdejmuje systemową kreskę pod polami tekstowymi i polami wyboru.
/// </summary>
/// <remarks>
/// Android rysuje pod każdym polem cienką linię — to pozostałość po jego własnym stylu pól,
/// z czasów, gdy pole nie miało ramki. Nasze pola mają ramkę (styl <c>FieldBorder</c>), więc
/// linia w środku ramki jest drugą, niepotrzebną krawędzią.
/// <para>
/// Zdjęcie jej wymaga wejścia w warstwę platformy, bo <c>Entry</c> i <c>Picker</c> nie mają
/// właściwości opisującej tę kreskę — jest tłem widoku systemowego. Robimy to raz, dla całej
/// aplikacji, przez dopisanie do mapowania właściwości: dzięki temu żaden ekran nie musi
/// o tym pamiętać, a nowe pole dostaje to samo zachowanie.
/// </para>
/// </remarks>
internal static class AndroidInputStyling
{
    /// <summary>Klucz wpisu w mapowaniu — musi być niepowtarzalny w obrębie kontrolki.</summary>
    private const string MappingKey = "TwisterCompanion.BezPodkreslenia";

    /// <summary>Dopisuje zdjęcie kreski do mapowania pól tekstowych i pól wyboru.</summary>
    public static void RemoveInputUnderline()
    {
        // Tylko te dwie kontrolki, bo tylko ich używamy. Dopisanie kolejnej byłoby kodem
        // bez zastosowania, a każdy taki wpis wykonuje się przy tworzeniu każdego widoku.
        EntryHandler.Mapper.AppendToMapping(MappingKey, (handler, _) => Clear(handler.PlatformView));
        PickerHandler.Mapper.AppendToMapping(MappingKey, (handler, _) => Clear(handler.PlatformView));
    }

    private static void Clear(Android.Views.View? platformView)
    {
        if (platformView is not null)
        {
            platformView.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        }
    }
}
