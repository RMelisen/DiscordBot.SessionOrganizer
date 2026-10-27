using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The cue lists for intents: talk (love, apologies, greetings, agreement, requests, questions about her).
internal static partial class MessageCues
{
    // ---- Intents ----------------------------------------------------------
    // What a message aimed at her is doing (ChatIntent). Phrases, not bare words, as on the mean
    // side: a phrase is nearly always meant, a bare word misfires. Every cue is lowercase,
    // accent-stripped and survives TokenizeOrdered — the apostrophe becomes a space, so
    // « t'es qui » is "t es qui". Matched on adjacent tokens, raw or squashed.
    // Chat spellings included: the apostrophe dropped (« jtaime », « je taime »), glued (« jtm »),
    // SMS (« jt »), English. A negator inside a phrase breaks it (« je suis pas amoureux de toi »).
    private static readonly string[] _lovePhrases =
    {
        "je t aime", "jtm", "jtaime", "jt aime", "je taime", "je t adore", "jt adore", "je tadore",
        "je te kiffe", "jte kiffe", "je t kiffe", "je craque pour toi",
        "je suis amoureux de toi", "je suis amoureuse de toi", "amoureux de toi", "amoureuse de toi",
        "je suis fou de toi", "je suis folle de toi",
        "epouse moi", "epouses moi", "tu veux m epouser", "veux tu m epouser", "marie toi avec moi",
        "je veux t epouser", "on se marie", "tu veux te marier avec moi", "marions nous",
        "sors avec moi", "tu veux sortir avec moi", "on sort ensemble",
        "jtd", "jtadore", "jtkiffe", "je te love", "jte love", "je t apprecie", "jt apprecie",
        "t es mon crush", "tu es mon crush", "je crush sur toi", "j ai un crush sur toi", "t es mon amour",
        "love you", "i love you", "love u", "luv u", "luv you", "love ya", "ily", "ilysm",
        // Missing her is the same affection, answered from the same pool.
        "tu m as manque", "tu m a manque", "tu m avais manque", "tu m as trop manque", "tu m as grave manque",
        "tu me manques", "tu me manque", "tu me manques trop", "i missed you", "missed you", "miss you",
    };

    // « excuse » alone is left out: « c'est une excuse » is the noun. So is a bare « je voulais
    // pas »: « je voulais pas venir » apologises for nothing.
    private static readonly string[] _apologyPhrases =
    {
        "pardon", "pardonne moi", "pardonnez moi", "desole", "desolee", "desoler", "dsl", "dsle", "deso",
        "excuse moi", "excusez moi", "je m excuse", "mes excuses", "toutes mes excuses", "scuse moi",
        "sorry", "sry", "sowwy", "my bad", "my b", "mea culpa",
        "c est ma faute", "c ma faute", "ma faute", "je regrette", "je retire ce que j ai dit", "j ai abuse",
        "je voulais pas te vexer", "je voulais pas te blesser", "je voulais pas dire ca",
        "pas fait expres", "c etait pas expres", "c etait pas voulu",
    };

    // « oups » apologises only as (nearly) the whole reply: « oups j'ai raté le boss » is about the
    // boss. So does « je voulais pas », which is the whole reply at three tokens.
    private static readonly string[] _apologyShortWords = { "oups", "oops", "oupsi", "whoops", "je voulais pas" };
    private const int MaxShortApologyTokens = 3;

    // Only with a « ? » in the message: « ça va, merci » is an answer, not a question. And each
    // phrase must close the message or be followed by one of _askingFollowers (IsClosingAsk):
    // « ça va être long ? » and « comment tu vas faire ? » are about something else.
    private static readonly string[] _howAreYouPhrases =
    {
        "ca va", "sa va", "cava", "sava", "comment tu vas", "comment vas tu", "tu vas bien", "tu vas comment",
        "ca roule", "ca gaze", "ca gaz", "ca dit quoi", "bien ou bien", "bien ou quoi",
        "la forme", "en forme", "la peche", "tu tiens le coup", "bien dormi", "comment tu te sens",
        "tu te sens bien", "ca se passe bien", "comment ca se passe",
        "ca baigne", "ca boume", "tu vas mieux", "tu te sens mieux",
        "comment tu te portes", "tu te portes bien", "comment va", "comment allez vous", "vous allez bien",
        "ta journee", "passe une bonne journee", "how are you", "how r u", "hru", "how you doing",
    };

    // What may follow such a phrase when it asks after her: nothing, a « ? » (IsClosingAsk), or
    // one of these. No « ce » and no « pour »: « ça va ce soir ? » and « ça va pour demain ? » ask
    // whether a plan works, not how she is.
    private static readonly HashSet<string> _askingFollowers = new()
    {
        "toi", "bien", "et", "ou", "sinon", "vous", "mieux", "aujourd", "ma", "syncs", "sync", "aussi", "la",
        "depuis", "toujours", "comment", "hein",
    };

