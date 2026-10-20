using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>What a player asked her in Minecraft's chat. Not stored: free to reorder.</summary>
public enum McOrder
{
    /// <summary>Not for her, or a refusal (« viens pas »): she says nothing.</summary>
    None,
    /// <summary>Called by name with nothing she understands: she answers « oui ? » and hears the player's next message.</summary>
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
    /// <summary>Harvest the ripe <see cref="McRequest.Crop"/> around her, replant, and bring it back.</summary>
    Harvest,
    /// <summary>Stay where she is and keep the farm: harvest what ripens, replant, store it in the chest.</summary>
    Farm,
}

/// <summary>What she can be sent to harvest. The body knows each by its lower-case name. Not stored.</summary>
public enum McCrop
{
    /// <summary>Whatever is ripe.</summary>
    Any,
    Wheat,
    Carrot,
    Potato,
    Beetroot,
    Melon,
    Pumpkin,
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

/// <summary>An order, with what it needs: the coordinates for GoTo (2 numbers: x z; 3: x y z), the resource and count
/// for Collect, the crop for Harvest.</summary>
public readonly record struct McRequest(McOrder Order, int[]? Coordinates = null, McResource? Resource = null, int Count = 0, McCrop? Crop = null)
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
/// <para>An order needs her name somewhere in the message (« syncs viens », « viens syncs »): the game
/// chat is shared, and « viens voir » between two players isn't for her. A message that only names
/// her (« syncs est nulle ») is Unknown only when the name opens or closes it, as a call does;
/// anywhere else it's talk about her, and she lets it go. After such a call, the same player's next
/// message is read with <c>called</c> set: as if it carried her name (« syncs » … « suis moi »).</para>
/// <para>A refusal is read around the verb only: « ne » just before it, « pas » or « jamais » just
/// after (« viens pas », « ne me suis pas », « ne va pas chercher »). A « pas » elsewhere in the
/// message doesn't cancel anything (« c'est pas grave, syncs viens »), and « plus de bois » is more
/// wood, not a refusal.</para>
/// <para>The lists are written the way people type in game chat: familiar French (« ramène ta fraise »,
/// « fais du bois »), common shortcuts (« vien », « diams »), a few English words. Add the misses
/// found in the wild, with a case in the checks.</para>
/// </remarks>
public static class McCommand
{
    public const int DefaultCount = 10;
    public const int MaxCount = 64;

    /// <summary>
    /// The names she answers to besides her in-game name: « sync » too, the missing s being the most
    /// common slip when typing her name in a hurry.
    /// </summary>
    public static readonly string[] HerNames = { "syncs", "sync" };

