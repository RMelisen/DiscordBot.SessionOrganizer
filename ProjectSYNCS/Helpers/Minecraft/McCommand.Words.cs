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
        // « ne me lâche pas » is follow; not « lâche pas » alone, which is « don't give up ».
        "ne me lache pas", "me lache pas", "lache moi pas",
    };
    // Checked before every other order: staying wins (« reste ici » has « ici »), and « bouge pas » means
    // stay, so stay is read before the refusal that would otherwise cancel it.
    private static readonly string[] StayPhrases =
    {
        "reste la", "reste ici", "reste", "restes", "stop", "stoppe", "arrete", "arretes", "arrete toi", "arrete tout",
        "bouge plus", "bouge pas", "ne bouge pas", "bouge pu", "pas bouger", "attends", "attend", "attends moi la",
        "pause", "halte", "fige toi", "freeze", "annule", "laisse tomber", "ca suffit", "c est bon arrete",
        "plus bouger", "sur place", "rester la", "rester ici", "rester", "t arreter", "arreter", "attendre",
        "ne plus bouger", "ne bouge plus", "ne bouge pu", "patiente", "fais rien", "ne fais rien", "fais plus rien",
        "ne fais plus rien", "laisse moi tranquille", "laisse nous tranquilles", "fiche moi la paix", "fous moi la paix",
        "lache moi la grappe", "stay", "wait", "hold", "cancel",
    };
    // « lâche-moi », « laisse-moi »: stay, but only as the whole message (« lâche-moi tes diamants » hands them over,
    // « laisse-moi passer » is something else). Read after handing over.
    private static readonly string[] LeaveMePhrases = { "lache moi", "lache nous", "laisse moi", "laisse nous", "leave me alone" };
    // « arrête de dire ça » is about her talk, not her task: the stop words followed by « de » and one of these aren't stay.
    private static readonly HashSet<string> StopTalking = new()
    {
        "dire", "parler", "raconter", "spam", "spammer", "mentir", "crier", "rire", "rigoler", "bouder", "rager", "troller",
        "chanter", "saouler", "souler", "insulter", "jurer", "pleurer", "gueuler", "rouspeter", "repeter", "repondre", "ecrire",
        "plaindre",
    };
    private static readonly HashSet<string> StopWords = new() { "arrete", "arretes", "arreter", "t arreter", "stop", "stoppe" };
    private static readonly string[] SleepPhrases =
    {
        "dodo", "au dodo", "fais dodo", "dors", "va dormir", "vas dormir", "dormir", "au lit", "va au lit",
        "va te coucher", "couche toi", "va te pieuter", "pionce", "roupille", "sieste", "aller dormir", "te coucher",
        "aller te coucher", "aller au lit", "on dort", "dans le lit", "dans ton lit", "au pieu", "va au pieu", "sleep", "go sleep",
        "go to bed", "go to sleep",
    };
    // Standing up from a crouch, or out of bed: stay, which lets go of the crouch and wakes her.
    // Free time: back to her own life. Not « vas-y » or « libre » alone, which mean too many other things.
    private static readonly string[] FreePhrases =
    {
        "t es libre", "tu es libre", "t es libre maintenant", "quartier libre", "fais ta vie", "vis ta vie", "va vivre ta vie",
        "c est bon tu peux y aller", "tu peux y aller", "tu peux disposer", "repos", "rompez", "dismissed", "go play",
        "tu peux arreter de me suivre",
        "fin de service", "t as fini ton service", "tu as fini ton service",
    };
    // After a free-time phrase, a question about later (« t'es libre ce soir ? »): talk, not an order.
    private static readonly HashSet<string> FreeLaterWords = new()
    {
        "ce", "cet", "cette", "demain", "soir", "aprem", "apres", "samedi", "dimanche", "lundi", "mardi", "mercredi",
        "jeudi", "vendredi", "weekend", "week", "we", "quand", "bientot", "tantot", "plus", "pour", "a",
    };
    private static readonly string[] StandUpPhrases = { "leve toi", "releve toi", "debout", "stand up", "get up", "wake up" };
    private static readonly string[] GuardPhrases =
    {
        "protege moi", "protege nous", "protege", "protection", "defends moi", "defend moi", "defends nous", "defends",
        "garde du corps", "garde moi", "monte la garde", "couvre moi", "couvre nous", "surveille moi", "escorte moi",
        "escorte", "au secours", "a l aide", "aide moi", "sauve moi", "bats toi", "tue les monstres", "tue les mobs",
        "tue le", "tue la", "tape le", "tape les", "attaque", "attaque le", "attaque les", "combat", "defense",
        "proteger", "me proteger", "nous proteger", "defendre", "me defendre", "me couvrir", "garder", "me garder",
        "tuer les monstres", "me sauver", "tue tout", "tue les tous", "protect me", "protect", "guard", "help me", "fight", "kill",
    };
    // « aide-moi à couper du bois » asks for help with something else: these followed by one of HelpWithWords aren't guard.
    private static readonly string[] HelpPhrases = { "aide moi", "aide nous", "help me" };
    private static readonly HashSet<string> HelpWithWords = new() { "a", "pour", "avec", "to", "with" };
    // A killing verb with a monster within three words after it (« tue les zombies », « bute l'araignée »): guard.
    private static readonly string[] KillVerbs =
    {
        "tue", "tues", "tuer", "bute", "butes", "buter", "defonce", "defoncer", "massacre", "extermine", "degomme", "degommer",
        "eclate", "tape", "frappe", "attaque", "combat", "occupe toi", "occupes toi", "kill", "slay",
    };
    private static readonly HashSet<string> FoeWords = new()
    {
        "zombie", "zombies", "zombi", "zombis", "squelette", "squelettes", "creeper", "creepers", "araignee", "araignees",
        "monstre", "monstres", "mob", "mobs", "sorciere", "sorcieres", "pillard", "pillards", "slime", "slimes", "phantom",
        "phantoms", "fantome", "fantomes", "blaze", "noye", "noyes", "momie", "momies", "ennemi", "ennemis", "bestioles",
        "skeleton", "skeletons", "spider", "spiders", "witch", "pillager", "pillagers", "husk", "drowned", "monsters",
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
        "fais moi du", "fais moi de la", "fais moi des", "fais nous du", "fais nous de la", "fais nous des", "prends", "prend",
        "prendre", "farme", "farmer", "farmes", "envoie moi", "passe moi", "i need", "we need", "dig", "bring", "fetch",
    };
    // Felling trees with no wood word (« syncs bûcheronne »): wood.
    private static readonly string[] LumberPhrases =
    {
        "bucheronne", "bucheronner", "fais la bucheronne", "fais le bucheron", "joue a la bucheronne", "mode bucheronne", "lumberjack",
    };
    // A resource asked for with no verb (« syncs du bois », « syncs 20 bois », « il faut de la pierre »): every other word one of
    // these, and at least a partitive, a quantity or a number. « syncs bois » alone is « drink ».
    private static readonly HashSet<string> BareAskFiller = new()
    {
        "du", "de", "d", "des", "la", "le", "l", "les", "un", "une", "encore", "plus", "peu", "demi", "moi", "nous", "me", "m",
        "pour", "il", "faut", "faudrait", "je", "j", "veux", "voudrais", "aimerais", "peux", "pourrais", "avoir", "on", "veut",
        "besoin", "a", "vite", "ok", "en", "aussi", "please",
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
        "feu de camp", "plante la tente", "monte la tente", "on bivouaque", "bivouac", "make camp",
    };
    private static readonly string[] UncampPhrases =
    {
        "demonte le camp", "demonte ton camp", "demonte le campement", "demonter le camp", "plie le camp", "replie le camp",
        "range le camp", "leve le camp", "defais le camp", "enleve le camp", "detruis le camp", "casse le camp", "pack up camp",
        "demonte ton campement", "plie le campement", "range le campement", "range ton campement", "range ton camp",
        "leve le campement", "detruis le campement", "plie la tente", "demonte la tente", "demonte le feu de camp",
    };
    private static readonly string[] RanchAlonePhrases =
    {
        "fais de l elevage", "faire de l elevage", "mode eleveuse", "mode elevage", "fais l eleveuse", "joue a l eleveuse",
    };
    private static readonly string[] RanchVerbs =
    {
        "occupe toi", "occupes toi", "occupe", "t occuper", "s occuper", "prends soin", "prend soin", "prendre soin",
        "entretiens", "entretient", "entretenir", "nourris", "nourrir", "nourrit", "eleve", "gere", "geres", "gerer",
        // Shearing and breeding are what a ranch does.
        "tonds", "tond", "tondre", "reproduis", "reproduire", "fais se reproduire", "fais reproduire", "accouple", "accoupler",
        "take care of", "feed", "breed", "shear",
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
    // Remembered places (Words.Places): GoToPhrases plus the ways back. Kept here, next to what it
    // reads: a static initializer never reads a field in another partial file.
    private static readonly string[] GoToPlaceVerbs = GoToPhrases.Concat(new[]
    {
        "rentre", "rentres", "rentrer", "retourne", "retournes", "retourner", "repars", "rejoins", "rejoint", "go back", "head to",
        "back to",
    }).ToArray();
    private static readonly string[] FollowPhrases =
    {
        "suis moi", "suis nous", "suivez moi", "suis", "suit moi", "suis moi stp", "suivre", "suit nous",
        "accompagne moi", "accompagne nous", "viens avec moi", "viens avec nous", "colle moi", "dans mes pas",
        "derriere moi", "a mes cotes", "me suivre", "nous suivre", "m accompagner", "venir avec moi", "me suives", "suives",
        "follow", "follow me",
    };
    private static readonly string[] ComePhrases =
    {
        "viens", "vien", "vient", "venir", "vi1", "viens ici", "viens la", "viens voir", "viens me voir",
        "viens vers moi", "ici", "par ici", "approche", "rapproche toi", "rejoins moi", "rejoint moi", "rejoins nous",
        "rejoint nous", "ramene toi", "ramene ta fraise", "rapplique", "radine toi", "amene toi", "pointe toi",
        "au pied", "venir ici", "me rejoindre", "nous rejoindre", "rejoindre", "t approcher", "approcher",
        "te rapprocher", "venir me voir", "reviens", "revient", "reviens ici", "reviens la", "reviens vers moi", "revenir",
        "grouille", "grouille toi", "depeche toi", "depeche", "magne toi", "t arrives", "tu arrives", "tu arrive",
        "viennes", "retrouve moi", "retrouve nous", "me retrouver", "nous retrouver", "tp sur moi", "tp moi", "tp a moi",
        "cmon", "come on", "come", "come here", "here",
    };

    private static readonly (McResource Resource, string[] Words)[] ResourceWords =
    {
        (McResource.Wood, new[] { "bois", "buche", "buches", "tronc", "troncs", "arbre", "arbres", "chene", "chenes", "bouleau",
            "bouleaux", "sapin", "sapins", "epicea", "epiceas", "acacia", "acacias", "log", "logs", "wood", "tree", "trees" }),
        (McResource.Stone, new[] { "pierre", "pierres", "roche", "roches", "caillou", "cailloux", "cobble",
            "cobblestone", "stone" }),
        (McResource.Coal, new[] { "charbon", "charbons", "coal" }),
        (McResource.Iron, new[] { "fer", "iron" }),
        (McResource.Copper, new[] { "cuivre", "copper" }),
        (McResource.Gold, new[] { "or", "gold" }),
        (McResource.Diamond, new[] { "diamant", "diamants", "diams", "diam", "dia", "diamond", "diamonds" }),
        (McResource.Dirt, new[] { "terre", "dirt" }),
        (McResource.Sand, new[] { "sable", "sand" }),
        (McResource.Gravel, new[] { "gravier", "graviers", "gravillons", "gravel" }),
        (McResource.Clay, new[] { "argile", "argiles", "clay" }),
        (McResource.Redstone, new[] { "redstone", "redstones", "redstoune" }),
        (McResource.Lapis, new[] { "lapis", "lazuli", "lapiz" }),
        (McResource.Emerald, new[] { "emeraude", "emeraudes", "emerald", "emeralds", "emeraldes" }),
        (McResource.Quartz, new[] { "quartz", "quarz" }),
        (McResource.Granite, new[] { "granite", "granit", "granites" }),
        (McResource.Diorite, new[] { "diorite", "diorites" }),
        (McResource.Andesite, new[] { "andesite", "andesites" }),
    };

    // A coordinate standing alone: « 120 », « -300 », « x:120 », or F3's « 120.523 » (floored to the block).
    // Not the digits inside a name (Bob123).
    private static readonly Regex Coordinate = new(@"(?<![\w.])-?\d{1,8}(?:\.\d+)?(?![\w.])", RegexOptions.Compiled);
    // A count: « 10 », « x10 », « 10x ».
    private static readonly Regex Count = new(@"(?<![\w.])x?(\d{1,4})x?(?![\w.])", RegexOptions.Compiled);
    // A count in stacks: « 2 stacks », « 1 stack » (64 each), before the count itself, which would read « 2 ».
    private static readonly Regex StackCount = new(@"(?<![\w.])(\d{1,3})\s*stacks?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

    // Common words one slip from a list word, typed as themselves: never read as that word (Same). « reviens » was
    // « retiens » (she saved a place instead of coming), « jardin » was « jardine », « entre » was « rentre ».
    private static readonly HashSet<string> NoSlip = new()
    {
        "reviens", "revient", "revenir", "jardin", "jardins", "entre", "coucher", "couche", "dormi", "fermer", "manger", "manges",
        "poches", "potage", "tendre", "bisou",
    };
    // An order for later, or asking why, is talk: « tu peux venir demain ? », « pourquoi tu me suis ? ».
    private static readonly HashSet<string> NotNowWords = new() { "demain", "hier", "pourquoi" };
    // Words between a subject and its verb that still leave the verb theirs (« j'ai déjà coupé », « t'as bien dormi »).
    private static readonly HashSet<string> PastAdverbs = new()
    {
        "deja", "bien", "pas", "jamais", "encore", "trop", "mal", "tout", "enfin", "presque", "vraiment", "toujours", "souvent",
    };
    private static readonly HashSet<string> Auxiliaries = new() { "ai", "as", "avons", "avez", "ont", "avais", "avait", "avions", "aviez", "avaient" };
    // « je vais chercher du bois », « je dois couper du bois »: the player's own plans, like « je » right before.
    private static readonly HashSet<string> SelfModals = new()
    {
        "vais", "dois", "peux", "veux", "voudrais", "pourrais", "devrais", "compte", "pense", "viens", "sais", "prefere", "aime",
        "adore", "deteste", "essaie", "essaye", "commence", "continue", "reviens",
    };
    private static readonly HashSet<string> ThirdPersons = new() { "il", "ils", "elle", "elles" };
    // « t'aimes pêcher ? », « tu sais pêcher ? »: asking her, after « tu », not telling her to. Not « aime » (« je t'aime,
    // suis-moi » lost its comma).
    private static readonly HashSet<string> LikeVerbs = new() { "aimes", "adores", "preferes", "detestes", "kiffes", "sais" };

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
