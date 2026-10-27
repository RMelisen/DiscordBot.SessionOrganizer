using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

// How McCommand finds things in the words: places and whose they are, what she carries, durations, phrases with one
// slip, crops and farm words, counts, refusals. Part of McCommand.
public static partial class McCommand
{
    private static bool AboutPlace(McRequest request) =>
        request.Order is McOrder.SavePlace or McOrder.GoToPlace or McOrder.Guide or McOrder.WherePlace or McOrder.ForgetPlace
            or McOrder.ListPlaces
        || request is { Order: McOrder.Patrol, Place: not null };

    // « de X », « d'X », « du X » or « chez X », X a player: the words without it, and X. « chez X » becomes « à la maison »,
    // which the service reads as X's home, whatever X named it. Never her own name.
    private static (string[] Words, string Player)? OwnerOut(string[] words, IReadOnlyCollection<string> players, IReadOnlyCollection<string> names)
    {
        var hers = names.Select(PlaceKey).ToHashSet();
        var keyed = players
            .Select(p => (Player: p, Key: PlaceKey(p)))
            .Where(p => p.Key.Length > 0 && !hers.Contains(p.Key) && !NameFiller.Contains(p.Key))
            .Select(p => (p.Player, Parts: p.Key.Split(' ')))
            .OrderByDescending(p => p.Parts.Length)
            .ToArray();
        for (var i = 0; i + 1 < words.Length; i++)
        {
            if (words[i] is not ("de" or "d" or "du" or "chez")) continue;
            foreach (var (player, parts) in keyed)
                if (i + 1 + parts.Length <= words.Length && parts.Select((p, k) => words[i + 1 + k] == p).All(x => x))
                {
                    string[] instead = words[i] == "chez" ? ["a", "la", "maison"] : [];
                    return ([.. words[..i], .. instead, .. words[(i + 1 + parts.Length)..]], player);
                }
        }
        return null;
    }

    /// <summary>A place's key: its name as the parser reads it (lower case, no accents, game-chat spellings mapped).</summary>
    public static string PlaceKey(string name) => string.Join(' ', Clean(Rating.Normalize(Ligatures(name))));

    // « œuf », « cœur »: Normalize drops the joined letters, so they're spelled out first.
    private static string Ligatures(string text) => text.Replace("œ", "oe").Replace("Œ", "Oe").Replace("æ", "ae").Replace("Æ", "Ae");

    /// <summary>
    /// A place's name as the player wrote it, accents kept, for her lines: the words of <paramref name="text"/> that
    /// read as <paramref name="key"/>, in lower case. The key itself when they can't be found (a name typed with stretched
    /// letters, say). Never longer than <see cref="MaxPlaceName"/>, and never with Minecraft's colour sign.
    /// </summary>
    public static string PlaceName(string text, string key)
    {
        var parts = key.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var raw = Regex.Matches(text, @"[\p{L}\p{N}]+").Select(m => m.Value).ToArray();
        var folded = raw.Select(Rating.Normalize).ToArray();
        for (var i = 0; parts.Length > 0 && i + parts.Length <= raw.Length; i++)
            if (parts.Select((p, k) => folded[i + k] == p).All(x => x))
            {
                var name = string.Join(' ', raw.Skip(i).Take(parts.Length)).ToLowerInvariant();
                return name.Length <= MaxPlaceName ? name : key;
            }
        return key;
    }

    // The first known place named in the words, the longest at that spot (« la mine de fer » before « la mine »), with
    // where it starts: « protège la base pendant qu'on mine » is the base.
    private static (string Key, int At)? PlaceIn(string[] words, IReadOnlyCollection<string> places)
    {
        var keys = places.OrderByDescending(k => k.Length).Select(k => (Key: k, Parts: k.Split(' '))).ToArray();
        for (var i = 0; i < words.Length; i++)
            foreach (var (key, parts) in keys)
                if (i + parts.Length <= words.Length && parts.Select((p, k) => words[i + k] == p).All(x => x)) return (key, i);
        return null;
    }

    // The place an order is about: a known one after its verb, else the name after a preposition (« va à la mine »,
    // « emmène-moi jusqu'à la base »), else nothing. Where-is takes a name after the verb directly (« où est la mine »).
    private static string? PlaceAfter(string[] words, (int Start, int Length) verb, (string Key, int At)? known, bool needsPreposition)
    {
        var after = verb.Start + verb.Length;
        if (known is { } k && k.At >= after) return k.Key;
        var last = words[after - 1];
        var preposition = Prepositions.Contains(last) || (after < words.Length && Prepositions.Contains(words[after]));
        if (needsPreposition && !preposition) return null;
        if (!needsPreposition && !preposition && (after >= words.Length || !Articles.Contains(words[after]))) return null;
        return NameAfter(words, after);
    }

