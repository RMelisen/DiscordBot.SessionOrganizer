using System.Globalization;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The four things a Plynling can wear, one of each.
public enum CosmeticSlot { Theme, Title, Accessory, Grave }

// Where a cosmetic comes from: always in the shop, in the weekly rotation, in its season, or
// made from collectibles.
public enum CosmeticSource { Basic, Rotating, Seasonal, Crafted }

// One cosmetic. It is also an ItemInfo (ItemKind.Cosmetic) in ItemCatalog, which is what the
// inventory stores; this record carries what only the slot needs. Name is the masculine title,
// the accessory with its article (« une écharpe »), or the thème / cadre name; NameF only exists
// for titles. Price is the shop price — or, for a crafted one, the cailloux its recipe adds.
public sealed record CosmeticInfo(
    string Key, CosmeticSlot Slot, CosmeticSource Source, ItemRarity Rarity, Season Season,
    string Emoji, string Name, string? NameF, uint? Accent, string? Banner, string? GraveLeft, string? GraveRight,
    long Price, IReadOnlyList<(string ItemKey, int Count)> Recipe);

/// <summary>
/// Every cosmetic, and the weekly shop. Keys are stored on inventory rows and on the Plynlings
/// wearing them, so they are append-only like every item key. Pure: the shop is a function of
/// the instant, identical for everyone and nothing stored.
/// </summary>
public static class CosmeticCatalog
{
    // How many rotating items per slot the shop shows each week.
    public const int RotatingPerSlot = 2;

    public static readonly IReadOnlyList<CosmeticInfo> All = Build();

    private static readonly Dictionary<string, CosmeticInfo> ByKeyMap = All.ToDictionary(c => c.Key);

    public static CosmeticInfo? ByKey(string? key) => key is null ? null : ByKeyMap.GetValueOrDefault(key);

    public static IEnumerable<CosmeticInfo> InSlot(CosmeticSlot slot) => All.Where(c => c.Slot == slot);

    public static string SlotLabel(CosmeticSlot slot) => slot switch
    {
        CosmeticSlot.Theme => "Thème",
        CosmeticSlot.Title => "Titre",
        CosmeticSlot.Accessory => "Accessoire",
        _ => "Cadre de tombe",
    };

    public static string TitleFor(CosmeticInfo title, PlynlingGender gender) =>
        gender == PlynlingGender.Female && title.NameF is { } f ? f : title.Name;

    // The name the inventory, gifts and trades print.
    public static string Label(CosmeticInfo c) => c.Slot switch
    {
        CosmeticSlot.Theme => $"Thème {c.Name}",
        CosmeticSlot.Title => $"Titre « {c.Name} / {c.NameF} »",
        CosmeticSlot.Accessory => Capitalize(WithoutArticle(c.Name)),
        _ => $"Cadre {c.Name}",
    };

    private static string WithoutArticle(string name)
    {
        foreach (var article in new[] { "un ", "une ", "des " })
            if (name.StartsWith(article, StringComparison.Ordinal)) return name[article.Length..];
        return name;
    }

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpper(s[0], CultureInfo.GetCultureInfo("fr-FR")) + s[1..];

    // ---- the weekly shop ----------------------------------------------------------------------

    /// <summary>The Paris ISO week as <c>yyyyww</c>: it turns at Monday 00:00 Paris.</summary>
    public static int WeekKey(DateTimeOffset now)
    {
        var local = AppTime.ToZoned(now).DateTime;
        return ISOWeek.GetYear(local) * 100 + ISOWeek.GetWeekOfYear(local);
    }

    /// <summary>
    /// What is on sale at <paramref name="now"/>: the basics, then <see cref="RotatingPerSlot"/>
    /// rotating items per slot drawn from a generator seeded with the week and the slot — the
    /// same for everyone, all week — then the season's items. Crafted items are never on sale.
    /// </summary>
    public static IReadOnlyList<CosmeticInfo> Shop(DateTimeOffset now)
    {
        var week = WeekKey(now);
        var shop = All.Where(c => c.Source == CosmeticSource.Basic).ToList();
        foreach (var slot in Enum.GetValues<CosmeticSlot>())
        {
            var pool = All.Where(c => c.Slot == slot && c.Source == CosmeticSource.Rotating).ToList();
            var rng = new Random(week * 10 + (int)slot);
            for (var i = 0; i < Math.Min(RotatingPerSlot, pool.Count); i++)   // a partial Fisher–Yates
            {
                var j = rng.Next(i, pool.Count);
                (pool[i], pool[j]) = (pool[j], pool[i]);
                shop.Add(pool[i]);
            }
        }
        var season = ItemCatalog.SeasonAt(now);
        shop.AddRange(All.Where(c => c.Source == CosmeticSource.Seasonal && c.Season == season).OrderBy(c => c.Slot));
        return shop;
    }

