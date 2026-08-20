namespace TwisterCompanion.Presentation.Abstractions;

/// <summary>
/// Nawigacja między ekranami widziana z perspektywy warstwy prezentacji.
/// </summary>
/// <remarks>
/// Istnieje po to, żeby ViewModel nie znał <c>Shell</c> ani żadnego innego typu MAUI.
/// Dzięki temu ViewModele dają się testować jednostkowo, a zmiana mechanizmu nawigacji
/// nie dotyka logiki ekranów. Implementacja żyje w projekcie hosta.
/// </remarks>
public interface INavigationService
{
    /// <summary>Przechodzi do wskazanej trasy.</summary>
    /// <param name="route">Nazwa trasy z <see cref="Navigation.Routes"/>.</param>
    Task GoToAsync(string route);

    /// <summary>Przechodzi do wskazanej trasy, przekazując parametry do ekranu docelowego.</summary>
    /// <param name="route">Nazwa trasy z <see cref="Navigation.Routes"/>.</param>
    /// <param name="parameters">Parametry odebrane przez ekran docelowy.</param>
    Task GoToAsync(string route, IReadOnlyDictionary<string, object> parameters);

    /// <summary>Wraca do poprzedniego ekranu.</summary>
    Task GoBackAsync();

    /// <summary>Wraca na ekran startowy, czyszcząc stos nawigacji.</summary>
    Task GoToRootAsync();
}
