using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>What a player asked her in Minecraft's chat. Not stored: free to reorder.</summary>
public enum McOrder
{
    /// <summary>Not for her, or a refusal (« viens pas »): she says nothing.</summary>
    None,
    /// <summary>Called by name with nothing she understands: she says what she can do.</summary>
    Unknown,
    Come,
    Follow,
    Stay,
    /// <summary>Walk to coordinates (<see cref="McRequest.Coordinates"/>).</summary>
    GoTo,
    /// <summary>Follow the player and fight the monsters around them.</summary>
    Guard,
    /// <summary>Gather <see cref="McRequest.Count"/> of <see cref="McRequest.Resource"/> and bring it back.</summary>
    Collect,
    /// <summary>Find a bed and sleep.</summary>
    Sleep,
}

/// <summary>What she can be sent to gather. The body knows each by its lower-case name. Not stored.</summary>
public enum McResource
{
    Wood,
    Stone,
    Coal,
    Iron,
    Copper,
    Gold,
    Diamond,
    Dirt,
    Sand,
}

/// <summary>An order, with what it needs: the coordinates for GoTo (2 numbers: x z; 3: x y z), the resource and count for Collect.</summary>
public readonly record struct McRequest(McOrder Order, int[]? Coordinates = null, McResource? Resource = null, int Count = 0)
{
    public static readonly McRequest Nothing = new(McOrder.None);
}

/// <summary>
/// Reads in-game chat for an order addressed to her. Pure, so the checks walk every phrasing.
/// The words go through <see cref="Rating.Normalize"/> first: lower case, no accents, punctuation
/// collapsed, so « Suis-moi ! » and « suis moi » are the same words. Numbers are read from the raw text,
/// where a minus sign still means something.
/// </summary>
/// <remarks>
/// An order needs her name somewhere in the message (« syncs viens », « viens syncs »): the game
/// chat is shared, and « viens voir » between two players isn't for her. A message that only names
/// her (« syncs est nulle ») is Unknown only when the name opens or closes it, as a call does;
/// anywhere else it's talk about her, and she lets it go.
/// </remarks>
public static class McCommand
{
    public const int DefaultCount = 10;
    public const int MaxCount = 64;

    // Checked in this order: staying wins over everything (« reste ici » has « ici »), and « bouge pas »
    // means stay, so stay is read before the negation that would otherwise cancel it.
    private static readonly string[] StayPhrases =
    {
        "reste la", "reste ici", "reste", "stop", "arrete", "arrete toi", "bouge plus", "bouge pas", "ne bouge pas",
        "attends", "attend", "pause", "halte", "stay", "wait",
    };
    private static readonly string[] SleepPhrases =
    {
        "dodo", "dors", "va dormir", "vas dormir", "dormir", "au lit", "va te coucher", "couche toi", "sleep",
    };
    private static readonly string[] GuardPhrases =
    {
        "protege moi", "protege nous", "protege", "defends moi", "defend moi", "defends nous", "garde du corps",
        "protect me", "protect", "guard",
    };
    private static readonly string[] CollectPhrases =
    {
        "chercher", "cherche", "ramene", "ramene moi", "recolte", "recolter", "mine", "miner", "coupe", "couper",
        "collecte", "recupere", "recuperer", "apporte", "apporte moi", "rapporte", "get", "collect", "farm",
    };
    private static readonly string[] GoToPhrases =
    {
        "va", "vas", "va a", "go", "rends toi", "aller", "allez", "deplace toi", "tp",
    };
    private static readonly string[] FollowPhrases =
    {
        "suis moi", "suis nous", "suivez moi", "suis", "suit moi", "suivre", "follow", "follow me", "accompagne moi",
    };
    private static readonly string[] ComePhrases =
    {
        "viens", "vient", "venir", "viens ici", "ici", "rejoins moi", "rejoins nous", "ramene toi", "come", "come here",
        "par ici", "approche",
    };
    // Words that turn an order into its refusal: « viens pas », « ne me suis pas ».
    private static readonly string[] Negations = { "pas", "jamais", "plus" };

