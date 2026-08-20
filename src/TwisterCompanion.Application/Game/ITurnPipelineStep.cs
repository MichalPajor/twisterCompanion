namespace TwisterCompanion.Application.Game;

/// <summary>
/// Jeden krok rozegrania tury.
/// </summary>
/// <remarks>
/// Tura jest rozbita na kroki, żeby kolejne etapy mogły ją rozszerzać <b>bez zmiany
/// istniejącego kodu</b>. Etap 6 wstawi krok losowania wydarzeń, Etap 7 krok odczytu
/// głosowego — w obu przypadkach wystarczy nowa klasa i jedna linia rejestracji.
/// <para>
/// <b>Kolejność kroków wynika z kolejności rejestracji w kontenerze</b>
/// (<c>ApplicationServiceCollectionExtensions.AddApplication</c>). Wstawienie kroku
/// w środek potoku to zmiana w tym jednym miejscu.
/// </para>
/// </remarks>
public interface ITurnPipelineStep
{
    /// <summary>Wykonuje krok, uzupełniając kontekst tury.</summary>
    /// <param name="context">Stan rozgrywanej tury.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task ExecuteAsync(TurnContext context, CancellationToken cancellationToken = default);
}
