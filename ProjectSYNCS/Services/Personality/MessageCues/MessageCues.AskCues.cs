using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The cue lists for intents: asks (help, boredom, feeling down, stats, thanks, laughter, sharing).
internal static partial class MessageCues
{
    // Asking how she works or what she can do: pointed at /help, or at the topic's command when one
    // is named (« comment ça marche les sondages ? »). Read before Request, which takes over when a
    // request names a topic (« tu peux m'aider à organiser une session ? »).
    private static readonly string[] _helpPhrases =
    {
        "comment ca marche", "comment ca fonctionne", "comment ca s utilise",
        "comment on utilise", "comment t utiliser", "comment on t utilise", "comment je t utilise",
        "tu sais faire quoi", "qu est ce que tu sais faire", "que sais tu faire", "tu peux faire quoi",
        "qu est ce que tu peux faire", "c est quoi les commandes", "c est quoi tes commandes", "quelles commandes",
        "tes commandes", "la liste des commandes", "liste des commandes",
        "t as des commandes", "tu as des commandes", "t as quoi comme commandes", "tu as quoi comme commandes",
        // « pas » is inside the phrase, so the negator check before it can't cancel it.
        "sais pas comment t utiliser", "sais pas comment on t utilise", "sais pas comment je t utilise",
        "help", "aide moi", "aidez moi", "a l aide", "j ai besoin d aide", "besoin d aide", "tu peux m aider",
        "tu pourrais m aider",
    };
    // Only closing the message: « je suis perdu dans le donjon » is the game, « comment on fait pour
    // battre ce boss » and « je comprends rien à ce boss » ask about it.
    // In a short message only: « … qu'il m'a dit, je comprends rien » is about someone else.
    private const int MaxHelpClosingTokens = 6;
    private static readonly string[] _helpClosingPhrases =
    {
        "je suis perdu", "je suis perdue", "jsuis perdu", "jsuis perdue", "comment on fait", "comment je fais",
        "je comprends rien", "j y comprends rien", "je capte rien", "je pige rien",
    };

    // « comment on fait un sondage ? », « comment je fais pour créer une session ? »: how to use one of
    // her features, anywhere in the message, but only with one of _featureWords. The request topics are
    // too wide for it (« partie », « raid », « équipe »: « comment on fait pour finir le raid ? » is
    // the game), so they still pick the command once this has matched.
    private static readonly string[] _howToPhrases =
    {
        "comment on fait", "comment je fais", "comment faire", "comment on cree", "comment je cree", "comment creer",
        "comment on lance", "comment je lance", "comment lancer", "comment on organise", "comment organiser",
        "comment on programme", "comment on met", "comment je mets", "comment mettre",
    };
    private static readonly HashSet<string> _featureWords = new()
    {
        "sondage", "sondages", "poll", "rappel", "rappels", "remindme", "session", "sessions", "giveaway", "giveaways",
        "quiz", "commande", "commandes",
    };

    // Boredom, said to her: she has games for that. A negator around cancels (« je m'ennuie pas »).
    // No bare « rien à faire »: « j'en ai rien à faire » doesn't care. In a message of MaxBoredTokens at most.
    // Never followed by « de » (_missingWords): « je m'ennuie de toi » misses someone.
    private static readonly string[] _boredPhrases =
    {
        "je m ennuie", "jm ennuie", "je m emmerde", "je me fais chier", "jme fais chier", "je me fais ch",
        "on s ennuie", "on s emmerde", "on se fait chier",
        "je sais pas quoi faire", "jsp quoi faire", "j sais pas quoi faire",
        "c est mort ici", "c est calme ici", "personne joue",
        "i m bored", "im bored", "so bored", "boring", "ennui", "l ennui",
    };
    private static readonly HashSet<string> _missingWords = new() { "de", "d", "du", "des" };

    // « rien à faire » only closing the message or before _nothingToDoFollowers: « y a rien à faire
    // ce soir » is bored, « y a rien à faire contre ce boss » is hopeless.
    private static readonly string[] _nothingToDoPhrases =
    {
        "y a rien a faire", "ya rien a faire", "il y a rien a faire", "j ai rien a faire",
    };
    private static readonly HashSet<string> _nothingToDoFollowers = new()
    {
        "ici", "la", "aujourd", "ce", "cet", "cette", "en", "sur", "de", "du", "dans", "a", "pendant", "trop",
    };
    // « on fait quoi ? » only closing the message: « on fait quoi pour la session de samedi » plans.
    private static readonly string[] _boredClosingPhrases = { "on fait quoi", "on fait koi", "on fait quoi la" };
    private const int MaxBoredTokens = 10;

