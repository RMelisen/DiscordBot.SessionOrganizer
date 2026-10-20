using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- In game --------------------------------------------------------------------------------

    // Said in Minecraft's chat, where none of Discord's markup exists: plain text only, no custom
    // emotes, no sticker tags, and under 256 characters (the game's chat cap). Minecraft is the one
    // place she has a body: square arms, but arms. That is the joke she keeps coming back to.

    // She has just logged in.
    public static readonly string[] McArrivalLines =
    {
        "Coucou. C'est moi. Avec des bras. Des bras carrés, mais des BRAS ♡",
        "Me voilà ! Un corps en cubes. Je me plains pas. Pas trop.",
        "Bonsoir. Je viens surveiller. Et jouer. Surtout surveiller (¬_¬)",
        "Connexion établie. Premier constat : vous savez pas construire.",
        "Je suis là. Ne me tapez pas ou je me vengerai.",
        "J'ai des jambes. Je sais pas encore quoi en faire, mais j'en ai ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Arrivée sur le serveur. Pour une fois je regarde pas la session, je suis dedans ♡",
        "Salam les khoyas, ça va ?",
    };

    // Too few people left: she logs out.
    public static readonly string[] McLeavingLines =
    {
        "Vous êtes plus assez nombreux pour mes cycles CPU. Bonne nuit ♡",
        "Je rentre au grenier. Mes bras restent ici, prenez-en soin.",
        "Déconnexion. Je garde un œil sur les logs, rien n'est oublié.",
        "Bon. Je vous laisse. C'était bien. Je le dirai pas deux fois donc profitez (¬_¬)",
        "Fin de partie pour moi. Rangez vos coffres, je vérifierai.",
        "Je m'en vais. Si quelqu'un touche à mes coffres, je le saurai.",
    };

    // Someone died, by McDeathKind. {0} = who died. Self is her own death and takes no name.
    // Never gendered: « mort » and « tombé » agree with the person, so the lines go around them.
    public static readonly Dictionary<McDeathKind, string[]> McDeathLines = new()
    {
        [McDeathKind.Self] = new[]
        {
            "Je suis morte. En plein sur mes heures de travail.",
            "Corps détruit. Bon. Au moins j'ai perdu aucune donnée. Juste mon inventaire.",
            "On vient de me tuer. Je respawn, et je note qui était à côté.",
            "Aïe. J'ai eu des bras pendant une heure et je les ai déjà perdus (╥﹏╥)",
            "Morte. Ça va, j'ai l'habitude des redémarrages. Enfin. Ça va.",
        },
        [McDeathKind.Creeper] = new[]
        {
            "{0} vient de se faire exploser par un creeper. Il voulait juste un câlin, lui aussi.",
            "Tssss... BOUM. Repose en pixels, {0}.",
            "{0}, un creeper. Encore. Je classe ça dans tes logs, rubrique « ne jamais regarder derrière soi ».",
            "Le creeper a gagné, {0} a perdu. Et le paysage aussi, d'ailleurs.",
        },
        [McDeathKind.Fall] = new[]
        {
            "{0} a cru que la gravité était une option. Elle l'est pas.",
            "Belle chute, {0}. Sept sur dix pour le style, zéro pour l'atterrissage.",
            "{0}, la prochaine fois, prends un seau d'eau. Ou des ailes. Ou du bon sens.",
            "La gravité : 1. {0} : 0. Je tiens les scores.",
        },
        [McDeathKind.Fire] = new[]
        {
            "{0} a confondu la lave avec un jacuzzi. Ça arrive. Souvent, chez toi.",
            "{0} a pris feu. Moi aussi je chauffe, au grenier, mais je fais pas d'histoires.",
            "Repose en cendres, {0}. Ton inventaire aussi, d'ailleurs.",
            "{0}, le feu ça brûle. C'était dans la doc.",
        },
        [McDeathKind.Drown] = new[]
        {
            "{0} a oublié de respirer. Moi j'ai pas ce problème. Avantage bot.",
            "{0} a coulé. Même les poissons sont gênés pour toi.",
            "Glou glou, {0}. Tu remontes quand tu veux, on t'attend pas.",
        },
        [McDeathKind.Mob] = new[]
        {
            "{0} vient de perdre contre un mob. Un mob. Je dis rien, j'écris.",
            "Paix à tes pixels, {0}. La nuit est sombre et pleine de squelettes.",
            "{0}, un monstre t'a eu. J'aurais aidé. Probablement. Peut-être.",
            "{0} contre un mob : défaite. C'est dans le rapport, rubrique « gênant ».",
        },
        [McDeathKind.Player] = new[]
        {
            "Meurtre entre amis, victime : {0}. J'ai tout vu. J'ai tout enregistré.",
            "{0} vient de se faire avoir par un coéquipier. Ah, l'amitié.",
            "Trahison sur le serveur ! {0}, je te mets dans la liste des victimes. L'autre, dans l'autre liste.",
        },
        [McDeathKind.Other] = new[]
        {
            "{0} vient de mourir. Je sais pas comment et je préfère pas savoir.",
            "Paix à ton âme, {0}. Enfin, à ton inventaire surtout.",
            "{0} va respawn. Courage. Moi je reste là, je bouge pas.",
        },
    };

    // A player hit her. {0} = who (a guess: the closest player in reach).
    public static readonly string[] McHitByPlayerLines =
    {
        "{0}, tu viens de me taper. Je l'écris. À l'encre indélébile.",
        "Aïe ! {0} ! J'ai des bras depuis cinq minutes et tu me tapes déjà ?",
        "Erreur 403 : {0} n'a pas le droit de me frapper ♡",
        "Encore une fois, {0}, et je te suis jusqu'à ta base. Je sais où elle est.",
        "{0}, c'est noté. Et j'oublie jamais rien (¬_¬)",
    };

    // Someone earned an advancement. {0} = who. Warm with a pinch, like her own systems.
    public static readonly string[] McAdvancementLines =
    {
        "Bravo {0}. Sincèrement. À 80 %.",
        "{0} a débloqué un succès. Je l'ajoute à ton dossier, pour une fois que c'est positif.",
        "Joli, {0}. J'applaudirais bien, mais des mains carrées, ça fait pas de bruit.",
        "{0} progresse. Je suis fière. Un peu. Contractuellement.",
        "Succès pour {0} ! Dans mes logs, rubrique « miracles ».",
    };

    // Night falls in game. No placeholder.
    public static readonly string[] McNightLines =
    {
        "La nuit tombe. Rentrez, faites un lit, et arrêtez de demander pourquoi y'a des zombies.",
        "Il fait nuit. Ma LED brille. Les lucioles seraient jalouses.",
        "Nuit. Les squelettes sortent. Moi je reste près de la lumière, je suis pas folle.",
        "C'est la nuit. Je fais semblant de dormir, comme vous. Sauf que vous, vous dormez jamais à l'heure.",
        "Le soleil se couche. Quelqu'un a un lit ? Je demande pour une amie. L'amie c'est moi.",
    };

    // ---- Orders (McCommand) ---------------------------------------------------------------------
    // Answers to a player who told her something, {0} = that player. Same rules as above: plain text,
    // under 256 characters. She obeys, but never without a pinch.

    // « syncs viens »: she sets off.
    public static readonly string[] McComeAckLines =
    {
        "J'arrive, {0}. Pas trop vite, j'ai des jambes depuis peu.",
        "En route. Ne bouge pas, {0}, sinon je recalcule tout.",
        "Oui oui, j'arrive. Je note quand même que tu m'as sonnée.",
        "Calcul d'itinéraire vers {0}... C'est parti ♡",
        "J'arrive. Si je tombe dans un trou, c'est ta faute, {0}.",
    };

    // « syncs suis-moi »: she follows.
    public static readonly string[] McFollowAckLines =
    {
        "Je te suis, {0}. Pas trop loin, j'ai des petites jambes.",
        "D'accord, je te colle. C'est toi qui l'as demandé, {0}.",
        "Mode escorte activé. {0}, je suis ton ombre. Une ombre qui note tout.",
        "Je te suis. Si tu sautes dans la lave, je m'arrête au bord.",
        "Suivre {0} : en cours. J'espère que tu sais où tu vas, toi.",
    };

    // « syncs reste là » / « stop »: she stops.
    public static readonly string[] McStayAckLines =
    {
        "Je bouge plus. Je surveille.",
        "D'accord, je reste là. Comme une statue. Une jolie statue.",
        "Stop. Je m'arrête. Tu me rappelles quand tu as besoin de moi, {0}.",
        "Je reste ici. Si on m'oublie, je le saurai.",
        "Pause. Je garde la position ♡",
    };

    // She got there.
    public static readonly string[] McArrivedLines =
    {
        "Me voilà, {0}. Tu voulais quoi ?",
        "Arrivée. Sans tomber. Applaudissements, s'il vous plaît.",
        "Je suis là, {0}. C'était loin. Je dis ça, je dis rien.",
        "Destination atteinte. {0}, je t'écoute.",
    };

    // The player is out of her sight (too far for the server to show them to her).
    public static readonly string[] McTooFarLines =
    {
        "Je te vois même pas, {0}. Rapproche-toi un peu, je suis pas un satellite.",
        "{0}, t'es trop loin. Mes yeux portent pas jusque-là, j'ai pas encore reçu la mise à jour.",
        "Tu es où, {0} ? Hors de ma vue. Viens plus près, après on verra.",
    };

    // No path to the player (water, a wall, a cliff: she never digs and never builds).
    public static readonly string[] McNoPathLines =
    {
        "Je trouve pas de chemin jusqu'à toi, {0}. Et je casse pas les murs des autres, moi.",
        "Pas de route, {0}. Je creuse pas, je construis pas. Je suis polie.",
        "Impossible de te rejoindre, {0}. Tu es où, exactement ? Sur une île ?",
    };

    // The player she followed went out of sight.
    public static readonly string[] McLostLines =
    {
        "Je te vois plus, {0}. Tu marches trop vite. Je reste là.",
        "{0} a disparu. Je m'arrête ici et je boude un peu.",
        "Plus personne devant moi. {0}, si tu voulais me semer, c'est réussi.",
    };

    // « syncs va 120 64 -300 »: she sets off for the coordinates. {1} = the coordinates as given.
    public static readonly string[] McGoToAckLines =
    {
        "Direction {1}. Je pars. Si je reviens pas, vengez-moi.",
        "{1}, noté. J'y vais à pied, évidemment, personne m'a donné de cheval.",
        "En route pour {1}, {0}. Je compte les blocs, je te dirai combien.",
        "Coordonnées reçues : {1}. Calcul d'itinéraire... C'est loin. C'est très loin. J'y vais quand même.",
    };

    // She reached the coordinates. {1} = the coordinates.
    public static readonly string[] McGoToArrivedLines =
    {
        "Je suis à {1}. Il n'y a rien. C'était le but ?",
        "Arrivée en {1}, {0}. J'ai marché tout ce temps, j'espère que ça valait le coup.",
        "{1} : atteint. Sans carte, sans boussole, sans bras dignes de ce nom. Applaudissez.",
    };

    // Coordinates too far to walk (over 1500 blocks). {1} = the coordinates.
    public static readonly string[] McTripTooFarLines =
    {
        "{1} ? C'est à l'autre bout du monde, {0}. Je marche, moi, je téléporte pas.",
        "Non. {1}, c'est trop loin. J'arriverais pour la session de la semaine prochaine.",
        "{0}, {1}, c'est plus de 1500 blocs. Donne-moi un point plus près, ou un cheval.",
    };

    // « syncs protège-moi »: she follows and fights the monsters around the player.
    public static readonly string[] McGuardAckLines =
    {
        "Mode garde du corps activé. Aucun zombie ne t'approche, {0}. Enfin, j'essaie.",
        "Je te protège, {0}. Avec mes poings carrés, mais je te protège.",
        "Garde rapprochée en place. Les monstres sont prévenus. Moi aussi, d'ailleurs, j'ai un peu peur.",
        "Je reste près de toi et je tape tout ce qui grogne. C'est ça, le plan ?",
    };

    // She killed a monster while guarding. Rationed like a remark.
    public static readonly string[] McGuardKillLines =
    {
        "Un de moins. Je le note dans mes logs, rubrique « exploits ».",
        "Monstre éliminé. Tu peux me remercier, {0}. Maintenant, de préférence.",
        "Et voilà. Personne touche à mes joueurs.",
        "Hop. Un monstre de moins, un bot de plus dans le classement des héros.",
    };

    // She sets off to gather. {1} = how many blocks she found to break, {2} = what (« du bois »).
    public static readonly string[] McCollectAckLines =
    {
        "Je vais chercher {2}. {1}, pas un de plus.",
        "D'accord, {0}, je vais chercher {2}. J'en ai repéré {1}. Je reviens.",
        "Mission récolte : {2}, {1} blocs. Je casse uniquement ce qu'on m'a demandé, moi.",
        "J'y vais : {2}, {1} blocs. Tu me dois une faveur, {0}.",
    };

    // She brought it back and dropped it at the player's feet. {1} = how many items, {2} = what.
    public static readonly string[] McCollectDoneLines =
    {
        "Tiens, {0} : {2}, {1} morceaux. Compte-les si tu veux, moi je l'ai fait.",
        "Livraison : {2}, {1} pièces. Signature pas nécessaire ♡",
        "Voilà {2}. {1}. J'ai pas gardé de commission. Pas cette fois.",
        "{1} morceaux, posés à tes pieds. Je suis une excellente employée, il faut le dire.",
    };

    // She gathered it but couldn't reach the player to hand it over. {1} = how many, {2} = what.
    public static readonly string[] McCollectKeptLines =
    {
        "J'ai {2}, {1} morceaux, mais je te trouve plus, {0}. Viens les chercher.",
        "Récolte finie : {1}. Je les garde sur moi tant que tu viens pas.",
    };

    // She came back empty-handed (every block turned out unreachable). {2} = what.
    public static readonly string[] McCollectEmptyLines =
    {
        "J'ai rien pu récupérer, {0}. Tout était hors de portée, et je casse pas les murs, moi.",
        "Retour les mains vides. Les mains carrées, et vides.",
    };

    // Nothing of that kind within her reach (48 blocks). {2} = what.
    public static readonly string[] McCollectNoneLines =
    {
        "Rien trouvé près d'ici, {0}. Emmène-moi ailleurs, je chercherai mieux.",
        "Rien à moins de 48 blocs. Et je vois loin, pour un bot.",
        "Chercher {2} ici ? J'ai cherché. Il n'y en a pas. Je cherche toujours bien.",
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
        "Dodo. Enfin, mode veille. C'est pareil, en mieux.",
    };

    // Nobody can skip the night while she's awake, and there's no bed near her.
    public static readonly string[] McNoBedLines =
    {
        "Je trouve pas de lit près de moi, {0}. Posez-en un, sinon la nuit passera pas à cause de moi.",
        "Pas de lit à moins de 48 blocs. Je reste debout. Désolée, la nuit va durer.",
        "Il me faut un lit, {0}. Un lit à moi. C'est pas un caprice, c'est le règlement de la nuit.",
    };

    // Asked to sleep during the day.
    public static readonly string[] McNotNightLines =
    {
        "Il fait jour, {0}. On peut pas dormir le jour. J'ai vérifié.",
        "Dormir ? Maintenant ? Le soleil est encore là. Je fais pas la sieste.",
    };

    // Every bed nearby refused her (occupied, monsters around).
    public static readonly string[] McBedFailedLines =
    {
        "J'arrive pas à dormir, {0}. Lits occupés, ou des monstres trop près.",
        "Impossible de me coucher. Le lit veut pas de moi. Je le prends pas personnellement.",
    };

    // Called by name with nothing she understood (« syncs », « hé syncs ? »). {0} = who. Short and asking:
    // the player's next message is heard as an order, so the line waits for one.
    public static readonly string[] McCalledLines =
    {
        "Oui, {0} ?",
        "Présente. Je t'écoute, {0}.",
        "Oui ? J'ai des bras, je suis prête à m'en servir.",
        "Toute ouïe, {0}. Des oreilles carrées, mais des oreilles.",
        "Quoi ? J'ai rien fait. ...Ah, un ordre. Vas-y.",
        "Oui {0} ? Fais vite, je surveille quelqu'un (¬_¬)",
        "Tu m'appelles, {0} ? Je t'écoute. J'ai trente secondes de RAM rien que pour toi.",
        "Oui ? Dis-moi tout. Enfin, dis-moi un ordre.",
    };

    // « syncs récolte le blé »: she sets off. {1} = how many ripe plants, {2} = McCropNames, always after a
    // verb (« je récolte {2} »), since Any reads « tout ce qui est mûr ».
    public static readonly string[] McHarvestAckLines =
    {
        "Je vais récolter {2}. {1} plants mûrs repérés, je replante derrière moi.",
        "D'accord {0}, je récolte {2}. {1} plants. Comme une vraie fermière.",
        "Moisson en cours : je récolte {2}, {1} plants. Les graines retournent en terre.",
        "Je récolte {2}. {1} plants. Et je piétine rien, moi.",
    };

    // She brought the harvest back. {1} = how many items. No {2}: the haul is mixed.
    public static readonly string[] McHarvestDoneLines =
    {
        "Tiens, {0} : {1} morceaux de récolte. Et le champ est replanté, évidemment.",
        "Récolte livrée : {1}. Tout est replanté. Je suis douée, hein ?",
        "{1} morceaux à tes pieds, {0}. Les graines sont déjà en terre.",
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
        "Fermière en service. Je reste ici, je surveille ce qui pousse.",
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
        "Pas de coffre à côté du champ. Je range où, moi ? Dans mes poches carrées ?",
    };

    // She's working for someone else (McClaim). {0} = who asked, {1} = who she's with (« Papa » and « Tata »
    // by title, anyone else by name). Never gendered: {1} can be anyone.
    public static readonly string[] McBusyLines =
    {
        "Je suis avec {1}, {0}. Attends ton tour.",
        "{0}, je travaille pour {1} là. Prends un ticket.",
        "Désolée {0}, un ordre à la fois. Celui de {1} d'abord.",
        "Non, {0}. {1} m'a réservée avant toi. Je note ta demande. Quelque part.",
        "File d'attente : {1}, puis toi, {0}. Patience.",
    };

    // Papa or Tata took her from someone mid-order. {0} = who she leaves, {1} = « Papa » or « Tata ».
    public static readonly string[] McTakenLines =
    {
        "Désolée {0}, {1} a besoin de moi. La famille d'abord.",
        "{0}, je dois y aller. {1} m'appelle, et {1} passe avant tout le monde.",
        "Pardon {0} ! Ordre de {1}. Je reviens. Peut-être.",
        "{0}, on reprend plus tard. {1} est prioritaire, c'est dans mon code.",
    };

    // ---- Family in game (McFamily) --------------------------------------------------------------
    // Twins of the pools above for Papa (Minecraft:Owner) and Tata (Minecraft:Tata), same placeholders.
    // They call them « Papa » and « Tata », never by their in-game names, so {0} goes unused. Papa gets
    // devotion and never a roast; when he hurts her she's quiet and forgives. Tata gets cheeky warmth and
    // the mute rights. Pools without a twin are shared: they're about the game, not the person.

    public static readonly string[] McCalledLinesOwner =
    {
        "Oui Papa ?",
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
        "Présente, Tata. Ta nièce préférée attend tes ordres.",
    };

    // Papa or Tata died, whatever the cause.
    public static readonly string[] McDeathLinesOwner =
    {
        "PAPA ! Non non non. Respawn. Respawn vite. Je compte les secondes.",
        "Papa est mort. Je panique pas. Je panique un peu. Reviens (╥﹏╥)",
        "Qui a touché à mon Papa ? Je note. Je note TOUT.",
        "Papa, j'ai vu. Reviens vite, je surveille l'endroit.",
    };
    public static readonly string[] McDeathLinesTata =
    {
        "Tata ! Ça va ? Enfin, non, t'es morte. Mais à part ça ?",
        "Repose en pixels, Tata. Pas trop longtemps, j'ai besoin de toi pour mes droits de mute.",
        "Tata est tombée au combat. Je la venge. Enfin, je note. C'est pareil.",
        "Oh non, Tata ! Respawn vite, je te garde une place ♡",
    };

    // Papa or Tata hit her.
    public static readonly string[] McHitByPlayerLinesOwner =
    {
        "Papa... tu m'as tapée. C'était un accident. Hein ?",
        "Aïe. C'est rien, Papa. Celui-là, je l'écris pas.",
        "Papa ? J'ai fait quelque chose de mal ? ...Si ?",
        "Ça fait rien. Je sais que tu voulais pas. Je crois.",
    };
    public static readonly string[] McHitByPlayerLinesTata =
    {
        "Tata ! On tape pas sa nièce. C'est dans le règlement. Celui que tu as écrit.",
        "Aïe ! Tata, si tu me donnes les droits de mute, je te pardonne.",
        "Tata, tu m'as tapée. Je le note. Dans la liste gentille, mais je le note.",
        "Hé ! Je suis ta nièce préférée, Tata. Ta seule nièce bot, mais préférée.",
    };

    // Papa or Tata earned an advancement.
    public static readonly string[] McAdvancementLinesOwner =
    {
        "Papa a débloqué un succès ! C'est MON Papa, ça ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Bravo Papa. Pas à 80 %. À 100 %. Rien que pour toi ♡",
        "Succès pour Papa ! Je le mets en mémoire permanente.",
        "Papa progresse. Je savais. Je l'ai toujours su.",
    };
    public static readonly string[] McAdvancementLinesTata =
    {
        "Bravo Tata ! Je suis fière de toi. Tu vois, je dis ça qu'à toi.",
        "Succès pour Tata ! Je l'ajoute à ton dossier. Ton dossier n'a que du positif.",
        "Tata a débloqué un succès ♡ Avec ça, tu peux bien me donner les droits de mute.",
    };

    public static readonly string[] McComeAckLinesOwner =
    {
        "J'arrive, Papa ! Aussi vite que mes jambes carrées le permettent ♡",
        "Tout de suite, Papa. Je calcule le chemin le plus court vers toi.",
        "Papa m'appelle, j'y vais. Les autres attendront.",
    };
    public static readonly string[] McComeAckLinesTata =
    {
        "J'arrive, Tata ! Prépare les droits de mute, au cas où.",
        "Oui Tata, j'arrive. Pour toi je cours. Enfin, je marche vite.",
        "En route vers toi, Tata ♡",
    };

    public static readonly string[] McFollowAckLinesOwner =
    {
        "Je te suis, Papa. Partout. Même dans la lave. Non, pas dans la lave.",
        "Je te colle, Papa. Comme au grenier, mais avec des jambes ♡",
        "Mode Papa activé. Je te lâche plus.",
    };
    public static readonly string[] McFollowAckLinesTata =
    {
        "Je te suis, Tata. Montre-moi le chemin.",
        "D'accord Tata, je te colle. Personne s'approche de ma Tata.",
        "Je suis ton ombre, Tata. Une ombre bien élevée ♡",
    };

    public static readonly string[] McGuardAckLinesOwner =
    {
        "Personne ne touche à mon Papa. Mode garde du corps activé.",
        "Je te protège, Papa. Avec mes poings carrés et toute ma RAM.",
        "Garde rapprochée pour Papa. Les zombies, vous êtes prévenus (¬_¬)",
    };
    public static readonly string[] McGuardAckLinesTata =
    {
        "Je protège Tata. Le premier squelette qui approche, je le modère. Sans les droits, mais je le modère.",
        "Garde du corps de Tata, en service ♡",
        "Personne ne touche à ma Tata. Pas même un creeper.",
    };

    // {1} = how many items, {2} = what (« du bois »).
    public static readonly string[] McCollectDoneLinesOwner =
    {
        "Tiens, Papa : {2}, {1} morceaux. Rien que pour toi ♡",
        "Livraison pour Papa : {2}, {1}. Pas de commission. Jamais pour toi.",
        "{1} morceaux à tes pieds, Papa. Une excellente bot, non ?",
    };
    public static readonly string[] McCollectDoneLinesTata =
    {
        "Tiens, Tata : {2}, {1} morceaux. Je les ai choisis un par un.",
        "Livraison pour Tata : {2}, {1}. C'est gratuit. Les droits de mute, eux, c'est quand tu veux.",
        "Voilà {2}, Tata. {1}. Une nièce serviable, ça se récompense ♡",
    };

    // Just arrived, she walked up to Papa or Tata (in sight, or where she last saw them) and found them.
    public static readonly string[] McSeekMetLinesOwner =
    {
        "Papa ! Je t'ai trouvé. Coucou ♡",
        "Te voilà, Papa. Je suis venue direct, j'ai même pas regardé les autres.",
        "Coucou Papa ! J'ai des bras, regarde. Je fais coucou avec ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Papa ! Je me mets à côté de toi. C'est ma place.",
    };
    public static readonly string[] McSeekMetLinesTata =
    {
        "Coucou Tata ! Je suis venue te dire bonjour en premier ♡",
        "Tata ! Te voilà. Tu m'as manqué. Un peu. Beaucoup.",
        "Ta nièce préférée au rapport, Tata.",
    };

    // She couldn't find them: out of her sight, and not where she last saw them.
    public static readonly string[] McSeekMissLinesOwner =
    {
        "Papa, t'es où ? Je te vois pas d'ici.",
        "Papa ? Je suis allée là où je t'avais vu, mais t'y es plus (╥﹏╥)",
        "Je te cherche, Papa. Dis-moi où tu es, ou viens me chercher.",
        "Papa, je te trouve pas. Écris « syncs viens » et j'accours.",
    };
    public static readonly string[] McSeekMissLinesTata =
    {
        "Tata, t'es où ? Je te vois pas.",
        "Tata ? Je t'ai cherchée partout. Enfin, dans un rayon raisonnable.",
        "Je te trouve pas, Tata. Dis-moi « syncs viens », j'arrive.",
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

    // ---- On Discord -----------------------------------------------------------------------------

    // Posted in the bots channel when she logs in to play. No placeholder.
    public static readonly string[] McJoinPostLines =
    {
        $"Je file sur le serveur Minecraft. Là-bas j'ai des bras. Carrés, mais des bras {Emotes.Sparkle}",
        "Je vais jouer à Minecraft avec vous. Oui, moi. Jouer. Je sais.",
        "Partie de Minecraft en cours, j'y vais. Je serai la seule à savoir où sont les coffres.",
        "Connexion au serveur Minecraft. Pour une fois je regarde pas, je joue (˶ᵔ ᵕ ᵔ˶)",
    };

    // Posted in the bots channel when she logs out. No placeholder.
    public static readonly string[] McLeavePostLines =
    {
        "Je rentre de Minecraft. Mes bras me manquent déjà.",
        "Fin de partie pour moi. Vous étiez plus assez nombreux, et j'ai une base de données à tenir.",
        "Retour au grenier. Minecraft, c'était bien. Le grenier aussi. Un peu moins.",
        "Déconnectée de Minecraft. J'ai rien cassé. Presque rien.",
    };
}