    // Staying with someone is following, and must be read before « reste », which alone means stay.
    private static readonly string[] FollowOverStayPhrases =
    {
        "reste avec moi", "reste avec nous", "reste pres de moi", "reste pres de nous", "reste a cote de moi",
        "reste derriere moi", "reste collee", "reste colle", "rester avec moi", "rester avec nous", "rester pres de moi",
    };
    // Checked before every other order: staying wins (« reste ici » has « ici »), and « bouge pas » means
    // stay, so stay is read before the refusal that would otherwise cancel it.
    private static readonly string[] StayPhrases =
    {
        "reste la", "reste ici", "reste", "restes", "stop", "stoppe", "arrete", "arretes", "arrete toi", "arrete tout",
        "bouge plus", "bouge pas", "ne bouge pas", "bouge pu", "pas bouger", "attends", "attend", "attends moi la",
        "pause", "halte", "fige toi", "freeze", "annule", "laisse tomber", "ca suffit", "c est bon arrete",
        "plus bouger", "sur place", "rester la", "rester ici", "rester", "t arreter", "arreter", "attendre",
        "ne plus bouger", "patiente", "stay", "wait", "hold", "cancel",
    };
    private static readonly string[] SleepPhrases =
    {
        "dodo", "au dodo", "fais dodo", "dors", "va dormir", "vas dormir", "dormir", "au lit", "va au lit",
        "va te coucher", "couche toi", "va te pieuter", "pionce", "roupille", "sieste", "aller dormir", "te coucher",
        "aller te coucher", "aller au lit", "sleep", "go sleep",
    };
    private static readonly string[] GuardPhrases =
    {
        "protege moi", "protege nous", "protege", "protection", "defends moi", "defend moi", "defends nous", "defends",
        "garde du corps", "garde moi", "monte la garde", "couvre moi", "couvre nous", "surveille moi", "escorte moi",
        "escorte", "au secours", "a l aide", "aide moi", "sauve moi", "bats toi", "tue les monstres", "tue les mobs",
        "tue le", "tue la", "tape le", "tape les", "attaque", "attaque le", "attaque les", "combat", "defense",
        "proteger", "me proteger", "nous proteger", "defendre", "me defendre", "me couvrir", "garder", "me garder",
        "tuer les monstres", "me sauver", "protect me", "protect", "guard", "help me", "fight", "kill",
    };
    private static readonly string[] CollectPhrases =
    {
        "chercher", "cherche", "cherches", "va chercher", "va me chercher", "ramene", "ramene moi", "ramenes",
        "recolte", "recolter", "mine", "miner", "mines", "coupe", "couper", "coupes", "abat", "abats", "abattre",
        "casse", "casser", "collecte", "collecter", "recupere", "recuperer", "ramasse", "ramasser", "trouve",
        "trouve moi", "trouver", "apporte", "apporte moi", "rapporte", "rapporte moi", "donne moi", "file moi",
        "chope", "chope moi", "fais du", "fais de la", "fais des", "faire du", "faire de la", "il me faut",
        "j ai besoin de", "j ai besoin d", "besoin de", "besoin d", "get", "collect", "farm", "gather", "chop",
        "aller chercher", "me ramener", "ramener", "m apporter", "apporter", "rapporter", "me trouver", "me donner",
        "dig", "bring", "fetch",
    };
    // Keeping a farm, read before stay (« reste à la ferme ») and guard (« garde le champ »). Either a phrase that
    // says it alone, or a care verb with a farm word a few words after it (« prends soin de mes plantations »).
    private static readonly string[] FarmAlonePhrases =
    {
        "cultive", "cultiver", "cultives", "jardine", "jardiner", "fermiere", "fais la fermiere", "joue a la fermiere",
        "mode ferme", "mode fermiere", "farm afk", "afk farm", "farm here", "keep the farm", "tend the farm",
    };
    // Care verbs that only mean farming next to a farm word: « garde le champ » is a farm, « garde moi » is not,
    // and « il reste du blé » is not a place to stay. The first ones (CropCareVerbs) take a crop too
    // (« occupe-toi du blé »); « garde des carottes » keeps some, it doesn't farm them.
    private static readonly string[] CropCareVerbs =
    {
        "occupe toi", "occupes toi", "occupe", "t occuper", "s occuper", "prends soin", "prend soin", "prendre soin",
        "entretiens", "entretient", "entretenir", "soigne", "soigner", "take care of", "tend",
    };
    private static readonly string[] FarmVerbs = CropCareVerbs.Concat(new[]
    {
        "gere", "geres", "gerer", "garde", "gardes", "garder", "surveille", "surveilles", "surveiller", "veille sur",
        "reste", "restes", "rester", "bosse", "travaille", "travailler", "keep", "watch", "guard", "stay at", "stay in",
        "work",
    }).ToArray();
    private static readonly string[] FarmWords =
    {
        "ferme", "fermes", "champ", "champs", "culture", "cultures", "potager", "potagers", "jardin", "plantation",
        "plantations", "recolte", "recoltes", "legumes", "farm", "field", "fields", "crops", "garden",
    };
    // Harvesting without naming a crop (« syncs récolte »).
    private static readonly string[] HarvestPhrases =
    {
        "recolte", "recolter", "recoltes", "recup la recolte", "fais la recolte", "faire la recolte", "fais les recoltes",
        "fais le champ", "fais les champs", "vide le champ", "vide les champs", "moissonne", "moissonner", "moissonnes",
        "cueille", "cueillir", "fauche", "faucher", "replante", "replanter", "replantes", "harvest",
    };
    // With a crop word, any gathering verb harvests, and so do these, which mean nothing without one
    // (« arrache les carottes », « farm le blé »; « arrache-toi » is not an order).
    private static readonly string[] CropOnlyVerbs =
    {
        "arrache", "arracher", "deterre", "deterrer", "prends", "prend", "prendre", "farme", "farmer", "recup",
    };
    private static readonly string[] CropVerbs = CollectPhrases.Concat(HarvestPhrases).Concat(CropOnlyVerbs).Distinct().ToArray();

