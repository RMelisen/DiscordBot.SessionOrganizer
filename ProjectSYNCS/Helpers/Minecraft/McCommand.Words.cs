using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

// Every phrase McCommand reads, as people type them in game chat, longest first, and the spellings it maps. Part of
// McCommand: the reading itself is in McCommand.cs and McCommand.Reading.cs.
public static partial class McCommand
{
    public const int DefaultCount = 10;
    public const int MaxCount = 64;

    /// <summary>
    /// The names she answers to besides her in-game name: « sync » too, the missing s being the most
    /// common slip when typing her name in a hurry.
    /// </summary>
    public static readonly string[] HerNames = { "syncs", "sync" };

    // Staying with someone is following, and must be read before « reste », which alone means stay.
    private static readonly string[] FollowOverStayPhrases =
    {
        "reste avec moi", "reste avec nous", "reste pres de moi", "reste pres de nous", "reste a cote de moi",
        "reste derriere moi", "reste collee", "reste colle", "rester avec moi", "rester avec nous", "rester pres de moi",
    };
    // Checked before every other order: staying wins (« reste ici » has « ici »), and « bouge pas » means
    // stay, so stay is read before the refusal that would otherwise cancel it.
    private static readonly string[] StayPhrases =
    {
        "reste la", "reste ici", "reste", "restes", "stop", "stoppe", "arrete", "arretes", "arrete toi", "arrete tout",
        "bouge plus", "bouge pas", "ne bouge pas", "bouge pu", "pas bouger", "attends", "attend", "attends moi la",
        "pause", "halte", "fige toi", "freeze", "annule", "laisse tomber", "ca suffit", "c est bon arrete",
        "plus bouger", "sur place", "rester la", "rester ici", "rester", "t arreter", "arreter", "attendre",
        "ne plus bouger", "patiente", "stay", "wait", "hold", "cancel",
    };
    private static readonly string[] SleepPhrases =
    {
        "dodo", "au dodo", "fais dodo", "dors", "va dormir", "vas dormir", "dormir", "au lit", "va au lit",
        "va te coucher", "couche toi", "va te pieuter", "pionce", "roupille", "sieste", "aller dormir", "te coucher",
        "aller te coucher", "aller au lit", "sleep", "go sleep",
    };
    private static readonly string[] GuardPhrases =
    {
        "protege moi", "protege nous", "protege", "protection", "defends moi", "defend moi", "defends nous", "defends",
        "garde du corps", "garde moi", "monte la garde", "couvre moi", "couvre nous", "surveille moi", "escorte moi",
        "escorte", "au secours", "a l aide", "aide moi", "sauve moi", "bats toi", "tue les monstres", "tue les mobs",
        "tue le", "tue la", "tape le", "tape les", "attaque", "attaque le", "attaque les", "combat", "defense",
        "proteger", "me proteger", "nous proteger", "defendre", "me defendre", "me couvrir", "garder", "me garder",
        "tuer les monstres", "me sauver", "protect me", "protect", "guard", "help me", "fight", "kill",
    };
    private static readonly string[] CollectPhrases =
    {
        "chercher", "cherche", "cherches", "va chercher", "va me chercher", "ramene", "ramene moi", "ramenes",
        "recolte", "recolter", "mine", "miner", "mines", "coupe", "couper", "coupes", "abat", "abats", "abattre",
        "casse", "casser", "collecte", "collecter", "recupere", "recuperer", "ramasse", "ramasser", "trouve",
        "trouve moi", "trouver", "apporte", "apporte moi", "rapporte", "rapporte moi", "donne moi", "file moi",
        "chope", "chope moi", "fais du", "fais de la", "fais des", "faire du", "faire de la", "il me faut",
        "j ai besoin de", "j ai besoin d", "besoin de", "besoin d", "get", "collect", "farm", "gather", "chop",
        "aller chercher", "me ramener", "ramener", "m apporter", "apporter", "rapporter", "me trouver", "me donner",
        "dig", "bring", "fetch",
    };
    // Keeping a farm, read before stay (« reste à la ferme ») and guard (« garde le champ »). Either a phrase that
    // says it alone, or a care verb with a farm word a few words after it (« prends soin de mes plantations »).
    private static readonly string[] FarmAlonePhrases =
    {
        "cultive", "cultiver", "cultives", "jardine", "jardiner", "fermiere", "fais la fermiere", "joue a la fermiere",
        "mode ferme", "mode fermiere", "farm afk", "afk farm", "farm here", "keep the farm", "tend the farm",
    };
    // Care verbs that only mean farming next to a farm word: « garde le champ » is a farm, « garde moi » is not,
    // and « il reste du blé » is not a place to stay. The first ones (CropCareVerbs) take a crop too
    // (« occupe-toi du blé »); « garde des carottes » keeps some, it doesn't farm them.
    private static readonly string[] CropCareVerbs =
    {
        "occupe toi", "occupes toi", "occupe", "t occuper", "s occuper", "prends soin", "prend soin", "prendre soin",
        "entretiens", "entretient", "entretenir", "soigne", "soigner", "take care of", "tend",
    };
    private static readonly string[] FarmVerbs = CropCareVerbs.Concat(new[]
    {
        "gere", "geres", "gerer", "garde", "gardes", "garder", "surveille", "surveilles", "surveiller", "veille sur",
        "reste", "restes", "rester", "bosse", "travaille", "travailler", "keep", "watch", "guard", "stay at", "stay in",
        "work",
    }).ToArray();
    private static readonly string[] FarmWords =
    {
        "ferme", "fermes", "champ", "champs", "culture", "cultures", "potager", "potagers", "jardin", "plantation",
        "plantations", "recolte", "recoltes", "legumes", "farm", "field", "fields", "crops", "garden",
    };
    // Keeping animals, read before the farm (« occupe-toi des animaux de la ferme » is the animals). A phrase that says it
    // alone, or a care verb with an animal word within four words after it. « garde » and « surveille » are not among the
    // verbs: guarding animals stays a patrol, and must never start a cull. Animal words match exactly (« lapins » is one
    // slip from « sapins »); « élever » and « élèves » stay out (one slip from « enlever », « enlèves »).
    // The camp (« monte le camp »), read before storing: « range le camp » is taking it down, not putting things away.
    private static readonly string[] CampPhrases =
    {
        "monte le camp", "monte un camp", "monte ton camp", "monte le campement", "monte un campement", "installe le camp",
        "installe un camp", "installe le campement", "installe un campement", "fais un camp", "fais le camp", "fais un campement",
        "construis un camp", "construis le camp", "plante le camp", "dresse le camp", "monter le camp", "on campe", "set up camp",
    };
    private static readonly string[] UncampPhrases =
    {
        "demonte le camp", "demonte ton camp", "demonte le campement", "demonter le camp", "plie le camp", "replie le camp",
        "range le camp", "leve le camp", "defais le camp", "enleve le camp", "detruis le camp", "casse le camp", "pack up camp",
    };
    private static readonly string[] RanchAlonePhrases =
    {
        "fais de l elevage", "faire de l elevage", "mode eleveuse", "mode elevage", "fais l eleveuse", "joue a l eleveuse",
    };
    private static readonly string[] RanchVerbs =
    {
        "occupe toi", "occupes toi", "occupe", "t occuper", "s occuper", "prends soin", "prend soin", "prendre soin",
        "entretiens", "entretient", "entretenir", "nourris", "nourrir", "nourrit", "eleve", "gere", "geres", "gerer",
        "take care of", "feed", "breed",
    };
    private static readonly HashSet<string> AnimalWords = new()
    {
        "animaux", "animal", "betes", "bete", "betail", "troupeau", "troupeaux", "enclos", "elevage", "vaches", "vache",
        "moutons", "mouton", "cochons", "cochon", "poules", "poule", "poulets", "poulet", "lapins", "lapin", "bergerie",
        "etable", "poulailler", "animals", "cows", "sheep", "pigs", "chickens",
    };
    // Harvesting without naming a crop (« syncs récolte »).
    private static readonly string[] HarvestPhrases =
    {
        "recolte", "recolter", "recoltes", "recup la recolte", "fais la recolte", "faire la recolte", "fais les recoltes",
        "fais le champ", "fais les champs", "vide le champ", "vide les champs", "moissonne", "moissonner", "moissonnes",
        "cueille", "cueillir", "fauche", "faucher", "replante", "replanter", "replantes", "harvest",
    };
    // With a crop word, any gathering verb harvests, and so do these, which mean nothing without one
    // (« arrache les carottes », « farm le blé »; « arrache-toi » is not an order).
    private static readonly string[] CropOnlyVerbs =
    {
        "arrache", "arracher", "deterre", "deterrer", "prends", "prend", "prendre", "farme", "farmer", "recup",
    };
    private static readonly string[] CropVerbs = CollectPhrases.Concat(HarvestPhrases).Concat(CropOnlyVerbs).Distinct().ToArray();

