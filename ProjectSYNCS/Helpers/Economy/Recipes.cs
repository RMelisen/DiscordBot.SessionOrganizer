namespace ProjectSYNCS.Helpers;

/// <summary>
/// What a recipe's ingredients mean. An ingredient key is a named collectible (« col.galet ») or
/// « any commons of a set » (<see cref="AnyCommon"/>, « any:champignons »): any of the set's commun
/// items, counted together. Never stored — recipes live in <see cref="CosmeticCatalog"/>. Pure, so
/// the screen, the craft and the checks agree.
/// </summary>
public static class Recipes
{
    private const string AnyPrefix = "any:";

    public static string AnyCommon(string setKey) => AnyPrefix + setKey;

    public static bool IsAnyCommon(string key, out string setKey)
    {
        var any = key.StartsWith(AnyPrefix, StringComparison.Ordinal);
        setKey = any ? key[AnyPrefix.Length..] : "";
        return any;
    }

    /// <summary>The items an ingredient can be: itself, or the set's communs in catalog order.</summary>
    public static IReadOnlyList<ItemInfo> Items(string key) =>
        IsAnyCommon(key, out var set)
            ? ItemCatalog.InSet(set).Where(i => i.Rarity == ItemRarity.Common).ToList()
            : ItemCatalog.ByKey(key) is { } item ? new[] { item } : Array.Empty<ItemInfo>();

    public static int Have(string key, IReadOnlyDictionary<string, int> held) =>
        IsAnyCommon(key, out _) ? Items(key).Sum(i => held.GetValueOrDefault(i.Key)) : held.GetValueOrDefault(key);

    /// <summary>
    /// What a craft takes for one ingredient. Any-common: one unit at a time from whichever stack is
    /// largest now (ties: earliest in the catalog), so stacks level down and the last single copies —
    /// the ones a collection turn-in needs — go last. The caller checked <see cref="Have"/> first.
    /// </summary>
    public static IReadOnlyList<(string ItemKey, int Count)> Take(string key, int count, IReadOnlyDictionary<string, int> held)
    {
        if (!IsAnyCommon(key, out _)) return new[] { (key, count) };
        var items = Items(key);
        var left = items.Select(i => held.GetValueOrDefault(i.Key)).ToArray();
        var taken = new int[items.Count];
        for (var n = 0; n < count; n++)
        {
            var best = -1;
            for (var i = 0; i < left.Length; i++)
                if (left[i] > 0 && (best < 0 || left[i] > left[best])) best = i;
            if (best < 0) break;
            left[best]--;
            taken[best]++;
        }
        return items.Select((i, idx) => (i.Key, taken[idx])).Where(t => t.Item2 > 0).ToList();
    }

    /// <summary>« Améthyste », or « champignon commun » / « champignons communs » by count.</summary>
    public static string Name(string key, int count)
    {
        if (!IsAnyCommon(key, out var set)) return ItemCatalog.ByKey(key) is { } item ? ItemCatalog.ClearName(item) : key;
        var (one, many) = set switch
        {
            "champignons" => ("champignon commun", "champignons communs"),
            "cailloux" => ("caillou commun", "cailloux communs"),
            "insectes" => ("insecte commun", "insectes communs"),
            "tresors" => ("trésor commun", "trésors communs"),
            "nature" => ("objet de la nature commun", "objets de la nature communs"),
            _ => ($"objet commun de la collection {SetName(set)}", $"objets communs de la collection {SetName(set)}"),
        };
        return count > 1 ? many : one;
    }

    public static string Emoji(string key) =>
        IsAnyCommon(key, out var set)
            ? ItemCatalog.Sets.FirstOrDefault(s => s.Key == set)?.Emoji ?? "❔"
            : ItemCatalog.ByKey(key)?.Emoji ?? "❔";

    /// <summary>
    /// Where to look for an ingredient: a named item's <see cref="ForagePlaces.Whereabouts"/>
    /// (« rare, à la Sommeille, l'été »), or the places of a set's communs (« à la Lisière »).
    /// </summary>
    public static string Where(string key) =>
        IsAnyCommon(key, out _)
            ? ForagePlaces.AtAny(Items(key).Where(i => i.Season == Season.None))
            : ItemCatalog.ByKey(key) is { } item ? ForagePlaces.Whereabouts(item) : "";

    /// <summary>What the items lack, named: « 2 × Améthyste », « 3 champignons communs ». Cailloux are not counted.</summary>
    public static List<string> Lacking(CosmeticInfo recipe, IReadOnlyDictionary<string, int> held) =>
        recipe.Recipe
            .Select(r => (r.ItemKey, Lack: r.Count - Have(r.ItemKey, held)))
            .Where(x => x.Lack > 0)
            .Select(x => IsAnyCommon(x.ItemKey, out _) ? $"{x.Lack} {Name(x.ItemKey, x.Lack)}" : $"{x.Lack} × {Name(x.ItemKey, x.Lack)}")
            .ToList();

    public static bool ItemsReady(CosmeticInfo recipe, IReadOnlyDictionary<string, int> held) =>
        recipe.Recipe.All(r => Have(r.ItemKey, held) >= r.Count);

    /// <summary>
    /// The recipes one find completes: not owned, items not ready before it, ready with it. Items
    /// only — cailloux never announce a recipe. Catalog order.
    /// </summary>
    public static IReadOnlyList<CosmeticInfo> NowReady(IReadOnlyDictionary<string, int> before, string foundKey)
    {
        var after = new Dictionary<string, int>(before) { [foundKey] = before.GetValueOrDefault(foundKey) + 1 };
        return CosmeticCatalog.All
            .Where(c => c.Source == CosmeticSource.Crafted && before.GetValueOrDefault(c.Key) == 0
                        && !ItemsReady(c, before) && ItemsReady(c, after))
            .ToList();
    }

    private static string SetName(string set) => ItemCatalog.Sets.FirstOrDefault(s => s.Key == set)?.Name ?? set;
}
