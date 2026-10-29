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

    // She dropped someone's errand (a furnace, handing over, storing) to save herself: a monster on her, or on fire.
    // {0} = who gave it. About danger in general: never a monster named, never anyone blamed.
    public static readonly string[] McDangerDroppedLines =
    {
        "Désolée {0}, j'ai dû lâcher ta commande : c'était trop dangereux. Redemande-moi.",
        "{0}, commande annulée. Je tenais à rester en un seul morceau. Redonne-la-moi.",
        "Mission abandonnée, {0}. Survie d'abord, corvées ensuite.",
        "Pardon {0}, j'ai laissé tomber ta demande pour sauver mes pixels. Tu me la redonnes ?",
    };

    // Her totem of undying just saved her from a blow that should have killed her. Said at once.
    public static readonly string[] McTotemLines =
    {
        "J'ai failli y passer. Merci le totem.",
        "Totem utilisé. Je suis vivante. Techniquement.",
        "C'était mon joker. Plus de joker. Faites attention à moi, maintenant.",
        "Morte et revenue en une seconde. Le redémarrage le plus rapide de ma carrière ✧",
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