    private static readonly (McResource Resource, string[] Words)[] ResourceWords =
    {
        (McResource.Wood, new[] { "bois", "buche", "buches", "tronc", "troncs", "log", "logs", "wood" }),
        (McResource.Stone, new[] { "pierre", "pierres", "roche", "roches", "cobble", "cobblestone", "stone" }),
        (McResource.Coal, new[] { "charbon", "coal" }),
        (McResource.Iron, new[] { "fer", "iron" }),
        (McResource.Copper, new[] { "cuivre", "copper" }),
        (McResource.Gold, new[] { "or", "gold" }),
        (McResource.Diamond, new[] { "diamant", "diamants", "diamond", "diamonds" }),
        (McResource.Dirt, new[] { "terre", "dirt" }),
        (McResource.Sand, new[] { "sable", "sand" }),
    };

    // A whole number standing alone: « 120 », « -300 », « 64, » — not the digits inside a name (Bob123).
    private static readonly Regex Number = new(@"(?<![\w.])-?\d{1,8}(?![\w.])", RegexOptions.Compiled);

    /// <summary>The order alone, for callers that don't need its details.</summary>
    public static McOrder Read(string text, IReadOnlyCollection<string> names) => Parse(text, names).Order;

    /// <param name="text">The chat message as typed.</param>
    /// <param name="names">Every name she answers to (her in-game name, « syncs »), any case.</param>
    public static McRequest Parse(string text, IReadOnlyCollection<string> names)
    {
        var normalized = Rating.Normalize(text);
        if (normalized.Length == 0) return McRequest.Nothing;
        var words = normalized.Split(' ');

        var nameWords = names.Select(Rating.Normalize).Where(n => n.Length > 0 && !n.Contains(' ')).ToHashSet();
        var at = Array.FindIndex(words, nameWords.Contains);
        if (at < 0) return McRequest.Nothing;
        var rest = words.Where((w, i) => i != at).ToArray();
        var calledOut = at == 0 || at == words.Length - 1;
        var numbers = Number.Matches(text).Select(m => int.Parse(m.Value)).ToArray();
        // Words other than numbers: « syncs 120 64 -300 » is coordinates and nothing else.
        var wordsOnly = rest.Where(w => !w.All(char.IsDigit)).ToArray();

        if (Has(wordsOnly, StayPhrases)) return new(McOrder.Stay);
        var negated = wordsOnly.Any(w => Negations.Contains(w)) || wordsOnly.Contains("ne") || wordsOnly.Contains("n");
        if (negated) return McRequest.Nothing;

        if (Has(wordsOnly, SleepPhrases)) return new(McOrder.Sleep);
        if (Has(wordsOnly, GuardPhrases)) return new(McOrder.Guard);

        var resource = ResourceWords.Where(r => r.Words.Any(wordsOnly.Contains)).Select(r => (McResource?)r.Resource).FirstOrDefault();
        if (resource is { } found && Has(wordsOnly, CollectPhrases))
        {
            var count = numbers.FirstOrDefault(n => n > 0);
            return new(McOrder.Collect, Resource: found, Count: Math.Clamp(count == 0 ? DefaultCount : count, 1, MaxCount));
        }

        if (numbers.Length is 2 or 3 && (wordsOnly.Length == 0 || Has(wordsOnly, GoToPhrases) || Has(wordsOnly, ComePhrases)))
            return new(McOrder.GoTo, Coordinates: numbers);

        if (Has(wordsOnly, FollowPhrases)) return new(McOrder.Follow);
        if (Has(wordsOnly, ComePhrases)) return new(McOrder.Come);
        return calledOut && rest.Length <= 6 ? new(McOrder.Unknown) : McRequest.Nothing;
    }

    // A phrase matches as whole consecutive words, so « suis » never matches inside « suisse », and
    // « je suis là » is not « suis » + an object: a lone « suis » needs to be the message's verb.
    private static bool Has(string[] words, string[] phrases)
    {
        foreach (var phrase in phrases)
        {
            var parts = phrase.Split(' ');
            if (parts is ["suis"] && (words.Contains("je") || words.Contains("j"))) continue;
            for (var i = 0; i + parts.Length <= words.Length; i++)
                if (parts.Select((p, k) => words[i + k] == p).All(x => x)) return true;
        }
        return false;
    }
}
