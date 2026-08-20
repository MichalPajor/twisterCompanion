using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Localization;

namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Komendy głosowe czytane z plików zasobów.
/// </summary>
/// <remarks>
/// Jeden klucz zasobu na komendę, frazy rozdzielone znakiem <c>|</c>. Taki zapis wybrano
/// zamiast osobnego klucza na każdą frazę, bo liczba synonimów zmienia się w trakcie
/// dopracowywania rozpoznawania, a klucz na frazę wymagałby zmian w kodzie przy każdym
/// dołożonym słowie.
/// </remarks>
internal sealed class VoiceCommandRegistry(ILocalizationService localization) : IVoiceCommandRegistry
{
    private static readonly VoiceCommandType[] AllCommands = Enum.GetValues<VoiceCommandType>();

    private readonly ILocalizationService _localization =
        localization ?? throw new ArgumentNullException(nameof(localization));

    /// <inheritdoc />
    public IReadOnlyList<VoiceCommandDefinition> GetCommands() =>
        [
            .. AllCommands
                .Select(type => new VoiceCommandDefinition(type, ReadPhrases(type)))
                .Where(definition => definition.Phrases.Count > 0),
        ];

    private IReadOnlyList<string> ReadPhrases(VoiceCommandType type)
    {
        string key = StringKeys.Voice.CommandPrefix + type;
        string value = _localization.GetString(key, StringCatalog.Voice);

        // Brak wpisu w zasobach oznacza komendę bez fraz — nie awarię. Serwis tłumaczeń
        // zwraca wtedy sam klucz, więc filtrujemy go, zamiast próbować go rozpoznawać.
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal))
        {
            return [];
        }

        return
        [
            .. value
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];
    }
}