    // Exact, or one slip on words of six letters or more (Same): « citrouile », « betteraves ». Misspellings
    // further than that are listed (« carrote », « betrave »).
    private static readonly (McCrop Crop, string[] Words)[] CropWords =
    {
        (McCrop.Wheat, new[] { "ble", "bles", "epi", "epis", "wheat" }),
        (McCrop.Carrot, new[] { "carotte", "carottes", "carrote", "carrotes", "carot", "carots", "carrot", "carrots" }),
        (McCrop.Potato, new[] { "patate", "patates", "patatte", "patattes", "potato", "potatoes" }),
        (McCrop.Beetroot, new[] { "betterave", "betteraves", "betrave", "betraves", "beetroot", "beetroots" }),
        (McCrop.Melon, new[] { "melon", "melons", "pasteque", "pasteques", "watermelon" }),
        (McCrop.Pumpkin, new[] { "citrouille", "citrouilles", "potiron", "potirons", "pumpkin", "pumpkins" }),
        // Generic: harvest whatever is ripe. Never against a resource (« du bois dans le champ » is wood).
        (McCrop.Any, new[] { "champ", "champs", "cultures", "plantations", "legumes", "potager", "recoltes", "crops" }),
    };

    private static readonly string[] GoToPhrases =
    {
        "va", "vas", "va a", "va en", "go", "go to", "goto", "rends toi", "aller", "allez", "deplace toi", "tp",
        "teleporte toi", "direction", "file", "file a", "cours", "dirige toi", "rendez vous", "rdv", "marche",
        "marche jusqu", "jusqu a", "aller a", "aller en", "te rendre", "te deplacer", "aller jusqu",
    };
    private static readonly string[] FollowPhrases =
    {
        "suis moi", "suis nous", "suivez moi", "suis", "suit moi", "suis moi stp", "suivre", "suit nous",
        "accompagne moi", "accompagne nous", "viens avec moi", "viens avec nous", "colle moi", "dans mes pas",
        "derriere moi", "a mes cotes", "me suivre", "nous suivre", "m accompagner", "venir avec moi", "follow",
        "follow me",
    };
    private static readonly string[] ComePhrases =
    {
        "viens", "vien", "vient", "venir", "vi1", "viens ici", "viens la", "viens voir", "viens me voir",
        "viens vers moi", "ici", "par ici", "approche", "rapproche toi", "rejoins moi", "rejoint moi", "rejoins nous",
        "rejoint nous", "ramene toi", "ramene ta fraise", "rapplique", "radine toi", "amene toi", "pointe toi",
        "au pied", "venir ici", "me rejoindre", "nous rejoindre", "rejoindre", "t approcher", "approcher",
        "te rapprocher", "venir me voir", "come", "come here", "here",
    };

    private static readonly (McResource Resource, string[] Words)[] ResourceWords =
    {
        (McResource.Wood, new[] { "bois", "buche", "buches", "tronc", "troncs", "arbre", "arbres", "chene", "bouleau",
            "sapin", "log", "logs", "wood", "tree", "trees" }),
        (McResource.Stone, new[] { "pierre", "pierres", "roche", "roches", "caillou", "cailloux", "cobble",
            "cobblestone", "stone" }),
        (McResource.Coal, new[] { "charbon", "charbons", "coal" }),
        (McResource.Iron, new[] { "fer", "iron" }),
        (McResource.Copper, new[] { "cuivre", "copper" }),
        (McResource.Gold, new[] { "or", "gold" }),
        (McResource.Diamond, new[] { "diamant", "diamants", "diams", "diam", "dia", "diamond", "diamonds" }),
        (McResource.Dirt, new[] { "terre", "dirt" }),
        (McResource.Sand, new[] { "sable", "sand" }),
    };

    // A coordinate standing alone: « 120 », « -300 », « x:120 », or F3's « 120.523 » (floored to the block).
    // Not the digits inside a name (Bob123).
    private static readonly Regex Coordinate = new(@"(?<![\w.])-?\d{1,8}(?:\.\d+)?(?![\w.])", RegexOptions.Compiled);
    // A count: « 10 », « x10 », « 10x ».
    private static readonly Regex Count = new(@"(?<![\w.])x?(\d{1,4})x?(?![\w.])", RegexOptions.Compiled);

