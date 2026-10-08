using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The event catalog. Keys (events and options) are stored: **never rename one**; append new events
/// at the end. Situations are inspired by Crusader Kings III and rewritten for the village — see
/// docs/plynling-writing-style.md. No « il »/« elle » in event text: the harness bans them, because
/// one text serves every gender pair.
/// </summary>
public static class PlynlingEvents
{
    private static readonly PlynlingStage[] Baby = { PlynlingStage.Baby };
    private static readonly PlynlingStage[] Teen = { PlynlingStage.Teen };
    private static readonly PlynlingStage[] Grown = { PlynlingStage.Adult, PlynlingStage.Elder };
    private static readonly PlynlingStage[] Elder = { PlynlingStage.Elder };
    private static readonly PlynlingStage[] AnyStage = Enum.GetValues<PlynlingStage>();

    private static readonly IReadOnlyList<EventEffect> Nothing = Array.Empty<EventEffect>();
    private static IReadOnlyList<EventEffect> E(params EventEffect[] effects) => effects;
    private static Dictionary<string, int> Stress(params (string Trait, int Amount)[] costs) => costs.ToDictionary(c => c.Trait, c => c.Amount);
    private static Dictionary<AiAxis, int> Ai(params (AiAxis Axis, int W)[] w) => w.ToDictionary(x => x.Axis, x => x.W);
    private static readonly Dictionary<string, int> NoStress = new();

    // A plain option (no challenge): the same outcome whatever happens.
    private static EventOption Plain(string key, string label, string outcome, IReadOnlyList<EventEffect> effects,
        Dictionary<AiAxis, int> ai, Dictionary<string, int>? stress = null, EventGate? gate = null) =>
        new(key, label, outcome, null, gate, null, effects, Nothing, stress ?? NoStress, ai);

    private static EventOption Try(string key, string label, EventChallenge challenge, string success, string failure,
        IReadOnlyList<EventEffect> onSuccess, IReadOnlyList<EventEffect> onFailure, Dictionary<AiAxis, int> ai,
        Dictionary<string, int>? stress = null, EventGate? gate = null) =>
        new(key, label, success, failure, gate, challenge, onSuccess, onFailure, stress ?? NoStress, ai);

    // An option that asks the other Plynling's owner (AskTarget). Owner-only unless said otherwise:
    // deciding alone never risks a refusal.
    private static EventOption Ask(string key, string label, string outcome, string responseKey, Dictionary<AiAxis, int> ai,
        bool ownerOnly = true) =>
        new(key, label, outcome, null, null, null, E(new AskTarget(responseKey)), Nothing, NoStress, ai, OwnerOnly: ownerOnly);

    // A response's answer: Stance leans it when the responder decides alone.
    private static EventOption Answer(string key, string label, string outcome, Stance stance, IReadOnlyList<EventEffect> effects,
        Dictionary<AiAxis, int> ai) =>
        new(key, label, outcome, null, null, null, effects, Nothing, NoStress, ai, Stance: stance);

