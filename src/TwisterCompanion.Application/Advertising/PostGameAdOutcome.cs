using TwisterCompanion.Application.Game;

namespace TwisterCompanion.Application.Advertising;

/// <summary>
/// Co stało się z reklamą po zakończonej partii.
/// </summary>
/// <param name="Summary">Podsumowanie partii, której dotyczy decyzja.</param>
/// <param name="InterstitialShown">Czy pokazano reklamę pełnoekranową.</param>
public sealed record PostGameAdOutcome(GameSummary Summary, bool InterstitialShown);
