namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Odtwarzanie krótkich efektów dźwiękowych z plików aplikacji.
/// </summary>
/// <remarks>
/// Port do platformy — sam <b>odtwarza</b>, i tylko to. Decyzja, <i>czy</i> w danej chwili
/// wolno zagrać i jak głośno, należy do <see cref="Feedback.IGameFeedback"/> w warstwie
/// aplikacji: zależy od ustawień i od tego, czy aplikacja właśnie mówi, a takich reguł nie da
/// się przetestować bez urządzenia, jeśli siedzą w kodzie platformowym.
/// <para>
/// Osobno od <see cref="IAudioCueService"/>: tamte sygnały mówią o stanie <b>mikrofonu</b>,
/// są generowane przez system i muszą przejść także przy wyłączonych dźwiękach gry.
/// </para>
/// </remarks>
public interface ISoundService
{
    /// <summary>
    /// Wczytuje wszystkie efekty do pamięci.
    /// </summary>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Wywoływane raz, przy starcie aplikacji. Pierwsze odtworzenie dźwięku wczytywanego
    /// dopiero w chwili użycia spóźnia się o kilkadziesiąt milisekund — a efekty w tej grze
    /// padają dokładnie w momencie, w którym coś się dzieje na ekranie.
    /// </remarks>
    Task PreloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Odtwarza efekt i wraca natychmiast, nie czekając na wybrzmienie.
    /// </summary>
    /// <param name="effect">Efekt do odtworzenia.</param>
    /// <param name="volume">Głośność z zakresu 0,0–1,0.</param>
    /// <remarks>
    /// Nie czeka <b>celowo</b> — w przeciwieństwie do <see cref="IAudioCueService"/>, gdzie
    /// czekanie chroni mikrofon przed usłyszeniem własnego sygnału. Tu nikt na dźwięk nie
    /// czeka: gra idzie dalej, a efekt dobrzmiewa w tle.
    /// <para>
    /// Nigdy nie rzuca wyjątku: brak dźwięku nie może przerwać partii.
    /// </para>
    /// </remarks>
    void Play(SoundEffect effect, double volume);
}

/// <summary>
/// Efekt dźwiękowy rozgrywki.
/// </summary>
/// <remarks>
/// Każda wartość ma jeden plik w <c>Resources/Raw</c>. Nazwy mówią, <b>co się stało</b>,
/// a nie jak dźwięk brzmi — dobranie innej próbki nie jest wtedy zmianą nazwy w pół aplikacji.
/// </remarks>
public enum SoundEffect
{
    /// <summary>Wylosowany ruch pojawił się na ekranie.</summary>
    MoveRevealed,

    /// <summary>W turze padło wydarzenie.</summary>
    EventTriggered,

    /// <summary>Gracz odpadł z gry.</summary>
    PlayerEliminated,

    /// <summary>Partia się rozpoczęła.</summary>
    GameStarted,

    /// <summary>Partia się skończyła.</summary>
    GameFinished,

    /// <summary>Naciśnięcie przycisku.</summary>
    ButtonTap,
}