    // A place's name after a verb: the filler skipped (« ici comme la »), then up to three words, up to « et » or « merci ».
    private static string? NameAfter(string[] words, int from)
    {
        var i = from;
        while (i < words.Length && NameFiller.Contains(words[i]))
        {
            // « chez nous », « chez moi »: a home's name, though both words are filler elsewhere.
            if (words[i] == "chez" && i + 1 < words.Length && words[i + 1] is "nous" or "moi") return $"chez {words[i + 1]}";
            i++;
        }
        var parts = new List<string>();
        for (; i < words.Length && parts.Count < 3 && !NameStop.Contains(words[i]); i++) parts.Add(words[i]);
        while (parts.Count > 0 && NameFiller.Contains(parts[^1])) parts.RemoveAt(parts.Count - 1);
        var name = string.Join(' ', parts);
        return name.Length == 0 ? null : name.Length <= MaxPlaceName ? name : name[..MaxPlaceName].TrimEnd();
    }

    // Remembering a place: a verb that says it (« retiens », « enregistre »), a looser one right before « ici » (« note
    // ici »), or « ici c'est » before an article (« ici c'est la mine »). Where the name may start.
    private static ((int Start, int Length) Verb, int NameFrom)? SaveAt(string[] words)
    {
        if (Find(words, SaveVerbs) is { } verb) return (verb, verb.Start + verb.Length);
        if (Find(words, SaveLooseVerbs) is { } loose && Find(words.Skip(loose.Start + loose.Length).ToArray(), HereWords) is { Start: 0 })
            return (loose, loose.Start + loose.Length);
        if (Find(words, HereIsPhrases) is { } hereIs && hereIs.Start + hereIs.Length < words.Length
            && (Articles.Contains(words[hereIs.Start + hereIs.Length]) || words[hereIs.Start + hereIs.Length] == "chez"))
            return (hereIs, hereIs.Start + hereIs.Length);
        return null;
    }

    // What follows a give verb (within three words): a possessive and a kind of goods (« ta canne à pêche », « tes pommes
    // de terre »), or goods with any article (« du poisson »). Goods null with Owned: something of hers she doesn't know.
    // A phrase that says it alone, or a verb with armour named in the words after it (GoodsAfter).
    private static (int Start, int Length)? ArmourVerb(string[] words, string[] verbs, string[] alone)
    {
        if (Find(words, alone) is { } said) return said;
        return Find(words, verbs) is { } verb && GoodsAfter(words, verb.Start + verb.Length) is { Goods: McGoods.Armour } ? verb : null;
    }

    private static (McGoods? Goods, bool Owned)? GoodsAfter(string[] words, int from)
    {
        var owned = false;
        for (int j = from, looked = 0; j < words.Length && looked < 4; j++)
        {
            if (GoodsFiller.Contains(words[j])) continue;
            looked++;
            if (NotGoods.Contains(words[j])) return null;
            if (Possessives.Contains(words[j])) { owned = true; continue; }
            // « un truc à manger », « quelque chose à bouffer »: food, not everything.
            if (words[j] is "truc" or "trucs" or "chose" && j + 2 < words.Length && words[j + 1] == "a"
                && GoodsWords.First(g => g.Goods == McGoods.Food).Words.Contains(words[j + 2]))
                return (McGoods.Food, owned);
            if (GoodsAt(words, j) is { } goods) return (goods, owned);
        }
        return owned ? (null, true) : null;
    }

    // The kind of goods a word names, with the words after it: « pommes de terre » is produce, not apples and dirt;
    // « poudre de redstone » is redstone, not gunpowder.
    private static McGoods? GoodsAt(string[] words, int j)
    {
        if (words[j] is "pomme" or "pommes" or "pom" && j + 2 < words.Length && words[j + 1] is "de" or "d" && words[j + 2] == "terre")
            return McGoods.Produce;
        if (words[j] == "poudre" && words.Skip(j + 1).Take(2).Contains("redstone")) return McGoods.Redstone;
        if (words[j] is "baton" or "batons" && words.Skip(j + 1).Take(2).Contains("blaze")) return McGoods.BlazeRods;
        if (CropWords.Any(c => c.Crop != McCrop.Any && c.Words.Any(w => Same(words[j], w)))) return McGoods.Produce;
        foreach (var (goods, list) in GoodsWords)
            if (list.Contains(words[j])) return goods;
        return null;
    }

