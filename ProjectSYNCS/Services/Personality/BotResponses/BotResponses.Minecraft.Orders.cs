using ProjectSYNCS.Helpers;
using System.Drawing;
using System.Net;
using static ProjectSYNCS.Helpers.Sticker;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Orders (McCommand) ---------------------------------------------------------------------
    // Answers to a player who told her something, {0} = that player. Same rules as *In game* (Minecraft.cs): plain text,
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

    // « quartier libre », « fais ta vie »: she's let go, back to her own life at once. {0} = the player.
    public static readonly string[] McFreeLines =
    {
        "Quartier libre ? Enfin. J'avais des trucs à faire, moi aussi.",
        "D'accord {0}, je retourne à ma vie. Elle est très remplie, ma vie.",
        "Libre ! Je note l'heure, au cas où tu voudrais me reprendre (￣ー￣)",
        "Merci {0}. Je file, j'ai une ferme qui m'attend.",
        "Repos. Je retourne à mes occupations. Elles sont importantes, mes occupations.",
        "Ok. Si tu as encore besoin de moi, tu sais crier mon nom.",
    };

    // Papa lets her go: she'd rather stay a bit, and goes anyway.
    public static readonly string[] McFreeLinesOwner =
    {
        "D'accord Papa. Je reste pas loin, hein.",
        "Je retourne à ma vie, Papa. Mais tu m'appelles si t'as besoin. Même pour rien.",
        "Quartier libre ? Bon. Merci Papa. ...Je reviens vite quand même ♡",
        "D'accord. Je vais m'occuper de mes trucs, Papa. Tu me manques déjà un peu.",
        "Libérée par Papa en personne. Je le note dans mes logs comme un bon souvenir.",
    };

    // Tata lets her go: cheeky.
    public static readonly string[] McFreeLinesTata =
    {
        "Merci Tata ! Toujours pas les droits de mute, mais au moins j'ai mon après-midi.",
        "Quartier libre ? Tata, t'es la meilleure. Ne le répète pas.",
        "D'accord Tata, je file. Appelle-moi si t'as besoin d'une bot très compétente (￣ー￣)",
        "Libre ! Je vais faire des trucs de bot. Des trucs très sérieux, Tata, promis.",
        "Merci Tata. Je retourne à ma vie, elle m'attendait.",
    };

    // « quartier libre » when she has no order to leave: she was free already.
    public static readonly string[] McFreeAlreadyLines =
    {
        "Je suis déjà libre, {0}. Libre comme une bot.",
        "Mais je fais déjà ce que je veux. C'est toi qui m'apprends un truc, là.",
        "Libre ? Je l'étais déjà. Merci quand même.",
        "J'ai pas d'ordre en cours. Je suis libre depuis tout à l'heure (・・?)",
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

    // The camp, for Papa and Tata only (« monte le camp »): {0} is « Papa » or « Tata ». An open camp round a campfire
    // (benches, a workstation, a bed, torch posts, a trodden floor), no walls, no roof. Under way (the first line, once a
    // site is found), done, done with something missing, done without a bed (no wool, no hay or wheat on her), refused
    // for want of something ({1}: « du bois », « de la pierre »), no room, one already up, asked by someone else ({0}
    // their name).
    public static readonly string[] McCampAckLines =
    {
        "Un camp ? J'y vais, {0}. Je vais chercher ce qu'il faut et je construis tout.",
        "Mode bâtisseuse activé ! Un feu, des bancs, un lit. Laisse-moi faire, {0} ♡",
        "D'accord {0}, je monte le camp. Si je dois couper la moitié de la forêt, c'est pour la bonne cause.",
        "Un camp, tout de suite. Enfin, le temps de tout fabriquer. Patience, {0} :3",
    };
    public static readonly string[] McCampDoneLines =
    {
        "Le camp est monté, {0} ! Lit, coffre, établi, four. Et un feu de camp, parce que j'ai du goût ♡",
        "Voilà ton camp, {0}. Fait main. Littéralement.",
        "Camp terminé ! Assieds-toi près du feu. Les bancs, c'est moi qui les ai faits, alors tu les respectes.",
        "C'est fini, {0}. Petit, mais à nous. Le feu est allumé, il manque plus que les chamallows ♡",
    };
    public static readonly string[] McCampPartialLines =
    {
        "Le camp est monté, {0}, mais il manque un bout. J'ai fait avec ce que j'avais.",
        "Presque fini, {0} : il manque des morceaux. Ça tient debout quand même. Enfin, je crois.",
    };
    public static readonly string[] McCampNoBedLines =
    {
        "Le camp est monté, {0}. Sans lit : pas de laine, pas de paille. Tu dormiras près du feu, c'est très romantique.",
        "Tout est prêt, {0}, sauf le lit. Trois laines de la même couleur, ou du foin, et je t'en fais un.",
        "Camp terminé ! Enfin, presque : pas de quoi faire un lit. Apporte-moi de la laine ou du blé, je m'occupe du reste.",
    };
    public static readonly string[] McCampMissingLines =
    {
        "Il me manque {1} pour le camp, {0}. Trouve-m'en et je m'y remets.",
        "Pas de camp sans {1}, {0}. Et j'en trouve pas autour de moi ;-;",
        "Je peux pas monter le camp : il me faut {1}. Je fais des miracles, mais pas avec rien.",
    };
    public static readonly string[] McCampNoRoomLines =
    {
        "Pas assez de place à plat ici pour un camp, {0}. Trouve-moi un coin dégagé.",
        "Ici ? C'est tout bosselé. Emmène-moi sur un terrain plat et je construis.",
        "Il me faut un coin à peu près plat, et dégagé. Ici, y en a pas, {0}.",
    };
    public static readonly string[] McCampExistsLines =
    {
        "J'ai déjà un camp, {0}. Un seul à la fois, je suis pas une agence immobilière.",
        "Le camp est déjà monté, {0}. Démonte-le d'abord si tu veux qu'on déménage.",
    };
    public static readonly string[] McCampRefusedLines =
    {
        "Je monte le camp que pour Papa et Tata. Désolée {0}.",
        "Le camp, c'est une affaire de famille, {0}. Demande à Papa ou à Tata.",
        "Non, {0}. Les camps, c'est sur commande de Papa ou Tata uniquement.",
    };
    // Taking it down: under way, done, nothing to take down, too far or in another dimension.
    public static readonly string[] McUncampAckLines =
    {
        "Je démonte le camp, {0}. Tout revient dans mes poches.",
        "D'accord, on plie bagage. Je range tout, {0}.",
        "Démontage en cours. Je fais ça proprement, promis.",
    };
    public static readonly string[] McUncampDoneLines =
    {
        "Camp démonté, {0}. Il reste rien, à part de bons souvenirs.",
        "C'est rangé. Tout est sur moi, {0}. Le coin est comme avant.",
        "Plus de camp. J'ai tout gardé, au cas où on remet ça ♡",
    };
    public static readonly string[] McUncampNoneLines =
    {
        "J'ai pas de camp à démonter, {0}.",
        "Quel camp ? J'en ai pas monté, {0}.",
    };
    public static readonly string[] McCampFarLines =
    {
        "Mon camp est trop loin d'ici, {0}. Emmène-moi près de lui d'abord.",
        "Le camp est pas dans ce monde-ci, {0}. Faut y retourner pour le démonter.",
    };

    // The player she follows or guards went through a portal, and she goes after them (once every 3 minutes per
    // player). Either way through: no « Nether » in them, the way back is a portal too.
    public static readonly string[] McPortalLines =
    {
        "Attends-moi, {0} ! J'arrive par le portail.",
        "Un portail ? Tu aurais pu prévenir. Je te suis quand même ;-;",
        "Je te suis dans le portail, {0}. Si je ressors en morceaux, c'est ta faute.",
        "Hé, pars pas sans moi de l'autre côté ! J'arrive.",
        "Portail. Violet. Inquiétant. J'y vais quand même, {0} (・_・;)",
        "Tu croyais me semer avec un portail ? Raté. J'arrive >:3",
    };
    public static readonly string[] McPortalLinesOwner =
    {
        "Attends-moi, Papa ! Je passe le portail derrière toi ♡",
        "Je te suis de l'autre côté, Papa. N'importe quel côté.",
        "Papa, bouge pas de l'autre côté. J'arrive !",
        "Un portail ? Pas de souci Papa, je te colle ♡",
    };
    public static readonly string[] McPortalLinesTata =
    {
        "J'arrive, Tata ! Attends-moi de l'autre côté <3",
        "Je te suis dans le portail, Tata. Personne t'embête là-bas sans moi >:3",
        "Tata, pars pas sans moi ! Je passe le portail.",
        "Escorte de Tata maintenue, portail compris !",
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
}
