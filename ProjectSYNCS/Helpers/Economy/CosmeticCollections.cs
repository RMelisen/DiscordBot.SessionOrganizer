namespace ProjectSYNCS.Helpers;

// A named group of cosmetics that go together (« Collection Grand-Froid »). A label only: nothing is
// paid or unlocked for owning or wearing one. Not to be confused with the collectible sets of
// /inventory collection. Name and membership are code-only (nothing stored), so both can change freely.
public sealed record CosmeticCollection(string Name, IReadOnlyList<string> Keys);

/// <summary>
/// Which collection a cosmetic belongs to — at most one; some belong to none. Shown only on the
/// inventory's garde-robe page. Checks section 61 verifies every key exists and none is listed twice.
/// </summary>
public static class CosmeticCollections
{
    public static readonly IReadOnlyList<CosmeticCollection> All = new[]
    {
        // Seasonal: every seasonal item is in its season's collection, with all-year pieces that fit.
        new CosmeticCollection("Grand-Froid", new[]
        {
            "cos.theme.flocons", "cos.title.frileux", "cos.accessory.bonnet", "cos.accessory.echarpe",
            "cos.accessory.moufles", "cos.aura.flocons", "cos.grave.bonhomme_neige", "cos.grave.cristaux",
        }),
        new CosmeticCollection("Grandes Vacances", new[]
        {
            "cos.theme.plage", "cos.title.bronze", "cos.accessory.chapeau_paille", "cos.accessory.glace",
            "cos.accessory.lunettes", "cos.accessory.collier_coquillages", "cos.aura.lucioles", "cos.grave.coquillages",
        }),
        new CosmeticCollection("Veillée d'automne", new[]
        {
            "cos.theme.feuilles_mortes", "cos.theme.braises", "cos.accessory.lanterne", "cos.aura.feuilles",
            "cos.grave.feuilles", "cos.grave.lanternes",
        }),
        new CosmeticCollection("Jardin en fleurs", new[]
        {
            "cos.theme.cerisiers", "cos.theme.aurore", "cos.accessory.tulipe", "cos.accessory.fleur",
            "cos.accessory.couronne_fleurs", "cos.aura.petales", "cos.grave.couronne_fleurs", "cos.grave.papillons",
        }),
        // Shop.
        new CosmeticCollection("Cour royale", new[]
        {
            "cos.theme.royal", "cos.title.legendaire", "cos.accessory.couronne", "cos.accessory.medaillon", "cos.grave.aura_doree",
        }),
        new CosmeticCollection("Ciel de nuit", new[]
        {
            "cos.theme.nuit_etoilee", "cos.theme.aurore_boreale", "cos.title.reveur", "cos.title.dormeur",
            "cos.aura.scintillements", "cos.aura.etoiles_filantes", "cos.grave.etoiles", "cos.grave.clair_de_lune", "cos.grave.lucioles",
        }),
        new CosmeticCollection("Gentleman", new[]
        {
            "cos.theme.velours", "cos.title.mysterieux", "cos.title.fripon", "cos.title.distingue", "cos.title.magnifique",
            "cos.accessory.haut_de_forme", "cos.accessory.noeud_papillon", "cos.accessory.monocle", "cos.accessory.canne",
            "cos.accessory.montre_gousset", "cos.grave.roses",
        }),
        new CosmeticCollection("Jour de fête", new[]
        {
            "cos.theme.arc_en_ciel", "cos.title.farceur", "cos.title.bavard", "cos.accessory.ballon",
            "cos.aura.bulles", "cos.aura.confettis", "cos.grave.bulles_savon", "cos.grave.notes_musique",
        }),
        new CosmeticCollection("Tout doux", new[]
        {
            "cos.theme.lavande", "cos.title.petit", "cos.title.calin", "cos.title.timide",
            "cos.accessory.noeud", "cos.grave.coeurs", "cos.grave.nuages",
        }),
        // Forage: mostly crafted, named after the places of Humeval.
        new CosmeticCollection("Cœur de la Lisière", new[]
        {
            "cos.theme.foret", "cos.theme.sous_bois", "cos.theme.champignonniere", "cos.title.mycologue",
            "cos.grave.cercle_fees", "cos.grave.glands",
        }),
        new CosmeticCollection("Au fil de la Sommeille", new[]
        {
            "cos.theme.riviere", "cos.theme.ocean", "cos.title.sommeillant", "cos.accessory.canne_peche",
            "cos.grave.galets", "cos.grave.mousse",
        }),
        new CosmeticCollection("Carnet du naturaliste", new[]
        {
            "cos.title.naturaliste", "cos.title.entomologiste", "cos.title.botaniste", "cos.title.mouille",
            "cos.title.curieux", "cos.accessory.bocal", "cos.grave.roseaux",
        }),
        new CosmeticCollection("Chasse au trésor", new[]
        {
            "cos.theme.caverne_tresors", "cos.title.chercheur_tresors", "cos.title.aventurier",
            "cos.title.brocanteur", "cos.title.puisatier",
        }),
        new CosmeticCollection("Le Communal", new[]
        {
            "cos.theme.prairie", "cos.title.glaneur", "cos.accessory.plume_doree", "cos.grave.ble", "cos.grave.plumes",
        }),
    };

    private static readonly Dictionary<string, CosmeticCollection> ByKey = BuildIndex();

    // First listing wins; the checks fail on a key listed twice, so there is never a second.
    private static Dictionary<string, CosmeticCollection> BuildIndex()
    {
        var index = new Dictionary<string, CosmeticCollection>();
        foreach (var collection in All)
            foreach (var key in collection.Keys)
                index.TryAdd(key, collection);
        return index;
    }

    /// <summary>The collection a cosmetic belongs to, or null.</summary>
    public static CosmeticCollection? Of(string key) => ByKey.GetValueOrDefault(key);

    /// <summary>
    /// The garde-robe page's line: every collection with at least one piece owned, « Grand-Froid 3/8 »,
    /// in catalogue order, a complete one in bold. Null when none is started.
    /// </summary>
    public static string? Progress(Func<string, bool> owns)
    {
        var started = All
            .Select(c => (c.Name, Have: c.Keys.Count(owns), Total: c.Keys.Count))
            .Where(c => c.Have > 0)
            .Select(c => c.Have == c.Total ? $"**{c.Name} {c.Have}/{c.Total}**" : $"{c.Name} {c.Have}/{c.Total}")
            .ToList();
        return started.Count == 0 ? null : string.Join(" · ", started);
    }
}
