using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using TwisterCompanion.Application.Abstractions;
using TwisterCompanion.Application.Localization;

namespace TwisterCompanion.Application.Tests;

/// <summary>
/// Testy stałych z kluczami zasobów używanymi z kodu C#.
/// </summary>
public class StringKeysTests
{
    private static readonly Assembly ResourceAssembly = typeof(ILocalizationService).Assembly;

    [Fact]
    public void KazdaStala_MaOdpowiadajacyKluczWWlasciwymKatalogu()
    {
        // Dodanie stałej bez tłumaczenia nie przejdzie tego testu. Bez niego brak byłby
        // widoczny dopiero na ekranie, w postaci klucza w nawiasach kwadratowych.
        IReadOnlySet<string> ui = ReadKeys("AppResources");
        IReadOnlySet<string> voice = ReadKeys("VoiceResources");

        string[] brakujace =
        [
            .. CollectKeys()
                .Where(entry => !(entry.IsVoice ? voice : ui).Contains(entry.Key))
                .Select(entry => $"{(entry.IsVoice ? "Voice" : "Ui")}:{entry.Key}")
                .Order(),
        ];

        Assert.Empty(brakujace);
    }

    [Fact]
    public void StalychJestPrzynajmniejTyleIleZadeklarowano() => Assert.NotEmpty(CollectKeys());

    [Fact]
    public void ZadneDwieStale_NieWskazujaTegoSamegoKlucza()
    {
        List<string> wartosci = [.. CollectKeys().Select(entry => entry.Key)];

        Assert.Equal(wartosci.Count, wartosci.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void PrefiksyKluczy_KoncszaSiePodkresleniem()
    {
        // Prefiksy służą do budowania kluczy z nazw wartości wyliczeniowych
        // (Voice_BodyPart_ + RightHand). Brak podkreślenia na końcu dałby klucz
        // typu Voice_BodyPartRightHand, którego nie ma w zasobach.
        string[] niepoprawne =
        [
            .. CollectConstants(typeof(StringKeys))
                .Where(entry => entry.FieldName.EndsWith("Prefix", StringComparison.Ordinal))
                .Where(entry => !entry.Value.EndsWith('_'))
                .Select(entry => entry.Value)
                .Order(),
        ];

        Assert.Empty(niepoprawne);
    }

    /// <summary>Zwraca stałe będące pełnymi kluczami, z informacją o katalogu.</summary>
    /// <remarks>
    /// Stałe z nazwą kończącą się na <c>Prefix</c> są pomijane — to fragmenty kluczy,
    /// a nie klucze. Katalog wynika z nazwy typu zagnieżdżonego.
    /// </remarks>
    private static IEnumerable<(string Key, bool IsVoice)> CollectKeys() =>
        CollectConstants(typeof(StringKeys))
            .Where(entry => !entry.FieldName.EndsWith("Prefix", StringComparison.Ordinal))
            .Select(entry => (entry.Value, entry.DeclaringTypeName == nameof(StringKeys.Voice)));

    private static IEnumerable<(string DeclaringTypeName, string FieldName, string Value)> CollectConstants(Type type)
    {
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field is { IsLiteral: true, IsInitOnly: false }
                && field.GetRawConstantValue() is string value)
            {
                yield return (type.Name, field.Name, value);
            }
        }

        foreach (Type nested in type.GetNestedTypes(BindingFlags.Public))
        {
            foreach ((string declaringTypeName, string fieldName, string value) in CollectConstants(nested))
            {
                yield return (declaringTypeName, fieldName, value);
            }
        }
    }

    private static IReadOnlySet<string> ReadKeys(string catalog)
    {
        ResourceManager manager = new(
            "TwisterCompanion.Application.Resources.Strings." + catalog,
            ResourceAssembly);

        using ResourceSet? set = manager.GetResourceSet(
            CultureInfo.InvariantCulture,
            createIfNotExists: true,
            tryParents: false);

        HashSet<string> keys = new(StringComparer.Ordinal);

        if (set is null)
        {
            return keys;
        }

        foreach (DictionaryEntry entry in set)
        {
            if (entry.Key is string key)
            {
                keys.Add(key);
            }
        }

        return keys;
    }
}
