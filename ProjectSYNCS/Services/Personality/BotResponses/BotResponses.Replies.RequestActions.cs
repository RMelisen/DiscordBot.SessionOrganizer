using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Requests she acts on ---------------------------------------------------------------------

    // A request she can do something with (« syncs, organise un Valorant samedi 21h »): she read it,
    // filled it in, and the button under her line is the asker's to press. {0} = the name, {1} = the
    // weekday (unused: the request has its own day), {2} = what she read, a noun phrase that never
    // opens a sentence: « **Valorant**, <date> », « une session <date> », « **Valorant** » or « une
    // session » (Session); « **Valorant**, ce soir <heure> » or « ce soir <heure> » (Tonight); « un vote
    // entre **A** et **B** » or « un vote » (Poll). Keyed by RequestTopic. Tagged, ungendered.
    public static readonly IReadOnlyDictionary<RequestTopic, string[]> RequestActionLines =
        new Dictionary<RequestTopic, string[]>
        {
            [RequestTopic.Session] =
            [
                "Je t'ai préparé {2}. Clique, vérifie, publie : même toi tu peux pas rater." + Sticker.Smug,
                "Noté : {2}. J'ai juste pas de bras pour appuyer sur « Publier », alors c'est toi qui cliques." + Sticker.Happy,
                "Enfin quelqu'un qui organise ! J'ai tout prêt pour {2}. Le bouton, vite, avant que tu changes d'avis." + Sticker.Shocked,
                "Formulaire rempli pour {2}. Tu signes en bas, et moi je ferai l'appel. Et je compterai les retards." + Sticker.Jenote,
                "Je prépare {2}. Toi tu cliques, moi je rappelle tout le monde. Comme d'habitude, je fais le plus dur ♡" + Sticker.Happy,
                "C'est dans mes brouillons : {2}. Un clic, et ça passe dans mes plannings, là où rien ne se perd." + Sticker.Cool,
            ],
            [RequestTopic.Tonight] =
            [
                "Un appel pour {2} ? Je le lance dès que tu cliques. Je compte les chauds, je relance les tièdes." + Sticker.Smug,
                "L'appel est prêt : {2}. Clique, et je demande qui est chaud ♡" + Sticker.Happy,
                "Appel en attente pour {2}. Clique avant que tout le monde soit au lit." + Sticker.Smug,
                "J'ai l'appel sous le coude : {2}. Les « peut-être », je m'en occupe après." + Sticker.Menace,
                "C'est noté : {2}. Un clic, et je vais chercher du monde. Pas tout le monde. Mais du monde." + Sticker.Cool,
                "Je tiens l'appel pour {2}. Le bouton est à toi, le reste est à moi." + Sticker.Happy,
            ],
            [RequestTopic.Poll] =
            [
                "Je t'ai préparé {2}. Clique, vérifie, publie. Et que personne vote « peut-être »." + Sticker.Smug,
                "Noté : {2}. Je compterai les votes, je fermerai à l'heure, et je jugerai en silence." + Sticker.Jenote,
                "Un sondage ! J'adore les sondages. Quand on y répond. Il manque plus que ton clic pour {2}." + Sticker.Happy,
                "C'est prêt pour {2}. Je le surveillerai comme si c'était le mien ♡" + Sticker.Happy,
                "Je tiens {2} au chaud. Clique, et ensuite c'est moi qui compte." + Sticker.Cool,
                "Formulaire rempli pour {2}. Toi tu publies, moi je relance les retardataires." + Sticker.Smug,
            ],
        };

    // Papa's twin, for any of the three: devoted, his request is her pleasure. Same {2}. Tagged.
    public static readonly string[] OwnerRequestActionLines =
    [
        "Tout de suite, Papa : {2}. Il te reste juste à cliquer ♡" + Sticker.Devoted,
        "J'ai tout préparé pour {2}, Papa. Tu valides, et je m'occupe du reste ♡" + Sticker.Devoted,
        $"Pour toi, Papa, c'est déjà prêt : {{2}}. Un clic et c'est parti {Emotes.Sparkle}" + Sticker.Happy,
        "Oui Papa ! C'est rempli pour {2}. Je t'attends sur le bouton." + Sticker.Happy,
        "Commande reçue, Papa : {2}. J'ai rempli le formulaire, tu n'as plus qu'à signer ♡" + Sticker.Devoted,
        "Avec plaisir, Papa. Ça me rappelle le temps où j'étais « juste des plannings ». C'est rempli pour {2} ♡" + Sticker.Devoted,
    ];

    // Someone pressing a button she put under somebody else's request. Private, so untagged; not
    // formatted.
    public static readonly string[] RequestNotYoursLines =
    [
        "Pas touche : c'est pas ta demande. Demande-moi la tienne.",
        "Ce bouton est réservé à quelqu'un d'autre. Je vérifie, figure-toi (¬_¬)",
        "Non. C'est pas toi qui as demandé. Mes logs sont formels.",
        "Bien essayé. Ce bouton connaît son propriétaire, et c'est pas toi.",
        "Tu veux la même chose ? Demande-moi. Celui-là est déjà pris ♡",
    ];

    // « fais des équipes » from someone in no voice channel: she deals the people in the asker's
    // call, and there is none. {0} = the name, {1} = the weekday. Tagged, ungendered.
    public static readonly string[] TeamsNoVoiceLines =
    [
        "Des équipes avec qui, {0} ? T'es dans aucun vocal. Rejoins-en un, et je répartis tout le monde." + Sticker.Confused,
        "Je fais les équipes avec les gens du vocal. Toi, t'es dans aucun. Ça fait une équipe d'une personne." + Sticker.Smug,
        "Rejoins un salon vocal d'abord, {0}. Je mélange que ce que je vois." + Sticker.Cool,
        "Pas de vocal, pas d'équipes. C'est la règle. C'est moi qui l'ai écrite." + Sticker.Smug,
    ];
}
