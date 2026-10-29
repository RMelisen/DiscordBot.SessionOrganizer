using System.Text.RegularExpressions;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The checks a story needs before it may run: keys unique, references resolved, catalog keys real,
/// numbers in range, template tokens valid, Discord's caps kept at the worst case. Every problem is
/// reported, never just the first. The slow walks (reachability, deciding alone) live in the checks
/// project.
/// </summary>
public static class StoryValidator
{
    /// <summary>The longest name a Plynling can carry (Discord's cap): the worst case for every length.</summary>
    public const int NameWorstCase = 32;

    public const int LabelCap = 80;          // a button label
    public const int RollRoom = 20;          // what the button adds for a roll (« · Sagesse · 95 % »)
    public const int TitleCap = 100;
    public const int ProseCap = 3000;        // one scene's, outcome's or ending's prose, every line included
    public const int PublicCap = 300;

    private static readonly Regex Token = new(@"\{(?:A|B|T|[abp]:[^|{}]*\|[^{}]*)\}", RegexOptions.Compiled);
    private static readonly Regex Choice = new(@"\{[abp]:([^|{}]*)\|([^{}]*)\}", RegexOptions.Compiled);

    public static IReadOnlyList<StoryError> Validate(IReadOnlyList<StoryArc> arcs, IReadOnlyList<CastMember> cast)
    {
        var errors = new List<StoryError>();
        void Error(StoryPos at, string message) => errors.Add(new StoryError(at, message));

        var castKeys = new HashSet<string>();
        foreach (var c in cast)
            if (!castKeys.Add(c.Key)) Error(c.Pos, $"character '{c.Key}' listed twice");

        // Global keys: arcs, memories, epithets, keepsakes.
        var arcsByKey = new Dictionary<string, StoryArc>();
        foreach (var a in arcs)
            if (!arcsByKey.TryAdd(a.Key, a)) Error(a.Pos, $"arc key '{a.Key}' already used at {arcsByKey[a.Key].Pos}");
        var memories = Unique(arcs.SelectMany(a => a.Memories), m => m.Key, m => m.Pos, "memory", Error);
        var epithets = Unique(arcs.SelectMany(a => a.Epithets), e => e.Key, e => e.Pos, "epithet", Error);
        var keepsakes = Unique(arcs.SelectMany(a => a.Keepsakes), k => k.Key, k => k.Pos, "keepsake", Error);
        // Grouped: a duplicate arc key is reported above and must not crash the rest.
        var flagsSet = arcs.GroupBy(a => a.Key).ToDictionary(g => g.Key,
            g => g.SelectMany(Effects).Where(e => e.Kind == EffectKind.Flag).Select(e => e.Key).ToHashSet());

        foreach (var m in memories.Values)
        {
            if (!castKeys.Contains(m.Holder)) Error(m.Pos, $"memory '{m.Key}': holder '{m.Holder}' is not in village.cast");
            else if (!m.Key.StartsWith(m.Holder + ".")) Error(m.Pos, $"memory '{m.Key}' must be named '{m.Holder}.<fact>' after its holder");
        }

        foreach (var arc in arcs)
            ValidateArc(arc, arcsByKey, flagsSet, memories, epithets, keepsakes, castKeys, Error);
        return errors;
    }

    private static Dictionary<string, T> Unique<T>(IEnumerable<T> items, Func<T, string> key, Func<T, StoryPos> pos, string what,
        Action<StoryPos, string> error)
    {
        var map = new Dictionary<string, T>();
        foreach (var item in items)
            if (!map.TryAdd(key(item), item)) error(pos(item), $"{what} '{key(item)}' already declared at {pos(map[key(item)])}");
        return map;
    }