    // Words that ask after her only opening a short question (MaxHowAreYouOpenerTokens): « tranquille ? »,
    // « bien et toi ? ». Further in, « on fait une partie tranquille ? » is about the game.
    private static readonly HashSet<string> _howAreYouOpeners = new() { "cv", "tranquille", "tranquillou", "tranquilou", "bien", "forme" };
    private const int MaxHowAreYouOpenerTokens = 3;

    private static readonly string[] _whoAreYouPhrases =
    {
        "t es qui", "tu es qui", "tes qui", "t es ki", "tes ki", "qui es tu", "qui est tu", "c est qui toi",
        "t es quoi", "tu es quoi", "c est quoi toi", "tu sers a quoi", "a quoi tu sers",
        "comment tu t appelles", "tu t appelles comment", "c est quoi ton nom", "ton nom c est quoi",
        "presente toi", "presentez vous", "tu peux te presenter",
        "qui t a cree", "qui t a creee", "qui t a code", "qui t a codee", "qui t a fait", "qui t a faite",
        "qui t a programme", "qui t a programmee",
        "qui est ton createur", "c est qui ton createur", "qui est ton pere", "c est qui ton pere",
        "qui est ton papa", "c est qui ton papa", "t es quoi comme bot", "t es quel genre de bot", "tu es quel genre de bot",
        "who are you", "what are you",
    };

    private static readonly string[] _whatAreYouDoingPhrases =
    {
        "tu fais quoi", "tu fais koi", "tu fe quoi", "tu fai quoi", "tu fais koa", "tu fais quoa", "tfq", "tfk",
        "tu fais quoi de beau", "tu fous quoi", "tu glandes quoi", "tu bosses sur quoi",
        "qu est ce que tu fais", "qu est ce que tu fous", "qu est ce que tu fabriques", "que fais tu", "tu fabriques quoi",
        "quoi de neuf", "koi de neuf", "quoi de 9", "quoi de beau", "quoi de bon", "quoi de nouveau",
        "tu deviens quoi", "qu est ce que tu deviens", "t es occupee", "tu es occupee", "t es ou", "tu es ou",
        "tu t ennuies", "tu t occupes comment", "what are you doing", "wyd", "whatcha doing",
        "what s up", "whats up", "wassup",
    };

    private static readonly string[] _goodnightPhrases =
    {
        "bonne nuit", "bonne nuit a tous", "bne nuit", "bn nuit", "bonnuit", "bonne nuitee", "nuit nuit",
        "good night", "goodnight", "gn", "nighty night", "sleep well", "dors bien",
        "beaux reves", "fais de beaux reves", "doux reves",
    };

    // Going to sleep, said rather than wished: only closing the message or a sentence, or before
    // one of _sleepFollowers (IsClosingAsk). « je vais dormir, à demain » goes to bed; « je vais
    // dormir chez Luca demain » and « je m'endors devant le film » don't.
    private static readonly string[] _sleepPhrases =
    {
        "je vais dormir", "jvais dormir", "je go dormir", "vais dormir", "go dormir",
        "je vais me coucher", "jvais me coucher", "je me couche", "vais me coucher", "je file me coucher",
        "je vais au lit", "je file au lit", "je go au lit", "je vais au pieu", "je vais pioncer", "je vais me pieuter",
        "je vais roupiller", "je m endors", "je tombe de sommeil",
        "dodo", "au dodo", "go dodo", "je vais faire dodo", "je vais dodo", "jvais dodo",
    };
    private static readonly HashSet<string> _sleepFollowers = new()
    {
        "bonne", "bn", "gn", "maintenant", "la", "aussi", "moi", "bye", "ciao", "tchao", "salut", "bisous", "les",
        "allez", "bon", "perso", "direct", "enfin", "hein", "syncs", "sync",
    };

    private static readonly string[] _goodbyePhrases =
    {
        "a demain", "a plus tard", "a tout a l heure", "a tout de suite", "a tantot", "a bientot",
        "a la prochaine", "a la revoyure", "a ce soir", "a lundi", "a mardi", "a mercredi", "a jeudi",
        "a vendredi", "a samedi", "a dimanche",
        "au revoir", "bye", "byebye", "ciao", "tchao", "adieu", "see you", "see ya", "cya",
        "bonne soiree", "bonne journee", "bonne fin de journee", "bonne aprem", "bonne apres midi",
        "bon weekend", "bon week end", "bonne semaine", "bonne continuation", "a la semaine prochaine",
        "a2m1", "a2main", "a plouche",
        "tchuss", "tchus", "tschuss", "adios", "sayonara", "hasta la vista", "take care", "prends soin de toi",
        "je file", "je me casse", "je me tire", "je me barre", "je me sauve", "je decolle", "je m en vais", "je vous laisse",
        "je dois y aller", "faut que j y aille", "je vais y aller", "je vais y go", "je deco", "je me deco",
    };

