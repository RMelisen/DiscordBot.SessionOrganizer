using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

/// <summary>What a player asked her in Minecraft's chat. Not stored: free to reorder.</summary>
public enum McOrder
{
    /// <summary>Not for her, or a refusal (« viens pas »): she says nothing.</summary>
    None,
    /// <summary>Called by name with nothing she understands: she answers « oui ? » and hears the player's next message.</summary>
    Unknown,
    Come,
    Follow,
    Stay,
    /// <summary>Walk to coordinates (<see cref="McRequest.Coordinates"/>).</summary>
    GoTo,
    /// <summary>Follow the player and fight the monsters around them.</summary>
    Guard,
    /// <summary>Gather <see cref="McRequest.Count"/> of <see cref="McRequest.Resource"/> and bring it back.</summary>
    Collect,
    /// <summary>Find a bed and sleep.</summary>
    Sleep,
    /// <summary>Harvest the ripe <see cref="McRequest.Crop"/> around her, replant, and bring it back.</summary>
    Harvest,
    /// <summary>Stay where she is and keep the farm: harvest what ripens, replant, store it in the chest.</summary>
    Farm,
    /// <summary>Hand the player what she carries (<see cref="McRequest.Goods"/>; null: something she doesn't know).</summary>
    Give,
    /// <summary>Put what she carries in the chest nearby, keeping her tools.</summary>
    Store,
    /// <summary>Say where she is. Takes her from nobody.</summary>
    Where,
    /// <summary>Crouch and stay put.</summary>
    Sit,
    /// <summary>Hop. Takes her from nobody.</summary>
    Jump,
    /// <summary>A greeting (« salut syncs », « fais coucou »): she waves back. Takes her from nobody.</summary>
    Wave,
    /// <summary>Remind the player in <see cref="McRequest.Seconds"/>, of <see cref="McRequest.Note"/> if any. Takes her from nobody.</summary>
    Timer,
    /// <summary>Fish until told otherwise.</summary>
    Fish,
    /// <summary>Guard a spot rather than a player: where she stands, or <see cref="McRequest.Place"/>.</summary>
    Patrol,
    /// <summary>Put <see cref="McRequest.Smelt"/> she carries in a furnace nearby and bring back the result.</summary>
    Smelt,
    /// <summary>Remember where the player stands as <see cref="McRequest.Place"/> (null: no name given).</summary>
    SavePlace,
    /// <summary>Walk to a remembered place (<see cref="McRequest.Place"/>; empty: home).</summary>
    GoToPlace,
    /// <summary>Lead the player to a remembered place, waiting when they fall behind.</summary>
    Guide,
    /// <summary>Say where a remembered place is. Takes her from nobody.</summary>
    WherePlace,
    /// <summary>Forget a remembered place. Takes her from nobody.</summary>
    ForgetPlace,
    /// <summary>List the places she remembers. Takes her from nobody.</summary>
    ListPlaces,
}

/// <summary>What she can be asked to hand over. The body knows each by its lower-case name. Not stored.</summary>
public enum McGoods
{
    /// <summary>Everything in her pockets (not what she wears).</summary>
    All,
    Food,
    Fish,
    Weapons,
    Tools,
    Pickaxe,
    Axe,
    Shovel,
    Rod,
    Bow,
    Arrows,
    /// <summary>Her armour, the pieces she wears included.</summary>
    Armour,
    Seeds,
    /// <summary>What a field gives: wheat, carrots, potatoes, beetroot, melon slices, pumpkins.</summary>
    Produce,
    Wood,
    Stone,
    Coal,
    Iron,
    Copper,
    Gold,
    Diamond,
    Dirt,
    Sand,
}

/// <summary>What she can be asked to put in a furnace. The body knows each by its lower-case name. Not stored.</summary>
public enum McSmelt
{
    /// <summary>Raw meat, fish, potatoes, kelp.</summary>
    Food,
    /// <summary>Raw iron, gold and copper, and their ores.</summary>
    Ore,
    /// <summary>Sand, for glass.</summary>
    Sand,
}

/// <summary>What she can be sent to harvest. The body knows each by its lower-case name. Not stored.</summary>
public enum McCrop
{
    /// <summary>Whatever is ripe.</summary>
    Any,
    Wheat,
    Carrot,
    Potato,
    Beetroot,
    Melon,
    Pumpkin,
}

/// <summary>What she can be sent to gather. The body knows each by its lower-case name. Not stored.</summary>
public enum McResource
{
    Wood,
    Stone,
    Coal,
    Iron,
    Copper,
    Gold,
    Diamond,
    Dirt,
    Sand,
}

