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
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing,
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
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Compassion, 2), (AiAxis.Sociability, 1))),
            }),

        // ---- adulte et ancien
        new EventDef("grown_parcel", EventType.Pulse, Grown, "Le colis égaré",
            "Un colis attend devant la porte de {A}. Pas d'adresse, juste un champignon dessiné un peu de travers. Ça tinte quand on le secoue.",
            new[]
            {
                Try("open", "L'ouvrir", new EventChallenge(PlynlingStat.Courage, 7),
                    "Dedans : une clochette, et un mot. « Pour sonner quand tu as besoin d'aide. » Pas de signature. {A} la garde près de son lit.",
                    "Dedans : une clochette qui sonne toute seule, toute la nuit. {A} la rapporte au marché au matin, les yeux cernés.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing,
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
    };

    private static readonly Dictionary<string, EventDef> ByKeyMap = All.ToDictionary(e => e.Key);

    public static EventDef? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    // Event text, expanded: names in bold, agreements resolved. Names must already be safe
    // (PlynlingCardUi.SafeName). Without a target, {B} never appears in the text.
    public static string Expand(string template, string a, PlynlingGender ga, string? b = null, PlynlingGender gb = PlynlingGender.Male) =>
        PlynlingVisitStory.Expand(template, a, ga, b ?? "", gb);
}
