using TwisterCompanion.Application.Settings;

namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Dostęp do ustawień aplikacji wraz z powiadamianiem o zmianach.
/// </summary>
/// <remarks>
/// Zmiana idzie przez <see cref="UpdateAsync"/> z funkcją przekształcającą, a nie przez
/// zapisywalne właściwości. Powód: dzięki temu każda zmiana przechodzi jedną ścieżką —
/// walidacja, zapis na dysk i rozgłoszenie zdarzenia dzieją się zawsze, bez możliwości
/// pominięcia któregoś kroku.
/// </remarks>
public interface ISettingsService
{
    /// <summary>Aktualne ustawienia.</summary>
    AppSettings Current { get; }

    /// <summary>
    /// Zgłaszane po każdej udanej zmianie ustawień — również po wczytaniu ich z dysku.
    /// </summary>
    /// <remarks>
    /// Wczytanie jest tu traktowane jak zmiana świadomie: subskrybenci (wygląd, język)
    /// stosują to, co jest w <see cref="Current"/>, a po starcie aplikacji jest tam najpierw
    /// stan domyślny i dopiero po chwili zapisany. Bez zdarzenia po wczytaniu każdy z nich
    /// musiałby pamiętać o osobnym wywołaniu — i to się już raz nie udało.
    /// </remarks>
    event EventHandler<AppSettings>? Changed;

    /// <summary>
    /// Wczytuje ustawienia z dysku i zgłasza <see cref="Changed"/>.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Uszkodzony albo nieczytelny plik nie jest błędem — serwis wraca wtedy do
    /// <see cref="AppSettings.Default"/>, żeby aplikacja dała się uruchomić.
    /// </remarks>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Zmienia ustawienia, zapisuje je i rozgłasza zmianę.</summary>
    /// <param name="change">Funkcja tworząca nowy stan na podstawie aktualnego.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task UpdateAsync(Func<AppSettings, AppSettings> change, CancellationToken cancellationToken = default);

    /// <summary>Przywraca ustawienia domyślne.</summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