    // A question about what she carries: the whole (Inventory) or one kind (Have). Null when it isn't one, or when a
    // gathering, handing-over or storing verb is in the message, which makes it an order (« t'as du fer ? ramène-m'en »).
    // A question about goods she can't fetch (« t'as du poisson ? », « t'as quoi comme épée ») stays a request to hand
    // them over, unless it asks how many.
    private static McRequest? CarryQuestion(string[] words)
    {
        if (words.Any(w => ChestWords.Contains(w))) return null;
        foreach (var verbs in new[] { GiveAllPhrases, GiveVerbs, StorePhrases, CollectPhrases })
            if (Find(words, verbs) is not null) return null;

        // « combien »: always a question, of a kind of goods or of everything (« t'as combien de trucs ? »). Looked up as is:
        // Find takes a word after « as » for the past tense, and « t'as combien » is not.
        if (words.Contains("combien") || Find(words, HowManyWords) is not null)
            return GoodsIn(words) is { } counted ? Asked(counted) : null;

        // « t'as quoi ? », « t'as quoi sur toi ? », « t'as quoi comme bois ? ».
        if (Find(words, CarryQuestions) is { } question)
        {
            var after = question.Start + question.Length;
            var goods = GoodsAfter(words, after)?.Goods;
            // Armour is the exception: « t'as quoi comme armure ? » can't mean « take it off ».
            if (goods is { } kind && kind != McGoods.All)
                return CarriedOnly.Contains(kind) && kind != McGoods.Armour ? null : new McRequest(McOrder.Have, Goods: kind);
            if (after == words.Length || goods == McGoods.All || CarryQuestionFollow.Contains(words[after])) return new McRequest(McOrder.Inventory);
            return null;
        }

        // « montre ton inventaire », « c'est quoi dans ton sac ? », « montre ce que t'as »: hers, not « mes poches ».
        var inventoryWord = NamesHerInventory(words);
        if (inventoryWord && Find(words, ShowVerbs) is not null) return new McRequest(McOrder.Inventory);
        // « syncs inventaire », « syncs ton inv ? »: the word alone.
        if (inventoryWord && words.All(w => InventoryFiller.Contains(w) || InventoryWords.Contains(w)))
            return new McRequest(McOrder.Inventory);

        // « t'as du fer ? », « t'aurais du bois ? »: goods she could fetch, so not a request to hand them over.
        if (Find(words, HaveVerbs) is { } have && GoodsAfter(words, have.Start + have.Length) is { Goods: { } had }
            && !CarriedOnly.Contains(had))
            return new McRequest(McOrder.Have, Goods: had);
        return null;

        static McRequest Asked(McGoods goods) => goods == McGoods.All ? new(McOrder.Inventory) : new(McOrder.Have, Goods: goods);
    }

    // An inventory word of hers (« ton inventaire », « tes poches »), never the player's (« mes poches »).
    private static bool NamesHerInventory(string[] words)
    {
        foreach (var phrase in InventoryWords)
        {
            var parts = phrase.Split(' ');
            for (var i = 0; i + parts.Length <= words.Length; i++)
                if (parts.Select((p, k) => words[i + k] == p).All(x => x) && !(i > 0 && TheirOwn.Contains(words[i - 1])))
                    return true;
        }
        return false;
    }

    // The first kind of goods named anywhere in the message: « combien de fer t'as », « t'as combien de pommes de terre ».
    private static McGoods? GoodsIn(string[] words)
    {
        for (var j = 0; j < words.Length; j++)
            if (GoodsAt(words, j) is { } goods) return goods;
        return null;
    }

    // What a cooking order is about: the first food, ore or sand word, else what the verb implies (melting is ore,
    // glass is sand, the rest is food).
    private static McSmelt SmeltIn(string[] words, (int Start, int Length) verb)
    {
        foreach (var w in words)
            foreach (var (smelt, list) in SmeltWords)
                if (list.Contains(w)) return smelt;
        var said = string.Join(' ', words.Skip(verb.Start).Take(verb.Length));
        return said.Contains("fond") || said == "smelt" ? McSmelt.Ore : said.Contains("verre") ? McSmelt.Sand : McSmelt.Food;
    }

