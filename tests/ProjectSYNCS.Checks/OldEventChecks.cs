using System.Globalization;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// 55. The 50 old C# events, frozen (OldEvents.txt, written once from the C# on 2026-11-19 before they moved
// to story files, never edited since; the C# and the mode that wrote it are gone): each old key still
// resolves through PlynlingEvents.ByKey to a def that plays exactly as before — texts, options, effects,
// targets, delays, leans, stances and gates. A mismatch is fixed in the .story file, never in OldEvents.txt.
public static class OldEventChecks
{
    private static readonly string File = Path.Combine(StoryChecks.RepoRoot(), "tests", "ProjectSYNCS.Checks", "OldEvents.txt");

    private const int OldCount = 50;

    // The owner's approved wording changes made while moving, one line each: OldEvents.txt keeps the old
    // line, and the check reads it as the new one. An entry whose old line is not in the file fails.
    // teen_befriend_ask: the label was 82 characters with a 32-character name, over the story cap of 80 (2026-11-19).
    private static readonly (string Key, string Was, string Now)[] Reworded =
    {
        ("teen_befriend_ask", "option ducks | Jeter le plan, et donner du pain aux canards avec {B}", "option ducks | Jeter le plan, et nourrir les canards avec {B}"),
    };

    /// <summary>The name a fingerprint gives a key it points to: the old key a story scene claims (`was:`), else the key.</summary>
    public static string Name(string key) => PlynlingEvents.ByKey(key)?.Story?.Scene.Was ?? key;

    /// <summary>
    /// What a player can tell of a def, one fact per line, under the invariant culture. Never the def's own
    /// key, its Condition or its WeightBonus; stages and target filter only on what opens (a pulse or a
    /// triggered event).
    /// </summary>
    public static string Fingerprint(EventDef def, Func<string, string> name)
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            var lines = new List<string>();
            Write(def, name, lines, "");
            return string.Join("\n", lines);
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    private static string Esc(string? s) => s is null ? "-" : s.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r");

    private static void Write(EventDef def, Func<string, string> name, List<string> lines, string indent)
    {
        void L(string s) => lines.Add(indent + s);
        L("title " + Esc(def.Title));
        L("type " + def.Type);
        L("scene " + Esc(def.Scene));
        L("weight " + def.Weight);
        L("target " + def.Target);
        L("break " + def.BreakLevel);
        L("trigger " + (def.Trigger?.ToString() ?? "-"));
        if (def.Type is EventType.Pulse or EventType.Triggered)
        {
            L("stages " + string.Join(",", def.Stages));
            L("who " + Who(def.TargetCondition));
        }
        foreach (var o in def.Options)
        {
            L($"option {o.Key} | {Esc(o.Label)}");
            L("  success " + Esc(o.Outcome));
            L("  failure " + Esc(o.FailOutcome));
            L("  gate " + Gate(o.Gate));
            L("  challenge " + (o.Challenge is { } c ? $"{c.Stat} {c.Difficulty}{(c.VsTarget ? " vs" : "")}" : "-"));
            L("  on-success");
            foreach (var e in o.OnSuccess) Effect(e, name, lines, indent + "    ");
            L("  on-failure");
            foreach (var e in o.OnFailure) Effect(e, name, lines, indent + "    ");
            L("  stress " + Pairs(o.StressCosts.Select(kv => (kv.Key, kv.Value))));
            L("  lean " + Pairs(o.Ai.Select(kv => (kv.Key.ToString(), kv.Value))));
            L($"  owner-only {o.OwnerOnly} stance {o.Stance} mystery {o.Mystery}");
        }
    }