    public static readonly IReadOnlyList<EventDef> All = new[]
    {
        // ---- bébé
        new EventDef("baby_puddle", EventType.Pulse, Baby, "La première flaque",
            "{A} découvre une flaque. Une vraie, avec un ciel dedans. {A} se penche, et le ciel se penche aussi.",
            new[]
            {
                Try("jump", "Sauter dedans à pieds joints", new EventChallenge(PlynlingStat.Courage, 4),
                    "Splash. Le ciel éclate en mille morceaux, puis se recolle. {A} recommence onze fois.",
                    "{A} glisse sur le bord et s'assoit dedans. Le ciel, vexé, ne dit rien.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing,
                    Ai((AiAxis.Boldness, 2), (AiAxis.Energy, 1)), Stress(("pensive", 20))),
                Plain("greet", "Saluer son reflet poliment",
                    "{A} fait une petite révérence. Le reflet aussi. C'est le début d'une grande amitié, au moins d'un côté.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 2), (AiAxis.Compassion, 1))),
                Plain("watch", "Attendre de voir si le ciel bouge",
                    "{A} attend une heure. Un nuage traverse la flaque. {A} repart {a:convaincu|convaincue} d'avoir vu le ciel de l'intérieur.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2)), gate: new TraitGate("pensive")),
            }),

        new EventDef("baby_snail", EventType.Pulse, Baby, "L'escargot du village",
            "L'escargot du village avance vers {A} depuis ce matin. Arrivée prévue vers midi.",
            new[]
            {
                Plain("wait", "L'attendre sagement",
                    "{A} attend. Quand l'escargot arrive enfin, {A} dort. L'escargot repart sans bruit, par délicatesse.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1)), Stress(("rowdy", 20))),
                Plain("run", "Courir à sa rencontre",
                    "{A} court, trébuche, roule, et arrive pile devant l'escargot, la tête en bas. « Bonjour », dit l'escargot, qui en a vu d'autres.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Energy, 2), (AiAxis.Boldness, 1))),
                Try("race", "Lui proposer une course", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "Pour gagner, {A} part avant le signal. Victoire d'une bonne longueur. L'escargot réclame une revanche pour la semaine prochaine.",
                    "{A} se fait doubler au dernier virage. Personne ne sait comment.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Greed, 1), (AiAxis.Honor, -1))),
            }),

        // ---- ado
        new EventDef("teen_shortcut", EventType.Pulse, Teen, "Le raccourci interdit",
            "Le hérisson chef de gare a planté un panneau : « Raccourci fermé ». Derrière le panneau, le raccourci a l'air très ouvert.",
            new[]
            {
                Try("sneak", "Passer quand même", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} passe, revient, et personne n'a rien vu. Sauf le héron, qui fait semblant de rien.",
                    "Le hérisson attendait derrière le premier buisson. {A} écope d'un sermon de vingt minutes, avec des schémas.",
                    E(new GrowStat(PlynlingStat.Intrigue)), E(new StressChange(20)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -2)), Stress(("honest", 30), ("just", 20))),
                Try("ask", "Demander au hérisson pourquoi", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "Une famille de grenouilles y fait la sieste. {A} promet de chuchoter et obtient un laissez-passer.",
                    "Le hérisson répond « parce que » et retourne à ses horaires.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Sociability, 1), (AiAxis.Honor, 1))),
                Plain("around", "Faire le grand tour",
                    "Le grand tour prend une heure. {A} y trouve trois glands, un caillou rond et une opinion très arrêtée sur les panneaux.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1)), Stress(("impatient", 20))),
            }),

        new EventDef("teen_contest", EventType.Pulse, Teen, "Le concours du moineau",
            "Le moineau organise son concours annuel de la plus belle pomme de pin. Celle de {A} est… unique.",
            new[]
            {
                Try("present", "La présenter fièrement", new EventChallenge(PlynlingStat.Diplomacy, 7),
                    "Le moineau la tourne, la retourne, et invente une catégorie sur mesure : « Pomme de pin avec du caractère ».",
                    "Le moineau note « intéressant » et passe à la suivante. {A} sait ce que veut dire « intéressant ».",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("well_spoken")), Nothing,
                    Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1)), Stress(("shy", 20))),
                Try("swap", "L'échanger discrètement contre une plus jolie", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "Premier prix. {A} range la médaille au fond d'un tiroir et n'en parle jamais.",
                    "La plus jolie appartenait au moineau. Le silence qui suit dure longtemps.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing,
                    Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1)), Stress(("honest", 40), ("just", 20))),
                Plain("polish", "Aider les autres à cirer les leurs",
                    "{A} passe l'après-midi à faire briller les pommes de pin des autres. Personne ne gagne grâce à ça, mais tout le monde brille.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2), (AiAxis.Sociability, 1))),
            }),

        // ---- adulte et ancien
        new EventDef("grown_parcel", EventType.Pulse, Grown, "Le colis égaré",
            "Un colis attend devant la porte de {A}. Pas d'adresse, juste un champignon dessiné un peu de travers. Ça tinte quand on le secoue.",
            new[]
            {
                Try("open", "L'ouvrir", new EventChallenge(PlynlingStat.Courage, 7),
                    "Dedans : une clochette, et un mot. « Pour sonner quand tu as besoin d'aide. » Pas de signature. {A} la garde près de son lit.",
                    "Dedans : une clochette qui sonne toute seule, toute la nuit. {A} la rapporte au marché au matin, les yeux cernés.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("cherished")), Nothing,
                    Ai((AiAxis.Boldness, 2)), Stress(("craven", 20), ("paranoid", 20))),
                Try("owner", "Chercher son propriétaire dans tout le village", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Après quatorze portes, la tortue du café reconnaît son dessin, et offre un chocolat chaud pour la peine.",
                    "Personne ne le réclame. {A} rentre {a:fatigué|fatiguée}, le colis sous le bras.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing,
                    Ai((AiAxis.Honor, 2), (AiAxis.Compassion, 1)), Stress(("greedy", 20))),
                Plain("shelf", "Le ranger pour plus tard",
                    "{A} pose le colis sur une étagère, entre le pot de miel et la boîte à biscuits. Le colis y est toujours.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            }),

        new EventDef("grown_scarf", EventType.Pulse, Grown, "L'écharpe prêtée",
            "Voilà trois semaines que {A} porte l'écharpe que {B} lui a prêtée. L'écharpe tient très chaud, et {B} n'a rien redemandé.",
            new[]
            {
                Plain("return", "La rendre, lavée et pliée",
                    "{A} rend l'écharpe pliée en quatre, avec un pot de confiture glissé dedans. {B} fait semblant de ne pas être {b:ému|émue}.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(15)), Ai((AiAxis.Honor, 2), (AiAxis.Compassion, 1))),
                Plain("keep", "La garder encore un peu",
                    "{A} la garde. {B} la reconnaît de loin et plisse les yeux, sans rien dire. Pour l'instant.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(-10)),
                    Ai((AiAxis.Greed, 2), (AiAxis.Honor, -1)), Stress(("honest", 20), ("generous", 20))),
                Try("share", "Proposer de la partager, un jour chacun", new EventChallenge(PlynlingStat.Diplomacy, 9),
                    "{B} accepte. L'écharpe change de cou chaque matin, et c'est devenu leur petite cérémonie.",
                    "{B} trouve l'idée étrange et reprend son écharpe sur-le-champ.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(25)), E(new AffinityShift(-5)),
                    Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1)), gate: new StatGate(PlynlingStat.Diplomacy, 8)),
            },
            Target: TargetKind.Known),

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

        // ---- the four big moments: the ask (pulse, social) and its answer (response, on the other's card)
        new EventDef("social_declare", EventType.Pulse, Grown, "Le cœur qui bat",
            "Depuis quelque temps, {A} rougit chaque fois que {B} passe. Même de loin. Même de dos.",
            new[]
            {
                Ask("declare", "Tout avouer à {B}", "{A} prend son courage à deux mains et va tout dire à {B}.", "reply_declare",
                    Ai((AiAxis.Boldness, 2), (AiAxis.Sociability, 1))),
                Plain("wait", "Garder ça pour soi encore un peu",
                    "{A} garde le secret, bien au chaud. Certaines choses mûrissent mieux à l'ombre.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Boldness, -1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.CanCouple),

        new EventDef("reply_declare", EventType.Response, Grown, "Une déclaration",
            "{B} est venu{b:|e} tout avouer à {A}, les joues rouges et la voix qui tremble.",
            new[]
            {
                Answer("yes", "Dire oui",
                    "{A} dit oui. {B} en oublie de respirer, puis rit, puis respire. Tout le village est au courant avant le soir.",
                    Stance.Accept, E(new Couple()), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Answer("no", "Dire non, doucement",
                    "{A} dit non, avec beaucoup de douceur. {B} hoche la tête et rentre par le chemin le plus long.",
                    Stance.Refuse, E(new Heartbreak()), Ai((AiAxis.Honor, 1))),
            }),

        new EventDef("social_rival", EventType.Pulse, Grown, "Le défi",
            "{B} s'est encore vanté{b:|e} d'être {b:le plus rapide|la plus rapide} du village. Devant tout le monde. Devant {A}.",
            new[]
            {
                Ask("challenge", "{b:Le|La} défier à la course jusqu'au vieux pont", "{A} lance le défi, et {B} relève le menton.", "reply_rival",
                    Ai((AiAxis.Boldness, 2), (AiAxis.Vengefulness, 1))),
                Plain("ignore", "Hausser les épaules",
                    "{A} hausse les épaules si haut que ça devient un exercice. Personne n'a gagné, mais personne n'a perdu.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Hostile),

        new EventDef("reply_rival", EventType.Response, Grown, "Le défi relevé",
            "{B} défie {A} à la course jusqu'au vieux pont. Le héron fait semblant de ne pas regarder.",
            new[]
            {
                new EventOption("race", "Courir", "{A} gagne d'une moustache. Le héron, qui ne regardait pas, applaudit.",
                    "{B} gagne d'une moustache. {A} réclame une revanche avant même d'avoir repris son souffle.",
                    null, new EventChallenge(PlynlingStat.Courage, 0, VsTarget: true),
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(-5)), E(new AffinityShift(-10)),
                    NoStress, Ai((AiAxis.Boldness, 2)), Stance: Stance.Accept),
                Answer("decline", "Refuser, avec un sourire en coin",
                    "{A} refuse, et sourit en coin. {B} ne sait plus si c'est une victoire ou une défaite.",
                    Stance.Refuse, E(new AffinityShift(-5)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("social_pact", EventType.Pulse, Grown, "Le pacte",
            "{A} et {B} ont partagé leur dernier gâteau, leur dernier secret et une averse entière sous la même feuille.",
            new[]
            {
                Ask("pact", "Proposer un pacte de meilleurs amis", "{A} propose un pacte, le petit doigt tendu.", "reply_pact",
                    Ai((AiAxis.Sociability, 2), (AiAxis.Honor, 1)), ownerOnly: false),
                Plain("enjoy", "Profiter de l'amitié telle quelle",
                    "{A} ne propose rien. Ce qui marche n'a pas besoin de contrat.",
                    E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.Bond == PlynlingBond.Friends),

        new EventDef("reply_pact", EventType.Response, Grown, "Le petit doigt",
            "{B} tend le petit doigt à {A} : un pacte de meilleurs amis, pour la vie, ou au moins jusqu'à jeudi.",
            new[]
            {
                Answer("yes", "Serrer le petit doigt", "Les deux petits doigts se serrent. C'est officiel, et le moineau sert de témoin.",
                    Stance.Accept, E(new SetAffinityAtLeast(PlynlingBonds.BestFriendsFrom + PlynlingBonds.BondMargin)), Ai((AiAxis.Sociability, 2))),
                Answer("later", "Dire « pas encore »", "{A} replie le doigt de {B}, gentiment. « Pas encore. Mais garde-le tendu. »",
                    Stance.Refuse, E(new AffinityShift(-5)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("social_mend", EventType.Pulse, Grown, "Le rameau d'olivier",
            "{A} croise {B} au marché. Les deux font semblant de ne pas se voir, avec beaucoup d'application.",
            new[]
            {
                Ask("mend", "Proposer de faire la paix", "{A} tend un rameau, un vrai, cueilli exprès.", "reply_mend",
                    Ai((AiAxis.Compassion, 2), (AiAxis.Vengefulness, -2)), ownerOnly: false),
                Plain("snub", "Continuer à faire semblant",
                    "{A} fait semblant si bien que {B} y croit presque. Le marchand de baies, lui, n'y croit pas du tout.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Vengefulness, 1))),
            },
            Target: TargetKind.Hostile),

        new EventDef("reply_mend", EventType.Response, Grown, "La paix ?",
            "{B} tend un rameau à {A}, cueilli exprès. Le marché entier retient son souffle.",
            new[]
            {
                Answer("yes", "Accepter le rameau", "{A} prend le rameau. On ne devient pas amis en un jour, mais on peut arrêter de se fâcher.",
                    Stance.Accept, E(new SetAffinityAtLeast(0)), Ai((AiAxis.Compassion, 2))),
                Answer("no", "Le laisser tomber", "Le rameau tombe dans la poussière. {B} le ramasse et repart avec, l'air digne.",
                    Stance.Refuse, E(new AffinityShift(-5)), Ai((AiAxis.Vengefulness, 2))),
            }),

        // ---- on-actions: what life brings ({T} = the trait(s) just revealed)
        new EventDef("on_welcome", EventType.Triggered, Baby, "Bienvenue",
            "{A} ouvre les yeux sur sa nouvelle maison. Tout est grand, tout sent bon. Déjà, on devine un petit caractère : {T}.",
            new[]
            {
                Plain("explore", "Explorer chaque coin", "{A} inspecte chaque coin, renifle chaque meuble, et adopte officiellement le coussin du fond.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1))),
                Plain("nap", "Faire une première sieste", "{A} choisit un rayon de soleil et s'y endort, comme si l'endroit avait toujours été là pour ça.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Energy, -1))),
            },
            Trigger: OnAction.Adopted),

        new EventDef("on_teen", EventType.Triggered, Teen, "Plus tout à fait bébé",
            "Un matin, {A} ne tient plus dans son ancien coin préféré. Quelque chose a changé : {T}, voilà ce que devient {A}.",
            new[]
            {
                Plain("proud", "Bomber le torse", "{A} se mesure contre la porte et grave un trait, très haut, en trichant un peu.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1))),
                Plain("shy", "Faire comme si de rien n'était", "{A} garde ses anciennes habitudes encore un peu. Grandir, ça peut attendre l'après-midi.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Boldness, -1))),
            },
            Trigger: OnAction.BecameTeen),

        new EventDef("on_adult", EventType.Triggered, Grown, "Adulte, maintenant",
            "{A} a fini de grandir. Sur ses traits se lit maintenant un dernier trait de caractère : {T}.",
            new[]
            {
                Plain("party", "Fêter ça au café", "La tortue du café offre une part de gâteau, et dit « déjà ? » trois fois.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
                Plain("plan", "Faire des projets", "{A} sort un carnet et y écrit « projets ». Puis, en dessous : « à voir ».",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
            },
            Trigger: OnAction.BecameAdult),

        new EventDef("on_visit_forgot", EventType.Triggered, AnyStage, "L'objet oublié",
            "Après la visite, {A} trouve un petit mouchoir brodé sous un coussin. C'est celui de {B}.",
            new[]
            {
                Plain("return", "Le rapporter tout de suite", "{A} court le rapporter. {B} ne s'était même pas rendu compte de l'oubli, et en est {b:touché|touchée}.",
                    E(new AffinityShift(5)), Ai((AiAxis.Honor, 1), (AiAxis.Energy, 1))),
                Plain("keep", "Le garder pour la prochaine fois", "{A} plie le mouchoir et le pose bien en vue. Une prochaine fois viendra bien.",
                    E(new FollowUp("follow_forgot", 24, 72)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Anyone, Trigger: OnAction.AfterVisit),

        new EventDef("follow_forgot", EventType.FollowUp, AnyStage, "Le mouchoir brodé",
            "Le mouchoir de {B} attend toujours sur l'étagère de {A}. Le mouchoir commence à sentir la maison.",
            new[]
            {
                Plain("bring", "Le rendre, enfin", "{A} rend le mouchoir, plié avec soin, et une fleur glissée dedans. {B} fait comme si c'était prévu.",
                    E(new AffinityShift(10), new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Honor, 1))),
                Plain("adopt", "Garder le mouchoir pour de bon", "{A} adopte le mouchoir. {B} le reconnaîtra un jour, mais ce jour n'est pas aujourd'hui.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(-5)), Ai((AiAxis.Greed, 1))),
            },
            Target: TargetKind.Anyone),

        new EventDef("on_bereaved", EventType.Triggered, AnyStage, "Une place vide",
            "La place de {B} est vide, et le village est un peu plus grand sans. {A} ne sait pas trop quoi faire de ses pattes.",
            new[]
            {
                Plain("remember", "Se souvenir des bons moments", "{A} raconte à voix haute leurs meilleures bêtises. Le héron écoute, pour une fois sans faire semblant.",
                    E(new StressChange(-10)), Ai((AiAxis.Sociability, 1))),
                Plain("plant", "Planter une graine en sa mémoire", "{A} plante une graine près du vieux pont. Au printemps, quelque chose poussera.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Anyone, Trigger: OnAction.Bereaved),

        new EventDef("on_sick", EventType.Triggered, AnyStage, "Le nez qui coule",
            "{A} éternue, renifle, et décide que le monde est injuste.",
            new[]
            {
                Plain("tea", "Réclamer une tisane", "La tortue du café envoie une tisane au miel, avec un mot : « Bois-la chaude. »",
                    E(new StressChange(-5)), Ai((AiAxis.Sociability, 1))),
                Plain("brave", "Faire comme si de rien n'était", "{A} fait comme si de rien n'était, entre deux éternuements spectaculaires.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1))),
            },
            Trigger: OnAction.FellSick),

        new EventDef("on_recovered", EventType.Triggered, AnyStage, "De nouveau sur pattes",
            "{A} se réveille, respire par le nez — par les deux narines — et se sent {a:neuf|neuve}.",
            new[]
            {
                Plain("run", "Faire le tour du village en courant", "{A} court partout, juste pour vérifier que tout marche encore. Tout marche.",
                    E(new ApplyModifier("fired_up")), Ai((AiAxis.Energy, 2))),
                Plain("thank", "Remercier ceux qui ont aidé", "{A} distribue des remerciements, et une tisane à la tortue, pour changer.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 1))),
            },
            Trigger: OnAction.Recovered),

        // ---- wave 1 (CK3 situations, retold): bébé
        new EventDef("baby_fledgling", EventType.Pulse, Baby, "L'oisillon tombé du nid",
            "Un oisillon tombé du nid piaille au bord du chemin. Pas de détresse : des reproches, à tout le monde, et surtout à {A}.",
            new[]
            {
                Try("climb", "Le remonter dans son nid", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} grimpe, branche après branche, l'oisillon sous le bras. En haut, l'oisillon fait remarquer que ce n'est pas son nid. Le sien est juste à côté.",
                    "{A} glisse à mi-hauteur et atterrit sur le derrière. L'oisillon, excédé, s'envole tout seul, pour ne plus avoir à regarder ça.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("light_heart")), Nothing, Ai((AiAxis.Boldness, 2))),
                Plain("moss", "Lui construire un nid de mousse en bas",
                    "{A} bâtit un nid de mousse, avec un toit en feuille et une sonnette. L'oisillon emménage aussitôt, et critique la décoration.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Compassion, 2))),
                Plain("listen", "Écouter ses reproches jusqu'au bout",
                    "{A} s'assoit et écoute. Au bout d'une heure, {A} sait tout sur les vers de terre, les chats et les gens qui marchent trop fort. L'oisillon s'endort au milieu d'une phrase.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1)), gate: new TraitGate("curious")),
            }),

        new EventDef("baby_honey", EventType.Pulse, Baby, "Le pot de miel",
            "Sur le comptoir du café, le pot de miel de la tortue brille au soleil. La tortue est au fond de la salle, et la tortue est très, très lente.",
            new[]
            {
                Try("dip", "Y tremper une patte en vitesse", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} trempe une patte, puis deux, puis le museau. La tortue atteint le comptoir le lendemain matin et ne remarque rien.",
                    "{A} reste {a:coincé|coincée} la patte dans le pot. Trois villageois et une cuillère ne sont pas de trop pour l'en sortir. La tortue, arrivée entre-temps, tend une serviette.",
                    E(new GrowStat(PlynlingStat.Intrigue)), E(new ApplyModifier("muddy_paws")),
                    Ai((AiAxis.Greed, 1), (AiAxis.Honor, -1), (AiAxis.Boldness, 1)), Stress(("bossy", 20))),
                Plain("ask", "Demander poliment une cuillère",
                    "{A} attend au comptoir. La tortue traverse la salle. Le soleil traverse le ciel. Au coucher, {A} obtient sa cuillère de miel, et l'impression d'avoir beaucoup grandi.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Honor, 1), (AiAxis.Energy, -1))),
            }),

        new EventDef("baby_brindille", EventType.Pulse, Baby, "Monsieur Brindille",
            "{A} présente à tout le village son nouvel ami, Monsieur Brindille. Personne ne le voit. Monsieur Brindille a pourtant des exigences très précises.",
            new[]
            {
                Plain("rules", "Respecter toutes ses règles",
                    "{A} met une place de plus à table, à gauche, jamais à droite, avec les croûtes du pain. Le village apprend vite à ne pas s'asseoir sur la chaise de gauche.",
                    E(new ApplyModifier("light_heart"), new FollowUp("baby_brindille_leaves", 48, 96)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1))),
                Plain("snail", "Le présenter à l'escargot",
                    "L'escargot salue poliment le vide. Après un long silence, l'escargot assure que Monsieur Brindille est charmant, mais parle un peu fort.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("baby_brindille_leaves", 48, 96)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("baby_brindille_leaves", EventType.FollowUp, AnyStage, "Le départ de Monsieur Brindille",
            "Ce matin, Monsieur Brindille a fait sa valise. Personne ne voit la valise non plus, mais la valise a l'air lourde.",
            new[]
            {
                Plain("station", "L'accompagner jusqu'à la gare",
                    "Le hérisson chef de gare poinçonne deux billets sans poser de question. {A} fait signe au train jusqu'à ce que le train disparaisse.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Sociability, 1))),
                Plain("gift", "Glisser un souvenir dans sa valise",
                    "{A} glisse un gland dans la valise invisible. Le gland disparaît pour de bon. Personne n'a jamais su comment.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Compassion, 1))),
            }),

        // ---- wave 1: ado
        new EventDef("teen_fireflies", EventType.Pulse, Teen, "Les lucioles du vieux pont",
            "Une nuit par an, les lucioles se donnent rendez-vous sur le vieux pont. C'est ce soir, bien après le couvre-feu.",
            new[]
            {
                Try("sneak", "Sortir sans bruit par la fenêtre", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} s'assoit au bord du pont. Les lucioles s'allument une à une, puis toutes ensemble, et l'eau en dessous s'allume aussi. À deux pas, le héron fait semblant de dormir.",
                    "La fenêtre grince. Puis le volet. Puis {A}, en atterrissant dans les orties. Retour au lit : les lucioles, ce sera l'année prochaine.",
                    E(new ApplyModifier("inspired"), new FollowUp("teen_fireflies_heron", 18, 30)), Nothing,
                    Ai((AiAxis.Boldness, 2), (AiAxis.Honor, -1)), Stress(("just", 20))),
                Plain("owl", "Demander à la chouette de venir aussi",
                    "La chouette accepte, apporte un carnet et note chaque luciole. Trois cent douze, selon le carnet. La chouette en est très fière.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Plain("dream", "Rester au lit et rêver des lucioles",
                    "{A} s'endort en comptant des lucioles imaginaires. Au matin, sur le rebord de la fenêtre, une vraie luciole dort aussi.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Energy, -1))),
            }),

        new EventDef("teen_fireflies_heron", EventType.FollowUp, AnyStage, "Le héron n'a rien vu",
            "Le lendemain, le héron attend {A} au bout du vieux pont, sur une patte, l'air de ne rien savoir du tout.",
            new[]
            {
                Plain("thank", "Le remercier pour son silence",
                    "Le héron répond : « Pour quoi ? Je n'ai rien vu. » Puis, sans tourner la tête : « La prochaine fois, passe par la porte. »",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Honor, 1))),
                Plain("whistle", "Passer en sifflotant",
                    "{A} passe en sifflotant. Le héron sifflote aussi, la même chanson, un peu faux.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Honor, -1))),
            }),

        new EventDef("teen_timetables", EventType.Pulse, Teen, "Les horaires d'été",
            "Le hérisson chef de gare a reçu tous les horaires de l'été, en vrac, dans un seul carton. Dehors, la rivière est tiède et le soleil est parfait.",
            new[]
            {
                Plain("sort", "Aider à tout classer",
                    "{A} classe tout l'après-midi, par ligne, par heure, par couleur de tampon. À la fin, le hérisson range le carton, ravi… et en sort un deuxième : l'hiver.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Energy, 1), (AiAxis.Compassion, 1)), Stress(("lazy", 30))),
                Plain("river", "Filer à la rivière",
                    "{A} passe l'après-midi dans l'eau tiède. En rentrant, {A} croise le hérisson, qui a tout classé seul et n'a jamais eu l'air aussi heureux.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Energy, -1), (AiAxis.Greed, 1)), Stress(("diligent", 30))),
                Try("both", "Proposer de classer… au bord de la rivière", new EventChallenge(PlynlingStat.Diplomacy, 7),
                    "Le hérisson accepte, à condition de garder ses chaussettes. Les horaires sont classés, les pieds sont au frais, et le train de 14 h 12 a failli partir au fil de l'eau.",
                    "Le hérisson refuse : les horaires ont le mal de mer. {A} classe à l'intérieur, les yeux sur la fenêtre.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_shells", EventType.Pulse, Teen, "Le jeu des coquilles",
            "Au marché, la pie fait tourner trois coquilles de noix sur un tonneau. Sous l'une, un gland. Peut-être.",
            new[]
            {
                Try("play", "Tenter sa chance", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} montre la coquille du milieu, et la soulève avant la pie : le gland est là. La pie applaudit, vexée.",
                    "{A} perd trois fois de suite. La pie propose une quatrième partie, « pour le plaisir ». {A} refuse, pour sa dignité.",
                    E(new ApplyModifier("sly")), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Greed, 1))),
                Try("cheat", "Glisser son propre gland sous une coquille", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "La pie soulève la coquille, trouve un gland qui n'est pas le sien, et reste un long moment sans voix.",
                    "La pie voit tout. La pie voit toujours tout. {A} repart avec un clin d'œil professionnel, et sans son gland.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1)), Stress(("honest", 30), ("just", 20))),
                Plain("expose", "Montrer à tout le monde où est vraiment le gland",
                    "{A} tapote le bec de la pie. Le gland tombe sur le tonneau. Le marché rit, et la pie aussi, un peu jaune.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
            }),

        // ---- wave 1: adulte et ancien
        new EventDef("grown_jam", EventType.Pulse, Grown, "Une remarque sur la confiture",
            "Au café, {B} goûte la confiture de {A} et déclare, assez fort pour toute la salle : « Ta confiture manque de caractère. »",
            new[]
            {
                Try("retort", "Répondre du tac au tac", new EventChallenge(PlynlingStat.Diplomacy, 8, VsTarget: true),
                    "{A} répond que le caractère, c'est comme le sucre : certains en mettent trop. La salle applaudit. {B} commande une deuxième tartine, sans un mot.",
                    "{A} cherche une réplique et la trouve trois heures plus tard, en se brossant les dents.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(-5)), E(new AffinityShift(-5)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Vengefulness, 1)), Stress(("calm", 20))),
                Plain("character", "Donner du caractère à la confiture",
                    "{A} dessine sur le pot une moustache, des sourcils froncés et un nom : Gustave. {B} regoûte, et admet un net progrès.",
                    E(new GrowStat(PlynlingStat.Stewardship), new AffinityShift(5)), Ai((AiAxis.Rationality, -1), (AiAxis.Sociability, 1)), Stress(("wrathful", 20))),
                Plain("sulk", "Bouder dans son coin",
                    "{A} boude jusqu'à la fermeture. La tortue débarrasse autour, très lentement, par solidarité.",
                    E(new AffinityShift(-10), new ApplyModifier("sulky")), Ai((AiAxis.Vengefulness, 2))),
            },
            Target: TargetKind.Anyone),

        new EventDef("grown_guest", EventType.Pulse, Grown, "Un escargot pour la nuit",
            "À la tombée du soir, un escargot voyageur frappe chez {A}. Loin de chez lui, l'escargot demande un lit pour la nuit. Sa coquille a l'air très, très pleine.",
            new[]
            {
                Plain("welcome", "L'accueillir pour la nuit",
                    "L'escargot sort de sa coquille un oreiller, une couverture, une lampe de chevet et un tapis. {A} demande à quoi sert le lit, alors. « À rien. Je voyage léger. »",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2)), Stress(("greedy", 20))),
                Plain("tortoise", "L'envoyer au café, où la tortue loue une chambre",
                    "Le lendemain, la tortue raconte que l'escargot a payé sa chambre avec une chanson, et que c'était une excellente affaire.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1)), Stress(("generous", 20))),
                Plain("supper", "Partager le dîner, mais pas la maison",
                    "{A} sert une soupe aux glands. L'escargot raconte ses voyages jusqu'à minuit, puis va dormir dans sa coquille, sur le paillasson.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_bramble", EventType.Pulse, Grown, "La grande ronce",
            "On raconte qu'au fond de la forêt pousse la plus grande ronce du pays, gardée par un vieux blaireau très grognon.",
            new[]
            {
                Try("pick", "Cueillir des mûres sous son nez", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} cueille trois mûres sous le nez du blaireau, qui ne se réveille même pas. {A} raconte l'exploit partout, avec un blaireau un peu plus gros à chaque fois.",
                    "Le blaireau ouvre un œil. {A} en ouvre deux et court jusqu'au village, sans une mûre, avec une épine en souvenir.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing, Ai((AiAxis.Boldness, 2)), Stress(("craven", 30))),
                Try("ask", "Demander poliment au blaireau", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Le blaireau grogne, puis montre un coin de la ronce : « Pas mûres. Reviens dans quelques jours. »",
                    "Le blaireau grogne longuement, contre les cueilleurs, les promeneurs et la météo. {A} repart sans mûres, mais très {a:bien informé|bien informée}.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_bramble_ripe", 72, 120)), Nothing,
                    Ai((AiAxis.Sociability, 1), (AiAxis.Honor, 1))),
                Plain("leave", "Laisser le blaireau tranquille",
                    "{A} cueille des mûres ordinaires au bord du chemin. Les mûres ordinaires sont très bonnes aussi, tant qu'on ne pense pas aux autres.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        new EventDef("grown_bramble_ripe", EventType.FollowUp, AnyStage, "Les mûres du blaireau",
            "Le blaireau avait dit « dans quelques jours ». {A} revient : la ronce est noire de mûres, et le blaireau attend, un panier à la patte.",
            new[]
            {
                Plain("together", "Cueillir ensemble",
                    "{A} et le blaireau cueillent en silence tout l'après-midi. Au moment de partir, le blaireau grogne quelque chose qui ressemble beaucoup à « reviens ».",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
                Plain("jam", "En faire de la confiture pour le blaireau",
                    "{A} rapporte trois pots. Le blaireau les renifle, les cache, et nie depuis les avoir jamais reçus.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 1))),
            }),

        new EventDef("grown_plants", EventType.Pulse, Grown, "Les plantes à arroser",
            "{B} part trois jours en voyage et confie sa collection de plantes à {A}. Les instructions font quatre pages, recto verso.",
            new[]
            {
                Plain("letter", "Suivre les instructions à la lettre",
                    "{A} chante pour la fougère, tourne le cactus d'un quart vers le nord, et ne regarde jamais le basilic dans les yeux, comme demandé page trois.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_plants_back", 60, 84)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1)),
                    Stress(("lazy", 20))),
                Plain("own", "Faire à sa façon",
                    "{A} arrose tout un peu et parle à chaque plante de la même voix. Les plantes ont l'air de prendre des vacances, comme tout le monde.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_plants_back", 60, 84)), Ai((AiAxis.Rationality, -1), (AiAxis.Energy, -1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.Bond is PlynlingBond.Friends or PlynlingBond.BestFriends or PlynlingBond.Lovers),

        new EventDef("grown_plants_back", EventType.FollowUp, AnyStage, "Le retour de voyage",
            "{B} rentre de voyage et fait le tour de sa collection, plante par plante, sans rien dire.",
            new[]
            {
                Plain("fern", "Avouer que la fougère a changé de couleur",
                    "{B} regarde la fougère, puis {A}, puis la fougère. « Ce jaune lui va très bien. Merci. »",
                    E(new AffinityShift(10)), Ai((AiAxis.Honor, 2))),
                Plain("wait", "Attendre les compliments",
                    "{B} finit le tour, se retourne et offre à {A} une bouture de chaque plante. Les instructions pour les boutures font six pages.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(5)), Ai((AiAxis.Sociability, 1))),
            },
            Target: TargetKind.Anyone),

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
                Plain("bribe", "Offrir des cailloux brillants à la rivière",
                    "{A} jette trois cailloux brillants, un par tour. La rivière les prend, réfléchit, et monte quand même. On n'achète pas une rivière, mais ça valait le coup d'essayer.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("teen_mudcastle_morning", 12, 24)), Ai((AiAxis.Greed, -1), (AiAxis.Zeal, 1))),
                Try("walls", "Renforcer les murs, vite", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "{A} empile, tasse, lisse. Quand l'eau arrive, les murs tiennent. Le château passe la nuit, fier comme un vrai.",
                    "{A} empile trop vite. La tour nord s'affaisse doucement dans les douves, avec beaucoup de dignité.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("teen_mudcastle_morning", 12, 24)), E(new FollowUp("teen_mudcastle_morning", 12, 24)),
                    Ai((AiAxis.Energy, 2), (AiAxis.Rationality, 1)), Stress(("lazy", 20))),
                Plain("shrine", "Bâtir un petit autel à la rivière",
                    "{A} sculpte un autel minuscule et y dépose une feuille de menthe. La rivière contourne l'autel. Le reste du château, beaucoup moins.",
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

        new EventDef("teen_unlucky", EventType.Pulse, Teen, "Ça porte malheur",
            "Depuis ce matin, {B} refuse de marcher à côté de {A}. La raison : hier, {A} a marché sur l'ombre du héron. Selon {B}, ça attire les averses, les guêpes et les lundis.",
            new[]
            {
                Plain("ritual", "Jouer le jeu : faire trois tours sur soi-même",
                    "{A} fait trois tours sur soi-même, jette une pincée de sel par-dessus son épaule et salue le héron. {B} respire mieux. Le héron, qui n'a rien demandé, salue en retour.",
                    E(new AffinityShift(10)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Try("prove", "Prouver à {B} que tout ça n'existe pas", new EventChallenge(PlynlingStat.Learning, 6),
                    "{A} tient un carnet pendant une semaine : ombres, averses, guêpes. Les chiffres sont formels. {B} lit tout, longtemps, et range sa patte de lapin. « Bon. Mais je garde le trèfle. »",
                    "{B} lit le carnet et conclut que c'est le carnet qui porte malheur. Le carnet est enterré au fond du jardin, avec les honneurs.",
                    E(new GrowStat(PlynlingStat.Learning), new AffinityShift(5)), Nothing, Ai((AiAxis.Rationality, 2)), Stress(("zealous", 20))),
                Try("tease", "Inventer une superstition toute neuve, juste pour voir", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} annonce que croiser l'escargot porte bonheur, mais seulement à reculons. Le lendemain, {B} traverse tout le village à reculons, à la recherche de l'escargot.",
                    "{B} flaire la blague et en invente une meilleure : les farceurs perdent leurs chaussettes. Le soir même, {A} ne retrouve plus ses chaussettes.",
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

        // ---- wave 3: bébé
        new EventDef("baby_berry_stall", EventType.Pulse, Baby, "L'étal sans marchand",
            "Au bord du chemin, un étal de fraises, et personne derrière. Un bocal, une pancarte : « Un caillou la fraise. Merci. » {A} n'a pas de caillou. {A} a très, très envie d'une fraise.",
            new[]
            {
                Try("take", "En prendre une, juste une", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} prend une fraise et la mange. La fraise est parfaite. Le remords aussi, jusqu'au soir.",
                    "Au moment où {A} tend la patte, la pancarte se retourne toute seule. Au dos : « Je te vois. — La pie. » {A} repart les pattes vides et le cœur battant.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1))),
                Plain("drawing", "Payer avec un dessin de fraise",
                    "{A} glisse dans le bocal un dessin de fraise, très ressemblant, et prend une vraie fraise. Le lendemain, la pancarte dit : « Un caillou la fraise, ou un dessin. Merci. »",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1))),
                Plain("mind", "Tenir l'étal en attendant le marchand",
                    "{A} tient l'étal tout l'après-midi, très {a:sérieux|sérieuse}, et vend onze fraises à onze passants. Le vieux lapin revient, compte, et offre la douzième.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Honor, 1), (AiAxis.Energy, 1)), Stress(("rowdy", 10))),
            }),

        new EventDef("baby_burp", EventType.Pulse, Baby, "Le silence de la bibliothèque",
            "À la bibliothèque, le silence est si profond qu'on entend les pages vieillir. C'est le moment que choisit le ventre de {A} pour lâcher un rot. Un énorme. La chouette ouvre un œil.",
            new[]
            {
                Try("sorry", "S'excuser, tout bas", new EventChallenge(PlynlingStat.Diplomacy, 4),
                    "{A} chuchote « pardon », si bas que la chouette doit se pencher. La chouette hoche la tête et referme l'œil. Plus tard, un livre apparaît sur la table de {A} : « Les bonnes manières, tome 1 ».",
                    "{A} chuchote « pardon », et le hoquet arrive. Hic. Hic. La chouette ouvre le deuxième œil.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Honor, 1))),
                Plain("encore", "En faire un deuxième, encore plus beau",
                    "{A} prend une grande inspiration et se surpasse. Les étagères tremblent. Au fond, quelqu'un applaudit. La chouette sort sans un mot, et revient avec un écriteau : « Silence. Même toi. »",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Boldness, 2), (AiAxis.Honor, -1)), gate: new TraitGate("rowdy")),
                Try("blame", "Regarder l'escargot d'un air accusateur", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} fixe l'escargot avec reproche. Toute la salle se tourne vers l'escargot. L'escargot, digne, rentre dans sa coquille et n'en ressort qu'à la fermeture.",
                    "L'escargot fixe {A} en retour, plus longtemps, avec plus de reproche. {A} craque en premier.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -2)), Stress(("charming", 10))),
            }),

        new EventDef("baby_lost", EventType.Pulse, Baby, "Où sont passés les autres ?",
            "{A} cueillait des mûres avec tout le monde. {A} relève la tête : plus personne. Juste des ronces, une mare, et une vieille crapaude qui regarde {A} par-dessus ses lunettes.",
            new[]
            {
                Plain("ask", "Demander son chemin à la crapaude",
                    "La crapaude indique le chemin, mais d'abord, la recette de sa soupe d'orties. En entier. Avec les variantes. {A} rentre à la nuit, le chemin en tête et la recette aussi.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("baby_lost_soup", 48, 96)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1)), Stress(("rowdy", 10))),
                Try("stream", "Suivre le ruisseau, {a:tout seul|toute seule}", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} suit le ruisseau, qui suit son idée, et débouche pile sous le vieux pont. Le héron salue d'une patte : « Tu es en retard pour le goûter. »",
                    "Le ruisseau tourne en rond, et {A} aussi. C'est finalement la crapaude qui ramène {A} au village, en grommelant, et en récitant sa recette.",
                    E(new GrowStat(PlynlingStat.Courage)), E(new FollowUp("baby_lost_soup", 48, 96)), Ai((AiAxis.Boldness, 2))),
                Plain("wait", "Rester là et attendre qu'on vienne",
                    "{A} s'assoit sur une souche. Les autres reviennent vite, en criant le nom de {A}, et {A} est {a:câliné|câlinée} par tout le monde. La crapaude, déçue, garde sa recette pour une autre fois.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        new EventDef("baby_lost_soup", EventType.FollowUp, AnyStage, "La soupe de la crapaude",
            "On frappe. C'est la vieille crapaude, une marmite dans les pattes. « Soupe d'orties. Pour grandir. Ne dis pas non : ça pique moins quand c'est chaud. »",
            new[]
            {
                Plain("taste", "Goûter, bravement",
                    "{A} goûte. Ça pique un peu, ça réchauffe beaucoup. La crapaude regarde {A} manger jusqu'à la dernière cuillère, puis repart sans un mot, satisfaite.",
                    E(new ApplyModifier("hearty")), Ai((AiAxis.Boldness, 1))),
                Plain("invite", "Inviter la crapaude à manger aussi",
                    "La crapaude s'installe, mange trois bols, raconte sa jeunesse dans une mare très chic, et oublie sa marmite en partant. La marmite est toujours là.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("baby_grow_up", EventType.Pulse, Baby, "Plus tard, je serai…",
            "{A} surgit d'un buisson, une brindille à la patte, et la pointe vers le héron : « Quand je serai {a:grand|grande}, je serai… » Le héron attend la suite, sur une patte, très intéressé.",
            new[]
            {
                Plain("explore", "« …partir explorer le monde entier ! »",
                    "« Commence par le fond du jardin », dit le héron. {A} part aussitôt, et revient avec une carte, un ver de terre et de grandes ambitions.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_dream_explore", 336, 384)), Ai((AiAxis.Boldness, 2))),
                Plain("cafe", "« …tenir le café, comme la tortue ! »",
                    "« Alors entraîne-toi », dit le héron, en tendant une tasse vide. {A} sert un café imaginaire, rend une monnaie imaginaire, et réclame un pourboire bien réel.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_dream_cafe", 336, 384)), Ai((AiAxis.Greed, 1), (AiAxis.Sociability, 1))),
                Plain("owl", "« …tout savoir, comme la chouette ! »",
                    "« Alors dis-moi combien j'ai de plumes », dit le héron. {A} commence à compter. Le soleil se couche à la plume trois cent quatre.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_dream_owl", 336, 384)), Ai((AiAxis.Rationality, 2))),
                Plain("chief", "« …chef du village, et de tout le monde ! »",
                    "« Très bien, chef », dit le héron, sans bouger d'une plume. {A} donne trois ordres au héron, deux à l'escargot et un à la rivière. Seul l'escargot obéit, mais l'escargot allait déjà dans ce sens.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_dream_chief", 336, 384)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1)), gate: new TraitGate("bossy")),
            }),

        // ---- wave 3: ado
        new EventDef("teen_secret", EventType.Pulse, Teen, "Un secret trop lourd",
            "Depuis des jours, {A} porte un secret. Un petit secret, mais lourd, comme un gland qui serait en plomb. {B} est {b:le seul|la seule} à qui {A} pourrait le dire.",
            new[]
            {
                Ask("tell", "Tout raconter à {B}", "{A} entraîne {B} derrière le vieux pont et chuchote tout, d'une traite.", "reply_secret",
                    Ai((AiAxis.Sociability, 2)), ownerOnly: false),
                Plain("journal", "L'écrire dans un carnet, puis cacher le carnet",
                    "{A} écrit le secret, ferme le carnet et le glisse sous une latte du plancher. Le secret pèse beaucoup moins. Le plancher grince un peu plus.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Sociability, -1), (AiAxis.Rationality, 1))),
                Plain("keep", "Le garder pour soi",
                    "{A} garde le secret. Certains jours, ça pèse. D'autres jours, ça tient chaud.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1)), Stress(("gregarious", 10))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.Bond is PlynlingBond.Friends or PlynlingBond.BestFriends),

        new EventDef("reply_secret", EventType.Response, AnyStage, "Un secret confié",
            "Derrière le vieux pont, {B} a chuchoté un secret à {A}, d'une traite, sans respirer.",
            new[]
            {
                Answer("promise", "Promettre de ne jamais le répéter",
                    "{A} promet, la patte sur le cœur. {B} respire enfin. Le secret dort maintenant chez deux personnes, et dort beaucoup mieux.",
                    Stance.Accept, E(new AffinityShift(15)), Ai((AiAxis.Honor, 2))),
                Answer("swap", "Confier un secret en retour",
                    "{A} se penche et chuchote à son tour. {B} écarquille les yeux. Les deux secrets se ressemblent comme deux glands.",
                    Stance.Accept, E(new AffinityShift(20)), Ai((AiAxis.Sociability, 2))),
                Answer("laugh", "Éclater de rire : ce n'est vraiment pas grave",
                    "« C'est tout ? » {B} rougit, puis rit aussi, un peu {b:vexé|vexée}. D'un coup, le secret ne pèse plus rien.",
                    Stance.Refuse, E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_treasure_map", EventType.Pulse, Teen, "La carte au trésor",
            "En ouvrant un vieux livre de la bibliothèque, {A} fait tomber une carte. Une vraie : un pont, un arbre tordu, une croix. La chouette n'a rien vu. D'habitude, la chouette voit tout.",
            new[]
            {
                Plain("dig", "Partir creuser, le soir même",
                    "{A} emprunte une pelle, compte les pas depuis le vieux pont, et creuse. À la nuit tombée, la pelle fait « tonk ».",
                    E(new FollowUp("teen_map_box", 1, 4)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1)), Stress(("craven", 10))),
                Try("study", "Étudier la carte avant de creuser", new EventChallenge(PlynlingStat.Learning, 6),
                    "{A} remarque que l'arbre tordu est dessiné à l'envers. Une heure de calculs plus tard, la croix est au bon endroit, et la pelle aussi.",
                    "{A} étudie tant que la carte finit en boule. {A} creuse au hasard : trois trous, puis un quatrième. Le quatrième fait « tonk ».",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("teen_map_box", 1, 4)), E(new FollowUp("teen_map_box", 1, 4)), Ai((AiAxis.Rationality, 2))),
                Plain("return", "Rendre la carte à la chouette",
                    "La chouette regarde la carte, puis {A}, puis la carte. « Garde-la. Certaines choses doivent être trouvées par quelqu'un. » Pas d'autre explication.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("teen_map_box", 24, 48)), Ai((AiAxis.Honor, 2))),
            }),

        new EventDef("teen_map_box", EventType.FollowUp, AnyStage, "Ce que la croix cachait",
            "Au fond du trou, une boîte en fer. Dans la boîte, un carnet. Sur la première page, d'une écriture toute ronde : « Journal du héron. Ne pas lire. Surtout toi. »",
            new[]
            {
                Try("read", "Lire, juste la première page", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "« Aujourd'hui, j'ai essayé de tenir sur une patte. Je suis tombé dans la rivière. Demain, je réessaie. » {A} referme le carnet, très doucement.",
                    "{A} lit trois lignes. Une ombre passe sur la page : le héron, sur une patte, juste derrière. « Surtout toi », répète le héron.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -1), (AiAxis.Rationality, 1)), Stress(("honest", 20))),
                Plain("give", "Le rapporter au héron, sans l'ouvrir",
                    "Le héron prend le carnet, le soupèse et le glisse sous son aile. Le héron fait semblant de ne pas être ému. Le héron fait ça très mal.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("rebury", "Le réenterrer pour le prochain chercheur",
                    "{A} referme la boîte, rebouche le trou et redessine la carte, en plus joli. La nouvelle carte part dans un autre livre, pour quelqu'un d'autre.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_ghost_stories", EventType.Pulse, Teen, "Histoires de fantômes",
            "La nuit est tombée. Autour du feu, {A} et {B} se racontent des histoires de fantômes. Une seule règle : le premier qui sursaute a perdu.",
            new[]
            {
                Try("story", "Raconter la plus terrifiante, sans sursauter", new EventChallenge(PlynlingStat.Courage, 6, VsTarget: true),
                    "{A} raconte le train fantôme qui arrive toujours à l'heure. Au moment où le train siffle — c'était le hérisson, au loin — {B} saute en l'air. Victoire.",
                    "{B} raconte la confiture qui se mange toute seule, la nuit. Au même moment, quelque chose bouge dans les fourrés. {A} saute si haut qu'une chouette doit s'écarter.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(5)), E(new AffinityShift(5)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("sheet", "S'éclipser, et revenir sous un drap", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} revient sous un drap, en gémissant. {B} hurle, puis rit, puis jure de se venger. Par précaution, {A} dort la lumière allumée pendant une semaine.",
                    "{A} se prend les pattes dans le drap et roule jusqu'au feu. {B} éteint le drap avec une tasse d'eau, sans même s'arrêter de raconter.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(5), new ApplyModifier("sly")), Nothing, Ai((AiAxis.Honor, -1), (AiAxis.Boldness, 1))),
                Plain("kind", "Finir sur une histoire qui finit bien",
                    "{A} raconte le fantôme qui voulait juste qu'on lui rende son écharpe. {B} trouve ça nul, et réclame la suite.",
                    E(new AffinityShift(10)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("teen_puppets", EventType.Pulse, Teen, "Les marionnettes de la foire",
            "À la foire, un marionnettiste ambulant joue sa pièce. L'une des marionnettes ressemble beaucoup à {A}. Trop. Et c'est celle qui tombe dans la rivière à chaque scène.",
            new[]
            {
                Plain("laugh", "Rire plus fort que tout le monde",
                    "{A} rit si fort que la salle rit de {A} qui rit de la marionnette. Le marionnettiste, ravi, ajoute une chute.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 2)), Stress(("arrogant", 30))),
                Try("heckle", "« Ce n'est pas du tout comme ça que ça s'est passé ! »", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "{A} raconte la vraie version. La foule préfère la vraie version. Le marionnettiste prend des notes.",
                    "La foule fait « chut ». La marionnette aussi, d'une petite voix aiguë. {A} se rassoit.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("well_spoken")), Nothing, Ai((AiAxis.Boldness, 2), (AiAxis.Vengefulness, 1)), Stress(("shy", 20))),
                Try("steal", "Se glisser derrière le rideau et prendre la marionnette", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} fait danser la marionnette, saluer, puis pousser le méchant dans la rivière. Tonnerre d'applaudissements. Le marionnettiste propose une tournée.",
                    "{A} s'emmêle dans les fils. Pendant cinq minutes, le public ne sait plus qui manipule qui.",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("inspired")), Nothing, Ai((AiAxis.Boldness, 2), (AiAxis.Honor, -1))),
            }),

        // ---- wave 3: adulte et ancien
        new EventDef("grown_snail_race", EventType.Pulse, Grown, "La course d'escargots",
            "C'est le jour de la grande course d'escargots. L'escargot du village s'est inscrit, contre l'avis de tout le monde, à commencer par l'escargot du village.",
            new[]
            {
                Try("bet", "Parier sur l'escargot du village", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "L'escargot du village arrive dernier, mais tous les autres ont quitté la piste pour suivre une feuille de laitue. Victoire par abandon. {A} empoche la cagnotte : trois glands et une médaille en bouchon.",
                    "L'escargot du village s'arrête à mi-course pour saluer quelqu'un. Puis tout le monde. {A} perd son pari, et gagne un ami.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("trade_sense")), Nothing, Ai((AiAxis.Greed, 1), (AiAxis.Zeal, 1))),
                Plain("cheer", "L'encourager depuis le bord de la piste",
                    "{A} crie « Allez ! » pendant six heures. L'escargot du village franchit la ligne au coucher du soleil, sous les applaudissements de {A}, et de {A} {a:seul|seule}.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 2), (AiAxis.Sociability, 1))),
                Try("carry", "Le porter un peu. Juste un peu.", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} soulève l'escargot « pour lui montrer le paysage » et le repose trois mètres plus loin. Personne n'a rien vu. L'escargot du village gagne, et ne pose aucune question.",
                    "Le moineau, qui arbitre, siffle. Disqualification. Pour la peine, {A} doit porter l'escargot jusqu'à chez lui. L'escargot trouve le trajet très agréable.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -2)), Stress(("honest", 30), ("just", 20))),
            }),

        new EventDef("grown_magpie", EventType.Pulse, Grown, "La pie voleuse",
            "Au marché, la pie fond sur {A} et repart avec le plus beau bouton de son manteau. Perchée tout en haut du chêne, la pie fait briller le bouton au soleil, très lentement, pour que {A} voie bien.",
            new[]
            {
                Try("climb", "Grimper le récupérer", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} grimpe jusqu'en haut. La pie, impressionnée, rend le bouton, et un deuxième, qui n'est pas à {A}.",
                    "{A} grimpe à mi-hauteur et reste {a:coincé|coincée}. La pie descend, s'assoit à côté, et commente le paysage jusqu'à l'arrivée des secours.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_magpie_stash", 48, 96)), Nothing, Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("trade", "Proposer un échange", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} propose un dé à coudre. La pie évalue, refuse, réclame une cuillère, accepte un ruban. Affaire conclue : la pie sait négocier, et {A} aussi.",
                    "La pie réclame le manteau entier, pour aller avec le bouton. Les négociations sont rompues.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_magpie_stash", 48, 96), new ApplyModifier("trade_sense")), Nothing, Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
                Plain("let", "Lui laisser le bouton",
                    "{A} fait au revoir au bouton. La pie, décontenancée, regarde le bouton d'un autre œil. Un bouton qu'on vous laisse brille moins.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_magpie_stash", 48, 96)), Ai((AiAxis.Greed, -1), (AiAxis.Rationality, 1)), Stress(("greedy", 20))),
            }),

        new EventDef("grown_magpie_stash", EventType.FollowUp, AnyStage, "Le trésor de la pie",
            "En passant sous le grand chêne, {A} voit briller quelque chose dans un creux : le trésor de la pie. Des boutons, des cuillères, une clé, des lunettes, et la sonnette du hérisson, disparue depuis l'hiver.",
            new[]
            {
                Plain("return", "Tout rapporter aux propriétaires",
                    "{A} fait le tour du village. Le hérisson récupère sa sonnette et la fait sonner toute la journée. Du haut du chêne, la pie boude, mais regarde {A} avec quelque chose comme du respect.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("gift", "Y déposer un cadeau pour la pie",
                    "{A} dépose un petit caillou poli au milieu du trésor. Le lendemain, devant la porte de {A}, quelqu'un a laissé une cuillère. Très brillante.",
                    E(new ApplyModifier("magpie_friend")), Ai((AiAxis.Compassion, 1))),
                Plain("secret", "Ne toucher à rien, et ne rien dire",
                    "{A} ne touche à rien. Chaque fois que le hérisson cherche sa sonnette, {A} regarde ailleurs, très fort.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Honor, -1)), Stress(("honest", 20))),
            }),

        new EventDef("grown_jam_cellar", EventType.Pulse, Grown, "La porte du garde-manger",
            "{A} est {a:resté|restée} aider la tortue à fermer le café. La tortue s'est endormie sur le comptoir. Au fond du garde-manger, {A} remarque une porte que personne n'a jamais remarquée.",
            new[]
            {
                Try("open", "Ouvrir la porte, sans bruit", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "Derrière, une cave pleine de pots de confiture jusqu'au plafond. Chaque pot porte une étiquette, une année et quelques mots : « Le printemps où le pont a gelé. » « L'année du héron amoureux. » Le plus vieux pot date d'avant le village.",
                    "La porte grince. La tortue ouvre un œil, puis l'autre, et dit, très lentement : « Pas… encore. »",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_cellar_morning", 8, 16)), Nothing,
                    Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1)), Stress(("honest", 20))),
                Plain("wake", "Réveiller la tortue pour demander",
                    "La tortue se réveille, regarde la porte, regarde {A}. Après un très long silence : « Reviens demain. Tôt. »",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_cellar_morning", 8, 16)), Ai((AiAxis.Honor, 2))),
                Plain("leave", "Faire comme si la porte n'existait pas",
                    "{A} prend une tartine, referme le garde-manger et rentre. Cette nuit-là, {A} rêve de portes. Les portes ont toutes un goût de confiture.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1)), Stress(("curious", 20))),
            }),

        new EventDef("grown_cellar_morning", EventType.FollowUp, AnyStage, "Les pots de la tortue",
            "Au petit matin, la tortue attend {A} devant le garde-manger, une lanterne à la patte. « Chaque année, je garde un pot. Pour me souvenir. Choisis. »",
            new[]
            {
                Plain("oldest", "Le plus vieux pot",
                    "La tortue ouvre le plus vieux pot. Ça sent une saison dont personne d'autre ne se souvient. La tortue raconte ; {A} écoute jusqu'à midi.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
                Plain("this_year", "Celui de cette année, encore sans étiquette",
                    "La tortue tend une plume : « Alors c'est toi qui écris l'étiquette. » {A} réfléchit longtemps, puis écrit son propre nom.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_hobby", EventType.Pulse, Grown, "Juste une fois",
            "Depuis des semaines, {B} insiste : {A} doit essayer son passe-temps préféré. « Juste une fois. Tu verras, c'est facile. » {B} a apporté tout le matériel, et une liste.",
            new[]
            {
                Try("try", "Essayer, de bon cœur", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} écoute, essaie, rate, réessaie, et réussit. {B} saute de joie et en parle à tout le village pendant une semaine.",
                    "{A} essaie avec beaucoup de bonne volonté, pour un résultat que personne n'arrive à nommer. {B} dit que c'est très bien pour une première fois, d'une voix bizarre.",
                    E(new GrowStat(PlynlingStat.Learning), new AffinityShift(15)), E(new AffinityShift(10)),
                    Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1)), Stress(("stubborn", 20))),
                Try("improve", "Proposer « une petite amélioration » à la méthode", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{B} essaie l'amélioration, s'arrête, réessaie, et regarde {A} avec des yeux ronds. C'est mieux. C'est même beaucoup mieux.",
                    "{B} explique, très calmement, pourquoi la méthode est ainsi depuis toujours. L'explication dure jusqu'au dîner.",
                    E(new GrowStat(PlynlingStat.Stewardship), new AffinityShift(10)), E(new AffinityShift(-5)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1)), Stress(("humble", 20))),
                Plain("watch", "Refuser gentiment, mais regarder",
                    "{A} refuse, avec un sourire. {B} range le matériel, puis le ressort : « Et si tu regardais, juste ? » {A} regarde. Regarder, c'est déjà bien.",
                    E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_bare_pantry", EventType.Pulse, Grown, "Un garde-manger bien vide",
            "Chez {B}, le garde-manger est presque vide : un pot de miel raclé jusqu'au fond, et trois glands. {B} sourit : « J'ai déjà mangé, mais sers-toi ! »",
            new[]
            {
                Try("basket", "Revenir avec un panier « trop lourd à porter »", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} revient avec un panier plein : « J'en ai trop, ça va se perdre. » {B} n'est pas dupe, et fait semblant de l'être, très bien.",
                    "{A} dépose le panier sur le paillasson et file. {B} reconnaît l'écriture de {A} sur l'étiquette du pot.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(20)), E(new AffinityShift(10)), Ai((AiAxis.Compassion, 2), (AiAxis.Honor, 1))),
                Plain("feast", "Organiser un grand goûter… chez {B}",
                    "Tout le village débarque chez {B}, chacun avec un plat. À la fin, les restes remplissent le garde-manger jusqu'au plafond. {B} soupçonne quelque chose, mais trop tard.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(15)), Ai((AiAxis.Sociability, 2))),
                Try("speak", "En parler franchement", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "« Ton garde-manger est vide. » {B} se tait, puis rit, puis raconte tout : un mauvais hiver, un pot cassé, la fierté. {A} et {B} font les courses ensemble.",
                    "{B} se raidit : « Tout va très bien. » Le sujet est clos, et la porte aussi, un peu vite.",
                    E(new AffinityShift(15)), E(new AffinityShift(-5)), Ai((AiAxis.Honor, 2)), Stress(("shy", 20))),
                Plain("tact", "Prendre un gland, et parler d'autre chose",
                    "{A} prend un gland, le mange lentement, et parle d'autre chose. Certaines fiertés ont besoin qu'on les laisse tranquilles.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1)), Stress(("compassionate", 20))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.Bond is PlynlingBond.Friends or PlynlingBond.BestFriends or PlynlingBond.Lovers),

        new EventDef("grown_wish_oak", EventType.Pulse, Grown, "Le vieux chêne aux vœux",
            "Ce soir, tout le village accroche des vœux aux branches du vieux chêne. {A} a un bout de papier, un crayon, et beaucoup trop de vœux pour un seul papier.",
            new[]
            {
                Plain("stones", "Souhaiter « beaucoup de cailloux »",
                    "{A} écrit son vœu en lettres bien rondes et l'accroche tout en haut, là où les vœux voient mieux.",
                    E(new FollowUp("grown_wish_stone", 24, 72)), Ai((AiAxis.Greed, 2))),
                Plain("fly", "Souhaiter « voler, une fois »",
                    "{A} écrit « voler, une fois », et souffle sur le papier pour l'aider à partir.",
                    E(new FollowUp("grown_wish_fly", 24, 72)), Ai((AiAxis.Boldness, 1), (AiAxis.Zeal, 1))),
                Plain("blank", "Accrocher le papier sans rien écrire",
                    "{A} accroche le papier blanc. Un vœu qu'on n'écrit pas, on ne peut pas le rater.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Rationality, 1), (AiAxis.Zeal, -1))),
            }),

        new EventDef("grown_wish_stone", EventType.FollowUp, AnyStage, "Beaucoup de cailloux",
            "Au matin, devant la porte de {A} : un caillou. Un seul, mais grand comme la maison. Le vieux chêne a lu le vœu au pied de la lettre.",
            new[]
            {
                Plain("bench", "En faire un banc pour tout le village",
                    "{A} taille, polit, et installe un banc devant la maison. Tout le village vient s'y asseoir. En un sens, c'est beaucoup de cailloux.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Compassion, 1))),
                Try("spend", "Essayer de le dépenser au café", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} pousse le caillou jusqu'au café. La tortue le contemple longuement, puis propose un chocolat chaud par jour, à vie, contre le droit de s'y adosser l'après-midi.",
                    "Le caillou bloque la porte du café pendant trois jours. Les clients passent par la fenêtre.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("trade_sense")), Nothing, Ai((AiAxis.Greed, 1))),
            }),

        new EventDef("grown_wish_fly", EventType.FollowUp, AnyStage, "Voler, une fois",
            "Une cigogne atterrit devant chez {A}, consulte un petit papier, puis {A}. « C'est toi, le vœu ? Monte. Un seul tour. »",
            new[]
            {
                Try("fly", "Monter, et garder les yeux ouverts", new EventChallenge(PlynlingStat.Courage, 8),
                    "Vu d'en haut, le village tient dans une patte. {A} voit le chêne, le pont, le café, et sa propre maison, toute petite. En redescendant, {A} n'a plus tout à fait la même taille, à l'intérieur.",
                    "{A} garde les yeux fermés tout le tour. Une fois en bas, {A} raconte quand même que c'était magnifique, et c'est sûrement vrai.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("inspired")), Nothing, Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Plain("ground", "Remercier, mais rester par terre",
                    "{A} remercie la cigogne et lui offre une tartine à la place. La cigogne repart, un peu déçue, mais rassasiée. Voler, c'est aussi savoir qu'on aurait pu.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("elder_story", EventType.Pulse, Elder, "Raconte, raconte !",
            "Une douzaine de petits du village s'assoient en rond autour de {A}, les yeux brillants. « Raconte quand tu étais {a:petit|petite} ! »",
            new[]
            {
                Try("embellish", "Embellir un peu. Beaucoup.", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} raconte la grande crue. Dans l'histoire, la crue devient un dragon, le dragon une montagne, et {A} sauve tout le monde deux fois. Les petits applaudissent. Au fond, le héron lève un sourcil.",
                    "{A} s'emmêle : le dragon arrive avant la crue, et la montagne parle. Un petit lève la patte : « Ça ne tient pas debout. » Les petits adorent quand même.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("well_spoken")), E(new ApplyModifier("light_heart")),
                    Ai((AiAxis.Zeal, 1), (AiAxis.Sociability, 1)), Stress(("honest", 20))),
                Plain("true", "Raconter la vraie histoire, toute simple",
                    "{A} raconte la première flaque, le premier caillou, la première fois au café. Rien d'extraordinaire. Les petits écoutent sans bouger, parce que c'est vrai.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("doze", "Commencer… et s'endormir au milieu",
                    "{A} commence une histoire de forêt, de nuit et de lanterne, et s'endort au milieu. Les petits inventent la fin eux-mêmes. Leur fin est bien meilleure.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Energy, -2))),
            }),

        new EventDef("elder_glasses", EventType.Pulse, Elder, "Les lunettes de la chouette",
            "Sur les étagères de {A}, les étiquettes des pots ont rapetissé. Toutes. En même temps. La chouette, de passage, pose sur la table une paire de lunettes rondes, sans un mot.",
            new[]
            {
                Plain("wear", "Les essayer",
                    "{A} met les lunettes. Les étiquettes reprennent leur taille normale. Le monde aussi. {A} découvre que le pot « miel » contient de la moutarde depuis trois ans.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2)), Stress(("arrogant", 30))),
                Plain("refuse", "Refuser : les étiquettes ont rapetissé, c'est tout",
                    "{A} rend les lunettes. La chouette les pose un peu plus loin sur la table, au cas où. Le lendemain, {A} les porte, « juste pour voir si les étiquettes sont revenues ».",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, -1))),
                Try("relabel", "Réécrire toutes les étiquettes, en très grand", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} réécrit chaque étiquette en lettres énormes. MIEL. CONFITURE. MOUTARDE (ATTENTION). Les étagères ressemblent à des affiches. La chouette approuve d'un hochement.",
                    "{A} réécrit tout en très grand, sans lunettes. Les étiquettes sont immenses, et toutes fausses.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1)), Stress(("lazy", 20))),
            }),

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

        // ---- wave 5: story cycles (CK3's story_cycles and activities) — longer scenes, three or four
        // steps, with the rewards CK3 hands out: gold (GiveCailloux), artifacts (GiveItem) and relief.
        // bébé: la cabane (bp2 « And Den They Were », playdates)
        new EventDef("baby_den", EventType.Pulse, Baby, "La cabane",
            "Au fond du jardin, entre deux grosses racines, {A} a trouvé l'endroit parfait pour une cabane. Le toit manque, les murs aussi, et à peu près tout le reste. {A} a déjà ramassé trois brindilles et un vieux torchon : c'est un début. {B} passe par là, les mains dans les poches, l'air de rien, et regarde la cabane avec énormément d'intérêt.",
            new[]
            {
                Plain("together", "Proposer à {B} de construire ensemble",
                    "{A} et {B} passent l'après-midi à porter des branches plus grandes qu'eux. Le torchon devient un toit, une feuille de rhubarbe devient une porte, et un caillou plat devient une table. Au coucher du soleil, la cabane tient debout. Un peu de travers, ce qui lui donne du caractère.",
                    E(new AffinityShift(15), new FollowUp("baby_den_storm", 24, 48)), Ai((AiAxis.Sociability, 2))),
                Try("alone", "La construire {a:tout seul|toute seule}, pour la surprise", new EventChallenge(PlynlingStat.Stewardship, 4),
                    "{A} construit, démolit, reconstruit. Le soir, une cabane minuscule et parfaite se dresse entre les racines, avec une fenêtre ronde et une sonnette en coquille de noix. {B} sonne trois fois avant d'oser entrer.",
                    "La cabane s'effondre chaque fois que {A} pose le toit. À la quatrième fois, {B} vient tenir le mur sans rien dire, et le toit tient enfin.",
                    E(new GrowStat(PlynlingStat.Stewardship), new AffinityShift(5), new FollowUp("baby_den_storm", 24, 48)),
                    E(new AffinityShift(10), new FollowUp("baby_den_storm", 24, 48)), Ai((AiAxis.Rationality, 1))),
                Plain("club", "Fonder un club secret, avec un mot de passe",
                    "{A} décrète que la cabane est un club. Le mot de passe est « confiture ». {B} est {b:admis|admise} après un examen très sévère : dire « confiture » sans rire. Ça prend trois essais.",
                    E(new AffinityShift(10), new ApplyModifier("sly"), new FollowUp("baby_den_storm", 24, 48)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1)),
                    gate: new TraitGate("bossy")),
            },
            Target: TargetKind.Anyone,
            TargetCondition: t => t.Affinity >= 0),

        new EventDef("baby_den_storm", EventType.FollowUp, AnyStage, "La nuit de l'orage",
            "Cette nuit, le vent souffle fort et la pluie tape aux carreaux. Au fond du jardin, la cabane affronte l'orage toute seule. {A} n'arrive pas à dormir. Et si le toit s'envolait ? Et si {B} y avait oublié son goûter ?",
            new[]
            {
                Try("rescue", "Courir protéger la cabane, sous la pluie", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} file sous la pluie, coince le torchon sous trois grosses pierres et rentre {a:trempé|trempée} jusqu'aux os, mais {a:fier|fière}. Le lendemain, la cabane est le seul endroit sec du jardin. {B} n'en revient pas.",
                    "{A} glisse dans la boue au deuxième pas et rentre aussitôt, {a:couvert|couverte} de terre du museau aux pattes. Au matin, la cabane a perdu son toit, mais gagné une flaque à l'intérieur, que {B} trouve formidable.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("baby_den_party", 24, 48)), E(new ApplyModifier("muddy_paws"), new FollowUp("baby_den_party", 24, 48)),
                    Ai((AiAxis.Boldness, 2))),
                Plain("trust", "Faire confiance à la cabane, et se rendormir",
                    "{A} décide que la cabane est solide, et se rendort. Au matin, le toit est dans le jardin du voisin et la porte dans l'arbre. Mais les murs tiennent, et les murs, c'est le plus important.",
                    E(new FollowUp("baby_den_party", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            },
            Target: TargetKind.Anyone),

        new EventDef("baby_den_party", EventType.FollowUp, AnyStage, "L'inauguration",
            "La cabane est prête pour sa grande inauguration. {A} a invité {B}, l'escargot et, après une longue hésitation, le héron, qui ne tiendra jamais à l'intérieur. Sur la table en caillou plat attendent trois glands, une fraise coupée en quatre et un ruban à couper.",
            new[]
            {
                Plain("ribbon", "Couper le ruban avec {B}",
                    "{A} et {B} coupent le ruban ensemble, avec les dents, faute de ciseaux. L'escargot applaudit lentement. Le héron passe la tête par la fenêtre et déclare la cabane ouverte. C'est officiel : le village compte un bâtiment de plus.",
                    E(new AffinityShift(10), new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 1))),
                Plain("dig", "Creuser une cave sous la cabane, « pour l'hiver »",
                    "À la troisième pelletée, la pelle heurte quelque chose de rond et de brillant : une bille ancienne, bleue comme un bout de ciel. {B} la déclare trésor officiel du club, et l'escargot vote pour, au bout d'un quart d'heure.",
                    E(new AffinityShift(5), new GiveItem("col.bille")), Ai((AiAxis.Boldness, 1), (AiAxis.Greed, 1))),
                Plain("snack", "Manger les glands et la fraise, puis faire la sieste",
                    "La fête dure exactement le temps de manger trois glands et une fraise. Ensuite, tout le monde fait la sieste dans la cabane, en tas, même l'escargot. Le héron monte la garde dehors, sur une patte.",
                    E(new LiftNeed(Need.Hunger, 0.2), new ApplyModifier("well_rested")), Ai((AiAxis.Greed, 1), (AiAxis.Energy, -1))),
            },
            Target: TargetKind.Anyone),

        // bébé: le loir du tiroir (the pet_animal story cycle: it moves in, brings gifts, then sleeps)
        new EventDef("baby_dormouse", EventType.Pulse, Baby, "Le loir du tiroir",
            "Depuis trois jours, les chaussettes de {A} disparaissent une par une. Ce matin, {A} ouvre le tiroir et découvre la vérité : un loir, roulé en boule dans un nid de chaussettes. Le loir ouvre un œil, bâille, et se rendort sans la moindre gêne.",
            new[]
            {
                Plain("keep", "Le laisser dormir, et lui donner un nom",
                    "{A} referme le tiroir tout doucement, en laissant une fente pour l'air. Le loir s'appelle désormais Chaussette. Chaussette ne réagit pas à son nom, ni à rien d'autre, d'ailleurs. Chaussette dort.",
                    E(new LiftNeed(Need.Happiness, 0.15), new FollowUp("baby_dormouse_gifts", 48, 96)), Ai((AiAxis.Compassion, 2))),
                Try("explain", "Expliquer poliment que c'est un tiroir à chaussettes", new EventChallenge(PlynlingStat.Diplomacy, 4),
                    "{A} explique, longtemps, avec des gestes. Le loir écoute les yeux fermés, puis déménage dans le tiroir du dessous, avec les chaussettes. C'est un compromis. Tout le monde a l'air satisfait, surtout le loir.",
                    "Le loir ronfle pendant toute l'explication. À la fin, {A} range les chaussettes qui restent dans une boîte à chapeau, et laisse le tiroir au loir.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("baby_dormouse_gifts", 48, 96)), E(new FollowUp("baby_dormouse_gifts", 48, 96)),
                    Ai((AiAxis.Rationality, 1))),
                Plain("show", "Porter le tiroir jusqu'au café pour le montrer",
                    "{A} porte le tiroir entier jusqu'au café. Le village défile devant le loir comme devant une vitrine. La tortue déclare n'avoir rien vu d'aussi paisible depuis des années, et le loir ne se réveille pas une seule fois.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("baby_dormouse_gifts", 48, 96)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("baby_dormouse_gifts", EventType.FollowUp, AnyStage, "Les cadeaux du loir",
            "Ce matin, devant le tiroir, {A} trouve un gland, posé bien droit. Le lendemain, un deuxième. Le surlendemain, une plume. Le loir dort toujours, ou fait très bien semblant.",
            new[]
            {
                Plain("trade", "Laisser une noisette en échange",
                    "{A} pose une noisette devant le tiroir. Le soir, la noisette a disparu, et un trèfle attend à sa place. Les échanges durent toute la semaine sans que personne ne voie jamais personne. C'est la plus belle correspondance du village.",
                    E(new GiveItem("col.trefle"), new FollowUp("baby_dormouse_sleep", 72, 120)), Ai((AiAxis.Compassion, 1))),
                Try("watch", "Guetter la nuit, pour le prendre sur le fait", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "Minuit. Le tiroir s'entrouvre. Le loir sort sur la pointe des pattes, un gland dans les bras, le pose devant le lit de {A}, le redresse, recule pour juger de l'effet, et retourne se coucher. {A} n'a jamais rien vu d'aussi sérieux.",
                    "{A} s'endort à onze heures et demie, {a:adossé|adossée} au lit. Au réveil, une plume est posée sur son museau.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveItem("col.gland"), new FollowUp("baby_dormouse_sleep", 72, 120)),
                    E(new GiveItem("col.plume"), new FollowUp("baby_dormouse_sleep", 72, 120)), Ai((AiAxis.Boldness, 1))),
                Plain("box", "Ranger chaque cadeau dans une boîte",
                    "{A} range les cadeaux dans une boîte, par ordre d'arrivée. La boîte se remplit vite. Sur le couvercle, {A} écrit : « Trésor du loir. Ne pas toucher (sauf moi). »",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveItem("col.gland"), new FollowUp("baby_dormouse_sleep", 72, 120)), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("baby_dormouse_sleep", EventType.FollowUp, AnyStage, "La grande sieste",
            "Le loir ne sort plus du tiroir. Ni pour les glands, ni pour les noisettes. {A} soulève la chaussette du dessus : le loir dort, roulé en boule, le museau dans la queue, avec un tout petit sourire. La chouette, consultée, explique qu'un loir peut dormir des mois. C'est ce que les loirs font de mieux.",
            new[]
            {
                Plain("tuck", "Le border avec la chaussette la plus douce",
                    "{A} choisit la chaussette la plus douce, celle à pois, et la pose sur le loir comme une couverture. Le loir soupire dans son sommeil. Chaque soir, avant de se coucher, {A} vérifie le tiroir et dit bonne nuit. Le loir ne répond jamais, mais le tiroir a l'air content.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Compassion, 2))),
                Plain("whisper", "Lui chuchoter de revenir vite",
                    "{A} chuchote au tiroir : « Reviens vite. » Le loir remue une oreille. Quand le loir se réveillera, {A} aura gardé pour lui la meilleure noisette du village, et toutes ses chaussettes, sauf une.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Sociability, 1))),
            }),

        // ado: le grand apprentissage (CK3 childhood education: a guardian, lessons, a final test)
        new EventDef("teen_apprentice", EventType.Pulse, Teen, "Le grand apprentissage",
            "C'est la tradition : à un certain âge, chaque jeune du village choisit un maître pour la saison. Trois portes sont ouvertes à {A}. La chouette de la bibliothèque, qui sait tout et le dit à voix basse. Le hérisson de la gare, qui sait l'heure de tout. Et la pie du marché, qui sait où sont les choses, surtout celles des autres.",
            new[]
            {
                Plain("owl", "Suivre la chouette",
                    "{A} pousse la porte de la bibliothèque. La chouette lève un œil, puis l'autre, et pousse vers {A} une pile de livres plus haute que {A}. « On commence par le premier. Ensuite, on en parle. »",
                    E(new FollowUp("teen_apprentice_owl", 48, 96)), Ai((AiAxis.Rationality, 2))),
                Plain("hedgehog", "Suivre le hérisson",
                    "{A} se présente à la gare. Le hérisson tend une montre, un carnet et un crayon, sans un mot. « Leçon un : on n'est jamais en avance. On est à l'heure, ou on est en retard. » {A} note tout.",
                    E(new FollowUp("teen_apprentice_station", 48, 96)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1))),
                Plain("magpie", "Suivre la pie",
                    "{A} rejoint la pie derrière son tonneau. La pie examine {A} de la tête aux pattes, lui rend un bouton que {A} n'avait pas vu partir, et soupire : « Bon. Tu as tout à apprendre. »",
                    E(new FollowUp("teen_apprentice_market", 48, 96)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1)), Stress(("honest", 10))),
            }),

        new EventDef("teen_apprentice_owl", EventType.FollowUp, AnyStage, "L'examen de la chouette",
            "La saison touche à sa fin. La chouette a préparé un examen : une seule question, écrite à la plume sur un carton. « Que sais-tu, maintenant, que tu ne savais pas avant ? » La chouette attend. Le silence de la bibliothèque attend aussi.",
            new[]
            {
                Try("recite", "Réciter tout ce qui a été appris, dans l'ordre", new EventChallenge(PlynlingStat.Learning, 7),
                    "{A} récite les étoiles, les rivières, les noms latins des fougères et l'année où le vieux pont a été construit. La chouette écoute jusqu'au bout, puis hoche la tête, une seule fois. Venant d'une chouette, c'est une médaille.",
                    "{A} commence par les étoiles et se perd dans les rivières. La chouette lève une aile : « Tu en sais plus que tu ne crois. Reviens l'an prochain, on vérifiera. »",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired"), new FollowUp("grown_master_owl", 168, 240)), E(new FollowUp("grown_master_owl", 168, 240)), Ai((AiAxis.Rationality, 2))),
                Plain("humble", "Répondre : « Que je ne sais presque rien. »",
                    "La chouette ferme les yeux un long moment. Puis la chouette sort de son tiroir une petite clé et la tend à {A} : la clé de la réserve, où dorment les livres que personne n'a le droit de lire. « Maintenant, tu peux commencer. »",
                    E(new GrowStat(PlynlingStat.Learning), new GiveItem("col.cle_rouillee"), new FollowUp("grown_master_owl", 168, 240)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1)), Stress(("arrogant", 20))),
            }),

        new EventDef("teen_apprentice_station", EventType.FollowUp, AnyStage, "La tournée d'inspection",
            "Dernier jour d'apprentissage. Le hérisson confie à {A} la grande tournée : vérifier chaque horloge du village, du café jusqu'au puits, et revenir à la gare avant le train de 17 h 03. Le hérisson ne regarde pas {A} partir. Le hérisson regarde sa montre.",
            new[]
            {
                Try("run", "Faire la tournée au pas de course", new EventChallenge(PlynlingStat.Stewardship, 7),
                    "{A} vérifie onze horloges, en remet trois à l'heure, réveille le coucou du café et pousse la porte de la gare à 17 h 02. Le hérisson range sa montre et tend à {A} une vieille pièce frappée d'une locomotive. « Pour ta première minute d'avance. »",
                    "L'horloge de la tortue retarde de deux heures, par principe. {A} discute, perd du temps, et arrive à 17 h 05. Le hérisson ne dit rien. Le silence est pire qu'un sermon.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveItem("col.piece_ancienne"), new FollowUp("grown_master_station", 168, 240)), E(new FollowUp("grown_master_station", 168, 240)), Ai((AiAxis.Energy, 2)), Stress(("lazy", 20))),
                Plain("plan", "Tracer d'abord le chemin le plus court sur une carte",
                    "{A} passe une heure à tracer l'itinéraire, puis fait la tournée en marchant, sans jamais courir, et arrive à 17 h 03 pile. Le hérisson regarde le plan, longtemps. Le plan est accroché depuis au mur de la gare, sous verre.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("trade_sense"), new FollowUp("grown_master_station", 168, 240)), Ai((AiAxis.Rationality, 2))),
            }),

        new EventDef("teen_apprentice_market", EventType.FollowUp, AnyStage, "La leçon de la pie",
            "Pour la dernière leçon, la pie emmène {A} au marché, le jour de la plus grande foule. « Aujourd'hui, tu me rapportes trois choses. Peu importe quoi. Mais personne ne doit te voir. Et tout doit être revenu à sa place avant ce soir. »",
            new[]
            {
                Try("borrow", "Emprunter, puis tout rendre sans être {a:vu|vue}", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} emprunte une cuillère, un ruban et la casquette du marchand de miel, les montre à la pie, et rend tout avant le soir sans que personne ne remarque rien. La pie fait la révérence, pour la première fois de sa vie, et offre à {A} une bague trouvée « on ne sait où ».",
                    "Le marchand de miel remarque l'absence de sa casquette au moment où {A} la porte sur la tête. Explications. Excuses. Pot de miel acheté pour se faire pardonner. Derrière le tonneau, la pie rit à s'en étouffer.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveItem("col.bague"), new FollowUp("grown_master_market", 168, 240)), E(new FollowUp("grown_master_market", 168, 240)), Ai((AiAxis.Honor, -2)), Stress(("honest", 30), ("just", 20))),
                Plain("refuse", "Refuser : rendre, oui ; prendre, non",
                    "La pie penche la tête. « Bien. C'est la vraie leçon. Tout le monde sait prendre ; les meilleurs savent quand ne pas le faire. » Puis la pie rend à {A}, discrètement, trois boutons perdus depuis le début de la saison.",
                    E(new GiveItem("col.bouton"), new ApplyModifier("clear_conscience"), new FollowUp("grown_master_market", 168, 240)), Ai((AiAxis.Honor, 2))),
                Plain("notes", "Regarder la pie faire, et prendre des notes",
                    "{A} observe la pie toute la journée. Le soir, le carnet contient trois pages de croquis, deux de théories et une liste intitulée « Comment ne plus jamais se faire avoir ». La pie lit la liste, et ajoute une ligne à la fin.",
                    E(new ApplyModifier("sly"), new FollowUp("grown_master_market", 168, 240)), Ai((AiAxis.Rationality, 1))),
            }),

        // ado: les jeux du village (CK3 tournaments: sign up, train, compete)
        new EventDef("teen_games", EventType.Pulse, Teen, "Les jeux du village",
            "Tous les trois ans, le village organise ses grands jeux. Le moineau a sorti son sifflet, l'ours a monté une estrade, et la liste des épreuves est clouée sur le vieux chêne : course en sac, lancer de pomme de pin, concours de grimaces. Une seule inscription par personne. {A} relit la liste pour la cinquième fois.",
            new[]
            {
                Plain("sack", "S'inscrire à la course en sac",
                    "{A} choisit son sac au marché : ni trop grand, ni trop petit, avec une vague odeur de pomme de terre. Toute la semaine, {A} s'entraîne à sauter dans l'allée. Les voisins, à force, ne lèvent même plus la tête.",
                    E(new FollowUp("teen_games_day", 48, 72)), Ai((AiAxis.Energy, 2))),
                Plain("pinecone", "S'inscrire au lancer de pomme de pin",
                    "{A} ramasse trente pommes de pin, les classe par poids, et lance les plus belles contre le mur du jardin jusqu'à la nuit. Le mur, d'abord étonné, a fini par se faire une raison.",
                    E(new FollowUp("teen_games_day", 48, 72)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("faces", "S'inscrire au concours de grimaces",
                    "{A} passe la semaine devant le miroir. Certaines grimaces font peur, d'autres font pitié. Une seule fait rire le miroir lui-même. C'est celle-là.",
                    E(new FollowUp("teen_games_day", 48, 72)), Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("teen_games_day", EventType.FollowUp, AnyStage, "Le jour des jeux",
            "Le grand jour est arrivé. Tout le village s'est installé dans l'herbe, des tartes plein les pattes. Le moineau siffle : c'est au tour de {A}. Devant, l'épreuve. Derrière, tout le village. Et au premier rang, le champion en titre, un écureuil qui n'a jamais perdu.",
            new[]
            {
                Try("all_in", "Tout donner, sans réfléchir", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} s'élance, et pendant quelques secondes, plus rien d'autre n'existe. Quand le bruit revient, c'est celui des applaudissements. L'écureuil, beau joueur, serre la patte de {A}. Le moineau accroche une médaille en bouchon au cou de {A}, et l'ours y ajoute une bourse.",
                    "{A} s'élance trop vite et finit dans la haie, sous les applaudissements, car le village applaudit tout. L'écureuil aide {A} à sortir. Le moineau invente un prix spécial : « Le plus bel élan ».",
                    E(new GrowStat(PlynlingStat.Courage), new GiveCailloux(20)), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("copy", "Observer l'écureuil, et copier sa technique", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} remarque que l'écureuil se gratte l'oreille gauche avant chaque essai. {A} se gratte l'oreille gauche aussi. Ça marche. Personne ne sait pourquoi, l'écureuil non plus. Deuxième place, à une moustache, et une petite bourse quand même.",
                    "{A} se gratte l'oreille gauche, puis la droite pour faire bonne mesure, et perd complètement le fil. Avant-dernière place. Intrigué, l'écureuil se gratte les deux oreilles à son tour.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveCailloux(10)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Honor, -1))),
                Plain("crowd", "Jouer pour le plaisir, en saluant la foule",
                    "{A} fait son épreuve en saluant la foule à chaque pas, sous les acclamations. Classement : septième sur huit. Applaudimètre : premier, de très loin. Le moineau, ému, décerne une mention « Ambiance ».",
                    E(new GrowStat(PlynlingStat.Diplomacy), new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 2))),
            }),

        // adulte et ancien: la carpe dorée (CK3's hunt for a mystical animal: rumour, the dawn, the tale)
        new EventDef("grown_golden_carp", EventType.Pulse, Grown, "La carpe dorée",
            "Au café, le vieux blaireau raconte, pour la centième fois, qu'une carpe dorée vit au fond de l'étang. Une carpe énorme, centenaire, qui ne remonte qu'à l'aube des jours de brume, et qui aurait avalé, dans sa jeunesse, la clé de l'ancienne mairie. Personne ne le croit. Ce matin, {A} n'en est pas si {a:sûr|sûre}.",
            new[]
            {
                Try("inquire", "Interroger les anciens, un par un", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "La tortue l'a vue une fois. Le héron dit ne l'avoir jamais vue, ce qui, chez le héron, veut dire oui. L'escargot a une carte dessinée de mémoire, avec une croix à l'endroit exact. Au prochain matin de brume, {A} saura où regarder.",
                    "Chacun a sa version : la carpe est dorée, argentée, invisible, ou c'est un caillou. {A} rentre avec quatre légendes et aucune piste, mais l'envie d'y aller quand même.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_golden_carp_dawn", 24, 72)), E(new FollowUp("grown_golden_carp_dawn", 24, 72)),
                    Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("watch", "Partir guetter, dès le prochain matin de brume",
                    "{A} prépare un thermos, une couverture et un carnet. Le blaireau regarde les préparatifs par-dessus sa tasse et marmonne quelque chose qui ressemble beaucoup à « enfin quelqu'un ».",
                    E(new FollowUp("grown_golden_carp_dawn", 24, 72)), Ai((AiAxis.Boldness, 1))),
                Plain("doubt", "Hausser les épaules : une carpe reste une carpe",
                    "{A} hausse les épaules et commande une autre tartine. Le blaireau soupire. Le soir même, en passant près de l'étang, {A} croit voir un reflet doré sous la surface. Sûrement la lune.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2)), Stress(("zealous", 20))),
            }),

        new EventDef("grown_golden_carp_dawn", EventType.FollowUp, AnyStage, "Le matin de brume",
            "L'aube se lève sur l'étang, et la brume est si épaisse qu'on ne voit plus l'autre rive. {A} attend sur la berge depuis une heure, une couverture sur les épaules. Puis l'eau se ride. Une forme immense remonte, lentement, et une écaille d'or perce la brume.",
            new[]
            {
                Try("still", "Ne plus bouger, ne plus respirer", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} ne bouge pas. La carpe remonte jusqu'à deux pattes de la berge et regarde {A} d'un œil rond, vieux comme l'étang. Puis la carpe ouvre la bouche, dépose sur la vase quelque chose de rond et de terni, et replonge : une pièce ancienne.",
                    "{A} éternue. La carpe disparaît dans un remous d'or, et la brume se referme. Sur l'eau, un cercle s'élargit, puis plus rien. Mais {A} l'a vue. Pour de vrai.",
                    E(new GrowStat(PlynlingStat.Courage), new GiveItem("col.piece_ancienne"), new FollowUp("grown_golden_carp_tale", 24, 48)),
                    E(new FollowUp("grown_golden_carp_tale", 24, 48)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, -1))),
                Plain("offer", "Lui lancer la moitié de sa tartine",
                    "{A} lance la moitié de sa tartine. La carpe remonte, l'avale d'une bouchée, et reste un moment à la surface, comme pour remercier. Puis la carpe replonge, et l'étang redevient un étang. {A} rentre avec un secret, et l'autre moitié de la tartine.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_golden_carp_tale", 24, 48)), Ai((AiAxis.Compassion, 2))),
                Try("sketch", "La dessiner dans le carnet, vite", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} dessine à toute vitesse : les écailles, la nageoire déchirée, l'œil. C'est le plus beau dessin que {A} ait jamais fait. Au café, le blaireau le regarde longtemps, puis l'accroche au-dessus du comptoir, sans un mot.",
                    "Le temps d'ouvrir le carnet, la carpe est repartie. Le dessin ressemble à une saucisse avec des yeux. {A} le garde quand même.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired"), new FollowUp("grown_golden_carp_tale", 24, 48)),
                    E(new FollowUp("grown_golden_carp_tale", 24, 48)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_golden_carp_tale", EventType.FollowUp, AnyStage, "Ce qu'on raconte au café",
            "Au café, tout le monde veut savoir. Le blaireau a gardé la meilleure place pour {A}, et la tortue a servi le chocolat avant même la commande. Toute la salle se tait quand {A} s'assoit.",
            new[]
            {
                Try("tell", "Tout raconter, avec les détails", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} raconte la brume, l'œil, l'écaille d'or. Le café retient son souffle jusqu'au bout. Le blaireau, pour la première fois depuis des années, n'a rien à ajouter. Le soir même, la légende a un nouveau chapitre, et {A} un surnom : « {a:celui|celle} qui a vu la carpe ».",
                    "{A} raconte trop vite, mélange la brume et la tartine, et la carpe finit par avoir des moustaches. Le café rit beaucoup. Plus personne ne sait ce qui est vrai, ce qui, pour une légende, est très bon signe.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("well_spoken")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2))),
                Plain("secret", "Garder le secret de la carpe",
                    "{A} sourit et ne dit rien. « Une carpe ? Quelle carpe ? » Le blaireau plisse les yeux, puis sourit à son tour : le blaireau a compris. Certaines légendes vivent mieux quand on ne les raconte pas.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
            }),

        // adulte et ancien: le mystère des cuillères (CK3's court mystery story cycle, with nobody hurt)
        new EventDef("grown_spoons", EventType.Pulse, Grown, "Le mystère des cuillères",
            "Depuis une semaine, les cuillères disparaissent. Une chez la tortue, deux chez l'ours, la petite cuillère à confiture de la chouette. Ce matin, c'est la cuillère préférée de {A} qui manque à l'appel. Au café, tout le monde soupçonne tout le monde, et le hérisson a commencé à cacher la sienne dans sa chaussette.",
            new[]
            {
                Try("map", "Mener l'enquête, carnet en main", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} note chaque disparition sur une carte du village. Les croix dessinent une ligne bien droite, qui part des maisons et file vers la rivière. Les cuillères ne s'envolent pas : quelqu'un les emporte, toujours dans la même direction.",
                    "{A} interroge tout le village et ne récolte que des soupçons. La pie a un alibi ; l'escargot aussi, très lent. Mais en rentrant, {A} trouve devant sa porte une piste de petites traces mouillées.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_spoons_river", 24, 48)), E(new FollowUp("grown_spoons_river", 24, 48)),
                    Ai((AiAxis.Rationality, 2))),
                Try("bait", "Laisser une cuillère en appât, et guetter", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} pose une cuillère brillante sur le rebord de la fenêtre et se cache derrière le rideau. À minuit, une patte mouillée l'attrape et file. {A} a tout vu : quelque chose de brun, de souple, qui sent la rivière.",
                    "{A} s'endort derrière le rideau. Au matin, l'appât a disparu. Le rideau aussi.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_spoons_river", 24, 48)), E(new FollowUp("grown_spoons_river", 24, 48)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("fork", "Manger sa soupe à la fourchette, en attendant",
                    "{A} décide que ce n'est pas si grave. Manger de la soupe à la fourchette prend du temps, mais apprend la patience. Le village, de son côté, continue à s'accuser mutuellement, avec de plus en plus d'imagination.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1))),
            }),

        new EventDef("grown_spoons_river", EventType.FollowUp, AnyStage, "Sous les saules",
            "La piste mène au bord de la rivière, sous les saules. De là monte un son étrange : un tintement léger, comme des cuillères qui se cognent doucement dans le vent. Entre les branches, une jeune loutre est assise au milieu d'un tas de cuillères. En voyant {A}, la loutre sursaute.",
            new[]
            {
                Plain("ask", "S'asseoir, et demander pourquoi",
                    "La loutre explique, tout bas. La nuit, sous les saules, le vent fait peur. Alors la loutre a fabriqué un carillon, pour que la nuit fasse de la musique au lieu de faire peur. Le carillon est magnifique, mais incomplet : une cuillère manque encore.",
                    E(new FollowUp("grown_spoons_end", 24, 48)), Ai((AiAxis.Compassion, 2))),
                Try("firm", "Exiger que tout soit rendu", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} parle fermement, sans crier. La loutre baisse les oreilles et promet de tout rendre avant demain. Puis, tout bas, la loutre parle du carillon, et de la peur du vent. La fermeté de {A} fond un peu.",
                    "La loutre plonge et disparaît avec la cuillère préférée de {A}. Le lendemain, la cuillère est revenue sur le paillasson, propre, avec un petit mot : « Pardon. »",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_spoons_end", 24, 48)), E(new FollowUp("grown_spoons_end", 24, 48)),
                    Ai((AiAxis.Honor, 1), (AiAxis.Compassion, -1)), Stress(("compassionate", 20))),
                Plain("help", "Aider à finir le carillon",
                    "{A} s'assoit et aide à accrocher les dernières cuillères. Le soir tombe. Le vent se lève, et sous les saules, la nuit se met à tinter doucement, comme une berceuse. La loutre s'endort au milieu d'une phrase.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_spoons_end", 24, 48)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_spoons_end", EventType.FollowUp, AnyStage, "Le carillon des saules",
            "Le village a fini par apprendre la vérité sur les cuillères. Ce soir, tout le monde est descendu sous les saules : la tortue, l'ours, la chouette, et la jeune loutre, toute petite au milieu de la foule, qui n'ose pas lever les yeux. Tout le monde attend de voir ce que {A} va dire.",
            new[]
            {
                Plain("give", "Accrocher sa propre cuillère au carillon",
                    "{A} sort sa cuillère de sa poche et l'accroche au carillon. Un silence. Puis la tortue accroche la sienne. Puis l'ours, puis la chouette. Au bout d'une heure, le carillon des saules compte une cuillère par habitant, et la loutre pleure de joie dans les bras de l'ours.",
                    E(new ApplyModifier("clear_conscience"), new LiftNeed(Need.Happiness, 0.25)), Ai((AiAxis.Compassion, 2), (AiAxis.Sociability, 1))),
                Plain("bells", "Rendre les cuillères, et offrir des clochettes à la place",
                    "{A} rend chaque cuillère à sa maison, et revient avec un sac de clochettes achetées au marché. Le nouveau carillon sonne plus clair que l'ancien. Pour dire merci, la loutre offre à {A} un caillou plat, poli par la rivière.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveItem("col.caillou_plat")), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Plain("verdict", "Laisser le village décider",
                    "Le village délibère longuement, au café. Verdict : la loutre gardera le carillon, mais viendra laver la vaisselle du café chaque dimanche, en échange des cuillères. La loutre accepte avec enthousiasme. La tortue n'a jamais eu une vaisselle aussi propre.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Rationality, 1))),
            }),

        // adulte et ancien: le grand banquet (CK3's feast activity: the preparations, an incident, the toast)
        new EventDef("grown_feast", EventType.Pulse, Grown, "Le grand banquet",
            "Voilà des années que personne n'a organisé de vrai banquet au village. {A} vient de décider que ça suffit. Une grande table sous le chêne, des lanternes, de la confiture, des gâteaux, et tout le monde invité. Reste un détail : tout préparer, en trois jours.",
            new[]
            {
                Try("grand", "Voir grand : des lanternes partout, trois desserts", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} court du marché au café, du café au jardin. Trois jours plus tard, la table croule sous les plats, les lanternes pendent à chaque branche, et l'ours a promis de jouer de l'accordéon. Tout est prêt, à une cuillère près.",
                    "Le premier dessert brûle, le deuxième s'effondre, le troisième est mangé par l'escargot pendant la nuit. Mais les lanternes sont magnifiques, et le banquet aura lieu quand même.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_feast_evening", 24, 48)), E(new FollowUp("grown_feast_evening", 24, 48)),
                    Ai((AiAxis.Energy, 2), (AiAxis.Greed, -1)), Stress(("lazy", 20))),
                Plain("potluck", "Demander à chacun d'apporter un plat",
                    "{A} passe dans chaque maison avec une liste. La tortue apportera ses tartelettes, l'ours ses beignets, la chouette « quelque chose de surprenant ». {A} n'a plus qu'à mettre la table, et à s'inquiéter du « surprenant ».",
                    E(new FollowUp("grown_feast_evening", 24, 48)), Ai((AiAxis.Sociability, 2))),
                Plain("small", "Un petit banquet, juste les plus proches",
                    "{A} dresse une petite table pour six, avec une nappe à carreaux et une seule lanterne. Ce ne sera pas le banquet du siècle. Ce sera peut-être mieux.",
                    E(new FollowUp("grown_feast_evening", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, -1))),
            }),

        new EventDef("grown_feast_evening", EventType.FollowUp, AnyStage, "Le soir du banquet",
            "Les lanternes s'allument une à une. Les invités arrivent, les plats circulent, les rires montent sous le chêne. Puis, au milieu du repas, l'ours renverse le grand pot de soupe, en plein sur la nappe. Silence. Tous les regards se tournent vers {A}.",
            new[]
            {
                Plain("slide", "Éclater de rire, et glisser sur la soupe",
                    "{A} éclate de rire et glisse sur la soupe jusqu'au bout de la table. Une seconde plus tard, tout le village glisse, l'ours le premier. On parlera longtemps du « banquet de la soupe », et toujours en riant.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_feast_toast", 2, 4)), Ai((AiAxis.Sociability, 2), (AiAxis.Rationality, -1))),
                Try("save", "Sauver la soirée : nappe propre, plat de réserve", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} change la nappe en trois gestes, sort le plat de réserve et lance une chanson pour couvrir le bruit. Personne n'a presque rien vu. L'ours, soulagé, glisse à {A} un pot de son miel secret.",
                    "{A} tire sur la nappe trop fort, et le reste du repas rejoint la soupe. Long silence. Puis la chouette déclare que c'était « surprenant », et c'est le mot de la soirée.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("hearty"), new FollowUp("grown_feast_toast", 2, 4)), E(new FollowUp("grown_feast_toast", 2, 4)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Compassion, 1))),
                Plain("comfort", "Aller consoler l'ours",
                    "{A} laisse la soupe et va s'asseoir à côté de l'ours, qui se cache derrière ses grosses pattes. « Une soupe, ça se refait. » L'ours renifle, se redresse, et le banquet reprend, avec un ours encore un peu rouge et très, très reconnaissant.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_feast_toast", 2, 4)), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_feast_toast", EventType.FollowUp, AnyStage, "Le toast",
            "La nuit est bien avancée. Les lanternes fatiguent, les assiettes sont vides, et quelqu'un réclame un discours. Puis tout le monde. {A} se lève, un verre de jus de pomme à la patte. Le chêne entier se tait.",
            new[]
            {
                Try("speech", "Faire un grand discours", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} parle du village, du chêne, de la soupe, et de chacun, par son nom. À la fin, l'ours se mouche bruyamment et la tortue applaudit avec une rapidité inédite. Le banquet est déclaré le meilleur depuis toujours, et les invités, en partant, laissent une petite bourse sur la table, pour le prochain.",
                    "{A} commence un discours, oublie la suite, et finit par dire : « Voilà. » La salle applaudit quand même, très fort, parce que c'était sincère.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new GiveCailloux(15)), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2)), Stress(("shy", 20))),
                Plain("short", "Lever son verre : « À nous. »",
                    "« À nous. » C'est tout. Le chêne entier répète « À nous ! », et les verres s'entrechoquent jusqu'à l'aube. Les discours les plus courts sont ceux dont on se souvient.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Rationality, 1))),
                Plain("thanks", "Remercier chaque invité, un par un",
                    "{A} fait le tour de la table et remercie chacun, avec un mot pour chaque plat, même le « surprenant ». Ça prend une heure. Personne ne trouve le temps long. En partant, la chouette glisse dans la poche de {A} une plume, « pour écrire le prochain menu ».",
                    E(new GiveItem("col.plume"), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        // ancien: voir la mer (CK3 pilgrimages and travel events)
        new EventDef("elder_sea", EventType.Pulse, Elder, "Voir la mer",
            "Toute une vie au village, et {A} n'a jamais vu la mer. La cigogne en parle comme d'un étang qui n'aurait pas de bout. Ce matin, {A} a préparé un baluchon : une écharpe, trois tartines, une carte dessinée par le héron. La route est longue, les pattes sont vieilles. Mais la décision est prise.",
            new[]
            {
                Plain("alone", "Partir {a:seul|seule}, à son rythme",
                    "{A} part avant l'aube, sans prévenir, une tartine à la patte. Sur le vieux pont, le héron regarde passer le baluchon et ne dit rien. C'est sa plus belle façon de dire bon voyage.",
                    E(new FollowUp("elder_sea_road", 24, 48)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, -1))),
                Plain("stork", "Demander à la cigogne de montrer le chemin",
                    "La cigogne accepte, à condition de voler devant et d'attendre à chaque croisement. Le voyage sera lent : la cigogne s'arrête aussi pour raconter des histoires, et chaque histoire a une suite.",
                    E(new FollowUp("elder_sea_road", 24, 48)), Ai((AiAxis.Sociability, 1))),
                Plain("snail", "Emmener l'escargot, qui n'a jamais voyagé non plus",
                    "L'escargot fait son baluchon en une minute : l'escargot est son propre baluchon. Les deux voyageurs partent côte à côte, à la vitesse de l'escargot. Personne n'est pressé. C'est tout l'intérêt.",
                    E(new FollowUp("elder_sea_road", 24, 48)), Ai((AiAxis.Compassion, 1), (AiAxis.Energy, -1))),
            }),

        new EventDef("elder_sea_road", EventType.FollowUp, AnyStage, "Sur la route",
            "Le troisième jour, la route traverse une lande battue par le vent. Au bord du chemin, une jeune cane pleure, assise sur sa valise : partie trop tard, et toute sa bande a déjà filé vers la mer. La nuit tombe, et le vent forcit.",
            new[]
            {
                Plain("shelter", "Partager l'abri d'un rocher, et la dernière tartine",
                    "{A} trouve un rocher creux, partage la dernière tartine et raconte des histoires du village jusqu'à ce que la cane s'endorme. Au matin, le vent est tombé. La cane marche à côté de {A}, et ne pleure plus du tout.",
                    E(new ApplyModifier("cherished"), new FollowUp("elder_sea_shore", 24, 48)), Ai((AiAxis.Compassion, 2))),
                Try("night", "Marcher de nuit, pour rattraper la bande", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} marche toute la nuit, la cane sur le dos. À l'aube, au sommet d'une dune, la bande est là, endormie. La cane court la rejoindre, puis revient serrer {A} très fort. Derrière la dune monte un bruit immense et doux : la mer.",
                    "La nuit est trop noire et les pattes trop vieilles. {A} et la cane dorment sous un buisson, à l'abri du vent. Au matin, la bande est revenue les chercher.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("elder_sea_shore", 12, 24)), E(new FollowUp("elder_sea_shore", 24, 48)), Ai((AiAxis.Boldness, 2))),
                Try("geese", "Héler les oies qui passent, pour demander de l'aide", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} agite l'écharpe. Une escadrille d'oies descend, écoute l'histoire, et repart avec la cane, en promettant de retrouver sa bande avant midi. La dernière oie, en partant, laisse tomber une plume aux pattes de {A}.",
                    "Les oies passent trop haut et n'entendent rien. {A} et la cane continuent ensemble, à pied, et la cane apprend en chemin trois chansons du village.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new GiveItem("col.plume"), new FollowUp("elder_sea_shore", 24, 48)), E(new FollowUp("elder_sea_shore", 24, 48)),
                    Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("elder_sea_shore", EventType.FollowUp, AnyStage, "La mer",
            "Au sommet de la dernière dune, {A} s'arrête. Devant, la mer : immense, grise et bleue, qui respire. Le vent sent le sel. Les vagues arrivent une par une, comme si l'océan venait saluer {A} en personne. La cigogne avait raison : un étang qui n'a pas de bout.",
            new[]
            {
                Plain("waves", "Tremper les pattes dans les vagues",
                    "{A} descend jusqu'à l'eau et laisse la première vague mouiller ses vieilles pattes. C'est froid. C'est salé. C'est exactement comme dans les histoires, en beaucoup plus grand. {A} reste là jusqu'au soir à compter les vagues, perd le compte, et recommence.",
                    E(new LiftNeed(Need.Happiness, 0.3), new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1))),
                Plain("pebble", "Choisir un galet à rapporter au village",
                    "{A} cherche longtemps, et choisit un galet tout rond, poli par des milliers de vagues. Au retour, le galet passera de patte en patte au café, et chacun, en le tenant, entendra un peu la mer.",
                    E(new GiveItem("col.galet"), new ApplyModifier("inspired")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("sit", "S'asseoir dans le sable, et ne plus penser à rien",
                    "{A} s'assoit dans le sable tiède et, pour la première fois depuis très longtemps, ne pense plus à rien. Le soleil se couche dans l'eau. Au village, plus tard, {A} dira simplement : « C'était grand. » Et ce sera tout, et ce sera assez.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Rationality, 1))),
            }),

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

        // adulte et ancien: les lettres du vieux pont (CK3's courting scheme; ends in the existing declaration)
        new EventDef("grown_letters", EventType.Pulse, Grown, "Les lettres du vieux pont",
            "Depuis des semaines, {A} pense à {B}. Le matin, le soir, et beaucoup entre les deux. Le dire en face semble impossible. Mais sous le vieux pont, entre deux pierres, se cache une petite boîte aux lettres que le village connaît depuis toujours. On y laisse des mots sans signature. Le héron les garde, sans jamais les lire.",
            new[]
            {
                Try("poem", "Écrire un poème, le plus beau possible", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} écrit douze vers sur les yeux de {B}, le vent, et le chocolat de la tortue. Le poème est glissé sous le pont, adressé à {B}. Le lendemain, la boîte contient une réponse, un seul mot : « Encore ? »",
                    "{A} écrit, rature, recommence, et finit par glisser sous le pont une lettre qui ne parle que de la météo. Le lendemain, la boîte contient une réponse : « Oui, très beau temps. Et toi ? »",
                    E(new GrowStat(PlynlingStat.Learning), new AffinityShift(10), new FollowUp("grown_letters_more", 24, 72)), E(new AffinityShift(5), new FollowUp("grown_letters_more", 24, 72)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Compassion, 1))),
                Plain("simple", "Écrire simplement : « Je pense à toi. »",
                    "{A} écrit trois mots, sans signature, et les glisse sous le pont, adressés à {B}. Trois jours passent. Puis, dans la boîte, une réponse : « Moi aussi, je pense à quelqu'un. » Ce n'est pas un oui. Ce n'est pas un non.",
                    E(new AffinityShift(5), new FollowUp("grown_letters_more", 24, 72)), Ai((AiAxis.Honor, 1))),
                Plain("drawer", "Garder ça pour soi, encore un peu",
                    "{A} prend une feuille, la regarde longtemps, puis la range dans un tiroir. Certaines lettres ont besoin de mûrir. Sous le pont, le héron attend : le héron a l'habitude.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.CanCouple),

        new EventDef("grown_letters_more", EventType.FollowUp, AnyStage, "La correspondance",
            "Depuis deux semaines, les lettres vont et viennent sous le vieux pont. On s'y raconte la journée, les nuages, une chanson entendue au café. {B} signe toujours d'un petit dessin de feuille. Ce matin, la dernière lettre se termine par une question : « Qui es-tu ? »",
            new[]
            {
                Try("meet", "Répondre : « Demain, au vieux pont, à l'aube. »", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} écrit l'heure et le lieu, et ajoute un petit dessin de feuille, en réponse. Le lendemain à l'aube, {B} est sur le pont, une écharpe autour du cou, et sourit en voyant arriver {A}. Le héron, très discret, regarde ailleurs avec une grande concentration.",
                    "La pluie mouille l'encre de l'invitation. Le lendemain, {B} attend au pont… à midi. {A}, à l'aube. Les deux se ratent de six heures, et le comprennent le soir, en riant chacun de son côté.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(15), new FollowUp("grown_letters_bridge", 24, 48)), E(new AffinityShift(5), new FollowUp("grown_letters_bridge", 24, 48)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("riddle", "Répondre par une devinette",
                    "{A} répond par une devinette : « Je prends mon chocolat avec trois sucres et je déteste les lundis. » Le lendemain, {B} a trouvé. La réponse dans la boîte dit seulement : « Je savais. »",
                    E(new AffinityShift(10), new FollowUp("grown_letters_bridge", 24, 48)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_letters_bridge", EventType.FollowUp, AnyStage, "Sur le vieux pont",
            "Le soleil se lève sur le vieux pont. {A} et {B} sont là, côte à côte, à regarder la rivière. Toutes les lettres sont dans la poche de {A}, et une bonne partie dans celle de {B}. Le moment est venu de dire tout haut ce qui a été écrit tout bas.",
            new[]
            {
                Ask("declare", "Tout avouer à {B}, tout haut",
                    "{A} prend une grande inspiration, se tourne vers {B}, et dit enfin, à voix haute, tout ce que les lettres disaient.", "reply_declare",
                    Ai((AiAxis.Boldness, 2), (AiAxis.Sociability, 1))),
                Plain("keep", "Proposer de garder les lettres, et rester amis",
                    "{A} sort les lettres et propose de les garder dans une boîte, à deux, en souvenir. {B} sourit, un peu {b:surpris|surprise}, et accepte. Certaines histoires sont plus belles à mi-chemin.",
                    E(new AffinityShift(10)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        // adulte et ancien: le juge d'un jour (CK3's hold court activity: three petitions)
        new EventDef("grown_judge", EventType.Pulse, Grown, "Le juge d'un jour",
            "Le maire du village, un vieux castor, a attrapé un rhume. Quelqu'un doit tenir l'audience du samedi, et le village a désigné {A}, à l'unanimité moins une voix (celle de {A}). Sous le chêne, la première affaire arrive déjà, en se disputant : l'ours accuse le hérisson de siffler ses horaires à l'aube sous sa fenêtre ; le hérisson accuse l'ours de ronfler si fort que les trains en tremblent.",
            new[]
            {
                Try("compromise", "Trouver un compromis", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} propose : le hérisson sifflera ses horaires à voix basse, et l'ours dormira la fenêtre fermée, avec un bonnet. Les deux réfléchissent, grognent, et se serrent la patte. Le village applaudit. Affaire suivante.",
                    "Le compromis ne plaît à personne. L'ours ronfle de vexation sur place, le hérisson siffle de protestation. {A} déclare l'affaire « en délibéré » et passe à la suivante, très vite.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_judge_tree", 1, 3)), E(new FollowUp("grown_judge_tree", 1, 3)),
                    Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Plain("rules", "Appliquer le règlement, à la lettre",
                    "{A} ouvre le vieux règlement du village. Article douze : « Nul ne sifflera avant le chant du coq. » Article treize : « Nul ne ronflera au-delà du raisonnable. » Les deux sont en tort, et paient chacun un pot de miel au café. La tortue est ravie.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_judge_tree", 1, 3)), Ai((AiAxis.Honor, 2))),
                Plain("swap", "Proposer un échange de maisons, pour une nuit",
                    "L'ours dort à la gare, le hérisson chez l'ours. Le lendemain, les deux reviennent avec des cernes : la gare est trop bruyante, la maison de l'ours trop silencieuse. Chacun comprend un peu mieux l'autre.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_judge_tree", 1, 3)), Ai((AiAxis.Rationality, -1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("grown_judge_tree", EventType.FollowUp, AnyStage, "La deuxième affaire",
            "Deuxième affaire. Deux écureuils se disputent un noisetier qui pousse exactement entre leurs deux jardins. Chacun jure l'avoir planté. Le noisetier a l'air de s'en moquer complètement. Sous le chêne, le public retient son souffle.",
            new[]
            {
                Plain("split", "Partager : une branche sur deux",
                    "{A} décrète que le noisetier sera partagé, une branche sur deux. Les écureuils comptent les branches : onze. Un long silence. Puis {A} attribue la onzième aux oiseaux. Tout le monde trouve ça juste, sauf les oiseaux, qui n'ont rien demandé.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_judge_snail", 1, 3)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Try("truth", "Chercher la vérité : qui l'a vraiment planté ?", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} examine les racines, les vieux registres et la mémoire de la tortue. Verdict : personne ne l'a planté. Un geai a oublié une noisette, voilà vingt ans. Le noisetier appartient donc au geai, qui ne réclame rien. Vaincus par la logique, les deux écureuils se partagent la récolte.",
                    "{A} enquête, et découvre que les deux écureuils disent vrai : chacun a planté une noisette, au même endroit, le même jour. Plus personne n'y comprend rien. L'affaire est classée sous le titre « Mystère ».",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_judge_snail", 1, 3)), E(new FollowUp("grown_judge_snail", 1, 3)), Ai((AiAxis.Rationality, 2))),
                Plain("harvest", "Organiser une grande récolte commune",
                    "{A} propose une récolte commune, avec goûter. Les deux écureuils grimpent, cueillent, et finissent par se lancer des noisettes en riant. À la fin, plus personne ne se souvient de qui a planté quoi.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_judge_snail", 1, 3)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_judge_snail", EventType.FollowUp, AnyStage, "La dernière affaire",
            "La dernière affaire est la plus délicate. L'escargot porte plainte contre le village tout entier, pour excès de vitesse. Selon l'escargot, tout le monde va beaucoup trop vite, tout le temps, et c'est épuisant à regarder. Le public est debout. Depuis sa fenêtre, le maire éternue d'impatience.",
            new[]
            {
                Plain("slow_hour", "Instaurer une heure lente, chaque jour",
                    "{A} décrète que, dès demain, de quatre à cinq heures, tout le village ira au rythme de l'escargot. Le premier jour est une catastrophe. Le deuxième, un peu moins. Au bout d'une semaine, l'heure lente est devenue le moment préféré de tout le monde.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Try("dismiss", "Débouter l'escargot, avec beaucoup de tact", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} explique, avec mille précautions, que le village ne peut pas ralentir. Mais offre à l'escargot une place d'honneur au café, d'où l'on voit tout passer sans avoir à suivre. L'escargot accepte. L'audience est levée sous les applaudissements, et le maire envoie, depuis son lit, une petite bourse « pour le dérangement ».",
                    "L'escargot se vexe et quitte l'audience, très lentement. La sortie dure vingt minutes, dans un silence gêné. Le maire envoie quand même une petite bourse, avec un mot : « Courage. »",
                    E(new GiveCailloux(15), new ApplyModifier("well_spoken")), E(new GiveCailloux(5)), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, -1))),
                Plain("recuse", "Se déclarer trop rapide pour être impartial",
                    "{A} se lève et déclare, très solennellement, ne pas pouvoir juger cette affaire : {A} marche beaucoup trop vite. L'escargot apprécie l'honnêteté. L'audience est levée. Le village rentre chez soi, plus lentement que d'habitude.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
            }),

        // adulte et ancien: la chronique du village (CK3's commissioned epics and legacies)
        new EventDef("grown_chronicle", EventType.Pulse, Grown, "La chronique du village",
            "À la bibliothèque, la chouette a ressorti le vieux livre de la chronique du village. La dernière page écrite date de l'année où le pont a gelé. Depuis, plus personne n'a rien noté. « Un village qui n'écrit pas son histoire finit par l'oublier », dit la chouette, en tendant la plume à {A}.",
            new[]
            {
                Plain("elders", "Accepter, et commencer par interroger les anciens",
                    "{A} prend la plume, un carnet, et fait le tour des anciens : la tortue, le blaireau, le hérisson. Chacun a une histoire, et chacun raconte celle des autres un peu de travers. Le carnet se remplit vite.",
                    E(new FollowUp("grown_chronicle_tarts", 24, 72)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Try("memory", "Accepter, et tout écrire de mémoire", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} s'installe à la bibliothèque et écrit d'une traite : la grande partie de balle, la course d'escargots, le banquet de la soupe. Le style est vif, les détails justes. La chouette lit par-dessus l'épaule de {A} et ne corrige que deux virgules.",
                    "{A} écrit trois pages, puis s'aperçoit que la course d'escargots se déroule avant la naissance de l'escargot. Tout est à refaire, avec l'aide des anciens.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_chronicle_tarts", 24, 72)), E(new FollowUp("grown_chronicle_tarts", 24, 72)),
                    Ai((AiAxis.Rationality, 2))),
                Plain("decline", "Refuser : trop de responsabilité",
                    "{A} rend la plume. La chouette ne dit rien, range le livre, et le pose bien en vue, sur la plus haute étagère. Chaque fois que {A} passe à la bibliothèque, le livre est là. Le livre attend.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        new EventDef("grown_chronicle_tarts", EventType.FollowUp, AnyStage, "Ce qu'on écrit, ce qu'on tait",
            "La chronique avance. Mais en relisant ses notes, {A} tombe sur une histoire que quelqu'un n'a pas envie de voir écrite : l'année où l'ours a mangé, seul, en une nuit, toutes les tartes de la fête des moissons. L'ours a supplié qu'on n'en parle pas. Le reste du village en parle encore.",
            new[]
            {
                Plain("write", "Tout écrire : la chronique doit être vraie",
                    "{A} écrit l'histoire des tartes, avec tous les détails. La chronique y gagne en vérité. L'ours, en lisant, rougit jusqu'aux oreilles, puis éclate de rire : après tout, c'étaient de très bonnes tartes.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_chronicle_reading", 24, 48)), Ai((AiAxis.Honor, 2)), Stress(("compassionate", 10))),
                Try("tender", "L'écrire, mais avec tendresse", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} écrit : « Cette année-là, l'ours aima tant les tartes de la fête qu'aucune ne fut laissée de côté. » L'ours lit la phrase trois fois et demande une copie, pour l'encadrer.",
                    "La tendresse se voit trop. Le village lit entre les lignes, et l'histoire des tartes devient encore plus célèbre qu'avant. L'ours soupire.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_chronicle_reading", 24, 48)), E(new FollowUp("grown_chronicle_reading", 24, 48)),
                    Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("skip", "Laisser cette histoire de côté",
                    "{A} tourne la page. Certaines histoires appartiennent à ceux qui les ont vécues. L'ours, qui l'apprend, dépose le lendemain devant la porte de {A} une tarte. Entière.",
                    E(new LiftNeed(Need.Hunger, 0.2), new FollowUp("grown_chronicle_reading", 24, 48)), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_chronicle_reading", EventType.FollowUp, AnyStage, "La première lecture",
            "Ce soir, la chouette a organisé la première lecture publique de la nouvelle chronique. La bibliothèque est pleine, jusque dans les escaliers. Les anciens sont au premier rang. {A} monte sur l'estrade, le livre dans les pattes, et ouvre la première page.",
            new[]
            {
                Try("read", "Lire avec le ton, les voix, tout", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} lit et fait toutes les voix : la tortue en très lent, le hérisson en très exact. La salle rit, pleure un peu, et applaudit longtemps. En rangeant la chronique, la chouette glisse dans la poche de {A} une vieille carte, trouvée dans la reliure.",
                    "{A} lit trop vite, saute une page et fait s'effondrer le vieux pont deux fois. La salle rit beaucoup. Au premier rang, la chouette prend des notes pour la deuxième édition.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new GiveItem("col.carte_tresor")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2)), Stress(("shy", 20))),
                Plain("tortoise", "Laisser la tortue lire à sa place",
                    "{A} tend le livre à la tortue. La tortue lit, très lentement, pendant trois heures. Personne ne part. À la fin, la tortue referme le livre et dit simplement : « C'est bien. C'est nous. »",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("shelf", "Poser la chronique sur l'étagère, sans discours",
                    "{A} pose la chronique sur l'étagère, sans un mot, et s'en va. Le lendemain, la file d'attente pour l'emprunter fait le tour de la bibliothèque. La chouette doit instaurer des tickets.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("clear_conscience")), Ai((AiAxis.Rationality, 1))),
            }),

        // adulte et ancien: le vieux pont (CK3's great projects and building investments)
        new EventDef("grown_bridge", EventType.Pulse, Grown, "La fissure du vieux pont",
            "Ce matin, une fissure est apparue sur le vieux pont. Une grande, qui part du milieu et file vers la rive. Le héron, qui vit dessous depuis toujours, l'a regardée toute la journée sans bouger. Le maire parle de « faire quelque chose », ce qui, chez le maire, veut dire ne rien faire. {A} n'a pas l'intention d'attendre.",
            new[]
            {
                Try("organize", "Organiser les travaux, avec tout le village", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} dresse un plan, répartit les tâches et fixe le début des travaux au samedi. L'ours portera les pierres, le hérisson tiendra le planning, la pie fournira les clous, sans dire d'où.",
                    "Le plan est parfait, mais le premier samedi tombe le jour de la course d'escargots, et personne ne vient. Les travaux sont repoussés d'une semaine.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_bridge_box", 48, 96)), E(new FollowUp("grown_bridge_box", 72, 120)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1)), Stress(("lazy", 20))),
                Plain("collect", "Faire une collecte au café",
                    "{A} pose un bocal sur le comptoir du café, avec une étiquette : « Pour le pont. » Le bocal se remplit vite : chacun a une histoire avec le vieux pont. Le héron, sans un mot, y dépose un caillou très ancien.",
                    E(new FollowUp("grown_bridge_box", 48, 96)), Ai((AiAxis.Sociability, 2))),
                Plain("heron", "Demander d'abord l'avis du héron",
                    "{A} s'assoit à côté du héron, sous le pont. Après un très long silence, le héron parle : du pont, de la rivière, d'avant le village. Puis, tout bas : « Répare-le. Mais garde la fissure. Ce pont a vécu. » {A} comprend.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_bridge_box", 48, 96)), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_bridge_box", EventType.FollowUp, AnyStage, "Le chantier",
            "Le chantier dure depuis une semaine. Les pierres sont posées, le mortier sèche, et le village s'est habitué à passer par le gué. Mais ce matin, en retirant la vieille pierre centrale, l'ours a trouvé quelque chose : une petite boîte en fer, rouillée, scellée, coincée dans les fondations depuis des générations.",
            new[]
            {
                Try("open", "Ouvrir la boîte, devant tout le monde", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} force le couvercle. Dedans : une boussole en cuivre, une lettre à moitié effacée, et un dessin du pont tout neuf, signé par les premiers bâtisseurs. Le village entier se penche. La boussole indique toujours le nord, et la lettre dit simplement : « Prenez-en soin. »",
                    "Le couvercle résiste, puis cède d'un coup, et tout le contenu tombe dans la rivière. Une lettre file au fil de l'eau. Seul un vieux bouton est sauvé, de justesse, par le héron.",
                    E(new GiveItem("col.boussole"), new FollowUp("grown_bridge_open", 48, 96)), E(new GiveItem("col.bouton"), new FollowUp("grown_bridge_open", 48, 96)),
                    Ai((AiAxis.Boldness, 1))),
                Plain("reseal", "La remettre dans les fondations, avec un mot de plus",
                    "{A} glisse dans la boîte un mot pour les suivants : la date, les noms de tous ceux qui ont aidé, et un dessin du héron. Puis la boîte retourne dans les fondations, sous la nouvelle pierre. Dans cent ans, quelqu'un la trouvera.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_bridge_open", 48, 96)), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, 1))),
            }),

        new EventDef("grown_bridge_open", EventType.FollowUp, AnyStage, "L'inauguration du pont",
            "Le pont est terminé. Les pierres neuves brillent à côté des anciennes, et, au milieu, la vieille fissure a été gardée, rebouchée de mortier doré. Tout le village est là, sur les deux rives. Le maire, guéri, a préparé un discours de quarante minutes. Sous le pont, le héron attend.",
            new[]
            {
                Plain("heron", "Laisser le héron traverser le premier",
                    "{A} interrompt le discours du maire et s'écarte. Le héron monte sur le pont, très lentement, sur une patte, puis sur l'autre, et le traverse d'un bout à l'autre, dans un silence complet. Arrivé de l'autre côté, le héron s'incline. Le village explose en applaudissements.",
                    E(new LiftNeed(Need.Happiness, 0.25)), Ai((AiAxis.Compassion, 2))),
                Try("speech", "Faire un discours de deux phrases", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "« Ce pont a vécu, et nous aussi. Merci à tous. » Deux phrases. Le maire, dont le discours en comptait cent vingt, applaudit plus fort que tout le monde, et remet à {A} ce qui reste de la bourse des travaux.",
                    "{A} commence ses deux phrases, puis en ajoute une troisième, puis une dixième. Le maire, ravi, prend la suite. L'inauguration dure jusqu'au soir.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new GiveCailloux(20)), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Sociability, 1)), Stress(("shy", 20))),
                Plain("run", "Traverser en courant, pour tester",
                    "{A} traverse le pont en courant, aller-retour, puis saute à pieds joints au milieu, sur la fissure dorée. Le pont ne bouge pas d'un millimètre. Le village applaudit, le héron soupire, et l'ours, rassuré, traverse à son tour.",
                    E(new ApplyModifier("fired_up")), Ai((AiAxis.Boldness, 2))),
            }),

        // ancien: le dernier élève (CK3's mentor and student relation events)
        new EventDef("elder_student", EventType.Pulse, Elder, "Le dernier élève",
            "Un petit du village vient chaque jour s'asseoir devant la maison de {A}, sans rien dire, à regarder. Aujourd'hui, le petit se décide enfin : « Tu veux bien m'apprendre ce que tu sais ? » Les pattes de {A} sont vieilles, mais la tête est pleine, et cette question, {A} l'attendait sans le savoir.",
            new[]
            {
                Plain("garden", "Lui apprendre le jardin",
                    "{A} et le petit commencent par le potager : semer, arroser, attendre, surtout attendre. Le petit est impatient. {A} aussi l'était, autrefois. Les premières pousses sortent au bout d'une semaine, et le petit danse autour.",
                    E(new FollowUp("elder_student_grows", 48, 96)), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Plain("stories", "Lui apprendre les histoires du village",
                    "{A} raconte : le pont, la crue, la grande partie de balle d'autrefois. Le petit écoute, répète, se trompe, recommence. Bientôt, le petit raconte les histoires mieux que {A}, avec des détails en plus.",
                    E(new FollowUp("elder_student_grows", 48, 96)), Ai((AiAxis.Sociability, 1))),
                Try("patience", "Lui apprendre la patience, la plus difficile des leçons", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Première leçon : regarder l'escargot traverser la place. En entier. Le petit tient dix minutes, puis vingt, puis l'heure entière. À la fin, le petit dit : « L'escargot a des rayures. Je ne l'avais jamais vu. » {A} sourit : c'est exactement la leçon.",
                    "Le petit tient trois minutes avant de courir après l'escargot pour l'aider à avancer. L'escargot, vexé, rebrousse chemin. {A} rit si fort que la leçon est reportée.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("elder_student_grows", 48, 96)), E(new FollowUp("elder_student_grows", 48, 96)),
                    Ai((AiAxis.Rationality, 2))),
            }),

        new EventDef("elder_student_grows", EventType.FollowUp, AnyStage, "Le petit qui grandit",
            "Les saisons passent. Le petit a grandi d'une tête. Ce matin, pour la première fois, c'est le petit qui montre quelque chose à {A} : une façon de faire que {A} ne connaissait pas, plus rapide, plus maligne. Le petit attend, un peu inquiet, ce que {A} va en dire.",
            new[]
            {
                Plain("praise", "Le féliciter, et apprendre à son tour",
                    "{A} essaie la nouvelle façon, la rate, recommence, et réussit. « Tu m'as appris quelque chose. » Le petit devient tout rouge de fierté. Le soir, {A} y repense longtemps : le plus beau cadeau d'un élève, c'est de dépasser son maître.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("cherished"), new FollowUp("elder_student_leaves", 72, 120)), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1)),
                    Stress(("arrogant", 20))),
                Plain("old_way", "Rappeler gentiment que l'ancienne méthode a fait ses preuves",
                    "{A} montre à nouveau l'ancienne méthode, lentement, en expliquant pourquoi. Le petit écoute, hoche la tête, et garde les deux méthodes. Un jour, le petit choisira. C'est aussi ça, apprendre.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("elder_student_leaves", 72, 120)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("elder_student_leaves", EventType.FollowUp, AnyStage, "Le départ du petit",
            "Le petit, qui n'est plus si petit, frappe à la porte de {A} avec un baluchon. Une grande école, loin, au-delà de la rivière, a accepté le petit. Le départ est demain matin. Le petit tient quelque chose, caché derrière le dos.",
            new[]
            {
                Plain("notebook", "Lui offrir ce qu'on a de plus précieux",
                    "{A} va chercher dans le tiroir un vieux carnet plein de notes, de dessins et de taches de confiture : tout ce que {A} a appris, depuis toujours. Le petit le serre contre son cœur. Puis sort de derrière son dos un trèfle à quatre feuilles, trouvé, jure le petit, après trois jours de recherches.",
                    E(new GiveItem("col.trefle_quatre"), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
                Plain("walk", "L'accompagner jusqu'au vieux pont",
                    "{A} accompagne le petit jusqu'au vieux pont, à petits pas. Sur le pont, le petit se retourne, fait de grands signes et crie : « Je reviendrai te montrer ! » {A} reste sur la rive jusqu'à ce que le baluchon ne soit plus qu'un point. Le héron, à côté, reste aussi.",
                    E(new LiftNeed(Need.Happiness, 0.2), new ApplyModifier("soothed")), Ai((AiAxis.Sociability, 1))),
            }),

        // ==== wave 8 (CK3: playdates, festivals, court guests, the imperial examination, the chariot race,
        // the party baron, natural disasters, adult education, debates; echoes are long follow-ups that land
        // in adulte: bébé and ado last a week each)

        // ---- wave 8: bébé
        new EventDef("baby_swing", EventType.Pulse, Baby, "La balançoire",
            "Sur la place, la balançoire est prise. Un jeune blaireau s'y balance depuis le matin et crie à chaque passage : « Encore dix ! » On en est au quarantième « encore dix ». {A} attend, les pattes croisées.",
            new[]
            {
                Try("push", "Proposer de pousser le blaireau, très haut", new EventChallenge(PlynlingStat.Diplomacy, 4),
                    "{A} pousse, le blaireau monte, crie de joie, et au retour propose de lui-même d'échanger. Depuis, les deux se poussent à tour de rôle, en comptant jusqu'à dix pour de vrai.",
                    "{A} pousse trop fort. Le blaireau atterrit dans le bac à sable, ravi, et réclame qu'on recommence. La balançoire reste prise.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Try("stand", "Se planter devant, et réclamer son tour", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} se plante devant la balançoire, les poings sur les hanches. Le blaireau ralentit, réfléchit, et descend : « Bon. Dix. Pas un de plus. » {A} en fait onze, par principe.",
                    "{A} se plante devant, mais un peu trop près. La balançoire repasse. {A} aussi, dans l'autre sens, sur les fesses.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing, Ai((AiAxis.Boldness, 2))),
                Plain("rope", "Trouver mieux : une corde et une branche basse",
                    "{A} noue une corde à une branche basse et invente sa propre balançoire, qui tourne, en plus. Le soir, le blaireau fait la queue derrière.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("baby_bath", EventType.Pulse, Baby, "L'heure du bain",
            "C'est l'heure du bain. {A} a disparu. Une bassine d'eau tiède attend au milieu de la pièce, et sous le lit, deux yeux brillent dans le noir, très décidés.",
            new[]
            {
                Plain("ducks", "Accepter, à condition d'emmener un canard en bois",
                    "{A} entre dans l'eau avec un canard en bois, puis un deuxième, puis une flotte entière. Le bain dure une heure et finit en bataille navale. Le plancher est trempé ; {A} brille comme un sou neuf.",
                    E(new LiftNeed(Need.Hygiene, 0.3)), Ai((AiAxis.Energy, 1), (AiAxis.Sociability, 1))),
                Try("escape", "Filer par la fenêtre", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} file par la fenêtre, traverse le jardin et plonge dans le tas de feuilles. Personne ne trouve {A} avant le dîner. Les feuilles, en revanche, ont trouvé un locataire.",
                    "{A} file par la fenêtre et atterrit pile dans le tonneau de pluie. Le bain a eu lieu quand même, en plus froid.",
                    E(new GrowStat(PlynlingStat.Intrigue)), E(new LiftNeed(Need.Hygiene, 0.2)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
                Plain("bubbles", "Étudier les bulles, très sérieusement",
                    "{A} passe le bain à observer les bulles : les grosses montent plus vite, les petites restent en bande. À la fin, {A} est propre sans s'en être {a:aperçu|aperçue}.",
                    E(new GrowStat(PlynlingStat.Learning), new LiftNeed(Need.Hygiene, 0.2)), Ai((AiAxis.Rationality, 2)), gate: new TraitGate("curious")),
            }),

        new EventDef("baby_dress_up", EventType.Pulse, Baby, "Le petit chef de gare",
            "{A} a trouvé une casquette trop grande, une pomme de pin en guise de montre, et s'est {a:installé|installée} sur le quai. Le hérisson chef de gare observe la scène depuis son guichet, sans un mot. Le train de 10 h 12 arrive dans trois minutes.",
            new[]
            {
                Try("whistle", "Siffler le départ, comme un vrai chef", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} siffle. Le train part. Pile à l'heure. Le hérisson sort de son guichet, regarde sa montre, regarde {A}, et note quelque chose dans son carnet. On ne saura jamais quoi, mais c'était souligné.",
                    "{A} siffle trop tôt. Le train ne part pas. Les voyageurs applaudissent quand même. Le hérisson soupire, puis siffle à son tour, un peu moins fort, pour ne vexer personne.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing, Ai((AiAxis.Boldness, 2))),
                Plain("tickets", "Vérifier les billets de tous les voyageurs",
                    "{A} poinçonne les billets avec les dents. L'ours a payé deux fois ; l'escargot n'a pas de billet, mais un abonnement d'avant le pont. Le hérisson, impressionné, laisse {A} tenir le carnet jusqu'au soir.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Plain("ask", "Demander au hérisson comment on devient chef",
                    "Le hérisson réfléchit longuement. « On arrive avant tout le monde. On part après tout le monde. Et on ne court jamais. » {A} répète la phrase toute la journée, en marchant très lentement.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1))),
                Plain("orders", "Donner des ordres à tout le quai",
                    "{A} ordonne aux voyageurs de monter, aux pigeons de descendre et au train d'attendre. Le train n'attend pas. Mais les pigeons, bizarrement, obéissent.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Boldness, 2)), gate: new TraitGate("bossy")),
            }),

        new EventDef("baby_imaginary", EventType.Pulse, Baby, "L'ami invisible",
            "Depuis mardi, {A} a un nouvel ami : Plouf. Plouf est invisible, mange beaucoup de biscuits, et a toujours une place à table. Ce matin, Plouf a une idée, et {A} est {a:seul|seule} à le savoir.",
            new[]
            {
                Plain("cafe", "Commander un chocolat pour Plouf au café",
                    "{A} commande deux chocolats. La tortue en sert deux, sans poser de questions, et essuie la tasse de Plouf avec le même soin que l'autre. Plouf, paraît-il, a tout bu.",
                    E(new LiftNeed(Need.Happiness, 0.15), new FollowUp("baby_imaginary_bye", 72, 120)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("blame", "Accuser Plouf pour les biscuits disparus",
                    "La boîte à biscuits est vide. « C'est Plouf. » Le héron, consulté, confirme que Plouf avait l'air coupable. {A} s'essuie la bouche, discrètement.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("baby_imaginary_bye", 72, 120)), Ai((AiAxis.Honor, -1), (AiAxis.Greed, 1))),
                Plain("explore", "Partir avec Plouf explorer le fond du jardin",
                    "Plouf connaît le chemin. Plouf a peur des limaces, alors {A} doit être {a:courageux|courageuse} pour deux. Le soir, {A} raconte l'expédition : Plouf a été très bien.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("baby_imaginary_bye", 72, 120)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("baby_imaginary_bye", EventType.FollowUp, AnyStage, "Au revoir, Plouf",
            "Ce matin, {A} met la table et s'arrête, une tasse à la patte. Plouf n'est pas là. {A} cherche sous le lit, derrière la porte, dans la boîte à biscuits. Puis comprend, sans trop savoir comment, que Plouf est parti voir un autre petit, quelque part, qui en a plus besoin.",
            new[]
            {
                Plain("letter", "Écrire une lettre à Plouf",
                    "{A} écrit une lettre de trois mots, avec un dessin de biscuit, et la confie au héron, qui promet de la porter « au bon endroit ». Le héron la range sous son aile, et l'y garde encore.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1))),
                Plain("chair", "Laisser sa chaise libre, au cas où",
                    "{A} laisse la chaise de Plouf libre. Un jour, quelqu'un s'y assoit : un vrai ami, cette fois, qui demande la permission de rester.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 1))),
                Plain("grown", "Annoncer qu'on est trop grand pour ces choses-là",
                    "{A} annonce à tout le village qu'on est désormais trop {a:grand|grande} pour ce genre de choses. Le soir, en se couchant, {A} dit quand même bonne nuit au plafond. Juste au cas où.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("baby_otter", EventType.Pulse, Baby, "La loutre du ponton",
            "Chaque soir, une vieille loutre s'assoit au bout du ponton et regarde l'eau, sans bouger, jusqu'à la nuit. Personne ne sait ce que la loutre attend. Ce soir, {A} s'approche, à petits pas.",
            new[]
            {
                Plain("sit", "S'asseoir à côté, sans rien dire",
                    "{A} s'assoit. La loutre ne dit rien. {A} non plus. Au bout d'un long moment, la loutre pose une patte sur la tête de {A}, juste une seconde. Puis les deux regardent l'eau jusqu'à la nuit.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 2))),
                Try("ask", "Demander ce qu'on attend, au bout d'un ponton", new EventChallenge(PlynlingStat.Diplomacy, 4),
                    "« Un bateau à voile rouge, parti quand j'avais ton âge. » La loutre sourit. « Je sais bien que le bateau ne reviendra pas. Mais j'aime l'heure où on l'attend. »",
                    "La loutre ne répond pas, mais tend à {A} un caillou plat. {A} le lance : trois ricochets. La loutre hoche la tête, et c'est tout pour ce soir.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), E(new GiveItem("col.caillou_plat")), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("boat", "Fabriquer un petit bateau, avec une voile rouge",
                    "{A} plie une feuille, la colore au jus de framboise, et pose le bateau sur l'eau, devant la loutre. La loutre le regarde partir, très loin. Ce soir-là, pour la première fois, la loutre rentre avant la nuit.",
                    E(new GrowStat(PlynlingStat.Stewardship), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
            }),

        // ---- wave 8: ado
        new EventDef("teen_fox_wedding", EventType.Pulse, Teen, "La noce des renards",
            "Grand soleil, et pourtant la pluie tombe. Au village, tout le monde sait ce que ça veut dire : les renards se marient. La noce traverse le bois en ce moment même, et la tradition est claire : personne ne doit regarder. {A} entend déjà les clochettes, juste derrière les fougères.",
            new[]
            {
                Try("peek", "Écarter les fougères, juste un peu", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} écarte deux fougères. Un long cortège de renards passe sous la pluie dorée, en silence, des clochettes aux oreilles. La mariée tourne la tête, regarde {A} droit dans les yeux, et sourit. {A} ne racontera jamais ça à personne.",
                    "Une fougère craque. Le cortège s'arrête net. Puis un vieux renard sort du rang, pose une part de gâteau de noce sur une souche, devant {A}, et repart. Personne ne sait si c'était une punition.",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("inspired")), E(new LiftNeed(Need.Hunger, 0.2)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
                Plain("count", "Fermer les yeux et compter jusqu'à cent",
                    "{A} ferme les yeux et compte. À quatre-vingt-dix, les clochettes s'éloignent. À cent, la pluie s'arrête. Sur la souche, devant {A}, quelqu'un a laissé une châtaigne bien ronde.",
                    E(new GiveItem("col.chataigne"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("flowers", "Laisser un bouquet au bord du chemin, sans regarder",
                    "{A} cueille trois fleurs mouillées et les pose au bord du chemin, les yeux fermés. Le lendemain, le bouquet a disparu. À sa place attend une plume rousse.",
                    E(new GiveItem("col.plume"), new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("teen_tug_of_war", EventType.Pulse, Teen, "La corde de la rivière",
            "Chaque année, le village et celui d'en face se disputent la rivière au tir à la corde, d'une rive à l'autre. Les perdants finissent dans l'eau. Cette année, le moineau a désigné {A} pour l'équipe. Sur l'autre rive, l'équipe adverse compte un castor. Un vrai.",
            new[]
            {
                Try("front", "Prendre la tête de la corde", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} tient bon, les pattes plantées dans la boue, et crie le rythme. Le castor glisse, puis toute l'équipe adverse, un par un, dans la rivière. Le village porte {A} en triomphe jusqu'au café, où l'ours offre la tournée et une petite bourse.",
                    "{A} tient bon, très bon, trop bon : quand l'équipe lâche, {A} part {a:seul|seule} dans la rivière. Le castor aide {A} à sortir, ce qui est humiliant, puis tend une serviette, ce qui l'est encore plus.",
                    E(new GrowStat(PlynlingStat.Courage), new GiveCailloux(15)), E(new LiftNeed(Need.Hygiene, 0.2)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("anchor", "S'enrouler la corde autour de la taille, en dernier", new EventChallenge(PlynlingStat.Stewardship, 7),
                    "{A} s'enroule la corde autour de la taille et s'assoit. Tout simplement. L'équipe adverse tire, tire, et ne comprend pas. La victoire doit plus au poids qu'à la force, mais une victoire est une victoire.",
                    "{A} s'enroule la corde trois fois autour de la taille, se retrouve {a:ficelé|ficelée} comme un saucisson, puis {a:traîné|traînée} doucement jusqu'au bord de l'eau. On s'arrête juste avant. Le castor, beau joueur, déclare le match nul.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(10)), Nothing, Ai((AiAxis.Rationality, 2))),
                Try("butter", "Beurrer discrètement le bout de corde adverse", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "Le bout adverse glisse comme une anguille. Victoire éclatante. Le soir, le castor renifle sa patte, et regarde {A} longuement. Pas un mot. Ça viendra.",
                    "Le beurre a coulé du mauvais côté. C'est l'équipe de {A} qui glisse dans la rivière, en entier. Personne ne sait d'où vient le beurre. Presque personne.",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("sly")), Nothing, Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1)), Stress(("honest", 30), ("just", 20))),
                Plain("song", "Rester sur la berge, et mener les chants",
                    "{A} invente une chanson sur le castor, qui rime avec « pas si fort », et tout le village la reprend. Le castor, vexé, perd sa concentration. Le moineau se demande si une chanson compte comme une aide extérieure, et décide que non.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("teen_runaway", EventType.Pulse, Teen, "Partir pour toujours",
            "Ce matin, {A} a décidé de quitter le village. Pour toujours. Le baluchon est prêt : deux tartines, un caillou porte-bonheur, une chaussette de rechange. La lettre d'adieu est punaisée sur la porte. Sur son pont, le héron regarde {A} approcher avec beaucoup d'intérêt.",
            new[]
            {
                Plain("hill", "Partir loin, jusqu'au bout du chemin",
                    "{A} marche jusqu'au bout du chemin, puis un peu plus loin, jusqu'à la colline d'où l'on voit tout le village, minuscule. Les tartines sont mangées là-haut. Puis le village a l'air de faire signe, et {A} rentre pour le dîner, avec une nouvelle façon de le regarder.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
                Try("raft", "Construire un radeau, et descendre la rivière", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "Le radeau flotte. Le radeau avance. Le radeau, après un dernier virage, accoste… au ponton du village, par l'autre côté. {A} débarque sous les applaudissements de l'escargot, qui croyait à une course.",
                    "Le radeau flotte trois secondes. {A} rentre à la nage, le baluchon sur la tête, et décrète que partir pour toujours peut attendre l'été.",
                    E(new GrowStat(PlynlingStat.Stewardship)), E(new LiftNeed(Need.Hygiene, 0.15)), Ai((AiAxis.Energy, 1), (AiAxis.Rationality, 1))),
                Plain("heron", "Demander d'abord conseil au héron",
                    "« Partir pour toujours, dit le héron, ça se prépare. Reviens demain. » {A} revient le lendemain, puis le surlendemain. Au bout d'une semaine, {A} a oublié de partir, mais connaît par cœur tous les oiseaux migrateurs.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("teen_crush", EventType.Pulse, Teen, "Un nom dans la marge",
            "Depuis quelque temps, {A} rougit quand {B} passe. {A} a écrit le nom de {B} dans la marge d'un livre de la bibliothèque, puis l'a effacé, puis l'a réécrit, plus petit. La chouette a remarqué. La chouette remarque tout.",
            new[]
            {
                Plain("flower", "Laisser une fleur devant la porte de {B}, sans signer",
                    "{A} dépose la fleur à l'aube et s'enfuit. {B} la trouve, la met dans un verre d'eau, et demande à tout le village qui l'a laissée. Personne ne sait. {A}, {a:interrogé|interrogée}, rougit jusqu'aux oreilles et parle très vite de la météo.",
                    E(new AffinityShift(10)), Ai((AiAxis.Compassion, 1), (AiAxis.Boldness, -1))),
                Try("talk", "Aller parler à {B}, de n'importe quoi", new EventChallenge(PlynlingStat.Courage, 6),
                    "{A} s'approche et parle des nuages pendant dix minutes. {B} écoute, répond, rit au bon moment. Ce n'est pas une déclaration. C'est mieux : c'est une conversation.",
                    "{A} s'approche, ouvre la bouche, et dit « bonjour la météo ». {B} cligne des yeux. {A} s'en va très vite, très dignement, dans la mauvaise direction.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(15)), E(new AffinityShift(5)), Ai((AiAxis.Boldness, 2)), Stress(("shy", 20))),
                Plain("owl", "Demander à la chouette un livre sur la question",
                    "La chouette pose devant {A} un recueil de poèmes, un seul, avec un marque-page déjà glissé à la bonne page. {A} lit le poème trois fois. Personne ne saura jamais lequel.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => !t.EitherInCouple),

        // ---- wave 8: adulte et ancien
        new EventDef("grown_moon_party", EventType.Pulse, Grown, "La fête de la lune",
            "{B} a glissé une invitation sous la porte de {A} : « Pleine lune, sur la colline. Apporte une couverture et une histoire. » Ce soir, la lune est énorme, posée sur la colline comme une tarte sur une table. {B} a déjà étendu une nappe, et attend.",
            new[]
            {
                Plain("story", "Apporter la plus belle histoire qu'on connaisse",
                    "{A} raconte l'histoire du lapin qui vit sur la lune et y fait des confitures. {B} jure voir le lapin, là, à gauche. Les deux restent jusqu'à ce que la lune ait traversé tout le ciel.",
                    E(new AffinityShift(15), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("tea", "Apporter une théière, deux tasses et du miel",
                    "Le thé fume dans le froid. {B} boit sa tasse en silence, puis dit : « C'est la première fois que quelqu'un vient. » {A} ressert du thé, sans rien dire. Rien d'autre à dire.",
                    E(new AffinityShift(20), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 2))),
                Try("poem", "Improviser un poème à la lune", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} improvise douze vers à la lune. {B} applaudit, puis en improvise douze autres, moins bons, mais avec beaucoup plus de rimes en « -ette ». C'est la meilleure soirée du mois.",
                    "{A} commence un poème et s'arrête au troisième vers, faute de rime pour « lune ». {B} propose « prune ». Le poème devient une recette. Personne ne s'en plaint.",
                    E(new GrowStat(PlynlingStat.Learning), new AffinityShift(15)), E(new AffinityShift(10)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1)), Stress(("shy", 15))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_spilled", EventType.Pulse, Grown, "La soupe renversée",
            "Au banquet du marché, {B} arrive avec un bol de soupe de potiron, trébuche sur une racine, et le renverse en entier sur le plus beau pull de {A}. Le marché se tait. Le moineau suspend sa fourchette en l'air. Tout le monde attend de voir ce que {A} va faire.",
            new[]
            {
                Try("laugh", "Éclater de rire, et en reprendre un bol", new EventChallenge(PlynlingStat.Diplomacy, 7),
                    "{A} éclate de rire, cueille un peu de soupe sur sa manche, la goûte, et la déclare excellente. Le marché rit aussi. {B}, rouge comme le potiron, apporte un deuxième bol, en marchant très, très prudemment.",
                    "{A} rit, mais un peu trop longtemps, d'un rire de plus en plus aigu. {B} ne sait plus si c'est grave. Personne ne le sait. Le moineau repose sa fourchette.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(20)), E(new AffinityShift(5)), Ai((AiAxis.Sociability, 2)), Stress(("wrathful", 20))),
                Plain("fashion", "Annoncer que c'est la nouvelle mode",
                    "« C'est voulu », annonce {A}. « Soupe sur laine. Très en vogue au-delà de la rivière. » Le lendemain, deux jeunes du village portent un pull taché de potiron. {B} n'en revient pas.",
                    E(new AffinityShift(10), new ApplyModifier("well_spoken")), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("sulk", "Rentrer se changer, sans un mot",
                    "{A} se lève et rentre se changer, sans un mot, avec une dignité de héron. {B} passe la soirée à fixer son bol vide. Le lendemain, un pull neuf, tricoté de travers, attend devant la porte de {A}.",
                    E(new AffinityShift(-5)), Ai((AiAxis.Vengefulness, 1), (AiAxis.Sociability, -1)), Stress(("forgiving", 15))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_empty_village", EventType.Pulse, Grown, "Le village vide",
            "Minuit. {A} a une envie terrible de tartine au miel, mais le pot est vide. Le café est fermé, la boulangerie aussi. Chez l'ours, chez le blaireau, chez la tortue : personne. Le village entier a disparu. Seule une lanterne brille au loin, du côté du moulin.",
            new[]
            {
                Try("follow", "Suivre la lanterne, sans bruit", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} se faufile jusqu'au moulin et colle un œil à la fenêtre : tout le village est là, en pyjama, autour d'un gâteau à cent bougies. C'est l'anniversaire de l'escargot. Personne ne sait qui a oublié d'inviter {A}. {A} entre, et on fait comme si de rien n'était, avec une part en plus.",
                    "{A} se faufile, marche sur une branche, et la porte du moulin s'ouvre d'un coup : « SURPRISE ! » La surprise n'est pas pour {A}, mais pour l'escargot, qui arrive juste derrière. L'escargot est très touché qu'on ait crié pour {A} aussi.",
                    E(new GrowStat(PlynlingStat.Intrigue), new LiftNeed(Need.Hunger, 0.2)), E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("bed", "Retourner se coucher, le ventre vide",
                    "{A} retourne se coucher et rêve de tartines. Au matin, devant la porte : une part de gâteau, une bougie plantée dedans, et un mot de l'escargot. « On t'a {a:cherché|cherchée} partout. »",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Energy, -1))),
                Plain("bake", "Inventer une tartine avec ce qui reste",
                    "Pas de miel, mais un fond de confiture de mûres, trois noisettes et une idée. La tartine de minuit de {A} est si réussie que, le lendemain, la tortue en veut la recette. {A} ne s'en souvient plus du tout.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("hearty")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_hare", EventType.Pulse, Grown, "Le voyageur sans bagage",
            "Un lièvre frappe à la porte de {A} à la nuit tombée. Le manteau est usé, les poches vides, les oreilles pleines de poussière de route. « Je n'ai rien pour payer, dit le lièvre. Seulement des histoires. Des vraies. Presque toutes. »",
            new[]
            {
                Plain("host", "Ouvrir sa porte, et écouter les histoires",
                    "Le lièvre raconte les montagnes, la mer, une ville où les maisons flottent. À l'aube, le lièvre repart, et laisse sur la table une vieille carte dessinée à la main, avec une croix tout au bout.",
                    E(new GiveItem("col.carte_tresor")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Try("deal", "Proposer un marché : le couvert contre un peu de travail", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "Le lièvre répare la gouttière, le volet et la chaise bancale avant le petit-déjeuner, en racontant une histoire par clou. En partant, le lièvre glisse à {A} quelques cailloux trouvés « sur une route d'argent ».",
                    "Le lièvre accepte, répare la chaise, et casse la table. Les deux finissent la nuit à rire, assis par terre, et le lièvre explique comment on mange sans table, dans le désert.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(15)), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1)), Stress(("generous", 15))),
                Plain("proof", "Demander une preuve, une seule",
                    "« Une preuve ? » Le lièvre sourit, retire son chapeau, et en sort un coquillage qui sent encore la mer. {A} le porte à son oreille : la mer est bien là, toute petite, au fond. Le lièvre reprend le coquillage. « Celui-là, je le garde. »",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2))),
            }),

        new EventDef("grown_clock_feud", EventType.Pulse, Grown, "La guerre des horloges",
            "L'horloge du café retarde de deux heures depuis toujours. La tortue dit que c'est voulu. Le hérisson chef de gare dit que c'est une honte. Ce matin, la dispute a éclaté sur la place, devant tout le monde, et les deux se tournent vers {A} en même temps : « Dis-lui, toi ! »",
            new[]
            {
                Try("mediate", "Proposer un compromis : une heure de retard seulement", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Après une longue négociation, l'horloge du café ne retarde plus que d'une heure, et la tortue offre au hérisson un chocolat chaque jour, à l'heure exacte de la gare. Les deux font semblant d'avoir gagné. C'est le signe d'un bon compromis.",
                    "Le compromis ne plaît à personne. La tortue et le hérisson, enfin d'accord sur quelque chose, se retournent ensemble contre {A}. {A} paie les chocolats.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("well_spoken")), Nothing, Ai((AiAxis.Sociability, 1), (AiAxis.Honor, 1))),
                Try("fix", "Régler l'horloge soi-même, en pleine nuit", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} règle l'horloge à minuit. Au matin, le café est à l'heure. La tortue arrive en avance pour la première fois de sa vie, trouve la porte fermée, et s'assoit sur le banc pour attendre. La tortue trouve ça très reposant. On ne touche plus à l'horloge.",
                    "{A} ouvre l'horloge. Un coucou en sort, très en colère, et refuse d'y retourner. Le café a désormais un coucou de comptoir, qui chante quand ça lui chante.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
                Plain("both", "Donner raison aux deux, à tour de rôle",
                    "{A} donne raison à la tortue le matin et au hérisson l'après-midi. Ça marche trois jours. Le quatrième, les deux comparent leurs notes. {A} prend des vacances au fond du jardin.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Honor, -1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_lost_letter", EventType.Pulse, Grown, "La lettre égarée",
            "Le facteur, un pigeon distrait, a glissé dans la boîte de {A} une lettre adressée à {B}. L'enveloppe est mal fermée. Très mal fermée. Un souffle suffirait à l'ouvrir, et on distingue déjà, en travers, les mots « ne le dis à personne ».",
            new[]
            {
                Plain("deliver", "Porter la lettre à {B}, sans l'ouvrir",
                    "{A} traverse le village, la lettre tenue du bout des doigts comme une braise. {B} l'ouvre devant {A}, lit, et éclate de rire : c'est la recette secrète de la tarte du blaireau. {B} promet une part. La promesse est tenue.",
                    E(new AffinityShift(10), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Try("read", "Lire juste la première ligne", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} lit la première ligne, puis toutes les autres, et recolle l'enveloppe sans une trace. C'est la recette secrète de la tarte du blaireau. Le soir même, {A} fait une tarte. Le blaireau la goûte, plisse les yeux, et ne dit rien. Pour l'instant.",
                    "L'enveloppe se déchire en deux. Impossible de faire comme si de rien n'était. {A} porte les deux moitiés à {B}, avec des excuses et un pot de colle. {B} recolle la lettre, la lit, et regarde {A} d'un drôle d'air.",
                    E(new GrowStat(PlynlingStat.Intrigue), new LiftNeed(Need.Hunger, 0.2)), E(new AffinityShift(-10)), Ai((AiAxis.Honor, -2)), Stress(("honest", 30), ("just", 15))),
                Plain("pigeon", "Rattraper le pigeon, et lui faire la leçon",
                    "{A} rattrape le pigeon et lui fait un exposé de vingt minutes sur l'importance des adresses. Le pigeon écoute, hoche la tête, et repart livrer la lettre… chez l'escargot.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_board_game", EventType.Pulse, Grown, "La partie de la semaine",
            "La chouette a sorti son vieux plateau de jeu, celui avec les pions en noyaux de cerise. « Une partie, dit la chouette. Un coup par jour, posé sur le rebord de ma fenêtre. » La dernière partie de la chouette a duré un hiver entier. {A} pose le premier pion.",
            new[]
            {
                Try("attack", "Attaquer tout de suite, sans réfléchir", new EventChallenge(PlynlingStat.Courage, 7),
                    "Le premier coup de {A} fait tomber trois pions d'un coup. Le lendemain, sur le rebord de la fenêtre, un mot de la chouette : « Intéressant. » La partie s'annonce longue.",
                    "Le premier coup de {A} est audacieux. Le deuxième coup de la chouette le rend ridicule. La partie s'annonce longue, et un peu humiliante.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_board_game_end", 72, 120)), E(new FollowUp("grown_board_game_end", 72, 120)),
                    Ai((AiAxis.Boldness, 2))),
                Plain("slow", "Jouer lentement, en étudiant chaque coup",
                    "{A} réfléchit une journée entière avant chaque coup, la nuit aussi. La chouette trouve enfin un adversaire à sa mesure, et commence à arriver en retard à la bibliothèque.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_board_game_end", 72, 120)), Ai((AiAxis.Rationality, 2))),
                Plain("notes", "Glisser un petit mot à côté de chaque coup",
                    "Chaque jour, {A} pose son pion et un petit mot à côté : la météo, un potin, une question sur les étoiles. La chouette répond à tout, en tout petit, au dos du mot. La partie avance lentement. La correspondance, très vite.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_board_game_end", 72, 120)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_board_game_end", EventType.FollowUp, AnyStage, "Le dernier coup",
            "Une semaine plus tard, deux pions seulement restent sur le plateau : un à {A}, un à la chouette. Le village entier passe devant la fenêtre pour regarder, mine de rien. Le prochain coup décide de tout, et c'est à {A} de jouer.",
            new[]
            {
                Try("win", "Jouer le coup préparé depuis trois jours", new EventChallenge(PlynlingStat.Learning, 9),
                    "{A} pose le pion. La chouette regarde le plateau longtemps, très longtemps, puis enlève ses lunettes et les essuie. « Personne ne m'avait battue depuis l'hiver de la grande neige. » La chouette offre à {A} le pion vainqueur, un noyau de cerise poli par les années.",
                    "{A} pose le pion. La chouette pose le sien juste à côté, avec un petit bruit sec. Fin de la partie. « Belle partie, dit la chouette. On recommence ? » Et {A} dit oui, malgré tout.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Rationality, 2))),
                Plain("draw", "Proposer la nulle, et un thé",
                    "{A} propose la nulle. La chouette réfléchit, puis accepte, et sort deux tasses. Le plateau reste sur le rebord, tel quel. Les deux pions y sont encore, et personne n'ose y toucher.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("lose", "Perdre exprès, pour faire plaisir",
                    "{A} joue un coup volontairement mauvais. La chouette le voit tout de suite, fronce les sourcils, et refuse de prendre le pion. « On ne me fait pas de cadeau. Rejoue. » {A} rejoue, et perd quand même. Tout le monde est content.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, -1)), Stress(("honest", 10))),
            }),

        new EventDef("grown_rumor", EventType.Pulse, Grown, "La rumeur du marché",
            "Une rumeur court au marché : {A} serait riche, très riche, et cacherait un trésor sous son lit. La pie jure que ça ne vient pas du marché, ce qui veut dire que ça vient du marché. Depuis ce matin, on salue {A} beaucoup plus bas.",
            new[]
            {
                Plain("open", "Démentir, en ouvrant grand sa porte",
                    "{A} ouvre sa porte à tout le village : sous le lit, trois chaussettes et un gland. La foule repart, déçue. Seule la pie reste, et examine le gland de très près.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Try("play", "Laisser courir, et jouer le jeu", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} achète un chapeau, prend un air mystérieux, et paie son pain avec une lenteur de millionnaire. Le boulanger, impressionné, offre une brioche « pour un client de marque ». La rumeur double.",
                    "{A} joue au riche trois jours, jusqu'à ce que passe le bonnet de la collecte pour la fête du village. Tout le monde regarde. {A} donne un caillou. Fin de la rumeur.",
                    E(new GrowStat(PlynlingStat.Intrigue), new LiftNeed(Need.Hunger, 0.2)), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1)), Stress(("honest", 20))),
                Plain("piggy", "En faire une vérité : commencer une tirelire",
                    "{A} décide qu'au fond, ce n'est pas une mauvaise idée, et commence une tirelire. Le premier caillou y tombe avec un joli bruit. Le deuxième, un peu plus tard. On commence petit.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("trade_sense")), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
            }),

        // adulte et ancien: le Grand Concours (CK3's imperial examination: the notice, the night before,
        // a slip of paper in the hall, then four endings)
        new EventDef("grown_exam", EventType.Pulse, Grown, "Le Grand Concours",
            "Une affiche est clouée sur la porte de la bibliothèque, écrite à la plume par la chouette en personne : « Grand Concours. Une épreuve, un lauréat, un nom peint sur le tableau d'honneur. » Le tableau est dans l'entrée. Le dernier nom peint date de douze hivers, et la peinture s'écaille. {A} relit l'affiche trois fois.",
            new[]
            {
                Plain("study", "S'inscrire, et réviser dès ce soir",
                    "{A} s'inscrit en lettres soignées, emprunte onze livres, et les empile sur la table de la cuisine. Le premier est ouvert avant le dîner. Le dîner attendra.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_exam_eve", 48, 96)), Ai((AiAxis.Rationality, 2)), Stress(("lazy", 15))),
                Plain("sign", "S'inscrire, pour voir",
                    "{A} signe d'un trait rapide, entre l'ours et l'escargot. L'escargot s'inscrit tous les ans, depuis toujours, et n'a encore jamais fini l'épreuve à temps.",
                    E(new FollowUp("grown_exam_eve", 48, 96)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("cakes", "Ne pas s'inscrire, mais cuisiner pour les candidats",
                    "Le jour du concours, un plateau de gâteaux au miel attend devant la bibliothèque. Les candidats mangent tout. Le soir, la chouette fait savoir que le concours de l'an prochain aura une catégorie « pâtisserie ». Personne ne sait si c'est une blague.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_exam_eve", EventType.FollowUp, AnyStage, "La veille du concours",
            "C'est la veille du concours. Les livres sont ouverts partout, même sur la chaise. Dehors, des rires : les autres candidats vont au café, « pour se détendre ». Et sur le banc devant chez {A}, un jeune loir sanglote dans son cahier, persuadé de tout rater.",
            new[]
            {
                Try("cram", "Réviser toute la nuit", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} révise jusqu'à ce que les étoiles pâlissent. À l'aube, tout est là, rangé, à sa place dans la tête. {A} part au concours les yeux rouges et l'esprit clair.",
                    "{A} révise jusqu'à l'aube, mais les mots finissent par danser sur la page. Au matin, {A} connaît par cœur la liste des fleuves… à l'envers.",
                    E(new ApplyModifier("inspired"), new FollowUp("grown_exam_paper", 12, 24)), E(new ApplyModifier("sleepless"), new FollowUp("grown_exam_paper", 12, 24)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
                Plain("cafe", "Rejoindre les autres au café",
                    "Au café, les candidats parlent de tout sauf du concours. La tortue sert des tisanes « pour la mémoire », et l'ours raconte son propre concours, perdu de très loin, avec une telle joie que plus personne n'a peur.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_exam_paper", 12, 24)), Ai((AiAxis.Sociability, 2))),
                Plain("dormouse", "S'asseoir à côté du loir, et réviser à deux",
                    "{A} s'assoit à côté du loir, et les deux révisent ensemble, une question chacun. À minuit, le loir ne pleure plus. À une heure, le loir dort sur l'épaule de {A}, et {A} n'ose plus bouger.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_exam_paper", 12, 24)), Ai((AiAxis.Compassion, 2))),
                Plain("sleep", "Se coucher tôt : une tête reposée vaut deux livres",
                    "{A} ferme les livres à huit heures, boit un lait chaud et dort comme une pierre. Au matin, la tête est légère. Un peu vide, peut-être, mais légère.",
                    E(new ApplyModifier("well_rested"), new FollowUp("grown_exam_paper", 12, 24)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            }),

        new EventDef("grown_exam_paper", EventType.FollowUp, AnyStage, "Le papier plié",
            "La salle de lecture a été vidée, et des pupitres alignés. La chouette distribue les sujets, puis s'installe sur son perchoir pour surveiller. Au bout d'une heure, la chouette dort. Un papier plié glisse alors jusqu'aux pattes de {A}, poussé par une patte inconnue. Dessus, d'une écriture minuscule : toutes les réponses.",
            new[]
            {
                Try("own", "Repousser le papier du pied, et écrire ses propres réponses", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} repousse le papier sous le pupitre voisin, du bout du pied, et écrit. Les mots viennent, un par un, puis tous ensemble. {A} pose la plume au moment où la chouette se réveille.",
                    "{A} repousse le papier et écrit. Mais la question trois porte sur les fougères, et {A} n'en connaît que la couleur. {A} rend sa copie le cœur un peu lourd, et la conscience légère.",
                    E(new FollowUp("grown_exam_laureate", 24, 48)), E(new FollowUp("grown_exam_honest", 24, 48)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1))),
                Try("report", "Réveiller la chouette, et lui remettre le papier", new EventChallenge(PlynlingStat.Learning, 7),
                    "{A} toussote. La chouette ouvre un œil, voit le papier, et le déchire en mille morceaux. Puis accorde à {A}, sans un mot, dix minutes de plus que tout le monde. Ces dix minutes suffisent.",
                    "{A} toussote. La chouette ouvre un œil, voit le papier, et le déchire en mille morceaux. Puis accorde à {A} dix minutes de plus. Dix minutes, ce n'est pas assez pour les fougères.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_exam_laureate", 24, 48)), E(new ApplyModifier("clear_conscience"), new FollowUp("grown_exam_honest", 24, 48)),
                    Ai((AiAxis.Honor, 2))),
                Try("peek", "Jeter un coup d'œil, un seul", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} déplie le papier sous le pupitre, copie tout, et le fait disparaître dans sa manche avant que la chouette ne s'étire. Personne n'a rien vu. Presque rien.",
                    "{A} déplie le papier. Le papier craque. La chouette ouvre un œil, puis deux, et descend de son perchoir en silence.",
                    E(new FollowUp("grown_exam_hollow", 24, 48)), E(new FollowUp("grown_exam_caught", 6, 12)), Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1)), Stress(("honest", 30), ("just", 20))),
            }),

        new EventDef("grown_exam_laureate", EventType.FollowUp, AnyStage, "Le tableau d'honneur",
            "Une semaine plus tard, tout le village se presse dans l'entrée de la bibliothèque. Le peintre a sorti ses pinceaux. La chouette déplie un papier, ajuste ses lunettes, et lit le nom du lauréat. C'est {A}. Le silence dure une seconde, puis l'escargot se met à applaudir, et tout le monde suit.",
            new[]
            {
                Try("speech", "Faire un discours, un vrai", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} remercie la chouette, les livres, la tortue, et l'escargot, pour son exemple de persévérance. L'escargot pleure. L'ours aussi. On peint le nom de {A} en lettres dorées, et la chouette remet la bourse du concours.",
                    "{A} commence à remercier tout le monde et oublie la moitié du village. Le discours dure quarante minutes, à force de rattrapages. On peint le nom quand même, et la bourse est remise pendant la quarante et unième.",
                    E(new ApplyModifier("laureate"), new GiveCailloux(25)), E(new GiveCailloux(25)), Ai((AiAxis.Sociability, 2)), Stress(("shy", 20))),
                Plain("thanks", "Dire merci, et rien d'autre",
                    "« Merci. » Puis {A} descend de l'estrade. Le village trouve ça très élégant, et en parle pendant des semaines. Le nom de {A} est peint en lettres dorées, et la bourse glissée dans sa patte.",
                    E(new ApplyModifier("laureate"), new GiveCailloux(25)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, -1))),
                Plain("library", "Offrir la bourse à la bibliothèque",
                    "{A} tend la bourse à la chouette : « Pour la réserve. » La chouette, émue, range ses lunettes trois fois de suite. Une semaine plus tard, une étagère neuve porte une petite plaque au nom de {A}.",
                    E(new ApplyModifier("laureate"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1)), Stress(("greedy", 20))),
            }),

        new EventDef("grown_exam_honest", EventType.FollowUp, AnyStage, "La mention de la chouette",
            "Les résultats sont affichés. Le nom peint sur le tableau d'honneur n'est pas celui de {A}, mais celui d'un jeune loir, qui n'en revient pas. En bas de la liste, pourtant, d'une écriture minuscule, la chouette a ajouté une ligne : « Mention spéciale : {A}, pour la copie la plus honnête. »",
            new[]
            {
                Plain("congrats", "Aller féliciter le loir",
                    "{A} serre la patte du loir, qui tremble encore. Le loir murmure : « C'est un peu grâce à toi, tu sais. » {A} ne sait pas trop pourquoi, mais rentre avec quelque chose de chaud dans la poitrine.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
                Plain("next", "Recopier la liste des livres, pour l'an prochain",
                    "{A} recopie la liste des livres, en ajoute deux, et punaise le tout au-dessus du lit. L'an prochain, le nom sera peint. En attendant, on révise les fougères.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
                Plain("frame", "Encadrer la mention",
                    "La mention est découpée, encadrée, et accrochée au-dessus de la cheminée. Les visiteurs la lisent à voix haute. {A} fait semblant de ne pas écouter, à chaque fois.",
                    E(new LiftNeed(Need.Happiness, 0.2), new ApplyModifier("clear_conscience")), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_exam_caught", EventType.FollowUp, AnyStage, "Pris sur le fait",
            "La chouette se tient devant le pupitre de {A}, le papier plié entre deux plumes. Toute la salle a cessé d'écrire. « Ce papier », dit la chouette, très doucement. « Je t'écoute. »",
            new[]
            {
                Plain("confess", "Tout avouer",
                    "{A} avoue tout, d'une petite voix. La chouette hoche la tête, déchire la copie, et en tend une neuve. « Recommence, avec ta tête à toi. Tu as une heure. » {A} écrit pendant une heure, moins bien, mais vrai. Le soir, sur la copie, un mot de la chouette : « Mieux. »",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Try("deny", "Jurer que le papier est arrivé tout seul", new EventChallenge(PlynlingStat.Intrigue, 9),
                    "« Arrivé tout seul, je le jure. » La chouette fixe {A} un long moment, puis le papier, puis la rangée de derrière, où un jeune corbeau regarde ses pattes avec beaucoup d'intérêt. La chouette va s'asseoir à côté du corbeau, et n'en bouge plus.",
                    "« Arrivé tout seul, je le jure. » La chouette retourne le papier : au dos, en tout petit, le nom de {A}, sur un vieux brouillon de {A}. Le silence de la salle est terrible. {A} rentre avant la fin.",
                    E(new GrowStat(PlynlingStat.Intrigue)), E(new ApplyModifier("guilty")), Ai((AiAxis.Honor, -2)), Stress(("honest", 30))),
            }),

        new EventDef("grown_exam_hollow", EventType.FollowUp, AnyStage, "Des lettres dorées",
            "Le nom de {A} est peint sur le tableau d'honneur, en lettres dorées. Tout le village applaudit. La chouette serre la patte de {A} : « Je n'avais jamais lu de réponses aussi justes. » Au fond de la manche de {A}, le petit papier plié pèse aussi lourd qu'une pierre.",
            new[]
            {
                Plain("confess", "Tout avouer, là, devant tout le monde",
                    "{A} sort le papier de sa manche et le tend à la chouette, devant tout le village. Silence. Puis la chouette prend un pinceau, efface le nom, et écrit à la place, en petit : « Le plus courageux. » Le village n'a jamais vu de tableau d'honneur aussi bizarre, ni aussi beau.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("keep", "Sourire, saluer, et garder le papier dans sa manche",
                    "{A} sourit, salue, et empoche la bourse. Le nom brille au soleil. Ensuite, chaque fois que {A} passe devant la bibliothèque, {A} change de trottoir.",
                    E(new GiveCailloux(25), new ApplyModifier("guilty")), Ai((AiAxis.Greed, 2), (AiAxis.Honor, -1)), Stress(("honest", 30), ("just", 20))),
                Plain("burn", "Brûler le papier, et mériter le nom après coup",
                    "Le soir, {A} brûle le papier dans la cheminée et regarde la petite flamme bleue. Le nom reste sur le tableau. {A} révise pourtant tous les soirs de l'année suivante, pour mériter, avec un peu de retard, ce qui y est écrit.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("laureate")), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
            }),

        // adulte et ancien: la Régate des Feuilles (CK3's chariot race: the build, the start, then
        // the lead or the back of the field)
        new EventDef("grown_regatta", EventType.Pulse, Grown, "La Régate des Feuilles",
            "Dimanche, c'est la Régate des Feuilles sur l'étang : des bateaux grands comme une main, faits de ce qu'on trouve, et poussés par le vent. Le premier prix est une bourse, et le droit de porter toute l'année la casquette de l'amiral. {A} a une semaine pour construire un bateau.",
            new[]
            {
                Plain("fast", "Un bateau fin et rapide, en écorce de bouleau",
                    "{A} taille une coque fine comme une feuille de papier, et une voile en plume de pie. Au premier essai, le bateau file si vite que {A} doit le rattraper à la nage.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_regatta_start", 48, 96)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("sturdy", "Une coque de noix, solide comme un rocher",
                    "{A} choisit la plus grosse coque de noix du marché, la calfeutre à la cire et y plante un mât en brindille. Le bateau n'est pas rapide. Le bateau ne coulera jamais. C'est une philosophie.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_regatta_start", 48, 96)), Ai((AiAxis.Rationality, 2))),
                Plain("pretty", "Le plus beau bateau de l'étang, avec un pavillon",
                    "{A} peint la coque en rouge, coud un pavillon brodé à son nom, et ajoute un minuscule équipage en glands. Les passants s'arrêtent pour regarder. Personne ne pose la question de la flottaison.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_regatta_start", 48, 96)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_regatta_start", EventType.FollowUp, AnyStage, "Le départ",
            "Vingt bateaux sont alignés entre deux roseaux. Le moineau tient le sifflet, l'ours tient les paris, et tout le village tient la rive. À côté du bateau de {A} : celui de la pie, noir et luisant, et celui du vieil escargot, qui participe depuis quarante ans sans jamais avoir fini. Coup de sifflet. Le vent se lève.",
            new[]
            {
                Try("gap", "Foncer dans le trou entre deux bateaux", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} souffle dans la voile et vise le trou. Le bateau s'y faufile à un cheveu près, frôle celui de la pie, et passe en tête au premier virage. Sur la rive, quelqu'un crie le nom de {A}.",
                    "{A} vise le trou. Le trou se referme. Le bateau rebondit sur celui de la pie, tourne trois fois sur lui-même, et repart… dans le mauvais sens.",
                    E(new FollowUp("grown_regatta_lead", 2, 6)), E(new FollowUp("grown_regatta_back", 2, 6)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("wind", "Lire le vent, et attendre la bonne rafale", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} attend. Les autres partent, s'emmêlent, se cognent. Puis la rafale arrive, exactement comme prévu, et pousse le bateau de {A} au-dessus de la mêlée, jusqu'en tête.",
                    "{A} attend la bonne rafale. La bonne rafale ne vient pas. Une mauvaise vient à la place, et range le bateau de {A} soigneusement derrière tous les autres.",
                    E(new FollowUp("grown_regatta_lead", 2, 6)), E(new FollowUp("grown_regatta_back", 2, 6)), Ai((AiAxis.Rationality, 2))),
                Plain("duckling", "Redresser le bateau d'un caneton qui chavire",
                    "À peine parti, le bateau d'un caneton chavire. {A} se penche, le redresse, l'égoutte, et le remet à l'eau. Le caneton repart, ravi. Le bateau de {A}, lui, est désormais tout au fond de la course.",
                    E(new ApplyModifier("cherished"), new FollowUp("grown_regatta_back", 2, 6)), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_regatta_lead", EventType.FollowUp, AnyStage, "La dernière bouée",
            "Le bateau de {A} est en tête, seul, à l'approche de la dernière bouée. La pie est trop loin pour inquiéter. Mais le long de la rive, sans un bruit, une petite coque grise remonte : le bateau du vieil escargot, porté par un courant que personne n'avait vu. Sur la rive, l'escargot ne respire plus.",
            new[]
            {
                Try("push", "Tout donner jusqu'à la ligne", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} tend la voile au maximum. Le bateau franchit la ligne avec une longueur d'avance. Le moineau siffle, l'ours compte la bourse, et on pose sur la tête de {A} la casquette de l'amiral, un peu grande. L'escargot, deuxième, est fou de joie : l'escargot n'avait jamais fini.",
                    "{A} tend la voile. La voile se déchire. Le bateau gris passe, lentement, majestueusement, et franchit la ligne. Quarante ans d'attente. Le village pleure. {A}, deuxième, pleure aussi, de joie, ou presque.",
                    E(new GiveCailloux(30), new ApplyModifier("fired_up")), E(new GiveCailloux(10), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
                Plain("let", "Laisser passer l'escargot",
                    "{A} relâche la voile, juste un peu. Le petit bateau gris passe, lentement, et franchit la ligne en premier. L'escargot ne dit rien pendant une minute entière. Puis l'escargot offre à {A} sa vieille coquille de rechange, gardée quarante ans pour ce jour-là.",
                    E(new GiveItem("col.coquille_escargot"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2)), Stress(("ambitious", 15))),
            }),

        new EventDef("grown_regatta_back", EventType.FollowUp, AnyStage, "Au fond de l'étang",
            "Le bateau de {A} est dernier, très dernier, coincé entre un nénuphar et une famille de grenouilles qui trouve ça très drôle. Devant, la course continue sans {A}. Mais {A} connaît l'étang : à gauche, derrière les roseaux, s'ouvre un passage étroit que personne n'emprunte jamais.",
            new[]
            {
                Try("reeds", "Prendre le passage des roseaux", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "Le bateau de {A} disparaît dans les roseaux, et ressort de l'autre côté, juste derrière la pie. Troisième place ! Les grenouilles, qui ont tout suivi, applaudissent avec leurs pattes palmées, ce qui fait un bruit très particulier.",
                    "Le bateau de {A} disparaît dans les roseaux, et n'en ressort pas. On le retrouve le lendemain, décoré de lentilles d'eau, avec un têtard à bord. Le têtard refuse de descendre.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveCailloux(10)), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("parade", "Profiter de la promenade, et saluer la rive",
                    "{A} renonce à la course et salue la foule, comme à une parade. Le bateau dérive doucement jusqu'à la ligne, bon dernier, sous la plus grande ovation de la journée. Le moineau invente un prix : « Le plus beau voyage ».",
                    E(new ApplyModifier("light_heart"), new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Sociability, 2))),
                Plain("frogs", "Engager les grenouilles comme équipage",
                    "{A} négocie avec les grenouilles : une mouche chacune, payable à l'arrivée. Les grenouilles poussent le bateau à la nage, un peu dans tous les sens. Septième place. L'équipage réclame sa paye, et une revanche.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
            }),

        // adulte et ancien: la belette du moulin (CK3's party baron: admire, envy or befriend the one
        // everyone loves; the three branches meet at the mill party)
        new EventDef("grown_darling", EventType.Pulse, Grown, "La belette du moulin",
            "Une belette s'est installée dans le vieux moulin, et en trois semaines, tout le village l'adore. La belette connaît le prénom de chacun, rit à toutes les blagues de l'ours, et donne des fêtes où même le hérisson arrive en retard. Ce matin encore, au café, on ne parle que de la belette. {A} remue son thé.",
            new[]
            {
                Plain("watch", "Observer la belette, pour percer son secret",
                    "{A} s'installe au fond du café, un carnet sur les genoux. La belette entre, et en une minute, salue onze personnes par leur nom, demande des nouvelles d'un genou, et complimente un chapeau. {A} note tout. Ça va être un long carnet.",
                    E(new FollowUp("grown_darling_lesson", 24, 72)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1))),
                Plain("envy", "Trouver que la belette, c'est surfait",
                    "« Surfait », dit {A} tout haut, dans le café silencieux. Toutes les têtes se tournent. {A} boit son thé avec une grande dignité, en se brûlant un peu. La guerre est déclarée, au moins d'un côté.",
                    E(new FollowUp("grown_darling_rival", 24, 72)), Ai((AiAxis.Vengefulness, 1), (AiAxis.Boldness, 1)), Stress(("content", 15))),
                Plain("knock", "Monter frapper à la porte du moulin",
                    "{A} monte au moulin avec un pot de confiture, et frappe. Rien. Puis la porte s'ouvre sur la belette, couverte de farine jusqu'aux oreilles, l'air un peu perdu. « Oh. Personne ne frappe jamais. On vient directement aux fêtes. Entre. »",
                    E(new FollowUp("grown_darling_visit", 24, 72)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_darling_lesson", EventType.FollowUp, AnyStage, "Le carnet",
            "Le carnet de {A} est plein, et le secret de la belette tient en une page : se souvenir des prénoms, poser des questions, écouter les réponses, et rire quand c'est drôle, pas avant. Ça paraît simple. {A} décide d'essayer la méthode sur l'ours, ce matin, au marché.",
            new[]
            {
                Try("method", "Appliquer la méthode, à la lettre", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} demande à l'ours des nouvelles de son genou, se souvient de son parfum de glace préféré, et écoute jusqu'au bout l'histoire de la grande tempête. L'ours, ému, offre un cornet. Le soir, une invitation arrive du moulin.",
                    "{A} demande à l'ours des nouvelles de son genou. L'ours n'a jamais eu mal au genou, et passe l'après-midi à s'inquiéter. Le soir, une invitation arrive quand même : la belette a entendu l'histoire, et l'a trouvée irrésistible.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("well_spoken"), new FollowUp("grown_darling_party", 48, 96)), E(new FollowUp("grown_darling_party", 48, 96)),
                    Ai((AiAxis.Sociability, 2))),
                Plain("own", "Refermer le carnet : on ne copie pas les gens",
                    "{A} referme le carnet et le range. On n'est pas une belette. Ce soir-là, au café, {A} raconte une histoire à sa façon, maladroite, trop longue. La tortue rit au bon moment. Le lendemain, une invitation arrive du moulin.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_darling_party", 48, 96)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_darling_rival", EventType.FollowUp, AnyStage, "La contre-fête",
            "Pour prouver qu'on peut s'amuser sans belette, {A} donne une fête le même soir que celle du moulin. Guirlandes, gâteaux, musique : tout est prêt, sauf les invités. À huit heures, la salle est vide. À huit heures et demie, on frappe à la porte.",
            new[]
            {
                Plain("open", "Ouvrir, en se préparant au pire",
                    "Sur le seuil : la belette, un gâteau dans les pattes, et derrière, la moitié du village. « On a annulé la nôtre, dit la belette. Une fête sans toi, ça n'avait pas de sens. » {A} reste bouche bée, puis s'écarte pour laisser entrer tout le monde.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_darling_party", 72, 120)), Ai((AiAxis.Sociability, 1))),
                Plain("hide", "Souffler les bougies, et faire le mort",
                    "{A} souffle les bougies. On frappe encore. Puis un papier glisse sous la porte : « On a vu la lumière. On laisse le gâteau devant. Viens au moulin samedi ? La belette. » Le gâteau est délicieux. C'est le pire.",
                    E(new LiftNeed(Need.Hunger, 0.2), new FollowUp("grown_darling_party", 72, 120)), Ai((AiAxis.Sociability, -1))),
                Try("full", "Ouvrir, et annoncer que c'est complet", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "« Complet », dit {A}. La belette regarde la salle vide derrière {A}, sourit, et repart sans un mot. Le lendemain, tout le village parle de la fête « complète » de {A}. Personne n'y était. Tout le monde voudrait y avoir été.",
                    "« Complet », dit {A}. Derrière, un ballon se dégonfle avec un long sifflement. La belette essaie de ne pas rire, très fort, et échoue. {A} finit par rire aussi. Les deux mangent tout le gâteau sur les marches.",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("sly"), new FollowUp("grown_darling_party", 72, 120)), E(new LiftNeed(Need.Hunger, 0.2), new FollowUp("grown_darling_party", 72, 120)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1)), Stress(("honest", 15))),
            }),

        new EventDef("grown_darling_visit", EventType.FollowUp, AnyStage, "Le moulin sans fête",
            "Dans le moulin, sans les lampions ni la foule, tout est très calme. La belette verse le thé, et ses pattes tremblent un peu. « Je donne des fêtes, avoue la belette, parce que le moulin grince la nuit. Avec du monde, on ne l'entend pas. »",
            new[]
            {
                Plain("tea", "Revenir prendre le thé, les soirs sans fête",
                    "{A} revient le lendemain, et le surlendemain. Le moulin grince toujours, mais les deux en rient maintenant, et lui ont même donné un nom. Quand vient la grande fête du samedi, la belette garde à {A} la meilleure place.",
                    E(new ApplyModifier("cherished"), new FollowUp("grown_darling_party", 72, 120)), Ai((AiAxis.Compassion, 2))),
                Try("grease", "Graisser les rouages, pour faire taire le moulin", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} grimpe dans les rouages avec une burette et graisse tout, jusqu'à la dernière dent. Le soir, silence complet. La belette dort douze heures d'affilée, et se réveille avec une seule idée : une fête, en l'honneur de {A}.",
                    "{A} graisse les rouages. Le moulin ne grince plus. Le moulin chante, maintenant, une note haute et tremblante, toute la nuit. La belette trouve ça plus joli, finalement, et l'invitation pour samedi arrive avec une partition.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_darling_party", 72, 120)), E(new FollowUp("grown_darling_party", 72, 120)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Compassion, 1))),
            }),

        new EventDef("grown_darling_party", EventType.FollowUp, AnyStage, "La fête du moulin",
            "Samedi soir. Le moulin brille de cent lanternes. On danse dans la cour, on mange dans l'escalier, et l'escargot porte un nœud papillon. À minuit, la belette grimpe sur un tonneau, lève son verre de jus de pomme, et cherche quelqu'un dans la foule. Son regard s'arrête sur {A}. « À {A} ! » Et tout le moulin répète.",
            new[]
            {
                Try("toast", "Grimper sur un tonneau, et porter un toast en retour", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} grimpe sur le tonneau d'à côté. « À la belette, qui a appris au moulin à se taire et au village à danser ! » Le moulin explose. On porte {A} et la belette jusqu'au matin, en chantant faux.",
                    "{A} grimpe sur le tonneau d'à côté, qui roule. {A} porte le toast en roulant à travers la cour, puis jusque dans la mare. Le moulin trouve ça encore mieux qu'un discours.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("party_soul")), E(new ApplyModifier("party_soul")), Ai((AiAxis.Sociability, 2)), Stress(("shy", 20))),
                Plain("dance", "Danser jusqu'à l'aube",
                    "{A} danse avec l'ours, avec le hérisson, avec trois grenouilles à la fois et, pour finir, avec la belette, pendant que le soleil se lève sur la roue du moulin. Personne ne se souvient de la musique. Tout le monde se souvient de la danse.",
                    E(new ApplyModifier("party_soul"), new LiftNeed(Need.Happiness, 0.25)), Ai((AiAxis.Energy, 2))),
                Plain("roof", "S'éclipser sur le toit, regarder les étoiles",
                    "Vers deux heures, {A} et la belette grimpent sur le toit du moulin. En bas, la fête continue toute seule. La belette montre les étoiles une par une, et leur donne des noms d'habitants du village. Une étoile porte désormais le nom de {A}. Discrètement.",
                    E(new ApplyModifier("cherished"), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        // adulte et ancien: la grande crue (CK3's natural disasters: the night, the shelter, the mud,
        // the thank-you supper)
        new EventDef("grown_flood", EventType.Pulse, Grown, "La nuit de la crue",
            "Trois jours de pluie, sans arrêt. Cette nuit, un grondement réveille {A} : la rivière est sortie de son lit. L'eau monte dans les rues basses, et on entend crier du côté du café. Pas le temps de réfléchir : où courir d'abord ?",
            new[]
            {
                Try("cellar", "Sauver la cave à confitures de la tortue", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} plonge dans la cave, de l'eau jusqu'au cou, et remonte les pots un par un, jusqu'au tout premier, étiqueté d'une année que personne ne connaît. La tortue serre le pot contre sa carapace, sans un mot. Les mots, ce sera pour plus tard.",
                    "{A} plonge dans la cave et remonte… un pot de cornichons. Un seul. La tortue le regarde, regarde {A}, {a:trempé|trempée}, et éclate de rire au milieu de la nuit et de l'eau. C'est déjà ça.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_flood_shelter", 6, 12)), E(new FollowUp("grown_flood_shelter", 6, 12)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Plain("snail", "Porter l'escargot en haut de la colline",
                    "La maison de l'escargot, c'est l'escargot. {A} le porte à deux mains jusqu'en haut de la colline, en courant, sous la pluie. L'escargot, qui n'est jamais allé aussi vite de sa vie, demande si on peut recommencer.",
                    E(new ApplyModifier("cherished"), new FollowUp("grown_flood_shelter", 6, 12)), Ai((AiAxis.Compassion, 2))),
                Plain("bell", "Sonner la cloche de la gare pour réveiller tout le monde",
                    "{A} court à la gare et sonne la cloche du hérisson, à toute volée. En cinq minutes, le village entier est debout, en chemise de nuit, et s'organise. Le hérisson arrive en dernier, pour la première fois de sa vie, et ne fera jamais le moindre reproche.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_flood_shelter", 6, 12)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("grown_flood_shelter", EventType.FollowUp, AnyStage, "Un toit pour tout le monde",
            "La maison de {A} est sur la hauteur, et l'eau ne l'atteint pas. Alors on frappe à la porte, toute la nuit : le blaireau et ses couvertures, trois grenouilles ravies, le moineau trempé, l'escargot. Au matin, on ne voit plus le plancher, et tout ce petit monde a faim.",
            new[]
            {
                Try("organize", "Organiser les rations et les lits, avec une liste", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} fait une liste, puis deux, puis un tableau. Chacun reçoit un coin, une couverture et une part de pain. Le blaireau, impressionné, prend des notes. Au bout de trois jours, la maison tourne comme une horloge : celle de la gare, pas celle du café.",
                    "{A} fait une liste, mais les grenouilles la mangent. Le reste du séjour se fait au hasard, dans la bonne humeur et un certain désordre. Personne n'a faim. Personne ne sait vraiment comment.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("trade_sense"), new FollowUp("grown_flood_mud", 24, 48)), E(new FollowUp("grown_flood_mud", 24, 48)),
                    Ai((AiAxis.Rationality, 2))),
                Plain("pantry", "Vider le garde-manger, jusqu'à la dernière noix",
                    "{A} sort tout : le miel, les noix, la confiture des grandes occasions. Le petit-déjeuner dure jusqu'à midi. Le garde-manger est vide, mais la maison n'a jamais été aussi pleine.",
                    E(new ApplyModifier("cherished"), new FollowUp("grown_flood_mud", 24, 48)), Ai((AiAxis.Compassion, 2)), Stress(("greedy", 20))),
                Plain("stories", "Raconter des histoires, pour que la nuit passe",
                    "Chaque soir, à la lumière d'une bougie, {A} raconte une histoire, et chacun en raconte une à son tour. Celle de l'escargot dure deux soirs entiers. Personne ne pense plus à l'eau, dehors.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_flood_mud", 24, 48)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_flood_mud", EventType.FollowUp, AnyStage, "Ce que la rivière rend",
            "L'eau s'est retirée. Le village est couvert d'une boue épaisse, qui sent la rivière et les racines. Tout le monde sort les pelles. Et dans la boue, on trouve de tout : des cuillères, une botte, des choses que la rivière gardait depuis très longtemps.",
            new[]
            {
                Try("dig", "Fouiller la boue, là où le courant a tourné", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} devine où le courant a ralenti, et creuse là. Sous la boue : une vieille pièce frappée d'un pont qui n'existe plus. La rivière la gardait pour quelqu'un. Visiblement, pour {A}.",
                    "{A} creuse au mauvais endroit, et trouve une botte. La botte contient une grenouille. La grenouille a beaucoup d'opinions sur le dérangement.",
                    E(new GiveItem("col.piece_ancienne"), new FollowUp("grown_flood_supper", 48, 96)), E(new FollowUp("grown_flood_supper", 48, 96)), Ai((AiAxis.Rationality, 1), (AiAxis.Greed, 1))),
                Plain("cafe", "Dégager le café de la tortue, pelle après pelle",
                    "{A} pellette toute la journée devant le café. Le soir, la porte s'ouvre de nouveau, et la tortue sert le premier chocolat de l'après-crue, sur un comptoir encore humide. Gratuit, pour tout le monde, jusqu'à nouvel ordre.",
                    E(new LiftNeed(Need.Hunger, 0.2), new FollowUp("grown_flood_supper", 48, 96)), Ai((AiAxis.Compassion, 1), (AiAxis.Energy, 1))),
                Plain("nest", "Aider le héron à refaire son nid, sous le vieux pont",
                    "Le nid du héron est parti avec la crue. {A} rapporte des brindilles, de la mousse, une plume trouvée dans la boue. Le héron arrange tout, sans un mot, puis s'y installe, sur une patte, et regarde {A} une longue seconde. Venant du héron, c'est un discours.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_flood_supper", 48, 96)), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1))),
            }),

        new EventDef("grown_flood_supper", EventType.FollowUp, AnyStage, "Le souper de la crue",
            "Une semaine après la crue, le village dresse une grande table sur la place, avec tout ce qu'on a sauvé. La tortue a sorti un pot de sa plus vieille confiture. Avant le dessert, l'ours se lève et tape sur son verre. « On voudrait dire merci à quelqu'un. » Tous les yeux se tournent vers {A}.",
            new[]
            {
                Plain("purse", "Accepter les remerciements, et la petite bourse",
                    "Le village a fait une collecte : une petite bourse, cousue par le blaireau. {A} la prend, rougit, et dit merci à son tour. Le reste de la soirée, on raconte la crue, et chaque fois, l'histoire grandit un peu.",
                    E(new ApplyModifier("flood_hero"), new GiveCailloux(20)), Ai((AiAxis.Sociability, 1), (AiAxis.Greed, 1))),
                Plain("all", "Lever son verre à tout le village",
                    "{A} se lève : « Ce n'est pas moi. C'est nous. » Puis nomme tout le monde, un par un, grenouilles comprises. Le village applaudit, chacun pour chacun, et la confiture de la tortue fait trois fois le tour de la table.",
                    E(new ApplyModifier("flood_hero"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1))),
                Plain("dishes", "S'éclipser à la cuisine pour faire la vaisselle",
                    "Quand on cherche {A} pour le discours, {A} est déjà à la cuisine, les manches retroussées, à essuyer les assiettes avec la tortue. Les deux parlent de la vieille confiture, de l'année sur l'étiquette, et de tout ce que la rivière a emporté, autrefois.",
                    E(new ApplyModifier("flood_hero"), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, -1))),
            }),

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

        // ---- wave 8, second batch: bébé
        new EventDef("baby_cant_sleep", EventType.Pulse, Baby, "Les yeux grands ouverts",
            "Minuit passé. Tout le village dort, sauf {A}, qui compte les poutres du plafond pour la neuvième fois. Dehors, sur la branche, la chouette ne dort pas non plus. La nuit, la chouette ne dort jamais : c'est son métier.",
            new[]
            {
                Plain("owl", "Ouvrir la fenêtre, et parler à la chouette",
                    "La chouette raconte ce qui se passe la nuit : les souris qui font leurs courses, la lune qui change de place, le hérisson qui ronfle en morse. À la troisième histoire, {A} dort, le nez sur le rebord.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("sheep", "Compter les moutons, sérieusement",
                    "{A} compte les moutons. Au quatre-vingtième, un mouton refuse de sauter. {A} négocie. Le mouton finit par sauter. {A} dort avant le quatre-vingt-unième.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
                Try("night", "Sortir explorer la nuit, en pyjama", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} sort en pyjama et découvre le village de nuit : les lucioles au-dessus de la mare, la boulangerie déjà allumée, et le boulanger qui offre la première brioche, encore chaude. {A} rentre à l'aube, et dort jusqu'à midi.",
                    "{A} sort en pyjama, fait trois pas, et entend un bruit. C'est une feuille. {A} rentre très vite, et dort enfin, sous la couverture, la tête comprise.",
                    E(new GrowStat(PlynlingStat.Courage), new LiftNeed(Need.Hunger, 0.15)), Nothing, Ai((AiAxis.Boldness, 2))),
            }),

        new EventDef("baby_mud_pie", EventType.Pulse, Baby, "La pâtisserie de boue",
            "{A} a ouvert une pâtisserie au bord de la flaque. Au menu : tarte de boue, gâteau de boue, et une spécialité de boue aux pâquerettes. Le premier client s'approche : l'ours, qui a l'air d'avoir très faim.",
            new[]
            {
                Plain("sell", "Vendre une tarte à l'ours, très cher",
                    "« Trois cailloux », annonce {A}. L'ours paie trois cailloux, très sérieusement, prend la tarte, la renifle, et la pose délicatement sur un muret, « pour plus tard ». Les trois cailloux sont rangés sous le lit, pour toujours.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(3)), Ai((AiAxis.Greed, 1), (AiAxis.Sociability, 1))),
                Plain("gift", "Offrir la plus belle part, gratuitement",
                    "{A} offre la plus belle part. L'ours la reçoit comme une médaille, et le lendemain, apporte en échange un vrai gâteau, au miel. {A} trouve l'échange équitable.",
                    E(new LiftNeed(Need.Hunger, 0.2)), Ai((AiAxis.Compassion, 2))),
                Try("tower", "Inventer un gâteau à trois étages, devant le client", new EventChallenge(PlynlingStat.Learning, 4),
                    "{A} invente sur place le gâteau de boue à trois étages, décoré de cailloux blancs. L'ours applaudit. Le gâteau tient debout une heure entière, un record.",
                    "Le troisième étage glisse sur le deuxième, puis sur {A}. L'ours aide à tout nettoyer, et rit tout le long.",
                    E(new GrowStat(PlynlingStat.Learning)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("baby_doudou", EventType.Pulse, Baby, "La grenouille en tricot",
            "La grenouille en tricot de {A} a disparu. Celle avec un œil en bouton et une patte recousue trois fois. Sans la grenouille, impossible de dormir, ni de manger, ni de faire quoi que ce soit. {A} a fouillé partout. Partout, sauf peut-être…",
            new[]
            {
                Plain("village", "Fouiller tout le village, maison par maison",
                    "{A} frappe à toutes les portes. Personne n'a vu la grenouille. Mais tout le monde promet de chercher, et l'ours colle même une affiche sur son kiosque : « Recherchée : grenouille, un œil, très aimée. »",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("baby_doudou_found", 24, 48)), Ai((AiAxis.Sociability, 2))),
                Plain("thread", "Suivre la piste : un fil de laine verte",
                    "Un fil de laine verte traverse le jardin, passe sous la haie, monte le long d'un arbre. {A} le suit jusqu'à la nuit, et rentre avec une pelote énorme et une piste très chaude.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("baby_doudou_found", 24, 48)), Ai((AiAxis.Rationality, 2))),
                Plain("new", "Essayer la grenouille neuve du magasin de jouets",
                    "Le blaireau du magasin de jouets propose une grenouille neuve, plus verte, avec deux yeux. {A} la regarde longtemps, et la repose. « Ce n'est pas la même. » Le blaireau hoche la tête : ça, le blaireau le savait.",
                    E(new FollowUp("baby_doudou_found", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
            }),

        new EventDef("baby_doudou_found", EventType.FollowUp, AnyStage, "La grenouille retrouvée",
            "Ce matin, la grenouille en tricot attend sur le rebord de la fenêtre. Lavée, séchée, avec un nouvel œil : un bouton doré. Posée à côté, une petite plume noire et blanche. Sur le toit d'en face, la pie regarde ailleurs avec beaucoup d'application.",
            new[]
            {
                Plain("thanks", "Crier merci vers le toit",
                    "{A} crie merci vers le toit. La pie fait semblant de ne pas entendre, puis laisse tomber, l'air de rien, un deuxième bouton doré, de rechange.",
                    E(new GiveItem("col.bouton"), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("hug", "Serrer la grenouille, et ne plus jamais la lâcher",
                    "{A} serre la grenouille si fort que le nouveau bouton laisse une marque sur la joue. Cette nuit-là, {A} dort douze heures. La grenouille aussi, sans doute.",
                    E(new ApplyModifier("well_rested"), new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Compassion, 1))),
                Plain("suspect", "Plisser les yeux vers la pie",
                    "{A} plisse les yeux vers le toit. La pie plisse les yeux en retour. Personne ne cède. Mais le bouton doré est très joli, et on décide, sans un mot, d'en rester là.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
            }),

        // ---- wave 8, second batch: ado
        new EventDef("teen_rival", EventType.Pulse, Teen, "Toujours deuxième",
            "Un jeune furet est arrivé au village, et depuis, à la bibliothèque, à la course, au lancer de pomme de pin, le furet finit premier, et {A} deuxième. Ce matin, le furet passe devant {A}, ralentit, et lâche : « Toujours deuxième ? » Puis s'éloigne en sifflotant.",
            new[]
            {
                Plain("train", "S'entraîner en secret, tous les matins",
                    "{A} se lève avant le soleil, court autour de l'étang, lit deux livres par jour, et lance des pommes de pin contre le mur du jardin jusqu'au dîner. Trois jours plus tard, un défi arrive, écrit à la main : la grande course, samedi.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("teen_rival_race", 24, 48)), Ai((AiAxis.Energy, 2)), Stress(("lazy", 15))),
                Plain("shrug", "Hausser les épaules : deuxième, c'est très bien",
                    "{A} hausse les épaules et retourne à son livre. Le furet s'arrête, vexé que ça ne marche pas. Le soir, un défi arrive, en bonne et due forme : la grande course, samedi.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("teen_rival_race", 24, 48)), Ai((AiAxis.Rationality, 1))),
                Try("retort", "Répondre du tac au tac", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "« Deuxième, peut-être. Mais moi, je ne siffle pas faux. » Le furet s'arrête net, et s'en va sans siffler. Le soir, un défi arrive : la grande course, samedi.",
                    "{A} cherche une réponse, et la trouve trois heures plus tard, en se brossant les dents. Trop tard. Le soir, un défi arrive : la grande course, samedi.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("teen_rival_race", 24, 48)), E(new FollowUp("teen_rival_race", 24, 48)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1)), Stress(("shy", 15))),
            }),

        new EventDef("teen_rival_race", EventType.FollowUp, AnyStage, "La grande course",
            "Samedi, la moitié du village s'est installée le long du chemin de l'étang. Le moineau tient le sifflet. Le furet s'étire, très sûr de soi. Le parcours : trois tours d'étang, le vieux pont, et retour. Coup de sifflet.",
            new[]
            {
                Try("sprint", "Partir à fond dès le départ", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} part comme une flèche et mène deux tours entiers. Au troisième, le furet revient, épaule contre épaule. Sur le vieux pont, {A} passe d'un museau. Le moineau hésite longtemps, puis lève la patte de {A}.",
                    "{A} part comme une flèche, mène deux tours, et s'effondre au troisième. Le furet gagne presque en marchant. Puis revient en arrière, et finit le dernier tour à côté de {A}, sans un mot.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("teen_rival_end", 6, 12)), E(new FollowUp("teen_rival_end", 6, 12)), Ai((AiAxis.Boldness, 2))),
                Try("pace", "Garder son souffle pour la fin", new EventChallenge(PlynlingStat.Stewardship, 7),
                    "{A} laisse filer le furet, garde son rythme, et le rattrape dans la dernière ligne droite, à bout de souffle. Victoire d'un cheveu. Le furet, pour la première fois, ne trouve rien à dire.",
                    "{A} garde son souffle pour la fin. La fin arrive trop vite. Le furet gagne, se retourne, et pour la première fois, ne dit rien.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("teen_rival_end", 6, 12)), E(new FollowUp("teen_rival_end", 6, 12)), Ai((AiAxis.Rationality, 2))),
                Try("brambles", "Couper par les ronces", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} coupe par les ronces, ressort devant le furet, et gagne. Le furet regarde les épines accrochées partout sur {A}, et sourit d'un drôle d'air.",
                    "{A} coupe par les ronces et y reste {a:coincé|coincée}. Le furet, en passant, s'arrête pour aider {A} à se dégager, et perd la course. Le moineau ne sait plus du tout quoi noter.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("teen_rival_end", 6, 12)), E(new FollowUp("teen_rival_end", 6, 12)),
                    Ai((AiAxis.Honor, -2)), Stress(("honest", 20), ("just", 20))),
            }),

        new EventDef("teen_rival_end", EventType.FollowUp, AnyStage, "Au bout du pont",
            "Le soir de la course, {A} trouve le furet, seul, au bout du vieux pont, les pattes dans le vide. Plus de sifflotement. « Là d'où je viens, dit le furet sans se retourner, quand on n'est pas premier, on n'existe pas. »",
            new[]
            {
                Plain("sit", "S'asseoir à côté, les pattes dans le vide",
                    "{A} s'assoit. Le soleil se couche. Au bout d'un moment, {A} dit : « Ici, on existe même septième. Demande à l'escargot. » Le furet rit, pour la première fois sans moquerie. Le lendemain, les deux courent ensemble, pour rien, juste pour courir.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
                Plain("rematch", "Proposer une revanche, chaque samedi",
                    "« Revanche samedi prochain ? Et celui d'après ? » Le furet se retourne, l'œil brillant. Depuis, chaque samedi, le village vient voir la course, et plus personne ne tient les comptes, surtout pas les deux coureurs.",
                    E(new ApplyModifier("fired_up")), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("teen_bakery", EventType.Pulse, Teen, "Le fournil à l'aube",
            "Le boulanger cherche quelqu'un pour l'aider le matin, avant le lever du soleil. Le salaire : quelques cailloux, et tous les croissants ratés. {A} se présente à quatre heures. Le fournil est chaud, sombre, et sent si bon que c'en est presque injuste.",
            new[]
            {
                Try("knead", "Pétrir la pâte, comme le boulanger montre", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "{A} pétrit, plie, laisse reposer, recommence. À six heures, la première fournée sort, dorée, parfaite. Le boulanger casse un croissant en deux, l'écoute craquer, et hoche la tête : « Demain aussi. »",
                    "{A} pétrit trop fort. Les croissants sortent en forme de cailloux. Le boulanger en goûte un quand même, très courageusement. « Demain, plus doucement. »",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("teen_bakery_week", 72, 120)), E(new LiftNeed(Need.Hunger, 0.2), new FollowUp("teen_bakery_week", 72, 120)),
                    Ai((AiAxis.Energy, 1), (AiAxis.Rationality, 1))),
                Plain("taste", "Goûter chaque fournée, par conscience professionnelle",
                    "{A} goûte la première fournée, puis la deuxième, par sécurité. À six heures, le boulanger compte les croissants, recompte, et regarde {A}, qui a des miettes jusqu'aux oreilles. « Demain, tu goûtes moins. Mais tu reviens. »",
                    E(new LiftNeed(Need.Hunger, 0.25), new FollowUp("teen_bakery_week", 72, 120)), Ai((AiAxis.Greed, 1), (AiAxis.Energy, -1)), Stress(("temperate", 15))),
                Plain("sing", "Chanter, pour que la pâte lève",
                    "Le boulanger assure que la pâte lève mieux en musique. {A} chante tout ce qu'on connaît, puis invente. La pâte lève. Le boulanger lève aussi un sourcil, mais ne dit rien, parce que la pâte a levé.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("teen_bakery_week", 72, 120)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("teen_bakery_week", EventType.FollowUp, AnyStage, "Le dernier jour au fournil",
            "Une semaine de fournées à l'aube. {A} sait maintenant pétrir, plier, et ne jamais ouvrir le four trop tôt. Le dernier jour, le boulanger tend une petite bourse, et pose à côté une boule de pâte crue. « Le salaire. Et ça, c'est pour ton pain à toi. Fais-en ce que tu veux. »",
            new[]
            {
                Plain("sun", "Façonner un pain en forme de soleil",
                    "{A} façonne un soleil aux rayons tressés. Le boulanger l'enfourne sans un mot, puis le pose en vitrine, avec une étiquette : « Pas à vendre ». La bourse, de son côté, pèse son poids de cailloux.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(15)), Ai((AiAxis.Rationality, 1))),
                Plain("long", "Faire un pain pour toute la place",
                    "Une seule boule de pâte, mais {A} l'étire, l'étire, et en fait un pain long comme un banc. Toute la place en mange un morceau. Le boulanger mesure le pain deux fois, et l'inscrit dans le carnet des records.",
                    E(new GiveCailloux(15), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("back", "Demander à revenir la semaine prochaine",
                    "{A} garde la boule de pâte dans un torchon et demande à revenir. Le boulanger sourit, pour la première fois de la semaine. « Quatre heures. Pas une minute de plus. » Avec le hérisson, ça fait deux personnes au village qui comptent les minutes.",
                    E(new GiveCailloux(15), new ApplyModifier("trade_sense")), Ai((AiAxis.Energy, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_hidden_garden", EventType.Pulse, Teen, "Sous le lierre",
            "Derrière la bibliothèque, un vieux mur couvert de lierre. {A} y est {a:passé|passée} cent fois. Aujourd'hui, le vent soulève le lierre, et dessous apparaît une petite porte en bois, sans poignée, entrouverte.",
            new[]
            {
                Try("push", "Pousser la porte", new EventChallenge(PlynlingStat.Courage, 6),
                    "La porte grince. Derrière : un jardin oublié, des fleurs hautes comme des arbres, une fontaine sèche, et un banc où quelqu'un a laissé un livre ouvert, la page cornée. Le jardin attend quelqu'un depuis longtemps.",
                    "La porte grince si fort que {A} s'enfuit. Une heure plus tard, {A} revient sur la pointe des pieds. La porte, patiente, attend toujours.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("teen_hidden_garden_owl", 24, 48)), E(new FollowUp("teen_hidden_garden_owl", 24, 48)), Ai((AiAxis.Boldness, 2))),
                Plain("ask", "Demander d'abord à la chouette où mène la porte",
                    "La chouette ferme son livre, très lentement. « Le jardin de la vieille bibliothécaire. Avant moi. » Puis, après un silence : « Personne n'y est entré depuis. Tu peux, si tu fais attention. »",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("teen_hidden_garden_owl", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Plain("ivy", "Remettre le lierre en place, et garder le secret",
                    "{A} remet le lierre en place, très soigneusement, et n'en dit rien à personne. Certaines portes sont plus belles fermées, avec un secret derrière. {A} repasse devant chaque jour, juste pour savoir.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        new EventDef("teen_hidden_garden_owl", EventType.FollowUp, AnyStage, "Le jardin de la bibliothécaire",
            "Dans le jardin, {A} a commencé à arracher les ronces, à remplir la fontaine, à replanter. Ce matin, quelqu'un est assis sur le banc, le livre à la page cornée sur les genoux : la chouette. « J'étais son élève, dit la chouette. Je n'ai jamais osé revenir. »",
            new[]
            {
                Plain("together", "Proposer de jardiner ensemble",
                    "Désormais, tous les matins, la chouette et {A} jardinent en silence. La fontaine coule de nouveau. Un jour, la chouette lit à voix haute la page cornée : un poème sur un jardin. La chouette pleure un peu. {A} fait semblant d'arroser.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
                Plain("give", "Rendre le jardin à la chouette",
                    "{A} tend le petit arrosoir à la chouette. « Le jardin vous attendait. » La chouette tient l'arrosoir longtemps, puis donne à {A} une petite clé de bronze, celle de la porte sous le lierre. « À nous deux, alors. »",
                    E(new GiveItem("col.cle_rouillee"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, 1))),
            }),

        new EventDef("teen_camp", EventType.Pulse, Teen, "L'île du lac",
            "Chaque été, les jeunes du village passent trois jours sur l'île du lac, avec un sac, une tente, et le moineau comme moniteur. Le radeau part dans une heure. {A} a fait son sac trois fois, et l'a défait deux fois.",
            new[]
            {
                Plain("light", "Partir avec presque rien, pour l'aventure",
                    "{A} emporte une couverture, un couteau à beurre et beaucoup d'optimisme. Sur le radeau, le moineau regarde le sac de {A}, puis {A}, et soupire profondément.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("teen_camp_night", 12, 24)), Ai((AiAxis.Boldness, 2))),
                Plain("everything", "Emporter tout ce qui pourrait servir",
                    "{A} emporte trois couvertures, une lanterne, une boussole, du miel, des pansements et un parapluie. Le radeau penche. Personne ne rit : tout le monde sait déjà chez qui on frappera quand la pluie viendra.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("teen_camp_night", 12, 24)), Ai((AiAxis.Rationality, 2))),
                Plain("stay", "Rester au village : trois jours, c'est long",
                    "{A} regarde le radeau partir depuis le ponton. Le village est très calme sans les jeunes. {A} aide la tortue au café, et le soir, entend des chansons qui viennent de l'île, portées par l'eau.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Boldness, -1), (AiAxis.Energy, -1))),
            }),

        new EventDef("teen_camp_night", EventType.FollowUp, AnyStage, "La nuit sur l'île",
            "Première nuit sur l'île. Le feu crépite, le moineau ronfle déjà sous sa tente, et quelqu'un vient de raconter l'histoire du monstre du lac, qui sort les nuits sans lune. Ce soir, justement, pas de lune. Et derrière les roseaux, quelque chose fait « plop ».",
            new[]
            {
                Try("look", "Aller voir ce qui fait « plop »", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} écarte les roseaux, la lanterne levée. Le monstre du lac est une vieille carpe, énorme, qui fait des bulles en dormant. {A} revient au feu et raconte, très sérieusement, avoir vu le monstre. C'est vrai, en un sens.",
                    "{A} s'avance, la lanterne levée. « Plop. » {A} recule. « Plop. » {A} est déjà sous la tente, sous trois couvertures. Le lendemain, on découvre une grenouille qui s'ennuyait.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("teen_camp_back", 24, 36)), E(new FollowUp("teen_camp_back", 24, 36)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("prank", "Faire « plop » à son tour, pour effrayer les autres", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} se glisse derrière la tente et fait « plop » avec un caillou dans l'eau. Panique générale, cris, une tente qui s'effondre. Le moineau se réveille, compte les têtes, et surveille {A} avec méfiance jusqu'à la fin du séjour.",
                    "{A} lance un caillou pour faire « plop ». Le caillou tombe dans la marmite de chocolat. Tout le monde sait d'où vient le caillou. On boit le chocolat quand même, caillou compris.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("teen_camp_back", 24, 36)), E(new FollowUp("teen_camp_back", 24, 36)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
                Plain("story", "Raconter une autre histoire, plus rassurante",
                    "{A} raconte l'histoire du monstre du lac qui avait peur du noir, et qui venait, les nuits sans lune, se réchauffer près des feux de camp. Plus personne n'a peur. Quelqu'un laisse même une couverture près des roseaux, au cas où.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("teen_camp_back", 24, 36)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("teen_camp_back", EventType.FollowUp, AnyStage, "Le radeau du retour",
            "Le dernier matin, on démonte les tentes. Sur le radeau du retour, tout le monde est fatigué, sale et heureux. Le moineau distribue les médailles du séjour, des bouchons peints, une par personne. Le moineau arrive devant {A} avec la dernière, et hésite sur l'inscription.",
            new[]
            {
                Plain("medal", "Accepter la médaille, quoi qu'on y lise",
                    "La médaille dit : « Le plus inattendu ». {A} ne sait pas si c'est un compliment. Le moineau non plus. {A} la porte quand même tout l'été, et finit par décider que oui.",
                    E(new LiftNeed(Need.Happiness, 0.2), new ApplyModifier("light_heart")), Ai((AiAxis.Sociability, 1))),
                Plain("swim", "Sauter du radeau, et finir à la nage",
                    "{A} saute du radeau à mi-chemin et finit la traversée à la nage, sous les acclamations. Le moineau crie quelque chose sur la sécurité, puis saute aussi, parce que tout le monde saute. Le radeau arrive vide au ponton.",
                    E(new GrowStat(PlynlingStat.Courage), new LiftNeed(Need.Hygiene, 0.2)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
                Plain("keepsake", "Garder un souvenir de l'île au fond de la poche",
                    "Au fond de la poche de {A} : une coquille d'escargot vide, en spirale parfaite, ramassée sur la plage de l'île. Posée sur la table de chevet, la coquille sent encore l'eau douce et le feu de camp.",
                    E(new GiveItem("col.coquille_escargot"), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1))),
            }),

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

        // la guerre des farces (rivals: the jam on the door, the escalation, the count)
        new EventDef("grown_prank", EventType.Pulse, Grown, "La porte à la confiture",
            "Ce matin, la porte de {A} est couverte de confiture. Toute la porte. Au milieu, un mot collé : « Bonne journée. » L'écriture est celle de {B}. Aucun doute possible : {B} a même signé.",
            new[]
            {
                Try("salt", "Riposter : remplacer le sucre de {B} par du sel", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "Le lendemain matin, de la maison de {B}, un cri monte, suivi d'un long silence, puis d'un rire. Un rire qui promet beaucoup de choses. La guerre est déclarée.",
                    "{A} se glisse chez {B} avec le sel, et trouve sur la table un mot : « Pas le sucre. Trop facile. » {B} avait prévu. {A} rentre, très {a:vexé|vexée}, et planifie mieux.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_prank_war", 24, 48)), E(new FollowUp("grown_prank_war", 24, 48)), Ai((AiAxis.Vengefulness, 2)), Stress(("forgiving", 20))),
                Plain("taste", "Goûter la porte, et remercier",
                    "{A} goûte la porte. Confiture d'abricots, excellente. {A} laisse un mot à son tour : « Merci. Encore ? » Le lendemain, une deuxième couche. Fraise, cette fois. La guerre devient bizarre.",
                    E(new AffinityShift(5), new LiftNeed(Need.Hunger, 0.2), new FollowUp("grown_prank_war", 24, 48)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("wash", "Laver la porte, comme si de rien n'était",
                    "{A} lave la porte en sifflotant, pendant que {B} passe devant, exprès, trois fois. Pas un regard. Rien n'est plus agaçant que l'absence de réaction. {B} le sait. {A} aussi.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_prank_war", 24, 48)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Hostile),

        new EventDef("grown_prank_war", EventType.FollowUp, AnyStage, "L'escalade",
            "Une semaine de guerre. Chaussettes cousues ensemble, tasse collée au comptoir, faux mots d'amour du hérisson glissés sous les portes. Le village compte les points. Ce soir, {A} tient la farce ultime : une poule, une brouette, et la cheminée de {B}. Mais par la fenêtre éclairée, on voit {B}, {b:seul|seule} à table, l'air très fatigué.",
            new[]
            {
                Plain("knock", "Ranger la brouette, et frapper à la porte",
                    "{A} gare la brouette, la poule dedans, et frappe. {B} ouvre, {b:surpris|surprise}. « Tu as l'air à bout. » {B} hausse les épaules : « Les farces, ça prend du temps. Le reste aussi. » {A} entre. La poule aussi.",
                    E(new AffinityShift(10), new FollowUp("grown_prank_truce", 6, 12)), Ai((AiAxis.Compassion, 2))),
                Try("hen", "Exécuter la farce ultime", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "La poule descend par la cheminée, très calmement, et s'installe sur la table de {B}, en face. {B} et la poule se regardent. Puis {B} éclate de rire, si fort que tout le village l'entend. Point pour {A}.",
                    "La poule refuse la cheminée, saute de la brouette et rentre au poulailler. La brouette roule toute seule jusqu'à la porte de {B}, qui ouvre, voit {A}, la brouette, les plumes. « Sérieusement ? »",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_prank_truce", 6, 12)), E(new FollowUp("grown_prank_truce", 6, 12)),
                    Ai((AiAxis.Vengefulness, 1), (AiAxis.Boldness, 1)), Stress(("compassionate", 15))),
            },
            Target: TargetKind.Hostile),

        new EventDef("grown_prank_truce", EventType.FollowUp, AnyStage, "Dix-sept partout",
            "{A} et {B} se font face, un carnet ouvert entre les deux : le décompte des farces, tenu par le moineau. Égalité parfaite. Dix-sept partout. Le moineau attend, crayon en l'air.",
            new[]
            {
                Plain("draw", "Déclarer l'égalité, et signer la paix",
                    "{A} et {B} signent le carnet, côte à côte, sous « match nul ». Le moineau le fait encadrer au café. Depuis, les deux se saluent poliment, avec une lueur dans l'œil qui inquiète tout le village.",
                    E(new AffinityShift(20), new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1))),
                Plain("team", "Proposer une alliance : farcer le village, ensemble",
                    "« Dix-sept partout. Et si on s'occupait du hérisson, maintenant ? » {B} sourit lentement. Le lendemain, toutes les horloges du village avancent de douze minutes. Personne ne sait qui. Tout le monde sait qui.",
                    E(new AffinityShift(25), new ApplyModifier("sly")), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
            },
            Target: TargetKind.Hostile),

        // la route de la foire (CK3's travel with a companion: the road, the inn, the fair)
        new EventDef("grown_journey", EventType.Pulse, Grown, "La route de la foire",
            "La grande foire de la ville d'en bas, c'est trois jours de marche. {B} y va aussi, et propose de faire la route ensemble : « On partage les frais, la carte, et les ampoules. » Départ à l'aube, sac au dos.",
            new[]
            {
                Plain("together", "Accepter, et partir ensemble",
                    "{A} et {B} partent à l'aube. Au bout d'une heure, chacun connaît les chansons préférées de l'autre. Au bout de deux, plus personne ne les supporte. Au bout de trois, on les chante en chœur.",
                    E(new AffinityShift(10), new FollowUp("grown_journey_inn", 24, 48)), Ai((AiAxis.Sociability, 2))),
                Plain("map", "Accepter, à condition de tenir la carte",
                    "{A} tient la carte, et la tient à l'envers pendant deux heures, avant que {B} ne le fasse remarquer, très gentiment. La carte change de mains. Le chemin aussi.",
                    E(new AffinityShift(5), new FollowUp("grown_journey_inn", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
                Plain("alone", "Refuser : on marche mieux {a:seul|seule}",
                    "{A} part {a:seul|seule}, une heure plus tard. Sur la route, {A} aperçoit {B} loin devant, toujours à la même distance, comme un repère. Le soir, les deux feux de camp brillent à cent pas l'un de l'autre. Personne ne fait le premier pas.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Sociability, -1), (AiAxis.Boldness, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_journey_inn", EventType.FollowUp, AnyStage, "L'auberge du sanglier",
            "Le deuxième soir, une averse pousse {A} et {B} dans une auberge au bord de la route. L'aubergiste, un vieux sanglier, n'a plus qu'une chambre, avec un seul lit, étroit, et un fauteuil qui a l'air de mordre.",
            new[]
            {
                Plain("armchair", "Prendre le fauteuil, sans discuter",
                    "{A} prend le fauteuil. Le fauteuil mord, effectivement. À trois heures, {B} se lève sans un mot, et étend sa propre couverture sur {A}. Au matin, personne n'en parle.",
                    E(new AffinityShift(15), new FollowUp("grown_journey_fair", 24, 48)), Ai((AiAxis.Compassion, 2))),
                Try("coin", "Tirer le lit à pile ou face", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} gagne à pile ou face. Puis propose la revanche. Puis la belle. Ça dure jusqu'à minuit, en riant. Finalement, personne ne dort dans le lit : les deux s'endorment sur la table, sur le jeu de cartes.",
                    "{A} perd à pile ou face, et s'installe dans le fauteuil. {B} montre alors la pièce : deux côtés face. Fou rire. Le sanglier apporte une deuxième couverture, par pitié.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(10), new FollowUp("grown_journey_fair", 24, 48)), E(new AffinityShift(10), new FollowUp("grown_journey_fair", 24, 48)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("talk", "Ne pas dormir, et parler toute la nuit",
                    "Ni lit, ni fauteuil : {A} et {B} s'assoient sous la fenêtre et parlent jusqu'à l'aube, de tout, des étoiles, du village, de choses jamais dites à personne. Au matin, les deux repartent avec des cernes et un drôle de sourire.",
                    E(new AffinityShift(20), new FollowUp("grown_journey_fair", 24, 48)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_journey_fair", EventType.FollowUp, AnyStage, "La grande foire",
            "La foire de la ville d'en bas : des étals à perte de vue, des musiciens, une odeur de pommes grillées. {A} et {B} ont quelques cailloux et une seule journée. Devant un stand, {B} s'arrête net devant une petite boîte à musique en forme d'escargot. Puis repart, trop vite.",
            new[]
            {
                Plain("gift", "Retourner discrètement acheter la boîte, pour {B}",
                    "{A} retourne au stand, achète la boîte et la cache dans son sac. Au dernier virage avant le village, {A} la tend à {B}. {B} ne dit rien, tourne la petite clé, et la boîte joue jusqu'au village.",
                    E(new AffinityShift(25), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2)), Stress(("greedy", 15))),
                Plain("apple", "Partager une pomme grillée, devant les musiciens",
                    "{A} et {B} partagent une pomme grillée sur une caisse, devant les musiciens. On ne dépense rien d'autre. On n'a besoin de rien d'autre.",
                    E(new AffinityShift(15), new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Sociability, 1))),
                Try("haggle", "Marchander les meilleurs prix, pour deux", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} marchande tout, partout, avec un aplomb terrifiant. À la fin de la journée, les deux repartent avec deux sacs pleins, et des cailloux encore en poche. {B} regarde {A} avec une admiration un peu inquiète.",
                    "{A} marchande si fort que le marchand de pommes, vexé, refuse de vendre quoi que ce soit. {B} doit acheter les pommes en cachette, à l'autre bout de la foire, et les deux rient tout le long du retour.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(10), new AffinityShift(10)), E(new AffinityShift(10)), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        // l'effraie du grenier (CK3's haunted-house mysteries, with a feathered answer)
        new EventDef("grown_ghost", EventType.Pulse, Grown, "Des pas au grenier",
            "Depuis trois nuits, on entend marcher dans le grenier de {A}. Des pas légers, un frottement, parfois un soupir. Le grenier est fermé à clé, et la clé est dans la poche de {A}. Ce soir, les pas reprennent.",
            new[]
            {
                Try("climb", "Monter au grenier, une bougie à la patte", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} monte. Les marches grincent. Au grenier, la bougie éclaire une vieille effraie, entrée par une tuile cassée, qui a fait son nid dans le carton des décorations de fête. L'effraie cligne des yeux. {A} aussi.",
                    "{A} monte, la bougie s'éteint, quelque chose souffle « hhhh » dans le noir, et {A} redescend toutes les marches d'un coup. En plein jour, le lendemain, on trouve l'explication : une vieille effraie, dans le carton des décorations.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_ghost_owl", 24, 48)), E(new FollowUp("grown_ghost_owl", 24, 48)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Plain("badger", "Demander au blaireau de monter avec soi",
                    "Le blaireau monte devant, un balai à la patte, très brave. Au grenier, une vieille effraie s'est installée dans le carton des décorations de fête. Le blaireau redescend, très digne, et ne reparle jamais du cri qui lui a échappé.",
                    E(new FollowUp("grown_ghost_owl", 24, 48)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("cotton", "Mettre du coton dans ses oreilles",
                    "{A} met du coton dans ses oreilles et dort très bien. Trois nuits plus tard, des plumes blanches descendent par l'escalier, et on finit par comprendre : une vieille effraie habite le grenier.",
                    E(new ApplyModifier("well_rested"), new FollowUp("grown_ghost_owl", 24, 48)), Ai((AiAxis.Energy, -1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_ghost_owl", EventType.FollowUp, AnyStage, "L'effraie du grenier",
            "L'effraie est vieille, presque blanche, et visiblement décidée à rester. La nuit, l'effraie chasse les souris du quartier. Le jour, l'effraie dort dans le carton des décorations, la tête sur une guirlande. Consultée, la chouette de la bibliothèque dit que c'est une lointaine cousine, très difficile.",
            new[]
            {
                Plain("stay", "La laisser rester, contre la chasse aux souris",
                    "Marché conclu : l'effraie reste, les souris partent. Le grenier n'a jamais été aussi tranquille. Parfois, la nuit, {A} entend un soupir là-haut, et ça a quelque chose de rassurant, maintenant.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Plain("box", "Lui construire un nichoir, dans le vieux chêne",
                    "{A} construit un nichoir au toit en pente, avec un carton de décorations neuf à l'intérieur, pour l'ambiance. L'effraie déménage après une semaine de réflexion. Sur le rebord, en partant, l'effraie laisse une plume, blanche et douce comme la neige.",
                    E(new GiveItem("col.plume"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_beetle_duel", EventType.Pulse, Grown, "Le duel du lucane",
            "Un lucane, très grand, très cornu, barre le chemin de {A}. « Tu as insulté ma mère. » {A} n'a jamais vu la mère du lucane. « Duel. Demain, à l'aube, sur la souche. » Le lucane repart sans attendre de réponse, en faisant claquer ses mandibules.",
            new[]
            {
                Try("fight", "Se présenter au duel, et lutter à la loyale", new EventChallenge(PlynlingStat.Courage, 8),
                    "À l'aube, sur la souche, {A} et le lucane luttent trois minutes. {A} renverse le lucane sur le dos, l'aide à se relever, et le lucane, ému, déclare l'honneur lavé. On apprend ensuite que le lucane s'était trompé de personne. Excuses longues et sincères.",
                    "À l'aube, sur la souche, le lucane soulève {A} au-dessus de sa tête, très facilement, et dépose {A} délicatement dans l'herbe. Puis demande : « Au fait, tu es bien… ? » Erreur sur la personne. Excuses longues et sincères.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("fired_up")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("talk", "Retrouver le lucane avant l'aube, et s'expliquer", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} trouve le lucane et pose trois questions. À la troisième, le lucane comprend que l'insulte venait de quelqu'un d'autre, qui portait le même chapeau. Le lucane offre un thé pour s'excuser. Le thé est très fort.",
                    "{A} s'explique. Le lucane écoute, hoche la tête, et répond : « Demain. À l'aube. » Le lendemain, le lucane ne vient pas : le lucane a fini par comprendre tout seul, et a beaucoup trop honte.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("champion", "Envoyer un champion : l'escargot",
                    "L'escargot accepte d'être le champion de {A}. À l'aube, l'escargot part vers la souche. Le lucane attend une heure, puis deux. L'escargot arrive à midi. Le lucane, épuisé d'attendre, déclare forfait.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        new EventDef("grown_portrait", EventType.Pulse, Grown, "Le portrait du café",
            "Une hermine peintre est de passage au village, avec un chevalet et une boîte de pinceaux. Le café veut un portrait pour son mur, et la tortue a choisi le modèle : {A}. La séance commence dans une heure. L'hermine taille déjà ses crayons.",
            new[]
            {
                Plain("noble", "Prendre la pose la plus noble possible",
                    "{A} prend la pose : menton levé, patte sur le cœur, regard au loin. Trois heures plus tard, le portrait est magnifique, et {A} ne sent plus sa nuque. Le tableau trône au café, et les clients le saluent en entrant.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1)), Stress(("humble", 15))),
                Plain("usual", "Faire comme d'habitude, sans poser",
                    "{A} boit son chocolat, lit le journal, chasse une miette. L'hermine peint tout. Le portrait est si ressemblant que l'escargot, en le voyant, dit bonjour au tableau, et attend la réponse.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Rationality, 1))),
                Try("still", "Tenir trois heures sans bouger", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} ne bouge pas. Pas un cil. Trois heures. L'hermine, impressionnée, offre à {A} une esquisse, avec une dédicace : « Au modèle le plus patient de la vallée ».",
                    "Au bout de dix minutes, le nez de {A} gratte. Au bout de vingt, l'oreille. Le portrait final montre {A} en train de se gratter, en plein mouvement, et c'est de loin le plus vivant de tout le café.",
                    E(new GrowStat(PlynlingStat.Stewardship), new LiftNeed(Need.Happiness, 0.15)), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            }),

        // ---- wave 8, second batch: ancien
        // les mémoires (CK3's commissioned books: the first page, the visitors, the reading)
        new EventDef("elder_memoirs", EventType.Pulse, Elder, "Les mémoires",
            "La chouette a offert à {A} un gros cahier relié, aux pages blanches. « Tes mémoires. Avant que tout s'envole. » Le cahier attend sur la table depuis une semaine. Ce matin, {A} débouche l'encrier. Reste à choisir par où commencer.",
            new[]
            {
                Plain("start", "Commencer par le tout début",
                    "{A} écrit : « Je suis {a:né|née} un mardi, d'après la rumeur. » Puis la suite vient toute seule : la première flaque, l'escargot, le héron. Le soir, quinze pages. La main tremble un peu. Pas les souvenirs.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("elder_memoirs_visitors", 48, 96)), Ai((AiAxis.Rationality, 1))),
                Plain("best", "Commencer par le plus beau souvenir",
                    "{A} ferme les yeux, choisit, et écrit le plus beau souvenir en premier, au cas où. Le reste suivra, dans le désordre. La vraie vie aussi était dans le désordre.",
                    E(new ApplyModifier("soothed"), new FollowUp("elder_memoirs_visitors", 48, 96)), Ai((AiAxis.Compassion, 1))),
                Plain("embellish", "Embellir un peu. Beaucoup",
                    "{A} écrit que la carpe dorée était grande comme une barque, que l'orage a duré dix jours, et que le héron, un jour, a ri. Le héron, qui lit par-dessus l'épaule, conteste le dernier point, très sèchement.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("elder_memoirs_visitors", 48, 96)), Ai((AiAxis.Honor, -1), (AiAxis.Boldness, 1)), Stress(("honest", 15))),
            }),

        new EventDef("elder_memoirs_visitors", EventType.FollowUp, AnyStage, "Les visiteurs",
            "La nouvelle a fait le tour du village : {A} écrit ses mémoires. Depuis, on frappe à la porte tous les jours. L'ours voudrait savoir s'y trouver, la tortue a « deux ou trois précisions », et l'escargot a apporté une liste de ses propres exploits, longue de quatre pages.",
            new[]
            {
                Plain("everyone", "Faire une place à tout le monde",
                    "{A} écoute tout le monde, note tout, et ajoute un chapitre par visiteur. Les mémoires de {A} deviennent celles du village entier. Le cahier ne suffit plus. La chouette en apporte un deuxième, sans rien dire.",
                    E(new ApplyModifier("cherished"), new FollowUp("elder_memoirs_reading", 72, 120)), Ai((AiAxis.Sociability, 2))),
                Plain("sign", "Accrocher un écriteau : « Mémoires en cours »",
                    "{A} accroche un écriteau sur la porte : « Mémoires en cours. Revenez au prochain chapitre. » Le village respecte l'écriteau, à peu près. Seul l'escargot attend devant, avec sa liste, toute la semaine.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("elder_memoirs_reading", 72, 120)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, -1))),
            }),

        new EventDef("elder_memoirs_reading", EventType.FollowUp, AnyStage, "La lecture",
            "Les mémoires sont finies. La chouette a organisé une lecture à la bibliothèque, et pour une fois, personne ne dit « chut ». Le village est là, au complet. {A} ouvre le cahier à la première page, et s'éclaircit la voix. Au premier rang, le héron s'est même assis.",
            new[]
            {
                Plain("all", "Tout lire, depuis le début",
                    "{A} lit pendant trois heures. On rit, on pleure, on proteste (l'ours), on corrige (la tortue). À la dernière page, le silence dure longtemps. Puis le héron se lève et, pour la première fois de mémoire de village, applaudit.",
                    E(new ApplyModifier("cherished"), new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 2))),
                Plain("library", "Confier le cahier à la bibliothèque, sans le lire",
                    "{A} pose le cahier sur le pupitre et le tend à la chouette : « Pour la réserve. Pour plus tard. » La chouette le range avec les livres que personne n'a le droit de lire, et y colle une étiquette : « À ouvrir quand on aura besoin de se souvenir. »",
                    E(new ApplyModifier("clear_conscience"), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Try("chapter", "Lire un seul chapitre, le plus important", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} choisit un seul chapitre : un matin ordinaire, au café, avec la tortue. Rien ne s'y passe. Tout le monde pleure. Au fond, la tortue sort son mouchoir propre, et le garde à la main longtemps.",
                    "{A} choisit un chapitre, puis un autre, puis s'aperçoit que tous sont les plus importants. La lecture dure jusqu'à minuit. Personne ne part.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("cherished")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("elder_acorn", EventType.Pulse, Elder, "Le gland",
            "{A} a ramassé un gland, ce matin, au pied du vieux chêne. Un beau gland, lisse, bien rond. Un chêne met cent ans à devenir grand. {A} retourne le gland dans sa patte, longtemps.",
            new[]
            {
                Plain("plant", "Le planter quand même, au milieu de la place",
                    "{A} plante le gland au milieu de la place, l'arrose, et plante à côté un petit panneau : « Pour plus tard. » Les petits du village viennent l'arroser à tour de rôle. Dans cent ans, quelqu'un s'assiéra à son ombre sans savoir. C'est très bien comme ça.",
                    E(new ApplyModifier("clear_conscience"), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 2))),
                Plain("pocket", "Le garder dans sa poche",
                    "Le gland va dans la poche, et y reste. Parfois, {A} le roule entre deux doigts en marchant. Ça ne porte pas spécialement bonheur. Mais ça rappelle qu'on peut toujours commencer quelque chose.",
                    E(new GiveItem("col.gland")), Ai((AiAxis.Rationality, 1))),
                Plain("give", "Le donner au plus petit du village",
                    "{A} donne le gland au plus petit du village, avec une seule consigne : « Plante-le le jour où tu en auras envie. » Le petit le serre dans sa patte comme un trésor, et court le montrer à tout le monde.",
                    E(new LiftNeed(Need.Happiness, 0.2), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

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

        new EventDef("baby_bear_birthday", EventType.Pulse, Baby, "L'anniversaire de l'ours",
            "C'est l'anniversaire de l'ours. Tout le village a apporté un cadeau. {A} n'a rien, et la fête commence dans une heure.",
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

        // ---- ado
        new EventDef("teen_catapult", EventType.Pulse, Teen, "La catapulte",
            "Avec une cuillère, un élastique et une planche, {A} a construit une catapulte. La catapulte lance des glands très loin. Personne ne sait encore exactement où.",
            new[]
            {
                Try("aim", "Viser le seau, au bout du jardin", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "Le gland atterrit pile dans le seau. Encore : pile dans le seau. Le blaireau, impressionné, commande une catapulte pour son magasin de jouets.",
                    "Le gland part de travers, traverse la haie, et rebondit sur le chapeau du hérisson. Le hérisson regarde le gland, puis la haie, puis note quelque chose dans son carnet.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(5)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
                Plain("far", "Lancer le plus loin possible",
                    "Le gland s'envole au-dessus du toit, de la gare, de la rivière. On ne le retrouvera jamais. Un chêne poussera peut-être un jour, très loin, grâce à {A}.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 2))),
                Plain("dismantle", "Démonter la catapulte avant l'accident",
                    "{A} démonte la catapulte et rend la cuillère à la cuisine. Personne ne saura jamais ce qui a failli arriver. C'est souvent le mieux.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
            }),

        new EventDef("teen_experiment", EventType.Pulse, Teen, "L'expérience",
            "Dans la cuisine, {A} mélange du vinaigre, du bicarbonate et un peu de jus de mûre, pour voir. Le bol se met à mousser. Beaucoup. De plus en plus.",
            new[]
            {
                Plain("notes", "Observer et prendre des notes",
                    "La mousse violette monte, déborde, et coule jusqu'au plancher. {A} note tout : le temps, la couleur, la hauteur. La chouette, à qui {A} montre le carnet, en demande une copie.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2))),
                Plain("lid", "Poser vite un couvercle",
                    "{A} pose un couvercle. Le couvercle s'envole jusqu'au plafond, et y reste collé. Le couvercle y est toujours. On s'y est habitué.",
                    E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Boldness, 1))),
                Plain("show", "Appeler tout le monde pour regarder",
                    "{A} appelle les voisins. Le temps que les voisins arrivent, la mousse a envahi la cuisine. On applaudit quand même, en pataugeant.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("teen_flute", EventType.Pulse, Teen, "La flûte du grenier",
            "{A} a trouvé une vieille flûte au grenier. Personne ne sait en jouer. Ça n'a jamais arrêté personne.",
            new[]
            {
                Try("practice", "S'entraîner tous les soirs", new EventChallenge(PlynlingStat.Learning, 6),
                    "Au bout d'une semaine, {A} joue un air entier, presque juste. Le moineau s'arrête de chanter pour écouter. C'est un compliment.",
                    "Au bout d'une semaine, {A} joue un air entier. Les chats du quartier aussi, en chœur. Les voisins proposent des leçons, avec insistance.",
                    E(new GrowStat(PlynlingStat.Learning)), Nothing, Ai((AiAxis.Rationality, 1))),
                Plain("window", "Jouer à la fenêtre, pour tout le village",
                    "{A} joue à la fenêtre, très fort, très faux, avec un enthousiasme magnifique. Le village ferme ses volets, un par un. Seul l'escargot reste, et applaudit à la fin, une heure plus tard.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("teen_flower_thief", EventType.Pulse, Teen, "Le voleur de fleurs",
            "Chaque matin, une fleur de plus disparaît du jardin de {A}. Proprement coupée, sans un brin par terre.",
            new[]
            {
                Try("watch", "Guetter à l'aube, derrière la fenêtre", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "À l'aube, un jeune lapin se glisse dans le jardin, coupe une fleur, et file… chez la vieille taupe aveugle, à qui le lapin apporte une fleur chaque matin, pour l'odeur. {A} plante une rangée de plus, exprès.",
                    "{A} guette, et s'endort à l'aube. Au réveil, une fleur manque, et à sa place, un petit mot : « Merci. C'est pour la taupe. »",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("cherished")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Rationality, 1))),
                Plain("sign", "Planter une pancarte : « Défense de cueillir »",
                    "{A} plante une pancarte. Le lendemain, une fleur manque, et la pancarte a été complétée : « Défense de cueillir (sauf pour la taupe) ».",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Honor, 1))),
            }),

        new EventDef("teen_arm_wrestling", EventType.Pulse, Teen, "Le bras de fer",
            "Au café, {B} pose le coude sur la table et tend la patte à {A}. « Bras de fer. Le perdant paie les chocolats. » Le café entier se retourne.",
            new[]
            {
                Try("wrestle", "Accepter, et serrer la patte", new EventChallenge(PlynlingStat.Courage, 6, VsTarget: true),
                    "{A} gagne, après une minute de grimaces des deux côtés. {B} paie les chocolats en riant, et réclame la revanche pour demain.",
                    "{B} gagne, d'un coup sec. {A} paie les chocolats. {B} boit la moitié du sien, et pousse le reste vers {A}.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(10)), E(new AffinityShift(10)), Ai((AiAxis.Boldness, 2))),
                Plain("thumbs", "Proposer plutôt un bras de fer… de pouces",
                    "Le bras de fer de pouces dure vingt minutes et finit en fou rire. Personne ne paie. Le café réclame une finale.",
                    E(new AffinityShift(15)), Ai((AiAxis.Sociability, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("teen_pillow_fight", EventType.Pulse, Teen, "Les polochons",
            "{A} dort chez {B}, pour la première fois. Minuit. Les lumières sont éteintes depuis longtemps. Dans le noir, un oreiller vole, et atterrit sur {A}.",
            new[]
            {
                Plain("fight", "Riposter, de toutes ses forces",
                    "{A} riposte. La bataille dure une heure, et se termine dans un nuage de plumes. On s'endort au milieu, à bout de souffle.",
                    E(new AffinityShift(15), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Energy, 2))),
                Plain("still", "Faire semblant de dormir",
                    "{A} ne bouge pas. {B} attend, perplexe, puis s'approche pour vérifier. {A} attrape {B} par surprise, et on rit jusqu'à l'aube.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(10)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("teen_broken_mirror", EventType.Pulse, Teen, "Le miroir cassé",
            "{A} a cassé le miroir de l'entrée. « Sept ans de malheur », annonce la pie, l'air de s'y connaître.",
            new[]
            {
                Plain("laugh", "Rire de la superstition",
                    "{A} rit. Le lendemain, {A} trouve un caillou en forme de cœur. Le surlendemain, une pièce. La pie, vexée, révise ses chiffres.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2))),
                Plain("ritual", "Faire le rituel de la pie pour conjurer le sort",
                    "Le rituel de la pie : tourner trois fois sur soi, toucher du bois, et donner un caillou à la pie. La pie remercie, très sérieusement. Le malheur, en tout cas, ne se montre pas.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Zeal, 1)), Stress(("cynical", 15))),
                Plain("mosaic", "Faire une mosaïque avec les morceaux",
                    "{A} colle les morceaux sur une planche. Le résultat reflète le monde en cent petits bouts. Le café l'accroche au mur.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_slang", EventType.Pulse, Teen, "Tout mirabelle",
            "Par accident, {A} a dit « c'est tout mirabelle » pour dire que c'était bien. Le lendemain, la moitié des jeunes du village le disent aussi.",
            new[]
            {
                Plain("claim", "Revendiquer l'invention",
                    "{A} rappelle à qui veut l'entendre qui a dit « tout mirabelle » en premier. Personne ne s'en souvient. Mais tout le monde le dit, et ça, c'est un peu à {A}.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Boldness, 1))),
                Plain("another", "Inventer une nouvelle expression",
                    "{A} tente « ça fait pomme ». Ça ne prend pas. Puis « c'est très escargot », pour dire lent. Ça prend tout de suite, au grand déplaisir de l'escargot.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_cabbage_hats", EventType.Pulse, Teen, "La mode du chou",
            "Au village, une nouvelle mode est arrivée : les chapeaux en feuilles de chou. Tous les jeunes en portent un. {A} n'en a pas.",
            new[]
            {
                Plain("make", "Fabriquer le plus beau chapeau de chou",
                    "{A} fabrique un chapeau de chou à trois étages, avec une plume. Le lendemain, la mode a changé : c'est {A} qui l'a changée.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1))),
                Plain("refuse", "Refuser la mode, par principe",
                    "{A} sort tête nue, très dignement. Une semaine plus tard, plus personne ne porte de chou, et {A} est la seule personne du village à n'avoir jamais eu l'air ridicule.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
                Plain("eat", "En acheter un, et le manger au déjeuner",
                    "{A} achète un chapeau de chou et le mange au déjeuner. C'est la mode la plus nourrissante de l'année.",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Greed, 1))),
            }),

        new EventDef("teen_fence", EventType.Pulse, Teen, "La palissade de la gare",
            "Quelqu'un a dessiné un énorme escargot à moustache sur la palissade de la gare. Le hérisson, furieux, dévisage tous les jeunes du village, un par un. Son regard s'arrête sur {A}, qui a de la craie sur les doigts. Pour une tout autre raison.",
            new[]
            {
                Try("explain", "Expliquer la craie, calmement", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "{A} montre le cahier de géométrie, plein de craie. Le hérisson vérifie, s'excuse, et demande à {A} de l'aider à trouver le vrai coupable. C'était l'escargot, qui voulait son portrait.",
                    "{A} explique, mais le hérisson ne croit rien. {A} passe l'après-midi à frotter la palissade. Le soir, le vrai coupable passe : l'escargot, qui voulait son portrait.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Honor, 1), (AiAxis.Sociability, 1))),
                Plain("improve", "Améliorer le dessin, tant qu'à faire",
                    "{A} ajoute un chapeau, un parapluie et un coucher de soleil. Le hérisson, d'abord furieux, finit par trouver ça « pas mal ». Le dessin reste.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
            }),

        new EventDef("teen_seed", EventType.Pulse, Teen, "La graine mystère",
            "La chouette a donné une graine à chaque jeune du village. Personne ne sait ce qui poussera. {A} a mis la sienne dans un pot, sur le rebord de la fenêtre.",
            new[]
            {
                Plain("care", "S'en occuper chaque jour, avec un carnet",
                    "Arrosage, soleil, notes. Au bout de dix jours : un petit pied de tomates cerises. Les premières sont pour la chouette.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
                Plain("talk", "Lui parler tous les soirs",
                    "{A} raconte sa journée au pot chaque soir. La plante pousse, pousse, et devient une courge rampante qui envahit la cuisine. Les histoires aident peut-être un peu trop.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("forget", "L'oublier un peu",
                    "{A} oublie le pot. Trois semaines plus tard, une fleur rouge magnifique a poussé, toute seule. Certaines choses poussent mieux quand on ne les regarde pas.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1))),
            }),

        new EventDef("teen_shopping", EventType.Pulse, Teen, "La liste des courses",
            "Pour la première fois, on a confié à {A} la liste des courses du mois, et la bourse qui va avec. La liste fait deux pages. La bourse, beaucoup moins.",
            new[]
            {
                Try("budget", "Tout acheter, et rapporter la monnaie", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "{A} négocie les pommes, compare les farines, et rentre avec tout, plus trois cailloux de monnaie, rendus fièrement.",
                    "{A} achète tout… sauf le sel, oublié. Et deux pots de miel en trop, achetés par erreur. Personne ne s'en plaint vraiment.",
                    E(new GrowStat(PlynlingStat.Stewardship)), E(new LiftNeed(Need.Hunger, 0.1)), Ai((AiAxis.Rationality, 1))),
                Plain("treat", "Tout acheter, plus un petit plaisir",
                    "{A} achète tout, et un chou à la crème, mangé sur le chemin du retour. La liste ne mentionnait pas de chou à la crème. La liste ne l'interdisait pas non plus.",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Greed, 1)), Stress(("temperate", 10))),
            }),

        new EventDef("teen_song_bridge", EventType.Pulse, Teen, "La chanson du pont",
            "{A} chante en traversant le vieux pont. Fort. Très fort. Au milieu du pont, le héron ouvre un œil.",
            new[]
            {
                Plain("louder", "Chanter encore plus fort",
                    "{A} chante plus fort. Le héron ouvre l'autre œil, puis s'envole sur la rive d'en face. Depuis, chaque fois que {A} approche, le héron change de rive, par précaution.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 2))),
                Plain("duet", "Inviter le héron à chanter aussi",
                    "Le héron réfléchit, puis pousse un cri long et grave, parfaitement juste. Personne n'avait jamais entendu chanter le héron. {A} non plus.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("teen_boredom", EventType.Pulse, Teen, "L'ennui",
            "Rien à faire. Tout le monde est occupé, le ciel est gris, et {A} a déjà relu tous ses livres deux fois. L'après-midi s'étire comme un chat.",
            new[]
            {
                Plain("tidy", "Ranger enfin sa chambre",
                    "{A} range la chambre, par désespoir. Sous le lit : un livre perdu, deux glands et une lettre jamais envoyée. L'après-midi finit en lecture.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
                Plain("game", "Inventer un jeu",
                    "{A} invente un jeu avec une balle, un mur et des règles de plus en plus compliquées. Le soir, trois jeunes du village jouent avec {A}. Les règles tiennent sur quatre pages.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Sociability, 1), (AiAxis.Energy, 1))),
                Plain("nap", "Faire une sieste de quatre heures",
                    "{A} fait une sieste de quatre heures. Au réveil, c'est déjà le soir, et ce n'est plus l'ennui : c'est le dîner.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Energy, -2))),
            }),

        new EventDef("teen_fish_tale", EventType.Pulse, Teen, "Grand comme ça",
            "Au café, {A} raconte avoir pêché un brochet grand comme ça. Les bras écartés au maximum. Tout le monde se tourne vers l'ours, qui a pêché le vrai brochet du village, l'an dernier.",
            new[]
            {
                Try("bluff", "Maintenir l'histoire, et en rajouter", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} en rajoute : le brochet parlait, et réclamait un avocat. Le café rit si fort que l'ours oublie de vérifier. L'histoire fait le tour du village avant le soir.",
                    "« Combien de kilos ? » demande l'ours. {A} dit un chiffre. L'ours dit le sien. Le brochet de {A} rétrécit à vue d'œil.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -1), (AiAxis.Boldness, 1)), Stress(("honest", 15))),
                Plain("truth", "Avouer : c'était une sardine",
                    "« C'était une sardine. Une grosse. » Le café éclate de rire, et l'ours offre à {A} une leçon de pêche pour dimanche.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
            }),

        new EventDef("teen_burnt_cake", EventType.Pulse, Teen, "Le gâteau brûlé",
            "{A} a voulu faire un gâteau pour la première fois. Une odeur de brûlé envahit la maison, et par la porte du four s'échappe une petite fumée noire.",
            new[]
            {
                Plain("save", "Sauver ce qui peut l'être",
                    "{A} gratte le dessus brûlé, et dessous, le gâteau est… presque bon. Avec beaucoup de confiture, très bon même.",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Rationality, 1))),
                Plain("again", "Tout recommencer, en lisant la recette, cette fois",
                    "Deuxième essai, recette sous les yeux. Le gâteau sort doré, parfait. {A} découvre que les recettes disent des choses utiles.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 2))),
                Plain("smoked", "Le servir comme un « gâteau fumé », exprès",
                    "{A} sert le gâteau fumé aux voisins, en expliquant que c'est une spécialité de la ville d'en bas. Les voisins sont polis. Très polis.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
            }),

        new EventDef("teen_lost_bet", EventType.Pulse, Teen, "Le pari perdu",
            "{A} a parié avec {B} que le moineau arriverait en retard au concours. Le moineau est arrivé en avance, pour la première fois de sa vie. Le gage : porter une tenue choisie par {B}, une journée entière.",
            new[]
            {
                Plain("honor", "Honorer le gage, la tête haute",
                    "{B} choisit un chapeau à fleurs, une cape et des chaussettes dépareillées. {A} porte tout, la tête haute, toute la journée. Le village applaudit au passage.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(15)), Ai((AiAxis.Honor, 2))),
                Try("haggle", "Négocier un gage plus raisonnable", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "Après négociation, le gage devient : un nœud papillon, pendant une heure. {B} admire la manœuvre, à contrecœur.",
                    "{A} négocie si mal que le gage passe à deux jours, avec un chapeau en plus. {B} en rit encore en rentrant.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(5)), E(new AffinityShift(10)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("teen_first_catch", EventType.Pulse, Teen, "La première prise",
            "L'ours a prêté sa vieille canne à {A}. Trois heures au bord de l'eau, sans une touche. Puis le bouchon plonge, d'un coup.",
            new[]
            {
                Try("pull", "Tirer de toutes ses forces", new EventChallenge(PlynlingStat.Courage, 6),
                    "{A} tire, tire, et sort de l'eau une truite dorée qui se débat dans le soleil. L'ours en a les larmes aux yeux : la canne n'avait rien pris depuis des années.",
                    "{A} tire de toutes ses forces, et sort de l'eau… une vieille botte. Dans la botte, une grenouille, très contrariée.",
                    E(new GrowStat(PlynlingStat.Courage), new LiftNeed(Need.Hunger, 0.2)), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
                Plain("release", "Remonter doucement, et relâcher le poisson",
                    "{A} remonte doucement un petit gardon, le regarde une seconde, et le remet à l'eau. L'ours hoche la tête : « Celui-là reviendra plus gros. »",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("teen_rowboat", EventType.Pulse, Teen, "La barque du blaireau",
            "Le blaireau prête sa vieille barque à {A} pour l'après-midi. L'étang est calme, le ciel bleu, et les rames sont presque de la même taille.",
            new[]
            {
                Try("island", "Ramer jusqu'à l'île du milieu", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "{A} rame jusqu'à l'île, en zigzag mais sans erreur, et pique-nique sous le saule. Au retour, le blaireau demande si la barque a pris l'eau. Non. Tant mieux.",
                    "{A} rame, mais toujours plus fort à gauche. La barque tourne en rond tout l'après-midi, sous l'œil très intéressé des canards.",
                    E(new GrowStat(PlynlingStat.Stewardship)), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Energy, 1))),
                Plain("drift", "Se laisser dériver, au fond de la barque",
                    "{A} s'allonge au fond de la barque et regarde les nuages. La barque dérive jusqu'aux roseaux, où {A} s'endort. Le soir, le blaireau vient chercher sa barque, et {A} avec.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1))),
            }),

        new EventDef("teen_bullies", EventType.Pulse, Teen, "Le goûter du mulot",
            "Derrière l'école, deux grands renardeaux ont pris le goûter d'un petit mulot, et le tiennent très haut, hors de portée. Le mulot ne pleure pas encore. Presque.",
            new[]
            {
                Try("stand", "S'interposer", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} s'interpose, très droit. Les renardeaux hésitent, se regardent, et rendent le goûter en marmonnant que c'était pour rire. Le mulot partage le goûter avec {A}.",
                    "{A} s'interpose, et un renardeau pose le goûter sur la tête de {A}, très haut. Le mulot et {A} sautillent ensemble. Puis le hérisson passe, ne rit pas du tout, et les renardeaux filent.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("clear_conscience")), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Honor, 1)), Stress(("craven", 20))),
                Plain("fetch", "Aller chercher le hérisson",
                    "{A} court chercher le hérisson, qui arrive un sifflet à la bouche. Les renardeaux rendent le goûter en un temps record. Le mulot suit {A} toute la semaine, comme une ombre reconnaissante.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Plain("share", "Partager son propre goûter avec le mulot",
                    "{A} partage sa tartine avec le mulot. Les renardeaux, sans public, s'ennuient et rendent l'autre goûter. Le mulot repart avec deux goûters.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 2))),
            }),

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
                    "{A} achète des bouchons en cire au marché. Le lendemain, {A} dort jusqu'à neuf heures, et rate le facteur, le livreur de pain et l'anniversaire de l'escargot.",
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

        // ---- ancien
        new EventDef("elder_birthdays", EventType.Pulse, Elder, "Les anniversaires d'avant",
            "Aujourd'hui, c'est l'anniversaire de {A}. Personne ne s'en souvient, ce qui arrive, à cet âge. {A}, en revanche, se souvient de tous les autres, un par un.",
            new[]
            {
                Plain("cake", "Se faire un gâteau, rien que pour soi",
                    "{A} fait un petit gâteau, plante une bougie, et chante, sans public. À la deuxième phrase, on frappe : la tortue, avec un gâteau aussi. La tortue s'en souvenait.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1))),
                Plain("remember", "Se rappeler les anniversaires d'autrefois",
                    "{A} s'assoit au soleil et se souvient : le gâteau tombé, la surprise ratée, la fois où tout le village avait chanté faux. Le soir, {A} est {a:heureux|heureuse} comme après une grande fête.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
                Plain("tell", "Le dire à tout le monde, au café",
                    "« C'est mon anniversaire. » Le café se tait, puis chante, très fort, très faux. La tortue sort une bougie de sous le comptoir. La tortue a toujours une bougie sous le comptoir.",
                    E(new LiftNeed(Need.Happiness, 0.25)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("elder_old_face", EventType.Pulse, Elder, "Un visage d'autrefois",
            "Au marché, une vieille cigogne s'arrête net devant {A}. « Toi ! La grande partie de balle ! Tu avais marqué contre ton camp ! » {A} n'en a aucun souvenir.",
            new[]
            {
                Plain("pretend", "Faire semblant de s'en souvenir",
                    "{A} fait semblant, et la cigogne raconte toute l'histoire. Au bout d'un moment, {A} s'en souvient vraiment, et les deux rient au milieu du marché comme des jeunes.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 1))),
                Plain("admit", "Avouer qu'on ne se souvient de rien",
                    "« Aucun souvenir. » La cigogne réfléchit, puis rit : « Moi non plus, en fait. C'était peut-être quelqu'un d'autre. » Les deux prennent un thé quand même, pour vérifier.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
            }),

        new EventDef("elder_unsent_letter", EventType.Pulse, Elder, "La lettre jamais envoyée",
            "Au fond d'un tiroir, {A} retrouve une lettre jamais envoyée : des excuses, écrites quarante ans plus tôt, pour une dispute dont {A} ne se rappelle plus la raison. Le destinataire, un vieux blaireau, habite toujours à l'autre bout du village.",
            new[]
            {
                Plain("send", "L'envoyer, enfin",
                    "{A} glisse la lettre dans la boîte. Trois jours plus tard, une réponse : « J'avais oublié la dispute. Toi, non. Viens prendre le thé. »",
                    E(new ApplyModifier("cherished"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, 1))),
                Plain("visit", "Porter la lettre en personne",
                    "{A} traverse le village, frappe, et tend la lettre. Le vieux blaireau la lit sur le seuil, puis serre {A} dans ses bras, sans rien dire, très longtemps.",
                    E(new ApplyModifier("soothed"), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 1), (AiAxis.Compassion, 1))),
                Plain("burn", "La brûler : c'est du passé",
                    "{A} brûle la lettre dans la cheminée. Ce qui devait être dit l'a été, quelque part, un jour, ou ne compte plus. La flamme est petite et chaude.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("elder_cane", EventType.Pulse, Elder, "La canne",
            "Le médecin a conseillé une canne à {A}. « Pour la sécurité. » Posée contre la porte, la canne a tout d'une ennemie.",
            new[]
            {
                Plain("carve", "La sculpter, et en faire une œuvre",
                    "{A} sculpte la canne : un escargot en haut, des feuilles tout le long, un héron au bout. Le village s'arrête pour l'admirer. {A} en oublierait presque que c'est une canne.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
                Plain("corner", "La laisser au coin de la porte",
                    "{A} laisse la canne au coin de la porte, par principe. Au bout d'une semaine, la canne sert à attraper les pommes du haut. C'est un début.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1))),
                Plain("adopt", "L'adopter, sans faire d'histoires",
                    "{A} prend la canne et se promène jusqu'à l'étang, plus loin que depuis des mois. La canne n'est pas une ennemie. C'est une collègue.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("elder_evening_bench", EventType.Pulse, Elder, "Le banc du soir",
            "Le soleil se couche derrière le moulin. {A} est {a:assis|assise} sur le banc devant chez soi, comme chaque soir. Le village rentre, et les lumières s'allument une à une.",
            new[]
            {
                Plain("count", "Compter les fenêtres qui s'allument",
                    "{A} compte les fenêtres qui s'allument : trente-deux. Chacune, {A} la connaît. Chacune a une histoire. Le compte est bon.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
                Plain("wave", "Saluer chaque passant",
                    "{A} salue chaque passant : le hérisson, l'ours, trois petits, la tortue qui rentre lentement. Tout le monde rend le salut. C'est une bonne journée qui se termine.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("elder_pardon", EventType.Pulse, Elder, "Pardon ?",
            "Depuis quelque temps, {A} entend un peu moins bien. Ce matin, au marché, {A} a demandé un chocolat et reçu un chapeau. Le chapeau est très joli, cela dit.",
            new[]
            {
                Plain("keep", "Garder le chapeau, et le porter",
                    "{A} paie le chapeau, le met, et redemande un chocolat, plus fort. Le chocolat arrive. Le chapeau reste. Bonne affaire.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Rationality, 1))),
                Plain("horn", "Se fabriquer un cornet acoustique",
                    "{A} fabrique un cornet en écorce. Ça marche très bien. Peut-être trop : {A} entend maintenant tous les potins du café, même ceux du fond.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("elder_knitting", EventType.Pulse, Elder, "Les aiguilles",
            "Un petit du village regarde {A} tricoter depuis une heure, sans un mot, la bouche ouverte. Les aiguilles cliquettent.",
            new[]
            {
                Plain("teach", "Lui apprendre, maille par maille",
                    "{A} pose de petites aiguilles dans les pattes du petit. Maille à l'endroit, maille à l'envers. Au bout d'une semaine, le petit tricote une écharpe bosselée, et l'offre à {A}.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
                Plain("gift", "Finir l'écharpe, et la lui offrir",
                    "{A} finit l'écharpe et la noue autour du cou du petit. Le petit la porte toute la saison, même les jours chauds.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Compassion, 1))),
            }),

        new EventDef("elder_knees", EventType.Pulse, Elder, "Les genoux savent",
            "Les genoux de {A} annoncent la pluie. Ce matin, les genoux annoncent un orage, un gros. Le ciel est pourtant tout bleu, et le village se moque gentiment.",
            new[]
            {
                Plain("warn", "Prévenir tout le monde quand même",
                    "{A} prévient tout le monde. On rit. À quinze heures, l'orage éclate. À quinze heures cinq, le village entier vient demander l'avis des genoux pour la semaine prochaine.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
                Plain("tea", "Se faire un thé, et attendre",
                    "{A} se fait un thé, ferme les volets, et attend. L'orage éclate à l'heure dite. Les genoux avaient raison. Les genoux ont toujours raison.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("elder_before_trains", EventType.Pulse, Elder, "Avant les trains",
            "Un petit du village demande à {A}, très sérieusement : « C'était comment, avant les trains ? » {A} ne se savait pas si {a:vieux|vieille}.",
            new[]
            {
                Plain("tale", "Inventer une histoire extraordinaire",
                    "{A} raconte qu'avant les trains, on voyageait à dos d'escargot géant, et que ça prenait des années. Le petit écoute, les yeux ronds. L'escargot, qui passait par là, ne dément pas.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("truth", "Dire la vérité : les trains existaient déjà",
                    "« Les trains existaient déjà. Mais le hérisson n'était pas encore chef de gare. » Le petit trouve ça encore plus incroyable.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Honor, 1))),
            }),

        new EventDef("elder_sunrise", EventType.Pulse, Elder, "Le lever du soleil",
            "{A} s'est {a:réveillé|réveillée} avant l'aube, sans raison. Dehors, tout est gris et silencieux. Le soleil va bientôt se lever, derrière la colline.",
            new[]
            {
                Plain("hill", "Monter sur la colline pour le voir",
                    "{A} monte la colline, lentement, et arrive au sommet pile au moment où le soleil se lève sur toute la vallée. Ça faisait longtemps. Ça valait chaque pas.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("soothed")), Ai((AiAxis.Energy, 1))),
                Plain("window", "Le regarder par la fenêtre, avec un thé",
                    "{A} prépare un thé et s'installe à la fenêtre. Le soleil se lève sur les toits, la gare, le moulin. Le premier oiseau chante. La journée commence bien.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1))),
            }),
    };

    private static readonly Dictionary<string, EventDef> ByKeyMap = All.ToDictionary(e => e.Key);

    public static EventDef? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    // Event text, expanded: names in bold, agreements resolved, {T} the new trait(s) in bold. Names must
    // already be safe (PlynlingCardUi.SafeName). Without a target, {B} never appears in the text. {T} is
    // replaced first, so a trait name is never read as a template.
    public static string Expand(string template, string a, PlynlingGender ga, string? b = null, PlynlingGender gb = PlynlingGender.Male,
        string? traits = null) =>
        PlynlingVisitStory.Expand(template.Replace("{T}", traits is null ? "" : $"**{traits}**"), a, ga, b ?? "", gb);
}
