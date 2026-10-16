using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>What kind of ship was asked in chat.</summary>
public enum ShipAskKind
{
    /// <summary>Two sides, as written (« ship X et Y »).</summary>
    Pair,
    /// <summary>The asker and whoever « on » / « nous » is (« on irait bien ensemble ? »): the person
    /// tagged, else the one replied to, else her — ChatterService decides, it sees the message.</summary>
    Us,
    /// <summary>The asker's best match on the server (« c'est qui mon âme sœur ? »).</summary>
    BestMatch,
    /// <summary>The asker's worst match on the server (« c'est qui mon pire match ? »).</summary>
    WorstMatch,
}

/// <summary>A ship asked in chat. <see cref="A"/> and <see cref="B"/> are the sides as written for a
/// <see cref="ShipAskKind.Pair"/>; the other kinds set only <see cref="A"/> (« moi »).</summary>
public sealed record ShipPair(string A, string B, ShipAskKind Kind = ShipAskKind.Pair);

/// <summary>
/// /ship asked in chat, pure: « ship X et Y », « tu me ship avec X ? », « X et Y, ça match ? »,
/// « on irait bien ensemble ? », « c'est qui mon âme sœur ? ». ChatterService then gives /ship's
/// verdict (ShipModule.Render), so the chat and the command agree. « moi » is the asker and « toi »
/// is her, exactly as /ship reads them (Rating).
/// </summary>
public static partial class ShipAsk
{
    // Past this, a side is a sentence rather than a name: « je pense que Luca et Sandra ça match »
    // is not « je pense que Luca » × « Sandra ».
    public const int MaxSideWords = 3;

    // The « on » forms are short: « on irait bien ensemble ? », not a paragraph that contains it.
    public const int MaxUsWords = 8;

    // A side holding a subject pronoun is a clause, not a name: « toi et moi on joue ensemble ? »
    // would otherwise ship « toi » × « moi on joue ».
    private static readonly HashSet<string> Pronouns = new(StringComparer.OrdinalIgnoreCase)
    {
        "je", "j", "tu", "t", "il", "elle", "on", "nous", "vous", "ils", "elles", "c", "ce", "ca", "ça", "que", "qu",
    };

