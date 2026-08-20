using TwisterCompanion.Application.Settings;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Pozycja na liście wyboru motywu.
/// </summary>
/// <param name="Preference">Wybór motywu.</param>
/// <param name="DisplayName">Nazwa w aktualnym języku.</param>
public sealed record ThemeOption(AppThemePreference Preference, string DisplayName)
{
    /// <inheritdoc />
    public override string ToString() => DisplayName;
}