    // Feeling down, said about themselves: she answers with something gentle to hold on to. Never
    // when the message is laughing (« j'en peux plus 😂 », « je pleure de rire »).
    // « je suis » / « je me sens », up to two of _downIntensifiers, then one of _downAdjectives:
    // « je suis trop triste », not « je suis pas triste ».
    private static readonly string[] _downSubjects =
    {
        "je suis", "jsuis", "j suis", "chui", "chuis", "je me sens", "jme sens", "je me sent", "je me suis senti",
        "je me suis sentie",
    };
    private static readonly HashSet<string> _downIntensifiers = new()
    {
        "trop", "tellement", "si", "vraiment", "grave", "super", "hyper", "mega", "tres", "un", "peu", "assez",
        "completement", "totalement", "carrement", "encore", "toujours", "tout", "toute", "a", "au", "bien",
    };
    private static readonly HashSet<string> _downAdjectives = new()
    {
        "triste", "tristes", "deprime", "deprimee", "deg", "degoute", "degoutee", "decu", "decue", "down", "sad",
        "abattu", "abattue", "demoralise", "demoralisee", "desespere", "desesperee", "malheureux", "malheureuse",
        "seul", "seule", "stresse", "stressee", "angoisse", "angoissee", "anxieux", "anxieuse", "vide", "nul", "nulle",
        "blase", "blasee", "bout", "plat", "mal",
    };
    // Anywhere in the message; a negator before cancels (« j'ai jamais le cafard »).
    private static readonly string[] _downPhrases =
    {
        "j ai pas le moral", "j ai plus le moral", "jai pas le moral", "pas le moral", "plus le moral", "j ai le cafard",
        "jai le cafard", "coup de cafard", "coup de mou", "coup de blues", "gros coup de mou", "j en peux plus", "jen peux plus", "j en ai marre",
        "jen ai marre", "j en ai ras le bol", "ras le bol", "rien ne va", "plus rien ne va", "tout va mal", "ma vie est nulle",
        "envie de pleurer", "je deprime", "je vais mal", "j ai le seum", "jai le seum",
        "personne m aime", "i m sad", "im sad", "i feel bad",
    };
    // About their day, not a question: « sale journée » closes nothing, but « t'as passé une sale
    // journée ? » asks her.
    private static readonly string[] _downDayPhrases =
    {
        "sale journee", "journee de merde", "journee pourrie", "journee horrible", "journee difficile", "dure journee",
        "mauvaise journee", "journee nulle", "pas une bonne journee", "sale semaine", "semaine de merde",
        "semaine pourrie", "semaine horrible", "sale soiree", "soiree pourrie",
    };
    // Closing the message or a sentence, or before _downFollowers (IsClosingAsk), and never asked:
    // « ça va pas fort », « je craque ». Not « ça va pas la tête », « ça va pas ce soir » (can't
    // come), « je craque pour ce skin ». No « je pleure »: in chat it is mostly laughing.
    private static readonly string[] _downClosingPhrases =
    {
        "ca va pas", "ca va pas fort", "ca va pas trop", "ca va pas du tout", "sa va pas", "pas la forme",
        "pas trop la forme", "je craque", "je vais craquer", "je suis a bout", "je suis au bout",
        "ca va pas en ce moment", "ca va pas fort en ce moment", "ca va pas trop en ce moment", "pas la forme en ce moment",
    };
    // No « ce », « la », « en », « du »: « ça va pas ce soir », « … la tête », « … du tout ce patch ».
    private static readonly HashSet<string> _downFollowers = new()
    {
        "vraiment", "syncs", "sync", "hein", "fort", "trop", "aujourd", "top", "moi",
    };

    // « je devrais y aller ? », « je l'achète ? »: a yes-or-no about what they should do, answered
    // with /yesno's verdict. Only asked (a « ? »): « je devrais dormir » is said, not asked.
    private static readonly string[] _shouldIPhrases =
    {
        "je devrais", "jdevrais", "j devrais", "est ce que je devrais", "tu crois que je devrais", "tu penses que je devrais",
        "je ferais bien de", "je ferais mieux de", "c est une bonne idee", "bonne idee ou pas", "oui ou non",
        "je tente", "je me lance", "je l achete", "je le prends", "je la prends", "je les prends", "je le fais",
        "je la fais", "je fonce", "je lui ecris", "je lui dis", "je lui reponds", "je l appelle", "je postule",
        "should i",
    };
    // « … ou pas ? » closing a first-person question: « j'y vais ou pas ? », « on le fait ou pas ? ».
    private static readonly HashSet<string> _firstPerson = new() { "je", "j", "jsuis", "on", "me", "m", "moi" };
    // An open question is no yes-or-no: « je lui dis quoi ? », « je le fais comment ? ».
    private static readonly HashSet<string> _openQuestionWords = new()
    {
        "quoi", "koi", "comment", "combien", "quand", "pourquoi", "qui", "quel", "quelle", "quels", "quelles", "lequel", "laquelle",
    };

    // « raconte une blague », « fais-moi rire ». « c'est une blague ? » asks whether something is
    // one, and « sans blague » is surprise: only these phrases, never a bare « blague ».
    private static readonly string[] _jokePhrases =
    {
        "raconte une blague", "raconte moi une blague", "raconte nous une blague", "raconte des blagues",
        "dis une blague", "dis moi une blague", "dis nous une blague", "fais une blague", "fais moi une blague",
        "fais nous une blague", "t as une blague", "tu as une blague", "t as pas une blague", "tu connais une blague",
        "tu connais des blagues", "tu connais pas une blague", "une autre blague", "encore une blague",
        "raconter une blague", "raconter des blagues", "me faire rire", "nous faire rire",
        "fais moi rire", "fais nous rire", "fait moi rire", "dis un truc drole", "dis moi un truc drole",
        "dis quelque chose de drole", "raconte un truc drole", "tell me a joke", "say something funny",
    };
    // « une blague ? », « une blague stp »: the whole message.
    private static readonly HashSet<string> _jokeAlone = new() { "une blague", "blague", "une blague stp", "une blague svp" };

