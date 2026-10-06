namespace ProjectSYNCS.Helpers;

/// <summary>
/// Every event rule, pure. Rolls are hashed (<see cref="StableRoll"/>) from the Plynling, the day or
/// the instance — never a <see cref="Random"/> — so the sweep and a click always agree, and a story
/// rebuilt later tells the same thing.
/// </summary>
public static class PlynlingEventEngine
{
    public const int MaxPendingPulses = 3;
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);
    public const int RecentWindow = 14;            // events not drawn again within the last 14 resolved, while another can be

    private const int PulseTimeSalt = 300, PulsePickSalt = 301, TargetSalt = 302, ChallengeSalt = 310, AloneSalt = 311, BreakSalt = 330,
        FollowUpSalt = 340, ResponseSalt = 341, TriggeredSalt = 342;
    private const int PulseFromMinute = 8 * 60, PulseSpanMinutes = 12 * 60;

    // Today's pulse: a minute between 08:00 and 20:00 Paris, hashed from the Plynling and the day.
    public static DateTimeOffset PulseAt(int plynlingId, int dayKey)
    {
        var minute = PulseFromMinute + (int)(StableRoll.Unit(plynlingId, dayKey, PulseTimeSalt) * PulseSpanMinutes);
        var wall = new DateTime(dayKey / 10000, dayKey / 100 % 100, dayKey % 100).AddMinutes(minute);
        return new DateTimeOffset(wall, AppTime.Zone.GetUtcOffset(wall));
    }

    public static bool Eligible(EventDef def, EventContext ctx) =>
        def.Stages.Contains(ctx.Stage) && (def.Condition?.Invoke(ctx) ?? true);

    private static double WeightOf(EventDef def, EventContext ctx)
    {
        double w = def.Weight;
        if (def.WeightByTrait is { } byTrait)
            foreach (var (trait, factor) in byTrait)
                if (ctx.Has(trait)) w *= factor;
        if (def.WeightByModifier is { } byModifier)
        {
            var active = PlynlingModifiers.Active(ctx.Self).Select(m => m.Info.Key).ToHashSet();
            foreach (var (key, factor) in byModifier)
                if (active.Contains(key)) w *= factor;
        }
        return Math.Max(0, w);
    }

    // The day's event among the pulse events it is eligible for, not seen recently, and — for a
    // social one — only when a target of that kind exists. `recent` is the keys of its last resolved
    // events, most recent first. They are excluded from the most recent back, but never all of the
    // eligible ones: a stage with fewer events than the window would otherwise lock for good, since
    // nothing new gets resolved and the window never moves. Then the least recently seen comes back.
    // `hasTarget` says whether a social event has a candidate meeting its target condition.
    public static EventDef? PickPulse(int plynlingId, int dayKey, IEnumerable<EventDef> defs, EventContext ctx,
        IReadOnlyList<string> recent, IReadOnlySet<TargetKind> targetable, Func<EventDef, bool>? hasTarget = null)
    {
        var eligible = defs
            .Where(d => d.Type == EventType.Pulse && d.BreakLevel == 0 && Eligible(d, ctx))
            .Where(d => d.Target == TargetKind.None || (targetable.Contains(d.Target) && (hasTarget?.Invoke(d) ?? true)))
            .Select(d => (Def: d, W: WeightOf(d, ctx)))
            .Where(x => x.W > 0)
            .ToList();
        var excluded = new HashSet<string>();
        foreach (var key in recent.Take(RecentWindow))
        {
            if (eligible.All(x => x.Def.Key == key || excluded.Contains(x.Def.Key))) break;
            excluded.Add(key);
        }
        var pool = eligible.Where(x => !excluded.Contains(x.Def.Key)).ToList();
        return pool.Count == 0 ? null : Weighted(pool, StableRoll.Unit(plynlingId, dayKey, PulsePickSalt));
    }

    // The mental break for a stress level just reached (hashed pick if a level ever has several).
    public static EventDef? PickBreak(int plynlingId, int salt, int level, IEnumerable<EventDef> defs)
    {
        var pool = defs.Where(d => d.BreakLevel == level).OrderBy(d => d.Key).ToList();
        return pool.Count == 0 ? null : pool[Math.Min(pool.Count - 1, (int)(StableRoll.Unit(plynlingId, salt, BreakSalt) * pool.Count))];
    }

    public static int? PickTarget(int plynlingId, int dayKey, IReadOnlyList<int> candidateIds)
    {
        if (candidateIds.Count == 0) return null;
        var sorted = candidateIds.OrderBy(i => i).ToList();
        return sorted[Math.Min(sorted.Count - 1, (int)(StableRoll.Unit(plynlingId, dayKey, TargetSalt) * sorted.Count))];
    }

    public static bool Visible(EventOption option, EventContext ctx) => option.Gate switch
    {
        null => true,
        TraitGate t => ctx.Has(t.TraitKey),
        StatGate s => ctx.Stat(s.Stat) >= s.AtLeast,
        _ => false,
    };

    public static int StressCost(EventOption option, EventContext ctx) =>
        option.StressCosts.Where(kv => ctx.Has(kv.Key)).Sum(kv => kv.Value);

    public static int Chance(EventChallenge challenge, EventContext ctx)
    {
        var against = challenge.VsTarget && ctx.OtherStats is { } theirs
            ? theirs.Single(l => l.Stat == challenge.Stat).Total
            : challenge.Difficulty;
        return Math.Clamp(50 + 5 * (ctx.Stat(challenge.Stat) - against), 5, 95);
    }

    public static bool Succeeds(int instanceId, int chance) =>
        StableRoll.Unit(instanceId, ChallengeSalt, 0) * 100 < chance;

    // What it may pick on its own: shown to it, costing it no stress, and not its owner's alone to take.
    // Never empty for a catalog event — the harness checks every event has such an option.
    public static IReadOnlyList<EventOption> AloneOptions(EventDef def, EventContext ctx) =>
        def.Options.Where(o => Visible(o, ctx) && StressCost(o, ctx) == 0 && !o.OwnerOnly).ToList();

    // The rule that keeps "not playing costs nothing": alone, an event never gains stress and never
    // applies a negative modifier or a coping trait — except a mental break, which only stress (from
    // the owner's own choices) can bring on. A heartbreak applies alone only in a response: it saddens
    // the one who declared, whose owner chose to (declaring is owner-only).
    public static bool AppliesWhenAlone(EventEffect effect, EventDef def) =>
        def.BreakLevel > 0 || effect switch
        {
            StressChange s => s.Amount < 0,
            ApplyModifier m => PlynlingModifiers.ByKey(m.Key) is { Negative: false },
            GainCoping => false,
            Heartbreak => def.Type == EventType.Response,
            _ => true,
        };

    // How willing a responder is: affinity (−100…100) and hidden compatibility (−20…20) — CK3's
    // acceptance. Applied to Accept options; its inverse to Refuse ones.
    public static double AcceptWeight(int affinity, int compatibility) =>
        Math.Max(0.05, 1 + affinity / 50.0 + compatibility / 20.0);

    // A response decided alone: its axes, as any choice, leaned by acceptance.
    public static EventOption DecideResponse(EventDef def, EventContext ctx, int instanceId)
    {
        var axes = PlynlingPersonality.Axes(ctx.Traits);
        var accept = ctx.Other is { } other ? AcceptWeight(other.Affinity, other.Compatibility) : 1;
        var pool = AloneOptions(def, ctx)
            .Select(o => (Def: o, W: Math.Max(1.0, 100 + o.Ai.Sum(kv => kv.Value * axes[kv.Key] / 10.0))
                                     * (o.Stance == Stance.Accept ? accept : o.Stance == Stance.Refuse ? 1 / accept : 1)))
            .ToList();
        return Weighted(pool, StableRoll.Unit(instanceId, ResponseSalt, 0));
    }

    public static DateTimeOffset FollowUpAt(int instanceId, FollowUp f, DateTimeOffset now) =>
        now.AddHours(f.MinHours + StableRoll.Unit(instanceId, FollowUpSalt, 0) * (f.MaxHours - f.MinHours));

    // The event an on-action brings: among those triggered by it and eligible for this Plynling.
    public static EventDef? PickTriggered(OnAction trigger, int plynlingId, int salt, IEnumerable<EventDef> defs, EventContext ctx)
    {
        var pool = defs.Where(d => d.Trigger == trigger && Eligible(d, ctx)).Select(d => (Def: d, W: WeightOf(d, ctx))).Where(x => x.W > 0).ToList();
        return pool.Count == 0 ? null : Weighted(pool, StableRoll.Unit(plynlingId, salt, TriggeredSalt));
    }

    public static EventOption DecideAlone(EventDef def, EventContext ctx, int instanceId)
    {
        var axes = PlynlingPersonality.Axes(ctx.Traits);
        var pool = AloneOptions(def, ctx)
            .Select(o => (Def: o, W: Math.Max(1.0, 100 + o.Ai.Sum(kv => kv.Value * axes[kv.Key] / 10.0))))
            .ToList();
        return Weighted(pool, StableRoll.Unit(instanceId, AloneSalt, 0));
    }

    // What its ado years leaned toward: every choice's AI weights, and the growth that actually applied.
    public static (IReadOnlyDictionary<AiAxis, int> Leaning, IReadOnlyDictionary<PlynlingStat, int> Growth) AdoHistory(
        IEnumerable<(EventOption Option, bool Success)> choices)
    {
        var leaning = new Dictionary<AiAxis, int>();
        var growth = new Dictionary<PlynlingStat, int>();
        foreach (var (option, success) in choices)
        {
            foreach (var (axis, v) in option.Ai) leaning[axis] = leaning.GetValueOrDefault(axis) + v;
            foreach (var g in (success ? option.OnSuccess : option.OnFailure).OfType<GrowStat>())
                growth[g.Stat] = growth.GetValueOrDefault(g.Stat) + g.Amount;
        }
        return (leaning, growth);
    }

    public static double AdultTraitWeight(TraitInfo trait, IReadOnlyDictionary<AiAxis, int> leaning, IReadOnlyDictionary<PlynlingStat, int> growth)
    {
        var dot = trait.Axes.Sum(kv => kv.Value * leaning.GetValueOrDefault(kv.Key));
        var fromGrowth = trait.Stats.Where(kv => kv.Value > 0).Sum(kv => 0.5 * growth.GetValueOrDefault(kv.Key));
        return Math.Max(0.25, 1 + dot / 400.0) + fromGrowth;
    }

    private static T Weighted<T>(IReadOnlyList<(T Def, double W)> pool, double roll)
    {
        var target = roll * pool.Sum(x => x.W);
        foreach (var (item, w) in pool)
        {
            if (target < w) return item;
            target -= w;
        }
        return pool[^1].Def;
    }
}
