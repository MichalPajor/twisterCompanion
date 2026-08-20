namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Krótkie sygnały dźwiękowe oznaczające stan mikrofonu.
/// </summary>
/// <remarks>
/// Port do platformy. Rozpoznawanie mowy działa sesjami, a gracz stoi z ręką na macie
/// i nie patrzy w ekran — bez sygnału nie ma pojęcia, czy jego „Dalej" zostanie usłyszane.
/// Wskaźnik na ekranie tego nie rozwiązuje, bo trzeba by na niego spojrzeć.
/// <para>
/// Sygnały są celowo oddzielone od efektów dźwiękowych rozgrywki z Etapu 11: te mówią
/// o stanie <b>urządzenia</b>, a nie o tym, co się stało w grze, i muszą zostać słyszalne
/// także wtedy, gdy gracz wyłączy dźwięki gry.
/// </para>
/// </remarks>
public interface IAudioCueService
{
    /// <summary>Odtwarza sygnał i kończy się, gdy sygnał przebrzmi.</summary>
    /// <param name="cue">Rodzaj sygnału.</param>
    /// <param name="cancellationToken">Token anulowania.</param>
    /// <remarks>
    /// Metoda czeka na wybrzmienie <b>celowo</b>: mikrofon wolno otworzyć dopiero po
    /// zakończeniu dźwięku, inaczej rozpoznawanie usłyszy własny sygnał.
    /// </remarks>
    Task PlayAsync(AudioCue cue, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rodzaj sygnału dźwiękowego.
/// </summary>
public enum AudioCue
{
    /// <summary>Mikrofon zaczyna słuchać — można wydać komendę.</summary>
    ListeningStarted,

    /// <summary>Mikrofon przestał słuchać — komenda nie zostanie teraz usłyszana.</summary>
    ListeningStopped,

    /// <summary>Komenda została rozpoznana i przyjęta.</summary>
    CommandAccepted,

    /// <summary>
    /// Pojedyncze tyknięcie odliczania.
    /// </summary>
    /// <remarks>
    /// Odtwarzane co sekundę w trakcie odliczania. Gracz stoi nad matą i nie patrzy na
    /// ekran — tykanie jest jedynym sposobem, żeby wiedział, ile czasu zostało, bez
    /// podnoszenia głowy.
    /// </remarks>
    CountdownTick,
}
