using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// CK3's AI personality. Each trait pushes these; the sum names the Plynling (PlynlingPersonality)
// and decides events for it when its owner does not (PlynlingEventEngine). Not stored.
public enum AiAxis { Boldness, Compassion, Greed, Energy, Honor, Rationality, Sociability, Vengefulness, Zeal }

/// <summary>
/// One trait. <see cref="Key"/> is stored and **never renamed**. <see cref="Group"/> is the
/// exclusion group: a Plynling never holds two personality traits of one group. The description is
/// shared by both genders, so it never agrees with the Plynling. Stress multipliers are CK3's
/// (PlynlingStress).
/// </summary>
public sealed record TraitInfo(
    string Key, TraitKind Kind, string Group, string NameM, string NameF, string DefaultEmoji, string Description,
    IReadOnlyDictionary<PlynlingStat, int> Stats, IReadOnlyDictionary<AiAxis, int> Axes,
    double StressGain = 1, double StressLoss = 1)
{
    public string Name(PlynlingGender gender) => gender == PlynlingGender.Female ? NameF : NameM;

    // Its tile (Assets/Icons/trait.<key>.png) once the bot's emojis are up, else the Unicode. Custom
    // markup: a button or select gets EmoteMarkup.Parse, never new Emoji.
    public string Emoji => ItemEmojis.For(ItemEmojis.TraitKey(Key)) ?? DefaultEmoji;
}

/// <summary>
/// The trait catalog, adapted from Crusader Kings III (values from the CK3 wiki's Traits page,
/// 2026-10-06; CK3's Martial and Prowess merge into Courage, taking the larger). Keys are CK3's
/// English names. Draws are hashed from the Plynling id (<see cref="StableRoll"/>), never a
/// <see cref="Random"/>, and the result is stored: appending a trait here changes only draws that
/// have not happened yet.
/// </summary>
public static class PlynlingTraits
{
    private const int ChildhoodSalt = 101;
    private const int PersonalitySalt = 110;    // + the slot: 110, 111, 112

    private static Dictionary<PlynlingStat, int> S(int dip = 0, int inte = 0, int sag = 0, int rus = 0, int cou = 0)
    {
        var d = new Dictionary<PlynlingStat, int>();
        void Put(PlynlingStat s, int v) { if (v != 0) d[s] = v; }
        Put(PlynlingStat.Diplomacy, dip); Put(PlynlingStat.Stewardship, inte); Put(PlynlingStat.Learning, sag);
        Put(PlynlingStat.Intrigue, rus); Put(PlynlingStat.Courage, cou);
        return d;
    }

    private static Dictionary<AiAxis, int> A(int bol = 0, int com = 0, int gre = 0, int ene = 0, int hon = 0,
        int rat = 0, int soc = 0, int ven = 0, int zea = 0)
    {
        var d = new Dictionary<AiAxis, int>();
        void Put(AiAxis a, int v) { if (v != 0) d[a] = v; }
        Put(AiAxis.Boldness, bol); Put(AiAxis.Compassion, com); Put(AiAxis.Greed, gre); Put(AiAxis.Energy, ene);
        Put(AiAxis.Honor, hon); Put(AiAxis.Rationality, rat); Put(AiAxis.Sociability, soc);
        Put(AiAxis.Vengefulness, ven); Put(AiAxis.Zeal, zea);
        return d;
    }

    private static TraitInfo C(string key, string m, string f, string emoji, string description,
        Dictionary<PlynlingStat, int> stats, Dictionary<AiAxis, int> axes) =>
        new(key, TraitKind.Childhood, "childhood", m, f, emoji, description, stats, axes);

    private static TraitInfo P(string group, string key, string m, string f, string emoji, string description,
        Dictionary<PlynlingStat, int> stats, Dictionary<AiAxis, int> axes, double gain = 1, double loss = 1) =>
        new(key, TraitKind.Personality, group, m, f, emoji, description, stats, axes, gain, loss);

    private static TraitInfo K(string key, string m, string f, string emoji, string description,
        Dictionary<PlynlingStat, int> stats, Dictionary<AiAxis, int> axes, double loss = 1) =>
        new(key, TraitKind.Coping, "coping", m, f, emoji, description, stats, axes, StressLoss: loss);

