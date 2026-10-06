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

    private const int PulseTimeSalt = 300, PulsePickSalt = 301, TargetSalt = 302, ChallengeSalt = 310, AloneSalt = 311, BreakSalt = 330;
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
    public static EventDef? PickPulse(int plynlingId, int dayKey, IEnumerable<EventDef> defs, EventContext ctx,
        IReadOnlyList<string> recent, IReadOnlySet<TargetKind> targetable)
    {
        var eligible = defs
            .Where(d => d.Type == EventType.Pulse && d.BreakLevel == 0 && Eligible(d, ctx))
            .Where(d => d.Target == TargetKind.None || targetable.Contains(d.Target))
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

    public static int Chance(EventChallenge challenge, EventContext ctx) =>
        Math.Clamp(50 + 5 * (ctx.Stat(challenge.Stat) - challenge.Difficulty), 5, 95);

    public static bool Succeeds(int instanceId, int chance) =>
        StableRoll.Unit(instanceId, ChallengeSalt, 0) * 100 < chance;

    // What it may pick on its own: shown to it, and costing it no stress. Never empty for a catalog
    // event — the harness checks every event has an ungated option with no stress cost at all.
    public static IReadOnlyList<EventOption> AloneOptions(EventDef def, EventContext ctx) =>
        def.Options.Where(o => Visible(o, ctx) && StressCost(o, ctx) == 0).ToList();

    // The rule that keeps "not playing costs nothing": alone, an event never gains stress and never
    // applies a negative modifier or a coping trait — except a mental break, which only stress (from
    // the owner's own choices) can bring on.
    public static bool AppliesWhenAlone(EventEffect effect, EventDef def) =>
        def.BreakLevel > 0 || effect switch
        {
            StressChange s => s.Amount < 0,
            ApplyModifier m => PlynlingModifiers.ByKey(m.Key) is { Negative: false },
            GainCoping => false,
            _ => true,
        };

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
