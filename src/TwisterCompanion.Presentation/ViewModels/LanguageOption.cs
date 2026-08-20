using System.Globalization;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Pozycja na liście wyboru języka.
/// </summary>
/// <param name="LanguageCode">Dwuliterowy kod języka, na przykład <c>pl</c>.</param>
/// <param name="DisplayName">Nazwa języka w tym właśnie języku.</param>
/// <remarks>
/// Nazwa jest podawana w języku, którego dotyczy („Polski", „English"), i dlatego
/// <b>nie podlega tłumaczeniu</b>. Użytkownik szukający swojego języka rozpozna go
/// po własnej nazwie, a nie po tłumaczeniu na język, którego może nie znać.
/// </remarks>
public sealed record LanguageOption(string LanguageCode, string DisplayName)
{
    /// <summary>Tworzy pozycję listy na podstawie kultury.</summary>
    /// <param name="culture">Kultura obsługiwana przez aplikację.</param>
    public static LanguageOption From(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        return new LanguageOption(
            culture.TwoLetterISOLanguageName,
            Capitalize(culture.NativeName, culture));
    }

    /// <inheritdoc />
    public override string ToString() => DisplayName;

    private static string Capitalize(string value, CultureInfo culture) =>
        string.IsNullOrEmpty(value)
            ? value
            : string.Concat(value[..1].ToUpper(culture), value[1..]);
}
