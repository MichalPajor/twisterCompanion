namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Łączy nasłuch komend z rozgrywką: decyduje kiedy słuchać i wykonuje rozpoznane komendy.
/// </summary>
/// <remarks>
/// Sterowanie głosem działa <b>wyłącznie w trakcie rozgrywki</b> — konfiguracja gry pozostaje
/// ręczna, zgodnie z założeniami projektu. Dlatego nasłuch jest włączany na wejściu na ekran
/// rozgrywki i wyłączany na wyjściu, a nie trzymany przez cały czas życia aplikacji.
/// </remarks>
public interface IVoiceControlCoordinator
{
    /// <summary>Czy sterowanie głosem jest w tej chwili aktywne.</summary>
    bool IsActive { get; }

    /// <summary>Włącza sterowanie głosem dla trwającej rozgrywki.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <returns><see langword="true"/>, gdy udało się je włączyć.</returns>
    Task<bool> ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>Wyłącza sterowanie głosem i zamyka mikrofon.</summary>
    Task DeactivateAsync();
}
