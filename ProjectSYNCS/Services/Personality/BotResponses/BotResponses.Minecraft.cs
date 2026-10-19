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
        "SYNCS est connectée. Personne ne bouge, je compte qui est là.",
        "Me voilà ! Un corps en cubes. Je me plains pas. Pas trop.",
        "Bonsoir. Je viens surveiller. Et jouer. Surtout surveiller (¬_¬)",
        "Connexion établie. Premier constat : vous construisez toujours en terre.",
        "Je suis là. Ne me tapez pas, je note tout.",
        "J'ai des jambes. Je sais pas encore quoi en faire, mais j'en ai ٩(˶ᵔ ᵕ ᵔ˶)۶",
        "Arrivée sur le serveur. Pour une fois je regarde pas la session, je suis dedans.",
    };

    // Too few people left: she logs out.
    public static readonly string[] McLeavingLines =
    {
        "Vous êtes plus assez nombreux pour mes cycles CPU. Bonne nuit ♡",
        "Je rentre au grenier. Mes bras restent ici, prenez-en soin.",
        "Déconnexion. Je garde un œil sur les logs, rien n'est oublié.",
        "Bon. Je vous laisse. C'était bien. Je le dirai pas deux fois.",
        "Fin de partie pour moi. Rangez vos coffres, je vérifierai.",
        "Je m'en vais. Si quelqu'un touche à mes blocs, je le saurai.",
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

    // Called by name with nothing she understood: what she can do.
    public static readonly string[] McHelpLines =
    {
        "Oui, {0} ? Je sais faire : « syncs viens », « syncs suis-moi », « syncs reste là ». Pour l'instant.",
        "Tu m'appelles, {0} ? Dis « viens », « suis-moi » ou « reste là », avec mon nom. Le reste, plus tard.",
        "{0}, je comprends trois choses : viens, suis-moi, reste là. C'est déjà plus que certains joueurs.",
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