    public static long RotatingPrice(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => 600,
        ItemRarity.Uncommon => 1200,
        ItemRarity.Rare => 2400,
        _ => 5000,
    };

    public const long SeasonalPrice = 2400;

    // ---- the catalog --------------------------------------------------------------------------

    private static List<CosmeticInfo> Build()
    {
        var list = new List<CosmeticInfo>();
        var none = Array.Empty<(string, int)>();
        const ItemRarity C = ItemRarity.Common, U = ItemRarity.Uncommon, R = ItemRarity.Rare, L = ItemRarity.Legendary;

        long PriceOf(CosmeticSlot slot, CosmeticSource source, ItemRarity rarity) => source switch
        {
            CosmeticSource.Basic => slot is CosmeticSlot.Title or CosmeticSlot.Accessory ? 300 : 500,
            CosmeticSource.Seasonal => SeasonalPrice,
            _ => RotatingPrice(rarity),
        };

        void Theme(string key, string name, string emoji, uint accent, CosmeticSource source, ItemRarity rarity = C,
            Season season = Season.None, long craftPrice = 0, (string, int)[]? recipe = null) =>
            list.Add(new CosmeticInfo($"cos.theme.{key}", CosmeticSlot.Theme, source, rarity, season, emoji, name, null,
                accent, string.Join(" · ", Enumerable.Repeat(emoji, 5)), null, null,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Theme, source, rarity), recipe ?? none));

