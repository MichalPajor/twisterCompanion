using TwisterCompanion.Application.AppReview;

namespace TwisterCompanion.Application.Tests.Fakes;

/// <summary>
/// Okienko oceny zastępcze — zapisuje, ile razy o nie poproszono.
/// </summary>
internal sealed class FakeReviewPlatform : IReviewPlatform
{
    /// <summary>Ile razy poproszono o okienko oceny.</summary>
    public int Requests { get; private set; }

    /// <inheritdoc />
    public bool IsAvailable { get; set; } = true;

    /// <summary>Czy wywołanie ma się wywrócić — ustawia test.</summary>
    public bool Throws { get; set; }

    /// <inheritdoc />
    public Task RequestReviewAsync(CancellationToken cancellationToken = default)
    {
        if (Throws)
        {
            throw new InvalidOperationException("Sklep niedostępny.");
        }

        Requests++;

        return Task.CompletedTask;
    }
}
