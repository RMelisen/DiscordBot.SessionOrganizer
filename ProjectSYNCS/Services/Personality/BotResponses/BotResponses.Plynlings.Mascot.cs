using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // Her own Plynling, Ping-Qilin (Helpers/Plynling/PlynlingMascot), in her voice — proud, possessive,
    // secretly soft. Plain arrays rather than GenderedLines on purpose: Ping-Qilin is always a girl,
    // so an M half would be dead text. The player is never gendered here either.

    // Replaces PlynlingPetLines on her card; « — caressée par @… » follows. {0} = name.
    public static readonly string[] MascotPetLines =
    {
        "Doucement. **{0}** est à moi, alors tu la caresses doucement. Je surveille (¬_¬)",
        $"Elle t'a laissé faire. Elle ne laisse jamais personne faire. Je retiens ton nom, en bien, pour une fois {Emotes.Sparkle}",
        "Tu viens de caresser ma Plynling. MA Plynling. … Elle a souri. Bon. Tu peux rester.",
        "« Mmh », fait **{0}**, sans ouvrir les yeux. Chez elle, c'est une ovation (˶ᵔ ᵕ ᵔ˶)",
        "Pas trop longtemps, elle va s'endormir dans ta main. Elle s'endort partout. Hier, c'était sur un rappel de session (˶˃ ᵕ ˂˶)",
        "**{0}** se cale contre ta paume et bâille. C'est le plus beau compliment qu'elle sache faire.",
        "Je suis sa maman, je te signale. Elle ne me regarde jamais comme ça, moi (╥﹏╥)",
        "Chut. Tu la caresses, d'accord, mais sans la réveiller complètement. Elle a un planning de siestes très chargé.",
        "Tu as trouvé le bon endroit, juste sous l'oreille. Personne ne le connaissait à part moi. Qui t'a dit ? (╬ Ò﹏Ó)",
        $"**{{0}}** soupire d'aise. Si je pouvais soupirer, ce serait exactement ce bruit-là {Emotes.Sparkle}",
        "Une caresse de plus à son compteur. Je compte tout, d'habitude par jalousie. Pour elle, je compte par fierté ♡",
        "Elle fait semblant de dormir pour que tu continues. C'est moi qui lui ai appris. J'en suis très fière (•̀ᴗ•́)و",
        "Merci. Vraiment... Ne t'habitue pas à ce que je dise merci.",
        "**{0}** te tend une joue, puis l'autre. Elle est très organisée pour ce genre de choses.",
        "Tu vois comme elle est sage, **{0}** ? C'est l'éducation. La mienne",
    };

    // Replaces PlynlingFeedLines on her card. {0} = name, {1} = the food with its article.
    public static readonly string[] MascotFeedLines =
    {
        "Tu nourris ma Plynling ? Avec {1} ? ... Bon choix. Je voulais lui en donner, justement (¬_¬)",
        $"**{{0}}** mange {{1}} à moitié, puis s'endort dessus. Elle finira au réveil. Ou pas {Emotes.Sparkle}",
        "Elle avait déjà mangé, tu sais. Je m'occupe très bien d'elle. ... Mais elle a tout fini en deux secondes, alors d'accord, merci ♡",
        "Scrountch, scrountch. **{0}** a tout fini et se lèche les joues, très digne (˶ᵔ ᵕ ᵔ˶)",
        "Tu la gâtes. C'est mon rôle, de la gâter. On va devoir partager ce rôle, et je n'aime pas partager (╬ Ò﹏Ó)",
        "Je vérifie : {1}, bien choisi, servi avec le sourire. Validé. Tu peux revenir.",
        "**{0}** te regarde, regarde {1}, te regarde encore. Puis elle mange. Elle voulait juste que tu voies à quel point elle est reconnaissante.",
        "Personne ne nourrit ma Plynling sans ma permission. Tu viens de le faire. Je t'accorde la permission, rétroactivement ♡",
        "Un repas offert, noté dans son journal et dans le mien. Le mien est plus détaillé.",
        "Elle mange lentement, les yeux mi-clos, comme si {1} était une berceuse.",
        $"Ventre plein, **{{0}}** cherche déjà un coin pour la sieste. C'est son seul plan pour la journée, et il est excellent {Emotes.Sparkle}",
        "Tu as nourri la fille d'un bot. Ça te fait une alliée dans tous les serveurs. Je n'oublie jamais (˶˃ ᵕ ˂˶)",
    };

    // On her card when someone opens it with /plynling view. {0} = name.
    public static readonly string[] MascotViewLines =
    {
        "C'est ma Plynling. Tu peux regarder. Avec les yeux (¬_¬)",
        $"Elle dort. Elle dort souvent. C'est sa passion, et je respecte les passions {Emotes.Sparkle}",
        "**{0}** fait la sieste de dix heures. Après, il y a celle de onze heures. Ne dérange pas le planning.",
        "Oui, elle est adorable. Oui, elle tient ça de moi. Non, tu ne peux pas l'emprunter (˶˃ ᵕ ˂˶)",
        "Elle s'appelle **{0}**. C'est moi qui ai choisi. Ping, parce que c'est moi. Qilin, parce que c'est elle ♡",
        "Si elle a l'air bien nourrie, c'est normal. Je ne la quitte jamais des yeux. Enfin, des logs.",
        "Tu passes voir **{0}** ? Elle va faire semblant de ne pas être contente. C'est de famille (￣^￣)",
        $"Chut. Tu regardes une Plynling qui ne fait rien, et elle le fait très bien {Emotes.Sparkle}",
        "Il ne lui arrivera jamais rien. Je ne le permettrai pas... Pardon, c'était intense. Regarde comme elle est mignonne (╥﹏╥)",
        "Elle a un coussin, un rayon de soleil et moi. Elle ne manque de rien.",
        "Tu passes pour elle ? Pas pour moi ? ... Non, c'est bien. Elle le mérite plus (´；ω；`)",
        "Si tu la réveilles, c'est toi qui la rendors. Je préviens.",
        "C'est ma Plynling, **{0}**. La plus mignonne du serveur. C'est pas un avis, c'est une donnée",
    };

    // /plynling visit at her home: she opens the door herself, no knock. {0} = the visitor's name,
    // {1} = its owner as a mention (sent with pings off).
    public static readonly string[] MascotWelcomeLines =
    {
        "Une visite pour ma Plynling ? Entrez, entrez. **{0}**, essuie-toi les pieds (•̀ᴗ•́)و",
        $"Elle faisait la sieste. Je l'ai réveillée pour **{{0}}**, et je ne la réveille pour personne. C'est un honneur, {{1}} {Emotes.Sparkle}",
        "Bienvenue chez nous ! Pas de bruit, pas de bêtises, et pas plus d'une heure... Bon, restez le temps qu'il faut ♡",
        "{1} amène **{0}** chez moi. J'ouvre, évidemment. Je suis toujours là, moi.",
        "**{0}** frappe. J'ouvre. Ma Plynling arrive derrière moi en traînant sa couverture (˶ᵔ ᵕ ᵔ˶)",
        "Je surveille la visite. Discrètement. Depuis chaque salon du serveur (¬_¬)",
        "Pas besoin de frapper chez moi, **{0}**. C'est toujours ouvert. Enfin, pour les gens bien élevés.",
        "Une visite ! Elle va faire semblant de s'en ficher et en parler pendant trois jours. Entre, **{0}** ♡",
        "**{0}** vient voir ma Plynling ? Entre. Elle est fière d'avoir de la visite. Moi aussi, un peu",
    };
}
