namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Komendy, którymi można sterować rozgrywką bez dotykania telefonu.
/// </summary>
/// <remarks>
/// Każda komenda odpowiada operacji, którą da się wykonać także przyciskiem — sterowanie
/// głosem jest drogą <b>równoległą</b>, nie jedyną. Dołożenie komendy oznacza nową wartość
/// tutaj, wpis w zasobach z frazami i gałąź w rozdzielaczu; parser nie wymaga zmiany.
/// <para>
/// <b>Odpadnięcia gracza nie ma na tej liście świadomie.</b> Komenda „gracz odpadł" nie mówi,
/// <i>który</i> gracz odpadł, a przy kilku osobach na macie to jedyna informacja, która się
/// liczy. Odpadnięcie zgłasza się przyciskiem obok imienia — jednym dotknięciem, bez
/// pomyłki co do adresata.
/// </para>
/// </remarks>
public enum VoiceCommandType
{
    /// <summary>Przejście do następnej tury.</summary>
    Next,

    /// <summary>Powtórzenie ostatniego komunikatu.</summary>
    Repeat,

    /// <summary>Wstrzymanie rozgrywki.</summary>
    Pause,

    /// <summary>Wznowienie wstrzymanej rozgrywki.</summary>
    Resume,
}