    // « dors bien » wishes it; « tu dors bien la nuit ? » asks about it.
    private static readonly HashSet<string> _notGoodnightAfter = new() { "tu", "t" };

    // Goodbyes that count only closing the message: « bon, j'y vais » leaves, « j'y vais aussi
    // samedi » is coming along; « allez salut » leaves, while « salut » alone is a hello.
    private static readonly string[] _closingGoodbyePhrases =
    {
        "j y vais", "je go", "je bouge", "allez salut", "bon salut", "allez ciao", "allez bye",
    };

    // « à plus » and « à toute » are matched apart (IsShortFarewell): « y en a plus », « il a
    // toute la nuit » contain them too. They count opening or closing the message, never after one
    // of these words. « a+ » is read as « à plus » before tokenizing (_aPlus).
    private static readonly HashSet<string> _shortFarewells = new() { "plus", "toute", "tte" };
    private static readonly HashSet<string> _notFarewellBefore = new() { "y", "en", "il", "elle", "on", "n", "ya", "qui" };

    // Indecision, her pet peeve: a short reply that is only a « peut-être » (MaxMaybeTokens).
    private static readonly string[] _maybePhrases =
    {
        "peut etre", "ptet", "ptetre", "pt etre", "peutetre", "on verra", "on va voir", "a voir", "faut voir", "je verrai",
        "pourquoi pas", "ca depend", "jsp", "je sais pas", "je ne sais pas", "j sais pas", "jsais pas", "aucune idee",
        "je sais pas trop", "je sais plus", "bof je sais pas", "mouais peut etre", "j en sais rien", "jen sais rien",
        "chais pas", "chai pas", "chaipas", "chepa", "chais po", "qui sait", "on sait jamais", "si ca se trouve",
        "pas sur", "pas sure", "j hesite", "50 50", "idk", "dunno", "maybe", "perhaps",
    };

    // The same, but a negator before them reverses them: « pas possible », « c'est pas probable ».
    // The phrases above take none — « non, je sais pas » is still undecided.
    private static readonly string[] _maybeNegatablePhrases =
    {
        "possible", "probablement", "probable", "eventuellement", "ca se peut",
    };
    private const int MaxMaybeTokens = 5;

    // Whether she is a bot, an AI, a person: asked or stated, anywhere in the message.
    private static readonly string[] _areYouABotPhrases =
    {
        "t es un bot", "tu es un bot", "t es une bot", "t es qu un bot", "t es juste un bot", "c est qu un bot",
        "t es un robot", "tu es un robot", "t es une ia", "tu es une ia", "t es une intelligence artificielle",
        "t es chatgpt", "c est chatgpt", "t es humaine", "tu es humaine", "t es un humain", "t es une humaine",
        "t es une vraie personne", "tu es une vraie personne", "t es reelle", "tu es reelle", "t es vivante",
        "tu es vivante", "t es consciente", "tu es consciente", "tu ressens", "tu as des sentiments",
        "t as des sentiments", "tu as des emotions", "t as des emotions", "tu penses vraiment",
        "y a quelqu un derriere", "c est rodhengard qui", "c est papa qui", "c est un humain qui",
        "es tu un bot", "es tu une ia", "es tu humaine", "es tu reelle", "t es une machine", "tu es une machine",
        "t es un programme", "tu es un programme", "t es un ai", "t es une ai", "t es un llm", "t es chat gpt",
        "c est chat gpt", "t es gpt", "t es claude", "t es reel", "t es vraie", "tu existes vraiment",
        "t existes vraiment", "tu as une ame", "t as une ame", "tu as un coeur", "t as un coeur",
        "c est qui qui ecrit", "c est toi qui ecris", "qui ecrit derriere", "qui tape derriere",
        "are you a bot", "are you human", "are you real", "r u a bot", "you re a bot", "ur a bot",
    };

