using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

public static partial class McCommand
{
    // ---- Handing things over and putting them away ----

    private static readonly string[] GiveAllPhrases =
    {
        "vide tes poches", "vides tes poches", "vide ton inventaire", "vide ton sac", "vide toi", "donne moi tout", "donne nous tout",
        "donne tout", "file moi tout", "passe moi tout", "lance moi tout", "jette tout", "lache tout", "balance tout",
        "rends moi tout", "donne moi ce que t as", "donne moi tout ce que t as", "file moi ce que t as", "donne moi ton stuff",
        "file moi ton stuff", "donne moi tes affaires", "rends moi mes affaires", "rends moi mon stuff", "donne moi ton inventaire",
        "drop tout", "jette tout", "drop all", "drop everything", "give me everything", "give me all", "empty your pockets",
        "empty your inventory",
    };
    private static readonly string[] GiveVerbs =
    {
        "donne moi", "donne nous", "donne", "donnes moi", "file moi", "file nous", "passe moi", "passe nous", "lance moi",
        "jette moi", "rends moi", "rend moi", "rends nous", "envoie moi", "balance moi", "prete moi", "file", "passe", "rends",
        "lache", "jette", "balance", "vide", "vides", "enleve", "retire", "sors", "me donner", "me passer", "me rendre", "me filer",
        "me preter", "nous donner", "je peux avoir", "j peux avoir", "je pourrais avoir", "puis je avoir", "tu me files", "tu me pretes",
        "give me", "give", "drop", "toss me", "hand me",
    };
    // What can be asked for that isn't a thing she carries: « donne-moi ton avis » is talk, not her pockets.
    private static readonly HashSet<string> NotGoods = new()
    {
        "avis", "conseil", "conseils", "opinion", "idee", "idees", "nom", "numero", "age", "reponse", "main", "coup", "raison",
        "chance", "nouvelles", "exemple", "indice", "indices", "astuce", "astuces", "secret", "secrets", "parole", "permission",
        // « t'as vu ma pioche ? », « t'as perdu tes os ? »: asking about something else, not for the goods.
        "vu", "vue", "vus", "entendu", "perdu", "oublie", "remarque", "cuit", "fait", "mis", "mange", "fini", "trouve", "pris",
        "casse", "jete", "vendu", "laisse",
    };
    // Skipped between a verb and its goods without counting as one of the words looked at (« t'as pas un peu de bouffe ? »).
    private static readonly HashSet<string> GoodsFiller = new() { "pas", "un", "une", "peu", "de", "d", "du", "des", "encore", "quelques" };
    // Armour put on or taken off: a verb with an armour word after it (« mets ton armure », « enlève ton armure »), or a
    // phrase that says it alone. « porte » needs the armour word (« la porte »); so does « mets » (« mets tout dans… »).
    private static readonly string[] DressVerbs =
    {
        "mets", "met", "remets", "remet", "mettre", "remettre", "enfile", "enfiles", "porte", "portes", "equipe", "equipes",
        "s equiper", "put on", "wear",
    };
    private static readonly string[] DressAlonePhrases =
    {
        "equipe toi", "equipes toi", "rhabille toi", "habille toi", "gear up", "suit up",
    };
    private static readonly string[] UndressVerbs =
    {
        "enleve", "enleves", "enlever", "retire", "retires", "retirer", "ote", "otes", "desequipe", "take off", "remove",
    };
    private static readonly string[] UndressAlonePhrases = { "desequipe toi", "desequipes toi" };
    // A question that's a request (« t'as du poisson ? »): only for goods she can't fetch, and never « ta », the possessive.
    private static readonly string[] HaveVerbs = { "t as", "tu as", "t aurais", "tu aurais", "t aurai", "you have", "got any" };
    // Hunger is a request for food. « j'ai … faim » is read apart (Hungry), whatever is between.
    private static readonly string[] HungerPhrases =
    {
        "j ai la dalle", "je creve la dalle", "je meurs de faim", "je creve de faim", "j ai plus de bouffe", "je suis affame",
        "je suis affamee", "nourris moi", "nourris nous", "feed me", "i need food", "i m hungry", "im hungry",
    };
    // The player's things or hers (« rends-moi mes diamants », « donne-moi ton fer »): what she carries, never something to fetch.
    private static readonly string[] Possessives = { "ton", "ta", "tes", "votre", "vos", "mon", "ma", "mes", "your", "my" };
    private static readonly string[] FishWords =
    {
        "poisson", "poissons", "poiscaille", "saumon", "saumons", "morue", "morues", "cabillaud", "cabillauds", "fish", "salmon", "cod",
    };
    private static readonly (McGoods Goods, string[] Words)[] GoodsWords =
    {
        (McGoods.All, new[] { "stuff", "affaires", "inventaire", "inv", "truc", "trucs", "items", "objets", "loot", "butin", "poches",
            "inventory" }),
        (McGoods.Produce, new[] { "recolte", "recoltes" }),
        (McGoods.Fish, FishWords),
        (McGoods.Food, new[] { "bouffe", "bouffer", "grailler", "nourriture", "manger", "provisions", "viande", "viandes", "steak",
            "steaks", "pain", "pains", "pomme", "pommes", "boeuf", "porc", "poulet", "mouton", "food", "bread", "meat" }),
        (McGoods.Weapons, new[] { "arme", "armes", "epee", "epees", "sword", "weapon", "weapons" }),
        (McGoods.Tools, new[] { "outil", "outils", "tools" }),
        (McGoods.Pickaxe, new[] { "pioche", "pioches", "pickaxe", "pick" }),
        (McGoods.Axe, new[] { "hache", "haches", "axe" }),
        (McGoods.Shovel, new[] { "pelle", "pelles", "shovel" }),
        (McGoods.Rod, new[] { "canne", "cannes", "rod" }),
        (McGoods.Bow, new[] { "arc", "arcs", "bow" }),
        (McGoods.Arrows, new[] { "fleche", "fleches", "arrows", "arrow" }),
        // Any piece is her armour, all of it: « mets ton casque » puts on the best she has, « donne-moi ton casque » hands it all.
        (McGoods.Armour, new[] { "armure", "armures", "casque", "casques", "plastron", "plastrons", "jambieres", "bottes",
            "bouclier", "boucliers", "armor", "armour", "helmet", "chestplate", "leggings", "boots", "shield" }),
        (McGoods.Seeds, new[] { "graine", "graines", "seeds" }),
        (McGoods.Wood, new[] { "bois", "buche", "buches", "planche", "planches", "wood", "logs" }),
        (McGoods.Stone, new[] { "pierre", "pierres", "cobble", "cailloux", "stone" }),
        (McGoods.Coal, new[] { "charbon", "coal" }),
        (McGoods.Iron, new[] { "fer", "iron" }),
        (McGoods.Copper, new[] { "cuivre", "copper" }),
        (McGoods.Gold, new[] { "or", "gold" }),
        (McGoods.Diamond, new[] { "diamant", "diamants", "diams", "diam", "diamond", "diamonds" }),
        (McGoods.Dirt, new[] { "terre", "dirt" }),
        (McGoods.Sand, new[] { "sable", "sand" }),
        (McGoods.Gravel, new[] { "gravier", "graviers", "gravel" }),
        (McGoods.Clay, new[] { "argile", "clay" }),
        (McGoods.Redstone, new[] { "redstone", "redstones" }),
        (McGoods.Lapis, new[] { "lapis", "lazuli" }),
        (McGoods.Emerald, new[] { "emeraude", "emeraudes", "emerald", "emeralds" }),
        (McGoods.Quartz, new[] { "quartz" }),
        (McGoods.Granite, new[] { "granite", "granit" }),
        (McGoods.Diorite, new[] { "diorite" }),
        (McGoods.Andesite, new[] { "andesite" }),
        (McGoods.Torches, new[] { "torche", "torches", "torch", "torchs" }),
        (McGoods.Sticks, new[] { "baton", "batons", "stick", "sticks" }),
        (McGoods.Wool, new[] { "laine", "laines", "wool" }),
        (McGoods.Leather, new[] { "cuir", "cuirs", "leather" }),
        (McGoods.String, new[] { "ficelle", "ficelles", "fil", "string" }),
        (McGoods.Feathers, new[] { "plume", "plumes", "feather", "feathers" }),
        (McGoods.Bones, new[] { "os", "bone", "bones" }),
        // « poudre » alone; « poudre de redstone » is read before it (GoodsAt).
        (McGoods.Gunpowder, new[] { "poudre", "gunpowder" }),
        (McGoods.Flint, new[] { "silex", "flint" }),
        (McGoods.Pearls, new[] { "perle", "perles", "pearl", "pearls", "enderpearl", "enderpearls", "ender" }),
        (McGoods.Saplings, new[] { "pousse", "pousses", "sapling", "saplings" }),
        (McGoods.Shears, new[] { "cisaille", "cisailles", "shears" }),
        (McGoods.Hoe, new[] { "houe", "houes", "hoe" }),
        (McGoods.Buckets, new[] { "seau", "seaux", "bucket", "buckets" }),
        (McGoods.Glass, new[] { "verre", "verres", "vitre", "vitres", "glass" }),
        (McGoods.Eggs, new[] { "oeuf", "oeufs", "egg", "eggs" }),
        (McGoods.Slime, new[] { "slime", "slimes", "slimeball" }),
        // « bâton de blaze » is read before « bâton » (GoodsAt).
        (McGoods.BlazeRods, new[] { "blaze", "blazerod", "blazerods" }),
        (McGoods.Paper, new[] { "papier", "papiers", "paper" }),
        (McGoods.Books, new[] { "livre", "livres", "bouquin", "bouquins", "book", "books" }),
        (McGoods.Beds, new[] { "lit", "lits", "bed", "beds" }),
        (McGoods.Saddle, new[] { "selle", "selles", "saddle" }),
        (McGoods.Compass, new[] { "boussole", "boussoles", "compass" }),
        (McGoods.Map, new[] { "carte", "cartes", "map", "maps" }),
        (McGoods.Ink, new[] { "encre", "ink" }),
    };
    // Goods she can't be sent to fetch: after a give verb, any article will do (« donne-moi du poisson »). The others
    // need a possessive, or « donne-moi du fer » would stop being the gathering order it has always been.
    private static readonly HashSet<McGoods> CarriedOnly = new()
    {
        McGoods.All, McGoods.Food, McGoods.Fish, McGoods.Weapons, McGoods.Tools, McGoods.Pickaxe, McGoods.Axe, McGoods.Shovel,
        McGoods.Rod, McGoods.Bow, McGoods.Arrows, McGoods.Armour, McGoods.Seeds, McGoods.Torches, McGoods.Sticks, McGoods.Wool,
        McGoods.Leather, McGoods.String, McGoods.Feathers, McGoods.Bones, McGoods.Gunpowder, McGoods.Flint, McGoods.Pearls,
        McGoods.Saplings, McGoods.Shears, McGoods.Hoe, McGoods.Buckets, McGoods.Glass, McGoods.Eggs, McGoods.Slime,
        McGoods.BlazeRods, McGoods.Paper, McGoods.Books, McGoods.Beds, McGoods.Saddle, McGoods.Compass, McGoods.Map, McGoods.Ink,
    };
    private static readonly string[] StorePhrases =
    {
        // Not « ranger » or « ranges » alone: one slip from « manger », « manges » (« t'as à manger ? »).
        "range tes affaires", "range ton stuff", "range ton inventaire", "range tout", "range", "ranger tes affaires",
        "ranger ton inventaire", "ranger tout", "tout ranger", "rangement", "fais du rangement", "fais le rangement",
        "depose tout", "depose tes affaires", "depose", "deposer", "stocke", "stocker", "stock", "store", "stash", "deposit",
    };
    // With a chest word, putting anything away is storing it (« vide tes poches dans le coffre »).
    private static readonly string[] ChestWords = { "coffre", "coffres", "tonneau", "tonneaux", "baril", "barils", "chest", "chests", "barrel", "barrels" };
    // Crafting for a player: these say it alone (the item after them, or none: asked back).
    private static readonly string[] CraftVerbs =
    {
        "fabrique", "fabrique moi", "fabrique nous", "fabriques moi", "fabriquer", "crafte", "crafte moi", "craft", "crafter",
        "craft moi", "tu peux me fabriquer", "tu peux me crafter", "tu me fabriques", "tu me craftes",
    };
    // These only with an item McCraft knows right after them: « fais-moi peur » is not an order.
    private static readonly string[] CraftMakeVerbs =
    {
        "fais moi", "fait moi", "fais nous", "tu peux me faire", "tu me fais", "me faire", "construis moi", "construit moi",
    };
    // Fetching from a chest (with a chest word): what says « bring it to me » alone. Not the give verbs (« file-moi »):
    // with a chest word those put things away.
    private static readonly string[] FetchVerbs =
    {
        "ramene moi", "ramene nous", "rapporte moi", "apporte moi", "amene moi", "va chercher", "vas chercher", "va me chercher",
        "aller chercher", "aller me chercher", "sors moi", "sors nous", "recupere", "recupere moi", "recuperer", "bring", "bring me",
        "fetch", "get me",
    };
    // A carrying verb alone fetches only from the chest (« ramène le fer du coffre »): « au coffre » puts it away.
    private static readonly string[] FetchFromVerbs = { "ramene", "rapporte", "apporte", "amene" };
    private static readonly string[] IntoChestVerbs =GiveVerbs.Concat(new[]
    {
        "mets", "met", "mettre", "vide", "vides", "jette", "balance", "fous", "fourre", "range", "depose", "pose", "poser", "put",
        "dump",
    }).Distinct().ToArray();

