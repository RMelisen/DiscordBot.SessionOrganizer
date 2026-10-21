using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// A roll asked in chat: <see cref="Count"/> dice of <see cref="Faces"/> faces (« 2d6 », « lance un
/// dé »), or a number from <see cref="Min"/> to <see cref="Max"/> (« un nombre entre 1 et 100 »).
/// </summary>
public readonly record struct DiceAsk(int Count, int Faces, long Min, long Max, bool IsRange);

/// <summary>One roll, in finished words: what was rolled (« 2d6 », « un nombre entre 1 et 100 ») and what came out.</summary>
public readonly record struct DiceRoll(string Spec, string Result);

/// <summary>
/// Dice and random numbers asked in chat (« lance un dé », « 1d20 », « choisis un nombre entre 1 et
/// 10 »). Pure but for the <see cref="Random"/> passed in. Read on the normalised text
/// (<see cref="Rating.Normalize"/>), so « dé » is « de »: the die word only counts right after a count
/// and before nothing but a few closers, or « lance un de tes sorts » would roll.
/// </summary>
public static partial class Dice
{
    public const int MaxDice = 20;
    public const int MaxFaces = 1000;
    // A roll is a short message: past this it is a sentence that happens to hold « un dé ».
    private const int MaxTokens = 12;
    // Twelve digits at most: the sum and the span stay far inside a long.
    private const long MaxNumber = 999_999_999_999;

    // « 2d6 », « 1d20 », « d20 ». The faces are required; « 3d » is a film.
    [GeneratedRegex(@"^(\d{1,2})?d(\d{1,4})$", RegexOptions.CultureInvariant)]
    private static partial Regex DiceTokenRegex();

    // Rolling, asked or ordered. « tire » too: « tire un nombre » (« tire au sort » has no die or number).
    private static readonly HashSet<string> RollVerbs = new()
    {
        "lance", "lances", "lancer", "lancez", "relance", "jette", "jettes", "jeter", "jetez", "roll", "rolls", "tire",
        "tires", "tirer",
        // « un jet de d20 »: the noun rolls too.
        "jet",
    };

    // Picking a number: « choisis un nombre », « donne-moi un chiffre », « pense à un nombre ».
    private static readonly HashSet<string> PickVerbs = new()
    {
        "choisis", "choisi", "choisir", "donne", "donnes", "donner", "pense", "penses", "dis", "genere", "generer",
        "trouve", "pick", "tire", "tires", "tirer", "lance", "lancer",
    };

    // A verb right after one of these is told, not asked: « j'ai lancé un dé ».
    private static readonly HashSet<string> PastAuxiliaries = new() { "ai", "as", "a", "avons", "avez", "ont", "avait", "avais" };

    private static readonly Dictionary<string, int> CountWords = new()
    {
        ["un"] = 1, ["une"] = 1, ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4, ["cinq"] = 5, ["six"] = 6, ["les"] = 2,
    };

    // What may follow the die word: the end, the faces, or a closer. « un dé de tes sorts » is no die.
    private static readonly HashSet<string> DieFollowers = new()
    {
        "a", "stp", "svp", "syncs", "sync", "pour", "please", "pls", "faces", "steuplait",
    };

    private static readonly HashSet<string> NumberNouns = new() { "nombre", "chiffre", "numero", "number" };
    private static readonly HashSet<string> RandomWords = new() { "hasard", "aleatoire", "random", "aleatoirement" };

    // A range with no bounds given: « un nombre au hasard ».
    private const long DefaultMin = 1, DefaultMax = 100;