    // Order matters only for draws not yet made; append new traits at the end of their kind.
    public static readonly IReadOnlyList<TraitInfo> All = new[]
    {
        // ---- childhood: one at bébé, kept for life
        C("bossy", "Autoritaire", "Autoritaire", "📣", "Distribue les rôles avant même que le jeu ait commencé.",
            S(inte: 1, cou: 1), A(bol: 25, gre: 25, hon: 15, rat: 15, ven: 15)),
        C("charming", "Adorable", "Adorable", "🥺", "Obtient une deuxième part de tarte rien qu'en regardant la première.",
            S(dip: 1, rus: 1), A(gre: 25, soc: 25, com: 15, rat: 15, ven: 15, hon: -15)),
        C("curious", "Curieux", "Curieuse", "🔍", "Soulève chaque pierre du chemin pour voir qui habite dessous.",
            S(dip: 1, sag: 1), A(bol: 25, com: 25, ene: 15, hon: 15, soc: 15, ven: -15)),
        C("pensive", "Rêveur", "Rêveuse", "☁️", "Regarde passer les nuages et connaît le nom de la plupart.",
            S(inte: 1, sag: 1), A(rat: 25, ene: 15, hon: 15, gre: -15, bol: -15, soc: -25)),
        C("rowdy", "Turbulent", "Turbulente", "🌪️", "Arrive en courant, repart en courant, et renverse le pot de miel entre les deux.",
            S(rus: 1, cou: 1), A(bol: 25, ene: 25, soc: 15, ven: 15, com: -15, hon: -15, rat: -15)),

        // ---- personality: two at ado, the fourth trait at adulte
        P("bravery", "brave", "Courageux", "Courageuse", "🦁", "Va voir ce qui fait du bruit dans le noir, et revient le raconter.",
            S(cou: 3), A(bol: 200, ene: 20, soc: 20, rat: -20)),
        P("bravery", "craven", "Peureux", "Peureuse", "🫣", "Connaît toutes les cachettes du village. Par précaution.",
            S(rus: 2, cou: -3), A(rat: 10, ene: -20, soc: -20, bol: -200)),
        P("temper", "calm", "Calme", "Calme", "🍃", "Même le héron du vieux pont trouve ce calme un peu exagéré.",
            S(dip: 1, rus: 1), A(rat: 75, ene: -10, ven: -10, bol: -20), loss: 1.1),
        P("temper", "wrathful", "Colérique", "Colérique", "💢", "Tape du pied, souffle très fort, puis réclame un câlin.",
            S(dip: -1, rus: -1, cou: 3), A(bol: 35, ven: 20, ene: 10, com: -20, rat: -35)),
        P("romance", "chaste", "Pudique", "Pudique", "🙈", "Rougit quand on lui tient la patte, même pour traverser.",
            S(sag: 2), A(hon: 20, ene: 10, zea: 10, gre: -20, soc: -20)),
        P("romance", "lustful", "Fleur bleue", "Fleur bleue", "💘", "A déjà gravé un cœur sur trois arbres différents.",
            S(dip: 2), A(soc: 35, gre: 20, ene: 10, hon: -10, zea: -10)),
        P("ambition", "content", "Content", "Contente", "😌", "Une noisette et un rayon de soleil : la journée est réussie.",
            S(sag: 2, rus: -1), A(hon: 10, soc: -10, ven: -10, zea: -10, bol: -35, ene: -35, gre: -50), loss: 1.1),
        P("ambition", "ambitious", "Ambitieux", "Ambitieuse", "🏆", "Veut le plus gros gland, la plus haute branche et le titre qui va avec.",
            S(1, 1, 1, 1, 1), A(ene: 75, gre: 75, bol: 50, soc: 20, zea: 10, hon: -20)),
        P("work", "diligent", "Travailleur", "Travailleuse", "🧺", "Range ses cailloux par taille, puis par couleur, puis recommence.",
            S(dip: 2, inte: 3, sag: 3), A(ene: 75, bol: 35, rat: 20, ven: 10), loss: 0.5),
        P("work", "lazy", "Paresseux", "Paresseuse", "🛌", "Considère la sieste comme un métier à plein temps.",
            S(-1, -1, -1, -1, -1), A(gre: 10, com: -10, soc: -10, ven: -10, bol: -20, ene: -50)),
        P("constancy", "stubborn", "Têtu", "Têtue", "🪨", "Quand c'est non, même la tortue du café n'insiste plus.",
            S(inte: 3), A(hon: 35, ven: 35, rat: -10)),
        P("constancy", "fickle", "Lunatique", "Lunatique", "🌗", "Adore les myrtilles. Déteste les myrtilles. Ça dépend de l'heure.",
            S(dip: 2, inte: -2, rus: 1), A(bol: 20, hon: -20, rat: -20, ven: -20)),
        P("grudge", "forgiving", "Indulgent", "Indulgente", "🤲", "Pardonne avant même qu'on ait fini de s'excuser.",
            S(dip: 2, sag: 1, rus: -2), A(com: 35, hon: 20, rat: 10, ene: -10, ven: -200)),
        P("grudge", "vengeful", "Rancunier", "Rancunière", "📝", "Tient une liste. Personne ne sait qui est dessus.",
            S(dip: -2, rus: 2, cou: 2), A(ven: 200, ene: 10, hon: -10, rat: -10, com: -20)),
        P("greed", "generous", "Généreux", "Généreuse", "🎁", "Revient du marché avec moins de cailloux et plus d'amis.",
            S(dip: 3), A(com: 35, hon: 20, soc: 10, gre: -200)),
        P("greed", "greedy", "Radin", "Radine", "🪙", "Compte ses cailloux deux fois, et ceux des autres une fois.",
            S(dip: -2), A(gre: 200, hon: -10, com: -20)),
        P("sociability", "gregarious", "Sociable", "Sociable", "🗣️", "Connaît le prénom de chaque escargot du village.",
            S(dip: 2), A(soc: 200, com: 35, bol: 20)),
        P("sociability", "shy", "Timide", "Timide", "🫥", "Dit bonjour tout bas, pour ne déranger personne.",
            S(dip: -2, sag: 1), A(ven: -10, zea: -10, bol: -20, soc: -200)),
        P("honesty", "honest", "Franc", "Franche", "🫡", "Dit que la confiture est ratée, puis en reprend, par politesse.",
            S(dip: 2, rus: -4), A(hon: 50, soc: 20, bol: 10, com: 10)),
        P("honesty", "deceitful", "Menteur", "Menteuse", "🤥", "Jure que le gâteau était déjà comme ça avant son passage.",
            S(dip: -2, rus: 4), A(rat: 10, bol: -10, com: -10, hon: -50)),
        P("pride", "humble", "Modeste", "Modeste", "🌱", "Gagne le concours du moineau et s'excuse auprès des autres.",
            S(dip: 1, rus: -1), A(com: 20, hon: 20, ene: -10, gre: -50)),
        P("pride", "arrogant", "Vaniteux", "Vaniteuse", "🪞", "Se recoiffe dans chaque flaque du chemin.",
            S(dip: -1, cou: 1), A(bol: 35, gre: 20, soc: 20, ene: 10, com: -20, hon: -20, rat: -20)),
        P("justice", "just", "Juste", "Juste", "⚖️", "Coupe la tarte en parts égales. À la règle.",
            S(inte: 2, sag: 1, rus: -3), A(hon: 200, rat: 20, ven: 10, zea: 10)),
        P("justice", "arbitrary", "Capricieux", "Capricieuse", "🎲", "Change les règles du jeu à chaque tour, et gagne souvent.",
            S(inte: -2, sag: -1, rus: 3), A(bol: 10, com: -10, zea: -10, rat: -20, hon: -200), gain: 0.5),
        P("patience", "patient", "Patient", "Patiente", "⏳", "Attend que l'escargot finisse sa phrase.",
            S(sag: 2), A(rat: 35, ven: 10, ene: -10, bol: -20)),
        P("patience", "impatient", "Impatient", "Impatiente", "⏰", "Ouvre le four toutes les deux minutes pour voir si c'est prêt.",
            S(sag: -2), A(bol: 20, ene: 10, ven: -10, rat: -35)),
        P("appetite", "temperate", "Frugal", "Frugale", "🥣", "Une baie le matin, une baie le soir. Et ça suffit, à ce qu'on dit.",
            S(inte: 2), A(ene: 10, ven: -10, gre: -35)),
        P("appetite", "gluttonous", "Gourmand", "Gourmande", "🍯", "A goûté chaque pot de miel du village. Deux fois.",
            S(inte: -2), A(gre: 35, ene: -10), loss: 1.1),
        P("trust", "trusting", "Confiant", "Confiante", "🤝", "Prête son écharpe au premier venu. L'écharpe n'est jamais revenue.",
            S(dip: 2, rus: -2), A(hon: 35, soc: 35, com: 20, rat: -20, ven: -20)),
        P("trust", "paranoid", "Méfiant", "Méfiante", "👀", "Renifle chaque cadeau avant de dire merci.",
            S(dip: -1, rus: 3), A(ven: 20, com: -10, hon: -20, rat: -20, soc: -35), gain: 2),
        P("belief", "zealous", "Superstitieux", "Superstitieuse", "🍀", "Garde le même gland en poche depuis toujours. Le seul jour sans le gland, la pluie est tombée.",
            S(cou: 2), A(zea: 200, ene: 20, rat: -20)),
        P("belief", "cynical", "Sceptique", "Sceptique", "🤨", "Un gland porte-bonheur ? Pas vu, pas cru.",
            S(sag: 2, rus: 2), A(rat: 35, com: -10, ene: -20, zea: -200)),
        P("compassion", "compassionate", "Bienveillant", "Bienveillante", "💗", "Garde toujours une noisette en poche pour qui en aurait besoin.",
            S(dip: 2, rus: -2), A(com: 200, hon: 35, soc: 35, gre: -20)),
        P("compassion", "callous", "Froid", "Froide", "🧊", "Écoute les malheurs des autres en hochant la tête, puis passe à autre chose.",
            S(dip: -2, rus: 2), A(rat: 10, soc: -10, hon: -35, com: -200)),
        // Softened from CK3's Sadistic: it teases, nothing cruel.
        P("compassion", "sadistic", "Moqueur", "Moqueuse", "😏", "A un surnom pour tout le monde, et aucun n'est flatteur.",
            S(rus: 2, cou: 2), A(bol: 20, soc: 20, com: -100)),
        P("eccentric", "eccentric", "Excentrique", "Excentrique", "🎩", "Porte une feuille de chou en guise d'écharpe. Par conviction.",
            S(dip: -2, sag: 2), A(bol: 75, hon: -20, soc: -20, rat: -200), gain: 1.5, loss: 1.5),

        // ---- coping: from mental breaks only, never drawn here
        K("comfort_eater", "Mange ses émotions", "Mange ses émotions", "🍪", "Quand ça ne va pas, la réponse est dans la boîte à biscuits.",
            S(inte: -1), A(gre: 5, ene: -5)),
        K("inappetetic", "Sans appétit", "Sans appétit", "🥄", "Tourne la cuillère dans le bol sans rien avaler. Ça passera.",
            S(dip: -1, cou: -3), A(gre: -5, ene: -10), loss: 1.25),
        K("contrite", "Repentant", "Repentante", "🙏", "S'excuse pour des choses que personne n'avait remarquées.",
            S(rus: -2), A(com: 10, hon: 10, zea: 10, ven: -10)),
        K("improvident", "Imprévoyant", "Imprévoyante", "💸", "Donne ses cailloux au premier qui les regarde.",
            S(dip: 1), A(zea: 10, com: 10, gre: -10)),
        K("reclusive", "Reclus", "Recluse", "🐚", "A collé un mot sur sa porte : « Plus tard ».",
            S(dip: -2, inte: -1), A(bol: -10, ene: -10, soc: -35), loss: 1.5),
        K("irritable", "Irritable", "Irritable", "🌩️", "Mieux vaut ne pas lui parler avant sa sieste. Ni après.",
            S(dip: -2, cou: 2), A(bol: 10, ene: 10, ven: 10, com: -10, rat: -20)),
        K("profligate", "Dépensier", "Dépensière", "🛍️", "Revient du marché les bras chargés, sans savoir de quoi.",
            S(), A(gre: 10, com: -10)),
        K("confider", "Confident", "Confidente", "🫂", "Va mieux après avoir tout raconté à quelqu'un. Vraiment tout.",
            S(dip: 1), A(soc: 20, com: 10)),
        K("journaller", "Écrit son journal", "Écrit son journal", "📔", "Note tout dans un petit carnet, même la météo de ses humeurs.",
            S(sag: 1), A(rat: 10), loss: 1.5),
        K("athletic", "Sportif", "Sportive", "🏃", "Fait trois fois le tour du village en courant quand quelque chose ne va pas.",
            S(cou: 1), A(ene: 25, bol: 5)),
    };