    // Agreeing with her. The phrases count in a reply of MaxDisagreementTokens at most (« j'ai bien vu
    // le film hier avec mon frère et ma sœur » agrees with nobody), the single words only as (nearly)
    // the whole reply, and « c'est vrai » and « vrai » not as a question (« c'est vrai ? » is « really? »).
    private static readonly string[] _youAreRightPhrases =
    {
        "t as raison", "tu as raison", "ta raison", "tas raison", "t a raison", "t as pas tort", "tu as pas tort",
        "t avais raison", "vous avez raison", "elle a raison", "syncs a raison",
        "trop raison", "grave raison", "tellement raison", "totalement raison", "completement raison",
        "entierement raison", "parfaitement raison", "bien raison",
        "pas faux", "c est pas faux", "bien vu", "trop vrai", "tellement vrai", "si vrai", "bien vrai", "so true",
        "je suis d accord", "d accord avec toi", "entierement d accord", "on est d accord", "tout a fait",
        "je valide", "je confirme", "je plussoie", "tu marques un point", "pas mieux", "j avoue", "javoue",
        "you re right", "youre right", "ur right", "fair enough", "true story",
    };
    private static readonly HashSet<string> _youAreRightWords = new()
    {
        "exact", "exactement", "exacto", "carrement", "grave", "vrai", "true", "facts", "absolument", "clairement",
        "evidemment", "effectivement", "parfaitement", "totalement", "precisement", "bingo", "indeed", "veridique",
        "plussoie",
    };
    private const int MaxAgreementTokens = 3;
    private static readonly string[] _cestVrai = { "c est vrai", "c est clair", "en effet" };

    // Disagreeing with her. The phrases count in a reply of MaxDisagreementTokens at most; the single
    // words only as (nearly) the whole reply — « c'est un faux compte » is not a contradiction — and
    // never after a negator (« pas faux » is agreement, caught above anyway).
    private static readonly string[] _youAreWrongPhrases =
    {
        "t as tort", "tu as tort", "tu te trompes", "tu t es trompee", "tu t trompes", "c est faux",
        "n importe quoi", "tu dis n importe quoi", "tu racontes n importe quoi",
        "mais non", "pas d accord", "je suis pas d accord", "c est pas vrai", "pas vrai",
        "rien a voir", "t es a cote de la plaque", "t es a l ouest",
        "t as faux", "tu as faux", "t es dans le faux", "tu es dans le faux", "archi faux", "completement faux",
        "totalement faux", "tu delires", "tu delire", "tu dis des betises", "tu dis des conneries",
        "tu racontes des betises", "tu racontes des conneries", "n imp", "c est pas ca",
        "jamais de la vie", "que nenni", "dans tes reves",
        "tu te plantes", "tu t es plantee", "tu t es trompe", "you re wrong", "youre wrong",
    };
    private static readonly string[] _youAreWrongWords = { "faux", "fake", "nope", "nimp", "wrong", "erreur" };
    private const int MaxDisagreementTokens = 8;

    // Adverbs that negate whatever verb they follow: « pas du tout » contradicts her as (nearly) the
    // whole reply, but « je m'ennuie pas du tout » is about being bored.
    private static readonly string[] _youAreWrongShortPhrases =
    {
        "pas du tout", "absolument pas", "pas vraiment", "certainement pas", "surement pas",
    };
    private const int MaxShortDisagreementTokens = 4;

    // A Request: asking her to do something (« tu peux… », « organise… »). Markers anywhere, or an
    // imperative opening the message; then a topic word says which command does it.
    private static readonly string[] _requestMarkers =
    {
        "tu peux", "peux tu", "tu pourrais", "pourrais tu", "tu veux bien", "tu voudrais", "est ce que tu peux",
        "tu sais faire", "stp", "svp", "s il te plait", "s il te plais", "s il vous plait",
        // These carry their own « pas », which would otherwise cancel the marker: « tu veux pas
        // lancer un sondage ? » asks for one.
        "tu veux pas", "tu pourrais pas", "tu voudrais pas",
        "j aimerais que tu", "je voudrais que tu", "je veux que tu", "faudrait que tu", "ca te dirait de",
    };
    // A marker right after one of these is a condition or a statement, not a request: « quand tu
    // peux », « si tu peux », « dès que tu peux », « je sais que tu peux ». « est-ce que tu peux » is
    // its own marker, so it still counts.
    private static readonly HashSet<string> _notRequestAfter = new() { "quand", "si", "que", "qu", "comme" };