    private static void ValidateArc(StoryArc arc, Dictionary<string, StoryArc> arcs, Dictionary<string, HashSet<string>> flagsSet,
        Dictionary<string, MemoryDecl> memories, Dictionary<string, EpithetDecl> epithets, Dictionary<string, KeepsakeDecl> keepsakes,
        HashSet<string> cast, Action<StoryPos, string> error)
    {
        var h = arc.Header;
        var hasOther = h.Other is not null;

        // Scenes and endings share one namespace: both are [-> …] targets.
        var targets = new Dictionary<string, StoryPos>();
        foreach (var (key, pos) in arc.Scenes.Select(s => (s.Key, s.Pos)).Concat(arc.Endings.Select(e => (e.Key, e.Pos))))
            if (!targets.TryAdd(key, pos)) error(pos, $"'{key}' is already a scene or an ending of this arc ({targets[key]})");
        if (arc.Scenes.Count == 0) error(arc.Pos, "an arc needs at least one scene");
        void Target(string? next, StoryPos at)
        {
            if (next is not null && !targets.ContainsKey(next)) error(at, $"[-> {next}]: no scene or ending '{next}' in this arc");
        }

        // Required endings and the other Plynling.
        if (arc.Endings.All(e => e.Key != "interrupted")) error(arc.Pos, "every arc needs an '## ending: interrupted' (its Plynling died mid-arc)");
        if (h.Family is StoryFamily.Love or StoryFamily.Friendship && !targets.ContainsKey("loss"))
            error(arc.Pos, $"a {h.Family.ToString().ToLowerInvariant()} arc needs a 'loss' scene or ending (the other Plynling died or left)");
        if (!hasOther && (h.Family is StoryFamily.Friendship or StoryFamily.Love or StoryFamily.Rivalry || h.Trigger.Kind is TriggerKind.Relation or TriggerKind.Death))
            error(arc.Pos, "this arc is about another Plynling: add 'other: <bond>' to the header");

        foreach (var scene in arc.Scenes)
        {
            if (scene.RetiredTo is not null) { Target(scene.RetiredTo, scene.Pos); continue; }
            if (scene.Handover && !hasOther) error(scene.Pos, $"scene '{scene.Key}' is handed over, but the arc has no 'other'");
            var optionKeys = new HashSet<string>();
            foreach (var o in scene.Options)
                if (!optionKeys.Add(o.Key)) error(o.Pos, $"option '{o.Key}' twice in scene '{scene.Key}'");
            var live = scene.Options.Where(o => !o.Retired).ToList();
            if (scene.Options.Count > 0 && live.Count is < 2 or > 4)
                error(scene.Pos, $"scene '{scene.Key}' has {live.Count} live options; a scene offers 2 to 4");
            if (live.Count > 0 && !live.Any(IsFree))
                error(scene.Pos, $"scene '{scene.Key}' has no free option (no needs, not owner-only, not secret, no stress cost): it could never be decided alone");
            foreach (var o in scene.Options)
            {
                foreach (var outcome in new[] { o.Outcome, o.Success, o.Failure }.OfType<StoryOutcome>()) Target(outcome.Next, outcome.Pos);
                foreach (var (trait, cost) in o.StressCosts)
                {
                    if (PlynlingTraits.ByKey(trait) is null) error(o.Pos, $"[stress {trait} {cost}]: no trait '{trait}'");
                    if (cost is < 1 or > 60) error(o.Pos, $"[stress {trait} {cost}]: a stress cost is 1 to 60");
                }
                if (o.Roll is { Difficulty: < 1 or > 20 } r) error(o.Pos, $"roll difficulty {r.Difficulty}: write 1 to 20");
                foreach (var (axis, n) in o.Lean)
                    if (n is < -5 or > 5) error(o.Pos, $"[lean {axis.ToString().ToLowerInvariant()} {n}]: a lean is -5 to 5");
                if (o.Needs.Any(c => c.Kind == ConditionKind.Flag) && o.LockText is null)
                    error(o.Pos, $"option '{o.Key}': a flag gate needs its lock text: [needs flag <f> \"…\"]");
                var room = (o.Roll is not null ? RollRoom : 0) + (o.Needs.Count > 0 ? 3 + LockLength(o, memories) : 0);
                var label = WorstCase(o.Label).Length;
                if (label + room > LabelCap)
                    error(o.Pos, $"option '{o.Key}': the button would be {label + room} characters at worst (label {label}, roll and lock {room}); the cap is {LabelCap}");
            }
            if (scene.Narration is { } n2) Target(n2.Next, n2.Pos);
            if (WorstCase(Join(scene.Prose)).Length > ProseCap) error(scene.Pos, $"scene '{scene.Key}': prose over {ProseCap} characters at worst");
            foreach (var outcome in Outcomes(scene))
            {
                // Anti-griefing: the other Plynling is touched outside the relation only in a moment its owner decides.
                foreach (var e in outcome.Effects.Where(e => e.Other && !scene.Handover))
                    error(e.Pos, $"[other remember {e.Key}]: only in a handed-over scene (mark the scene 'handover')");
                if (WorstCase(Join(outcome.Prose)).Length > ProseCap) error(outcome.Pos, $"outcome prose over {ProseCap} characters at worst");
                foreach (var e in outcome.Effects.Where(e => e.Kind is EffectKind.Epithet or EffectKind.Keepsake or EffectKind.Trait or EffectKind.Unlock))
                    error(e.Pos, $"[{e.Kind.ToString().ToLowerInvariant()} {e.Key}]: marks (epithet, keepsake, trait, unlock) are given only in an ending");
            }
        }

        foreach (var ending in arc.Endings)
        {
            if (WorstCase(Join(ending.Prose)).Length > ProseCap) error(ending.Pos, $"ending '{ending.Key}': prose over {ProseCap} characters at worst");
            foreach (var e in ending.Marks.Where(e => e.Other))
                error(e.Pos, $"[other remember {e.Key}]: only in a handed-over scene, never in an ending");
            var traits = ending.Marks.Where(e => e.Kind == EffectKind.Trait).ToList();
            if (traits.Count > 1) error(traits[1].Pos, $"ending '{ending.Key}': one trait change at most");
            foreach (var t in traits)
            {
                var from = PlynlingTraits.ByKey(t.Key);
                if (from is null) continue;   // reported with the other trait keys
                if (t.To is null)
                {
                    if (from.Kind != TraitKind.Personality) error(t.Pos, $"[trait {t.Key}]: only a personality trait can be gained");
                }
                else if (PlynlingTraits.ByKey(t.To) is { } to && (from.Kind != TraitKind.Personality || to.Kind != TraitKind.Personality || from.Group != to.Group || from.Key == to.Key))
                    error(t.Pos, $"[trait {t.Key} -> {t.To}]: a swap trades a personality trait for another of the same group");
            }
        }

        // Conditions: keys and the other Plynling.
        foreach (var c in Conditions(arc))
        {
            switch (c.Kind)
            {
                case ConditionKind.Trait or ConditionKind.OtherTrait when PlynlingTraits.ByKey(c.Key) is null:
                    error(c.Pos, $"no trait '{c.Key}'"); break;
                case ConditionKind.Memory when !memories.ContainsKey(c.Key):
                    error(c.Pos, $"memory '{c.Key}' is not declared in any story"); break;
                case ConditionKind.Keepsake when !keepsakes.ContainsKey(c.Key):
                    error(c.Pos, $"keepsake '{c.Key}' is not declared in any story"); break;
                case ConditionKind.Epithet when !epithets.ContainsKey(c.Key):
                    error(c.Pos, $"epithet '{c.Key}' is not declared in any story"); break;
                case ConditionKind.Standing when !cast.Contains(c.Key):
                    error(c.Pos, $"standing: '{c.Key}' is not in village.cast"); break;
                case ConditionKind.Ended:
                {
                    var (a, e) = Split(c.Key);
                    if (!arcs.TryGetValue(a, out var other)) error(c.Pos, $"ended: no arc '{a}'");
                    else if (other.Endings.All(x => x.Key != e)) error(c.Pos, $"ended: arc '{a}' has no ending '{e}'");
                    break;
                }
                case ConditionKind.Flag when c.Key.Contains('.'):
                {
                    var (a, f) = Split(c.Key);
                    if (!flagsSet.TryGetValue(a, out var set)) error(c.Pos, $"flag '{c.Key}': no arc '{a}'");
                    else if (!set.Contains(f)) error(c.Pos, $"flag '{c.Key}': arc '{a}' never sets '{f}'");
                    break;
                }
            }
            if (!hasOther && c.Kind is ConditionKind.OtherTrait or ConditionKind.Relation)
                error(c.Pos, $"[{c.Kind.ToString().ToLowerInvariant()} {c.Key}] reads the other Plynling, but the arc has no 'other'");
        }

        // Effects: keys, ranges and the other Plynling.
        foreach (var e in Effects(arc))
        {
            var said = $"[{e.Kind.ToString().ToLowerInvariant()} {e.Key}]";
            switch (e.Kind)
            {
                case EffectKind.Remember or EffectKind.Forget or EffectKind.Reveal when !memories.ContainsKey(e.Key):
                    error(e.Pos, $"{said}: memory '{e.Key}' is not declared in any story"); break;
                case EffectKind.Reveal when memories[e.Key].Public:
                    error(e.Pos, $"{said}: only a private memory can be revealed"); break;
                case EffectKind.Modifier when PlynlingModifiers.ByKey(e.Key) is null:
                    error(e.Pos, $"{said}: no modifier '{e.Key}'"); break;
                case EffectKind.Item when ItemCatalog.ByKey(e.Key) is null:
                    error(e.Pos, $"{said}: no item '{e.Key}'"); break;
                case EffectKind.Epithet when !epithets.ContainsKey(e.Key):
                    error(e.Pos, $"{said}: epithet '{e.Key}' is not declared in any story"); break;
                case EffectKind.Keepsake when !keepsakes.ContainsKey(e.Key):
                    error(e.Pos, $"{said}: keepsake '{e.Key}' is not declared in any story"); break;
                case EffectKind.Unlock when !arcs.ContainsKey(e.Key):
                    error(e.Pos, $"{said}: no arc '{e.Key}'"); break;
                case EffectKind.Trait:
                    foreach (var k in new[] { e.Key, e.To }.OfType<string>())
                        if (PlynlingTraits.ByKey(k) is null) error(e.Pos, $"[trait …]: no trait '{k}'");
                    break;
                case EffectKind.Stress when e.Amount is < -100 or > 60:
                    error(e.Pos, $"[stress {e.Amount}]: write -100 to 60"); break;
                case EffectKind.Cailloux when e.Amount is < 1 or > 100:
                    error(e.Pos, $"[cailloux {e.Amount}]: write 1 to 100"); break;
                case EffectKind.Affinity when e.Amount is < -50 or > 50:
                    error(e.Pos, $"[affinity {e.Amount}]: write -50 to 50"); break;
                case EffectKind.Lift when e.Amount is <= 0 or > 0.3:
                    error(e.Pos, $"[lift {e.Key} {e.Amount}]: write more than 0 and at most 0.3"); break;
            }
            if (!hasOther && (e.Other || e.Kind == EffectKind.Affinity))
                error(e.Pos, $"{said} touches the other Plynling, but the arc has no 'other'");
        }

        // Texts: tokens and caps.
        foreach (var (text, pos, what) in Texts(arc))
        {
            var left = Token.Replace(text, "");
            foreach (Match bad in Regex.Matches(left, @"\{[^{}]*\}?|\}"))
                error(pos, $"{what}: '{bad.Value}' is not a template token ({{A}}, {{B}}, {{T}}, {{a:…|…}}, {{b:…|…}}, {{p:…|…}})");
            if (!hasOther && Token.Matches(text).Any(m => m.Value == "{B}" || m.Value.StartsWith("{b:") || m.Value.StartsWith("{p:")))
                error(pos, $"{what}: {{B}}, {{b:…}} and {{p:…}} name the other Plynling, but the arc has no 'other'");
            var cap = what.EndsWith("title") ? TitleCap : what.StartsWith("public") ? PublicCap : int.MaxValue;
            if (WorstCase(text).Length > cap) error(pos, $"{what} over {cap} characters at worst");
        }
    }

