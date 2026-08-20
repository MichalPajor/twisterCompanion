namespace TwisterCompanion.Presentation.Navigation;

/// <summary>
/// Nazwy tras nawigacji — jedno źródło prawdy dla całej aplikacji.
/// </summary>
/// <remarks>
/// Stałe są używane w dwóch miejscach: warstwa prezentacji nawiguje po nich przez
/// <see cref="Abstractions.INavigationService"/>, a host MAUI rejestruje pod nimi strony.
/// Trzymanie ich tutaj oznacza, że literówka w nazwie trasy jest błędem kompilacji,
/// a nie awarią w czasie działania.
/// </remarks>
public static class Routes
{
    /// <summary>
    /// Nazwa zawartości Shella dla ekranu startowego — bez prefiksu trasy absolutnej.
    /// </summary>
    public const string HomeContent = "home";

    /// <summary>Ekran startowy. Trasa absolutna — czyści stos nawigacji.</summary>
    public const string Home = "//" + HomeContent;

    /// <summary>Ekran rozgrywki.</summary>
    public const string Game = "game";

    /// <summary>Zarządzanie listą graczy.</summary>
    public const string Players = "players";

    /// <summary>Wybór trybu gry.</summary>
    public const string GameModes = "modes";

    /// <summary>Paczki Custom Events.</summary>
    public const string EventPacks = "events";

    /// <summary>Ustawienia aplikacji.</summary>
    public const string Settings = "settings";

    /// <summary>Opis zasad wybranego trybu gry.</summary>
    public const string Rules = "rules";

    /// <summary>Wprowadzenie „Jak grać".</summary>
    public const string Onboarding = "onboarding";

    /// <summary>
    /// Nazwy parametrów przekazywanych między ekranami.
    /// </summary>
    /// <remarks>
    /// Z tego samego powodu co trasy: literówka w nazwie parametru ma być błędem kompilacji,
    /// a nie ekranem, który po prostu nic nie dostał.
    /// </remarks>
    public static class Parameters
    {
        /// <summary>Klucz trybu gry, którego zasady mają być pokazane.</summary>
        public const string GameModeKey = "gameModeKey";
    }

}
