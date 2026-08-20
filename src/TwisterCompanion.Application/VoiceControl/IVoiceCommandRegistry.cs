namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Zbiór komend głosowych obowiązujących w języku aplikacji.
/// </summary>
/// <remarks>
/// Frazy pochodzą z plików zasobów, a nie z kodu — dołożenie synonimu („dawaj" obok
/// „dalej") jest zmianą w <c>.resx</c>, bez rekompilacji logiki i bez dotykania parsera.
/// <para>
/// Język nie jest parametrem: gracze mówią w tym samym języku, w którym aplikacja do nich
/// mówi, a serwis tłumaczeń jest jedynym źródłem tej informacji. Parametr sugerowałby wybór,
/// którego nie ma, i pozwalałby poprosić o frazy w języku innym niż ten, w którym faktycznie
/// zostaną odczytane zasoby.
/// </para>
/// </remarks>
public interface IVoiceCommandRegistry
{
    /// <summary>Zwraca komendy z frazami w aktualnym języku aplikacji.</summary>
    IReadOnlyList<VoiceCommandDefinition> GetCommands();
}