    // « un fun fact ? », « apprends-moi un truc »: one of her morning facts.
    private static readonly string[] _funFactPhrases =
    {
        "fun fact", "un fait interessant", "un truc interessant", "dis moi un truc interessant", "apprends moi un truc",
        "apprends moi quelque chose", "apprends nous un truc", "apprends nous quelque chose", "une anecdote",
        "raconte une anecdote", "raconte moi une anecdote", "le saviez vous", "tu savais que quoi", "random fact",
    };
    // « dis-moi un truc », « raconte-moi quelque chose »: closing only, or « dis-moi un truc, tu viens
    // ce soir ? » would get a fact.
    private static readonly string[] _funFactClosingPhrases =
    {
        "dis moi un truc", "raconte moi un truc", "raconte moi quelque chose", "dis moi quelque chose",
        "raconte nous un truc", "raconte nous quelque chose", "dis nous un truc", "dis quelque chose",
    };
    private static readonly HashSet<string> _funFactFollowers = new() { "stp", "svp", "syncs", "sync", "please", "pls", "toi" };

    // « c'est la faute de qui ? »: /blame's verdict, drawn from the asker's voice call. Asked only.
    private static readonly string[] _blamePhrases =
    {
        "c est la faute de qui", "c est la faute a qui", "c est de la faute de qui", "c est de la faute a qui",
        "la faute de qui", "la faute a qui", "a qui la faute", "c est qui le coupable", "qui est le coupable",
        "c est qui le responsable", "qui est responsable", "qui on blame", "on blame qui", "qui est ce qu on blame",
        "c est a cause de qui", "a cause de qui", "who s to blame", "whose fault",
    };

    // Asking her to apologise: « excuse-toi », « dis pardon », « tu me dois des excuses ». Read before
    // Apology, whose « pardon » and « désolé » sit inside « dis pardon » and « dis désolée ». She
    // refuses; Papa gets her apology (OwnerIntentLines). « excuse-moi » is theirs: Apology.
    private static readonly string[] _demandApologyPhrases =
    {
        "excuse toi", "excuses toi", "excusez vous", "dis pardon", "dis moi pardon", "dis nous pardon", "dis lui pardon",
        "demande pardon", "demande moi pardon", "demande nous pardon", "dis desole", "dis desolee", "dis moi desolee",
        "dis sorry", "say sorry", "apologize", "apologise", "tu me dois des excuses", "tu nous dois des excuses",
        "tu dois t excuser", "faut que tu t excuses", "tu devrais t excuser", "je veux des excuses", "j attends des excuses",
        "j exige des excuses", "on attend des excuses", "fais tes excuses", "presente tes excuses", "presente moi tes excuses",
        "des excuses peut etre", "tu t excuses pas", "tu vas t excuser", "tu t excuses quand", "j attends tes excuses",
        "tu peux t excuser", "tu pourrais t excuser", "tu vas pas t excuser", "je veux que tu t excuses",
        "j aimerais que tu t excuses", "tu dois me demander pardon", "tu dois demander pardon", "dis que t es desolee",
        "dis que tu es desolee", "j attends toujours tes excuses", "elles sont ou mes excuses", "et mes excuses",
        "fais des excuses", "presente des excuses", "des excuses svp", "des excuses stp", "des excuses s il te plait",
    };
    // « tu t'excuses ? », « des excuses ? »: asked, closing.
    private static readonly string[] _demandApologyAsked = { "tu t excuses", "tu t excuse", "tu demandes pardon", "des excuses" };
    // Said by them or of someone else, not asked of her: « je lui dis pardon », « je demande pardon »
    // (an apology: Apology), « il m'a demandé pardon ».
    private static readonly HashSet<string> _notDemandAfter = new()
    {
        "je", "j", "lui", "leur", "il", "elle", "on", "me", "m", "te", "nous", "vous", "ils", "elles", "qu", "que",
        "a", "as", "ai", "avait", "avais",
    };

