using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 7 story cycles: bébé and ado.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave7Young() => new[]
    {
        // ---- wave 7: more story cycles
        // bébé: la langue des grenouilles (CK3's learn_language scheme)
        new EventDef("baby_frogs", EventType.Pulse, Baby, "La langue des grenouilles",
            "Au bord de la mare, les grenouilles discutent toute la journée. « Croâ », dit l'une. « Croâ croâ », répond l'autre, l'air vexé. {A} est {a:convaincu|convaincue} que ces conversations sont passionnantes, et a décidé d'apprendre la langue des grenouilles. Sur un nénuphar, une vieille grenouille observe {A} avec un air de professeur à la retraite.",
            new[]
            {
                Plain("teacher", "Demander des leçons à la vieille grenouille",
                    "La vieille grenouille accepte, à condition que {A} vienne tous les matins, à l'heure, et sans faire de vagues. Première leçon : « croâ » veut dire bonjour. Ou au revoir. Ou « attention, un héron ». Tout dépend de l'intonation.",
                    E(new FollowUp("baby_frogs_lessons", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1))),
                Try("listen", "Écouter en cachette, et deviner {a:tout seul|toute seule}", new EventChallenge(PlynlingStat.Learning, 4),
                    "{A} passe la journée derrière un roseau à noter les croâ dans un carnet. Le soir, {A} a compris trois mots : « mouche », « soleil » et « tais-toi, Gérard ». C'est un début prometteur.",
                    "{A} note deux cents croâ, tous pareils. La vieille grenouille, attendrie, finit par proposer des leçons, « avant que tu ne t'abîmes les oreilles ».",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("baby_frogs_lessons", 24, 48)), E(new FollowUp("baby_frogs_lessons", 24, 48)),
                    Ai((AiAxis.Rationality, 2))),
                Plain("croak", "Répondre « croâ » à tout le monde, pour voir",
                    "{A} lance un grand « croâ » au milieu de la mare. Silence total. Puis toutes les grenouilles éclatent de rire, ce qui, chez les grenouilles, ressemble beaucoup à un éternuement collectif. La vieille grenouille soupire : « Du travail en perspective », et propose des leçons.",
                    E(new FollowUp("baby_frogs_lessons", 24, 48)), Ai((AiAxis.Boldness, 2))),
            }),

        new EventDef("baby_frogs_lessons", EventType.FollowUp, AnyStage, "Les leçons de la mare",
            "Tous les matins, {A} retrouve la vieille grenouille sur son nénuphar. On apprend les croâ graves, les croâ aigus, les croâ qui montent à la fin, comme une question. Aujourd'hui, c'est l'examen : la grenouille veut entendre {A} raconter sa journée, en grenouille.",
            new[]
            {
                Try("story", "Raconter sa journée, en grenouille", new EventChallenge(PlynlingStat.Learning, 4),
                    "{A} croasse le petit-déjeuner, le chemin, la flaque et le goûter. La vieille grenouille hoche la tête à chaque phrase, et éclate de rire au passage de la flaque. À la fin, la grenouille croasse quelque chose de très doux, qui veut dire, paraît-il, « bravo ».",
                    "{A} se trompe d'intonation au deuxième croâ et annonce, sans le vouloir, qu'un héron arrive. Panique générale dans la mare. Une heure entière pour tout rattraper.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired"), new FollowUp("baby_frogs_talk", 24, 48)), E(new FollowUp("baby_frogs_talk", 24, 48)),
                    Ai((AiAxis.Rationality, 1))),
                Plain("song", "Chanter une chanson du village, en grenouille",
                    "{A} chante la chanson de la récolte, en croâ. Toute la mare s'arrête pour écouter. Au dernier couplet, trois grenouilles reprennent en chœur. La vieille grenouille déclare l'examen réussi, « pour la musicalité ».",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("baby_frogs_talk", 24, 48)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("baby_frogs_talk", EventType.FollowUp, AnyStage, "La grande conversation",
            "Ce soir, pour la première fois, les grenouilles ont invité {A} à leur grande conversation du crépuscule. Tout le monde est assis en rond sur les nénuphars. Le sujet du jour est très important : la mouche passée ce matin, et si c'était bien la même que la semaine dernière.",
            new[]
            {
                Try("debate", "Donner son avis sur la mouche", new EventChallenge(PlynlingStat.Diplomacy, 4),
                    "{A} croasse que c'était sûrement la même, mais avec un autre chapeau. Débat enflammé. Au bout d'une heure, la mare vote : {A} a raison. La vieille grenouille, très fière, offre à {A} un petit caillou brillant, ramassé au fond de l'eau : un quartz.",
                    "{A} croasse trop fort et lance par erreur un débat sur la lune. Le débat dure toute la nuit. Plus personne ne se souvient de la mouche. Tout le monde est ravi.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new GiveItem("col.quartz")), E(new LiftNeed(Need.Happiness, 0.15)),
                    Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1))),
                Plain("smile", "Écouter, et sourire",
                    "{A} écoute la conversation jusqu'à la nuit, sans tout comprendre, mais en riant aux bons moments. En partant, la vieille grenouille croasse doucement : « Reviens quand tu veux. » Ça, {A} l'a compris tout de suite.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Rationality, 1), (AiAxis.Compassion, 1))),
            }),

        // bébé: la boîte à musique (CK3's artifact events: find it broken, repair it, learn its story)
        new EventDef("baby_music_box", EventType.Pulse, Baby, "La boîte à musique",
            "Au marché aux puces, sous une pile de vieux chapeaux, {A} a trouvé une boîte à musique. Le couvercle est peint de petites étoiles, la manivelle est tordue, et quand on l'ouvre, rien ne se passe, à part un tout petit grincement triste. Le marchand en demande un caillou. Un seul.",
            new[]
            {
                Plain("buy", "L'acheter, pour un caillou",
                    "{A} pose le caillou sur la table, prend la boîte à deux pattes et rentre en marchant très doucement, comme on porte un oisillon. À la maison, la boîte trône sur l'oreiller. Reste à la réparer.",
                    E(new FollowUp("baby_music_box_repair", 24, 48)), Ai((AiAxis.Compassion, 1))),
                Try("haggle", "Marchander, pour le principe", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} fait remarquer la manivelle tordue, le grincement triste et les étoiles écaillées. Le marchand, épuisé, cède la boîte pour rien, et ajoute un vieux bouton en cadeau, pour que {A} s'en aille.",
                    "Le marchand, vexé, monte le prix à deux cailloux. {A} paie en boudant un peu, et rentre quand même avec la boîte.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveItem("col.bouton"), new FollowUp("baby_music_box_repair", 24, 48)), E(new FollowUp("baby_music_box_repair", 24, 48)),
                    Ai((AiAxis.Greed, 1))),
                Plain("peek", "L'ouvrir sur place, pour voir dedans",
                    "{A} soulève le fond de la boîte. Dedans : un minuscule cylindre à picots, un ressort fatigué, et un papier plié en huit, avec une partition écrite à la main. Le marchand n'en savait rien. Intrigué, le marchand la laisse pour un caillou quand même.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("baby_music_box_repair", 24, 48)), Ai((AiAxis.Rationality, 2)), gate: new TraitGate("curious")),
            }),

        new EventDef("baby_music_box_repair", EventType.FollowUp, AnyStage, "L'atelier du hérisson",
            "Le dimanche, le hérisson chef de gare répare aussi les horloges, dans un atelier qui sent l'huile et le bois. {A} pose la boîte sur l'établi. Le hérisson ajuste ses lunettes, ouvre la boîte, écoute le grincement triste, et déclare : « Opérable. »",
            new[]
            {
                Try("screws", "Tenir les petites vis pendant la réparation", new EventChallenge(PlynlingStat.Stewardship, 4),
                    "{A} tend chaque vis au bon moment, sans en perdre une seule. Le hérisson redresse la manivelle, change le ressort, souffle sur le cylindre. Au bout de deux heures, la boîte joue trois notes, toutes justes. « Le reste viendra », dit le hérisson.",
                    "Une vis s'échappe, roule sous l'établi et disparaît pour toujours. Le hérisson en fabrique une autre avec un clou, en soupirant. La boîte joue trois notes, dont une un peu fausse. C'est la plus jolie.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("baby_music_box_song", 24, 48)), E(new FollowUp("baby_music_box_song", 24, 48)),
                    Ai((AiAxis.Energy, 1))),
                Plain("watch", "Regarder le hérisson faire, sans toucher à rien",
                    "{A} regarde, le menton posé sur l'établi, pendant que le hérisson démonte, nettoie et remonte. Le hérisson explique chaque pièce à voix basse, comme un secret. En fin d'après-midi, {A} connaît le nom de toutes les pièces, et la boîte joue de nouveau.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("baby_music_box_song", 24, 48)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("baby_music_box_song", EventType.FollowUp, AnyStage, "La chanson de la boîte",
            "La boîte est réparée. {A} tourne la manivelle, et une petite mélodie s'échappe, douce et un peu triste, une chanson que personne au village ne connaît. Au café, en l'entendant, la tortue s'arrête net, et repose sa tasse, très lentement.",
            new[]
            {
                Plain("ask", "Demander à la tortue d'où vient la chanson",
                    "La tortue s'assoit, ce qui prend du temps. La chanson, raconte la tortue, était celle d'une danseuse de passage, bien avant le vieux pont. Une seule soirée, et tout le village avait dansé. La tortue fredonne la fin, que la boîte avait oubliée. Le soir même, {A} la sait par cœur.",
                    E(new ApplyModifier("cherished"), new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 1))),
                Plain("gift", "Offrir la boîte à la tortue",
                    "{A} pose la boîte sur le comptoir, devant la tortue. La tortue ne dit rien pendant un très long moment. Puis la tortue range la boîte derrière le comptoir, à côté de la caisse, et sert à {A} un chocolat chaud. Depuis, tous les jours à l'heure du goûter, la boîte joue au café.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2))),
                Plain("keep", "La garder, et la jouer chaque soir avant de dormir",
                    "Chaque soir, {A} tourne la manivelle avant d'éteindre la lumière. La mélodie remplit la chambre, puis le couloir, puis, par la fenêtre ouverte, un petit bout de la rue. Les voisins ont pris l'habitude de s'endormir en même temps que {A}.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            }),

        // ado: la guerre des étals (CK3's tax rivalry story cycle, as a summer of syrup stalls)
        new EventDef("teen_stalls", EventType.Pulse, Teen, "La guerre des étals",
            "Pour l'été, {A} a ouvert un étal de sirop de sureau à l'entrée du marché. Les affaires marchaient bien, jusqu'à ce matin : juste en face, l'écureuil des jeux du village a ouvert un étal de sirop de mûre. Même table. Même parasol. Prix : un caillou de moins.",
            new[]
            {
                Try("price", "Baisser les prix, encore plus bas", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "{A} baisse le prix, puis ajoute une paille gratuite, puis une deuxième. Les clients traversent la rue. L'écureuil baisse à son tour. Le soir, les deux étals vendent presque à perte, mais celui de {A} a vendu le plus.",
                    "L'écureuil baisse encore plus vite, et offre un biscuit avec chaque verre. {A} vend trois verres de toute la journée, dont deux au hérisson, par loyauté.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(5), new FollowUp("teen_stalls_market", 24, 48)), E(new FollowUp("teen_stalls_market", 24, 48)),
                    Ai((AiAxis.Greed, 1), (AiAxis.Vengefulness, 1))),
                Plain("recipe", "Améliorer le sirop : une pointe de miel, une feuille de menthe",
                    "{A} passe la nuit à goûter : trop sucré, pas assez, trop de menthe, presque. Au matin, le sirop de {A} a un goût que personne ne sait nommer, mais que tout le monde veut regoûter. La file d'attente commence à l'aube.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("teen_stalls_market", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
                Plain("talk", "Traverser la rue pour discuter avec l'écureuil",
                    "{A} traverse la rue, un verre de sirop à la patte. L'écureuil, méfiant, goûte, puis offre un verre de mûre en échange. Les deux sirops sont bons. La discussion dure jusqu'au soir, et personne ne parle de prix.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("teen_stalls_market", 24, 48)), Ai((AiAxis.Sociability, 2), (AiAxis.Compassion, 1))),
            }),

        new EventDef("teen_stalls_market", EventType.FollowUp, AnyStage, "Le grand jour de marché",
            "C'est le jour du grand marché, celui où tout le village passe. Les deux étals sont prêts. L'écureuil a accroché une banderole immense. {A} a une idée en réserve. La pie, qui adore les rivalités, s'est assise exactement entre les deux, avec une tartine.",
            new[]
            {
                Try("show", "Faire le spectacle : chanter et jongler avec des citrons", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "{A} jongle avec trois citrons, puis quatre, en chantant les qualités du sureau. Le marché entier s'arrête pour regarder, puis pour acheter. De l'autre côté de la rue, l'écureuil applaudit malgré tout.",
                    "Le quatrième citron atterrit dans le sirop de l'écureuil. Éclaboussures, fou rire général, et l'écureuil vend tout son stock en une heure, « depuis que le sirop a du citron ».",
                    E(new GrowStat(PlynlingStat.Diplomacy), new GiveCailloux(15), new FollowUp("teen_stalls_end", 24, 48)), E(new FollowUp("teen_stalls_end", 24, 48)),
                    Ai((AiAxis.Boldness, 2), (AiAxis.Sociability, 1)), Stress(("shy", 20))),
                Try("spot", "Placer son étal pile sur le passage des clients", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} avance l'étal de trois pas, pile là où le chemin se rétrécit. Impossible de passer sans voir le sureau. Les ventes doublent. De loin, l'écureuil plisse les yeux : bien joué.",
                    "Le hérisson, qui surveille le marché, fait remarquer que l'étal bloque le passage des chariots. Retour à la place d'origine, avec un sermon sur la circulation.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveCailloux(10), new FollowUp("teen_stalls_end", 24, 48)), E(new FollowUp("teen_stalls_end", 24, 48)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Honor, -1))),
                Plain("merge", "Proposer à l'écureuil de vendre ensemble : sureau-mûre",
                    "{A} et l'écureuil collent les deux tables et inventent le sirop sureau-mûre. La pie, déçue de perdre sa rivalité, en commande trois verres. Les deux étals ne font plus qu'un, et la file d'attente aussi.",
                    E(new GiveCailloux(10), new FollowUp("teen_stalls_end", 24, 48)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("teen_stalls_end", EventType.FollowUp, AnyStage, "La fin de l'été",
            "L'été se termine. Les étals vont être rangés jusqu'à l'année prochaine. L'écureuil traverse la rue une dernière fois, les pattes dans les poches. Le marché est calme, et le parasol de {A} commence à pencher.",
            new[]
            {
                Plain("shake", "Serrer la patte de l'écureuil",
                    "{A} serre la patte de l'écureuil. « À l'année prochaine. » « À l'année prochaine. » Puis, après un silence : « Ton sirop était meilleur. » « Le tien aussi. » Personne ne sait qui a gagné l'été. Ça n'a plus vraiment d'importance.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, 1))),
                Plain("count", "Compter la recette de l'été",
                    "{A} renverse la boîte à monnaie sur la table, compte, puis recompte. La boîte est lourde, et la saison a été bonne. {A} met une part de côté pour l'an prochain, et dépense le reste en biscuits, par tradition.",
                    E(new GiveCailloux(10), new ApplyModifier("trade_sense")), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
            }),

        // ado: l'amitié en trois étapes (CK3's befriend scheme, ending in a real ask)
        new EventDef("teen_befriend", EventType.Pulse, Teen, "L'amitié en trois étapes",
            "Depuis quelque temps, {A} aimerait bien devenir {a:ami|amie} avec {B}. Le problème, c'est que {A} ne sait pas du tout comment on fait. La chouette a sûrement un livre là-dessus. En attendant, {A} a écrit un plan en trois étapes sur un bout de papier. L'étape un est soulignée deux fois.",
            new[]
            {
                Plain("hello", "Suivre le plan : étape un, dire bonjour",
                    "{A} attend {B} au coin de la rue et dit bonjour. {B} répond bonjour. Fin de l'étape un. {A} rentre le cœur battant, et coche la case avec un soin particulier.",
                    E(new AffinityShift(5), new FollowUp("teen_befriend_rain", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1))),
                Plain("jam", "Commencer par un petit cadeau anonyme",
                    "{A} laisse devant la porte de {B} un pot de confiture, avec un mot : « De la part de quelqu'un. » {B} goûte, et passe la journée à chercher qui. {A} passe la journée à faire semblant de ne pas savoir.",
                    E(new AffinityShift(10), new FollowUp("teen_befriend_rain", 24, 48)), Ai((AiAxis.Compassion, 1))),
                Try("blunt", "Aller droit au but : « Tu veux être mon {b:ami|amie} ? »", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "{B} cligne des yeux, {b:surpris|surprise}, puis rit. « On peut commencer par un goûter. » C'est une réponse. C'est même une très bonne réponse.",
                    "{B} répond poliment « peut-être » et s'éclipse. {A} range le plan dans sa poche. Retour à l'étape un, mais en plus discret.",
                    E(new AffinityShift(15), new FollowUp("teen_befriend_rain", 24, 48)), E(new AffinityShift(5), new FollowUp("teen_befriend_rain", 24, 48)),
                    Ai((AiAxis.Boldness, 2)), Stress(("shy", 20))),
            },
            Target: TargetKind.Anyone,
            TargetCondition: t => t.Bond == PlynlingBond.Acquaintances && t.Affinity >= 0),

        new EventDef("teen_befriend_rain", EventType.FollowUp, AnyStage, "L'étape deux",
            "L'étape deux du plan dit : « Faire quelque chose ensemble. » {A} avait tout prévu : la pêche, les cerfs-volants, le concours de grimaces. Mais ce matin, la pluie tombe à verse, et {B} est {b:coincé|coincée} sous le même auvent que {A}, devant la boulangerie fermée.",
            new[]
            {
                Plain("cards", "Sortir le jeu de cartes de sa poche",
                    "{A} sort le jeu de cartes. {A} et {B} jouent sous l'auvent jusqu'à ce que la pluie s'arrête, puis encore un peu après. {B} triche très mal, et {A} fait semblant de ne rien voir. C'est le début de quelque chose.",
                    E(new AffinityShift(10), new FollowUp("teen_befriend_ask", 24, 48)), Ai((AiAxis.Sociability, 1))),
                Try("race", "Proposer une course sous la pluie jusqu'au café", new EventChallenge(PlynlingStat.Courage, 6),
                    "{A} et {B} courent sous la pluie, sautent dans toutes les flaques, et arrivent au café en dégoulinant, riant si fort que la tortue sert deux chocolats sans rien demander.",
                    "{A} glisse dans la première flaque et s'assoit dedans. {B} hésite, puis s'assoit à côté, dans la flaque, par solidarité. Les deux restent là un moment, sous la pluie, à rire comme des idiots.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(15), new FollowUp("teen_befriend_ask", 24, 48)), E(new AffinityShift(10), new FollowUp("teen_befriend_ask", 24, 48)),
                    Ai((AiAxis.Boldness, 2))),
                Plain("talk", "Parler, simplement",
                    "Sous l'auvent, {A} et {B} parlent de tout : des nuages, de la gare, d'un rêve bizarre. Au bout d'une heure, {A} se rend compte que l'étape deux est en train de se faire toute seule, sans plan.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(10), new FollowUp("teen_befriend_ask", 24, 48)), Ai((AiAxis.Sociability, 2))),
            },
            Target: TargetKind.Anyone),

        new EventDef("teen_befriend_ask", EventType.FollowUp, AnyStage, "L'étape trois",
            "L'étape trois du plan est écrite en tout petit, en bas de la page : « Demander. » {A} relit le mot une dizaine de fois. Ça paraît simple, sur le papier. {B} est là, à quelques pas, en train de donner du pain aux canards.",
            new[]
            {
                Ask("ask", "Demander à {B} : « On est amis, maintenant ? »",
                    "{A} prend une grande inspiration, s'approche, et pose la question, le papier serré dans la patte.", "reply_befriend",
                    Ai((AiAxis.Sociability, 2), (AiAxis.Boldness, 1))),
                Plain("ducks", "Jeter le plan, et donner du pain aux canards avec {B}",
                    "{A} froisse le plan et le jette dans la poubelle la plus proche. Puis s'approche, prend un morceau de pain et le donne aux canards, à côté de {B}. Personne ne demande rien. Pas besoin, finalement.",
                    E(new AffinityShift(10)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Anyone),

        new EventDef("reply_befriend", EventType.Response, AnyStage, "Une question au bord de l'étang",
            "Au bord de l'étang, {B} s'est {b:approché|approchée} de {A}, un papier froissé dans la patte, et a demandé : « On est amis, maintenant ? »",
            new[]
            {
                Answer("yes", "Dire oui, et partager le pain",
                    "{A} dit oui, et tend un morceau de pain. {B} le donne aux canards, puis lit le papier à voix haute, étape par étape. Les deux rient jusqu'à la nuit tombée.",
                    Stance.Accept, E(new SetAffinityAtLeast(PlynlingBonds.FriendsFrom + PlynlingBonds.BondMargin)), Ai((AiAxis.Sociability, 2))),
                Answer("soon", "« Presque. Encore un goûter ou deux. »",
                    "{A} sourit : « Presque. Encore un goûter ou deux. » {B} sort un crayon et ajoute une étape quatre au plan : « Goûters. »",
                    Stance.Refuse, E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            }),
    };
}
