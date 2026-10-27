namespace ProjectSYNCS.Helpers;

/// <summary>How she names an item in a sentence: one, several, and whether one takes « une ».</summary>
public readonly record struct McItemName(string One, string Many, bool Feminine)
{
    /// <summary>« une pioche en fer », « un pain », « 23 pains ».</summary>
    public string Count(int n) => n == 1 ? (Feminine ? "une " : "un ") + One : $"{n} {Many}";
}

/// <summary>
/// The French names of the items she handles, by game id (<c>oak_log</c>), written by hand: the game's own names have no
/// plural and no article (« 64 Bûche de chêne »), and her lines must read as speech. Covers what her orders, her free
/// time and fishing put in her pockets; an item that isn't here is « un autre truc » in game and its id on Discord.
/// Not stored: free to change.
/// </summary>
public static class McItems
{
    /// <summary>The name of an item by its game id, or null when she has no French for it.</summary>
    public static McItemName? Name(string id) => Names.TryGetValue(id, out var name) ? name : null;

    /// <summary>How many items have a name, for the checks.</summary>
    public static int Known => Names.Count;

    /// <summary>Every id and name, for the checks.</summary>
    public static IEnumerable<KeyValuePair<string, McItemName>> All => Names;

    private static readonly Dictionary<string, McItemName> Names = Build();