    // Quantities in words, read only right before the resource (« dix bûches », « une stack de pierre »),
    // so « du bois pour une maison » isn't one log. Two-word ones are tried first.
    private static readonly Dictionary<string, int> QuantityPairs = new()
    {
        ["une dizaine"] = 10, ["une douzaine"] = 12, ["une quinzaine"] = 15, ["une vingtaine"] = 20,
        ["une trentaine"] = 30, ["un peu"] = 5, ["un stack"] = 64, ["une stack"] = 64, ["demi stack"] = 32,
        ["un max"] = 64,
    };
    private static readonly Dictionary<string, int> QuantityWords = new()
    {
        ["un"] = 1, ["une"] = 1, ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4, ["cinq"] = 5, ["six"] = 6, ["sept"] = 7,
        ["huit"] = 8, ["neuf"] = 9, ["dix"] = 10, ["onze"] = 11, ["douze"] = 12, ["treize"] = 13, ["quatorze"] = 14,
        ["quinze"] = 15, ["seize"] = 16, ["vingt"] = 20, ["trente"] = 30, ["quarante"] = 40, ["cinquante"] = 50,
        ["soixante"] = 60, ["quelques"] = 5, ["plein"] = 32, ["beaucoup"] = 32, ["max"] = 64, ["stack"] = 64,
        ["stacks"] = 64,
    };

    // Game-chat spellings, mapped to the word the lists use. « chui » / « jsuis » become « je suis », so
    // « chui là » is never read as « suis » (follow me). Politeness is dropped: it changes nothing.
    private static readonly Dictionary<string, string> Slang = new()
    {
        ["mwa"] = "moi", ["moa"] = "moi", ["mw"] = "moi", ["moua"] = "moi", ["twa"] = "toi", ["toa"] = "toi",
        ["tw"] = "toi", ["ns"] = "nous", ["isi"] = "ici", ["issi"] = "ici", ["chui"] = "je suis", ["chuis"] = "je suis",
        ["jsuis"] = "je suis", ["jsui"] = "je suis", ["chu"] = "je suis", ["jai"] = "j ai", ["tt"] = "tout",
        ["stp"] = "", ["svp"] = "", ["plz"] = "", ["pls"] = "", ["please"] = "", ["steuplait"] = "", ["steup"] = "",
    };

    /// <summary>The order alone, for callers that don't need its details.</summary>
    public static McOrder Read(string text, IReadOnlyCollection<string> names) => Parse(text, names).Order;

