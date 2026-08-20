namespace TwisterCompanion.Domain.Enums;

/// <summary>
/// Klasyfikacja wiekowa zawartości paczki wydarzeń.
/// </summary>
/// <remarks>
/// Aplikacja dostarcza wyłącznie paczki oznaczone jako <see cref="Everyone"/> — treści
/// dla dorosłych utrudniłyby publikację w sklepach. Wyliczenie istnieje po to, żeby
/// dodanie takich paczek w przyszłości było kwestią danych i filtra, a nie przebudowy
/// modelu. Paczki użytkownika też mogą być tak oznaczone.
/// </remarks>
public enum EventPackAgeRating
{
    /// <summary>Zawartość odpowiednia dla każdego, w tym dla dzieci.</summary>
    Everyone,

    /// <summary>Zawartość dla dorosłych.</summary>
    Adult,
}
