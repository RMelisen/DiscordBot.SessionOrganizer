using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // /plynling train (docs/agents/plynling-rpg.md): the private answer to a drill, one pool per body stat.
    // {0} = name.
    public static readonly GenderedLines PlynlingDrillForceLines = new(
        M: new[]
        {
            "Un sac de grain, du moulin jusqu'au four. **{0}** l'a porté tout seul, en soufflant comme une bouilloire (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** soulève une bûche. Puis une deuxième. Puis il me regarde, pour vérifier que j'ai bien vu. J'ai vu >:(",
            "Tu as posé une citrouille sur une planche. **{0}** pousse, pousse… La citrouille a bougé d'un doigt. Victoire officielle ✧",
            "« Je suis très fort », annonce **{0}** en tenant un caillou au-dessus de sa tête. Un gros caillou, d'accord. (•̀ᴗ•́)و",
            "Bras de fer contre moi. J'ai perdu exprès. Évidemment, exprès. **{0}** n'a pas besoin de le savoir (¬‿¬)",
            "Tu fais tirer à **{0}** la charrette de la Courge. Vide, la charrette. Il en est quand même très fier.",
        },
        F: new[]
        {
            "Un sac de grain, du moulin jusqu'au four. **{0}** l'a porté toute seule, en soufflant comme une bouilloire (˶ᵔ ᵕ ᵔ˶)",
            "**{0}** soulève une bûche. Puis une deuxième. Puis elle me regarde, pour vérifier que j'ai bien vu. J'ai vu >:(",
            "Tu as posé une citrouille sur une planche. **{0}** pousse, pousse… La citrouille a bougé d'un doigt. Victoire officielle ✧",
            "« Je suis très forte », annonce **{0}** en tenant un caillou au-dessus de sa tête. Un gros caillou, d'accord. (•̀ᴗ•́)و",
            "Bras de fer contre moi. J'ai perdu exprès. Évidemment, exprès. **{0}** n'a pas besoin de le savoir (¬‿¬)",
            "Tu fais tirer à **{0}** la charrette de la Courge. Vide, la charrette. Elle en est quand même très fière.",
        });

    public static readonly GenderedLines PlynlingDrillAgilityLines = new(
        M: new[]
        {
            "La Sommeille, de pierre en pierre, sans se mouiller. **{0}** a réussi. Sauf la dernière pierre, qu'on ne compte pas (˶˃ ᵕ ˂˶)",
            "Tu lances des glands, **{0}** les esquive. Presque tous. Celui qui a rebondi sur sa tête, on n'en parle pas.",
            "**{0}** marche sur le muret du clos du Tilleul, les bras écartés. Les abeilles applaudissent. Enfin, elles bourdonnent, c'est pareil ✧",
            "Roulade avant, roulade arrière… **{0}** finit sur le côté sans faire exprès, et appelle ça un enchaînement (•̀ᴗ•́)",
            "Je compte jusqu'à trois et tu attrapes la plume avant qu'elle touche le sol. **{0}** l'a attrapée à deux. Tricheur (¬_¬)",
            "Tu fais danser **{0}** entre les flaques du chemin. Pas une éclaboussure. Moi, j'en ai pris une. Je ne sais pas comment.",
        },
        F: new[]
        {
            "La Sommeille, de pierre en pierre, sans se mouiller. **{0}** a réussi. Sauf la dernière pierre, qu'on ne compte pas (˶˃ ᵕ ˂˶)",
            "Tu lances des glands, **{0}** les esquive. Presque tous. Celui qui a rebondi sur sa tête, on n'en parle pas.",
            "**{0}** marche sur le muret du clos du Tilleul, les bras écartés. Les abeilles applaudissent. Enfin, les abeilles bourdonnent, c'est pareil ✧",
            "Roulade avant, roulade arrière… **{0}** finit sur le côté sans faire exprès, et appelle ça un enchaînement (•̀ᴗ•́)",
            "Je compte jusqu'à trois et tu attrapes la plume avant qu'elle touche le sol. **{0}** l'a attrapée à deux. Tricheuse (¬_¬)",
            "Tu fais danser **{0}** entre les flaques du chemin. Pas une éclaboussure. Moi, j'en ai pris une. Je ne sais pas comment.",
        });

    public static readonly GenderedLines PlynlingDrillEnduranceLines = new(
        M: new[]
        {
            "Trois fois le tour de la place du Puits. **{0}** a fini le dernier en marchant, mais il a fini (ง •̀_•́)ง",
            "Tu as fait monter la Côte à **{0}**. Il est redescendu tout rose, et il veut recommencer demain. Je ne comprends pas cette énergie.",
            "**{0}** tient la pose de l'arbre pendant une minute entière. Maître Frêne n'aurait pas fait mieux (˶ᵔ ᵕ ᵔ˶)",
            "Course jusqu'au moulin et retour pour **{0}**. J'ai chronométré. Je ne dirai pas combien, mais c'est mieux qu'hier ✧",
            "Pas de pause, pas de goûter, pas de sieste. **{0}** me regarde comme si c'était ma faute. C'était ma faute (¬‿¬)",
            "« Encore un tour ? » Tu as demandé, **{0}** a dit oui. Ses jambes ont dit non, mais on ne leur demandait rien.",
        },
        F: new[]
        {
            "Trois fois le tour de la place du Puits. **{0}** a fini le dernier en marchant, mais elle a fini (ง •̀_•́)ง",
            "Tu as fait monter la Côte à **{0}**. Elle est redescendue toute rose, et elle veut recommencer demain. Je ne comprends pas cette énergie.",
            "**{0}** tient la pose de l'arbre pendant une minute entière. Maître Frêne n'aurait pas fait mieux (˶ᵔ ᵕ ᵔ˶)",
            "Course jusqu'au moulin et retour pour **{0}**. J'ai chronométré. Je ne dirai pas combien, mais c'est mieux qu'hier ✧",
            "Pas de pause, pas de goûter, pas de sieste. **{0}** me regarde comme si c'était ma faute. C'était ma faute (¬‿¬)",
            "« Encore un tour ? » Tu as demandé, **{0}** a dit oui. Ses jambes ont dit non, mais on ne leur demandait rien.",
        });

    public static readonly GenderedLines PlynlingDrillIntelligenceLines = new(
        M: new[]
        {
            "La devinette de la Sauge, celle avec la grenouille et les trois clés. **{0}** a trouvé. Moi aussi, évidemment. Juste un peu après (¬_¬)",
            "Tu caches un gland sous l'un des trois gobelets. **{0}** suit le bon. Il dit que c'est de la logique. Il a louché, je l'ai vu.",
            "**{0}** compte les pommes du clos du Tilleul : quarante-trois. Le Tilleul dit quarante-deux. Personne n'a cédé ✧",
            "Trois pages d'un vieux registre avec Maître Frêne. **{0}** a tout compris, et a posé une question si longue que Maître Frêne a dû s'asseoir (˶˃ ᵕ ˂˶)",
            "Tu montres une carte du village à **{0}**. Il retrouve la Maison aux Simples du premier coup. Je l'ai bien entraîné, ça compte un peu pour moi aussi (•̀ᴗ•́)",
            "Une partie de cartes à l'auberge du Pont-Vieux. **{0}** a gagné, puis a expliqué les règles à tout le monde. Après coup.",
        },
        F: new[]
        {
            "La devinette de la Sauge, celle avec la grenouille et les trois clés. **{0}** a trouvé. Moi aussi, évidemment. Juste un peu après (¬_¬)",
            "Tu caches un gland sous l'un des trois gobelets. **{0}** suit le bon. Elle dit que c'est de la logique. Elle a louché, je l'ai vu.",
            "**{0}** compte les pommes du clos du Tilleul : quarante-trois. Le Tilleul dit quarante-deux. Personne n'a cédé ✧",
            "Trois pages d'un vieux registre avec Maître Frêne. **{0}** a tout compris, et a posé une question si longue que Maître Frêne a dû s'asseoir (˶˃ ᵕ ˂˶)",
            "Tu montres une carte du village à **{0}**. Elle retrouve la Maison aux Simples du premier coup. Je l'ai bien entraînée, ça compte un peu pour moi aussi (•̀ᴗ•́)",
            "Une partie de cartes à l'auberge du Pont-Vieux. **{0}** a gagné, puis a expliqué les règles à tout le monde. Après coup.",
        });

    public static readonly GenderedLines PlynlingDrillSpeedLines = new(
        M: new[]
        {
            "Course contre une poule du Communal. **{0}** a gagné d'une plume. La poule conteste (ᗒᗣᗕ)",
            "Tu fais un signe, **{0}** part comme une flèche et revient avec un pain de Pain-Blond. Je n'avais pas demandé de pain. Je le garde ✧",
            "**{0}** a fait l'aller-retour jusqu'au Pont-Vieux avant que j'aie fini ma phrase. Je recommence ma phrase. Il est déjà reparti.",
            "**{0}** attrape une libellule des Mouilles à la course, et la relâche tout de suite : la pauvre avait l'air vexée (˶˃ ᵕ ˂˶)",
            "Trois tours de la place en courant. Au troisième, **{0}** me double. Je ne courais pas. Je le précise quand même (¬_¬)",
            "Tu siffles, **{0}** démarre. Tu siffles encore, il freine. Il a pris goût au sifflet. Il siffle, maintenant. Partout (•̀ᴗ•́)",
        },
        F: new[]
        {
            "Course contre une poule du Communal. **{0}** a gagné d'une plume. La poule conteste (ᗒᗣᗕ)",
            "Tu fais un signe, **{0}** part comme une flèche et revient avec un pain de Pain-Blond. Je n'avais pas demandé de pain. Je le garde ✧",
            "**{0}** a fait l'aller-retour jusqu'au Pont-Vieux avant que j'aie fini ma phrase. Je recommence ma phrase. Elle est déjà repartie.",
            "**{0}** attrape une libellule des Mouilles à la course, et la relâche tout de suite : la pauvre avait l'air vexée (˶˃ ᵕ ˂˶)",
            "Trois tours de la place en courant. Au troisième, **{0}** me double. Je ne courais pas. Je le précise quand même (¬_¬)",
            "Tu siffles, **{0}** démarre. Tu siffles encore, elle freine. Elle a pris goût au sifflet. Elle siffle, maintenant. Partout (•̀ᴗ•́)",
        });

    // A drill's rare permanent +1, under the drill line; the stat follows on its own line. {0} = name.
    public static readonly GenderedLines PlynlingDrillBoostLines = new(
        M: new[]
        {
            "Attends. **{0}** vient de réussir ce qu'il ratait depuis des jours. Et ça, ça reste ✧",
            "Quelque chose a changé, je l'ai vu. **{0}** aussi : il n'arrête pas de sourire tout seul (˶ᵔ ᵕ ᵔ˶)",
            "Je note dans mon carnet : aujourd'hui, **{0}** a fait un vrai progrès. Je souligne deux fois.",
            "C'est le genre de jour dont on se souvient. Moi, je me souviens de tout. Mais **{0}** aussi, cette fois (˶˃ ᵕ ˂˶)",
            "Je fais semblant que ce n'est rien. **{0}** vient de franchir une marche, et c'est énorme (╥﹏╥)♡",
            "Tu as vu **{0}** ? Tu as vu ?! Ce n'est pas l'effort d'un jour, ça. C'est pour de bon.",
        },
        F: new[]
        {
            "Attends. **{0}** vient de réussir ce qu'elle ratait depuis des jours. Et ça, ça reste ✧",
            "Quelque chose a changé, je l'ai vu. **{0}** aussi : elle n'arrête pas de sourire toute seule (˶ᵔ ᵕ ᵔ˶)",
            "Je note dans mon carnet : aujourd'hui, **{0}** a fait un vrai progrès. Je souligne deux fois.",
            "C'est le genre de jour dont on se souvient. Moi, je me souviens de tout. Mais **{0}** aussi, cette fois (˶˃ ᵕ ˂˶)",
            "Je fais semblant que ce n'est rien. **{0}** vient de franchir une marche, et c'est énorme (╥﹏╥)♡",
            "Tu as vu **{0}** ? Tu as vu ?! Ce n'est pas l'effort d'un jour, ça. C'est pour de bon.",
        });

    // The public level-up, posted in the channel. {0} = name, {1} = the new level.
    public static readonly GenderedLines PlynlingLevelUpLines = new(
        M: new[]
        {
            "🎉 **{0}** passe au niveau **{1}** ! Je fais comme si je n'étais pas fière. Je suis très fière (˶ᵔ ᵕ ᵔ˶)",
            "Niveau **{1}** pour **{0}**. Annoncé ici, devant tout le monde, parce qu'il me l'a demandé trois fois (¬_¬)",
            "Tout le village peut le savoir : **{0}** est niveau **{1}**. Le Tilleul a dit « ah ». C'est beaucoup, venant de lui ✧",
            "**{0}** vient d'atteindre le niveau **{1}** et réclame une médaille. Je n'ai pas de médaille. J'ai découpé une étoile dans une feuille.",
            "Niveau **{1}** ! **{0}** fait le tour de la place du Puits pour l'annoncer en personne. Je l'aide un peu : NIVEAU **{1}** ! (ﾉ◕ヮ◕)ﾉ*:･ﾟ✧",
            "Un niveau de plus : **{0}** est niveau **{1}**. Je l'avais vu venir avant tout le monde, je tiens à le dire.",
        },
        F: new[]
        {
            "🎉 **{0}** passe au niveau **{1}** ! Je fais comme si je n'étais pas fière. Je suis très fière (˶ᵔ ᵕ ᵔ˶)",
            "Niveau **{1}** pour **{0}**. Annoncé ici, devant tout le monde, parce qu'elle me l'a demandé trois fois (¬_¬)",
            "Tout le village peut le savoir : **{0}** est niveau **{1}**. Le Tilleul a dit « ah ». C'est beaucoup, venant du Tilleul ✧",
            "**{0}** vient d'atteindre le niveau **{1}** et réclame une médaille. Je n'ai pas de médaille. J'ai découpé une étoile dans une feuille.",
            "Niveau **{1}** ! **{0}** fait le tour de la place du Puits pour l'annoncer en personne. Je l'aide un peu : NIVEAU **{1}** ! (ﾉ◕ヮ◕)ﾉ*:･ﾟ✧",
            "Un niveau de plus : **{0}** est niveau **{1}**. Je l'avais vu venir avant tout le monde, je tiens à le dire.",
        });

    // A level point placed from the buttons; the stat's new total follows on its own line. {0} = name.
    public static readonly GenderedLines PlynlingPointLines = new(
        M: new[]
        {
            "C'est noté. **{0}** saura s'en servir, j'en suis sûre (•̀ᴗ•́)و",
            "Point placé. **{0}** fait comme s'il l'avait toujours eu (¬_¬)",
            "**{0}** bombe le torse. Un point, et il se prend pour Maître Frêne ✧",
            "Bon choix pour **{0}**. Enfin, je crois. Je n'aurais pas fait pareil, mais c'est ton Plynling.",
            "Je range ce point bien au chaud chez **{0}**. Ça ne se perd pas, un point comme ça (˶ᵔ ᵕ ᵔ˶)",
            "Tu as hésité longtemps. **{0}**, pas du tout : il a dit merci avant même que tu cliques ♡",
        },
        F: new[]
        {
            "C'est noté. **{0}** saura s'en servir, j'en suis sûre (•̀ᴗ•́)و",
            "Point placé. **{0}** fait comme si elle l'avait toujours eu (¬_¬)",
            "**{0}** bombe le torse. Un point, et elle se prend pour Maître Frêne ✧",
            "Bon choix pour **{0}**. Enfin, je crois. Je n'aurais pas fait pareil, mais c'est ton Plynling.",
            "Je range ce point bien au chaud chez **{0}**. Ça ne se perd pas, un point comme ça (˶ᵔ ᵕ ᵔ˶)",
            "Tu as hésité longtemps. **{0}**, pas du tout : elle a dit merci avant même que tu cliques ♡",
        });
}
