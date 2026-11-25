using System.Globalization;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The five things a Plynling can wear, one of each. Append-only: the weekly shop seeds its draw on
// the slot's number, so a slot inserted mid-list would reshuffle every week's selection. The order
// players see is CosmeticCatalog.SlotOrder.
public enum CosmeticSlot { Theme, Title, Accessory, Grave, Aura }

// Where a cosmetic comes from: always in the shop, in the weekly rotation, in its season, or
// made from collectibles.
public enum CosmeticSource { Basic, Rotating, Seasonal, Crafted }

// One cosmetic. It is also an ItemInfo (ItemKind.Cosmetic) in ItemCatalog, which is what the
// inventory stores; this record carries what only the slot needs. Name is the masculine title,
// the accessory with its article (« une écharpe »), or the thème / cadre name; NameF only exists
// for titles. Price is the shop price — or, for a crafted one, the cailloux its recipe adds.
// A recipe ingredient is a collectible key or « any commons of a set » (Recipes.AnyCommon).
// Trophy splits crafted cosmetics into « Chefs-d'œuvre » (aimed hunts) and « Ouvrages » (duplicates).
public sealed record CosmeticInfo(
    string Key, CosmeticSlot Slot, CosmeticSource Source, ItemRarity Rarity, Season Season,
    string DefaultEmoji, string Name, string? NameF, uint? Accent,
    long Price, IReadOnlyList<(string ItemKey, int Count)> Recipe, bool Trophy = false)
{
    // Its icon once uploaded (ItemEmojis), else its Unicode. The thème's banner and the cadre's two
    // sides are made of it, so they are read at display time and picture the icon too.
    public string Emoji => ItemEmojis.For(Key) ?? DefaultEmoji;
    public string? Banner => Slot == CosmeticSlot.Theme ? string.Join(" · ", Enumerable.Repeat(Emoji, 5)) : null;
    public string? GraveLeft => Slot == CosmeticSlot.Grave ? $"{Emoji} {Emoji}" : null;
    public string? GraveRight => GraveLeft;
}

// Reads and writes the slot columns on Plynling by slot, so nothing switches on the property
// names but here.
public static class CosmeticSlots
{
    public static string? Get(Plynling p, CosmeticSlot slot) => slot switch
    {
        CosmeticSlot.Theme => p.ThemeKey,
        CosmeticSlot.Title => p.TitleKey,
        CosmeticSlot.Accessory => Accessories(p).FirstOrDefault()?.Key,        // the first accessory
        CosmeticSlot.Aura => p.AuraKey,
        _ => p.GraveKey,
    };

    // Single-slot writes. For the accessory it is the first picker (AccessoryFit.Pick), so one
    // item's style is never carried onto another: the outgoing one's is remembered, the incoming
    // one's restored (null, so its default, when none is).
    public static void Set(Plynling p, CosmeticSlot slot, string? key)
    {
        switch (slot)
        {
            case CosmeticSlot.Theme: p.ThemeKey = key; break;
            case CosmeticSlot.Title: p.TitleKey = key; break;
            case CosmeticSlot.Accessory:
                var saved = SavedStyles.Parse(p.SavedAccessoryStyles);
                Apply(p, AccessoryFit.Pick(Accessories(p), 0, key is null ? null : new WornAccessory(key, saved.GetValueOrDefault(key))));
                break;
            case CosmeticSlot.Aura: p.AuraKey = key; break;
            default: p.GraveKey = key; break;
        }
    }

    /// <summary>
    /// The worn accessories as stored, in slot order with the gaps closed (a lone second reads as the
    /// first). Retired keys are kept: only the catalogue lookups ignore them.
    /// </summary>
    public static IReadOnlyList<WornAccessory> Accessories(Plynling p) =>
        new[] { (p.AccessoryKey, p.AccessoryStyle), (p.Accessory2Key, p.Accessory2Style), (p.Accessory3Key, p.Accessory3Style) }
            .Where(c => c.Item1 is not null)
            .Select(c => new WornAccessory(c.Item1!, c.Item2))
            .ToList();

