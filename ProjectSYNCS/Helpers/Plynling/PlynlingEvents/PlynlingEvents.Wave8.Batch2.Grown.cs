using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 8, second batch: adulte et ancien (scarecrows to the pond war).
public static partial class PlynlingEvents
{
    private static EventDef[] Wave8Batch2Grown() => new[]
    {
        // ---- wave 8, second batch: adulte et ancien
        // le concours d'épouvantails (CK3 contests: the build, the night raid, the judging)
        new EventDef("grown_scarecrow", EventType.Pulse, Grown, "Le concours d'épouvantails",
            "Le moineau a annoncé le concours du plus bel épouvantail, jugé dans une semaine sur la place. Le juge, c'est le moineau lui-même, ce que personne ne semble trouver bizarre. L'an dernier, le blaireau a gagné avec un épouvantail si réussi que le moineau n'a jamais osé s'approcher pour remettre le ruban. {A} a une botte de paille, un vieux manteau, et une idée.",
            new[]
            {
                Plain("scary", "Le plus effrayant possible",
                    "{A} coud des sourcils froncés, des dents en noyaux de prune, et un manteau noir qui claque au vent. Le premier soir, {A} sursaute en le croisant dans le jardin. C'est bon signe.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_scarecrow_crows", 72, 120)), Ai((AiAxis.Boldness, 2))),
                Plain("elegant", "Le plus élégant du village",
                    "{A} lui offre un chapeau à plume, un nœud papillon et une canne. L'épouvantail a plus d'allure que la moitié du village. Les corbeaux viennent le saluer.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_scarecrow_crows", 72, 120)), Ai((AiAxis.Sociability, 2))),
                Plain("twin", "Un épouvantail à son image",
                    "{A} lui fait porter son écharpe, son chapeau et son air du dimanche. La ressemblance est troublante. Le facteur lui dit bonjour deux fois, et attend la réponse.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_scarecrow_crows", 72, 120)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_scarecrow_crows", EventType.FollowUp, AnyStage, "La nuit des corbeaux",
            "Une nuit, des croassements réveillent {A}. Dans le jardin, sous la lune, une bande de corbeaux s'est installée sur l'épouvantail : un sur le chapeau, deux sur les bras, et le plus gros dans la poche du manteau. Les corbeaux n'ont pas peur du tout. Les corbeaux ont l'air de beaucoup s'amuser.",
            new[]
            {
                Try("chase", "Sortir en chemise de nuit, et chasser les corbeaux", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} sort en agitant un balai, et les corbeaux s'envolent en croassant très fort, vexés. Au matin, l'épouvantail est intact, à un bouton près : les corbeaux l'ont emporté en souvenir.",
                    "{A} sort en agitant un balai. Les corbeaux s'envolent, tournent, et se reposent tous sur {A}. Au matin, l'épouvantail est intact, et {A} a des plumes partout.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_scarecrow_judging", 72, 120)), E(new FollowUp("grown_scarecrow_judging", 72, 120)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
                Try("bells", "Ajouter des clochettes et des rubans qui claquent", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} accroche des clochettes aux manches et des rubans au chapeau. Au moindre souffle, l'épouvantail tinte et danse. Les corbeaux le regardent, se consultent, et vont s'installer chez le blaireau.",
                    "{A} accroche des clochettes. Les corbeaux adorent les clochettes. La nuit suivante, les corbeaux reviennent à douze, pour le concert.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_scarecrow_judging", 72, 120)), E(new FollowUp("grown_scarecrow_judging", 72, 120)),
                    Ai((AiAxis.Rationality, 2))),
                Plain("deal", "Négocier avec le plus gros corbeau",
                    "{A} propose un marché au plus gros corbeau : des miettes de pain chaque matin, contre la paix pour l'épouvantail. Le corbeau accepte d'un hochement de bec. Le jour du concours, les corbeaux viennent même encourager.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_scarecrow_judging", 72, 120)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_scarecrow_judging", EventType.FollowUp, AnyStage, "Le jugement du moineau",
            "Jour du concours. Les épouvantails sont alignés sur la place, et le moineau passe devant chacun, un carnet à la patte, à distance prudente. Devant celui du blaireau, le moineau recule de trois pas. Puis vient le tour de celui de {A}. Le moineau s'approche, et ses plumes se hérissent un peu.",
            new[]
            {
                Try("speech", "Présenter son épouvantail avec un petit discours", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} présente l'épouvantail comme un vieil ami : son nom, ses goûts, sa peur des chats. Le moineau rit, prend des notes, et décerne à {A} le premier prix, avec la bourse. Le blaireau, beau joueur, vient serrer la manche de l'épouvantail.",
                    "{A} commence le discours. Une rafale fait pivoter l'épouvantail vers le moineau, d'un coup. Le moineau s'envole sur le toit de la gare, et n'en redescend qu'à la nuit. Le prix arrive par courrier le lendemain : deuxième place.",
                    E(new GiveCailloux(25), new ApplyModifier("well_spoken")), E(new GiveCailloux(10), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2)), Stress(("shy", 15))),
                Plain("gift", "Offrir l'épouvantail au potager de la tortue",
                    "{A} retire l'épouvantail du concours et l'installe dans le potager de la tortue, qui se fait voler ses fraises depuis des années. Plus une fraise ne disparaît. La tortue donne à l'épouvantail le nom de {A}.",
                    E(new ApplyModifier("cherished"), new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Compassion, 2))),
                Plain("inside", "Se glisser dans le manteau, et bouger au bon moment",
                    "Quand le moineau s'approche, l'épouvantail lui fait un clin d'œil. Le moineau pousse un cri que toute la vallée entend. Prix spécial du jury : « Le plus vivant ». Le moineau refuse de le remettre en main propre.",
                    E(new GrowStat(PlynlingStat.Intrigue), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
            }),

        // le coin du blaireau (CK3's hunt activity: the invitation, the fog, the find)
        new EventDef("grown_forage", EventType.Pulse, Grown, "Le coin du blaireau",
            "Le vieux blaireau, qui connaît la forêt mieux que personne, propose à {A} de l'accompagner à la cueillette des morilles. « Une condition : tu ne diras jamais à personne où on va. » Départ demain, avant l'aube. Le blaireau a déjà deux paniers.",
            new[]
            {
                Plain("swear", "Jurer le secret, et préparer son panier",
                    "{A} jure, la patte levée. Le blaireau ne sourit pas, mais tend un bandeau. « Pour le chemin. » {A} met le bandeau. Le secret est bien gardé, au moins jusqu'à la forêt.",
                    E(new FollowUp("grown_forage_fog", 12, 24)), Ai((AiAxis.Honor, 1))),
                Plain("notes", "Accepter, et noter discrètement le chemin",
                    "{A} accepte, et glisse un carnet dans sa poche. Tout le long du chemin, {A} note les arbres, les pierres, les virages. Le blaireau ne voit rien, ou fait semblant.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_forage_fog", 12, 24)), Ai((AiAxis.Honor, -1), (AiAxis.Greed, 1)), Stress(("honest", 20))),
                Plain("sleep", "Refuser : avant l'aube, très peu pour soi",
                    "« Avant l'aube ? Non merci. » Le blaireau hausse les épaules et part seul. Le lendemain soir, une petite morille attend devant la porte de {A}, sans un mot. C'est tout le blaireau, ça.",
                    E(new GiveItem("food.morel")), Ai((AiAxis.Energy, -2))),
            }),

        new EventDef("grown_forage_fog", EventType.FollowUp, AnyStage, "Le brouillard",
            "Au cœur de la forêt, le brouillard tombe d'un coup, épais comme du lait. Le blaireau, devant, a disparu. {A} appelle. Rien. Seulement le bruit des gouttes sur les feuilles, et quelque part, très loin, un pic qui tape.",
            new[]
            {
                Try("track", "Suivre les traces du blaireau dans la mousse", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} s'accroupit, trouve une empreinte, puis une autre, puis une brindille cassée. Dix minutes plus tard, le blaireau est là, assis sur une souche. « Tu en as mis, du temps. » C'était un test. {A} l'a réussi.",
                    "{A} suit des traces, longtemps, très {a:sûr|sûre} de soi, et tombe nez à nez avec un sanglier, qui suivait aussi des traces. Les deux s'excusent. Le blaireau retrouve {A} une heure plus tard, mort de rire.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_forage_find", 2, 6)), E(new FollowUp("grown_forage_find", 2, 6)), Ai((AiAxis.Rationality, 2))),
                Plain("wait", "Ne plus bouger, et attendre",
                    "{A} s'assoit au pied d'un chêne, et attend. Le brouillard passe autour, comme une rivière. Au bout d'une heure, le blaireau sort de la brume, l'air satisfait. « Bien. On ne court jamais dans le brouillard. »",
                    E(new ApplyModifier("soothed"), new FollowUp("grown_forage_find", 2, 6)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
                Try("whistle", "Siffler deux notes, comme le blaireau a appris", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} siffle deux notes. Une réponse arrive, sur la gauche. Puis le blaireau, qui s'était caché tout près, exprès. « Tu as retenu. Bien. »",
                    "{A} siffle. Un merle répond. Puis deux. Puis toute la forêt. Le blaireau finit par retrouver {A} au milieu d'un concert d'oiseaux, et en oublie de faire la leçon.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_forage_find", 2, 6)), E(new FollowUp("grown_forage_find", 2, 6)), Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("grown_forage_find", EventType.FollowUp, AnyStage, "La clairière",
            "Le blaireau écarte une branche basse. Derrière, au pied d'un vieux frêne, une clairière où la mousse est criblée de morilles, des dizaines, comme si quelqu'un les avait plantées. Le blaireau parle tout bas : « Mon coin. Depuis quarante ans. Personne d'autre ne l'a jamais vu. »",
            new[]
            {
                Plain("few", "Ne cueillir que le nécessaire",
                    "{A} cueille trois morilles, pas une de plus. Le blaireau regarde le panier, puis {A}. « Tu reviendras, alors. » C'est une invitation. Le blaireau n'en a jamais fait.",
                    E(new GiveItem("food.morel"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, 1))),
                Plain("fill", "Remplir le panier à ras bord",
                    "{A} remplit le panier à ras bord. Le blaireau ne dit rien, mais sur le chemin du retour, marche un peu plus vite, et ne se retourne pas.",
                    E(new GiveItem("food.morel"), new LiftNeed(Need.Hunger, 0.2)), Ai((AiAxis.Greed, 2)), Stress(("temperate", 15), ("generous", 15))),
                Try("truffle", "Gratter au pied du frêne, là où la terre sent bon", new EventChallenge(PlynlingStat.Learning, 9),
                    "{A} gratte au pied du frêne, là où la terre sent la noisette et le sous-bois. Une truffe noire, grosse comme une noix. Le blaireau en reste bouche bée. Quarante ans, et le blaireau ne savait pas.",
                    "{A} gratte, gratte, et trouve un ver de terre, très étonné. Le blaireau rit si fort qu'une morille tombe du panier.",
                    E(new GiveItem("food.truffle")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Rationality, 1), (AiAxis.Greed, 1))),
            }),

        // le grand hoquet (CK3's epidemics, played for laughs: the outbreak, the cure, the relapse)
        new EventDef("grown_hiccups", EventType.Pulse, Grown, "Le grand hoquet",
            "Ça a commencé avec l'ours, au café, mardi. Puis la tortue. Puis le moineau, en plein milieu d'un discours. Aujourd'hui, la moitié du village a le hoquet, un hoquet tenace, qui ne part plus. On entend le village de loin : hic. Hic. Le médecin est en vacances.",
            new[]
            {
                Plain("infirmary", "Ouvrir une infirmerie dans sa cuisine",
                    "{A} installe des chaises, des couvertures et une grande théière dans la cuisine. Les malades arrivent un par un, en faisant hic. La cuisine résonne comme une horloge détraquée.",
                    E(new ApplyModifier("cherished"), new FollowUp("grown_hiccups_cure", 24, 48)), Ai((AiAxis.Compassion, 2))),
                Plain("books", "Chercher un remède dans les livres de la chouette",
                    "{A} passe la journée à la bibliothèque. La chouette, qui a le hoquet aussi, fait « chut… hic » toutes les deux minutes. À la nuit, {A} a trouvé trois remèdes, dont un avec un œuf et la pleine lune.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_hiccups_cure", 24, 48)), Ai((AiAxis.Rationality, 2))),
                Plain("shut", "S'enfermer chez soi, pour ne pas l'attraper",
                    "{A} ferme les volets, bouche la serrure, et attend. Le soir, dans le silence de la maison : hic. {A} l'a quand même. Autant aider, maintenant.",
                    E(new FollowUp("grown_hiccups_cure", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, -1))),
            }),

        new EventDef("grown_hiccups_cure", EventType.FollowUp, AnyStage, "Le remède",
            "Trois jours de hoquet. Le village n'en peut plus. Le moineau n'arrive plus à siffler, le hérisson n'arrive plus à annoncer les trains, et la tortue a renversé quarante tasses. Tout le monde se tourne vers {A}, qui a peut-être une idée.",
            new[]
            {
                Try("boo", "Leur faire très peur, à tous en même temps", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} rassemble le village derrière la boulangerie, puis surgit, un drap sur la tête : « BOUH ! » Silence. Plus un seul hic. Le village est guéri d'un coup, un peu fâché, mais guéri.",
                    "{A} surgit, un drap sur la tête : « BOUH ! » Personne n'a peur. Tout le monde rit. Et en riant, le hoquet passe, chez tout le monde, d'un seul coup. Personne ne comprend pourquoi. Le remède est noté quand même.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_hiccups_relapse", 24, 48)), E(new FollowUp("grown_hiccups_relapse", 24, 48)), Ai((AiAxis.Boldness, 2))),
                Try("water", "Le grand verre d'eau bu à l'envers, pour tout le monde", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} aligne quarante verres d'eau sur la place et montre comment boire à l'envers, penché en avant. Le village s'exécute, en rang, très sérieusement. Une demi-heure plus tard, plus un hic. Le moineau siffle la fin de l'épidémie.",
                    "Quarante verres d'eau bus à l'envers, et quarante chemises trempées. Le hoquet est toujours là. Le hoquet finit par partir tout seul, le lendemain, sans prévenir, comme le hoquet était venu.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_hiccups_relapse", 24, 48)), E(new FollowUp("grown_hiccups_relapse", 24, 48)), Ai((AiAxis.Rationality, 2))),
                Plain("nap", "Une tisane pour tout le monde, et une sieste générale",
                    "{A} prépare un chaudron de tisane et ordonne une sieste générale. Le village dort tout l'après-midi, sur la place, dans l'herbe. Au réveil, plus aucun hic. Juste des marques d'herbe sur les joues.",
                    E(new ApplyModifier("soothed"), new FollowUp("grown_hiccups_relapse", 24, 48)), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_hiccups_relapse", EventType.FollowUp, AnyStage, "Hic",
            "Le village est guéri. Tout le monde remercie {A} ; on parle de statue, ou au moins d'une plaque. Et ce matin, au café, au moment de prendre son chocolat, devant tout le monde, {A} fait : « Hic. » Toutes les têtes se tournent. « Hic. »",
            new[]
            {
                Plain("laugh", "En rire avec tout le monde",
                    "{A} éclate de rire, entre deux hics. Le café entier rit avec {A}, et en riant, le hoquet passe. Personne ne fait de statue, finalement, mais la tortue baptise un chocolat « le Hic », et c'est mieux.",
                    E(new LiftNeed(Need.Happiness, 0.2), new ApplyModifier("light_heart")), Ai((AiAxis.Sociability, 2))),
                Plain("own", "Appliquer son propre remède, très dignement",
                    "{A} applique son propre remède, très dignement, devant tout le monde. Ça marche. Le café applaudit un remède qui marche même sur son inventeur. {A} salue, et fait un dernier petit hic, minuscule, qui ne compte pas.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Rationality, 1))),
            }),

        // la guerre de la mare (CK3's feuds between houses: the flags, the raid, the peace)
        new EventDef("grown_pond_war", EventType.Pulse, Grown, "La guerre de la mare",
            "Les grenouilles et les canards ne se parlent plus. Les grenouilles réclament la mare depuis toujours. Les canards aussi, depuis plus longtemps encore, à les entendre. Ce matin, chaque camp a planté un drapeau sur la rive. Les deux drapeaux se regardent. Les deux camps regardent {A}.",
            new[]
            {
                Try("treaty", "Proposer un traité : le matin aux uns, l'après-midi aux autres", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Après trois heures de palabres, le traité est signé d'une patte palmée et d'une empreinte de bec. La mare est partagée. Tout le monde est mécontent à parts égales. C'est le signe d'un bon traité, même si ce traité ne tiendra peut-être pas.",
                    "{A} propose le partage. Les grenouilles veulent le matin. Les canards aussi. La réunion finit dans un grand plouf collectif, et {A} rentre {a:trempé|trempée}.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_pond_war_raid", 24, 48)), E(new FollowUp("grown_pond_war_raid", 24, 48)), Ai((AiAxis.Sociability, 1), (AiAxis.Honor, 1))),
                Plain("frogs", "Prendre le parti des grenouilles",
                    "{A} se range du côté des grenouilles, qui chantent en l'honneur de {A} toute la nuit. Les canards, eux, tournent le dos à {A} avec une synchronisation parfaite.",
                    E(new FollowUp("grown_pond_war_raid", 24, 48)), Ai((AiAxis.Boldness, 1)), Stress(("just", 15))),
                Plain("ducks", "Prendre le parti des canards",
                    "{A} se range du côté des canards, qui défilent devant {A} en file indienne, en signe d'honneur. Les grenouilles coassent des choses que personne ne traduira.",
                    E(new FollowUp("grown_pond_war_raid", 24, 48)), Ai((AiAxis.Boldness, 1)), Stress(("just", 15))),
            }),

        new EventDef("grown_pond_war_raid", EventType.FollowUp, AnyStage, "Le raid de minuit",
            "En pleine nuit, des cris montent de la mare. {A} accourt : les grenouilles ont volé le drapeau des canards, les canards celui des grenouilles, et dans la mêlée, les deux drapeaux sont tombés au milieu de l'eau. Les deux camps se tournent vers {A}, chacun persuadé que {A} soutient l'autre.",
            new[]
            {
                Try("dive", "Plonger récupérer les deux drapeaux", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} plonge, une fois, deux fois, et ressort avec les deux drapeaux, trempés, emmêlés, cousus ensemble par les algues. Les deux camps contemplent le drapeau double. Personne n'ose le défaire.",
                    "{A} plonge et ressort avec un seul drapeau, méconnaissable, couvert de vase. Grenouilles et canards le réclament en même temps. Puis, ensemble, se moquent de la vase sur {A}. C'est un début.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_pond_war_peace", 24, 48)), E(new FollowUp("grown_pond_war_peace", 24, 48)), Ai((AiAxis.Boldness, 2))),
                Plain("sew", "Proposer de coudre un seul drapeau, pour tous",
                    "Les grenouilles veulent du vert, les canards du blanc. Au petit matin, le drapeau de la mare est vert et blanc, à rayures, et assez laid. Tout le monde l'adore.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_pond_war_peace", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Compassion, 1))),
            }),

        new EventDef("grown_pond_war_peace", EventType.FollowUp, AnyStage, "La fête de la mare",
            "Une semaine plus tard, grenouilles et canards organisent ensemble une fête sur la rive, la première depuis que la mare existe. Sur la nappe, des mouches pour les uns, du pain pour les autres, et des gâteaux pour {A}, {a:l'invité|l'invitée} d'honneur. Le vieux crapaud, qui n'est d'aucun camp, se lève pour le discours.",
            new[]
            {
                Plain("listen", "Écouter le discours du crapaud",
                    "Le crapaud parle longtemps, d'une voix grave : de la mare d'avant, de l'hiver où la mare avait gelé, et où canards et grenouilles s'étaient tenu chaud, ensemble. Personne ne s'en souvenait. Ce soir, tout le monde s'en souvient.",
                    E(new ApplyModifier("soothed"), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Rationality, 1), (AiAxis.Compassion, 1))),
                Plain("swim", "Lancer la première baignade commune",
                    "{A} se jette à l'eau. Les canards suivent, puis les grenouilles. La mare déborde un peu. Le drapeau à rayures flotte au-dessus, laid et magnifique.",
                    E(new ApplyModifier("party_soul"), new LiftNeed(Need.Hygiene, 0.2)), Ai((AiAxis.Energy, 1), (AiAxis.Sociability, 1))),
            }),
    };
}