    /// <summary>What the message asks to roll, or null when it asks none.</summary>
    public static DiceAsk? Read(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var t = Rating.Normalize(MentionOrEmoteRegex().Replace(content, " "))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (t.Length is 0 or > MaxTokens) return null;

        var rollVerb = HasVerb(t, RollVerbs);

        // « 2d6 » anywhere; « d20 » only rolled or (nearly) alone, since « d4 » is also Diablo IV.
        foreach (var w in t)
        {
            if (DiceTokenRegex().Match(w) is not { Success: true } m) continue;
            var faces = int.Parse(m.Groups[2].Value);
            if (!m.Groups[1].Success && !rollVerb && t.Length > 2) continue;
            var count = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : 1;
            return Dicey(count, faces);
        }

        // « lance un dé », « lance les dés », « jette deux dés à 20 faces ».
        if (rollVerb)
            for (var i = 1; i < t.Length; i++)
            {
                if (t[i] is not ("de" or "des")) continue;
                if (i + 1 < t.Length && !DieFollowers.Contains(t[i + 1]) && !t[i + 1].All(char.IsDigit)) continue;
                int count;
                if (CountWords.TryGetValue(t[i - 1], out var word)) count = word;
                else if (int.TryParse(t[i - 1], out var digits)) count = digits;
                // « un jet de dés »: one roll of two dice, the classic.
                else if (t[i - 1] == "de" && i >= 2 && t[i - 2] == "jet") count = t[i] == "des" ? 2 : 1;
                else continue;
                return Dicey(count, FacesAfter(t, i + 1) ?? 6);
            }

        // « un nombre entre 1 et 10 », « choisis un chiffre », « un nombre au hasard » — picked, not
        // counted: « le nombre de joueurs entre 4 et 8 » asks for nothing.
        // « random entre 1 et 100 », « au hasard de 1 à 6 »: a range said random needs no noun.
        if (t.Any(RandomWords.Contains) && !t.Any(NumberNouns.Contains) && RangeAfter(t, 0) is { } bare)
            return new DiceAsk(0, 0, bare.Min, bare.Max, IsRange: true);

        // « un nombre », never « le nombre »: « donne-moi le nombre de joueurs » asks a count.
        var noun = Array.FindIndex(t, 1, w => NumberNouns.Contains(w));
        if (noun < 0 || t[noun - 1] is not ("un" or "une")) return null;
        var range = RangeAfter(t, noun + 1);
        // « un nombre entre 1 et 10 » or « un nombre ? » opening the message picks too; « un nombre de
        // joueurs limité » doesn't.
        var opens = noun == 1 && t[0] is "un" or "une" && (range is not null || t.Length <= 3);
        if (!HasVerb(t, PickVerbs) && !t.Any(RandomWords.Contains) && !opens) return null;

        var (min, max) = range ?? (DefaultMin, DefaultMax);
        return new DiceAsk(0, 0, min, max, IsRange: true);
    }

    // « entre 1 et 10 », « de 1 à 6 », lowest first.
    private static (long Min, long Max)? RangeAfter(string[] t, int from)
    {
        for (var i = from; i + 3 < t.Length; i++)
        {
            if (!(t[i] == "entre" && t[i + 2] == "et") && !(t[i] == "de" && t[i + 2] == "a")) continue;
            if (!TryNumber(t[i + 1], out var lo) || !TryNumber(t[i + 3], out var hi)) return null;
            return (Math.Min(lo, hi), Math.Max(lo, hi));
        }
        return null;
    }

    /// <summary>The roll itself: each die and their sum (« 3 + 5 = 8 »), or the number drawn.</summary>
    public static DiceRoll Roll(DiceAsk ask, Random random)
    {
        if (ask.IsRange)
            return new DiceRoll($"un nombre entre {ask.Min} et {ask.Max}", random.NextInt64(ask.Min, ask.Max + 1).ToString());

        var rolls = Enumerable.Range(0, ask.Count).Select(_ => random.Next(1, ask.Faces + 1)).ToArray();
        var result = rolls.Length == 1 ? rolls[0].ToString() : $"{string.Join(" + ", rolls)} = {rolls.Sum()}";
        return new DiceRoll($"{ask.Count}d{ask.Faces}", result);
    }

    private static DiceAsk? Dicey(int count, int faces) =>
        count is >= 1 and <= MaxDice && faces is >= 2 and <= MaxFaces ? new DiceAsk(count, faces, 0, 0, IsRange: false) : null;

    // « à 20 faces », « 20 faces »: the faces named after the die word.
    private static int? FacesAfter(string[] t, int from)
    {
        for (var i = from; i + 1 < t.Length; i++)
            if (t[i + 1] == "faces" && int.TryParse(t[i], out var faces)) return faces;
        return null;
    }

    // A verb of the set, not told in the past (« j'ai lancé »).
    private static bool HasVerb(string[] t, HashSet<string> verbs)
    {
        for (var i = 0; i < t.Length; i++)
            if (verbs.Contains(t[i]) && (i == 0 || !PastAuxiliaries.Contains(t[i - 1]))) return true;
        return false;
    }

    private static bool TryNumber(string word, out long value) =>
        long.TryParse(word, out value) && value is >= 0 and <= MaxNumber;

    [GeneratedRegex(@"<a?:\w+:\d+>|<[@#][!&]?\d+>", RegexOptions.CultureInvariant)]
    private static partial Regex MentionOrEmoteRegex();
}