    private static readonly Dictionary<string, TraitInfo> ByKeyMap = All.ToDictionary(t => t.Key);
    private static readonly TraitInfo[] Childhood = All.Where(t => t.Kind == TraitKind.Childhood).ToArray();
    private static readonly TraitInfo[] Personality = All.Where(t => t.Kind == TraitKind.Personality).ToArray();
    private static readonly TraitInfo[] Coping = All.Where(t => t.Kind == TraitKind.Coping).ToArray();
    public const int MaxCoping = 2;
    private const int CopingSalt = 400;         // + the instance

    public static TraitInfo? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    // A mental break's coping trait: uniform among those it lacks — never from its other traits. Null at two.
    public static TraitInfo? DrawCoping(int plynlingId, int salt, IReadOnlyCollection<string> held)
    {
        if (held.Count(k => ByKey(k)?.Kind == TraitKind.Coping) >= MaxCoping) return null;
        var pool = Coping.Where(t => !held.Contains(t.Key)).ToArray();
        return pool.Length == 0 ? null : Pick(pool, plynlingId, CopingSalt + salt);
    }

    // A named coping trait (an option's GainCoping), under the same cap as a drawn one.
    public static TraitInfo? CopingIfOwed(string key, IReadOnlyCollection<string> held) =>
        held.Count(k => ByKey(k)?.Kind == TraitKind.Coping) >= MaxCoping || held.Contains(key)
            ? null
            : ByKey(key) is { Kind: TraitKind.Coping } trait ? trait : null;

