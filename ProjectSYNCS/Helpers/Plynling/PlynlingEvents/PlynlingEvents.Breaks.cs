using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Mental breaks, one per stress level and then CK3's stress_threshold events. Set in Humeval
// (docs/village-lore.md) since 7.74.2: a break can hit any stage, so nothing assumes where the
// Plynling lives, and every one ends gently, as the village's tone asks.
public static partial class PlynlingEvents
{
    private static EventDef[] Breaks() => new[]
    {
        // ---- mental breaks (one per stress level; triggered when it climbs past one)
        new EventDef("break_cloud", EventType.Triggered, AnyStage, "Le petit nuage gris",
            "Depuis ce matin, {A} traîne un petit nuage gris au-dessus du chapeau. Ce n'est pas un vrai nuage. Ça y ressemble beaucoup, et ça pleut un peu dessus quand personne ne regarde.",
            new[]
            {
                Plain("shout", "Crier un bon coup à la Lisière",
                    "{A} crie si fort que trois corbeaux changent d'arbre. La Mère Lichen ne lève même pas la tête. Le nuage s'en va. Reste une petite humeur de chien.",
                    E(new StressChange(-80), new ApplyModifier("grumpy")), Ai((AiAxis.Boldness, 2), (AiAxis.Vengefulness, 1))),
                Plain("tell", "Aller tout raconter à quelqu'un",
                    "{A} parle longtemps à la Courge, qui écoute, pour une fois, sans rien répéter. Le nuage s'éloigne. Quelque chose a changé dans sa façon de faire face.",
                    E(new StressChange(-70), new GainCoping()), Ai((AiAxis.Sociability, 2))),
                Plain("curl", "Se rouler en boule sous une feuille de bardane",
                    "{A} reste sous la feuille jusqu'au soir. Le nuage finit par s'ennuyer, et va pleuvoir sur les semis du Noyer, qui en avaient besoin.",
                    E(new StressChange(-60)), Ai((AiAxis.Energy, -1), (AiAxis.Sociability, -1))),
            },
            BreakLevel: 1),

        new EventDef("break_drop", EventType.Triggered, AnyStage, "La goutte d'eau",
            "Une miette de travers, et c'est la goutte d'eau. {A} sent quelque chose monter, monter…",
            new[]
            {
                Plain("smash", "Tout casser (un peu)",
                    "Un pot de confiture n'a pas survécu, ni le tabouret. {A} se sent mieux, et un peu {a:honteux|honteuse}.",
                    E(new StressChange(-90), new ApplyModifier("grumpy"), new GainCoping()), Ai((AiAxis.Vengefulness, 2), (AiAxis.Rationality, -2))),
                Plain("walk", "Partir marcher très loin",
                    "{A} marche jusqu'aux Mouilles, et revient à la nuit tombée, {a:couvert|couverte} de boue jusqu'au chapeau, l'air plus léger.",
                    E(new StressChange(-80), new ApplyModifier("muddy_paws")), Ai((AiAxis.Energy, 2))),
                Plain("cry", "Pleurer un bon coup",
                    "{A} pleure contre l'épaule de la Courge, qui sent la soupe et a toujours un torchon propre. « Là, mon chou. Là. »",
                    E(new StressChange(-70)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            },
            BreakLevel: 2),

        new EventDef("break_toomuch", EventType.Triggered, AnyStage, "Trop, c'est trop",
            "{A} n'a plus envie de rien. Même le miel du Tilleul a un goût de rien.",
            new[]
            {
                Plain("hide", "Se terrer chez soi",
                    "{A} tire les rideaux deux jours entiers. À la réouverture, quelque chose a changé.",
                    E(new StressChange(-100), new GainCoping()), Ai((AiAxis.Sociability, -2))),
                Plain("help", "Accepter l'aide de la Sauge",
                    "La Sauge apporte une couverture, une soupe et une tisane imbuvable. « Bois ça, et ne fais pas cette tête. » Ça va un peu mieux. Surtout grâce à la soupe.",
                    E(new StressChange(-65)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("drift", "Se laisser porter",
                    "{A} se laisse flotter quelques jours, comme une feuille sur la Sommeille. Les choses glissent, puis reviennent doucement à leur place.",
                    E(new StressChange(-100), new ApplyModifier("distracted")), Ai((AiAxis.Energy, -2))),
            },
            BreakLevel: 3),

        // More breaks (CK3's stress_threshold events). One rule for every option: a lot of relief with a
        // malus (a negative modifier or a coping trait), or no malus and less relief. A break can hit any
        // stage (PickBreak does not filter), so nothing here is for grown-ups only.
        // level 1 (stress 100+): malus −75/−80, none −45
        new EventDef("break_spiral", EventType.Triggered, AnyStage, "Les pensées qui tournent",
            "Dans la tête de {A}, les pensées tournent comme des feuilles dans le vent : la confiture à finir, la lettre à écrire, la fenêtre restée ouverte, la confiture encore. Impossible d'en attraper une seule. {A} essaie, et la feuille suivante passe déjà.",
            new[]
            {
                Plain("journal", "Tout écrire dans un carnet, pour vider sa tête",
                    "{A} écrit, page après page, jusqu'à ce que la tête soit vide et le carnet plein. Ça marche si bien que, depuis, le carnet ne quitte plus sa poche.",
                    E(new StressChange(-75), new GainCoping("journaller")), Ai((AiAxis.Rationality, 2))),
                Plain("run", "Courir jusqu'à ne plus penser à rien",
                    "{A} court jusqu'au Pont-Vieux, puis jusqu'au moulin, puis revient par la place du Puits. Les pensées sont restées en route. Depuis, au moindre souci, {A} part en courant.",
                    E(new StressChange(-80), new GainCoping("athletic")), Ai((AiAxis.Energy, 2))),
                Plain("tea", "Attendre que ça passe, une tisane entre les mains",
                    "{A} s'assoit avec une tisane et laisse les pensées tourner. Au bout de la deuxième tasse, les pensées ralentissent. Pas toutes. Assez.",
                    E(new StressChange(-45)), Ai((AiAxis.Energy, -1))),
            },
            BreakLevel: 1),

        new EventDef("break_snap", EventType.Triggered, AnyStage, "La moutarde au nez",
            "Maître Frêne fait remarquer à {A}, très poliment, qu'un lacet est défait. C'est la remarque de trop. {A} sent la moutarde monter, monter, et la moutarde ne demande qu'à sortir.",
            new[]
            {
                Plain("slam", "Répondre sèchement, et claquer la porte de l'auberge",
                    "{A} répond quelque chose de très sec et claque la porte de l'auberge si fort que la clochette tombe. Maître Frêne, interdit, refait son propre lacet par réflexe. La colère est sortie. La gêne arrive juste derrière, et reste quelques jours.",
                    E(new StressChange(-80), new ApplyModifier("on_edge")), Ai((AiAxis.Vengefulness, 2))),
                Plain("leaves", "Taper du pied dans tous les tas de feuilles du hameau",
                    "{A} tape du pied dans le premier tas de feuilles, puis dans le deuxième, puis dans tous les tas du hameau. Ça fait un bruit très satisfaisant. Depuis, au moindre agacement, {A} cherche un tas de feuilles, et gare à qui se trouve à côté.",
                    E(new StressChange(-75), new GainCoping("irritable")), Ai((AiAxis.Energy, 1), (AiAxis.Vengefulness, 1))),
                Plain("count", "Compter jusqu'à dix, puis jusqu'à cent",
                    "{A} compte jusqu'à dix. Puis jusqu'à cent. À quatre-vingt-sept, Maître Frêne est reparti vers la Maison commune, et le lacet est toujours défait. La moutarde redescend, lentement, sans tout à fait disparaître.",
                    E(new StressChange(-45)), Ai((AiAxis.Rationality, 2))),
            },
            BreakLevel: 1),

        new EventDef("break_biscuits", EventType.Triggered, AnyStage, "La boîte à biscuits",
            "Rien ne va depuis trois jours, et {A} se retrouve à minuit devant la boîte à biscuits de Pain-Blond, sans trop savoir comment. La boîte est pleine. Pour l'instant.",
            new[]
            {
                Plain("eat", "Tout manger, jusqu'au dernier biscuit",
                    "{A} mange les biscuits un par un, puis deux par deux. Au fond de la boîte, plus que des miettes, et un grand calme. Depuis ce soir-là, la boîte à biscuits est devenue une amie très proche.",
                    E(new StressChange(-80), new GainCoping("comfort_eater")), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, -1))),
                Plain("shut", "Refermer la boîte, et ne plus rien avaler",
                    "{A} referme la boîte d'un geste sec, puis ne mange plus rien de la journée, ni du lendemain. Le ventre proteste. La tête, curieusement, se tait. L'habitude aussi s'installe.",
                    E(new StressChange(-75), new GainCoping("inappetetic")), Ai((AiAxis.Greed, -1), (AiAxis.Zeal, 1))),
                Plain("one", "Un seul biscuit, et retour au lit",
                    "{A} prend un seul biscuit, le mange très lentement, et retourne se coucher. Ce n'est pas grand-chose. C'est déjà ça.",
                    E(new StressChange(-45)), Ai((AiAxis.Rationality, 1))),
            },
            BreakLevel: 1),

        // level 2 (stress 200+): malus −90, none −60
        new EventDef("break_impostor", EventType.Triggered, AnyStage, "Pas à la hauteur",
            "Ce matin, {A} a croisé son reflet dans l'eau du Puits sans le reconnaître. Depuis, une petite voix répète que {A} fait tout de travers, que tout le hameau le sait, et que ce n'est qu'une question de temps avant que quelqu'un le dise tout haut.",
            new[]
            {
                Plain("sorry", "S'excuser auprès de tout le monde, pour tout",
                    "{A} fait le tour du hameau et s'excuse : auprès de la Courge pour une chope cassée, auprès du Pont-Vieux pour rien de précis. Tout le monde est très surpris, le pont aussi. La petite voix se tait enfin, mais {A} a pris l'habitude de s'excuser.",
                    E(new StressChange(-90), new GainCoping("contrite")), Ai((AiAxis.Honor, 2))),
                Plain("hide", "Ne plus sortir, pour que personne ne remarque rien",
                    "{A} reste chez soi trois jours, rideaux tirés : on ne juge pas ce qu'on ne voit pas. Au quatrième jour, la petite voix s'est lassée. Mais dehors, chaque bruit fait sursauter {A}.",
                    E(new StressChange(-90), new ApplyModifier("shaken")), Ai((AiAxis.Sociability, -2))),
                Plain("ask", "Demander au Tilleul : « Je fais tout de travers ? »",
                    "Le Tilleul réfléchit très longtemps, une abeille sur l'épaule. « Tu as renversé mon miel, l'hiver dernier. Ou alors c'était un autre. À part ça, non. » Ce n'est pas tout à fait suffisant. Mais c'est un début.",
                    E(new StressChange(-60)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
            },
            BreakLevel: 2),

        new EventDef("break_list", EventType.Triggered, AnyStage, "La liste sans fin",
            "La liste de {A} fait maintenant trois pages : réparer la barrière, rendre son panier à la Sauge, arroser, écrire, ranger, répondre. Chaque fois qu'une ligne est barrée, deux nouvelles apparaissent. Ce matin, {A} regarde la liste, et la liste regarde {A}.",
            new[]
            {
                Plain("burn", "Brûler la liste dans le feu",
                    "La liste brûle très bien. Pendant une heure, {A} se sent {a:léger|légère} comme une aigrette de pissenlit. Puis la barrière, le panier et l'arrosage reviennent, dans le désordre, et sans liste pour s'y retrouver.",
                    E(new StressChange(-90), new ApplyModifier("tense")), Ai((AiAxis.Rationality, -2), (AiAxis.Boldness, 1))),
                Plain("shop", "Tout laisser, et filer au marché de la place",
                    "{A} laisse tout en plan et file au marché. Une écharpe, deux bocaux, un chapeau à plume et une lanterne dont personne n'a besoin. En rentrant, la liste est toujours là, mais {A} a un nouveau chapeau, et une nouvelle manie.",
                    E(new StressChange(-90), new GainCoping("profligate")), Ai((AiAxis.Greed, 1))),
                Plain("one_line", "Barrer une seule ligne, et s'arrêter là",
                    "{A} choisit la ligne la plus courte, « rendre le panier », et la barre. Puis range la liste dans un tiroir. Le reste attendra. Le reste attend toujours.",
                    E(new StressChange(-60)), Ai((AiAxis.Rationality, 1))),
            },
            BreakLevel: 2),

        new EventDef("break_nightmares", EventType.Triggered, AnyStage, "Le même rêve",
            "Depuis une semaine, {A} dort mal. Toutes les nuits, le même rêve : une charrette qui monte la Côte sans {A}, une porte qui ne s'ouvre pas, et l'Intendant qui dit « trop tard », très lentement, en le notant. Ce soir, {A} n'ose plus fermer les yeux.",
            new[]
            {
                Plain("lantern", "Rester debout toute la nuit, lanterne allumée",
                    "{A} reste {a:assis|assise} jusqu'à l'aube, la lanterne allumée, devant un livre dont les pages ne tournent pas. Pas de rêve, pas de charrette. Mais au matin, les yeux piquent, et les nuits suivantes ne valent guère mieux.",
                    E(new StressChange(-90), new ApplyModifier("sleepless")), Ai((AiAxis.Boldness, 1))),
                Plain("heron", "Tout raconter au Fou, à l'aube",
                    "{A} raconte le rêve au Fou, sur le Pont-Vieux. Le Fou écoute sans rien dire, puis : « Moi, c'est une pierre qui me demande l'heure. Je lui réponds toujours. » Ça fait du bien de ne pas être {a:seul|seule}. Depuis, {A} raconte tout à quelqu'un.",
                    E(new StressChange(-90), new GainCoping("confider")), Ai((AiAxis.Sociability, 2))),
                Plain("pillow", "Changer d'oreiller, et réessayer",
                    "{A} change d'oreiller, de côté, de couverture, puis de coin pour dormir. La nuit est courte, mais le rêve ne revient qu'une fois. On progresse.",
                    E(new StressChange(-60)), Ai((AiAxis.Rationality, 1))),
            },
            BreakLevel: 2),

        // level 3 (stress 300+): malus −110/−120, none −70
        new EventDef("break_empty", EventType.Triggered, AnyStage, "Plus rien n'a de goût",
            "Le miel n'a plus de goût. Le soleil n'a plus de chaleur. Même le pain de Pain-Blond, ce matin, avait un goût de rien. {A} reste {a:assis|assise} sur le lit, à regarder le mur, et le mur n'a rien à dire non plus.",
            new[]
            {
                Plain("door", "Fermer la porte, et ne plus voir personne",
                    "{A} ferme la porte et colle un mot dessus : « Plus tard. » Les jours passent derrière la porte. Le vide s'use, lentement, comme une semelle. Quand la porte se rouvre, quelque chose a changé pour de bon.",
                    E(new StressChange(-110), new GainCoping("reclusive")), Ai((AiAxis.Sociability, -2))),
                Plain("give", "Tout donner, puisque plus rien ne compte",
                    "{A} distribue son écharpe, ses plus beaux glands et sa tasse préférée au premier venu. Le premier venu, très gêné, rapporte la tasse le lendemain. Le reste ne revient pas. Un poids est parti, et une drôle d'habitude est née.",
                    E(new StressChange(-110), new GainCoping("improvident")), Ai((AiAxis.Greed, -2))),
                Plain("window", "Rester là, et laisser le temps faire",
                    "{A} reste là. Le soir, le Fou passe sous la fenêtre en comptant tout bas, puis repasse le lendemain matin. Le troisième jour, {A} ouvre la fenêtre pour demander ce qu'il compte. « Les jours où tu n'as pas ouvert. Trois. On s'arrête là ? » C'est peu. C'est quelque chose.",
                    E(new StressChange(-70)), Ai((AiAxis.Energy, -2))),
            },
            BreakLevel: 3),

        new EventDef("break_storm", EventType.Triggered, AnyStage, "La tempête",
            "Tout le monde, aujourd'hui, est insupportable. La Courge est trop bavarde, la Mère Lichen trop silencieuse, l'Intendant trop exact, le soleil trop jaune. {A} sent monter une tempête, une vraie, avec des éclairs.",
            new[]
            {
                Plain("burst", "Laisser éclater la tempête, sur tout le monde",
                    "{A} dit ses quatre vérités à la Courge, à l'Intendant, au coq du Noyer, et même au soleil. Le hameau se tait. Le soir, {A} va beaucoup mieux, et le hameau beaucoup moins. Le lendemain, il faut faire le tour des excuses, et la Courge en a déjà fait une histoire.",
                    E(new StressChange(-120), new ApplyModifier("on_edge")), Ai((AiAxis.Vengefulness, 2))),
                Plain("weeds", "Arracher toutes les mauvaises herbes du hameau",
                    "{A} arrache les mauvaises herbes du jardin, puis celles du voisin, puis celles de la place du Puits. Le hameau n'a jamais été aussi propre. La tempête s'épuise dans les racines, mais les mains en tremblent encore trois jours.",
                    E(new StressChange(-110), new ApplyModifier("tense")), Ai((AiAxis.Energy, 2))),
                Plain("hole", "Crier dans un trou, au fond du jardin",
                    "{A} creuse un trou au fond du jardin, crie dedans tout ce qui doit sortir, et rebouche le trou. En passant, {A} dit « pardon » à un fil de la Trame. Personne n'a rien entendu. Une partie de la tempête dort maintenant sous les fraisiers.",
                    E(new StressChange(-70)), Ai((AiAxis.Rationality, 1))),
            },
            BreakLevel: 3),

        new EventDef("break_flee", EventType.Triggered, AnyStage, "Partir loin",
            "Un matin, {A} se réveille avec une seule idée : partir. N'importe où, mais loin d'ici, loin de tout ce qui pèse. Le baluchon est à moitié fait avant même que {A} ait décidé quoi que ce soit.",
            new[]
            {
                Plain("go", "Partir sans prévenir personne",
                    "{A} monte la Côte à l'aube, marche jusqu'au soir sans se retourner, et dort sous une haie. Au retour, le lendemain, tout le hameau guettait sur la place. {A} revient plus calme, mais {a:secoué|secouée}, et sursaute au moindre craquement de branche.",
                    E(new StressChange(-120), new ApplyModifier("shaken")), Ai((AiAxis.Boldness, 2), (AiAxis.Sociability, -1))),
                Plain("train", "Monter dans la carriole du colporteur, sans billet de retour",
                    "Le colporteur fait de la place sans poser de question, mais en levant un sourcil. {A} descend au premier tournant de la Côte, lui achète un bocal de confiture inconnue, une lanterne et un chapeau, et rentre à pied. Le voyage a tout réglé. Les achats, eux, ne font que commencer.",
                    E(new StressChange(-110), new GainCoping("profligate")), Ai((AiAxis.Boldness, 1), (AiAxis.Greed, 1))),
                Plain("garden", "Partir… jusqu'au bout du jardin",
                    "{A} prend le baluchon, marche jusqu'au bout du jardin, s'assoit sous le pommier et mange les tartines du voyage. Le grand départ attendra. Le pommier, lui, a passé une très bonne journée.",
                    E(new StressChange(-70)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            },
            BreakLevel: 3),
    };
}