    // A verb with one of its objects within four words after it (« surveille la base »). With skipMe, a verb followed by
    // « moi » or « nous » is about the player (« protège-moi à la maison » is guard, not patrol).
    // With window 0, the verb alone is enough (its object is elsewhere: « reste ici et surveille »).
    private static (int Start, int Length)? CareFor(string[] words, string[] verbs, Func<string[], int, bool> isObject, bool skipMe = false,
        int window = 4)
    {
        foreach (var phrase in verbs.OrderByDescending(p => p.Length))
        {
            var parts = phrase.Split(' ');
            for (var i = 0; i + parts.Length <= words.Length; i++)
            {
                if (!parts.Select((p, k) => Same(words[i + k], p)).All(x => x)) continue;
                if (i > 0 && words[i - 1] is "je" or "j") continue;
                var after = i + parts.Length;
                if (skipMe && after < words.Length && words[after] is "moi" or "nous" or "me" or "m") continue;
                if (window == 0) return (i, parts.Length);
                for (var j = after; j < Math.Min(words.Length, after + window); j++)
                    if (isObject(words, j)) return (i, parts.Length);
            }
        }
        return null;
    }

    /// <summary>How long a timer runs, in seconds, from the raw text; null when no length of time is given.</summary>
    public static int? DurationIn(string text)
    {
        if (ClockTime.IsMatch(text)) return null;
        if (HoursMinutes.Match(text) is { Success: true } hm)
            return int.Parse(hm.Groups["h"].Value) * 3600 + int.Parse(hm.Groups["m"].Value) * 60;
        var m = Duration.Match(text);
        if (!m.Success)
            return BareMinutes.Match(text) is { Success: true } bare ? int.Parse(bare.Groups["n"].Value) * 60 : null;
        if (m.Groups["quart"].Success) return 15 * 60;
        if (m.Groups["demi"].Success) return 30 * 60;
        var number = m.Groups["n"].Value.ToLowerInvariant();
        var n = int.TryParse(number, out var digits) ? digits : QuantityWords.GetValueOrDefault(number, 1);
        var unit = m.Groups["u"].Value.ToLowerInvariant();
        return n * (unit.StartsWith('h') ? 3600 : unit.StartsWith('m') ? 60 : 1);
    }

    // What to remind them of: the raw text after the first « de », « pour », « que » once the duration and her name are
    // out (« rappelle-moi dans 5 min de sortir le fer » → « sortir le fer »). Null when there's none.
    private static string? TimerNote(string text, IReadOnlyCollection<string> names)
    {
        var rest = Duration.Replace(HoursMinutes.Replace(text, " "), " ");
        foreach (var name in names.Where(n => n.Length > 0))
            rest = Regex.Replace(rest, $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(name)}(?![\p{{L}}\p{{N}}])", " ", RegexOptions.IgnoreCase);
        var start = NoteStart.Match(rest);
        if (!start.Success) return null;
        var note = Regex.Replace(rest[(start.Index + start.Length)..].Replace("§", ""), @"\s+", " ").Trim().TrimEnd('?', '!', '.', ',', ';', ' ');
        if (note.Length < 2) return null;
        return note.Length <= NoteMax ? note : note[..NoteMax].TrimEnd() + "…";
    }

    // Where a phrase first matches, as whole consecutive words, so « suis » never matches inside
    // « suisse ». A lone « suis » needs to be the message's verb: « je suis là » is not « suis » + an
    // object. Longer phrases are tried first, so « reste avec moi » isn't read as « reste ».
    // The player talking about themselves (« je range ma chambre », « ça fait 5 min que j'attends ») is not an order,
    // so a phrase right after « je » doesn't count, unless it starts with it (« j'ai faim »).
    private static (int Start, int Length)? Find(string[] words, string[] phrases)
    {
        foreach (var phrase in phrases.OrderByDescending(p => p.Length))
        {
            var parts = phrase.Split(' ');
            if (parts is ["suis"] && (words.Contains("je") || words.Contains("j"))) continue;
            for (var i = 0; i + parts.Length <= words.Length; i++)
            {
                if (!parts.Select((p, k) => Same(words[i + k], p)).All(x => x)) continue;
                if (i > 0 && words[i - 1] is "je" or "j" && parts[0] is not ("je" or "j")) continue;
                // The player's own plans (« je vais chercher du bois »), and someone else's doing (« il reste du blé »).
                if (OwnPlan(words, i) && parts[0] is not ("je" or "j")) continue;
                if (i > 0 && ThirdPersons.Contains(words[i - 1]) && !ThirdPersons.Contains(parts[0])) continue;
                if (i > 1 && LikeVerbs.Contains(words[i - 1]) && words[i - 2] is "t" or "tu") continue;
                // « en train de couper du bois »: what someone is doing, not what she should do.
                if (i > 1 && words[i - 1] is "de" or "d" && words[i - 2] == "train") continue;
                // Past tense (« je t'ai donné ma pioche », « j'ai déjà vidé mes poches ») reports, it doesn't ask.
                if (parts[0] is not ("je" or "j" or "t" or "tu" or "a") && Past(words, i)) continue;
                // By the word as typed: « pêché » matches « pêcher » with one slip, and is still past tense.
                if (parts.Length == 1 && i > 0 && NounAfter.TryGetValue(words[i], out var nounAfter) && nounAfter.Contains(words[i - 1])) continue;
                return (i, parts.Length);
            }
        }
        return null;
    }

