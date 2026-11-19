using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
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
}
