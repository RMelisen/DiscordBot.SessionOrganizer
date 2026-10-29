using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

public static partial class McCommand
{
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
        "liste tes lieux", "t as quels endroits", "tu as quels endroits", "t as quels lieux", "lieux connus", "endroits connus",
        "tes waypoints", "waypoints", "tes spots", "tes reperes", "reperes",
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
        "connais le chemin", "tu connais le chemin", "show me the way", "show me", "take me", "lead me", "lead the way",
    };
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
        "sentinelle", "mode sentinelle", "fais le guet", "faire le guet", "guette", "guetter", "patrol", "sentry", "guard here",
        "keep watch",
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
}