    /// <param name="text">The chat message as typed.</param>
    /// <param name="names">Every name she answers to (her in-game name, « syncs »), any case.</param>
    /// <param name="called">The player called her just before (a bare « syncs »): her name isn't needed this time,
    /// and a message with nothing she understands is let go rather than read as another call.</param>
    public static McRequest Parse(string text, IReadOnlyCollection<string> names, bool called = false)
    {
        var words = Clean(Rating.Normalize(text));
        if (words.Length == 0) return McRequest.Nothing;

        // Her name, or a one-letter slip of it (« sycns », « sincs ») when it's long enough not to be another word.
        var nameWords = names.Select(Rating.Normalize).Where(n => n.Length > 0 && !n.Contains(' ')).ToHashSet();
        var at = Array.FindIndex(words, w => nameWords.Contains(w) || (w.Length >= 5 && nameWords.Any(n => n.Length >= 5 && Close(w, n))));
        if (at < 0 && !called) return McRequest.Nothing;
        var rest = words.Where((w, i) => i != at).ToArray();
        var calledOut = at >= 0 && (at == 0 || at == words.Length - 1);
        var numbers = Coordinate.Matches(text)
            .Select(m => (int)Math.Floor(double.Parse(m.Value, CultureInfo.InvariantCulture))).ToArray();
        var counts = Count.Matches(text).Select(m => int.Parse(m.Groups[1].Value)).ToArray();
        // Words other than numbers: « syncs 120 64 -300 » is coordinates and nothing else.
        var wordsOnly = rest.Where(w => !w.All(char.IsDigit) && !IsCountToken(w)).ToArray();

        // An order, unless refused around its verb (null: refused, so she says nothing at all).
        McRequest? When(string[] phrases, McRequest request) =>
            Find(wordsOnly, phrases) is not { } hit ? McRequest.Nothing
            : Refused(wordsOnly, hit) ? null
            : request;

        if (Find(wordsOnly, FollowOverStayPhrases) is not null) return When(FollowOverStayPhrases, new(McOrder.Follow)) ?? McRequest.Nothing;
        if (Find(wordsOnly, FarmAlonePhrases) is not null) return When(FarmAlonePhrases, new(McOrder.Farm)) ?? McRequest.Nothing;
        if (FarmCare(wordsOnly) is { } care) return Refused(wordsOnly, care) ? McRequest.Nothing : new(McOrder.Farm);
        if (Find(wordsOnly, StayPhrases) is not null) return new(McOrder.Stay);

        foreach (var (phrases, order) in new[] { (SleepPhrases, McOrder.Sleep), (GuardPhrases, McOrder.Guard) })
        {
            var found = When(phrases, new(order));
            if (found is null) return McRequest.Nothing;
            if (found.Value.Order != McOrder.None) return found.Value;
        }

        // A crop, before the resources: « pommes de terre » has « terre » (dirt).
        var resourceAt = Array.FindIndex(wordsOnly, w => ResourceWords.Any(r => r.Words.Contains(w)));
        // The verb must be another word than the crop: « la récolte est prête » names the harvest, it doesn't ask for it.
        if (CropIn(wordsOnly) is { } crop && (crop.Crop != McCrop.Any || resourceAt < 0)
            && Find(wordsOnly.Select((w, i) => i == crop.At ? "" : w).ToArray(), CropVerbs) is { } verb)
            return Refused(wordsOnly, verb) ? McRequest.Nothing : new(McOrder.Harvest, Crop: crop.Crop);

        if (resourceAt >= 0)
        {
            var wanted = ResourceWords.First(r => r.Words.Contains(wordsOnly[resourceAt])).Resource;
            var count = counts.FirstOrDefault(n => n > 0);
            if (count == 0) count = QuantityBefore(wordsOnly, resourceAt);
            var collect = When(CollectPhrases,
                new(McOrder.Collect, Resource: wanted, Count: Math.Clamp(count == 0 ? DefaultCount : count, 1, MaxCount)));
            if (collect is null) return McRequest.Nothing;
            if (collect.Value.Order != McOrder.None) return collect.Value;
        }
        // « syncs récolte », « syncs moissonne »: whatever is ripe. Not with a resource (« récolte du sable » is
        // sand), and not « récolte » after an article, which is the noun (« la récolte est prête »).
        else if (Find(wordsOnly, HarvestPhrases) is { } said
                 && !(said.Length == 1 && wordsOnly[said.Start] is "recolte" or "recoltes" && said.Start > 0
                      && wordsOnly[said.Start - 1] is "la" or "ta" or "ma" or "sa" or "une" or "les" or "des" or "tes" or "mes" or "ses" or "notre" or "votre"))
        {
            var harvest = When(HarvestPhrases, new(McOrder.Harvest, Crop: McCrop.Any));
            if (harvest is null) return McRequest.Nothing;
            if (harvest.Value.Order != McOrder.None) return harvest.Value;
        }

        if (numbers.Length is 2 or 3)
        {
            if (wordsOnly.Length == 0) return new(McOrder.GoTo, Coordinates: numbers);
            foreach (var phrases in new[] { GoToPhrases, ComePhrases })
            {
                var goTo = When(phrases, new(McOrder.GoTo, Coordinates: numbers));
                if (goTo is null) return McRequest.Nothing;
                if (goTo.Value.Order != McOrder.None) return goTo.Value;
            }
        }

        foreach (var (phrases, order) in new[] { (FollowPhrases, McOrder.Follow), (ComePhrases, McOrder.Come) })
        {
            var found = When(phrases, new(order));
            if (found is null) return McRequest.Nothing;
            if (found.Value.Order != McOrder.None) return found.Value;
        }
        return calledOut && rest.Length <= 6 ? new(McOrder.Unknown) : McRequest.Nothing;
    }

    // Where a phrase first matches, as whole consecutive words, so « suis » never matches inside
    // « suisse ». A lone « suis » needs to be the message's verb: « je suis là » is not « suis » + an
    // object. Longer phrases are tried first, so « reste avec moi » isn't read as « reste ».
    private static (int Start, int Length)? Find(string[] words, string[] phrases)
    {
        foreach (var phrase in phrases.OrderByDescending(p => p.Length))
        {
            var parts = phrase.Split(' ');
            if (parts is ["suis"] && (words.Contains("je") || words.Contains("j"))) continue;
            for (var i = 0; i + parts.Length <= words.Length; i++)
                if (parts.Select((p, k) => Same(words[i + k], p)).All(x => x)) return (i, parts.Length);
        }
        return null;
    }

    // A word of a phrase: exact, or one slip away for long verbs (« protge », « chercer », « rapplque »).
    // Short words must be exact: at four or five letters, one slip makes another word (« viens » / « liens »).
    private static bool Same(string word, string part) => word == part || (part.Length >= 6 && word.Length >= 5 && Close(word, part));

    // At most one edit apart: a letter added, dropped, changed, or two neighbours swapped.
    private static bool Close(string a, string b)
    {
        if (Math.Abs(a.Length - b.Length) > 1) return false;
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        return d[a.Length, b.Length] <= 1;
    }

