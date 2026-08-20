using TwisterCompanion.Domain.Abstractions;
using TwisterCompanion.Domain.Entities;
using TwisterCompanion.Domain.Enums;
using TwisterCompanion.Domain.EventSelection;
using TwisterCompanion.Domain.Randomness;

namespace TwisterCompanion.Domain.Tests.EventSelection;

/// <summary>
/// Testy losowania wydarzeń — realizacja mierzalnych kryteriów ukończenia Etapu 6.
/// </summary>
public class WeightedEventSelectorTests
{
    [Fact]
    public void SelectNext_BezPaczki_NieZwracaWydarzenia()
    {
        IEventSelector selector = CreateSelector();

        Assert.Null(selector.SelectNext(new EventSelectionContext { TurnNumber = 1 }));
    }

    [Fact]
    public void SelectNext_PustaPaczka_NieZwracaWydarzenia()
    {
        IEventSelector selector = CreateSelector();

        Assert.Null(selector.SelectNext(new EventSelectionContext
        {
            Pack = EventPack.Create("Pusta"),
            TurnNumber = 1,
        }));
    }

    [Fact]
    public void SelectNext_PrzySzansieSto_ZwracaWydarzenieWKazdejDozwolonejTurze()
    {
        // Kryterium ukończenia: przy 100% wydarzenie pada co turę, z zachowaniem
        // minimalnego odstępu między wydarzeniami.
        IEventSelector selector = CreateSelector();
        EventPack pack = EventPack.Create("Pewniak", [GameEvent.CreateCustom("Zawsze", 100)]);

        int wystapienia = SimulateTurns(selector, pack, turns: 30);

        Assert.Equal(30, wystapienia);
    }

    [Fact]
    public void SelectNext_PrzySzansieZero_NigdyNieZwracaWydarzenia()
    {
        // Kryterium ukończenia: przy 0% wydarzenie nie pada nigdy.
        IEventSelector selector = CreateSelector();
        EventPack pack = EventPack.Create("Nigdy", [GameEvent.CreateCustom("Nigdy", 0)]);

        Assert.Equal(0, SimulateTurns(selector, pack, turns: 200));
    }

    [Fact]
    public void SelectNext_WydarzeniaWylaczone_SaIgnorowane()
    {
        // Kryterium ukończenia: wydarzenia wyłączone nie biorą udziału w losowaniu.
        IEventSelector selector = CreateSelector();
        EventPack pack = EventPack.Create("Wyłączone",
        [
            GameEvent.CreateCustom("Wyłączone", 100) with { IsEnabled = false },
        ]);

        Assert.Equal(0, SimulateTurns(selector, pack, turns: 200));
    }

    [Fact]
    public void SelectNext_NieNarzucaOdstepuMiedzyWydarzeniami()
    {
        // Globalny odstęp między wydarzeniami został usunięty, bo przy dwóch graczach działał
        // rażąco niesprawiedliwie: wydarzenia padały co drugą turę, więc trafiały wciąż tego
        // samego gracza, a drugi nie dostawał ich wcale. Częstotliwość jest wyborem gracza.
        IEventSelector selector = CreateSelector();
        EventPack pack = EventPack.Create("Pewniak", [GameEvent.CreateCustom("Zawsze", 100)]);

        List<int> turyZWydarzeniem = SimulateTurnsRecordingTurns(selector, pack, turns: 10);

        Assert.Equal(Enumerable.Range(1, 10), turyZWydarzeniem);
    }

    [Fact]
    public void SelectNext_PrzyDwochGraczach_WydarzeniaTrafiajaOboje()
    {
        // Sedno usuniętego ograniczenia: przy naprzemiennych turach każdy gracz musi mieć
        // szansę na wydarzenie, a nie tylko ten, na którego wypadła parzysta tura.
        IEventSelector selector = CreateSelector();
        EventPack pack = EventPack.Create("Częste", [GameEvent.CreateCustom("Zadanie", 60)]);

        List<int> tury = SimulateTurnsRecordingTurns(selector, pack, turns: 400);

        int nieparzyste = tury.Count(turn => turn % 2 == 1);
        int parzyste = tury.Count - nieparzyste;

        Assert.True(nieparzyste > 0 && parzyste > 0, "Wydarzenia trafiły tylko jednego gracza.");

        // Przy naprzemiennych turach podział powinien być bliski połowie.
        double udzialPierwszego = (double)nieparzyste / tury.Count;

        Assert.InRange(udzialPierwszego, 0.4, 0.6);
    }

    [Fact]
    public void SelectNext_MnoznikZero_WylaczaWydarzeniaCalkowicie()
    {
        // Tryb Classic (Etap 9) ma dawać czystego spinnera, bez wydarzeń.
        IEventSelector selector = CreateSelector();
        EventPack pack = EventPack.Create("Pewniak", [GameEvent.CreateCustom("Zawsze", 100)]);

        GameEvent? wynik = selector.SelectNext(new EventSelectionContext
        {
            Pack = pack,
            TurnNumber = 1,
            Options = EventSelectionOptions.Disabled,
        });

        Assert.Null(wynik);
    }

    [Fact]
    public void SelectNext_WydarzenieJednorazowe_PadaTylkoRaz()
    {
        IEventSelector selector = CreateSelector();
        GameEvent jednorazowe = GameEvent.CreateCustom("Raz", 100) with { IsOneShot = true };
        EventPack pack = EventPack.Create("Jednorazowe", [jednorazowe]);

        List<int> tury = SimulateTurnsRecordingTurns(selector, pack, turns: 30);

        Assert.Single(tury);
    }

