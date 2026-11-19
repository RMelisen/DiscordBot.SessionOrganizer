using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Turns validated story files into the <see cref="EventDef"/>s the event engine plays
/// (design spec 2026-11-17, "How C works"). One def per live scene, keyed <c>arc.scene</c>: the
/// first scene opens the arc (a pulse, or the moment its trigger brings), every later one is a
/// follow-up. Prose kept on the def is the unconditional part only; the story renders the rest from
/// <see cref="EventDef.Story"/> and <see cref="EventOption.Source"/>. Pure: no disk, no clock.
/// </summary>
public static class StoryCompiler
{
    /// <summary>
    /// The one option of a narration scene (no options, a single <c>[-> next]</c>). Stored
    /// (<c>PlynlingEventInstance.OptionKey</c>): never rename it. It cannot collide with an author's
    /// option: option keys are per scene, and the parser makes a narration only from a scene with no
    /// <c>*</c> option at all (StoryParser.Scenes), so no validator rule is needed. A narration scene later
    /// given options must keep its old runs' `continue` as a <c>[retired]</c> option.
    /// </summary>
    public const string NarrationOption = "continue";
    public const string NarrationLabel = "Continuer";

    /// <summary>A long drawn arc opens only this long after the Plynling's last long start.</summary>
    public static readonly TimeSpan LongDrawGap = TimeSpan.FromDays(21);

    // pace: next_day → a follow-up 20–28 h later (hashed, PlynlingEventEngine.FollowUpAt).
    public const double NextDayMinHours = 20, NextDayMaxHours = 28;

    /// <summary>A triggered arc's weight when its header writes none (EventDef's ordinary weight).</summary>
    public const int OrdinaryWeight = 100;

    public static string Key(string arc, string scene) => $"{arc}.{scene}";

    public static IReadOnlyList<EventDef> Compile(StoryLoad load)
    {
        if (load.Errors.Count > 0) throw new StoryLoadException(load.Errors);
        var keepsakes = load.Arcs.SelectMany(a => a.Keepsakes).ToDictionary(k => k.Key);
        return load.Arcs.SelectMany(a => new ArcCompiler(a, keepsakes).Defs()).ToList();
    }

    /// <summary>
    /// Every retired scene's key, mapped to the live scene it leads to (through a chain of retirements; one
    /// leading to an ending, or nowhere, is left out), and every live scene's old key (`was:`) to that scene.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Aliases(StoryLoad load)
    {
        var map = new Dictionary<string, string>();
        foreach (var arc in load.Arcs)
        {
            var c = new ArcCompiler(arc, new Dictionary<string, KeepsakeDecl>());
            foreach (var s in arc.Scenes.Where(s => s.RetiredTo is not null))
                if (c.Resolve(s.Key) is StoryScene live) map[Key(arc.Key, s.Key)] = Key(arc.Key, live.Key);
            foreach (var s in arc.Scenes.Where(s => s.RetiredTo is null && s.Was is not null))
                map[s.Was!] = Key(arc.Key, s.Key);
        }
        return map;
    }

    /// <summary>A prose block's unconditional lines; paragraphs joined by a blank line.</summary>
    public static string Plain(IReadOnlyList<StoryParagraph>? prose) =>
        prose is null ? "" : string.Join("\n\n", prose
            .Select(p => string.Join("\n", p.Lines.Where(l => l.When.Count == 0).Select(l => l.Text)))
            .Where(p => p.Length > 0));

    private sealed class ArcCompiler(StoryArc arc, IReadOnlyDictionary<string, KeepsakeDecl> keepsakes)
    {
        private readonly Dictionary<string, StoryScene> _scenes = arc.Scenes.GroupBy(s => s.Key).ToDictionary(g => g.Key, g => g.First());
        private readonly Dictionary<string, StoryEnding> _endings = arc.Endings.GroupBy(e => e.Key).ToDictionary(g => g.Key, g => g.First());

