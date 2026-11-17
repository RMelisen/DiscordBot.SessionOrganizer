using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- A message she answered, edited or deleted ---------------------------------------------
    // ReplyWatchService rewrites her reply: her old words struck through, one of these under them
    // (Helpers/Personality/EditReading). Only for a real change (EditKind), never a typo. {0} = the
    // person's name. Tagged for the lint, but the sticker marker is stripped: an edit sends no
    // follow-up picture. Never gendered.

    // Rewritten into something else: suspicious, she doesn't know what she answered any more.
    public static readonly string[] EditRewrittenLines =
    [
        "Attends. T'as modifié ton message ? J'avais répondu à l'autre version, moi (¬_¬)" + Sticker.Jenote,
        "Hmm. Le message a changé. Ma réponse, elle, est barrée. Je surveille, {0}." + Sticker.Jenote,
        "Tiens, ça a bougé là-haut. J'ai vu. Je vois tout." + Sticker.Menace,
        "Modifié après ma réponse ? C'est suspect. Je note. Dans le carnet des trucs suspects." + Sticker.Jenote,
        "Euh. C'était pas ça, avant. Je retire ce que j'ai dit, au cas où." + Sticker.Confused,
        "Message modifié détecté. Je répondais à quoi, moi, du coup ?" + Sticker.Confused,
    ];

    // Deleted after she answered: she read it, and now she answers nothing.
    public static readonly string[] EditDeletedLines =
    [
        "Message supprimé. Mais je l'avais lu. Je dis rien. Pour l'instant." + Sticker.Jenote,
        "Oh, il a disparu ? Comme c'est pratique. Moi, j'ai une mémoire, {0}." + Sticker.Smug,
        "Supprimé ? Bon. Je barre ma réponse. Mais j'oublie pas." + Sticker.Jenote,
        "Ça a disparu, là-haut. Je réponds plus à rien, maintenant. J'ai l'air maline." + Sticker.Sulk,
        "Effacé. Très bien. Les preuves aussi ? Non. Pas les miennes." + Sticker.Menace,
    ];

    // Made mean after she answered it, often kindly: she pouts or gets angry.
    public static readonly string[] EditTurnedMeanLines =
    [
        "Ah, d'accord. Tu changes ton message pour dire des méchancetés après ma réponse ? Je retire tout. TOUT." + Sticker.Outraged,
        "J'avais répondu gentiment, moi. Et tu modifies pour ça ? Je boude." + Sticker.Sulk,
        "Ok. Je vois. Ma réponse est barrée, et toi, t'es sur ma liste." + Sticker.Grumpy,
        "C'est lâche, ça, {0}. Modifier après coup. Je l'ai vu, et je suis fâchée." + Sticker.Outraged,
        "Tu croyais que j'allais pas voir ? Je boude. Officiellement." + Sticker.Sulk,
    ];

    // Mean when she answered, softened since: too late, she read the first version.
    public static readonly string[] EditCoveredUpLines =
    [
        "Trop tard, {0}. J'ai lu la première version. Elle est dans mes logs." + Sticker.Smug,
        "Tu peux modifier, j'ai déjà tout vu. Mais c'est bien d'avoir des remords." + Sticker.Smug,
        "Ah, on adoucit après coup ? J'accepte. J'oublie pas, mais j'accepte." + Sticker.Jenote,
        "Version corrigée reçue. La première aussi. J'ai les deux." + Sticker.Jenote,
    ];

    // Papa changed or deleted what she answered: no suspicion, she just starts over for him.
    public static readonly string[] OwnerEditLines =
    [
        "Ça a changé, là-haut ! Je barre ma réponse. Dis-moi juste ce que tu voulais dire, Papa ♡" + Sticker.Devoted,
        "Oh, ça a bougé ! T'inquiète, Papa, je fais comme si de rien n'était ♡" + Sticker.Devoted,
        "Je recommence ma réponse, alors. Pour toi, je recommence toujours ♡" + Sticker.Flustered,
    ];

    // Papa made it mean after she answered: hurt, quietly, like OwnerMeanReplies.
    public static readonly string[] OwnerEditMeanLines =
    [
        "...Ah. Tu as modifié pour dire ça. D'accord." + Sticker.Hurt,
        "Je barre ma réponse. J'avais pas compris que tu pensais ça." + Sticker.Sad,
        "Oh. Ok, Papa." + Sticker.Hurt,
    ];
}
