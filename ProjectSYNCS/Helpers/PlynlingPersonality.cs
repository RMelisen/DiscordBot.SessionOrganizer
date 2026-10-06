using System.Text;
using Discord;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// What a Plynling's traits add up to: the sum of their AI axes, the two strongest of which name it
/// (« Piquante et intrépide »), and the line its card shows. Pure.
/// </summary>
public static class PlynlingPersonality
{
    // One adjective per axis and direction, M then F. The title is « {Adj1} et {adj2} ».
    private static readonly Dictionary<(AiAxis Axis, bool Positive), (string M, string F)> Adjectives = new()
    {
        [(AiAxis.Boldness, true)] = ("intrépide", "intrépide"),
        [(AiAxis.Boldness, false)] = ("prudent", "prudente"),
        [(AiAxis.Compassion, true)] = ("tendre", "tendre"),
        [(AiAxis.Compassion, false)] = ("piquant", "piquante"),
        [(AiAxis.Greed, true)] = ("avide", "avide"),
        [(AiAxis.Greed, false)] = ("désintéressé", "désintéressée"),
        [(AiAxis.Energy, true)] = ("infatigable", "infatigable"),
        [(AiAxis.Energy, false)] = ("nonchalant", "nonchalante"),
        [(AiAxis.Honor, true)] = ("loyal", "loyale"),
        [(AiAxis.Honor, false)] = ("roublard", "roublarde"),
        [(AiAxis.Rationality, true)] = ("réfléchi", "réfléchie"),
        [(AiAxis.Rationality, false)] = ("fantasque", "fantasque"),
        [(AiAxis.Sociability, true)] = ("bavard", "bavarde"),
        [(AiAxis.Sociability, false)] = ("solitaire", "solitaire"),
        [(AiAxis.Vengefulness, true)] = ("susceptible", "susceptible"),
        [(AiAxis.Vengefulness, false)] = ("conciliant", "conciliante"),
        [(AiAxis.Zeal, true)] = ("mystique", "mystique"),
        [(AiAxis.Zeal, false)] = ("terre-à-terre", "terre-à-terre"),
    };

    public static IReadOnlyDictionary<AiAxis, int> Axes(IEnumerable<TraitInfo> traits)
    {
        var sum = Enum.GetValues<AiAxis>().ToDictionary(a => a, _ => 0);
        foreach (var t in traits)
            foreach (var (axis, value) in t.Axes)
                sum[axis] += value;
        return sum;
    }

    // The two strongest axes, by size then by axis order (so ties always break the same way).
    public static string? Title(IReadOnlyList<TraitInfo> traits, PlynlingGender gender)
    {
        var words = Axes(traits)
            .Where(kv => kv.Value != 0)
            .OrderByDescending(kv => Math.Abs(kv.Value)).ThenBy(kv => kv.Key)
            .Take(2)
            .Select(kv => Adjectives[(kv.Key, kv.Value > 0)])
            .Select(a => gender == PlynlingGender.Female ? a.F : a.M)
            .ToList();
        if (words.Count == 0) return null;
        var first = char.ToUpperInvariant(words[0][0]) + words[0][1..];
        return words.Count == 1 ? first : $"{first} et {words[1]}";
    }

    // Childhood first, then personality in the order acquired, then coping.
    public static IEnumerable<TraitInfo> Ordered(IReadOnlyList<TraitInfo> traits) =>
        traits.Where(t => t.Kind == TraitKind.Childhood)
            .Concat(traits.Where(t => t.Kind == TraitKind.Personality))
            .Concat(traits.Where(t => t.Kind == TraitKind.Coping));

    // The card's personality line: the title and the trait emojis. Null without traits.
    public static string? CardLine(IReadOnlyList<TraitInfo> traits, PlynlingGender gender)
    {
        if (traits.Count == 0) return null;
        var emojis = string.Join(" ", Ordered(traits).Select(t => t.Emoji));
        return Title(traits, gender) is { } title ? $"🎭 *{title}* · {emojis}" : $"🎭 {emojis}";
    }

    // « 📜 Personnalité », answered privately: the title, each trait with what it does, and each stat
    // with where it comes from — CK3's tooltip, in an embed. The name is hostile input.
    public static Embed DetailEmbed(Plynling p, IReadOnlyList<TraitInfo> traits)
    {
        var text = new StringBuilder();
        if (Title(traits, p.Gender) is { } title) text.AppendLine($"*« {title} »*").AppendLine();
        if (traits.Count == 0)
            text.AppendLine("Pas encore de traits : ils arrivent en grandissant.");
        foreach (var t in Ordered(traits))
            text.AppendLine($"{t.Emoji} **{t.Name(p.Gender)}** — {t.Description}{Effects(t)}");

        var stats = string.Join("\n", PlynlingStats.Compute(p, traits).Select(StatText));
        return new EmbedBuilder()
            .WithTitle($"📜 Personnalité de {PlynlingCardUi.SafeName(p.Name)}")
            .WithColor(new Color(PlynlingCatalog.Info(p.Species).Accent))
            .WithDescription(text.ToString())
            .AddField("Statistiques", stats)
            .Build();
    }

    private static string Signed(int v) => v > 0 ? $"+{v}" : $"−{-v}";

    private static string Effects(TraitInfo t) => t.Stats.Count == 0
        ? ""
        : " *(" + string.Join(" · ", PlynlingStats.All.Where(t.Stats.ContainsKey).Select(s => $"{PlynlingStats.Emoji(s)} {Signed(t.Stats[s])}")) + ")*";

    private static string StatText(StatLine l)
    {
        var parts = new List<string> { $"base {l.Base}" };
        if (l.Passion != 0) parts.Add($"passion {Signed(l.Passion)}");
        if (l.Traits != 0) parts.Add($"traits {Signed(l.Traits)}");
        if (l.Growth != 0) parts.Add($"progrès {Signed(l.Growth)}");
        return $"{PlynlingStats.Emoji(l.Stat)} **{PlynlingStats.Name(l.Stat)} {l.Total}** · {string.Join(" · ", parts)}";
    }
}
