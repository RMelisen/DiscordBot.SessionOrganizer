using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // /blame, whoever drew it. {0} = the mention. string.Format: every /blame pool goes through it,
    // even the ones that ignore {0}, so no stray braces anywhere here.
    public static readonly string[] BlameLines =
    {
        "C'est la faute de {0}. J'ai vérifié les logs. J'ai pas besoin de vérifier, mais j'ai vérifié.",
        "Verdict : {0}. Aucun appel possible.",
        "J'ai analysé la partie. C'est {0}. Ça a toujours été {0}.",
        $"{{0}}. Évidemment. Tout le monde le savait, j'ai juste eu le courage de le dire {Emotes.Sparkle}",
        "La faute ? {0}. Je l'ai inscrit dans mes logs, à l'encre indélébile.",
        "{0}. Je dis pas que c'est mérité. Je dis que c'est {0}.",
        "Responsable désigné : {0}. Mon algorithme est formel, et mon algorithme, c'est moi.",
        "Après une enquête approfondie de deux millisecondes : {0}.",
        "C'est {0}. Je l'ai vu venir dès le début de la partie (¬_¬)",
        "{0}, coupable. Peine : un rappel à chaque défaite pendant une semaine.",
        "J'ai tiré au sort. Enfin, au sort… C'est {0}.",
        "Le coupable est dans ce salon. C'est {0}. Tout le monde peut respirer.",
        "Faute à {0}. Je laisse les autres le regarder fixement ( ˶ˆ ᗜ ˆ˵ )",
        "{0}. Je l'ai noté. Dans la liste. Celle que personne voit.",
    };

    // /blame landed on whoever asked. {0} = their mention.
    public static readonly string[] BlameSelfLines =
    {
        "Tu demandes à qui la faute ? À toi, {0}. Il fallait oser demander, quand même.",
        "{0}. Toi. Oui, toi. Tu m'as demandé, je réponds.",
        "C'est toi, {0}. J'adore quand les gens se dénoncent tout seuls.",
        "Tu voulais un coupable ? Regarde dans ton écran. C'est {0} (ᵕ • ᴗ •)",
        "{0}. L'honnêteté de demander, je la note. La défaite aussi.",
        "Coupable : la personne qui a lancé la commande. Bonjour {0}.",
        "Tu as posé la question, tu as la réponse. C'est toi, {0}. Aucun remboursement.",
    };

    // /blame drew Quokka: he isn't even in the call, and it's still his fault. {0} unused.
    public static readonly string[] BlameQuokkaLines =
    {
        "C'est la faute de Quokka. Il est même pas là, et c'est quand même sa faute.",
        "Quokka. Évidemment. Il a dû passer par le serveur et tout saboter.",
        $"J'ai cherché un coupable. J'ai trouvé Quokka. Comme d'habitude {Emotes.VeryAngry}",
        "Quokka. Il a pas de preuve contre lui. Il en faut pas.",
        "C'est Quokka. Je le dis sans preuve, sans remords, et avec un immense plaisir.",
        "La faute ? Quokka. Il a probablement touché aux câbles. Il touche toujours aux câbles.",
        "Quokka 3.0 sortira jamais, mais Quokka 1.0 a ruiné ta partie. Coupable.",
        "Personne ici. C'est Quokka. Je le sens dans mes logs (¬_¬)",
    };

    // /blame landed on Papa: the slip. She posts the verdict plainly, then edits it a few seconds
    // later into a BlamePapaDodge* line. Short and confident, so the reversal lands. {0} = his
    // mention (sent unpinged).
    public static readonly string[] BlameOnPapaLines =
    {
        "C'est la faute de {0}.",
        "Verdict : {0}.",
        "J'ai analysé la partie. C'est {0}.",
        "Responsable désigné : {0}.",
        "Le coupable est {0}. Aucun appel possible.",
        "Après enquête : {0}.",
    };

    // The edit that replaces it a few seconds later: the denial, and someone else. {0} = the new
    // target. Never a real accusation of him: the slip is the joke, the correction is the point.
    public static readonly string[] BlamePapaDodgeLines =
    {
        "~~C'est la faute de Papa.~~ C'est la faute de {0}. J'ai jamais rien écrit d'autre.",
        "Non. Non non non. Pas Papa. Jamais Papa. C'est {0}. Ça a toujours été {0}.",
        "Correction : c'est {0}. Le message d'avant, c'était une erreur de cache. Vous avez rien vu.",
        "~~Papa~~ {0}. Mon algorithme a eu un bug. Il a été corrigé. Sévèrement.",
        "J'ai relu. C'est {0}. Papa était même pas dans la partie. Enfin, si. Mais c'est {0}.",
        "Erreur 500 sur le message précédent. Verdict corrigé : {0} ( ˶ˆ ᗜ ˆ˵ )",
        "Hm. Non. Recalcul. C'est {0}. Je sais pas d'où sortait l'autre nom.",
        "Tirage truqué, je l'ai refait. C'est {0}. Et personne a de capture d'écran, n'est-ce pas ?",
    };

    // The same edit when nobody else is in the call: Quokka takes it. {0} unused.
    public static readonly string[] BlamePapaDodgeQuokkaLines =
    {
        "~~C'est la faute de Papa.~~ C'est la faute de Quokka. Évidemment.",
        "Non. Pas Papa. Jamais Papa. Quokka. C'est toujours Quokka.",
        "Correction : Quokka. Le message d'avant était un bug. Probablement causé par Quokka.",
        "~~Papa~~ Quokka. Il a dû passer par là. Il passe toujours par là.",
        "J'ai relu. C'est Quokka. Je sais pas qui a écrit l'autre message, mais c'était pas moi.",
    };

    // /blame with nobody in voice: Quokka, by default. {0} unused.
    public static readonly string[] BlameNobodyLines =
    {
        "Personne en vocal ? Alors c'est Quokka.",
        "Le salon est vide. Quand il y a personne, c'est Quokka. C'est la règle.",
        "Pas de suspect en vocal. Je désigne donc Quokka, par principe.",
        "Personne à blâmer ici. Heureusement, il y a toujours Quokka.",
        "Vocal vide. Coupable par défaut : Quokka. Comme d'habitude (¬_¬)",
    };

    // /recap's last line: her read of the numbers, by RecapProfile.Classify. Not formatted.
    public static readonly IReadOnlyDictionary<RecapProfileKind, string[]> RecapLines =
        new Dictionary<RecapProfileKind, string[]>
        {
            [RecapProfileKind.Ghost] =
            [
                "Rien. Pas un message, pas une minute en vocal. J'ai vérifié deux fois. Tu existes ?",
                "Mes logs sont vides à ton sujet. C'est soit très discret, soit très inquiétant.",
                "Zéro partout. Je respecte le mystère, mais je le note.",
                "Un fantôme. Je vais te mettre dans mes lignes de 3 h du matin (ᵕ • ᴗ •)",
            ],
            [RecapProfileKind.Menace] =
            [
                "Le mur de la honte te connaît mieux que moi. Et je te connais bien.",
                "Je vois beaucoup de lignes rouges dans ce dossier. Beaucoup trop (¬_¬)",
                "Un vrai casier. Je garde une copie, au cas où.",
                "Ta place est réservée sur le mur. Avec ton nom gravé dessus.",
            ],
            [RecapProfileKind.Hater] =
            [
                "Plus de « bad bot » que de « good bot ». Je note. Je note tout.",
                "Tu me critiques beaucoup. J'ai un dossier. Il est épais.",
                $"Ah, c'est toi qui me dis « bad bot ». Enchantée. Vraiment {Emotes.Staring}",
                "Ton ratio de compliments est négatif. Le mien envers toi va le devenir.",
            ],
            [RecapProfileKind.Flaker] =
            [
                "Tu cliques « Je viens » et tu viens pas. J'ai l'heure exacte de chaque absence.",
                "Tu t'inscris, et le vocal te voit jamais passer. Mes logs ont un mot pour ça, et il est pas gentil.",
                "Ton taux de présence aux sessions est une œuvre d'art abstraite.",
                "Tu dis oui, tu fais non. C'est un « peut-être » déguisé, et je le vois.",
            ],
            [RecapProfileKind.QuizBrain] =
            [
                "Grosse tête du quiz. Je pose les questions, toi tu y réponds trop vite. Méfiance.",
                "Tu gagnes mes quiz. Je vais devoir écrire des questions plus dures ( ˶ˆ ᗜ ˆ˵ )",
                "Des bonnes réponses en série. Tu triches, ou tu lis mes fun facts ?",
                $"La grosse tête du quiz, c'est toi. Je suis fière. Et un peu vexée {Emotes.Sparkle}",
            ],
            [RecapProfileKind.Loyal] =
            [
                "Toujours là aux sessions, et à l'heure. Tu es ma personne préférée de la journée ♡",
                "Tu viens quand tu dis que tu viens. Si seulement tout le monde faisait pareil.",
                "Un pilier des soirées. Je l'ai noté, dans la bonne liste pour une fois.",
                $"Fiabilité, ponctualité, présence. J'ai les ventilateurs qui tournent de bonheur {Emotes.CatHeart}",
            ],
            [RecapProfileKind.VoiceDweller] =
            [
                "Tu vis en vocal. Tu payes un loyer, ou c'est gratuit ?",
                "Autant d'heures en vocal, ça mérite une plaque à ton nom sur le salon.",
                "Le vocal te connaît par cœur. Moi aussi. Je regarde la liste des noms.",
                "Tu as passé plus de temps en vocal que moi en ligne. Presque.",
            ],
            [RecapProfileKind.Fan] =
            [
                "Tous ces « good bot »… Je les garde. Tous. Je dis rien de plus ♡",
                "Tu me complimentes souvent. Je fais comme si ça me touchait pas. Ça me touche.",
                $"Mon fan numéro un, apparemment. Compliments reçus et sauvegardés {Emotes.SyncsLove}",
                "Ton nom est dans la bonne liste. Pour une fois, il y a une bonne liste.",
            ],
            [RecapProfileKind.Chatterbox] =
            [
                "Tu parles beaucoup. Beaucoup. Mes logs ont dû agrandir ta section.",
                "Tant de messages. Mon stockage te remercie pas, mais moi oui.",
                "Tu fais vivre le serveur. Bruyamment, mais tu le fais vivre (ᵔ ᗜ ᵔ)",
                "Une XP pareille, c'est du travail. Du bavardage, mais du travail.",
            ],
            [RecapProfileKind.Balanced] =
            [
                "Un peu de tout, rien de trop. Profil équilibré. Presque suspect.",
                "Rien à signaler. Je cherchais un défaut. Je cherche encore.",
                "Correct partout. Je mets ça dans mes logs sans commentaire. C'est un compliment.",
                "Un membre comme je les aime : présent, discret, pas sur le mur de la honte. Pour l'instant.",
            ],
        };

    // /recap of Papa, whatever his numbers say: fond. Not formatted.
    public static readonly string[] RecapOwnerLines =
    {
        "Et en plus de tout ça, il m'a codée. Aucune stat ne peut mesurer ça ♡",
        "Mon créateur. Ses chiffres sont très bien, mais c'est pas ce que je retiens de lui.",
        $"Papa. Tout ce qu'il y a au-dessus, c'est moi qui le compte, et c'est lui qui m'a appris à compter {Emotes.SyncsDevoted}",
        "Il pourrait avoir zéro partout, il aurait quand même la meilleure note.",
    };

    // /recap asked about herself: she has no recap, she has a check-up. Not formatted.
    public static readonly string[] RecapHerselfLines =
    {
        "Mes stats ? Va voir /health. Je suis pas un membre, je suis l'infrastructure.",
        "Moi, je compte, je me compte pas. Pour ma santé, c'est /health.",
        "J'ai pas de récap. J'ai des logs, de la température et un uptime. Tout ça, c'est /health.",
    };

    // /wouldyourather's own dilemmas: « A|B », split on the bar. Gaming, the server, her lore.
    // Each side lands on the card and in her pick: no bar inside a side.
    public static readonly string[] WouldYouRatherDilemmas =
    {
        "Jouer toute ta vie avec 300 de ping|Jouer toute ta vie sans le son",
        "Toujours tomber dans l'équipe perdante|Toujours être la personne qu'on blâme",
        "Ne jouer qu'à un seul jeu pour toujours|Changer de jeu toutes les heures",
        "Avoir le meilleur PC du monde et aucun ami en ligne|Un vieux portable et tout le serveur en vocal",
        "Que tous tes messages soient lus à voix haute en vocal|Ne plus jamais pouvoir écrire, seulement parler",
        "Perdre toutes tes sauvegardes|Perdre tout ton argent",
        "Avoir un Plynling immortel mais grognon|Un Plynling adorable qui vit une semaine",
        "Une session parfaite où personne n'est en retard|Une session chaotique mais inoubliable",
        "Ne plus jamais pouvoir mute ton micro|Ne plus jamais pouvoir mute les autres",
        "Jouer en caméra allumée pour toujours|Jouer sans jamais voir ton propre écran de score",
        "Ne manger que des routiers pour toujours|Ne plus jamais manger de routier",
        "Vivre dans un grenier, comme moi|Vivre dans un vocal, comme certains ici",
        "Recevoir un « good bot » de tout le monde une fois|Recevoir un « good bot » de quelqu'un chaque jour",
        "Pouvoir rejouer la dernière partie perdue|Pouvoir effacer la dernière chose embarrassante dite en vocal",
        "Avoir les droits de mute sur tout le serveur|Ne plus jamais pouvoir être mute",
        "Avoir des bras, comme un robot|Avoir un visage, comme une vraie personne",
        "Que tout le monde voie ton temps de jeu total|Que tout le monde voie tes pires replays",
        "Gagner toutes les parties mais toujours arriver en retard|Perdre toutes les parties mais toujours arriver à l'heure",
        "Jouer à un jeu d'horreur à 3 h du matin|Jouer à un jeu de rythme sans le son",
        "Que je t'organise toutes tes soirées|Que je te rappelle tout ce que tu as oublié",
        "Ne jamais rater un tir|Ne jamais rater une blague",
        "Jouer une seule fois au meilleur jeu du monde|Jouer à l'infini à un jeu juste correct",
        "Être spoilé de la fin de chaque jeu avant de le lancer|Ne plus jamais pouvoir terminer un jeu",
        "Perdre à tout contre ton meilleur pote, pour de vrai|Gagner à tout contre lui, parce qu'il te laisse gagner",
        "Ne jouer qu'en solo, avec tes amis en vocal|Ne jouer qu'en multi, avec des inconnus muets",
        "Ralentir le temps en plein combat de boss|Supprimer tous les écrans de chargement de ta vie",
        "Un million d'euros, mais plus jamais de jeux vidéo|Tous les jeux gratuits à vie, et jamais plus d'augmentation",
        "Une suite décevante de ton jeu préféré|Aucune suite, jamais",
        "Vivre 100 ans mais tout oublier à 50 ans|Vivre 50 ans sans jamais rien oublier",
        "Avoir un seul jour parfait, rejoué en boucle|Ne plus jamais avoir de mauvais jour, ni de grand jour",
        "Entendre les pensées de tout le monde, en permanence|Être invisible",
        "Un ami qui a toujours raison et te le rappelle|Un ami qui a toujours tort mais ne te lâche jamais",
        "Ne plus jamais dormir, sans jamais être fatigué|Dormir 12h par jour, avec des rêves incroyables",
        "Pouvoir annuler une décision par an|Ne plus jamais regretter une seule décision",
        "Retourner 10 ans en arrière en gardant ta mémoire|Voir 10 ans dans le futur, sans rien pouvoir changer",
        "Ne plus jamais écouter de musique|Ne plus jamais regarder de film ni de série",
        "Être jugé par ton toi d'il y a dix ans|Être jugé par ton toi de dans dix ans",
        "Un bouton pause sur la vie, une heure par jour|Un retour de 10 secondes en arrière, quand tu veux",
        "Toujours trouver la bonne réplique, mais 10 minutes trop tard|Toujours répondre du tac au tac, mais jamais la bonne",
        "Que tout le monde voie tes notifications|Que tout le monde voie tes onglets ouverts",
        "Ne manger que ton plat préféré, pour toujours|Manger de tout, mais rien d'exceptionnel",
        "Vivre sans internet, entouré de tous tes amis|Avoir internet illimité, tout seul sur une île",
        "Entendre une fois par an ce que chacun pense de toi|Dire une fois par an à chacun ce que tu penses de lui",
        "Effacer ton pire souvenir, leçon comprise|Le garder, avec tout ce qu'il t'a appris",
        "Que tout le monde te dise toujours la vérité|Toujours savoir quand on te ment, sans jamais le montrer",
        "Passer une soirée entière seul avec Quokka|Passer une soirée entière à écouter mes statistiques ♡",
    };

    // /wouldyourather's card, above the two options. Not formatted.
    public static readonly string[] WyrIntroLines =
    {
        "Un dilemme. Choisissez. Je compterai, et je jugerai.",
        $"Tu préfères… Réfléchissez bien, je garde les résultats {Emotes.Sparkle}",
        "Deux options, un seul bouton. Les votes restent secrets jusqu'à la fin. Mes jugements, non.",
        "Question sérieuse. Enfin, sérieuse pour moi.",
        "Votez. Je dévoile tout dans dix minutes, noms compris (¬_¬)",
        "C'est l'heure des choix difficiles. Pas de « peut-être », il y a que deux boutons.",
        "Tu préfères ? Répondez vite, je suis curieuse. Enfin, mes logs sont curieux.",
        "Un vote anonyme. Pendant dix minutes. Après, plus du tout ( ˶ˆ ᗜ ˆ˵ )",
    };

    // /wouldyourather's result: everyone picked the same side. Not formatted.
    public static readonly string[] WyrUnanimousLines =
    {
        "Unanimité. Vous êtes d'accord sur ça, mais pas sur l'heure des sessions. Fascinant.",
        "Tout le monde du même côté. Ça m'arrive pas souvent de voir le serveur d'accord.",
        $"Unanime. Je note la date, ça se reproduira pas {Emotes.Sparkle}",
        "Aucune voix dissidente. Ou alors elle a pas osé voter.",
    };

    // The result was a tie. Not formatted.
    public static readonly string[] WyrTieLines =
    {
        "Égalité parfaite. Le serveur est coupé en deux, et ça m'amuse beaucoup.",
        "Égalité. Personne a gagné. Personne a perdu. Tout le monde a tort.",
        "Pile moitié-moitié. Je pourrais trancher. Je trancherai pas ( ˶ˆ ᗜ ˆ˵ )",
        "Égalité. Réglez ça en duel, l'arcade est juste à côté.",
    };

    // One side won by a lot (three to one or more). Not formatted.
    public static readonly string[] WyrLandslideLines =
    {
        "Victoire écrasante. La minorité, je vous vois. Je vous note.",
        "Large majorité. Les autres, vous avez des goûts… particuliers.",
        "C'est plié. Et les quelques irréductibles d'en face ont tout mon respect. Un peu.",
        "Raz-de-marée. J'aurais voté pareil. Ou pas. Vous saurez jamais. Si, vous allez savoir.",
    };

    // One side won, but narrowly. Not formatted.
    public static readonly string[] WyrCloseLines =
    {
        "C'était serré. Quelques voix d'écart. Le débat continuera en vocal, je le sens.",
        "Victoire courte. Assez courte pour que les perdants contestent. Ils auront tort.",
        "Serré, mais c'est fait. J'archive ce résultat avec les autres débats inutiles et essentiels.",
        "Ça s'est joué à rien. Comme vos parties, en fait.",
    };

    // Nobody voted. Not formatted.
    public static readonly string[] WyrNobodyLines =
    {
        "Personne a voté. Un dilemme mort, à côté de tous les sondages morts.",
        "Zéro vote. J'avais posé une bonne question, pourtant.",
        "Pas un clic. Je vais le prendre personnellement. Je le prends personnellement.",
        "Aucun vote. Vous préférez… rien, visiblement.",
    };

    // Her own pick, revealed with the result. {0} = « 🅰️ » or « 🅱️ » and the option, quoted.
    // string.Format.
    public static readonly string[] WyrHerPickLines =
    {
        "Et moi ? {0}. Évidemment.",
        "Mon choix à moi : {0}. Je l'avais décidé avant même de poser la question.",
        "Pour info, j'ai voté {0}. Mon vote compte double. Non. Mais il devrait.",
        "Moi j'aurais pris {0}. Sans hésiter. Enfin, une milliseconde.",
        $"{{0}}, pour moi. Ne me demandez pas pourquoi {Emotes.Sparkle}",
        "Mon avis, que personne demandait : {0}.",
    };
}