    // Exact, or one slip on words of six letters or more (Same): « citrouile », « betteraves ». Misspellings
    // further than that are listed (« carrote », « betrave »).
    private static readonly (McCrop Crop, string[] Words)[] CropWords =
    {
        (McCrop.Wheat, new[] { "ble", "bles", "epi", "epis", "wheat" }),
        (McCrop.Carrot, new[] { "carotte", "carottes", "carrote", "carrotes", "carot", "carots", "carrot", "carrots" }),
        (McCrop.Potato, new[] { "patate", "patates", "patatte", "patattes", "potato", "potatoes" }),
        (McCrop.Beetroot, new[] { "betterave", "betteraves", "betrave", "betraves", "beetroot", "beetroots" }),
        (McCrop.Melon, new[] { "melon", "melons", "pasteque", "pasteques", "watermelon" }),
        (McCrop.Pumpkin, new[] { "citrouille", "citrouilles", "potiron", "potirons", "pumpkin", "pumpkins" }),
        (McCrop.Bamboo, new[] { "bambou", "bambous", "bamboo", "bamboos" }),
        // « canne à sucre » by its « sucre »: « canne » alone is her fishing rod (« donne-moi ta canne »).
        (McCrop.SugarCane, new[] { "sucre", "sugarcane" }),
        (McCrop.Cactus, new[] { "cactus", "cacti", "cactuses", "cactees" }),
        // Generic: harvest whatever is ripe. Never against a resource (« du bois dans le champ » is wood).
        (McCrop.Any, new[] { "champ", "champs", "cultures", "plantations", "legumes", "potager", "recoltes", "crops" }),
    };

