using System.Globalization;
using System.Text;

namespace ProjectSYNCS.Helpers;

// A place of Humeval a Plynling can be sent to forage (docs/village-lore.md § 5). Key is code-only
// and never stored; Name carries its article (« la Sommeille »); Aliases are the normalized typed
// forms (no accents, lower case) — a contract with players, so add rather than rename.
public sealed record ForagePlace(string Key, string Name, string Emoji, IReadOnlyList<string> Aliases);

/// <summary>
/// Where collectibles are found, and the aimed forage. A place belongs to the item: each set has
/// default places and an item may override them, so a new collection only lists where its items
/// are found. Pure; no static field here reads another file's static field (CLAUDE.md), items
/// are resolved at call time.
/// </summary>
public static class ForagePlaces
{
    // How often an aimed forage's collectible comes from the place itself; the rest is today's draw.
    public const double Tilt = 0.75;

    public static readonly IReadOnlyList<ForagePlace> All = new[]
    {
        new ForagePlace("sommeille", "la Sommeille", "🌊", new[] { "sommeille", "riviere", "pont", "pont vieux" }),
        new ForagePlace("lisiere", "la Lisière", "🌲", new[] { "lisiere", "foret", "bois" }),
        new ForagePlace("mouilles", "les Mouilles", "🐸", new[] { "mouilles", "marais", "etang" }),
        new ForagePlace("communal", "le Communal", "🌾", new[] { "communal", "pature" }),
        new ForagePlace("puits", "la place du Puits", "🪣", new[] { "puits", "place", "marche", "village" }),
    };

    // A set's places, unless the item names its own.
    private static readonly Dictionary<string, string[]> SetPlaces = new()
    {
        ["cailloux"] = new[] { "sommeille" },
        ["champignons"] = new[] { "lisiere" },
        ["insectes"] = new[] { "mouilles" },
        ["nature"] = new[] { "communal" },
        ["tresors"] = new[] { "puits" },
    };

    // Items found somewhere else than their set's places (keys without « col. »). Saisons has no
    // set default: each of its items says where it grows.
    private static readonly Dictionary<string, string[]> ItemPlaces = new()
    {
        ["primevere"] = new[] { "communal" },
        ["cerise"] = new[] { "communal" },
        ["coquelicot"] = new[] { "communal" },
        ["coquillage"] = new[] { "sommeille" },
        ["chataigne"] = new[] { "lisiere" },
        ["citrouille"] = new[] { "puits" },
        ["flocon"] = new[] { "communal" },
        ["cristal_givre"] = new[] { "sommeille" },
    };

    public static ForagePlace? ByKey(string? key) => key is null ? null : All.FirstOrDefault(p => p.Key == key);

    /// <summary>Where an item is found; empty for anything that is not a collectible.</summary>
    public static IReadOnlyList<ForagePlace> Of(ItemInfo item)
    {
        if (item.Kind != ItemKind.Collectible) return Array.Empty<ForagePlace>();
        var bare = item.Key.StartsWith("col.", StringComparison.Ordinal) ? item.Key[4..] : item.Key;
        var keys = ItemPlaces.GetValueOrDefault(bare) ?? (item.Set is { } set ? SetPlaces.GetValueOrDefault(set) : null);
        return keys is null ? Array.Empty<ForagePlace>() : keys.Select(k => ByKey(k)!).ToList();
    }

    public static IEnumerable<ItemInfo> ItemsAt(ForagePlace place) => ItemCatalog.Collectibles.Where(i => Of(i).Contains(place));

    /// <summary>« à la Sommeille », « aux Mouilles », « au Communal », « sur la place du Puits ».</summary>
    public static string At(ForagePlace place) => place.Key switch
    {
        "mouilles" => "aux Mouilles",
        "communal" => "au Communal",
        "puits" => "sur la place du Puits",
        _ => "à " + place.Name,
    };

