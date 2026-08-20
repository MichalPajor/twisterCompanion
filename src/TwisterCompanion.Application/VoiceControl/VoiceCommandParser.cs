using System.Globalization;
using System.Text;
using TwisterCompanion.Application.Abstractions;

namespace TwisterCompanion.Application.VoiceControl;

/// <summary>
/// Dopasowuje rozpoznany tekst do komend: najpierw dokładnie, potem z tolerancją na przekręcenia.
/// </summary>
/// <remarks>
/// Rozpoznawanie mowy zwraca całe zdania, a nie pojedyncze słowa — „no dalej", „dalej dalej",
/// „okej dalej". Dlatego szukamy frazy <b>wewnątrz</b> tekstu, a nie porównujemy całości.
/// <para>
/// <b>Dopasowanie rozmyte jest asymetrycznie ostrożne.</b> Nierozpoznana komenda tylko irytuje
/// — gracz powtórzy. Komenda rozpoznana w przypadkowym słowie z rozmowy przerywa turę w środku
/// ruchu, więc tolerancja rośnie z długością frazy: krótkie słowa muszą trafić dokładnie,
/// bo przy trzech literach każda pomyłka to już inne słowo.
/// </para>
/// <para>
/// Frazy są wczytywane raz na język i trzymane w pamięci: parser dostaje każdy wynik częściowy,
/// więc chodzi kilka razy na sekundę i nie może za każdym razem sięgać do zasobów.
/// </para>
/// </remarks>
internal sealed class VoiceCommandParser(
    IVoiceCommandRegistry registry,
    ILocalizationService localization) : IVoiceCommandParser
{
    private readonly IVoiceCommandRegistry _registry =
        registry ?? throw new ArgumentNullException(nameof(registry));

    private readonly ILocalizationService _localization =
        localization ?? throw new ArgumentNullException(nameof(localization));

    private readonly Lock _guard = new();

    private string? _cachedCultureName;
    private List<NormalizedPhrase> _cachedPhrases = [];

    /// <inheritdoc />
    public bool TryParse(string? recognizedText, out VoiceCommandType command)
    {
        command = default;

        if (string.IsNullOrWhiteSpace(recognizedText))
        {
            return false;
        }

        string[] tokens = Normalize(recognizedText).Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
        {
            return false;
        }

        List<NormalizedPhrase> phrases = GetPhrases();

        // Dwa przejścia, a nie jedno: fraza dopasowana dokładnie zawsze wygrywa z frazą
        // dopasowaną z tolerancją, nawet jeśli ta druga stoi wcześniej na liście.
        foreach (NormalizedPhrase phrase in phrases)
        {
            if (ContainsExact(tokens, phrase.Tokens))
            {
                command = phrase.Command;

                return true;
            }
        }

        foreach (NormalizedPhrase phrase in phrases)
        {
            if (ContainsApproximate(tokens, phrase))
            {
                command = phrase.Command;

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sprowadza tekst do postaci porównywalnej: małe litery, bez znaków diakrytycznych,
    /// bez interpunkcji, pojedyncze odstępy.
    /// </summary>
    /// <remarks>
    /// Znaki diakrytyczne znikają, bo rozpoznawanie mowy myli „powtórz" z „powtorz", a
    /// interpunkcja, bo dopisuje kropki i przecinki tam, gdzie nikt ich nie wypowiedział.
    /// Polskie „ł" jest osobną literą bez rozkładu na znak podstawowy, więc wymaga własnej
    /// podmiany — sama normalizacja Unicode by go nie ruszyła.
    /// </remarks>
    private static string Normalize(string value)
    {
        string decomposed = value.ToLowerInvariant()
            .Replace('ł', 'l')
            .Normalize(NormalizationForm.FormD);

        StringBuilder builder = new(decomposed.Length);

        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (char.IsWhiteSpace(character))
            {
                builder.Append(' ');
            }
        }

        return string.Join(
            ' ',
            builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Sprawdza, czy fraza występuje w tekście słowo w słowo.</summary>
    private static bool ContainsExact(string[] tokens, string[] phraseTokens)
    {
        if (phraseTokens.Length > tokens.Length)
        {
            return false;
        }

        for (int start = 0; start <= tokens.Length - phraseTokens.Length; start++)
        {
            bool matches = true;

            for (int offset = 0; offset < phraseTokens.Length; offset++)
            {
                if (!string.Equals(tokens[start + offset], phraseTokens[offset], StringComparison.Ordinal))
                {
                    matches = false;

                    break;
                }
            }

            if (matches)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sprawdza, czy w tekście jest fragment dostatecznie bliski frazie.
    /// </summary>
    /// <remarks>
    /// Okno przesuwane o długości frazy, a nie porównanie całego tekstu: „okej dalej proszę"
    /// ma zadziałać, a przy porównaniu całości odległość byłaby ogromna.
    /// </remarks>
    private static bool ContainsApproximate(string[] tokens, NormalizedPhrase phrase)
    {
        if (phrase.MaxDistance == 0 || phrase.Tokens.Length > tokens.Length)
        {
            return false;
        }

        for (int start = 0; start <= tokens.Length - phrase.Tokens.Length; start++)
        {
            string window = string.Join(' ', tokens, start, phrase.Tokens.Length);

            if (Distance(window, phrase.Text, phrase.MaxDistance) <= phrase.MaxDistance)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Liczy odległość edycyjną, przerywając, gdy przekroczy dopuszczalną wartość.
    /// </summary>
    /// <remarks>
    /// Dwa wiersze zamiast pełnej macierzy i wczesne wyjście, bo metoda jest wywoływana dla
    /// każdej frazy przy każdym wyniku częściowym.
    /// </remarks>
    private static int Distance(string left, string right, int limit)
    {
        if (Math.Abs(left.Length - right.Length) > limit)
        {
            return limit + 1;
        }

        int[] previous = new int[right.Length + 1];
        int[] current = new int[right.Length + 1];

        for (int index = 0; index <= right.Length; index++)
        {
            previous[index] = index;
        }

        for (int leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            current[0] = leftIndex;
            int rowMinimum = current[0];

            for (int rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                int substitution = previous[rightIndex - 1]
                    + (left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1);

                current[rightIndex] = Math.Min(
                    Math.Min(previous[rightIndex] + 1, current[rightIndex - 1] + 1),
                    substitution);

                rowMinimum = Math.Min(rowMinimum, current[rightIndex]);
            }

            if (rowMinimum > limit)
            {
                return limit + 1;
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private List<NormalizedPhrase> GetPhrases()
    {
        string cultureName = _localization.CurrentCulture.Name;

        lock (_guard)
        {
            if (string.Equals(_cachedCultureName, cultureName, StringComparison.Ordinal))
            {
                return _cachedPhrases;
            }

            List<NormalizedPhrase> phrases = [];

            foreach (VoiceCommandDefinition definition in _registry.GetCommands())
            {
                foreach (string phrase in definition.Phrases)
                {
                    string normalized = Normalize(phrase);

                    if (normalized.Length == 0)
                    {
                        continue;
                    }

                    phrases.Add(new NormalizedPhrase(
                        definition.Type,
                        normalized,
                        normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                        GetMaxDistance(normalized)));
                }
            }

            // Dłuższe frazy najpierw: „gracz odpadl" ma zostać dopasowane jako całość,
            // zanim zadziała krótszy synonim.
            phrases.Sort((first, second) => second.Text.Length.CompareTo(first.Text.Length));

            _cachedCultureName = cultureName;
            _cachedPhrases = phrases;

            return phrases;
        }
    }

    /// <summary>
    /// Ile pomyłek wolno wybaczyć frazie o danej długości.
    /// </summary>
    /// <remarks>
    /// Progi wynikają z tego, jak myli się rozpoznawanie mowy: przy krótkich słowach jedna
    /// litera zmienia znaczenie („stop" i „stoi"), przy dłuższych zwykle gubi się końcówka
    /// albo pojedynczy dźwięk („powtorz" zamiast „powtorzy").
    /// </remarks>
    private static int GetMaxDistance(string phrase) => phrase.Length switch
    {
        <= 4 => 0,
        <= 8 => 1,
        _ => 2,
    };

    /// <summary>Fraza przygotowana do porównań.</summary>
    private sealed record NormalizedPhrase(
        VoiceCommandType Command,
        string Text,
        string[] Tokens,
        int MaxDistance);
}
