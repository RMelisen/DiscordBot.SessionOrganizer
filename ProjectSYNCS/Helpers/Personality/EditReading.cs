namespace ProjectSYNCS.Helpers;

/// <summary>What an edit did to a message she had answered (<see cref="EditReading.Read"/>).</summary>
public enum EditKind
{
    /// <summary>A typo, spacing, a link preview loading: nothing she answered changed.</summary>
    None,

    /// <summary>The message now says something else: suspicious.</summary>
    Rewritten,

    /// <summary>Made mean after she answered it: she pouts or gets angry.</summary>
    TurnedMean,

    /// <summary>Mean when she answered, softened since: too late, she read it.</summary>
    CoveredUp,
}

/// <summary>
/// Reading an edit or a deletion of a message she replied to (<c>ReplyWatchService</c>), and
/// writing her reply's new version. Pure, so the « real change » rule can be checked without a
/// gateway.
/// </summary>
public static class EditReading
{
    // Past this, both texts are cut before comparing: the distance is quadratic and the text is
    // whatever anyone typed. A change past the first 400 characters is not one she answered.
    private const int CompareCap = 400;

    // A real change moves a third of the text, and never fewer than 3 characters:
    // « slaut » → « salut » is a typo, « oui » → « non » is not.
    private const double ChangedShare = 0.34;
    private const int MinChanged = 3;

    /// <summary>
    /// What the edit from <paramref name="before"/> to <paramref name="after"/> did, given whether
    /// each version reads as mean (ChatterService's own reading, passed in).
    /// </summary>
    public static EditKind Read(string before, string after, bool wasMean, bool isMean)
    {
        if (!wasMean && isMean) return EditKind.TurnedMean;
        if (wasMean && !isMean) return EditKind.CoveredUp;
        return IsRealChange(before, after) ? EditKind.Rewritten : EditKind.None;
    }

    /// <summary>
    /// Whether the text changed beyond a typo (case, accents and spacing aside). A negation or a
    /// yes or no appearing or going is always real, however short: « je viens pas » is not a typo
    /// of « je viens ».
    /// </summary>
    public static bool IsRealChange(string before, string after)
    {
        var (a, b) = (Normalize(before), Normalize(after));
        if (a == b) return false;
        if (!Flips(a).SetEquals(Flips(b))) return true;
        var distance = Distance(a, b);
        var cut = Math.Max(MinChanged, (int)Math.Ceiling(Math.Max(a.Length, b.Length) * ChangedShare));
        return distance >= cut;
    }

    // The words that turn a message around on their own.
    private static readonly HashSet<string> FlipWords = ["pas", "plus", "jamais", "rien", "personne", "non", "oui", "ouais", "nan"];

    private static HashSet<string> Flips(string normalized) =>
        normalized.Split([' ', ',', '.', '!', '?', ';', ':', '\''], StringSplitOptions.RemoveEmptyEntries)
            .Where(FlipWords.Contains).ToHashSet();

    /// <summary>
    /// Her reply after she noticed: her old words struck through, line by line (a strike doesn't
    /// cross a line break), then her new line. Just the new line when both wouldn't fit in one
    /// message, or when there is nothing of hers to strike.
    /// </summary>
    public static string Struck(string hers, string line)
    {
        var old = string.Join('\n', hers.Replace("~~", "").Split('\n')
            .Select(l => l.Trim()).Where(l => l.Length > 0).Select(l => $"~~{l}~~"));
        var both = old.Length == 0 ? line : $"{old}\n{line}";
        return both.Length <= 2000 ? both : line;
    }

    private static string Normalize(string text)
    {
        var folded = new string(text.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                        != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        var spaced = string.Join(' ', folded.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return spaced.Length <= CompareCap ? spaced : spaced[..CompareCap];
    }

    // Levenshtein, two rows.
    private static int Distance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) previous[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }
}
