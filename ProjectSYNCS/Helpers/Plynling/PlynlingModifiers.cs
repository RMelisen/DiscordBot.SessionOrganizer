using System.Globalization;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public enum Need { Hunger, Happiness, Hygiene }

/// <summary>
/// A timed modifier. <see cref="Key"/> is stored (in <c>Plynling.Modifiers</c>) and **never renamed**.
/// Drain factors multiply how fast a need empties (0.8 = slower); Meal multiplies a meal's hunger,
/// Gift the happy gift's chance, StressDecay the morning's decay. <see cref="Negative"/> is exactly
/// "makes something worse" (the harness checks it): deciding alone never applies one, outside a
/// mental break. No modifier may speed hunger up.
/// </summary>
public sealed record ModifierInfo(
    string Key, string NameM, string NameF, string Emoji, string Description, TimeSpan Duration, bool Negative,
    IReadOnlyDictionary<PlynlingStat, int> Stats,
    double Hunger = 1, double Happiness = 1, double Hygiene = 1, double Meal = 1, double Gift = 1, double StressDecay = 1)
{
    public string Name(PlynlingGender g) => g == PlynlingGender.Female ? NameF : NameM;
}

public static class PlynlingModifiers
{
    private static readonly Dictionary<PlynlingStat, int> NoStats = new();
    private static Dictionary<PlynlingStat, int> S(PlynlingStat stat, int v) => new() { [stat] = v };