    private static readonly HashSet<string> _requestImperatives = new()
    {
        "organise", "organiser", "lance", "lancer", "relance", "fais", "fait", "cree", "creer", "prepare", "programme",
        "planifie", "rappelle", "mute", "ban", "kick", "vire", "tire", "montre", "aide",
        "ouvre", "ajoute", "mets", "trouve", "choisis", "genere", "envoie", "donne", "annule", "decale", "reporte",
        "modifie", "previens", "melange",
    };
    private static readonly (RequestTopic Topic, string[] Words)[] _requestTopics =
    {
        // Tonight before Session: « organise une partie ce soir » is /tonight's job.
        (RequestTopic.Tonight, new[] { "ce soir", "tonight" }),
        (RequestTopic.Moderation, new[] { "mute", "ban", "bannir", "bannis", "kick", "expulse", "expulser", "vire", "virer",
            "timeout", "warn", "punis", "punir", "exclus", "exclure" }),
        (RequestTopic.Poll, new[] { "sondage", "sondages", "vote", "voter", "votes", "poll", "strawpoll" }),
        (RequestTopic.Reminder, new[] { "rappel", "rappels", "rappelle", "rappelles", "rappeler", "remind", "remindme",
            "previens", "prevenir", "reveiller" }),
        (RequestTopic.Teams, new[] { "equipe", "equipes", "team", "teams", "groupes" }),
        (RequestTopic.Giveaway, new[] { "giveaway", "giveaways", "tirage", "concours", "loterie", "tombola" }),
        (RequestTopic.Quiz, new[] { "quiz", "interro", "questionnaire", "trivia", "culture generale", "culture g" }),
        (RequestTopic.Session, new[] { "session", "sessions", "soiree", "partie", "game", "event", "evenement", "planning",
            "seance", "raid", "lan", "rdv", "rendez vous" }),
    };

    // Checking she's still there. Each phrase must close the message or its question, or be followed
    // by one of _thereFollowers (IsClosingAsk): « t'es là ? » and « tu dors ou quoi » ask, « t'es là
    // ce soir ? » and « t'es la meilleure » don't. Her name alone with a « ? » counts too (ReadIntent).
    private static readonly string[] _areYouTherePhrases =
    {
        "t es la", "tu es la", "tes la", "t es toujours la", "t es encore la", "tu es toujours la", "t es bien la",
        "tu dors", "t es reveillee", "tu es reveillee", "t es en vie", "tu es en vie", "t es dans les vapes",
        "y a quelqu un", "il y a quelqu un", "ya quelqu un", "y a qqn", "ya qqn", "ohe", "eho", "ho he",
        "t es partie", "t es passee ou", "t as disparu", "t es plus la", "tu es plus la",
        // Her silence noticed: « tu m'ignores ? », « tu réponds plus ? ». Closing only, like the rest:
        // « tu réponds pas à ma question ? » wants the answer (AnswerMe), « t'es morte de rire » laughs.
        "tu m ignores", "tu m ignore", "pourquoi tu m ignores", "tu reponds plus", "tu reponds pas",
        "tu me reponds plus", "tu me reponds pas", "reponds moi", "t es morte", "tu es morte", "t es mort",
    };
    private static readonly HashSet<string> _thereFollowers = new()
    {
        "syncs", "sync", "toi", "la", "encore", "ou", "quoi", "hein", "pas", "oui", "dis", "maintenant",
    };

    // Wanting the answer she didn't give: « réponds à ma question », « t'as pas répondu », « tu esquives ».
    // In a message of MaxAnswerMeTokens at most: a longer one is telling a story.
    private static readonly string[] _answerMePhrases =
    {
        "reponds moi", "repond moi", "t as pas repondu", "tu as pas repondu", "t as toujours pas repondu",
        "tu as toujours pas repondu", "tu m as pas repondu", "tu m as toujours pas repondu", "tu n as pas repondu",
        "tu n as toujours pas repondu", "tu ne m as pas repondu", "t as jamais repondu", "tu as jamais repondu",
        "j attends ta reponse", "j attends toujours ta reponse", "j attends une reponse", "toujours pas de reponse",
        "toujours pas repondu", "et ma reponse", "pourquoi tu reponds pas", "pourquoi tu me reponds pas",
        "pourquoi tu reponds jamais", "pourquoi tu me reponds jamais", "esquive pas", "esquives pas",
        "tu esquives la question", "tu esquive la question", "tu evites la question", "tu fuis la question",
        "tu ignores ma question", "tu ignore ma question", "t as ignore ma question", "tu as ignore ma question",
    };
    // Closing the message or before one of _answerMeFollowers: « tu esquives » is her dodging, « tu
    // esquives bien ses attaques » is the game.
    private static readonly string[] _answerMeClosing =
    {
        "tu esquives", "tu esquive", "tu reponds jamais", "tu me reponds jamais",
    };
    private static readonly HashSet<string> _answerMeFollowers = new()
    {
        "syncs", "sync", "hein", "encore", "toujours", "la", "quoi", "toi", "a",
    };
    // A form of « répondre » and « ma question » in the same message: « répond à ma question », « tu
    // réponds pas à ma question ? », « j'ai pas eu de réponse à ma question ». Not after a subject that
    // isn't her within AnswerSubjectWindow tokens before (« il a pas répondu à ma question »).
    private static readonly HashSet<string> _answerWords = new()
    {
        "reponds", "repond", "repondre", "repondu", "reponse", "repons", "rep",
    };
    private static readonly string[] _myQuestion = { "ma question", "mes questions", "ma demande" };
    private static readonly HashSet<string> _notHerAnswering = new() { "il", "elle", "ils", "elles", "on", "personne" };
    // « j'esquive pas », « il esquive pas »: not her dodging.
    private static readonly HashSet<string> _notAnswerMeAfter = new() { "j", "je", "il", "elle", "ils", "elles", "on" };
    private const int AnswerSubjectWindow = 3;
    // « réponds ! », « réponds stp »: the verb and nothing but these.
    private static readonly HashSet<string> _answerMeFillers = new()
    {
        "syncs", "sync", "stp", "svp", "alors", "bah", "allez", "mais", "enfin", "oh", "eh", "hein", "moi",
    };
    private const int MaxAnswerMeTokens = 15;

