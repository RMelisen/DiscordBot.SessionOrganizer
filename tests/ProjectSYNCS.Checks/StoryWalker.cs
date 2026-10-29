using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// One step of a path: the scene, the option taken (null for a narration), and the roll (null without one).
public sealed record WalkStep(StoryScene Scene, StoryOption? Option, bool? Success);

public sealed record WalkPath(IReadOnlyList<WalkStep> Steps, StoryEnding Ending, bool Alone, int Cailloux, int TraitChanges,
    IReadOnlyDictionary<string, int> StressCosts, bool FromLoss);

public sealed record WalkResult(IReadOnlyList<WalkPath> Paths, IReadOnlyList<string> Problems,
    IReadOnlyList<(string Profile, IReadOnlyDictionary<string, double> EndingShare)> InCharacter);

/// <summary>
/// Walks every path of an arc, for the checks and the path report. The arc's own flags are followed
/// exactly; every other condition counts as satisfiable (the walk looks for what *can* happen). The
/// in-character reading instead counts them false, and picks options the way
/// PlynlingEventEngine.Decide does, with rolls at 50 %.
/// </summary>
public static class StoryWalker
{
    public const int MaxPaths = 20_000;
    private const string Neutral = "neutre";

    public static WalkResult Walk(StoryArc arc, IReadOnlyList<StoryArc> all)
    {
        var problems = new List<string>();
        var scenes = arc.Scenes.GroupBy(s => s.Key).ToDictionary(g => g.Key, g => g.First());
        var endings = arc.Endings.GroupBy(e => e.Key).ToDictionary(g => g.Key, g => g.First());
        var paths = new List<WalkPath>();
        var reached = new HashSet<string>();
        var reachedFromLoss = new HashSet<string>();
        var tooMany = false;

        void Visit(string key, HashSet<string> flags, List<WalkStep> steps, HashSet<string> onPath, bool fromLoss)
        {
            if (tooMany) return;
            (fromLoss ? reachedFromLoss : reached).Add(key);
            if (endings.TryGetValue(key, out var ending))
            {
                paths.Add(MakePath(steps, ending, fromLoss));
                if (paths.Count > MaxPaths) { tooMany = true; problems.Add($"too many paths (over {MaxPaths})"); }
                return;
            }
            if (!scenes.TryGetValue(key, out var scene)) return;   // a dangling [-> …], reported by the validator
            if (scene.RetiredTo is not null) { Visit(scene.RetiredTo, flags, steps, onPath, fromLoss); return; }
            if (!onPath.Add(key)) { problems.Add($"loop through {key}"); return; }
            if (scene.Narration is { } n)
                Follow(new WalkStep(scene, null, null), n);
            foreach (var o in scene.Options.Where(o => !o.Retired && o.Needs.All(c => Satisfiable(c, flags))))
            {
                if (o.Outcome is { } plain) Follow(new WalkStep(scene, o, null), plain);
                if (o.Success is { } s) Follow(new WalkStep(scene, o, true), s);
                if (o.Failure is { } f) Follow(new WalkStep(scene, o, false), f);
            }
            onPath.Remove(key);

            void Follow(WalkStep step, StoryOutcome outcome)
            {
                if (outcome.Next is null) return;
                var after = new HashSet<string>(flags);
                foreach (var e in outcome.Effects)
                    if (e.Kind == EffectKind.Flag) after.Add(e.Key);
                    else if (e.Kind == EffectKind.Unflag) after.Remove(e.Key);
                steps.Add(step);
                Visit(outcome.Next, after, steps, onPath, fromLoss);
                steps.RemoveAt(steps.Count - 1);
            }
        }

        if (arc.Scenes.Count > 0) Visit(arc.Scenes[0].Key, new(), new(), new(), false);
        if (scenes.ContainsKey("loss")) Visit("loss", new(), new(), new(), true);

        // Reachability: what only the loss branch reaches is reached by the engine's jump.
        foreach (var s in arc.Scenes.Where(s => s.RetiredTo is null && s.Key != "loss" && !reached.Contains(s.Key) && !reachedFromLoss.Contains(s.Key)))
            problems.Add($"unreachable {s.Key}");
        foreach (var e in arc.Endings.Where(e => e.Key is not ("interrupted" or "loss") && !reached.Contains(e.Key) && !reachedFromLoss.Contains(e.Key)))
            problems.Add($"unreachable {e.Key}");
        var main = arc.Endings.Count(e => e.Key is not ("interrupted" or "loss") && !(reachedFromLoss.Contains(e.Key) && !reached.Contains(e.Key)));
        if (main is < 3 or > 5) problems.Add($"endings: {main} (an arc has 3 to 5, besides interrupted and loss)");

        // Flags: each one set is tested, here or by another arc; each one tested is set.
        var set = StoryValidator.Effects(arc).Where(e => e.Kind == EffectKind.Flag).Select(e => e.Key).ToHashSet();
        var tested = StoryValidator.Conditions(arc).Where(c => c.Kind == ConditionKind.Flag && !c.Key.Contains('.')).Select(c => c.Key)
            .Concat(all.SelectMany(StoryValidator.Conditions).Where(c => c.Kind == ConditionKind.Flag && c.Key.StartsWith(arc.Key + "."))
                .Select(c => c.Key[(arc.Key.Length + 1)..]))
            .ToHashSet();
        foreach (var f in tested.Except(set).Order()) problems.Add($"flag {f} never set");
        foreach (var f in set.Except(tested).Order()) problems.Add($"flag {f} never tested");

        // Deciding alone: free options only, every branch must end.
        void Alone(string key, HashSet<string> onPath)
        {
            if (endings.ContainsKey(key) || !scenes.TryGetValue(key, out var scene)) return;
            if (scene.RetiredTo is not null) { Alone(scene.RetiredTo, onPath); return; }
            if (!onPath.Add(key)) return;   // reported as a loop
            if (scene.Narration?.Next is { } n) Alone(n, onPath);
            var free = scene.Options.Where(StoryValidator.IsFree).ToList();
            if (scene.Options.Count > 0 && free.Count == 0) problems.Add($"alone dead end at {key}");
            foreach (var o in free)
                foreach (var next in new[] { o.Outcome, o.Success, o.Failure }.Select(x => x?.Next).OfType<string>())
                    Alone(next, onPath);
            onPath.Remove(key);
        }
        if (arc.Scenes.Count > 0) Alone(arc.Scenes[0].Key, new());
        if (scenes.ContainsKey("loss")) Alone("loss", new());

        foreach (var p in paths)
        {
            if (p.Cailloux > 100) problems.Add($"cailloux {p.Cailloux} on a path to {p.Ending.Key}");
            if (p.TraitChanges > 1) problems.Add($"two trait changes on a path to {p.Ending.Key}");
        }

        // Every text in the four gender pairs, names at their longest.
        var name = new string('x', StoryValidator.NameWorstCase);
        foreach (var (text, pos, what) in StoryValidator.Texts(arc))
            foreach (var ga in Enum.GetValues<PlynlingGender>())
                foreach (var gb in Enum.GetValues<PlynlingGender>())
                {
                    var s = PlynlingEvents.Expand(text, name, ga, name, gb, traits: "Trait");
                    if (s.Contains('{') || s.Contains('}')) problems.Add($"template left in {what} ({pos})");
                }

        return new WalkResult(paths, problems.Distinct().ToList(), InCharacter(arc, scenes, endings));
    }

