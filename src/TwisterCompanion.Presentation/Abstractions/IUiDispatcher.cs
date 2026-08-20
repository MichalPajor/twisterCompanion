namespace TwisterCompanion.Presentation.Abstractions;

/// <summary>
/// Przenosi wykonanie na wątek interfejsu użytkownika.
/// </summary>
/// <remarks>
/// Port do platformy. Odliczanie czasu tury tyka na wątku puli, a zmiana właściwości
/// powiązanej z widokiem musi nastąpić na wątku interfejsu — inaczej Android przerywa
/// aplikację wyjątkiem o dostępie do widoku z obcego wątku.
/// <para>
/// Interfejs jest celowo minimalny: warstwa prezentacji nie ma prawa wiedzieć nic więcej
/// o wątkach platformy niż to, że istnieje jeden wyróżniony.
/// </para>
/// </remarks>
public interface IUiDispatcher
{
    /// <summary>Wykonuje działanie na wątku interfejsu użytkownika.</summary>
    /// <param name="action">Działanie do wykonania.</param>
    /// <remarks>
    /// Nie czeka na wykonanie. Wywołanie z wątku interfejsu wykonuje działanie od razu.
    /// </remarks>
    void Post(Action action);
}