    // ---- Asking what she carries ----

    // « t'as quoi ? »: what she carries, when nothing else follows, or « sur toi », « dans tes poches », « comme bois ».
    // Followed by anything else it's another question (« qu'est-ce que t'as fait ? »).
    private static readonly string[] CarryQuestions =
    {
        "t as quoi", "tu as quoi", "qu est ce que t as", "qu est ce que tu as", "qu as tu", "t aurais quoi", "tu portes quoi",
        "tu transportes quoi", "qu est ce que tu portes", "qu est ce que tu transportes", "what do you have", "what have you got",
        "what you got", "what do you carry",
    };
    private static readonly HashSet<string> CarryQuestionFollow = new() { "sur", "dans", "comme", "en", "on", "in" };
    // « montre ton inventaire », « c'est quoi ton stuff », « y a quoi dans tes poches »: one of these with an inventory word.
    private static readonly string[] ShowVerbs =
    {
        "montre", "montre moi", "montre nous", "fais voir", "fais moi voir", "fais nous voir", "affiche", "liste", "check", "quoi",
        "show", "show me", "list", "what s",
    };
    private static readonly string[] InventoryWords =
    {
        "inventaire", "inv", "poches", "poche", "sac", "stuff", "affaires", "items", "ce que t as", "ce que tu as", "ce que tu portes",
        "inventory", "pockets",
    };
    // Whose pockets: « mes poches » are the player's, which she can't see.
    private static readonly HashSet<string> TheirOwn = new() { "mes", "mon", "ma", "nos", "notre", "my", "our" };
    // Around a bare inventory word (« syncs ton inv ? »).
    private static readonly HashSet<string> InventoryFiller = new() { "ton", "ta", "tes", "your", "le", "la", "les", "l", "the", "et", "alors" };
    private static readonly string[] HowManyWords = { "how many", "how much" };

