namespace TwisterCompanion.Presentation.Abstractions;

/// <summary>
/// ViewModel przyjmujący parametry przekazane przy nawigacji.
/// </summary>
/// <remarks>
/// Własny interfejs, a nie mechanizm MAUI: warstwa prezentacji nie zna typów platformy,
/// więc host tłumaczy swój sposób przekazywania parametrów na to jedno wywołanie.
/// <para>
/// Parametry docierają <b>przed</b> inicjalizacją ekranu, więc
/// <see cref="ViewModels.ViewModelBase.InitializeAsync"/> może już na nich polegać.
/// </para>
/// </remarks>
public interface INavigationParameterReceiver
{
    /// <summary>Przyjmuje parametry nawigacji.</summary>
    /// <param name="parameters">Parametry przekazane przez ekran źródłowy.</param>
    void ApplyParameters(IReadOnlyDictionary<string, object> parameters);
}