    // Her opinion of two sides at once: « tu penses quoi de @A et @B ? », « @A et @B, t'en penses
    // quoi ? ». A ship only when the sides are people (OpinionPairIsShip): both tagged, or « moi » /
    // « toi » on one side, or « ensemble » / « en couple » after them. « t'en penses quoi de la pizza
    // et des pâtes ? » stays an opinion.
    [GeneratedRegex(@"(?<!\w)(?:t['’ ]?\s*en\s+penses\s+quoi|tu\s+en\s+penses\s+quoi|tu\s+penses\s+quoi|que\s+penses[-\s]tu|qu['’ ]?\s*est[-\s]ce\s+que\s+tu\s+penses|ton\s+avis\s+sur|tu\s+dis\s+quoi)\s+(?:(?:de|du|des|sur)\s+|d['’]\s*)?(?<a>.+?)\s+(?:et|avec|x|&|\+)\s+(?<b>.+?)(?<together>[\s,]+(?:ensemble|en\s+couple|comme\s+couple))?[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OpinionAboutPairRegex();

    [GeneratedRegex(@"^(?<a>.+?)\s+(?:et|avec|x|&|\+)\s+(?<b>.+?)(?<together>[\s,]+(?:ensemble|en\s+couple|comme\s+couple))?[\s,:]+(?:t['’ ]?\s*en\s+penses\s+quoi|tu\s+en\s+penses\s+quoi|qu['’ ]?\s*en\s+penses[-\s]tu|ton\s+avis)[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OpinionBeforePairRegex();

    [GeneratedRegex(@"^(?:<@!?\d+>\s*)+$", RegexOptions.CultureInvariant)]
    private static partial Regex OnlyMentionsRegex();

    // « ship X et Y », « tu peux shipper X avec Y ? », « le ship entre X et Y », « shippe X x Y ».
    [GeneratedRegex(@"(?<!\w)ship\w*\s+(?:(?:entre|de)\s+)?(?<a>.+?)\s+(?:et|avec|x|&|\+)\s+(?<b>.+?)[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ShipRegex();

    // « compatibilité entre X et Y », « love calculator X et Y », « test d'amour X et Y ».
    [GeneratedRegex(@"(?<!\w)(?:compatibilit[ée]|compat|love\s+(?:calculator|calc|test)|test\s+d['’ ]?\s*amour|taux\s+d['’ ]?\s*amour)\s+(?:(?:entre|de)\s+)?(?<a>.+?)\s+(?:et|avec|x|&|\+)\s+(?<b>.+?)[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CompatibilityRegex();

    // « tu me ship avec X », « shippe-moi avec X », « tu me vois avec X ? », « j'irais bien avec X ? »,
    // « je suis compatible avec X ? ».
    [GeneratedRegex(@"(?<!\w)(?:me\s+ship\w*|ship\w*[-\s]moi|tu\s+me\s+(?:vois|verrais)|j['’ ]?\s*irais\s+bien|je\s+vais\s+bien|je\s+suis\s+compatible)\s+avec\s+(?<b>.+?)[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WithMeRegex();

    // « X et Y, ça match ? », « Luca et moi on est compatibles ? », « toi et moi on irait bien
    // ensemble ? », « X et Y, ça ferait un beau couple ? », « X et Y en couple ? ». Plural
    // « compatibles » only, and no bare « ensemble », « ça marche » or « ça colle »: « la manette et le
    // PC c'est compatible ? », « vendredi et samedi ensemble ? » and « samedi et dimanche, ça marche ? »
    // are about hardware and dates.
    [GeneratedRegex(@"^(?<a>.+?)\s+(?:et|avec|x|&|\+)\s+(?<b>.+?)[\s,:]+(?:(?:on|ils|elles|vous)\s+(?:(?:est|sont|serai(?:en)?t|seront|sera|[êe]tes|serions)\s+)?)?(?:[çcs]a\s+)?(?:match\w*|compatibles|(?:vont|iraient|irait|irions|allons|va|allez)\s+(?:bien\s+)?ensemble|(?:ferai(?:en)?t|ferions|feraient|font)\s+un\s+(?:beau|bon|joli)\s+couple|un\s+(?:beau|bon|joli)\s+couple|faits?\s+l['’ ]?\s*un\s+pour\s+l['’ ]?\s*autre|en\s+couple)[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CompatRegex();

    // « Luca x Sandra ? », the shipping notation, as the whole message with its « ? ». Neither side
    // starts with a digit (« 2 x 3 ? » is arithmetic).
    [GeneratedRegex(@"^(?<a>[^\d\s?][^?]*?)\s+x\s+(?<b>[^\d\s?][^?]*?)\s*\?+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NotationRegex();

    // « le couple X et Y », « couple X / Y ? »: only after « le » or opening the message (« un couple
    // de lapins et de chats » counts nothing).
    [GeneratedRegex(@"(?:^|(?<!\w)le\s+)couple\s+(?<a>.+?)\s+(?:et|x|&|\+|/)\s+(?<b>.+?)[\s?!.]*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CoupleRegex();

    // « Luca ❤️ Sandra ? », « Luca <3 Sandra ? », as the whole message and asked: « gg ❤️ bien joué »
    // is a compliment.
    [GeneratedRegex(@"^(?<a>.+?)\s*(?:❤️|❤|💕|💘|💞|💖|<3)\s*(?<b>.+?)\s*\?+$", RegexOptions.CultureInvariant)]
    private static partial Regex HeartRegex();

    // Two people tagged with a ship word and no « et » between them: « ship @Luca @Sandra »,
    // « @Luca @Sandra compatibles ? ». The last two distinct mentions are the sides (her own tag, on
    // the mention path, comes first).
    [GeneratedRegex(@"(?<!\w)(?:ship\w*|compatibles|beau\s+couple|love\s*calc\w*)(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ShipWordRegex();

    [GeneratedRegex(@"<@!?\d+>", RegexOptions.CultureInvariant)]
    private static partial Regex MentionRegex();

    // The asker and someone unnamed: « on irait bien ensemble ? », « tu nous ship ? ». Always with
    // « bien », « compatibles » or « couple »: « on est ensemble ce soir ? » is about playing.
    [GeneratedRegex(@"(?<!\w)(?:on\s+(?:irait|ira|va|serait)\s+bien\s+ensemble|on\s+(?:est|serait|sera)\s+(?:compatibles?|faits?\s+l['’ ]?\s*un\s+pour\s+l['’ ]?\s*autre)|on\s+(?:ferait|fait|formerait)\s+un\s+(?:beau|bon|joli)\s+couple|tu\s+nous\s+(?:ship\w*|vois\s+ensemble|verrais\s+ensemble)|ship\w*[-\s]nous|notre\s+compatibilit[ée]|on\s+(?:va\s+finir|finira|finirait)\s+ensemble|tu\s+nous\s+(?:vois|verrais)\s+en\s+couple|on\s+(?:est|serait|ferait)\s+(?:un\s+)?(?:beau|bon|joli)\s+couple|on\s+(?:est|serait)\s+faits?\s+pour\s+[êe]tre\s+ensemble)(?!\w)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UsRegex();

    // « c'est qui mon âme sœur ? », « avec qui tu me ship ? », « trouve-moi l'âme sœur », « mon ship
    // idéal ». Never « match » or « partenaire » alone: on a gaming server « mon meilleur match » is a
    // game, and « trouve-moi un partenaire » is a teammate.
    [GeneratedRegex(@"(?<!\w)(?:(?:mon|ma)\s+[âa]me\s+s(?:œ|oe)ur|avec\s+qui\s+tu\s+me\s+(?:ship\w*|vois|verrais|mettrais|caserais)|avec\s+qui\s+(?:je\s+(?:vais|irais)\s+le\s+mieux|je\s+suis\s+le\s+plus\s+compatible)|trouve[sz]?[-\s]moi\s+(?:l['’ ]?\s*[âa]me\s+s(?:œ|oe)ur|un\s+(?:mec|copain|amoureux)|une\s+(?:copine|amoureuse))|qui\s+est\s+fait\w*\s+pour\s+moi|(?:la\s+)?personne\s+faite\s+pour\s+moi|qui\s+(?:irait|ira)\s+bien\s+avec\s+moi|(?:vais\s+finir|finis|finirai|finirais)\s+avec\s+qui[\s?!.]*$|avec\s+qui\s+je\s+(?:devrais|vais|pourrais)\s+(?:sortir|finir|me\s+marier|me\s+caser)[\s?!.]*$|qui\s+va\s+m['’ ]?\s*[ée]pouser|tu\s+me\s+(?:mets|cases|mettrais|caserais)\s+avec\s+qui|(?:mon|ma)\s+futur\w*\s+(?:mari|femme|copain|copine|[ée]poux|[ée]pouse)|(?:mon|ma)\s+(?:meilleur\w*\s+ship|ship\s+(?:parfait|id[ée]al)))(?!\w)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BestMatchRegex();

    // « c'est qui mon pire ship ? », « avec qui je suis le moins compatible ? ».
    [GeneratedRegex(@"(?<!\w)(?:(?:mon|ma)\s+pire\s+ship|avec\s+qui\s+(?:je\s+suis\s+le\s+moins\s+compatible|je\s+(?:vais|irais)\s+le\s+moins\s+bien)|(?:mon|ma)\s+anti[-\s]?[âa]me\s+s(?:œ|oe)ur)(?!\w)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WorstMatchRegex();

    // Her name, and what only frames the question, opening the message: « syncs, est-ce que Luca et
    // moi ça match ? », « à ton avis X et Y, compatibles ? ».
    [GeneratedRegex(@"^(?:<@!?\d+>[\s,:]*)*(?:syncs?(?!\w)[\s,:]*)?(?:(?:est[-\s]ce\s+que|est[-\s]ce\s+qu['’]|tu\s+penses\s+que|tu\s+crois\s+que|[àa]\s+ton\s+avis|selon\s+toi|franchement|dis[-\s]moi\s+si|dis)[\s,:]*)*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LeadingRegex();

    /// <summary>The ship asked about, or null when the message asks for none.</summary>
    public static ShipPair? Read(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var text = Opinion.TrimTail(content.Replace('\n', ' ').Trim());
        if (text.Length == 0) return null;

        // The server-wide searches first: « avec qui tu me ship ? » is not « me ship avec qui ».
        if (WorstMatchRegex().IsMatch(text)) return new ShipPair("moi", string.Empty, ShipAskKind.WorstMatch);
        if (BestMatchRegex().IsMatch(text)) return new ShipPair("moi", string.Empty, ShipAskKind.BestMatch);

        if (ShipRegex().Match(text) is { Success: true } ship && Pair(ship) is { } shipped) return shipped;
        if (CompatibilityRegex().Match(text) is { Success: true } compatibility && Pair(compatibility) is { } measured)
            return measured;

        if (WithMeRegex().Match(text) is { Success: true } withMe && Side(withMe.Groups["b"].Value) is { } partner)
            return new ShipPair("moi", partner);

        if (OpinionAboutPairRegex().Match(text) is { Success: true } about && Pair(about) is { } aboutPair
            && OpinionPairIsShip(about, aboutPair))
            return aboutPair;

        // The anchored forms, on the text without its framing first (« syncs, est-ce que Luca et moi ça
        // match ? »), then as written: the framing strip also takes leading tags, which may be a side
        // (« @A et @B ça match ? »).
        var unframed = LeadingRegex().Replace(text, "");
        foreach (var candidate in unframed == text ? new[] { text } : new[] { unframed, text })
        {
            if (CoupleRegex().Match(candidate) is { Success: true } couple && Pair(couple) is { } coupled) return coupled;
            if (CompatRegex().Match(candidate) is { Success: true } compat && Pair(compat) is { } matched) return matched;
            if (NotationRegex().Match(candidate) is { Success: true } notation && Pair(notation) is { } noted) return noted;
            if (HeartRegex().Match(candidate) is { Success: true } heart && Pair(heart) is { } hearted) return hearted;
            if (OpinionBeforePairRegex().Match(candidate) is { Success: true } before && Pair(before) is { } beforePair
                && OpinionPairIsShip(before, beforePair))
                return beforePair;
        }

        if (ShipWordRegex().IsMatch(text))
        {
            var tagged = MentionRegex().Matches(text).Select(m => m.Value).Distinct().ToList();
            if (tagged.Count >= 2) return new ShipPair(tagged[^2], tagged[^1]);
        }

        if (CountWords(text) <= MaxUsWords && UsRegex().IsMatch(text)) return new ShipPair("moi", string.Empty, ShipAskKind.Us);

        return null;
    }

    private static ShipPair? Pair(Match m) =>
        Side(m.Groups["a"].Value) is { } a && Side(m.Groups["b"].Value) is { } b ? new ShipPair(a, b) : null;

    // An opinion asked about two sides is a ship when they are people: « ensemble » after them, or one
    // side surely a person (tagged, « moi », « toi ») and the other person-like (that, or a capitalised
    // name). « tu penses quoi de moi et de mes cheveux » and « t'en penses quoi de Minecraft et
    // Fortnite ? » stay opinions.
    private static bool OpinionPairIsShip(Match m, ShipPair pair)
    {
        if (m.Groups["together"].Success) return true;

        static bool IsSurelyPerson(string side) =>
            OnlyMentionsRegex().IsMatch(side)
            || side.Equals("moi", StringComparison.OrdinalIgnoreCase) || side.Equals("toi", StringComparison.OrdinalIgnoreCase);
        static bool IsPersonLike(string side) =>
            IsSurelyPerson(side) || (char.IsUpper(side[0]) && side.Split(' ').Length <= 2);

        return (IsSurelyPerson(pair.A) && IsPersonLike(pair.B)) || (IsSurelyPerson(pair.B) && IsPersonLike(pair.A));
    }

    private static string? Side(string raw)
    {
        // Tags only (« <@999> <@1> », her own tag then a side): the last one is the side.
        var trimmed = raw.Trim().TrimEnd(',', ':', '?', '!', '.').Trim();
        if (OnlyMentionsRegex().IsMatch(trimmed))
            return MentionRegex().Matches(trimmed)[^1].Value;

        // Opinion.Clean keeps « moi » and « toi », which are sides here.
        var side = Opinion.Clean(raw);
        if (side is null) return null;

        var words = side.Split(new[] { ' ', '\'', '’' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > MaxSideWords || words.Any(Pronouns.Contains)) return null;
        return side;
    }

    private static int CountWords(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
