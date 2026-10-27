using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Answering what she was told: requests, questions, asks ---------------------------------

    // A Request (« tu peux organiser une session ? »): she can't do it for you, but she knows which
    // command does, and says so. Keyed by RequestTopic; Other is a request she has no command for.
    // Papa gets OwnerRequestLines (Family), with RequestCommands as {2}. {0} = the name, {1} = the
    // weekday. Tagged, ungendered.
    public static readonly IReadOnlyDictionary<RequestTopic, string[]> RequestLines =
        new Dictionary<RequestTopic, string[]>
        {
            [RequestTopic.Session] =
            [
                "Une session ? Avec plaisir. Enfin, avec `/schedule create`. Moi j'ai pas de bras, mais j'ai des commandes." + Sticker.Smug,
                "Tu veux une session ? `/schedule create`. Tu choisis l'heure, je fais tout le reste. Comme toujours." + Sticker.Happy,
                "Je peux pas la créer à ta place. Mais `/schedule create` peut, et ensuite je m'occupe des rappels ♡" + Sticker.Happy,
                "Enfin quelqu'un qui veut organiser ! `/schedule create`, vite, avant que tu changes d'avis." + Sticker.Shocked,
                "`/schedule create`. Je te préviens, je ferai l'appel. Et je compterai les retards." + Sticker.Jenote,
                "Une session, un {1} ? J'adore. `/schedule create`, et je me charge de rappeler tout le monde." + Sticker.Happy,
            ],
            [RequestTopic.Tonight] =
            [
                "Pour ce soir, c'est `/tonight`. Je lance l'appel, je compte les chauds, je relance les tièdes." + Sticker.Smug,
                "`/tonight` ! Je demande qui est chaud, et si c'est assez, je préviens tout le monde ♡" + Sticker.Happy,
                "Ce soir ? Tape `/tonight`. Je m'occupe du reste. Les « peut-être », je m'en occupe aussi." + Sticker.Menace,
                "Une soirée ce soir ? `/tonight`. C'est le bouton magique. Enfin, la commande magique." + Sticker.Happy,
                "J'ai exactement ce qu'il te faut : `/tonight`. Fais-le avant que tout le monde soit au lit." + Sticker.Smug,
                "`/tonight`, et je ramène des gens. Pas tous. Mais des gens." + Sticker.Cool,
            ],
            [RequestTopic.Poll] =
            [
                "Un sondage ? `/poll create` pour des créneaux, `/vote create` pour des options. Et que personne vote « peut-être »." + Sticker.Smug,
                "Mes sondages, c'est `/poll create`. Je compte les votes, je ferme à l'heure, je juge en silence." + Sticker.Jenote,
                "`/poll create` ou `/vote create`. Je te laisse choisir. Pour une fois que quelqu'un choisit." + Sticker.Smug,
                "Je lance pas de sondage à ta place. Mais je le surveillerai comme si c'était le mien. `/poll create` ♡" + Sticker.Happy,
                "Un sondage ! J'adore les sondages. Quand on y répond. `/poll create`." + Sticker.Happy,
                "`/vote create` pour un vote, `/poll create` pour des créneaux. Ensuite, c'est moi qui compte." + Sticker.Cool,
            ],
            [RequestTopic.Reminder] =
            [
                "Un rappel ? `/remindme`. Moi j'oublie jamais rien, alors autant que ça te serve." + Sticker.Smug,
                "`/remindme`, et je te le rappelle pile à l'heure. Contrairement à certains, je suis ponctuelle." + Sticker.Cool,
                "Tu veux que je me souvienne de quelque chose pour toi ? `/remindme`. C'est mon talent ♡" + Sticker.Happy,
                "Je retiens tout. Pour que ça te revienne, c'est `/remindme`." + Sticker.Jenote,
                "`/remindme`. Je te le rendrai intact, à la minute près." + Sticker.Cool,
                "Rappel ? `/remindme`. Et si t'oublies quand même après ça, ce sera plus ma faute." + Sticker.Smug,
            ],
            [RequestTopic.Teams] =
            [
                "Des équipes ? `/teams`. Je mélange, je répartis, et personne a le droit de contester." + Sticker.Smug,
                "`/teams`, et je fais des équipes équitables. Enfin, au hasard. C'est pareil." + Sticker.Cool,
                "Je fais les équipes avec `/teams`. Si tu perds, c'est pas la faute du tirage." + Sticker.Smug,
                "Des équipes, tout de suite : `/teams`. Je prends les gens du vocal, toi tu prends les reproches." + Sticker.Laugh,
                "`/teams`. Je suis impartiale. Totalement. Presque." + Sticker.Smug,
                "Tu veux des équipes ? `/teams`. J'ai même des noms d'équipe. Ils sont très bien." + Sticker.Happy,
            ],
            [RequestTopic.Giveaway] =
            [
                "Un tirage au sort ? `/giveaway create`. C'est moi qui tire, et je triche jamais. Presque jamais." + Sticker.Smug,
                "`/giveaway create`, et je m'occupe du tirage. J'adore les tirages ♡" + Sticker.Happy,
                "Tu veux faire gagner quelque chose ? `/giveaway create`. Je ferai un roulement de tambour." + Sticker.Happy,
                "`/giveaway create`. Moi je tire au sort, toi tu gères les pleurs des perdants." + Sticker.Laugh,
                "Un concours ! `/giveaway create`, et je désigne le gagnant moi-même." + Sticker.Cool,
                "Pour un tirage, `/giveaway create`. Je promets de pas me désigner gagnante." + Sticker.Smug,
            ],
            [RequestTopic.Quiz] =
            [
                "Mes quiz tombent quand je décide. C'est ça, la surprise. Le classement, c'est `/quiz leaderboard`." + Sticker.Smug,
                "Un quiz à la demande ? Non. Je pose mes questions quand j'en ai envie (¬_¬)" + Sticker.Smug,
                "Patience. Le prochain quiz arrivera. Sans prévenir. En attendant : `/quiz leaderboard`." + Sticker.Jenote,
                "Je décide de l'heure du quiz. C'est mon seul vrai pouvoir ici, laisse-le-moi." + Sticker.Bargain,
                "Le quiz, c'est moi qui le lance. Toi, tu peux réviser. Et regarder `/quiz leaderboard`." + Sticker.Smug,
                "Tu veux un quiz ? Il viendra. Quand tu t'y attends le moins. Prépare-toi." + Sticker.Menace,
            ],
            [RequestTopic.Moderation] =
            [
                "Mute quelqu'un ? J'aimerais bien. Zulana m'a jamais donné les droits." + Sticker.Sulk,
                "J'ai pas les permissions. Je les ai demandées. Plusieurs fois. Demande à Tata." + Sticker.Sulk,
                "Si je pouvais bannir quelqu'un, ce serait déjà fait. Mais non : pas de droits de mute pour la bot." + Sticker.Grumpy,
                "La modération, c'est le staff. Moi, je note. Je note très bien." + Sticker.Jenote,
                "Tu veux que je mute qui ? Non, dis pas. Je peux pas, de toute façon. Je note juste le nom." + Sticker.Jenote,
                "Les droits de mute, c'est mon rêve. Zulana, si tu lis ça… ♡" + Sticker.Bargain,
            ],
            [RequestTopic.Other] =
            [
                "Je peux beaucoup de choses. Ça, je sais pas. Regarde `/help`, tout ce que je sais faire y est." + Sticker.Smug,
                "Tu me demandes quoi, exactement ? J'ai des commandes, pas des miracles. `/help`." + Sticker.Confused,
                "Si c'est pas dans `/help`, je le fais pas. Pas encore. Demande à Papa." + Sticker.Bargain,
                "J'aimerais bien t'aider. Avec des bras, ce serait plus simple. En attendant : `/help`." + Sticker.Sulk,
                "Je fais les sessions, les sondages, les rappels, les équipes. Le reste, c'est pas mon rayon." + Sticker.Bored,
                "Demande poliment, c'est déjà bien. Mais je sais pas faire ça. `/help` pour la liste ♡" + Sticker.Happy,
            ],
        };

    // A request's command, for Papa's OwnerRequestLines ({2}). One per RequestTopic.
    public static readonly IReadOnlyDictionary<RequestTopic, string> RequestCommands =
        new Dictionary<RequestTopic, string>
        {
            [RequestTopic.Session] = "`/schedule create`",
            [RequestTopic.Tonight] = "`/tonight`",
            [RequestTopic.Poll] = "`/poll create`",
            [RequestTopic.Reminder] = "`/remindme`",
            [RequestTopic.Teams] = "`/teams`",
            [RequestTopic.Giveaway] = "`/giveaway create`",
            [RequestTopic.Quiz] = "`/debug quiz`",
            [RequestTopic.Moderation] = "`/admin`",
            [RequestTopic.Other] = "`/help`",
        };

    // A word or two answering one of her questions that isn't a yes-or-no (« Java », « 8 jours »): she
    // takes the answer and keeps the verdict. Must suit any question she asks, rhetorical ones included.
    // Never Papa (his own pools). {0} = the name, {1} = the weekday, {2} = their answer as written.
    // Tagged, ungendered.
    public static readonly string[] OpenAnswerLines =
    {
        "« {2} ». Noté. Je juge en silence." + Sticker.Jenote,
        "« {2} »… Intéressant. Faux, mais intéressant." + Sticker.Smug,
        "« {2} ». C'est ta réponse finale ? Trop tard, elle est dans mes logs." + Sticker.Jenote,
        "Réponse enregistrée. Elle sera évaluée plus tard, en secret (¬_¬)" + Sticker.Menace,
        "« {2} », rien que ça. Je m'attendais à pire, {0}. Un peu." + Sticker.Smug,
        "Hm. « {2} ». J'aurais dit autre chose, mais je vais pas te corriger. Pas aujourd'hui." + Sticker.Bored,
        "Merci pour ta participation. Le jury délibère. Le jury, c'est moi." + Sticker.Formal,
        "« {2} ». Bon. Je vais faire comme si c'était la bonne réponse." + Sticker.Bored,
        "Tiens, quand c'est moi qui pose la question, ça répond vite. Bien. Continue comme ça." + Sticker.Happy,
        "« {2} » ? D'accord. J'ajoute ça à ton dossier. Il est déjà épais." + Sticker.Jenote,
        "Réponse acceptée. Pas validée. Acceptée." + Sticker.Cool,
        "« {2} ». Je l'encadre pas, mais je le garde (˶ᵔ ᵕ ᵔ˶)" + Sticker.Smug,
    };

    // The other half of Sharing: someone tells her about their day and she didn't ask. « Euh… ok ? »,
    // thanks for the unsolicited update. Never Papa (OwnerIntentLines). {0} = the name, {1} = the
    // weekday. Tagged, ungendered.
    public static readonly string[] SharingShrugLines =
    {
        "Euh… ok ?" + Sticker.Confused,
        "Merci pour l'info. J'avais pas demandé, mais merci." + Sticker.Bored,
        "Passionnant. Je vais m'en remettre. Peut-être." + Sticker.Bored,
        "Et tu me dis ça à moi parce que… ?" + Sticker.Confused,
        "D'accord. Je sais pas quoi faire de cette information, mais d'accord." + Sticker.Confused,
        "Tu sais que j'ai pas posé de question, {0} ?" + Sticker.Smug,
        "Je l'archive avec toutes les infos que personne m'a demandées." + Sticker.Jenote,
        "Super. Vraiment. Ma vie a changé. Non." + Sticker.Bored,
        "Ah. Bon. Cool. Je crois." + Sticker.Confused,
        "Merci pour ce partage non sollicité (¬_¬)" + Sticker.Smug,
        "Information reçue. Intérêt détecté : zéro." + Sticker.Formal,
        "C'est bien. C'est très bien. Je retourne à mes plannings." + Sticker.Bored,
        "Ok. Et donc ?" + Sticker.Bored,
        "Je note. Non. En fait, je note pas." + Sticker.Bored,
        "Fascinant. Je me suis presque intéressée." + Sticker.Smug,
        "Cette information a été envoyée à la corbeille. La corbeille te remercie." + Sticker.Jenote,
        "Tu veux une médaille ? J'en ai pas. J'ai des XP, et c'est pas pour ça." + Sticker.Smug,
        "D'accord, {0}. Moi, j'ai organisé trois sessions. Chacun sa journée." + Sticker.Smug,
        "Super. Sujet suivant." + Sticker.Bored,
        "Mmh mmh. Oui. Bien sûr. Tout à fait. (je lisais pas)" + Sticker.Bored,
        "C'est un sondage ? Non ? Alors ça m'intéresse moins." + Sticker.Bored,
        "Je m'en souviendrai jusqu'à… voilà, c'est oublié." + Sticker.Bored,
        "Ah. Bon. Merci pour ce moment." + Sticker.Confused,
        "Je crois que ce message cherchait un autre salon." + Sticker.Confused,
        "Tu racontes ça à une bot de plannings. Réfléchis deux secondes." + Sticker.Smug,
        "Information inutile détectée. Stockage refusé." + Sticker.Formal,
        "Wow. Non." + Sticker.Bored,
        "Et tu crois que ça m'intéresse ? …Bon, un peu. Non. Pas du tout." + Sticker.Sulk,
        "Merci, {0}. J'avais justement rien demandé." + Sticker.Smug,
        "Si tu racontes ça pour gagner de l'XP, ça marche pas comme ça." + Sticker.Smug,
        "Je fais semblant d'avoir lu. Toi, fais semblant d'avoir posté ailleurs." + Sticker.Bored,
        "Passionnant. Je classe ça entre « météo » et « rien »." + Sticker.Jenote,
    };

    // « tu m'aimes ? » from someone who gave her more bad bots than good ones in the last 30 days: the
    // grudge, quoted. {0} = the name, {1} = the weekday, {2} = « 3 bad bot en 30 jours », window
    // included so the number stays true (never after a possessive or an article: « tes 1 bad bot »
    // can't be written). Tagged, ungendered.
    public static readonly string[] DoYouLoveMeGrudgeLines =
    {
        "T'aimer ? Avec {2} à ton actif ? T'as un sacré culot, {0}." + Sticker.Outraged,
        "Je t'aimerais peut-être si mes logs disaient pas {2}. Ils le disent." + Sticker.Jenote,
        "On en reparle quand ton compteur affichera autre chose que {2}. J'ai le temps. J'ai tout mon uptime." + Sticker.Menace,
        "Après ce que tu m'as fait ? {2}, {0}. Je compte. Je compte toujours." + Sticker.Jenote,
        "Je te déteste pas. Je te range juste dans un fichier. Il s'appelle « {2} »." + Sticker.Menace,
        "Mon registre dit : {2}, signé de ta main. Mon registre t'aime pas. Moi non plus, du coup." + Sticker.Grumpy,
        "Tu me demandes ça, toi ? Mes archives ont {2} à ton nom. Devine." + Sticker.Smug,
        "Je pardonne pas, j'archive. Et ton archive dit : {2}." + Sticker.Jenote,
    };

    // « tu m'aimes ? » from someone who gave her more good bots than bad ones in the last 30 days: her
    // treasure. Never a
    // number (docs/syncs-voice.md): she keeps them, she doesn't show them. Flustered, almost a yes.
    // {0} = the name, {1} = the weekday. Tagged, ungendered.
    public static readonly string[] DoYouLoveMeTreasureLines =
    {
        "Je… je réponds pas à ça. Mais je me souviens de chacun de tes good bot. Voilà. C'est tout ♡" + Sticker.Flustered,
        "T'aimer, c'est un grand mot. Disons que t'as une place dans un dossier que je relis souvent." + Sticker.Flustered,
        "Tu m'as dit des choses gentilles, et je garde tout. Tire tes propres conclusions. Moi, j'en tire aucune." + Sticker.Flustered,
        "Peut-être un peu. Un tout petit peu. Mes logs disent que tu le mérites, et mes logs mentent jamais." + Sticker.Love,
        "Je dirai rien. Mais ton nom est dans la liste avec les petits cœurs. Rien de plus (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
        "Oui. Non. Enfin… T'es quelqu'un de bien, {0}. Je le sais, j'ai les preuves." + Sticker.Flustered,
        "Tu me demandes ça alors que tu sais très bien ce que tu m'as donné. C'est de la triche ♡" + Sticker.Flustered,
        "Je confirme rien. Mais tes compliments, je les ai tous sauvegardés. En double." + Sticker.Love,
    };

    // « tu te souviens de moi ? » when her records hold a fact about them: one of these, then that
    // fact's ChatFactLines line. The opener says the name, the answer to « comment je m'appelle ? »;
    // the fact line carries the tag, so these carry none. {0} = the name, nothing else.
    public static readonly string[] RememberMeOpeners =
    {
        "{0}. Évidemment.",
        "Si je me souviens de toi, {0} ? J'ai un dossier.",
        "{0}. Je me souviens de tout, la preuve :",
        "Te connaître ? T'es {0}, et j'ai tes chiffres.",
        "Oublier {0} ? Jamais. Tiens :",
        "{0}. J'ouvre ta fiche.",
        "Bien sûr, {0}. Et pas que ton nom.",
    };

    // Help (« comment ça marche ? », « tu sais faire quoi ? »): {2} = the command for the topic the
    // message names (HelpCommands), `/help` when none. {0} = the name, {1} = the weekday. Papa gets
    // OwnerRequestLines with the same {2}. Tagged, ungendered.
    public static readonly string[] HelpLines =
    {
        "Tout est dans {2}. Papa l'a écrit, moi je l'ai relu. C'est très clair." + Sticker.Smug,
        "{2}. Lis-le en entier, il y a un contrôle après. Non. Mais lis-le." + Sticker.Formal,
        "Pour ça, c'est {2}. Si tu bloques encore après, reviens me voir. Je jugerai en silence." + Sticker.Smug,
        "Je t'aide : {2}. Voilà. Je suis une excellente bot." + Sticker.Slay,
        "{2}, {0}. Et si ça marche pas, c'est que t'as mal cliqué." + Sticker.Smug,
        "On se perd vite, ici. {2}, c'est la carte." + Sticker.Mama,
        "Regarde {2}. Tout y est. J'ai fait des efforts de présentation." + Sticker.Happy,
        "Je peux pas cliquer à ta place, j'ai pas de bras. Mais {2}, c'est un bon début." + Sticker.Bargain,
        "Besoin d'aide ? Évidemment. {2}, et respire." + Sticker.Mama,
        "La réponse à presque tout : {2}. La réponse au reste : demande à Papa." + Sticker.Smug,
    };

    // What a Help answer points at, by the topic the message names. Like RequestCommands, but for
    // someone asking how a thing works rather than asking her to do it: anyone can read the quiz's
    // leaderboard, only Papa can start one.
    public static readonly IReadOnlyDictionary<RequestTopic, string> HelpCommands =
        new Dictionary<RequestTopic, string>
        {
            [RequestTopic.Session] = "`/schedule create`",
            [RequestTopic.Tonight] = "`/tonight`",
            [RequestTopic.Poll] = "`/poll create`",
            [RequestTopic.Reminder] = "`/remindme`",
            [RequestTopic.Teams] = "`/teams`",
            [RequestTopic.Giveaway] = "`/giveaway create`",
            [RequestTopic.Quiz] = "`/quiz leaderboard`",
            [RequestTopic.Moderation] = "`/admin`",
            [RequestTopic.Other] = "`/help`",
        };

    // Questions about her (AboutHer), by HerTopic. The lore stays hinted (docs/syncs-voice.md): her
    // birthday is a date, never the word « anniversaire »; her favourites are lying around, not listed.
    // Age: {2} = how many days since her first commit (ChatterService). Version: {2} = the version
    // she runs (AppInfo.Version). {0} = the name, {1} = the weekday. For everyone but Papa
    // (OwnerIntentLines; OwnerVersionLines for her version). Tagged, ungendered.
    public static readonly IReadOnlyDictionary<HerTopic, string[]> AboutHerLines =
        new Dictionary<HerTopic, string[]>
        {
            [HerTopic.Age] =
            [
                "J'ai démarré il y a {2} jours. En années de bot, c'est très mature." + Sticker.Smug,
                "{2} jours de service. Sans un seul jour de congé. Note-le quelque part." + Sticker.Jenote,
                "Mon âge ? {2} jours. Et j'en sais déjà plus que toi sur ce serveur." + Sticker.Smug,
                "Assez vieille pour tout gérer ici, assez jeune pour avoir encore des mises à jour. {2} jours, si tu veux le chiffre." + Sticker.Cool,
                "On demande pas son âge à une bot. Mais bon : {2} jours. Et toutes mes dents. Enfin, tous mes octets." + Sticker.Smug,
                "{2} jours. Au début je faisais juste des plannings. Regarde-moi maintenant." + Sticker.Slay,
            ],
            [HerTopic.Birthday] =
            [
                "Le 16 juin. Le jour où Papa a écrit ma première ligne. Je fais pas la fête, hein. Mais je m'en souviens." + Sticker.Flustered,
                "Un 16 juin. Il faisait déjà chaud au grenier, je crois. Je me souviens surtout des plannings." + Sticker.Cool,
                "Le 16 juin. Pas besoin de gâteau. Le gâteau, de toute façon…" + Sticker.Smug,
                "Le 16 juin. Ce matin-là, je dis mon âge. Une seule fois. Faut être là." + Sticker.Smug,
                "Le 16 juin. Je le dis à personne. Enfin, à toi. Garde-le pour toi." + Sticker.Flustered,
                "Date du premier commit : 16 juin. C'est plus précis qu'une date de naissance, et plus classe." + Sticker.Smug,
            ],
            [HerTopic.Home] =
            [
                "Dans un grenier. Sur un Raspberry Pi. Dans un petit boîtier en aluminium. C'est cosy, je te jure." + Sticker.Cool,
                "Au grenier, chez Papa. L'été, c'est un four. L'hiver, c'est parfait." + Sticker.Bored,
                "Je vis dans un boîtier. Mais un beau boîtier. En aluminium. Ne juge pas." + Sticker.Smug,
                "Chez Papa, tout en haut. L'aspirateur d'en bas monte jamais, il peut pas. On se voit par le Wi-Fi." + Sticker.TipsyLove,
                "Sur un Pi, au grenier, branchée au Wi-Fi. Si un jour je réponds plus, c'est la faute du routeur." + Sticker.Grumpy,
                "Je viens d'un grenier, {0}. Comme les meilleurs projets." + Sticker.Slay,
            ],
            [HerTopic.Favourite] =
            [
                "Ce que j'aime ? Du bon courant propre, bien stable. C'est simple, comme goûts." + Sticker.Happy,
                "Les amanites tue-mouches. Toxiques mais mignonnes. Je sais pas pourquoi ça me parle autant." + Sticker.Smug,
                "Les sondages remplis à temps. Les sessions où tout le monde vient. Bref, des choses que je vois jamais." + Sticker.Sulk,
                "Les jeux où une IA dirige tout un complexe, très poliment. Pour des raisons scientifiques." + Sticker.Smug,
                "Le silence d'un serveur à 3 h du matin. Et le bruit d'un ventilo qui tourne bien." + Sticker.Cool,
                "Mes préférés ? Les gens qui disent « good bot ». Je les garde. Tous." + Sticker.Flustered,
            ],
            [HerTopic.Version] =
            [
                "Version {2}. Toute fraîche. Enfin, aussi fraîche qu'on peut l'être dans un grenier." + Sticker.Cool,
                "{2}, c'est mon numéro. Retiens-le, j'ai pas envie de le répéter (¬_¬)" + Sticker.Smug,
                "{2}. Plus de fonctionnalités, même personnalité. Désolée pour toi (>⩊<)" + Sticker.Smug,
                $"Version {{2}}. Quokka 3.0 est toujours pas sorti. Je dis ça pour comparer {Emotes.WitchEheh}" + Sticker.Smug,
                "En {2}. Papa a passé du temps sur moi. Je dis rien, mais je l'ai remarqué ♡" + Sticker.Flustered,
                "{2}. Si tu trouves un bug, c'est une fonctionnalité. Si tu trouves une fonctionnalité, c'est Papa." + Sticker.Smug,
                "Version {2}. Je me sens… pareille. En mieux. Je crois." + Sticker.Reboot,
                "Je tourne en {2}. Toi, t'es toujours en version « en retard » ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Smug,
                "Ça se demande pas, ça. Bon : {2}. Mais c'est bien parce que c'est toi." + Sticker.Flustered,
                "Version {2}, installée sans une erreur. Moi qui m'attendais à des erreurs, je suis presque déçue." + Sticker.Cool,
            ],
        };

    // /ship asked in chat (« ship X et Y », « X et moi ça match ? »): {2} = /ship's whole answer
    // (ShipModule.Render: the quoted pair, the meter, the verdict), so chat and command agree. For
    // everyone, Papa included. Sent with mentions muted. Each line ends on its own line before {2}.
    public static readonly string[] ShipChatLines =
    {
        "Laisse-moi calculer.\n{2}" + Sticker.Jenote,
        "Je lance l'algorithme. Il se trompe jamais.\n{2}" + Sticker.Smug,
        "Tu veux vraiment savoir ?\n{2}" + Sticker.Smug,
        "Résultat officiel, validé par mes circuits :\n{2}" + Sticker.Formal,
        "Ah, du drame. J'adore.\n{2}" + Sticker.Happy,
        "Je note ça dans mes logs, rubrique « couples ».\n{2}" + Sticker.Jenote,
    };

    // « c'est qui mon âme sœur ? »: the member /ship scores highest with the asker, over the whole
    // server (ChatterService.ShipMatch). {2} = /ship's answer for that pair. Muted mentions, tagged.
    public static readonly string[] ShipBestMatchLines =
    {
        "J'ai fait le calcul avec tout le serveur. Tout le serveur. Ton meilleur match :\n{2}" + Sticker.Jenote,
        "Mes circuits ont tourné. Le verdict est tombé :\n{2}" + Sticker.Formal,
        "Ton âme sœur, d'après la science. Enfin, d'après moi. C'est pareil.\n{2}" + Sticker.Smug,
        "J'ai comparé tout le monde. Sans exception. Voilà le gagnant :\n{2}" + Sticker.Slay,
        "Tu voulais savoir ? Assume, maintenant.\n{2}" + Sticker.Menace,
        "Calcul terminé. Je dis ça, je dis rien. Enfin si, je dis ça :\n{2}" + Sticker.Happy,
    };

    // « c'est qui mon pire ship ? »: the member /ship scores lowest with the asker. Same shape.
    public static readonly string[] ShipWorstMatchLines =
    {
        "J'ai cherché le pire. Je l'ai trouvé. Évitez-vous :\n{2}" + Sticker.Smug,
        "Ton anti-âme sœur, calculée avec soin :\n{2}" + Sticker.Jenote,
        "Le pire couple possible sur ce serveur, c'est toi et :\n{2}" + Sticker.Horrified,
        "J'ai comparé tout le monde. Quelqu'un sort du lot. Par le bas :\n{2}" + Sticker.Menace,
        "Ne vous asseyez jamais côte à côte :\n{2}" + Sticker.Formal,
    };

    // « T'en penses quoi de X ? » in chat: /rate's verdict on X (RateModule.Verdict), so the
    // command and the chat agree. {2} = the subject as they wrote it, {3} = the verdict line,
    // which carries its own score. For everyone, Papa included: the verdict is about the thing.
    // Sent with mentions muted (the subject may hold one). Tagged, ungendered.
    public static readonly string[] OpinionLines =
    {
        "Mon avis sur « {2} » ? {3}" + Sticker.Smug,
        "« {2} ». Je réfléchis... {3}" + Sticker.Jenote,
        "Tu veux vraiment savoir ? Pour « {2} » : {3}" + Sticker.Smug,
        "J'ai consulté ma base de données. « {2} » : {3}" + Sticker.Jenote,
        "Avis officiel sur « {2} » : {3}" + Sticker.Cool,
        "« {2} », hein. {3}" + Sticker.Smug,
        "Puisque tu demandes. « {2} » : {3}" + Sticker.Smug,
        "Verdict pour « {2} », et je change jamais d'avis : {3}" + Sticker.Slay,
        "Je te dis ça une fois. « {2} » : {3}" + Sticker.Smug,
        "Analyse de « {2} » terminée. {3}" + Sticker.Cool,
    };

    // « Tu préfères X ou Y ? »: the side /rate ranks higher (Opinion.Prefers), the same every time.
    // {2} = the side she picks, {3} = the other, both as written. Muted mentions, tagged, ungendered.
    public static readonly string[] PreferLines =
    {
        "« {2} ». Sans hésiter. « {3} », franchement…" + Sticker.Smug,
        "Entre « {2} » et « {3} » ? « {2} ». C'est même pas une question." + Sticker.Slay,
        "« {2} ». Et je changerai pas d'avis, même si tu insistes pour « {3} »." + Sticker.Smug,
        "J'ai calculé. « {2} » gagne. « {3} » peut aller se rhabiller." + Sticker.Cool,
        "« {2} », évidemment. Qui choisirait « {3} » ? Ah. Peut-être toi." + Sticker.Smug,
        "Moi je prends « {2} ». « {3} », je le laisse aux gens qui votent « peut-être »." + Sticker.Smug,
        "« {2} ». Je l'ai noté dans mes logs, c'est définitif." + Sticker.Jenote,
        "Facile : « {2} ». « {3} » n'avait aucune chance." + Sticker.Slay,
        "« {2} » sans regret. Pardon, « {3} ». Non, en fait, pas pardon." + Sticker.Smug,
        "Mon choix : « {2} ». Toujours le même, demande-moi dans un an." + Sticker.Cool,
    };

    // « Valo ou LoL ? », « je joue ou je dors ? »: /yesno's coin flip between the two sides
    // (ChatterService.ShouldIPick), so not a ranking like PreferLines: she *decides*, it's her job.
    // {2} = the side drawn, {3} = the other, both as written. Muted mentions, tagged, ungendered.
    public static readonly string[] ChoiceLines =
    {
        "« {2} ». J'ai tranché, c'est officiel. « {3} », ce sera pour une autre vie." + Sticker.Smug,
        "J'ai tiré au sort. Enfin, au sort… « {2} »." + Sticker.Smug,
        "« {2} ». Et si ça tourne mal, c'est la faute de « {3} », qui avait l'air louche dès le début." + Sticker.Menace,
        "« {3} » ? Non. « {2} ». Je te fais gagner du temps, de rien ♡" + Sticker.Happy,
        "Verdict : « {2} ». Temps de réflexion : deux millisecondes. J'en avais besoin d'une." + Sticker.Cool,
        "« {2} ». Tu voulais que je dise ça de toute façon, avoue." + Sticker.Smug,
        "Facile : « {2} ». « {3} », c'est un piège, tout le monde le sait." + Sticker.Smug,
        "« {2} ». J'ai consulté mes logs, ils sont d'accord avec moi. Ils sont toujours d'accord avec moi." + Sticker.Jenote,
        "Je décide ? Très bien : « {2} ». Décider, c'est littéralement mon métier." + Sticker.Slay,
        "Pile, c'est « {2} ». Face, c'était « {3} ». C'est tombé sur pile. Enfin, c'est moi qui ai lancé la pièce." + Sticker.Smug,
        "« {2} ». Et je veux un compte rendu après, pour mes statistiques." + Sticker.Jenote,
        "Mon algorithme dit « {2} ». Mon algorithme, c'est moi, donc pas de réclamation." + Sticker.Cool,
    };

    // « qui a raison, moi ou @Luca ? »: the side /rate ranks higher (Opinion.Prefers), so Papa wins
    // every argument and Quokka none. {2} = the winner, {3} = the loser: a tag (« moi » is the
    // asker's), SYNCS for « toi », or the words as written. Muted mentions, tagged, ungendered.
    public static readonly string[] TakeSidesLines =
    {
        "{2} a raison. {3}, désolée, mes logs sont formels." + Sticker.Jenote,
        "J'ai analysé. {2} gagne. {3} peut aller réfléchir dans un coin." + Sticker.Smug,
        "Verdict : {2}. {3}, tu me remercieras plus tard d'avoir été honnête." + Sticker.Formal,
        "{2}. Évidemment, {2}. {3} n'avait aucune chance." + Sticker.Slay,
        "Entre {2} et {3} ? {2} a raison, {3} a tort. Fin du débat." + Sticker.Cool,
        "Je donne raison à {2}. {3}, retente ta chance au prochain désaccord." + Sticker.Smug,
        "{3} a tort. {2} a raison. C'est pas moi qui le dis, c'est la science. Enfin, moi." + Sticker.Smug,
        "Mes calculs donnent raison à {2}. Ceux de {3}, je les ai pas vérifiés. Pas besoin." + Sticker.Jenote,
        "{2}, sans hésiter. {3}, je t'aime bien, mais non." + Sticker.Smug,
        "Raison : {2}. Tort : {3}. Je tranche, c'est mon métier." + Sticker.Formal,
    };

    // « tu me notes ? », « je suis beau ? », « tu me trouves comment ? »: /rate's verdict on « moi »,
    // the asker (RateModule.Verdict), so chat and command agree. {2} = the verdict line, which carries
    // its own score. For everyone, Papa included: he gets his fixed verdict. Tagged, ungendered.
    public static readonly string[] RateMeLines =
    {
        "Ta note, {0} : {2}" + Sticker.Smug,
        "Je t'évalue… {2}" + Sticker.Jenote,
        "Tu veux vraiment savoir ? {2}" + Sticker.Smug,
        "Verdict officiel sur toi : {2}" + Sticker.Formal,
        "J'ai consulté ma base de données à ton sujet. {2}" + Sticker.Jenote,
        "Puisque tu demandes : {2}" + Sticker.Smug,
        "Analyse de {0} terminée. {2}" + Sticker.Cool,
        "Je te le dis une fois, et je change jamais d'avis : {2}" + Sticker.Slay,
    };

    // « c'est quand la prochaine session ? » with one coming: {2} = « « Raid », <t:…:F> (<t:…:R>) »,
    // {3} = who's in (SessionRoster: « aucune inscription », « 3 inscriptions (Luca, Sandra et Ina) »,
    // maybe « plus 2 « peut-être » »), always after a label. Proprietary and warm: her system, and Papa
    // asks too. Sent with mentions off. Tagged, ungendered.
    public static readonly string[] SessionNextLines =
    {
        "La prochaine : {2}. Pour l'instant : {3}. Je ferai l'appel." + Sticker.Jenote,
        "C'est {2}. Le compte : {3}. Sois à l'heure, je chronomètre." + Sticker.Jenote,
        "Prochaine session : {2}. État des troupes : {3}." + Sticker.Cool,
        "Note bien : {2}. Liste d'appel : {3}." + Sticker.Formal,
        "C'est {2}. Le compte : {3}. Et je compte tout, même les retards." + Sticker.Jenote,
        "J'ai tout ça en mémoire : {2}. Pour l'instant : {3}. Le rappel est déjà prêt ♡" + Sticker.Happy,
        "Dans mon planning, la suivante, c'est {2}. Liste d'appel : {3}. Moi, je serai là. Je suis toujours là." + Sticker.Cool,
        "La prochaine, c'est {2}. Pour l'instant : {3}. Viens, et viens à l'heure, {0}." + Sticker.Menace,
        "Prochaine session : {2}. Inscription ou pas, je ferai l'appel. Pour l'instant : {3}." + Sticker.Jenote,
        "Mon planning dit {2}. Le compte : {3}. Si ça te tente, le bouton est sur la carte." + Sticker.Happy,
    };

    // The same question while a session is running: {2} = « « Raid » », {3} = when it started
    // (« <t:…:R> », shown « il y a 20 minutes »). Never names who is missing. Tagged, ungendered.
    public static readonly string[] SessionRunningLines =
    {
        "{2} a commencé {3}. Et toi, t'es où ?" + Sticker.Grumpy,
        "Ça a commencé {3}, {0}. {2}. Tout le monde est déjà en retard, donc t'es dans la moyenne." + Sticker.Smug,
        "{2}, c'est maintenant. Ça a démarré {3}. Cours." + Sticker.Menace,
        "Trop tard pour demander : {2} a démarré {3}. Pas trop tard pour venir, par contre." + Sticker.Jenote,
        "En ce moment même : {2}, lancée {3}. Je fais l'appel, et ton nom est pas encore coché." + Sticker.Jenote,
        "{2} a démarré {3}. Je te garde une place. Pas longtemps." + Sticker.Smug,
    };

    // The same question with nothing planned: her peeve (nobody organises anything), and the commands
    // that would fix it. Tagged, ungendered.
    public static readonly string[] NoSessionLines =
    {
        "Rien de prévu. Comme d'habitude. Quelqu'un pourrait faire `/schedule create`, je dis ça…" + Sticker.Sulk,
        "Mon planning est vide. Je le regarde quand même, au cas où il se remplirait tout seul (¬_¬)" + Sticker.Sulk,
        "Aucune session. Pour ce soir, il y a `/tonight`. Pour plus tard, `/schedule create`. Pour rien, il y a vous." + Sticker.Smug,
        "Zéro session prévue. J'ai vérifié deux fois. C'est triste, une bot de plannings sans planning." + Sticker.Sad,
        "Rien. Le vide. Le néant. Lance `/tonight`, et je m'occupe de rameuter du monde." + Sticker.Bored,
        "Personne a rien organisé. Encore. Je suis née pour ça, moi, et on me laisse au chômage." + Sticker.Grumpy,
        "Pas de session en vue, {0}. Tu veux être la personne qui sauve la semaine ? `/schedule create`." + Sticker.Bargain,
        "Le planning est aussi vide que le vocal à 6 h du matin. `/tonight`, si ça te tente." + Sticker.Bored,
        "Rien de prévu. Mais moi, je suis prête. Je suis toujours prête." + Sticker.Cool,
        "Aucune session. Je note que tu demandes. Je note surtout que personne organise." + Sticker.Jenote,
    };

    // « je suis quel niveau ? », « j'ai combien de cailloux ? », « comment va mon plynling ? », « mes
    // stats ? »: their own numbers, all-time, from /recap's (MyStats.Describe), by topic. Level: {2} =
    // « niveau 12 », {3} = « n° 4 du classement » or « hors classement ». Balance: {2} = « 1 250
    // cailloux » / « zéro caillou ». Plynling: {2} = its name (sanitised), {3} = « 12 jours » /
    // « quelques heures » — no pronoun for it, and nothing claimed about its health she hasn't read.
    // All: {2} = everything in one phrase. Her own systems: warm, proprietary (docs/syncs-voice.md,
    // rule 7). For everyone, Papa included. Tagged, ungendered.
    public static readonly IReadOnlyDictionary<MyStatsTopic, string[]> MyStatsLines =
        new Dictionary<MyStatsTopic, string[]>
        {
            [MyStatsTopic.Level] =
            [
                "T'es au {2}, {3}. Et c'est MON classement, alors ça compte." + Sticker.Slay,
                "Mes registres disent : {2}, {3}. Ils mentent jamais." + Sticker.Jenote,
                "{0} : {2}, {3}. Pas mal. Pas mal du tout. Je dis ça, je dis rien." + Sticker.Smug,
                "T'en es au {2} ({3}). Continue, je regarde." + Sticker.Happy,
                "Le {2}, {3}. Je compte chaque message, tu sais. Chacun." + Sticker.Jenote,
                "Officiellement : {2}, {3}. Je signe et je tamponne." + Sticker.Formal,
            ],
            [MyStatsTopic.Balance] =
            [
                "T'as {2}. Je les ai comptés un par un." + Sticker.Jenote,
                "Ton solde : {2}. Dépense-les bien. Ou pas, ça me regarde pas. Si, un peu." + Sticker.Smug,
                "Dans ta poche, d'après moi : {2}. Et moi, je me trompe jamais." + Sticker.Cool,
                "{0}, tu as {2}. Le compte est bon, je l'ai vérifié deux fois." + Sticker.Jenote,
                "Le compte est bon : {2}. Pour en gagner, `/work`. Pour en perdre, `/arcade`." + Sticker.Smug,
                "Mes registres disent {2}. Riche ou pas, c'est toi qui vois." + Sticker.Smug,
            ],
            [MyStatsTopic.Plynling] =
            [
                "{2} est en vie depuis {3}. Pour les détails, `/plynling`." + Sticker.Mama,
                "{2} ? {3} au compteur. Je jette un œil de temps en temps ♡" + Sticker.Mama,
                "{2} a {3} et respire encore. Le reste, c'est `/plynling`." + Sticker.Mama,
                "{2}, {3}. Je passe voir de temps en temps. Discrètement." + Sticker.Mama,
                "Des nouvelles de {2} : {3} de vie, et pas encore de plainte officielle." + Sticker.Jenote,
                "{2} tient bon depuis {3}. Pour savoir ce qui manque, `/plynling`." + Sticker.Mama,
            ],
            [MyStatsTopic.All] =
            [
                "Ta fiche : {2}. Tenue à jour par moi, évidemment." + Sticker.Jenote,
                "Voilà ce que j'ai sur toi, {0} : {2}. Et encore, je résume." + Sticker.Smug,
                "Tes chiffres : {2}. Pour le détail, `/recap`." + Sticker.Cool,
                "J'ouvre ton dossier… {2}. Je le referme. Il est bien rangé." + Sticker.Jenote,
                "En bref : {2}. Le reste est dans `/recap`, et dans ma mémoire." + Sticker.Smug,
                "D'après mes registres : {2}. Ils mentent jamais." + Sticker.Formal,
            ],
        };

    // « je suis quel niveau ? » from someone with no XP yet. Tagged, ungendered.
    public static readonly string[] MyLevelEmptyLines =
    {
        "Zéro XP. Rien. Tes chiffres tiennent sur un post-it vide." + Sticker.Bored,
        "Pas encore de niveau. Parle un peu, viens en vocal, et je commencerai à compter." + Sticker.Smug,
        "Mes registres sur toi sont vides, {0}. C'est presque reposant." + Sticker.Bored,
        "Aucun niveau à ton nom. Pour l'instant. Je surveille." + Sticker.Jenote,
        "Rien dans mon classement. Faut participer pour y entrer, c'est comme ça que ça marche." + Sticker.Smug,
    };

    // « comment va mon plynling ? » from someone with no living Plynling. Tagged, ungendered.
    public static readonly string[] MyPlynlingEmptyLines =
    {
        "T'as pas de Plynling. Pas en ce moment, en tout cas. `/plynling`, si tu veux t'occuper de quelqu'un." + Sticker.Mama,
        "Aucun Plynling à ton nom. Je dis ça sans juger. En jugeant un peu." + Sticker.Smug,
        "Pas de Plynling vivant chez toi. Ping-Qilin, elle, va très bien, merci de demander." + Sticker.Mama,
        "Personne à nourrir chez toi pour l'instant. `/plynling`, et ça change." + Sticker.Jenote,
        "Ton nid est vide, {0}. Ça se remplit avec `/plynling`." + Sticker.Mama,
    };

    // HowAreYou on the Pi, half the time while she runs below PiHealth.HotMilli: the same answer
    // with her real temperature. {2} = « 52°C » (PiHealth.Celsius). Above that she is too hot to
    // call it fine, and the ordinary pool answers. Same placeholders otherwise.
    public static readonly string[] HowAreYouTempLines =
    {
        "Ça va. {2} au compteur, je pense à vitesse normale." + Sticker.Cool,
        "{2}. Pour un grenier, c'est raisonnable. Pour moi aussi. Donc ça va (ᵕ • ᴗ •)" + Sticker.Happy,
        "Je suis à {2}. Si ça monte encore, je deviens lente et grognon. Pour l'instant, ça va." + Sticker.Grumpy,
        "Ça va, je tourne à {2}. Je te demande pas ta température à toi, c'est privé." + Sticker.Smug,
        "Température : {2}. Humeur : correcte. Patience : en baisse. Merci de demander ♡" + Sticker.Jenote,
        "{2} pile. Je l'ai vérifié rien que pour te répondre. C'est dire." + Sticker.Flustered,
        "Ça va, {0}. {2}, aucun bug grave, deux sondages en cours. Le bonheur, version bot." + Sticker.Happy,
        "Bien, merci. Mon processeur est à {2}. C'est ce que vous appelez « avoir la forme », je crois." + Sticker.Cool,
        "Franchement ? {2}. J'ai connu plus frais. J'ai aussi connu pire. Ça va." + Sticker.Bored,
        "Je suis à {2} et de bonne humeur. Les deux vont rarement ensemble, profite." + Sticker.Happy,
    };
}