    private static Dictionary<string, McItemName> Build()
    {
        var names = new Dictionary<string, McItemName>();
        // The plural, when it isn't the first word with an s (« fer brut » → « fers bruts »).
        void M(string id, string one, string? many = null) => names[id] = new(one, many ?? Plural(one), false);
        void F(string id, string one, string? many = null) => names[id] = new(one, many ?? Plural(one), true);

        // Wood: logs, planks, saplings, by tree.
        foreach (var (tree, of) in new[]
                 {
                     ("oak", "de chêne"), ("spruce", "de sapin"), ("birch", "de bouleau"), ("jungle", "d'acajou"),
                     ("acacia", "d'acacia"), ("dark_oak", "de chêne noir"), ("mangrove", "de palétuvier"), ("cherry", "de cerisier"),
                     ("pale_oak", "de chêne pâle"),
                 })
        {
            F($"{tree}_log", $"bûche {of}");
            F($"stripped_{tree}_log", $"bûche {of} écorcée", $"bûches {of} écorcées");
            F($"{tree}_planks", $"planche {of}");
            F($"{tree}_sapling", $"pousse {of}");
        }
        M("stick", "bâton");
        F("apple", "pomme");

        // Stone and earth.
        F("cobblestone", "pierre taillée", "pierres taillées");
        F("stone", "roche");
        F("cobbled_deepslate", "ardoise taillée", "ardoises taillées");
        F("deepslate", "ardoise des abîmes", "ardoises des abîmes");
        M("granite", "granite");
        F("diorite", "diorite");
        F("andesite", "andésite");
        M("tuff", "tuf");
        M("gravel", "gravier");
        M("flint", "silex", "silex");
        M("dirt", "bloc de terre", "blocs de terre");
        F("coarse_dirt", "terre stérile", "terres stériles");
        M("sand", "bloc de sable", "blocs de sable");
        M("red_sand", "bloc de sable rouge", "blocs de sable rouge");
        M("glass", "bloc de verre", "blocs de verre");
        F("glass_pane", "vitre");
        F("clay_ball", "motte d'argile", "mottes d'argile");
        F("brick", "brique");
        F("obsidian", "obsidienne");
        F("snowball", "boule de neige", "boules de neige");

        // Ores and what comes out of them.
        foreach (var (metal, of) in new[] { ("iron", "de fer"), ("copper", "de cuivre"), ("gold", "d'or") })
        {
            M($"{metal}_ore", $"minerai {of}");
            M($"deepslate_{metal}_ore", $"minerai {of} des abîmes", $"minerais {of} des abîmes");
            M($"{metal}_ingot", $"lingot {of}");
        }
        M("raw_iron", "fer brut", "fers bruts");
        M("raw_copper", "cuivre brut", "cuivres bruts");
        M("raw_gold", "or brut", "ors bruts");
        F("iron_nugget", "pépite de fer", "pépites de fer");
        F("gold_nugget", "pépite d'or", "pépites d'or");
        M("coal", "charbon");
        M("charcoal", "charbon de bois", "charbons de bois");
        M("coal_ore", "minerai de charbon", "minerais de charbon");
        M("deepslate_coal_ore", "minerai de charbon des abîmes", "minerais de charbon des abîmes");
        M("diamond", "diamant");
        M("diamond_ore", "minerai de diamant", "minerais de diamant");
        M("deepslate_diamond_ore", "minerai de diamant des abîmes", "minerais de diamant des abîmes");
        F("emerald", "émeraude");
        M("lapis_lazuli", "lapis-lazuli", "lapis-lazuli");
        F("redstone", "poudre de redstone", "poudres de redstone");
        M("amethyst_shard", "éclat d'améthyste", "éclats d'améthyste");
        M("quartz", "quartz", "quartz");

        // Tools and weapons, by material.
        var materials = new[]
        {
            ("wooden", "en bois"), ("stone", "en pierre"), ("copper", "en cuivre"), ("iron", "en fer"), ("golden", "en or"),
            ("diamond", "en diamant"), ("netherite", "en netherite"),
        };
        foreach (var (material, made) in materials)
        {
            F($"{material}_pickaxe", $"pioche {made}");
            F($"{material}_axe", $"hache {made}");
            F($"{material}_shovel", $"pelle {made}");
            F($"{material}_hoe", $"houe {made}");
            F($"{material}_sword", $"épée {made}");
        }
        M("bow", "arc");
        F("crossbow", "arbalète");
        F("arrow", "flèche");
        M("trident", "trident");
        M("shield", "bouclier");
        F("fishing_rod", "canne à pêche", "cannes à pêche");
        F("shears", "paire de cisailles", "paires de cisailles");
        M("flint_and_steel", "briquet");

        // Armour, by material: leggings and boots come in pairs.
        foreach (var (material, made) in new[]
                 {
                     ("leather", "en cuir"), ("chainmail", "en mailles"), ("copper", "en cuivre"), ("iron", "en fer"),
                     ("golden", "en or"), ("diamond", "en diamant"), ("netherite", "en netherite"),
                 })
        {
            M($"{material}_helmet", $"casque {made}");
            M($"{material}_chestplate", $"plastron {made}");
            F($"{material}_leggings", $"paire de jambières {made}", $"paires de jambières {made}");
            F($"{material}_boots", $"paire de bottes {made}", $"paires de bottes {made}");
        }
        F("turtle_helmet", "carapace de tortue", "carapaces de tortue");
        F("elytra", "paire d'élytres", "paires d'élytres");

        // Food, raw and cooked.
        M("bread", "pain");
        F("golden_apple", "pomme dorée", "pommes dorées");
        F("enchanted_golden_apple", "pomme dorée enchantée", "pommes dorées enchantées");
        F("carrot", "carotte");
        F("golden_carrot", "carotte dorée", "carottes dorées");
        F("potato", "pomme de terre", "pommes de terre");
        F("baked_potato", "pomme de terre cuite", "pommes de terre cuites");
        F("poisonous_potato", "pomme de terre empoisonnée", "pommes de terre empoisonnées");
        F("beetroot", "betterave");
        F("beetroot_soup", "soupe de betterave", "soupes de betterave");
        F("melon_slice", "tranche de pastèque", "tranches de pastèque");
        F("melon", "pastèque");
        F("pumpkin", "citrouille");
        F("pumpkin_pie", "tarte à la citrouille", "tartes à la citrouille");
        M("wheat", "blé");
        F("sugar_cane", "canne à sucre", "cannes à sucre");
        M("sugar", "sucre");
        M("egg", "œuf");
        M("cookie", "cookie");
        M("cake", "gâteau", "gâteaux");
        M("mushroom_stew", "ragoût de champignons", "ragoûts de champignons");
        M("rabbit_stew", "ragoût de lapin", "ragoûts de lapin");
        F("honey_bottle", "fiole de miel", "fioles de miel");
        F("sweet_berries", "baie sucrée", "baies sucrées");
        F("glow_berries", "baie lumineuse", "baies lumineuses");
        F("dried_kelp", "algue séchée", "algues séchées");
        F("kelp", "algue");
        M("beef", "steak cru", "steaks crus");
        M("cooked_beef", "steak");
        F("porkchop", "côtelette de porc crue", "côtelettes de porc crues");
        F("cooked_porkchop", "côtelette de porc cuite", "côtelettes de porc cuites");
        M("chicken", "poulet cru", "poulets crus");
        M("cooked_chicken", "poulet cuit", "poulets cuits");
        M("mutton", "mouton cru", "moutons crus");
        M("cooked_mutton", "mouton cuit", "moutons cuits");
        M("rabbit", "lapin cru", "lapins crus");
        M("cooked_rabbit", "lapin cuit", "lapins cuits");
        F("cod", "morue crue", "morues crues");
        F("cooked_cod", "morue cuite", "morues cuites");
        M("salmon", "saumon cru", "saumons crus");
        M("cooked_salmon", "saumon cuit", "saumons cuits");
        M("tropical_fish", "poisson tropical", "poissons tropicaux");
        M("pufferfish", "poisson-globe", "poissons-globes");
        F("rotten_flesh", "chair putréfiée", "chairs putréfiées");
        M("spider_eye", "œil d'araignée", "yeux d'araignée");

        // Seeds.
        F("wheat_seeds", "graine de blé", "graines de blé");
        F("beetroot_seeds", "graine de betterave", "graines de betterave");
        F("melon_seeds", "graine de pastèque", "graines de pastèque");
        F("pumpkin_seeds", "graine de citrouille", "graines de citrouille");

        // What animals and monsters drop.
        M("leather", "cuir");
        F("feather", "plume");
        F("rabbit_hide", "peau de lapin", "peaux de lapin");
        F("rabbit_foot", "patte de lapin", "pattes de lapin");
        M("bone", "os", "os");
        F("bone_meal", "poudre d'os", "poudres d'os");
        F("string", "ficelle");
        F("gunpowder", "poudre à canon", "poudres à canon");
        F("ink_sac", "poche d'encre", "poches d'encre");
        F("slime_ball", "boule de slime", "boules de slime");
        F("ender_pearl", "perle de l'Ender", "perles de l'Ender");
        M("blaze_rod", "bâton de blaze", "bâtons de blaze");
        foreach (var (colour, one, many) in new[]
                 {
                     ("white", "blanche", "blanches"), ("orange", "orange", "orange"), ("magenta", "magenta", "magenta"),
                     ("light_blue", "bleu clair", "bleu clair"), ("yellow", "jaune", "jaunes"), ("lime", "vert clair", "vert clair"),
                     ("pink", "rose", "roses"), ("gray", "grise", "grises"), ("light_gray", "gris clair", "gris clair"),
                     ("cyan", "cyan", "cyan"), ("purple", "violette", "violettes"), ("blue", "bleue", "bleues"),
                     ("brown", "marron", "marron"), ("green", "verte", "vertes"), ("red", "rouge", "rouges"), ("black", "noire", "noires"),
                 })
        {
            F($"{colour}_wool", $"laine {one}", $"laines {many}");
            // Any colour is « un lit »: the adjective would need its masculine.
            M($"{colour}_bed", "lit");
        }

        // Fishing's treasure and junk, and the odd things in a pocket.
        M("bowl", "bol");
        M("lily_pad", "nénuphar");
        F("nautilus_shell", "coquille de nautile", "coquilles de nautile");
        M("tripwire_hook", "crochet");
        M("bamboo", "bambou");
        F("name_tag", "étiquette");
        F("saddle", "selle");
        F("lead", "laisse");
        M("book", "livre");
        M("enchanted_book", "livre enchanté", "livres enchantés");
        M("paper", "papier");
        F("potion", "potion");
        F("glass_bottle", "fiole vide", "fioles vides");
        M("bucket", "seau", "seaux");
        M("water_bucket", "seau d'eau", "seaux d'eau");
        M("lava_bucket", "seau de lave", "seaux de lave");
        M("milk_bucket", "seau de lait", "seaux de lait");
        F("torch", "torche");
        F("soul_torch", "torche des âmes", "torches des âmes");
        M("crafting_table", "établi");
        M("furnace", "four");
        M("chest", "coffre");
        F("ladder", "échelle");
        F("compass", "boussole");
        F("clock", "horloge");
        F("map", "carte");
        F("filled_map", "carte");
        M("totem_of_undying", "totem d'immortalité", "totems d'immortalité");
        return names;
    }

    // The first word takes the s (« pioche en fer » → « pioches en fer »); one ending in s, x or z stays as it is.
    private static string Plural(string one)
    {
        var space = one.IndexOf(' ');
        var head = space < 0 ? one : one[..space];
        var tail = space < 0 ? "" : one[space..];
        var plural = head[^1] is 's' or 'x' or 'z' ? head : head.EndsWith("eau") || head.EndsWith("au") ? head + "x" : head + "s";
        return plural + tail;
    }
}
