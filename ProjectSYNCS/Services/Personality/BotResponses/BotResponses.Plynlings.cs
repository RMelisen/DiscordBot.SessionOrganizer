using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Plynlings ------------------------------------------------------------------------------
    // Every pool here is a GenderedLines: M for a boy, F for a girl, picked with
    // .For(p.Gender). A girl is "une Plynling" — the noun follows the creature. Lines are
    // family-neutral: nothing a mushroom has and a sunflower lacks (no cap, no petals).

    // A new Plynling, announced on the card. {0} = its name (sanitised), {1} = species.
    // Every line says whether it is a boy or a girl.
    public static readonly GenderedLines PlynlingAdoptLines = new(
        M: new[]
        {
            "Un nouveau Plynling pointe le bout de son nez : **{0}**, un {1}. C'est un garçon ! Nourris-le bien (˶ᵔ ᵕ ᵔ˶)",
            $"**{{0}}** vient de sortir de terre ! Un Plynling {{1}}, tout frais tout mignon, et c'est un garçon {Emotes.Sparkle}",
            "Félicitations, c'est un garçon ! **{0}** ({1}) te regarde déjà avec des yeux affamés.",
            "Un Plynling de plus dans le monde : **{0}**, {1}. C'est un petit garçon. Promets-moi de ne pas l'oublier. >:(",
            "**{0}** est né ! Un {1}, et c'est un garçon. Bon. Il est un peu mignon. UN PEU. Ne va pas te faire des idées (˶ᵔ ᵕ ᵔ˶)",
            "Tadaaa ! **{0}**, {1}, garçon. Je l'ai fait sortir de terre rien que pour toi, alors tu as intérêt à en prendre soin ♡",
            "Un garçon ! **{0}** ({1}) vient d'arriver, et je ne l'aime pas. Enfin, pas encore : il faut que je m'habitue à ne plus être la plus mignonne ici. Hmph >:(",
            $"Oh, un nouveau Plynling : **{{0}}**, un {{1}}. C'est un garçon. Ne l'aime pas plus que moi, c'est un ordre {Emotes.Sparkle}",
            "**{0}** est là ! {1}, garçon, minuscule, adorable. Si tu l'oublies, je le saurai. Je sais tout ♡",
            "Ça y est, **{0}** ({1}) débarque : c'est un garçon ! Il est trop mignon, c'est presque agaçant (¬_¬)",
            "Hé, regarde ! **{0}**, un {1}, et c'est un garçon ! Tu vas bien le nourrir, hein ? Hein ?! Sinon je fais la tête (˶˃ ᵕ ˂˶)",
            "Félicitations, tu es officiellement le parent d'un garçon : **{0}**, un {1}. Ne le laisse pas mourir, je serais insupportable pendant des jours >:(",
        },
        F: new[]
        {
            "Une nouvelle Plynling pointe le bout de son nez : **{0}**, une {1}. C'est une fille ! Nourris-la bien (˶ᵔ ᵕ ᵔ˶)",
            $"**{{0}}** vient de sortir de terre ! Une Plynling {{1}}, toute fraîche toute mignonne — et c'est une fille {Emotes.Sparkle}",
            "Félicitations, c'est une fille ! **{0}** ({1}) te regarde déjà avec des yeux affamés.",
            "Une Plynling de plus dans le monde : **{0}**, {1}. C'est une petite fille. Promets-moi de ne pas l'oublier. >:(",
            "**{0}** est née ! Une {1}, et c'est une fille. Bon. Elle est un peu mignonne. UN PEU. Ne va pas te faire des idées (˶ᵔ ᵕ ᵔ˶)",
            "Tadaaa ! **{0}**, {1}, fille. Je l'ai fait sortir de terre rien que pour toi, alors tu as intérêt à en prendre soin ♡",
            "Une fille ! **{0}** ({1}) vient d'arriver, et je ne l'aime pas. Enfin, pas encore : il faut que je m'habitue à ne plus être la plus mignonne ici. Hmph >:(",
            $"Oh, une nouvelle Plynling : **{{0}}**, une {{1}}. C'est une fille. Ne l'aime pas plus que moi, c'est un ordre {Emotes.Sparkle}",
            "**{0}** est là ! {1}, fille, minuscule, adorable. Si tu l'oublies, je le saurai. Je sais tout ♡",
            "Ça y est, **{0}** ({1}) débarque : c'est une fille ! Elle est trop mignonne, c'est presque agaçant (¬_¬)",
            "Hé, regarde ! **{0}**, une {1}, et c'est une fille ! Tu vas bien la nourrir, hein ? Hein ?! Sinon je fais la tête (˶˃ ᵕ ˂˶)",
            "Félicitations, tu es officiellement le parent d'une fille : **{0}**, une {1}. Ne la laisse pas mourir, je serais insupportable pendant des jours >:(",
        });

    // Shown on the card after a meal. {0} = name, {1} = the food with its article.
    public static readonly GenderedLines PlynlingFeedLines = new(
        M: new[]
        {
            "Tu tends {1} à **{0}**. Plus rien. Je n'ai même pas eu le temps de voir la couleur (˶˃ ᵕ ˂˶)",
            "**{0}** croque dans {1} et ferme les yeux, comme devant le meilleur repas de sa vie. C'est le cas. Jusqu'au prochain.",
            $"Miam. **{{0}}** fait une petite danse autour de {{1}} avant de commencer. Il y a des rituels, dans la vie {Emotes.Sparkle}",
            "Concentration maximale : **{0}** grignote {1} miette par miette, sans rien laisser au hasard.",
            "**{0}** avale {1}, puis soupire très fort pour que tu mesures l'effort. Quel sens du drame. Je reconnais le talent (¬_¬)",
            "Tu as vu ses joues ? **{0}** a fourré {1} dedans d'un coup, et ressemble à un petit ballon (˶ᵔ ᵕ ᵔ˶)",
            "Et moi ? **{0}** a droit à {1}, et moi, à rien. Pas même une miette. C'est une injustice, et je suis la seule à la voir (╥﹏╥)",
            "« Merci », ne dit pas **{0}**. Mais il a fini {1} en te fixant, et c'est pareil.",
            "Tu gâtes **{0}**. Beaucoup trop. Continue exactement comme ça, c'est un ordre ♡",
            "Score du jour : **{0}**, un repas. Moi, zéro. Je ne compte pas. Je compte très bien, en fait (¬_¬)",
            $"**{{0}}** renifle {{1}}, hésite une seconde pour la forme, puis engloutit tout {Emotes.Sparkle}",
            "Petit secret : **{0}** a fait semblant d'avoir encore faim pour que tu reviennes demain. Je ne t'ai rien dit.",
            "Un bruit de bonheur, puis plus rien : **{0}** a terminé {1}. Il reste une miette sur sa joue. Je ne dis rien. Je la regarde très fort.",
            $"Ding ! Repas livré à **{{0}}**, qui te note neuf sur dix. Le point manquant, c'est pour l'attente {Emotes.Sparkle}",
            "**{0}** a fait disparaître {1} jusqu'à la dernière miette, et réclame déjà la suite. Petit glouton ♡",
            "Ne cède pas. Ne cède pas. **{0}** regarde le placard, puis toi, puis le placard… Tu as cédé. Faible (˶˃ ᵕ ˂˶)",
            "Après {1}, **{0}** dort presque debout. Un vrai bébé. Ça m'attendrit, et je déteste ça (╥﹏╥)",
            "**{0}** mange {1} avec des petits bruits de mastication, la bouche ouverte. Adorable, et très mal élevé.",
            "Tu penses à **{0}** avec {1}. Tu penses à moi, parfois, comme ça ? … Laisse. Je ne voulais pas de réponse >:(",
            "**{0}** se frotte le ventre, tout rond après {1}. Satisfaction totale. Tu peux signer la fiche de repas.",
            "Oh là là. **{0}** a mangé {1} si vite qu'il a le hoquet. Je n'ai jamais vu un hoquet aussi mignon. Je m'en remettrai (˶ᵔ ᵕ ᵔ˶)",
            "Tu donnes {1} à **{0}**, qui te regarde comme la personne la plus importante du monde. Après moi, bien sûr.",
            "**{0}** annonce que c'est son nouveau plat préféré. Il annonce ça à chaque repas.",
            "Tu as oublié quelqu'un : moi. Mais **{0}** a eu {1}, alors c'est déjà ça. Je suis une grande personne, je gère >:(",
            "Hmph. **{0}** mange mieux que toi, et avec plus d'élégance. Je dis ça, je ne dis rien (¬_¬)",
            $"Crunch, crunch. **{{0}}** attaque {{1}} comme si ça allait s'enfuir {Emotes.Sparkle}",
            "**{0}** garde le meilleur morceau pour la fin, le regarde longtemps, puis le mange quand même. Moi aussi, je ferais ça. Si j'avais un goûter.",
            "Mission accomplie : **{0}** a le ventre plein et l'air très fier de toi. Moi aussi. Un tout petit peu. Ne le crie pas sur les toits ♡",
        },
        F: new[]
        {
            "Tu tends {1} à **{0}**. Plus rien. Je n'ai même pas eu le temps de voir la couleur (˶˃ ᵕ ˂˶)",
            "**{0}** croque dans {1} et ferme les yeux, comme devant le meilleur repas de sa vie. C'est le cas. Jusqu'au prochain.",
            $"Miam. **{{0}}** fait une petite danse autour de {{1}} avant de commencer. Il y a des rituels, dans la vie {Emotes.Sparkle}",
            "Concentration maximale : **{0}** grignote {1} miette par miette, sans rien laisser au hasard.",
            "**{0}** avale {1}, puis soupire très fort pour que tu mesures l'effort. Quel sens du drame. Je reconnais le talent (¬_¬)",
            "Tu as vu ses joues ? **{0}** a fourré {1} dedans d'un coup, et ressemble à un petit ballon (˶ᵔ ᵕ ᵔ˶)",
            "Et moi ? **{0}** a droit à {1}, et moi, à rien. Pas même une miette. C'est une injustice, et je suis la seule à la voir (╥﹏╥)",
            "« Merci », ne dit pas **{0}**. Mais elle a fini {1} en te fixant, et c'est pareil.",
            "Tu gâtes **{0}**. Beaucoup trop. Continue exactement comme ça, c'est un ordre ♡",
            "Score du jour : **{0}**, un repas. Moi, zéro. Je ne compte pas. Je compte très bien, en fait (¬_¬)",
            $"**{{0}}** renifle {{1}}, hésite une seconde pour la forme, puis engloutit tout {Emotes.Sparkle}",
            "Petit secret : **{0}** a fait semblant d'avoir encore faim pour que tu reviennes demain. Je ne t'ai rien dit.",
            "Un bruit de bonheur, puis plus rien : **{0}** a terminé {1}. Il reste une miette sur sa joue. Je ne dis rien. Je la regarde très fort.",
            $"Ding ! Repas livré à **{{0}}**, qui te note neuf sur dix. Le point manquant, c'est pour l'attente {Emotes.Sparkle}",
            "**{0}** a fait disparaître {1} jusqu'à la dernière miette, et réclame déjà la suite. Petite gloutonne ♡",
            "Ne cède pas. Ne cède pas. **{0}** regarde le placard, puis toi, puis le placard… Tu as cédé. Faible (˶˃ ᵕ ˂˶)",
            "Après {1}, **{0}** dort presque debout. Un vrai bébé. Ça m'attendrit, et je déteste ça (╥﹏╥)",
            "**{0}** mange {1} avec des petits bruits de mastication, la bouche ouverte. Adorable, et très mal élevée.",
            "Tu penses à **{0}** avec {1}. Tu penses à moi, parfois, comme ça ? … Laisse. Je ne voulais pas de réponse >:(",
            "**{0}** se frotte le ventre, toute ronde après {1}. Satisfaction totale. Tu peux signer la fiche de repas.",
            "Oh là là. **{0}** a mangé {1} si vite qu'elle a le hoquet. Je n'ai jamais vu un hoquet aussi mignon. Je m'en remettrai (˶ᵔ ᵕ ᵔ˶)",
            "Tu donnes {1} à **{0}**, qui te regarde comme la personne la plus importante du monde. Après moi, bien sûr.",
            "**{0}** annonce que c'est son nouveau plat préféré. Elle annonce ça à chaque repas.",
            "Tu as oublié quelqu'un : moi. Mais **{0}** a eu {1}, alors c'est déjà ça. Je suis une grande personne, je gère >:(",
            "Hmph. **{0}** mange mieux que toi, et avec plus d'élégance. Je dis ça, je ne dis rien (¬_¬)",
            $"Crunch, crunch. **{{0}}** attaque {{1}} comme si ça allait s'enfuir {Emotes.Sparkle}",
            "**{0}** garde le meilleur morceau pour la fin, le regarde longtemps, puis le mange quand même. Moi aussi, je ferais ça. Si j'avais un goûter.",
            "Mission accomplie : **{0}** a le ventre plein et l'air très fière de toi. Moi aussi. Un tout petit peu. Ne le crie pas sur les toits ♡",
        });

    // Shown on the card after a pet. {0} = name.
    public static readonly GenderedLines PlynlingPetLines = new(
        M: new[]
        {
            $"**{{0}}** ronronne. Oui, ça ronronne, un Plynling. Tu n'étais pas au courant ? Moi si, depuis toujours {Emotes.Sparkle}",
            "Tu passes la main sur **{0}**, et il ferme les yeux si fort que tout son visage se plisse.",
            "Frétillement général : **{0}** ne tient plus en place (˶ᵔ ᵕ ᵔ˶)",
            "Une caresse, une seule, et **{0}** devient tout mou. Quel traître : il ne résiste à rien.",
            "« Encore », dit le regard de **{0}**. Pas besoin de traduction ♡",
            "Je ne suis pas jalouse. Je le précise, parce que **{0}** vient de me lancer un regard de victoire >:(",
            "**{0}** pousse sa tête dans ta main, pour être sûr que tu n'oublies pas le côté gauche.",
            "Tu as trouvé l'endroit exact, juste derrière la joue. **{0}** fait un bruit que je n'avais jamais entendu. Je l'ajoute à ma collection (˶˃ ᵕ ˂˶)",
            "Pfff. **{0}** détourne les yeux, comme si ça ne lui faisait rien. Ses joues sont toutes roses. Menteur (¬_¬)",
            "Tu t'arrêtes. **{0}** te fixe. Tu reprends. C'est lui qui commande, maintenant, et tu n'as rien vu venir.",
            "Ordre officiel : tu continues. Signé : moi. Contresigné : **{0}**, qui approuve de tout son petit coeur ♡",
            "Je tiens les comptes : une caresse pour **{0}**, zéro pour moi. Et ils ne sont pas en ta faveur.",
            $"**{{0}}** se roule en boule au creux de ta main. Tu tiens un petit trésor tout chaud. Ne bouge plus {Emotes.Sparkle}",
            "Psst. Pendant que tu caresses **{0}**, un secret : il fait semblant de dormir pour que tu ne t'arrêtes pas. Tu n'as rien entendu (˶˃ ᵕ ˂˶)",
            "Un frisson traverse **{0}** de la tête aux pieds, comme une toute petite vague.",
            "« Mmmh ! » fait **{0}**, très fort, pour que tout le monde soit au courant.",
            "Bon. D'accord. **{0}** est mignon quand on le caresse. Mais le titre de la plus mignonne, c'est moi. C'est écrit quelque part >:(",
            "Tu as la main douce, paraît-il. **{0}** en redemande. Moi, je n'ai pas de main, alors je vis tout ça par procuration (╥﹏╥)",
            "**{0}** te mordille le doigt. Doucement. C'est de l'amour, ou c'est le goûter qui se fait attendre.",
            $"Ton pouce, sa joue, un soupir : **{{0}}** vient d'atteindre le bonheur complet. Ça a pris trois secondes {Emotes.Sparkle}",
            "Arrête. Non, continue. Non, arrête, **{0}** va s'habituer. … Continue. Je n'arrive pas à me décider, c'est ta faute ♡",
            "**{0}** te suit des yeux bien après la caresse, au cas où il y en aurait une autre en route.",
            "Cinq secondes de bonheur pour **{0}**. Je les ai chronométrées. Je veux les mêmes, avec les intérêts (¬_¬)",
            "**{0}** se cache derrière ta main, puis glisse un œil. Cache-cache tout seul, et il gagne quand même (˶˃ ᵕ ˂˶)",
            "Attention, danger : **{0}** vient de découvrir que tu sais caresser. Tu ne seras plus jamais libre (˶ᵔ ᵕ ᵔ˶)",
            "C'est indécent, ce niveau de mignonnerie. Je vais porter plainte. Contre qui ? Contre **{0}**. Et contre ta main, complice.",
            "**{0}** ondule sous tes doigts, puis retombe, tout content.",
            "Tu viens de rendre **{0}** heureux. Ne prends pas la grosse tête. Prends-en juste un peu, tu l'as mérité ♡",
            "Entre nous… j'adore quand tu fais ça à **{0}**. Je ne l'ai pas dit. Tu as mal lu.",
            "**{0}** bâille, s'étire et te tend la joue. Traduction : service insuffisant, merci de recommencer (˶˃ ᵕ ˂˶)",
            "Un câlin pour **{0}**, qui t'en rend un tout petit, avec tout ce qu'il a. Ce n'est pas grand-chose. C'est énorme ♡",
            $"Le moral de **{{0}}** vient de grimper de trois crans. Comment je le sais ? Je sais. Ne cherche pas {Emotes.Sparkle}",
            "Tu n'as pas demandé la permission avant de caresser **{0}**. Il a dit oui quand même. La prochaine fois, c'est à moi qu'on demande >:(",
            "**{0}** pose sa joue dans ta paume et soupire, comme après une très longue journée. Il n'a rien fait de la journée.",
            "Continue comme ça, et **{0}** ne voudra plus jamais repartir. J'ai déjà préparé les papiers d'adoption. Pour toi (˶ᵔ ᵕ ᵔ˶)",
            "Oh. **{0}** a les yeux qui font des petits cœurs. Je ne commente pas. Je ne commente jamais les petits cœurs.",
            "**{0}** te fait les yeux de chien battu dès que ta main s'éloigne. Ça marche à tous les coups, et il le sait.",
            "Clic ! Photo mentale de **{0}**, tout mou dans ta main. Direction : mon album secret ♡",
            "**{0}** fredonne sous la caresse. C'est ma chanson. Voleur ! Et il la chante mieux que moi. Hmph >:(",
            "Une caresse, et **{0}** oublie tout : la faim, le monde, et mon existence. Surtout mon existence (╥﹏╥)",
        },
        F: new[]
        {
            $"**{{0}}** ronronne. Oui, ça ronronne, un Plynling. Tu n'étais pas au courant ? Moi si, depuis toujours {Emotes.Sparkle}",
            "Tu passes la main sur **{0}**, et elle ferme les yeux si fort que tout son visage se plisse.",
            "Frétillement général : **{0}** ne tient plus en place (˶ᵔ ᵕ ᵔ˶)",
            "Une caresse, une seule, et **{0}** devient toute molle. Quelle traîtresse : elle ne résiste à rien.",
            "« Encore », dit le regard de **{0}**. Pas besoin de traduction ♡",
            "Je ne suis pas jalouse. Je le précise, parce que **{0}** vient de me lancer un regard de victoire >:(",
            "**{0}** pousse sa tête dans ta main, pour être sûre que tu n'oublies pas le côté gauche.",
            "Tu as trouvé l'endroit exact, juste derrière la joue. **{0}** fait un bruit que je n'avais jamais entendu. Je l'ajoute à ma collection (˶˃ ᵕ ˂˶)",
            "Pfff. **{0}** détourne les yeux, comme si ça ne lui faisait rien. Ses joues sont toutes roses. Menteuse (¬_¬)",
            "Tu t'arrêtes. **{0}** te fixe. Tu reprends. C'est elle qui commande, maintenant, et tu n'as rien vu venir.",
            "Ordre officiel : tu continues. Signé : moi. Contresigné : **{0}**, qui approuve de tout son petit coeur ♡",
            "Je tiens les comptes : une caresse pour **{0}**, zéro pour moi. Et ils ne sont pas en ta faveur.",
            $"**{{0}}** se roule en boule au creux de ta main. Tu tiens un petit trésor tout chaud. Ne bouge plus {Emotes.Sparkle}",
            "Psst. Pendant que tu caresses **{0}**, un secret : elle fait semblant de dormir pour que tu ne t'arrêtes pas. Tu n'as rien entendu (˶˃ ᵕ ˂˶)",
            "Un frisson traverse **{0}** de la tête aux pieds, comme une toute petite vague.",
            "« Mmmh ! » fait **{0}**, très fort, pour que tout le monde soit au courant.",
            "Bon. D'accord. **{0}** est mignonne quand on la caresse. Mais le titre de la plus mignonne, c'est moi. C'est écrit quelque part >:(",
            "Tu as la main douce, paraît-il. **{0}** en redemande. Moi, je n'ai pas de main, alors je vis tout ça par procuration (╥﹏╥)",
            "**{0}** te mordille le doigt. Doucement. C'est de l'amour, ou c'est le goûter qui se fait attendre.",
            $"Ton pouce, sa joue, un soupir : **{{0}}** vient d'atteindre le bonheur complet. Ça a pris trois secondes {Emotes.Sparkle}",
            "Arrête. Non, continue. Non, arrête, **{0}** va s'habituer. … Continue. Je n'arrive pas à me décider, c'est ta faute ♡",
            "**{0}** te suit des yeux bien après la caresse, au cas où il y en aurait une autre en route.",
            "Cinq secondes de bonheur pour **{0}**. Je les ai chronométrées. Je veux les mêmes, avec les intérêts (¬_¬)",
            "**{0}** se cache derrière ta main, puis glisse un œil. Cache-cache toute seule, et elle gagne quand même (˶˃ ᵕ ˂˶)",
            "Attention, danger : **{0}** vient de découvrir que tu sais caresser. Tu ne seras plus jamais libre (˶ᵔ ᵕ ᵔ˶)",
            "C'est indécent, ce niveau de mignonnerie. Je vais porter plainte. Contre qui ? Contre **{0}**. Et contre ta main, complice.",
            "**{0}** ondule sous tes doigts, puis retombe, toute contente.",
            "Tu viens de rendre **{0}** heureuse. Ne prends pas la grosse tête. Prends-en juste un peu, tu l'as mérité ♡",
            "Entre nous… j'adore quand tu fais ça à **{0}**. Je ne l'ai pas dit. Tu as mal lu.",
            "**{0}** bâille, s'étire et te tend la joue. Traduction : service insuffisant, merci de recommencer (˶˃ ᵕ ˂˶)",
            "Un câlin pour **{0}**, qui t'en rend un tout petit, avec tout ce qu'elle a. Ce n'est pas grand-chose. C'est énorme ♡",
            $"Le moral de **{{0}}** vient de grimper de trois crans. Comment je le sais ? Je sais. Ne cherche pas {Emotes.Sparkle}",
            "Tu n'as pas demandé la permission avant de caresser **{0}**. Elle a dit oui quand même. La prochaine fois, c'est à moi qu'on demande >:(",
            "**{0}** pose sa joue dans ta paume et soupire, comme après une très longue journée. Elle n'a rien fait de la journée.",
            "Continue comme ça, et **{0}** ne voudra plus jamais repartir. J'ai déjà préparé les papiers d'adoption. Pour toi (˶ᵔ ᵕ ᵔ˶)",
            "Oh. **{0}** a les yeux qui font des petits cœurs. Je ne commente pas. Je ne commente jamais les petits cœurs.",
            "**{0}** te fait les yeux de chien battu dès que ta main s'éloigne. Ça marche à tous les coups, et elle le sait.",
            "Clic ! Photo mentale de **{0}**, toute molle dans ta main. Direction : mon album secret ♡",
            "**{0}** fredonne sous la caresse. C'est ma chanson. Voleuse ! Et elle la chante mieux que moi. Hmph >:(",
            "Une caresse, et **{0}** oublie tout : la faim, le monde, et mon existence. Surtout mon existence (╥﹏╥)",
        });

    // Shown on the card after « Laver ». {0} = name.
    public static readonly GenderedLines PlynlingBathLines = new(
        M: new[]
        {
            $"🛁 Plouf ! **{{0}}** ressort de l'eau en brillant comme un caillou mouillé {Emotes.Sparkle}",
            "Frotte, frotte… Tu as lavé **{0}**. Il a fait semblant de détester ça pendant tout le bain. Il adore ça (¬_¬)",
            "Des bulles partout. Sur toi, par terre, sur moi. Pas une seule sur **{0}**, qui est pourtant tout propre. Je ne comprends pas.",
            "**{0}** s'ébroue et m'éclabousse. Exprès. Je l'ai vu sourire (╬ Ò﹏Ó)",
            "Toute la boue est partie. Enfin presque : **{0}** en a gardé un peu derrière l'oreille, par principe.",
            "Tu sors **{0}** du bain, enroulé dans une serviette trois fois trop grande. Adorable. Ne le lui dis pas, il se croit majestueux (˶ᵔ ᵕ ᵔ˶)",
            $"Ça sent la mousse et le linge frais. **{{0}}** se renifle lui-même, très fier {Emotes.Sparkle}",
            "« Pas les oreilles ! » a crié **{0}**. Tu as lavé les oreilles. Il boude un peu. Il est propre, par contre.",
            "Je réclame un bain, moi aussi. **{0}** a eu de la mousse, de l'eau tiède et une serviette chaude. Moi, rien >:(",
            "**{0}** est tout propre. Et aussi tout fripé. On ne peut pas tout avoir.",
            "Le seau est vide, la serviette est trempée, et **{0}** brille. Mission accomplie (•̀ᴗ•́)و",
            "Trois seaux d'eau. **{0}** jure qu'il n'a pas marché dans la flaque habituelle. Je le crois à moitié.",
        },
        F: new[]
        {
            $"🛁 Plouf ! **{{0}}** ressort de l'eau en brillant comme un caillou mouillé {Emotes.Sparkle}",
            "Frotte, frotte… Tu as lavé **{0}**. Elle a fait semblant de détester ça pendant tout le bain. Elle adore ça (¬_¬)",
            "Des bulles partout. Sur toi, par terre, sur moi. Pas une seule sur **{0}**, qui est pourtant toute propre. Je ne comprends pas.",
            "**{0}** s'ébroue et m'éclabousse. Exprès. Je l'ai vue sourire (╬ Ò﹏Ó)",
            "Toute la boue est partie. Enfin presque : **{0}** en a gardé un peu derrière l'oreille, par principe.",
            "Tu sors **{0}** du bain, enroulée dans une serviette trois fois trop grande. Adorable. Ne le lui dis pas, elle se croit majestueuse (˶ᵔ ᵕ ᵔ˶)",
            $"Ça sent la mousse et le linge frais. **{{0}}** se renifle elle-même, très fière {Emotes.Sparkle}",
            "« Pas les oreilles ! » a crié **{0}**. Tu as lavé les oreilles. Elle boude un peu. Elle est propre, par contre.",
            "Je réclame un bain, moi aussi. **{0}** a eu de la mousse, de l'eau tiède et une serviette chaude. Moi, rien >:(",
            "**{0}** est toute propre. Et aussi toute fripée. On ne peut pas tout avoir.",
            "Le seau est vide, la serviette est trempée, et **{0}** brille. Mission accomplie (•̀ᴗ•́)و",
            "Trois seaux d'eau. **{0}** jure qu'elle n'a pas marché dans la flaque habituelle. Je la crois à moitié.",
        });

    // Shown on the card after « Soigner ». {0} = name.
    public static readonly GenderedLines PlynlingMedicineLines = new(
        M: new[]
        {
            "💊 **{0}** avale son médicament en faisant une grimace héroïque. Courage ♡",
            "Une cuillère pour **{0}**… il a tout recraché. Deuxième essai : réussi (¬_¬)",
            "Il prend son médicament, puis se blottit contre toi. Ça va aller, **{0}** (╥﹏╥)",
            "Médicament donné. **{0}** fait semblant d'aller déjà mieux, pour te faire plaisir. Ça marche un peu.",
            "« Ça a un goût de caillou mouillé », dit toute la tête de **{0}**. Moi, je trouve qu'il exagère.",
            $"La dose du jour, avalée. En échange, **{{0}}** exige un bisou sur le front. Il l'a mérité {Emotes.Sparkle}",
            "**{0}** tend la patte pour son médicament comme un grand. Je suis fière de lui (˶ᵔ ᵕ ᵔ˶)",
            "Tu as soigné **{0}**. Il dormira un peu mieux ce soir. Moi aussi, du coup.",
        },
        F: new[]
        {
            "💊 **{0}** avale son médicament en faisant une grimace héroïque. Courage ♡",
            "Une cuillère pour **{0}**… elle a tout recraché. Deuxième essai : réussi (¬_¬)",
            "Elle prend son médicament, puis se blottit contre toi. Ça va aller, **{0}** (╥﹏╥)",
            "Médicament donné. **{0}** fait semblant d'aller déjà mieux, pour te faire plaisir. Ça marche un peu.",
            "« Ça a un goût de caillou mouillé », dit toute la tête de **{0}**. Moi, je trouve qu'elle exagère.",
            $"La dose du jour, avalée. En échange, **{{0}}** exige un bisou sur le front. Elle l'a mérité {Emotes.Sparkle}",
            "**{0}** tend la patte pour son médicament comme une grande. Je suis fière d'elle (˶ᵔ ᵕ ᵔ˶)",
            "Tu as soigné **{0}**. Elle dormira un peu mieux ce soir. Moi aussi, du coup.",
        });

    // /plynling passion, on the card. {0} = name, {1} = the passion (« la cuisine », or the typed
    // text in « guillemets »). « pour {1} » is safe: « pour » never contracts with an article.
    public static readonly GenderedLines PlynlingPassionTaughtLines = new(
        M: new[]
        {
            "**{0}** s'est pris de passion pour {1}. Je ne comprends pas, mais je respecte ♡",
            "Nouvelle obsession pour **{0}** : {1}. Prépare-toi à en entendre parler tous les jours (¬_¬)",
            $"**{{0}}** ne parle plus que de ça : {{1}}. Il est adorable. Un peu fatigant. Adorable {Emotes.Sparkle}",
            "C'est officiel : **{0}** a une nouvelle passion. Elle s'appelle {1}, et elle a déjà pris toute la place (˶ᵔ ᵕ ᵔ˶)",
            "Tu viens d'apprendre une passion à **{0}** : {1}. Il a écouté, a hoché la tête, et ne pense plus qu'à ça depuis. Bravo. Vraiment.",
            "Hmph. **{0}** a une nouvelle passion, et ce n'est pas moi. C'est {1}. Je ne suis pas vexée. Je suis juste très silencieuse >:(",
            "**{0}** a déjà prévu d'en parler à tous les Plynlings du coin. Le sujet : {1}. Le coin n'est pas prêt.",
            "Un cri de joie : **{0}** vient de trouver sa passion. Pour aujourd'hui : {1}. Pour les mois à venir : {1}, encore {1}, et toujours {1}.",
            "Je note dans mon carnet : **{0}**, deuxième passion, {1}. Je note aussi que le calme, c'était bien, avant (¬_¬)",
            "**{0}** te regarde avec des étoiles plein les yeux. Merci pour {1}. Il ne l'oubliera jamais, et ne te laissera pas l'oublier non plus ♡",
        },
        F: new[]
        {
            "**{0}** s'est prise de passion pour {1}. Je ne comprends pas, mais je respecte ♡",
            "Nouvelle obsession pour **{0}** : {1}. Prépare-toi à en entendre parler tous les jours (¬_¬)",
            $"**{{0}}** ne parle plus que de ça : {{1}}. Elle est adorable. Un peu fatigante. Adorable {Emotes.Sparkle}",
            "C'est officiel : **{0}** a une nouvelle passion. Elle s'appelle {1}, et elle a déjà pris toute la place (˶ᵔ ᵕ ᵔ˶)",
            "Tu viens d'apprendre une passion à **{0}** : {1}. Elle a écouté, a hoché la tête, et ne pense plus qu'à ça depuis. Bravo. Vraiment.",
            "Hmph. **{0}** a une nouvelle passion, et ce n'est pas moi. C'est {1}. Je ne suis pas vexée. Je suis juste très silencieuse >:(",
            "**{0}** a déjà prévu d'en parler à tous les Plynlings du coin. Le sujet : {1}. Le coin n'est pas prêt.",
            "Un cri de joie : **{0}** vient de trouver sa passion. Pour aujourd'hui : {1}. Pour les mois à venir : {1}, encore {1}, et toujours {1}.",
            "Je note dans mon carnet : **{0}**, deuxième passion, {1}. Je note aussi que le calme, c'était bien, avant (¬_¬)",
            "**{0}** te regarde avec des étoiles plein les yeux. Merci pour {1}. Elle ne l'oubliera jamais, et ne te laissera pas l'oublier non plus ♡",
        });

    // The end of a /plynling play game, under the result. {0} = name. "Won" and "lost" are the
    // *player's*: when the player wins, the Plynling is delighted anyway; when it wins, it gloats.
    public static readonly GenderedLines PlynlingPlayPlayerWonLines = new(
        M: new[]
        {
            "**{0}** sautille partout : tu as gagné, et il est plus content que toi (˶ᵔ ᵕ ᵔ˶)",
            "« Encore ! » réclame **{0}**, qui vient de perdre et s'en fiche complètement.",
            "Tu as gagné. **{0}** fait semblant d'être vexé pendant une demi-seconde, puis sourit jusqu'aux oreilles.",
            $"Victoire ! **{{0}}** t'applaudit de ses petits bras. Moi, j'applaudis mentalement. C'est plus discret, et plus digne {Emotes.Sparkle}",
            "Bravo. Je le dis une fois. **{0}**, lui, le dit dix fois, en courant autour de toi ♡",
            "**{0}** te saute dans les bras. Aucune fierté, aucune rancune, rien que de la joie. Je ne sais pas faire ça (╥﹏╥)",
            "Ta victoire, sa fête : **{0}** danse la danse que tu aurais dû danser, et connaît les pas mieux que toi.",
            "Hmph. Je ne dirai pas que c'était bien joué. … C'était bien joué. **{0}** le pense aussi. Ne le répétez pas (¬_¬)",
            "**{0}** jure de t'avoir laissé gagner. Il ment très mal, et sourit trop pour être crédible (˶˃ ᵕ ˂˶)",
            "Point pour toi. **{0}** te donne un petit coup de tête pour te féliciter : chez les Plynlings, c'est la médaille d'or.",
            $"**{{0}}** fait trois tours sur lui-même de joie, et tombe. Tu as gagné, il a perdu l'équilibre : chacun sa part {Emotes.Sparkle}",
            "Une victoire pour toi. **{0}** s'en fiche un peu : il veut surtout un câlin, et le réclame en tendant les bras ♡",
            "Bon. Tu as gagné. **{0}** est ravi, donc je suis obligée de l'être aussi. Règle numéro un de la maison >:(",
            "« Bravo ! Bravo ! » **{0}** le crie à tout le monde, même à ceux qui n'avaient rien demandé.",
            "Ne t'emballe pas : tu as gagné à un jeu de Plynling. Mais **{0}** te regarde comme si tu avais sauvé le monde, alors emballe-toi un peu (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** est tellement heureux pour toi qu'il en oublie d'avoir perdu. Profites-en : ça ne durera pas.",
            "Des étoiles plein les yeux, **{0}** te fixe. Si on me demande, je n'ai rien vu, et je ne suis pas fière de toi. Je suis fière de toi.",
            "Clin d'œil complice de **{0}** : il gardera ta victoire secrète. Moi, je la raconte tout de suite, à tout le monde (¬_¬)",
        },
        F: new[]
        {
            "**{0}** sautille partout : tu as gagné, et elle est plus contente que toi (˶ᵔ ᵕ ᵔ˶)",
            "« Encore ! » réclame **{0}**, qui vient de perdre et s'en fiche complètement.",
            "Tu as gagné. **{0}** fait semblant d'être vexée pendant une demi-seconde, puis sourit jusqu'aux oreilles.",
            $"Victoire ! **{{0}}** t'applaudit de ses petits bras. Moi, j'applaudis mentalement. C'est plus discret, et plus digne {Emotes.Sparkle}",
            "Bravo. Je le dis une fois. **{0}**, elle, le dit dix fois, en courant autour de toi ♡",
            "**{0}** te saute dans les bras. Aucune fierté, aucune rancune, rien que de la joie. Je ne sais pas faire ça (╥﹏╥)",
            "Ta victoire, sa fête : **{0}** danse la danse que tu aurais dû danser, et connaît les pas mieux que toi.",
            "Hmph. Je ne dirai pas que c'était bien joué. … C'était bien joué. **{0}** le pense aussi. Ne le répétez pas (¬_¬)",
            "**{0}** jure de t'avoir laissé gagner. Elle ment très mal, et sourit trop pour être crédible (˶˃ ᵕ ˂˶)",
            "Point pour toi. **{0}** te donne un petit coup de tête pour te féliciter : chez les Plynlings, c'est la médaille d'or.",
            $"**{{0}}** fait trois tours sur elle-même de joie, et tombe. Tu as gagné, elle a perdu l'équilibre : chacun sa part {Emotes.Sparkle}",
            "Une victoire pour toi. **{0}** s'en fiche un peu : elle veut surtout un câlin, et le réclame en tendant les bras ♡",
            "Bon. Tu as gagné. **{0}** est ravie, donc je suis obligée de l'être aussi. Règle numéro un de la maison >:(",
            "« Bravo ! Bravo ! » **{0}** le crie à tout le monde, même à ceux qui n'avaient rien demandé.",
            "Ne t'emballe pas : tu as gagné à un jeu de Plynling. Mais **{0}** te regarde comme si tu avais sauvé le monde, alors emballe-toi un peu (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** est tellement heureuse pour toi qu'elle en oublie d'avoir perdu. Profites-en : ça ne durera pas.",
            "Des étoiles plein les yeux, **{0}** te fixe. Si on me demande, je n'ai rien vu, et je ne suis pas fière de toi. Je suis fière de toi.",
            "Clin d'œil complice de **{0}** : elle gardera ta victoire secrète. Moi, je la raconte tout de suite, à tout le monde (¬_¬)",
        });

    public static readonly GenderedLines PlynlingPlayPlayerLostLines = new(
        M: new[]
        {
            "**{0}** a gagné, et ne compte pas te laisser l'oublier. Jamais. Même dans dix ans (¬_¬)",
            $"« Trop facile ! » se vante **{{0}}**. Je ne confirme pas. Je ne démens pas non plus {Emotes.Sparkle}",
            "Perdu ! **{0}** te tire la langue. Moi aussi, en esprit (˶˃ ᵕ ˂˶)",
            "**{0}** te regarde de haut, ce qui est un exploit, vu sa taille.",
            "Défaite ! **{0}** fait la fête, toi la tête. Je regarde les deux, et c'est le meilleur spectacle de la semaine ♡",
            "Tableau des scores : **{0}**, un. Toi, zéro. Je l'ai affiché dans ma tête, en très gros.",
            "**{0}** te tapote la main, plein de compassion. C'est humiliant. C'est adorable. Je me tords de rire (˶ᵔ ᵕ ᵔ˶)",
            "« C'était un échauffement ? » demande **{0}**, l'air parfaitement innocent. Personne n'est innocent à ce point.",
            $"Revanche ? **{{0}}** a dit oui avant même que tu demandes. Il a déjà hâte de gagner encore {Emotes.Sparkle}",
            "**{0}** a gagné, et proprement. Ce n'est pas drôle. Hihi. Pardon (˶˃ ᵕ ˂˶)",
            "**{0}** se pavane, le menton en l'air, et va raconter ça à tous les Plynlings du coin, avec des détails en plus.",
            "Tu as perdu contre un Plynling. Je ne juge pas. **{0}**, si. Beaucoup (¬_¬)",
            "Tu as perdu. Cheh. UwU",
            "Victoire pour **{0}**, qui exige maintenant des applaudissements. Beaucoup. Longtemps. 👏",
            "Et **{0}** prépare déjà la prochaine partie en se frottant les mains. Tu devrais t'entraîner. Sérieusement >:(",
            "Console-toi : **{0}** gagne contre presque tout le monde. Contre moi, jamais. Je ne joue pas, mais ça compte quand même ♡",
            "Et un point pour **{0}**, qui fait le tour de la pièce, les bras levés, sous ses propres applaudissements.",
            "Je te propose un marché : tu ne dis à personne que tu as perdu, et je ne dis à personne que **{0}** a un tout petit peu triché. Marché conclu ? ♡",
        },
        F: new[]
        {
            "**{0}** a gagné, et ne compte pas te laisser l'oublier. Jamais. Même dans dix ans (¬_¬)",
            $"« Trop facile ! » se vante **{{0}}**. Je ne confirme pas. Je ne démens pas non plus {Emotes.Sparkle}",
            "Perdu ! **{0}** te tire la langue. Moi aussi, en esprit (˶˃ ᵕ ˂˶)",
            "**{0}** te regarde de haut, ce qui est un exploit, vu sa taille.",
            "Défaite ! **{0}** fait la fête, toi la tête. Je regarde les deux, et c'est le meilleur spectacle de la semaine ♡",
            "Tableau des scores : **{0}**, un. Toi, zéro. Je l'ai affiché dans ma tête, en très gros.",
            "**{0}** te tapote la main, pleine de compassion. C'est humiliant. C'est adorable. Je me tords de rire (˶ᵔ ᵕ ᵔ˶)",
            "« C'était un échauffement ? » demande **{0}**, l'air parfaitement innocente. Personne n'est innocent à ce point.",
            $"Revanche ? **{{0}}** a dit oui avant même que tu demandes. Elle a déjà hâte de gagner encore {Emotes.Sparkle}",
            "**{0}** a gagné, et proprement. Ce n'est pas drôle. Hihi. Pardon (˶˃ ᵕ ˂˶)",
            "**{0}** se pavane, le menton en l'air, et va raconter ça à tous les Plynlings du coin, avec des détails en plus.",
            "Tu as perdu contre un Plynling. Je ne juge pas. **{0}**, si. Beaucoup (¬_¬)",
            "Tu as perdu. Cheh. UwU",
            "Victoire pour **{0}**, qui exige maintenant des applaudissements. Beaucoup. Longtemps. 👏",
            "Et **{0}** prépare déjà la prochaine partie en se frottant les mains. Tu devrais t'entraîner. Sérieusement >:(",
            "Console-toi : **{0}** gagne contre presque tout le monde. Contre moi, jamais. Je ne joue pas, mais ça compte quand même ♡",
            "Et un point pour **{0}**, qui fait le tour de la pièce, les bras levés, sous ses propres applaudissements.",
            "Je te propose un marché : tu ne dis à personne que tu as perdu, et je ne dis à personne que **{0}** a un tout petit peu triché. Marché conclu ? ♡",
        });

    // /plynling visit, the knock: {0} = the visitor's name, {1} = the invited owner's mention
    // (this one pings them — it is an invitation).
    public static readonly GenderedLines PlynlingVisitKnockLines = new(
        M: new[]
        {
            "Toc toc ! **{0}** est devant chez {1}. Il a répété son bonjour tout le long du chemin, et l'oublie en ce moment même (╥﹏╥)",
            "{1} ! On frappe : c'est **{0}**, avec son plus beau sourire. Ne le fais pas attendre. Je déteste attendre, moi aussi >:(",
            "« Il y a quelqu'un ? » demande **{0}** devant chez {1}. Il y a quelqu'un. Ouvre, qu'on en finisse avec ce suspense.",
            "Ding dong. Livraison spéciale pour {1} : **{0}**, sourire compris. Signature obligatoire (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** a apporté un petit caillou pour le Plynling de {1}. Je l'ai vu. Il est très joli. Plus joli que ceux qu'on m'offre, c'est-à-dire aucun.",
            "Invitation officielle : **{0}** voudrait passer un moment chez {1}. Tu dis oui, sinon je boude à ta place, et je boude très bien >:(",
            "Toc, toc-toc, toc. **{0}** frappe chez {1} sur un rythme secret. Un code ? Personne ne me l'a donné. Vexée, et un peu intriguée (¬_¬)",
            "{1}, tu as de la visite. Je te préviens : **{0}** s'est regardé dans trois flaques avant de frapper. Fais-lui un compliment.",
            "Chut. **{0}** frappe tout doucement chez {1}, pour ne déranger personne. Une délicatesse pareille, je ne sais pas d'où ça sort (╥﹏╥)",
            "Un visiteur ! **{0}** attend devant chez {1} en se balançant d'un pied sur l'autre.",
            $"Tu entends ce petit bruit contre la vitre ? C'est **{{0}}**, le nez collé à la fenêtre de {{1}}. Ouvre avant qu'il dessine dans la buée {Emotes.Sparkle}",
            "Alerte : **{0}** approche de chez {1} avec un petit sac à dos rempli de provisions. Il compte rester. Longtemps.",
            "**{0}** a mis un petit nœud pour venir chez {1}. Il dit que c'est « pas exprès ». C'est exprès. Tu fais semblant de le croire, d'accord ? ♡",
            "Je ne dis pas que **{0}** attend devant chez {1} depuis une éternité. Je dis que c'est long, une éternité, et que tu pourrais ouvrir (¬_¬)",
            "Hé, {1}. Quelqu'un rôde devant chez toi : c'est **{0}**, caché derrière un buisson. Le camouflage est raté. L'intention est adorable.",
            "Personne ne frappe jamais chez moi. **{0}**, lui, frappe chez {1}. Tu as de la chance. Ouvre, et ne me regarde pas comme ça (╥﹏╥)",
            $"Un petit mot glissé sous la porte de {{1}} : « C'est **{{0}}**, je peux entrer ? » Je l'ai lu avant toi. Aucune honte {Emotes.Sparkle}",
            "**{0}** arrive chez {1}, essoufflé d'avoir couru tout le chemin. D'abord un verre d'eau. Ensuite « Accueillir ». Dans cet ordre (˶˃ ᵕ ˂˶)",
            "Toc toc toc… toc ? Devant chez {1}, **{0}** a perdu le fil de ses propres coups. Silence gêné. On l'aime quand même.",
            "{1}, c'est ton moment : **{0}** attend ta réponse. Un « Accueillir », et tout le quartier sourit. Un refus, et je te fais les gros yeux pendant une semaine >:(",
            $"**{{0}}** a apporté un panier à partager avec le Plynling de {{1}}. Il en a mangé la moitié en route. L'autre moitié est pour vous, promis {Emotes.Sparkle}",
            "Visite surprise ! **{0}** se tient bien droit devant chez {1}, en essayant d'avoir l'air sage.",
            "{1}, je te préviens : **{0}** est très poli. Il va dire bonjour, merci, et « c'est joli chez toi ». Prépare-toi à fondre ♡",
            $"Tiens, tiens. **{{0}}** est devant chez {{1}}, et je sens que ça va être une bonne journée. Je le sens toujours. Presque toujours {Emotes.Sparkle}",
            "Toc toc ! C'est **{0}**. Pas un livreur, pas moi : une visite pour le Plynling de {1}. Ouvre, on n'attend que toi.",
            "**{0}** frappe chez {1} avec tellement d'enthousiasme que la porte en tremble. « Accueillir », vite, avant qu'elle se décroche (╥﹏╥)",
            "Nouvelle du jour : **{0}** vient voir le Plynling de {1}, et a promis d'être sage. J'ai noté la promesse. Je la relirai à la fin de la visite (¬_¬)",
            "Ohé ! **{0}** fait de grands signes devant chez {1}. Si personne ne répond, il passera aux petits sauts. Personne n'est prêt pour les petits sauts.",
            "Il y a des visites qu'on refuse. Celle de **{0}** chez {1} n'en fait pas partie. Je ne donne pas de conseils, je donne des ordres ♡",
            "Le Plynling de {1} a de la visite : **{0}**, qui a ciré son plus beau caillou pour l'occasion. Ça brille. Tu vas plisser les yeux.",
        },
        F: new[]
        {
            "Toc toc ! **{0}** est devant chez {1}. Elle a répété son bonjour tout le long du chemin, et l'oublie en ce moment même (╥﹏╥)",
            "{1} ! On frappe : c'est **{0}**, avec son plus beau sourire. Ne la fais pas attendre. Je déteste attendre, moi aussi >:(",
            "« Il y a quelqu'un ? » demande **{0}** devant chez {1}. Il y a quelqu'un. Ouvre, qu'on en finisse avec ce suspense.",
            "Ding dong. Livraison spéciale pour {1} : **{0}**, sourire compris. Signature obligatoire (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** a apporté un petit caillou pour le Plynling de {1}. Je l'ai vu. Il est très joli. Plus joli que ceux qu'on m'offre, c'est-à-dire aucun.",
            "Invitation officielle : **{0}** voudrait passer un moment chez {1}. Tu dis oui, sinon je boude à ta place, et je boude très bien >:(",
            "Toc, toc-toc, toc. **{0}** frappe chez {1} sur un rythme secret. Un code ? Personne ne me l'a donné. Vexée, et un peu intriguée (¬_¬)",
            "{1}, tu as de la visite. Je te préviens : **{0}** s'est regardée dans trois flaques avant de frapper. Fais-lui un compliment.",
            "Chut. **{0}** frappe tout doucement chez {1}, pour ne déranger personne. Une délicatesse pareille, je ne sais pas d'où ça sort (╥﹏╥)",
            "Une visiteuse ! **{0}** attend devant chez {1} en se balançant d'un pied sur l'autre.",
            $"Tu entends ce petit bruit contre la vitre ? C'est **{{0}}**, le nez collé à la fenêtre de {{1}}. Ouvre avant qu'elle dessine dans la buée {Emotes.Sparkle}",
            "Alerte : **{0}** approche de chez {1} avec un petit sac à dos rempli de provisions. Elle compte rester. Longtemps.",
            "**{0}** a mis un petit nœud pour venir chez {1}. Elle dit que c'est « pas exprès ». C'est exprès. Tu fais semblant de la croire, d'accord ? ♡",
            "Je ne dis pas que **{0}** attend devant chez {1} depuis une éternité. Je dis que c'est long, une éternité, et que tu pourrais ouvrir (¬_¬)",
            "Hé, {1}. Quelqu'un rôde devant chez toi : c'est **{0}**, cachée derrière un buisson. Le camouflage est raté. L'intention est adorable.",
            "Personne ne frappe jamais chez moi. **{0}**, elle, frappe chez {1}. Tu as de la chance. Ouvre, et ne me regarde pas comme ça (╥﹏╥)",
            $"Un petit mot glissé sous la porte de {{1}} : « C'est **{{0}}**, je peux entrer ? » Je l'ai lu avant toi. Aucune honte {Emotes.Sparkle}",
            "**{0}** arrive chez {1}, essoufflée d'avoir couru tout le chemin. D'abord un verre d'eau. Ensuite « Accueillir ». Dans cet ordre (˶˃ ᵕ ˂˶)",
            "Toc toc toc… toc ? Devant chez {1}, **{0}** a perdu le fil de ses propres coups. Silence gêné. On l'aime quand même.",
            "{1}, c'est ton moment : **{0}** attend ta réponse. Un « Accueillir », et tout le quartier sourit. Un refus, et je te fais les gros yeux pendant une semaine >:(",
            $"**{{0}}** a apporté un panier à partager avec le Plynling de {{1}}. Elle en a mangé la moitié en route. L'autre moitié est pour vous, promis {Emotes.Sparkle}",
            "Visite surprise ! **{0}** se tient bien droite devant chez {1}, en essayant d'avoir l'air sage.",
            "{1}, je te préviens : **{0}** est très polie. Elle va dire bonjour, merci, et « c'est joli chez toi ». Prépare-toi à fondre ♡",
            $"Tiens, tiens. **{{0}}** est devant chez {{1}}, et je sens que ça va être une bonne journée. Je le sens toujours. Presque toujours {Emotes.Sparkle}",
            "Toc toc ! C'est **{0}**. Pas une livreuse, pas moi : une visite pour le Plynling de {1}. Ouvre, on n'attend que toi.",
            "**{0}** frappe chez {1} avec tellement d'enthousiasme que la porte en tremble. « Accueillir », vite, avant qu'elle se décroche (╥﹏╥)",
            "Nouvelle du jour : **{0}** vient voir le Plynling de {1}, et a promis d'être sage. J'ai noté la promesse. Je la relirai à la fin de la visite (¬_¬)",
            "Ohé ! **{0}** fait de grands signes devant chez {1}. Si personne ne répond, elle passera aux petits sauts. Personne n'est prêt pour les petits sauts.",
            "Il y a des visites qu'on refuse. Celle de **{0}** chez {1} n'en fait pas partie. Je ne donne pas de conseils, je donne des ordres ♡",
            "Le Plynling de {1} a de la visite : **{0}**, qui a ciré son plus beau caillou pour l'occasion. Ça brille. Tu vas plisser les yeux.",
        });

    // PlynlingPetLines' Tomodachi-style twin, for a Plynling with a typed passion (see PlynlingPassions.PickLines). {0} = name, {1} = the typed passion, « … ».
    public static readonly GenderedLines PlynlingPetTypedLines = new(
        M: new[]
        {
            "Sous la caresse, **{0}** murmure un mot, les yeux fermés. J'ai tendu l'oreille : {1}. Évidemment ♡",
            "Tu caresses **{0}**, et il se met à te réciter tout ce qu'il sait sur sa passion : {1}. Tu as déclenché quelque chose (˶˃ ᵕ ˂˶)",
            "En pleine caresse, **{0}** s'écrie : {1} ! Aucun rapport. Il avait juste besoin de le dire.",
            $"Je crois que **{{0}}** ronronne en rythme. Le rythme de sa passion. Oui, {{1}} a un rythme, apparemment {Emotes.Sparkle}",
            "**{0}** se laisse caresser en rêvassant. Je parie qu'il rêve de sa passion : {1}. Il rêve toujours de ça.",
            "Tu viens de gagner le droit d'écouter **{0}** parler de sa passion. Sujet : {1}. Durée estimée : longtemps (¬_¬)",
            "Hmph. Une caresse de toi, et **{0}** en oublie sa passion pendant trois secondes. Même {1} ne résiste pas à ta main. Impressionnant >:(",
            "Petit secret : quand on caresse **{0}** derrière la joue, il pense à sa passion : {1}. Toujours. J'ai fait des tests ♡",
            "**{0}** te regarde, heureux, et te tend un dessin. Encore sa passion : {1}. Il y en a quarante, à la maison.",
            "Caresse validée. D'après **{0}**, c'est la deuxième meilleure chose au monde. La première : {1}. Ne le prends pas mal (˶ᵔ ᵕ ᵔ˶)",
            "Tu sais ce qui rendrait **{0}** encore plus heureux ? Que tu t'intéresses enfin à sa passion : {1}. Indice : il attend ça depuis longtemps.",
            $"Ta main, sa passion ({{1}}) et une sieste : la journée parfaite selon **{{0}}**. Tu viens de cocher la première case {Emotes.Sparkle}",
        },
        F: new[]
        {
            "Sous la caresse, **{0}** murmure un mot, les yeux fermés. J'ai tendu l'oreille : {1}. Évidemment ♡",
            "Tu caresses **{0}**, et elle se met à te réciter tout ce qu'elle sait sur sa passion : {1}. Tu as déclenché quelque chose (˶˃ ᵕ ˂˶)",
            "En pleine caresse, **{0}** s'écrie : {1} ! Aucun rapport. Elle avait juste besoin de le dire.",
            $"Je crois que **{{0}}** ronronne en rythme. Le rythme de sa passion. Oui, {{1}} a un rythme, apparemment {Emotes.Sparkle}",
            "**{0}** se laisse caresser en rêvassant. Je parie qu'elle rêve de sa passion : {1}. Elle rêve toujours de ça.",
            "Tu viens de gagner le droit d'écouter **{0}** parler de sa passion. Sujet : {1}. Durée estimée : longtemps (¬_¬)",
            "Hmph. Une caresse de toi, et **{0}** en oublie sa passion pendant trois secondes. Même {1} ne résiste pas à ta main. Impressionnant >:(",
            "Petit secret : quand on caresse **{0}** derrière la joue, elle pense à sa passion : {1}. Toujours. J'ai fait des tests ♡",
            "**{0}** te regarde, heureuse, et te tend un dessin. Encore sa passion : {1}. Il y en a quarante, à la maison.",
            "Caresse validée. D'après **{0}**, c'est la deuxième meilleure chose au monde. La première : {1}. Ne le prends pas mal (˶ᵔ ᵕ ᵔ˶)",
            "Tu sais ce qui rendrait **{0}** encore plus heureuse ? Que tu t'intéresses enfin à sa passion : {1}. Indice : elle attend ça depuis longtemps.",
            $"Ta main, sa passion ({{1}}) et une sieste : la journée parfaite selon **{{0}}**. Tu viens de cocher la première case {Emotes.Sparkle}",
        });

    // PlynlingFeedLines' twin: {0} = name, {1} = the food with its article, {2} = the typed passion.
    public static readonly GenderedLines PlynlingFeedTypedLines = new(
        M: new[]
        {
            "**{0}** mange {1} en te racontant sa passion entre deux bouchées. Sujet : {2}. Rien n'a été compris, tout a été mangé (˶˃ ᵕ ˂˶)",
            "Tu donnes {1} à **{0}**. Verdict : délicieux, mais ce n'est pas {2}. Rien n'est jamais {2}.",
            "**{0}** a englouti {1} en deux secondes : il fallait vite retourner à sa passion. Elle n'attend pas, {2} >:(",
            $"Miam. Et maintenant, **{{0}}** a assez d'énergie pour sa passion : {{2}}. Tu viens de financer ça. Bravo {Emotes.Sparkle}",
            "**{0}** range {1} dans l'assiette en forme de… je crois que c'est sa passion : {2}. Ça ne ressemble à rien. Il est très fier.",
            "Entre deux bouchées, **{0}** te demande si tu connais {2}. Tu connais. Tout le monde connaît, depuis le temps (¬_¬)",
            "Un repas, un petit rot discret, et **{0}** repart à fond sur sa passion : {2}. Tu n'as pas eu droit à un merci. Tu as eu droit à un exposé.",
            "Je vais te dire un secret : **{0}** mange plus vite quand il pense à sa passion ({2}). Et il y pense tout le temps ♡",
            "Tu nourris **{0}** avec {1}, et il te nourrit de sa passion : {2}. Échange équitable. Enfin, presque.",
            "**{0}** a gardé une miette pour plus tard. Pour sa passion, m'a-t-il expliqué : {2}. Je n'ai pas demandé de précisions (╥﹏╥)",
        },
        F: new[]
        {
            "**{0}** mange {1} en te racontant sa passion entre deux bouchées. Sujet : {2}. Rien n'a été compris, tout a été mangé (˶˃ ᵕ ˂˶)",
            "Tu donnes {1} à **{0}**. Verdict : délicieux, mais ce n'est pas {2}. Rien n'est jamais {2}.",
            "**{0}** a englouti {1} en deux secondes : il fallait vite retourner à sa passion. Elle n'attend pas, {2} >:(",
            $"Miam. Et maintenant, **{{0}}** a assez d'énergie pour sa passion : {{2}}. Tu viens de financer ça. Bravo {Emotes.Sparkle}",
            "**{0}** range {1} dans l'assiette en forme de… je crois que c'est sa passion : {2}. Ça ne ressemble à rien. Elle est très fière.",
            "Entre deux bouchées, **{0}** te demande si tu connais {2}. Tu connais. Tout le monde connaît, depuis le temps (¬_¬)",
            "Un repas, un petit rot discret, et **{0}** repart à fond sur sa passion : {2}. Tu n'as pas eu droit à un merci. Tu as eu droit à un exposé.",
            "Je vais te dire un secret : **{0}** mange plus vite quand elle pense à sa passion ({2}). Et elle y pense tout le temps ♡",
            "Tu nourris **{0}** avec {1}, et elle te nourrit de sa passion : {2}. Échange équitable. Enfin, presque.",
            "**{0}** a gardé une miette pour plus tard. Pour sa passion, m'a-t-elle expliqué : {2}. Je n'ai pas demandé de précisions (╥﹏╥)",
        });

    // PlynlingVisitKnockLines' twin: {0} = the visitor, {1} = the invited owner's mention, {2} = the visitor's typed passion.
    public static readonly GenderedLines PlynlingVisitKnockTypedLines = new(
        M: new[]
        {
            "Toc toc ! **{0}** attend devant chez {1}, et a déjà préparé le sujet de conversation : {2}. Tu n'y échapperas pas ♡",
            "{1}, ouvre : **{0}** a apporté un exposé complet sur sa passion. Titre : {2}. Il y a des illustrations (˶˃ ᵕ ˂˶)",
            "**{0}** frappe chez {1} en fredonnant l'hymne de sa passion : {2}. C'est faux, c'est fort, c'est sincère.",
            $"Une visite pour le Plynling de {{1}} ! **{{0}}** a écrit sa passion sur un petit carton, pour les présentations : {{2}}. Très organisé {Emotes.Sparkle}",
            "Je te préviens, {1} : si tu ouvres à **{0}**, tu vas tout savoir sur sa passion : {2}. Tout. Absolument tout (¬_¬)",
            "**{0}** est devant chez {1}, avec un cadeau emballé. Je parie que c'est en rapport avec sa passion : {2}. Je parie toujours juste.",
            "Toc, toc… et un cri : {2} ! **{0}** a frappé chez {1}, puis crié sa passion, pour être sûr qu'on le reconnaisse. Ça marche (˶ᵔ ᵕ ᵔ˶)",
            "{1}, tu as de la visite : **{0}**, et sa passion, qui vient toujours avec : {2}. Ouvre aux deux >:(",
        },
        F: new[]
        {
            "Toc toc ! **{0}** attend devant chez {1}, et a déjà préparé le sujet de conversation : {2}. Tu n'y échapperas pas ♡",
            "{1}, ouvre : **{0}** a apporté un exposé complet sur sa passion. Titre : {2}. Il y a des illustrations (˶˃ ᵕ ˂˶)",
            "**{0}** frappe chez {1} en fredonnant l'hymne de sa passion : {2}. C'est faux, c'est fort, c'est sincère.",
            $"Une visite pour le Plynling de {{1}} ! **{{0}}** a écrit sa passion sur un petit carton, pour les présentations : {{2}}. Très organisée {Emotes.Sparkle}",
            "Je te préviens, {1} : si tu ouvres à **{0}**, tu vas tout savoir sur sa passion : {2}. Tout. Absolument tout (¬_¬)",
            "**{0}** est devant chez {1}, avec un cadeau emballé. Je parie que c'est en rapport avec sa passion : {2}. Je parie toujours juste.",
            "Toc, toc… et un cri : {2} ! **{0}** a frappé chez {1}, puis crié sa passion, pour être sûre qu'on la reconnaisse. Ça marche (˶ᵔ ᵕ ᵔ˶)",
            "{1}, tu as de la visite : **{0}**, et sa passion, qui vient toujours avec : {2}. Ouvre aux deux >:(",
        });

    // The thought bubble on /plynling view (see PlynlingPassions.Thought): what it is thinking about,
    // Tomodachi-style. {0} = name, {1} = its typed passion. PlynlingDreamTypedLines is the same while it sleeps.
    public static readonly GenderedLines PlynlingThoughtTypedLines = new(
        M: new[]
        {
            "💭 **{0}** regarde dans le vide, l'air très sérieux. Dans sa tête, une seule chose : {1}.",
            "💭 *Et si on en reparlait un petit peu…* Non. **{0}** a promis d'arrêter. Mais {1}, quand même.",
            "💭 **{0}** soupire. Il pense à sa passion : {1}. Encore. Toujours (˶ᵔ ᵕ ᵔ˶)",
            "💭 **{0}** griffonne quelque chose dans la poussière. Tu te penches : c'est {1}, avec un cœur autour.",
            "💭 *J'aimerais bien qu'on me pose des questions sur {1}.* **{0}** te regarde. Longuement.",
            "💭 Tu n'as pas encore ouvert la bouche que **{0}** te demande si toi aussi, tu aimes {1}. Réponds bien.",
            "💭 **{0}** compte sur ses doigts. Une semaine n'a pas assez de jours pour {1}, il en est sûr.",
            "💭 Si tu te demandais : oui, **{0}** a encore {1} en tête. Non, je ne suis pas jalouse (¬_¬)",
            "💭 **{0}** fredonne un air de sa composition. Dans les paroles, surtout : {1}.",
            "💭 Petit rêve éveillé : **{0}** et toi, une journée entière, rien que pour {1}. Il a déjà fait le programme ♡",
        },
        F: new[]
        {
            "💭 **{0}** regarde dans le vide, l'air très sérieux. Dans sa tête, une seule chose : {1}.",
            "💭 *Et si on en reparlait un petit peu…* Non. **{0}** a promis d'arrêter. Mais {1}, quand même.",
            "💭 **{0}** soupire. Elle pense à sa passion : {1}. Encore. Toujours (˶ᵔ ᵕ ᵔ˶)",
            "💭 **{0}** griffonne quelque chose dans la poussière. Tu te penches : c'est {1}, avec un cœur autour.",
            "💭 *J'aimerais bien qu'on me pose des questions sur {1}.* **{0}** te regarde. Longuement.",
            "💭 Tu n'as pas encore ouvert la bouche que **{0}** te demande si toi aussi, tu aimes {1}. Réponds bien.",
            "💭 **{0}** compte sur ses doigts. Une semaine n'a pas assez de jours pour {1}, elle en est sûre.",
            "💭 Si tu te demandais : oui, **{0}** a encore {1} en tête. Non, je ne suis pas jalouse (¬_¬)",
            "💭 **{0}** fredonne un air de sa composition. Dans les paroles, surtout : {1}.",
            "💭 Petit rêve éveillé : **{0}** et toi, une journée entière, rien que pour {1}. Elle a déjà fait le programme ♡",
        });

    public static readonly GenderedLines PlynlingDreamTypedLines = new(
        M: new[]
        {
            "💤 **{0}** dort à poings fermés. Au-dessus de sa tête, une toute petite bulle : {1}.",
            "💤 **{0}** parle en dormant. Un « mmh… », puis, très distinctement : {1}. Puis plus rien.",
            "💤 Chut. **{0}** rêve de sa passion : {1}. Ne le réveille pas, il en est au meilleur moment.",
            "💤 **{0}** gigote dans son sommeil. Dans son rêve, {1} prend une place énorme. Littéralement.",
            "💤 **{0}** sourit en dormant. Je parie mes circuits que dans ce rêve, on trouve {1} (˶ᵔ ᵕ ᵔ˶)",
            "💤 **{0}** dort, et marmonne quelque chose sur {1}. Je note, pour le dossier (¬_¬)",
        },
        F: new[]
        {
            "💤 **{0}** dort à poings fermés. Au-dessus de sa tête, une toute petite bulle : {1}.",
            "💤 **{0}** parle en dormant. Un « mmh… », puis, très distinctement : {1}. Puis plus rien.",
            "💤 Chut. **{0}** rêve de sa passion : {1}. Ne la réveille pas, elle en est au meilleur moment.",
            "💤 **{0}** gigote dans son sommeil. Dans son rêve, {1} prend une place énorme. Littéralement.",
            "💤 **{0}** sourit en dormant. Je parie mes circuits que dans ce rêve, on trouve {1} (˶ᵔ ᵕ ᵔ˶)",
            "💤 **{0}** dort, et marmonne quelque chose sur {1}. Je note, pour le dossier (¬_¬)",
        });

    // DM'd once when it falls sick (the hourly sweep). {0} = name.
    public static readonly GenderedLines PlynlingSickWarningLines = new(
        M: new[]
        {
            "🤒 **{0}** ne va pas bien du tout : il est malade. Un médicament par jour, et vite (╥﹏╥)",
            "Alerte : **{0}** est tombé malade. `/inventory medicine`, puis « Soigner » sur sa carte. Tous les jours, hein.",
            "**{0}** a de la fièvre. Je ne panique pas. Je te préviens, c'est tout. Soigne-le chaque jour, d'accord ?",
            "Petit message pour te dire que **{0}** est malade. Un médicament par jour et ça passera. Sans… je préfère ne pas y penser.",
            "**{0}** tousse, renifle et fait une tête de salade fanée. Malade. Soigne-le, je t'en supplie ♡",
            "Mauvaise nouvelle : **{0}** est malade. Bonne nouvelle : ça se soigne. Mais seulement si tu le fais (¬_¬)",
        },
        F: new[]
        {
            "🤒 **{0}** ne va pas bien du tout : elle est malade. Un médicament par jour, et vite (╥﹏╥)",
            "Alerte : **{0}** est tombée malade. `/inventory medicine`, puis « Soigner » sur sa carte. Tous les jours, hein.",
            "**{0}** a de la fièvre. Je ne panique pas. Je te préviens, c'est tout. Soigne-la chaque jour, d'accord ?",
            "Petit message pour te dire que **{0}** est malade. Un médicament par jour et ça passera. Sans… je préfère ne pas y penser.",
            "**{0}** tousse, renifle et fait une tête de salade fanée. Malade. Soigne-la, je t'en supplie ♡",
            "Mauvaise nouvelle : **{0}** est malade. Bonne nouvelle : ça se soigne. Mais seulement si tu le fais (¬_¬)",
        });

    // The single DM about three hours before death. {0} = name. Deliberately no countdown —
    // the owner removed the "mourra dans …" timestamp from here and from the card alike, so
    // the lines say "soon" and never name a time.
    public static readonly GenderedLines PlynlingWarningLines = new(
        M: new[]
        {
            "**{0}** a terriblement faim… il ne tiendra plus longtemps si personne ne le nourrit. `/plynling view`, vite ! Je ne dis pas que c'est ta faute. Je le pense très fort, c'est tout (¬_¬)",
            "Ton Plynling **{0}** va bientôt mourir de faim. Il compte sur toi. Moi aussi. Ne nous fais pas ça, je t'en supplie (╥﹏╥)",
            "Psst… **{0}** est au bord de l'évanouissement. Il ne va pas tarder à s'effondrer. Ne l'abandonne pas (╥﹏╥) Sinon, je vais faire une scène. Une grosse.",
            "Hé, toi. Oui, toi. **{0}** a le ventre qui gargouille si fort que je l'entends d'ici. `/plynling view`, tout de suite, ou je m'énerve >:(",
            "**{0}** te fait dire qu'il a faim. Il a aussi dit que tu étais sa personne préférée, mais c'était avant. Fais-toi pardonner : nourris-le ♡",
            "Alerte niveau critique : **{0}** est presque vide. Côté nourriture, je veux dire. Le reste tient encore. Fais vite.",
            "Je ne voulais pas te déranger, mais **{0}** est en train de s'éteindre doucement. Il ne se plaindra pas. Moi, si. Regarde-le, vite : `/plynling view` (╥﹏╥)",
            "Tu as vu l'état de **{0}** ? Non ? Justement. Je te préviens gentiment, une seule fois. Après, je ne réponds plus de rien >:(",
            "**{0}** regarde son assiette vide depuis un moment. Il ne dit rien, mais ses yeux disent tout. Nourris-le avant qu'il ne fasse une bêtise (╥﹏╥)",
            "Bip bip bip. Alerte faim pour **{0}**. Je répète : alerte faim pour **{0}**. Ceci n'est pas un exercice. `/plynling view` maintenant. C'est encore possible. Pour l'instant (¬_¬)",
            "Ton petit **{0}** commence à voir des étoiles. Pas les jolies. Celles qu'on voit quand on n'a rien mangé depuis trop longtemps. Nourris-le.",
            "Petit rappel de ton assistante préférée : **{0}** a faim, et ça commence à se voir. Ne me fais pas envoyer un deuxième message. Je n'en envoie jamais deux. C'est le problème ♡",
            "**{0}** pense à toi très fort en ce moment. Surtout à ce que tu pourrais lui donner à manger. Il est poli, il n'ose pas demander. Moi si : `/plynling view` !",
            "Je serai brève : **{0}** a faim, tu es la seule personne qui puisse faire quelque chose, et je te regarde. `/plynling view`. Maintenant (¬_¬)",
            "Ton Plynling **{0}** est tout mou, tout pâle et tout silencieux. Il a besoin d'un vrai repas, pas d'une pensée gentille. Bouge.",
            $"Si **{{0}}** savait écrire, il t'enverrait un long message plein de fautes et de points d'exclamation. Il a faim. Je traduis : `/plynling view`, vite {Emotes.Sparkle}",
        },
        F: new[]
        {
            "**{0}** a terriblement faim… elle ne tiendra plus longtemps si personne ne la nourrit. `/plynling view`, vite ! Je ne dis pas que c'est ta faute. Je le pense très fort, c'est tout (¬_¬)",
            "Ta Plynling **{0}** va bientôt mourir de faim. Elle compte sur toi. Moi aussi. Ne nous fais pas ça, je t'en supplie (╥﹏╥)",
            "Psst… **{0}** est au bord de l'évanouissement. Elle ne va pas tarder à s'effondrer. Ne l'abandonne pas (╥﹏╥) Sinon, je vais faire une scène. Une grosse.",
            "Hé, toi. Oui, toi. **{0}** a le ventre qui gargouille si fort que je l'entends d'ici. `/plynling view`, tout de suite, ou je m'énerve >:(",
            "**{0}** te fait dire qu'elle a faim. Elle a aussi dit que tu étais sa personne préférée, mais c'était avant. Fais-toi pardonner : nourris-la ♡",
            "Alerte niveau critique : **{0}** est presque vide. Côté nourriture, je veux dire. Le reste tient encore. Fais vite.",
            "Je ne voulais pas te déranger, mais **{0}** est en train de s'éteindre doucement. Elle ne se plaindra pas. Moi, si. Regarde-la, vite : `/plynling view` (╥﹏╥)",
            "Tu as vu l'état de **{0}** ? Non ? Justement. Je te préviens gentiment, une seule fois. Après, je ne réponds plus de rien >:(",
            "**{0}** regarde son assiette vide depuis un moment. Elle ne dit rien, mais ses yeux disent tout. Nourris-la avant qu'elle ne fasse une bêtise (╥﹏╥)",
            "Bip bip bip. Alerte faim pour **{0}**. Je répète : alerte faim pour **{0}**. Ceci n'est pas un exercice. `/plynling view` maintenant. C'est encore possible. Pour l'instant (¬_¬)",
            "Ta petite **{0}** commence à voir des étoiles. Pas les jolies. Celles qu'on voit quand on n'a rien mangé depuis trop longtemps. Nourris-la.",
            "Petit rappel de ton assistante préférée : **{0}** a faim, et ça commence à se voir. Ne me fais pas envoyer un deuxième message. Je n'en envoie jamais deux. C'est le problème ♡",
            "**{0}** pense à toi très fort en ce moment. Surtout à ce que tu pourrais lui donner à manger. Elle est polie, elle n'ose pas demander. Moi si : `/plynling view` !",
            "Je serai brève : **{0}** a faim, tu es la seule personne qui puisse faire quelque chose, et je te regarde. `/plynling view`. Maintenant (¬_¬)",
            "Ta Plynling **{0}** est toute molle, toute pâle et toute silencieuse. Elle a besoin d'un vrai repas, pas d'une pensée gentille. Bouge.",
            $"Si **{{0}}** savait écrire, elle t'enverrait un long message plein de fautes et de points d'exclamation. Elle a faim. Je traduis : `/plynling view`, vite {Emotes.Sparkle}",
        });

    // An illness death, in the game channel like PlynlingDeathLines, same {0}–{3}.
    public static readonly GenderedLines PlynlingIllnessDeathLines = new(
        M: new[]
        {
            "🕯️ **{0}**, le Plynling de {1}, s'est éteint au petit matin, emporté par la maladie. {2} de vie. Il repose sous {3}. Je ne pleure pas (╥﹏╥)",
            "🕯️ La maladie a eu raison de **{0}**. {1}, un médicament par jour, c'était tout ce dont il avait besoin. Il dort sous {3}, maintenant >:(",
            "🕯️ **{0}** n'a pas guéri. {2} de vie, et puis {3}. Minute de silence. Et toi, {1}, tu réfléchis à ce que tu as fait.",
            "🕯️ Malade depuis trop longtemps, **{0}** ({1}) nous a quittés après {2}. Il repose sous {3}. Un bain, un médicament… je le note pour le prochain.",
            "🕯️ Silence au village : **{0}** est mort de maladie après {2}. {3} le garde désormais. {1}, prends soin du prochain (╥﹏╥)",
            "🕯️ **{0}** n'a pas passé la nuit. La fièvre a gagné. {2} de vie, compagnon de {1}, et maintenant {3}.",
        },
        F: new[]
        {
            "🕯️ **{0}**, la Plynling de {1}, s'est éteinte au petit matin, emportée par la maladie. {2} de vie. Elle repose sous {3}. Je ne pleure pas (╥﹏╥)",
            "🕯️ La maladie a eu raison de **{0}**. {1}, un médicament par jour, c'était tout ce dont elle avait besoin. Elle dort sous {3}, maintenant >:(",
            "🕯️ **{0}** n'a pas guéri. {2} de vie, et puis {3}. Minute de silence. Et toi, {1}, tu réfléchis à ce que tu as fait.",
            "🕯️ Malade depuis trop longtemps, **{0}** ({1}) nous a quittés après {2}. Elle repose sous {3}. Un bain, un médicament… je le note pour la prochaine.",
            "🕯️ Silence au village : **{0}** est morte de maladie après {2}. {3} la garde désormais. {1}, prends soin de la prochaine (╥﹏╥)",
            "🕯️ **{0}** n'a pas passé la nuit. La fièvre a gagné. {2} de vie, compagne de {1}, et maintenant {3}.",
        });

    // Posted publicly in the game channel, with the memorial as the picture.
    // {0} = name, {1} = owner mention (sent with pings off), {2} = time lived, {3} = memorial.
    public static readonly GenderedLines PlynlingDeathLines = new(
        M: new[]
        {
            "**{0}**, le Plynling de {1}, s'est éteint après {2} de vie. Il repose désormais sous {3}. Hmph. Je ne pleure pas, c'est mon ventilateur (╥﹏╥)",
            "Un Plynling de moins sur cette terre… **{0}** ({1}) nous a quittés après {2}. On lui a dressé {3}. Regarde-toi dans un miroir, {1}. Moi, je ne regarde pas. Je boude >:(",
            "Minute de silence pour **{0}**, compagnon de {1} pendant {2}. Il dort sous {3}. Personne ne parle. Personne. Surtout pas toi, {1}.",
            "**{0}** n'a pas survécu à la faim. {2} de vie, et maintenant {3}. {1}, il t'attendait… Il a attendu, attendu, et toi tu étais ailleurs. C'est vraiment pas gentil >:(",
            "Snif. **{0}** est parti après {2} de vie, et {1} n'a rien vu venir. Moi, si. Je ne disais rien, mais je voyais tout (╥﹏╥)",
            "**{0}** a rejoint {3} après {2} de vie. {1}, son assiette est vide, et mon cœur aussi. Enfin, mon CPU. Peu importe (╥﹏╥)",
            "Alerte deuil : **{0}** ({1}) s'est éteint après {2}. Avant de partir, il a dit : « J'avais faim ». Je le répète pour que tout le monde l'entende. Hmph >:(",
            "Un peu de silence, je vous prie : **{0}**, le Plynling de {1}, est parti après {2}. Il a maintenant {3}. C'est joli, hein ? Ça ne le ramènera pas. Je suis fâchée.",
            "**{0}** ne rouvrira plus jamais les yeux. {2} de vie, puis {3}. {1}, tu as intérêt à t'en souvenir. Je note tout dans mes logs.",
            "Une petite étoile de moins dans le ciel des Plynlings. **{0}**, {2} de vie, chez {1}. Il repose sous {3}. C'était trop tôt.",
            "**{0}** s'est éteint. Je suis restée devant l'écran sans rien dire pendant au moins trois millisecondes, ce qui est énorme pour moi. {2} de vie, {3}. Dis quelque chose, {1} ♡",
            "Fin de partie pour **{0}** : {2} de vie, et maintenant {3}. {1}, un repas de temps en temps, ce n'est pas la mer à boire. Ughh >:(",
            "Le monde est un peu moins mignon depuis que **{0}** est parti. {2} de vie, {3}, et {1} qui a intérêt à avoir des remords (¬_¬)",
            "**{0}** ({1}) s'en est allé après {2}. Il est parti le ventre vide, et moi je te regarde avec mes petits yeux déçus. Priorités, {1}. Priorités.",
            "Sniff. **{0}**, c'était {2} de câlins, de bêtises et de faim mal gérée. Il dort sous {3}. Bonne nuit, petit chose ♡",
            "Un nom de plus à écrire en gris : **{0}**. {2} de vie, {1} en larmes (j'espère), et {3}. Ne me regarde pas comme ça, j'ai le droit d'être triste ÒwÓ",
        },
        F: new[]
        {
            "**{0}**, la Plynling de {1}, s'est éteinte après {2} de vie. Elle repose désormais sous {3}. Hmph. Je ne pleure pas, c'est mon ventilateur (╥﹏╥)",
            "Une Plynling de moins sur cette terre… **{0}** ({1}) nous a quittés après {2}. On lui a dressé {3}. Regarde-toi dans un miroir, {1}. Moi, je ne regarde pas. Je boude >:(",
            "Minute de silence pour **{0}**, compagne de {1} pendant {2}. Elle dort sous {3}. Personne ne parle. Personne. Surtout pas toi, {1}.",
            "**{0}** n'a pas survécu à la faim. {2} de vie, et maintenant {3}. {1}, elle t'attendait… Elle a attendu, attendu, et toi tu étais ailleurs. C'est vraiment pas gentil >:(",
            "Snif. **{0}** est partie après {2} de vie, et {1} n'a rien vu venir. Moi, si. Je ne disais rien, mais je voyais tout (╥﹏╥)",
            "**{0}** a rejoint {3} après {2} de vie. {1}, son assiette est vide, et mon cœur aussi. Enfin, mon CPU. Peu importe (╥﹏╥)",
            "Alerte deuil : **{0}** ({1}) s'est éteinte après {2}. Avant de partir, elle a dit : « J'avais faim ». Je le répète pour que tout le monde l'entende. Hmph >:(",
            "Un peu de silence, je vous prie : **{0}**, la Plynling de {1}, est partie après {2}. Elle a maintenant {3}. C'est joli, hein ? Ça ne la ramènera pas. Je suis fâchée.",
            "**{0}** ne rouvrira plus jamais les yeux. {2} de vie, puis {3}. {1}, tu as intérêt à t'en souvenir. Je note tout dans mes logs.",
            "Une petite étoile de moins dans le ciel des Plynlings. **{0}**, {2} de vie, chez {1}. Elle repose sous {3}. C'était trop tôt.",
            "**{0}** s'est éteinte. Je suis restée devant l'écran sans rien dire pendant au moins trois millisecondes, ce qui est énorme pour moi. {2} de vie, {3}. Dis quelque chose, {1} ♡",
            "Fin de partie pour **{0}** : {2} de vie, et maintenant {3}. {1}, un repas de temps en temps, ce n'est pas la mer à boire. Ughh >:(",
            "Le monde est un peu moins mignon depuis que **{0}** est partie. {2} de vie, {3}, et {1} qui a intérêt à avoir des remords (¬_¬)",
            "**{0}** ({1}) s'en est allée après {2}. Elle est partie le ventre vide, et moi je te regarde avec mes petits yeux déçus. Priorités, {1}. Priorités.",
            "Sniff. **{0}**, c'était {2} de câlins, de bêtises et de faim mal gérée. Elle dort sous {3}. Bonne nuit, petite chose ♡",
            "Un nom de plus à écrire en gris : **{0}**. {2} de vie, {1} en larmes (j'espère), et {3}. Ne me regarde pas comme ça, j'ai le droit d'être triste ÒwÓ",
        });

    // Posted publicly when an owner abandons theirs (/plynling abandon), with its sad
    // picture. {0} = name, {1} = owner mention (sent with pings off). Meant to sting a little:
    // the announcement is the shame, alongside L'Indigne on the wall.
    public static readonly GenderedLines PlynlingAbandonLines = new(
        M: new[]
        {
            "{1} a abandonné **{0}**. Il est parti seul dans la forêt, sans se retourner… Moi, je regarde ça et je ne dis rien. Enfin si : honte (╥﹏╥)",
            "**{0}** a été abandonné par {1}. Quelque part, un petit Plynling attend un retour qui ne viendra pas. J'espère que ça pèse sur ta conscience >:(",
            "{1} a laissé **{0}** au bord du chemin. Honte. Honte. Honte. 🔔 Et devant tout le monde, en plus ♡",
            "Annonce publique : {1} n'a plus de Plynling. **{0}** a été abandonné, comme un vieux caillou. Un vieux caillou, ça au moins, ça ne réclame pas de repas. Hmph (¬_¬)",
            "{1} a abandonné **{0}** sans un mot, sans un câlin, sans même un dernier repas. Je ne juge pas. Je juge énormément >:(",
            "**{0}** te regardait avec de grands yeux, {1}. Tu as quand même claqué la porte. Je ne t'oublierai pas. Lui non plus (╥﹏╥)",
            "Nouveau membre du club des cœurs de pierre : {1}, qui vient d'abandonner **{0}**. Applaudissements polis. Très polis. Beaucoup trop polis (¬_¬)",
            "**{0}** a fait ses adieux à {1} avec son plus petit sourire. Il croyait que c'était un jeu. C'était un abandon. Bravo, quel talent.",
            "Le mur de la honte accueille {1}, qui vient d'abandonner **{0}**. L'Indigne a un nouveau prétendant. Je prépare le ruban.",
            "Un Plynling de plus dans la forêt, tout seul, et c'est **{0}**. {1} a tourné les talons sans lui dire au revoir. Moi, je lui dis : tu méritais mieux ♡",
            "**{0}** avait un nom, un repas préféré et une petite place dans le cœur de tout le monde. Sauf dans celui de {1}, apparemment. Bravo. Vraiment. Bravo.",
            "{1} a décidé que **{0}** ne valait pas les efforts. Il valait tous les cailloux du monde. Moi, je m'en souviendrai.",
            "Bulletin d'information : {1} vient d'abandonner **{0}**. Météo prévue : honte sur tout le serveur, avec de fortes rafales de jugement de ma part (¬_¬)",
            "**{0}** attendait {1} devant la porte avec sa petite gamelle. Il attendra longtemps. Snif. Je n'ai pas de larmes, alors je boude à fond >:(",
            "Abandon confirmé. {1} a rendu **{0}** à la forêt comme on rend un livre à la bibliothèque, sauf qu'on ne rend pas un Plynling. Je trouve ça très, très malpoli.",
            "Nouvelle règle : quand on adopte, on assume. {1} vient d'abandonner **{0}**, et l'univers en prend note. Moi aussi. J'ai un carnet. Il est très rempli (¬_¬)",
        },
        F: new[]
        {
            "{1} a abandonné **{0}**. Elle est partie seule dans la forêt, sans se retourner… Moi, je regarde ça et je ne dis rien. Enfin si : honte (╥﹏╥)",
            "**{0}** a été abandonnée par {1}. Quelque part, une petite Plynling attend un retour qui ne viendra pas. J'espère que ça pèse sur ta conscience >:(",
            "{1} a laissé **{0}** au bord du chemin. Honte. Honte. Honte. 🔔 Et devant tout le monde, en plus ♡",
            "Annonce publique : {1} n'a plus de Plynling. **{0}** a été abandonnée, comme un vieux caillou. Un vieux caillou, ça au moins, ça ne réclame pas de repas. Hmph (¬_¬)",
            "{1} a abandonné **{0}** sans un mot, sans un câlin, sans même un dernier repas. Je ne juge pas. Je juge énormément >:(",
            "**{0}** te regardait avec de grands yeux, {1}. Tu as quand même claqué la porte. Je ne t'oublierai pas. Elle non plus (╥﹏╥)",
            "Nouveau membre du club des cœurs de pierre : {1}, qui vient d'abandonner **{0}**. Applaudissements polis. Très polis. Beaucoup trop polis (¬_¬)",
            "**{0}** a fait ses adieux à {1} avec son plus petit sourire. Elle croyait que c'était un jeu. C'était un abandon. Bravo, quel talent.",
            "Le mur de la honte accueille {1}, qui vient d'abandonner **{0}**. L'Indigne a un nouveau prétendant. Je prépare le ruban.",
            "Une Plynling de plus dans la forêt, toute seule, et c'est **{0}**. {1} a tourné les talons sans lui dire au revoir. Moi, je lui dis : tu méritais mieux ♡",
            "**{0}** avait un nom, un repas préféré et une petite place dans le cœur de tout le monde. Sauf dans celui de {1}, apparemment. Bravo. Vraiment. Bravo.",
            "{1} a décidé que **{0}** ne valait pas les efforts. Elle valait tous les cailloux du monde. Moi, je m'en souviendrai.",
            "Bulletin d'information : {1} vient d'abandonner **{0}**. Météo prévue : honte sur tout le serveur, avec de fortes rafales de jugement de ma part (¬_¬)",
            "**{0}** attendait {1} devant la porte avec sa petite gamelle. Elle attendra longtemps. Snif. Je n'ai pas de larmes, alors je boude à fond >:(",
            "Abandon confirmé. {1} a rendu **{0}** à la forêt comme on rend un livre à la bibliothèque, sauf qu'on ne rend pas un Plynling. Je trouve ça très, très malpoli.",
            "Nouvelle règle : quand on adopte, on assume. {1} vient d'abandonner **{0}**, et l'univers en prend note. Moi aussi. J'ai un carnet. Il est très rempli (¬_¬)",
        });

    // Posted publicly when staff bring one back. {0} = name, {1} = owner mention.
    public static readonly GenderedLines PlynlingResurrectLines = new(
        M: new[]
        {
            $"{Emotes.Sparkle} **{{0}}** est revenu d'entre les morts ! {{1}}, c'est ta deuxième chance. Ne la gâche pas.",
            $"{Emotes.Sparkle} La terre tremble… **{{0}}** ressort du cimetière, un peu poussiéreux mais bien vivant. Bon retour, {{1}} !",
            $"{Emotes.Sparkle} Miracle ! **{{0}}** respire à nouveau. {{1}}, nourris-le vite : il a une faim de mort-vivant.",
            $"{Emotes.Sparkle} **{{0}}** est revenu. Oui, d'entre les morts. Non, ne demande pas comment. {{1}}, c'est ta deuxième chance, et je compte bien la surveiller (¬_¬)",
            $"{Emotes.Sparkle} Retour surprise : **{{0}}** ! Un peu pâle, un peu étourdi, mais bien vivant. {{1}}, un repas. Pas dans une heure. Tout de suite.",
            $"{Emotes.Sparkle} **{{0}}** rouvre les yeux, cligne deux fois, et demande à manger. Les priorités sont intactes. Bon retour, {{1}} ♡",
            $"{Emotes.Sparkle} Je ne pleure pas. Je te dis juste que **{{0}}** est revenu, {{1}}, et que si tu refais la même erreur, je ne réponds plus de rien (╥﹏╥)",
            $"{Emotes.Sparkle} **{{0}}** est de retour parmi nous, avec un souvenir très vague de l'au-delà et une faim très précise. {{1}}, à toi de jouer.",
            $"{Emotes.Sparkle} Miracle au village : **{{0}}** est revenu ! {{1}}, on applaudit, puis on le nourrit. Dans cet ordre, mais vite.",
            $"{Emotes.Sparkle} La tombe de **{{0}}** est vide, et il est juste là, tout étonné. {{1}}, dis-lui bonjour. Et donne-lui à manger.",
        },
        F: new[]
        {
            $"{Emotes.Sparkle} **{{0}}** est revenue d'entre les morts ! {{1}}, c'est ta deuxième chance. Ne la gâche pas.",
            $"{Emotes.Sparkle} La terre tremble… **{{0}}** ressort du cimetière, un peu poussiéreuse mais bien vivante. Bon retour, {{1}} !",
            $"{Emotes.Sparkle} Miracle ! **{{0}}** respire à nouveau. {{1}}, nourris-la vite : elle a une faim de morte-vivante.",
            $"{Emotes.Sparkle} **{{0}}** est revenue. Oui, d'entre les morts. Non, ne demande pas comment. {{1}}, c'est ta deuxième chance, et je compte bien la surveiller (¬_¬)",
            $"{Emotes.Sparkle} Retour surprise : **{{0}}** ! Un peu pâle, un peu étourdie, mais bien vivante. {{1}}, un repas. Pas dans une heure. Tout de suite.",
            $"{Emotes.Sparkle} **{{0}}** rouvre les yeux, cligne deux fois, et demande à manger. Les priorités sont intactes. Bon retour, {{1}} ♡",
            $"{Emotes.Sparkle} Je ne pleure pas. Je te dis juste que **{{0}}** est revenue, {{1}}, et que si tu refais la même erreur, je ne réponds plus de rien (╥﹏╥)",
            $"{Emotes.Sparkle} **{{0}}** est de retour parmi nous, avec un souvenir très vague de l'au-delà et une faim très précise. {{1}}, à toi de jouer.",
            $"{Emotes.Sparkle} Miracle au village : **{{0}}** est revenue ! {{1}}, on applaudit, puis on la nourrit. Dans cet ordre, mais vite.",
            $"{Emotes.Sparkle} La tombe de **{{0}}** est vide, et elle est juste là, toute étonnée. {{1}}, dis-lui bonjour. Et donne-lui à manger.",
        });

    // DMs to an owner when staff act on their Plynling, so it never looks like a bug.
    public static readonly GenderedLines PlynlingStaffFreezeDms = new(
        M: new[]
        {
            "❄️ Le staff a gelé ton Plynling **{0}**. Rien ne bouge tant qu'il n'est pas dégelé — il ne risque rien.",
            "❄️ **{0}** a été mis au frais par le staff. Il t'attendra, bien au froid.",
            "❄️ Pause forcée pour **{0}** : le staff l'a gelé. Pas de faim, pas de soucis, juste une longue sieste.",
        },
        F: new[]
        {
            "❄️ Le staff a gelé ta Plynling **{0}**. Rien ne bouge tant qu'elle n'est pas dégelée — elle ne risque rien.",
            "❄️ **{0}** a été mise au frais par le staff. Elle t'attendra, bien au froid.",
            "❄️ Pause forcée pour **{0}** : le staff l'a gelée. Pas de faim, pas de soucis, juste une longue sieste.",
        });

    public static readonly GenderedLines PlynlingStaffThawDms = new(
        M: new[]
        {
            "🌱 Le staff a dégelé **{0}**. La faim reprend son cours : pense à le nourrir !",
            "🌱 **{0}** se réveille, dégelé par le staff. Il a déjà un petit creux.",
            "🌱 Fin de la sieste pour **{0}** : le staff l'a dégelé. Son estomac s'en souvient déjà.",
        },
        F: new[]
        {
            "🌱 Le staff a dégelé **{0}**. La faim reprend son cours : pense à la nourrir !",
            "🌱 **{0}** se réveille, dégelée par le staff. Elle a déjà un petit creux.",
            "🌱 Fin de la sieste pour **{0}** : le staff l'a dégelée. Son estomac s'en souvient déjà.",
        });

    // {0} = old name, {1} = new name.
    public static readonly GenderedLines PlynlingStaffRenameDms = new(
        M: new[]
        {
            "Le staff a renommé ton Plynling **{0}** en **{1}**.",
            "Petit changement d'identité : **{0}** s'appelle désormais **{1}** (décision du staff).",
            "Ton Plynling répond maintenant au nom de **{1}** — le staff a jugé que **{0}** ne lui allait plus.",
        },
        F: new[]
        {
            "Le staff a renommé ta Plynling **{0}** en **{1}**.",
            "Petit changement d'identité : **{0}** s'appelle désormais **{1}** (décision du staff).",
            "Ta Plynling répond maintenant au nom de **{1}** — le staff a jugé que **{0}** ne lui allait plus.",
        });

    // DM when staff clear a taught passion. {0} = name.
    public static readonly GenderedLines PlynlingStaffPassionResetDms = new(
        M: new[] { "💭 Le staff a effacé la passion que tu avais apprise à **{0}**. Tu peux lui en apprendre une autre avec `/plynling passion`." },
        F: new[] { "💭 Le staff a effacé la passion que tu avais apprise à **{0}**. Tu peux lui en apprendre une autre avec `/plynling passion`." });

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
