namespace ProjectSYNCS.Helpers;

public enum ItemKind { Food, Collectible }

public enum ItemRarity { Common, Uncommon, Rare, Legendary }

public enum Season { None, Spring, Summer, Autumn, Winter }

// One thing a person can hold. The key is stored on InventoryItem rows, so it is **stable**:
// renaming one orphans every copy already held. Set is the collection it belongs to (null for
// foods); Season restricts when it can be found (None: all year).
public sealed record ItemInfo(
    string Key, ItemKind Kind, string Emoji, string Name, string? Set, ItemRarity Rarity, Season Season, PlynlingFood? Food);

public sealed record CollectionSet(string Key, string Emoji, string Name, long Reward);

/// <summary>
/// Every item there is — foods and collectibles; cosmetics would be a third kind — and the
/// draws that find them. Pure: every draw takes the <see cref="Random"/> it should use and the
/// instant it happens (seasons are read on the Paris calendar).
/// </summary>
public static class ItemCatalog
{
    public const double ForageFoodChance = 0.20;

    public static readonly IReadOnlyList<CollectionSet> Sets = new[]
    {
        new CollectionSet("cailloux", "🪨", "Cailloux", 100),
        new CollectionSet("nature", "🍂", "Nature", 150),
        new CollectionSet("tresors", "💎", "Trésors", 200),
        new CollectionSet("saisons", "❄️", "Saisons", 300),
    };

    public static readonly IReadOnlyList<ItemInfo> All = BuildAll();

    private static readonly Dictionary<string, ItemInfo> ByKeyMap = All.ToDictionary(i => i.Key);

    public static IEnumerable<ItemInfo> Collectibles => All.Where(i => i.Kind == ItemKind.Collectible);

    public static ItemInfo? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    public static string FoodKey(PlynlingFood food) => $"food.{food.ToString().ToLowerInvariant()}";

    public static IEnumerable<ItemInfo> InSet(string setKey) => All.Where(i => i.Set == setKey);

    public static long SellPrice(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => 2,
        ItemRarity.Uncommon => 5,
        ItemRarity.Rare => 15,
        _ => 50,
    };

    public static string RarityLabel(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => "commun",
        ItemRarity.Uncommon => "peu commun",
        ItemRarity.Rare => "rare",
        _ => "légendaire",
    };

    // The shop's price for `quantity` of a food: the menu price each, 10 % off from 5.
    public static long ShopPrice(FoodInfo food, int quantity)
    {
        var full = food.Price * quantity;
        return quantity >= 5 ? (long)Math.Round(full * 0.9, MidpointRounding.AwayFromZero) : full;
    }

    // Paris calendar: spring March–May, summer June–August, autumn September–November, winter the rest.
    public static Season SeasonAt(DateTimeOffset now) => AppTime.ToZoned(now).Month switch
    {
        3 or 4 or 5 => Season.Spring,
        6 or 7 or 8 => Season.Summer,
        9 or 10 or 11 => Season.Autumn,
        _ => Season.Winter,
    };

    public static bool Findable(ItemInfo item, DateTimeOffset now) =>
        item.Kind == ItemKind.Collectible && (item.Season == Season.None || item.Season == SeasonAt(now));

    /// <summary>
    /// A collectible find: a rarity (commun 60 %, peu commun 28 %, rare 10 %, légendaire 2 %),
    /// then an item of that rarity among what is findable now — a rarity with nothing findable
    /// falls back to commun.
    /// </summary>
    public static ItemInfo DrawCollectible(Random rng, DateTimeOffset now)
    {
        var roll = rng.NextDouble();
        var rarity = roll < 0.60 ? ItemRarity.Common : roll < 0.88 ? ItemRarity.Uncommon : roll < 0.98 ? ItemRarity.Rare : ItemRarity.Legendary;
        var pool = Collectibles.Where(i => i.Rarity == rarity && Findable(i, now)).ToList();
        if (pool.Count == 0) pool = Collectibles.Where(i => i.Rarity == ItemRarity.Common && Findable(i, now)).ToList();
        return pool[rng.Next(pool.Count)];
    }

