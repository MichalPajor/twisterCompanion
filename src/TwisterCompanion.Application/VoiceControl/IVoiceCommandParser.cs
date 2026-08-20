namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Rozpoznaje komendę w tekście zwróconym przez rozpoznawanie mowy.
/// </summary>
public interface IVoiceCommandParser
{
    /// <summary>Próbuje znaleźć komendę w rozpoznanym tekście.</summary>
    /// <param name="recognizedText">Tekst od rozpoznawania mowy — częściowy albo finalny.</param>
    /// <param name="command">Znaleziona komenda.</param>
    /// <returns><see langword="true"/>, gdy tekst zawiera komendę.</returns>
    /// <remarks>
    /// Frazy pochodzą z języka aplikacji — patrz <see cref="IVoiceCommandRegistry"/>.
    /// </remarks>
    bool TryParse(string? recognizedText, out VoiceCommandType command);
}
