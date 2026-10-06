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
                    E(new ApplyModifier("lucky")), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Greed, 1))),
                Try("cheat", "Glisser son propre gland sous une coquille", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "La pie soulève la coquille, trouve un gland qui n'est pas le sien, et reste un long moment sans voix.",
                    "La pie voit tout. La pie voit toujours tout. {A} repart avec un clin d'œil professionnel, et sans son gland.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1)), Stress(("honest", 30), ("just", 20))),
                Plain("expose", "Montrer à tout le monde où est vraiment le gland",
                    "{A} tapote le bec de la pie. Le gland tombe sur le tonneau. Le marché rit, et la pie aussi, un peu jaune.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Honor, 2))),
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
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 2)), Stress(("greedy", 20))),
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