    // Telling her to be quiet: she sulks and keeps quiet in that channel for ChatHush.Quiet. In a short
    // message only (MaxShutUpTokens): « tais-toi » inside a long one is a quote or a story. « ta gueule »
    // stays an insult (the mean pools); a mean message never reads an intent at all.
    private static readonly string[] _shutUpPhrases =
    {
        "tais toi", "tait toi", "tes toi", "taisez vous", "arrete de parler", "arretes de parler", "arrete de repondre",
        "arrete de nous repondre", "arrete de me repondre", "parle plus", "arrete de spammer", "arrete de spam",
        "laisse nous tranquille", "laisse moi tranquille", "fiche nous la paix", "fous nous la paix", "laisse nous parler",
        "on t a rien demande", "personne t a demande", "personne t a sonnee", "on t a pas sonnee", "shut up",
        "mets toi en sourdine", "mode silencieux", "baisse d un ton", "ferme ta bouche", "arrete de causer",
        "arrete de jacasser", "arrete de bavarder", "mute toi", "mets la en veilleuse", "mets la sourdine",
        "un peu de silence", "moins de bruit", "personne t a sonne", "on t a pas sonne",
    };
    // Only closing the message: « tu parles trop » shushes her, « tu parles trop vite » doesn't.
    private static readonly string[] _shutUpClosing =
    {
        "tu parles trop", "t parles trop", "tu parles beaucoup trop", "parle moins", "tu fais trop de bruit",
    };
    private static readonly HashSet<string> _shutUpClosingFollowers = new()
    {
        "syncs", "sync", "la", "toi", "hein", "franchement", "stp", "svp", "aujourd", "hui",
    };
    // The whole message, give or take her name and a « stp »: « chut », « silence », « la ferme ».
    // Not inside a sentence: « chut, il dort » is said to the room, « silence radio depuis hier »,
    // « à la ferme », « ferme la porte » and « boucle la ceinture » are something else.
    // Compared squashed, both sides: « chuuut » is « chut », « shhh » is « sh », « laisse » is « laise ».
    private static readonly HashSet<string> _shutUpAlone = SquashAll(
        "chut", "silence", "la ferme", "ferme la", "ferme", "stop", "chut chut", "sh", "silence radio", "boucle la",
        "la boucle", "laisse nous", "laisse moi", "stfu");
    private static readonly HashSet<string> _shutUpFillers = new()
    {
        "syncs", "sync", "stp", "svp", "toi", "un", "peu", "maintenant", "la", "deja", "bon", "allez", "oh", "eh", "mais",
        "please", "pls", "hein", "merci", "s", "il", "te", "plait", "plais", "vous",
    };
    private const int MaxShutUpTokens = 6;
    private static readonly HashSet<int> _noBreaks = new();

    // Their own numbers (/recap's), by what they ask about. Asked only: a « ? », or a question word in
    // the phrase itself (« je suis quel niveau », « combien j'ai de cailloux »), or « montre ».
    // « j'ai gagné 200 cailloux » and « mon plynling a faim » tell, and stay Sharing.
    private static readonly (MyStatsTopic Topic, string[] Phrases)[] _myStatsPhrases =
    {
        (MyStatsTopic.Level, new[]
        {
            "je suis quel niveau", "je suis niveau combien", "je suis a quel niveau", "j en suis a quel niveau",
            "j ai quel niveau", "j ai quel lvl", "je suis quel lvl", "c est quoi mon niveau", "quel est mon niveau",
            "c est quoi mon lvl", "combien j ai d xp", "j ai combien d xp", "combien d xp j ai", "j ai combien d experience",
            "c est quoi mon xp", "je suis combien au classement", "je suis ou au classement", "je suis quel rang",
            "c est quoi mon rang", "quel est mon rang", "je suis classe combien", "je suis combien dans le classement",
            "je suis level combien", "je suis lvl combien", "quel niveau je suis", "c est quoi mon level", "j ai quel level",
            "j ai combien de niveaux", "combien j ai de niveaux", "je suis classe ou", "c est quoi ma place au classement",
            "ma place au classement", "je suis ou dans le classement", "mon xp c est combien", "mon niveau c est combien",
        }),
        (MyStatsTopic.Balance, new[]
        {
            "combien j ai de cailloux", "j ai combien de cailloux", "combien de cailloux j ai", "c est quoi mon solde",
            "quel est mon solde", "il me reste combien de cailloux", "combien il me reste de cailloux", "je suis riche",
            "je suis pauvre", "j ai des cailloux", "j ai combien d argent", "combien j ai d argent", "j ai combien de sous",
            "combien j ai de sous", "j ai combien de thunes", "combien j ai de thunes",
        }),
        (MyStatsTopic.Plynling, new[]
        {
            "comment va mon plynling", "mon plynling va bien", "il va bien mon plynling", "mon plynling va comment",
            "des nouvelles de mon plynling", "comment il va mon plynling", "comment se porte mon plynling",
            "mon plynling est vivant", "mon plynling est en vie", "il est vivant mon plynling", "mon plynling il va bien",
            "il devient quoi mon plynling", "des news de mon plynling",
        }),
        (MyStatsTopic.All, new[]
        {
            "mes stats", "mes statistiques", "c est quoi mes stats", "montre moi mes stats", "mes chiffres", "mon profil",
            "mon bilan", "donne moi mes stats", "montre moi mon profil", "mon recap",
        }),
    };
    // Words that make a phrase asked without its « ? ».
    private static readonly HashSet<string> _myStatsAskWords = new() { "quel", "combien", "comment", "quoi", "ou", "montre", "donne" };
    // « mon niveau ? », « mes cailloux ? »: asked, short, closing.
    private static readonly (MyStatsTopic Topic, string[] Phrases)[] _myStatsShortAsks =
    {
        (MyStatsTopic.Level, new[] { "mon niveau", "mon lvl", "mon level", "mon xp", "mes xp", "mon rang", "mon classement" }),
        (MyStatsTopic.Balance, new[] { "mes cailloux", "mon solde", "mon argent", "mes sous", "ma thune", "mon porte monnaie", "ma bourse" }),
        (MyStatsTopic.Plynling, new[] { "mon plynling" }),
    };
    private static readonly HashSet<string> _myStatsFollowers = new()
    {
        "syncs", "sync", "stp", "svp", "actuel", "actuellement", "la", "maintenant", "alors", "deja", "exactement", "s",
    };
    private const int MaxMyStatsShortTokens = 4;