    private static readonly string[] GoToPhrases =
    {
        "va", "vas", "va a", "va en", "go", "go to", "goto", "rends toi", "aller", "allez", "deplace toi", "tp",
        "teleporte toi", "direction", "file", "file a", "cours", "dirige toi", "rendez vous", "rdv", "marche",
        "marche jusqu", "jusqu a", "aller a", "aller en", "te rendre", "te deplacer", "aller jusqu",
    };
    private static readonly string[] FollowPhrases =
    {
        "suis moi", "suis nous", "suivez moi", "suis", "suit moi", "suis moi stp", "suivre", "suit nous",
        "accompagne moi", "accompagne nous", "viens avec moi", "viens avec nous", "colle moi", "dans mes pas",
        "derriere moi", "a mes cotes", "me suivre", "nous suivre", "m accompagner", "venir avec moi", "follow",
        "follow me",
    };
    private static readonly string[] ComePhrases =
    {
        "viens", "vien", "vient", "venir", "vi1", "viens ici", "viens la", "viens voir", "viens me voir",
        "viens vers moi", "ici", "par ici", "approche", "rapproche toi", "rejoins moi", "rejoint moi", "rejoins nous",
        "rejoint nous", "ramene toi", "ramene ta fraise", "rapplique", "radine toi", "amene toi", "pointe toi",
        "au pied", "venir ici", "me rejoindre", "nous rejoindre", "rejoindre", "t approcher", "approcher",
        "te rapprocher", "venir me voir", "come", "come here", "here",
    };

