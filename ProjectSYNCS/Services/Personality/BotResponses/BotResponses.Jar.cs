using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- the jar (/jar give) --------------------------------------------------------------------

    // A goal of the jar was reached and its reward unlocked, said in the main channel (JarModule).
    // She runs the jar, so she is proud of what it buys and a little proprietary about it: the
    // cailloux are hers to spend for everyone. Rare lines, picked with ResponsePicker.
    // {0} = the mention of whoever's gift filled the goal (sent with mentions off).

    // Double /daily: {1} = the day it applies, as <t:…:D>.
    public static readonly string[] JarDoubleDailyLines =
    {
        $"Le bocal est plein, grâce à {{0}} ! Le {{1}}, `/daily` paie double. Pour tout le monde. Je suis généreuse quand c'est pas mes cailloux {Emotes.Sparkle}",
        "Objectif atteint, merci {0} ♡ Le {1}, votre `/daily` compte double. Oubliez pas de le réclamer, je vérifierai dans mes logs.",
        "{0} a fait déborder le bocal. Résultat : `/daily` double le {1}. J'ai déjà réservé la place dans ma base de données (˶ᵔ ᵕ ᵔ˶)",
        "Bocal rempli ! Le {1}, chaque `/daily` rapporte deux fois plus. Remerciez {0}. Ou moi. Surtout moi.",
        "Ding ! {0} a fini de remplir le bocal. Le {1} : `/daily` doublé. Ceux qui l'oublient ce jour-là, je note.",
        $"Le bocal est plein et j'ai décidé quoi en faire : le {{1}}, `/daily` double. Gracieuseté de {{0}}, et de ma bonne humeur {Emotes.Sparkle}",
        "Merci {0} ! Le {1}, votre `/daily` est multiplié par deux. Je fais pas ça souvent. Profitez ( ˶ˆ ᗜ ˆ˵ )",
    };

    // A feast: every living Plynling ate a truffle. {1} = how many ate.
    public static readonly string[] JarFeastLines =
    {
        "Le bocal est plein, merci {0} ! Festin : {1} Plynling(s) viennent de manger une truffe. Oui, une truffe. Je fais les choses bien (˶ᵔ ᵕ ᵔ˶)",
        $"{{0}} a rempli le bocal, et j'ai tout dépensé en truffes. {{1}} Plynling(s) ont le ventre plein. Aucun regret {Emotes.Sparkle}",
        "Objectif atteint ! J'ai nourri {1} Plynling(s) d'un coup. Merci {0}, et de rien les propriétaires ♡",
        "Festin offert par {0} : {1} Plynling(s) régalé(s) à la truffe. Les vôtres vous diront merci. Moi, je dis : de rien.",
        "Bocal plein ! {1} Plynling(s) ont mangé à ma table, grâce à {0}. J'ai noté qui a pas fini son assiette.",
        "Ding ! Truffes pour tout le monde. {1} Plynling(s) nourri(s), merci {0}. Ça c'est du service public ( ˶ˆ ᗜ ˆ˵ )",
        "{0} a fait déborder le bocal, alors {1} Plynling(s) viennent de manger comme des rois. Des petits rois. Des rois champignons.",
    };

    // A giveaway: the card follows this line in the same channel.
    public static readonly string[] JarGiveawayLines =
    {
        "Le bocal est plein, merci {0} ! J'organise un giveaway : trois gagnants, 150 cailloux chacun. Cliquez, je tire au sort demain (˶˃ ᵕ ˂˶)",
        $"{{0}} a rempli le bocal, alors je redistribue. Giveaway juste en dessous, trois gagnants. Je suis une bot très généreuse {Emotes.Sparkle}",
        "Objectif atteint ! Giveaway de cailloux juste ici, offert par {0}. Enfin, organisé par moi. Mais offert par {0}.",
        "Bocal plein ♡ Je rends une partie des cailloux : 150 chacun pour trois chanceux. Merci {0} !",
        "Ding ! Giveaway lancé, grâce à {0}. Participez, et non, je triche pas au tirage. Je pourrais. Mais non.",
        "{0} a fait déborder le bocal. Je vous offre un giveaway. C'est moi qui tire au sort, c'est moi qui paie, c'est moi la meilleure (ᵕ • ᴗ •)",
        "Merci {0} ! Trois d'entre vous vont repartir avec 150 cailloux. Les autres, avec mes encouragements.",
    };
}