    // Append new modifiers at the end; keys are stored. Retired keys stay retired — never reuse one:
    // a row still holding it would read as the new meaning. Retired: "lucky" (« Porte-bonheur »), whose
    // uses became the story-specific rewards below. An unknown key on a row is skipped (Active).
    public static readonly IReadOnlyList<ModifierInfo> All = new[]
    {
        new ModifierInfo("inspired", "Inspiré", "Inspirée", "💡", "Les idées arrivent plus vite que les mots.",
            TimeSpan.FromDays(3), false, S(PlynlingStat.Learning, 2)),
        new ModifierInfo("fired_up", "Plein d'élan", "Pleine d'élan", "🔥", "Prêt à grimper n'importe quel arbre, même les grands.",
            TimeSpan.FromDays(3), false, S(PlynlingStat.Courage, 2)),
        new ModifierInfo("well_rested", "Bien reposé", "Bien reposée", "🛏️", "Une nuit si bonne que l'estomac fait la grasse matinée aussi.",
            TimeSpan.FromDays(2), false, NoStats, Hunger: 0.8),
        new ModifierInfo("light_heart", "Le cœur léger", "Le cœur léger", "🎈", "Les petits tracas glissent dessus comme la pluie sur une feuille.",
            TimeSpan.FromDays(2), false, NoStats, Happiness: 0.8),
        new ModifierInfo("soothed", "Apaisé", "Apaisée", "🫖", "Une tasse chaude, une couverture, et le monde peut attendre.",
            TimeSpan.FromDays(3), false, NoStats, Happiness: 0.8, StressDecay: 2),
        new ModifierInfo("grumpy", "Grognon", "Grognonne", "🌧️", "Tout agace. Surtout ce qui ne fait rien.",
            TimeSpan.FromDays(2), true, NoStats, Happiness: 1.3),
        new ModifierInfo("muddy_paws", "Les pattes sales", "Les pattes sales", "🐾", "Laisse des empreintes partout, même au plafond, on ne sait pas comment.",
            TimeSpan.FromDays(1), true, NoStats, Hygiene: 1.5),
        new ModifierInfo("sulky", "Boudeur", "Boudeuse", "😤", "Mange du bout des lèvres, pour bien montrer quelque chose.",
            TimeSpan.FromDays(2), true, NoStats, Meal: 0.8),
        new ModifierInfo("distracted", "Distrait", "Distraite", "🌀", "Commence trois phrases et n'en finit aucune.",
            TimeSpan.FromDays(3), true, S(PlynlingStat.Learning, -2)),
        new ModifierInfo("woods_cold", "Rhume des bois", "Rhume des bois", "🤧", "Éternue des feuilles mortes. C'est moins joli qu'on croit.",
            TimeSpan.FromDays(2), true, NoStats, Happiness: 1.2, Hygiene: 1.3),
        new ModifierInfo("fragrant", "Parfumé", "Parfumée", "🌿", "Laisse derrière soi une odeur de menthe et de dimanche.",
            TimeSpan.FromDays(3), false, NoStats, Hygiene: 0.7),
        // Rewards named for what happened, as CK3 does (« Honorable Soul », « Practicing Trade »…).
        new ModifierInfo("cherished", "Choyé", "Choyée", "💛", "Quelque part, quelqu'un a pensé à ce petit cœur. Ça réchauffe pour des jours.",
            TimeSpan.FromDays(3), false, NoStats, Happiness: 0.7),
        new ModifierInfo("clear_conscience", "La conscience tranquille", "La conscience tranquille", "🕊️", "Rien ne pèse, pas même un gland. Les soucis glissent tout seuls.",
            TimeSpan.FromDays(3), false, NoStats, StressDecay: 1.5),
        new ModifierInfo("well_spoken", "En verve", "En verve", "💬", "Trouve le mot juste avant même que la phrase commence.",
            TimeSpan.FromDays(3), false, S(PlynlingStat.Diplomacy, 2)),
        new ModifierInfo("trade_sense", "Le sens des affaires", "Le sens des affaires", "🧺", "Compte les glands d'un coup d'œil, et ne se trompe jamais sur la monnaie.",
            TimeSpan.FromDays(3), false, S(PlynlingStat.Stewardship, 2)),
        new ModifierInfo("sly", "Malicieux", "Malicieuse", "🦊", "Sait toujours où est caché le dernier biscuit.",
            TimeSpan.FromDays(3), false, S(PlynlingStat.Intrigue, 2)),
        new ModifierInfo("hearty", "Bien calé", "Bien calée", "🍲", "Chaque repas tient deux fois mieux au ventre.",
            TimeSpan.FromDays(2), false, NoStats, Meal: 1.25),
        new ModifierInfo("magpie_friend", "Ami des pies", "Amie des pies", "🪶", "Un objet brillant attend parfois sur le rebord de la fenêtre.",
            TimeSpan.FromDays(5), false, NoStats, Gift: 1.5),
        // Mental-break penalties (CK3 « On Edge », « Afraid », « Tense », « Sleep Deprived »).
        new ModifierInfo("on_edge", "À cran", "À cran", "⚡", "Répond avant même qu'on ait fini de parler. Et pas gentiment.",
            TimeSpan.FromDays(3), true, S(PlynlingStat.Diplomacy, -2)),
        new ModifierInfo("shaken", "Ébranlé", "Ébranlée", "🫨", "Sursaute quand une feuille tombe. Même une petite.",
            TimeSpan.FromDays(3), true, S(PlynlingStat.Courage, -2)),
        new ModifierInfo("tense", "Tendu", "Tendue", "😬", "Recompte trois fois les glands, et se trompe quand même.",
            TimeSpan.FromDays(3), true, S(PlynlingStat.Stewardship, -2)),
        new ModifierInfo("sleepless", "Les nuits blanches", "Les nuits blanches", "🌙", "Compte les moutons. Les moutons, eux, dorment très bien.",
            TimeSpan.FromDays(3), true, NoStats, StressDecay: 0.7),
        // What an event's GrowStat gives (Practice): +1 for a few days — the permanent +1 is the rare
        // exception (PlynlingEventEngine.GrowsForGood). One per stat, so a second lesson refreshes it.
        new ModifierInfo("practice_diplomacy", "En confiance", "En confiance", "🌼", "Trouve encore les mots qu'il faut, et le ton qui va avec.",
            TimeSpan.FromDays(5), false, S(PlynlingStat.Diplomacy, 1)),
        new ModifierInfo("practice_stewardship", "Bien organisé", "Bien organisée", "🧮", "Les glands sont comptés, les pots étiquetés, les cailloux triés par couleur.",
            TimeSpan.FromDays(5), false, S(PlynlingStat.Stewardship, 1)),
        new ModifierInfo("practice_learning", "La tête pleine", "La tête pleine", "📖", "Repense à la journée chaque soir, et en tire une petite leçon de plus.",
            TimeSpan.FromDays(5), false, S(PlynlingStat.Learning, 1)),
        new ModifierInfo("practice_intrigue", "L'œil malin", "L'œil malin", "🗝️", "Remarque les détails que personne ne voit. Et s'en souvient.",
            TimeSpan.FromDays(5), false, S(PlynlingStat.Intrigue, 1)),
        new ModifierInfo("practice_courage", "Le cœur bien accroché", "Le cœur bien accroché", "⛰️", "Grimpe un peu plus haut qu'hier. Sans regarder en bas.",
            TimeSpan.FromDays(5), false, S(PlynlingStat.Courage, 1)),
        // Wave 8: the endings of the long arcs (the exam, the mill party, the flood).
        new ModifierInfo("laureate", "Lauréat", "Lauréate", "🎓", "Un nom peint en lettres dorées, et la tête qui suit.",
            TimeSpan.FromDays(5), false, S(PlynlingStat.Learning, 2)),
        new ModifierInfo("party_soul", "L'âme de la fête", "L'âme de la fête", "🎉", "Entend encore la musique, longtemps après la fin du bal.",
            TimeSpan.FromDays(3), false, NoStats, Happiness: 0.75),
        new ModifierInfo("flood_hero", "Héros de la crue", "Héroïne de la crue", "🌊", "Le village salue bien bas au passage, et ça donne du cœur.",
            TimeSpan.FromDays(5), false, S(PlynlingStat.Courage, 2)),
        new ModifierInfo("guilty", "La conscience lourde", "La conscience lourde", "🪨", "Un petit papier plié, au fond d'une manche, pèse plus lourd qu'une pierre.",
            TimeSpan.FromDays(3), true, NoStats, StressDecay: 0.7),
    };