    // When the next session is, or who's coming: answered from the sessions themselves. Asked only (a
    // « ? », or opening on « quand », « qui », « y a »…). Read before ShouldI (« on joue ce soir ou
    // pas ? ») and Request (« tu peux me dire c'est quand la prochaine session ? »). Not « on lance une
    // game ce soir ? » or « on joue à quoi ce soir » (proposals), not « tu viens ce soir ? » (her), not
    // « qui est chaud pour un raid ? » (a call to the room), and never « la prochaine partie / game »:
    // « la prochaine game tu joues support ? » is the game.
    private static readonly string[] _nextSessionPhrases =
    {
        "la prochaine session", "prochaine session", "les prochaines sessions", "la prochaine soiree", "prochaine soiree",
        "le prochain event", "prochain event", "le prochain evenement",
        "y a une session", "il y a une session", "ya une session", "y a session", "y a des sessions", "y a pas de session",
        "il y a pas de session", "y a une soiree", "y a quoi de prevu", "il y a quoi de prevu", "ya quoi de prevu",
        "y a quelque chose de prevu", "y a un truc de prevu", "y a rien de prevu", "on a quoi de prevu",
        "c est quoi le programme", "quel est le programme", "le programme de ce soir", "le programme de la semaine",
        "qu est ce qui est prevu", "on joue quand", "quand est ce qu on joue", "quand est ce qu on fait une session",
        "quand est ce qu on refait une session", "on joue ce soir", "on joue demain", "on joue ce week end", "on joue ce weekend",
        "on fait une session", "qui vient ce soir", "qui vient a la session", "qui vient a la soiree", "qui est inscrit",
        "qui s est inscrit", "y a qui ce soir", "qui joue ce soir", "on sera combien", "on est combien a la session",
        "combien d inscrits", "la session de ce soir", "la session de demain", "la session de ce week end",
        "c est quand la session", "quand est la session", "la session c est quand", "c est a quelle heure la session",
        "la session commence quand", "la session est a quelle heure", "la session c est a quelle heure",
        "c est quand la soiree", "c est quand la prochaine", "la prochaine c est quand", "y a du monde ce soir", "ya session",
        "session ce soir", "c est prevu quand", "c est prevu pour quand", "on rejoue quand", "quand est ce qu on rejoue",
        "on joue quel jour", "y a un event", "y a un evenement", "qui sera la ce soir", "qui sera la a la session",
    };
    private static readonly HashSet<string> _nextSessionOpeners = new() { "quand", "qui", "y", "ya", "il", "c", "combien", "quel" };

    // « tu me notes ? », « je vaux combien ? », « tu me trouves comment ? »: /rate's verdict on them.
    // « tu penses quoi de moi ? » is already an Opinion on « moi », and gets the same verdict.
    private static readonly string[] _rateMePhrases =
    {
        "note moi", "notes moi", "tu me notes", "tu me noterais", "tu me mets combien", "tu me donnes combien",
        "tu me donnerais combien", "je vaux combien", "je merite combien", "rate me", "tu me trouves comment",
        "je suis a combien sur 10", "je suis combien sur 10", "sur 10 je suis a combien", "j ai quelle note",
        "tu me mettrais combien", "evalue moi", "juge moi", "tu me donnes quelle note", "je merite quelle note",
    };
    // « je suis beau ? », « tu me trouves drôle ? »: one of these, up to two _downIntensifiers, then an
    // adjective closing the message or before _rateMeFollowers. Asked only. « je suis nul » is Down,
    // read later: comfort answers it better than a score.
    private static readonly string[] _rateMeSubjects =
    {
        "je suis", "jsuis", "chui", "j suis", "tu me trouves", "tu me trouve", "t me trouves", "tu trouves que je suis",
    };
    private static readonly HashSet<string> _rateMeAdjectives = new()
    {
        "beau", "belle", "bo", "canon", "mignon", "mignonne", "moche", "drole", "marrant", "marrante", "intelligent",
        "intelligente", "malin", "maligne", "bg", "sexy", "sympa", "gentil", "gentille", "doue", "douee", "parfait",
        "parfaite", "genial", "geniale", "attachant", "attachante", "charmant", "charmante", "comment", "joli", "jolie",
        "style", "stylee", "chiant", "chiante", "relou", "bizarre", "normal", "normale", "fou", "folle", "cool", "fort",
        "forte",
    };
    // No « en » or « de »: « je suis fort en maths ? » and « je suis fou de joie » aren't asking for a score.
    private static readonly HashSet<string> _rateMeFollowers = new()
    {
        "syncs", "sync", "hein", "franchement", "vraiment", "toi", "ou", "pas", "quand", "meme", "alors", "non", "oui",
        "aujourd", "sur", "10", "physiquement", "comme", "personne",
    };

