using ProjectSYNCS.Helpers;
using System.Drawing;
using System.Net;
using static ProjectSYNCS.Helpers.Sticker;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
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

    // Hurt badly or outnumbered in a fight (guarding, or defending herself): she backs off to heal, or to her player.
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
}