    // ---- Fishing and cooking ----

    private static readonly string[] FishPhrases =
    {
        // Not « pêches »: one slip from « poches ».
        "peche", "pecher", "va pecher", "aller pecher", "va a la peche", "fais de la peche", "part a la peche",
        "a la peche", "pecheuse", "mode peche", "lance ta ligne", "jette ta ligne", "lance ta canne", "jette ta canne",
        "fish", "fishing", "go fish", "go fishing",
    };
    // With a fish word: « attrape des poissons ».
    private static readonly string[] CatchVerbs = { "attrape", "attraper", "attrapes", "catch" };
    private static readonly string[] SmeltPhrases =
    {
        "cuis", "cuit", "cuire", "fais cuire", "faire cuire", "fait cuire", "cuisiner", "fais griller", "griller", "fais rotir",
        "fais a manger", "faire a manger", "prepare a manger", "fais fondre", "faire fondre", "fondre", "fonds", "fond", "smelt",
        "cook", "enfourne", "mets au four", "met au four", "mets a cuire", "utilise le four", "fais du verre", "faire du verre",
        "fais des lingots", "faire des lingots", "fais un lingot", "fais les lingots", "utilise le fourneau", "utilise le fumoir",
        "fais des briques", "faire des briques", "fais de la brique", "fais du charbon de bois", "faire du charbon de bois",
    };
    // Putting something in the furnace: one of these with « four » within four words after (« mets le fer au four »).
    private static readonly string[] IntoFurnaceVerbs = { "mets", "met", "mettre", "pose", "poser", "balance", "jette", "fous", "fourre", "put" };
    private static readonly string[] FurnaceWords = { "four", "fours", "fourneau", "furnace", "smoker", "fumoir" };
    private static readonly (McSmelt Smelt, string[] Words)[] SmeltWords =
    {
        (McSmelt.Food, new[] { "viande", "viandes", "boeuf", "porc", "poulet", "mouton", "lapin", "steak", "steaks", "cotelette",
            "cotelettes", "poisson", "poissons", "saumon", "morue", "cabillaud", "patate", "patates", "bouffe", "nourriture",
            "algue", "algues", "varech", "food", "meat", "fish", "kelp" }),
        (McSmelt.Ore, new[] { "fer", "or", "cuivre", "minerai", "minerais", "metal", "metaux", "lingot", "lingots", "ore", "ores",
            "iron", "gold", "copper" }),
        (McSmelt.Sand, new[] { "sable", "verre", "vitre", "vitres", "sand", "glass" }),
        (McSmelt.Clay, new[] { "argile", "brique", "briques", "clay", "brick", "bricks" }),
        // Logs to charcoal: « fais du charbon de bois », « cuis les bûches ». After food and ore, so « charbon » alone isn't wood.
        (McSmelt.Wood, new[] { "bois", "buche", "buches", "tronc", "troncs", "logs", "log", "charcoal" }),
    };
}
