using System.Text;

namespace ProjectSYNCS.Helpers;

// The glitch easter egg's text side: a line she was about to say, corrupted. Pure, with the
// Random passed in, so a corruption can be checked without a gateway. GlitchService decides
// when; this only decides how.
//
// Two or three of: a letter swapped for a broken byte or its look-alike digit, a stuttered
// word, a word smeared with combining strokes, and sometimes the end cut off. Only words with
// three letters or more are touched, and never a token carrying markup — custom emotes,
// mentions, links — so nothing breaks and nobody gets pinged by mangled markup.
public static class Glitch
{
    // How rarely an eligible line comes out corrupted, and then at most once per Cooldown.
    public const double Chance = 1.0 / 200;
    public static readonly TimeSpan Cooldown = TimeSpan.FromDays(1);

    // How long the corrupted line stands before she quietly puts the real one back.
    public static readonly TimeSpan RestoreMin = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan RestoreMax = TimeSpan.FromSeconds(15);

    private const double TruncateChance = 0.3;

    // Look-alikes first, then the broken byte: the line stays readable, just wrong.
    private static readonly Dictionary<char, string> Swaps = new()
    {
        ['a'] = "@", ['e'] = "3", ['i'] = "1", ['o'] = "0", ['s'] = "5",
        ['é'] = "�", ['è'] = "�", ['à'] = "�", ['ç'] = "�", ['ê'] = "�",
    };

    // Combining overlays: the strike-through smear of a corrupted glyph.
    private static readonly char[] Strokes = { '̵', '̶', '̷', '̸' };

    /// <summary>
    /// The corrupted version of <paramref name="line"/>, or null when it has nothing safe to
    /// corrupt (too short, or all markup).
    /// </summary>
    public static string? Corrupt(string line, Random rng)
    {
        var words = line.Split(' ');
        var eligible = Enumerable.Range(0, words.Length).Where(i => IsEligible(words[i])).ToList();
        if (eligible.Count == 0) return null;

        // Two effects, sometimes three, each on its own word where the line allows.
        var effects = new List<Action<int>>
        {
            i => words[i] = Swap(words[i], rng),
            i => words[i] = Stutter(words[i]),
            i => words[i] = Smear(words[i], rng),
        };
        Shuffle(effects, rng);
        var count = rng.Next(2, 4);
        var targets = eligible.OrderBy(_ => rng.Next()).ToList();
        for (var k = 0; k < count; k++)
            effects[k % effects.Count](targets[k % targets.Count]);

        var result = string.Join(' ', words);

        // Sometimes she doesn't get to the end. Cut at a space, past the first third.
        if (rng.NextDouble() < TruncateChance)
        {
            var cut = result.LastIndexOf(' ', Math.Max(0, result.Length * 3 / 4));
            if (cut > result.Length / 3)
                result = result[..cut].TrimEnd(',', ';', ':', ' ', '—') + "—";
        }

        return result == line ? null : result;
    }

    private static bool IsEligible(string word) =>
        word.Count(char.IsLetter) >= 3
        && !word.Contains('<') && !word.Contains('>') && !word.Contains('@')
        && !word.StartsWith("http", StringComparison.OrdinalIgnoreCase);

    // One or two letters swapped for a look-alike or a broken byte.
    private static string Swap(string word, Random rng)
    {
        var chars = Enumerable.Range(0, word.Length).Where(i => Swaps.ContainsKey(char.ToLowerInvariant(word[i]))).ToList();
        if (chars.Count == 0)
        {
            // Nothing swappable: a broken byte in place of one letter.
            var letters = Enumerable.Range(0, word.Length).Where(i => char.IsLetter(word[i])).ToList();
            var at = letters[rng.Next(letters.Count)];
            return word[..at] + "�" + word[(at + 1)..];
        }

        var sb = new StringBuilder(word);
        var picks = chars.OrderBy(_ => rng.Next()).Take(rng.Next(1, 3)).OrderByDescending(i => i);
        foreach (var i in picks)
        {
            sb.Remove(i, 1);
            sb.Insert(i, Swaps[char.ToLowerInvariant(word[i])]);
        }
        return sb.ToString();
    }

    // « sessions » → « ses— ses— sessions »: she gets stuck on it.
    private static string Stutter(string word)
    {
        var lead = word.TakeWhile(c => !char.IsLetter(c)).Count();
        var stem = word.Substring(lead, Math.Min(3, word.Length - lead));
        return $"{word[..lead]}{stem}— {stem}— {word[lead..]}";
    }

    // Every letter of the word struck through with a combining stroke.
    private static string Smear(string word, Random rng)
    {
        var sb = new StringBuilder(word.Length * 2);
        foreach (var c in word)
        {
            sb.Append(c);
            if (char.IsLetter(c)) sb.Append(Strokes[rng.Next(Strokes.Length)]);
        }
        return sb.ToString();
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
