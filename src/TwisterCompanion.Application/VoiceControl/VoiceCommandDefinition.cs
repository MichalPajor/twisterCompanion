namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Komenda głosowa razem z frazami, którymi da się ją wywołać.
/// </summary>
/// <param name="Type">Rodzaj komendy.</param>
/// <param name="Phrases">
/// Frazy w aktualnym języku, w postaci surowej — normalizacją zajmuje się parser.
/// </param>
public sealed record VoiceCommandDefinition(VoiceCommandType Type, IReadOnlyList<string> Phrases);