    private static bool Satisfiable(StoryCondition c, HashSet<string> flags) =>
        c.Kind == ConditionKind.Flag && !c.Key.Contains('.') ? flags.Contains(c.Key) != c.Negated : true;

    private static WalkPath MakePath(List<WalkStep> steps, StoryEnding ending, bool fromLoss)
    {
        var effects = steps.Select(Outcome).OfType<StoryOutcome>().SelectMany(o => o.Effects).Concat(ending.Marks).ToList();
        var stress = new Dictionary<string, int>();
        foreach (var (trait, cost) in steps.Where(s => s.Option is not null).SelectMany(s => s.Option!.StressCosts))
            stress[trait] = stress.GetValueOrDefault(trait) + cost;
        return new WalkPath(steps.ToList(), ending, steps.All(s => s.Option is null || StoryValidator.IsFree(s.Option)),
            (int)effects.Where(e => e.Kind == EffectKind.Cailloux).Sum(e => e.Amount), effects.Count(e => e.Kind == EffectKind.Trait), stress, fromLoss);
    }

    public static StoryOutcome? Outcome(WalkStep step) => step.Option is null
        ? step.Scene.Narration
        : step.Success switch { true => step.Option.Success, false => step.Option.Failure, null => step.Option.Outcome };

    // Where it ends when it decides alone, in character: for « neutre » and each profile the arc speaks to.
    private static List<(string, IReadOnlyDictionary<string, double>)> InCharacter(StoryArc arc,
        Dictionary<string, StoryScene> scenes, Dictionary<string, StoryEnding> endings)
    {
        var profiles = new List<(string Name, IReadOnlyList<TraitInfo> Traits)> { (Neutral, Array.Empty<TraitInfo>()) };
        var named = arc.Header.Stages.FirstOrDefault() == PlynlingStage.Baby
            ? PlynlingTraits.All.Where(t => t.Kind == TraitKind.Childhood)
            : StoryValidator.Conditions(arc).Where(c => c.Kind is ConditionKind.Trait or ConditionKind.OtherTrait).Select(c => c.Key)
                .Concat(arc.Scenes.SelectMany(s => s.Options).SelectMany(o => o.StressCosts.Keys))
                .Concat(StoryValidator.Effects(arc).Where(e => e.Kind == EffectKind.Trait).SelectMany(e => new[] { e.Key, e.To }.OfType<string>()))
                .Distinct().Select(PlynlingTraits.ByKey).OfType<TraitInfo>();
        profiles.AddRange(named.Select(t => (t.NameM, (IReadOnlyList<TraitInfo>)new[] { t })));

        var result = new List<(string, IReadOnlyDictionary<string, double>)>();
        foreach (var (profileName, traits) in profiles)
        {
            var shares = new Dictionary<string, double>();
            if (arc.Scenes.Count > 0) Spread(arc.Scenes[0].Key, new HashSet<string>(), 1.0, new HashSet<string>());
            result.Add((profileName, shares));

            void Spread(string key, HashSet<string> flags, double p, HashSet<string> onPath)
            {
                if (endings.ContainsKey(key)) { shares[key] = shares.GetValueOrDefault(key) + p; return; }
                if (!scenes.TryGetValue(key, out var scene)) return;
                if (scene.RetiredTo is not null) { Spread(scene.RetiredTo, flags, p, onPath); return; }
                if (!onPath.Add(key)) return;
                // A handed-over decision is the other's, whose traits the walk does not know.
                var held = scene.Handover ? Array.Empty<TraitInfo>() : traits;
                if (scene.Narration is { } n) Go(n, p);
                var axes = PlynlingPersonality.Axes(held);
                var pool = scene.Options
                    .Where(o => !o.Retired && !o.OwnerOnly && o.Needs.All(c => Holds(c, held, flags))
                                && o.StressCosts.Where(kv => held.Any(t => t.Key == kv.Key)).Sum(kv => kv.Value) == 0)
                    .Select(o => (o, Math.Max(1.0, 100 + o.Lean.Sum(kv => kv.Value * axes.GetValueOrDefault(kv.Key) / 10.0))))
                    .ToList();
                var total = pool.Sum(x => x.Item2);
                foreach (var (o, w) in pool)
                {
                    var share = p * w / total;
                    if (o.Outcome is { } plain) Go(plain, share);
                    if (o.Success is { } s) Go(s, share / 2);
                    if (o.Failure is { } f) Go(f, share / 2);
                }
                onPath.Remove(key);

                void Go(StoryOutcome outcome, double q)
                {
                    if (outcome.Next is null) return;
                    var after = new HashSet<string>(flags);
                    foreach (var e in outcome.Effects)
                        if (e.Kind == EffectKind.Flag) after.Add(e.Key);
                        else if (e.Kind == EffectKind.Unflag) after.Remove(e.Key);
                    Spread(outcome.Next, after, q, onPath);
                }
            }
        }
        return result;
    }

    // In character, only what the walk knows holds: its own traits and the arc's flags.
    private static bool Holds(StoryCondition c, IReadOnlyList<TraitInfo> traits, HashSet<string> flags)
    {
        var truth = c.Kind switch
        {
            ConditionKind.Trait => traits.Any(t => t.Key == c.Key),
            ConditionKind.Flag when !c.Key.Contains('.') => flags.Contains(c.Key),
            _ => false,
        };
        return truth != c.Negated;
    }
}