    /// <summary>
    /// Writes every accessory slot from a change, and remembers the latest style of every item that
    /// was or is worn (taken off, moved, restyled), so putting one back restores it.
    /// </summary>
    public static void Apply(Plynling p, AccessoryChange change)
    {
        var saved = SavedStyles.Parse(p.SavedAccessoryStyles).ToDictionary(kv => kv.Key, kv => kv.Value);
        foreach (var item in Accessories(p).Concat(change.Worn))    // the new ones last: they win
            if (item is { Style: { } style }) saved[item.Key] = style;
        p.AccessoryKey = change.At(0)?.Key;
        p.AccessoryStyle = change.At(0)?.Style;
        p.Accessory2Key = change.At(1)?.Key;
        p.Accessory2Style = change.At(1)?.Style;
        p.Accessory3Key = change.At(2)?.Key;
        p.Accessory3Style = change.At(2)?.Style;
        p.SavedAccessoryStyles = SavedStyles.Format(saved);
    }

    /// <summary>Takes the accessory <paramref name="key"/> off wherever it is worn, the later ones moving up. Nothing if it is not worn.</summary>
    public static void TakeOff(Plynling p, string key)
    {
        var worn = Accessories(p);
        var at = worn.ToList().FindIndex(w => w.Key == key);
        if (at >= 0) Apply(p, AccessoryFit.Pick(worn, at, null));
    }

    // What it wears, slot by slot, as catalog entries (an unknown key reads as nothing).
    public static CosmeticInfo? Worn(Plynling p, CosmeticSlot slot) => CosmeticCatalog.ByKey(Get(p, slot));
}

/// <summary>
/// Every cosmetic, and the weekly shop. Keys are stored on inventory rows and on the Plynlings
/// wearing them, so they are append-only like every item key. Pure: the shop is a function of
/// the instant, identical for everyone and nothing stored.
/// </summary>
public static class CosmeticCatalog
{
    // How many rotating items per slot the shop shows each week.
    public const int RotatingPerSlot = 2;

    // The order players see the slots in (the Garde-robe, the inventory): the aura beside the
    // accessory it is worn with, the cadre last since it only shows on a grave.
    public static readonly IReadOnlyList<CosmeticSlot> SlotOrder = new[]
    {
        CosmeticSlot.Theme, CosmeticSlot.Title, CosmeticSlot.Accessory, CosmeticSlot.Aura, CosmeticSlot.Grave,
    };

    public static readonly IReadOnlyList<CosmeticInfo> All = Build();

    private static readonly Dictionary<string, CosmeticInfo> ByKeyMap = All.ToDictionary(c => c.Key);

    public static CosmeticInfo? ByKey(string? key) => key is null ? null : ByKeyMap.GetValueOrDefault(key);

    public static IEnumerable<CosmeticInfo> InSlot(CosmeticSlot slot) => All.Where(c => c.Slot == slot);

    public static string SlotLabel(CosmeticSlot slot) => slot switch
    {
        CosmeticSlot.Theme => "Thème",
        CosmeticSlot.Title => "Titre",
        CosmeticSlot.Accessory => "Accessoire",
        CosmeticSlot.Aura => "Aura",
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
        CosmeticSlot.Aura => $"Aura {c.Name}",
        _ => $"Cadre {c.Name}",
    };

    // The short name for a list — the Garde-robe: the thème or cadre name, the masculine title
    // (text about a person, not a Plynling, stays in the generic masculine), the accessory bare.
    public static string ShortName(CosmeticInfo c) => c.Slot == CosmeticSlot.Accessory ? Capitalize(WithoutArticle(c.Name)) : c.Name;

    public static string SlotPlural(CosmeticSlot slot) => slot switch
    {
        CosmeticSlot.Theme => "thèmes",
        CosmeticSlot.Title => "titres",
        CosmeticSlot.Accessory => "accessoires",
        CosmeticSlot.Aura => "auras",
        _ => "cadres",
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

    /// <summary>When the shop next changes: the coming Monday 00:00 Paris.</summary>
    public static DateTimeOffset NextRotation(DateTimeOffset now)
    {
        var today = AppTime.ToZoned(now).Date;
        var days = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        var monday = today.AddDays(days == 0 ? 7 : days);
        return new DateTimeOffset(monday, AppTime.Zone.GetUtcOffset(monday));
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
            Season season = Season.None, long craftPrice = 0, (string, int)[]? recipe = null, bool trophy = false) =>
            list.Add(new CosmeticInfo($"cos.theme.{key}", CosmeticSlot.Theme, source, rarity, season, emoji, name, null,
                accent,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Theme, source, rarity), recipe ?? none, trophy));

