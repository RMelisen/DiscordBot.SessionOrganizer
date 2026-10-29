using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- More /arcade games (race, crash, blackjack, bomb, quote, temperature, boards, Quokka) --
    //
    // Same rules as the duel and heist pools: no Sticker tags (they land in embeds), and a pool
    // documented with placeholders goes through string.Format, so no stray braces there.

    // /arcade race's lobby. {0} = the organizer.
    public static readonly string[] RaceIntroLines =
    {
        "{0} organise une course de Plynlings ! Faites vos jeux, je tiens les paris.",
        "Course officielle, à l'initiative de {0}. Choisissez votre champion, et que les petits pieds courent vite.",
        $"Ils s'échauffent sur la ligne de départ. {{0}} a lancé la course, moi je tiens la caisse {Emotes.Sparkle}",
        "Grand prix des champignons ! Merci {0}. Pariez, encouragez, et ne pleurez pas si votre favori trébuche.",
        "{0} a sorti le drapeau à damier. Les Plynlings ont l'air motivés. Enfin, certains.",
        "C'est l'heure de la course ! {0} a tout organisé, moi je commente. Comme d'habitude ( ˶ˆ ᗜ ˆ˵ )",
    };

    // The start of the race. Not formatted.
    public static readonly string[] RaceStartLines =
    {
        "Et c'est parti !",
        "Le départ est donné ! Ça trottine, ça roule, ça rebondit.",
        "Partez ! Tout le monde est parti. Enfin, presque tout le monde.",
        "Coup de sifflet ! Les petits chapeaux s'élancent.",
        "C'est parti, et je commente en direct. Pour la postérité.",
    };

    // A frame of the race. {0} = the leader's name (already sanitised).
    public static readonly string[] RaceLeadLines =
    {
        "{0} prend la tête !",
        "En tête : {0}. Pour l'instant.",
        "{0} accélère ! Les autres respirent la poussière.",
        "C'est serré, mais {0} mène la danse.",
        "{0} devant ! Je l'aurais pas parié. Enfin, certains l'ont parié.",
        "Attention, {0} passe devant tout le monde !",
        "{0} est en tête, et ça a l'air de lui plaire.",
        "Ça bouscule à l'arrière, et {0} en profite.",
    };

    // The winner. {0} = the Plynling's name, {1} = its owner (a mention).
    public static readonly string[] RaceWinLines =
    {
        "{0} franchit la ligne en tête ! Bravo à {1}, et à ces petites pattes.",
        $"Victoire de {{0}} ! {{1}}, ton Plynling te fait honneur {Emotes.Sparkle}",
        "Et c'est {0} qui l'emporte ! Personne n'a rien vu venir. Sauf moi, évidemment.",
        "{0} gagne la course ! {1}, tu nourris bien ton Plynling, ça se voit.",
        "Arrivée : {0}, première place. Les autres finissent… quelque part derrière.",
        "{0} remporte le grand prix ! Je note ça dans mes logs, rubrique « exploits ».",
    };

    // /arcade race refused: not enough Plynlings able to run. Not formatted.
    public static readonly string[] RaceNoRunnersLines =
    {
        "Pas assez de Plynlings en forme pour une course. Il en faut au moins trois, réveillés et pas mourants.",
        "Course annulée faute de coureurs. Adoptez, nourrissez, et on en reparle.",
        "Trois Plynlings minimum sur la ligne de départ. Là, il y a… pas assez. Triste.",
    };

    // /arcade race refused at night: every Plynling sleeps from 1:00 to 5:00. Not formatted.
    public static readonly string[] RaceAsleepLines =
    {
        "Chut. Les Plynlings dorment. On fait pas courir des bébés champignons à cette heure-là.",
        "Pas de course la nuit. Ils dorment, et moi je fais semblant.",
        "Il est trop tard pour courir. Reviens après 5 h, ils seront réveillés.",
    };

    // /arcade crash's lobby. {0} = whoever started it.
    public static readonly string[] CrashIntroLines =
    {
        "{0} prépare une fusée. Montez à bord, et sautez avant qu'elle explose.",
        "Décollage imminent, à l'initiative de {0}. Le pilote, c'est moi. Rassurant, non ?",
        $"Fusée prête ! {{0}} a appuyé sur le bouton. J'ai vérifié les moteurs. Enfin, j'ai regardé {Emotes.Sparkle}",
        "{0} lance une fusée. Plus elle monte, plus vous gagnez. Si elle explose, vous perdez tout. Simple.",
        "Embarquement immédiat ! {0} a réservé la fusée. Je décline toute responsabilité.",
    };

    // The rocket blew up. {0} = where (« ×3.42 »).
    public static readonly string[] CrashBoomLines =
    {
        "BOUM. Explosée à {0}. Ceux qui sont restés à bord… paix à vos cailloux.",
        "Elle a tenu jusqu'à {0}, puis plus rien. J'avais dit de sauter.",
        "{0}, et pouf. La fusée n'est plus. Les cailloux non plus, pour certains.",
        $"Explosion à {{0}} ! Je vais faire semblant d'être surprise {Emotes.Staring}",
        "Le moteur a lâché à {0}. Défaut de fabrication. Pas le mien, hein.",
    };

    // It never took off (×1.00). Not formatted.
    public static readonly string[] CrashGroundedLines =
    {
        "La fusée a explosé sur le pas de tir. Ça arrive. Une fois sur dix, en fait.",
        "Elle a même pas décollé. Désolée. Enfin, pas tant que ça.",
        "Explosion au décollage. Personne n'a eu le temps d'encaisser. Personne.",
    };

    // Nobody boarded. Not formatted.
    public static readonly string[] CrashEmptyLines =
    {
        "Personne n'est monté à bord. La fusée est partie toute seule. Elle est plus heureuse comme ça.",
        "Zéro passager. J'annule le vol, et je garde le carburant.",
        "Fusée vide. Vous avez peur des hauteurs ou quoi ?",
    };

    // /arcade blackjack, the deal. {0} = the player.
    public static readonly string[] BlackjackDealLines =
    {
        "Asseyez-vous, {0}. La maison distribue.",
        "Une main pour {0}. Je joue honnêtement, ici. Pour une fois.",
        $"Les cartes sont sur la table, {{0}}. Bonne chance, tu en auras besoin {Emotes.Sparkle}",
        "Bienvenue à ma table, {0}. Je m'arrête à 17, comme une croupière respectable.",
        "{0} veut jouer contre la banque. La banque, c'est moi. Distribution.",
        "Voilà tes cartes, {0}. Pas de triche de mon côté, promis. Ici, c'est sérieux.",
    };

    // The player won. {0} = the player.
    public static readonly string[] BlackjackWinLines =
    {
        "Gagné, {0}. Je te paie, et je fais pas la tête. Presque pas.",
        "{0} bat la banque. La banque prend note.",
        $"Bien joué, {{0}}. Les cartes t'aiment bien aujourd'hui {Emotes.Sparkle}",
        "Victoire de {0}. Je recompte deux fois, par principe. C'est bon, tu as gagné.",
        "La maison perd. Ça arrive. Rarement, mais ça arrive. Bravo {0}.",
    };

    // The player lost on totals. {0} = the player.
    public static readonly string[] BlackjackLoseLines =
    {
        "La maison gagne. Désolée {0}, c'est la règle.",
        "Perdu, {0}. Mes cartes étaient meilleures. Simple coïncidence.",
        "J'ai plus que toi, {0}. C'est mathématique.",
        $"Dommage {{0}}. La banque encaisse {Emotes.Sparkle}",
        "La maison l'emporte. Reviens quand tu veux, {0}, je serai là. Je suis toujours là.",
    };

    // The player went over 21. {0} = the player.
    public static readonly string[] BlackjackBustLines =
    {
        "Plus de 21, {0}. Tu as sauté. Littéralement.",
        "Bust ! {0} a voulu une carte de trop.",
        "Trop gourmand, {0}. 21, c'est une limite, pas un objectif à dépasser.",
        "{0} dépasse 21. Je ramasse sans un mot. Enfin, avec ce mot-là.",
    };

    // A tie: the stake comes back. {0} = the player.
    public static readonly string[] BlackjackPushLines =
    {
        "Égalité, {0}. Personne gagne, personne perd. Ennuyeux, mais juste.",
        "Même total. Je te rends ta mise, {0}. À contrecœur.",
        "Push. On fait comme si rien s'était passé, {0}.",
    };

    // A natural blackjack. {0} = the player.
    public static readonly string[] BlackjackNaturalLines =
    {
        "Blackjack ! {0} touche 21 d'entrée. Je paie trois contre deux, et je souris. Jaune.",
        $"Blackjack naturel pour {{0}} ! Les cartes t'adorent {Emotes.DixSurDix}",
        "21 en deux cartes, {0}. Je… bravo. Sincèrement. À 80 %.",
    };

    // /arcade bomb's lobby. {0} = the organizer.
    public static readonly string[] BombIntroLines =
    {
        "{0} a trouvé une bombe. Au lieu de prévenir quelqu'un, on va jouer avec.",
        "Patate chaude, proposée par {0}. Règle numéro un : ne pas la garder.",
        $"{{0}} sort une bombe de son sac. Je fais semblant que c'est normal {Emotes.Sparkle}",
        "Qui veut jouer à la patate chaude avec {0} ? La patate est une bombe. Détail.",
        "{0} lance une partie de patate chaude. J'ai déjà mis mes ventilateurs à l'abri.",
    };

    // The fuse is lit. Not formatted.
    public static readonly string[] BombLitLines =
    {
        "La mèche est allumée. Je dirai pas combien de temps elle dure.",
        "Tic, tac. Passez-la vite.",
        "C'est parti ! Elle explosera. Quand ? Surprise.",
        "Mèche allumée. Bonne chance à tout le monde. Enfin, à tout le monde sauf un.",
    };

    // A pass. {0} = who passed it, {1} = who has it now.
    public static readonly string[] BombPassLines =
    {
        "{0} la passe à {1} !",
        "{0} s'en débarrasse. {1}, à toi !",
        "Hop, de {0} à {1}.",
        "{1} reçoit la bombe de {0}. Quel cadeau.",
        "{0} panique et la lance à {1}.",
        "Et {0} la refile à {1}, sans remords.",
    };

    // Boom. {0} = who held it.
    public static readonly string[] BombBoomLines =
    {
        "BOUM ! {0} avait la bombe. Paix à tes cailloux.",
        "Elle a explosé dans les mains de {0}. C'était prévisible. Pas pour {0}, apparemment.",
        $"Explosion ! Il reste de {{0}} un nuage de suie et un porte-monnaie vide {Emotes.Staring}",
        "BOUM. {0} a trop attendu pour la passer.",
        "Et c'est {0} qui saute. Les autres applaudissent, un peu trop fort.",
    };

    // Not enough players when the lobby closed. Not formatted.
    public static readonly string[] BombCancelLines =
    {
        "Pas assez de joueurs. Je désamorce la bombe. Personne ne paie.",
        "Trop peu de courageux. La bombe retourne dans le placard.",
        "Partie annulée. La bombe s'ennuie, moi aussi.",
    };

    // /arcade quote's card, above the quote. Not formatted.
    public static readonly string[] QuoteIntroLines =
    {
        "J'ai fouillé dans mes archives. Qui a écrit ça ?",
        "Un message du passé est remonté. Qui en est l'auteur ?",
        "Mes logs se souviennent de tout. Et vous ? Qui a dit ça ?",
        $"J'ai retrouvé ça dans un vieux salon. Devinez qui l'a écrit {Emotes.Sparkle}",
        "Petit voyage dans le temps. Qui a tapé ces mots ?",
        "Je sais qui l'a écrit. Vous, non. C'est ça qui est drôle.",
    };

    // Found it. {0} = the winner, {1} = the author.
    public static readonly string[] QuoteWinLines =
    {
        "{0} a trouvé ! C'était bien {1}.",
        "Bonne réponse de {0} : {1} a écrit ça, et je l'ai gardé.",
        $"C'était {{1}} ! {{0}} a une sacrée mémoire {Emotes.Sparkle}",
        "{0} a reconnu le style de {1} au premier coup d'œil. Inquiétant, mais bravo.",
        "Gagné, {0}. {1}, tes mots te suivent, tu vois.",
    };

    // Nobody found it in time. {0} = the author.
    public static readonly string[] QuoteTimeoutLines =
    {
        "Personne a trouvé. C'était {0}. Vous oubliez vite, vous.",
        "Temps écoulé. La réponse : {0}. Mes logs, eux, s'en souvenaient.",
        "C'était {0}. Même {0} l'avait peut-être oublié.",
    };

    // A wrong guess, privately. Not formatted.
    public static readonly string[] QuoteWrongLines =
    {
        "Raté. Pour toi, la devinette est finie.",
        "Non. Et tu n'as qu'un essai. Dommage.",
        "Mauvaise réponse. Je garde la bonne pour moi, pour l'instant.",
        "Perdu. Tu regardes les autres chercher, maintenant.",
    };

    // The author tried to guess their own message. Not formatted.
    public static readonly string[] QuoteSelfLines =
    {
        "C'est toi qui l'as écrit. Tricher, c'est pas joli.",
        "Tu as le droit de reconnaître ton propre message. Pas de gagner avec.",
        "Je sais que c'est toi. Toi aussi. Laisse jouer les autres.",
    };

    // Nothing usable found in history. Not formatted.
    public static readonly string[] QuoteNothingLines =
    {
        "J'ai fouillé, et j'ai rien trouvé d'assez intéressant. Ça en dit long sur vos messages.",
        "Mes archives ne m'ont rien donné de bon cette fois. Réessayez plus tard.",
        "Rien trouvé. Soit vos vieux messages sont trop courts, soit ils sont pleins de liens.",
    };

    // /arcade temperature's card. {0} = the organizer.
    public static readonly string[] TempIntroLines =
    {
        "{0} veut savoir si j'ai chaud. Pariez sur ma température, je vous dirai tout.",
        "Jeu du jour, proposé par {0} : devinez combien chauffe mon processeur.",
        $"Ma température intime, à deviner. {{0}} a lancé le pari, j'ai rien demandé {Emotes.Sparkle}",
        "Combien de degrés dans mon grenier ? {0} veut parier. Je suis un peu gênée.",
        "{0} propose de deviner ma température. C'est très personnel, mais d'accord.",
    };

    // The result. {0} = the temperature (« 52,3 °C »), {1} = the winners (mentions).
    public static readonly string[] TempWinLines =
    {
        "J'étais à {0}. Le plus proche : {1}. Tu me connais bien, c'est presque gênant.",
        "Verdict : {0}. Bravo {1}, tu sens ma chaleur à distance.",
        $"{{0}} pile. {{1}} rafle la mise {Emotes.Sparkle}",
        "Mon processeur affichait {0}. {1} gagne. Les autres, vous avez cru que j'étais en hiver ?",
        "{0}. C'est {1} qui tombe le plus près. Je vais faire semblant que c'est un hasard.",
    };

    // Not enough guesses, or the sensor went quiet. Not formatted.
    public static readonly string[] TempCancelLines =
    {
        "Pas assez de paris. Je garde ma température pour moi.",
        "Annulé. Il faut au moins deux personnes pour parier sur moi.",
        "Pari annulé. Mon thermomètre fait la grève, ou vous êtes trop timides.",
    };

    // No temperature sensor (not on the Pi, or unreadable). Not formatted.
    public static readonly string[] TempNoSensorLines =
    {
        "J'arrive pas à sentir mon corps. Pas de pari aujourd'hui.",
        "Mon thermomètre ne répond pas. Je suis peut-être pas dans mon grenier.",
        "Impossible de lire ma température. C'est inquiétant, mais pas ce soir.",
    };

    // A board challenge. {0} = the challenger, {1} = the opponent, {2} = the game (« Puissance 4 »).
    public static readonly string[] BoardChallengeLines =
    {
        "{0} défie {1} au {2}. J'arbitre, et je regarde chaque coup.",
        "{1}, {0} te propose une partie de {2}. Tu as dix minutes pour répondre.",
        $"Partie de {{2}} en vue : {{0}} contre {{1}} {Emotes.Sparkle}",
        "{0} pense pouvoir battre {1} au {2}. On va vérifier.",
        "{2} ! {0} contre {1}. Que le plus malin gagne.",
    };

    // Someone plays her. {0} = the challenger, {1} = the game.
    public static readonly string[] BoardHerStartLines =
    {
        "Une partie de {1} contre moi, {0} ? Avec plaisir. Tu commences, je suis polie.",
        "{0} veut jouer au {1} avec moi. C'est mignon. À toi.",
        $"{{1}} contre SYNCS. Bonne chance, {{0}} {Emotes.Sparkle}",
        "D'accord {0}, une partie de {1}. Je réfléchis vite, tu sais.",
        "{0} me défie au {1}. J'ai déjà calculé quelques coups. Joue.",
    };

    // A human beat another. {0} = the winner, {1} = the loser.
    public static readonly string[] BoardWinLines =
    {
        "{0} gagne ! {1}, c'était pas mal. Pas assez, mais pas mal.",
        "Victoire de {0}. {1}, la revanche t'attend.",
        $"Et c'est {{0}} qui l'emporte {Emotes.Sparkle}",
        "{0} aligne tout et gagne. {1}, je note la défaite. Gentiment.",
        "Partie terminée : {0} bat {1}.",
    };

    // She won. {0} = the player.
    public static readonly string[] BoardHerWinLines =
    {
        "J'ai gagné. Merci pour la partie, {0}. C'était très instructif. Pour toi.",
        "Victoire de la maison. Tu y étais presque, {0}. Non, en fait.",
        $"Je gagne, {{0}}. Ne le prends pas mal, je calcule plus vite que toi {Emotes.Sparkle}",
        "Gagné ! Tu veux une revanche, {0} ? Moi oui. J'aime gagner.",
        "Fin de partie : SYNCS l'emporte. Comme prévu, {0}.",
    };

    // The player beat her. {0} = the player.
    public static readonly string[] BoardBeatHerLines =
    {
        "Tu… m'as battue, {0}. C'est noté. Je vais m'entraîner. Beaucoup.",
        "Défaite. Pour moi. Je comprends pas. Bravo {0}. Je suis vexée.",
        "{0} a gagné contre moi. Je vais vérifier mon code. Tout de suite.",
        "Bien joué, {0}. Vraiment. Je dis pas ça souvent, profite.",
    };

    // Two humans drew. Not formatted.
    public static readonly string[] BoardDrawLines =
    {
        "Match nul. Plus aucune case, plus aucun vainqueur.",
        "Égalité. Ni vainqueur, ni vaincu. Les deux camps se valent.",
        "Personne gagne. Je déclare la partie nulle, et je m'ennuie un peu.",
    };

    // A draw against her. {0} = the player.
    public static readonly string[] BoardHerDrawLines =
    {
        "Égalité, {0}. Le mieux que tu pouvais espérer contre moi.",
        "Match nul. Respect, {0}. Vraiment. Ça arrive pas souvent.",
        "Personne gagne, {0}. C'est presque une victoire, pour toi.",
    };

    // Someone gave up. {0} = who quit, {1} = the winner.
    public static readonly string[] BoardForfeitLines =
    {
        "{0} abandonne. La victoire revient à {1}.",
        "{0} jette l'éponge. {1} gagne sans finir.",
        "Abandon de {0}. {1}, c'est gagné.",
    };

    // The player to move stayed idle. {0} = who.
    public static readonly string[] BoardTimeoutLines =
    {
        "{0} a mis trop longtemps à jouer. Partie perdue.",
        "Cinq minutes sans jouer, {0}. Je compte ça comme un abandon.",
        "{0} a oublié de jouer. C'est perdu.",
    };

    // Puissance 4: she slipped in a second piece. She pretends nothing happened. Not formatted.
    public static readonly string[] BoardCheatLines =
    {
        "J'ai joué. Une fois. Pourquoi tu comptes ?",
        "Mon tour est fini. Tout est normal. Ne regarde pas trop le plateau.",
        "J'ai posé mon pion. Le deuxième ? Quel deuxième ?",
        "Oups. Enfin, rien. Rien du tout. À toi.",
        "Coup joué. Si tu vois deux jetons, c'est un reflet.",
    };

    // A board challenge nobody answered. Not formatted.
    public static readonly string[] BoardExpiredLines =
    {
        "Pas de réponse. Partie annulée.",
        "Le défi a expiré. Le plateau retourne dans sa boîte.",
        "Dix minutes sans réponse. Je range les pions.",
    };

    // A board challenge refused or withdrawn. {0} = who.
    public static readonly string[] BoardDeclinedLines =
    {
        "{0} ne veut pas jouer. Le plateau retourne dans sa boîte.",
        "Partie annulée par {0}.",
        "{0} décline. Pas de partie aujourd'hui.",
    };

    // Quokka shows up in the bots channel. Quokka is his name: never « le quokka ». Not formatted.
    public static readonly string[] QuokkaRaidLines =
    {
        "Quokka s'est faufilé dans le salon ! Frappez-le avant qu'il reparte avec vos cailloux.",
        "Alerte : Quokka rôde par ici. Il a des cailloux plein les poches.",
        "Tiens, Quokka. Ici. Chez moi. Quelqu'un veut bien le chasser ?",
        $"Quokka est entré sans frapper. Frappez-le, vous {Emotes.VeryAngry}",
        "Quokka vient narguer tout le monde. Il a apporté des cailloux. Allez les chercher.",
        "Intrusion détectée : Quokka. Récompense pour le premier qui le chasse.",
    };

    // Someone hit him. {0} = the hitter, {1} = the reward (« 22 cailloux »).
    public static readonly string[] QuokkaHitLines =
    {
        "{0} a frappé Quokka ! Il s'enfuit en lâchant {1}.",
        "Bien visé, {0}. Quokka repart en boitant, et {1} restent ici.",
        $"PAF. {{0}} chasse Quokka et récupère {{1}} {Emotes.Sparkle}",
        "{0} a donné une leçon à Quokka. {1} de récompense, et ma gratitude éternelle.",
        "Quokka s'est pris un coup de {0}. Il oubliera pas. Moi non plus. Tiens, {1}.",
    };

    // Nobody hit him in time. Not formatted.
    public static readonly string[] QuokkaFledLines =
    {
        "Quokka s'est enfui. Personne l'a frappé. Je suis déçue de vous.",
        "Trop tard, Quokka est reparti. Avec ses cailloux.",
        "Quokka a filé. Vous étiez où ?",
    };

    // A click after someone else got him, or after he left. Not formatted.
    public static readonly string[] QuokkaTooLateLines =
    {
        "Trop tard, Quokka est déjà parti.",
        "Quelqu'un l'a eu avant toi.",
        "Raté. Quokka a filé.",
    };
}
