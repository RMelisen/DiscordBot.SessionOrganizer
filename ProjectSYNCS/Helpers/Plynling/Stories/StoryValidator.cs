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

    /// <summary>What the parser reads but the engine does not run yet: the validator refuses each with "not supported yet".</summary>
    public static readonly IReadOnlySet<string> NotYetSupported = new HashSet<string>
        { "trigger relation", "trigger death <bond>", "trigger oldage", "condition chain", "effect other" };

    /// <summary>A break may relieve more than any other event: down to this.</summary>
    public const int BreakStressFloor = -150;

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

        // Old keys (`was:`): an old C# event's key, claimed by one live scene in the whole catalog.
        var claimed = new Dictionary<string, (string Scene, StoryPos Pos)>();
        foreach (var arc in arcs)
            foreach (var s in arc.Scenes.Where(s => s.Was is not null))
            {
                var key = StoryCompiler.Key(arc.Key, s.Key);
                if (!StoryVocabulary.IsKey(s.Was!) || s.Was!.Contains('.'))
                    Error(s.Pos, $"was: '{s.Was}' is not an old event key (lowercase, digits and '_', no dot)");
                else if (s.RetiredTo is not null)
                    Error(s.Pos, $"was: scene '{s.Key}' is retired; an old key is claimed by the live scene that replaces it");
                else if (!claimed.TryAdd(s.Was, (key, s.Pos)))
                    Error(s.Pos, $"was: '{s.Was}' is claimed by both {claimed[s.Was].Scene} ({claimed[s.Was].Pos}) and {key}");
            }

        // Unlocks: a locked arc is unlocked somewhere, and an [unlock] names a locked arc.
        var unlocked = arcs.SelectMany(Effects).Where(e => e.Kind == EffectKind.Unlock).Select(e => e.Key).ToHashSet();
        foreach (var a in arcs.Where(a => a.Header.Locked && !unlocked.Contains(a.Key)))
            Error(a.Pos, $"arc '{a.Key}' is locked but never unlocked: no ending says [unlock {a.Key}]");
        foreach (var e in arcs.SelectMany(Effects).Where(e => e.Kind == EffectKind.Unlock && arcsByKey.TryGetValue(e.Key, out var target) && !target.Header.Locked))
            Error(e.Pos, $"[unlock {e.Key}]: arc '{e.Key}' is not locked (add 'locked' to its header)");
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

        // Size, triggers and the other Plynling.
        var isShort = h.Size == StorySize.Short;
        var isBreak = h.Trigger.Kind == TriggerKind.Break;
        if (h.Trigger.Kind is TriggerKind.Relation or TriggerKind.OldAge || h.Trigger is { Kind: TriggerKind.Death, Arg: not null })
            error(arc.Pos, $"trigger '{h.Trigger.Kind.ToString().ToLowerInvariant()}{(h.Trigger.Arg is { } targ ? " " + targ : "")}' is not supported yet "
                + "(use draw, adoption, stage teen|adult, sick, recovered, visit, death or break 1-3)");
        else if (h.Trigger is { Kind: TriggerKind.Stage, Arg: not ("teen" or "adult") })
            error(arc.Pos, $"trigger 'stage {h.Trigger.Arg}' is not supported yet (stage teen or adult only)");
        // {T}: the trait a moment reveals, said in the first scene of an arc brought by adoption or a stage.
        var firstScene = arc.Scenes.Count > 0 ? SceneTexts(arc.Scenes[0]).ToHashSet() : new HashSet<(string, StoryPos, string)>();
        var tOk = h.Trigger.Kind is TriggerKind.Adoption or TriggerKind.Stage;
        foreach (var t in Texts(arc).Where(t => t.Text.Contains("{T}") && !(tOk && firstScene.Contains(t))))
            error(t.Pos, $"{t.What}: {{T}} is only for the first scene of an arc whose trigger is adoption or stage");
        var bringsNobody = h.Trigger.Kind is TriggerKind.Sick or TriggerKind.Recovered or TriggerKind.Break
            or TriggerKind.Adoption or TriggerKind.Stage;
        var aboutAnother = h.Family is StoryFamily.Friendship or StoryFamily.Love or StoryFamily.Rivalry;
        if (!hasOther && ((aboutAnother && !bringsNobody)
                          || h.Trigger.Kind is TriggerKind.Relation or TriggerKind.Death or TriggerKind.Visit))
            error(arc.Pos, "this arc is about another Plynling: add 'other: <bond>' to the header");
        // A trigger arc's other Plynling comes with the trigger (the visitor, the departed) or not at all: the
        // engine never filters it, so a bond list or other_if: there would be silently ignored.
        if (h.Trigger.Kind is TriggerKind.Visit or TriggerKind.Death
            && (h.Other is { Kind: not OtherKind.Anyone } || h.OtherIf.Count > 0))
            error(arc.Pos, $"trigger {h.Trigger.Kind.ToString().ToLowerInvariant()}: the other Plynling comes with the trigger, "
                + "so write 'other: anyone' and no 'other_if:'");
        else if (bringsNobody && (hasOther || aboutAnother))
            error(arc.Pos, $"trigger {h.Trigger.Kind.ToString().ToLowerInvariant()} brings no other Plynling: "
                + (hasOther ? "remove 'other:'" : "a friendship, love or rivalry arc needs trigger draw, visit or death"));
        // A target is hostile or known, never both.
        if (h.Other is { Kind: OtherKind.Bonds, Bonds: var bonds } && bonds.Any(PlynlingBonds.IsHostile) && !bonds.All(PlynlingBonds.IsHostile))
            error(arc.Pos, "other: hostile bonds (rivals, enemies) and the others cannot be mixed: the other Plynling is hostile or known");
        if (h.OtherIf.Count > 0 && !hasOther) error(arc.Pos, "'other_if' filters the other Plynling: add 'other:' to the header");
        foreach (var c in h.OtherIf)
        {
            if (c.Kind is not (ConditionKind.Relation or ConditionKind.Affinity or ConditionKind.CanCouple))
                error(c.Pos, $"[{c.Kind.ToString().ToLowerInvariant()} {c.Key}]: other_if takes only relation, affinity and can_couple");
            if (c.Kind == ConditionKind.Affinity && c.Number is < -100 or > 100)
                error(c.Pos, $"[affinity {c.Number}]: write -100 to 100");
        }
        var inOtherIf = new HashSet<StoryCondition>(h.OtherIf, ReferenceEqualityComparer.Instance);
        foreach (var c in Conditions(arc).Where(c => c.Kind is ConditionKind.Affinity or ConditionKind.CanCouple && !inOtherIf.Contains(c)))
            error(c.Pos, $"[{(c.Kind == ConditionKind.Affinity ? "affinity" : "can_couple")}]: reads the other Plynling as a candidate, so it goes only in other_if:");

        foreach (var scene in arc.Scenes)
        {
            // The first scene is the arc's opener, found by position: retiring it would leave the arc without one.
            if (scene.RetiredTo is not null && scene == arc.Scenes[0])
                error(scene.Pos, $"scene '{scene.Key}' is the first scene of the arc: it cannot be retired");
            if (scene.RetiredTo is not null) { Target(scene.RetiredTo, scene.Pos); continue; }
            if (scene.Handover) Handover(arc, scene, hasOther, error);
            foreach (var o in scene.Options.Where(o => o.Stance != Stance.Neutral && !scene.Handover))
                error(o.Pos, $"option '{o.Key}': [accept] and [refuse] go only in a handover scene (the answer's side)");
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
                if (o.Roll is { Vs: false, Difficulty: < 1 or > 20 } r) error(o.Pos, $"roll difficulty {r.Difficulty}: write 1 to 20");
                if (o.Roll is { Vs: true } && !hasOther)
                    error(o.Pos, $"option '{o.Key}': [roll … vs] rolls against the other Plynling, but the arc has no 'other'");
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
                foreach (var e in outcome.Effects.Where(e => e.Other && scene.Handover))
                    error(e.Pos, $"[other remember {e.Key}]: [other …] effects are not supported yet");
                foreach (var e in outcome.Effects.Where(e => e.Kind is EffectKind.Couple or EffectKind.Heartbreak && !scene.Handover))
                    error(e.Pos, $"[{e.Kind.ToString().ToLowerInvariant()}]: only in a handover scene (the other owner's answer)");
                if (WorstCase(Join(outcome.Prose)).Length > ProseCap) error(outcome.Pos, $"outcome prose over {ProseCap} characters at worst");
                if (outcome.Face is { B: not null } && !hasOther)
                    error(outcome.Pos, "[… B:…]: the face tag names the other Plynling, but the arc has no 'other'");
                foreach (var e in outcome.Effects.Where(e => e.Kind is EffectKind.Epithet or EffectKind.Keepsake or EffectKind.Trait or EffectKind.Unlock))
                    error(e.Pos, isShort
                        ? $"[{e.Kind.ToString().ToLowerInvariant()} {e.Key}]: a short event gives no mark (epithet, keepsake, trait, unlock)"
                        : $"[{e.Kind.ToString().ToLowerInvariant()} {e.Key}]: marks (epithet, keepsake, trait, unlock) are given only in an ending");
                if (outcome.End && !isShort)
                    error(outcome.Pos, "[end] closes a short event; a long event ends on an '## ending:'");
            }
            if (live.Count(o => o.Mystery) > 1)
                error(scene.Pos, $"scene '{scene.Key}': at most one [mystery] option per scene");
        }

        foreach (var ending in arc.Endings)
        {
            if (WorstCase(Join(ending.Prose)).Length > ProseCap) error(ending.Pos, $"ending '{ending.Key}': prose over {ProseCap} characters at worst");
            foreach (var e in ending.Marks.Where(e => e.Other))
                error(e.Pos, $"[other remember {e.Key}]: only in a handed-over scene, never in an ending");
            var marks = ending.Marks.Where(e => e.Kind is EffectKind.Epithet or EffectKind.Keepsake or EffectKind.Trait).ToList();
            if (isShort && (marks.Count > 0 || ending.Marks.Any(e => e.Kind == EffectKind.Unlock)))
                error(ending.Pos, $"ending '{ending.Key}': a short event gives no mark (epithet, keepsake, trait, unlock)");
            else if (marks.Count > 1) error(marks[1].Pos, $"ending '{ending.Key}': one mark at most (epithet, keepsake or trait)");
            foreach (var t in marks.Where(e => e.Kind == EffectKind.Trait))
            {
                var from = PlynlingTraits.ByKey(t.Key);
                if (h.Stages.Any(s => s is PlynlingStage.Baby or PlynlingStage.Teen))
                    error(t.Pos, $"[trait {t.Key}]: a trait changes only in an adult or elder arc (stage: adult, elder)");
                if (from is null) continue;   // reported with the other trait keys
                if (t.To is null)
                    error(t.Pos, $"[trait {t.Key}]: a trait change is a swap, [trait <from> -> <to>], never a gain");
                else if (PlynlingTraits.ByKey(t.To) is { } to && (from.Kind != TraitKind.Personality || to.Kind != TraitKind.Personality || from.Group != to.Group || from.Key == to.Key))
                    error(t.Pos, $"[trait {t.Key} -> {t.To}]: a swap trades a personality trait for another of the same group");
            }
        }

        // Conditions: keys and the other Plynling.
        foreach (var c in Conditions(arc))
        {
            switch (c.Kind)
            {
                case ConditionKind.Chain:
                    error(c.Pos, "condition 'chain' is not supported yet (it can never hold)"); break;
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
                case EffectKind.Stress when e.Amount < (isBreak ? BreakStressFloor : -100) || e.Amount > 60:
                    error(e.Pos, $"[stress {e.Amount}]: write {(isBreak ? BreakStressFloor : -100)} to 60"); break;
                // A coping trait comes only from a mental break (CK3 ties each to its choice).
                case EffectKind.Coping when !isBreak:
                    error(e.Pos, "[coping …]: only in a mental break (trigger: break 1-3)"); break;
                case EffectKind.Coping when e.Key != "" && PlynlingTraits.ByKey(e.Key) is not { Kind: TraitKind.Coping }:
                    error(e.Pos, $"[coping {e.Key}]: no coping trait '{e.Key}'"); break;
                case EffectKind.Cailloux when e.Amount is < 1 or > 100:
                    error(e.Pos, $"[cailloux {e.Amount}]: write 1 to 100"); break;
                case EffectKind.Affinity when e.Amount is < -50 or > 50:
                    error(e.Pos, $"[affinity {e.Amount}]: write -50 to 50"); break;
                case EffectKind.Lift when e.Amount is <= 0 or > 0.3:
                    error(e.Pos, $"[lift {e.Key} {e.Amount}]: write more than 0 and at most 0.3"); break;
            }
            if (!hasOther && (e.Other || e.Kind is EffectKind.Affinity or EffectKind.Bond))
                error(e.Pos, $"{said} touches the other Plynling, but the arc has no 'other'");
        }

        // Texts: tokens and caps.
        foreach (var (text, pos, what) in Texts(arc))
        {
            foreach (var bad in BadTokens(text))
                error(pos, $"{what}: '{bad}' is not a template token ({{A}}, {{B}}, {{T}}, {{a:…|…}}, {{b:…|…}}, {{p:…|…}})");
            if (!hasOther && Token.Matches(text).Any(m => m.Value == "{B}" || m.Value.StartsWith("{b:") || m.Value.StartsWith("{p:")))
                error(pos, $"{what}: {{B}}, {{b:…}} and {{p:…}} name the other Plynling, but the arc has no 'other'");
            var cap = what.EndsWith("title") ? TitleCap : int.MaxValue;
            if (WorstCase(text).Length > cap) error(pos, $"{what} over {cap} characters at worst");
        }
    }

    // A handover scene is the other owner's answer, played by the Response engine on their card: never the
    // opener, real options that all end the run there, no run flags read or set (the reply is a new instance
    // on another Plynling), queued at once.
    private static void Handover(StoryArc arc, StoryScene scene, bool hasOther, Action<StoryPos, string> error)
    {
        var said = $"scene '{scene.Key}'";
        if (!hasOther) error(scene.Pos, $"{said} is handed over, but the arc has no 'other'");
        if (arc.Scenes.Count > 0 && scene == arc.Scenes[0]) error(scene.Pos, $"{said}: a handover scene is never the first scene (the opener is the asker's)");
        if (scene.Narration is not null) error(scene.Pos, $"{said}: a handover scene gives the other owner options; it cannot be a narration");
        if (scene.Pace.Kind != PaceKind.Now) error(scene.Pos, $"{said}: a handover scene is written 'pace: now' (the answer is queued at once)");
        foreach (var o in scene.Options.Where(o => !o.Retired))
        {
            foreach (var outcome in new[] { o.Outcome, o.Success, o.Failure }.OfType<StoryOutcome>().Where(x => !x.End))
                error(outcome.Pos, $"option '{o.Key}': a handover scene's outcomes end with [end] (the run stops at the answer)");
            if (o.Needs.Any(c => c.Kind == ConditionKind.Flag))
                error(o.Pos, $"option '{o.Key}': a handover scene reads no run flags (the answer is a new event on the other Plynling)");
        }
        var lines = scene.Prose.Concat(Outcomes(scene).SelectMany(x => x.Prose)).SelectMany(p => p.Lines);
        foreach (var l in lines.Where(l => l.When.Any(c => c.Kind == ConditionKind.Flag)))
            error(l.Pos, $"{said}: a handover scene reads no run flags (the answer is a new event on the other Plynling)");
        foreach (var e in Outcomes(scene).SelectMany(x => x.Effects).Where(e => e.Kind is EffectKind.Flag or EffectKind.Unflag))
            error(e.Pos, $"[{e.Kind.ToString().ToLowerInvariant()} {e.Key}]: a handover scene sets no flags (the run ends at the answer)");
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

    /// <summary>
    /// What in a text looks like a template token but is none ({A}, {B}, {T}, {a:…|…}, {b:…|…}, {p:…|…}):
    /// a stray brace, a misspelt or unclosed token. Shared by the visit validator.
    /// </summary>
    public static IEnumerable<string> BadTokens(string text) =>
        Regex.Matches(Token.Replace(text, ""), @"\{[^{}]*\}?|\}").Select(m => m.Value);

    /// <summary>The text at its longest: names at <see cref="NameWorstCase"/>, each agreement at its longer form.</summary>
    public static string WorstCase(string text) =>
        Choice.Replace(text, m => m.Groups[1].Length >= m.Groups[2].Length ? m.Groups[1].Value : m.Groups[2].Value)
            .Replace("{A}", new string('x', NameWorstCase)).Replace("{B}", new string('x', NameWorstCase)).Replace("{T}", new string('x', 40));

    private static string Join(IEnumerable<StoryParagraph> prose) => string.Join("\n\n", prose.Select(p => string.Join(' ', p.Lines.Select(l => l.Text))));

    public static IEnumerable<StoryOutcome> Outcomes(StoryScene scene) =>
        scene.Options.SelectMany(o => new[] { o.Outcome, o.Success, o.Failure }).Append(scene.Narration).OfType<StoryOutcome>();

    /// <summary>Every condition the arc holds: other_if, requires, weight bonuses, gates, conditional lines.</summary>
    public static IEnumerable<StoryCondition> Conditions(StoryArc arc)
    {
        var lines = arc.Scenes.SelectMany(s => s.Prose.Concat(Outcomes(s).SelectMany(o => o.Prose)))
            .Concat(arc.Endings.SelectMany(e => e.Prose)).SelectMany(p => p.Lines);
        return arc.Header.OtherIf
            .Concat(arc.Header.Requires)
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
        foreach (var m in arc.Memories) yield return (m.Wording, m.Pos, $"memory '{m.Key}'");
        foreach (var e in arc.Epithets) yield return (e.Title, e.Pos, $"epithet '{e.Key}' title");
        foreach (var k in arc.Keepsakes)
        {
            yield return (k.Name, k.Pos, $"keepsake '{k.Key}' title");
            yield return (k.Story, k.Pos, $"keepsake '{k.Key}'");
        }
        foreach (var s in arc.Scenes)
            foreach (var t in SceneTexts(s)) yield return t;
        foreach (var e in arc.Endings)
        {
            yield return (e.Title, e.Pos, $"ending '{e.Key}' title");
            foreach (var l in e.Prose.SelectMany(p => p.Lines)) yield return (l.Text, l.Pos, "prose");
        }
    }

    /// <summary>The texts of one scene: its title, prose, options and outcomes.</summary>
    public static IEnumerable<(string Text, StoryPos Pos, string What)> SceneTexts(StoryScene s)
    {
        yield return (s.Title, s.Pos, $"scene '{s.Key}' title");
        foreach (var l in s.Prose.SelectMany(p => p.Lines)) yield return (l.Text, l.Pos, "prose");
        foreach (var o in s.Options)
        {
            yield return (o.Label, o.Pos, $"option '{o.Key}' label");
            if (o.LockText is not null) yield return (o.LockText, o.Pos, $"option '{o.Key}' lock");
        }
        foreach (var l in Outcomes(s).SelectMany(o => o.Prose).SelectMany(p => p.Lines)) yield return (l.Text, l.Pos, "prose");
    }
}
