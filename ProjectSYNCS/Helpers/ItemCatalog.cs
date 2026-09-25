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

    // The Champignons set is found mostly by foraging: 60 % of a forage's collectibles come from
    // it, against 10 % of every other source's — otherwise its 30 items would crowd out the other
    // sets' 32 in every find.
    public const string MushroomSet = "champignons";
    public const double ForageMushroomShare = 0.60;
    public const double MushroomShareElsewhere = 0.10;

    // The other sources. Half a happy gift's wins stay cailloux, the other half bring an item;
    // a won game finds one 1 time in 5; a good visit, 15 % for each owner.
    public const double GiftCaillouxShare = 0.5;
    public const double PlayFindChance = 0.20;
    public const double VisitFindChance = 0.15;

    // How often a person may send their Plynling foraging (kept on their wallet row).
    public static readonly TimeSpan ForageCooldown = TimeSpan.FromHours(4);

    public static readonly IReadOnlyList<CollectionSet> Sets = new[]
    {
        new CollectionSet("cailloux", "🪨", "Cailloux", 100),
        new CollectionSet("nature", "🍂", "Nature", 150),
        new CollectionSet("tresors", "💎", "Trésors", 200),
        new CollectionSet("saisons", "❄️", "Saisons", 300),
        new CollectionSet(MushroomSet, "🍄", "Champignons", 1000),
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

    public static string SeasonLabel(Season season) => season switch
    {
        Season.Spring => "🌸 printemps",
        Season.Summer => "☀️ été",
        Season.Autumn => "🍁 automne",
        Season.Winter => "❄️ hiver",
        _ => "toute l'année",
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
    public static ItemInfo DrawCollectible(Random rng, DateTimeOffset now) =>
        DrawFrom(rng, now, mushroom: rng.NextDouble() < MushroomShareElsewhere);

    // First which half — the Champignons or everything else — then the rarity, then the item.
    private static ItemInfo DrawFrom(Random rng, DateTimeOffset now, bool mushroom)
    {
        var roll = rng.NextDouble();
        var rarity = roll < 0.60 ? ItemRarity.Common : roll < 0.88 ? ItemRarity.Uncommon : roll < 0.98 ? ItemRarity.Rare : ItemRarity.Legendary;
        var half = Collectibles.Where(i => (i.Set == MushroomSet) == mushroom && Findable(i, now)).ToList();
        var pool = half.Where(i => i.Rarity == rarity).ToList();
        if (pool.Count == 0) pool = half.Where(i => i.Rarity == ItemRarity.Common).ToList();
        return pool[rng.Next(pool.Count)];
    }

    // How a set is laid out in the book and the inventory: in one block, or — past 10 items,
    // where 30 lines of emoji markup would overflow an embed field's 1024 — one block per rarity.
    public static IReadOnlyList<(string Label, IReadOnlyList<ItemInfo> Items)> Sections(string setKey)
    {
        var items = InSet(setKey).ToList();
        if (items.Count <= 10) return new[] { ("", (IReadOnlyList<ItemInfo>)items) };
        return items.GroupBy(i => i.Rarity).OrderBy(g => g.Key)
            .Select(g => (RarityPlural(g.Key), (IReadOnlyList<ItemInfo>)g.ToList())).ToList();
    }

    public static string RarityPlural(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => "communs",
        ItemRarity.Uncommon => "peu communs",
        ItemRarity.Rare => "rares",
        _ => "légendaires",
    };

    // The emoji for plain-text places — autocomplete suggestions — where custom emoji markup
    // would show as raw « <:name:id> ». Empty for a custom one.
    public static string TextEmoji(ItemInfo item) => item.Emoji.StartsWith('<') ? "" : item.Emoji + " ";

    /// <summary>What a forage brings back: 1 in 5 a food for the pantry, otherwise a collectible.</summary>
    public static ItemInfo DrawForage(Random rng, DateTimeOffset now)
    {
        if (rng.NextDouble() >= ForageFoodChance) return DrawFrom(rng, now, mushroom: rng.NextDouble() < ForageMushroomShare);
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

        // Champignons — 30, pictured by the bot's own emojis (Helpers/Emotes.Mushrooms.cs, made by
        // tools/mushroom-emojis/upload.py). Names deliberately differ from the four foods
        // (« Lentin du chêne », not « Shiitake »), since autocomplete lists both kinds together.
        Add("champignons", "champignon_paris", Emotes.ShroomChampignonParis, "Champignon de Paris", ItemRarity.Common);
        Add("champignons", "champignon_paille", Emotes.ShroomChampignonPaille, "Champignon de paille", ItemRarity.Common);
        Add("champignons", "enoki", Emotes.ShroomEnoki, "Énoki", ItemRarity.Common);
        Add("champignons", "shimeji", Emotes.ShroomShimeji, "Shimeji", ItemRarity.Common);
        Add("champignons", "lentin_chene", Emotes.ShroomLentinChene, "Lentin du chêne", ItemRarity.Common);
        Add("champignons", "coulemelle", Emotes.ShroomCoulemelle, "Coulemelle", ItemRarity.Common);
        Add("champignons", "nonnette_voilee", Emotes.ShroomNonnetteVoilee, "Nonnette voilée", ItemRarity.Common);
        Add("champignons", "clitocybe", Emotes.ShroomClitocybe, "Clitocybe en entonnoir", ItemRarity.Common);
        Add("champignons", "armillaire", Emotes.ShroomArmillaire, "Armillaire couleur de miel", ItemRarity.Common);
        Add("champignons", "russule_comestible", Emotes.ShroomRussuleComestible, "Russule comestible", ItemRarity.Common);
        Add("champignons", "petit_gris", Emotes.ShroomPetitGris, "Petit-gris", ItemRarity.Common);
        Add("champignons", "coprin_chevelu", Emotes.ShroomCoprinChevelu, "Coprin chevelu", ItemRarity.Common);
        Add("champignons", "lactaire_delicieux", Emotes.ShroomLactaireDelicieux, "Lactaire délicieux", ItemRarity.Uncommon);
        Add("champignons", "bolet_trembles", Emotes.ShroomBoletTrembles, "Bolet des trembles", ItemRarity.Uncommon);
        Add("champignons", "girolle", Emotes.ShroomGirolle, "Girolle", ItemRarity.Uncommon);
        Add("champignons", "russule_doree", Emotes.ShroomRussuleDoree, "Russule dorée", ItemRarity.Uncommon);
        Add("champignons", "polypore_soufre", Emotes.ShroomPolyporeSoufre, "Polypore soufré", ItemRarity.Uncommon);
        Add("champignons", "coprin_encre", Emotes.ShroomCoprinEncre, "Coprin noir d'encre", ItemRarity.Uncommon);
        Add("champignons", "agaric_bohus", Emotes.ShroomAgaricBohus, "Agaric de Bohus", ItemRarity.Uncommon);
        Add("champignons", "pholiote_doree", Emotes.ShroomPholioteDoree, "Pholiote dorée", ItemRarity.Uncommon);
        Add("champignons", "gomphide", Emotes.ShroomGomphide, "Gomphide glutineux", ItemRarity.Uncommon);
        Add("champignons", "cepe_bordeaux", Emotes.ShroomCepeBordeaux, "Cèpe de Bordeaux", ItemRarity.Rare);
        Add("champignons", "trompette_mort", Emotes.ShroomTrompetteMort, "Trompette de la mort", ItemRarity.Rare);
        Add("champignons", "morille_conique", Emotes.ShroomMorilleConique, "Morille conique", ItemRarity.Rare, Season.Spring);
        Add("champignons", "lactaire_indigo", Emotes.ShroomLactaireIndigo, "Lactaire indigo", ItemRarity.Rare);
        Add("champignons", "champignon_homard", Emotes.ShroomChampignonHomard, "Champignon homard", ItemRarity.Rare);
        Add("champignons", "maitake", Emotes.ShroomMaitake, "Maitake", ItemRarity.Rare);
        Add("champignons", "truffe_noire", Emotes.ShroomTruffeNoire, "Truffe noire", ItemRarity.Legendary, Season.Winter);
        Add("champignons", "oronge", Emotes.ShroomOronge, "Oronge", ItemRarity.Legendary);
        Add("champignons", "matsutake", Emotes.ShroomMatsutake, "Matsutaké", ItemRarity.Legendary);
        return items;
    }
}
