namespace TwisterCompanion.App.Services;

/// <summary>
/// Odpowiada na jedno pytanie: czy wolno animować.
/// </summary>
/// <remarks>
/// Pytanie ma dwa źródła i oba muszą się zgodzić — systemowe ograniczenie animacji
/// (ustawienia dostępności Androida) oraz przełącznik w ustawieniach aplikacji. Bez wspólnego
/// miejsca każdy ekran sprawdzałby tylko to źródło, o którym pamiętał jego autor.
/// <para>
/// Interfejs jest publiczny, bo trafia do konstruktorów stron — a te są publiczne, bo tworzy
/// je kontener.
/// </para>
/// </remarks>
public interface IAnimationPolicy
{
    /// <summary>Czy animacje interfejsu mają być odtwarzane.</summary>
    bool AreAnimationsEnabled { get; }
}
