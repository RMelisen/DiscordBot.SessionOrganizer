using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Who someone asked her to side with, as written: two sides (« qui a raison, moi ou @Luca ? »), or
/// only the asker (<see cref="B"/> null: « qui a raison ? », « dis-lui que j'ai raison »), whose
/// opponent is whoever the message tags or replies to.
/// </summary>
public sealed record SidesAsk(string A, string? B);

/// <summary>
/// « Qui a raison ? » in chat, pure. The sides are pulled out here; ChatterService then picks the one
/// /rate ranks higher (<see cref="Opinion.Prefers"/>), so her favourites win every argument: Papa
/// first, Quokka never. Same guards as <see cref="Opinion"/>: whitespace squeezed, a timeout on every
/// pattern, a side three words at most.
/// </summary>
public static partial class TakeSides
{
    private const int MaxSideTokens = 4;

    // « qui a raison, moi ou Luca ? », « qui a tort entre Luca et Sandra ? », « tranche entre moi et
    // Luca », « départage Luca et Sandra ». « selon toi » / « à ton avis » may close it.
    [GeneratedRegex(@"(?<!\w)(?:(?:c['’ ]?est )?qui (?:qui )?(?:a|à) (?:le plus )?(?:raison|tort)(?: selon toi| à ton avis| a ton avis)?(?: entre)?|tranche entre|d[ée]partage(?:[- ]nous)?(?: entre)?)[\s,:]+(?<a>[^?,;.!:]+?) (?:ou|et) (?<b>[^?,;.!:]+?)(?:[\s,]+(?:selon toi|à ton avis|a ton avis))?[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Opinion.MatchTimeoutMs)]
    private static partial Regex AskedFirstRegex();

    // « moi ou Luca, qui a raison ? ».
    [GeneratedRegex(@"^(?<a>[^?,;.!:]+?) (?:ou|et) (?<b>[^?,;.!:]+?)[\s,:?]+(?:c['’ ]?est )?qui (?:qui )?(?:a|à) (?:le plus )?(?:raison|tort)(?: selon toi| à ton avis| a ton avis)?[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Opinion.MatchTimeoutMs)]
    private static partial Regex SidesFirstRegex();

    // The whole message, with an opponent to find: « qui a raison ? », « à ton avis, qui a raison ? »,
    // « dis-lui que j'ai raison », « dis-lui qu'il a tort », « tranche », « départage-nous ».
    [GeneratedRegex(@"^(?:(?:alors|bon|bref|du coup|franchement|(?:à|a) ton avis|selon toi)[\s,]+)?(?:(?:c['’ ]?est )?qui (?:qui )?(?:a|à) (?:le plus )?(?:raison|tort)(?: ici| des deux| de nous deux| selon toi| à ton avis| a ton avis)?|dis[- ](?:lui|leur|le lui) (?:que j['’ ]?ai raison|qu['’ ]?(?:il|elle|ils|elles) (?:a|ont) tort)|tranche|d[ée]partage[- ]nous)[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Opinion.MatchTimeoutMs)]
    private static partial Regex OpponentRegex();

    // Mentions at either edge: « @Luca qui a raison ? » tags the opponent, who is found from the tags.
    [GeneratedRegex(@"^(?:<@!?\d+>[\s,]*)+|(?:[\s,]*<@!?\d+>)+$", RegexOptions.CultureInvariant, Opinion.MatchTimeoutMs)]
    private static partial Regex EdgeMentionsRegex();

    // Her name as an address at the start: « syncs, qui a raison ? ».
    [GeneratedRegex(@"^(?:(?:dis|hey|eh|oh|yo)[\s,]+)?syncs?[\s,:!]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Opinion.MatchTimeoutMs)]
    private static partial Regex LeadingNameRegex();

    // A side that is the rest of a question: « qui a raison et qui a tort ? » names nobody.
    private static readonly HashSet<string> NotASide = new(StringComparer.OrdinalIgnoreCase)
    {
        "qui", "quoi", "qui a tort", "qui a raison", "personne", "les deux", "nous deux", "tout le monde",
    };

    /// <summary>The sides asked about, or null when the message asks her to take none.</summary>
    public static SidesAsk? Read(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            var text = Opinion.TrimTail(Opinion.Squeeze(content));
            text = LeadingNameRegex().Replace(text, "").Trim();

            foreach (var regex in new[] { AskedFirstRegex(), SidesFirstRegex() })
                if (regex.Match(text) is { Success: true } m && Side(m.Groups["a"].Value) is { } a && Side(m.Groups["b"].Value) is { } b
                    && !string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                    return new SidesAsk(a, b);

            var bare = LeadingNameRegex().Replace(EdgeMentionsRegex().Replace(text, "").Trim(), "").Trim();
            return OpponentRegex().IsMatch(bare) ? new SidesAsk("moi", null) : null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    // A side as written, its articles and her trailing name dropped (Opinion.Clean); null when it is
    // no side: too long, a question word, or nobody.
    private static string? Side(string raw)
    {
        if (Opinion.Clean(raw) is not { } side || NotASide.Contains(side)) return null;
        return Rating.Normalize(side).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= MaxSideTokens ? side : null;
    }
}
