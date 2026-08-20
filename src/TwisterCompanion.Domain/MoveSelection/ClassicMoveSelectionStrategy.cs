using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Entities;

namespace TwisterCompanion.Domain.MoveSelection;

/// <summary>
/// Losowanie klasyczne — każdy z 16 ruchów z jednakową szansą, bez żadnej pamięci.
/// </summary>
/// <remarks>
/// Dokładny odpowiednik plastikowego spinnera z pudełka: powtórzenia pod rząd są możliwe,
/// serie tej samej kończyny też. Istnieje z dwóch powodów.
/// <para>
/// Po pierwsze, tryb Classic ma dawać dokładnie to doświadczenie — część graczy chce
/// prawdziwego spinnera, a nie algorytmu.
/// </para>
/// <para>
/// Po drugie, jest punktem odniesienia dla testów: dopiero porównanie z nim pokazuje,
/// czy <see cref="SmartMoveSelectionStrategy"/> faktycznie coś wnosi.
/// </para>
/// </remarks>
public sealed class ClassicMoveSelectionStrategy(IRandomProvider randomProvider) : IMoveSelectionStrategy
{
    private readonly IRandomProvider _randomProvider =
        randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));

    /// <inheritdoc />
    /// <remarks>
    /// Kontekst jest ignorowany — o to właśnie chodzi w tej strategii.
    /// </remarks>
    public Move SelectNext(MoveSelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Move.All[_randomProvider.Next(Move.All.Count)];
    }
}