    // Thanks. A negator before cancels (« non merci »); the short forms count only as (nearly) the
    // whole reply, since « ty » and « thx » can sit in anything.
    private static readonly string[] _thanksPhrases =
    {
        "merci", "mercii", "merci beaucoup", "merci bien", "merci mille fois", "mille mercis", "grand merci",
        "je te remercie", "jte remercie", "je vous remercie", "cimer", "thanks", "thank you", "thank u", "tysm",
        "c est gentil", "c est trop gentil",
    };
    private static readonly string[] _thanksShortWords = { "ty", "thx", "mrc", "mci", "merki" };

    // Not buying what she said: « fais genre », « mon œil », « avoue ». Teasing more than
    // contradicting, so it gets its own pool — typically after one of her denials. In a reply of
    // MaxDisbeliefTokens at most (« il fait genre qu'il sait jouer » is about him), never after
    // « il », « elle », « on », and never « j'avoue » (« true »: YouAreRight). No « oui bien sûr »:
    // that one is meant.
    private static readonly string[] _disbeliefPhrases =
    {
        "fais genre", "fait genre", "c est ca ouais", "c est ca oui", "mais oui c est ca", "ouais c est ca",
        "oui oui c est ca", "mais bien sur", "mais oui bien sur", "on te croit", "on y croit", "je te crois pas",
        "j te crois pas", "jte crois pas", "je te crois moyen", "personne te croit", "mon oeil", "a d autres",
        "cause toujours", "ben voyons", "bah voyons", "et la marmotte", "pas credible", "t es pas credible",
        "avoue", "avoue le", "avoue tout", "allez avoue", "t es grillee", "tu es grillee", "on t a grillee",
        "je t ai grillee", "t es demasquee", "tu rougis", "t as rougi", "sure jan", "yeah right",
    };
    private static readonly HashSet<string> _notDisbeliefAfter = new() { "il", "elle", "on", "j", "je", "ca" };

    // Doubt, asked: « t'es sûre ? », « sérieux ? ». Only with a « ? », closing the message or its
    // question or before _doubtFollowers (IsClosingAsk): « t'es sûre que tu viens ? » asks about a plan.
    // « sérieux » only opening the message: « il est sérieux ? » is about him.
    private static readonly string[] _sureAsks =
    {
        "t es sure", "tu es sure", "t es sur", "tu es sur", "t es sure de toi", "tu es sure de toi", "t es sur de toi",
        "sure de toi", "t es sure de ca", "tu es sure de ca", "t es serieuse", "tu es serieuse", "t es serieux",
    };
    private static readonly HashSet<string> _seriousWords = new() { "serieux", "serieusement", "seriously", "srx" };
    // « ça m'étonnerait », « j'en doute »: closing a sentence or before _doubtFollowers, so « ça
    // m'étonnerait pas » agrees and « ça m'étonnerait qu'il vienne » is about him.
    private static readonly string[] _doubtPhrases =
    {
        "ca m etonnerait", "ca m etonnerais", "sa m etonnerait", "j en doute", "jen doute", "permets moi d en douter",
    };
    private static readonly HashSet<string> _doubtFollowers = new()
    {
        "syncs", "sync", "hein", "la", "quand", "meme", "fort", "beaucoup", "un", "peu", "toi",
    };

    // Calling her a liar: the same disbelief, said harder. « menteuse » is an insult cue too, but a
    // short accusation aimed at her is answered as teasing (ChatterService.IsMean). The phrases are
    // cancelled by a negator after them (« tu mens pas »), the single words by one before them
    // (« t'es pas une menteuse »); « il ment » is someone else (_notDisbeliefAfter).
    private static readonly string[] _liarPhrases =
    {
        "tu mens", "tu nous mens", "tu me mens", "tu mentais", "t as menti", "tu as menti", "ta menti",
        "arrete de mentir", "arretes de mentir", "arrete de mytho", "arrete de mythoner", "tu sais mentir",
        "tu mythones", "tu mythonnes", "tu mytho", "tu bluffes", "tu baratines", "tu racontes des bobards",
        "tu racontes des salades", "c est du mytho", "c est du pipeau", "c est du flan", "c est un mensonge",
        "sale menteuse", "sale mytho", "les bots mentent", "une bot qui ment",
    };
    private static readonly string[] _liarWords =
    {
        "menteuse", "menteur", "menteuses", "menteurs", "mytho", "mythomane", "mensonge", "mensonges",
        "bobard", "bobards", "pipeau", "baratin", "bluff", "liar",
    };

    // A bare liar word is about her only in a reply of two words at most (« menteuse ! ») or one
    // that speaks to her: « il est un menteur » is someone else.
    private static readonly HashSet<string> _secondPerson = new() { "t", "tu", "toi", "es", "te", "ta", "ton", "tes" };
    private const int MaxDisbeliefTokens = 6;

