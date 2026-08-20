using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Presentation.ViewModels;

/// <summary>
/// Pozycja na liście wyboru głosu.
/// </summary>
/// <param name="Id">
/// Identyfikator głosu w syntezatorze albo <see langword="null"/> dla głosu domyślnego
/// systemu.
/// </param>
/// <param name="DisplayName">Nazwa widoczna na liście.</param>
/// <remarks>
/// Nazwy głosów pochodzą z systemu i <b>nie podlegają tłumaczeniu</b> — użytkownik znajdzie
/// na liście dokładnie to, co widzi w ustawieniach systemu. Tłumaczona jest wyłącznie
/// pozycja „domyślny systemowy", bo to nasz opis, a nie nazwa własna głosu.
/// </remarks>
public sealed record VoiceOption(string? Id, string DisplayName)
{
    /// <summary>Tworzy pozycję listy na podstawie głosu zgłoszonego przez system.</summary>
    /// <param name="voice">Głos dostępny w syntezatorze.</param>
    public static VoiceOption From(SpeechVoice voice)
    {
        ArgumentNullException.ThrowIfNull(voice);

        return new VoiceOption(voice.Id, voice.Name);
    }

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}
