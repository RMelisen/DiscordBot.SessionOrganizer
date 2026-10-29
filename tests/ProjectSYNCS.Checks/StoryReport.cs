using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Helpers;

/// <summary>
/// The path report the owner reads before a playthrough, in French: which endings can be reached and
/// how, where it ends when it decides alone, then every path written out as reading.
/// </summary>
public static class StoryReport
{
    private static readonly Regex Choice = new(@"\{[abp]:([^|{}]*)\|([^{}]*)\}", RegexOptions.Compiled);

    public static string Write(StoryArc arc, WalkResult walk)
    {
        var sb = new StringBuilder();
        var liveScenes = arc.Scenes.Count(s => s.RetiredTo is null);
        var endings = arc.Endings.Where(e => e.Key != "interrupted").ToList();
        sb.AppendLine($"{arc.Header.Title} ({arc.Key}) — {liveScenes} scènes, {endings.Count} fins, {walk.Paths.Count} chemins");
        if (walk.Problems.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("⚠ Problèmes");
            foreach (var p in walk.Problems) sb.AppendLine("  " + p);
        }

        sb.AppendLine();
        sb.AppendLine("Fins atteignables");
        var width = endings.Select(e => e.Key.Length).DefaultIfEmpty(0).Max() + 2;
        foreach (var e in endings)
        {
            var to = walk.Paths.Where(p => p.Ending.Key == e.Key).ToList();
            if (to.Count == 0) { sb.AppendLine($"  ⚠ {e.Key.PadRight(width)}0 chemin — jamais atteinte"); continue; }
            var line = $"  {e.Key.PadRight(width)}{Count(to.Count, "chemin")}   le plus court : {Count(to.Min(p => p.Steps.Count), "étape")}";
            if (to.All(p => p.FromLoss)) line += "   après la perte";
            if (Only(to) is { } via) line += "   seulement via " + via;
            sb.AppendLine(line);
        }

        sb.AppendLine();
        sb.AppendLine("Décidé seul (sans le propriétaire)");
        var alone = walk.Paths.Where(p => p.Alone && !p.FromLoss).OrderBy(p => p.Steps.Count).FirstOrDefault();
        sb.AppendLine(alone is null
            ? "  ⚠ aucun chemin ne se décide seul"
            : $"  {alone.Ending.Key} : {string.Join(" → ", alone.Steps.Select(s => s.Option is null ? s.Scene.Key : $"{s.Scene.Key}/{s.Option.Key}"))} (stress 0) ✓");
        sb.AppendLine("En personnage (jets à 50 %)");
        foreach (var (profile, shares) in walk.InCharacter)
        {
            var parts = shares.Where(kv => kv.Value >= 0.01).OrderByDescending(kv => kv.Value)
                .Select(kv => $"{kv.Key} {Math.Round(kv.Value * 100).ToString(CultureInfo.InvariantCulture)} %");
            sb.AppendLine($"  {profile} : {string.Join(" · ", parts)}");
        }

        for (var i = 0; i < walk.Paths.Count; i++)
        {
            var path = walk.Paths[i];
            sb.AppendLine();
            sb.AppendLine($"Chemin {i + 1} / {walk.Paths.Count} → {path.Ending.Key}{(path.FromLoss ? " (après la perte)" : "")}");
            for (var k = 0; k < path.Steps.Count; k++)
            {
                var step = path.Steps[k];
                if (step.Option is not { } o) { sb.AppendLine($"  {k + 1}. {Variants(step.Scene.Title)} — (sans choix)"); }
                else
                {
                    var tags = new List<string>();
                    if (o.Roll is { } r) tags.Add($"jet {PlynlingStats.Name(r.Stat)} {r.Difficulty} · {(step.Success == true ? "réussi" : "raté")}");
                    if (o.Needs.Count > 0) tags.Add("🔒 " + Lock(o));
                    if (o.OwnerOnly) tags.Add("propriétaire seulement");
                    tags.AddRange(o.StressCosts.Select(kv => $"stress {kv.Key} +{kv.Value}"));
                    sb.AppendLine($"  {k + 1}. {Variants(step.Scene.Title)} — « {Variants(o.Label)} »{(tags.Count > 0 ? "   " + string.Join(" · ", tags) : "")}");
                }
                // What it read before choosing, then what came of the choice.
                Prose(sb, step.Scene.Prose, "     ");
                if (StoryWalker.Outcome(step) is { } outcome) Prose(sb, outcome.Prose, "   → ");
            }
            sb.AppendLine($"  Fin : {Variants(path.Ending.Title)}");
            Prose(sb, path.Ending.Prose, "     ");
            sb.AppendLine("  Marques : " + Marks(arc, path));
            sb.AppendLine("  Mémoires : " + Memories(arc, path));
        }
        return sb.ToString();
    }