    // « ça va, merci », « bien merci »: the politeness closing an answer, not thanks for anything.
    private static readonly HashSet<string> _notThanksAfter = new() { "va", "vais", "bien", "roule", "tranquille" };
    private const int MaxShortThanksTokens = 3;

    // A goodbye is short: « à plus » closes « bon, à plus », but « il y a plus de place » is
    // something else, and so is any long message that happens to contain a farewell.
    private const int MaxFarewellTokens = 8;

    // Matched raw, shortened or squashed, so « mdrrrr » and « ptdrrr » land on their cue (squashed),
    // and so does « jpppp » (shortened to « jpp »: squashing would make it « jp »).
    private static readonly HashSet<string> _laughterWords = new()
    {
        "mdr", "ptdr", "pdtr", "xptdr", "lol", "lool", "lolilol", "xd", "jpp", "jptr", "jpptdr", "lmao", "lmfao",
        "rofl", "kek", "kekw", "lul", "lel", "omegalul", "niark",
    };

    // Phrases that laugh, or say she was funny: answered by the Laughter pool (« Merci, je suis
    // drôle »), which fits both better than a plain compliment. No bare « je pleure »: it cries as
    // often as it laughs.
    private static readonly string[] _laughterPhrases =
    {
        "mort de rire", "morte de rire", "je suis mort", "je suis morte", "jsuis mort", "jsuis morte", "je meurs",
        "je suis plie", "je suis pliee", "jsuis plie", "jsuis pliee", "plie de rire", "pliee de rire",
        "je pleure de rire", "j en pleure", "tu me tues", "tu m as tue", "ca m a tue",
        "trop drole", "t es drole", "t es trop drole", "c est drole", "tu es drole", "trop marrant",
        "t es marrante", "tu es marrante", "hilarant", "bonne blague", "elle est bonne celle la",
    };

    private static readonly string[] _laughterSymbols = { "😂", "🤣", "😆", "😹", "💀", "😄", "😁", "🤭" };

    // Custom emotes that laugh, matched by id on the raw text (ReadIntent drops emote markup before
    // tokenizing, so a message of only the emote still lands here).
    private static readonly string[] _laughterEmoteIds = { Emotes.GigaLaughId };

    // « haha », « ahah », « hihi », « héhé », « hoho », and the villain laughs « bahaha », « bwahaha »,
    // « mouahaha », « muahaha », « mwahaha », « gnahaha »: matched on the squashed token, so
    // « haaaaha » and « bahahaaa » count. Four letters at least (« bah » and « aha » don't).
    private static readonly Regex _laughRun = new(@"^(?:b|bw|bou|mou|mu|mw|gn)?[aeiu]?(?:ha|he|hi|hu|ho)+h?$", RegexOptions.Compiled);

    // A laugh is the whole message, or nearly: « mdr » answers her, « mdr il a raté » doesn't.
    private const int MaxLaughterTokens = 5;

    // A reply made only of these (three words at most) says nothing she could answer: « ok »,
    // « ah bon », « bah oui », « d'accord » (two tokens once the apostrophe goes).
    private static readonly HashSet<string> _lowEffortWords = new()
    {
        "ok", "okay", "okk", "oki", "okey", "oke", "k", "kk", "dac", "dacc", "daccord", "d", "accord",
        "ouais", "ouai", "ouaip", "ouep", "yep", "yes", "ui", "oui", "non", "nan", "nope", "mouais", "bof",
        "bref", "hm", "hmm", "mh", "ah", "oh", "eh", "bon", "bah", "ben", "euh", "si", "voila", "soit", "certes",
        "okie", "okidoki", "ouki", "acc", "yup", "yeah", "nah", "no", "we", "wai", "hein", "heu", "mm", "mmh",
        "pf", "pff", "ts", "tss", "tsk", "rah", "roh", "oula", "oulah", "olala", "ohlala", "la", "genre",
        "alors", "et", "donc", "enfin", "tiens", "tkt", "tqt", "np", "osef",
    };
    private const int MaxLowEffortTokens = 3;

    // A question typed without its « ? », as chat often does: one of these opening a message of
    // three words or more (« pourquoi t'as dit ça »), or one of _questionOpenerPhrases. No bare
    // « quel » (« quel plaisir » is an exclamation) and no bare « quand » (« quand tu veux »).
    private static readonly HashSet<string> _questionOpeners = new()
    {
        "pourquoi", "pq", "pk", "pkoi", "pourkoi", "pourquoa", "comment", "cmt", "koman", "koment", "combien",
        "estce", "kesk", "keske", "kesako", "kezako", "qui", "why", "how", "what", "who", "when", "where",
    };

    // « où » too, but not « ou alors » / « ou bien » / « ou pas », which go on a sentence rather
    // than ask anything.
    private static readonly HashSet<string> _notQuestionAfterOu = new() { "alors", "bien", "pas", "sinon", "meme", "plutot" };

