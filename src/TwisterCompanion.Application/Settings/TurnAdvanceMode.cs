namespace TwisterCompanion.Application.Settings;

/// <summary>
/// Sposób przechodzenia do następnej tury.
/// </summary>
public enum TurnAdvanceMode
{
    /// <summary>Gracze sami decydują — przyciskiem albo komendą głosową.</summary>
    Manual,

    /// <summary>Tury następują automatycznie, w ustalonym odstępie czasu.</summary>
    Automatic,
}
