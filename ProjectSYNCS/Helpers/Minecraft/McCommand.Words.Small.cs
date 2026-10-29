using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectSYNCS.Helpers;

public static partial class McCommand
{
    // ---- Small things: sitting, hopping, waving, telling where she is ----

    private static readonly string[] SitPhrases =
    {
        "assis", "assise", "assieds toi", "assied toi", "assis toi", "assoie toi", "assois toi", "t asseoir", "s asseoir",
        "asseoir", "pose toi", "couchee", "accroupis toi", "accroupie", "accroupis", "baisse toi", "a genoux", "mets toi a genoux",
        "sit", "sit down", "crouch",
    };
    // A dance is hopping too: it's all she can do with square legs.
    private static readonly string[] JumpPhrases =
    {
        "saute", "sautes", "sauter", "sautille", "fais un saut", "fais des sauts", "fais des bonds", "bondis", "danse", "danser",
        "fais une danse", "salto", "fais un salto", "backflip", "twerk", "twerke", "fais la fete", "jump", "dance",
    };
    // Goodbyes: she waves too, with a goodbye line.
    private static readonly string[] ByePhrases =
    {
        "bonne nuit", "au revoir", "aurevoir", "bye", "bye bye", "ciao", "tchao", "a plus", "a toute", "a demain", "a la prochaine",
        "bisous", "adieu", "bonne soiree", "bonne journee", "bonne fin de soiree", "a tout a l heure", "a tantot", "a la revoyure",
        "je deco", "je me deco", "je vais deco", "je dois y aller", "je vais y aller", "je vais dormir", "je vais me coucher",
        "je vais au lit", "je vais dodo", "see you", "see ya", "good night", "gn",
    };
    // « a+ », « @+ », « ++ »: Normalize drops the « + », so they become « à plus » before it.
    private static readonly Regex SeeYou = new(@"(?<![\p{L}\p{N}+])(?:[aà@]\s?\+|\+\+)(?![\p{L}\p{N}+])", RegexOptions.Compiled);
    // Compliments and thanks: read only where a call would be (McCommand.ParseWords), so « bravo syncs » isn't « oui ? ».
    // Interjections alone; an adjective only said to her (« t'es géniale »), as « syncs la base est jolie » is about the
    // base. Not « forte » alone: « syncs t'es assez forte » is a dare, not a compliment.
    private static readonly string[] PraisePhrases =
    {
        "t es la meilleure", "tu es la meilleure", "t es trop forte", "tu es trop forte", "t es forte", "tu es forte",
        "t es geniale", "tu es geniale", "t es parfaite", "t es incroyable", "t es une championne", "t es top", "trop forte",
        "bien joue", "bien jouee", "bien ouej", "bon travail", "beau travail", "bon boulot", "beau boulot", "good job",
        "well done", "good bot", "good girl", "gg wp", "felicitations", "felicitation", "masterclass", "championne", "chapeau",
        "t es une boss", "t es la boss", "t es la best", "t es un genie", "t es un amour", "t es un ange", "t assures", "tu assures",
        "bien vu", "parfait", "nickel", "impeccable", "respect", "bravo", "ggwp", "gg", "wp", "gj", "nice", "goat",
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
        "salut", "slt", "bonjour", "bjr", "bonsoir", "bsr", "hello", "hey", "hi", "yo", "wesh", "wsh", "cc", "re", "salam", "hola",
        "kikoo", "wave", "say hi",
    };
    private static readonly string[] WherePhrases =
    {
        "t es ou", "tu es ou", "ou es tu", "ou t es", "ou tu es", "ou est ce que tu es", "ou est ce que t es", "ou te trouves tu",
        "ou tu te trouves", "tes coordonnees", "tes coords", "tes coordos", "ta position", "ta pos", "donne ta position",
        "coordonnees", "coords", "position", "t ou", "tes ou", "t es passee ou", "tu es passee ou", "t es passe ou",
        "t es partie ou", "t es perdue", "tu es perdue", "on est ou", "ou on est", "ou est ce qu on est", "je suis ou", "ou je suis",
        "where are we", "where are you", "where r u", "your coords", "your position",
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
    private static readonly Regex BareMinutes = new(@"\b(?:dans|timer|minuteur|chrono|alarme)\s+(?<n>\d{1,3})\b(?!\s*\p{L})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // « réveille-moi à 20h »: a time of day, not twenty hours. Timers only take a delay.
    private static readonly Regex ClockTime = new(@"\b(?:a|à|vers|pour)\s+\d{1,2}\s*h(?:\s*\d{2})?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
}
