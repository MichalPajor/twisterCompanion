namespace TwisterCompanion.Application.Abstractions;

/// <summary>
/// Zbiór tekstów, z którego pochodzi tłumaczenie.
/// </summary>
/// <remarks>
/// Teksty interfejsu i teksty czytane na głos są rozdzielone celowo. Etykieta przycisku
/// ma być krótka („Dalej"), a wypowiedź ma brzmieć naturalnie w mowie i może wymagać
/// innej interpunkcji albo pełniejszego zdania. Trzymanie ich w jednym pliku prowadzi do
/// kompromisów, na których traci albo ekran, albo głos.
/// </remarks>
public enum StringCatalog
{
    /// <summary>Teksty widoczne na ekranie.</summary>
    Ui,

    /// <summary>Teksty przeznaczone do odczytu na głos.</summary>
    Voice,
}