    [Fact]
    public void SelectNext_WlasnyOdstepWydarzenia_JestRespektowany()
    {
        IEventSelector selector = CreateSelector();
        GameEvent zOdstepem = GameEvent.CreateCustom("Rzadkie", 100) with { CooldownTurns = 5 };
        EventPack pack = EventPack.Create("Z odstępem", [zOdstepem]);

        List<int> tury = SimulateTurnsRecordingTurns(selector, pack, turns: 30);

        Assert.True(tury.Count > 1, "Wydarzenie powinno wrócić po upływie własnego odstępu.");

        for (int i = 1; i < tury.Count; i++)
        {
            Assert.True(tury[i] - tury[i - 1] >= 5, $"Odstęp {tury[i] - tury[i - 1]} jest za krótki.");
        }
    }

    [Fact]
    public void SelectNext_SumaSzansPowyzejStu_DajePewneWystapienie()
    {
        // Użytkownik ma prawo ustawić dowolne wartości — traktujemy to jako pewne
        // wystąpienie, a nie błąd. Ekran paczek ostrzega w takiej sytuacji.
        IEventSelector selector = CreateSelector();
        EventPack pack = EventPack.Create("Przesada",
        [
            GameEvent.CreateCustom("A", 80),
            GameEvent.CreateCustom("B", 70),
        ]);

        Assert.Equal(30, SimulateTurns(selector, pack, turns: 30));
    }

    [Fact]
    public void SelectNext_WybieraProporcjonalnieDoSzans()
    {
        // Suma szans decyduje, JAK CZĘSTO cokolwiek się dzieje, a proporcje między
        // wydarzeniami — CO się wtedy dzieje. Ten test sprawdza drugą część.
        const int turns = 20_000;
        IEventSelector selector = CreateSelector();
        GameEvent czeste = GameEvent.CreateCustom("Częste", 75);
        GameEvent rzadkie = GameEvent.CreateCustom("Rzadkie", 25);
        EventPack pack = EventPack.Create("Proporcje", [czeste, rzadkie]);

        Dictionary<Guid, int> liczniki = new() { [czeste.Id] = 0, [rzadkie.Id] = 0 };

        for (int turn = 1; turn <= turns; turn++)
        {
            GameEvent? wynik = selector.SelectNext(new EventSelectionContext
            {
                Pack = pack,
                TurnNumber = turn,
                Options = EventSelectionOptions.Default,
            });

            if (wynik is not null)
            {
                liczniki[wynik.Id]++;
            }
        }

        int razem = liczniki[czeste.Id] + liczniki[rzadkie.Id];
        double udzialCzestego = (double)liczniki[czeste.Id] / razem;

        Assert.True(razem > 0, "Żadne wydarzenie nie padło.");
        Assert.InRange(udzialCzestego, 0.72, 0.78);
    }

    [Fact]
    public void SelectNext_CzestotliwoscOdpowiadaSumieSzans()
    {
        // Suma 20% powinna dawać wydarzenie w około jednej piątej tur.
        const int turns = 20_000;
        IEventSelector selector = CreateSelector();
        EventPack pack = EventPack.Create("Dwadzieścia",
        [
            GameEvent.CreateCustom("A", 10),
            GameEvent.CreateCustom("B", 10),
        ]);

        int wystapienia = SimulateTurns(selector, pack, turns);
        double udzial = (double)wystapienia / turns;

        Assert.InRange(udzial, 0.18, 0.22);
    }

    [Fact]
    public void SelectNext_TeSamoZiarno_DajeIdentycznaSekwencje()
    {
        EventPack pack = EventPack.Create("Test",
        [
            GameEvent.CreateCustom("A", 30),
            GameEvent.CreateCustom("B", 30),
        ]);

        List<int> pierwsza = SimulateTurnsRecordingTurns(CreateSelector(777), pack, 100);
        List<int> druga = SimulateTurnsRecordingTurns(CreateSelector(777), pack, 100);

        Assert.Equal(pierwsza, druga);
    }

    private static IEventSelector CreateSelector(int seed = 4242) =>
        new WeightedEventSelector(new SeededRandomProvider(seed));

    /// <summary>Rozgrywa tury, prowadząc historię wydarzeń jak silnik gry, i liczy wystąpienia.</summary>
    private static int SimulateTurns(IEventSelector selector, EventPack pack, int turns) =>
        SimulateTurnsRecordingTurns(selector, pack, turns).Count;

    /// <summary>Rozgrywa tury i zwraca numery tur, w których padło wydarzenie.</summary>
    private static List<int> SimulateTurnsRecordingTurns(
        IEventSelector selector,
        EventPack pack,
        int turns)
    {
        EventSelectionOptions options = EventSelectionOptions.Default;
        Dictionary<Guid, int> lastEventTurns = [];
        int? lastEventTurn = null;
        List<int> result = [];

        for (int turn = 1; turn <= turns; turn++)
        {
            GameEvent? selected = selector.SelectNext(new EventSelectionContext
            {
                Pack = pack,
                TurnNumber = turn,
                LastEventTurn = lastEventTurn,
                LastEventTurns = lastEventTurns,
                Options = options,
            });

            if (selected is null)
            {
                continue;
            }

            result.Add(turn);
            lastEventTurn = turn;
            lastEventTurns[selected.Id] = turn;
        }

        return result;
    }
}
