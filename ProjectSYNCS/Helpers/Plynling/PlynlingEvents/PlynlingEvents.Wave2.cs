using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 2.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave2() => new[]
    {
        // ---- wave 2: bébé (a bébé holds only its childhood trait: gates and costs use those)
        new EventDef("baby_pebble", EventType.Pulse, Baby, "Le caillou de compagnie",
            "{A} shoote dans un caillou. Le caillou roule, s'arrête, et a l'air d'attendre la suite. C'est décidé : c'est un ami. Petit détail : au village, un caillou, c'est de l'argent.",
            new[]
            {
                Plain("name", "Lui donner un nom et l'emmener partout",
                    "{A} l'appelle Monsieur Rond. Monsieur Rond visite le marché, la gare et le fond d'une poche. Au café, {A} paie par erreur avec Monsieur Rond. La tortue le reconnaît, et le rend, par respect.",
                    E(new ApplyModifier("light_heart"), new FollowUp("baby_pebble_lost", 48, 96)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("spend", "Le dépenser : c'est quand même un caillou",
                    "{A} pose le caillou sur le comptoir et obtient une cuillère de miel. Le caillou rejoint les autres dans la caisse. {A} lui fait au revoir de la patte, un peu longtemps.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
                Try("hide", "Le cacher pour que personne ne le dépense", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} cache le caillou sous l'oreiller et vérifie toute la nuit. Le caillou est toujours là. Le caillou est toujours là. Le caillou est toujours là.",
                    "{A} le cache dans la poche du manteau, et le manteau part au lavage. Le caillou ressort très propre, et un peu vexé.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("baby_pebble_lost", 48, 96)), Nothing, Ai((AiAxis.Greed, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("baby_pebble_lost", EventType.FollowUp, AnyStage, "Le caillou disparu",
            "Ce matin, la poche de {A} est vide. Au café, la caisse de la tortue contient quatre cent douze cailloux. L'un d'eux a l'air un peu rond.",
            new[]
            {
                Try("grab", "Le reprendre en douce dans la caisse", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} glisse une patte dans la caisse pendant que la tortue se retourne — un bon quart d'heure — et ressort le bon caillou du premier coup.",
                    "{A} tire le mauvais caillou. Puis un autre. La tortue finit de se retourner et tend la patte, sans un mot.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -1), (AiAxis.Boldness, 1)), Stress(("bossy", 10))),
                Plain("trade", "Proposer un échange à la tortue",
                    "{A} propose trois glands et un dessin. La tortue rend le caillou, garde le dessin, et garde aussi les glands, « pour les frais de garde ».",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1), (AiAxis.Honor, 1))),
                Plain("free", "Le laisser vivre sa vie de caillou",
                    "{A} décide que le caillou est parti voir du pays. Depuis, chaque fois que la tortue rend la monnaie, {A} vérifie quand même.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("baby_night_noise", EventType.Pulse, Baby, "Gratte, gratte",
            "En pleine nuit, quelque chose gratte sous la fenêtre de {A}. Gratte, gratte. Puis ça tousse, poliment.",
            new[]
            {
                Try("look", "Aller voir, la couverture sur la tête", new EventChallenge(PlynlingStat.Courage, 4),
                    "C'est le hérisson chef de gare, lanterne à la patte, qui fait sa ronde. « Tout est à l'heure », annonce le hérisson, avant de repartir vérifier la lune.",
                    "{A} se prend les pattes dans la couverture et roule jusqu'à la porte. Le bruit s'arrête net. Le lendemain, le hérisson demande si quelqu'un a entendu un drôle de bruit cette nuit.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing, Ai((AiAxis.Boldness, 2))),
                Plain("hide", "Se cacher sous la couverture jusqu'au matin",
                    "{A} attend sous la couverture, les yeux grands ouverts. Au matin, sous la fenêtre : des traces de petites pattes, et un ticket de train poinçonné.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Boldness, -1), (AiAxis.Rationality, 1))),
                Plain("note", "Noter chaque bruit dans un carnet",
                    "{A} note tout : gratte-gratte à minuit, toux polie à minuit deux, re-gratte à minuit cinq. Le carnet devient le premier horaire jamais écrit par {A}. Le hérisson, ému, le recopie.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 2)), gate: new TraitGate("curious")),
            }),

        new EventDef("baby_sideline", EventType.Pulse, Baby, "Sur le bord du terrain",
            "Sur la place, une partie de balle bat son plein. Sur le côté, {B} regarde, {b:assis|assise} dans l'herbe, en faisant semblant de ne pas regarder.",
            new[]
            {
                Try("invite", "Inviter {B} dans son équipe", new EventChallenge(PlynlingStat.Diplomacy, 4),
                    "{B} hésite, puis se lève. Première passe : ratée. Deuxième passe : ratée, mais mieux. L'équipe de {A} perd, et personne ne s'en souvient.",
                    "{B} secoue la tête et serre ses genoux plus fort. {A} retourne jouer, en regardant souvent sur le côté.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(15)), E(new AffinityShift(5)),
                    Ai((AiAxis.Sociability, 2), (AiAxis.Compassion, 1)), Stress(("bossy", 10))),
                Plain("sit", "S'asseoir à côté de {B}, sans rien dire",
                    "{A} s'assoit à côté de {B}. Ensemble, {A} et {B} regardent la balle aller, venir, et finir dans la rivière. De loin le meilleur moment du match.",
                    E(new AffinityShift(10)), Ai((AiAxis.Compassion, 2), (AiAxis.Energy, -1)), Stress(("rowdy", 10))),
                Plain("play", "Retourner jouer : la balle n'attend pas",
                    "{A} marque deux buts et en encaisse trois. En rentrant, {A} décide de jouer tous les jours, pour toujours.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Energy, 2))),
            },
            Target: TargetKind.Anyone),

        // ---- wave 2: ado
        new EventDef("teen_ballgame", EventType.Pulse, Teen, "La grande partie de balle",
            "Une fois l'an, tout le village joue à la balle d'un bout à l'autre du village, avec une courge séchée. Les règles tiennent en une ligne, et personne ne se souvient de la ligne.",
            new[]
            {
                Plain("join", "Plonger dans la mêlée",
                    "{A} plonge dans la mêlée. Quelque part, quelqu'un crie « pas les oreilles ! ». La partie ne fait que commencer.",
                    E(new FollowUp("teen_ballgame_middle", 2, 6)), Ai((AiAxis.Boldness, 2), (AiAxis.Energy, 1)), Stress(("craven", 20))),
                Plain("pies", "Tenir la buvette de tartes avec l'ours",
                    "{A} vend des tartes aux joueurs qui passent, puis aux joueurs qui repassent. L'ours compte la recette : tout le monde a gagné quelque chose, sauf les tartes.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
                Try("referee", "Aider le moineau à arbitrer", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "{A} siffle trois fautes, deux hors-jeu et un câlin non réglementaire. Le moineau, ravi, promet à {A} un sifflet à sa taille pour l'an prochain.",
                    "Personne n'écoute, pas même le moineau. {A} finit la journée {a:perché|perchée} sur une barrière, à siffler pour le principe.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Honor, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("teen_ballgame_middle", EventType.FollowUp, AnyStage, "Au cœur de la mêlée",
            "La courge arrive droit sur {A}. Entre {A} et le but — le puits du village — se dressent trois blaireaux, la pie et un tas de foin.",
            new[]
            {
                Try("charge", "Foncer tout droit", new EventChallenge(PlynlingStat.Courage, 6),
                    "{A} passe entre les blaireaux, sous la pie et à travers le foin. La courge touche le puits. Le village entier hurle, sans savoir encore pour quelle équipe.",
                    "{A} rebondit sur le premier blaireau comme sur un édredon. Le blaireau s'excuse. La courge est déjà loin.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("teen_ballgame_end", 2, 6)), E(new FollowUp("teen_ballgame_end", 2, 6)),
                    Ai((AiAxis.Boldness, 2))),
                Try("sweater", "Cacher la courge sous son pull", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} marche jusqu'au puits en sifflotant, avec un très gros ventre. Personne ne soupçonne rien. Le moineau consulte le règlement : rien ne l'interdit.",
                    "La courge glisse du pull au pire moment, juste devant l'ours. L'ours sourit et confisque la courge contre une tarte.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("teen_ballgame_end", 2, 6)), E(new FollowUp("teen_ballgame_end", 2, 6)),
                    Ai((AiAxis.Honor, -1), (AiAxis.Boldness, 1)), Stress(("honest", 20))),
                Plain("pass", "Passer la courge au plus petit joueur",
                    "{A} passe la courge à un mulot haut comme une pomme. Le mulot court, court, et personne n'ose le plaquer. Le plus beau but de la journée.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("teen_ballgame_end", 2, 6)), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("teen_ballgame_end", EventType.FollowUp, AnyStage, "Le coup de sifflet final",
            "Le soleil se couche. Les joueurs sont couverts de boue, de foin et de tarte. Le moineau grimpe sur un tonneau pour annoncer le score.",
            new[]
            {
                Plain("cheer", "Applaudir tout le monde",
                    "Onze à onze, ou douze à neuf, selon qui compte. Tout le monde applaudit tout le monde. Le moineau garde la courge pour l'an prochain.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Sociability, 1))),
                Try("recount", "Contester le score, pour le principe", new EventChallenge(PlynlingStat.Diplomacy, 7),
                    "{A} rejoue le match avec des glands sur une table du café. Le moineau, convaincu, accorde un point de plus. Le gland qui jouait la courge est mangé dans la foulée.",
                    "{A} rejoue le match avec des glands. À la fin, les glands ont perdu aussi.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Vengefulness, 1), (AiAxis.Rationality, 1)), Stress(("humble", 10))),
                Plain("memento", "Garder un morceau de courge en souvenir",
                    "{A} pose le morceau de courge sur l'étagère. Chaque fois que {A} le regarde, ça sent un peu la tarte, et beaucoup la victoire.",
                    E(new ApplyModifier("fired_up")), Ai((AiAxis.Zeal, 1))),
            }),

        new EventDef("teen_mudcastle", EventType.Pulse, Teen, "Le château de boue",
            "Sur la berge, {A} a bâti un château de boue : des tours, des douves, un pont-levis en brindilles. La rivière monte. La rivière a l'air de le faire exprès.",
            new[]
            {
                Plain("bribe", "Planter un panneau : « Château privé »",
                    "{A} plante un panneau sur la berge : « Château privé. Défense de monter. » La rivière monte quand même : les rivières ne savent pas lire. Ça valait le coup d'essayer.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("teen_mudcastle_morning", 12, 24)), Ai((AiAxis.Greed, -1), (AiAxis.Zeal, 1))),
                Try("walls", "Renforcer les murs, vite", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "{A} empile, tasse, lisse. Quand l'eau arrive, les murs tiennent. Le château passe la nuit, fier comme un vrai.",
                    "{A} empile trop vite. La tour nord s'affaisse doucement dans les douves, avec beaucoup de dignité.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("teen_mudcastle_morning", 12, 24)), E(new FollowUp("teen_mudcastle_morning", 12, 24)),
                    Ai((AiAxis.Energy, 2), (AiAxis.Rationality, 1)), Stress(("lazy", 20))),
                Plain("shrine", "Monter la garde, {a:convaincu|convaincue} que le château tiendra",
                    "{A} s'assoit devant le pont-levis, les bras croisés, et ne bouge plus. L'eau monte jusqu'aux orteils de {A}, hésite, et contourne. Au matin, la tour du pont-levis est encore debout. Le reste du château, beaucoup moins.",
                    E(new ApplyModifier("inspired"), new FollowUp("teen_mudcastle_morning", 12, 24)), Ai((AiAxis.Zeal, 2)), gate: new TraitGate("zealous")),
            }),

        new EventDef("teen_mudcastle_morning", EventType.FollowUp, AnyStage, "Le lendemain sur la berge",
            "Au matin, {A} retourne voir le château. La rivière est redescendue, l'air innocent, comme si la nuit n'avait jamais existé.",
            new[]
            {
                Plain("concede", "Saluer la rivière : bien jouée, pour cette fois",
                    "{A} salue la rivière d'un hochement de tête. « Tu as gagné cette manche, rivière. » La rivière clapote, bonne joueuse.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Honor, 1)), Stress(("vengeful", 10))),
                Plain("rebuild", "Reconstruire, plus haut et plus loin",
                    "{A} reconstruit trois pas plus loin. Le nouveau château a une vue magnifique sur la rivière, et la rivière une vue magnifique sur le château.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("teen_unlucky", EventType.Pulse, Teen, "L'escargot vexé",
            "Depuis ce matin, {B} évite {A}. La raison : {B} en est {b:sûr|sûre}, l'escargot du village comprend tout ce qu'on dit, et l'escargot boude depuis que {A} l'a traité de « lent », mardi, à voix haute. Selon {B}, être vu avec {A}, c'est se fâcher avec l'escargot.",
            new[]
            {
                Plain("ritual", "Jouer le jeu : aller s'excuser auprès de l'escargot",
                    "{A} se présente devant l'escargot avec une feuille de laitue et des excuses en trois points. L'escargot écoute jusqu'au bout, mange la laitue, et repart. {B} respire mieux : c'est clairement un pardon.",
                    E(new AffinityShift(10)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Try("prove", "Prouver à {B} que l'escargot n'a rien entendu", new EventChallenge(PlynlingStat.Learning, 6),
                    "{A} suit l'escargot pendant une semaine, carnet en main : manger, dormir, manger, traverser la place. Pas une seule bouderie. {B} lit tout, longtemps, et referme le carnet. « Bon. Mais je lui dirai quand même bonjour, par politesse. »",
                    "{B} lit le carnet et conclut que l'escargot boude aussi le carnet. Le carnet est enterré au fond du jardin, avec les honneurs.",
                    E(new GrowStat(PlynlingStat.Learning), new AffinityShift(5)), Nothing, Ai((AiAxis.Rationality, 2)), Stress(("zealous", 20))),
                Try("tease", "Faire croire à {B} que l'escargot a tout répété à la tortue", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} glisse à {B}, l'air grave, que l'escargot a tout raconté à la tortue. Le lendemain, {B} traverse le café en s'excusant auprès de chaque table, au cas où. La tortue n'y comprend rien, mais accepte toutes les excuses.",
                    "{B} flaire la blague et en invente une meilleure : l'escargot cacherait les chaussettes des farceurs. Le soir même, {A} ne retrouve plus ses chaussettes.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(-5)), Nothing, Ai((AiAxis.Honor, -1), (AiAxis.Boldness, 1)), Stress(("honest", 20))),
            },
            Target: TargetKind.Known),

        // ---- wave 2: adulte et ancien
        new EventDef("grown_smell", EventType.Pulse, Grown, "Une odeur, peut-être",
            "Au café, l'escargot s'approche de {A}, s'arrête à une distance très étudiée, et dit, avec une immense délicatesse : « Ça sent… le voyage, par ici. »",
            new[]
            {
                Plain("bath", "Filer prendre un bain",
                    "{A} file au bain et frotte jusqu'aux oreilles. En repassant au café, {A} sent la savonnette. L'escargot hoche la tête, soulagé, sans un mot.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1)), Stress(("arrogant", 20))),
                Plain("quest", "Partir en quête d'un parfum digne de ce nom",
                    "{A} décide qu'un bain ne suffira pas. Ce que {A} mérite, c'est un parfum. Première étape : le marché.",
                    E(new FollowUp("grown_smell_market", 6, 18)), Ai((AiAxis.Zeal, 1), (AiAxis.Energy, 1))),
                Plain("deny", "Répondre que ça ne sent rien",
                    "« Ça ne sent rien », répond {A}. L'escargot recule d'un pas, puis de deux, pour mieux vérifier. Dans le café, tout le monde respire par la bouche, par politesse.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, -1)), Stress(("honest", 10))),
            },
            // Below half: it can come before the card turns dirty (PlynlingLife.DirtyBelow).
            Condition: c => c.Self.Hygiene < 0.5),

        new EventDef("grown_smell_market", EventType.FollowUp, AnyStage, "La chasse au parfum",
            "Au marché, chaque étal sent quelque chose. {A} a un flacon vide, un nez sérieux, et la pie qui suit à distance, très intéressée.",
            new[]
            {
                Plain("mint", "De la menthe, pour la fraîcheur",
                    "{A} cueille la menthe du jardin de la tortue, avec sa permission, puis sans, puis de nouveau avec. Le flacon sent le matin.",
                    E(new FollowUp("grown_smell_bottle", 12, 24)), Ai((AiAxis.Energy, 1))),
                Plain("honey", "Du miel, pour la douceur",
                    "Le marchand de miel offre une goutte « pour la science ». Le flacon sent le dimanche.",
                    E(new FollowUp("grown_smell_bottle", 12, 24)), Ai((AiAxis.Sociability, 1))),
                Try("moss", "De la mousse du vieux pont, pour le mystère", new EventChallenge(PlynlingStat.Learning, 7),
                    "{A} gratte un peu de mousse sous le vieux pont. Le héron fait semblant de ne rien voir, puis de ne rien sentir. Le flacon sent la pluie qui arrive.",
                    "{A} gratte la mauvaise mousse. Le flacon sent le vieux pont. Depuis, curieusement, le vieux pont sent très bon.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_smell_bottle", 12, 24)), E(new FollowUp("grown_smell_bottle", 12, 24)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("grown_smell_bottle", EventType.FollowUp, AnyStage, "Le flacon",
            "Le flacon est prêt. {A} met une goutte derrière chaque oreille, puis une troisième, au cas où, et retourne au café.",
            new[]
            {
                Plain("quiet", "Entrer discrètement et s'asseoir au fond",
                    "{A} s'assoit au fond. L'escargot lève la tête, renifle, et fait un tout petit signe d'approbation : son plus beau compliment depuis des années.",
                    E(new ApplyModifier("fragrant")), Ai((AiAxis.Honor, 1))),
                Try("show", "Faire le tour de la salle pour que tout le monde sente", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Toute la salle veut une goutte. La tortue commande un flacon pour ses dimanches. {A} vient d'inventer un métier.",
                    "Trois gouttes, c'était deux de trop. Les clients ouvrent les fenêtres, poliment. L'escargot rentre dans sa coquille.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("fragrant")), Nothing, Ai((AiAxis.Sociability, 2)), Stress(("shy", 20))),
            }),

        new EventDef("grown_matchmaker", EventType.Pulse, Grown, "Le hérisson est amoureux",
            "Chaque matin, le hérisson chef de gare commande un café, ne le boit pas, et regarde la tortue essuyer le comptoir. Ça dure depuis onze ans.",
            new[]
            {
                Try("note", "Glisser un mot doux de la part du hérisson", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} glisse un mot sous la tasse de la tortue : « Le train de 7 h 14 passe pour vous. » La tortue lit le mot, très lentement, puis sourit, encore plus lentement.",
                    "Le mot atterrit sous la tasse du héron. Le héron le lit, rougit jusqu'au bout du bec, et ne remet plus les pattes au café pendant trois jours.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_matchmaker_date", 48, 96)), Nothing,
                    Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1)), Stress(("shy", 20))),
                Plain("coach", "Entraîner le hérisson à parler lui-même",
                    "{A} fait répéter le hérisson toute une soirée : « Bonjour, votre café est délicieux. » Le lendemain, le hérisson se lance : « Bonjour, votre délicieux est café. » La tortue rit. C'est un début.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_matchmaker_date", 48, 96)), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, 1))),
                Plain("wait", "Ne pas s'en mêler",
                    "{A} ne s'en mêle pas. Le hérisson commande son café, ne le boit pas. Certaines histoires prennent leur temps, surtout avec une tortue.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1)), Stress(("impatient", 10))),
            }),

        new EventDef("grown_matchmaker_date", EventType.FollowUp, AnyStage, "Le dernier train",
            "Le hérisson a invité la tortue à regarder passer le dernier train. Le hérisson a demandé à {A} de tenir la lanterne, « juste au cas où ».",
            new[]
            {
                Plain("lantern", "Tenir la lanterne, et regarder ailleurs",
                    "Le dernier train passe. Le hérisson ne regarde pas le train. La tortue non plus. {A} tient la lanterne jusqu'au bout, en regardant très fort les étoiles.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 1))),
                Try("delay", "Retarder le train de cinq minutes", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} convainc le conducteur de faire une pause « pour les grenouilles ». Cinq minutes de plus sur le quai. Pour la première fois de sa vie, le hérisson ne regarde pas l'heure.",
                    "Le train part à l'heure pile. Rassuré par tant de ponctualité, le hérisson prend la patte de la tortue.",
                    E(new GrowStat(PlynlingStat.Intrigue)), E(new ApplyModifier("light_heart")), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
            }),

        new EventDef("grown_bad_dish", EventType.Pulse, Grown, "Le plat mijoté",
            "Toute la semaine, {B} a mijoté un plat « rien que pour {A} ». Le plat est vert. Le plat fait un petit bruit. {B} attend, les yeux brillants.",
            new[]
            {
                Plain("lie", "Dire que c'est délicieux, et finir l'assiette",
                    "{A} mange tout, en souriant. {B}, {b:ravi|ravie}, ressert aussitôt, et note la recette pour la semaine prochaine.",
                    E(new AffinityShift(15)), Ai((AiAxis.Compassion, 2), (AiAxis.Honor, -1)), Stress(("honest", 30))),
                Try("truth", "Dire la vérité, avec beaucoup de douceur", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "« C'est très… vert. » {B} goûte enfin son propre plat, devient vert aussi, et éclate de rire. La soirée finit au café, sur deux tartines de confiture.",
                    "{B} pose la cuillère, très {b:droit|droite}. « Je vois. » Le plat retourne en cuisine, et {B} aussi, pour un bon moment.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(10), new ApplyModifier("clear_conscience")), E(new AffinityShift(-10)), Ai((AiAxis.Honor, 2))),
                Try("plant", "Nourrir la plante verte, une bouchée à la fois", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "Bouchée par bouchée, la plante verte mange tout. {B} trouve l'assiette vide et en pleure presque de joie. Le lendemain, la plante a fleuri. Personne ne veut savoir de quoi.",
                    "La plante recrache la troisième bouchée. Sur la nappe. Devant {B}. Le silence est total, à part le petit bruit du plat.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(10)), E(new AffinityShift(-15)),
                    Ai((AiAxis.Honor, -1), (AiAxis.Boldness, 1)), Stress(("honest", 20))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_campfire", EventType.Pulse, Grown, "Autour du feu",
            "Le soir, au bord de la rivière, le village fait griller du miel sur des bâtons. {A} se retrouve à côté de {B}, qu'on ne connaît que de vue. Le miel de {B} vient de tomber dans le feu.",
            new[]
            {
                Plain("share", "Partager son bâton avec {B}",
                    "{A} tend son bâton. {B} croque, {A} croque, le miel coule partout. Ça colle aux moustaches, et ça colle un peu les gens ensemble aussi.",
                    E(new AffinityShift(15), new ApplyModifier("hearty")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Ask("ask", "Demander à {B} : « Tu veux qu'on soit amis ? »",
                    "{A} prend une grande inspiration et pose la question, comme ça, au-dessus du feu.", "reply_campfire",
                    Ai((AiAxis.Sociability, 2), (AiAxis.Boldness, 1))),
                Plain("sparks", "Regarder les étincelles monter, sans rien dire",
                    "Les étincelles montent, montent, et deviennent des étoiles. Ou alors c'étaient déjà des étoiles. {B} regarde aussi.",
                    E(new ApplyModifier("soothed"), new AffinityShift(5)), Ai((AiAxis.Energy, -1), (AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Anyone,
            TargetCondition: t => t.Bond == PlynlingBond.Acquaintances && t.Affinity >= 0),

        new EventDef("reply_campfire", EventType.Response, AnyStage, "Une question au-dessus du feu",
            "Entre deux bouchées de miel, {B} a demandé à {A} : « Tu veux qu'on soit amis ? »",
            new[]
            {
                Answer("yes", "Dire oui, la bouche pleine de miel",
                    "{A} dit oui, ou quelque chose d'approchant, la bouche pleine de miel. {B} comprend très bien.",
                    Stance.Accept, E(new SetAffinityAtLeast(PlynlingBonds.FriendsFrom + PlynlingBonds.BondMargin)), Ai((AiAxis.Sociability, 2))),
                Answer("later", "« Reviens au prochain feu, on verra. »",
                    "{A} sourit : « Reviens au prochain feu, on verra. » {B} promet d'apporter son propre miel, cette fois.",
                    Stance.Refuse, E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_stork_tales", EventType.Pulse, Grown, "Les histoires de la cigogne",
            "Une cigogne de passage s'est posée au café. Depuis midi, la cigogne raconte : la mer qui chante, la montagne qui éternue, le pays où les escargots courent. Personne n'a rien commandé depuis trois heures.",
            new[]
            {
                Plain("believe", "Tout croire, et demander la suite",
                    "{A} croit tout, même les escargots qui courent. La cigogne, touchée, raconte la suite jusqu'à la nuit, et promet d'envoyer une carte.",
                    E(new ApplyModifier("inspired"), new FollowUp("grown_stork_card", 72, 120)), Ai((AiAxis.Zeal, 1), (AiAxis.Sociability, 1)), Stress(("cynical", 20))),
                Try("doubt", "Poser des questions qui fâchent", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} demande la hauteur exacte de la montagne qui éternue. La cigogne hésite, puis avoue : la montagne n'éternue pas, la montagne tousse. Toute la salle respecte {A} pour cette précision.",
                    "La cigogne a réponse à tout, chiffres compris. {A} rentre {a:convaincu|convaincue} que les escargots courent, quelque part.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_stork_card", 72, 120)), E(new FollowUp("grown_stork_card", 72, 120)),
                    Ai((AiAxis.Rationality, 2))),
                Try("tell", "Raconter à son tour une histoire du village", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} raconte la grande partie de balle, la courge et le mulot. La cigogne écoute, bec ouvert, et note tout. Très loin d'ici, on parlera bientôt du village.",
                    "{A} raconte l'histoire du hérisson qui a raté un train, une seule fois, voilà neuf ans. La cigogne s'endort poliment.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_stork_card", 72, 120), new ApplyModifier("well_spoken")), Nothing,
                    Ai((AiAxis.Sociability, 2)), Stress(("shy", 20))),
            }),

        new EventDef("grown_stork_card", EventType.FollowUp, AnyStage, "Une carte de très loin",
            "Le hérisson apporte une carte pour {A}. Pas de timbre, juste une plume. Au dos : « Je te l'avais dit. » Devant, le dessin d'un escargot qui court.",
            new[]
            {
                Plain("pin", "L'accrocher au-dessus du lit",
                    "{A} accroche la carte au-dessus du lit. Certains soirs, {A} jurerait que l'escargot du dessin a avancé.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Zeal, 1))),
                Plain("show", "La montrer à l'escargot du village",
                    "L'escargot du village examine longuement le dessin, puis déclare : « Frimeur. »",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("elder_lap", EventType.Pulse, Elder, "Le tour de l'étang",
            "Comme chaque matin depuis toujours, {A} fait le tour de l'étang. À mi-chemin, les pattes disent non. Le banc, juste là, n'a jamais paru aussi bien placé.",
            new[]
            {
                Try("push", "Finir le tour, coûte que coûte", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} finit le tour, lentement, très {a:droit|droite}. Sur une patte, le héron salue l'arrivée d'un battement d'aile.",
                    "{A} s'arrête deux fois, puis trois. Le tour se termine à l'heure du déjeuner, et c'est quand même un tour.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Zeal, 1)), Stress(("lazy", 20))),
                Plain("bench", "S'asseoir sur le banc et regarder l'étang",
                    "{A} s'assoit. Sur l'étang, une libellule fait le tour à sa place, plusieurs fois, pour être sûre. {A} n'avait jamais remarqué que le matin avait autant de couleurs.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1), (AiAxis.Rationality, 1)), Stress(("stubborn", 20))),
                Plain("snail", "Inviter l'escargot à faire le tour, à son rythme",
                    "{A} et l'escargot partent ensemble. Le tour prend la journée. C'est le meilleur tour de l'étang de toute une vie.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
            }),
    };
}