    // How many personality traits each stage is owed (the childhood one is owed from bébé on).
    private static int PersonalityOwed(PlynlingStage stage) => stage switch
    {
        PlynlingStage.Baby => 0,
        PlynlingStage.Teen => 2,
        _ => 3,
    };

    /// <summary>
    /// The traits a Plynling at <paramref name="stage"/> is owed and lacks, in the order to add them.
    /// Pure: the same id, stage and held traits always give the same answer. Slot n of the
    /// personality traits is always rolled with the same salt, so a Plynling drawn as an ado and
    /// again as an adulte keeps its first two and only adds the third. The adulte slot is uniform
    /// unless <paramref name="adultWeight"/> is given (the ado years, from the event history).
    /// </summary>
    public static IReadOnlyList<TraitInfo> Draw(int plynlingId, PlynlingStage stage, IReadOnlyCollection<string> held,
        Func<TraitInfo, double>? adultWeight = null)
    {
        var owned = held.Select(ByKey).OfType<TraitInfo>().ToList();
        var drawn = new List<TraitInfo>();
        if (!owned.Any(t => t.Kind == TraitKind.Childhood))
            drawn.Add(Pick(Childhood, plynlingId, ChildhoodSalt));

        var have = owned.Count(t => t.Kind == TraitKind.Personality);
        for (var slot = have; slot < PersonalityOwed(stage); slot++)
        {
            var taken = owned.Concat(drawn).Where(t => t.Kind == TraitKind.Personality).Select(t => t.Group).ToHashSet();
            var pool = Personality.Where(t => !taken.Contains(t.Group)).ToArray();
            // The adulte trait (slot 2) leans toward what its ado years were like, when that is known.
            drawn.Add(slot == 2 && adultWeight is not null
                ? StableRoll.Weighted(pool.Select(t => (t, Math.Max(0, adultWeight(t)))).ToList(), plynlingId, PersonalitySalt + slot, 0)
                : Pick(pool, plynlingId, PersonalitySalt + slot));
        }
        return drawn;
    }

    // The stage that brings a trait: the childhood one comes with bébé, personality slots 0 and 1
    // with ado, slot 2 with adulte.
    public static PlynlingStage StageOf(TraitKind kind, int personalitySlot) =>
        kind == TraitKind.Childhood ? PlynlingStage.Baby
        : personalitySlot < 2 ? PlynlingStage.Teen
        : PlynlingStage.Adult;

    // Whether a stage began within the last two days — reached, and recently. What happened then (its
    // « est devenu… » moment, the traits it brought) belongs in the journal; a backfill for an older
    // Plynling stays silent: a full journal would otherwise lose its oldest memories to traits it
    // « always had ».
    public static readonly TimeSpan JournalWindow = TimeSpan.FromDays(2);

    public static bool JustGained(PlynlingStage stage, TimeSpan age) =>
        age >= PlynlingLife.StageStart(stage) && age - PlynlingLife.StageStart(stage) < JournalWindow;

    private static TraitInfo Pick(TraitInfo[] pool, int plynlingId, int salt) => StableRoll.Pick(pool, plynlingId, salt, 0);
}