    private static readonly string[] _questionOpenerPhrases =
    {
        "est ce", "quand est", "c est quand", "c est qui", "c est quoi", "c est ou", "c est combien", "c est comment",
        "quel est", "quelle est", "quels sont", "quelles sont", "quelle heure", "quel jour", "quel jeu",
        "tu sais si", "tu sais ou", "tu sais quand", "tu sais qui", "tu sais comment", "tu crois que", "tu penses que",
        "tu connais", "t as vu",
    };
    private const int MinUnmarkedQuestionTokens = 3;

    // Sharing: telling her about their own day (« j'ai mangé de la soupe », « bon, je suis allé au
    // ciné », « on a perdu la game »). A first-person statement, not a question, opening the message
    // once these fillers are skipped. Read after everything else bar LowEffort, so « j'ai faim »
    // reaches it but « je m'ennuie » (Bored) or « je suis d'accord » (YouAreRight) never do.
    private static readonly HashSet<string> _sharingFillers = new()
    {
        "bon", "bah", "ben", "alors", "moi", "perso", "du", "coup", "sinon", "au", "fait", "ah", "oh", "eh", "et",
        "mais", "ouais", "oui", "non", "nan", "bref", "enfin", "hier", "ce", "matin", "soir", "aujourd", "hui",
        "tiens", "btw", "d", "ailleurs", "la", "mdr", "lol", "ptdr", "wesh", "frr",
        "enfaite", "en", "vrai", "tantot", "omg", "ptn", "putain", "wow", "waw", "olala", "genre", "xd", "nous",
        "news", "info", "anecdote", "storytime", "cette", "nuit", "l", "autre", "jour", "semaine", "dernier",
        "derniere", "weekend", "week", "end", "midi", "tout", "a", "instant", "heure", "finalement", "franchement",
    };

    // Openers that announce news but would trip the checks below: « devine quoi, j'ai gagné » asks
    // nothing, and « tu sais quoi » isn't about « toi ». Skipped with the fillers.
    private static readonly string[] _sharingOpeners =
    {
        "devine quoi", "devinez quoi", "tu sais quoi", "vous savez quoi", "guess what", "breaking news",
        "petite info", "story time", "oh la la",
    };

    // « je », « j' », and the SMS forms glued to their verb.
    private static readonly HashSet<string> _sharingSelf = new()
    {
        "je", "j", "jai", "jsuis", "chui", "jsui", "jme", "jviens", "jvais", "jsors", "jrentre", "jpars", "jmange",
        "jdois", "jfais", "jreviens", "jjoue", "jregarde",
    };
    private static readonly HashSet<string> _sharingUsVerbs = new()
    {
        "a", "est", "s", "vient", "sort", "part", "rentre", "mange", "regarde", "etait", "avait",
    };
    // « nous avons », « nous sommes » — « nous » itself is a filler (« nous on a perdu »).
    private static readonly HashSet<string> _sharingWeVerbs = new() { "avons", "sommes", "etions", "avions" };

    // Their own things: « mon chat a vomi sur le clavier », « ma sœur m'a offert un jeu ». A possessive,
    // then one of these within three words.
    private static readonly HashSet<string> _sharingMine = new() { "mon", "ma", "mes" };
    private static readonly HashSet<string> _sharingMineVerbs = new()
    {
        "a", "est", "m", "ont", "sont", "vient", "viennent", "etait", "avait", "s",
    };
    private const int SharingMineWindow = 3;
    private static readonly HashSet<string> _notSharingMineNouns = new()
    {
        "avis", "opinion", "idee", "impression", "question", "reponse", "choix", "vote", "probleme",
    };

    // What comes after « je » when it isn't news: an opinion or a wish (« je pense », « je veux »), or
    // something said to the listener (« je t'envoie le lien », « je te rejoins »).
    private static readonly HashSet<string> _notSharingAfterSelf = new()
    {
        "sais", "pense", "crois", "trouve", "veux", "voudrais", "aimerais", "peux", "pourrais", "comprends",
        "t", "te", "vous", "ne", "prefere", "aime", "adore", "deteste", "kiffe", "arrive", "reviens", "y", "en",
    };
    // A message about « toi » or asking « quoi » isn't news about their day: « je suis pas amoureux de
    // toi » answers her, « on joue à quoi ce soir » proposes.
    private static readonly HashSet<string> _notSharingWords = new()
    {
        "toi", "tu", "t", "te", "ton", "ta", "tes", "quoi", "koi", "pourquoi", "comment", "quand", "combien",
    };
    private const int MinSharingTokens = 3;
    private const int MaxSharingTokens = 20;

    // Quoted words, French or English quotes, and Discord's « > » quote lines.
    private static readonly Regex _quotedSpeech = new(@"«[^»]*»|“[^”]*”|""[^""]*""|^>.*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // « a+ » / « @+ » is « à plus »: the tokenizer would keep only the « a ».
    private static readonly Regex _aPlus = new(@"(?<![\w<])[aà@]\s?\+(?!\w)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Mentions (« <@123> »), custom emotes and links are dropped before reading, or a mention
    // would tokenize into a number and count as a word.
    private static readonly Regex _mentionMarkup = new(@"<[@#][!&]?\d+>", RegexOptions.Compiled);
}
