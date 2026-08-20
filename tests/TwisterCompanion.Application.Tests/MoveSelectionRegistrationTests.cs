using Microsoft.Extensions.DependencyInjection;
using TwisterCompanion.Application.DependencyInjection;
using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.MoveSelection;
using TwisterCompanion.Domain.Randomness;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy rejestracji algorytmu losowania — realizacja kryterium „zamiana strategii przez
/// kontener nie wymaga zmian w kodzie, który jej używa".
/// </summary>
public class MoveSelectionRegistrationTests
{
    [Fact]
    public void AddApplication_RejestrujeLosowanieInteligentneJakoDomyslne()
    {
        using ServiceProvider provider = BuildProvider();

        IMoveSelectionStrategy strategy = provider.GetRequiredService<IMoveSelectionStrategy>();

        Assert.IsType<SmartMoveSelectionStrategy>(strategy);
    }

    [Fact]
    public void AddApplication_RejestrujeStrategieJakoSingleton()
    {
        // Strategie są bezstanowe, więc tworzenie ich przy każdym losowaniu byłoby marnotrawstwem.
        using ServiceProvider provider = BuildProvider();

        Assert.Same(
            provider.GetRequiredService<IMoveSelectionStrategy>(),
            provider.GetRequiredService<IMoveSelectionStrategy>());
    }

    [Fact]
    public void PodmianaStrategii_NieWymagaZmianyKoduKorzystajacego()
    {
        // Konsument reprezentuje silnik gry z Etapu 5: zna wyłącznie interfejs.
        // Ten sam, niezmieniony kod działa z obiema strategiami — o to chodzi we wzorcu.
        using ServiceProvider inteligentne = BuildProvider();
        using ServiceProvider klasyczne = BuildProvider(services =>
            services.AddSingleton<IMoveSelectionStrategy, ClassicMoveSelectionStrategy>());

        Move zInteligentnego = new MoveConsumer(
            inteligentne.GetRequiredService<IMoveSelectionStrategy>()).DrawNext();

        Move zKlasycznego = new MoveConsumer(
            klasyczne.GetRequiredService<IMoveSelectionStrategy>()).DrawNext();

        Assert.Contains(zInteligentnego, Move.All);
        Assert.Contains(zKlasycznego, Move.All);
    }

    [Fact]
    public void PodmianaStrategii_OstatniaRejestracjaWygrywa()
    {
        using ServiceProvider provider = BuildProvider(services =>
            services.AddSingleton<IMoveSelectionStrategy, ClassicMoveSelectionStrategy>());

        Assert.IsType<ClassicMoveSelectionStrategy>(
            provider.GetRequiredService<IMoveSelectionStrategy>());
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection>? overrides = null)
    {
        ServiceCollection services = new();

        services.AddSingleton<IRandomProvider>(new SeededRandomProvider(seed: 2024));
        services.AddApplication();

        overrides?.Invoke(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Atrapa kodu korzystającego z losowania — odpowiednik silnika gry.
    /// </summary>
    private sealed class MoveConsumer(IMoveSelectionStrategy strategy)
    {
        private readonly MoveHistory _history = new(MoveSelectionOptions.Default.HistoryLength);

        public Move DrawNext()
        {
            Move move = strategy.SelectNext(new MoveSelectionContext
            {
                RecentMoves = _history.Snapshot(),
            });

            _history.Add(move);

            return move;
        }
    }
}
