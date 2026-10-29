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
}
