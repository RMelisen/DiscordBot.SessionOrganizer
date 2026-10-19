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
}

/// <summary>
/// Reads in-game chat for an order addressed to her. Pure, so the checks walk every phrasing.
/// The text goes through <see cref="Rating.Normalize"/> first: lower case, no accents, punctuation
/// collapsed, so « Suis-moi ! » and « suis moi » are the same words.
/// </summary>
/// <remarks>
/// An order needs her name somewhere in the message (« syncs viens », « viens syncs »): the game
/// chat is shared, and « viens voir » between two players isn't for her. A message that only names
/// her (« syncs est nulle ») is Unknown only when the name opens or closes it, as a call does;
/// anywhere else it's talk about her, and she lets it go.
/// </remarks>
public static class McCommand
{
    // Checked in this order: staying wins over coming (« reste ici » has « ici »), and « bouge pas »
    // means stay, so stay is read before the negation that would otherwise cancel it.
    private static readonly string[] StayPhrases =
    {
        "reste la", "reste ici", "reste", "stop", "arrete", "arrete toi", "bouge plus", "bouge pas", "ne bouge pas",
        "attends", "attend", "pause", "halte", "stay", "wait",
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

    /// <param name="text">The chat message as typed.</param>
    /// <param name="names">Every name she answers to (her in-game name, « syncs »), any case.</param>
    public static McOrder Read(string text, IReadOnlyCollection<string> names)
    {
        var normalized = Rating.Normalize(text);
        if (normalized.Length == 0) return McOrder.None;
        var words = normalized.Split(' ');

        var nameWords = names.Select(Rating.Normalize).Where(n => n.Length > 0 && !n.Contains(' ')).ToHashSet();
        var at = Array.FindIndex(words, nameWords.Contains);
        if (at < 0) return McOrder.None;
        var rest = words.Where((w, i) => i != at).ToArray();
        var calledOut = at == 0 || at == words.Length - 1;

        if (Has(rest, StayPhrases)) return McOrder.Stay;
        var negated = rest.Any(w => Negations.Contains(w)) || rest.Contains("ne") || rest.Contains("n");
        if (Has(rest, FollowPhrases)) return negated ? McOrder.None : McOrder.Follow;
        if (Has(rest, ComePhrases)) return negated ? McOrder.None : McOrder.Come;
        return calledOut && rest.Length <= 6 ? McOrder.Unknown : McOrder.None;
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