        public IEnumerable<EventDef> Defs()
        {
            var h = arc.Header;
            // A rivalry is with a hostile Plynling; every other bond with one it knows. The bond itself filters the candidates.
            var target = h.Other switch
            {
                null => TargetKind.None,
                { Kind: OtherKind.Bonds } o => o.Bonds.All(PlynlingBonds.IsHostile) ? TargetKind.Hostile : TargetKind.Known,
                { Kind: OtherKind.Known } => TargetKind.Known,
                { Kind: OtherKind.Hostile } => TargetKind.Hostile,
                _ => TargetKind.Anyone,
            };
            // other_if: filters the candidates too (relation, affinity, can_couple).
            var otherIf = h.OtherIf;
            Func<TargetInfo, bool>? targetCondition = h.Other switch
            {
                { Kind: OtherKind.Bonds } other => t => other.Bonds.Contains(t.Bond) && StoryConditions.OnTarget(otherIf, t),
                not null when otherIf.Count > 0 => t => StoryConditions.OnTarget(otherIf, t),
                _ => null,
            };
            // `weight:` is the arc's draw weight; unwritten (0), whatever the trigger, it weighs the ordinary 100.
            var weight = h.Weight > 0 ? h.Weight : OrdinaryWeight;
            for (var i = 0; i < arc.Scenes.Count; i++)
            {
                var scene = arc.Scenes[i];
                if (scene.RetiredTo is not null) continue;   // an alias (Aliases), not a def
                // A handover scene is the other owner's answer: a Response queued on the other Plynling by the
                // asking option's AskTarget, as today's replies (no target, condition or trigger of its own).
                if (scene.Handover)
                {
                    yield return new EventDef(Key(arc.Key, scene.Key), EventType.Response, h.Stages, scene.Title, Plain(scene.Prose), Options(scene),
                        Weight: weight, Story: new StoryLink(arc, scene));
                    continue;
                }
                var opens = i == 0;
                var (type, trigger) = opens ? Opening(h.Trigger) : (EventType.FollowUp, (OnAction?)null);
                yield return new EventDef(Key(arc.Key, scene.Key), type, h.Stages, scene.Title, Plain(scene.Prose), Options(scene),
                    Weight: weight, Target: target, Condition: opens ? Opens() : null, Trigger: trigger, TargetCondition: targetCondition,
                    Story: new StoryLink(arc, scene), WeightBonus: opens && h.WeightBonuses.Count > 0 ? Bonus() : null,
                    BreakLevel: opens && h.Trigger is { Kind: TriggerKind.Break, Arg: { } level } ? int.Parse(level, System.Globalization.CultureInfo.InvariantCulture) : 0);
            }
        }

        private static (EventType, OnAction?) Opening(StoryTrigger t) => t switch
        {
            { Kind: TriggerKind.Draw } => (EventType.Pulse, null),
            { Kind: TriggerKind.Adoption } => (EventType.Triggered, OnAction.Adopted),
            { Kind: TriggerKind.Stage, Arg: "teen" } => (EventType.Triggered, OnAction.BecameTeen),
            { Kind: TriggerKind.Stage, Arg: "adult" } => (EventType.Triggered, OnAction.BecameAdult),
            { Kind: TriggerKind.Sick } => (EventType.Triggered, OnAction.FellSick),
            { Kind: TriggerKind.Recovered } => (EventType.Triggered, OnAction.Recovered),
            { Kind: TriggerKind.Visit } => (EventType.Triggered, OnAction.AfterVisit),
            { Kind: TriggerKind.Death, Arg: null } => (EventType.Triggered, OnAction.Bereaved),
            // A break has no on-action: QueueBreaksAsync picks one by its BreakLevel when stress climbs past a level.
            { Kind: TriggerKind.Break } => (EventType.Triggered, null),
            _ => throw new InvalidOperationException($"trigger {t.Kind} {t.Arg}: the validator should have refused it"),
        };

        // Whether the arc may open: its requires, once, cooldown, unlock, and the gap between long drawn arcs.
        private Func<EventContext, bool> Opens()
        {
            var h = arc.Header;
            var longDraw = h.Size == StorySize.Long && h.Trigger.Kind == TriggerKind.Draw;
            return ctx =>
            {
                var k = ctx.Knowledge;
                var started = k.LastStarted.TryGetValue(arc.Key, out var last);
                if (h.Once && started) return false;
                if (h.Cooldown is { } cd && started && k.Now - last < cd) return false;
                if (h.Locked && !k.Marks.Contains((MarkKind.Unlock, arc.Key))) return false;
                if (longDraw && k.LastLongStart is { } l && k.Now - l < LongDrawGap) return false;
                return StoryConditions.All(h.Requires, ctx);
            };
        }

        private Func<EventContext, int> Bonus()
        {
            var bonuses = arc.Header.WeightBonuses;
            return ctx => bonuses.Where(b => StoryConditions.All(b.When, ctx)).Sum(b => b.Bonus);
        }

        private IReadOnlyList<EventOption> Options(StoryScene scene)
        {
            if (scene.Narration is { } n)
            {
                var source = new StoryOption(NarrationOption, NarrationLabel, [], null, false, null, new Dictionary<AiAxis, int>(),
                    new Dictionary<string, int>(), false, false, false, n, null, null, n.Pos);
                return [new EventOption(NarrationOption, NarrationLabel, Plain(n.Prose), null, null, null, Effects(n), [],
                    source.StressCosts, source.Lean, Source: source)];
            }
            return scene.Options.Select(o =>
            {
                var success = o.Roll is null ? o.Outcome : o.Success;
                var failure = o.Roll is null ? null : o.Failure;
                EventGate? gate = o.Retired ? new RetiredGate() : o.Needs.Count > 0 ? new StoryGate(o.Needs, o.LockText, o.Secret) : null;
                return new EventOption(o.Key, o.Label, Plain(success?.Prose), failure is null ? null : Plain(failure.Prose), gate,
                    o.Roll is { } r ? new EventChallenge(r.Stat, r.Vs ? 0 : r.Difficulty, VsTarget: r.Vs) : null, Effects(success), Effects(failure),
                    o.StressCosts, o.Lean, OwnerOnly: o.OwnerOnly, Stance: o.Stance, Source: o, Mystery: o.Mystery);
            }).ToList();
        }