    private static string Pairs(IEnumerable<(string Key, int Value)> pairs)
    {
        var list = pairs.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}").ToList();
        return list.Count == 0 ? "-" : string.Join(",", list);
    }

    // Every bond × affinity −1/0/1 × can couple or not, as a 0/1 string; `any` with no filter.
    private static string Who(Func<TargetInfo, bool>? condition)
    {
        if (condition is null) return "any";
        var bits = new System.Text.StringBuilder();
        foreach (var bond in Enum.GetValues<PlynlingBond>())
            foreach (var affinity in new[] { -1, 0, 1 })
                foreach (var couple in new[] { false, true })
                    bits.Append(condition(new TargetInfo(new Plynling(), bond, affinity, 0, false, couple)) ? '1' : '0');
        return bits.ToString();
    }

    // A hidden trait gate reads the same written in C# (TraitGate) or in a story file ([needs trait k] [secret]).
    private static string Gate(EventGate? gate) => gate switch
    {
        null => "-",
        TraitGate t => $"trait {t.TraitKey} hidden",
        StoryGate { Secret: true, LockText: null, Needs: [{ Kind: ConditionKind.Trait, Negated: false } n] } => $"trait {n.Key} hidden",
        StoryGate s => $"story secret={s.Secret} lock={Esc(s.LockText)} needs="
            + string.Join(";", s.Needs.Select(n => $"{n.Kind} {n.Key} {n.Negated} {n.Op} {n.Number}")),
        _ => gate.ToString()!,
    };

    private static void Effect(EventEffect e, Func<string, string> name, List<string> lines, string indent)
    {
        switch (e)
        {
            case FollowUp f:
                lines.Add($"{indent}follow {name(f.EventKey)} {f.MinHours}-{f.MaxHours}");
                break;
            case AskTarget a when PlynlingEvents.ByKey(a.ResponseKey) is { } reply:
                lines.Add(indent + "ask {");
                Write(reply, name, lines, indent + "  ");
                lines.Add(indent + "}");
                break;
            case AskTarget a:
                lines.Add($"{indent}ask {a.ResponseKey} (missing)");
                break;
            default:
                lines.Add(indent + e);
                break;
        }
    }

    private static IEnumerable<string> Baseline()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            var pool = PlynlingEventEngine.AdoPool(PlynlingEvents.All);
            double Mean(Func<EventOption, double> f) => pool.Length == 0 ? 0 : pool.Average(d => d.Options.Average(f));
            double Grown(IReadOnlyList<EventEffect> effects, PlynlingStat s) => effects.OfType<GrowStat>().Where(g => g.Stat == s).Sum(g => g.Amount);
            var lines = new List<string>();
            foreach (var axis in Enum.GetValues<AiAxis>())
                lines.Add($"ado {axis} {Mean(o => o.Ai.GetValueOrDefault(axis)):F4}");
            foreach (var stat in PlynlingStats.All)
                lines.Add($"ado-grow {stat} {Mean(o => o.Challenge is null ? Grown(o.OnSuccess, stat) : (Grown(o.OnSuccess, stat) + Grown(o.OnFailure, stat)) / 2):F4}");
            return lines;
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    private static bool IsBaseline(string line) => line.StartsWith("ado ", StringComparison.Ordinal) || line.StartsWith("ado-grow ", StringComparison.Ordinal);

    public static void Run(Action<string> fail)
    {
        if (!System.IO.File.Exists(File)) { fail("old events: OldEvents.txt is missing"); return; }
        var blocks = new List<(string Key, List<string> Lines)>();
        var before = new List<string>();
        foreach (var line in System.IO.File.ReadAllLines(File))
        {
            if (line.StartsWith("== ", StringComparison.Ordinal)) blocks.Add((line[3..], new List<string>()));
            else if (IsBaseline(line)) before.Add(line);
            else if (blocks.Count > 0) blocks[^1].Lines.Add(line);
        }
        if (blocks.Count != OldCount) fail($"old events: OldEvents.txt holds {blocks.Count} blocks, not {OldCount}");

        foreach (var (key, was, now) in Reworded)
        {
            var block = blocks.FirstOrDefault(b => b.Key == key).Lines;
            var at = block?.IndexOf(was) ?? -1;
            if (at < 0) fail($"old events: the reworded line of {key} is not in OldEvents.txt: « {was} »");
            else block![at] = now;
        }

        foreach (var (key, stored) in blocks)
        {
            if (PlynlingEvents.ByKey(key) is not { } def) { fail($"old events: {key} no longer resolves"); continue; }
            var now = Fingerprint(def, Name).Split('\n');
            if (now.SequenceEqual(stored)) continue;
            var i = 0;
            while (i < now.Length && i < stored.Count && now[i] == stored[i]) i++;
            fail($"old events: {key} differs at line {i + 1}: was « {(i < stored.Count ? stored[i] : "(end)")} », now « {(i < now.Length ? now[i] : "(end)")} »");
        }

        var claimed = PlynlingEvents.All.Select(d => Name(d.Key)).Where(n => PlynlingEvents.Removed.ContainsKey(n)).ToList();
        if (claimed.Count > 0) fail("old events: a was: key is a removed key: " + string.Join(", ", claimed));

        var now2 = Baseline().ToList();
        foreach (var line in now2)
        {
            var parts = line.Split(' ');
            var old = before.FirstOrDefault(b => b.StartsWith($"{parts[0]} {parts[1]} ", StringComparison.Ordinal))?.Split(' ')[2] ?? "?";
            Console.WriteLine($"ado baseline: {parts[0]} {parts[1]} {old} → {parts[2]}");
        }
    }
}