    private static (string Arc, string Key) Split(string dotted)
    {
        var i = dotted.IndexOf('.');
        return (dotted[..i], dotted[(i + 1)..]);
    }

    // Free: what deciding alone can always pick, whatever the Plynling holds.
    public static bool IsFree(StoryOption o) => !o.Retired && o.Needs.Count == 0 && !o.OwnerOnly && !o.Secret && o.StressCosts.Count == 0;

    // The lock shown next to a gated option: its own text, else what the condition names.
    private static int LockLength(StoryOption o, Dictionary<string, MemoryDecl> memories)
    {
        if (o.LockText is not null) return WorstCase(o.LockText).Length;
        return o.Needs.Sum(c => c.Kind switch
        {
            ConditionKind.Trait or ConditionKind.OtherTrait when PlynlingTraits.ByKey(c.Key) is { } t => Math.Max(t.NameM.Length, t.NameF.Length),
            ConditionKind.Memory when memories.TryGetValue(c.Key, out var m) => m.Wording.Length,
            _ => 20,
        });
    }

    /// <summary>The text at its longest: names at <see cref="NameWorstCase"/>, each agreement at its longer form.</summary>
    public static string WorstCase(string text) =>
        Choice.Replace(text, m => m.Groups[1].Length >= m.Groups[2].Length ? m.Groups[1].Value : m.Groups[2].Value)
            .Replace("{A}", new string('x', NameWorstCase)).Replace("{B}", new string('x', NameWorstCase)).Replace("{T}", new string('x', 40));

