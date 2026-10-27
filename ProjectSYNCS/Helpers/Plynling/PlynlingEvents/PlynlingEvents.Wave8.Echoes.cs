using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 8 story cycles: secrets, rivals, echoes of childhood, and ancien events.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave8Echoes() => new[]
    {
        // adulte et ancien: le secret de l'aube (CK3's discovered secrets, kept harmless)
        new EventDef("grown_secret_dance", EventType.Pulse, Grown, "Le secret de l'aube",
            "À l'aube, sous le vieux pont, {A} surprend une scène étonnante : {B} prend une leçon de danse. Le professeur est le héron, qui compte les pas sur une patte. {B} marche sur les pieds du héron, s'excuse, recommence, et n'a aucune idée que quelqu'un regarde.",
            new[]
            {
                Plain("quiet", "Repartir sans bruit, et garder le secret",
                    "{A} recule sur la pointe des pieds, et n'en parle à personne. Le soir, au café, quand {B} prétend avoir mal aux pieds « à cause de nouvelles chaussures », {A} hoche la tête, très {a:sérieux|sérieuse}.",
                    E(new AffinityShift(5), new FollowUp("grown_secret_dance_known", 48, 96)), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, 1))),
                Try("join", "Sortir des roseaux, et demander à danser aussi", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} sort des roseaux. {B} se fige, rouge jusqu'aux oreilles. Le héron, imperturbable, désigne une place à côté : « Le pas chassé. Un, deux. » Désormais, {A} et {B} se marchent sur les pieds à tour de rôle, avec beaucoup de sérieux.",
                    "{A} sort des roseaux, glisse sur la berge, et entre dans la leçon en roulant. {B} éclate de rire. Le héron note : « Original. Mais non. »",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(15), new FollowUp("grown_secret_dance_known", 48, 96)), E(new AffinityShift(10), new FollowUp("grown_secret_dance_known", 48, 96)),
                    Ai((AiAxis.Boldness, 2)), Stress(("shy", 20))),
                Plain("clap", "Applaudir très fort depuis les roseaux",
                    "{A} applaudit à tout rompre. {B} sursaute, rate le pas, et tombe dans la rivière. Le héron soupire. {B} sort de l'eau et regarde {A} avec une froideur de fond de rivière.",
                    E(new AffinityShift(-15)), Ai((AiAxis.Boldness, 1), (AiAxis.Compassion, -1)), Stress(("compassionate", 15))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_secret_dance_known", EventType.FollowUp, AnyStage, "Tu as vu",
            "{B} attend devant chez {A}, les bras croisés. « Le héron m'a tout dit. Tu as vu. » Ce n'est pas une question. Puis, plus bas : « Le bal du village, c'est dans une semaine. Je voulais que ce soit une surprise. »",
            new[]
            {
                Plain("promise", "Promettre de ne rien dire, juré",
                    "{A} jure, très fort, la patte sur le cœur. {B} sourit enfin, et propose à {A} de venir voir la dernière répétition, puisque de toute façon, le secret est éventé.",
                    E(new AffinityShift(10), new FollowUp("grown_secret_dance_ball", 72, 120)), Ai((AiAxis.Honor, 2))),
                Plain("partner", "Proposer d'être le partenaire de {B}, au bal",
                    "« Tu as besoin d'un partenaire, non ? Quelqu'un qui connaît déjà le secret. » {B} regarde {A} longuement, puis tend la patte. « Le pas chassé. Un, deux. On commence demain, à l'aube. »",
                    E(new AffinityShift(15), new FollowUp("grown_secret_dance_ball", 72, 120)), Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_secret_dance_ball", EventType.FollowUp, AnyStage, "Le bal du village",
            "Le bal du village, sur la place, sous les lampions. L'orchestre de grillons attaque la valse. Tout le monde danse mal, joyeusement. Puis {B} entre sur la piste, et c'est autre chose : le pas chassé est parfait. Au bord de la place, le héron essuie quelque chose dans son œil. {B} cherche {A} du regard.",
            new[]
            {
                Try("dance", "Rejoindre {B} sur la piste", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} rejoint {B}. Un, deux. Le pas chassé, puis un tour, puis un autre que le héron n'a jamais enseigné. La place s'arrête pour regarder. À la fin, on applaudit si fort que les grillons rejouent la valse depuis le début.",
                    "{A} rejoint {B}, et marche sur les pieds de {B} dès le premier pas. Puis au deuxième. Au troisième, {B} rit tellement que la danse s'arrête. Ce n'était pas la valse prévue. C'était peut-être mieux.",
                    E(new AffinityShift(20), new ApplyModifier("light_heart")), E(new AffinityShift(15)), Ai((AiAxis.Sociability, 2))),
                Plain("applaud", "Applaudir le plus fort de toute la place",
                    "{A} applaudit plus fort que tout le monde, et siffle même, ce qui ne se fait pas au bal. {B} salue, rouge de bonheur, et adresse une révérence rien qu'à {A}.",
                    E(new AffinityShift(10), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            },
            Target: TargetKind.Known),

        // adulte et ancien: l'orage et le rival (CK3's party baron endings: the rival who becomes a friend)
        new EventDef("grown_storm_shelter", EventType.Pulse, Grown, "Le chêne creux",
            "L'orage éclate d'un coup, en pleine forêt. Le seul abri à des lieues : le vieux chêne creux. {A} s'y engouffre, {a:trempé|trempée}, et se fige : au fond du creux, déjà {b:installé|installée}, attend {B}. De tout le village, c'est bien la dernière personne que {A} voulait croiser. Dehors, la pluie redouble.",
            new[]
            {
                Plain("corner", "Se serrer dans le coin opposé, sans un mot",
                    "{A} se tasse dans le coin opposé. {B} aussi. Le chêne est petit. Les coudes se touchent. Chaque coup de tonnerre fait sursauter les deux en même temps, ce qui est très agaçant.",
                    E(new AffinityShift(5), new FollowUp("grown_storm_night", 2, 6)), Ai((AiAxis.Rationality, 1))),
                Plain("share", "Couper son sandwich au miel en deux",
                    "{A} sort le sandwich, le coupe en deux, et tend une moitié sans regarder. Un long silence. Puis une patte prend la moitié. « Pas mal », marmonne {B}, la bouche pleine. Venant de {B}, c'est presque un compliment.",
                    E(new AffinityShift(10), new FollowUp("grown_storm_night", 2, 6)), Ai((AiAxis.Compassion, 2)), Stress(("vengeful", 20))),
                Plain("rain", "Ressortir sous la pluie, par fierté",
                    "{A} ressort sous l'orage, par principe. Dix minutes plus tard, {A} revient, {a:trempé|trempée} jusqu'aux os, et se rassoit sans un mot. {B} ne dit rien non plus, mais tend une feuille sèche pour s'essuyer.",
                    E(new ApplyModifier("woods_cold"), new FollowUp("grown_storm_night", 2, 6)), Ai((AiAxis.Boldness, 1), (AiAxis.Vengefulness, 1))),
            },
            Target: TargetKind.Hostile),

        new EventDef("grown_storm_night", EventType.FollowUp, AnyStage, "La nuit dans le chêne",
            "La nuit tombe, et l'orage ne faiblit pas. Dans le noir du chêne, {B} finit par parler, très bas : « Tu te souviens pourquoi on se dispute, toi ? Moi, je ne sais plus. » Un éclair illumine le creux. {B} a l'air sincère, et un peu {b:fatigué|fatiguée} de cette vieille histoire.",
            new[]
            {
                Plain("forgot", "Avouer qu'on ne sait plus non plus",
                    "{A} cherche, longtemps. Une histoire de pomme de pin ? De place au café ? Rien. Les deux finissent par rire dans le noir, si fort que le chêne en tremble. Le tonnerre, vexé, s'éloigne.",
                    E(new AffinityShift(20), new FollowUp("grown_storm_morning", 6, 12)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("remember", "S'en souvenir parfaitement, et le raconter",
                    "{A} raconte : le concours, la pomme de pin, le moineau, l'injustice. {B} écoute jusqu'au bout, puis donne sa version. Ce ne sont pas du tout les mêmes histoires. À trois heures du matin, la dispute est devenue si comique que les deux en rient encore.",
                    E(new AffinityShift(15), new FollowUp("grown_storm_morning", 6, 12)), Ai((AiAxis.Rationality, 1))),
                Plain("fear", "Avouer qu'on a peur de l'orage",
                    "« J'ai peur de l'orage », dit {A}, très vite. Un silence. Puis {B} : « Moi aussi. Depuis toujours. » Les deux se rapprochent un peu, et attendent la fin du tonnerre épaule contre épaule, sans rien ajouter.",
                    E(new AffinityShift(20), new FollowUp("grown_storm_morning", 6, 12)), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1)), Stress(("arrogant", 15))),
            },
            Target: TargetKind.Hostile),

        new EventDef("grown_storm_morning", EventType.FollowUp, AnyStage, "Après l'orage",
            "Au matin, le ciel est lavé. {A} et {B} sortent du chêne, les pattes raides, et prennent le chemin du village, côte à côte. À l'entrée du village, les vieilles habitudes reviennent : chacun s'apprête à partir de son côté, comme avant. {B} hésite.",
            new[]
            {
                Plain("peace", "Proposer la paix, pour de bon",
                    "« On arrête ? » {B} réfléchit, puis tend la patte. La poignée est maladroite, trop longue, et sent encore un peu le chêne mouillé. Ce midi-là, la tortue voit entrer {A} et {B} ensemble au café, et laisse tomber une tasse.",
                    E(new AffinityShift(25), new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1)), Stress(("vengeful", 20))),
                Plain("rivals", "Proposer une rivalité, mais amicale",
                    "« La pomme de pin, l'an prochain. Je te battrai. » {B} sourit, pour la première fois : « Compte là-dessus. » Les deux se séparent à l'entrée du village en se faisant de grands signes, très menaçants et très joyeux.",
                    E(new AffinityShift(15), new ApplyModifier("fired_up")), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
            },
            Target: TargetKind.Hostile),

        // adulte et ancien: échos de « Plus tard, je serai… » (CK3's I Want a Pony!: the dream comes back)
        new EventDef("grown_dream_explore", EventType.FollowUp, AnyStage, "Le fond du jardin",
            "En rangeant le grenier, {A} retrouve une vieille carte dessinée au crayon : le fond du jardin, un ver de terre, une flèche, et en lettres énormes, « LE MONDE ENTIER ». Le héron avait dit, ce jour-là : « Commence par le fond du jardin. » Le jardin est fait depuis longtemps. Le reste du monde, pas encore.",
            new[]
            {
                Try("hills", "Partir enfin, au-delà de la colline", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} part à l'aube, passe la colline, puis la suivante, et découvre une vallée que personne au village n'a jamais décrite. {A} revient trois jours plus tard, avec une boussole trouvée sur un rocher, et de nouvelles flèches à dessiner.",
                    "{A} part à l'aube, se perd derrière la deuxième colline, et tourne en rond jusqu'au soir. Au retour, le héron lève un œil : « Le monde est grand. Recommence par le fond du jardin. » {A} rit, pour la première fois depuis des jours.",
                    E(new GrowStat(PlynlingStat.Courage), new GiveItem("col.boussole")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 2))),
                Plain("map", "Compléter la carte d'enfance, au crayon",
                    "{A} reprend la carte d'enfant et y ajoute, une à une, toutes les routes connues : le vieux pont, la gare, le moulin, le chemin de la mer. Le ver de terre reste au centre, par respect.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired")), Ai((AiAxis.Rationality, 1))),
                Plain("heron", "Aller montrer la carte au héron",
                    "{A} déplie la carte devant le héron. Le héron la regarde longtemps, sur une patte, puis tapote de la pointe du bec un endroit vide, tout au bord. « Là. Je n'y suis jamais allé. » Depuis, les deux en parlent parfois, en regardant la rivière.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
            }),

        new EventDef("grown_dream_cafe", EventType.FollowUp, AnyStage, "La tasse ébréchée",
            "Au café, la tortue sort de sous le comptoir une petite tasse ébréchée. « Tu te souviens ? Tu servais des cafés imaginaires au héron, et tu réclamais un pourboire. » La tortue pose la tasse devant {A}. « Mes pattes fatiguent. Le café aurait bien besoin d'aide, le samedi. »",
            new[]
            {
                Try("apron", "Prendre le tablier, ce samedi", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "Samedi, {A} sert quarante chocolats, rend la monnaie juste, et prépare la commande de l'escargot avant même que l'escargot n'ouvre la bouche. Le soir, la tortue verse dans la tasse ébréchée les pourboires de la journée.",
                    "Samedi, {A} renverse trois chocolats, confond le miel et la moutarde, et sert l'ours deux fois. Le soir, la tortue rit si fort que sa carapace en résonne. « Moi, le premier jour, j'ai mis le feu au rideau. »",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(20)), E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 1), (AiAxis.Energy, 1))),
                Plain("regular", "Garder la tasse, et venir en client",
                    "{A} garde la tasse et promet de venir chaque samedi, comme client. Un très bon client, qui laisse toujours un pourboire dans la tasse ébréchée, posée au bout du comptoir. La tortue fait semblant de ne pas comprendre.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1))),
                Plain("secret", "Demander d'abord le secret du chocolat",
                    "La tortue regarde à droite, à gauche, puis chuchote le secret à l'oreille de {A}. C'est très simple, et très surprenant. {A} jure de ne jamais le répéter, et ne l'a jamais répété.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("hearty")), Ai((AiAxis.Rationality, 1), (AiAxis.Greed, 1))),
            }),

        new EventDef("grown_dream_owl", EventType.FollowUp, AnyStage, "Trois cent cinq",
            "Une plume grise tombe sur la table de {A}, par la fenêtre ouverte. Le héron est sur le rebord. « Trois cent cinq », dit le héron. {A} met une seconde à comprendre : le soir où {A} comptait ses plumes, le soleil s'était couché à la trois cent quatre. Le héron a attendu tout ce temps pour donner la suite.",
            new[]
            {
                Plain("count", "Reprendre le compte, à partir de trois cent six",
                    "{A} reprend le compte à voix haute. Le héron se tient immobile, très patient. À la nuit tombée, le compte est fini : sept cent douze. Le héron hoche la tête, une fois. Le grand mystère d'enfance est résolu, et c'était beau.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired")), Ai((AiAxis.Rationality, 2))),
                Plain("keep", "Garder la plume, comme marque-page",
                    "{A} glisse la plume dans le livre de chevet. Chaque soir, en l'ouvrant, {A} lit une page de plus. Le héron n'en parle jamais. Mais le héron vient plus souvent sur le rebord.",
                    E(new GiveItem("col.plume"), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1))),
                Try("riddle", "Répondre au héron par une énigme", new EventChallenge(PlynlingStat.Learning, 8),
                    "« Combien d'écailles a la carpe dorée ? » Le héron ouvre le bec, le referme, et s'envole, l'air soucieux. Trois jours plus tard, une écaille dorée attend sur la table, avec un mot : « Je cherche encore. »",
                    "« Combien de pierres compte le vieux pont ? » Le héron répond aussitôt : « Mille quarante et une. » Le héron a eu toute une vie pour compter.",
                    E(new GrowStat(PlynlingStat.Learning)), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("grown_dream_chief", EventType.FollowUp, AnyStage, "Le chef de la fête",
            "Le maire cherche quelqu'un pour organiser la fête d'été. Personne ne se propose. Puis l'escargot lève lentement une antenne : « Quand {A} était {a:petit|petite}, {A} m'a donné un ordre. J'ai obéi. Je propose {A}. » Le village, à la surprise générale, approuve.",
            new[]
            {
                Try("lead", "Accepter, et tout diriger", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} donne des ordres à tout le monde, avec tact, cette fois. La fête est la plus belle depuis des années. Au premier rang, l'escargot porte un ruban : « Premier à avoir obéi ».",
                    "{A} donne trop d'ordres, trop vite. La fête finit en désordre, mais en joyeux désordre, et seul l'escargot a suivi chaque consigne à la lettre. Ça compte.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("well_spoken")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("deputy", "Accepter, à condition que l'escargot soit adjoint",
                    "L'escargot, ému, prend son rôle très au sérieux, et inspecte chaque lampion, un par un. Les préparatifs prennent deux fois plus de temps, et sont deux fois plus soignés.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Plain("decline", "Refuser poliment : c'était un rêve d'enfant",
                    "{A} refuse, avec un sourire. Le village insiste un peu, pour la forme, puis se tourne vers l'ours. Le soir, l'escargot passe chez {A}, très lentement, pour dire : « Tu aurais fait un très bon chef. » {A} garde la phrase pour les mauvais jours.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        // adulte et ancien: échos du grand apprentissage (the old master comes back)
        new EventDef("grown_master_owl", EventType.FollowUp, AnyStage, "La clé de la réserve",
            "La chouette frappe chez {A}, pour la première fois, emmitouflée dans un châle. « Je pars trois jours, de l'autre côté de la rivière. La bibliothèque a besoin de quelqu'un qui sache dire chut. » La chouette tend la clé de la réserve, celle des livres que personne n'a le droit de lire. « Je ne te demande pas de ne pas les ouvrir. »",
            new[]
            {
                Plain("guard", "Garder la bibliothèque, sans ouvrir la réserve",
                    "{A} garde la bibliothèque trois jours, dit « chut » quarante fois, et ne touche pas à la réserve. Au retour, la chouette vérifie la poussière sur la poignée, intacte, et sourit. « Tu as retenu la leçon d'autrefois. »",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Try("read", "Ouvrir la réserve, et lire un seul livre", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} ouvre la réserve et choisit un livre au hasard : c'est le journal de la chouette, écrit quand la chouette avait l'âge de {A}. {A} le lit d'une traite, et le remet exactement à sa place. Au retour, la chouette regarde {A} et dit seulement : « Alors, tu sais. »",
                    "{A} ouvre la réserve. Un nuage de poussière en sort, avec un papillon de nuit très âgé et très vexé, qui suit {A} pendant trois jours. Au retour, la chouette salue le papillon par son nom.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, -1))),
            }),

        new EventDef("grown_master_station", EventType.FollowUp, AnyStage, "Le train de 17 h 03",
            "Le hérisson chef de gare a attrapé un rhume, le premier de sa carrière. Couché, une bouillotte sur le ventre, le hérisson tend à {A} la montre de gare, la casquette et le sifflet. « Le 17 h 03. Le grand train de l'année. Tu te souviens de la leçon un ? » {A} s'en souvient : on n'est jamais en avance.",
            new[]
            {
                Try("station", "Tenir la gare toute la journée, comme le hérisson", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} ouvre la gare à l'aube, vend les billets, chasse un pigeon du quai, et siffle le départ du 17 h 03 à la seconde exacte. Le soir, {A} rapporte la montre au hérisson, qui la regarde, regarde {A}, et éternue de fierté.",
                    "{A} siffle le départ du 17 h 03 à 17 h 02. Le train part. Les voyageurs ne remarquent rien. Mais au lit, le hérisson a entendu, et a compté. Le hérisson n'en dira jamais rien. Mais le hérisson sait.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(15)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
                Plain("soup", "Tenir la gare, et porter la soupe entre deux trains",
                    "Entre chaque train, {A} court porter une soupe, une tisane, le journal. Le hérisson finit par guérir, surtout pour pouvoir dormir tranquille. Le 17 h 03 part à l'heure. À peu près.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_master_market", EventType.FollowUp, AnyStage, "La retraite de la pie",
            "La pie prend sa retraite. Le tonneau du marché est vide, et la pie porte un petit sac sur l'épaule. « Avant de partir, dit la pie à {A}, je dois rendre tout ce que j'ai emprunté depuis trente ans. Ça fait beaucoup. Tu m'aides ? » Le sac fait un bruit de cuillères.",
            new[]
            {
                Try("night", "Tout rendre en une nuit, sans être {a:vu|vue}", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "Toute la nuit, {A} et la pie remettent à leur place les cuillères, les boutons, les dés à coudre et une paire de lunettes que tout le monde cherchait. Au matin, le village retrouve trente ans d'objets perdus. En partant, la pie laisse à {A} une bague. « Celle-là, je l'ai trouvée, pas empruntée. Promis. »",
                    "{A} et la pie se font surprendre par l'ours, au moment de remettre sa propre cuillère dans son propre tiroir. Long silence. Puis l'ours éclate de rire, et aide à rendre le reste.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveItem("col.bague")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Honor, 1), (AiAxis.Boldness, 1))),
                Plain("stall", "Tout rendre au grand jour, sur un étal",
                    "{A} étale tout sur une table, au milieu du marché, avec une pancarte : « Rendu par la pie ». Le village vient chercher ses affaires en riant. La pie, très gênée, reçoit en échange tant de cadeaux que le petit sac déborde au départ.",
                    E(new ApplyModifier("clear_conscience"), new ApplyModifier("magpie_friend")), Ai((AiAxis.Honor, 2), (AiAxis.Sociability, 1))),
            }),

        // ancien: jamais trop tard (CK3's adult education and debates: a season at the academy)
        new EventDef("elder_academy", EventType.Pulse, Elder, "Jamais trop tard",
            "Une lettre arrive de l'autre côté de la vallée, cachetée de cire verte. L'académie des Collines accepte, pour une saison, quelques élèves « d'un certain âge ». {A} relit la lettre en ajustant ses lunettes. Dehors, le potager attend, le fauteuil aussi. La vallée paraît très loin, et très près.",
            new[]
            {
                Plain("go", "Faire sa valise",
                    "{A} fait sa valise : trois carnets, un pull, un pot de miel pour la route. Le village accompagne {A} jusqu'au vieux pont, comme pour un tout jeune. Le héron, sur une patte, fait un signe de tête qui veut dire « enfin ».",
                    E(new FollowUp("elder_academy_master", 48, 96)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("walk", "Partir à pied, par le chemin des crêtes",
                    "{A} part à pied, par les crêtes, en prenant son temps. Le voyage dure quatre jours. {A} arrive à l'académie avec des ampoules, un carnet plein de croquis, et l'impression d'avoir déjà appris quelque chose.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("elder_academy_master", 48, 96)), Ai((AiAxis.Energy, 1), (AiAxis.Boldness, 1))),
                Plain("stay", "Ranger la lettre : c'est pour les jeunes, ces choses-là",
                    "{A} range la lettre dans le tiroir et retourne au potager. Le soir, {A} la ressort, la relit, puis la remet. Les tomates sont très belles cette année. C'est déjà beaucoup.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1), (AiAxis.Boldness, -1))),
            }),

        new EventDef("elder_academy_master", EventType.FollowUp, AnyStage, "Le maître des questions",
            "Le maître de l'académie est une vieille taupe presque aveugle, qui ne répond jamais à une question que par une autre question. Les jeunes élèves en pleurent de rage. Au premier cours, la taupe se tourne droit vers {A}, sans voir : « Et toi, qu'es-tu {a:venu|venue} chercher si loin, à ton âge ? »",
            new[]
            {
                Try("question", "Répondre par une question, à son tour", new EventChallenge(PlynlingStat.Learning, 9),
                    "« Et toi, pourquoi enseignes-tu encore, à ton âge ? » La salle retient son souffle. La taupe éclate de rire, pour la première fois depuis des années, et fait asseoir {A} au premier rang. Le reste de la saison, les deux se répondent par des questions, et les jeunes prennent des notes.",
                    "{A} répond par une question. La taupe répond par une question. {A} aussi. Au bout de vingt minutes, plus personne ne sait de quoi on parlait, et la cloche sonne. Les jeunes appellent ça « le grand match nul ».",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("elder_academy_students", 48, 96)), E(new FollowUp("elder_academy_students", 48, 96)), Ai((AiAxis.Rationality, 2))),
                Plain("truth", "Répondre franchement : « Je ne sais pas encore. »",
                    "« Je ne sais pas encore. » La taupe hoche lentement la tête. « Bien. C'est la seule bonne réponse, ici. Les autres ont mis trois semaines à la trouver. » Les jeunes élèves regardent {A} d'un autre œil, désormais.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("elder_academy_students", 48, 96)), Ai((AiAxis.Honor, 2))),
                Plain("young", "S'asseoir au fond, avec les jeunes",
                    "{A} s'installe au fond et demande aux jeunes comment ça marche, ici. Une musaraigne explique tout, très vite, avec des schémas. En échange, {A} explique les fractions. C'est le début d'une drôle d'amitié.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("elder_academy_students", 48, 96)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("elder_academy_students", EventType.FollowUp, AnyStage, "La nuit des étudiants",
            "Un soir, on gratte à la fenêtre de {A}. Ce sont les jeunes élèves, la musaraigne en tête, qui font le mur pour aller danser au village voisin. « Tu viens ? » C'est une blague, évidemment. Tout le monde rit. Tout le monde attend aussi la réponse.",
            new[]
            {
                Try("dance", "Y aller, et danser plus longtemps que tous", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} enjambe la fenêtre (doucement), danse la gigue, la polka et une danse inventée sur place, et rentre à l'aube en portant la musaraigne endormie sur son dos. Au petit-déjeuner, la taupe fait semblant de ne rien savoir, et sourit dans son bol.",
                    "{A} enjambe la fenêtre, se coince le pull dans le volet, et reste {a:suspendu|suspendue} là jusqu'à ce que les jeunes reviennent, à l'aube, pour décrocher {A} en riant. Toute la saison, ce volet s'appellera « le volet de {A} ».",
                    E(new GrowStat(PlynlingStat.Courage), new LiftNeed(Need.Happiness, 0.2), new FollowUp("elder_academy_debate", 48, 96)), E(new FollowUp("elder_academy_debate", 48, 96)),
                    Ai((AiAxis.Energy, 2), (AiAxis.Boldness, 1))),
                Plain("fountain", "Y aller, mais pour raconter des histoires à la fontaine",
                    "{A} s'assoit à la fontaine du village voisin pendant que les jeunes dansent. Peu à peu, les danseurs s'arrêtent pour écouter : la crue d'autrefois, le vieux pont, la carpe dorée. À minuit, plus personne ne danse. Tout le monde écoute.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("elder_academy_debate", 48, 96)), Ai((AiAxis.Sociability, 2))),
                Plain("window", "Rester, mais laisser la fenêtre ouverte",
                    "{A} reste, laisse la fenêtre entrouverte et une lampe allumée. À trois heures, les jeunes rentrent sur la pointe des pieds, et trouvent sur le rebord une assiette de biscuits. Personne n'en parle jamais. Les biscuits disparaissent quand même.",
                    E(new ApplyModifier("cherished"), new FollowUp("elder_academy_debate", 48, 96)), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("elder_academy_debate", EventType.FollowUp, AnyStage, "La grande dispute",
            "Fin de saison : la grande dispute publique. Le jeune geai le plus brillant de l'académie monte sur l'estrade, gonfle ses plumes, et lance devant tout le monde : « Les vieux ne savent que des choses vieilles. » Puis désigne {A}. La salle se retourne. Au premier rang, la taupe croise les pattes et attend.",
            new[]
            {
                Try("argue", "Répondre point par point, sans hausser le ton", new EventChallenge(PlynlingStat.Learning, 9),
                    "{A} répond calmement, point par point, avec trois exemples, une date et une recette de confiture. À la fin, le geai ouvre le bec, le referme, et descend de l'estrade. Puis revient, l'air penaud, pour noter la recette.",
                    "{A} répond point par point, mais perd le fil au deuxième. Le geai triomphe. Puis, dans le couloir, le geai rattrape {A} : « Ton premier point, c'était le meilleur. Tu peux me le réexpliquer ? »",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("well_spoken"), new FollowUp("elder_academy_home", 48, 96)), E(new FollowUp("elder_academy_home", 48, 96)),
                    Ai((AiAxis.Rationality, 2)), Stress(("shy", 20))),
                Try("joke", "Répondre par une plaisanterie", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "« Les vieux ne savent que des choses vieilles, dit {A}. Par exemple, où le cuisinier cache les biscuits. » La salle éclate de rire. Le geai aussi, finalement, après une longue seconde. Fin de la dispute, début d'une amitié.",
                    "La plaisanterie tombe à plat. Un seul rire, au fond : la taupe, qui rit cinq minutes toute seule et finit par entraîner la salle entière. Plus personne ne sait pourquoi on rit. C'est encore mieux.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("elder_academy_home", 48, 96)), E(new LiftNeed(Need.Happiness, 0.15), new FollowUp("elder_academy_home", 48, 96)),
                    Ai((AiAxis.Sociability, 2))),
                Plain("concede", "Concéder : « Tu as raison. Apprends-moi. »",
                    "{A} descend vers le geai et lui demande, sincèrement, de lui apprendre quelque chose de neuf. Le geai, désarmé, explique les étoiles filantes, à sa façon, très bien. La taupe applaudit. Ce n'était pas une dispute, finalement. C'était un cours.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("elder_academy_home", 48, 96)), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1)), Stress(("arrogant", 20))),
            }),

        new EventDef("elder_academy_home", EventType.FollowUp, AnyStage, "Le retour",
            "La saison est finie. Sur le seuil de l'académie, la taupe tend à {A} un petit paquet ficelé, et dit, pour la première fois, une phrase sans point d'interrogation : « Tu vas me manquer. » Trois jours plus tard, {A} passe le vieux pont. Tout le village attend sur l'autre rive, et le héron a mis sa plus belle patte devant.",
            new[]
            {
                Plain("parcel", "Ouvrir le paquet de la taupe",
                    "Dans le paquet : une carte de la vallée, dessinée à la main par la taupe, avec tous les chemins qu'on ne voit pas d'en haut. Au dos, une seule question, bien sûr : « Et maintenant ? »",
                    E(new GiveItem("col.carte_tresor"), new ApplyModifier("inspired")), Ai((AiAxis.Rationality, 1))),
                Plain("teach", "Réunir les petits du village, et leur raconter l'académie",
                    "Dès le lendemain, {A} réunit les petits sous le vieux chêne, et répond à toutes leurs questions par d'autres questions. Les petits en pleurent de rage. {A} n'a jamais été aussi {a:heureux|heureuse}.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("cherished")), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("chair", "Rentrer, et retrouver son fauteuil",
                    "{A} rentre, pose la valise, et s'assoit dans le fauteuil. Le fauteuil a gardé sa forme. Le potager a poussé tout seul, dans tous les sens. {A} le regarde longtemps, et commence déjà une liste de questions pour l'an prochain.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            }),

        // ---- wave 8: ancien
        new EventDef("elder_rocking_chair", EventType.Pulse, Elder, "Le fauteuil à bascule",
            "Le village s'est cotisé pour offrir à {A} un fauteuil à bascule, avec un coussin brodé : « Repos bien mérité ». Toute la place regarde {A} découvrir le cadeau. Le blaireau, qui l'a fabriqué, tortille sa casquette.",
            new[]
            {
                Plain("sit", "S'y asseoir tout de suite, et se balancer",
                    "{A} s'assoit, se balance une fois, deux fois, et s'endort au troisième balancement, devant tout le village. On fait silence. Le blaireau, très fier, déclare le fauteuil réussi.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1), (AiAxis.Compassion, 1))),
                Try("ride", "Y voir plutôt un véhicule", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} pose le fauteuil en haut de la côte, s'y installe, et descend la grand-rue en se balançant, sous les cris du village. Arrivée devant le café, sans une égratignure. Le blaireau commence déjà les plans d'un deuxième fauteuil, avec des freins.",
                    "{A} pose le fauteuil en haut de la côte. Le fauteuil part sans {A}, descend toute la grand-rue tout seul, et s'arrête pile devant le café. La tortue s'y assoit aussitôt, et refuse de le rendre.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("fired_up")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 2), (AiAxis.Energy, 1))),
                Plain("attic", "Remercier, et le monter au grenier",
                    "{A} remercie très poliment, monte le fauteuil au grenier, et redescend d'un pas très vif, pour bien montrer. Le soir, en secret, {A} remonte essayer le fauteuil. Le fauteuil est parfait. Personne ne le saura jamais.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Boldness, 1)), Stress(("humble", 15))),
            }),

        new EventDef("elder_old_rival", EventType.Pulse, Elder, "Une dernière partie",
            "Un vieux renard descend du train de midi, une canne à la patte et une boîte sous le bras. {A} le reconnaît tout de suite : le seul adversaire qui ait jamais battu {A} aux noix, voilà cinquante ans, d'un seul point. Le renard pose la boîte sur une table du café et l'ouvre : le même plateau. « Une dernière ? »",
            new[]
            {
                Try("play", "Jouer pour gagner, enfin", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "La partie dure tout l'après-midi. Au dernier coup, {A} gagne d'un seul point. Le renard regarde le plateau, puis éclate de rire : « Cinquante ans que j'attendais ça. » Le renard laisse le plateau à {A}, et reprend le train du soir.",
                    "La partie dure tout l'après-midi. Au dernier coup, le renard gagne d'un seul point. Encore. « On se revoit dans cinquante ans ? » demande le renard. {A} dit oui, très sérieusement.",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("fired_up")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("talk", "Laisser le plateau fermé, et parler du bon vieux temps",
                    "{A} referme la boîte. Les deux parlent jusqu'au train du soir : des chemins qui ont changé, des gens partis, et de la partie d'autrefois, que chacun se souvient d'avoir gagnée. Sur le quai, le renard dit : « La prochaine fois, on joue. » Les deux savent qu'on dira la même chose.",
                    E(new ApplyModifier("soothed"), new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Sociability, 2))),
                Plain("gift", "Laisser gagner le renard, encore une fois",
                    "{A} joue un peu moins bien, exprès, au tout dernier coup. Le renard gagne d'un point, comme avant. Sur le quai, le renard se retourne : « Tu m'as laissé gagner. Merci. » Puis le train part.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("elder_dance", EventType.Pulse, Elder, "La première danse",
            "Au bal du village, {A} est {a:assis|assise} près de l'orchestre, à battre la mesure du bout de sa canne. Un tout petit du village s'approche, tire sur la manche de {A}, et demande, très sérieusement : « Tu veux danser ? » Le tout petit arrive à peine aux genoux de {A}.",
            new[]
            {
                Plain("slow", "Accepter, et ouvrir la danse",
                    "{A} se lève, prend les deux pattes du petit, et danse, tout doucement, au milieu de la place. D'autres s'arrêtent pour regarder, puis pour imiter. À la fin, toute la place danse au rythme lent de {A}.",
                    E(new LiftNeed(Need.Happiness, 0.25), new ApplyModifier("cherished")), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Try("spin", "Montrer comment on dansait autrefois", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} lance le petit dans un tourbillon, puis un autre : une vieille danse que plus personne ne connaît. Les anciens du village se lèvent un à un et retrouvent les pas. Le temps d'une chanson, le bal a cinquante ans de moins.",
                    "{A} lance un tourbillon, et la canne avec, et un peu de sa dignité. Le petit rattrape la canne, la rapporte, et propose : « On fait plutôt la danse lente ? » Oui. On fait plutôt la danse lente.",
                    E(new GrowStat(PlynlingStat.Courage), new LiftNeed(Need.Happiness, 0.2)), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Energy, 2), (AiAxis.Boldness, 1))),
                Plain("lesson", "Proposer d'abord une leçon, sur le côté",
                    "{A} et le petit s'installent dans un coin, et {A} enseigne les pas un par un, en comptant. À la dernière chanson, le petit entraîne {A} sur la piste et fait tout parfaitement, sauf sur les pieds de {A}, qui s'en moque bien.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
            }),
    };
}
