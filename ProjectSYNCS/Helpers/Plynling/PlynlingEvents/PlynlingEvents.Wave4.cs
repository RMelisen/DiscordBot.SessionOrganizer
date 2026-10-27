using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 4.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave4() => new[]
    {
        // ---- wave 4: bébé
        new EventDef("baby_ice_cream", EventType.Pulse, Baby, "Cinq minutes au glacier",
            "Le vieil ours du glacier doit s'absenter « cinq minutes » et confie le stand à {A}. Trois parfums, une cuillère, et une file de clients qui arrive déjà.",
            new[]
            {
                Plain("taste", "Goûter chaque parfum, pour bien conseiller",
                    "{A} goûte la myrtille, puis la noisette, puis le miel, puis la myrtille, pour comparer. Quand l'ours revient, {A} dort dans le bac à cornets, très {a:bien informé|bien informée}.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Greed, 1), (AiAxis.Energy, -1))),
                Try("serve", "Servir les clients, comme un vrai glacier", new EventChallenge(PlynlingStat.Stewardship, 4),
                    "{A} sert onze cornets, rend la monnaie juste et ajoute un sourire gratuit. L'ours revient au bout d'une heure, compte la caisse, et offre à {A} une boule de chaque parfum.",
                    "{A} empile trois boules sur le cornet du hérisson. La tour penche, penche, et atterrit sur le chapeau du hérisson. Le hérisson repart avec, très digne.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("trade_sense")), Nothing, Ai((AiAxis.Honor, 1), (AiAxis.Energy, 1)), Stress(("rowdy", 10))),
                Plain("sign", "Afficher « Revenez dans cinq minutes » et attendre",
                    "{A} écrit la pancarte et s'assoit devant. Les clients attendent cinq minutes, puis dix, puis repartent. En revenant, l'ours trouve la pancarte excellente, et la garde.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            }),

        new EventDef("baby_why", EventType.Pulse, Baby, "Pourquoi ?",
            "Depuis ce matin, {A} suit la chouette dans toute la bibliothèque. « Pourquoi le ciel est bleu ? Pourquoi les livres sentent la poussière ? Pourquoi tu dors le jour ? » La chouette n'a pas encore répondu à la première.",
            new[]
            {
                Try("more", "Continuer : encore un pourquoi, juste un", new EventChallenge(PlynlingStat.Learning, 4),
                    "La chouette finit par répondre, à tout, dans l'ordre. Au coucher du soleil, {A} sait pourquoi le ciel est bleu, et la chouette ne sait plus très bien pourquoi tout ça a commencé.",
                    "Les questions s'emmêlent. « Pourquoi pourquoi ? » La chouette ferme les deux yeux et fait semblant de dormir. Au bout d'un moment, pour de vrai.",
                    E(new GrowStat(PlynlingStat.Learning)), Nothing, Ai((AiAxis.Rationality, 2))),
                Plain("turn", "Laisser la chouette poser une question, pour changer",
                    "La chouette réfléchit, puis demande : « Pourquoi tu poses toutes ces questions ? » {A} réfléchit très longtemps. C'est la meilleure question de la journée.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("book", "Chercher les réponses {a:tout seul|toute seule} dans les livres",
                    "{A} ouvre le plus gros livre de la bibliothèque, page un. Le soir, {A} en est à la page trois, et a trouvé quarante nouvelles questions.",
                    E(new ApplyModifier("inspired")), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, -1)), gate: new TraitGate("pensive")),
            },
            WeightByTrait: new Dictionary<string, double> { ["curious"] = 2 }),

        new EventDef("baby_kite", EventType.Pulse, Baby, "Le cerf-volant rouge",
            "Dans la vitrine du marchand de jouets, un cerf-volant rouge attend. Le marchand, un vieux blaireau, propose un marché : « Aide-moi toute la semaine, et le cerf-volant est à toi. »",
            new[]
            {
                Plain("deal", "Accepter : une semaine, ce n'est rien",
                    "{A} serre la patte du blaireau. Première tâche : épousseter quatre cents billes. Une par une.",
                    E(new FollowUp("baby_kite_week", 96, 120)), Ai((AiAxis.Energy, 1), (AiAxis.Honor, 1))),
                Plain("save", "Économiser ses cailloux à la place",
                    "{A} met un caillou de côté chaque jour. Au bout d'une semaine, {A} a sept cailloux, et plus du tout envie d'un cerf-volant : maintenant, {A} veut un tambour.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
                Try("make", "Fabriquer son propre cerf-volant", new EventChallenge(PlynlingStat.Learning, 4),
                    "Deux brindilles, une feuille de rhubarbe, un fil de laine. Le cerf-volant de {A} monte moins haut que le rouge, mais monte. C'est déjà beaucoup.",
                    "Le cerf-volant de {A} reste au sol, obstinément. {A} le promène au bout de son fil, comme un chien très plat.",
                    E(new GrowStat(PlynlingStat.Learning)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("baby_kite_week", EventType.FollowUp, AnyStage, "La semaine du blaireau",
            "Une semaine a passé. Les billes brillent, la vitrine aussi, et le blaireau décroche le cerf-volant rouge.",
            new[]
            {
                Plain("fly", "Le faire voler sur la colline",
                    "Le cerf-volant rouge monte si haut que le héron lève la tête. {A} tient le fil à deux pattes, et le vent tire de l'autre côté. C'est un très bon match nul.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Energy, 1))),
                Plain("share", "Le prêter aux autres petits du village",
                    "{A} prête le cerf-volant à tour de rôle. Le soir, le cerf-volant est un peu déchiré, et {A} a neuf nouveaux amis.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 2))),
            }),

        // ---- wave 4: ado
        new EventDef("teen_dare", EventType.Pulse, Teen, "Le plus haut des chênes",
            "{B} a grimpé tout en haut du grand chêne et fait de grands signes à {A}. « Monte ! D'ici, on voit jusqu'à la gare ! » D'en bas, la gare a l'air très loin, et le chêne très haut.",
            new[]
            {
                Try("climb", "Grimper rejoindre {B}", new EventChallenge(PlynlingStat.Courage, 6),
                    "{A} grimpe, branche après branche, sans regarder en bas. En haut, on voit la gare, le train de 16 h 02, et le hérisson qui fait signe, avec des jumelles.",
                    "À mi-hauteur, {A} décide que la vue est très belle aussi d'ici. {B} redescend tenir compagnie à {A}, sur la branche du milieu.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(10)), E(new AffinityShift(5)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Plain("lawn", "Répondre que la vue d'en bas est meilleure",
                    "{A} s'allonge dans l'herbe, pattes derrière la tête. « La vue est parfaite, d'ici. » {B} hésite, puis redescend vérifier. C'est vrai que la vue est parfaite.",
                    E(new ApplyModifier("soothed"), new AffinityShift(5)), Ai((AiAxis.Energy, -1), (AiAxis.Rationality, 1))),
                Plain("spot", "Rester en bas, pour rattraper {B} au cas où",
                    "{A} reste au pied du chêne, pattes tendues. {B} ne tombe pas. Mais {B} a vu {A} en bas, et redescend plus vite que prévu, pour dire merci.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(10)), Ai((AiAxis.Compassion, 2))),
            },
            Target: TargetKind.Known),

        new EventDef("teen_poetry", EventType.Pulse, Teen, "Le duel de poèmes",
            "Au café, le moineau a lancé un duel de poèmes. Premier inscrit : {B}. Deuxième inscrit, inscrit par {B} sans rien demander : {A}.",
            new[]
            {
                Try("verse", "Écrire le plus beau poème du café", new EventChallenge(PlynlingStat.Learning, 6, VsTarget: true),
                    "{A} lit quatre vers sur la rivière en hiver. Le café se tait. Le moineau essuie une larme, donne la victoire à {A}, et recopie le poème sur le mur.",
                    "{B} lit un poème sur la confiture qui rime de partout. Le café applaudit debout. {A} applaudit aussi, du bout des pattes.",
                    E(new GrowStat(PlynlingStat.Learning), new AffinityShift(-5)), E(new AffinityShift(-5)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Vengefulness, 1)), Stress(("shy", 20))),
                Try("roast", "Improviser un poème… sur {B}", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "« {B} rime avec rien, et c'est très bien ainsi. » Le café éclate de rire. {B} aussi, sans le vouloir.",
                    "Le poème tombe à plat. {B} réplique avec un poème sur {A}, qui, lui, rime.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(-5)), E(new AffinityShift(-10)),
                    Ai((AiAxis.Vengefulness, 2)), Stress(("compassionate", 20))),
                Plain("duet", "Proposer un poème à deux voix",
                    "{A} et {B} écrivent ensemble, un vers chacun. Le poème ne veut rien dire du tout, mais sonne très bien. Le moineau, perplexe, déclare un match nul.",
                    E(new AffinityShift(10)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            },
            Target: TargetKind.Hostile),

        new EventDef("teen_tarts", EventType.Pulse, Teen, "Les tartelettes envolées",
            "La tortue avait posé douze tartelettes sur le rebord de la fenêtre du café. Maintenant, sur le rebord : des miettes, et une petite trace de patte.",
            new[]
            {
                Try("trail", "Suivre la piste de miettes", new EventChallenge(PlynlingStat.Learning, 6),
                    "La piste traverse la salle, contourne le comptoir… et finit dans la poche du tablier de la tortue. La tortue regarde sa poche longuement. « Ah. Oui. Ça me revient. »",
                    "La piste mène à l'escargot, couvert de miettes, qui jure n'avoir rien vu. Faute de preuves, l'affaire est classée.",
                    E(new GrowStat(PlynlingStat.Learning)), Nothing, Ai((AiAxis.Rationality, 2))),
                Plain("accuse", "Accuser le premier qui passe",
                    "{A} accuse le héron, qui passait par là. Le héron, offensé, refuse de répondre, sur une patte puis sur l'autre. La tortue lui offre une tartelette, pour s'excuser à la place de {A}.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Vengefulness, 1), (AiAxis.Honor, -1)), Stress(("just", 20), ("honest", 10))),
                Plain("bake", "Proposer à la tortue d'en refaire, ensemble",
                    "{A} et la tortue refont douze tartelettes, et cette fois {A} monte la garde sur le rebord. Le soir, le compte est presque bon : onze tartelettes, et {A}, de la confiture sur le museau.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Compassion, 1), (AiAxis.Energy, 1))),
            }),

        // ---- wave 4: adulte et ancien
        new EventDef("grown_teahouse", EventType.Pulse, Grown, "La maison de thé sous le lierre",
            "À l'orée du bois, une vieille maison de thé dort sous le lierre. La porte tient par habitude. À l'intérieur, une théière attend, comme si quelqu'un allait revenir.",
            new[]
            {
                Try("restore", "La remettre en état, planche par planche", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} passe trois semaines à poncer, clouer et chasser les araignées, poliment. La maison de thé brille. Même le lierre a l'air d'avoir pris un bain.",
                    "{A} répare le toit, puis le toit répare autre chose en tombant dans le salon. La maison sera prête, mais pas tout de suite.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_teahouse_opening", 48, 96)), E(new FollowUp("grown_teahouse_opening", 96, 144)),
                    Ai((AiAxis.Energy, 2)), Stress(("lazy", 20))),
                Plain("quick", "Juste un coup de balai et des coussins",
                    "{A} balaie, pose des coussins sur tout ce qui ressemble à une chaise, et accroche une lanterne. Ce n'est pas parfait. C'est très accueillant.",
                    E(new FollowUp("grown_teahouse_opening", 24, 72)), Ai((AiAxis.Rationality, 1))),
                Plain("leave", "La laisser à son lierre et à sa théière",
                    "{A} referme la porte doucement. Certaines maisons préfèrent attendre quelqu'un d'autre. En partant, {A} entend la théière siffler, toute seule, une fois.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            }),

        new EventDef("grown_teahouse_opening", EventType.FollowUp, AnyStage, "Le soir de l'ouverture",
            "La maison de thé ouvre ce soir. Le village arrive par petits groupes, curieux. Reste à choisir ce qui fera la soirée.",
            new[]
            {
                Plain("music", "Demander au moineau de chanter",
                    "Le moineau chante jusqu'à minuit, des chansons que personne ne connaît et que tout le monde fredonne en rentrant. La maison de thé a trouvé sa voix.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Sociability, 1))),
                Plain("stories", "Inviter la cigogne à raconter ses voyages",
                    "La cigogne revient exprès. Les histoires sont encore plus fausses que la dernière fois, et encore meilleures. On refuse du monde.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Zeal, 1))),
                Try("quiet", "Une soirée silencieuse : thé, lanternes, rien d'autre", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Le village boit son thé à la lueur des lanternes. Personne ne parle pendant deux heures. En partant, tout le monde dit merci, tout bas.",
                    "Le silence tient onze minutes, jusqu'à ce que le hérisson éternue. Ensuite, impossible d'arrêter de rire. C'est une très bonne soirée quand même.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("soothed")), E(new ApplyModifier("light_heart")),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, -1))),
            }),

        new EventDef("grown_surprise", EventType.Pulse, Grown, "Des chuchotements dans le noir",
            "{A} rentre à la nuit tombée. Derrière la porte : des chuchotements, un « chut ! », puis quelqu'un qui marche sur quelqu'un d'autre.",
            new[]
            {
                Try("window", "S'enfuir par la fenêtre, sans bruit", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} file par la fenêtre, fait le tour du village et revient par la porte de derrière, pile au moment où tout le monde crie « Surprise ! » vers la porte de devant. Personne ne comprend comment {A} est {a:arrivé|arrivée} derrière.",
                    "{A} reste {a:coincé|coincée} dans la fenêtre, à moitié dehors. C'est là que tout le monde allume les lanternes en criant « Surprise ! ». De l'avis général, la meilleure surprise de l'année.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_surprise_cake", 1, 3), new ApplyModifier("sly")), E(new FollowUp("grown_surprise_cake", 1, 3)),
                    Ai((AiAxis.Boldness, -1), (AiAxis.Rationality, 1))),
                Try("door", "Ouvrir la porte d'un coup, {a:prêt|prête} à tout", new EventChallenge(PlynlingStat.Courage, 8),
                    "« Surprise ! » crie tout le village, gâteaux à la patte. {A} fait semblant de ne s'être douté de rien. Personne n'est dupe, et c'est très bien comme ça.",
                    "{A} ouvre d'un coup, en criant plus fort que tout le monde. Le hérisson lâche le gâteau. Le gâteau survit, presque entier.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_surprise_cake", 1, 3)), E(new FollowUp("grown_surprise_cake", 1, 3)),
                    Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Plain("knock", "Frapper chez soi, poliment",
                    "{A} frappe à sa propre porte. Silence. Puis une petite voix : « Ne rentre pas tout de suite ! » {A} attend dehors, très {a:patient|patiente}, jusqu'au « Surprise ! ».",
                    E(new FollowUp("grown_surprise_cake", 1, 3)), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, 1))),
            }),

        new EventDef("grown_surprise_cake", EventType.FollowUp, AnyStage, "Trois gâteaux",
            "La fête bat son plein. Sur la table, trois gâteaux faits par trois villageois : un énorme, un moyen, un tout petit. Tout le monde regarde {A}.",
            new[]
            {
                Plain("big", "Le plus gros, évidemment",
                    "{A} prend le plus gros. L'ours, qui l'a fait, rayonne. {A} met trois jours à le finir, et ne regrette rien.",
                    E(new ApplyModifier("hearty")), Ai((AiAxis.Greed, 2)), Stress(("temperate", 20))),
                Plain("medium", "Le moyen, pour ne vexer personne",
                    "{A} prend le moyen. Le gros et le petit se regardent, vexés tous les deux. La tortue, qui a fait le moyen, sourit très lentement.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Rationality, 1))),
                Plain("small", "Le tout petit : les meilleures choses…",
                    "Le tout petit gâteau est l'œuvre de l'escargot, qui y a passé la semaine. C'est le meilleur gâteau du monde, en une seule bouchée.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 1), (AiAxis.Greed, -1)), Stress(("gluttonous", 20))),
            }),

        new EventDef("grown_riddles", EventType.Pulse, Grown, "La nuit des devinettes",
            "Une fois par an, la chouette organise la nuit des devinettes. La chouette n'a jamais perdu. Ce soir, la chouette regarde {A} avec un intérêt inquiétant.",
            new[]
            {
                Try("answer", "Répondre à la devinette de la chouette", new EventChallenge(PlynlingStat.Learning, 8),
                    "« Qu'est-ce qui a des feuilles mais pas de branches ? » « Un livre. » La chouette cligne d'un œil, puis de l'autre. Pour la première fois, la nuit des devinettes a un vainqueur qui n'est pas la chouette.",
                    "{A} répond « un chou ». La réponse est fausse, mais la chouette la note dans un carnet, pour l'an prochain.",
                    E(new GrowStat(PlynlingStat.Learning)), Nothing, Ai((AiAxis.Rationality, 2))),
                Try("ask", "Poser une devinette à la chouette", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} demande ce qui est plus grand que la chouette et plus léger qu'une plume. La chouette réfléchit jusqu'à l'aube. La réponse : son ombre. La chouette rit, pour la première fois en public.",
                    "La chouette répond avant que {A} ait fini la question, puis donne trois autres bonnes réponses, au cas où.",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("sly")), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1)), Stress(("shy", 20))),
                Plain("watch", "Regarder les autres jouer, avec un chocolat chaud",
                    "{A} regarde le hérisson répondre « un train » à toutes les devinettes. Deux fois, c'est la bonne réponse.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1))),
            }),

        new EventDef("grown_fishing_rival", EventType.Pulse, Grown, "Ça mord ?",
            "{A} pêche tranquillement au bord de l'étang. {B} arrive, s'assoit juste à côté et lance sa ligne exactement au même endroit. « Ça mord ? »",
            new[]
            {
                Try("contest", "Le plus gros poisson aura raison", new EventChallenge(PlynlingStat.Stewardship, 8, VsTarget: true),
                    "{A} sort une carpe magnifique. {B} sort une botte. Dans la botte, un tout petit poisson, qui regarde {A} avec reproche. {A} relâche les deux.",
                    "{B} sort une carpe magnifique et la montre à {A}, longtemps, de très près. {A} sort une feuille morte, très jolie aussi.",
                    E(new GrowStat(PlynlingStat.Stewardship), new AffinityShift(-5)), E(new AffinityShift(-10)), Ai((AiAxis.Vengefulness, 1), (AiAxis.Boldness, 1))),
                Plain("move", "Changer de place, sans un mot",
                    "{A} se déplace de trois pas. {B} aussi. Puis encore trois. Le soir venu, {A} et {B} ont fait le tour de l'étang ensemble, sans un poisson et sans un mot.",
                    E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
                Plain("share", "Partager son appât",
                    "{A} tend la boîte d'appâts. {B} hésite, se sert et marmonne un merci. Ça mord enfin, des deux côtés.",
                    E(new AffinityShift(10)), Ai((AiAxis.Compassion, 2), (AiAxis.Vengefulness, -1)), Stress(("vengeful", 20))),
            },
            Target: TargetKind.Hostile),

        new EventDef("grown_overheard_gift", EventType.Pulse, Grown, "Ce qu'on dit du pull",
            "Au marché, derrière un étal, {A} entend la voix de {B} : « Le pull tricoté par {A} ? Une horreur. Je le mets pour faire plaisir. » {B} ne sait pas que {A} est juste là.",
            new[]
            {
                Plain("laugh", "Éclater de rire et se montrer",
                    "{A} surgit en riant : « Une horreur, hein ? » {B} devient rouge comme une baie, puis rit aussi. Depuis, le pull a un surnom affectueux.",
                    E(new AffinityShift(10)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1)), Stress(("wrathful", 20))),
                Try("ask", "Demander des explications", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{B} avoue tout : le pull gratte, les manches traînent par terre, mais {B} l'aime quand même, parce que c'est {A} qui l'a fait. {A} propose de le retricoter. {B} refuse : le pull est parfait comme ça.",
                    "{B} se défend mal, {A} insiste trop. Le pull devient une affaire d'état jusqu'au soir.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(10)), E(new AffinityShift(-10)), Ai((AiAxis.Honor, 1), (AiAxis.Vengefulness, 1)), Stress(("shy", 20))),
                Plain("silent", "Faire comme si de rien n'était",
                    "{A} s'éloigne sans bruit. Le lendemain, {B} porte le pull, avec un grand sourire. {A} sourit aussi. Chacun sait quelque chose, et ce n'est pas la même chose.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_secret_note", EventType.Pulse, Grown, "Un mot sans signature",
            "Dans la poche de {A}, un petit papier plié en quatre : « Ta façon de saluer le héron, le matin, illumine ma journée. » Pas de signature. Juste une tache de confiture.",
            new[]
            {
                Try("investigate", "Mener l'enquête", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} compare l'écriture avec celle de la moitié du village. Aucun doute : c'est celle de {B}. {A} garde la découverte pour soi, pour l'instant.",
                    "{A} soupçonne tout le monde, puis le héron, puis personne. L'enquête s'arrête, faute de suspects.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_note_reveal", 24, 72)), E(new FollowUp("grown_note_reveal", 24, 72)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
                Plain("reply", "Écrire une réponse et la remettre dans sa poche",
                    "{A} écrit « Merci. Toi aussi, sûrement. » et glisse le papier dans sa poche, côté ouvert. Le lendemain, le papier a disparu.",
                    E(new FollowUp("grown_note_reveal", 24, 72)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("keep", "Garder le mot, et saluer le héron encore mieux",
                    "{A} garde le mot dans sa poche. Le lendemain, {A} salue le héron avec une telle conviction que le héron en tombe presque de sa patte.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Anyone,
            TargetCondition: t => t.Affinity >= 0),

        new EventDef("grown_note_reveal", EventType.FollowUp, AnyStage, "La signature",
            "Au café, {B} renverse sa tasse en voyant {A}, puis se cache derrière le menu. Sur le menu, une petite tache de confiture.",
            new[]
            {
                Plain("thank", "Remercier {B} pour le mot",
                    "{A} s'assoit en face de {B} : « Merci pour le mot. » {B} sort de derrière le menu, rouge jusqu'aux oreilles. Le reste du café fait semblant de lire le menu aussi.",
                    E(new AffinityShift(15)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("heron", "Saluer {B} exactement comme le héron, le matin",
                    "{A} se met sur une patte et salue {B} comme on salue le héron. {B} éclate de rire, et renverse une deuxième tasse.",
                    E(new AffinityShift(10)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("pretend", "Faire comme si de rien n'était",
                    "{A} commande un chocolat chaud et ne dit rien. {B} sort de derrière le menu, {b:soulagé|soulagée}, et un peu {b:déçu|déçue} aussi.",
                    E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("elder_old_toy", EventType.Pulse, Elder, "La toupie du grenier",
            "En rangeant le grenier, {A} retrouve une toupie en bois, toute rayée. Autrefois, cette toupie tournait mieux que toutes les autres.",
            new[]
            {
                Try("spin", "La faire tourner, pour voir", new EventChallenge(PlynlingStat.Learning, 6),
                    "{A} enroule la ficelle et tire d'un coup sec. La toupie tourne, tourne, et chante le même petit bruit qu'autrefois. {A} la regarde jusqu'au dernier tour.",
                    "La ficelle glisse. La toupie roule sous l'armoire, comme autrefois aussi. Certaines choses ne changent pas.",
                    E(new ApplyModifier("light_heart")), E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, 1))),
                Plain("give", "La donner au plus petit du village",
                    "{A} offre la toupie à un petit qui n'en a jamais vu. Le petit la fait tourner du premier coup. {A} fait semblant d'être {a:vexé|vexée}, et ne l'est pas du tout.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 2))),
                Plain("keep", "La remettre dans sa boîte, bien au chaud",
                    "{A} enveloppe la toupie dans un mouchoir et la range. Certaines choses n'ont plus besoin de tourner. Savoir qu'une toupie attend au grenier, ça suffit.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
            }),
    };
}