    // Game-chat words to the lists' words: slang mapped (Slang), politeness dropped, and letters held down
    // for emphasis squeezed back (« viiiiens », « stoooop »: three or more of a letter become one).
    private static string[] Clean(string normalized)
    {
        var words = new List<string>();
        foreach (var raw in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var word = Regex.Replace(raw, @"(\p{L})\1{2,}", "$1");
            var mapped = Slang.TryGetValue(word, out var to) ? to : word;
            words.AddRange(mapped.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
        return words.ToArray();
    }

    // The crop a message names and where, the first one found; « pomme(s) de terre » is a potato, not an apple and
    // dirt. A crop and a generic word (« le blé du champ ») is the crop.
    private static (McCrop Crop, int At)? CropIn(string[] words)
    {
        for (var i = 0; i + 2 < words.Length; i++)
            if (words[i] is "pomme" or "pommes" or "pom" && words[i + 1] is "de" or "d" && words[i + 2] == "terre") return (McCrop.Potato, i);
        (McCrop, int)? generic = null;
        for (var i = 0; i < words.Length; i++)
            foreach (var (crop, list) in CropWords)
                if (list.Any(c => Same(words[i], c)))
                {
                    if (crop != McCrop.Any) return (crop, i);
                    generic ??= (crop, i);
                }
        return generic;
    }

    // A care verb with a farm word (or, after « occupe-toi » and the like, a crop) within four words after it.
    private static (int Start, int Length)? FarmCare(string[] words)
    {
        foreach (var phrase in FarmVerbs.OrderByDescending(p => p.Length))
        {
            var parts = phrase.Split(' ');
            var takesCrop = CropCareVerbs.Contains(phrase);
            for (var i = 0; i + parts.Length <= words.Length; i++)
            {
                if (!parts.Select((p, k) => Same(words[i + k], p)).All(x => x)) continue;
                var after = i + parts.Length;
                for (var j = after; j < Math.Min(words.Length, after + 4); j++)
                    if (IsFarmWord(words, j) || (takesCrop && CropWords.Any(c => c.Crop != McCrop.Any && c.Words.Any(w => Same(words[j], w)))))
                        return (i, parts.Length);
            }
        }
        return null;
    }

    // « ferme » is a farm only after an article or a possessive (« la ferme », « ma ferme »): « et ferme-la » is not.
    private static bool IsFarmWord(string[] words, int j) =>
        FarmWords.Any(f => Same(words[j], f))
        && (words[j] is not ("ferme" or "fermes")
            || j > 0 && words[j - 1] is "la" or "ma" or "ta" or "sa" or "notre" or "votre" or "les" or "mes" or "tes" or "nos"
                or "vos" or "une" or "cette" or "de");

    // « x10 », « 10x »: a count, not a word.
    private static bool IsCountToken(string word) => Regex.IsMatch(word, @"^x?\d+x?$");

    // A quantity written in words right before the resource, a « de » / « d' » / « du » / « des » allowed in
    // between: « dix bûches », « une stack de pierre », « une dizaine de bûches ». Zero when there's none.
    private static int QuantityBefore(string[] words, int resourceAt)
    {
        var j = resourceAt - 1;
        if (j >= 0 && words[j] is "de" or "d" or "du" or "des") j--;
        if (j < 0) return 0;
        if (j >= 1 && QuantityPairs.TryGetValue(words[j - 1] + " " + words[j], out var pair)) return pair;
        return QuantityWords.TryGetValue(words[j], out var single) ? single : 0;
    }

    // A refusal next to the verb: « ne » up to two words before (« ne me suis »), « pas » just before
    // (« ne va pas chercher »), « pas » or « jamais » up to two words after, or « plus » unless it means
    // more (« plus de bois »). Wider than that, a « pas » belongs to another sentence.
    private static bool Refused(string[] words, (int Start, int Length) hit)
    {
        for (var i = Math.Max(0, hit.Start - 2); i < hit.Start; i++)
            if (words[i] is "ne" or "n") return true;
        if (hit.Start > 0 && words[hit.Start - 1] == "pas") return true;
        var after = hit.Start + hit.Length;
        for (var i = after; i < Math.Min(words.Length, after + 2); i++)
        {
            if (words[i] is "pas" or "jamais") return true;
            if (words[i] == "plus" && (i + 1 >= words.Length || words[i + 1] is not ("de" or "d"))) return true;
        }
        return false;
    }
}
