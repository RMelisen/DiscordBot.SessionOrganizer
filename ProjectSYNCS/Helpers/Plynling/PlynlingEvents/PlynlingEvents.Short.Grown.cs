using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 8 short events: adulte et ancien.
public static partial class PlynlingEvents
{
    private static EventDef[] ShortGrown() => new[]
    {
        // ---- adulte et ancien
        new EventDef("grown_apple_idea", EventType.Pulse, Grown, "La pomme et l'idée",
            "{A} faisait la sieste sous le pommier quand une pomme est tombée, pile sur la tête. Et avec la pomme, une idée.",
            new[]
            {
                Plain("write", "Noter l'idée tout de suite",
                    "{A} note l'idée : une gouttière qui arrose le potager toute seule. Une semaine plus tard, la gouttière marche. Les voisins en commandent trois.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(10)), Ai((AiAxis.Rationality, 2))),
                Plain("eat", "Manger la pomme, et tant pis pour l'idée",
                    "{A} mange la pomme. La pomme est excellente. L'idée est partie. Ça arrive.",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Greed, 1), (AiAxis.Energy, -1))),
                Plain("owl", "Courir raconter l'idée à la chouette",
                    "La chouette écoute, puis sort un livre très ancien : quelqu'un a déjà eu la même idée, sous un autre pommier, autrefois. {A} est {a:déçu|déçue}. La chouette, ravie.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_umbrella", EventType.Pulse, Grown, "Le parapluie à pois",
            "En sortant du café, {A} a pris le mauvais parapluie. Celui-ci est rouge, à pois blancs, avec un nom brodé sur le manche : celui du hérisson.",
            new[]
            {
                Plain("return", "Le rapporter tout de suite",
                    "{A} court à la gare. Le hérisson, trempé, tient le parapluie de {A} et regarde le ciel d'un air sévère. L'échange se fait sans un mot, avec une petite courbette.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("keep", "Le garder jusqu'à la fin de l'averse",
                    "{A} garde le parapluie jusqu'au soir, et le rapporte sec et plié. Le hérisson, coincé sous l'auvent, en a profité pour refaire tous les horaires d'hiver.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Greed, 1)), Stress(("honest", 10))),
            }),

        new EventDef("grown_bad_day", EventType.Pulse, Grown, "Un jour sans",
            "Le lacet a cassé, le pain a brûlé, la porte a coincé. Et ce n'est que huit heures du matin. {A} regarde le lit avec beaucoup d'intérêt.",
            new[]
            {
                Plain("bed", "Retourner se coucher, et recommencer à midi",
                    "{A} retourne se coucher. À midi, {A} se relève. Le pain de midi est parfait. La journée était juste partie du mauvais pied.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Energy, -1))),
                Plain("count", "Continuer, en comptant les catastrophes",
                    "{A} continue, en comptant. À la onzième catastrophe, {A} éclate de rire au milieu de la rue. La douzième ne vient jamais.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Rationality, 1))),
                Plain("cafe", "Aller se plaindre à la tortue",
                    "La tortue écoute la liste, hoche la tête, et sert un chocolat avec deux sucres de plus. « Les jours sans, on les compense. »",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_woodpecker", EventType.Pulse, Grown, "Toc-toc-toc",
            "Depuis une semaine, le nouveau voisin, un pic-vert, tape sur son tronc dès l'aube. Toc-toc-toc. Tous les matins. {A} n'a pas dormi après six heures depuis lundi.",
            new[]
            {
                Try("talk", "Aller lui parler, poliment", new EventChallenge(PlynlingStat.Diplomacy, 7),
                    "{A} frappe chez le pic-vert, qui ne se doutait pas qu'on l'entendait. Le pic-vert promet de commencer à neuf heures, et offre un pot de larves au miel. {A} accepte le pot, par politesse.",
                    "{A} explique le problème. Le pic-vert écoute, hoche la tête, en tapant le rythme du bec. Le lendemain : toc-toc-toc, à six heures. Mais plus doucement.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Sociability, 1), (AiAxis.Honor, 1))),
                Plain("plugs", "Acheter des bouchons d'oreilles",
                    "{A} achète des bouchons en cire au marché. Le lendemain, {A} dort jusqu'à neuf heures, et rate le facteur, le livreur de pain et l'arrivée de l'escargot au bout du jardin, attendue depuis l'été.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Rationality, 1))),
                Plain("duet", "Taper aussi, en rythme",
                    "À l'aube, {A} répond au pic-vert en tapant sur une casserole. Le pic-vert s'arrête, surpris, puis répond. Ça devient un duo. Les voisins, eux, envisagent de déménager.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_compliment", EventType.Pulse, Grown, "La meilleure confiture",
            "Au marché, {B} a déclaré devant tout le monde que {A} faisait la meilleure confiture du village. {A} n'a jamais fait de confiture de sa vie.",
            new[]
            {
                Plain("correct", "Rectifier, gentiment",
                    "« Ce n'est pas moi, c'est la tortue. » {B} rougit, puis rit. Le soir, {A} essaie quand même de faire de la confiture, pour voir.",
                    E(new AffinityShift(10), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Try("jam", "Accepter, et faire une confiture cette nuit même", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} passe la nuit sur une confiture de mûres. Au matin, le pot est parfait. {B} goûte, et confirme : la meilleure du village. Maintenant, c'est vrai.",
                    "{A} passe la nuit sur la confiture, qui finit en caramel de mûres collé au fond. {B} goûte quand même, très courageusement, et en redemande.",
                    E(new GrowStat(PlynlingStat.Stewardship), new AffinityShift(15)), E(new AffinityShift(10)), Ai((AiAxis.Boldness, 1)), Stress(("honest", 15))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_snide", EventType.Pulse, Grown, "La remarque",
            "En passant devant la table de {A}, {B} glisse que ce chapeau « a dû être très à la mode, autrefois ». Le café a entendu. Le café attend.",
            new[]
            {
                Try("retort", "Répondre du tac au tac", new EventChallenge(PlynlingStat.Diplomacy, 7, VsTarget: true),
                    "« Comme ta coiffure, alors. » Le café éclate de rire. {B} ouvre la bouche, la referme, et sort sans finir son thé.",
                    "{A} cherche une réplique, la trouve en rentrant, et la répète devant le miroir, très {a:satisfait|satisfaite}. Le miroir rit poliment.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(-5)), E(new AffinityShift(-5)), Ai((AiAxis.Vengefulness, 1), (AiAxis.Boldness, 1))),
                Plain("smile", "Sourire, et ne rien dire",
                    "{A} sourit, et retourne à son chocolat. {B}, {b:frustré|frustrée}, attend une réponse qui ne vient pas, et finit par partir. Le café trouve que {A} a gagné sans dire un mot.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Rationality, 1))),
                Plain("agree", "Approuver : « Autrefois, oui. Et ça reviendra. »",
                    "{A} approuve, et prédit le retour de la mode. Trois semaines plus tard, trois jeunes du village portent le même chapeau. {B} enrage.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
            },
            Target: TargetKind.Hostile),

        new EventDef("grown_moving_day", EventType.Pulse, Grown, "Le déménagement",
            "{B} déménage à l'autre bout du village, et a besoin de bras. Les cartons s'entassent jusqu'au plafond, l'armoire ne passe pas la porte, et le chariot a une roue qui grince.",
            new[]
            {
                Try("wardrobe", "S'attaquer à l'armoire", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} trouve l'angle exact : en biais, un pied en l'air, en retenant son souffle. L'armoire passe. {B} applaudit, puis offre à {A} une tarte entière.",
                    "L'armoire reste coincée dans la porte. {A} et {B} déjeunent de part et d'autre, en se passant les tartines par le trou de la serrure. On la démonte le lendemain.",
                    E(new AffinityShift(15), new LiftNeed(Need.Hunger, 0.15)), E(new AffinityShift(10)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
                Plain("boxes", "Porter les cartons, toute la journée",
                    "{A} porte quarante-trois cartons. Le quarante-quatrième contenait les assiettes. Personne n'en parle. {B} offre un dîner, servi dans des bols.",
                    E(new AffinityShift(15)), Ai((AiAxis.Compassion, 1), (AiAxis.Energy, 1))),
                Plain("excuse", "Inventer une excuse pour ne pas venir",
                    "{A} invente un rhume. {B} déménage avec l'ours, qui porte l'armoire à lui seul. Le lendemain, {B} apporte une soupe à {A}, pour le rhume.",
                    E(new AffinityShift(-5)), Ai((AiAxis.Energy, -1), (AiAxis.Honor, -1)), Stress(("honest", 20))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_gray_bench", EventType.Pulse, Grown, "Sur le banc de la place",
            "{B} est {b:assis|assise} sur le banc de la place depuis le matin, sans rien faire, le regard dans le vide. Les passants saluent ; {B} ne répond pas.",
            new[]
            {
                Plain("sit", "S'asseoir à côté, en silence",
                    "{A} s'assoit, sans poser de question. Au bout d'une heure, {B} pose la tête sur l'épaule de {A}. Au bout de deux, {B} dit merci.",
                    E(new AffinityShift(20)), Ai((AiAxis.Compassion, 2))),
                Plain("cake", "Poser un gâteau sur le banc, sans rien demander",
                    "{A} pose un gâteau sur le banc, entre les deux. {B} le regarde longtemps, puis en prend une part. Puis une deuxième. Puis parle.",
                    E(new AffinityShift(15)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("joke", "Raconter la blague la plus bête qu'on connaisse",
                    "{A} raconte la blague de l'escargot qui prend le train. {B} ne rit pas. Puis, trois minutes plus tard, éclate de rire d'un coup, et ne peut plus s'arrêter.",
                    E(new AffinityShift(10), new ApplyModifier("light_heart")), Ai((AiAxis.Sociability, 2))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_rival_ladder", EventType.Pulse, Grown, "L'échelle",
            "{B}, avec qui {A} ne s'entend pas, frappe à la porte, l'air très {b:gêné|gênée}. « Mon échelle est cassée. Mon toit fuit. Tu as une échelle. » Ce n'est pas une question. Presque.",
            new[]
            {
                Plain("lend", "Prêter l'échelle",
                    "{A} prête l'échelle, sans un mot. Le lendemain, l'échelle revient, réparée, avec une marche en plus et un pot de miel accroché au barreau du haut.",
                    E(new AffinityShift(15)), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1))),
                Plain("help", "Prêter l'échelle, et monter aider",
                    "{A} tient l'échelle, puis monte sur le toit, puis répare la fuite avec {B}. Au coucher du soleil, les deux sont assis sur le toit, sans se disputer, pour une fois.",
                    E(new AffinityShift(20)), Ai((AiAxis.Compassion, 2)), Stress(("vengeful", 20))),
                Plain("refuse", "Refuser, poliment mais fermement",
                    "« Non. » {B} repart sous la pluie. Le soir, en écoutant l'averse, {A} pense au toit de {B}, et dort moins bien que prévu.",
                    E(new AffinityShift(-10)), Ai((AiAxis.Vengefulness, 2)), Stress(("compassionate", 20), ("forgiving", 15))),
            },
            Target: TargetKind.Hostile),

        new EventDef("grown_spring_clean", EventType.Pulse, Grown, "Le grand ménage",
            "{A} a décidé de faire le grand ménage : le grenier, la cave, les placards. Au bout d'une heure, la pile « à jeter » est plus petite que la pile « on ne sait jamais ».",
            new[]
            {
                Plain("throw", "Tout jeter, sans pitié",
                    "{A} jette tout. La maison respire. {A} aussi. Deux jours plus tard, {A} cherche désespérément une ficelle, jetée lundi.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
                Plain("give", "Tout donner, sur une table au marché",
                    "{A} installe une table au marché, avec une pancarte : « Gratuit ». Tout part en une heure. La pie repart avec trois boutons, sans même faire semblant d'en avoir besoin.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("keep", "Tout garder, finalement",
                    "{A} remet tout en place, dans le même désordre. Au passage, une bille perdue depuis des années refait surface. Le grand ménage est reporté au printemps prochain, comme chaque année.",
                    E(new GiveItem("col.bille")), Ai((AiAxis.Greed, 1))),
            }),

        new EventDef("grown_redecorate", EventType.Pulse, Grown, "La nouvelle déco",
            "{A} trouve sa maison un peu triste. Au marché, on vend des rideaux à fleurs, de la peinture jaune, et un tapis en forme d'escargot.",
            new[]
            {
                Plain("yellow", "Tout repeindre en jaune",
                    "{A} repeint tout en jaune, plafond compris. La maison ressemble à un œuf à la coque. C'est très gai. Les visiteurs plissent les yeux en entrant.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Boldness, 1))),
                Plain("rug", "Acheter le tapis escargot",
                    "{A} achète le tapis escargot. L'escargot du village, invité pour l'inauguration, reste une heure à le contempler, très ému.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Compassion, 1))),
                Plain("nothing", "Ne rien changer : la maison est bien comme ça",
                    "{A} rentre sans rien acheter, et regarde la maison d'un œil neuf. La maison, au fond, est très bien comme ça.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_free_day", EventType.Pulse, Grown, "Une journée libre",
            "Pour la première fois depuis longtemps, {A} n'a rien de prévu. Rien du tout. La journée entière est vide, et brille comme une assiette propre.",
            new[]
            {
                Plain("nothing", "Ne rien faire, mais vraiment rien",
                    "{A} ne fait rien, avec beaucoup de sérieux. Le soir, {A} est {a:épuisé|épuisée} d'avoir tant reposé. C'était parfait.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -2))),
                Plain("walk", "Partir marcher au hasard",
                    "{A} marche au hasard et découvre un sentier inconnu, une source, et un banc de pierre qui donne sur toute la vallée. Le banc devient un endroit à soi.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Energy, 1), (AiAxis.Boldness, 1))),
                Plain("visit", "Rendre visite à tout le monde",
                    "{A} fait le tour du village, maison par maison. Onze tasses de thé. Le soir, {A} ne dort pas, mais connaît toutes les nouvelles.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_snickers", EventType.Pulse, Grown, "Les rires dans le dos",
            "Toute la matinée, les gens sourient en croisant {A}. Certains pouffent. La pie rit franchement. Personne ne dit rien.",
            new[]
            {
                Plain("window", "Aller se regarder dans une vitrine",
                    "Dans la vitrine du boulanger, {A} découvre une feuille de chou collée dans le dos, avec écrit dessus : « Je suis très {a:gentil|gentille} ». C'est signé l'escargot. C'est vrai, en plus.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
                Plain("laugh", "Rire aussi, sans savoir pourquoi",
                    "{A} rit aussi, de bon cœur. Le village rit encore plus. Le soir, quelqu'un décolle enfin la feuille dans le dos de {A} : « Je suis très {a:gentil|gentille} ». Signé l'escargot.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_tin_box", EventType.Pulse, Grown, "La boîte en fer",
            "En balayant devant chez soi, {A} trouve une petite boîte en fer. Dedans : une clé, un dessin pâli, et un mot : « Pour retrouver le chemin. »",
            new[]
            {
                Try("owner", "Chercher à qui appartient la boîte", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Après une semaine d'enquête, la boîte retourne à la vieille loutre du ponton, qui la croyait perdue depuis trente ans. La clé était celle de sa première maison. La loutre offre le dessin à {A}.",
                    "Personne ne reconnaît la boîte. {A} la dépose au kiosque des objets trouvés. Un matin, la boîte a disparu, et un mot la remplace : « Merci. »",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("cherished")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Honor, 1))),
                Plain("keep", "La garder, au cas où",
                    "{A} range la boîte dans un tiroir. Parfois, {A} la ressort et regarde la clé, en se demandant quelle porte la clé ouvrait. C'est un très bon mystère pour les soirs de pluie.",
                    E(new GiveItem("col.cle_rouillee")), Ai((AiAxis.Greed, 1))),
            }),

        new EventDef("grown_spices", EventType.Pulse, Grown, "Le marchand d'épices",
            "Un marchand venu de très loin a dressé son étal au marché : des épices rouges, jaunes, violettes, et des odeurs que le village n'a jamais senties. Les prix sont fous. Les odeurs, encore plus.",
            new[]
            {
                Try("haggle", "Marchander une pincée de chaque", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} marchande en riant, le marchand aussi. {A} repart avec sept petits sachets pour le prix de trois. Le soir, la soupe de {A} a un goût de voyage.",
                    "{A} marchande, le marchand aussi, et {A} repart avec un seul sachet, très cher, de quelque chose qui pique beaucoup. La soupe du soir fait pleurer, de joie ou de piment.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("hearty")), Nothing, Ai((AiAxis.Greed, 1))),
                Plain("listen", "Écouter les histoires du marchand",
                    "Le marchand raconte les routes, les déserts, les villes au bord de la mer. {A} écoute jusqu'à la fermeture. En partant, le marchand offre une pincée de cannelle, « pour se souvenir ».",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_mapmaker", EventType.Pulse, Grown, "La cartographe",
            "Une cartographe de passage, une martre aux lunettes rondes, cherche quelqu'un pour la guider jusqu'à la source de la rivière. Personne au village n'y est jamais allé.",
            new[]
            {
                Try("guide", "Proposer de servir de guide", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} et la martre remontent la rivière pendant trois jours. À la source : une petite cascade, et une grotte pleine de cristaux. La martre dessine tout, et inscrit sur la carte : « Source de {A} ».",
                    "{A} et la martre se perdent dès le deuxième jour, et reviennent au village par le chemin d'en face. La carte de la martre indique désormais : « Ici, on s'est perdus. Très joli quand même. »",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("inspired")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Boldness, 2))),
                Plain("sketch", "Lui dessiner ce qu'on connaît du chemin",
                    "{A} dessine tout ce qu'on sait : le pont, la gare, le moulin, puis « après, on ne sait pas ». La martre est ravie : c'est exactement là que commence son travail.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_school_roof", EventType.Pulse, Grown, "La collecte pour l'école",
            "Le maire fait le tour des maisons : on collecte pour réparer le toit de l'école. La boîte est presque vide, et le maire a l'air fatigué.",
            new[]
            {
                Plain("give", "Donner, généreusement",
                    "{A} glisse une poignée de cailloux dans la boîte. Le maire regarde dedans, puis {A}, puis dans la boîte encore. Le toit est réparé avant l'hiver.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 1), (AiAxis.Greed, -1)), Stress(("greedy", 20))),
                Plain("roof", "Proposer plutôt de réparer le toit soi-même",
                    "{A} monte sur le toit de l'école avec l'ours et une pile de tuiles. Le toit est réparé en deux jours, et l'argent de la collecte part en goûters pour l'école.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Energy, 1), (AiAxis.Compassion, 1))),
                Plain("pebble", "Donner un petit caillou, en s'excusant",
                    "{A} donne un petit caillou. Le maire remercie, très sincèrement : « C'est le geste qui compte. » Le petit caillou est noté dans le carnet avec le même soin que les autres.",
                    E(new LiftNeed(Need.Happiness, 0.05)), Ai((AiAxis.Greed, 1))),
            }),

        new EventDef("grown_pie_contest", EventType.Pulse, Grown, "Le concours de tartes",
            "À la fête du village, le concours de mangeurs de tartes commence dans cinq minutes. L'ours a gagné les neuf dernières éditions. Une place est libre, juste à côté de l'ours.",
            new[]
            {
                Try("eat", "S'inscrire, et manger le plus vite possible", new EventChallenge(PlynlingStat.Courage, 7),
                    "Sept tartes. L'ours en est à six, et pose sa fourchette. Pour la première fois en dix ans, l'ours perd, et serre la patte de {A} en riant, la moustache pleine de crème.",
                    "Trois tartes, et {A} abandonne, le ventre tendu comme un tambour. L'ours en mange douze. {A} rentre presque en roulant.",
                    E(new GiveCailloux(15), new ApplyModifier("hearty")), E(new LiftNeed(Need.Hunger, 0.3)), Ai((AiAxis.Greed, 1), (AiAxis.Boldness, 1)), Stress(("temperate", 20))),
                Plain("judge", "Proposer d'être juge, plutôt",
                    "{A} devient juge, ce qui consiste à goûter un morceau de chaque tarte, pour vérifier la qualité. C'est le meilleur poste du concours.",
                    E(new LiftNeed(Need.Hunger, 0.2)), Ai((AiAxis.Rationality, 1))),
                Plain("cheer", "Encourager l'escargot, inscrit pour la première fois",
                    "L'escargot mange une seule tarte, en deux heures, très dignement. {A} applaudit à chaque bouchée. L'escargot reçoit le prix de la persévérance.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_dream_machine", EventType.Pulse, Grown, "La machine du rêve",
            "Cette nuit, {A} a rêvé d'une machine merveilleuse, avec des roues, des ressorts, et une cheminée qui fait des bulles. Au réveil, le rêve est encore là, très net.",
            new[]
            {
                Try("build", "La construire, de mémoire", new EventChallenge(PlynlingStat.Learning, 8),
                    "Trois jours de travail. La machine fait des bulles, exactement comme dans le rêve. Ça ne sert à rien, et c'est merveilleux. Les petits du village viennent la regarder chaque soir.",
                    "Trois jours de travail. La machine fait un bruit terrible, tremble, et produit… une seule bulle. Une belle bulle, cela dit.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Rationality, 2))),
                Plain("draw", "La dessiner, et passer à autre chose",
                    "{A} dessine la machine dans un carnet, avec toutes ses roues. Le dessin est très beau. La machine restera un rêve, et c'est peut-être mieux ainsi.",
                    E(new ApplyModifier("inspired")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_old_kindness", EventType.Pulse, Grown, "Un bienfait oublié",
            "Une lettre arrive, d'une écriture inconnue. Un hérisson de la ville d'en bas écrit : « Vous m'avez aidé un soir de pluie, autrefois, quand j'étais perdu. Je ne l'ai jamais oublié. » Dans l'enveloppe, quelques cailloux. {A} ne se souvient de rien.",
            new[]
            {
                Plain("reply", "Répondre, même sans se souvenir",
                    "{A} répond avec sincérité : on ne se souvient pas, mais on est très {a:content|contente} que ça ait compté. Une correspondance commence, qui dure encore.",
                    E(new GiveCailloux(10), new ApplyModifier("cherished")), Ai((AiAxis.Sociability, 1), (AiAxis.Honor, 1))),
                Plain("forward", "Garder les cailloux pour aider quelqu'un d'autre",
                    "{A} range les cailloux dans une boîte marquée « Pour le prochain soir de pluie ». La boîte sert trois fois cette année-là.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_marsh_remedy", EventType.Pulse, Grown, "Le remède de la crapaude",
            "{A} a mal au dos depuis une semaine. Au marais, la vieille crapaude herboriste prépare des remèdes. Ses remèdes marchent, paraît-il. Mais ont un goût d'étang.",
            new[]
            {
                Plain("drink", "Boire le remède, d'un trait",
                    "Le remède a le goût d'un étang qui aurait avalé une chaussette. Mais le lendemain, le dos va beaucoup mieux. La crapaude n'en doutait pas une seconde.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Boldness, 1))),
                Plain("chat", "Rester bavarder avec la crapaude",
                    "La crapaude raconte le marais, les herbes, les recettes de sa grand-mère. {A} repart avec un carnet de remèdes, et le dos toujours tordu.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Sociability, 1))),
                Plain("plank", "Refuser poliment, et dormir sur une planche",
                    "{A} remercie, rentre, et dort sur une planche. Ça marche aussi, au bout de trois nuits. La crapaude, quand on le lui raconte, hausse les épaules : « La planche, c'est mon autre remède. »",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_canvas", EventType.Pulse, Grown, "La toile blanche",
            "{A} a acheté des couleurs et une toile. La toile est blanche. Très blanche. Le pinceau attend.",
            new[]
            {
                Plain("bridge", "Peindre le vieux pont",
                    "{A} peint le vieux pont, avec le héron dessus. Le pont est un peu tordu, le héron très réussi. Le héron, consulté, approuve d'un hochement de bec.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
                Plain("feelings", "Peindre ce qu'on ressent, sans réfléchir",
                    "{A} peint des taches, des spirales, un grand cercle orange. Personne ne sait ce que c'est. Le café l'accroche quand même, à l'envers, et c'est encore mieux.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Boldness, 1))),
                Plain("tortoise", "Peindre la tortue du café",
                    "{A} peint la tortue derrière son comptoir. La tortue pose une heure sans bouger, ce qui n'est pas difficile pour une tortue. Le portrait rejoint le mur du café.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_heron_warning", EventType.Pulse, Grown, "L'avertissement du héron",
            "Le héron, qui ne parle presque jamais, arrête {A} sur le vieux pont : « Ne passe pas par le bois aujourd'hui. » Puis se tait. Le bois, c'est le chemin le plus court pour le marché.",
            new[]
            {
                Plain("listen", "Écouter le héron, et faire le détour",
                    "{A} fait le grand détour. Le soir, on apprend qu'une branche énorme est tombée sur le chemin du bois, à midi. Le héron, interrogé, dit seulement : « Les arbres parlent. »",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Try("woods", "Passer quand même, mais prudemment", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} passe par le bois, prudemment. À midi, un craquement : une énorme branche tombe, trois pas derrière. {A} arrive au marché les jambes en coton, et avec une estime toute neuve pour le héron.",
                    "{A} passe par le bois, et trouve le chemin barré par une branche tombée, énorme. Une heure de détour quand même. Au retour, le héron ne dit rien. Le silence du héron en dit long.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing, Ai((AiAxis.Boldness, 2))),
            }),

        new EventDef("grown_hedge", EventType.Pulse, Grown, "La haie qui penche",
            "Le blaireau et le lapin, voisins de {A}, se disputent la haie entre leurs jardins. Selon le blaireau, la haie penche chez le lapin. Selon le lapin, c'est le blaireau qui penche. Les deux viennent chercher {A} pour trancher.",
            new[]
            {
                Try("measure", "Mesurer la haie, avec une ficelle", new EventChallenge(PlynlingStat.Stewardship, 7),
                    "{A} mesure, au centimètre près. La haie est parfaitement droite : ce sont les deux maisons qui penchent, chacune de son côté. Les voisins, déconcertés, se réconcilient pour en discuter.",
                    "{A} mesure, se trompe, remesure, se trompe dans l'autre sens. Au bout d'une heure, les deux voisins, lassés, vont boire un thé ensemble et laissent {A} avec la ficelle.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Nothing, Ai((AiAxis.Rationality, 2))),
                Plain("flowers", "Proposer de planter des fleurs à la place",
                    "{A} propose d'arracher la haie et de planter des fleurs. Les voisins hésitent, puis acceptent. Les deux jardins n'en font plus qu'un, et les deux voisins prennent le thé au milieu.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_wrong_name", EventType.Pulse, Grown, "Bonjour, Josette",
            "Depuis un mois, le nouveau boulanger appelle {A} par le mauvais prénom. Tous les matins. « Bonjour, Josette ! » Trop tard pour corriger, maintenant ?",
            new[]
            {
                Plain("correct", "Corriger, enfin",
                    "{A} corrige. Le boulanger devient tout rouge, s'excuse dix fois, et offre une brioche par semaine pendant un mois, pour réparer.",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Honor, 1))),
                Plain("josette", "Devenir Josette, à la boulangerie",
                    "{A} répond « Bonjour ! » comme si de rien n'était. Un an plus tard, la moitié du village appelle {A} Josette, et {A} s'y est très bien {a:habitué|habituée}.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            }),

        new EventDef("grown_clover", EventType.Pulse, Grown, "Le trèfle du talus",
            "Dans l'herbe du talus, {A} aperçoit un trèfle à quatre feuilles. Juste là. Le premier de sa vie.",
            new[]
            {
                Plain("pick", "Le cueillir, et le garder dans un livre",
                    "{A} cueille le trèfle et compte les feuilles : trois. C'était un effet de lumière. Un trèfle quand même, et un très beau.",
                    E(new GiveItem("col.trefle")), Ai((AiAxis.Greed, 1))),
                Plain("leave", "Le laisser, pour la personne suivante",
                    "{A} le laisse. Quelqu'un d'autre en a peut-être plus besoin. Le reste de la journée, {A} se sent {a:chanceux|chanceuse} quand même.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 1))),
                Try("search", "Chercher d'autres trèfles autour", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} fouille le talus, brin par brin, et trouve un deuxième trèfle à quatre feuilles, un vrai. Le premier n'en avait que trois, finalement.",
                    "{A} cherche tout l'après-midi. Rien. Et le premier trèfle, recompté, n'en avait que trois. Belle journée quand même.",
                    E(new GiveItem("col.trefle_quatre")), Nothing, Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_sad_bear", EventType.Pulse, Grown, "L'ours ne sourit plus",
            "L'ours du kiosque à glaces ne sourit plus depuis une semaine. Les cornets sont toujours aussi bons, mais l'ours les tend sans un mot.",
            new[]
            {
                Try("ask", "Demander à l'ours ce qui ne va pas", new EventChallenge(PlynlingStat.Diplomacy, 7),
                    "L'ours finit par avouer : demain, le kiosque a quarante ans, et personne ne s'en souvient. Le lendemain, le village entier vient fêter le kiosque. L'ours pleure dans un cornet.",
                    "L'ours assure que tout va bien, très vite. {A} n'insiste pas. Le lendemain, l'ours sourit de nouveau, sans qu'on sache pourquoi. Peut-être que la question a suffi.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("cherished")), Nothing, Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("daily", "Acheter une glace chaque jour, et dire merci",
                    "{A} achète une glace chaque jour pendant une semaine, et dit merci à chaque fois. Le septième jour, l'ours sourit. Le huitième, l'ours offre la glace.",
                    E(new LiftNeed(Need.Hunger, 0.15), new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Compassion, 1), (AiAxis.Greed, 1))),
            }),

        new EventDef("grown_hat_wind", EventType.Pulse, Grown, "Le chapeau envolé",
            "Une rafale arrache le chapeau de {A}, qui file au-dessus des toits, vers la rivière.",
            new[]
            {
                Try("run", "Courir après", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} court, saute une haie, et attrape le chapeau au vol sur le vieux pont, à un bec du héron. Le héron, qui allait le prendre, fait semblant de rien.",
                    "{A} court, saute une haie, et le chapeau plonge dans la rivière. Trois jours plus tard, le chapeau revient, porté par une famille de canards qui en a fait un nid.",
                    E(new GrowStat(PlynlingStat.Courage)), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Energy, 2))),
                Plain("let", "Le laisser partir",
                    "{A} regarde le chapeau disparaître. Une semaine plus tard, une carte postale arrive de la ville d'en bas : un dessin du chapeau, sur la tête d'un épouvantail. Le chapeau va bien.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_magician", EventType.Pulse, Grown, "Le prestidigitateur",
            "Un lapin prestidigitateur fait des tours sur la place. Le lapin cherche un volontaire dans le public, et son regard s'arrête sur {A}.",
            new[]
            {
                Plain("stage", "Monter sur l'estrade",
                    "Le lapin fait disparaître le chapeau de {A}, puis le fait réapparaître… dans la poche du hérisson, au premier rang. Le hérisson n'a pas du tout apprécié. Le public, si.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Try("spot", "Rester dans le public, et repérer le truc", new EventChallenge(PlynlingStat.Learning, 7),
                    "{A} observe et comprend tout : la manche, le double fond, le pigeon dans le chapeau. Le soir, {A} refait le tour pour les petits du village, sans le pigeon.",
                    "{A} observe très attentivement, et ne comprend rien du tout. Le pigeon, lui, regarde {A} d'un air narquois.",
                    E(new GrowStat(PlynlingStat.Learning)), Nothing, Ai((AiAxis.Rationality, 2))),
            }),

        new EventDef("grown_goose_toll", EventType.Pulse, Grown, "Le péage de l'oie",
            "Une oie s'est installée au milieu du vieux pont et réclame un péage : une graine par passage. Le héron, à côté, ne dit rien. Le héron a déjà payé.",
            new[]
            {
                Try("contest", "Contester le péage", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} demande à voir l'autorisation de péage. L'oie fouille ses plumes, ne trouve rien, et part en grommelant. Le héron récupère sa graine.",
                    "{A} conteste. L'oie conteste la contestation. Au bout d'une heure, {A} paie deux graines : une pour passer, une pour le temps perdu.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Honor, 1), (AiAxis.Boldness, 1))),
                Plain("pay", "Payer, et passer",
                    "{A} paie une graine. L'oie fait une petite courbette et souhaite une bonne journée. C'est le péage le plus aimable de la vallée.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 1))),
                Plain("swim", "Traverser à la nage, par principe",
                    "{A} traverse la rivière à la nage, sous le regard outré de l'oie. De l'autre côté, {A} s'ébroue, très digne, et très {a:mouillé|mouillée}.",
                    E(new GrowStat(PlynlingStat.Courage), new LiftNeed(Need.Hygiene, 0.2)), Ai((AiAxis.Boldness, 2))),
            }),

        new EventDef("grown_hammock", EventType.Pulse, Grown, "Le hamac",
            "{A} a tendu un hamac entre les deux pommiers du jardin. Le soleil est doux, le vent léger, et les abeilles bourdonnent comme une berceuse.",
            new[]
            {
                Plain("nap", "Faire la sieste du siècle",
                    "{A} s'endort à midi et se réveille au coucher du soleil, une pomme sur le ventre, tombée pendant le sommeil. La meilleure sieste de l'année.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Energy, -2))),
                Plain("read", "Lire, en se balançant",
                    "{A} lit tout un livre en se balançant doucement. À la dernière page, le hamac s'arrête, comme pour laisser le temps de finir.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
                Plain("share", "Inviter les voisins à essayer",
                    "Le blaireau essaie, puis le lapin, puis l'ours. Le hamac tient pour le blaireau, pour le lapin, et pas du tout pour l'ours. On en rit toute la soirée.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2))),
            }),
    };
}
