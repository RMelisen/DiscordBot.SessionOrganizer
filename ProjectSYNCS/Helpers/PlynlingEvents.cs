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
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 2))),
                Plain("cafe", "« …tenir le café, comme la tortue ! »",
                    "« Alors entraîne-toi », dit le héron, en tendant une tasse vide. {A} sert un café imaginaire, rend une monnaie imaginaire, et réclame un pourboire bien réel.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Greed, 1), (AiAxis.Sociability, 1))),
                Plain("owl", "« …tout savoir, comme la chouette ! »",
                    "« Alors dis-moi combien j'ai de plumes », dit le héron. {A} commence à compter. Le soleil se couche à la plume trois cent quatre.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2))),
                Plain("chief", "« …chef du village, et de tout le monde ! »",
                    "« Très bien, chef », dit le héron, sans bouger d'une plume. {A} donne trois ordres au héron, deux à l'escargot et un à la rivière. Seul l'escargot obéit, mais l'escargot allait déjà dans ce sens.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1)), gate: new TraitGate("bossy")),
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

        new EventDef("teen_ghost_stories", EventType.Pulse, Teen, "Histoires au coin du feu",
            "La nuit est tombée. Autour du feu, {A} et {B} se racontent des histoires à faire peur. Une seule règle : le premier qui sursaute a perdu.",
            new[]
            {
                Try("story", "Raconter la plus effrayante, sans sursauter", new EventChallenge(PlynlingStat.Courage, 6, VsTarget: true),
                    "{A} raconte le hérisson qui a perdu sa montre, une nuit, et qui la cherche encore, à pas lents, de jardin en jardin. Au moment où une brindille craque — c'était le hérisson, au loin, qui cherchait vraiment sa montre — {B} saute en l'air. Victoire.",
                    "{B} raconte la tortue qui, la nuit, recompte les cailloux de sa caisse, un par un, en se rapprochant des maisons. Au même moment, quelque chose bouge dans les fourrés. {A} saute si haut qu'une chouette doit s'écarter.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(5)), E(new AffinityShift(5)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("sheet", "S'éclipser, et grogner comme le blaireau dans les fourrés", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} grogne dans les fourrés, exactement comme le vieux blaireau. {B} hurle, puis rit, puis jure de se venger. Au loin, le vrai blaireau grogne en retour. Plus personne ne rit. Par précaution, {A} dort la lumière allumée pendant une semaine.",
                    "{A} grogne si fort que ça finit en quinte de toux. {B} tend une tasse d'eau vers les fourrés, sans même s'arrêter de raconter.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(5), new ApplyModifier("sly")), Nothing, Ai((AiAxis.Honor, -1), (AiAxis.Boldness, 1))),
                Plain("kind", "Finir sur une histoire qui finit bien",
                    "{A} raconte le blaireau grognon qui voulait juste qu'on lui rende son écharpe. {B} trouve ça nul, et réclame la suite.",
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

        new EventDef("grown_wish_oak", EventType.Pulse, Grown, "La fête des souhaits",
            "Ce soir, c'est la fête des souhaits : chacun accroche un souhait aux branches du vieux chêne, puis en décroche un autre au hasard, à réaliser avant la fin de la semaine. {A} a un bout de papier, un crayon, et beaucoup trop de souhaits pour un seul papier.",
            new[]
            {
                Plain("stones", "Souhaiter « beaucoup de cailloux »",
                    "{A} écrit son souhait en lettres bien rondes et l'accroche tout en haut, là où tout le monde le verra.",
                    E(new FollowUp("grown_wish_stone", 24, 72)), Ai((AiAxis.Greed, 2))),
                Plain("fly", "Souhaiter « voler, une fois »",
                    "{A} écrit « voler, une fois », et accroche le papier à la branche la plus haute, pour donner l'exemple.",
                    E(new FollowUp("grown_wish_fly", 24, 72)), Ai((AiAxis.Boldness, 1), (AiAxis.Zeal, 1))),
                Plain("blank", "Accrocher le papier sans rien écrire",
                    "{A} accroche le papier blanc. Qui le décrochera choisira pour {A}. Et un souhait qu'on n'écrit pas, on ne peut pas le rater.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Rationality, 1), (AiAxis.Zeal, -1))),
            }),

        new EventDef("grown_wish_stone", EventType.FollowUp, AnyStage, "Beaucoup de cailloux",
            "Au matin, devant la porte de {A} : un caillou. Un seul, mais grand comme la maison. À côté, l'ours s'essuie le front : c'est l'ours qui a décroché le souhait, l'ours l'a pris au pied de la lettre, et l'ours a roulé ce caillou depuis la carrière.",
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
            "Une cigogne atterrit devant chez {A}, consulte un petit papier, puis {A}. « C'est toi, le souhait ? Je l'ai décroché à la fête. Monte. Un seul tour. »",
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
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired")), Nothing, Ai((AiAxis.Rationality, 2))),
                Plain("humble", "Répondre : « Que je ne sais presque rien. »",
                    "La chouette ferme les yeux un long moment. Puis la chouette sort de son tiroir une petite clé et la tend à {A} : la clé de la réserve, où dorment les livres que personne n'a le droit de lire. « Maintenant, tu peux commencer. »",
                    E(new GrowStat(PlynlingStat.Learning), new GiveItem("col.cle_rouillee")), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1)), Stress(("arrogant", 20))),
            }),

        new EventDef("teen_apprentice_station", EventType.FollowUp, AnyStage, "La tournée d'inspection",
            "Dernier jour d'apprentissage. Le hérisson confie à {A} la grande tournée : vérifier chaque horloge du village, du café jusqu'au puits, et revenir à la gare avant le train de 17 h 03. Le hérisson ne regarde pas {A} partir. Le hérisson regarde sa montre.",
            new[]
            {
                Try("run", "Faire la tournée au pas de course", new EventChallenge(PlynlingStat.Stewardship, 7),
                    "{A} vérifie onze horloges, en remet trois à l'heure, réveille le coucou du café et pousse la porte de la gare à 17 h 02. Le hérisson range sa montre et tend à {A} une vieille pièce frappée d'une locomotive. « Pour ta première minute d'avance. »",
                    "L'horloge de la tortue retarde de deux heures, par principe. {A} discute, perd du temps, et arrive à 17 h 05. Le hérisson ne dit rien. Le silence est pire qu'un sermon.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveItem("col.piece_ancienne")), Nothing, Ai((AiAxis.Energy, 2)), Stress(("lazy", 20))),
                Plain("plan", "Tracer d'abord le chemin le plus court sur une carte",
                    "{A} passe une heure à tracer l'itinéraire, puis fait la tournée en marchant, sans jamais courir, et arrive à 17 h 03 pile. Le hérisson regarde le plan, longtemps. Le plan est accroché depuis au mur de la gare, sous verre.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("trade_sense")), Ai((AiAxis.Rationality, 2))),
            }),

        new EventDef("teen_apprentice_market", EventType.FollowUp, AnyStage, "La leçon de la pie",
            "Pour la dernière leçon, la pie emmène {A} au marché, le jour de la plus grande foule. « Aujourd'hui, tu me rapportes trois choses. Peu importe quoi. Mais personne ne doit te voir. Et tout doit être revenu à sa place avant ce soir. »",
            new[]
            {
                Try("borrow", "Emprunter, puis tout rendre sans être {a:vu|vue}", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} emprunte une cuillère, un ruban et la casquette du marchand de miel, les montre à la pie, et rend tout avant le soir sans que personne ne remarque rien. La pie fait la révérence, pour la première fois de sa vie, et offre à {A} une bague trouvée « on ne sait où ».",
                    "Le marchand de miel remarque l'absence de sa casquette au moment où {A} la porte sur la tête. Explications. Excuses. Pot de miel acheté pour se faire pardonner. Derrière le tonneau, la pie rit à s'en étouffer.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveItem("col.bague")), Nothing, Ai((AiAxis.Honor, -2)), Stress(("honest", 30), ("just", 20))),
                Plain("refuse", "Refuser : rendre, oui ; prendre, non",
                    "La pie penche la tête. « Bien. C'est la vraie leçon. Tout le monde sait prendre ; les meilleurs savent quand ne pas le faire. » Puis la pie rend à {A}, discrètement, trois boutons perdus depuis le début de la saison.",
                    E(new GiveItem("col.bouton"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("notes", "Regarder la pie faire, et prendre des notes",
                    "{A} observe la pie toute la journée. Le soir, le carnet contient trois pages de croquis, deux de théories et une liste intitulée « Comment ne plus jamais se faire avoir ». La pie lit la liste, et ajoute une ligne à la fin.",
                    E(new ApplyModifier("sly")), Ai((AiAxis.Rationality, 1))),
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
