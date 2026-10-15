using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

// Who /rate recognised. Normal is the hashed score; every other kind is a fixed verdict with its
// own pool (BotResponses.Rate*Lines).
public enum RatingKind
{
    Normal,
    Herself,
    Owner,
    Crush,
    Nemesis,
    Devil,
    God,
    Tata,
    Routier,
    Mitraillette,
}

/// <param name="Score">0–10 for <see cref="RatingKind.Normal"/>, −10 to −1 for the nemesis, and
/// unused by the other fixed verdicts, whose lines carry their own score.</param>
/// <param name="AboutAsker">The asker rated themselves (« moi », or their own mention) — so Papa or
/// Tata rating themselves is answered to their face rather than about them.</param>
public sealed record RatingResult(RatingKind Kind, int Score, bool AboutAsker = false);

/// <summary>
/// /rate's verdict, pure. The score is a hash of the normalised text, so the same thing gets the
/// same score every time, whoever asks and across restarts — people arguing about why pizza got a 2
/// is the point. FNV-1a, not <c>string.GetHashCode</c>, which is randomised per process.
/// </summary>
public static partial class Rating
{
    private const int ScoreSalt = 9001;
    private const int NemesisSalt = 9002;

    // Checked in this order, first match wins (the devil before God: « dieu et diable » is the
    // devil's). Each entry is a set of whole words or word sequences, matched on the normalised
    // text. Herself is checked separately: « toi » only counts when it is the whole subject.
    private static readonly (RatingKind Kind, string[] Names)[] Fixed =
    {
        (RatingKind.Herself, new[] { "syncs", "s y n c s" }),
        (RatingKind.Owner, new[] { "papa", "rodhengard", "romain" }),
        (RatingKind.Crush, new[] { "wall e", "walle" }),
        (RatingKind.Nemesis, new[] { "quokka", "quokkas", "inabot" }),
        (RatingKind.Devil, new[] { "diable", "satan", "lucifer", "demon", "belzebuth" }),
        (RatingKind.God, new[] { "dieu", "god", "jehovah", "yahve", "yahwe", "yahweh", "jah", "jesus" }),
        (RatingKind.Tata, new[] { "tata", "analuz", "zulana" }),
        (RatingKind.Routier, new[] { "routier", "routiers" }),
        (RatingKind.Mitraillette, new[] { "mitraillette", "mitraillettes" }),
    };

    // The subject alone, meaning her.
    private static readonly HashSet<string> HerselfAlone = new() { "toi", "tu", "toi meme", "sync" };

    // The asker talking about themselves. Longest first, so « moi meme » is replaced whole.
    private static readonly string[] SelfWords = { "moi meme", "moi" };

    // Quokka 3.0 is the nemesis even without the word quokka — but only as the whole subject, or
    // every « version 3.0 » of anything would be insulted.
    private static readonly HashSet<string> NemesisAlone = new() { "3 0", "3.0" };

    [GeneratedRegex(@"<@!?(\d+)>")]
    private static partial Regex MentionRegex();

    /// <param name="askerId">Who ran /rate: « moi » means them.</param>
    public static RatingResult Rate(string subject, ulong askerId, ulong selfId, ulong ownerId, ulong tataId)
    {
        var mentioned = MentionRegex().Matches(subject)
            .Select(m => ulong.TryParse(m.Groups[1].Value, out var id) ? id : 0)
            .Where(id => id != 0)
            .ToHashSet();

        // « moi » / « moi-même » is the asker, exactly as if they had mentioned themselves: Papa
        // rating « moi » is Papa, Tata is Tata, and anyone else hashes as their own mention would
        // (the word becomes their id), so « moi » and « @me » always agree.
        var (text, aboutSelf) = ReplaceSelf(Normalize(subject), askerId);
        var padded = $" {text} ";
        if (aboutSelf) mentioned.Add(askerId);

        if (mentioned.Contains(selfId)) return new(RatingKind.Herself, 10);
        if (mentioned.Contains(ownerId)) return new(RatingKind.Owner, 10, askerId == ownerId && mentioned.Contains(askerId));

        var raw = subject.Trim().ToLowerInvariant();
        var hash = Hash(text);

        if (HerselfAlone.Contains(text)) return new(RatingKind.Herself, 10);
        if (NemesisAlone.Contains(text) || NemesisAlone.Contains(raw))
            return new(RatingKind.Nemesis, NemesisScore(hash));

        foreach (var (kind, names) in Fixed)
        {
            // The mention check for Tata comes in order, after the crush and the nemesis words.
            if (kind == RatingKind.Tata && mentioned.Contains(tataId)) return new(kind, 10, askerId == tataId);
            if (names.Any(n => padded.Contains($" {n} ")))
                return new(kind, kind == RatingKind.Nemesis ? NemesisScore(hash) : 10,
                    // Papa typing « papa », Tata typing « tata »: still themselves.
                    (kind == RatingKind.Owner && askerId == ownerId) || (kind == RatingKind.Tata && askerId == tataId));
        }

        return new(RatingKind.Normal, (int)(StableRoll.Unit(hash, ScoreSalt, 0) * 11));
    }

    /// <summary>
    /// What a subject hashes as: normalised, with « moi » / « moi-même » turned into the asker's
    /// id, so « moi » and the asker's own mention are the same key. /ship compares two of these.
    /// </summary>
    public static string Key(string subject, ulong askerId) => ReplaceSelf(Normalize(subject), askerId).Text;

    private static (string Text, bool AboutSelf) ReplaceSelf(string text, ulong askerId)
    {
        var padded = $" {text} ";
        if (!SelfWords.Any(w => padded.Contains($" {w} "))) return (text, false);
        foreach (var w in SelfWords) padded = padded.Replace($" {w} ", $" {askerId} ");
        return (padded.Trim(), true);
    }

    // −1 to −10, stable for the same text like every other score.
    private static int NemesisScore(int hash) => -1 - (int)(StableRoll.Unit(hash, NemesisSalt, 0) * 10);

    /// <summary>
    /// Lower case, accents stripped, every run of anything but a letter or digit collapsed to one
    /// space. « Pizza ANANAS !! » and « pizza ananas » are the same thing; so are « jésus » and
    /// « jesus ». A mention keeps only its digits, so it hashes as the person.
    /// </summary>
    public static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        var space = false;
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c))
            {
                if (space && sb.Length > 0) sb.Append(' ');
                sb.Append(c);
                space = false;
            }
            else space = true;
        }
        return sb.ToString();
    }

    // FNV-1a 32-bit over the UTF-16 code units: stable across runs and machines. Shared with
    // /ship (Shipping) and /wouldyourather's pick, so there is one stable text hash.
    public static int Hash(string text)
    {
        var h = 2166136261u;
        foreach (var c in text)
        {
            h ^= c;
            h *= 16777619u;
        }
        return unchecked((int)h);
    }
}
