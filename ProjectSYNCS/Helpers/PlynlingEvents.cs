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
                    Ai((AiAxis.Boldness, 2), (AiAxis.Energy, 1)), Stress(("craven", 20))),
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
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing,
                    Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1)), Stress(("shy", 20))),
                Try("swap", "L'échanger discrètement contre une plus jolie", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "Premier prix. {A} range la médaille au fond d'un tiroir et n'en parle jamais.",
                    "La plus jolie appartenait au moineau. Le silence qui suit dure longtemps.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing,
                    Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1)), Stress(("honest", 40), ("just", 20))),
                Plain("polish", "Aider les autres à cirer les leurs",
                    "{A} passe l'après-midi à faire briller les pommes de pin des autres. Personne ne gagne grâce à ça, mais tout le monde brille.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 2), (AiAxis.Sociability, 1))),
            }),

        // ---- adulte et ancien
        new EventDef("grown_parcel", EventType.Pulse, Grown, "Le colis égaré",
            "Un colis attend devant la porte de {A}. Pas d'adresse, juste un champignon dessiné un peu de travers. Ça tinte quand on le secoue.",
            new[]
            {
                Try("open", "L'ouvrir", new EventChallenge(PlynlingStat.Courage, 7),
                    "Dedans : une clochette, et un mot. « Pour sonner quand tu as besoin d'aide. » Pas de signature. {A} la garde près de son lit.",
                    "Dedans : une clochette qui sonne toute seule, toute la nuit. {A} la rapporte au marché au matin, les yeux cernés.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("lucky")), Nothing,
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
                    E(new StressChange(-90), new ApplyModifier("soothed")), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("drift", "Se laisser porter",
                    "{A} se laisse flotter quelques jours. Les choses glissent, puis reviennent doucement à leur place.",
                    E(new StressChange(-100), new ApplyModifier("distracted")), Ai((AiAxis.Energy, -2))),
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