        void Title(string key, string m, string f, CosmeticSource source, ItemRarity rarity = C, long craftPrice = 0, (string, int)[]? recipe = null,
            bool trophy = false, Season season = Season.None) =>
            list.Add(new CosmeticInfo($"cos.title.{key}", CosmeticSlot.Title, source, rarity, season, "🏷️", m, f,
                null,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Title, source, rarity), recipe ?? none, trophy));

        void Accessory(string key, string emoji, string name, CosmeticSource source, ItemRarity rarity = C,
            Season season = Season.None, long craftPrice = 0, (string, int)[]? recipe = null, bool trophy = false) =>
            list.Add(new CosmeticInfo($"cos.accessory.{key}", CosmeticSlot.Accessory, source, rarity, season, emoji, name, null,
                null,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Accessory, source, rarity), recipe ?? none, trophy));

        void Grave(string key, string emoji, string name, CosmeticSource source, ItemRarity rarity = C, long craftPrice = 0, (string, int)[]? recipe = null,
            bool trophy = false, Season season = Season.None) =>
            list.Add(new CosmeticInfo($"cos.grave.{key}", CosmeticSlot.Grave, source, rarity, season, emoji, name, null,
                null,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Grave, source, rarity), recipe ?? none, trophy));

        // An aura's Name is bare and plural (« Bulles »): the card says « Entouré de bulles ».
        void Aura(string key, string emoji, string name, CosmeticSource source, ItemRarity rarity = C, long craftPrice = 0,
            (string, int)[]? recipe = null, bool trophy = false, Season season = Season.None) =>
            list.Add(new CosmeticInfo($"cos.aura.{key}", CosmeticSlot.Aura, source, rarity, season, emoji, name, null,
                null,
                source == CosmeticSource.Crafted ? craftPrice : PriceOf(CosmeticSlot.Aura, source, rarity), recipe ?? none, trophy));

        // « Any commons of a set » ingredient (Recipes): any common mushroom, pebble…
        static (string, int) Any(string set, int count) => (Recipes.AnyCommon(set), count);

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
        // Crafted. Trophies (« Chefs-d'œuvre ») are aimed hunts of 60–150 forages, Ouvrages 10–25 from one
        // place's commons; the checks (section 61) measure every recipe against its band.
        Theme("champignonniere", "Champignonnière", "🍄", 0xB5523B, Cr, craftPrice: 300, trophy: true,
            recipe: new[] { ("col.girolle", 1), ("col.coprin_chevelu", 2), ("col.cepe_bordeaux", 1) });
        Theme("caverne_tresors", "Caverne aux trésors", "💎", 0x5B3F8C, Cr, craftPrice: 300, trophy: true,
            recipe: new[] { ("col.quartz", 3), ("col.amethyste", 2), ("col.opale", 1) });
        Theme("riviere", "Rivière", "🏞️", 0x4A90B8, Cr, craftPrice: 100,
            recipe: new[] { ("col.galet", 2), ("col.caillou_plat", 1), ("col.quartz", 1) });
        Theme("sous_bois", "Sous-bois", "🌳", 0x5B6E3A, Cr, craftPrice: 100,
            recipe: new[] { Any("champignons", 6) });

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
        Title("chercheur_tresors", "Le Chercheur de trésors", "La Chercheuse de trésors", Cr, craftPrice: 200, trophy: true,
            recipe: new[] { ("col.carte_tresor", 1), ("col.boussole", 1), ("col.piece_ancienne", 5) });
        Title("botaniste", "Le Botaniste", "La Botaniste", Cr, craftPrice: 200, trophy: true,
            recipe: new[] { ("col.feuille_chene", 3), ("col.trefle", 3), ("col.trefle_quatre", 2) });
        Title("naturaliste", "Le Naturaliste", "La Naturaliste", Cr, craftPrice: 300, trophy: true,
            recipe: new[] { ("col.morpho_bleu", 1), ("col.coccinelle", 2), ("col.chenille", 2) });
        Title("glaneur", "Le Glaneur", "La Glaneuse", Cr, craftPrice: 100,
            recipe: new[] { Any("nature", 5) });
        Title("brocanteur", "Le Brocanteur", "La Brocanteuse", Cr, craftPrice: 100,
            recipe: new[] { ("col.bouton", 2), ("col.bille", 2), ("col.cle_rouillee", 1) });
        Title("grognon", "Le Grognon", "La Grognonne", Rot, C);
        Title("calin", "Le Câlin", "La Câline", Rot, C);
        Title("distrait", "Le Distrait", "La Distraite", Rot, C);
        Title("bavard", "Le Bavard", "La Bavarde", Rot, U);
        Title("timide", "Le Timide", "La Timide", Rot, U);
        Title("fripon", "Le Fripon", "La Friponne", Rot, R);
        Title("immortel", "L'Immortel", "L'Immortelle", Rot, L);
        Title("frileux", "Le Frileux", "La Frileuse", S, season: Season.Winter);
        Title("bronze", "Le Bronzé", "La Bronzée", S, season: Season.Summer);
        // One Ouvrage per place of Humeval it is named after.
        Title("sommeillant", "Le Sommeillant", "La Sommeillante", Cr, craftPrice: 100,
            recipe: new[] { ("col.silex", 2), ("col.galet", 2), ("col.agate", 1) });
        Title("mouille", "Le Mouillé", "La Mouillée", Cr, craftPrice: 100,
            recipe: new[] { ("col.mouche", 2), ("col.moustique", 2), ("col.chenille", 1) });
        // Coins thrown down the well.
        Title("puisatier", "Le Puisatier", "La Puisatière", Cr, craftPrice: 100,
            recipe: new[] { ("col.cle_rouillee", 2), ("col.bille", 2), ("col.piece_ancienne", 1) });
        Title("mycologue", "Le Mycologue", "La Mycologue", Cr, craftPrice: 300, trophy: true,
            recipe: new[] { ("col.lactaire_indigo", 1), ("col.pholiote_doree", 1), ("col.polypore_soufre", 1) });
        Title("entomologiste", "L'Entomologiste", "L'Entomologiste", Cr, craftPrice: 300, trophy: true,
            recipe: new[] { ("col.grillon", 2), ("col.abeille", 1), ("col.coccinelle", 2) });

        // Accessoires
        Accessory("noeud", "🎀", "un nœud", B);
        Accessory("echarpe", "🧣", "une écharpe", Rot, C);
        Accessory("fleur", "🌼", "une fleur", Rot, C);
        Accessory("casquette", "🧢", "une casquette", Rot, C);
        Accessory("ballon", "🎈", "un ballon", Rot, C);
        Accessory("lunettes", "🕶️", "des lunettes de soleil", Rot, U);
        Accessory("haut_de_forme", "🎩", "un haut-de-forme", Rot, R);
        Accessory("baguette", "🪄", "une baguette magique", Rot, R);
        Accessory("couronne", "👑", "une petite couronne", Rot, L);
        Accessory("tulipe", "🌷", "une tulipe", S, season: Season.Spring);
        Accessory("glace", "🍦", "une glace", S, season: Season.Summer);
        Accessory("moufles", "🧤", "des moufles", S, season: Season.Winter);
        // « plume d'or », not « plume dorée »: that is already the collectible it is made from.
        Accessory("plume_doree", "🪶", "une plume d'or", Cr, craftPrice: 200, trophy: true,
            recipe: new[] { ("col.plume", 5), ("col.plume_doree", 1) });
        // Summer only: the Coquillage is found nowhere else.
        Accessory("collier_coquillages", "🐚", "un collier de coquillages", Cr, craftPrice: 200, trophy: true,
            recipe: new[] { ("col.coquillage", 2), ("col.galet", 3) });
        Accessory("medaillon", "📿", "un médaillon ancien", Cr, craftPrice: 300, trophy: true,
            recipe: new[] { ("col.couronne", 1), ("col.bague", 2) });
        Accessory("bocal", "🫙", "un bocal à insectes", Cr, craftPrice: 100,
            recipe: new[] { ("col.fourmi", 2), ("col.mouche", 1), ("col.coccinelle", 1) });
        Accessory("canne_peche", "🎣", "une petite canne à pêche", Cr, craftPrice: 100,
            recipe: new[] { ("col.silex", 2), ("col.caillou_plat", 2), ("col.galet", 1) });
        Accessory("chapeau_paille", "👒", "un chapeau de paille", S, season: Season.Summer);
        Accessory("bonnet", "🧶", "un bonnet", S, season: Season.Winter);
        // Autumn had no accessory: lanterns for the long evenings.
        Accessory("lanterne", "🪔", "une petite lanterne", S, season: Season.Autumn);
        // Spring only: primevères grow at le Communal in spring, among the nature commons found there all year.
        Accessory("couronne_fleurs", "💐", "une couronne de fleurs", Cr, craftPrice: 100,
            recipe: new[] { ("col.primevere", 1), Any("nature", 3) });

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
        // Named « Aura dorée » until 8.6.0, renamed so it doesn't read as one of the auras; the key stays.
        Grave("aura_doree", "✨", "Halo doré", Rot, L);
        Grave("cristaux", "💎", "Cristaux", Cr, craftPrice: 200, trophy: true,
            recipe: new[] { ("col.cristal_givre", 2), ("col.flocon", 3) });
        Grave("cercle_fees", "🍄", "Cercle de fées", Cr, craftPrice: 300, trophy: true,
            recipe: new[] { ("col.russule_doree", 1), ("col.trompette_mort", 1) });
        Grave("lucioles", "💫", "Lucioles", Cr, craftPrice: 100,
            recipe: new[] { ("col.moustique", 2), ("col.fourmi", 1), ("col.chenille", 1) });
        Grave("glands", "🌰", "Glands", Cr, craftPrice: 100,
            recipe: new[] { Any("nature", 4), ("col.pomme_pin", 1) });
        Grave("plumes", "🪶", "Plumes", Rot, C);
        Grave("nuages", "☁️", "Nuages", Rot, C);
        Grave("coquillages", "🐚", "Coquillages", Rot, C);
        Grave("bulles_savon", "🫧", "Bulles de savon", Rot, U);
        Grave("coeurs", "💕", "Cœurs", Rot, U);
        Grave("notes_musique", "🎶", "Notes de musique", Rot, R);
        // cos.grave.citrouilles (autumn) was retired in 8.8.0 (RetirePumpkinGraveFrame): never reuse the key.
        Grave("bonhomme_neige", "⛄", "Bonhomme de neige", S, season: Season.Winter);
        // Moss on the river stones; reeds of the marsh.
        Grave("mousse", "🌿", "Mousse", Cr, craftPrice: 100,
            recipe: new[] { Any("cailloux", 6) });
        Grave("roseaux", "🌾", "Roseaux", Cr, craftPrice: 100,
            recipe: new[] { Any("insectes", 6) });

        // Auras — drawn around the Plynling, worn with any accessory (tools/plynling-art/auras.py).
        // Scintillements bring back the look the rarer species wore until 8.5.1, for everyone.
        Aura("scintillements", "✨", "Scintillements", Rot, R);
        Aura("bulles", "🫧", "Bulles", Rot, U);
        // Spring only: both flowers are found at le Communal in spring, and the crafting check measures
        // a recipe in one season (CraftingChecks.ExpectedForages).
        Aura("petales", "🌸", "Pétales", Cr, craftPrice: 300, trophy: true,
            recipe: new[] { ("col.primevere", 8), ("col.cerise", 2) });
        // One for each other season, beside Pétales for spring; sold in their season only. Drawn by the
        // bot from the layers alone, like every aura.
        // « Lucioles d'été »: a crafted cadre is already called « Lucioles », and a crafted name is unique.
        Aura("lucioles", "🌟", "Lucioles d'été", S, season: Season.Summer);
        Aura("feuilles", "🍁", "Feuilles mortes", S, season: Season.Autumn);
        Aura("flocons", "❄️", "Flocons", S, season: Season.Winter);

        return list;
    }
}
