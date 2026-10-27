using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Mental breaks, one per stress level and then CK3's stress_threshold events.
public static partial class PlynlingEvents
{
    private static EventDef[] Breaks() => new[]
    {
        // ---- mental breaks (one per stress level; triggered when it climbs past one)
        new EventDef("break_cloud", EventType.Triggered, AnyStage, "Le petit nuage noir",
            "Depuis ce matin, un petit nuage noir suit {A} partout, et pleut un peu dessus quand personne ne regarde.",
            new[]
            {
                Plain("shout", "Crier un bon coup dans la forêt",
                    "{A} crie si fort que trois corbeaux changent d'adresse. Le nuage, impressionné, s'en va. Reste une petite humeur de chien.",
                    E(new StressChange(-80), new ApplyModifier("grumpy")), Ai((AiAxis.Boldness, 2), (AiAxis.Vengefulness, 1))),
                Plain("tell", "Aller tout raconter à quelqu'un",
                    "{A} parle longtemps. Le nuage écoute aussi, puis s'éloigne. Quelque chose a changé dans sa façon de faire face.",
                    E(new StressChange(-70), new GainCoping()), Ai((AiAxis.Sociability, 2))),
                Plain("curl", "Se rouler en boule sous une feuille",
                    "{A} reste sous la feuille jusqu'au soir. Le nuage finit par s'ennuyer.",
                    E(new StressChange(-60)), Ai((AiAxis.Energy, -1), (AiAxis.Sociability, -1))),
            },
            BreakLevel: 1),

        new EventDef("break_drop", EventType.Triggered, AnyStage, "La goutte d'eau",
            "Une miette de travers, et c'est la goutte d'eau. {A} sent quelque chose monter, monter…",
            new[]
            {
                Plain("smash", "Tout casser (un peu)",
                    "Un pot de confiture n'a pas survécu. {A} se sent mieux, et un peu {a:honteux|honteuse}.",
                    E(new StressChange(-90), new ApplyModifier("grumpy"), new GainCoping()), Ai((AiAxis.Vengefulness, 2), (AiAxis.Rationality, -2))),
                Plain("walk", "Partir marcher très loin",
                    "{A} revient à la nuit tombée, {a:couvert|couverte} de boue jusqu'aux oreilles, l'air plus léger.",
                    E(new StressChange(-80), new ApplyModifier("muddy_paws")), Ai((AiAxis.Energy, 2))),
                Plain("cry", "Pleurer un bon coup",
                    "{A} pleure contre la carapace de la tortue du café, qui a toujours un mouchoir propre.",
                    E(new StressChange(-70)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            },
            BreakLevel: 2),

        new EventDef("break_toomuch", EventType.Triggered, AnyStage, "Trop, c'est trop",
            "{A} n'a plus envie de rien. Même le miel a un goût de rien.",
            new[]
            {
                Plain("hide", "Se terrer chez soi",
                    "{A} ferme les volets deux jours entiers. À la réouverture, quelque chose a changé.",
                    E(new StressChange(-100), new GainCoping()), Ai((AiAxis.Sociability, -2))),
                Plain("help", "Accepter l'aide du hérisson",
                    "Le hérisson apporte une couverture, une soupe, et ses horaires de train préférés, à lire pour s'endormir.",
                    E(new StressChange(-65)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("drift", "Se laisser porter",
                    "{A} se laisse flotter quelques jours. Les choses glissent, puis reviennent doucement à leur place.",
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
                    "{A} court jusqu'au vieux pont, puis jusqu'à la gare, puis revient. Les pensées sont restées en route. Depuis, au moindre souci, {A} enfile ses chaussures de course.",
                    E(new StressChange(-80), new GainCoping("athletic")), Ai((AiAxis.Energy, 2))),
                Plain("tea", "Attendre que ça passe, une tisane à la patte",
                    "{A} s'assoit avec une tisane et laisse les pensées tourner. Au bout de la deuxième tasse, les pensées ralentissent. Pas toutes. Assez.",
                    E(new StressChange(-45)), Ai((AiAxis.Energy, -1))),
            },
            BreakLevel: 1),

        new EventDef("break_snap", EventType.Triggered, AnyStage, "La moutarde au nez",
            "Le hérisson fait remarquer à {A}, très gentiment, qu'un lacet est défait. C'est la remarque de trop. {A} sent la moutarde monter, monter, et la moutarde ne demande qu'à sortir.",
            new[]
            {
                Plain("slam", "Répondre sèchement, et claquer la porte du café",
                    "{A} répond quelque chose de très sec et claque la porte du café si fort que la clochette tombe. Le hérisson, interdit, refait son propre lacet par réflexe. La colère est sortie. La gêne arrive juste derrière, et reste quelques jours.",
                    E(new StressChange(-80), new ApplyModifier("on_edge")), Ai((AiAxis.Vengefulness, 2))),
                Plain("leaves", "Taper du pied dans tous les tas de feuilles du village",
                    "{A} tape du pied dans le premier tas de feuilles, puis dans le deuxième, puis dans tous les tas du village. Ça fait un bruit très satisfaisant. Depuis, au moindre agacement, {A} cherche un tas de feuilles, et gare à qui se trouve à côté.",
                    E(new StressChange(-75), new GainCoping("irritable")), Ai((AiAxis.Energy, 1), (AiAxis.Vengefulness, 1))),
                Plain("count", "Compter jusqu'à dix, puis jusqu'à cent",
                    "{A} compte jusqu'à dix. Puis jusqu'à cent. À quatre-vingt-sept, le hérisson est parti, et le lacet est toujours défait. La moutarde redescend, lentement, sans tout à fait disparaître.",
                    E(new StressChange(-45)), Ai((AiAxis.Rationality, 2))),
            },
            BreakLevel: 1),

        new EventDef("break_biscuits", EventType.Triggered, AnyStage, "La boîte à biscuits",
            "Rien ne va depuis trois jours, et {A} se retrouve à minuit devant la boîte à biscuits, sans trop savoir comment. La boîte est pleine. Pour l'instant.",
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
            "Ce matin, {A} a croisé son reflet dans une flaque sans le reconnaître. Depuis, une petite voix répète que {A} fait tout de travers, que tout le village le sait, et que ce n'est qu'une question de temps avant que quelqu'un le dise tout haut.",
            new[]
            {
                Plain("sorry", "S'excuser auprès de tout le monde, pour tout",
                    "{A} fait le tour du village et s'excuse : auprès de la tortue pour une tasse cassée l'an dernier, auprès du héron pour rien de précis. Tout le monde est très surpris. La petite voix se tait enfin, mais {A} a pris l'habitude de s'excuser.",
                    E(new StressChange(-90), new GainCoping("contrite")), Ai((AiAxis.Honor, 2))),
                Plain("hide", "Ne plus sortir, pour que personne ne remarque rien",
                    "{A} reste chez soi trois jours, rideaux tirés : on ne juge pas ce qu'on ne voit pas. Au quatrième jour, la petite voix s'est lassée. Mais dehors, chaque bruit fait sursauter {A}.",
                    E(new StressChange(-90), new ApplyModifier("shaken")), Ai((AiAxis.Sociability, -2))),
                Plain("ask", "Demander à la tortue : « Je fais tout de travers ? »",
                    "La tortue réfléchit très longtemps, comme toujours, puis répond : « Tu as renversé du sucre l'an dernier. À part ça, non. » Ce n'est pas tout à fait suffisant. Mais c'est un début.",
                    E(new StressChange(-60)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
            },
            BreakLevel: 2),

        new EventDef("break_list", EventType.Triggered, AnyStage, "La liste sans fin",
            "La liste de {A} fait maintenant trois pages : réparer la barrière, rendre le livre à la chouette, arroser, écrire, ranger, répondre. Chaque fois qu'une ligne est barrée, deux nouvelles apparaissent. Ce matin, {A} regarde la liste, et la liste regarde {A}.",
            new[]
            {
                Plain("burn", "Brûler la liste dans la cheminée",
                    "La liste brûle très bien. Pendant une heure, {A} se sent {a:léger|légère} comme une plume. Puis la barrière, le livre et l'arrosage reviennent, dans le désordre, et sans liste pour s'y retrouver.",
                    E(new StressChange(-90), new ApplyModifier("tense")), Ai((AiAxis.Rationality, -2), (AiAxis.Boldness, 1))),
                Plain("shop", "Tout laisser, et filer au marché",
                    "{A} laisse tout en plan et file au marché. Une écharpe, deux bocaux, un chapeau à plume et une lampe dont personne n'a besoin. En rentrant, la liste est toujours là, mais {A} a un nouveau chapeau, et une nouvelle manie.",
                    E(new StressChange(-90), new GainCoping("profligate")), Ai((AiAxis.Greed, 1))),
                Plain("one_line", "Barrer une seule ligne, et s'arrêter là",
                    "{A} choisit la ligne la plus courte, « rendre le livre », et la barre. Puis range la liste dans un tiroir. Le reste attendra. Le reste attend toujours.",
                    E(new StressChange(-60)), Ai((AiAxis.Rationality, 1))),
            },
            BreakLevel: 2),

        new EventDef("break_nightmares", EventType.Triggered, AnyStage, "Le même rêve",
            "Depuis une semaine, {A} dort mal. Toutes les nuits, le même rêve : un train qui part sans {A}, une porte qui ne s'ouvre pas, et la tortue qui dit « trop tard », très lentement. Ce soir, {A} n'ose plus fermer les yeux.",
            new[]
            {
                Plain("lantern", "Rester debout toute la nuit, lanterne allumée",
                    "{A} reste {a:assis|assise} jusqu'à l'aube, la lanterne allumée, devant un livre dont les pages ne tournent pas. Pas de rêve, pas de train. Mais au matin, les yeux piquent, et les nuits suivantes ne valent guère mieux.",
                    E(new StressChange(-90), new ApplyModifier("sleepless")), Ai((AiAxis.Boldness, 1))),
                Plain("heron", "Tout raconter au héron, à l'aube",
                    "{A} raconte le rêve au héron, sur le vieux pont. Le héron écoute sans rien dire, puis dit : « Moi, c'est un poisson qui me demande l'heure. » Ça fait du bien de ne pas être {a:seul|seule}. Depuis, {A} raconte tout à quelqu'un.",
                    E(new StressChange(-90), new GainCoping("confider")), Ai((AiAxis.Sociability, 2))),
                Plain("pillow", "Changer d'oreiller, et réessayer",
                    "{A} change d'oreiller, de côté, de couverture, puis de chambre. La nuit est courte, mais le rêve ne revient qu'une fois. On progresse.",
                    E(new StressChange(-60)), Ai((AiAxis.Rationality, 1))),
            },
            BreakLevel: 2),

        // level 3 (stress 300+): malus −110/−120, none −70
        new EventDef("break_empty", EventType.Triggered, AnyStage, "Plus rien n'a de goût",
            "Le miel n'a plus de goût. Le soleil n'a plus de chaleur. Même le chocolat de la tortue, ce matin, avait un goût de rien. {A} reste {a:assis|assise} sur le lit, à regarder le mur, et le mur n'a rien à dire non plus.",
            new[]
            {
                Plain("door", "Fermer la porte, et ne plus voir personne",
                    "{A} ferme la porte et colle un mot dessus : « Plus tard. » Les jours passent derrière la porte. Le vide s'use, lentement, comme une semelle. Quand la porte se rouvre, quelque chose a changé pour de bon.",
                    E(new StressChange(-110), new GainCoping("reclusive")), Ai((AiAxis.Sociability, -2))),
                Plain("give", "Tout donner, puisque plus rien ne compte",
                    "{A} distribue son écharpe, ses plus beaux glands et sa tasse préférée au premier venu. Le premier venu, très gêné, rapporte la tasse le lendemain. Le reste ne revient pas. Un poids est parti, et une drôle d'habitude est née.",
                    E(new StressChange(-110), new GainCoping("improvident")), Ai((AiAxis.Greed, -2))),
                Plain("window", "Rester là, et laisser le temps faire",
                    "{A} reste là. Le soir, l'escargot passe sous la fenêtre, puis repasse le lendemain matin, sans rien dire. Le troisième jour, {A} ouvre la fenêtre pour dire bonjour. C'est peu. C'est quelque chose.",
                    E(new StressChange(-70)), Ai((AiAxis.Energy, -2))),
            },
            BreakLevel: 3),

        new EventDef("break_storm", EventType.Triggered, AnyStage, "La tempête",
            "Tout le monde, aujourd'hui, est insupportable. La pie est trop bavarde, le héron trop silencieux, l'escargot trop lent, le soleil trop jaune. {A} sent monter une tempête, une vraie, avec des éclairs.",
            new[]
            {
                Plain("burst", "Laisser éclater la tempête, sur tout le monde",
                    "{A} dit ses quatre vérités à la pie, au héron, à l'escargot, et même au soleil. Le village se tait. Le soir, {A} va beaucoup mieux, et le village beaucoup moins. Les excuses prendront des semaines.",
                    E(new StressChange(-120), new ApplyModifier("on_edge")), Ai((AiAxis.Vengefulness, 2))),
                Plain("weeds", "Arracher toutes les mauvaises herbes du village",
                    "{A} arrache les mauvaises herbes du jardin, puis celles du voisin, puis celles de la place. Le village n'a jamais été aussi propre. La tempête s'épuise dans les racines, mais les pattes en tremblent encore trois jours.",
                    E(new StressChange(-110), new ApplyModifier("tense")), Ai((AiAxis.Energy, 2))),
                Plain("hole", "Crier dans un trou, au fond du jardin",
                    "{A} creuse un trou au fond du jardin, crie dedans tout ce qui doit sortir, et rebouche le trou. Personne n'a rien entendu. Une partie de la tempête dort maintenant sous les fraisiers.",
                    E(new StressChange(-70)), Ai((AiAxis.Rationality, 1))),
            },
            BreakLevel: 3),

        new EventDef("break_flee", EventType.Triggered, AnyStage, "Partir loin",
            "Un matin, {A} se réveille avec une seule idée : partir. N'importe où, mais loin d'ici, loin de tout ce qui pèse. Le baluchon est à moitié fait avant même que {A} ait décidé quoi que ce soit.",
            new[]
            {
                Plain("go", "Partir sans prévenir personne",
                    "{A} part à l'aube et marche trois jours, sans but, en dormant sous les haies. Au retour, le village a eu très peur. {A} revient plus calme, mais {a:secoué|secouée}, et sursaute au moindre craquement de branche.",
                    E(new StressChange(-120), new ApplyModifier("shaken")), Ai((AiAxis.Boldness, 2), (AiAxis.Sociability, -1))),
                Plain("train", "Prendre le premier train, avec un aller simple",
                    "Le hérisson poinçonne le billet sans poser de question, mais en levant un sourcil. {A} descend au premier arrêt, achète un bocal de confiture inconnue, une lanterne et un chapeau, et reprend le train du retour. Le voyage a tout réglé. Les achats, eux, ne font que commencer.",
                    E(new StressChange(-110), new GainCoping("profligate")), Ai((AiAxis.Boldness, 1), (AiAxis.Greed, 1))),
                Plain("garden", "Partir… jusqu'au bout du jardin",
                    "{A} prend le baluchon, marche jusqu'au bout du jardin, s'assoit sous le pommier et mange les tartines du voyage. Le grand départ attendra. Le pommier, lui, a passé une très bonne journée.",
                    E(new StressChange(-70)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            },
            BreakLevel: 3),
    };
}