    private static readonly (McResource Resource, string[] Words)[] ResourceWords =
    {
        (McResource.Wood, new[] { "bois", "buche", "buches", "tronc", "troncs", "arbre", "arbres", "chene", "bouleau",
            "sapin", "log", "logs", "wood", "tree", "trees" }),
        (McResource.Stone, new[] { "pierre", "pierres", "roche", "roches", "caillou", "cailloux", "cobble",
            "cobblestone", "stone" }),
        (McResource.Coal, new[] { "charbon", "charbons", "coal" }),
        (McResource.Iron, new[] { "fer", "iron" }),
        (McResource.Copper, new[] { "cuivre", "copper" }),
        (McResource.Gold, new[] { "or", "gold" }),
        (McResource.Diamond, new[] { "diamant", "diamants", "diams", "diam", "dia", "diamond", "diamonds" }),
        (McResource.Dirt, new[] { "terre", "dirt" }),
        (McResource.Sand, new[] { "sable", "sand" }),
    };

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
        "me preter", "nous donner", "give me", "give", "drop", "toss me", "hand me",
    };
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
    // Hunger is a request for food.
    private static readonly string[] HungerPhrases =
    {
        "j ai faim", "j ai trop faim", "j ai la dalle", "je meurs de faim", "je creve de faim", "j ai plus de bouffe", "i m hungry",
        "im hungry",
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
        (McGoods.Food, new[] { "bouffe", "nourriture", "manger", "provisions", "viande", "viandes", "steak", "steaks", "pain", "pains",
            "pomme", "pommes", "boeuf", "porc", "poulet", "mouton", "food", "bread", "meat" }),
        (McGoods.Weapons, new[] { "arme", "armes", "epee", "epees", "sword", "weapon", "weapons" }),
        (McGoods.Tools, new[] { "outil", "outils", "tools" }),
        (McGoods.Pickaxe, new[] { "pioche", "pioches", "pickaxe", "pick" }),
        (McGoods.Axe, new[] { "hache", "haches", "axe" }),
        (McGoods.Shovel, new[] { "pelle", "pelles", "shovel" }),
        (McGoods.Rod, new[] { "canne", "cannes", "rod" }),
        (McGoods.Bow, new[] { "arc", "arcs", "bow" }),
        (McGoods.Arrows, new[] { "fleche", "fleches", "arrows", "arrow" }),
        (McGoods.Armour, new[] { "armure", "armures", "armor", "armour" }),
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
    };
    // Goods she can't be sent to fetch: after a give verb, any article will do (« donne-moi du poisson »). The others
    // need a possessive, or « donne-moi du fer » would stop being the gathering order it has always been.
    private static readonly HashSet<McGoods> CarriedOnly = new()
    {
        McGoods.All, McGoods.Food, McGoods.Fish, McGoods.Weapons, McGoods.Tools, McGoods.Pickaxe, McGoods.Axe, McGoods.Shovel,
        McGoods.Rod, McGoods.Bow, McGoods.Arrows, McGoods.Armour, McGoods.Seeds,
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
    private static readonly string[] IntoChestVerbs = GiveVerbs.Concat(new[]
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
        "a la peche", "pecheuse", "mode peche", "fish", "fishing", "go fish", "go fishing",
    };
    // With a fish word: « attrape des poissons ».
    private static readonly string[] CatchVerbs = { "attrape", "attraper", "attrapes", "catch" };
    private static readonly string[] SmeltPhrases =
    {
        "cuis", "cuit", "cuire", "fais cuire", "faire cuire", "fait cuire", "cuisiner", "fais griller", "griller", "fais rotir",
        "fais a manger", "faire a manger", "prepare a manger", "fais fondre", "faire fondre", "fondre", "fonds", "fond", "smelt",
        "cook", "enfourne", "mets au four", "met au four", "mets a cuire", "utilise le four", "fais du verre", "faire du verre",
        "fais des lingots", "faire des lingots", "fais un lingot", "fais les lingots",
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
    };

    // ---- Small things: sitting, hopping, waving, telling where she is ----

    private static readonly string[] SitPhrases =
    {
        "assis", "assise", "assieds toi", "assied toi", "assis toi", "assoie toi", "assois toi", "t asseoir", "s asseoir",
        "asseoir", "pose toi", "couchee", "accroupis toi", "accroupie", "accroupis", "baisse toi", "sit", "sit down", "crouch",
    };
    // A dance is hopping too: it's all she can do with square legs.
    private static readonly string[] JumpPhrases =
    {
        "saute", "sautes", "sauter", "sautille", "fais un saut", "fais des sauts", "fais des bonds", "bondis", "danse", "danser",
        "fais une danse", "jump", "dance",
    };
    // Goodbyes: she waves too, with a goodbye line.
    private static readonly string[] ByePhrases =
    {
        "bonne nuit", "au revoir", "aurevoir", "bye", "bye bye", "ciao", "tchao", "a plus", "a toute", "a demain", "a la prochaine",
        "bisous", "adieu", "see you", "see ya", "good night", "gn",
    };
    // Compliments and thanks: read only where a call would be (McCommand.ParseWords), so « bravo syncs » isn't « oui ? ».
    // Interjections alone; an adjective only said to her (« t'es géniale »), as « syncs la base est jolie » is about the
    // base. Not « forte » alone: « syncs t'es assez forte » is a dare, not a compliment.
    private static readonly string[] PraisePhrases =
    {
        "t es la meilleure", "tu es la meilleure", "t es trop forte", "tu es trop forte", "t es forte", "tu es forte",
        "t es geniale", "tu es geniale", "t es parfaite", "t es incroyable", "t es une championne", "t es top", "trop forte",
        "bien joue", "bien jouee", "bien ouej", "bon travail", "beau travail", "bon boulot", "beau boulot", "good job",
        "well done", "good bot", "good girl", "gg wp", "felicitations", "felicitation", "masterclass", "championne", "chapeau",
        "respect", "bravo", "ggwp", "gg", "wp", "gj", "nice", "goat",
    };
    private static readonly string[] ThanksPhrases =
    {
        "merci", "merci beaucoup", "merci bien", "mercii", "merki", "mrc", "mci", "cimer", "thanks", "thx", "thank you",
        "thank u", "ty", "tyty", "c est gentil", "trop gentille", "t es gentille",
    };
    // Greetings: she waves back. Read last, so « salut syncs viens » is still a come.
    private static readonly string[] WavePhrases =
    {
        "fais coucou", "fais un coucou", "fais nous coucou", "fais moi coucou", "dis coucou", "dis bonjour", "dis salut",
        "dis bonsoir", "fais signe", "fais un signe", "fais nous signe", "salue", "salue moi", "salue nous", "coucou", "kikou",
        "salut", "slt", "bonjour", "bjr", "bonsoir", "bsr", "hello", "hey", "hi", "yo", "wesh", "cc", "re", "salam", "wave",
        "say hi",
    };
    private static readonly string[] WherePhrases =
    {
        "t es ou", "tu es ou", "ou es tu", "ou t es", "ou tu es", "ou est ce que tu es", "ou est ce que t es", "ou te trouves tu",
        "ou tu te trouves", "tes coordonnees", "tes coords", "tes coordos", "ta position", "ta pos", "donne ta position",
        "coordonnees", "coords", "position", "t ou", "tes ou", "t es passee ou", "tu es passee ou", "t es passe ou",
        "t es partie ou", "t es perdue", "tu es perdue", "where are you", "where r u", "your coords", "your position",
    };

    // ---- Timers ----

    // Not « préviens » alone: one slip from « reviens » (« je reviens dans 5 min »).
    private static readonly string[] TimerVerbs =
    {
        "previens moi", "previens nous", "me prevenir", "nous prevenir", "me previens", "tu me previens", "me rappelles",
        "tu me rappelles", "rappelle moi", "rappelle nous", "rappel moi", "me rappeler",
        "nous rappeler", "dis moi", "fais moi signe", "ping moi", "reveille moi", "alerte moi", "sonne", "timer", "minuteur",
        "chrono", "chronometre", "alarme", "compte a rebours", "remind me", "ping me", "tell me", "warn me",
    };
    // « 1h30 », « 2 h 15 ».
    private static readonly Regex HoursMinutes = new(@"\b(?<h>\d{1,2})\s*h\s*(?<m>\d{2})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // « ping moi dans 5 »: a bare number after « dans » is minutes.
    private static readonly Regex BareMinutes = new(@"\bdans\s+(?<n>\d{1,3})\b(?!\s*\p{L})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    /// <summary>The longest note a reminder carries back.</summary>
    public const int NoteMax = 80;
    // A length of time, read from the raw text: « dans 5 min », « 10mn », « une demi-heure », « un quart d'heure », « 30 s ».
    private static readonly Regex Duration = new(
        @"(?:\b(?:dans|d['’]ici|pendant|pour|in)\s+)?(?:\b(?<quart>un\s+quart\s+d['’]\s*heure)\b|\b(?<demi>une\s+demi[-\s]?heure)\b"
        + @"|\b(?<n>\d{1,4}|une?|deux|trois|quatre|cinq|six|sept|huit|neuf|dix|onze|douze|treize|quatorze|quinze|seize|vingt|trente|quarante|cinquante|soixante)"
        + @"\s*(?<u>secondes?|seconds?|secs?|s|minutes?|mins?|mn|m|heures?|hours?|h)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // Where the note starts, once the duration is out: the first « de », « pour », « que » (« rappelle-moi de sortir le fer »).
    private static readonly Regex NoteStart = new(@"(?:^|[\s,])(?:de|pour|que)\s+|(?:^|[\s,])(?:d|qu)['’]\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ---- Remembered places ----

    /// <summary>How many places she remembers at most for one player, and how long a name may be.</summary>
    public const int MaxPlaces = 20;
    public const int MaxPlaceName = 24;
    private static readonly string[] SaveVerbs =
    {
        "retiens", "retient", "retenir", "memorise", "memoriser", "enregistre", "enregistrer", "sauvegarde", "sauvegarder",
        "souviens toi de", "souviens toi d", "souviens toi", "rappelle toi de", "rappelle toi d", "rappelle toi", "remember", "save",
    };
    // These save a place only right before « ici » or « cet endroit » (« note ici : la mine »): alone they mean other things.
    private static readonly string[] SaveLooseVerbs =
    {
        "note", "noter", "marque", "marquer", "nomme", "nommer", "appelle", "baptise", "ajoute", "ajouter", "mark", "name", "call", "add",
    };
    // Setting home, the way server plugins say it: saved as « maison ».
    private static readonly string[] SaveHomePhrases = { "set home", "sethome", "definis la maison", "ici c est chez moi", "ici c est chez nous" };
    private static readonly string[] HereWords =
    {
        "ici", "cet endroit", "ce lieu", "ce coin", "ce spot", "cette position", "cet emplacement", "la position", "ma position",
        "here", "this place", "this spot",
    };
    // « ici c'est la mine »: only with an article after it, or « ici c'est trop bien » would be a place called « trop bien ».
    private static readonly string[] HereIsPhrases = { "ici c est", "ici cest", "c est ici", "cest ici" };
    private static readonly string[] ForgetVerbs = { "oublie", "oublies", "oublier", "efface", "effacer", "supprime", "supprimer", "forget", "delete" };
    private static readonly string[] ListPlacesPhrases =
    {
        "tes lieux", "tes endroits", "quels lieux", "quels endroits", "quel lieux", "liste des lieux", "liste les lieux",
        "liste tes lieux", "lieux connus", "endroits connus", "tes waypoints", "waypoints", "tes spots", "tes reperes", "reperes",
        "tu connais quels endroits", "tu connais quels lieux", "endroits que tu connais", "lieux que tu connais", "your places",
        "list places",
    };
    private static readonly string[] WherePlaceVerbs =
    {
        "ou est", "ou sont", "ou se trouve", "ou se trouvent", "c est ou", "cest ou", "c est par ou", "coordonnees de",
        "coordonnees du", "coords de", "coords du", "position de", "position du", "where is",
    };
    // The same question with the place first (« la mine c'est où ? », « elle est où la base ? »): only with a place she knows.
    private static readonly string[] WhereIsAnywhere = { "c est ou", "cest ou", "est ou", "elle est ou", "il est ou", "c ou", "sont ou" };
    private static readonly string[] GuideVerbs =
    {
        "montre moi le chemin", "montre nous le chemin", "montre moi la route", "montre nous la route", "montre moi", "montre nous",
        "guide moi", "guide nous", "guide", "emmene moi", "emmene nous", "emmene", "amene moi", "amene nous", "conduis moi",
        "conduis nous", "mene moi", "mene nous", "accompagne moi", "accompagne nous", "ramene moi", "ramene nous",
        "show me the way", "show me", "take me", "lead me", "lead the way",
    };
    private static readonly string[] GoToPlaceVerbs = GoToPhrases.Concat(new[]
    {
        "rentre", "rentres", "rentrer", "retourne", "retournes", "retourner", "repars", "rejoins", "rejoint", "go back", "head to",
        "back to",
    }).ToArray();
    // Going home: « rentre », « rentre chez nous ». The service tries « maison », then the other home words.
    private static readonly string[] HomeVerbs = { "rentre", "rentres", "rentrer", "go home" };
    private static readonly string[] Prepositions = { "a", "au", "aux", "vers", "jusqu", "jusque", "chez", "to", "at" };
    private static readonly string[] Articles = { "la", "le", "l", "les", "ma", "mon", "mes", "notre", "nos", "ta", "ton", "the", "my", "our" };
    // Skipped before a place's name (« retiens ici comme la mine »), and at its end.
    private static readonly HashSet<string> NameFiller = new()
    {
        "ici", "cet", "cette", "ce", "endroit", "lieu", "coin", "spot", "emplacement", "position", "ca", "comme", "sous", "le", "la",
        "les", "l", "nom", "de", "d", "du", "des", "c", "est", "cest", "en", "tant", "que", "qu", "mon", "ma", "mes", "notre", "nos",
        "un", "une", "pour", "a", "au", "aux", "tout", "appele", "appelee", "s", "il", "te", "t", "toi", "moi", "nous", "vous", "me",
        "m", "lui", "the", "as", "this", "place", "here", "it", "called", "on", "y", "chez", "vers", "jusqu", "jusque",
    };
    private static readonly HashSet<string> NameStop = new() { "et", "puis", "merci", "please", "thanks", "thx", "stp", "svp" };

    // ---- Guarding a place ----

    private static readonly string[] PatrolAlonePhrases =
    {
        "patrouille", "patrouiller", "patrouilles", "fais une ronde", "fais des rondes", "fais la ronde", "fais ta ronde",
        "sentinelle", "mode sentinelle", "patrol", "sentry", "guard here", "keep watch",
    };
    private static readonly string[] PatrolVerbs =
    {
        "surveille", "surveiller", "surveilles", "garde", "garder", "gardes", "protege", "proteger", "proteges", "defends", "defend",
        "defendre", "veille sur", "monte la garde", "guard", "protect", "watch",
    };
    private static readonly string[] BaseWords =
    {
        "base", "bases", "maison", "baraque", "home", "house", "village", "camp", "campement", "coin", "zone", "endroit", "spot",
        "chateau", "fort", "forteresse", "bunker", "porte", "entree", "cabane", "abri", "perimetre", "ici", "spawn", "enclos",
        "animaux", "betes", "vaches", "moutons", "cochons", "poules", "chevaux", "qg", "lieu", "coffre", "coffres", "affaires",
    };
    // « reste ici et surveille »: staying with a guarding verb anywhere is guarding the spot.
    private static readonly string[] StayHerePhrases = { "reste", "restes", "rester", "reste la", "reste ici", "bouge pas", "ne bouge pas", "bouge plus" };

    // A coordinate standing alone: « 120 », « -300 », « x:120 », or F3's « 120.523 » (floored to the block).
    // Not the digits inside a name (Bob123).
    private static readonly Regex Coordinate = new(@"(?<![\w.])-?\d{1,8}(?:\.\d+)?(?![\w.])", RegexOptions.Compiled);
    // A count: « 10 », « x10 », « 10x ».
    private static readonly Regex Count = new(@"(?<![\w.])x?(\d{1,4})x?(?![\w.])", RegexOptions.Compiled);

    // Quantities in words, read only right before the resource (« dix bûches », « une stack de pierre »),
    // so « du bois pour une maison » isn't one log. Two-word ones are tried first.
    private static readonly Dictionary<string, int> QuantityPairs = new()
    {
        ["une dizaine"] = 10, ["une douzaine"] = 12, ["une quinzaine"] = 15, ["une vingtaine"] = 20,
        ["une trentaine"] = 30, ["un peu"] = 5, ["un stack"] = 64, ["une stack"] = 64, ["demi stack"] = 32,
        ["un max"] = 64,
    };
    private static readonly Dictionary<string, int> QuantityWords = new()
    {
        ["un"] = 1, ["une"] = 1, ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4, ["cinq"] = 5, ["six"] = 6, ["sept"] = 7,
        ["huit"] = 8, ["neuf"] = 9, ["dix"] = 10, ["onze"] = 11, ["douze"] = 12, ["treize"] = 13, ["quatorze"] = 14,
        ["quinze"] = 15, ["seize"] = 16, ["vingt"] = 20, ["trente"] = 30, ["quarante"] = 40, ["cinquante"] = 50,
        ["soixante"] = 60, ["quelques"] = 5, ["plein"] = 32, ["beaucoup"] = 32, ["max"] = 64, ["stack"] = 64,
        ["stacks"] = 64,
    };

    // Game-chat spellings, mapped to the word the lists use. « chui » / « jsuis » become « je suis », so
    // « chui là » is never read as « suis » (follow me). Politeness is dropped: it changes nothing.
    private static readonly Dictionary<string, string> Slang = new()
    {
        ["mwa"] = "moi", ["moa"] = "moi", ["mw"] = "moi", ["moua"] = "moi", ["twa"] = "toi", ["toa"] = "toi",
        ["tw"] = "toi", ["ns"] = "nous", ["isi"] = "ici", ["issi"] = "ici", ["chui"] = "je suis", ["chuis"] = "je suis",
        ["jsuis"] = "je suis", ["jsui"] = "je suis", ["chu"] = "je suis", ["jai"] = "j ai", ["tt"] = "tout",
        ["stp"] = "", ["svp"] = "", ["plz"] = "", ["pls"] = "", ["please"] = "", ["steuplait"] = "", ["steup"] = "",
    };
}
