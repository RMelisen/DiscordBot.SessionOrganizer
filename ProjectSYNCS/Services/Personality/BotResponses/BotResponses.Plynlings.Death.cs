using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // DM'd once when it falls sick (the hourly sweep). {0} = name.
    public static readonly GenderedLines PlynlingSickWarningLines = new(
        M: new[]
        {
            "🤒 **{0}** ne va pas bien du tout : il est malade. Un médicament par jour, et vite (╥﹏╥)",
            "Alerte : **{0}** est tombé malade. `/inventory shop`, puis « Soigner » sur sa carte. Tous les jours, hein.",
            "**{0}** a de la fièvre. Je ne panique pas. Je te préviens, c'est tout. Soigne-le chaque jour, d'accord ?",
            "Petit message pour te dire que **{0}** est malade. Un médicament par jour et ça passera. Sans… je préfère ne pas y penser.",
            "**{0}** tousse, renifle et fait une tête de salade fanée. Malade. Soigne-le, je t'en supplie ♡",
            "Mauvaise nouvelle : **{0}** est malade. Bonne nouvelle : ça se soigne. Mais seulement si tu le fais (¬_¬)",
        },
        F: new[]
        {
            "🤒 **{0}** ne va pas bien du tout : elle est malade. Un médicament par jour, et vite (╥﹏╥)",
            "Alerte : **{0}** est tombée malade. `/inventory shop`, puis « Soigner » sur sa carte. Tous les jours, hein.",
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
}