    private static readonly Dictionary<string, ModifierInfo> ByKeyMap = All.ToDictionary(m => m.Key);

    public static ModifierInfo? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    // The few days' +1 an event's GrowStat gives to one stat.
    public static ModifierInfo Practice(PlynlingStat stat) => ByKeyMap[stat switch
    {
        PlynlingStat.Diplomacy => "practice_diplomacy",
        PlynlingStat.Stewardship => "practice_stewardship",
        PlynlingStat.Learning => "practice_learning",
        PlynlingStat.Intrigue => "practice_intrigue",
        PlynlingStat.Courage => "practice_courage",
        _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
    }];

    // What the row holds, in order of end. Unknown keys and malformed entries are skipped, never thrown on.
    public static IReadOnlyList<(ModifierInfo Info, DateTimeOffset Ends)> Active(Plynling p)
    {
        var list = new List<(ModifierInfo, DateTimeOffset)>();
        foreach (var part in (p.Modifiers ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var bits = part.Split(':');
            if (bits.Length == 2 && ByKey(bits[0]) is { } info
                && long.TryParse(bits[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix))
                list.Add((info, DateTimeOffset.FromUnixTimeSeconds(unix)));
        }
        return list.OrderBy(x => x.Item2).ToList();
    }

    public static void Write(Plynling p, IEnumerable<(string Key, DateTimeOffset Ends)> modifiers)
    {
        var text = string.Join(";", modifiers.Select(m => $"{m.Key}:{m.Ends.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}"));
        p.Modifiers = text.Length == 0 ? null : text;
    }

    // Keeps only the active modifiers that pass `keep` — every removal (an end, a staff removal,
    // a refresh) goes through here. Plus `add`, when given. Callers rebase first (PlynlingLife).
    public static void Keep(Plynling p, Func<(ModifierInfo Info, DateTimeOffset Ends), bool> keep, (string Key, DateTimeOffset Ends)? add = null)
    {
        var kept = Active(p).Where(keep).Select(m => (m.Info.Key, m.Ends));
        Write(p, add is { } extra ? kept.Append(extra) : kept);
    }

    public static DateTimeOffset? NextEnd(Plynling p) => Active(p).Select(m => (DateTimeOffset?)m.Ends).FirstOrDefault();

    // A need's drain multiplier now: the product of its modifiers, clamped — hunger can only slow.
    public static double Multiplier(Plynling p, Need need)
    {
        var product = Active(p).Aggregate(1.0, (m, x) => m * need switch
        {
            Need.Hunger => x.Info.Hunger,
            Need.Happiness => x.Info.Happiness,
            _ => x.Info.Hygiene,
        });
        return need == Need.Hunger ? Math.Clamp(product, 0.5, 1) : Math.Clamp(product, 0.5, 2);
    }

    public static double MealFactor(Plynling p) => Math.Clamp(Active(p).Aggregate(1.0, (m, x) => m * x.Info.Meal), 0.5, 2);
    public static double GiftFactor(Plynling p) => Math.Clamp(Active(p).Aggregate(1.0, (m, x) => m * x.Info.Gift), 0.5, 2);
    public static double StressDecayFactor(Plynling p) => Math.Clamp(Active(p).Aggregate(1.0, (m, x) => m * x.Info.StressDecay), 0.5, 2);
    public static int StatDelta(Plynling p, PlynlingStat stat) => Active(p).Sum(x => x.Info.Stats.GetValueOrDefault(stat));
}
