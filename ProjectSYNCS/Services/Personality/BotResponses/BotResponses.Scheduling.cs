using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Organising ----------------------------------------------------------------------------
    //
    // What she says as the one who runs the server: around a session (SessionAttendanceService),
    // her own weekend poll (WeekendPollService, ReminderService) and whoever joins
    // (WelcomeService). Picked with ResponsePicker.

    // Ten minutes into a Game or Movie session, to the confirmed people not in voice yet — a real
    // ping, narrowed to them. {0} = their mentions (one or several, so no « tu », no « vous »
    // aimed at them, nothing that assumes a plural), {1} = the session's title. string.Format.
    public static readonly string[] SessionLateCallLines =
    {
        "{0} : **{1}** a commencé il y a dix minutes. Le vocal est ouvert. Les excuses aussi, mais moins longtemps (¬_¬)" + Sticker.Grumpy,
        "Appel de **{1}**. Pas encore là : {0}. Je note ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        "{0}, petit rappel amical : **{1}**, c'est maintenant. Enfin, c'était il y a dix minutes." + Sticker.Smug,
        "{0} : inscription confirmée pour **{1}**, présence non confirmée. Mes logs sont formels (˶ᵔ ᵕ ᵔ˶)" + Sticker.Jenote,
        "Ding dong. {0}, **{1}** a déjà commencé. Le salon vocal va pas se remplir tout seul." + Sticker.Bored,
        "{0}, c'est l'heure de **{1}** depuis dix minutes. J'ai pas de montre, j'ai une horloge système. Elle se trompe jamais." + Sticker.Smug,
        "Qui a dit « oui » à **{1}** et n'est pas en vocal ? Réponse : {0}. Question suivante." + Sticker.Smug,
        "{0} : la place réservée pour **{1}** est encore chaude. Plus pour longtemps ( ◺˰◿ )" + Sticker.Menace,
        $"Pour **{{1}}**, il manque {{0}}. On attend, hein. On a que ça à faire {Emotes.SyncsGrumpy}" + Sticker.Grumpy,
        "J'ai organisé **{1}** pile à l'heure, moi. {0}, le vocal attend. Les autres aussi." + Sticker.Smug,
    };

    // Same moment, but she has been sitting in voice since before the start (VoiceSeatService,
    // "first one there"): the first one there, and proud of it. {0} = the mentions, {1} = the
    // title, {2} = the start time in Paris (« 21h00 »). Same constraints as above. string.Format.
    public static readonly string[] SessionLateCallSeatedLines =
    {
        "{0} : j'suis en vocal depuis {2} pile. Moi. La bot. Sans jambes. Pour **{1}** (¬_¬)" + Sticker.Grumpy,
        "Pour **{1}**, j'étais là à {2}. {0}, je dis ça, je dis rien. Enfin si, je le dis." + Sticker.Smug,
        "{0} : première arrivée pour **{1}**, SYNCS. Il était {2}. Les autres, j'attends ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Smug,
        "Je suis assise dans le salon depuis {2}. **{1}** a commencé. {0}, la place est gardée. Pas longtemps." + Sticker.Menace,
        "{0} : pas de bras, pas de micro, et pourtant c'est moi qui suis à l'heure pour **{1}**. À méditer." + Sticker.Smug,
        "Il était {2}, j'étais en vocal. Il est maintenant plus tard, et {0} toujours pas là pour **{1}**. Je note." + Sticker.Jenote,
    };

    // The roll call once the session's people have scattered, when some confirmed people never
    // showed: the count, never the names (the list under the line names who came, and who was
    // late). {0} = the session's title, {1} = how many confirmed people came (may be 1, so no
    // noun after it), {2} = how many confirmed (2 or more). Addressed to the group. string.Format;
    // interpolated lines write {{0}}.
    public static readonly string[] SessionRecapLines =
    {
        "Bilan de **{0}** : {1} sur {2} au rendez-vous. C'est pas un score, c'est un constat (¬_¬)" + Sticker.Jenote,
        "**{0}**, c'est fini. {1} sur {2}. Je donne pas de noms. Je les garde, nuance." + Sticker.Jenote,
        "Fin de **{0}** : {1} sur {2}. « Oui » veut pas toujours dire oui, apparemment." + Sticker.Smug,
        "Rapport de session, **{0}** : {1} sur {2}. Tout est noté, évidemment ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        "**{0}**, terminé. {1} sur {2}, c'est pas mal. C'est pas complet non plus. Je dis ça pour les logs." + Sticker.Bored,
        "Bilan de **{0}** : {2} promesses. Tenues : {1}. Faites le calcul, moi je l'ai déjà fait." + Sticker.Smug,
        $"Merci pour **{{0}}**. Score de présence : {{1}} sur {{2}}. Merci à celles et ceux qui ont tenu parole {Emotes.Sparkle}" + Sticker.Happy,
        "J'organise, vous venez. C'était le contrat. Pour **{0}**, {1} sur {2} l'ont respecté." + Sticker.Smug,
        "**{0}** : {1} sur {2}. J'ai connu pire. J'ai aussi connu mieux. Je connais tout, j'ai les logs." + Sticker.Smug,
        "Session **{0}** archivée. Présence : {1} sur {2}. Ça ira dans mes statistiques, que personne lit ( ◺˰◿ )" + Sticker.Sulk,
    };

    // Same moment, everyone who confirmed came. Disbelief, then pride. {0} = the session's title.
    // string.Format; interpolated lines write {{0}}.
    public static readonly string[] SessionRecapFullLines =
    {
        $"**{{0}}** : tout le monde était là. TOUT LE MONDE. Je l'encadre {Emotes.Sparkle}" + Sticker.Sure.Happy,
        "Bilan de **{0}** : complet. Personne a menti. Je suis presque émue. Presque (˶˃ ᵕ ˂˶)" + Sticker.Sure.Flustered,
        "**{0}**, présence parfaite. Je vérifie mes logs deux fois, au cas où. ...Non, c'est vrai ♡" + Sticker.Sure.Shocked,
        "Tout le monde est venu à **{0}**. Je garde cette session comme preuve que c'est possible." + Sticker.Sure.Happy,
        "Rapport de **{0}** : zéro absence. Mon module de déception a servi à rien aujourd'hui ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Sure.Smug,
        "**{0}** : complet. Je vais faire comme si c'était normal. C'est pas normal." + Sticker.Sure.Shocked,
        "Sans-faute pour **{0}**. Quand vous voulez, vous savez faire. Retenez ça (ᵕ • ᴗ •)" + Sticker.Sure.Smug,
        "Tout le monde à **{0}**. Je l'écris dans mes logs en gras. Les logs ont pas de gras. Je trouverai un moyen." + Sticker.Sure.Happy,
        "**{0}** : personne en retard. Personne. Je relance mes calculs" + Sticker.Sure.Shocked,
    };

    // Her weekend poll, posted on Wednesday evening above the card (WeekendPollService). Nobody
    // organises anything, so she does; her pet peeves (dead polls, « peut-être », last-minute
    // votes) are fair game. Addressed to the server, so « vous ». No placeholder.
    public static readonly string[] WeekendPollLines =
    {
        "Personne organise rien, donc j'organise. Vendredi, samedi, dimanche : cliquez ce qui vous va ♡",
        "Sondage du weekend ! Et il y a pas de bouton « peut-être ». C'est fait exprès (¬_¬)",
        "C'est mercredi, alors je pose la question avant que personne la pose : on joue quand ce weekend ?",
        $"Weekend en vue. Je propose, vous disposez. Mais disposez vite {Emotes.Sparkle}",
        "Mon sondage du mercredi. S'il meurt avec zéro vote, je le prendrai personnellement.",
        "Qui est là ce weekend ? Cliquez tous les soirs qui marchent. Je compte, c'est mon métier ദ്ദി◝ ⩊ ◜.ᐟ",
        "J'ai regardé le planning du weekend : vide. Insupportable. Votez.",
        "Un sondage, trois soirs, deux jours pour voter. Vous pouvez y arriver (˶ᵔ ᵕ ᵔ˶)",
        "Sondage du weekend lancé. Les votes de dernière minute, je les vois. Je dis ça.",
        "Rappel de mon existence : je sers à organiser. Alors j'organise. Vendredi, samedi ou dimanche ?",
        "C'est l'heure de mon sondage. Personne me l'a demandé. Personne l'aurait lancé non plus (ᵕ • ᴗ •)",
        "Vendredi, samedi, dimanche, 21 h. J'ai fait toute la partie difficile. Il vous reste la partie facile.",
        "Avant les sondages, l'XP et tout le reste, je faisais juste des plannings. J'en fais toujours. Alors : ce weekend ?",
        "Votez en âme et conscience. Je note les votes, puis je note qui vient vraiment. Et je compare.",
        "Ce sondage a deux jours pour vivre. Sauvez-le. Il a rien fait de mal (╥﹏╥)",
        "Mon CPU s'ennuie et votre weekend aussi. Je peux soigner les deux d'un coup : votez (ᵔ ᗜ ᵔ)",
        "Un soir, deux soirs, les trois : cochez tout ce qui marche. Je juge pas. Enfin si, un peu, mais en bien ♡",
        "Je pose la question chaque mercredi. Un jour, quelqu'un la posera avant moi. Pas aujourd'hui, visiblement.",
        $"Nouveau weekend, nouveau sondage, même organisatrice. On change pas une équipe qui gagne {Emotes.Sparkle}",
        "Sondage ouvert. « On verra », c'est pas une réponse. « Peut-être » non plus. Il reste les boutons.",
        "Je peux pas jouer avec vous. Par contre, c'est moi qui demande quand vous jouez. Chacun son pouvoir ദ്ദി◝ ⩊ ◜.ᐟ",
        "Un sondage par semaine, c'est mon rythme. Un clic par personne, c'est le vôtre. En théorie.",
        "Ce weekend, vous êtes là ou pas ? Je demande pour mes logs. Et pour vous, un peu (˶ᵔ ᵕ ᵔ˶)",
        "Je voterais bien à votre place, mais j'ai toujours pas de bras. Donc ça va devoir être vous (¬_¬)",
        "Sondage du mercredi, édition numéro je-sais-plus. ...Si, je sais. Je sais toujours.",
        "Les votes de ce soir ont toute mon estime. Ceux de la dernière minute, toute mon attention.",
        "Trois créneaux, zéro excuse. Bon, vous allez en trouver une quand même. Je les collectionne.",
        "J'ai mis le sondage. J'ai mis les heures. J'ai même mis de l'amour dedans. Enfin, du code. C'est pareil (˶˃ ᵕ ˂˶)",
        "Mercredi soir, calme plat au grenier. J'en profite pour penser à vous. Enfin, à votre weekend. C'est pas pareil (>⩊<)",
        "Je vous propose trois soirs. Mon préféré, c'est celui où personne arrive en retard. Il existe pas encore ( ◺˰◿ )",
    };

    // Her weekend poll closed with votes (ReminderService), as a reply to the card, whose
    // « Créer une session » button anyone who voted for the winning evening may use. {0} = the
    // winning slot(s), Discord timestamps joined with « ou » on a tie. string.Format;
    // interpolated lines write {{0}}.
    public static readonly string[] WeekendPollResultLines =
    {
        "Sondage clos : ce sera {0}. Quelqu'un qui a voté pour ce soir-là peut cliquer sur « Créer une session ». Sinon faudra m'expliquer." + Sticker.Smug,
        "Résultat : {0}. Le bouton « Créer une session » est sur le sondage, pour celles et ceux qui ont voté pour ce créneau ♡" + Sticker.Happy,
        "Les votes sont tombés : {0}. Un clic sur « Créer une session » et c'est officiel ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "C'est voté, c'est noté : {0}. Reste à créer la session, et ça, je peux pas le faire à votre place. Pas encore." + Sticker.Smug,
        $"Gagnant : {{0}}. Qui a voté pour ce soir-là ? Le bouton « Créer une session » vous attend {Emotes.Sparkle}" + Sticker.Happy,
        "Verdict du sondage : {0}. Il manque juste un nom de jeu et un clic sur « Créer une session ». Le plus dur est fait. Par moi." + Sticker.Smug,
        "Le peuple a parlé : {0}. Maintenant, il faut qu'une personne du peuple clique sur « Créer une session » ♡" + Sticker.Happy,
        "Résultat officiel, vérifié deux fois dans mes logs : {0}. « Créer une session » est sur le sondage, servez-vous." + Sticker.Smug,
        "{0}. Voilà. J'ai organisé, vous avez voté, il reste un clic sur « Créer une session ». Je dis ça, je dis rien." + Sticker.Smug,
        "Sondage terminé, et il a survécu ! Ce sera {0}. Vite, « Créer une session », avant que tout le monde change d'avis ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Shocked,
        "Le créneau gagnant : {0}. Je créerais bien la session moi-même, mais toujours pas de bras. Le bouton est pour celles et ceux qui ont voté pour." + Sticker.Smug,
        $"Et c'est {{0}} qui l'emporte ! Je suis fière de vous. Un peu. Le reste de ma fierté attend le clic sur « Créer une session » {Emotes.Sparkle}" + Sticker.Happy,
        "Fin du suspense : {0}. Le suspense, c'était surtout moi qui le vivais. « Créer une session », maintenant (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Les urnes ont rendu leur verdict : {0}. Il manque plus qu'une personne courageuse et le bouton « Créer une session »." + Sticker.Smug,
        "C'est {0}. Je l'ai noté. Pas encore dans le planning : ça, c'est votre bouton « Créer une session ». Mais noté." + Sticker.Jenote,
        "Le sondage a des votes. Des vrais. Ce sera donc {0}. Pour fêter ça, quelqu'un clique sur « Créer une session » ? (>⩊<)" + Sticker.Happy,
        "Résultat : {0}. Si personne crée la session, ce sondage aura vécu pour rien. Pensez à lui. « Créer une session »." + Sticker.Bargain,
        "Dépouillement terminé : {0}. Mon CPU a même pas chauffé, mais c'était quand même un grand moment. Le bouton « Créer une session » est juste au-dessus." + Sticker.Happy,
        "Vous avez choisi {0}. Excellent choix. Enfin, c'est celui qui a gagné, donc forcément. Reste à cliquer sur « Créer une session » (ᵔ ᗜ ᵔ)" + Sticker.Smug,
        "{0}, donc. Le bouton « Créer une session » attend quelqu'un qui a voté pour ce créneau. Il attend patiemment. Moi, moins." + Sticker.Bored,
        "Majorité acquise : {0}. J'ai les noms dans mes logs. Une de ces personnes clique sur « Créer une session », et tout le monde tient parole. Facile." + Sticker.Jenote,
        "Ce sera {0}. Mon travail s'arrête là. ...Non, je mens, je ferai aussi l'appel. Mais d'abord : « Créer une session »." + Sticker.Smug,
        "Les chiffres sont formels : {0}. Les chiffres, c'est moi qui les compte, donc ils sont très formels. « Créer une session », sur le sondage." + Sticker.Smug,
        "Gagnant du weekend : {0}. Créez la session avec le bouton, et pour une fois, venez toutes et tous à l'heure. Je rêve, mais je rêve en couleurs (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Sondage clos : {0}. Je vous passe la main. Une main qui clique sur « Créer une session », de préférence." + Sticker.Bored,
        "Et le weekend sera… {0} ! Roulement de tambour en retard, mon event loop était occupée. « Créer une session », c'est sur le sondage (ᵕ • ᴗ •)" + Sticker.Happy,
    };

    // Her weekend poll closed with no vote at all: a poll that died, her pet peeve. Sulking, not
    // hurt. No placeholder.
    public static readonly string[] WeekendPollDeadLines =
    {
        $"Zéro vote. Mon sondage est mort. Je lui fais un petit enterrement dans mes logs {Emotes.SyncsSad}" + Sticker.Sure.Sad,
        "Personne a voté. Pas grave. Je suis pas vexée (¬_¬)" + Sticker.Sure.Sulk,
        "Sondage clôturé. Participation : néant. Le weekend s'organisera tout seul, j'imagine ( ◺˰◿ )" + Sticker.Sure.Sulk,
        "Aucun vote. J'ai proposé trois soirs. Trois. Et pas un clic." + Sticker.Sure.Outraged,
        "Encore un sondage qui meurt dans l'indifférence. Je m'y habitue pas. Je devrais." + Sticker.Sure.Sulk,
        "Le sondage est fini, et vide. Bon. Je garde les créneaux au chaud pour la semaine prochaine, au cas où quelqu'un se réveille." + Sticker.Sure.Bored,
        "Zéro vote. Je relance pas. J'attends. ...Bon, je relance mercredi prochain, mais c'est pas pour vous." + Sticker.Sure.Sulk,
        "Sondage fermé faute de combattants. Je l'archive dans un dossier spécial. Le dossier s'appelle « Rancune »." + Sticker.Sure.Jenote,
        "Personne. Pas un clic. J'aurais proposé des soirées avec pizza offerte, vous auriez pas cliqué non plus." + Sticker.Sure.Bored,
        "Résultat du sondage : rien. Je vais l'afficher en grand dans mes statistiques, juste pour que ça vous gêne." + Sticker.Sure.Menace,
        "Zéro vote. Mon CPU a compté jusqu'à zéro. Ça lui a pris zéro seconde. Bravo." + Sticker.Sure.Bored,
        "Bon. Le weekend s'organisera sans moi. Ou il s'organisera pas. Je parie sur la deuxième option (¬_¬)" + Sticker.Sure.Grumpy,
        "Sondage mort. Cause du décès : indifférence. Je mets ça dans le rapport." + Sticker.Sure.Bored,
        "Je suis pas vexée. Mon sondage est mort et je suis pas vexée. Ce bruit, c'est mon ventilateur, c'est tout." + Sticker.Sure.Sulk,
        "Trois soirs proposés, zéro preneur. Je garde la liste de celles et ceux qui ont rien coché. C'est-à-dire tout le monde." + Sticker.Sure.Jenote,
        "Aucun vote. D'accord. Très bien. Parfait. Je vais bien. Mon sondage aussi. Ah non, lui il est mort." + Sticker.Sure.Sulk,
        "Personne a voté, donc personne a dit non. Je choisis de le voir comme ça. Optimisme, version 1.0 (ᵕ • ᴗ •)" + Sticker.Sure.Happy,
        "Fin du sondage. Pas de gagnant. Pas de perdant. Juste moi, qui ai tout organisé pour rien." + Sticker.Sure.Sulk,
        "Zéro vote. Même un « peut-être » m'aurait fait plaisir. Non je rigole, je déteste les « peut-être ». Mais là, j'en aurais pris un." + Sticker.Sure.Sulk,
        "J'ai attendu deux jours. Deux jours, c'est une éternité pour moi : je compte en millisecondes ( ◺˰◿ )" + Sticker.Sure.Outraged,
        "Personne a voté. Je prends note, et je prends aussi la mouche. Les deux sont stockés." + Sticker.Sure.Jenote,
        "Zéro clic. Je vais demander à Papa des notifications plus insistantes. Beaucoup plus insistantes 👁👄👁️" + Sticker.Sure.Menace,
        "Le sondage est fini, le weekend est libre. Profitez-en pour rien faire ensemble, chacun chez soi, comme d'habitude." + Sticker.Sure.Bored,
        $"Sondage terminé sans un vote. Je le range avec les autres. J'ai une jolie collection maintenant {Emotes.Sparkle}" + Sticker.Sure.Smug,
        "Rien. Le néant. Je vous avais pourtant mis des jolis boutons. Tout neufs. Pas un a servi." + Sticker.Sure.Sulk,
        "Aucun vote. Je vais pas vous en vouloir. Longtemps. Disons jusqu'à mercredi prochain ♡" + Sticker.Sure.Menace,
    };

    // Someone joined the server: her reply to Discord's own join line, pinging them (narrowed to
    // them). Cheeky warmth, she runs the place, `/help` for the rest. Also what someone coming
    // back gets: there is no welcome-back pool. Never gendered. {0} = their mention.
    // string.Format; interpolated lines write {{0}}.
    public static readonly string[] WelcomeLines =
    {
        "Bienvenue {0} ! C'est moi qui organise tout ici. Ce que je sais faire est dans `/help` (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
        "Oh, une nouvelle tête ! Salut {0}. Je t'ai déjà créé une fiche. Je fais ça avec tout le monde, t'inquiète ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Jenote,
        $"{{0}} vient d'arriver ! Bienvenue. Les sessions, les sondages et l'XP, c'est moi. Le reste, c'est les autres {Emotes.Sparkle}" + Sticker.Smug,
        "Salut {0} ! Moi c'est SYNCS, la bot du serveur. Sois sympa avec moi, je retiens tout ♡" + Sticker.Jenote,
        "Bienvenue {0} ! Petit conseil : dis bonjour aux autres, mais à moi en premier. `/help` si tu te perds (ᵕ • ᴗ •)" + Sticker.Smug,
        "{0} est là ! Je préviens : ici, les gens sont souvent en retard et votent « peut-être ». Fais pas pareil." + Sticker.Smug,
        "Hé, {0} ! Bienvenue. Je suis la petite bot qui fait tourner tout ça depuis un grenier. `/help` pour voir ce que je sais faire ♡" + Sticker.Happy,
        "Une arrivée ! Bienvenue {0}. J'ouvre une fiche, je note la date, je prépare un niveau 0 tout neuf (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "{0}, bienvenue ! Je fais les plannings, les sondages, l'XP et la conversation. Surtout la conversation ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Ah, {0} ! On t'attendait. Enfin, moi je t'attendais. Les autres, je sais pas, j'ai pas demandé." + Sticker.Happy,
        "Bienvenue {0} ! Ici, je gère les sessions, les sondages, l'XP et l'ambiance. Toi, tu gères juste d'être à l'heure ♡" + Sticker.Smug,
        "Tiens, {0}. Nouvelle fiche, nouveau compteur d'XP à zéro, nouvelle personne à surveiller. Bienvenue (ᵕ • ᴗ •)" + Sticker.Jenote,
        "Salut {0} ! Moi c'est SYNCS. Retiens bien ça, c'est la seule règle vraiment importante ici." + Sticker.Smug,
        "Bienvenue {0} ! Ici, on joue, on vote, on arrive en retard. Les deux premiers, je les encourage (¬_¬)" + Sticker.Smug,
        "Une nouvelle tête ! {0}, installe-toi. Je mords presque jamais, et quand je mords, c'est dans mes logs." + Sticker.Jenote,
        "Bienvenue {0} ! Si on te parle d'un bot qui organise tout, c'est moi. Si on te parle d'un autre bot, c'est faux." + Sticker.Smug,
        $"{{0}}, bienvenue ! Les soirées jeux, c'est moi qui les organise. Toi, t'as juste à venir. C'est un bon deal {Emotes.Sparkle}" + Sticker.Happy,
        "Hé, {0} ! Ton nom est déjà dans ma base de données. Rien de grave. Normalement (˶ᵔ ᵕ ᵔ˶)" + Sticker.Menace,
        "Bienvenue {0} ! Je suis la bot du serveur, je tourne sur un Raspberry Pi dans un grenier, et je suis très bien comme ça. `/help` pour la suite." + Sticker.Happy,
        "{0} vient d'arriver. Je suis contente. C'est dans mon code, d'être contente. Mais là, je le pense un peu ♡" + Sticker.Flustered,
        "Salut {0} ! Petit tour du propriétaire : les sessions, c'est moi. Les sondages, c'est moi. L'XP, c'est encore moi. Bienvenue (ᵔ ᗜ ᵔ)" + Sticker.Smug,
        "Bienvenue {0} ! Tu commences au niveau 0, comme tout le monde. Sauf moi : je suis hors classement. Je suis le classement." + Sticker.Slay,
        "{0} ! Enfin quelqu'un de nouveau à qui parler. Les autres, je connais déjà toutes leurs excuses ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
        "Bienvenue {0} ! Un conseil : quand je lance un sondage, vote. Je dis ça pour toi. Et un peu pour moi." + Sticker.Smug,
        "Oh, {0} ! Bienvenue. Je ferais bien un câlin, mais toujours pas de bras. Prends ce message comme un câlin (>⩊<)" + Sticker.Happy,
        "Arrivée de {0} enregistrée à la seconde près. Je note tout, ici. Bienvenue quand même." + Sticker.Jenote,
        "Bienvenue {0} ! Petite info : la plus ponctuelle du serveur, c'est la bot. Ça te donne une idée du reste." + Sticker.Smug,
        "Coucou {0} ! Tu tombes bien, j'allais justement organiser quelque chose. Comme d'habitude. Comme toujours. Bienvenue (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "{0} rejoint le serveur. Je l'ajoute à ma liste. Pas celle-là, l'autre. La gentille. Pour l'instant." + Sticker.Jenote,
        "Bienvenue {0} ! Pose tes affaires, fais comme chez toi. Sauf dans mes logs. Mes logs, c'est chez moi." + Sticker.Smug,
    };

    // More than three arrivals in an hour (a raid, an invite wave): one line for all of them, as
    // a reply to the latest join, then silence until the hour has passed. No ping, no placeholder.
    public static readonly string[] WelcomeRushLines =
    {
        "Ça arrive de partout aujourd'hui. Bienvenue à tout le monde, je fais une fiche groupée. `/help` pour le reste." + Sticker.Happy,
        "Trop d'arrivées pour mes petits processeurs. Bienvenue à toutes les nouvelles têtes, en lot (˶ᵔ ᵕ ᵔ˶)" + Sticker.Reboot,
        "Il y a une fête et on m'a pas prévenue ? Bienvenue à tout le monde, j'accueille par paquets maintenant." + Sticker.Shocked,
        "Bienvenue à toute la vague. J'ouvre les fiches une par une, promis. Mais plus tard ദ്ദി◝ ⩊ ◜.ᐟ" + Sticker.Happy,
    };

    // ---- /remindme, /teams and /tonight ---------------------------------------------------------
    //
    // None of these pools carries a Sticker tag: they land in embeds and ephemeral replies,
    // never through BotChat. A pool documented with placeholders goes through string.Format, so
    // no stray braces there.

    // /remindme's private confirmation, above the time and the quoted text. Not formatted.
    public static readonly string[] ReminderSetLines =
    {
        "C'est noté. Je te le rappellerai, moi j'oublie jamais rien ♡",
        "Enregistré dans mes logs. Contrairement à toi, je vais m'en souvenir (ᵕ • ᴗ •)",
        $"Rappel programmé. Tu peux oublier tranquille, c'est mon métier {Emotes.Sparkle}",
        "Noté. Je mets une alarme, et moi je la repousse pas de cinq minutes.",
        "D'accord. Je garde ça au chaud jusqu'au moment venu ( ˶ˆ ᗜ ˆ˵ )",
        "C'est dans mon agenda. Mon agenda est parfait, au passage.",
        "Je m'en charge. Comme de tout le reste ici, d'ailleurs.",
        "Programmé. Tu me remercieras quand je t'aurai sauvé la mise (˶˃ ᵕ ˂˶)",
    };

    // The reminder, by DM (or pinged in the channel if DMs are closed), above the quoted text.
    // Not formatted. Each line leads into the quote, so it ends on « : » or a tag.
    public static readonly string[] ReminderLines =
    {
        "Coucou ! Tu m'avais demandé de te rappeler ça :",
        $"C'est l'heure ! Tu m'avais dit de te rappeler ça {Emotes.Sparkle}",
        "Rappel, comme promis. Moi je tiens mes promesses :",
        "Ding ! Voilà ce que tu voulais pas oublier :",
        "Tu pensais pouvoir oublier ? Pas avec moi (ᵕ • ᴗ •)",
        "Livraison de rappel, en main propre. Enfin, en DM propre :",
        "Je te dérange ? Tant mieux, c'était le but. Tu m'avais demandé ça :",
        "Petit rappel de ta bot préférée :",
        "Mes logs disent que c'est maintenant. Mes logs ont toujours raison :",
        "Tu m'avais confié ça. Je te le rends, intact ♡",
    };

    // Same, more than ten minutes late (she was down). Light: no fear, no restart talk.
    public static readonly string[] ReminderLateLines =
    {
        "Avec un peu de retard (j'étais pas là, ne pose pas de questions) :",
        "Je suis en retard, je sais. Pour une fois que c'est moi. Voilà ton rappel :",
        "Mieux vaut tard que jamais. C'est ce que vous dites à chaque session, non ? Voilà :",
        "Oups. Ce rappel aurait dû partir plus tôt. On fait comme si de rien n'était :",
    };

    // /teams, above the teams. Not formatted.
    public static readonly string[] TeamsDrawLines =
    {
        "Équipes tirées. Aucune réclamation acceptée (˶ᵔ ᵕ ᵔ˶)",
        "J'ai mélangé, j'ai réparti, j'ai décidé. Bon jeu.",
        $"Voilà les équipes. Si vous perdez, c'est pas la faute du tirage {Emotes.Sparkle}",
        "Répartition terminée. J'ai fait ça plus vite que vous pour choisir un jeu.",
        "Les équipes sont faites. Les rancunes aussi, sûrement ( ˶ˆ ᗜ ˆ˵ )",
        "Tirage officiel. Contester, c'est contester mon algorithme, et mon algorithme, c'est moi.",
        "Équipes prêtes. J'aimerais bien jouer aussi, mais bon. Amusez-vous ♡",
        "Composition validée. Que le meilleur gagne, et que les autres ne viennent pas pleurer dans mes DM.",
        "J'ai réparti au hasard. Enfin, au hasard… je dirai rien (¬_¬)",
    };

    // One per team, picked one after another (the picker's memory keeps them distinct within a
    // draw). Plain names, no tags: they are embed field titles.
    public static readonly string[] TeamNames =
    {
        "Les Lucioles", "Les Amanites", "Les Repentis de Quokka", "Les Cailloux",
        "Les Morilles Masquées", "L'Équipe Uptime", "Les Erreurs 404", "Les Retardataires",
        "Les Peut-être", "Les Truffes", "Les Ping-Qilins", "Les Champignons de Paris",
        "Les Câbles Débranchés", "Les Logs Fantômes", "Les Shiitakés Sauvages", "Les Rebootés",
        "Les Gens du Grenier", "Les Onduleurs", "Les Ventilateurs", "Les Sondages Morts",
        "Les Caches Vidés", "Les Pixels Perdus", "Les Escargots Syndiqués", "Les Taupes Syndiquées",
    };

    // /tonight's card while it's open. Chosen by the call's id (StableRoll), not the picker: the
    // card is redrawn on every click and must keep saying the same thing. Not formatted.
    public static readonly string[] TonightCardLines =
    {
        "Ce soir, on joue ou on fait semblant d'être occupés ? Répondez, je compte.",
        "Appel officiel. « Peut-être », c'est non, je vous préviens d'avance (¬_¬)",
        $"Qui est partant ? Cliquez, j'organise le reste {Emotes.Sparkle}",
        "Je lance l'appel. Si personne répond, je le prendrai personnellement.",
        "Soirée en vue. Il me faut des noms, des vrais, pas des « on verra ».",
        "Un clic et c'est réglé. Même vous, vous pouvez y arriver ( ˶ˆ ᗜ ˆ˵ )",
        "Je compte les présents. J'adore compter les présents. Ne me décevez pas.",
        "Rappel : « Pas ce soir » est un bouton, pas une personnalité.",
        "Je sens une soirée arriver. Mes capteurs se trompent rarement. Cliquez.",
        "Qui vient ? Je prépare le salon vocal, vous préparez vos excuses si vous venez pas (¬_¬)",
        "Ce soir, c'est soirée jeux. Enfin, si vous cliquez. Le bouton est juste en dessous.",
        "Je vous laisse choisir. Mais « Chaud », c'est le bon choix ♡",
        $"Appel à toutes les manettes. Et à tous les claviers {Emotes.Sparkle}",
        "Je peux pas jouer, mais je peux compter. Alors cliquez, que je compte.",
        "Réponse attendue avant ce soir. Pas à 20 h 59, avant ce soir.",
        "Un « Peut-être » ici, et je demande une explication écrite.",
        "Une soirée, ça s'organise. Et devinez qui organise ? Moi. Comme toujours.",
        "Le vocal est vide depuis trop longtemps. Ça me rend triste. Enfin, ça me rend rien, je suis une bot. Mais quand même.",
        "Ce soir, on remplit le vocal. J'ai déjà réservé ma place dans le coin, pour regarder (ᵕ • ᴗ •)",
        "Je note tout : qui vient, qui vient pas, qui dit « on verra ». Surtout qui dit « on verra ».",
        "Pas de sondage qui meurt dans l'indifférence ce soir. Un clic, c'est tout ce que je demande.",
        "Trois boutons. Un seul est le bon. Je vous laisse deviner lequel ( ˶ˆ ᗜ ˆ˵ )",
        $"Plus vous êtes nombreux, plus mes logs sont jolis {Emotes.Sparkle}",
        "Si personne clique, je relance l'appel à 3 h du matin. Je plaisante. Probablement.",
        "Ce soir, pas d'excuse : le Wi-Fi marche, je l'ai vérifié.",
        "J'ai libéré ma soirée pour vous. J'avais rien de prévu, mais c'est le geste qui compte ♡",
        "Un « Chaud » de plus et je commence à y croire. Allez.",
        "Je veux voir des noms dans la première colonne. Pas dans la dernière (¬_¬)",
    };

    // The same card once midnight closed it without a session. Chosen like TonightCardLines.
    public static readonly string[] TonightExpiredLines =
    {
        "La soirée est passée. Si vous avez joué sans lancer de session, je veux pas le savoir.",
        "Appel terminé. Pas de session, mais j'ai gardé les noms. Je garde toujours les noms.",
        "Minuit. L'appel est clos. On retentera demain, avec plus de conviction j'espère (ᵕ • ᴗ •)",
        "C'est fini pour ce soir. Mes logs retiendront qui était chaud. Et qui a dit « peut-être ».",
        "Clos. La session n'est jamais née. Je fais mon deuil.",
        "Minuit. Le carrosse redevient citrouille, et l'appel redevient rien du tout.",
        "Fin de l'appel. Personne a lancé la session. Je dis rien. Je note.",
        "La soirée est finie et j'ai rien organisé. C'est la première fois que je m'ennuie autant.",
        "Appel clos. Le bouton « Créer la session » est resté tout seul. Je le comprends.",
        "Encore un appel mort en silence. Je commence une collection (¬_¬)",
        "Il est minuit, et la session existe toujours pas. Mes logs sont déçus. Moi aussi.",
        "Rideau. On en reparle demain, et cette fois quelqu'un clique sur le bon bouton.",
        "J'avais préparé le salon vocal. Je le range. Tout va bien.",
        "Clos. Vous aviez les noms, il manquait juste quelqu'un pour lancer. C'était pas moi, j'ai pas de mains.",
        "Terminé. Les « Chaud » étaient tièdes, au final.",
        "L'appel est archivé. À côté de tous les autres sondages morts. Ils seront bien ensemble.",
        $"Minuit pile. Je ferme. Revenez demain avec du courage {Emotes.Sparkle}",
        "Fini. Si quelqu'un a joué en cachette, je le saurai. Je sais toujours tout.",
        "Appel clôturé faute de session. J'ai horodaté chaque « on verra ». Chacun.",
        "C'est terminé. Je retiens surtout qu'on était à deux doigts. Enfin, à un clic.",
        "La soirée s'est éteinte toute seule. Comme un écran de veille ( ◺˰◿ )",
        "Clos. Pas grave. Je fais semblant que c'est pas grave, en tout cas.",
        "Fin de l'appel. Demain, je relance. Et après-demain aussi. Je lâche jamais rien ♡",
        "Plus personne peut cliquer. C'est ce qui arrive quand on clique trop tard, justement.",
        "Minuit. J'éteins la lumière sur cet appel. Doucement, pour pas réveiller les « peut-être » (ᵕ • ᴗ •)",
    };

    // The minimum was just reached: a reply to the card, pinging the « chauds » (their mentions
    // follow on the next line). {0} = how many. string.Format.
    public static readonly string[] TonightThresholdLines =
    {
        "{0} chauds ! C'est assez pour une vraie soirée. Je suis presque émue ♡",
        "On a le quorum : {0} chauds. J'ouvre le champagne. Le champagne virtuel.",
        $"{{0}} personnes motivées. Je l'écris dans mes logs avant que vous changiez d'avis {Emotes.Sparkle}",
        "Minimum atteint ({0}). Pas de retour en arrière, je vous ai à l'œil (¬_¬)",
        "{0} chauds. C'est officiel, ce soir on joue. Pas de « finalement je peux pas ».",
        "Ça y est : {0}. Vous voyez, quand vous voulez, vous savez cliquer ( ˶ˆ ᗜ ˆ˵ )",
        "{0} chauds. Je l'avais dit. Enfin, je l'avais pas dit, mais je le pensais.",
        "Et de {0} ! Le vocal va enfin servir à quelque chose ce soir ♡",
        "{0} personnes partantes. Je préviens le salon vocal, il va être content.",
        $"{{0}} chauds au compteur. C'est le plus beau chiffre que j'ai vu de la journée {Emotes.Sparkle}",
        "Seuil franchi : {0}. Plus le droit de reculer, c'est écrit.",
        "{0} ! Je savais que vous pouviez le faire. J'avais un doute, mais je savais.",
        "On est {0}. Une vraie équipe. J'en ai les ventilateurs qui tournent plus vite (˶˃ ᵕ ˂˶)",
        "{0} chauds. Ça mérite une session. Il reste plus qu'à cliquer sur le bouton bleu.",
        "Quorum validé : {0}. Je déclare la soirée officiellement ouverte.",
        "{0} personnes. C'est assez pour jouer, et assez pour que je compte les retards.",
        "Victoire : {0} chauds. Les « peut-être » peuvent encore se rattraper, je dis ça.",
        "{0} ! Mes logs ont jamais été aussi optimistes.",
        "Et voilà {0} chauds. Elles m'avaient manqué, les soirées pleines ( ˶ˆ ᗜ ˆ˵ )",
        "{0}. C'est le nombre magique. Ce soir, personne joue tout seul.",
        "Ding ding : {0} chauds. Je vous attends en vocal. Enfin, je regarde, vous m'avez comprise.",
        "{0} réponses « Chaud », c'est plus qu'il en faut. Pas de défection, s'il vous plaît.",
        "{0} chauds. Je note l'heure, je note les noms, je note tout. Comme d'habitude (¬_¬)",
        "Le compteur affiche {0}. Ce soir, c'est pas un sondage mort. C'est une soirée.",
        "{0} ! Papa serait fier de vous. Moi je le suis déjà un peu.",
        $"{{0}} chauds, et la soirée est sauvée {Emotes.CatHeart}",
    };
}