    // Waking her up: anywhere, but short. « debout » alone would be « stand up » in a game.
    private static readonly string[] _wakeUpPhrases = { "reveille toi", "reveilles toi", "debout la dedans", "wake up" };
    private const int MaxWakeUpTokens = 5;

    // « allô ? », « allô syncs ? »: opening the message, with nothing but these after it. Not « nan
    // mais allô » or « allô quoi », the meme that means « are you serious? ».
    private static readonly HashSet<string> _alloFollowers = new()
    {
        "syncs", "sync", "allo", "toi", "la", "hein", "y", "ya", "a", "il", "quelqu", "un", "qqn",
    };

    // Questions about her: how old, when she was born, where she lives, what she likes. Read before
    // the opinion (« tu aimes quoi comme jeu ? » would rate « quoi comme jeu »).
    private static readonly (HerTopic Topic, string[] Phrases)[] _aboutHerPhrases =
    {
        (HerTopic.Age, new[]
        {
            "t as quel age", "tu as quel age", "ta quel age", "quel age tu as", "quel age as tu", "quel age t as",
            "t as combien d ans", "tu as combien d ans", "ton age", "t es vieille", "tu es vieille", "t es jeune",
            "how old are you", "how old r u",
        }),
        (HerTopic.Birthday, new[]
        {
            "ton anniversaire", "ton anniv", "ton anni", "ta date de naissance", "t es nee quand", "tu es nee quand",
            "quand es tu nee", "quand est ce que t es nee", "t es nee le", "tu es nee le", "ta naissance",
            "your birthday",
        }),
        (HerTopic.Home, new[]
        {
            "t habites ou", "tu habites ou", "tu vis ou", "ou tu vis", "ou tu habites", "ou est ce que tu vis",
            "ou est ce que tu habites", "tu viens d ou", "d ou tu viens", "d ou viens tu", "tu tournes sur quoi",
            "tu tournes ou", "t es hebergee ou", "tu es hebergee ou",
            "where do you live",
        }),
        (HerTopic.Favourite, new[]
        {
            "tu aimes quoi", "t aimes quoi", "tu kiffes quoi", "t kiffes quoi", "qu est ce que tu aimes",
            "qu est ce que t aimes", "tu manges quoi", "tu bois quoi", "tu ecoutes quoi", "tu joues a quoi",
            "c est quoi tes gouts", "tes gouts",
            // « t'aimes quel jeu ? » asks her taste; read later, the opinion would rate « quel jeu ».
            // No « tu préfères quel »: « tu préfères quel jeu, Minecraft ou Fortnite ? » is PreferLines.
            "tu aimes quel", "t aimes quel", "tu aimes quelle", "t aimes quelle", "tu aimes quels", "t aimes quels",
            "tu aimes quelles", "t aimes quelles", "tu kiffes quel", "t kiffes quel", "tu kiffes quelle", "t kiffes quelle",
            "tu preferes quoi comme", "t preferes quoi comme",
            "t as des hobbies", "tu as des hobbies", "t as un hobby", "tu as un hobby", "tes hobbies", "ton hobby",
            "t as des passions", "tu as des passions", "t as une passion", "tu as une passion", "tes passions",
            "t as des loisirs", "tu as des loisirs", "tes loisirs",
        }),
    };

    // Her version: « t'es en quelle version ? », « c'est quoi ta version ? », « t'es à jour ? ».
    // Closing the message or its question, or before _versionFollowers only: « t'es en quelle
    // version de Minecraft ? » and « ta version des faits » are something else.
    private static readonly string[] _versionPhrases =
    {
        "t es en quelle version", "tu es en quelle version", "tes en quelle version", "t es quelle version",
        "tu es quelle version", "tes quelle version", "t es a quelle version", "tu es a quelle version",
        "t en es a quelle version", "tu en es a quelle version", "quelle version tu es", "quelle version es tu",
        "quelle version t es", "t es en version combien", "tu es en version combien", "tu tournes en quelle version",
        "tu tournes sur quelle version", "t as quelle version", "tu as quelle version",
        "c est quoi ta version", "c quoi ta version", "quelle est ta version", "ta version c est quoi",
        "ta version actuelle", "ton numero de version", "ta derniere version", "ta derniere mise a jour",
        "ta derniere maj", "t es a jour", "tu es a jour", "tes a jour",
        "what version are you", "what version are you on", "what s your version", "which version are you",
    };
    private static readonly HashSet<string> _versionFollowers = new()
    {
        "syncs", "sync", "actuellement", "maintenant", "aujourd", "exactement", "deja", "la", "now", "today",
    };