    private static string Join(IEnumerable<StoryParagraph> prose) => string.Join("\n\n", prose.Select(p => string.Join(' ', p.Lines.Select(l => l.Text))));

    public static IEnumerable<StoryOutcome> Outcomes(StoryScene scene) =>
        scene.Options.SelectMany(o => new[] { o.Outcome, o.Success, o.Failure }).Append(scene.Narration).OfType<StoryOutcome>();

    /// <summary>Every condition the arc holds: requires, weight bonuses, gates, conditional lines.</summary>
    public static IEnumerable<StoryCondition> Conditions(StoryArc arc)
    {
        var lines = arc.Scenes.SelectMany(s => s.Prose.Concat(Outcomes(s).SelectMany(o => o.Prose)))
            .Concat(arc.Endings.SelectMany(e => e.Prose)).SelectMany(p => p.Lines);
        return arc.Header.Requires
            .Concat(arc.Header.WeightBonuses.SelectMany(b => b.When))
            .Concat(arc.Scenes.SelectMany(s => s.Options).SelectMany(o => o.Needs))
            .Concat(lines.SelectMany(l => l.When));
    }

    /// <summary>Every effect the arc holds: outcomes, narrations and endings' marks.</summary>
    public static IEnumerable<StoryEffect> Effects(StoryArc arc) =>
        arc.Scenes.SelectMany(Outcomes).SelectMany(o => o.Effects).Concat(arc.Endings.SelectMany(e => e.Marks));