    // A word after an auxiliary, maybe past an adverb (« j'ai vidé », « t'as bien dormi », « j'ai déjà coupé »), or after
    // « a » when it's a participle as typed (« on a coupé du bois », « Bob a miné du fer »): « à » comes before an
    // infinitive (« aide-moi à couper »), never before an order. The caller leaves out phrases that open with the subject,
    // which are questions (« t'as du fer ? »).
    private static bool Past(string[] words, int i)
    {
        var k = i - 1;
        while (k >= 0 && k >= i - 2 && PastAdverbs.Contains(words[k])) k--;
        if (k < 0) return false;
        if (Auxiliaries.Contains(words[k])) return true;
        // « y a quoi dans ton sac » is « il y a », not a past tense.
        var typed = words[i];
        return words[k] == "a" && !(k > 0 && words[k - 1] == "y")
               && typed[^1] is 'e' or 'i' or 'u' or 's' or 't' && !typed.EndsWith("re") && !typed.EndsWith("er") && !typed.EndsWith("ir");
    }

    // « je » two words back at most, past a modal and what goes with it (« je vais chercher », « je dois aller chercher »,
    // « je vais y aller »). No further: the commas are gone, and « je m'en vais, suis-moi » is an order.
    private static bool OwnPlan(string[] words, int i)
    {
        var k = i - 1;
        var modal = false;
        while (k >= 0 && k >= i - 2 && (SelfModals.Contains(words[k]) || words[k] is "aller" or "y" or "me" or "m" or "en"))
            modal |= SelfModals.Contains(words[k--]);
        return modal && k >= 0 && words[k] is "je" or "j";
    }

    // One-word verbs that are something else after these words: « la pêche », « j'ai pêché », « c'est cuit », « au fond ».
    private static readonly Dictionary<string, HashSet<string>> NounAfter = new()
    {
        ["peche"] = new() { "la", "ma", "ta", "sa", "une", "de", "ai", "as", "a", "fait", "aime", "adore" },
        ["cuit"] = new() { "est", "c", "bien", "trop", "pas", "deja", "ai", "as", "a" },
        ["fond"] = new() { "au", "le", "du", "de", "a", "en" },
        ["range"] = new() { "la", "le", "ma", "ta", "sa", "une", "en" },
        // « la base est protégée ? » asks, it doesn't order.
        ["protegee"] = new() { "est", "es", "bien", "sont", "pas", "trop" },
        ["protege"] = new() { "est", "es", "bien", "sont", "pas", "trop" },
        ["gardee"] = new() { "est", "es", "bien", "sont", "pas", "trop" },
        ["surveillee"] = new() { "est", "es", "bien", "sont", "pas", "trop" },
        // « je suis fauché » is broke; « la cuisine » the room; « ça marche » works; « je vais te suivre » follows her.
        ["fauche"] = new() { "suis", "es", "est", "t", "trop", "grave", "completement", "sommes", "etes", "sont" },
        ["fauchee"] = new() { "suis", "es", "est", "t", "trop", "grave", "completement", "sommes", "etes", "sont" },
        ["cuisine"] = new() { "la", "ma", "ta", "sa", "une", "de", "en", "notre", "cette" },
        ["marche"] = new() { "ca", "c", "le", "au", "du", "un", "ce", "en" },
        ["suivre"] = new() { "te" },
        ["suit"] = new() { "te" },
    };

