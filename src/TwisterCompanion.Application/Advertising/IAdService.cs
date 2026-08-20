using TwisterCompanion.Domain.Enums;

namespace TwisterCompanion.Application.Advertising;

/// <summary>
/// Reklamy widziane od strony aplikacji — z twardymi regułami, kiedy wolno je pokazać.
/// </summary>
/// <remarks>
/// Osobny typ od <see cref="IAdPlatform"/>, bo reguły mają obowiązywać <b>bez wyjątku</b>,
/// niezależnie od tego, kto o reklamę poprosi. Gdyby siedziały w koordynatorze, to jego
/// wywołanie byłoby jedyną poprawną drogą, a każde inne — cichą luką. Tutaj są w typie,
/// który jest jedyną drogą do platformy.
/// <para>
/// Reguły są trzy i wszystkie wynikają z ustaleń z użytkownikiem: reklama pełnoekranowa
/// <b>wyłącznie</b> po zakończonej partii (<see cref="GameState.Finished"/>), nigdy w trakcie
/// odczytu głosowego i nigdy przy otwartym nasłuchu komend.
/// </para>
/// </remarks>
public interface IAdService
{
    /// <summary>Czy w tym wydaniu aplikacji reklamy w ogóle istnieją.</summary>
    bool IsAvailable { get; }

    /// <summary>Przygotowuje reklamy do pokazywania.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><see langword="true"/>, gdy wolno żądać reklam.</returns>
    Task<bool> PrepareAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pokazuje reklamę pełnoekranową, jeśli reguły na to pozwalają.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><see langword="true"/>, gdy reklama została pokazana.</returns>
    Task<bool> ShowInterstitialAsync(CancellationToken cancellationToken = default);
}