    // « ton jeu préféré », « ta couleur favorite »: a possessive, then « préféré » within three words.
    private static readonly HashSet<string> _yourWords = new() { "ton", "ta", "tes" };
    private static readonly HashSet<string> _favouriteWords = new()
    {
        "prefere", "preferee", "preferes", "preferees", "prefer", "favori", "favorite", "favoris", "favorites", "fav",
    };
    private const int FavouriteWindow = 3;

    // Her love life, asked about: « t'es célibataire ? », « t'as un crush ? ». Not a declaration —
    // « je suis amoureux de toi » is Love, read first.
    private static readonly string[] _crushPhrases =
    {
        "t es celibataire", "tu es celibataire", "t es en couple", "tu es en couple", "t as un copain",
        "tu as un copain", "t as un mec", "tu as un mec", "t as une copine", "t as un amoureux", "tu as un amoureux",
        "t as quelqu un dans ta vie", "tu as quelqu un dans ta vie", "t as un crush", "tu as un crush", "c est qui ton crush",
        "ton crush", "t es amoureuse", "tu es amoureuse", "tu aimes quelqu un", "t aimes quelqu un",
        "tu sors avec quelqu un", "t sors avec quelqu un", "ton genre de robot", "ton style de robot",
        "t as un faible pour", "tu as un faible pour",
        "tu kiffes quelqu un", "t kiffes quelqu un", "your crush",
        "t aimes bien qui sur le serveur", "tu aimes bien qui sur le serveur", "t aimes qui sur le serveur",
        "tu aimes qui sur le serveur", "tu kiffes qui sur le serveur", "t as quelqu un en vue", "tu as quelqu un en vue",
        "qui te fait craquer",
    };

    // The same asked short, closing the message or before _crushFollowers (IsClosingAsk): « tu kiffes
    // qui ? », « t'as quelqu'un ? ». « t'as quelqu'un pour jouer ? » wants a teammate, « tu aimes qui
    // comme perso ? » and « tu kiffes qui en jungle ? » are the game.
    private static readonly string[] _crushClosingPhrases =
    {
        "tu kiffes qui", "t kiffes qui", "tu aimes qui", "t aimes qui", "tu aimes bien qui", "t aimes bien qui",
        "t as quelqu un", "tu as quelqu un",
    };
    private static readonly HashSet<string> _crushFollowers = new()
    {
        "syncs", "sync", "hein", "ici", "secretement", "vraiment", "alors", "toi",
    };

    // What she feels about the asker: « tu m'aimes ? », « tu me détestes ? », « tu m'en veux ? ».
    // Closing the message or its question, or before _lovesMeFollowers (IsClosingAsk), with or
    // without a « ? »: « tu m'aimes hein » asks too. « tu m'en veux si je pars ? » and « tu me
    // supportes en bot lane ? » are something else. Read after Love (« je t'aime, tu m'aimes ? »
    // declares) and Apology (« désolé, tu m'en veux ? » apologises).
    private static readonly string[] _doYouLoveMePhrases =
    {
        "tu m aimes", "tu m aime", "t m aimes", "tu maimes", "tu m adores", "tu m adore", "tu me kiffes",
        "tu me kiffe", "tu m apprecies", "tu m apprecie", "tu me detestes", "tu me deteste", "tu me hais",
        "tu m en veux", "tu m en veut", "tu me supportes", "tu me supporte",
        "je suis ton prefere", "je suis ta preferee", "c est moi ton prefere", "c est moi ta preferee",
        "je suis ton humain prefere", "je suis ta personne preferee", "je suis ton chouchou", "je suis ta chouchoute",
        "do you love me", "do you hate me",
    };
    private static readonly HashSet<string> _lovesMeFollowers = new()
    {
        "bien", "un", "peu", "pas", "plus", "encore", "toujours", "hein", "syncs", "sync", "vraiment", "quand",
        "meme", "moi", "au", "moins", "beaucoup", "trop", "ou", "quoi", "non", "oui", "dis", "alors", "du",
        "tout", "secretement", "maintenant", "aussi",
    };

