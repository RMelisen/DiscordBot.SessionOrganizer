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

    // Append new modifiers at the end; keys are stored.
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
        new ModifierInfo("lucky", "Porte-bonheur", "Porte-bonheur", "🍀", "Trouve des trèfles à quatre feuilles sans même les chercher.",
            TimeSpan.FromDays(3), false, NoStats, Gift: 1.5),
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
    };

    private static readonly Dictionary<string, ModifierInfo> ByKeyMap = All.ToDictionary(m => m.Key);

    public static ModifierInfo? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

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
