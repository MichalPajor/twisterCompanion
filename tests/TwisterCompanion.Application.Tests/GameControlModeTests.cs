using TwisterCompanion.Application.Settings;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy przejść między parą przełączników w ustawieniach a jednym wyborem z trzech.
/// </summary>
/// <remarks>
/// Reguła wykluczania — sterowanie głosem tylko przy turach ręcznych — mieszkała dotąd
/// w dwóch miejscach: w ekranie ustawień i w składaniu nastaw partii. Przycisk na ekranie
/// rozgrywki byłby trzecim. Ten zestaw pilnuje jedynej kopii, która została.
/// </remarks>
public class GameControlModeTests
{
    [Theory]
    [InlineData(GameControlMode.Manual)]
    [InlineData(GameControlMode.Automatic)]
    [InlineData(GameControlMode.Voice)]
    public void ZapisIOdczyt_DajaTenSamTryb(GameControlMode tryb)
    {
        AppSettings ustawienia = GameControlModes.Apply(AppSettings.Default, tryb);

        Assert.Equal(tryb, GameControlModes.From(ustawienia));
    }

    [Fact]
    public void TrybAutomatyczny_WylaczaSterowanieGlosem()
    {
        // Przy turach zmieniających się samoczynnie nie ma czym sterować głosem, więc
        // zapisanie obu naraz byłoby stanem, którego aplikacja nie umie obsłużyć.
        AppSettings zGlosem = AppSettings.Default with { IsVoiceControlEnabled = true };

        AppSettings wynik = GameControlModes.Apply(zGlosem, GameControlMode.Automatic);

        Assert.False(wynik.IsVoiceControlEnabled);
        Assert.Equal(TurnAdvanceMode.Automatic, wynik.TurnAdvanceMode);
    }

    [Fact]
    public void TrybGlosowy_WymuszaTuryReczne()
    {
        AppSettings automatyczne = AppSettings.Default with { TurnAdvanceMode = TurnAdvanceMode.Automatic };

        AppSettings wynik = GameControlModes.Apply(automatyczne, GameControlMode.Voice);

        Assert.Equal(TurnAdvanceMode.Manual, wynik.TurnAdvanceMode);
        Assert.True(wynik.IsVoiceControlEnabled);
    }

    [Fact]
    public void UstawieniaZObomaWlaczonymi_CzytaneSaJakoAutomatyczne()
    {
        // Stan niemożliwy do ustawienia przez interfejs, ale możliwy w pliku ustawień —
        // na przykład po ręcznej edycji. Odczyt musi dać jedną, przewidywalną odpowiedź.
        AppSettings sprzeczne = AppSettings.Default with
        {
            TurnAdvanceMode = TurnAdvanceMode.Automatic,
            IsVoiceControlEnabled = true,
        };

        Assert.Equal(GameControlMode.Automatic, GameControlModes.From(sprzeczne));
    }

    [Fact]
    public void Przelaczanie_ObchodziWszystkieTrybyIWraca()
    {
        GameControlMode tryb = GameControlMode.Manual;
        List<GameControlMode> odwiedzone = [];

        for (int krok = 0; krok < GameControlModes.All.Count; krok++)
        {
            odwiedzone.Add(tryb);
            tryb = GameControlModes.Next(tryb);
        }

        Assert.Equal(GameControlModes.All, odwiedzone);
        Assert.Equal(GameControlMode.Manual, tryb);
    }
}