    // A word of a phrase: exact, or one slip away for long verbs (« protge », « chercer », « rapplque »).
    // Short words must be exact: at four or five letters, one slip makes another word (« viens » / « liens »).
    // A common word typed as itself (NoSlip: « reviens », « jardin ») is never another one.
    private static bool Same(string word, string part) =>
        word == part || (part.Length >= 6 && word.Length >= 5 && !NoSlip.Contains(word) && Close(word, part));

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
            // « tu viens ou pas ? » is impatience, not a refusal.
            if (words[i] == "pas" && words[i - 1] == "ou") continue;
            if (words[i] is "pas" or "jamais") return true;
            if (words[i] == "plus" && (i + 1 >= words.Length || words[i + 1] is not ("de" or "d"))) return true;
        }
        return false;
    }

    // An order she keeps doing refused with « plus » (« ne me suis plus », « me protège plus », « ne pêche plus »): stop
    // doing it, which is staying. Not with « tu » just before, a complaint (« tu me suis plus ? »).
    private static bool NoLonger(string[] words, (int Start, int Length) hit)
    {
        for (var i = Math.Max(0, hit.Start - 3); i < hit.Start; i++)
            if (words[i] is "tu" or "t") return false;
        var after = hit.Start + hit.Length;
        for (var i = after; i < Math.Min(words.Length, after + 2); i++)
            if (words[i] == "plus" && (i + 1 >= words.Length || words[i + 1] is not ("de" or "d"))) return true;
        return false;
    }

    private static bool Ongoing(McOrder order) =>
        order is McOrder.Follow or McOrder.Guard or McOrder.Patrol or McOrder.Farm or McOrder.Ranch or McOrder.Fish;

    // « j'ai faim », « j'ai trop faim », « j'ai super faim »: « j'ai », up to two words, « faim »; not « j'ai plus faim ».
    private static bool Hungry(string[] words)
    {
        for (var i = 2; i < words.Length; i++)
        {
            if (words[i] != "faim") continue;
            for (var k = i - 1; k >= Math.Max(1, i - 3); k--)
            {
                if (words[k] is "pas" or "plus" or "jamais") break;
                if (words[k] == "ai" && words[k - 1] is "j" or "je") return true;
            }
        }
        return false;
    }

    // A known place named right after a verb, past a preposition (« viens à la base », « attends-moi à la mine »), with only
    // « moi », « me rejoindre » and the like between.
    private static bool PlaceRightAfter(string[] words, (int Start, int Length) verb, (string Key, int At) place)
    {
        var i = place.At - 1;
        while (i >= 0 && Articles.Contains(words[i])) i--;
        var after = verb.Start + verb.Length;
        if (i < after || !Prepositions.Contains(words[i])) return false;
        for (var j = after; j < i; j++)
            if (words[j] is not ("moi" or "nous" or "me" or "m" or "rejoindre" or "retrouver")) return false;
        return true;
    }

    // « arrête de dire ça »: a stop word, « de », maybe « me », and a talking word. Her talk, not her task.
    private static bool StopsTalking(string[] words, (int Start, int Length) hit)
    {
        if (!StopWords.Contains(string.Join(' ', words.Skip(hit.Start).Take(hit.Length)))) return false;
        var j = hit.Start + hit.Length;
        if (j >= words.Length || words[j] is not ("de" or "d")) return false;
        j++;
        if (j < words.Length && words[j] is "me" or "m" or "nous" or "te" or "t" or "se" or "s") j++;
        return j < words.Length && StopTalking.Contains(words[j]);
    }

    // A resource asked for with no verb: « syncs du bois », « syncs 20 bois », « il faut de la pierre ». Every word a resource
    // word, a quantity or BareAskFiller, with at least one partitive, quantity or number; « un » alone isn't one (« syncs un
    // arbre ! » points at it).
    private static bool BareAsk(string[] words, bool counted)
    {
        var resource = false;
        var amount = counted;
        foreach (var w in words)
        {
            if (ResourceWords.Any(r => r.Words.Contains(w))) { resource = true; continue; }
            if (w is not ("un" or "une") && QuantityWords.ContainsKey(w) || w is "du" or "de" or "d" or "des" or "dizaine" or "douzaine" or "vingtaine")
            {
                amount = true;
                continue;
            }
            if (!BareAskFiller.Contains(w)) return false;
        }
        return resource && amount;
    }

    // « il y a », « y a »: what's there, not a call (« y a du fer par ici ? »).
    private static bool ThereIs(string[] words) =>
        words.Contains("ya") || words.Select((w, i) => w == "y" && i + 1 < words.Length && words[i + 1] is "a" or "avait").Any(x => x);
}
