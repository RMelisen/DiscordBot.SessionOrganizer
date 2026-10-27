namespace ProjectSYNCS.Helpers;

/// <summary>
/// Something she can craft for a player. <see cref="Key"/> is the item's game id, or for a tool with materials a bare key
/// (<c>pickaxe</c>) the material is put in front of (<c>iron_pickaxe</c>). <see cref="Words"/> are in <c>Clean</c>'s
/// normalised form. A count is a number of items, crafted in whole recipes of <see cref="Yield"/>; a tool is one by
/// default, anything else one recipe's worth; never more than <see cref="Max"/>.
/// </summary>
public sealed record McCraftable(string Key, string[] Words, int Yield, bool Tool, int Max);

/// <summary>
/// What « fabrique-moi une pioche en fer » asks for: the item and how many. Tools and weapons in wood, stone, iron, gold
/// and diamond, a bow, a fishing rod, shears, and a few everyday things. Pure, walked by the checks.
/// </summary>
public static class McCraft
{
    public static readonly McCraftable[] All =
    [
        new("pickaxe", ["pioche", "pioches", "pickaxe", "pick"], 1, true, 4),
        new("axe", ["hache", "haches", "axe"], 1, true, 4),
        new("shovel", ["pelle", "pelles", "shovel"], 1, true, 4),
        new("hoe", ["houe", "houes", "binette", "binettes", "hoe"], 1, true, 4),
        new("sword", ["epee", "epees", "sword"], 1, true, 4),
        new("bow", ["arc", "arcs", "bow"], 1, true, 4),
        new("fishing_rod", ["canne a peche", "cannes a peche", "canne", "cannes", "fishing rod"], 1, true, 4),
        new("shears", ["cisailles", "cisaille", "shears"], 1, true, 4),
        new("planks", ["planche", "planches", "planks"], 4, false, 64),
        new("stick", ["baton", "batons", "stick", "sticks"], 4, false, 64),
        new("crafting_table", ["table de craft", "crafting table", "table", "tables", "etabli", "etablis", "workbench"], 1, false, 64),
        new("chest", ["coffre", "coffres", "chest"], 1, false, 64),
        new("furnace", ["four", "fours", "furnace"], 1, false, 64),
        new("torch", ["torche", "torches", "torch"], 4, false, 64),
        new("ladder", ["echelle", "echelles", "ladder", "ladders"], 3, false, 64),
    ];

    // The tools that come in materials: a bare one (« une pioche ») is the best she can afford, the body's call.
    private static readonly HashSet<string> MaterialTools = ["pickaxe", "axe", "shovel", "hoe", "sword"];

    private static readonly Dictionary<string, string> Materials = new()
    {
        ["bois"] = "wooden", ["wood"] = "wooden", ["wooden"] = "wooden",
        ["pierre"] = "stone", ["roche"] = "stone", ["cobble"] = "stone", ["stone"] = "stone",
        ["fer"] = "iron", ["iron"] = "iron",
        ["or"] = "golden", ["gold"] = "golden", ["golden"] = "golden",
        ["diamant"] = "diamond", ["diamants"] = "diamond", ["diams"] = "diamond", ["diamond"] = "diamond",
    };

    // Between the verb and the item: articles, « moi », a few adjectives.
    private static readonly HashSet<string> Fillers =
    [
        "un", "une", "des", "de", "du", "d", "la", "le", "les", "l", "moi", "me", "nous", "quelques", "petit", "petite", "nouveau",
        "nouvelle", "nouvelles", "nouveaux", "autre", "ton", "ta", "tes",
    ];

    // Her names for the bare tool keys and the planks, which McItems names by their full ids only.
    private static readonly Dictionary<string, McItemName> Bare = new()
    {
        ["pickaxe"] = new("pioche", "pioches", true), ["axe"] = new("hache", "haches", true),
        ["shovel"] = new("pelle", "pelles", true), ["hoe"] = new("houe", "houes", true),
        ["sword"] = new("épée", "épées", true), ["planks"] = new("planche", "planches", true),
    };

    /// <summary>
    /// The item asked for within the three words after the verb (fillers aside), with its material when one follows a
    /// tool (« pioche en fer », « épée de diamant »), and the count: the first number in the message, else one tool or
    /// one recipe's worth, at most <see cref="McCraftable.Max"/>. Null when nothing there is something she makes.
    /// </summary>
    public static (string Item, int Count)? Read(string[] words, int from, int[] counts)
    {
        var looked = 0;
        for (var j = from; j < words.Length && looked < 3; j++)
        {
            if (Fillers.Contains(words[j])) continue;
            looked++;
            foreach (var c in All)
            foreach (var phrase in c.Words.OrderByDescending(w => w.Length))
            {
                var parts = phrase.Split(' ');
                if (j + parts.Length > words.Length || !parts.Select((p, k) => words[j + k] == p).All(x => x)) continue;
                var item = MaterialTools.Contains(c.Key) && MaterialAt(words, j + parts.Length) is { } m ? $"{m}_{c.Key}" : c.Key;
                var asked = counts.FirstOrDefault(n => n > 0);
                return (item, asked > 0 ? Math.Min(asked, c.Max) : c.Tool ? 1 : c.Yield);
            }
        }
        return null;
    }

    // « en fer », « de diamant », « d'or », or the material word right after (« pioche fer »). « or » only after a
    // preposition: alone, it's « or ».
    private static string? MaterialAt(string[] words, int k)
    {
        if (k >= words.Length) return null;
        if (words[k] is "en" or "de" or "d") return k + 1 < words.Length && Materials.TryGetValue(words[k + 1], out var after) ? after : null;
        return words[k] != "or" && Materials.TryGetValue(words[k], out var right) ? right : null;
    }

    /// <summary>How she names <paramref name="count"/> of an item in a line: « une pioche en fer », « 16 torches ».</summary>
    public static string Label(string item, int count) =>
        (McItems.Name(item) ?? (Bare.TryGetValue(item, out var bare) ? bare : new McItemName(item, item, false))).Count(count);
}