    /// <summary>What a forage brings back: 1 in 5 a food for the pantry, otherwise a collectible.</summary>
    public static ItemInfo DrawForage(Random rng, DateTimeOffset now)
    {
        if (rng.NextDouble() >= ForageFoodChance) return DrawCollectible(rng, now);
        var roll = rng.NextDouble();
        var food = roll < 0.60 ? PlynlingFood.Mushroom : roll < 0.85 ? PlynlingFood.Shiitake : roll < 0.95 ? PlynlingFood.Morel : PlynlingFood.Truffle;
        return ByKey(FoodKey(food))!;
    }

    private static List<ItemInfo> BuildAll()
    {
        var items = PlynlingCatalog.Foods
            .Select(f => new ItemInfo(FoodKey(f.Food), ItemKind.Food, "🍄", f.Name, null, ItemRarity.Common, Season.None, f.Food))
            .ToList();

        void Add(string set, string key, string emoji, string name, ItemRarity rarity, Season season = Season.None) =>
            items.Add(new ItemInfo($"col.{key}", ItemKind.Collectible, emoji, name, set, rarity, season, null));

        Add("cailloux", "galet", "🪨", "Galet", ItemRarity.Common);
        Add("cailloux", "caillou_plat", "🥏", "Caillou plat", ItemRarity.Common);
        Add("cailloux", "silex", "🔪", "Silex", ItemRarity.Common);
        Add("cailloux", "quartz", "🔷", "Quartz", ItemRarity.Uncommon);
        Add("cailloux", "agate", "🟠", "Agate", ItemRarity.Uncommon);
        Add("cailloux", "amethyste", "🟣", "Améthyste", ItemRarity.Rare);
        Add("cailloux", "opale", "🌈", "Opale", ItemRarity.Rare);
        Add("cailloux", "meteorite", "☄️", "Météorite", ItemRarity.Legendary);

        Add("nature", "feuille_chene", "🍂", "Feuille de chêne", ItemRarity.Common);
        Add("nature", "gland", "🌰", "Gland", ItemRarity.Common);
        Add("nature", "plume", "🪶", "Plume", ItemRarity.Common);
        Add("nature", "trefle", "☘️", "Trèfle", ItemRarity.Common);
        Add("nature", "pomme_pin", "🌲", "Pomme de pin", ItemRarity.Uncommon);
        Add("nature", "coquille_escargot", "🐌", "Coquille d'escargot", ItemRarity.Uncommon);
        Add("nature", "trefle_quatre", "🍀", "Trèfle à quatre feuilles", ItemRarity.Rare);
        Add("nature", "plume_doree", "✨", "Plume dorée", ItemRarity.Legendary);

        Add("tresors", "bouton", "🔘", "Bouton", ItemRarity.Common);
        Add("tresors", "bille", "🔮", "Bille", ItemRarity.Common);
        Add("tresors", "cle_rouillee", "🗝️", "Clé rouillée", ItemRarity.Common);
        Add("tresors", "piece_ancienne", "🪙", "Pièce ancienne", ItemRarity.Uncommon);
        Add("tresors", "bague", "💍", "Bague", ItemRarity.Uncommon);
        Add("tresors", "boussole", "🧭", "Boussole", ItemRarity.Rare);
        Add("tresors", "carte_tresor", "🗺️", "Carte au trésor", ItemRarity.Rare);
        Add("tresors", "couronne", "👑", "Couronne", ItemRarity.Legendary);

        Add("saisons", "primevere", "🌼", "Primevère", ItemRarity.Common, Season.Spring);
        Add("saisons", "cerise", "🍒", "Cerise", ItemRarity.Rare, Season.Spring);
        Add("saisons", "coquelicot", "🌺", "Coquelicot", ItemRarity.Common, Season.Summer);
        Add("saisons", "coquillage", "🐚", "Coquillage", ItemRarity.Rare, Season.Summer);
        Add("saisons", "chataigne", "🌰", "Châtaigne", ItemRarity.Common, Season.Autumn);
        Add("saisons", "citrouille", "🎃", "Citrouille", ItemRarity.Rare, Season.Autumn);
        Add("saisons", "flocon", "❄️", "Flocon", ItemRarity.Common, Season.Winter);
        Add("saisons", "cristal_givre", "💎", "Cristal de givre", ItemRarity.Rare, Season.Winter);
        return items;
    }
}