    private static string Count(int n, string word) => $"{n} {word}{(n > 1 ? "s" : "")}";

    // The step every path to an ending shares through a lock or a roll, when there is one.
    private static string? Only(List<WalkPath> paths)
    {
        static IEnumerable<(StoryOption Option, bool? Success)> Special(WalkPath p) =>
            p.Steps.Where(s => s.Option is not null && (s.Option.Needs.Count > 0 || s.Option.Roll is not null)).Select(s => (s.Option!, s.Success));
        var common = paths.Skip(1).Aggregate(Special(paths[0]).ToList(), (acc, p) => acc.Intersect(Special(p)).ToList());
        if (common.Count == 0) return null;
        var (o, success) = common[0];
        var how = success switch { true => " réussi", false => " raté", null => "" };
        return $"« {o.Key} »{how}{(o.Needs.Count > 0 ? $" ({Lock(o)})" : "")}";
    }

    private static string Lock(StoryOption o) => o.LockText is { } text ? Variants(text) : string.Join(", ", o.Needs.Select(Said));

    private static string Said(StoryCondition c)
    {
        var what = c.Kind switch
        {
            ConditionKind.Trait when PlynlingTraits.ByKey(c.Key) is { } t => t.NameM,
            ConditionKind.OtherTrait when PlynlingTraits.ByKey(c.Key) is { } t => "l'autre : " + t.NameM,
            ConditionKind.Stat or ConditionKind.Standing => $"{c.Kind.ToString().ToLowerInvariant()} {c.Key} {(c.Op == CompareOp.AtLeast ? ">=" : "<")} {c.Number}",
            _ => $"{c.Kind.ToString().ToLowerInvariant()} {c.Key}",
        };
        return c.Negated ? "pas " + what : what;
    }

    private static void Prose(StringBuilder sb, IReadOnlyList<StoryParagraph> prose, string indent)
    {
        foreach (var l in prose.SelectMany(p => p.Lines))
        {
            var when = l.When.Count == 0 ? "" : $"[si {string.Join(", ", l.When.Select(Said))}] ";
            sb.AppendLine(indent + when + Variants(l.Text));
        }
    }

    private static string Masculine(string text) => Choice.Replace(text, m => m.Groups[1].Value);

    // Both forms of an agreement, read once: « sorti/sortie », « il/elle », « collé(e) ».
    private static string Variants(string text) =>
        Choice.Replace(text, m => m.Groups[1].Length == 0 ? $"({m.Groups[2].Value})" : $"{m.Groups[1].Value}/{m.Groups[2].Value}");

    private static string Marks(StoryArc arc, WalkPath path)
    {
        var effects = path.Steps.Select(StoryWalker.Outcome).OfType<StoryOutcome>().SelectMany(o => o.Effects).Concat(path.Ending.Marks).ToList();
        var marks = new List<string>();
        foreach (var e in effects)
            switch (e.Kind)
            {
                case EffectKind.Epithet:
                    marks.Add($"épithète « {Masculine(arc.Epithets.FirstOrDefault(x => x.Key == e.Key)?.Title ?? e.Key)} »"); break;
                case EffectKind.Keepsake:
                    marks.Add($"souvenir « {arc.Keepsakes.FirstOrDefault(x => x.Key == e.Key)?.Name ?? e.Key} »"); break;
                case EffectKind.Trait:
                    var from = PlynlingTraits.ByKey(e.Key)?.NameM ?? e.Key;
                    marks.Add(e.To is null ? $"trait {from}" : $"trait {from} → {PlynlingTraits.ByKey(e.To)?.NameM ?? e.To}"); break;
                case EffectKind.Unlock:
                    marks.Add($"débloque {e.Key}"); break;
            }
        if (path.Cailloux > 0) marks.Add($"{path.Cailloux} cailloux");
        return marks.Count == 0 ? "aucune" : string.Join(" · ", marks);
    }

    private static string Memories(StoryArc arc, WalkPath path)
    {
        var given = path.Steps.Select(StoryWalker.Outcome).OfType<StoryOutcome>().SelectMany(o => o.Effects).Concat(path.Ending.Marks)
            .Where(e => e.Kind == EffectKind.Remember)
            .Select(e => (arc.Memories.FirstOrDefault(m => m.Key == e.Key)?.Wording ?? e.Key) + (e.Other ? " (l'autre)" : ""))
            .ToList();
        return given.Count == 0 ? "aucune" : string.Join(" · ", given);
    }
}
