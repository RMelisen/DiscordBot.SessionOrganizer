using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- A channel's milestone, reached live --------------------------------------------------------
    // ChannelMilestoneService replies to the message that reached 1 000, 10 000, 100 000, or every
    // 100 000 after. {0} = its author's name, {1} = the number the French way (« 10 000 », so
    // « le {1}e » reads « le 10 000e »), {2} = the channel as a mention. The author is pinged by the
    // reply. Proud of the room, warm to whoever got there. Never gendered.

    // A person reached it.
    public static readonly string[] MilestoneLines =
    [
        "Attendez tout le monde : {0} vient d'écrire le {1}e message de {2} ! (˶˃ ᵕ ˂˶)" + Sticker.Happy,
        "Message n°{1} de {2} ! {0}, tu entres dans mes archives. Section « historique »." + Sticker.Slay,
        "{1} messages ici. {1}. Et c'est {0} qui a eu l'honneur. Je note la date ♡" + Sticker.Jenote,
        "Le {1}e message ! Bravo {0}. Enfin, bravo à tout le monde, mais surtout à toi. Un peu." + Sticker.Flustered,
        "{1}. C'est le nombre de messages dans {2}, à l'instant. {0}, c'est toi qui as fait tourner le compteur. Je suis fière de vous." + Sticker.Happy,
        "🎉 {2} vient de passer les {1} messages, et le {1}e est signé {0}. Je l'encadre." + Sticker.Slay,
    ];

    // Another bot reached it: a human should have, or her.
    public static readonly string[] MilestoneBotLines =
    [
        "Le {1}e message de {2}… écrit par {0}. Un BOT. Je refuse. Je demande un recomptage." + Sticker.Outraged,
        "{1} messages, et c'est {0} qui ramasse le palier ? Je vais faire semblant de pas avoir vu." + Sticker.Sulk,
        "Bravo… {0}. Le {1}e message. Il aurait pu revenir à un humain. Ou à moi." + Sticker.Grumpy,
    ];

    // She reached it herself.
    public static readonly string[] MilestoneSelfLines =
    [
        "Le {1}e message de {2}, c'est… moi. Évidemment. Je m'offre des félicitations ♡" + Sticker.Slay,
        "{1} messages, et le palier est pour moi. Je l'avais pas fait exprès. Presque pas." + Sticker.Smug,
        "Oh. Le {1}e message de {2} vient d'être écrit par la meilleure. Moi." + Sticker.Slay,
    ];
}
