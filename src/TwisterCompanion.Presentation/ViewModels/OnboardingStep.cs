namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Jeden ekran wprowadzenia „Jak grać".
/// </summary>
/// <param name="Glyph">Znak jednobarwny kroku.</param>
/// <param name="Title">Tytuł kroku w aktualnym języku.</param>
/// <param name="Body">Treść kroku w aktualnym języku.</param>
public sealed record OnboardingStep(string Glyph, string Title, string Body);
