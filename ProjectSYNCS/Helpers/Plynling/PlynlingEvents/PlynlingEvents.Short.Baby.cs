using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 8 short events: bébé.
public static partial class PlynlingEvents
{
    private static EventDef[] ShortBaby() => new[]
    {
        // ==== wave 8, short events: one scene, two or three quick choices, no sequel (CK3's yearly flavour
        // events: Comet Sighted!, Peek-a-boo!, The Flower Thief, Snide Remarks, Lost and Found, Old Regrets…)

        // ---- bébé
        new EventDef("baby_comet", EventType.Pulse, Baby, "La comète",
            "Ce soir, une étoile à longue traîne traverse le ciel, très lentement. Tout le village est dehors, le nez en l'air. {A} n'a jamais rien vu d'aussi beau, ni d'aussi long.",
            new[]
            {
                Plain("wish", "Faire un vœu, très fort",
                    "{A} fait un vœu en serrant les poings si fort que les oreilles tremblent. Le vœu reste secret. Le lendemain, {A} vérifie trois fois. Pas encore. Mais bientôt.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Compassion, 1))),
                Plain("chase", "Courir derrière la comète",
                    "{A} court derrière la comète jusqu'au bout du champ. La comète gagne. {A} rentre {a:essoufflé|essoufflée}, et très {a:fier|fière} d'avoir fait la course avec une comète.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Energy, 2))),
                Plain("owl", "Demander à la chouette ce que c'est",
                    "La chouette explique : une boule de glace qui voyage, et qui repassera dans soixante-seize ans. {A} décide de l'attendre. Ça laisse le temps de goûter.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2))),
            }),

        new EventDef("baby_tower", EventType.Pulse, Baby, "La tour de cubes",
            "{A} construit une tour de cubes en bois. La tour arrive déjà aux genoux de l'ours. Un dernier cube attend, dans la patte de {A}.",
            new[]
            {
                Try("top", "Poser le dernier cube tout en haut", new EventChallenge(PlynlingStat.Stewardship, 4),
                    "Le cube tient. La tour tient. {A} ne respire plus pendant une minute entière, puis applaudit si fort que tout s'écroule. Ça valait le coup.",
                    "La tour penche, hésite, et s'effondre dans un fracas magnifique. {A} trouve ça encore mieux.",
                    E(new GrowStat(PlynlingStat.Stewardship)), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Rationality, 1))),
                Plain("crash", "Tout renverser d'un coup de patte",
                    "{A} donne un grand coup de patte. Les cubes volent partout, un atterrit dans la soupe. C'était le but depuis le début.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Energy, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("baby_peekaboo", EventType.Pulse, Baby, "Coucou, caché",
            "{A} a trouvé la meilleure cachette du monde : derrière le rideau. On voit les pieds qui dépassent. Le hérisson, en visite, fait semblant de chercher partout.",
            new[]
            {
                Plain("still", "Ne surtout pas bouger",
                    "{A} ne bouge pas pendant dix minutes. Le hérisson cherche sous le tapis, dans la théière, derrière le pain, puis abandonne, très déçu. {A} sort en triomphe.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1))),
                Plain("giggle", "Pouffer de rire",
                    "Le rideau pouffe. Le hérisson s'arrête et tend l'oreille. « Quel drôle de rideau. » Le rideau éclate de rire.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 1))),
                Plain("boo", "Surgir d'un coup : « Coucou ! »",
                    "{A} surgit : « COUCOU ! » Le hérisson fait un bond, perd une épine, et l'offre à {A} en souvenir.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 2))),
            }),

        new EventDef("baby_sweets", EventType.Pulse, Baby, "Les bonbons du haut",
            "Sur l'étagère du haut trône le bocal de bonbons au miel. {A} a déjà empilé deux coussins et un tabouret. Personne ne regarde.",
            new[]
            {
                Try("climb", "Grimper, en silence", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} grimpe, attrape un bonbon, redescend, remet tout en place. Crime parfait. Sauf la petite trace de miel sur le nez.",
                    "Le tabouret vacille, les coussins glissent, le bocal tombe. Pas un bonbon de cassé. Mais beaucoup d'explications à donner.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Greed, 1), (AiAxis.Boldness, 1))),
                Plain("ask", "Demander poliment",
                    "{A} demande, très poliment, avec la formule magique. Ça marche. Deux bonbons, même, pour la politesse.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new LiftNeed(Need.Hunger, 0.1)), Ai((AiAxis.Honor, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("baby_doll", EventType.Pulse, Baby, "La poupée oubliée",
            "Sur un banc du parc, quelqu'un a oublié une poupée de chiffon. La poupée regarde le ciel de ses yeux en boutons, l'air un peu triste. Le soir tombe.",
            new[]
            {
                Plain("stay", "Rester à côté, pour lui tenir compagnie",
                    "{A} s'assoit à côté et raconte des histoires à la poupée, jusqu'à ce qu'une petite souris arrive en courant, en larmes. La souris serre la poupée, puis {A}, puis la poupée encore.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
                Plain("kiosk", "La porter au kiosque des objets trouvés",
                    "L'ours range la poupée sur l'étagère des objets trouvés, entre un parapluie et une chaussette orpheline. « Quelqu'un viendra. » Quelqu'un vient, le lendemain matin.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Honor, 1))),
            }),

        new EventDef("baby_funny_words", EventType.Pulse, Baby, "Vieille comme le pain",
            "Au café, {A} annonce à voix haute que la tortue est « vieille comme le pain ». Silence. Toutes les têtes se tournent vers la tortue.",
            new[]
            {
                Plain("explain", "Expliquer : le pain, c'est vieux, et c'est le meilleur",
                    "« Le pain, c'est très vieux, et c'est le meilleur. » La tortue réfléchit, puis pose une tartine devant {A}. Le compliment a été accepté.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Sociability, 1))),
                Plain("hide", "Disparaître sous la table",
                    "{A} disparaît sous la table. La tortue rit si fort que les tasses tremblent. « Vieille comme le pain. Je vais le faire broder. »",
                    E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Boldness, -1))),
            }),

        new EventDef("baby_pirate", EventType.Pulse, Baby, "Le capitaine du bac à sable",
            "Le bac à sable est devenu un océan, la pelle un mât, et {A} le capitaine. À l'horizon avance l'escargot : c'est évidemment un navire ennemi.",
            new[]
            {
                Plain("board", "À l'abordage !",
                    "{A} aborde l'escargot en criant. L'escargot, beau joueur, se rend, et paie la rançon : une feuille de salade, un peu mâchée.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 2))),
                Plain("treaty", "Proposer un traité de paix",
                    "{A} et l'escargot signent un traité dans le sable : le bac est partagé. L'escargot reste jusqu'au soir, à faire le phare.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("dig", "Creuser pour trouver le trésor",
                    "{A} creuse, creuse, et trouve une bille bleue, enterrée là par un autre capitaine, autrefois.",
                    E(new GiveItem("col.bille")), Ai((AiAxis.Greed, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("baby_nightmare", EventType.Pulse, Baby, "Le mauvais rêve",
            "{A} se réveille en pleine nuit, le cœur battant : dans le rêve, une limace géante mangeait toutes les tartines du monde. La chambre est très noire.",
            new[]
            {
                Plain("candle", "Allumer la bougie, et vérifier sous le lit",
                    "Sous le lit : une chaussette, un gland, aucune limace. {A} se rendort, la bougie allumée, juste au cas où.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
                Plain("owl", "Aller frapper chez la chouette, à côté",
                    "La chouette ouvre, écoute, et explique que les limaces géantes ne mangent pas de tartines, seulement des nuages. C'est très rassurant. {A} dort jusqu'au matin.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Sociability, 1))),
                Plain("toast", "Manger une tartine, pour la sauver",
                    "{A} file à la cuisine et mange une tartine, pour qu'au moins celle-là soit sauvée. Mission accomplie. Retour au lit, le ventre content.",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Greed, 1))),
            }),

        new EventDef("baby_slope", EventType.Pulse, Baby, "La pente",
            "Derrière la gare, une longue pente d'herbe descend jusqu'à la rivière. {A} la regarde depuis le haut. La pente, en bas, a l'air de faire signe.",
            new[]
            {
                Try("roll", "Se laisser rouler jusqu'en bas", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} roule, roule, roule, et s'arrête pile au bord de l'eau, la tête qui tourne et le ventre qui rit. Puis remonte, pour recommencer onze fois.",
                    "{A} roule, rebondit sur une motte, et termine dans les roseaux, {a:coiffé|coiffée} d'un nénuphar. La grenouille du coin, propriétaire du nénuphar, réclame qu'on le rende.",
                    E(new GrowStat(PlynlingStat.Courage), new LiftNeed(Need.Happiness, 0.1)), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
                Plain("sled", "Descendre sur une feuille, comme en traîneau",
                    "{A} trouve une grande feuille de rhubarbe et descend dessus comme sur un traîneau. Le soir, la moitié des petits du village fait la queue avec sa feuille.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("baby_apple_tree", EventType.Pulse, Baby, "Tout en haut du pommier",
            "{A} a grimpé dans le pommier pour attraper une pomme. La pomme est attrapée. Le problème, c'est la descente : la branche du bas paraît maintenant très, très loin.",
            new[]
            {
                Plain("call", "Appeler à l'aide",
                    "{A} appelle. L'ours arrive, tend les bras, et {A} saute dedans. L'ours fait semblant que {A} pèse très lourd, puis dépose {A} par terre, avec la pomme.",
                    E(new LiftNeed(Need.Hunger, 0.1)), Ai((AiAxis.Sociability, 1))),
                Try("down", "Redescendre sans aide, branche par branche", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} redescend, une branche, puis une autre, la pomme entre les dents. En bas, {A} croque la pomme comme une médaille.",
                    "{A} rate la dernière branche et atterrit dans un tas de feuilles mortes, la pomme toujours en patte. Bilan : zéro dégât, une pomme.",
                    E(new GrowStat(PlynlingStat.Courage)), E(new LiftNeed(Need.Hunger, 0.1)), Ai((AiAxis.Boldness, 2))),
                Plain("wait", "Attendre que le pommier se baisse",
                    "{A} attend. Le pommier ne se baisse pas. Au bout d'une heure, le héron passe, tend son long cou, et dépose {A} en bas, comme on cueille un fruit.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Energy, -1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("baby_secret_language", EventType.Pulse, Baby, "La langue secrète",
            "{A} a inventé une langue que personne d'autre ne parle. « Blouf », ça veut dire bonjour. « Plimpli », ça veut dire biscuit. Le problème, c'est que personne ne comprend les commandes au café.",
            new[]
            {
                Plain("teach", "Apprendre la langue à la tortue",
                    "La tortue apprend vite : au bout d'une semaine, la tortue sert les « plimpli » sans qu'on les demande deux fois. C'est sa première langue étrangère.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
                Plain("dictionary", "Écrire un dictionnaire",
                    "{A} écrit un dictionnaire de onze mots, avec des dessins. La chouette le range à la bibliothèque, au rayon « langues rares ».",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
                Plain("biscuit", "Redemander un biscuit, avec les mots de tout le monde",
                    "{A} redemande un biscuit, en vrai français. Ça marche tout de suite. Les langues secrètes, c'est joli, mais les biscuits, c'est mieux.",
                    E(new LiftNeed(Need.Hunger, 0.1)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("baby_butterfly", EventType.Pulse, Baby, "Le papillon dans le bocal",
            "{A} a attrapé un papillon dans un bocal. Le papillon est bleu, magnifique, et cogne doucement contre le verre.",
            new[]
            {
                Plain("free", "Ouvrir le bocal",
                    "{A} ouvre le bocal. Le papillon reste une seconde sur le bord, puis s'envole, fait un tour au-dessus de {A}, et s'en va. Ça ressemblait beaucoup à un merci.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2))),
                Plain("draw", "Le dessiner d'abord, puis le libérer",
                    "{A} dessine le papillon, toutes les taches, une par une. Puis ouvre le bocal. Le dessin est accroché au mur. Le papillon, au ciel.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Compassion, 1))),
            }),

        new EventDef("baby_market", EventType.Pulse, Baby, "Un caillou au marché",
            "Pour la première fois, {A} va au marché avec un caillou à dépenser. Un seul. Les étals sont immenses, et tout coûte un caillou.",
            new[]
            {
                Plain("honey", "Acheter un bâton de miel",
                    "{A} achète un bâton de miel et le mange très lentement, pour que le caillou dure longtemps.",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Greed, 1))),
                Plain("flower", "Acheter une fleur, pour l'offrir",
                    "{A} achète une fleur et l'offre au premier qui passe : le hérisson, qui ne sait pas du tout quoi en faire, et la garde toute la journée à la boutonnière.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 2))),
                Plain("save", "Garder le caillou, pour plus tard",
                    "{A} garde le caillou au fond de la poche, et rentre sans rien. Le soir, le caillou est toujours là. C'est une sensation très nouvelle.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Greed, 1))),
            }),

        new EventDef("baby_rainy_day", EventType.Pulse, Baby, "Le jour de pluie",
            "La pluie tombe depuis ce matin, sans arrêt. Pas de jardin, pas de flaque, pas de copains. {A} colle le nez à la vitre, et soupire très fort, pour que tout le monde entende.",
            new[]
            {
                Plain("fort", "Construire une cabane de couvertures",
                    "{A} construit une cabane avec toutes les couvertures de la maison et deux chaises. À l'intérieur, le soleil est revenu, au moins dans la tête.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Energy, 1))),
                Plain("drops", "Faire la course des gouttes sur la vitre",
                    "{A} choisit une goutte, l'encourage, la regarde descendre. La goutte gagne. Puis perd. Puis regagne. Le championnat dure tout l'après-midi.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
                Plain("out", "Sortir quand même, en ciré",
                    "{A} sort en ciré jaune et saute dans toutes les flaques du village. Le jour de pluie devient le meilleur jour de la semaine.",
                    E(new LiftNeed(Need.Happiness, 0.2), new ApplyModifier("muddy_paws")), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("baby_bear_birthday", EventType.Pulse, Baby, "Le réveil de l'ours",
            "L'ours rouvre sa boutique après tout un hiver à dormir. Tout le village a apporté un cadeau pour fêter son retour. {A} n'a rien, et la fête commence dans une heure.",
            new[]
            {
                Plain("draw", "Faire un dessin de l'ours",
                    "{A} dessine l'ours, avec une couronne et beaucoup trop de dents. L'ours encadre le dessin et l'accroche au-dessus de la caisse. Les autres cadeaux vont au placard.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1))),
                Plain("song", "Inventer une chanson",
                    "{A} chante une chanson inventée, qui fait rimer « ours » avec « course », « douce » et « mousse ». L'ours la fait rechanter quatre fois.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 2))),
                Plain("pebble", "Offrir son plus beau caillou",
                    "{A} offre son plus beau caillou, rond et blanc. L'ours le reçoit comme un diamant, et le pose sur le comptoir, à côté de la caisse, pour toujours.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("baby_puppy", EventType.Pulse, Baby, "Le chiot perdu",
            "Un chiot tout rond suit {A} depuis la boulangerie. Quand {A} s'arrête, le chiot s'arrête. Quand {A} repart, le chiot repart. Le chiot n'a pas de collier.",
            new[]
            {
                Plain("home", "Chercher sa maison",
                    "{A} et le chiot font le tour du village. À la ferme d'en haut, une grande chienne attend sur le seuil, et lèche le chiot de la tête aux pieds. {A} reçoit une part de tarte pour la peine.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1))),
                Plain("play", "Jouer avec le chiot jusqu'au soir",
                    "{A} et le chiot jouent à la balle, au bâton, à la course. Le soir, le chiot rentre chez soi tout seul : le chemin, le chiot le connaissait depuis le début.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Energy, 2))),
            }),

        new EventDef("baby_daisy_crown", EventType.Pulse, Baby, "La couronne de pâquerettes",
            "{A} a tressé une couronne de pâquerettes. La couronne est un peu de travers, mais c'est la première. Reste à décider qui la portera.",
            new[]
            {
                Plain("wear", "La porter soi-même, toute la journée",
                    "{A} porte la couronne toute la journée, et salue le village d'un geste très royal. Le village salue en retour. Le moineau fait même une révérence.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 1))),
                Plain("heron", "La poser sur la tête du héron",
                    "{A} grimpe sur le parapet et pose la couronne sur la tête du héron. Le héron ne bouge pas. Le héron la garde jusqu'au soir, très digne.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("baby_name", EventType.Pulse, Baby, "Son nom en lettres",
            "{A} vient d'apprendre à écrire son nom. Toutes les lettres, dans le bon ordre, presque. Le crayon brûle d'en écrire plus.",
            new[]
            {
                Plain("everywhere", "L'écrire partout",
                    "{A} écrit son nom sur le mur, sur la porte, et sur le dos du blaireau qui faisait la sieste. Le blaireau porte le nom de {A} une semaine entière, sans le savoir.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Boldness, 1))),
                Plain("letter", "Écrire une lettre à la chouette",
                    "{A} écrit une lettre à la chouette : son nom, et un dessin. La chouette répond le lendemain, avec son propre nom et un dessin aussi, plus raté.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("baby_thunder", EventType.Pulse, Baby, "Le tonnerre",
            "Un coup de tonnerre fait trembler les vitres. Puis un autre. {A} a déjà disparu sous la couverture. Seul le bout du nez dépasse.",
            new[]
            {
                Plain("count", "Compter entre l'éclair et le tonnerre",
                    "La chouette l'a dit : on compte entre l'éclair et le tonnerre, et plus on compte loin, plus l'orage s'éloigne. Un, deux, trois… sept… douze. L'orage s'en va. {A} a gagné.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
                Plain("drum", "Taper sur une casserole, plus fort que le tonnerre",
                    "{A} tape sur une casserole à chaque coup de tonnerre. Le tonnerre finit par se taire. Les voisins aussi, depuis longtemps.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 2))),
                Plain("blanket", "Rester sous la couverture",
                    "{A} reste sous la couverture. La couverture tient bon. Quand {A} ressort, le soleil est là, et l'orage n'a rien pu faire contre la couverture.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Boldness, -1))),
            }),

        new EventDef("baby_shadow", EventType.Pulse, Baby, "La chose qui suit",
            "Ce matin, {A} a découvert que quelque chose suit {A} partout, en silence, sur le sol. Quand {A} lève une patte, la chose lève une patte. C'est très suspect.",
            new[]
            {
                Plain("lose", "Essayer de la semer",
                    "{A} court, tourne, se cache derrière un arbre. À l'ombre de l'arbre, la chose disparaît. Victoire ! Puis {A} ressort au soleil.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
                Plain("friend", "Lui proposer d'être amis",
                    "{A} propose. La chose accepte, évidemment. Depuis, {A} et son ombre font tout ensemble, sauf la nuit, où l'ombre prend congé.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 1))),
                Plain("ask", "Demander à la chouette d'où vient la chose",
                    "La chouette explique le soleil, la lumière, les ombres. {A} écoute très sérieusement, puis demande si l'ombre mange. La chouette doit admettre que personne ne sait.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2)), gate: new TraitGate("curious")),
            }),
    };
}