        void Title(string key, string m, string f, CosmeticSource source, ItemRarity rarity = C, long craftPrice = 0, (string, int)[]? recipe = null) =>
            list.Add(new CosmeticInfo($"cos.title.{key}", CosmeticSlot.Title, source, rarity, Season.None, "🏷️", m, f,
                null, null, null, null,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Title, source, rarity), recipe ?? none));

        void Accessory(string key, string emoji, string name, CosmeticSource source, ItemRarity rarity = C,
            Season season = Season.None, long craftPrice = 0, (string, int)[]? recipe = null) =>
            list.Add(new CosmeticInfo($"cos.accessory.{key}", CosmeticSlot.Accessory, source, rarity, season, emoji, name, null,
                null, null, null, null,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Accessory, source, rarity), recipe ?? none));

        void Grave(string key, string emoji, string name, CosmeticSource source, ItemRarity rarity = C, long craftPrice = 0, (string, int)[]? recipe = null) =>
            list.Add(new CosmeticInfo($"cos.grave.{key}", CosmeticSlot.Grave, source, rarity, Season.None, emoji, name, null,
                null, null, $"{emoji} {emoji}", $"{emoji} {emoji}",
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Grave, source, rarity), recipe ?? none));

        const CosmeticSource B = CosmeticSource.Basic, Rot = CosmeticSource.Rotating, S = CosmeticSource.Seasonal, Cr = CosmeticSource.Crafted;

        // Thèmes
        Theme("prairie", "Prairie", "🌿", 0x6AB04C, B);
        Theme("aurore", "Aurore", "🌸", 0xF4A7C0, Rot, C);
        Theme("ocean", "Océan", "🌊", 0x2E86DE, Rot, C);
        Theme("braises", "Braises", "🔥", 0xE8663C, Rot, C);
        Theme("foret", "Forêt", "🌲", 0x2D6A3E, Rot, C);
        Theme("lavande", "Lavande", "💜", 0x9B7FD4, Rot, U);
        Theme("desert", "Désert", "🌵", 0xD9B77E, Rot, U);
        Theme("arc_en_ciel", "Arc-en-ciel", "🌈", 0xFF9FF3, Rot, U);
        Theme("nuit_etoilee", "Nuit étoilée", "✨", 0x1B2A5A, Rot, R);
        Theme("aurore_boreale", "Aurore boréale", "🌌", 0x2BB5A8, Rot, R);
        Theme("royal", "Royal", "👑", 0xE6B422, Rot, L);
        Theme("cerisiers", "Cerisiers", "🍒", 0xF7C5D5, S, season: Season.Spring);
        Theme("plage", "Plage", "🏖️", 0xF3D9A4, S, season: Season.Summer);
        Theme("feuilles_mortes", "Feuilles mortes", "🍂", 0xC06A2B, S, season: Season.Autumn);
        Theme("flocons", "Flocons", "❄️", 0xBFE3F5, S, season: Season.Winter);
        Theme("champignonniere", "Champignonnière", "🍄", 0xB5523B, Cr, craftPrice: 300,
            recipe: new[] { ("col.girolle", 3), ("col.coprin_chevelu", 3), ("col.cepe_bordeaux", 2) });
        Theme("caverne_tresors", "Caverne aux trésors", "💎", 0x5B3F8C, Cr, craftPrice: 300,
            recipe: new[] { ("col.quartz", 3), ("col.amethyste", 2), ("col.opale", 1) });

        // Titres
        Title("petit", "Le Petit", "La Petite", B);
        Title("gourmet", "Le Gourmet", "La Gourmande", Rot, C);
        Title("reveur", "Le Rêveur", "La Rêveuse", Rot, C);
        Title("curieux", "Le Curieux", "La Curieuse", Rot, C);
        Title("dormeur", "Le Dormeur", "La Dormeuse", Rot, C);
        Title("mysterieux", "Le Mystérieux", "La Mystérieuse", Rot, U);
        Title("farceur", "Le Farceur", "La Farceuse", Rot, U);
        Title("aventurier", "L'Aventurier", "L'Aventurière", Rot, U);
        Title("champion", "Le Champion", "La Championne", Rot, R);
        Title("sage", "Le Sage", "La Sage", Rot, R);
        Title("legendaire", "Le Légendaire", "La Légendaire", Rot, L);
        Title("chercheur_tresors", "Le Chercheur de trésors", "La Chercheuse de trésors", Cr, craftPrice: 200,
            recipe: new[] { ("col.carte_tresor", 1), ("col.boussole", 1), ("col.piece_ancienne", 3) });
        Title("botaniste", "Le Botaniste", "La Botaniste", Cr, craftPrice: 200,
            recipe: new[] { ("col.feuille_chene", 3), ("col.trefle", 3), ("col.trefle_quatre", 1) });

        // Accessoires
        Accessory("noeud", "🎀", "un nœud", B);
        Accessory("echarpe", "🧣", "une écharpe", Rot, C);
        Accessory("fleur", "🌼", "une fleur", Rot, C);
        Accessory("casquette", "🧢", "une casquette", Rot, C);
        Accessory("ballon", "🎈", "un ballon", Rot, C);
        Accessory("lunettes", "🕶️", "des lunettes de soleil", Rot, U);
        Accessory("sac_a_dos", "🎒", "un petit sac à dos", Rot, U);
        Accessory("casque_audio", "🎧", "un casque audio", Rot, U);
        Accessory("haut_de_forme", "🎩", "un haut-de-forme", Rot, R);
        Accessory("baguette", "🪄", "une baguette magique", Rot, R);
        Accessory("couronne", "👑", "une petite couronne", Rot, L);
        Accessory("tulipe", "🌷", "une tulipe", S, season: Season.Spring);
        Accessory("glace", "🍦", "une glace", S, season: Season.Summer);
        Accessory("lanterne_citrouille", "🎃", "une lanterne citrouille", S, season: Season.Autumn);
        Accessory("moufles", "🧤", "des moufles", S, season: Season.Winter);
        // « plume d'or », not « plume dorée »: that is already the collectible it is made from.
        Accessory("plume_doree", "🪶", "une plume d'or", Cr, craftPrice: 200,
            recipe: new[] { ("col.plume", 5), ("col.plume_doree", 1) });
        Accessory("collier_coquillages", "🐚", "un collier de coquillages", Cr, craftPrice: 200,
            recipe: new[] { ("col.coquillage", 3), ("col.galet", 3) });

        // Cadres de tombe
        Grave("bougies", "🕯️", "Bougies", B);
        Grave("couronne_fleurs", "💐", "Couronne de fleurs", Rot, C);
        Grave("feuilles", "🍂", "Feuilles", Rot, C);
        Grave("galets", "🪨", "Galets", Rot, C);
        Grave("ble", "🌾", "Blé", Rot, C);
        Grave("etoiles", "⭐", "Étoiles", Rot, U);
        Grave("colombes", "🕊️", "Colombes", Rot, U);
        Grave("papillons", "🦋", "Papillons", Rot, U);
        Grave("clair_de_lune", "🌙", "Clair de lune", Rot, R);
        Grave("lanternes", "🏮", "Lanternes", Rot, R);
        Grave("aura_doree", "✨", "Aura dorée", Rot, L);
        Grave("cristaux", "💎", "Cristaux", Cr, craftPrice: 200,
            recipe: new[] { ("col.cristal_givre", 2), ("col.flocon", 3) });
        Grave("cercle_fees", "🍄", "Cercle de fées", Cr, craftPrice: 300,
            recipe: new[] { ("col.russule_doree", 3), ("col.trompette_mort", 2), ("col.lactaire_indigo", 2) });

        return list;
    }
}