    // Whether she remembers the asker: « tu te souviens de moi ? », « comment je m'appelle ? ».
    // Closing, or before _rememberFollowers: « tu te souviens de moi en vocal hier ? » and « tu sais
    // qui je suis allé voir ? » are something else. Read after WhoAreYou (« c'est quoi ton nom » is
    // hers; « c'est quoi mon nom » is theirs).
    private static readonly string[] _rememberMePhrases =
    {
        "tu te souviens de moi", "tu te souviens pas de moi", "tu te rappelles de moi", "tu te rappelle de moi",
        "tu te rappelles moi", "tu te rappelles pas de moi", "tu sais qui je suis", "tu sais c est qui moi",
        "tu sais comment je m appelle", "comment je m appelle", "comment je m apelle", "comment je m appel",
        "c est quoi mon nom", "c est quoi mon prenom", "c est quoi mon pseudo", "mon nom c est quoi",
        "mon prenom c est quoi", "mon pseudo c est quoi", "tu connais mon nom", "tu connais mon prenom",
        "tu connais mon pseudo", "tu sais mon nom", "tu sais mon prenom", "tu m as oublie", "tu m as oubliee",
        "tu m as deja oublie", "tu m as deja oubliee", "tu m oublies", "do you remember me",
    };

    // The same, only asked: « tu me connais, je suis toujours en retard » says "you know me",
    // and « je suis qui pour juger » asks nothing of her.
    private static readonly string[] _rememberMeAskedPhrases =
    {
        "tu me connais", "tu me reconnais", "je suis qui", "qui suis je", "c est qui moi", "who am i",
    };
    private static readonly HashSet<string> _rememberFollowers = new()
    {
        "syncs", "sync", "deja", "au", "moins", "hein", "encore", "toujours", "vraiment", "ou", "quoi", "pas",
        "moi", "dis", "alors", "bien", "meme", "enfin",
    };

    // Her mood, asked: « t'es fâchée ? », « tu boudes ? », « t'es triste ? », « t'es jalouse ? ».
    // Only with a « ? », closing the message or its question, or before _upsetFollowers: « t'es
    // triste » said flat is slang for lame, and the mean side answers it. Both genders listed: some
    // get hers wrong, and the question is the same.
    private static readonly string[] _areYouUpsetPhrases =
    {
        "t es fachee", "t es fache", "tu es fachee", "tu es fache", "t es pas fachee", "t es pas fache",
        "t es en colere", "tu es en colere", "t es enervee", "t es enerve", "tu es enervee",
        "tu boudes", "tu boude", "tu fais la tete", "tu me fais la tete", "tu fais la gueule", "tu me fais la gueule",
        "t es vexee", "t es vexe", "tu es vexee", "t es pas vexee", "t es pas vexe",
        "t es triste", "tu es triste", "t es pas triste", "t es jalouse", "t es jaloux", "tu es jalouse",
        "t es pas jalouse", "t es pas jaloux", "t es de mauvaise humeur", "t es blessee", "tu es blessee",
        "t es deg", "t es degoutee", "t es degoute",
    };
    private static readonly HashSet<string> _upsetFollowers = new()
    {
        "syncs", "sync", "contre", "encore", "toujours", "hein", "ou", "quoi", "la", "vraiment", "un", "peu",
        "maintenant", "de", "pas", "avec", "moi", "aujourd", "depuis", "aussi", "alors",
    };

    // A reply that is only a yes or a no (ReadAnswer), compared squashed (« ouiii », « nooon »), once
    // _answerFillers are dropped and repeats folded (« oui oui », « bah non »). Not « exactement »'s
    // agreement nor « peut-être », which keep their own intents.
    private static readonly HashSet<string> _yesAnswers = SquashAll(
        "oui", "ouais", "ouai", "ouaip", "ouep", "ui", "wi", "yes", "yep", "yup", "yeah", "si", "carrement", "grave",
        "evidemment", "evidement", "bien sur", "bien entendu", "absolument", "tout a fait", "affirmatif",
        "clairement", "totalement", "certainement", "of course", "ofc", "volontiers", "avec plaisir", "oui merci");
    private static readonly HashSet<string> _noAnswers = SquashAll(
        "non", "nan", "nn", "no", "nope", "nah", "niet", "pas du tout", "du tout", "absolument pas", "jamais",
        "certainement pas", "surtout pas", "negatif", "pas vraiment", "non merci", "jamais de la vie");
    private static readonly HashSet<string> _answerFillers = SquashAll(
        "bah", "ben", "beh", "euh", "heu", "ah", "oh", "eh", "bon", "mais", "alors", "hm", "hmm", "mh", "bref");
    private const int MaxAnswerTokens = 5;

    private static HashSet<string> SquashAll(params string[] cues) => cues.Select(Squash).ToHashSet();
}
