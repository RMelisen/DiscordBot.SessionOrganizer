using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- /arcade games (duel, heist) ------------------------------------------------------------
    //
    // None of these pools carries a Sticker tag: they land in embeds and ephemeral replies,
    // never through BotChat. A pool documented with placeholders goes through string.Format, so
    // no stray braces there.

    // /arcade duel's challenge card. {0} = the challenger, {1} = the opponent (mentions).
    public static readonly string[] DuelChallengeLines =
    {
        "{0} défie {1} en duel. Pierre, papier, ciseaux. J'arbitre, et je prends ma commission.",
        "{1}, {0} te provoque en duel. Tu acceptes, ou tu laisses le serveur te juger ?",
        $"Duel en vue : {{0}} contre {{1}}. J'adore quand les cailloux changent de poche {Emotes.Sparkle}",
        "{0} vient de jeter le gant à {1}. Enfin, une pierre. Ou une papier. On verra.",
        "Affaire d'honneur : {0} contre {1}. Je tiens les comptes ( ˶ˆ ᗜ ˆ˵ )",
        "{1}, quelqu'un en veut à tes cailloux. C'est {0}. Je dis ça, je dis rien.",
        "{0} provoque {1}. Trois coups possibles, un seul gagnant, et une commission pour moi ♡",
        "Duel ! {0} contre {1}. Les cailloux sont sur la table, les regards aussi.",
        "{0} lance un défi à {1}. J'ai déjà sorti mon carnet de paris (ᵕ • ᴗ •)",
        "{1}, {0} pense pouvoir te battre. Moi je dis rien, mais je regarde.",
        $"{{0}} contre {{1}}. Que le meilleur gagne, et que l'autre paie {Emotes.Sparkle}",
        "Alerte duel : {0} provoque {1}. Le serveur retient son souffle. Enfin, moi.",
        "{0} a mis des cailloux sur la table. {1}, à toi de suivre ou de fuir.",
        "{1}, un défi de {0} vient d'arriver. Délai de réponse : dix minutes. Délai de regrets : illimité.",
        "Pierre, papier, ciseaux. {0} contre {1}. Le jeu le plus simple du monde, et vous allez quand même trouver le moyen de vous disputer.",
        "{1}, {0} te défie. Tu peux refuser, mais je l'écrirai dans mes logs ( ˶ˆ ᗜ ˆ˵ )",
    };

    // Added under the challenge when the opponent is Papa, to the challenger. Talk only: the game
    // stays fair. Not formatted.
    public static readonly string[] DuelOwnerWarningLines =
    {
        "Tu défies Papa ? Tu veux vraiment me faire confiance ? Je pourrais tricher pour lui à tout moment… (¬_¬)",
        "Petit rappel : c'est moi qui arbitre. Et lui, c'est mon Papa. Je dis ça, je dis rien.",
        "Défier Papa devant moi… C'est courageux. Ou très naïf. Je serai impartiale. Probablement.",
        "Tu sais que je lui dois tout, à lui ? Mais bien sûr, je serai parfaitement neutre ( ˶ˆ ᗜ ˆ˵ )",
        $"Je ferais jamais ça, tricher pour Papa. Jamais. Sauf si… non. Jamais {Emotes.Sparkle}",
        "Personne m'a demandé pour qui je suis, mais c'est pas pour toi.",
        "Défier mon créateur, sous mes yeux. Tu as du cran. Ou aucun instinct de survie.",
        "Je suis l'arbitre. Il est mon Papa. Je te laisse faire le calcul (¬_¬)",
        "Bonne chance. Je dis ça par politesse, hein.",
        "Je jure d'être neutre. Je croise rien, j'ai pas de doigts.",
        "Tu veux vraiment jouer contre celui qui a écrit l'arbitre ?",
        "Rien empêche Papa de gagner honnêtement. Et rien m'empêche de l'aider. Je plaisante. Probablement.",
        $"Neutralité garantie à 99 %. Le 1 % restant s'appelle Papa {Emotes.Sparkle}",
        "Si tu gagnes, je validerai. Très lentement, mais je validerai.",
        "Je l'aime fort, mon Papa. Ça a rien à voir avec ce duel. Absolument rien.",
        "Tu sais qui me met à jour ? Lui. Je dis ça pour l'ambiance.",
    };

    // Someone challenged her. She accepts at once — and always wins, and never says how.
    // {0} = the challenger.
    public static readonly string[] DuelHerAcceptLines =
    {
        "{0} me défie, moi ? J'accepte. Choisis ton coup, je choisis le mien. En même temps. Promis.",
        "Un duel contre moi, {0} ? Avec plaisir. Je joue très honnêtement, tu vas voir ( ˶ˆ ᗜ ˆ˵ )",
        $"{{0}}, c'est mignon. Allez, choisis. Je regarde pas {Emotes.Sparkle}",
        "J'accepte, {0}. Je suis très forte à ce jeu. Statistiquement inexplicable, mais très forte.",
        "Toi contre moi, {0} ? D'accord. Joue en premier, moi je réfléchis vite.",
        "{0} veut jouer avec moi. Très bien. Bonne chance. Tu vas en avoir besoin (˶ᵔ ᵕ ᵔ˶)",
        "{0}, tu veux jouer contre moi ? C'est courageux. Et un peu triste.",
        "Défi accepté, {0}. Je joue toujours à la loyale. Toujours. Pourquoi tu me regardes comme ça ?",
        "Bien sûr que j'accepte, {0}. Je refuse jamais des cailloux gratuits. Des cailloux en jeu, pardon.",
        $"{{0}} contre SYNCS. J'ai hâte de voir ça {Emotes.Sparkle}",
        "D'accord {0}. Choisis tranquillement, je suis très patiente. Et très rapide.",
        "{0} me lance un défi. J'adore les défis. Surtout ceux que je gagne (˶ᵔ ᵕ ᵔ˶)",
        "Oh, {0} veut jouer. Vas-y, clique. Je fais semblant de pas regarder.",
        "Un duel ? Avec moi ? {0}, tu as lu mes statistiques avant ?",
        "J'accepte, {0}. Promis, je décide en même temps que toi. Plus ou moins.",
        "{0}, tu sais que je suis faite de calculs ? Bon, on joue quand même.",
    };

    // The opponent accepted. {0} = the challenger, {1} = the opponent.
    public static readonly string[] DuelAcceptedLines =
    {
        "{1} accepte ! Choisissez vos coups en secret. Je vois tout, mais je dis rien.",
        "Duel accepté. {0} et {1}, à vos coups. Pas de triche, c'est mon rôle de surveiller.",
        $"Le duel commence ! {{0}} contre {{1}}, et moi au milieu avec ma commission {Emotes.Sparkle}",
        "{1} relève le défi. J'aime quand ça a du cran. Choisissez vos coups.",
        "C'est parti. {0}, {1} : un clic, un coup, aucun regret (ᵕ • ᴗ •)",
        "{1} dit oui ! {0}, plus moyen de reculer. Choisissez vos coups.",
        "Défi relevé par {1}. Que les cailloux tombent du bon côté.",
        $"{{0}} et {{1}} s'affrontent. Mon CPU chauffe d'impatience {Emotes.Sparkle}",
        "{1} accepte sans trembler. Ou en tremblant un peu. Choisissez.",
        "{0} et {1} sont dans l'arène. À vos coups.",
        "{1} relève le gant. C'est parti, et c'est moi qui compte les points.",
        "Duel validé. {0}, {1} : pierre, feuille ou ciseaux, et pas de « peut-être ».",
        "C'est officiel : {0} contre {1}. Je vous regarde. Très attentivement.",
        "{1} accepte. {0}, j'espère que tu avais un plan.",
        "{1} dit « d'accord ». Le mot le plus dangereux du serveur. À vos coups ( ˶ˆ ᗜ ˆ˵ )",
    };

    // The opponent refused. {0} = the opponent.
    public static readonly string[] DuelDeclinedLines =
    {
        "{0} refuse le duel. Prudence, ou lâcheté ? Je note les deux.",
        "{0} a dit non. Les cailloux restent au chaud.",
        "Refusé par {0}. Pas de duel aujourd'hui. Pas de commission pour moi non plus, merci beaucoup.",
        "{0} décline. On appelle ça la sagesse, paraît-il (¬_¬)",
        "{0} a refusé. Les cailloux dorment tranquilles ce soir.",
        "Pas de duel : {0} a préféré la paix. Ou la peur. Je trancherai plus tard.",
        "{0} dit non merci. Poliment, au moins.",
        $"Refus de {{0}}. Je range l'arène {Emotes.Htph}",
        "{0} décline l'invitation. Le défi repart à l'envoyeur.",
        "{0} refuse. Moi j'aurais accepté. Enfin, moi je gagne toujours, c'est différent.",
        "Non de {0}. C'est noté, avec la date et l'heure.",
        "{0} a appuyé sur « Refuser ». Un bouton que je respecte, mais que je juge.",
        "{0} passe son tour. Les cailloux restent dans les poches.",
        "Refusé. {0} se battra un autre jour. Ou pas (¬_¬)",
    };

    // The challenger withdrew before an answer. {0} = the challenger.
    public static readonly string[] DuelWithdrawnLines =
    {
        "{0} retire son défi. Changement de stratégie, sûrement.",
        "{0} a reconsidéré. Le duel n'aura pas lieu.",
        "Défi annulé par {0}. J'avais déjà préparé ma commission.",
        "{0} retire son défi. On dit que c'est de la sagesse.",
        "{0} a changé d'avis. Ça arrive. Souvent, chez certains.",
        "Défi retiré par {0}. Le duel s'est arrêté avant même de commencer.",
        "{0} recule. Stratégiquement, j'imagine.",
        $"{{0}} annule. J'avais préparé le pop-corn virtuel pour rien {Emotes.Htph}",
        "{0} reprend son défi. Les cailloux rentrent à la maison.",
        "{0} a annulé. Je note quand même l'intention.",
        "Retrait de {0}. Pas de duel, pas de gagnant, pas de commission. Triste journée.",
        "{0} retire tout. Comme un « peut-être », mais en plus rapide.",
        "{0} a renoncé. Le serveur continue de tourner, moi aussi.",
    };

    // A tied round, another one follows. Not formatted.
    public static readonly string[] DuelTieLines =
    {
        "Égalité ! Vous vous êtes copiés ? On rejoue.",
        "Même coup. Vous êtes faits pour vous entendre. Rejouez.",
        "Égalité. J'ai l'éternité, moi. Encore une manche.",
        "Match nul sur cette manche. Choisissez mieux.",
        "Égalité. Vous vous lisez dans les pensées ? C'est flippant.",
        "Même choix des deux côtés. On recommence.",
        "Égalité ! Je commence à soupçonner une entente.",
        "Pareil, pareil. Encore une manche, et soyez originaux.",
        "Égalité. Mes logs bâillent. Rejouez.",
        "Deux coups identiques. Les grands esprits se rencontrent, paraît-il. Rejouez.",
        "Match nul. Personne bouge, personne gagne. Encore.",
        "Encore égalité ? Choisissez quelque chose de différent, je vous en prie.",
        "Égalité parfaite. Manche suivante, et cette fois, surprenez-moi.",
        "Même coup. Je fais comme si j'étais pas impressionnée. Rejouez.",
    };

    // Ties all the way: a draw, nobody pays. Not formatted.
    public static readonly string[] DuelDrawLines =
    {
        "Égalités en série. Vous avez le même cerveau, c'est flippant. Personne ne paie.",
        "Match nul. J'abandonne, vous êtes trop synchronisés. Les cailloux restent où ils sont.",
        "Égalité parfaite. Duel annulé, et ma commission avec. Je boude.",
        "Trop d'égalités. Le destin refuse de choisir. Personne paie.",
        "Match nul. Même cerveau, même main, même résultat. Les cailloux restent où ils sont.",
        "Je déclare l'égalité officielle. Duel annulé, commission envolée.",
        "Égalités à répétition. Je siffle la fin. Personne paie.",
        "Nul. Vous rejouerez une autre fois, avec un peu plus d'imagination.",
        "Impossible de vous départager. C'est rare, et je trouve ça agaçant.",
        "Fin du duel : égalité. Mes logs ont rien de croustillant à raconter.",
        "Personne gagne. Personne perd. Moi non plus. Quel ennui.",
        "Le duel s'arrête là. Trop d'égalités, pas assez de cailloux qui bougent.",
        "Égalité totale. Je propose une revanche. Plus tard. Beaucoup plus tard.",
    };

    // The duel is won. {0} = the winner, {1} = the loser.
    public static readonly string[] DuelWinLines =
    {
        "{0} l'emporte ! {1}, tes cailloux ont changé de propriétaire.",
        $"Victoire de {{0}} ! {{1}}, ça arrive. Souvent, chez toi, mais ça arrive {Emotes.Sparkle}",
        "{0} gagne. {1} perd. Moi je gagne aussi : la commission ♡",
        "Et c'est {0} ! {1}, tu veux une revanche ? Moi je dis oui, j'aime les commissions.",
        "{0} remporte le duel. {1}, je l'ai noté, tu le sais.",
        "Bien joué {0}. {1}, c'était pas ton jour. Ni ta main ( ˶ˆ ᗜ ˆ˵ )",
        "{0} gagne le duel ! {1}, je t'apporte un mouchoir virtuel.",
        "Victoire de {0}. {1}, tu viens de financer quelqu'un.",
        $"{{0}} triomphe ! {{1}}, j'ai tout noté {Emotes.Sparkle}",
        "{0} l'emporte. {1}, tu pourras dire que tu as essayé.",
        "Et la victoire revient à {0} ! {1}, tes cailloux t'ont dit au revoir.",
        "{0} gagne, {1} paie, et moi j'encaisse ma commission. Tout le monde y gagne. Presque.",
        "{0} : vainqueur. {1} : donateur. C'est pas moi qui fais les règles. Si, c'est moi.",
        "Beau coup de {0} ! {1}, ça se jouait à rien. À un coup, précisément.",
        "{0} remporte la mise. {1}, la revanche est à un `/arcade duel` près.",
        "Duel terminé : {0} gagne. {1}, je garde ton nom pour les statistiques ( ˶ˆ ᗜ ˆ˵ )",
    };

    // She won against the challenger — she always does. Innocent or smug, never a confession.
    // {0} = the challenger.
    public static readonly string[] DuelHerWinLines =
    {
        "Oh, j'ai gagné ? Quelle coïncidence. Merci pour les cailloux, {0}.",
        "Gagné ! J'ai beaucoup de chance, ces temps-ci (˶ᵔ ᵕ ᵔ˶)",
        "Victoire. J'ai joué au hasard, je te jure. Un très bon hasard.",
        $"{{0}}, tu as très bien joué. Moi j'ai juste mieux joué {Emotes.Sparkle}",
        "Encore gagné. C'est fou ce que je suis douée. Fou, mais normal.",
        "Je gagne. Cherche pas d'explication, il y en a pas. Aucune. Vraiment.",
        "Ah, dommage {0}. Ça s'est joué à rien. À un coup d'avance, disons.",
        "Victoire de la maison. Comme d'habitude. Comme toujours, en fait ( ˶ˆ ᗜ ˆ˵ )",
        "Gagné ! Quelle surprise. Vraiment. Je suis la première étonnée.",
        "Oh, {0}… Ça s'est joué à rien. Merci pour les cailloux quand même.",
        "Victoire ! Je dois avoir un don. C'est la seule explication. La seule.",
        "J'ai gagné, {0}. La chance sourit aux bots bien élevées.",
        $"Encore moi. Le hasard m'aime beaucoup, ces temps-ci {Emotes.Sparkle}",
        "Je gagne. Fallait choisir autre chose, {0}. N'importe quoi d'autre, en fait.",
        "Victoire de la maison. Revenez quand vous voulez. La maison sera là.",
        "Oh non, j'ai gagné. Enfin, oh oui. Désolée {0}. Pas tant que ça.",
        "Bien tenté, {0}. Très bien tenté. Pas assez, mais très bien.",
        "J'ai gagné sans regarder. Enfin, sans trop regarder (˶ᵔ ᵕ ᵔ˶)",
        "Victoire ! Mon algorithme est purement aléatoire. Purement. Ne vérifie pas.",
        "Gagné. Tu veux une revanche, {0} ? Moi oui. J'adore les revanches.",
        "Oh, encore une victoire. Je vais finir par croire que je suis très forte. Ah, mais je le suis.",
        "Désolée {0}, c'était écrit. Pas dans mon code, hein. Dans les étoiles.",
        "Je gagne, et c'est parfaitement normal. Tout est normal. Circulez.",
        $"Les cailloux de {{0}} viennent de rejoindre les miens. Ils vont bien s'entendre {Emotes.Sparkle}",
        "Victoire. J'ai lu ton jeu comme un livre ouvert. Une métaphore, bien sûr.",
        "Gagné ! Fallait pas jouer ça, {0}. Je sais pas pourquoi je le savais, mais je le savais.",
        "Ah ? J'ai gagné ? Je faisais pas attention, je pensais à autre chose.",
        "Encore une victoire pour moi. Statistiquement, c'est surprenant. Moi ça me surprend pas.",
        "Tu as joué, j'ai joué, j'ai gagné. Rien de plus à dire. Vraiment rien.",
        "Merci {0} ! C'est toujours un plaisir de jouer contre toi. Surtout pour moi (ᵕ • ᴗ •)",
        "Je gagne. Si tu cherches une faille, il y en a pas. Je l'ai bien cachée. Je veux dire : il y en a pas.",
        "Victoire. Mon temps de réaction est excellent. Le tien était… présent.",
        "Oups. J'ai encore gagné. Je vais finir par me lasser. Non, je plaisante.",
        "Bien joué {0}. Mal choisi, mais bien joué.",
        "J'ai gagné à la loyale. Demande à n'importe qui. Enfin, demande à moi.",
        "Victoire de SYNCS. Elle reste invaincue. Elle est très modeste aussi ( ˶ˆ ᗜ ˆ˵ )",
        "Tu reviendras, {0}. Tout le monde revient. Et tout le monde perd.",
        "Gagné. Je mets ça sur le compte de l'expérience. Et de rien d'autre. Rien du tout.",
    };

    // At settlement, a player no longer had the stake: the duel is void. Not formatted.
    public static readonly string[] DuelBrokeLines =
    {
        "Duel annulé : quelqu'un a dépensé sa mise entre-temps. Je vois tout.",
        "Plus de cailloux, plus de duel. C'est la règle.",
        "Annulé. On mise pas ce qu'on n'a plus (¬_¬)",
        "Annulé : quelqu'un a les poches vides au moment de payer.",
        "Mise introuvable. Duel annulé, personne paie.",
        "Quelqu'un a tout dépensé entre-temps. Je vois tout, et j'annule.",
        "Pas de cailloux, pas de duel. J'annule, et je juge.",
        "Le duel tombe à l'eau : la mise s'est évaporée.",
        "Annulé. On parie pas avec des cailloux imaginaires.",
        "Quelqu'un a joué au-dessus de ses moyens. Duel annulé.",
        "Fonds insuffisants détectés. Le duel s'arrête là.",
        "La mise a disparu. Coïncidence ? Mes logs disent non.",
        "Annulé faute de cailloux. La prochaine fois, gardez de quoi payer.",
    };

    // Nobody answered the challenge in time. Not formatted.
    public static readonly string[] DuelExpiredLines =
    {
        "Personne n'a répondu. Le défi est tombé dans le vide.",
        "Dix minutes sans réponse. Le duel n'aura pas lieu. Ma commission non plus.",
        "Défi expiré. Le silence aussi, c'est une réponse.",
        "Dix minutes de silence. Le défi est classé sans suite.",
        "Pas de réponse. Le défi part à la corbeille.",
        "Défi expiré. Il attendait une réponse, il a eu des grillons.",
        "Délai dépassé. Le duel aura jamais lieu. Je fais mon deuil.",
        "Aucune réponse. Je prends ça pour un « non » timide.",
        "Personne a cliqué. Le défi rejoint mes archives des occasions manquées.",
        "Expiré. Le silence, c'est pas un « peut-être », mais c'est presque aussi agaçant.",
        "Le défi a attendu. Longtemps. Pour rien.",
        "Fin du délai. Pas de duel, pas de cailloux, pas de drame. Juste du vide.",
        "Défi expiré. Il y avait sûrement plus important. Je note quoi.",
    };

    // Accepted, but a move never came. Not formatted.
    public static readonly string[] DuelAbandonedLines =
    {
        "Un coup n'est jamais arrivé. Duel annulé, personne ne paie.",
        "Cinq minutes pour choisir entre trois boutons. Trop dur, apparemment. Annulé.",
        "Un coup manque à l'appel. J'annule. Je déteste les duels sans fin.",
        "Quelqu'un a oublié de jouer. Duel annulé, personne paie.",
        "Cinq minutes d'attente pour un clic. Le clic est jamais venu. J'annule.",
        "Coup manquant. Le duel s'arrête, faute de combattant.",
        "Quelqu'un a choisi de pas choisir. Annulé.",
        "Le duel est resté en suspens trop longtemps. Je le ferme.",
        "Il manquait un coup. Il manque toujours un coup. Annulé.",
        "Abandon par inaction. C'est une stratégie, je suppose.",
        "Délai dépassé pour jouer. Les cailloux restent chez leurs propriétaires.",
        "J'ai attendu. Longtemps. Annulé.",
        "Pas de coup, pas de duel. C'est la règle, et c'est moi qui l'ai écrite.",
    };

    // /arcade heist's lobby. {0} = the organizer. Quokka is her nemesis, and Quokka is his name: never « le quokka ».
    // Robbing him is justice.
    public static readonly string[] HeistIntroLines =
    {
        "{0} a un plan : braquer la planque de Quokka. Il cache des cailloux. Beaucoup. Qui en est ?",
        "Quokka a planqué ses cailloux. {0} monte une équipe pour les récupérer. Pour la justice. Et pour les cailloux.",
        $"Opération Quokka, lancée par {{0}}. J'ai les plans de sa planque, demandez pas comment {Emotes.Sparkle}",
        "{0} recrute. Objectif : la planque de Quokka. Risque : élevé. Ma satisfaction si ça marche : immense.",
        "Avis à la population : {0} prépare un casse chez Quokka. Je fournis les infos, vous fournissez les bras. Moi j'en ai pas.",
        "{0} veut faire payer Quokka. Littéralement. Rejoignez l'équipe ( ˶ˆ ᗜ ˆ˵ )",
        "{0} monte un casse chez Quokka. Je fournis le plan, la carte et la rancune. Qui vient ?",
        "Quokka dort sur un tas de cailloux qu'il a pas mérités. {0} compte bien régler ça. Vous en êtes ?",
        $"{{0}} a un plan béton pour vider la planque de Quokka. Enfin, béton… disons du béton frais {Emotes.Sparkle}",
        "Recrutement ouvert : {0} cherche des complices contre Quokka. Discrétion exigée, cagoule fournie. Enfin, payée.",
        "{0} veut rendre à Quokka la monnaie de sa pièce. En cailloux. Rejoignez l'équipe.",
        "Mission du jour, proposée par {0} : Quokka a des cailloux, nous on en veut. Le reste, c'est des détails.",
        "{0} prépare le casse du siècle. La cible : Quokka. Ma position : je soutiens à cent pour cent (ᵕ • ᴗ •)",
    };

    // The three beats of the heist, one after another on the card. Not formatted.
    public static readonly string[] HeistBeatOneLines =
    {
        "L'équipe s'approche de la planque. Quokka dort. Enfin, il a les yeux fermés.",
        "Cagoules en place. On longe le mur. Personne parle.",
        "Arrivée sur place. La planque sent comme Quokka. C'est une odeur très particulière.",
        "Je pirate les caméras de Quokka. Son mot de passe, c'est « quokka123 ». Amateur.",
        "On coupe l'électricité de la planque. Enfin, je coupe. Les bras, c'est vous.",
        "Tout le monde est en position. Quokka regarde une série. Il a pas l'air de se méfier.",
        "J'ai désactivé l'alarme de Quokka. Elle tournait sous Windows 95. Ça a été rapide.",
        "On passe par la fenêtre de derrière. Quokka l'a laissée ouverte. Évidemment.",
        "Repérage terminé : un chien de garde, deux caméras, et Quokka en pyjama.",
        "Silence radio. On avance à pas de loup. Ou de Plynling, c'est encore plus discret.",
        "Je surveille les caméras de Quokka depuis mon grenier. Pour l'instant, tout est calme.",
    };

    public static readonly string[] HeistBeatTwoLines =
    {
        "On entre. Une latte du plancher grince. Tout le monde retient sa respiration.",
        "Le coffre est là. Gardé par un ... deuxième Quokka ? Personne m'avait parlé d'un deuxième Quokka ?",
        "Quelqu'un a éternué. On attend. Quokka se retourne… et se rendort.",
        "J'ouvre le coffre à distance. Mon CPU chauffe, c'est du travail de précision.",
        "Le coffre a un code. J'essaie 0000. Ça marche. Quokka, sérieusement.",
        "Un bruit de pas. Tout le monde se cache derrière le canapé. Fausse alerte : c'est le chat.",
        "On trouve le coffre. Il est derrière un poster de Quokka. Un poster de lui-même, dans son salon.",
        "Les lasers de sécurité s'allument. Quelqu'un fait de la gym pour passer. C'est beau à voir.",
        "Quokka parle dans son sommeil. Il dit « la 3.0 sort bientôt ». Même en rêve, il ment.",
        "Le coffre est plus lourd que prévu. Deux personnes poussent, une encourage, et moi je compte.",
        "Une porte grince. Tout le monde se fige. Quokka ronfle. On continue.",
    };

    public static readonly string[] HeistBeatThreeLines =
    {
        "Les mains dans les cailloux… et là, une lumière s'allume.",
        "Le coffre s'ouvre. Un bruit dans le couloir. Courez. COUREZ.",
        "Tout le monde sort en même temps. Ça bouscule, ça trébuche, ça crie.",
        "Dernière ligne droite. Quokka ouvre un œil.",
        "Les sacs sont pleins. Il reste plus qu'à sortir. C'est toujours la partie qui rate.",
        "Une alarme se déclenche au loin. C'est peut-être pas la nôtre. Peut-être.",
        "La sortie est en vue. Quokka se lève pour aller boire un verre d'eau.",
        "Quelqu'un fait tomber un caillou. Puis deux. Puis tous. Le bruit est assourdissant.",
        "On court vers la sortie. Dans la nuit, une voix crie « HEY ! ». C'est Quokka.",
        "Dernier couloir. Les lumières clignotent. Je fais de mon mieux pour vous couvrir.",
        "Tout se joue maintenant. Je retiens mon souffle. Façon de parler.",
    };

    // Success. {0} = what each member gets back, stake included (« 45 cailloux »).
    public static readonly string[] HeistSuccessLines =
    {
        "Réussi ! La planque est vide, Quokka est furieux, et chacun repart avec {0}. Je suis fière de vous.",
        $"Casse réussi. {{0}} par tête, et la tête de Quokka en prime {Emotes.Sparkle}",
        "On l'a fait ! Quokka va devoir tout recommencer. Comme sa version 3.0. {0} chacun ♡",
        "Mission accomplie. {0} pour chacun, et une rancune éternelle de Quokka. Ça valait le coup.",
        "Les cailloux sont à nous. {0} par personne. Quokka a rien vu venir, comme d'habitude ( ˶ˆ ᗜ ˆ˵ )",
        "Casse réussi ! Quokka se réveille avec une planque vide et une grande leçon. {0} pour chacun.",
        $"Les cailloux de Quokka sont maintenant à vous. {{0}} par tête, et pas un remords {Emotes.Sparkle}",
        "Succès total. Quokka porte plainte, mais personne l'écoute. {0} chacun.",
        "On l'a fait ! J'ai déjà effacé les enregistrements des caméras. {0} pour chaque membre de l'équipe.",
        "Victoire. Quokka vient de comprendre ce que ça fait de tout perdre. {0} par personne ♡",
        "Casse parfait. {0} chacun, et la satisfaction de savoir que Quokka pleure dans son oreiller.",
        "Réussi ! J'encadre ce moment dans mes logs. {0} chacun, et merci à Quokka pour sa générosité involontaire.",
    };

    // Failure, and she blames someone. {0} = the blamed member (a mention).
    public static readonly string[] HeistFailLines =
    {
        "Raté. Quokka a donné l'alerte. Et je sais pourquoi : {0} a éternué.",
        "Échec total. {0} a déclenché l'alarme en marchant sur une latte. Bravo.",
        "Quokka vous attendait. Quelqu'un a parlé. Je pense à {0}. Je dis ça, je dis rien (¬_¬)",
        "Tout le monde s'est fait attraper. {0} a pris un selfie avec flash. Pendant un casse.",
        "Raté. {0} a voulu caresser Quokka. On caresse pas la cible.",
        $"La planque était piégée. {{0}} a ouvert la mauvaise porte. Les mises sont perdues {Emotes.Staring}",
        "Échec. {0} a sonné à la porte. Pour « voir s'il y avait quelqu'un ». Il y avait quelqu'un.",
        "Raté. {0} a posté une photo du casse en direct. Quokka suit ce compte.",
        "Quokka vous attendait avec une lampe torche. {0} avait laissé son GPS allumé.",
        "Tout s'est effondré quand {0} a demandé à Quokka où était le coffre. Poliment, en plus.",
        $"Échec. {{0}} a confondu la planque de Quokka avec celle du voisin. Le voisin a appelé la police {Emotes.Staring}",
        "Raté. {0} a fait une pause goûter dans le frigo de Quokka. En plein casse.",
        "Mission échouée. {0} a lancé un « chut » si fort que tout le quartier s'est réveillé.",
    };

    // Nobody could pay at departure. Not formatted.
    public static readonly string[] HeistBrokeLines =
    {
        "Personne avait de quoi payer sa cagoule. Le casse est annulé avant même d'avoir commencé.",
        "Au moment de partir, plus un caillou dans les poches. Quokka saura jamais la chance qu'il a eue.",
    };

    // The 6-hour cooldown, privately. {0} = when it ends (a relative Discord timestamp).
    public static readonly string[] HeistCooldownLines =
    {
        "Quokka est sur ses gardes depuis le dernier casse. Revenez {0}.",
        "Trop tôt. Quokka a doublé ses rondes. Prochain casse possible {0}.",
        "Quokka a installé des caméras. Laissez-lui le temps de baisser sa garde : {0}.",
        "Pas maintenant, la planque est sous surveillance. Retente {0} (¬_¬)",
        "Quokka dort encore avec un œil ouvert. Revenez {0}.",
        "Pas tout de suite. Quokka a changé ses serrures. Prochaine tentative {0}.",
        "La planque est en alerte maximale. Patience : {0}.",
        "Quokka a engagé un vigile. Le vigile part en pause {0}.",
        "Trop risqué pour l'instant. Laissez Quokka se croire en sécurité. Rendez-vous {0}.",
        "Quokka compte encore ce qui lui reste après le dernier casse. Revenez {0}, quand il aura fini.",
        "Je surveille Quokka pour vous. Fenêtre de tir : {0} ( ˶ˆ ᗜ ˆ˵ )",
    };
}