    /// <summary>Every text a player may read, with where it is and what it is.</summary>
    public static IEnumerable<(string Text, StoryPos Pos, string What)> Texts(StoryArc arc)
    {
        yield return (arc.Header.Title, arc.Pos, "arc title");
        yield return (arc.Header.Public, arc.Pos, "public line");
        foreach (var m in arc.Memories) yield return (m.Wording, m.Pos, $"memory '{m.Key}'");
        foreach (var e in arc.Epithets) yield return (e.Title, e.Pos, $"epithet '{e.Key}' title");
        foreach (var k in arc.Keepsakes)
        {
            yield return (k.Name, k.Pos, $"keepsake '{k.Key}' title");
            yield return (k.Story, k.Pos, $"keepsake '{k.Key}'");
        }
        foreach (var s in arc.Scenes)
        {
            yield return (s.Title, s.Pos, $"scene '{s.Key}' title");
            if (s.Public is not null) yield return (s.Public, s.Pos, $"public line of '{s.Key}'");
            foreach (var l in s.Prose.SelectMany(p => p.Lines)) yield return (l.Text, l.Pos, "prose");
            foreach (var o in s.Options)
            {
                yield return (o.Label, o.Pos, $"option '{o.Key}' label");
                if (o.LockText is not null) yield return (o.LockText, o.Pos, $"option '{o.Key}' lock");
                if (o.Public is not null) yield return (o.Public, o.Pos, $"public line of '{o.Key}'");
            }
            foreach (var l in Outcomes(s).SelectMany(o => o.Prose).SelectMany(p => p.Lines)) yield return (l.Text, l.Pos, "prose");
        }
        foreach (var e in arc.Endings)
        {
            yield return (e.Title, e.Pos, $"ending '{e.Key}' title");
            foreach (var l in e.Prose.SelectMany(p => p.Lines)) yield return (l.Text, l.Pos, "prose");
        }
    }
}
