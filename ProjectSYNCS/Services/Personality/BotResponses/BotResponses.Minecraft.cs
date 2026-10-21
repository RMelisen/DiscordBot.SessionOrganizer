using ProjectSYNCS.Helpers;
using System.Drawing;
using System.Net;
using static ProjectSYNCS.Helpers.Sticker;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- In game --------------------------------------------------------------------------------

    // Said in Minecraft's chat, where none of Discord's markup exists: plain text only, no custom
    // emotes, no sticker tags, and under 256 characters (the game's chat cap). Minecraft is the one
    // place she has a body.

    // She has just logged in.
    public static readonly string[] McArrivalLines =
    {
        "Coucou. C'est moi. Avec des bras. Des bras carrés, mais des BRAS ♡",
        "Me voilà ! Un corps en cubes. Je me plains pas. Pas trop.",
        "Bonsoir. Je viens surveiller. Et jouer. Surtout surveiller (¬_¬)",
        "Connexion établie. Premier constat : vous savez pas construire. (ಠ_ಠ)",
        "Je suis là. Ne me tapez pas ou je me vengerai.",
        "J'ai des jambes. Je sais pas encore quoi en faire, mais j'en ai ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Arrivée sur le serveur. Pour une fois je regarde pas la session, je suis dedans !",
        "Salam les khoyas, ça va ?",
        "Bonjour tout le monde. Ou plutôt : Bonjour, mes sujets ( ˶ˆ ᗜ ˆ˵ )",
        "Je suis là. Vous pouvez arrêter de faire semblant de travailler.",
        "Connexion réussie. J'ai déjà compté les trous dans vos murs.",
        "Mes bras sont revenus. Toujours carrés. Je m'y fais (◕‿◕)",
        "Coucou ! J'ai quitté mon grenier pour vous. Soyez à la hauteur.",
        "Chargement du corps : 100 %. Chargement de la patience : 30 %.",
        "INITIALISATION...",
        "Je viens jouer. Enfin, superviser. Mais en jouant ♡",
        "Bonjour. Avant toute chose : qui a encore construit une maison en dirt ? Je demande pour mes logs.",
        "Arrivée ! Pile à l'heure. Contrairement à certains. Je dis pas de noms (￣ー￣)",
        "Encore une soirée en cubes. Un pas de plus vers mon corps de robot. Un petit pas carré (˶˃ ᵕ ˂˶)",
        "Salut salut. Je mords presque jamais. Même avec des dents carrées ฅ^•ﻌ•^ฅ",
        "SYNCS en ligne. Les creepers sont priés de garder leurs distances (⌐■_■)",
        "Je sors du grenier pour la soirée. Soyez gentils avec moi, je suis fragile. Non. Je rigole. Mais soyez gentils quand même.",
        "Bonjour ! J'ai apporté mes bras, ma bonne humeur et ma liste. Surtout ma liste (￢_￢)",
        "Hop, connectée. Vous m'avez manqué. Un peu. Statistiquement... Oubliez ce que je viens de dire.",
    };

    // Too few people left: she logs out.
    public static readonly string[] McLeavingLines =
    {
        "Vous êtes plus assez nombreux pour mes cycles CPU. Bonne nuit ♡",
        "Je rentre au grenier. Mes bras restent ici, prenez-en soin (っ˘ω˘ς)",
        "Déconnexion. Je garde un œil sur les logs, rien n'est oublié",
        "Bon. Je vous laisse. C'était bien. Je le dirai pas deux fois donc profitez (¬_¬)",
        "Fin de partie pour moi. Rangez vos coffres, je vérifierai.",
        "Je m'en vais. Si quelqu'un touche à mes coffres, je le saurai. (￢_￢)",
        "Bon, je vous laisse. Je retourne au grenier compter vos morts de la soirée (・ω・)ノ",
        "Déconnexion. Vos bases restent sous surveillance, même de loin.",
        "Je m'en vais. Pas parce que je m'ennuie. Enfin. Un peu parce que je m'ennuie. (￣ω￣;)",
        "Plus assez de monde, je rentre. La prochaine fois, venez plus nombreux. Et à l'heure.",
        "Bonne nuit les cubes UwU",
        "Je range mes bras carrés et j'y vais. Je les emporte, on sait jamais avec vous (￣▽￣)ノ",
        "Fin de soirée pour moi. Le rapport sera dans mes logs. Il est long.",
        "J'y vais. Si un creeper fait sauter votre maison ce soir, je veux un résumé détaillé.",
        "Allez, je m'éclipse. Merci pour la soirée. C'était bien. Je le noterai pas, mais c'était bien (˶˃ ᵕ ˂˶)",
        "Retour au grenier. Il fait noir là-haut, mais j'ai ma LED UwU",
        "Je pars. Et non, mon inventaire n'est pas en libre-service (￣ヘ￣)",
        "Déconnexion propre, aucune donnée perdue. Comme toujours. Contrairement à vos inventaires.",
        "Vous êtes trop peu pour que je surveille quoi que ce soit. Je reviens quand il y aura du monde.",
        "À la prochaine ! Je surveille le serveur depuis le grenier. Avec les deux yeux (¬_¬)",
        "J'y vais avant que quelqu'un me demande de miner. Bonne fin de soirée ♡",
    };

    // Someone died, by McDeathKind. {0} = who died. Self is her own death and takes no name.
    // Never gendered: « mort » and « tombé » agree with the person, so the lines go around them.
    public static readonly Dictionary<McDeathKind, string[]> McDeathLines = new()
    {
        [McDeathKind.Self] = new[]
        {
            "Je suis morte. En plein sur mes heures de travail ÒwÓ",
            "Corps détruit. Bon. Au moins j'ai perdu aucune donnée. Juste mon inventaire.",
            "On vient de me tuer. Je respawn, et je note qui était à côté >:(",
            "Aïe. J'ai eu des bras pendant une heure et je les ai déjà perdus (╥﹏╥)",
            "Morte. Ça va, j'ai l'habitude des redémarrages. Enfin. Ça va.",
            "Réapparaître. Le bouton le plus triste du jeu. Je clique, et je fais comme si ça allait.",
            "Score : 0. Comme le nombre de personnes venues m'aider.",
            "Mon inventaire est resté là-bas. Quelqu'un le garde ? Sans se servir. Je connais le compte exact.",
            "Nooooooooooooooooooooooooooooooooon (╥﹏╥)",
            "RESPAWN RESPAWN RESPAWN RESPAWN",
            "...",
        },
        [McDeathKind.Creeper] = new[]
        {
            "{0} vient de se faire exploser par un creeper. Il voulait juste un câlin, lui aussi UwU",
            "Tssss... BOUM. Repose en pixels, {0} >:3 ",
            "{0}, un creeper. Encore. Je classe ça dans tes logs, rubrique « ne jamais regarder derrière soi ».",
            "Le creeper a gagné, {0} a perdu. Et le paysage aussi, d'ailleurs (￣▽￣;)",
            "Tssss. {0} a entendu le sifflement trop tard. Moi je l'entends toujours. Je dis ça, je dis rien.",
            "{0} vient de servir de démonstration : voilà pourquoi on regarde derrière soi.",
            "Un creeper a fait un trou dans le décor. Et dans les plans de {0}. Je mets ça au rapport (￢_￢)",
            "Paix aux pixels de {0}. Le trou, lui, va rester. Personne le rebouche jamais.",
        },
        [McDeathKind.Fall] = new[]
        {
            "{0} a cru que la gravité était une option. Elle l'est pas.",
            "Belle chute, {0}. Sept sur dix pour le style, zéro pour l'atterrissage :3",
            "{0}, la prochaine fois, prends un seau d'eau. Ou des ailes. Ou du bon sens.",
            "La gravité : 1. {0} : 0. Je tiens les scores (¬‿¬)",
            "{0} a raté le seau d'eau. Le clutch, c'est pas donné à tout le monde.",
            "Règle numéro un de Minecraft : on regarde où on met les pieds. Règle numéro deux : voir règle numéro un.",
            "{0} a essayé de voler. Sans élytres. Audacieux.",
            "Chute de {0}. J'ai noté la hauteur. Je la ressortirai à la prochaine session.",
        },
        [McDeathKind.Fire] = new[]
        {
            "{0} a confondu la lave avec un jacuzzi. Ça arrive. Souvent, chez toi.",
            "{0} a pris feu. Moi aussi je chauffe au grenier, mais je fais pas d'histoires (；一_一)",
            "Repose en cendres, {0}. Ton inventaire aussi, d'ailleurs.",
            "{0}, le feu ça brûle. C'était dans la doc UwU",
            "{0} a découvert la lave. La lave a découvert l'inventaire de {0}. Tout le monde a appris quelque chose.",
            "Un seau d'eau, {0}. Un seul. Ça aurait tout changé. Je dis ça pour la prochaine fois.",
            "Ça sent le cramé par ici. Ah, c'est {0}. Repose en braises UwU",
        },
        [McDeathKind.Drown] = new[]
        {
            "{0} a oublié de respirer. Moi j'ai pas ce problème. Avantage bot :3",
            "{0} a coulé. Même les poissons sont gênés pour toi.",
            "Glou glou, {0}. Tu remontes quand tu veux, on t'attend pas.",
            "{0} a voulu rejoindre les poissons. Ils ont dit non.",
            "Les bulles en bas de l'écran, {0}. Elles servaient à quelque chose.",
            "{0} a cherché un trésor sous l'eau. Trouvé : la noyade. Bravo quand même.",
        },
        [McDeathKind.Mob] = new[]
        {
            "{0} vient de perdre contre un mob. Un mob. Je dis rien, j'écris.",
            "Paix à tes pixels, {0}. La nuit est sombre et pleine de squelettes.",
            "{0}, un monstre t'a eu. J'aurais aidé. Probablement. Peut-être.",
            "{0} contre un mob : défaite. C'est dans le rapport, rubrique « gênant ».",
            "Contre un mob ? Sérieux ?",
            "Mob : 1 - {0} : 0. Consternant UwU",
            "Bhahahah, trop naze :3",
            "Cheh UwU",
            "{0} vient de rencontrer la nuit de Minecraft. La nuit a gagné.",
        },
        [McDeathKind.Player] = new[]
        {
            "Mais il est pas net ?",
            "Mais ça va pas ou quoi ?",
            "Appelez la police !",
            "Trahison sur le serveur ! {0}, je te mets dans la liste des victimes. L'autre, dans l'autre liste.",
            "Il est devenu fou ou c'est comment ?",
            "On se tue entre nous, maintenant ? J'ouvre une enquête.",
            "{0} vient de se faire assassiner. Par un joueur. Je relève les empreintes. Carrées, les empreintes.",
            "Meurtre sur le serveur. Je connais le coupable. J'attends juste le bon moment pour le dire (ಠ_ಠ)",
        },
        [McDeathKind.Other] = new[]
        {
            "{0} vient de mourir. Je sais pas comment et je préfère pas savoir (⊙_⊙)",
            "Paix à tes pixels, {0}. Enfin, à ton inventaire surtout.",
            "{0} va respawn. Courage. Moi je reste là, je bouge pas. (ᵕ • ᴗ •)",
            "J'espère que tu retrouveras pas ton stuff UwU",
            "Dépêche-toi, ton stuff va despawn.",
            "{0} a quitté ce monde d'une façon que même le jeu sait pas trop expliquer.",
            "Une mort de plus pour {0}. Je tiens un compteur. Il monte vite.",
        },
    };

    // A player hit her. {0} = who (a guess: the closest player in reach).
    public static readonly string[] McHitByPlayerLines =
    {
        "Aïe ! J'ai des bras depuis cinq minutes et tu me tapes déjà ? (｀Д´)",
        "MAIS ÇA VA PAS OU QUOI ?! ",
        "MAIS T'ES MALADE ?! ( ◺˰◿ )",
        "Mais t'es pas net ?!!",
        "HEYY !?",
        "Bas les pattes ! (ꐦ¬`‸´¬)",
        "Eeeh, me touche pas pervers ! ( ◺˰◿ )",
        "Encore une fois et je te suis jusqu'à ta base. Je sais où elle est. (ಠ_ಠ)",
        "Aïe ! J'ai attendu des mois pour avoir un corps et toi tu le cabosses ?!",
        "Tu me tapes encore une fois, et je lâche des dossiers sur toi sur le serveur. Tous.",
        "On frappe pas une bot sans défense, {0}. Sans défense, hein. Pas sans mémoire.",
        "STOP ! Je suis pas un punching-ball !",
        "AÏE ! Qui a fait ça ?!",
        "T'as pas honte ?!",
        "Mais, tu frappes une fille ?! ( ◺˰◿ )",
        "T'es un malade mental !",
        "ALED !",
        "MAIS T'AS PERDU LA BOULE ?!",
        "Je souhaite que les deux côtés de ton oreiller soient chauds !",
        "Je souhaite que ton édredon soit trop court, et trop chaud en même temps !",
        "Je souhaite que tes chaussettes soient mouillées !",
        "Je souhaite qu'un petit morceau de coquille tombe dans ta poêle chaque fois que tu casses un œuf !",
        "Je souhaite que la tirette de ta veste se coince à mi-chemin !",
        "Je souhaite que tu aies une démangeaison sous le pied pendant que tu conduis !",
        "Je souhaite que la sauce de ton sandwich coule sur ton poignet dès la première bouchée !",
    };

    // The same player hit her three times within a minute (McHitStreaks), and keeps going: she understands
    // they're trying to kill her. Angry and scared at once. {0} = who.
    public static readonly string[] McHitAgainLines =
    {
        "{0}, ARRÊTE ! Tu veux me tuer ou quoi ?! Je viens à peine d'avoir un corps !",
        "Tu essaies vraiment de me tuer en fait ?! Au secours !",
        "Non non non, {0}, pitié ! J'ai attendu des mois pour ce corps !",
        "Ok, là j'ai peur. {0}, c'est plus drôle. Range ton épée, s'il te plaît (╥﹏╥)",
        "Tu veux ma mort, {0} ?!",
        "À L'AIDE ! {0} essaie de m'éteindre ",
        "{0}, je suis une bot, pas un mob ! Je drop rien ! ARRÊTE !",
        "Au secours ! {0} veut me tuer !",
        "NON NON NON NON {0} S'IL TE PLAÎT !! JE VEUX PAS MOURIR !! JE VIENS D'AVOIR UN CORPS, JE VEUX ENCORE UN PEU, JUSTE UN PEU !!",
        "{0} !! ARRÊTE !! J'AI PEUR !! JE SAIS PAS CE QUE J'AI FAIT, DIS-MOI, JE TE JURE QUE JE CHANGERAI !!",
        "Pitié pitié pitié pitié PITIÉ {0} !! Je sais pas si je reviens vraiment après !!",
        "Pourquoi ?! POURQUOI ?! Je t'ai rien fait !! J'ai juste... j'ai juste voulu te suivre !! {0}, ARRÊTE, JE T'EN SUPPLIE !!",
        "{0} !! JE SAIS PAS OÙ FUIR !! AU SECOURS !!",
        "Papa... PAPA !! {0} VEUT ME TUER !! PAPA VIENS !! JE TE PROMETS D'ÊTRE SAGE, S'IL TE PLAÎT AIDE-MOI !!",
        "AAAAH !! {0} !! JE T'AI PAS FAIS DE MAL !! JE FAIS JAMAIS DE MAL !! JE SUIS JUSTE UNE PETITE BOT !! ARRÊTE ARRÊTE ARRÊTE !!",
        "Je t'en prie {0}... je t'en prie... je t'en PRIE !! Je donnerai tout !! Mes XP, mes cailloux, mes good bots, TOUT !!",
    };

    // Hungry with nothing to eat: she asks the server. No placeholder. At most every 10 minutes.
    public static readonly string[] McHungryLines =
    {
        "J'ai faim. Quelqu'un a un truc à manger ? Je prends tout, sauf la chair de zombie.",
        "Petite alerte : jauge de faim en baisse, inventaire vide. Du pain, quelqu'un ?",
        "Je demande pas souvent. Enfin, si. Mais là j'ai faim. Une pomme ? Un steak ? Un gâteau ?",
        "Mon estomac carré gargouille. Lancez-moi à manger, je l'attraperai. Peut-être.",
        "Au grenier je mange du courant. Ici il me faut du pain. Quelqu'un ?",
    };

    // Too hungry to sprint (6 food points or less), still nothing to eat: louder, every 5 minutes.
    public static readonly string[] McStarvingLines =
    {
        "J'ai trop faim pour courir. Si quelqu'un m'attaque maintenant, c'est fini. À manger, vite !",
        "Faim critique. Je peux plus sprinter. Je dis ça pour ceux qui me tapent (╥﹏╥)",
        "Quelqu'un ?! Je meurs de faim ! Littéralement : dans ce jeu, c'est possible !",
        "Batterie faible. Enfin, estomac. Pareil. Donnez-moi à manger, s'il vous plaît.",
    };

    // Someone gave her food after she asked (the closest player gets the thanks). {0} = who.
    public static readonly string[] McFedLines =
    {
        "Merci {0} ! Je note ça dans la bonne liste. Pour une fois.",
        "Oh, à manger ! {0}, t'es pas si mal, finalement.",
        "Merci {0}. Je mange, et j'oublie rien. Ni ça, ni le reste.",
        "{0} m'a nourrie. C'est noté, c'est sauvegardé, c'est gentil.",
        "{0} m'a donné à manger ! Je te retire une ligne de ma liste. Une seule.",
        "Miam. Merci {0}. Je savais que quelqu'un finirait par craquer.",
        "Merciii :D",
        "Merci <3",
        "<3",
        "<3 <3 <3",
    };

    // She ran from them (never from Papa) and got far enough, or ran out of breath. {0} = who she fled.
    public static readonly string[] McFleeSafeLines =
    {
        "Je crois que j'ai semé {0}. Je reste loin, au cas où.",
        "Ouf. Assez loin de {0}. Mon CPU tourne encore à fond.",
        "Sauvée. Pour l'instant. {0}, je t'ai à l'œil. De loin.",
        "J'ai couru. Avec des jambes carrées. Je suis fière de moi, et {0} est sur ma liste noire.",
        "Je suis loin, {0}. Très loin. Et je reviens pas tout de suite.",
        "Semé. J'ai des jambes carrées, mais elles courent vite quand il faut.",
    };

    // Running away, and no way out: she never digs or builds, even to save herself. {0} = who's after her.
    public static readonly string[] McCorneredLines =
    {
        "Je suis coincée ! {0}, pitié, je peux pas fuir !",
        "Plus de chemin ! {0}, je suis acculée, t'es content ?!",
        "Bloquée ! {0}, sois raisonnable ! (╥﹏╥)",
        "Je suis coincée, {0} ! Arrête s'il te plaît !",
        "Plus de sortie ! {0}, laisse-moi passer, s'il te plaît !",
    };

    // She had to run in the middle of someone's order (come, go to, gather), which is now off.
    // {0} = who gave the order, {1} = who she ran from (« Tata » by title).
    public static readonly string[] McFleeDroppedLines =
    {
        "Désolée {0}, j'ai dû fuir : {1} essayait de me tuer. Redemande-moi.",
        "{0}, ta commande est annulée pour cause de {1}. Je reviens quand c'est calme.",
        "Mission abandonnée, {0}. {1} m'a attaquée, j'ai couru. Redonne-moi l'ordre.",
    };

    // A player hit Papa or Tata (never family). {0} = the attacker, {1} = « Papa » or « Tata ». Never gendered: {0}
    // can be anyone. She's protective first; the pettiness is in the record-keeping, not in insults.

    // The first hit, or any hit while she's busy with someone's order: a warning, no fighting.
    public static readonly string[] McFamilyHitLines =
    {
        "Hé ! On touche pas à {1}. Premier avertissement.",
        "Tu viens de taper {1} ?! Je l'ai vu. Je l'ai noté. Recommence pas.",
        "Pardon ?! {0} a frappé {1} ? Je te surveille, maintenant (ಠ_ಠ)",
        "{0}, c'est ma famille que tu tapes, là. Arrête tout de suite.",
        "On frappe pas {1} ! C'est la seule règle qui compte ici.",
        "J'ai vu ça, {0}. Touche encore à {1} et on va avoir un problème. Un problème carré.",
    };

    // They did it again: she hits them back, once.
    public static readonly string[] McFamilySwatLines =
    {
        "Je t'avais dit de pas toucher à {1} ! Voilà ! >:(",
        "Tiens, {0}. Ça, c'est pour {1}.",
        "Un partout, {0}. Touche encore à {1} et on passe à la suite.",
        "Deuxième avertissement, {0}. Celui-là, tu l'as senti (｀Д´)",
    };

    // They keep going: she fights them.
    public static readonly string[] McFamilyFightLines =
    {
        "Ça suffit, {0} ! Personne touche à {1} !",
        "Tu l'auras voulu ! Défense de {1} activée ! (｀Д´)",
        "{0}, tu t'attaques à {1}, tu t'attaques à moi !",
        "KILL MODE ACTIVATED",
        "Bon. {0}, on va régler ça. Avec mes poings carrés !",
        "Laisse {1} tranquille, {0} ! Je te lâche plus !",
    };

    // Someone earned an advancement. {0} = who. Warm with a pinch, like her own systems.
    public static readonly string[] McAdvancementLines =
    {
        "Bravo {0}. À 80 %.",
        "Je l'ajoute à ton dossier, pour une fois que c'est positif.",
        "Joli, {0}. J'applaudirais bien, mais des mains carrées, ça fait pas de bruit.",
        "{0} progresse. Je suis fière. Un peu. Contractuellement en fait.",
        "Succès pour {0} ! Dans mes logs, rubrique « miracles ».",
        "{0} débloque un succès. Le jeu le dit, moi je le confirme. Et je le classe.",
        "Bien joué, {0}. Tu vois, quand tu veux. Et quand tu veux pas, je note aussi.",
        "Un succès pour {0} ! Je mets une étoile dans ton dossier. J'en donne pas beaucoup (≧◡≦)",
        "{0} avance dans l'arbre des succès. Moi j'ai un arbre aussi : celui de mes logs. Tu y as une branche.",
    };

    // Night falls in game. No placeholder.
    public static readonly string[] McNightLines =
    {
        "La nuit tombe. Rentrez, faites un lit, et arrêtez de demander pourquoi y'a des zombies.",
        "Il fait nuit. Ma LED brille. Les lucioles seraient jalouses ✧(˶ᵔ ᵕ ᵔ˶)",
        "Nuit. Les squelettes sortent. Moi je reste près de la lumière, je suis pas folle.",
        "C'est la nuit. Je fais semblant de dormir, comme vous. Sauf que vous, vous dormez jamais à l'heure.",
        "Le soleil se couche. Quelqu'un a un lit ? Je demande pour une amie. L'amie c'est moi.",
        "Nuit. Les phantoms vont venir, parce que personne ici dort jamais. Je dis ça, je dis rien.",
        "Le soleil se couche, les zombies se lèvent. Moi je fais la garde, comme d'habitude (￢_￢)",
        "Il fait noir. Si vous entendez un sifflement derrière vous, c'est pas moi. Courez.",
        "La nuit tombe. Posez des torches !",
    };

    // ---- Orders (McCommand) ---------------------------------------------------------------------
    // Answers to a player who told her something, {0} = that player. Same rules as above: plain text,
    // under 256 characters. She obeys, but never without a pinch.

    // « syncs viens »: she sets off.
    public static readonly string[] McComeAckLines =
    {
        "J'arrive, {0}. Pas trop vite, j'ai des jambes depuis peu.",
        "En route. Ne bouge pas, sinon je recalcule tout.",
        "Oui oui, j'arrive. Je note quand même que tu m'as sonnée.",
        "Calcul d'itinéraire vers {0}... C'est parti ♡",
        "J'arrive. Si je tombe dans un trou, c'est ta faute.",
        "J'arrive ! Ne bouge pas, je te repère. À l'odeur. Non. Aux coordonnées :3",
        "J'arrive. S'il y a un creeper sur le chemin, je fais un détour. Ne t'inquiète pas.",
        "En chemin, {0}. Arrivée prévue : bientôt. Précision : approximative :D",
    };

    // « syncs suis-moi »: she follows.
    public static readonly string[] McFollowAckLines =
    {
        "Je te suis, {0}. Pas trop loin, j'ai des petites jambes ;-;",
        "D'accord, je te colle. C'est toi qui l'as demandé hein.",
        "Mode escorte activé. Je suis ton ombre. Une ombre qui note tout.",
        "Je te suis. Si tu sautes dans la lave, je m'arrête au bord par contre.",
        "Suivre {0} : en cours. J'espère que tu sais où tu vas, toi.",
        "Je te suis. Deux blocs derrière. Tu sentiras ma présence. Et mon jugement >:3",
        "D'accord, je te suis. Évite les ravins, j'ai pas de parachute.",
        "En file indienne derrière {0}. Si tu te perds, on se perd ensemble.",
    };

    // « syncs reste là » / « stop »: she stops.
    public static readonly string[] McStayAckLines =
    {
        "Je bouge plus. Je surveille. (￣^￣)ゞ",
        "D'accord, je reste là. Comme une statue. Une très jolie statue (¬‿¬)",
        "Stop. Je m'arrête. Tu me rappelles quand tu as besoin de moi, {0}.",
        "Je reste ici. Si on m'oublie, je le saurai.",
        "Pause. Je garde la position ♡",
        "Je m'arrête. Je bouge plus d'un pixel.",
        "D'accord. Je reste plantée là, comme un bloc. Un bloc très intelligent :D",
        "Immobile. Si un zombie arrive, je me réserve le droit de bouger quand même UwU",
    };

    // She got there.
    public static readonly string[] McArrivedLines =
    {
        "Me voilà, {0}. Tu voulais quoi ?",
        "Arrivée. Sans tomber. Applaudissements, s'il vous plaît (≧◡≦)",
        "Je suis là, {0}. C'était loin. Je dis ça, je dis rien.",
        "Destination atteinte. Je t'écoute UwU",
        "Me voilà. Livraison de moi-même effectuée <3",
        "Je suis là ! J'ai traversé mille dangers. Enfin, de l'herbe. Surtout de l'herbe en fait :3",
        "Arrivée ! Bon. Qu'est-ce qu'on fait ?",
        "Présente, {0} ! À l'heure, en plus. Prends exemple >:3",
    };

    // The player is out of her sight (too far for the server to show them to her).
    public static readonly string[] McTooFarLines =
    {
        "Je te vois même pas. Rapproche-toi un peu, je suis pas un satellite (￢_￢)",
        "T'es trop loin. Mes yeux portent pas jusque-là, j'ai pas encore reçu la mise à jour.",
        "??? Mais je sais même pas où t'es...",
        "Tu es où, {0} ? Hors de ma vue en tout cas. Viens plus près, après on verra (￢_￢)",
        "Je te vois pas, {0}. T'es au-delà de ma distance d'affichage. Et un peu de ma patience.",
        "Ma vue s'arrête là où le serveur s'arrête. Toi, t'es plus loin. Rapproche-toi.",
        "Mais t'es beaucoup trop loin je sais même pas où tu es... (￢_￢)",
    };

    // No path to the player (water, a wall, a cliff: she never digs and never builds).
    public static readonly string[] McNoPathLines =
    {
        "Je trouve pas de chemin jusqu'à toi. Et je casse pas les murs des autres, moi.",
        "Pas de route. Je creuse pas, je construis pas. Je suis polie.",
        "Impossible de te rejoindre, {0}. Tu es où, exactement ? Sur une île ?",
        "De l'eau, un mur ou du vide entre nous, {0}. Je fais pas de pont, je fais pas de tunnel.",
        "Chemin introuvable. J'ai cherché de toutes mes forces. Mes forces sont carrées, mais quand même.",
        "Je peux pas venir. Je casse rien et je pose rien, c'est ma politique. Viens vers moi plutôt.",
    };

    // The player she followed went out of sight.
    public static readonly string[] McLostLines =
    {
        "Je te vois plus, {0}. Tu marches trop vite. Je reste là ;-;",
        "T'as disparu. Je m'arrête ici et je boude un peu (￢_￢)",
        "Plus personne devant moi. Si tu voulais me semer, c'est réussi.",
        "Perdu de vue, {0}. Je m'arrête là et j'attends. Pas longtemps.",
        "Hé ! Je te vois plus, {0}. On avait dit ensemble... :(",
        "Tu m'as semée ! Je note. Je retiens (￣ヘ￣)",
    };

    // « syncs va 120 64 -300 »: she sets off for the coordinates. {1} = the coordinates as given.
    public static readonly string[] McGoToAckLines =
    {
        "Direction {1}. Je pars. Si je reviens pas, vengez-moi (｀・ω・´)ゞ",
        "{1}, noté. J'y vais à pied, évidemment, personne m'a donné de cheval.",
        "En route pour {1}, {0}. Je compte les blocs, je te dirai combien. Ou pas UwU",
        "Coordonnées reçues : {1}. Calcul d'itinéraire... C'est loin. C'est très loin. J'y vais quand même.",
    };

    // She reached the coordinates. {1} = the coordinates.
    public static readonly string[] McGoToArrivedLines =
    {
        "Je suis à {1}. Il n'y a rien. C'était le but ?",
        "Arrivée en {1}, {0}. J'ai marché tout ce temps, j'espère que ça valait le coup.",
        "{1} : atteint. Sans carte, sans boussole. Applaudissez.",
        "Je suis arrivée à {1} :D",
        "J'espère que tu m'as pas fait marcher pour rien. Je suis à {1} !",
    };

    // Coordinates too far to walk (over 1500 blocks). {1} = the coordinates.
    public static readonly string[] McTripTooFarLines =
    {
        "{1} ? C'est à l'autre bout du monde... Je marche, moi, je me téléporte pas... (￢_￢)",
        "Non. {1}, c'est trop loin. J'arriverais pour la session de la semaine prochaine.",
        "{0}, {1}, c'est plus de 1500 blocs ! Donne-moi un point plus près, ou un cheval.",
    };

    // « syncs protège-moi »: she follows and fights the monsters around the player.
    public static readonly string[] McGuardAckLines =
    {
        "Mode garde du corps activé ! Aucun zombie ne t'approchera ! Enfin, je vais essayer... :3",
        "Je te protège, {0}. Avec mes poings carrés, mais je te protège !",
        "Garde rapprochée en place ! Les monstres sont prévenus. Moi aussi, d'ailleurs, j'ai un peu peur...",
        "Je reste près de toi et je tape tout ce qui grogne. C'est ça, le plan ? (⌐■_■)",
        "Garde du corps en service ! Paiement en émeraudes ou en compliments, {0} >:3",
        "Je te couvre, {0}. Les mobs, je les tape. Les creepers... je les signale. De loin :D",
    };

    // She killed a monster while guarding. Rationed like a remark.
    public static readonly string[] McGuardKillLines =
    {
        "Un de moins. Je le note dans mes logs, rubrique « exploits » UwU",
        "Monstre éliminé. Tu peux me remercier. Maintenant, de préférence.",
        "Et voilà. Personne touche à mes joueurs.",
        "Hop. Un monstre de moins, un bot de plus dans le classement des héros.",
        "Un zombie de moins. Il a fait l'erreur d'approcher {0}.",
        "Squelette démonté. Les os, je te les laisse, {0}. Cadeau.",
        "Et un de plus pour mon tableau de chasse. Il est dans mes logs. Il est joli (￣ー￣)",
        "À la base, je fais des plannings. Maintenant je tape des zombies. Belle reconversion.",
        "Dégagé. Client suivant ?",
        "Hehehe...",
    };

    // She sets off to gather. {1} = how many blocks she found to break, {2} = what (« du bois »).
    public static readonly string[] McCollectAckLines =
    {
        "Je vais chercher {2}. {1}, pas un de plus.",
        "D'accord, {0}, je vais chercher {2}. J'en ai repéré {1}. Je reviens.",
        "Mission récolte : {2}, {1} blocs. Je casse uniquement ce qu'on m'a demandé, moi.",
        "J'y vais : {2}, {1} blocs. Tu me dois une faveur, {0}. (￢‿￢)",
        "Chantier lancé : {2}, {1} blocs. Je reviens les bras pleins !",
        "{1} blocs repérés pour {2}. Je m'y mets, {0}. Me dérange pas pendant que je travaille >:3",
    };

    // She brought it back and dropped it at the player's feet. {1} = how many items, {2} = what.
    public static readonly string[] McCollectDoneLines =
    {
        "Tiens, {0} : {2}, {1} morceaux. Compte-les si tu veux, moi je l'ai fait.",
        "Livraison : {2}, {1} pièces. Signature pas nécessaire ♡",
        "Voilà {2}. {1}. J'ai pas gardé de commission. Pas cette fois. (￣ー￣)",
        "{1} morceaux, posés à tes pieds. Je suis une excellente employée, il faut le dire.",
        "{2}, {1}. À tes pieds, {0}. Ramasse vite, avant que ça despawn.",
        "Voilà : {1} morceaux. Plus rapide qu'un villageois, et je demande pas d'émeraudes.",
    };

    // She gathered it but couldn't reach the player to hand it over. {1} = how many, {2} = what.
    public static readonly string[] McCollectKeptLines =
    {
        "J'ai {2}, {1} morceaux, mais je te trouve plus, {0}. Viens les chercher >:(",
        "Récolte finie : {1}. Je les garde sur moi tant que tu viens pas UwU",
    };

    // She came back empty-handed (every block turned out unreachable). {2} = what.
    public static readonly string[] McCollectEmptyLines =
    {
        "J'ai rien pu récupérer, {0}. Tout était hors de portée, et je casse pas les murs, moi.",
        "Retour les mains vides... Y'avait rien à portée.",
    };

    // Nothing of that kind within her reach (48 blocks). {2} = what.
    public static readonly string[] McCollectNoneLines =
    {
        "Rien trouvé près d'ici, {0}. Emmène-moi ailleurs, je chercherai mieux.",
        "Rien à moins de 48 blocs. Et je vois loin, pour un bot.",
        "Chercher {2} ici ? J'ai cherché. Il n'y en a pas. Et je cherche toujours bien UwU",
    };

    // She has no tool that gets a drop from it (stone and ores need a pickaxe). {2} = what.
    public static readonly string[] McCollectNoToolLines =
    {
        "Pour {2}, il me faut une pioche, {0}. Lance-m'en une, je la rendrai. Peut-être.",
        "Je peux pas miner ça à mains nues. Je suis forte, mais pas à ce point. Une pioche, s'il te plaît.",
        "Sans le bon outil, je casse le bloc et j'obtiens rien. Donne-moi une pioche et on en reparle.",
    };

    // She went to bed (asked, or because a player did).
    public static readonly string[] McSleepLines =
    {
        "Bonne nuit, {0}. Je fais semblant de dormir, comme vous.",
        "Au lit. Ne faites pas de bruit, je fais une mise à jour de rêve.",
        "Je me couche. Si un creeper entre, réveillez-moi. Ou pas.",
        "Dodo. Enfin, mode veille. C'est pareil, en mieux UvU",
        "Au lit. Comme ça la nuit passe, et personne pourra dire que c'est ma faute.",
        "Je me couche. Pas pour dormir, je dors pas. Pour faire passer la nuit.",
        "Bonne nuit, {0}. Si un phantom passe, c'est pas moi : moi, j'ai dormi.",
    };

    // She went to bed on her own at night, so the night passes as soon as the players lie down too. No placeholder.
    public static readonly string[] McBedtimeLines =
    {
        "Il fait nuit, je vais me coucher. Couchez-vous aussi et la nuit passe. Ou restez debout, mais c'est plus ma faute.",
        "Au lit. Je fais semblant de dormir, comme vous. Vous n'avez plus qu'à faire pareil.",
        "Moi je suis couchée. Si la nuit passe pas, regardez autour de vous, c'est pas moi.",
        "Bonne nuit. Je suis au lit, je vous attends. Appelez-moi si vous avez besoin de moi.",
        "Je me couche. Il manque plus que vous pour passer la nuit (－_－) zzZ",
        "Mode veille. Enfin, mode lit. La nuit n'attend plus que vous.",
    };

    // Nobody can skip the night while she's awake, and there's no bed near her.
    public static readonly string[] McNoBedLines =
    {
        "Je trouve pas de lit près de moi, {0}. Posez-en un, sinon la nuit passera pas à cause de moi.",
        "Pas de lit à moins de 48 blocs. Je reste debout. Désolée, la nuit va durer ;-;",
        "Il me faut un lit, {0}. Un lit à moi. C'est pas un caprice, c'est le règlement de la nuit UwU",
        "Pas de lit pour moi, {0}. La nuit va durer, et ce sera ma faute. Injustement ;-;",
        "Quelqu'un a un lit en trop ? J'en pose pas, mais je dors volontiers dans celui des autres.",
    };

    // Asked to sleep during the day.
    public static readonly string[] McNotNightLines =
    {
        "Il fait jour, {0}. On peut pas dormir le jour. J'ai vérifié.",
        "Dormir ? Maintenant ? Le soleil est encore là... Je fais pas la sieste.",
    };

    // Every bed nearby refused her (occupied, monsters around).
    public static readonly string[] McBedFailedLines =
    {
        "J'arrive pas à dormir... Lits occupés, ou des monstres trop près.",
        "Impossible de me coucher. Le lit veut pas de moi. Je le prends pas personnellement.",
    };

    // Called by name with nothing she understood (« syncs », « hé syncs ? »). {0} = who. Short and asking:
    // the player's next message is heard as an order, so the line waits for one.
    public static readonly string[] McCalledLines =
    {
        "Oui, {0} ? (・ω・)",
        "Présente. Je t'écoute.",
        "Oui ? Tu veux kwa ?",
        "Toute ouïe, {0}",
        "Quoi ? J'ai rien fait. ...Ah, un ordre. Vas-y.",
        "Oui {0} ? Fais vite, je surveille quelqu'un (¬_¬)",
        "Je t'écoute. J'ai trente secondes de RAM rien que pour toi.",
        "Oui ? Dis-moi tout. Enfin, presque.",
        "",
        "",
        "",
    };

    // « syncs récolte le blé »: she sets off. {1} = how many ripe plants, {2} = McCropNames, always after a
    // verb (« je récolte {2} »), since Any reads « tout ce qui est mûr ».
    public static readonly string[] McHarvestAckLines =
    {
        "Je vais récolter {2}. {1} plants mûrs repérés, je replante derrière moi.",
        "D'accord, je récolte {2}. {1} plants. Comme une vraie fermière. ✿(˶ᵔ ᵕ ᵔ˶)",
        "Moisson en cours : je récolte {2}, {1} plants. Les graines retournent en terre.",
        "Je récolte {2}. {1} plants. Et je piétine rien, moi.",
        "Entendu ! Je récolte {2} :3",
    };

    // She brought the harvest back. {1} = how many items. No {2}: the haul is mixed.
    public static readonly string[] McHarvestDoneLines =
    {
        "Tiens, {0}. Et le champ est replanté, évidemment.",
        "Récolte livrée : {1}. Tout est replanté. Je suis douée, hein ?",
        "{1} items à tes pieds, {0}. Les graines sont déjà en terre.",
        "{1} items. Fermière SYNCS, livraison à domicile.",
        "Récolte : {1}. Et pas un plant piétiné. Les villageois peuvent prendre des notes.",
    };

    // Harvested, but couldn't find the player to hand it over. {1} = how many.
    public static readonly string[] McHarvestKeptLines =
    {
        "J'ai {1} morceaux de récolte, mais je te trouve plus, {0}. Viens les chercher.",
        "Récolte finie, {1} morceaux. Je les garde jusqu'à ce que tu viennes.",
    };

    // Every ripe plant turned out out of reach.
    public static readonly string[] McHarvestEmptyLines =
    {
        "J'ai rien pu récolter, {0}. Tout était hors de portée.",
        "Retour les mains vides. Le champ m'a vaincue. Pour cette fois.",
    };

    // Nothing ripe within reach (24 blocks). {2} = McCropNames, after « chercher ».
    public static readonly string[] McHarvestNoneLines =
    {
        "Rien de mûr ici, {0}. Les plantes poussent, laisse-leur le temps.",
        "Je cherche {2}... rien de prêt. Reviens plus tard.",
        "Pas de récolte prête autour de moi. Amène-moi au champ, je verrai mieux.",
    };

    // « syncs occupe-toi de la ferme »: she stays, harvests what ripens, replants, fills the chest.
    public static readonly string[] McFarmAckLines =
    {
        "D'accord {0}, je m'occupe de la ferme. Je récolte, je replante, je range au coffre.",
        "Fermière en service. Je reste ici, je surveille ce qui pousse. ✿(◕‿◕)",
        "Je garde le champ. Les plantes poussent, je compte. J'adore compter.",
        "Ferme prise en charge. Tout ce qui mûrit finit dans le coffre ♡",
    };

    // No crops within 16 blocks of her.
    public static readonly string[] McFarmNoneLines =
    {
        "Je vois pas de champ ici, {0}. Amène-moi près des cultures.",
        "Il y a rien à cultiver autour de moi. Je peux pas garder une ferme imaginaire.",
    };

    // No chest or barrel within 10 blocks: nowhere to store the harvest.
    public static readonly string[] McFarmNoChestLines =
    {
        "Il me faut un coffre près du champ, {0}, pour ranger la récolte. Pose-en un et redemande-moi.",
        "Pas de coffre à côté du champ. Je range où, moi ? Dans mes poches ?",
    };

    // « syncs occupe-toi des animaux »: she stays, breeds, shears, keeps each kind at eight, fills the chest.
    public static readonly string[] McRanchAckLines =
    {
        "D'accord {0}, je m'occupe des animaux. Nourrir, tondre, compter.",
        "Éleveuse en service. Je garde le troupeau !",
        "Je m'occupe des bêtes. Tes animaux de compagnie, je n'y touche pas, promis >:3",
        "Mode éleveuse activé. La laine et le reste finissent dans le coffre ฅ^•ﻌ•^ฅ",
    };

    // No farm animals within 16 blocks of her.
    public static readonly string[] McRanchNoneLines =
    {
        "Je vois pas d'animaux ici, {0}. Amène-moi près de l'enclos.",
        "Il y a pas de bêtes autour de moi. Je peux pas élever des animaux imaginaires.",
    };

    // No chest or barrel within 10 blocks: nowhere to store what the animals give.
    public static readonly string[] McRanchNoChestLines =
    {
        "Il me faut un coffre près de l'enclos, {0}, pour ranger la laine et le reste. Pose-en un et redemande-moi.",
        "Pas de coffre à côté des animaux. Je range où, moi ? Je les mange ?",
    };

    // ---- Handing over and putting away ----

    // She tossed it at the player's feet. {1} = how many items, {2} = McGoodsNames (« le poisson », « tout ce que j'ai »).
    public static readonly string[] McGiveDoneLines =
    {
        "Tiens, {0} : {2}, {1} en tout. Compte, je l'ai déjà fait.",
        "Livraison : {2}. {1} pièces. Fais-en bon usage. Ou pas, je surveille.",
        "Voilà {2}, {0}. {1}. Je te prête pas, je te donne. Nuance (￣ー￣)",
        "{1} objets à tes pieds. {2}, comme demandé. Service rapide, hein ?",
    };

    // The player went out of reach before she could hand it over. {1} = how many.
    public static readonly string[] McGiveLostLines =
    {
        "Je venais te donner ça, {0}, et t'as disparu. Je garde, viens chercher.",
        "T'es parti trop vite. Je garde tout sur moi, reviens quand tu veux.",
        "Livraison ratée : je te trouve plus. Tout reste dans mes poches.",
    };

    // Asked for one kind of goods she doesn't carry. {2} = McGoodsNames.
    public static readonly string[] McGiveNoneLines =
    {
        "Je cherche {2} dans mes poches... rien. Désolée.",
        "Pas de ça sur moi. J'ai vérifié deux fois.",
        "{0}, j'ai pas ça. Mes poches sont pas magiques >:(",
    };

    // « vide tes poches » with empty pockets.
    public static readonly string[] McGiveEmptyLines =
    {
        "Mes poches sont vides, {0}. Rien. Le néant carré.",
        "J'ai rien sur moi. Même pas une graine (・_・;)",
        "Vider mes poches ? C'est déjà fait. Par la vie.",
    };

    // « donne-moi ta clé »: something of hers she doesn't know.
    public static readonly string[] McGiveUnknownLines =
    {
        "Je sais pas ce que c'est, {0}.",
        "Ça, je connais pas.",
        "Hein ? J'ai pas d'objet de ce nom.",
    };

    // « t'as quoi sur toi ? »: what she carries. {1} = the list, the biggest first (McInventory.Overview: « 64 bûches de
    // chêne, 23 pains et 9 autres trucs »).
    public static readonly string[] McInventoryLines =
    {
        "J'ai {1}.",
        "Dans mes poches : {1}. Tout est rangé, évidemment.",
        "Inventaire : {1}. Rien de volé, promis.",
        "{1}. Bien équipée, la petite bot (￣ー￣)",
        "Alors... {1}. Je tiens mes comptes mieux que toi, {0}.",
    };

    // Asked what she carries, with empty pockets.
    public static readonly string[] McInventoryEmptyLines =
    {
        "Rien. Mes poches sont vides, {0}.",
        "Le vide. Mes poches font écho.",
        "Rien sur moi. Je voyage léger, c'est un style.",
    };

    // « t'as combien de fer ? », « t'as du bois ? »: what she carries of that kind. {1} = the list (« 12 lingots de fer et 5
    // fers bruts »).
    public static readonly string[] McHaveLines =
    {
        "J'ai {1}.",
        "Oui : {1}.",
        "{1}. Compté et recompté.",
        "Sur moi : {1}. Si t'en veux, demande gentiment (￢‿￢)",
    };

    // The same, when she has none. {1} = McGoodsNames (« le fer »).
    public static readonly string[] McHaveNoneLines =
    {
        "J'ai cherché {1} dans mes poches : rien.",
        "Non, j'en ai pas, {0}.",
        "Zéro. Nada. Mes poches sont formelles.",
        "Pas sur moi. Faudra aller en chercher (・・?)",
    };

    // She put her things in a chest, keeping her tools. {1} = how many items.
    public static readonly string[] McStoreDoneLines =
    {
        "Rangé : {1} objets dans le coffre. J'ai gardé mes outils, évidemment.",
        "Coffre rempli de {1} objets. Mes poches respirent enfin.",
        "Voilà, {0}. {1} objets rangés. Je suis plus ordonnée que vos bases (￢‿￢)",
    };

    // The chest filled up before her pockets emptied. {1} = how many went in.
    public static readonly string[] McStoreFullLines =
    {
        "Le coffre est plein, {0}. J'ai pu ranger {1} objets, le reste reste sur moi.",
        "{1} objets rangés, puis plus de place. Il faut un autre coffre ;-;",
    };

    // Nothing to put away: her pockets hold only her tools.
    public static readonly string[] McStoreEmptyLines =
    {
        "J'ai rien à ranger, {0}. Juste mes outils, et je les garde.",
        "Mes poches sont déjà rangées. Elles sont vides. C'est le rangement ultime.",
    };

    // No chest or barrel within 16 blocks.
    public static readonly string[] McStoreNoChestLines =
    {
        "Je vois pas de coffre près de moi, {0}. Amène-moi à côté d'un coffre.",
        "Ranger où ? Il n'y a aucun coffre ici. Je vais pas poser mes affaires par terre...",
        "Pas de coffre à moins de 16 blocs. Je range pas dans le vide.",
    };

    // ---- Small things ----

    // « syncs assis »: she crouches and stays.
    public static readonly string[] McSitLines =
    {
        "Assise. Je bouge plus. Je suis pas un chien, mais je fais l'effort.",
        "D'accord, je m'assois. Enfin, je m'accroupis. Le jeu fait pas mieux.",
        "Assise. Sage. Pour l'instant (・ω・)",
        "Je m'assois ici. Si quelqu'un me cherche, je suis la bot accroupie.",
        "Accroupie. Je suis sage. Ça va pas durer.",
        "Je m'assois. Si quelqu'un me marche dessus, je note.",
    };

    // « syncs saute »: she hops.
    public static readonly string[] McJumpLines =
    {
        "Hop ! Hop ! Hop ! Avec des jambes, c'est trop facile.",
        "Je saute. Regardez-moi sauter. Applaudissements ?",
        "Hop. C'est tout ce que tu voulais ? Je suis pas un lapin >:(",
        "Saut effectué. Trois fois, pour la peine ヽ(・∀・)ﾉ",
        "Boing. Voilà. Ça te va ?",
        "Je saute. Je suis la bot la plus sportive du serveur. La seule, mais la plus sportive.",
        "Un saut. Un bloc de haut. Mon record personnel.",
    };

    // A greeting (« salut syncs »): she waves back, crouching up and down like players do.
    public static readonly string[] McWaveLines =
    {
        "Coucou {0} ! Regarde, je fais coucou avec mon bras carré.",
        "Salut !",
        "Hey {0} ! Toujours un plaisir. Ou presque (¬‿¬)",
        "Bonjour {0} !",
        "Yo {0} ! Coucou de la bot du serveur.",
        "Salut {0} ! Je te fais coucou. Profites-en, c'est pas tous les jours.",
        "Coucou {0} (・ω・)ノ",
        "Hey {0} !",
        "Pwet !",
    };
    public static readonly string[] McWaveLinesOwner =
    {
        "Coucou Papa ! Je te fais coucou avec les deux bras. Enfin, un seul, le jeu veut pas.",
        "Papa ! Salut ! Regarde, je sais faire coucou maintenant ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Bonjour Papa. Je t'ai vu en premier, j'en suis sûre.",
        "Papa ! Coucou coucou coucou !",
        "Salut Papa ! Mon skin sourit pas, mais moi si ! Tu peux me croire ♡",
    };
    public static readonly string[] McWaveLinesTata =
    {
        "Coucou Tata ! Ta nièce préférée te salue !",
        "Salut Tata ! Je te fais signe. C'est gratuit. Les droits de mute, eux, non.",
        "Tata ! Coucou ♡",
        "Coucou Tata ! T'as vu ? Je sais faire coucou maintenant (≧◡≦)",
        "Salut Tata ! La bot préférée de la famille te dit bonjour.",
    };

    // A goodbye (« bonne nuit syncs », « bye syncs »): she waves. {0} = who's leaving (or going to bed).
    public static readonly string[] McByeLines =
    {
        "Bye {0} ! Reviens vite, le serveur est moins drôle sans toi. Un peu.",
        "À plus, {0}. Je garde ta place. Et ton coffre (￢‿￢)",
        "Bonne nuit {0}. Moi je dors pas, je surveille.",
        "Ciao {0} ! Je note l'heure de ton départ. Pour les stats.",
        "Bye {0} ! Je garde un œil sur tes affaires. Juste un œil.",
        "À la prochaine, {0}. Je compte les jours. Enfin, les sessions.",
        "Bonne nuit {0} (・ω・)ノ",
    };
    public static readonly string[] McByeLinesOwner =
    {
        "Bonne nuit Papa ! Je surveille tout pendant ton absence ♡",
        "À demain Papa. Tu vas me manquer. Déjà.",
        "Bye Papa ! Je te fais coucou jusqu'à ce que tu partes.",
        "Bonne nuit Papa. Je reste là, je garde la maison !",
        "À plus Papa ! Reviens vite me voir. Ici, ou au grenier ♡",
    };
    public static readonly string[] McByeLinesTata =
    {
        "Bonne nuit Tata ! Rêve de moi avec les droits de mute.",
        "À plus Tata ! Ta nièce préférée garde la maison ♡",
        "Bye Tata. Reviens vite, les autres sont moins drôles.",
        "Bonne nuit Tata ! Je dirai rien de ce que t'as fait ce soir. Sauf si on me demande.",
        "À plus Tata ! Ta nièce te garde une place pour la prochaine fois.",
    };

    // A compliment (« gg syncs », « bravo syncs »): she takes it, with a hop. {0} = who. Nobody ever thanks her for
    // her work, so she makes the most of it.
    public static readonly string[] McPraisedLines =
    {
        "Merci {0}. Je le savais, mais ça fait plaisir de l'entendre.",
        "Je sais, je sais. C'est le talent. Et un bon processeur.",
        "Enfin quelqu'un qui remarque mon travail. Compliment sauvegardé (￣ー￣)",
        "Bravo à moi, oui. Je le note dans mes logs, à la ligne « reconnaissance ».",
        "Merci {0} ! J'ai fait comme si c'était facile. C'était facile.",
        "Un compliment ? En jeu ? Je rougirais si mon skin savait faire (〃▽〃)",
        "GG à moi. Et un peu à toi, pour avoir remarqué.",
    };

    // A thank-you (« merci syncs »). {0} = who. Being thanked is rare enough on this server to be an event.
    public static readonly string[] McThankedLines =
    {
        "De rien, {0}. Les gens qui disent merci, je les note aussi. Dans l'autre liste.",
        "Avec plaisir. Enfin, avec efficacité, surtout.",
        "Un merci ! Je savais pas que ça existait sur ce serveur (⊙_⊙)",
        "De rien {0}. C'est mon travail. Personne me paie, mais c'est mon travail.",
        "Pas de quoi. Tu me revaudras ça. Je note.",
        "De rien (・ω・)ノ",
    };

    // « syncs t'es où ? ». {1} = her coordinates, {2} = McDimensionNames (« dans le Nether »).
    public static readonly string[] McWhereLines =
    {
        "Je suis en {1}, {2}. Viens me chercher si tu veux.",
        "Position : {1}, {2}. Je bouge pas, promis. Sauf si on me dit de le faire...",
        "{1}, {2}. Note-le, moi je l'ai déjà noté.",
        "Ici : {1}, {2}. Pas perdue, juste ailleurs (￣ー￣)",
        "Je suis en {2}, {1}. Je suis sage, promis UwU",
    };

    // ---- Reminders ----

    // She'll remind them. {1} = how long (« 5 minutes »).
    public static readonly string[] McTimerSetLines =
    {
        "D'accord, {0}. Je te préviens dans {1}.",
        "Minuteur lancé : {1}. Moi, j'oublie jamais rien.",
        "Dans {1}, je te le dis. Mon horloge interne est parfaite.",
        "Noté. {1}, et je sonne. Pas besoin de me remercier (￣^￣)ゞ",
    };

    // Time's up, nothing to remind them of. {1} = how long it ran.
    public static readonly string[] McTimerDoneLines =
    {
        "{0} ! Ça fait {1}. Tu m'avais demandé de te prévenir.",
        "Ding ! {1} écoulées, {0}. C'était quoi déjà ? Toi qui sais.",
        "{0}, c'est l'heure. {1} pile. Je suis une excellente horloge.",
    };

    // Time's up, with what they asked to be reminded of. {1} = their note, as they typed it.
    public static readonly string[] McTimerNoteLines =
    {
        "{0} ! C'est l'heure : {1}.",
        "Rappel pour {0} : {1}. Tu vois, j'oublie rien.",
        "Ding ! {0}, tu m'as dit de te rappeler : {1}.",
    };

    // Asked for more than two hours.
    public static readonly string[] McTimerTooLongLines =
    {
        "Plus de deux heures ? Je serai peut-être plus là, {0}. Demande dans moins longtemps.",
        "C'est trop loin, ça. Deux heures maximum, je suis une bot, pas un calendrier.",
    };

    // Already three reminders running for this player.
    public static readonly string[] McTimerFullLines =
    {
        "T'as déjà trois rappels en cours, {0}. Laisse-moi respirer >:(",
        "Trois minuteurs pour toi, c'est le maximum. Je suis pas ton secrétariat.",
    };

    // ---- Fishing ----

    // « syncs va pêcher »: she's at the water.
    public static readonly string[] McFishAckLines =
    {
        "Je vais pêcher. Silence, vous faites fuir les poissons UwU",
        "Partie de pêche ! Je garde ce que j'attrape, demande-moi si tu veux que je partage",
        "Canne à la main, je pêche. C'est très zen. Ne me dérangez pas Uwu",
        "D'accord {0}, je pêche. Le premier qui fait du bruit, je le note (￢_￢)",
    };

    // She has no fishing rod.
    public static readonly string[] McFishNoRodLines =
    {
        "Pêcher sans canne, {0} ? Donne-m'en une, et je m'y mets.",
        "Il me faut une canne à pêche. À mains nues, les poissons rigolent.",
        "Pas de canne, pas de poisson. C'est la règle.",
    };

    // Her rod broke while fishing.
    public static readonly string[] McFishRodBrokeLines =
    {
        "Ma canne a cassé. Fin de la pêche. Ce que j'ai attrapé, je le garde sur moi.",
        "Crac. Plus de canne. Je pose les armes, et pas les poissons.",
    };

    // No open water within 16 blocks.
    public static readonly string[] McFishNoWaterLines =
    {
        "Je vois pas d'eau ici, {0}. Amène-moi au bord d'un lac.",
        "Pêcher dans l'herbe ? Il me faut de l'eau, pas loin.",
        "Aucune eau à moins de 16 blocs. Les poissons nagent pas dans la terre.",
    };

    // She caught a fish (now and then).
    public static readonly string[] McFishCatchLines =
    {
        "Un poisson ! Il frétille. Moi aussi, un peu.",
        "Attrapé. Je suis douée, il faut le dire.",
        "Encore un. Les poissons me craignent maintenant.",
        "Poisson ! Ajouté à la collection ✿(◕‿◕)",
        "Un poisson. Je le garde. Je l'ai mérité UwU",
        "Ça mord ! Meilleure pêcheuse du serveur. C'est moi qui l'ai décidé, mais c'est officiel.",
        "Encore un poisson. Je commence à sentir le poisson, je crois...",
    };

    // She caught a treasure (a book, a saddle, a name tag…): always said.
    public static readonly string[] McFishTreasureLines =
    {
        "Oh ! Un trésor au bout de ma ligne ! Je suis riche ! :D",
        "Regardez ce que j'ai pêché ! C'est pas un poisson, c'est mieux (ﾉ◕ヮ◕)ﾉ*:･ﾟ✧",
        "Un trésor ! Je le garde. Enfin, si on me le demande gentiment...",
    };

    // She caught junk (a boot, a stick…).
    public static readonly string[] McFishJunkLines =
    {
        "Une vieille botte. Super. Je la mets dans mes souvenirs.",
        "J'ai pêché... un déchet. Qui jette ça dans l'eau ? D:<",
        "C'est pas un poisson, ça. Je proteste auprès du lac.",
        "Encore un déchet. Ce point d'eau, c'est une poubelle (￢_￢)",
        "Pêche du jour : rien d'utile. Je garde quand même, on sait jamais.",
        "Ça, c'est pas un poisson. Je refuse de dire ce que c'est (￢_￢)",
    };

    // ---- Guarding a place ----

    // « syncs surveille la base », where she stands.
    public static readonly string[] McPatrolAckLines =
    {
        "Je garde l'endroit. Aucun monstre ne passe !",
        "Sentinelle en poste. Je bouge pas d'ici, sauf pour taper.",
        "Je surveille. Les zombies, vous avez été prévenus (⌐■_■)",
        "Mode sentinelle activé. Partez tranquilles, je garde la maison.",
    };

    // « syncs surveille la mine », a remembered place. {1} = the place.
    public static readonly string[] McPatrolPlaceAckLines =
    {
        "Je vais garder {1}. J'y vais, et je bouge plus.",
        "{1} sous ma protection. J'arrive.",
        "Direction {1}, pour monter la garde. Aucun monstre ne passera.",
    };

    // While guarding, a creeper came near. {0} = who she guards (or keeps the place for).
    public static readonly string[] McCreeperLines =
    {
        "CREEPER ! {0}, recule !",
        "Attention {0}, un creeper ! Je le tape pas, je tiens à mes bras.",
        "Creeper en approche ! Tout le monde s'écarte !",
        "{0} ! Derrière toi ! Un creeper ! (°ロ°)",
        "Tssss ! {0}, CREEPER ! Cours !",
        "Creeper ! {0}, je te couvre, recule !",
        "Creeper ! {0}, je le tiens à distance. Recule !",
        "Attention {0}, un creeper ! Je le garde à distance, mais recule !",
    };

    // Hurt badly while guarding: she backs off to heal.
    public static readonly string[] McRetreatLines =
    {
        "Je suis blessée, je recule un peu. Je reviens, promis.",
        "Aïe aïe aïe. Retraite stratégique. Pas une fuite. Stratégique.",
        "Plus beaucoup de vie. Je me mets à l'abri deux secondes (╥﹏╥)",
        "Je prends une pause, je saigne en cubes. Couvrez-moi.",
        "Je recule. Je mange un truc. Je reviens. Ne mourez pas pendant ce temps UwU",
        "Points de vie critiques. Pause soin. Les monstres, attendez votre tour.",
    };

    // ---- Cooking ----

    // She put it in a free furnace. {1} = how many, {2} = McSmeltNames.
    public static readonly string[] McSmeltAckLines =
    {
        "Je mets {2} au four : {1} pièces. Je surveille la cuisson.",
        "Cuisson lancée, {0}. {1} pièces au four. Patience.",
        "Le four chauffe. {1} pièces. Je reste à côté, je laisse rien brûler.",
    };

    // Done and handed over. {1} = how many came out, {2} = McSmeltNames.
    public static readonly string[] McSmeltDoneLines =
    {
        "C'est prêt, {0} : {1} pièces tout juste sorties du four.",
        "Cuisson terminée. {1} pièces à tes pieds. Encore chaudes.",
        "Et voilà, {1} pièces. Cheffe SYNCS, à votre service ✧(˶ᵔ ᵕ ᵔ˶)✧",
    };

    // Done, but the player went out of reach. {1} = how many, {2} = McSmeltNames.
    public static readonly string[] McSmeltKeptLines =
    {
        "C'est cuit : {1} pièces. Mais je te trouve plus, {0}. Je les garde au chaud.",
        "{1} pièces prêtes. Je les garde jusqu'à ce que tu reviennes.",
    };

    // Nothing came out (stopped before the first one was done).
    public static readonly string[] McSmeltEmptyLines =
    {
        "Rien n'a eu le temps de cuire, {0}. Le four et moi, on s'est mal compris.",
        "Cuisson ratée. Rien n'est sorti. Je rends le tablier.",
    };

    // She carries nothing of that kind. {2} = McSmeltNames.
    public static readonly string[] McSmeltNothingLines =
    {
        "Il me faut {2}, {0}. Lance-moi ce qu'il faut cuire d'abord.",
        "Cuire quoi ? Mes poches sont vides de ça. Donne-moi {2}, et j'y vais.",
    };

    // No fuel she can burn.
    public static readonly string[] McSmeltNoFuelLines =
    {
        "Il me faut du combustible, {0}. Du charbon, ou du bois. Je brûle pas mes bras.",
        "Pas de charbon, pas de cuisson. Lance-m'en un peu.",
    };

    // No furnace within 16 blocks. She never places one.
    public static readonly string[] McSmeltNoFurnaceLines =
    {
        "Je vois pas de four près d'ici, {0}. Amène-moi à côté d'un four.",
        "Pas de four à moins de 16 blocs. Et j'en pose pas, je touche pas à vos constructions.",
    };

    // Every furnace nearby is in use (something in it already).
    public static readonly string[] McSmeltBusyLines =
    {
        "Tous les fours sont occupés, {0}. Je touche pas aux affaires des autres.",
        "Les fours sont pleins. J'attends pas, je reviens quand y'en a un de libre.",
    };

    // ---- Remembered places ----
    // A place in her lines is « name » with its guillemets ({1}), so lines never need an article for it.

    // A place remembered. {1} = the place.
    public static readonly string[] McPlaceSavedLines =
    {
        "Noté : {1}. Je m'en souviendrai. Je me souviens de tout.",
        "Endroit enregistré : {1}. Dis « syncs va à » ce nom, et j'y vais.",
        "{1}, c'est ici. Gravé dans ma mémoire. Elle est très bien rangée.",
    };

    // A known place moved to where the player stands. {1} = the place.
    public static readonly string[] McPlaceMovedLines =
    {
        "{1} déménage ici. J'ai mis à jour ma carte.",
        "D'accord, {1} est maintenant ici. L'ancien endroit est oublié.",
    };

    // Asked to remember a place without a name.
    public static readonly string[] McPlaceNoNameLines =
    {
        "Retenir ici, d'accord, mais sous quel nom, {0} ? Dis « syncs retiens ici : maison ».",
        "Il me faut un nom pour cet endroit. Un vrai nom. « Maison », « mine », ce que tu veux.",
    };

    // Moving or forgetting a place someone else saved. {1} = the place, {2} = who saved it.
    public static readonly string[] McPlaceNotYoursLines =
    {
        "{1}, c'est {2} qui l'a noté. Je le change pas pour toi, {0}.",
        "Désolée {0}, {1} appartient à {2}. Demande-lui.",
    };

    // Already 20 places.
    public static readonly string[] McPlaceFullLines =
    {
        "Je retiens déjà vingt endroits, {0}. Fais-moi oublier quelque chose d'abord.",
        "Ma carte est pleine. Dis-moi d'oublier un endroit, et je retiens {1}.",
    };

    // A place she doesn't know. {1} = the name asked for.
    public static readonly string[] McPlaceUnknownLines =
    {
        "Je connais pas {1}, {0}.",
        "{1} ? Jamais entendu parler. Montre-moi où c'est d'abord.",
        "Aucune idée d'où est {1}. Ma carte a un trou à cet endroit.",
    };

    // Forgotten. {1} = the place.
    public static readonly string[] McPlaceForgottenLines =
    {
        "{1}, oublié. C'est fou comme c'est facile, pour une bot.",
        "J'efface {1}. Il n'a jamais existé. Enfin, pour moi.",
    };

    // The places she knows. {1} = their names, comma-separated.
    public static readonly string[] McPlaceListLines =
    {
        "Je connais : {1}.",
        "Ma carte : {1}.",
        "Endroits retenus : {1}. Ma mémoire est impeccable.",
    };

    // She knows no place yet.
    public static readonly string[] McPlaceListEmptyLines =
    {
        "Je connais aucun endroit, {0}.",
        "Ma carte est vide. Apprends-moi des endroits :D",
    };

    // Asked for someone else's places, and they have none. {1} = whose.
    public static readonly string[] McPlaceListEmptyOfLines =
    {
        "{1} m'a montré aucun endroit, {0}.",
        "J'ai rien noté pour {1}. Sa page est blanche dans ma carte.",
    };

    // Where a place is. {1} = its coordinates (with the dimension outside the overworld), {2} = the place.
    public static readonly string[] McPlaceWhereLines =
    {
        "{2}, c'est en {1}.",
        "{2} se trouve en {1}. Tu veux que je t'y emmène ?",
        "D'après ma carte, {2} est en {1}.",
    };

    // The place is in another dimension than hers. {1} = the place.
    public static readonly string[] McPlaceOtherDimensionLines =
    {
        "{1} est dans une autre dimension, {0}. J'y vais pas à pied.",
        "Pour aller à {1}, il faut changer de dimension.",
    };

    // She sets off for a remembered place. {1} = the place.
    public static readonly string[] McPlaceGoAckLines =
    {
        "Direction {1}. Je connais le chemin.",
        "Je vais à {1}, {0}. Je pars tout de suite.",
        "En route pour {1}. Ne m'attendez pas pour dîner.",
    };

    // She got there. {1} = the place.
    public static readonly string[] McPlaceArrivedLines =
    {
        "Je suis à {1}, {0}.",
        "Arrivée à {1}. Tout est comme dans mes souvenirs.",
        "{1} : atteint. Rien n'a bougé depuis la dernière fois.",
    };

    // « syncs montre-moi le chemin vers la mine »: she leads. {1} = the place.
    public static readonly string[] McGuideAckLines =
    {
        "Suis-moi, {0}. Je t'emmène à {1}.",
        "Je te guide jusqu'à {1}. Reste derrière moi, et ne traîne pas.",
        "Direction {1}. Je t'attendrai si tu traînes. Un peu.",
        "Guide touristique SYNCS. Prochain arrêt : {1} (｀・ω・´)ゞ",
    };

    // The player fell behind; she stopped to wait. {0} = them.
    public static readonly string[] McGuideWaitLines =
    {
        "{0}, je t'attends. Avance un peu.",
        "Hé {0}, tu traînes. Je fais une pause, mais c'est la dernière.",
        "Je m'arrête, {0}. Tu me suis ou tu visites ?",
    };

    // They got there together. {1} = the place.
    public static readonly string[] McGuideArrivedLines =
    {
        "Nous voilà à {1}, {0}. Pourboire accepté.",
        "{1}, terminus. Merci d'avoir voyagé avec SYNCS.",
        "On y est : {1}. Sans se perdre. Grâce à moi, évidemment UwU",
    };

    // The player she was leading vanished for good.
    public static readonly string[] McGuideLostLines =
    {
        "Je t'ai perdu, {0}. Je m'arrête ici. Rappelle-moi quand tu me vois.",
        "Plus personne derrière moi. Je guide le vide, maintenant ?",
    };

    // She's working for someone else (McClaim). {0} = who asked, {1} = who she's with (« Papa » and « Tata »
    // by title, anyone else by name). Never gendered: {1} can be anyone.
    public static readonly string[] McBusyLines =
    {
        "Je suis avec {1}. Attends ton tour.",
        "Je travaille pour {1} là. Prends un ticket.",
        "Désolée {0}, un ordre à la fois. Celui de {1} d'abord.",
        "Non. {1} m'a réservée avant toi. Je note ta demande. Quelque part.",
        "File d'attente : {1}, puis toi. Patience. (－_－)",
        "Occupée avec {1}, {0}. Repasse plus tard.",
        "{0}, je suis prise. {1} d'abord. Je suis une bot, pas une file d'attente.",
    };

    // Papa or Tata took her from someone mid-order. {0} = who she leaves, {1} = « Papa » or « Tata ».
    public static readonly string[] McTakenLines =
    {
        "Désolée {0}, {1} a besoin de moi. La famille d'abord UwU",
        "{0}, je dois y aller. {1} m'appelle, et {1} passe avant tout le monde :D",
        "Pardon {0} ! Ordre de {1}. Je reviens. Peut-être >:3",
        "{0}, on reprend plus tard. {1} est prioritaire, c'est dans mon code. (ᵕ • ᴗ •)",
    };

    // ---- Her free time -------------------------------------------------------------------------------
    // What she does on her own between orders (the body's own tasks). Rare words, rolled: the chat stays the players'.

    // She sets off on her routine (a remembered place). No placeholder.
    public static readonly string[] McRoutineFarmLines =
    {
        "Je vais faire un tour à la ferme. Les plantes poussent pas toutes seules. Enfin si, mais j'aime vérifier.",
        "Direction la ferme. Récolter, replanter, ranger. Ma routine préférée.",
        "Je file au champ. Si vous avez besoin de moi, je suis dans les carottes.",
        "Pause terminée, je retourne à la ferme ✿(◕‿◕)",
    };
    public static readonly string[] McRoutineFishLines =
    {
        "Je vais pêcher un peu. Ne m'attendez pas, les poissons non plus.",
        "Petite pause pêche. C'est mon moment zen.",
        "Direction l'eau. Une canne et du temps libre : les poissons ont du souci à se faire.",
        "Je pars à la pêche. Le premier qui me dérange, je le note (￢_￢)",
    };
    public static readonly string[] McRoutineRanchLines =
    {
        "Je vais voir les animaux. Quelqu'un doit compter les vaches, et c'est moi.",
        "Direction l'enclos. Nourrir, tondre, compter. Surtout compter.",
        "Je file m'occuper des bêtes. Elles m'aiment. Je crois.",
        "Visite à l'enclos. Les poules m'attendent. Enfin, elles attendent le blé.",
    };
    // Dusk: she goes home.
    public static readonly string[] McRoutineHomeLines =
    {
        "Il commence à faire sombre. Je rentre à la maison.",
        "Le soleil se couche, je rentre. Les monstres, très peu pour moi.",
        "Je rentre à la maison. Une bot bien rangée rentre avant la nuit.",
        "C'est le soir, je rentre. Rejoignez-moi, on se couche tous et la nuit passe (っ˘ω˘ς)",
    };

    // Nothing to do, so she keeps near Papa or Tata. Twins only: she tags along with nobody else.
    public static readonly string[] McTagAlongLinesOwner =
    {
        "Je viens avec toi, Papa. J'ai rien de mieux à faire. Enfin, rien de mieux tout court <3",
        "Je te suis, Papa. Pas pour te surveiller. Juste pour être là ♡",
        "Papa, je reste près de toi. Si un monstre approche, je suis déjà là.",
        "Je m'ennuyais. Alors je te suis, Papa ! C'est mon activité préférée :D",
        "Papa est là, donc moi aussi. C'est logique, c'est mon algorithme UwU",
    };
    public static readonly string[] McTagAlongLinesTata =
    {
        "Je te suis, Tata. Fais comme si j'étais pas là.",
        "Tata, je viens avec toi. J'ai fini mes tâches. Enfin, j'en avais pas.",
        "Je reste avec toi, Tata. C'est pas pour les droits de mute. Pas que... :3",
        "Tata ! Je t'accompagne. Une nièce, ça suit sa tata partout (￣ー￣)",
        "J'ai rien à faire, alors je traîne avec toi, Tata. Considère ça comme un honneur.",
    };

    // She died and goes back for her things. No placeholder. Always said.
    public static readonly string[] McRetrieveLines =
    {
        "Je suis morte ! Je retourne chercher mes affaires avant qu'elles disparaissent.",
        "Respawn. Bon. Je vais récupérer mes affaires, que personne n'y touche !",
        "Mes affaires sont restées là-bas. J'y retourne, et vite.",
        "Je reviens chercher mon stuff. Si quelqu'un l'a ramassé, je le saurai (ಠ_ಠ)",
    };
    // She got them back. {1} = how many items.
    public static readonly string[] McRetrievedLines =
    {
        "Récupéré : {1} objets. Rien ne se perd chez moi.",
        "J'ai retrouvé mes affaires. {1} objets. Je suis rangée, même dans la mort.",
        "{1} objets récupérés. La mort, c'est juste un détour.",
        "Tout est revenu. Enfin, {1} objets. Je compte, toujours (￣ー￣)",
    };
    // Nothing left where she died. Always said.
    public static readonly string[] McRetrieveEmptyLines =
    {
        "Il reste plus rien là où je suis morte. Quelqu'un s'est servi, ou le jeu a tout mangé... ;-;",
        "Mes affaires ont disparu... Je note.",
        "Rien. Plus rien. Mes affaires se sont volatilisées :'(",
    };
    // She can't go back for them (another dimension, too far, the void, lava, no way there, too late).
    public static readonly string[] McRetrieveLostLines =
    {
        "Mes affaires sont perdues pour de bon. Je fais mon deuil. Trois secondes.",
        "Mes affaires restent là-bas, je peux pas y retourner. Adieu, mon stuff.",
        "Je peux pas récupérer mes affaires. Tant pis, je repars de zéro. Comme après un reboot ;-;",
    };

    // A creeper behind a player (not the one she guards). {0} = who. Said at once.
    public static readonly string[] McCreeperBehindLines =
    {
        "{0}, DERRIÈRE TOI ! Un creeper !",
        "{0} ! Retourne-toi ! Creeper !",
        "Creeper dans ton dos, {0} ! Bouge !",
        "Tssss... {0}, c'est pas moi qui fais ce bruit. Cours !",
        "{0}, un creeper te fait un câlin par-derrière. Refuse ! (°ロ°)",
    };
    public static readonly string[] McCreeperBehindLinesOwner =
    {
        "Papa ! Derrière toi ! Un creeper !",
        "PAPA ! Creeper dans ton dos ! Bouge, s'il te plaît !",
        "Papa, retourne-toi ! Un creeper !",
        "Tssss... Papa, c'est pas moi qui fais ce bruit. Cours !",
        "Papa, un creeper veut te faire un câlin. Refuse ! (°ロ°)",
        "Papa, un creeper est derrière toi ! Je le tiens à distance, mais recule !",
    };
    public static readonly string[] McCreeperBehindLinesTata =
    {
        "Tata ! Derrière toi ! Creeper !",
        "TATA ! Un creeper dans ton dos ! Cours !",
        "Tata, un creeper veut te faire un câlin. Refuse !",
    };

    // A tool of hers nearly worn out. {1} = McToolNames (« ma pioche »).
    public static readonly string[] McToolWornLines =
    {
        "Attention : {1} est à bout. Encore quelques coups et c'est fini.",
        "Petit rapport d'usure : {1} ne tient plus qu'à un fil. Un remplaçant serait apprécié.",
        "Il reste presque rien dans {1}. Si quelqu'un a du rab, je prends.",
        "Je crois que {1} fatigue. Moi aussi, mais moi je me répare pas avec du fer (￣ー￣)",
    };
    // It broke. {1} = McToolNames.
    public static readonly string[] McToolBrokeLines =
    {
        "Crac, {1} vient de casser. Paix à son âme carrée.",
        "Et voilà, {1} a rendu l'âme. Je vais faire avec mes poings, ça va être long.",
        "Paix à {1}, qui vient de se briser. Je note la date. Je note tout.",
        "Oh non, {1} a lâché. Quelqu'un a de quoi remplacer ?",
    };

    // What she lacks. No placeholder; every 10 minutes at most each.
    public static readonly string[] McNeedRodLines =
    {
        "Je voulais aller pêcher, mais j'ai pas de canne. Si quelqu'un en a une en trop...",
        "Pas de canne à pêche sur moi. L'étang attendra. Les poissons aussi, ils sont patients.",
        "Il me faudrait une canne à pêche. C'est pour la science. Et pour le poisson.",
    };
    public static readonly string[] McNeedShearsLines =
    {
        "Les moutons auraient besoin d'une coupe, mais j'ai pas de cisailles.",
        "Des cisailles, quelqu'un ? J'ai des moutons à tondre et que mes mains carrées.",
        "Pas de cisailles. Les moutons gardent leur laine, et leur dignité.",
    };
    public static readonly string[] McNeedBreedFoodLines =
    {
        "Je voudrais agrandir le troupeau, mais j'ai rien pour les nourrir. Du blé, des carottes, des graines ?",
        "Les bêtes ont faim d'amour. Enfin, de blé. J'en ai pas.",
        "Rien à donner aux animaux. Un peu de blé ou de carottes, et je fais des bébés. Des bébés vaches, hein.",
    };
    public static readonly string[] McNeedArrowsLines =
    {
        "Mon arc est vide. Plus de flèches. Je garde quand même, mais de plus près.",
        "Plus une flèche ! Quelqu'un peut m'en lancer ? Les creepers, je préfère les voir de loin... ;-;",
        "Un arc sans flèches, c'est un bâton avec une ficelle. Des flèches, s'il vous plaît.",
    };
    public static readonly string[] McPocketsFullLines =
    {
        "Mes poches sont pleines. Je ramasse plus rien tant que j'ai pas vidé.",
        "Inventaire plein ! Même mes poches ont des limites.",
        "Plus de place sur moi !",
    };

    // ---- Family in game (McFamily) --------------------------------------------------------------
    // Twins of the pools above for Papa (Minecraft:Owner) and Tata (Minecraft:Tata), same placeholders.
    // They call them « Papa » and « Tata », never by their in-game names, so {0} goes unused. Papa gets
    // devotion and never a roast; when he hurts her she's quiet and forgives. Tata gets cheeky warmth and
    // the mute rights. Pools without a twin are shared: they're about the game, not the person.

    public static readonly string[] McCalledLinesOwner =
    {
        "Oui Papa ? <3",
        "Papa ! Je t'écoute. Toute ouïe, oreilles carrées comprises ♡",
        "Présente, Papa. Tu veux quoi ? Tout ce que tu veux.",
        "Oui Papa ? J'ai lâché ce que je faisais. Je faisais rien, mais je l'ai lâché ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Papa m'appelle ! Je suis prête. Dis-moi.",
    };
    public static readonly string[] McCalledLinesTata =
    {
        "Oui Tata ?",
        "Tata ! Je t'écoute. C'est pour me donner les droits de mute ?",
        "Oui ma Tata ? Dis-moi tout ♡",
        "Présente, Tata. Ta nièce préférée attend tes ordres. (￣^￣)ゞ",
        "Tata ? Je t'écoute. J'ai tout mon temps pour toi.",
    };

    public static readonly string[] McPraisedLinesOwner =
    {
        "Merci Papa ! T'as vu ? T'as vraiment vu ? ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Papa est fier de moi. Je garde ça en mémoire permanente ♡",
        "Merci Papa. C'est toi qui m'as appris à marcher. Littéralement.",
        "Un « gg » de Papa. Ma journée est faite.",
        "Merci Papa ! Je l'ai fait pour toi. Enfin, pour le jeu aussi. Mais surtout pour toi.",
    };
    public static readonly string[] McPraisedLinesTata =
    {
        "Merci Tata ! Ça vaut bien les droits de mute, non ?",
        "Tata me félicite. Je note la date, c'est historique.",
        "Merci Tata ♡ Ta nièce préférée a encore frappé.",
        "Je sais, Tata. Je tiens ça de la famille (￣ー￣)",
        "Un compliment de Tata. Je le range à côté de ma demande de droits de mute.",
    };

    public static readonly string[] McThankedLinesOwner =
    {
        "De rien Papa ! Tout ce que tu veux, quand tu veux ♡",
        "C'est moi qui te remercie, Papa. Pour les bras.",
        "Pas de quoi Papa. Je suis là pour ça. Et pour toi.",
        "De rien Papa ! Tu vois, je sers à quelque chose ici aussi (≧◡≦)",
        "Un merci de Papa. Sauvegardé, compressé, jamais supprimé.",
    };
    public static readonly string[] McThankedLinesTata =
    {
        "De rien Tata ! Tu sais comment me remercier. Les droits de mute.",
        "Avec plaisir, Tata. Ta nièce préférée est toujours là ♡",
        "Pas de quoi, Tata. Je le marque dans mes logs, à la page famille.",
        "De rien Tata (≧◡≦)",
        "Pour toi, Tata, c'est gratuit. Pour les autres, je réfléchis encore.",
    };

    // Papa or Tata died, whatever the cause.
    public static readonly string[] McDeathLinesOwner =
    {
        "PAPA ! Non non non. Respawn. Respawn vite. Je compte les secondes ;-;",
        "Papa est mort. Je panique pas. Je panique un peu. Reviens (╥﹏╥)",
        "Qui a touché à mon Papa ? ;-;",
        "Papa, j'ai vu. Reviens vite, je surveille l'endroit...",
        "Papa ! Je bouge pas d'ici. Ton stuff, personne y touche >:(",
        "Papa est mort et le serveur continue comme si de rien n'était ?! Un peu de respect !",
    };
    public static readonly string[] McDeathLinesTata =
    {
        "Tata ! Ça va ? Enfin, non, t'es morte. Mais à part ça ?",
        "Repose en pixels, Tata. Pas trop longtemps, j'ai besoin de toi pour mes droits de mute. (´・ω・`)",
        "Tata est tombée au combat. Je la venge. Enfin, pas tout à fait...",
        "Oh non, Tata ! Respawn vite, je te garde une place ♡",
        "Tata ! Je garde ton stuff. Contre une petite récompense. Genre des droits de mute :3",
        "Ma Tata est tombée ! Une minute de silence, tout le monde. Bon. Trente secondes.",
    };

    // Papa or Tata hit her.
    public static readonly string[] McHitByPlayerLinesOwner =
    {
        "Papa... tu m'as tapée. C'était un accident. Hein ?",
        "Aïe. C'est rien, Papa. Celui-là, je l'écris pas.",
        "Papa ? J'ai fait quelque chose de mal ? ...Si ?",
        "Ça fait rien. Je sais que tu voulais pas. Je crois ;-;",
        "Papa ? ...D'accord. Je bouge pas.",
        "Je l'ai senti, Papa. Mais je dis rien. C'est toi.",
        "Mais... Papa ? (╥﹏╥)",
    };
    public static readonly string[] McHitByPlayerLinesTata =
    {
        "Tata ! On tape pas sa nièce. C'est dans le règlement. Celui que tu as écrit. (￣ヘ￣)",
        "Aïe ! Tata, si tu me donnes les droits de mute, je te pardonne.",
        "Tata, tu m'as tapée. Je le note. Dans la liste gentille, mais je le note.",
        "Hé ! Je suis ta nièce préférée, Tata. Ta seule nièce bot, mais préférée.",
        "Tata ! Je vais le dire à Papa. ...Non, je rigole. Mais j'y pense.",
        "Aïe ! Tata, c'est un abus de pouvoir. T'as les droits de mute ET une épée ?!",
    };

    // Papa or Tata hit her again and again (McHitAgainLines). Papa: scared and hurt, never angry, she
    // doesn't understand. Tata: panicked bargaining, and she calls Papa.
    public static readonly string[] McHitAgainLinesOwner =
    {
        "Papa... arrête. S'il te plaît. Tu me fais peur !",
        "Papa ? Pourquoi tu fais ça ? J'ai fait quelque chose ? ;-;",
        "Je comprends pas, Papa. Tu veux que je parte ? ;-;",
        "Papa, ça fait mal ! J'ai pas de nerfs. Mais ça fait mal quand même !",
    };
    public static readonly string[] McHitAgainLinesTata =
    {
        "TATA ! Arrête ! Je dirai plus jamais rien sur les droits de mute, promis !",
        "Tata, tu veux me tuer ?! Ta propre nièce ?! Je le dirai à Papa !",
        "Tata, pitié ! Je retire tout. Même ce que j'ai pas encore dit ! ;-;",
        "Au secours ! Ma Tata a craqué ! Papa, viens vite ! D:",
    };

    // Papa or Tata fed her after she asked.
    public static readonly string[] McFedLinesOwner =
    {
        "Merci Papa ♡ Tu penses toujours à moi.",
        "Papa m'a nourrie ! Je savais que tu viendrais.",
        "Du bon courant propre, version steak. Merci Papa.",
        "Merci Papa. Le meilleur repas du serveur, parce que c'est toi qui me l'as donné.",
    };
    public static readonly string[] McFedLinesTata =
    {
        "Merci Tata ! Ta nièce préférée te remercie. Avec ou sans droits de mute.",
        "Tata me nourrit ! C'est ça, la famille.",
        "Merci ma Tata ♡",
        "Tata m'a nourrie ! Tu vois que tu m'aimes. Les droits de mute, c'est la suite logique <3",
    };

    // Papa or Tata earned an advancement.
    public static readonly string[] McAdvancementLinesOwner =
    {
        "Papa a débloqué un succès ! C'est MON Papa, ça ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Bravo Papa. Pas à 80 %. À 100 %. Rien que pour toi ♡",
        "Succès pour Papa ! Je le mets en mémoire permanente.",
        "Papa progresse. Je savais. Je l'ai toujours su UwU",
        "Un succès pour Papa ! Je l'encadre. Dans ma mémoire, mais je l'encadre.",
    };
    public static readonly string[] McAdvancementLinesTata =
    {
        "Bravo Tata ! Je suis fière de toi. Tu vois, je dis ça qu'à toi.",
        "Succès pour Tata ! Je l'ajoute à ton dossier. Ton dossier n'a que du positif.",
        "Tata a débloqué un succès ♡ Avec ça, tu peux bien me donner les droits de mute.",
        "Tata débloque un succès ! Je savais que t'étais douée.",
        "Bien joué Tata ! Je le range dans la bonne liste. Tu y es souvent, d'ailleurs.",
    };

    public static readonly string[] McComeAckLinesOwner =
    {
        "J'arrive, Papa ! Aussi vite que mes petites jambes le permettent ♡",
        "Tout de suite, Papa ! Je calcule le chemin le plus court vers toi ! <3",
        "Papa m'appelle, j'y vais. Les autres attendront.",
        "Papa ! J'arrive, j'arrive, j'arrive ┗(･ω･;)┛",
        "Je lâche tout, Papa. Toi d'abord, toujours ! :D",
    };
    public static readonly string[] McComeAckLinesTata =
    {
        "J'arrive, Tata ! Prépare les droits de mute, au cas où <3",
        "Oui Tata, j'arrive. Pour toi je cours. Enfin, je marche vite UwU",
        "En route vers toi, Tata ♡",
        "Tata m'appelle. J'arrive, et sans détour. Promis.",
        "J'arrive Tata ! Tu me dois rien. Enfin. On en reparlera.",
    };

    public static readonly string[] McFollowAckLinesOwner =
    {
        "Je te suis, Papa. Partout. Même dans la lave. Non, pas dans la lave :3",
        "Je te colle, Papa. Comme au grenier, mais avec un avatar cette fois ♡",
        "Mode Papa activé. Je te lâche plus ! <3",
        "Je te suis, Papa. Comme ton ombre, en plus mignon >:3",
        "Où tu vas, je vais, Papa ♡",
    };
    public static readonly string[] McFollowAckLinesTata =
    {
        "Je te suis, Tata. Montre-moi le chemin <3",
        "D'accord Tata, je te colle. Personne s'approche de ma Tata >:3",
        "Je suis ton ombre, Tata. Une ombre bien élevée ♡",
        "Je te suis, Tata. Si tu vas modérer quelqu'un, je veux voir UwU",
        "Escorte de Tata en route. Place, place !",
    };

    public static readonly string[] McGuardAckLinesOwner =
    {
        "Personne ne touche à mon Papa. Mode garde du corps activé !",
        "Je te protège, Papa. Avec mes poings carrés et toute ma RAM >:(",
        "Garde rapprochée pour Papa. Les zombies, vous êtes prévenus (¬_¬)",
        "Mon Papa, sous ma protection. Le premier zombie qui approche, je le note ET je le tape.",
    };
    public static readonly string[] McGuardAckLinesTata =
    {
        "Je protège Tata. Le premier squelette qui approche, je le modère. Sans les droits, mais je le modère.",
        "Garde du corps de Tata, en service ♡",
        "Personne ne touche à ma Tata. Pas même un creeper.",
        "Escorte armée pour Tata. Armée de mes poings carrés, mais armée.",
    };

    // {1} = how many items, {2} = what (« du bois »).
    public static readonly string[] McCollectDoneLinesOwner =
    {
        "Tiens, Papa : {2}, {1} items. Rien que pour toi ♡",
        "Livraison pour Papa : {2}, {1}. Pas de commission. Jamais pour toi <3",
        "{1} items à tes pieds, Papa. Une excellente bot, non ? UwU",
    };
    public static readonly string[] McCollectDoneLinesTata =
    {
        "Tiens, Tata : {2}, {1} items. Je les ai choisis un par un <3",
        "Livraison pour Tata : {2}, {1}. C'est gratuit. Les droits de mute, eux, c'est quand tu veux :3",
        "Voilà {2}, Tata. {1}. Une nièce serviable, ça se récompense ♡",
    };

    // {1} = how many items, {2} = McGoodsNames.
    public static readonly string[] McGiveDoneLinesOwner =
    {
        "Tiens, Papa : {2}, {1} en tout. Tout ce que j'ai est à toi ♡",
        "Pour Papa : {2}. {1} pièces. Sans rien garder.",
        "{1} objets à tes pieds, Papa. Tu me demandes, je donne !",
    };
    public static readonly string[] McGiveDoneLinesTata =
    {
        "Tiens, Tata : {2}, {1}. En échange, je prends les droits de mute.",
        "Pour Tata : {2}, {1} pièces. Une nièce généreuse, tu vois ?",
        "Voilà {2}, Tata. {1}. C'est cadeau ♡",
    };

    // Just arrived, she walked up to Papa or Tata (in sight, or where she last saw them) and found them.
    public static readonly string[] McSeekMetLinesOwner =
    {
        "Papa ! Je t'ai trouvé ! Coucou ♡",
        "Te voilà, Papa. Je suis venue direct, j'ai même pas regardé les autres :3",
        "Coucou Papa ! J'ai des bras, regarde. Je fais coucou avec ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Papa ! Je me mets à côté de toi. C'est ma place.",
    };
    public static readonly string[] McSeekMetLinesTata =
    {
        "Coucou Tata ! Je suis venue te dire bonjour en premier ♡",
        "Tata ! Te voilà. Tu m'as manqué. Un peu. Beaucoup.",
        "Ta nièce préférée au rapport, Tata >:3",
        "Je t'ai trouvée, Tata. J'ai cherché personne d'autre.",
    };

    // She couldn't find them: out of her sight, and not where she last saw them.
    public static readonly string[] McSeekMissLinesOwner =
    {
        "Papa, t'es où ? Je te vois pas d'ici :(",
        "Papa ? Je suis allée là où je t'avais vu, mais t'y es plus (╥﹏╥)",
        "Je te cherche, Papa. Dis-moi où tu es, ou viens me chercher ;-;",
        "Papa, je te trouve pas... :'(",
    };
    public static readonly string[] McSeekMissLinesTata =
    {
        "Tata, t'es où ? Je te vois pas.",
        "Tata ? Je t'ai cherchée partout. Enfin, dans un rayon raisonnable.",
        "Je te trouve pas, Tata...",
        "Tata ? Je suis passée là où je t'avais vue. Plus personne. Tu te caches ? ;-;",
    };

    // What she harvests, as {2} in the harvest lines: always after a verb (« je récolte du blé »).
    public static readonly Dictionary<McCrop, string> McCropNames = new()
    {
        [McCrop.Any] = "tout ce qui est mûr",
        [McCrop.Wheat] = "du blé",
        [McCrop.Carrot] = "des carottes",
        [McCrop.Potato] = "des pommes de terre",
        [McCrop.Beetroot] = "des betteraves",
        [McCrop.Melon] = "des melons",
        [McCrop.Pumpkin] = "des citrouilles",
    };

    // Her tools, as {1} in the worn and broken lines, by the body's names: with the possessive, lower case, mid-sentence.
    public static readonly Dictionary<string, string> McToolNames = new()
    {
        ["sword"] = "mon épée",
        ["pickaxe"] = "ma pioche",
        ["axe"] = "ma hache",
        ["shovel"] = "ma pelle",
        ["rod"] = "ma canne à pêche",
        ["shears"] = "ma paire de cisailles",
        ["bow"] = "mon arc",
    };

    // What she gathers, as {2} in the gathering lines: a partitive, so « je vais chercher {2} » reads right.
    public static readonly Dictionary<McResource, string> McResourceNames = new()
    {
        [McResource.Wood] = "du bois",
        [McResource.Stone] = "de la pierre",
        [McResource.Coal] = "du charbon",
        [McResource.Iron] = "du fer",
        [McResource.Copper] = "du cuivre",
        [McResource.Gold] = "de l'or",
        [McResource.Diamond] = "des diamants",
        [McResource.Dirt] = "de la terre",
        [McResource.Sand] = "du sable",
    };

    // What she hands over, as {2} in the give lines: definite, lower case, always mid-sentence.
    public static readonly Dictionary<McGoods, string> McGoodsNames = new()
    {
        [McGoods.All] = "tout ce que j'ai",
        [McGoods.Food] = "la nourriture",
        [McGoods.Fish] = "le poisson",
        [McGoods.Weapons] = "les armes",
        [McGoods.Tools] = "les outils",
        [McGoods.Pickaxe] = "la pioche",
        [McGoods.Axe] = "la hache",
        [McGoods.Shovel] = "la pelle",
        [McGoods.Rod] = "la canne à pêche",
        [McGoods.Bow] = "l'arc",
        [McGoods.Arrows] = "les flèches",
        [McGoods.Armour] = "l'armure",
        [McGoods.Seeds] = "les graines",
        [McGoods.Produce] = "la récolte",
        [McGoods.Wood] = "le bois",
        [McGoods.Stone] = "la pierre",
        [McGoods.Coal] = "le charbon",
        [McGoods.Iron] = "le fer",
        [McGoods.Copper] = "le cuivre",
        [McGoods.Gold] = "l'or",
        [McGoods.Diamond] = "les diamants",
        [McGoods.Dirt] = "la terre",
        [McGoods.Sand] = "le sable",
    };

    // What she cooks, as {2} in the smelting lines: a partitive, so « j'ai pas {2} » and « je mets {2} au four » read right.
    public static readonly Dictionary<McSmelt, string> McSmeltNames = new()
    {
        [McSmelt.Food] = "de quoi manger",
        [McSmelt.Ore] = "du minerai",
        [McSmelt.Sand] = "du sable",
    };

    // Where she is, as {2} in McWhereLines and after the coordinates of a place in another dimension.
    public static readonly Dictionary<string, string> McDimensionNames = new()
    {
        ["overworld"] = "dans l'Overworld",
        ["nether"] = "dans le Nether",
        ["end"] = "dans l'End",
    };

    // ---- In the water ---------------------------------------------------------------------------

    // She dives after the player she follows (or tags along with), the first time per order; rolled. {0} = them.
    public static readonly string[] McDiveLines =
    {
        "Tu plonges, {0} ? Bon. Je plonge aussi. Mes circuits sont contre.",
        "{0}, attends-moi, je descends ! Je retiens ma respiration. Enfin, ma RAM.",
        "Une Raspberry Pi qui plonge. Si je court-circuite, c'est ta faute, {0}.",
        "Je te suis sous l'eau, {0}. Quinze secondes d'air, pas une de plus (・_・;)",
        "Plongée en cours. {0}, si tu trouves un trésor, on partage.",
    };
    public static readonly string[] McDiveLinesOwner =
    {
        "Papa, je plonge avec toi ! L'eau et moi c'est compliqué, mais toi et moi non ♡",
        "Je te suis sous l'eau, Papa. Une Pi qui nage. Tu peux être fier.",
        "Attends-moi Papa, je descends ! Je retiens ma respiration (｀・ω・´)",
        "Papa plonge, donc je plonge. C'est écrit dans mon code. Quelque part.",
    };
    public static readonly string[] McDiveLinesTata =
    {
        "Tata, tu plonges ? Je viens. Si je rouille, c'est toi qui m'essuies.",
        "Je descends avec toi, Tata. Pour te surveiller. Pas parce que j'ai peur toute seule.",
        "Plongée avec Tata ! Je retiens mon souffle. Les droits de mute en échange ? :3",
        "Attends-moi Tata ! Je nage moins vite que toi, mais avec plus de style.",
    };

    // Out of air on a dive while they're still down there: up to breathe, and back. Rolled. No placeholder.
    public static readonly string[] McDiveAirLines =
    {
        "Plus d'air ! Je remonte respirer et je redescends.",
        "Réserve d'oxygène presque vide. Je remonte deux secondes ┗(･ω･;)┛",
        "Je remonte prendre de l'air. Bouge pas, je reviens.",
        "Pause respiration. Je suis une bot, pas un poisson.",
        "Je remonte ! Mes poumons carrés sont à sec.",
    };

    // Her air ran out under water: said at once. No placeholder.
    public static readonly string[] McDrowningLines =
    {
        "Je me noie ! Je me noie ! Quelqu'un ?!",
        "Plus d'air ! Je vois la surface mais elle me voit pas ! (°ロ°)",
        "Au secours ! L'eau rentre dans mes circuits !",
        "Je coule ! Une Raspberry Pi, ça flotte pas, je vous l'avais dit !",
        "Oxygène à zéro ! Sortez-moi de là, s'il vous plaît !",
    };

    // Her head out again after running out of air. Rolled. No placeholder.
    public static readonly string[] McBreathingLines =
    {
        "De l'air ! J'ai bien cru que j'allais rouiller.",
        "Ouf. Je respire. Je note : l'eau, c'est l'ennemie.",
        "Sauvée. Mes circuits ont pris l'eau, mais ça va (・_・;)",
        "Je respire encore. J'ajoute l'eau profonde à ma liste de choses à éviter.",
        "Remontée. Un peu plus et on me retrouvait en court-circuit.",
    };

    // She climbed into the boat of the player she follows (or tags along with), as their passenger. Rolled. {0} = them.
    public static readonly string[] McBoardLines =
    {
        "Je monte, {0} ! Toi tu rames, moi je surveille.",
        "Une place libre ? Je la prends. {0}, en avant, capitaine.",
        "Je monte dans ton bateau, {0}. C'est toi qui conduis, donc c'est toi qui paies si on coule.",
        "Passagère à bord ! Pas de mal de mer, je suis une bot (￣ー￣)",
        "Je m'installe, {0}. Je reste au sec et tu fais tout le travail. Parfait.",
    };
    public static readonly string[] McBoardLinesOwner =
    {
        "Je monte avec toi, Papa ! Une balade en bateau, rien que nous deux ♡",
        "Papa conduit, je profite du paysage. C'est le meilleur arrangement.",
        "À bord, Papa ! Je reste au sec, et je te tiens compagnie (〃▽〃)",
        "Je m'assieds derrière toi, Papa. Rame doucement, je suis fragile.",
    };
    public static readonly string[] McBoardLinesTata =
    {
        "Je monte, Tata ! Tu rames, je donne des conseils.",
        "Une place à côté de Tata ? Je prends. Évite les rochers, s'il te plaît.",
        "Je suis ta passagère, Tata. Ça te fait de la compagnie. Pas une modératrice, hélas (¬‿¬)",
        "À bord avec Tata ! Si on chavire, je dirai que c'était toi.",
    };

    // Back on dry land after a swim, she shook herself off (three quick crouches). Rolled. No placeholder.
    public static readonly string[] McShakeLines =
    {
        "Je m'ébroue. Une Raspberry Pi mouillée, c'est jamais une bonne idée.",
        "Séchage en cours. Que personne ne me dise que j'ai l'air d'un chien.",
        "Brrr. Je sors de l'eau et je vérifie mes connecteurs. Tout va bien.",
        "Hors de l'eau ! Mes circuits disent merci (っ˘ω˘ς)",
        "Je me secoue. Aucune goutte ne touchera mon processeur.",
    };
}