    /// <summary>« à la Sommeille » or « à la Lisière ou au Communal »: every place the items are found at.</summary>
    public static string AtAny(IEnumerable<ItemInfo> items) =>
        string.Join(" ou ", items.SelectMany(Of).Distinct().Select(At));

    /// <summary>Where to look for an item: « rare, à la Sommeille, l'été ».</summary>
    public static string Whereabouts(ItemInfo item)
    {
        var parts = new List<string> { ItemCatalog.RarityLabel(item.Rarity) };
        var at = AtAny(new[] { item });
        if (at.Length > 0) parts.Add(at);
        if (item.Season != Season.None) parts.Add(When(item.Season));
        return string.Join(", ", parts);
    }

    // « au printemps », « l'été », « l'automne », « l'hiver ».
    public static string When(Season season) => season switch
    {
        Season.Spring => "au printemps",
        Season.Summer => "l'été",
        Season.Autumn => "l'automne",
        _ => "l'hiver",
    };

    /// <summary>
    /// A typed place, forgiving: case and accents ignored, a leading article optional; the whole
    /// text may be an alias or the place's name, or its last word an alias (« la place du puits »).
    /// </summary>
    public static ForagePlace? Match(string? text)
    {
        var t = Normalize(text);
        foreach (var article in new[] { "la ", "le ", "les ", "l'", "au ", "aux " })
            if (t.StartsWith(article, StringComparison.Ordinal)) { t = t[article.Length..].TrimStart(); break; }
        if (t.Length == 0) return null;
        var last = t.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1];
        foreach (var elided in new[] { "l'", "d'" })   // « près de l'étang »
            if (last.StartsWith(elided, StringComparison.Ordinal)) last = last[elided.Length..];
        return All.FirstOrDefault(p => p.Aliases.Contains(t) || NameWithoutArticle(p) == t)
               ?? All.FirstOrDefault(p => p.Aliases.Contains(last));
    }

    /// <summary>
    /// A forage's find and where it happened. Not aimed: exactly <see cref="ItemCatalog.DrawForage"/>
    /// (same random numbers, so today's odds stand), then one of the item's places is named — any
    /// place for food. Aimed: the same food chance, then <see cref="Tilt"/> of the collectibles from the
    /// place's findable items, the rest from today's draw.
    /// </summary>
    public static (ItemInfo Item, ForagePlace Place) Draw(Random rng, DateTimeOffset now, ForagePlace? aimed)
    {
        if (aimed is null)
        {
            var item = ItemCatalog.DrawForage(rng, now);
            var places = Of(item);
            return (item, places.Count > 0 ? places[rng.Next(places.Count)] : All[rng.Next(All.Count)]);
        }
        if (rng.NextDouble() < ItemCatalog.ForageFoodChance) return (ItemCatalog.DrawFood(rng), aimed);
        if (rng.NextDouble() < Tilt)
        {
            var here = ItemsAt(aimed).Where(i => ItemCatalog.Findable(i, now)).ToList();
            if (here.Any(i => i.Rarity == ItemRarity.Common)) return (ItemCatalog.DrawAmong(rng, here), aimed);
        }
        return (ItemCatalog.DrawCollectible(rng, now), aimed);
    }

    private static string NameWithoutArticle(ForagePlace p) => Normalize(p.Name) switch
    {
        var n when n.StartsWith("la ", StringComparison.Ordinal) => n[3..],
        var n when n.StartsWith("le ", StringComparison.Ordinal) => n[3..],
        var n when n.StartsWith("les ", StringComparison.Ordinal) => n[4..],
        var n => n,
    };

    // Lower case, no accents, single spaces, typographic apostrophe folded, a hyphen read as a space
    // (« Pont-Vieux »), other punctuation dropped (« marais ! »).
    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var decomposed = text.Trim().ToLowerInvariant().Replace('’', '\'').Replace('-', ' ').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && (c is '\'' or ' ' || char.IsLetterOrDigit(c))) sb.Append(c);
        return string.Join(' ', sb.ToString().Normalize(NormalizationForm.FormC).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