/// <summary>An order, with what it needs: the coordinates for GoTo (2 numbers: x z; 3: x y z), the resource and count
/// for Collect, the crop for Harvest, the goods for Give, what to smelt, a place's key (<see cref="McCommand.PlaceKey"/>),
/// a timer's length and note.</summary>
public readonly record struct McRequest(McOrder Order, int[]? Coordinates = null, McResource? Resource = null, int Count = 0,
    McCrop? Crop = null, McGoods? Goods = null, McSmelt? Smelt = null, string? Place = null, int Seconds = 0, string? Note = null)
{
    public static readonly McRequest Nothing = new(McOrder.None);
}

/// <summary>
/// Reads in-game chat for an order addressed to her. Pure, so the checks walk every phrasing.
/// The words go through <see cref="Rating.Normalize"/> first: lower case, no accents, punctuation
/// collapsed, so « Suis-moi ! » and « suis moi » are the same words. Numbers are read from the raw text,
/// where a minus sign still means something.
/// </summary>
/// <remarks>
/// <para>An order needs her name somewhere in the message (« syncs viens », « viens syncs »): the game
/// chat is shared, and « viens voir » between two players isn't for her. A message that only names
/// her (« syncs est nulle ») is Unknown only when the name opens or closes it, as a call does;
/// anywhere else it's talk about her, and she lets it go. After such a call, the same player's next
/// message is read with <c>called</c> set: as if it carried her name (« syncs » … « suis moi »).</para>
/// <para>A refusal is read around the verb only: « ne » just before it, « pas » or « jamais » just
/// after (« viens pas », « ne me suis pas », « ne va pas chercher »). A « pas » elsewhere in the
/// message doesn't cancel anything (« c'est pas grave, syncs viens »), and « plus de bois » is more
/// wood, not a refusal.</para>
/// <para>The lists are written the way people type in game chat: familiar French (« ramène ta fraise »,
/// « fais du bois »), common shortcuts (« vien », « diams »), a few English words. Add the misses
/// found in the wild, with a case in the checks.</para>
/// </remarks>
public static class McCommand
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
        "drop all", "drop everything", "give me everything", "give me all", "empty your pockets", "empty your inventory",
    };
    private static readonly string[] GiveVerbs =
    {
        "donne moi", "donne nous", "donne", "donnes moi", "file moi", "file nous", "passe moi", "passe nous", "lance moi",
        "jette moi", "rends moi", "rend moi", "rends nous", "envoie moi", "balance moi", "prete moi", "file", "passe", "rends",
        "lache", "jette", "me donner", "me passer", "me rendre", "me filer", "me preter", "nous donner", "give me", "give", "drop",
        "toss me", "hand me",
    };
    // The player's things or hers (« rends-moi mes diamants », « donne-moi ton fer »): what she carries, never something to fetch.
    private static readonly string[] Possessives = { "ton", "ta", "tes", "votre", "vos", "mon", "ma", "mes", "your", "my" };
    private static readonly string[] FishWords =
    {
        "poisson", "poissons", "poiscaille", "saumon", "saumons", "morue", "morues", "cabillaud", "cabillauds", "fish", "salmon", "cod",
    };
    private static readonly (McGoods Goods, string[] Words)[] GoodsWords =
    {
        (McGoods.All, new[] { "stuff", "affaires", "inventaire", "truc", "trucs", "items", "objets", "loot", "butin", "inventory" }),
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
        "range tes affaires", "range ton stuff", "range ton inventaire", "range tout", "range", "ranges", "ranger",
        "depose tout", "depose tes affaires", "depose", "deposer", "stocke", "stocker", "stock", "store", "stash", "deposit",
    };
    // With a chest word, putting anything away is storing it (« vide tes poches dans le coffre »).
    private static readonly string[] ChestWords = { "coffre", "coffres", "tonneau", "tonneaux", "baril", "barils", "chest", "chests", "barrel", "barrels" };
    private static readonly string[] IntoChestVerbs = GiveVerbs.Concat(new[]
    {
        "mets", "met", "mettre", "vide", "vides", "jette", "balance", "fous", "fourre", "range", "depose", "put", "dump",
    }).Distinct().ToArray();

    // ---- Fishing and cooking ----

    private static readonly string[] FishPhrases =
    {
        // Not « pêches »: one slip from « poches ».
        "peche", "pecher", "va pecher", "aller pecher", "va a la peche", "fais de la peche", "part a la peche",
        "a la peche", "pecheuse", "mode peche", "fish", "fishing", "go fish", "go fishing",
    };
    private static readonly string[] SmeltPhrases =
    {
        "cuis", "cuire", "fais cuire", "faire cuire", "fait cuire", "cuisiner", "fais griller", "griller", "fais rotir",
        "fais fondre", "faire fondre", "fondre", "fonds", "smelt", "cook", "enfourne", "mets au four", "met au four",
        "mets a cuire", "utilise le four", "fais du verre", "faire du verre",
    };
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
        "asseoir", "pose toi", "sit", "sit down",
    };
    private static readonly string[] JumpPhrases = { "saute", "sautes", "sauter", "sautille", "fais un saut", "fais des sauts", "bondis", "jump" };
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
        "coordonnees", "coords", "position", "where are you", "where r u", "your coords", "your position",
    };

    // ---- Timers ----

    private static readonly string[] TimerVerbs =
    {
        "previens moi", "previens nous", "previens", "prevenir", "me prevenir", "rappelle moi", "rappelle nous", "me rappeler",
        "nous rappeler", "dis moi", "fais moi signe", "ping moi", "reveille moi", "alerte moi", "sonne", "timer", "minuteur",
        "chrono", "chronometre", "alarme", "compte a rebours", "remind me", "ping me", "tell me", "warn me",
    };
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

    /// <summary>How many places she remembers at most, and how long a name may be.</summary>
    public const int MaxPlaces = 20;
    public const int MaxPlaceName = 24;
    private static readonly string[] SaveVerbs =
    {
        "retiens", "retient", "retenir", "memorise", "memoriser", "enregistre", "enregistrer", "sauvegarde", "sauvegarder",
        "souviens toi de", "souviens toi d", "souviens toi", "rappelle toi de", "rappelle toi d", "rappelle toi", "remember", "save",
    };
    // These save a place only right before « ici » or « cet endroit » (« note ici : la mine »): alone they mean other things.
    private static readonly string[] SaveLooseVerbs = { "note", "noter", "marque", "marquer", "nomme", "nommer", "appelle", "baptise", "mark", "name", "call" };
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
        "tu connais quels endroits", "your places", "list places",
    };
    private static readonly string[] WherePlaceVerbs =
    {
        "ou est", "ou sont", "ou se trouve", "ou se trouvent", "c est ou", "cest ou", "c est par ou", "coordonnees de",
        "coordonnees du", "coords de", "coords du", "position de", "position du", "where is",
    };
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
        "animaux", "betes", "qg", "lieu",
    };

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

    /// <summary>The order alone, for callers that don't need its details.</summary>
    public static McOrder Read(string text, IReadOnlyCollection<string> names) => Parse(text, names).Order;

    /// <param name="text">The chat message as typed.</param>
    /// <param name="names">Every name she answers to (her in-game name, « syncs »), any case.</param>
    /// <param name="called">The player called her just before (a bare « syncs »): her name isn't needed this time,
    /// and a message with nothing she understands is let go rather than read as another call.</param>
    /// <param name="places">The keys of the places she remembers (<see cref="PlaceKey"/>), so « va à la mine » finds one.</param>
    public static McRequest Parse(string text, IReadOnlyCollection<string> names, bool called = false, IReadOnlyCollection<string>? places = null)
    {
        var words = Clean(Rating.Normalize(text));
        if (words.Length == 0) return McRequest.Nothing;

        // Her name, or a one-letter slip of it (« sycns », « sincs ») when it's long enough not to be another word.
        var nameWords = names.Select(Rating.Normalize).Where(n => n.Length > 0 && !n.Contains(' ')).ToHashSet();
        var at = Array.FindIndex(words, w => nameWords.Contains(w) || (w.Length >= 5 && nameWords.Any(n => n.Length >= 5 && Close(w, n))));
        if (at < 0 && !called) return McRequest.Nothing;
        var rest = words.Where((w, i) => i != at).ToArray();
        var calledOut = at >= 0 && (at == 0 || at == words.Length - 1);
        var numbers = Coordinate.Matches(text)
            .Select(m => (int)Math.Floor(double.Parse(m.Value, CultureInfo.InvariantCulture))).ToArray();
        var counts = Count.Matches(text).Select(m => int.Parse(m.Groups[1].Value)).ToArray();
        // Words other than numbers: « syncs 120 64 -300 » is coordinates and nothing else.
        var wordsOnly = rest.Where(w => !w.All(char.IsDigit) && !IsCountToken(w)).ToArray();

        // An order, unless refused around its verb (null: refused, so she says nothing at all).
        McRequest? When(string[] phrases, McRequest request) =>
            Find(wordsOnly, phrases) is not { } hit ? McRequest.Nothing
            : Refused(wordsOnly, hit) ? null
            : request;

        // An order whose verb was found, unless refused around it (« ne retiens pas »): then nothing at all.
        McRequest Unless((int Start, int Length) hit, string[] around, McRequest request) =>
            Refused(around, hit) ? McRequest.Nothing : request;
        var known = places ?? Array.Empty<string>();
        var place = PlaceIn(rest, known);

        // A timer first: its note can say anything (« rappelle-moi d'arrêter le four » is not stay).
        if (Find(wordsOnly, TimerVerbs) is { } timerVerb && DurationIn(text) is { } seconds)
            return Unless(timerVerb, wordsOnly, new(McOrder.Timer, Seconds: seconds, Note: TimerNote(text, names)));

        // Places: remembering one, forgetting one, listing them. Read on the words with their numbers (« base 2 »).
        if (SaveAt(rest) is { } save)
            return Unless(save.Verb, rest, new(McOrder.SavePlace, Place: NameAfter(rest, save.NameFrom)));
        if (Find(rest, ForgetVerbs) is { } forget && NameAfter(rest, forget.Start + forget.Length) is { } forgotten)
            return Unless(forget, rest, new(McOrder.ForgetPlace, Place: place?.Key ?? forgotten));
        if (Find(wordsOnly, ListPlacesPhrases) is { } listing) return Unless(listing, wordsOnly, new(McOrder.ListPlaces));

        if (Find(wordsOnly, FollowOverStayPhrases) is not null) return When(FollowOverStayPhrases, new(McOrder.Follow)) ?? McRequest.Nothing;
        if (Find(wordsOnly, FarmAlonePhrases) is not null) return When(FarmAlonePhrases, new(McOrder.Farm)) ?? McRequest.Nothing;
        if (FarmCare(wordsOnly) is { } care) return Refused(wordsOnly, care) ? McRequest.Nothing : new(McOrder.Farm);

        // Guarding a place, before stay and guard: « protège la base » is not « protège-moi ». A remembered place named
        // with it is where she guards (« surveille la mine »); otherwise, where she stands.
        var placeWord = place is { } p ? p.Key.Split(' ')[0] : null;
        if (Find(wordsOnly, PatrolAlonePhrases) is { } patrolAlone)
            return Unless(patrolAlone, wordsOnly, new(McOrder.Patrol, Place: place?.Key));
        if (CareFor(wordsOnly, PatrolVerbs, (w, j) => BaseWords.Contains(w[j]) || w[j] == placeWord, skipMe: true) is { } patrol)
            return Unless(patrol, wordsOnly, new(McOrder.Patrol, Place: place?.Key));

        if (Find(wordsOnly, SitPhrases) is { } sit) return Unless(sit, wordsOnly, new(McOrder.Sit));
        if (Find(wordsOnly, StayPhrases) is not null) return new(McOrder.Stay);

        foreach (var (phrases, order) in new[] { (SleepPhrases, McOrder.Sleep), (GuardPhrases, McOrder.Guard) })
        {
            var found = When(phrases, new(order));
            if (found is null) return McRequest.Nothing;
            if (found.Value.Order != McOrder.None) return found.Value;
        }

        // What she carries: handed over, or put in a chest. Before gathering, so « donne-moi ton fer » isn't a trip to the
        // mine. Handing over is read before storing, whose « ranger » is one slip from « manger » (« donne-moi à manger »);
        // with a chest word, it's all storing (« jette tes poissons dans le coffre »).
        var chest = wordsOnly.Any(w => ChestWords.Contains(w));
        var give = Find(wordsOnly, GiveVerbs);
        if (!chest && Find(wordsOnly, GiveAllPhrases) is { } giveAll) return Unless(giveAll, wordsOnly, new(McOrder.Give, Goods: McGoods.All));
        if (!chest && give is { } giveVerb && GoodsAfter(wordsOnly, giveVerb.Start + giveVerb.Length) is { Goods: { } goods } asked
            && (asked.Owned || CarriedOnly.Contains(goods)))
            return Unless(giveVerb, wordsOnly, new(McOrder.Give, Goods: goods));
        if (Find(wordsOnly, StorePhrases) is { } stored) return Unless(stored, wordsOnly, new(McOrder.Store));
        if (chest && Find(wordsOnly, IntoChestVerbs) is { } intoChest) return Unless(intoChest, wordsOnly, new(McOrder.Store));

        // Fishing: « va pêcher », or fish asked for with a gathering verb (« ramène du poisson »).
        if (Find(wordsOnly, FishPhrases) is { } fishing) return Unless(fishing, wordsOnly, new(McOrder.Fish));
        if (wordsOnly.Any(w => FishWords.Contains(w)) && Find(wordsOnly, CollectPhrases) is { } fishFetch)
            return Unless(fishFetch, wordsOnly, new(McOrder.Fish));

        // Cooking, before crops and resources: « cuis les patates », « fais fondre le fer ».
        if (Find(wordsOnly, SmeltPhrases) is { } smelt) return Unless(smelt, wordsOnly, new(McOrder.Smelt, Smelt: SmeltIn(wordsOnly, smelt)));

        // A place she knows, named after a going verb, before gathering: « va à la mine de fer » has « mine » and « fer ».
        if (place is { } knownPlace)
            foreach (var (verbs, order) in new[] { (WherePlaceVerbs, McOrder.WherePlace), (GuideVerbs, McOrder.Guide), (GoToPlaceVerbs, McOrder.GoToPlace) })
                if (Find(rest, verbs) is { } placeVerb && placeVerb.Start + placeVerb.Length <= knownPlace.At)
                    return Unless(placeVerb, rest, new(order, Place: knownPlace.Key));

        // A crop, before the resources: « pommes de terre » has « terre » (dirt).
        var resourceAt = Array.FindIndex(wordsOnly, w => ResourceWords.Any(r => r.Words.Contains(w)));
        // The verb must be another word than the crop: « la récolte est prête » names the harvest, it doesn't ask for it.
        if (CropIn(wordsOnly) is { } crop && (crop.Crop != McCrop.Any || resourceAt < 0)
            && Find(wordsOnly.Select((w, i) => i == crop.At ? "" : w).ToArray(), CropVerbs) is { } verb)
            return Refused(wordsOnly, verb) ? McRequest.Nothing : new(McOrder.Harvest, Crop: crop.Crop);

        if (resourceAt >= 0)
        {
            var wanted = ResourceWords.First(r => r.Words.Contains(wordsOnly[resourceAt])).Resource;
            var count = counts.FirstOrDefault(n => n > 0);
            if (count == 0) count = QuantityBefore(wordsOnly, resourceAt);
            var collect = When(CollectPhrases,
                new(McOrder.Collect, Resource: wanted, Count: Math.Clamp(count == 0 ? DefaultCount : count, 1, MaxCount)));
            if (collect is null) return McRequest.Nothing;
            if (collect.Value.Order != McOrder.None) return collect.Value;
        }
        // « syncs récolte », « syncs moissonne »: whatever is ripe. Not with a resource (« récolte du sable » is
        // sand), and not « récolte » after an article, which is the noun (« la récolte est prête »).
        else if (Find(wordsOnly, HarvestPhrases) is { } said
                 && !(said.Length == 1 && wordsOnly[said.Start] is "recolte" or "recoltes" && said.Start > 0
                      && wordsOnly[said.Start - 1] is "la" or "ta" or "ma" or "sa" or "une" or "les" or "des" or "tes" or "mes" or "ses" or "notre" or "votre"))
        {
            var harvest = When(HarvestPhrases, new(McOrder.Harvest, Crop: McCrop.Any));
            if (harvest is null) return McRequest.Nothing;
            if (harvest.Value.Order != McOrder.None) return harvest.Value;
        }

        if (numbers.Length is 2 or 3)
        {
            if (wordsOnly.Length == 0) return new(McOrder.GoTo, Coordinates: numbers);
            foreach (var phrases in new[] { GoToPhrases, ComePhrases })
            {
                var goTo = When(phrases, new(McOrder.GoTo, Coordinates: numbers));
                if (goTo is null) return McRequest.Nothing;
                if (goTo.Value.Order != McOrder.None) return goTo.Value;
            }
        }

        // Remembered places: where one is, the way there with the player, or the walk alone. A place she knows anywhere
        // after the verb, else the name after a preposition (« va à la mine »), which the service may not know.
        if (Find(rest, WherePlaceVerbs) is { } whereIs && PlaceAfter(rest, whereIs, place, needsPreposition: false) is { } asked1)
            return Unless(whereIs, rest, new(McOrder.WherePlace, Place: asked1));
        if (Find(rest, GuideVerbs) is { } guide && PlaceAfter(rest, guide, place, needsPreposition: true) is { } asked2)
            return Unless(guide, rest, new(McOrder.Guide, Place: asked2));
        if (Find(rest, GoToPlaceVerbs) is { } goPlace)
        {
            if (PlaceAfter(rest, goPlace, place, needsPreposition: true) is { } asked3)
                return Unless(goPlace, rest, new(McOrder.GoToPlace, Place: asked3));
            // « rentre », « rentre chez nous »: home.
            if (Find(rest, HomeVerbs) is { } home && rest.Skip(home.Start + home.Length).All(w => NameFiller.Contains(w) || NameStop.Contains(w)))
                return Unless(home, rest, new(McOrder.GoToPlace, Place: ""));
        }

        foreach (var (phrases, order) in new[] { (FollowPhrases, McOrder.Follow), (ComePhrases, McOrder.Come) })
        {
            var found = When(phrases, new(order));
            if (found is null) return McRequest.Nothing;
            if (found.Value.Order != McOrder.None) return found.Value;
        }

        // The small things, last: a greeting with an order in it is the order.
        foreach (var (phrases, order) in new[] { (JumpPhrases, McOrder.Jump), (WherePhrases, McOrder.Where), (WavePhrases, McOrder.Wave) })
        {
            var found = When(phrases, new(order));
            if (found is null) return McRequest.Nothing;
            if (found.Value.Order != McOrder.None) return found.Value;
        }
        // « donne-moi ta clé »: something of hers she doesn't know. She says so rather than « oui ? ».
        if (give is { } unknownGive && GoodsAfter(wordsOnly, unknownGive.Start + unknownGive.Length) is { Goods: null, Owned: true })
            return Unless(unknownGive, wordsOnly, new(McOrder.Give));
        return calledOut && rest.Length <= 6 ? new(McOrder.Unknown) : McRequest.Nothing;
    }

    /// <summary>A place's key: its name as the parser reads it (lower case, no accents, game-chat spellings mapped).</summary>
    public static string PlaceKey(string name) => string.Join(' ', Clean(Rating.Normalize(name)));

    /// <summary>
    /// A place's name as the player wrote it, accents kept, for her lines: the words of <paramref name="text"/> that
    /// read as <paramref name="key"/>, in lower case. The key itself when they can't be found (a name typed with stretched
    /// letters, say). Never longer than <see cref="MaxPlaceName"/>, and never with Minecraft's colour sign.
    /// </summary>
    public static string PlaceName(string text, string key)
    {
        var parts = key.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var raw = Regex.Matches(text, @"[\p{L}\p{N}]+").Select(m => m.Value).ToArray();
        var folded = raw.Select(Rating.Normalize).ToArray();
        for (var i = 0; parts.Length > 0 && i + parts.Length <= raw.Length; i++)
            if (parts.Select((p, k) => folded[i + k] == p).All(x => x))
            {
                var name = string.Join(' ', raw.Skip(i).Take(parts.Length)).ToLowerInvariant();
                return name.Length <= MaxPlaceName ? name : key;
            }
        return key;
    }

    // A known place named in the words (« la mine de fer » before « la mine »), with where it starts.
    private static (string Key, int At)? PlaceIn(string[] words, IReadOnlyCollection<string> places)
    {
        foreach (var key in places.OrderByDescending(k => k.Length))
        {
            var parts = key.Split(' ');
            for (var i = 0; i + parts.Length <= words.Length; i++)
                if (parts.Select((p, k) => words[i + k] == p).All(x => x)) return (key, i);
        }
        return null;
    }

    // The place an order is about: a known one after its verb, else the name after a preposition (« va à la mine »,
    // « emmène-moi jusqu'à la base »), else nothing. Where-is takes a name after the verb directly (« où est la mine »).
    private static string? PlaceAfter(string[] words, (int Start, int Length) verb, (string Key, int At)? known, bool needsPreposition)
    {
        var after = verb.Start + verb.Length;
        if (known is { } k && k.At >= after) return k.Key;
        var last = words[after - 1];
        var preposition = Prepositions.Contains(last) || (after < words.Length && Prepositions.Contains(words[after]));
        if (needsPreposition && !preposition) return null;
        if (!needsPreposition && !preposition && (after >= words.Length || !Articles.Contains(words[after]))) return null;
        return NameAfter(words, after);
    }

    // A place's name after a verb: the filler skipped (« ici comme la »), then up to three words, up to « et » or « merci ».
    private static string? NameAfter(string[] words, int from)
    {
        var i = from;
        while (i < words.Length && NameFiller.Contains(words[i])) i++;
        var parts = new List<string>();
        for (; i < words.Length && parts.Count < 3 && !NameStop.Contains(words[i]); i++) parts.Add(words[i]);
        while (parts.Count > 0 && NameFiller.Contains(parts[^1])) parts.RemoveAt(parts.Count - 1);
        var name = string.Join(' ', parts);
        return name.Length == 0 ? null : name.Length <= MaxPlaceName ? name : name[..MaxPlaceName].TrimEnd();
    }

    // Remembering a place: a verb that says it (« retiens », « enregistre »), a looser one right before « ici » (« note
    // ici »), or « ici c'est » before an article (« ici c'est la mine »). Where the name may start.
    private static ((int Start, int Length) Verb, int NameFrom)? SaveAt(string[] words)
    {
        if (Find(words, SaveVerbs) is { } verb) return (verb, verb.Start + verb.Length);
        if (Find(words, SaveLooseVerbs) is { } loose && Find(words.Skip(loose.Start + loose.Length).ToArray(), HereWords) is { Start: 0 })
            return (loose, loose.Start + loose.Length);
        if (Find(words, HereIsPhrases) is { } hereIs && hereIs.Start + hereIs.Length < words.Length
            && Articles.Contains(words[hereIs.Start + hereIs.Length]))
            return (hereIs, hereIs.Start + hereIs.Length);
        return null;
    }

    // What follows a give verb (within three words): a possessive and a kind of goods (« ta canne à pêche », « tes pommes
    // de terre »), or goods with any article (« du poisson »). Goods null with Owned: something of hers she doesn't know.
    private static (McGoods? Goods, bool Owned)? GoodsAfter(string[] words, int from)
    {
        var owned = false;
        for (var j = from; j < Math.Min(words.Length, from + 4); j++)
        {
            if (Possessives.Contains(words[j])) { owned = true; continue; }
            if (words[j] is "pomme" or "pommes" or "pom" && j + 2 < words.Length && words[j + 1] is "de" or "d" && words[j + 2] == "terre")
                return (McGoods.Produce, owned);
            if (CropWords.Any(c => c.Crop != McCrop.Any && c.Words.Any(w => Same(words[j], w)))) return (McGoods.Produce, owned);
            foreach (var (goods, list) in GoodsWords)
                if (list.Contains(words[j])) return (goods, owned);
        }
        return owned ? (null, true) : null;
    }

    // What a cooking order is about: the first food, ore or sand word, else what the verb implies (melting is ore,
    // glass is sand, the rest is food).
    private static McSmelt SmeltIn(string[] words, (int Start, int Length) verb)
    {
        foreach (var w in words)
            foreach (var (smelt, list) in SmeltWords)
                if (list.Contains(w)) return smelt;
        var said = string.Join(' ', words.Skip(verb.Start).Take(verb.Length));
        return said.Contains("fond") || said == "smelt" ? McSmelt.Ore : said.Contains("verre") ? McSmelt.Sand : McSmelt.Food;
    }

    // A verb with one of its objects within four words after it (« surveille la base »). With skipMe, a verb followed by
    // « moi » or « nous » is about the player (« protège-moi à la maison » is guard, not patrol).
    private static (int Start, int Length)? CareFor(string[] words, string[] verbs, Func<string[], int, bool> isObject, bool skipMe = false)
    {
        foreach (var phrase in verbs.OrderByDescending(p => p.Length))
        {
            var parts = phrase.Split(' ');
            for (var i = 0; i + parts.Length <= words.Length; i++)
            {
                if (!parts.Select((p, k) => Same(words[i + k], p)).All(x => x)) continue;
                var after = i + parts.Length;
                if (skipMe && after < words.Length && words[after] is "moi" or "nous" or "me" or "m") continue;
                for (var j = after; j < Math.Min(words.Length, after + 4); j++)
                    if (isObject(words, j)) return (i, parts.Length);
            }
        }
        return null;
    }

    /// <summary>How long a timer runs, in seconds, from the raw text; null when no length of time is given.</summary>
    public static int? DurationIn(string text)
    {
        var m = Duration.Match(text);
        if (!m.Success) return null;
        if (m.Groups["quart"].Success) return 15 * 60;
        if (m.Groups["demi"].Success) return 30 * 60;
        var number = m.Groups["n"].Value.ToLowerInvariant();
        var n = int.TryParse(number, out var digits) ? digits : QuantityWords.GetValueOrDefault(number, 1);
        var unit = m.Groups["u"].Value.ToLowerInvariant();
        return n * (unit.StartsWith('h') ? 3600 : unit.StartsWith('m') ? 60 : 1);
    }

    // What to remind them of: the raw text after the first « de », « pour », « que » once the duration and her name are
    // out (« rappelle-moi dans 5 min de sortir le fer » → « sortir le fer »). Null when there's none.
    private static string? TimerNote(string text, IReadOnlyCollection<string> names)
    {
        var rest = Duration.Replace(text, " ");
        foreach (var name in names.Where(n => n.Length > 0))
            rest = Regex.Replace(rest, $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(name)}(?![\p{{L}}\p{{N}}])", " ", RegexOptions.IgnoreCase);
        var start = NoteStart.Match(rest);
        if (!start.Success) return null;
        var note = Regex.Replace(rest[(start.Index + start.Length)..].Replace("§", ""), @"\s+", " ").Trim().TrimEnd('?', '!', '.', ',', ';', ' ');
        if (note.Length < 2) return null;
        return note.Length <= NoteMax ? note : note[..NoteMax].TrimEnd() + "…";
    }

    // Where a phrase first matches, as whole consecutive words, so « suis » never matches inside
    // « suisse ». A lone « suis » needs to be the message's verb: « je suis là » is not « suis » + an
    // object. Longer phrases are tried first, so « reste avec moi » isn't read as « reste ».
    private static (int Start, int Length)? Find(string[] words, string[] phrases)
    {
        foreach (var phrase in phrases.OrderByDescending(p => p.Length))
        {
            var parts = phrase.Split(' ');
            if (parts is ["suis"] && (words.Contains("je") || words.Contains("j"))) continue;
            for (var i = 0; i + parts.Length <= words.Length; i++)
                if (parts.Select((p, k) => Same(words[i + k], p)).All(x => x)) return (i, parts.Length);
        }
        return null;
    }

    // A word of a phrase: exact, or one slip away for long verbs (« protge », « chercer », « rapplque »).
    // Short words must be exact: at four or five letters, one slip makes another word (« viens » / « liens »).
    private static bool Same(string word, string part) => word == part || (part.Length >= 6 && word.Length >= 5 && Close(word, part));

    // At most one edit apart: a letter added, dropped, changed, or two neighbours swapped.
    private static bool Close(string a, string b)
    {
        if (Math.Abs(a.Length - b.Length) > 1) return false;
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        return d[a.Length, b.Length] <= 1;
    }

    // Game-chat words to the lists' words: slang mapped (Slang), politeness dropped, and letters held down
    // for emphasis squeezed back (« viiiiens », « stoooop »: three or more of a letter become one).
    private static string[] Clean(string normalized)
    {
        var words = new List<string>();
        foreach (var raw in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var word = Regex.Replace(raw, @"(\p{L})\1{2,}", "$1");
            var mapped = Slang.TryGetValue(word, out var to) ? to : word;
            words.AddRange(mapped.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
        return words.ToArray();
    }

    // The crop a message names and where, the first one found; « pomme(s) de terre » is a potato, not an apple and
    // dirt. A crop and a generic word (« le blé du champ ») is the crop.
    private static (McCrop Crop, int At)? CropIn(string[] words)
    {
        for (var i = 0; i + 2 < words.Length; i++)
            if (words[i] is "pomme" or "pommes" or "pom" && words[i + 1] is "de" or "d" && words[i + 2] == "terre") return (McCrop.Potato, i);
        (McCrop, int)? generic = null;
        for (var i = 0; i < words.Length; i++)
            foreach (var (crop, list) in CropWords)
                if (list.Any(c => Same(words[i], c)))
                {
                    if (crop != McCrop.Any) return (crop, i);
                    generic ??= (crop, i);
                }
        return generic;
    }

    // A care verb with a farm word (or, after « occupe-toi » and the like, a crop) within four words after it.
    private static (int Start, int Length)? FarmCare(string[] words)
    {
        foreach (var phrase in FarmVerbs.OrderByDescending(p => p.Length))
        {
            var parts = phrase.Split(' ');
            var takesCrop = CropCareVerbs.Contains(phrase);
            for (var i = 0; i + parts.Length <= words.Length; i++)
            {
                if (!parts.Select((p, k) => Same(words[i + k], p)).All(x => x)) continue;
                var after = i + parts.Length;
                for (var j = after; j < Math.Min(words.Length, after + 4); j++)
                    if (IsFarmWord(words, j) || (takesCrop && CropWords.Any(c => c.Crop != McCrop.Any && c.Words.Any(w => Same(words[j], w)))))
                        return (i, parts.Length);
            }
        }
        return null;
    }

    // « ferme » is a farm only after an article or a possessive (« la ferme », « ma ferme »): « et ferme-la » is not.
    private static bool IsFarmWord(string[] words, int j) =>
        FarmWords.Any(f => Same(words[j], f))
        && (words[j] is not ("ferme" or "fermes")
            || j > 0 && words[j - 1] is "la" or "ma" or "ta" or "sa" or "notre" or "votre" or "les" or "mes" or "tes" or "nos"
                or "vos" or "une" or "cette" or "de");

    // « x10 », « 10x »: a count, not a word.
    private static bool IsCountToken(string word) => Regex.IsMatch(word, @"^x?\d+x?$");

    // A quantity written in words right before the resource, a « de » / « d' » / « du » / « des » allowed in
    // between: « dix bûches », « une stack de pierre », « une dizaine de bûches ». Zero when there's none.
    private static int QuantityBefore(string[] words, int resourceAt)
    {
        var j = resourceAt - 1;
        if (j >= 0 && words[j] is "de" or "d" or "du" or "des") j--;
        if (j < 0) return 0;
        if (j >= 1 && QuantityPairs.TryGetValue(words[j - 1] + " " + words[j], out var pair)) return pair;
        return QuantityWords.TryGetValue(words[j], out var single) ? single : 0;
    }

    // A refusal next to the verb: « ne » up to two words before (« ne me suis »), « pas » just before
    // (« ne va pas chercher »), « pas » or « jamais » up to two words after, or « plus » unless it means
    // more (« plus de bois »). Wider than that, a « pas » belongs to another sentence.
    private static bool Refused(string[] words, (int Start, int Length) hit)
    {
        for (var i = Math.Max(0, hit.Start - 2); i < hit.Start; i++)
            if (words[i] is "ne" or "n") return true;
        if (hit.Start > 0 && words[hit.Start - 1] == "pas") return true;
        var after = hit.Start + hit.Length;
        for (var i = after; i < Math.Min(words.Length, after + 2); i++)
        {
            if (words[i] is "pas" or "jamais") return true;
            if (words[i] == "plus" && (i + 1 >= words.Length || words[i + 1] is not ("de" or "d"))) return true;
        }
        return false;
    }
}
