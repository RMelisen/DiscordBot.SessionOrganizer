using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// What someone asked her opinion of, as written: one subject (« t'en penses quoi de X ? ») or two
/// sides (« tu préfères X ou Y ? », <see cref="Other"/> set).
/// </summary>
public sealed record OpinionAsk(string Subject, string? Other);

/// <summary>
/// Her opinion in chat, pure. The subject is pulled out of the message here; ChatterService then
/// gives /rate's verdict on it (<see cref="Rating.Rate"/>), so she says the same thing about the
/// same thing whether asked by command or in chat, and a preference goes to the side /rate scores
/// higher (<see cref="Prefers"/>).
/// </summary>
public static partial class Opinion
{
    // The subject's cap: past it, it isn't a subject but a paragraph.
    public const int MaxSubjectLength = 80;

    // Every pattern here and in ShipAsk runs on chat text anyone can send, on the gateway task. A
    // real question matches in microseconds; a match still running at this point is crafted input
    // backtracking, and reads as no question at all (Read catches the timeout).
    internal const int MatchTimeoutMs = 250;

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant, MatchTimeoutMs)]
    private static partial Regex WhitespaceRegex();

    /// <summary>
    /// The text on one line, every run of whitespace (Unicode spaces included) down to one space. The
    /// patterns allow « \s+ » next to « .+? » and « [\s,]+ »; a long run gives each of them a share to
    /// try, which is how a single message could keep a match busy for minutes.
    /// </summary>
    internal static string Squeeze(string text) => WhitespaceRegex().Replace(text, " ").Trim();

    // « tu préfères X ou Y », « plutôt X ou Y ».
    [GeneratedRegex(@"(?<!\w)(?:tu pr[ée]f[èée]res|t['’ ]?pr[ée]f[èée]res|tu choisirais|tu choisis|plut[ôo]t)\s+(?<a>.+?)\s+ou\s+(?<b>.+?)[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeoutMs)]
    private static partial Regex PreferRegex();

    // « t'en penses quoi de X », « que penses-tu de X », « ton avis sur X », « tu aimes X ». « penses? »:
    // « tu pense quoi » is how it's often typed.
    [GeneratedRegex(@"(?<!\w)(?:t['’ ]?en penses? quoi|tu en penses? quoi|tu penses? quoi|que penses?[- ]tu|qu['’ ]?est[- ]ce que tu penses?|ton avis sur|t['’ ]?as un avis sur|tu as un avis sur|tu aimes|t['’ ]?aimes|tu kiffes|t['’ ]?kiffes)\s+(?<rest>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeoutMs)]
    private static partial Regex AboutRegex();

    // « X, t'en penses quoi ? », « X : ton avis ? ».
    [GeneratedRegex(@"^(?<rest>.+?)[\s,:]+(?:t['’ ]?en penses? quoi|tu en penses? quoi|qu['’ ]?en penses?[- ]tu|ton avis)[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeoutMs)]
    private static partial Regex BeforeRegex();

    // Leading words that aren't the subject: « de la pizza » is « pizza », as /rate would be typed.
    [GeneratedRegex(@"^(?:(?:de|du|des|sur|le|la|les|un|une|bien|vraiment)\s+|[dl]['’]\s*)+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeoutMs)]
    private static partial Regex LeadingRegex();

    // Her name tacked on the end (« …de la pizza, syncs ? ») and the closing punctuation.
    [GeneratedRegex(@"[\s,]*(?:(?<!\w)syncs?)?[\s?!.,]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeoutMs)]
    private static partial Regex TrailingRegex();

    // A mention at either edge: « @SYNCS pizza, t'en penses quoi ? » tags her, it doesn't rate her.
    // Dropped only when something is left — « t'en penses quoi de @Luca ? » is about Luca.
    [GeneratedRegex(@"^(?:<@!?\d+>[\s,]*)+|(?:[\s,]*<@!?\d+>)+$", RegexOptions.CultureInvariant, MatchTimeoutMs)]
    private static partial Regex EdgeMentionRegex();

    // A « subject » that is really the rest of a question: « tu penses quoi de ça ? » has none.
    private static readonly HashSet<string> NotASubject = new(StringComparer.OrdinalIgnoreCase)
    {
        "quoi", "qui", "ca", "ça", "ce", "cela", "tout", "rien", "ce que", "ce qui", "quoi de neuf",
    };

    // Nor is one that opens on a question word: « tu aimes qui comme perso ? » asks, it names nothing.
    [GeneratedRegex(@"^(?:qui|quoi|quel|quelle|quels|quelles|lequel|laquelle|lesquels|lesquelles)\s",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex QuestionWordRegex();

    // « tu préfères quel jeu, Minecraft ou Fortnite ? »: the question before the comma isn't a side.
    [GeneratedRegex(@"^(?:qui|quoi|quel|quelle|quels|quelles|lequel|laquelle|lesquels|lesquelles)\b[^,:]*[,:]\s*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PreferQuestionRegex();

    // What joins two sides; never a subject on its own.
    private static readonly HashSet<string> Joiners = new(StringComparer.OrdinalIgnoreCase)
    {
        "et", "ou", "avec", "x", "&", "+", "/", "vs", "contre",
    };

    // What may trail a question without being part of it: emoji, custom emotes, and her name as an
    // address (« …ça match syncs ? 🙂 »). The patterns here and in ShipAsk are anchored to the end.
    [GeneratedRegex(@"(?:[\s\p{So}\p{Sk}\p{Cs}‍️]|<a?:\w+:\d+>)+$", RegexOptions.CultureInvariant, MatchTimeoutMs)]
    private static partial Regex TrailingDecorRegex();

    [GeneratedRegex(@"[\s,]+syncs?(?![\w.])(?=[\s?!.…]*$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeoutMs)]
    private static partial Regex TrailingNameRegex();

    /// <summary>The message without its trailing emoji, emotes and closing « syncs ».</summary>
    internal static string TrimTail(string text) =>
        TrailingNameRegex().Replace(TrailingDecorRegex().Replace(text, ""), "").Trim();

    /// <summary>The subject or the two sides asked about, or null when the message asks no opinion.</summary>
    public static OpinionAsk? Read(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            return ReadSqueezed(TrimTail(Squeeze(content)));
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    private static OpinionAsk? ReadSqueezed(string text)
    {
        if (PreferRegex().Match(text) is { Success: true } prefer
            && Clean(PreferQuestionRegex().Replace(prefer.Groups["a"].Value.Trim(), "")) is { } a
            && Clean(prefer.Groups["b"].Value) is { } b)
            return new OpinionAsk(a, b);

        var about = AboutRegex().Match(text);
        if (about.Success && Clean(about.Groups["rest"].Value) is { } subject) return new OpinionAsk(subject, null);

        var before = BeforeRegex().Match(text);
        if (before.Success && Clean(before.Groups["rest"].Value) is { } leading) return new OpinionAsk(leading, null);

        return null;
    }

    /// <summary>
    /// Whether she prefers <paramref name="a"/> to <paramref name="b"/>: the fixed verdicts rank
    /// first (herself, Papa, then the ones she adores), the nemesis and the devil last, ordinary
    /// things by their /rate score. A tie is settled by the pair's hash, the same whichever side
    /// comes first.
    /// </summary>
    public static bool Prefers(RatingResult a, RatingResult b, string keyA, string keyB)
    {
        var (ra, rb) = (Rank(a), Rank(b));
        if (ra != rb) return ra > rb;

        var (first, second) = string.CompareOrdinal(keyA, keyB) <= 0 ? (keyA, keyB) : (keyB, keyA);
        var firstWins = (Rating.Hash(first + "|" + second) & 1) == 0;
        return firstWins == (first == keyA);
    }

    private static int Rank(RatingResult r) => r.Kind switch
    {
        RatingKind.Herself => 13,
        RatingKind.Owner => 12,
        RatingKind.God or RatingKind.Crush or RatingKind.Tata or RatingKind.Routier => 11,
        RatingKind.Mitraillette => 0,
        RatingKind.Nemesis => -5,
        RatingKind.Devil => -10,
        _ => r.Score,
    };

    // A side as /rate would be typed: articles, her trailing name, punctuation and an edge mention
    // dropped. Null when nothing usable is left. Shared with ShipAsk.
    internal static string? Clean(string raw)
    {
        var s = TrailingRegex().Replace(LeadingRegex().Replace(raw.Trim(), ""), "").Trim();
        var unmentioned = EdgeMentionRegex().Replace(s, "").Trim();
        // Only when what's left is a subject: « <@1> et <@2> » minus both edges is a bare « et ».
        if (unmentioned.Length > 0 && !Joiners.Contains(unmentioned) && !NotASubject.Contains(unmentioned))
            s = TrailingRegex().Replace(LeadingRegex().Replace(unmentioned, ""), "").Trim();
        if (s.Length == 0 || s.Length > MaxSubjectLength || NotASubject.Contains(s) || QuestionWordRegex().IsMatch(s)) return null;
        return s;
    }
}