        // An outcome's effects in order, then where it leads: a follow-up for a scene, the ending and its
        // marks for an ending, nothing for [end].
        private IReadOnlyList<EventEffect> Effects(StoryOutcome? outcome)
        {
            if (outcome is null) return [];
            var list = outcome.Effects.Select(Effect).ToList();
            switch (outcome.Next is { } next ? Resolve(next) : null)
            {
                // Handing the scene over asks the other owner (a Response on their card) instead of following up.
                case StoryScene { Handover: true } s:
                    list.Add(new AskTarget(Key(arc.Key, s.Key)));
                    break;
                case StoryScene s:
                    var (min, max) = Hours(s.Pace);
                    list.Add(new FollowUp(Key(arc.Key, s.Key), min, max));
                    break;
                case StoryEnding e:
                    list.Add(new EndStory(arc.Key, e.Key));
                    list.AddRange(e.Marks.Select(Effect));
                    break;
            }
            return list;
        }

        // A scene or an ending by key, following retired scenes to what replaced them (a loop gives null).
        public object? Resolve(string key)
        {
            var seen = new HashSet<string>();
            while (seen.Add(key))
            {
                if (_endings.TryGetValue(key, out var ending)) return ending;
                if (!_scenes.TryGetValue(key, out var scene)) return null;
                if (scene.RetiredTo is null) return scene;
                key = scene.RetiredTo;
            }
            return null;
        }

        private static (double, double) Hours(StoryPace pace) => pace.Kind switch
        {
            PaceKind.Now => (0, 0),
            PaceKind.NextDay => (NextDayMinHours, NextDayMaxHours),
            PaceKind.Range => (pace.Hours, pace.MaxHours),
            _ => (pace.Hours, pace.Hours),
        };

        // [other …] lands on the other Plynling, only in a handed-over scene, which the validator still refuses.
        private EventEffect Effect(StoryEffect e) => e.Kind switch
        {
            _ when e.Other => throw new InvalidOperationException($"{e.Pos}: [other {e.Kind} {e.Key}] is not supported yet"),
            EffectKind.Flag => new SetFlag(e.Key),
            EffectKind.Unflag => new ClearFlag(e.Key),
            EffectKind.Remember => new Remember(e.Key),
            EffectKind.Forget => new Forget(e.Key),
            EffectKind.Reveal => new RevealMemory(e.Key),
            EffectKind.Stress => new StressChange((int)e.Amount),
            EffectKind.Modifier => new ApplyModifier(e.Key),
            EffectKind.Grow => new GrowStat(Word<PlynlingStat>(e)),
            EffectKind.Affinity => new AffinityShift((int)e.Amount),
            EffectKind.Cailloux => new GiveCailloux((int)e.Amount),
            EffectKind.Item => new GiveItem(e.Key),
            EffectKind.Lift => new LiftNeed(Word<Need>(e), e.Amount),
            EffectKind.Epithet => new GiveMark(MarkKind.Epithet, e.Key),
            EffectKind.Keepsake => new GiveMark(MarkKind.Keepsake, e.Key, keepsakes.GetValueOrDefault(e.Key)?.Hurts ?? false),
            EffectKind.Unlock => new GiveMark(MarkKind.Unlock, e.Key),
            EffectKind.Trait when e.To is { } to => new SwapTrait(e.Key, to),
            EffectKind.Coping => new GainCoping(e.Key == "" ? null : e.Key),
            EffectKind.Couple => new Couple(),
            EffectKind.Heartbreak => new Heartbreak(),
            // A bond is reached only past BondFor's margin.
            EffectKind.Bond => new SetAffinityAtLeast(e.Key switch
            {
                "peace" => 0,
                "friends" => PlynlingBonds.FriendsFrom + PlynlingBonds.BondMargin,
                "bestfriends" => PlynlingBonds.BestFriendsFrom + PlynlingBonds.BondMargin,
                _ => throw new InvalidOperationException($"{e.Pos}: [bond {e.Key}]: the vocabulary should have refused it"),
            }),
            _ => throw new InvalidOperationException($"{e.Pos}: [{e.Kind} {e.Key}] cannot be compiled; the validator should have refused it"),
        };

        private static T Word<T>(StoryEffect e) where T : struct, Enum =>
            StoryVocabulary.TryEnum<T>(e.Key, out var v) ? v : throw new InvalidOperationException($"{e.Pos}: '{e.Key}' is not a {typeof(T).Name}");
    }
}
