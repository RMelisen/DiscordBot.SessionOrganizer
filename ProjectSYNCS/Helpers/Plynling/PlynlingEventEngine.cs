using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// What the ado years were like, for the adulte trait (PlynlingEventEngine.AdultTraitWeight).
public sealed record AdoYears(IReadOnlyDictionary<AiAxis, int> Leaning, IReadOnlyDictionary<PlynlingStat, int> Growth, int Count);

/// <summary>
/// Every event rule, pure. Rolls are hashed (<see cref="StableRoll"/>) from the Plynling, the day or
/// the instance — never a <see cref="Random"/> — so the sweep and a click always agree, and a story
/// rebuilt later tells the same thing.
/// </summary>
public static class PlynlingEventEngine
{
    public const int MaxPendingPulses = 3;
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);
    public const int RecentWindow = 14;            // pulses not drawn again within the last 14 resolved pulses, while another can be

    private const int PulseTimeSalt = 300, PulsePickSalt = 301, TargetSalt = 302, ChallengeSalt = 310, AloneSalt = 311, BreakSalt = 330,
        FollowUpSalt = 340, ResponseSalt = 341, TriggeredSalt = 342;
    private const int PulseFromMinute = 8 * 60, PulseSpanMinutes = 12 * 60;

    // Today's pulse: a minute between 08:00 and 20:00 Paris, hashed from the Plynling and the day.
    public static DateTimeOffset PulseAt(int plynlingId, int dayKey)
    {
        var minute = PulseFromMinute + (int)(StableRoll.Unit(plynlingId, dayKey, PulseTimeSalt) * PulseSpanMinutes);
        return AppTime.AtWallClock(AppTime.FromDayKey(dayKey).AddMinutes(minute));
    }

    // When it decides alone: 24 h after the event became available — unless a freeze ended since.
    // Nothing happens while frozen, the clock included, so an event that was waiting gets a fresh 24 h
    // from the thaw (which resets LiveSince). Deaths cancel what waits, so the LiveSince of a
    // resurrection or an adoption never meets an older open event.
    public static DateTimeOffset DueAt(PlynlingEventInstance inst, Plynling p) =>
        inst.AvailableAt < p.LiveSince && p.LiveSince + Lifetime > inst.ExpiresAt ? p.LiveSince + Lifetime : inst.ExpiresAt;

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
    // pulse events, most recent first (follow-ups, responses and the rest never take a slot). They are excluded from the most recent back, but never all of the
    // eligible ones: a stage with fewer events than the window would otherwise lock for good, since
    // nothing new gets resolved and the window never moves. Then the least recently seen comes back.
    // `hasTarget` says whether a social event has a candidate meeting its target condition.
    public static EventDef? PickPulse(int plynlingId, int dayKey, IEnumerable<EventDef> defs, EventContext ctx,
        IReadOnlyList<string> recent, Func<EventDef, bool> hasTarget)
    {
        var eligible = defs
            .Where(d => d.Type == EventType.Pulse && d.BreakLevel == 0 && Eligible(d, ctx))
            .Where(d => d.Target == TargetKind.None || hasTarget(d))
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
        return pool.Count == 0 ? null : StableRoll.Weighted(pool, plynlingId, dayKey, PulsePickSalt);
    }

    // The mental break for a stress level just reached (hashed pick if a level ever has several).
    public static EventDef? PickBreak(int plynlingId, int salt, int level, IEnumerable<EventDef> defs)
    {
        var pool = defs.Where(d => d.BreakLevel == level).OrderBy(d => d.Key).ToList();
        return pool.Count == 0 ? null : StableRoll.Pick(pool, plynlingId, salt, BreakSalt);
    }

    public static int? PickTarget(int plynlingId, int dayKey, IReadOnlyList<int> candidateIds) =>
        candidateIds.Count == 0 ? null : StableRoll.Pick(candidateIds.OrderBy(i => i).ToList(), plynlingId, dayKey, TargetSalt);

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

    // Whether an event's lesson (GrowStat) sticks for good, beyond its few days of Practice: rare, so a
    // stat keeps a meaning. One roll per instance, stored (PlynlingEventInstance.GrewForGood).
    public const double PermanentGrowthChance = 0.10;
    private const int GrowthSalt = 360;

    public static bool GrowsForGood(int instanceId) =>
        StableRoll.Unit(instanceId, GrowthSalt, 0) < PermanentGrowthChance;

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

    // Whether an option's effect took place — what ApplyEventAsync does and what the story tells.
    public static bool Applies(EventEffect effect, EventDef def, bool decidedAlone) =>
        !decidedAlone || AppliesWhenAlone(effect, def);

    // How willing a responder is: affinity (−100…100) and hidden compatibility (−20…20) — CK3's
    // acceptance. Applied to Accept options; its inverse to Refuse ones.
    public static double AcceptWeight(int affinity, int compatibility) =>
        Math.Max(0.05, 1 + affinity / 50.0 + compatibility / 20.0);

    // A response decided alone: its axes, as any choice, leaned by acceptance.
    public static EventOption DecideResponse(EventDef def, EventContext ctx, int instanceId) =>
        Decide(def, ctx, ctx.Other is { } other ? AcceptWeight(other.Affinity, other.Compatibility) : 1, instanceId, ResponseSalt);

    public static EventOption DecideAlone(EventDef def, EventContext ctx, int instanceId) =>
        Decide(def, ctx, 1, instanceId, AloneSalt);

    // In character: each option it may take alone, weighted by its axes, and an Accept/Refuse stance
    // by `accept` or its inverse (1 outside a response).
    private static EventOption Decide(EventDef def, EventContext ctx, double accept, int instanceId, int salt)
    {
        var axes = PlynlingPersonality.Axes(ctx.Traits);
        var pool = AloneOptions(def, ctx)
            .Select(o => (o, Math.Max(1.0, 100 + o.Ai.Sum(kv => kv.Value * axes[kv.Key] / 10.0))
                             * (o.Stance == Stance.Accept ? accept : o.Stance == Stance.Refuse ? 1 / accept : 1)))
            .ToList();
        return StableRoll.Weighted(pool, instanceId, salt, 0);
    }

    public static DateTimeOffset FollowUpAt(int instanceId, FollowUp f, DateTimeOffset now) =>
        now.AddHours(f.MinHours + StableRoll.Unit(instanceId, FollowUpSalt, 0) * (f.MaxHours - f.MinHours));

    // The event an on-action brings: among those triggered by it and eligible for this Plynling.
    public static EventDef? PickTriggered(OnAction trigger, int plynlingId, int salt, IEnumerable<EventDef> defs, EventContext ctx)
    {
        var pool = defs.Where(d => d.Trigger == trigger && Eligible(d, ctx)).Select(d => (Def: d, W: WeightOf(d, ctx))).Where(x => x.W > 0).ToList();
        return pool.Count == 0 ? null : StableRoll.Weighted(pool, plynlingId, salt, TriggeredSalt);
    }

    // What its ado years leaned toward: every choice's AI weights, the growth that actually applied, and
    // how many choices that was.
    public static AdoYears AdoHistory(IEnumerable<(EventOption Option, bool Success)> choices)
    {
        var leaning = new Dictionary<AiAxis, int>();
        var growth = new Dictionary<PlynlingStat, int>();
        var count = 0;
        foreach (var (option, success) in choices)
        {
            count++;
            foreach (var (axis, v) in option.Ai) leaning[axis] = leaning.GetValueOrDefault(axis) + v;
            foreach (var g in (success ? option.OnSuccess : option.OnFailure).OfType<GrowStat>())
                growth[g.Stat] = growth.GetValueOrDefault(g.Stat) + g.Amount;
        }
        return new AdoYears(leaning, growth, count);
    }

    // An average ado choice: per axis, and per stat for the growth it brings (a challenge counted half
    // won), the mean of each event's options, averaged over every event the ado years can meet. The
    // catalog leans kind and sociable and grows Diplomacy most, so a history is read as its distance from
    // `count` average choices — otherwise even an owner who clicks at random raises a kind, talkative adulte.
    private static readonly EventDef[] AdoEvents = PlynlingEvents.All
        .Where(d => d.Stages.Contains(PlynlingStage.Teen) && d.BreakLevel == 0 && d.Options.Count > 0).ToArray();

    private static readonly IReadOnlyDictionary<AiAxis, double> AdoLeaningBaseline = Enum.GetValues<AiAxis>().ToDictionary(a => a,
        a => AdoEvents.Length == 0 ? 0 : AdoEvents.Average(d => d.Options.Average(o => (double)o.Ai.GetValueOrDefault(a))));

    private static readonly IReadOnlyDictionary<PlynlingStat, double> AdoGrowthBaseline = PlynlingStats.All.ToDictionary(s => s,
        s => AdoEvents.Length == 0 ? 0 : AdoEvents.Average(d => d.Options.Average(o =>
        {
            double Grown(IReadOnlyList<EventEffect> effects) => effects.OfType<GrowStat>().Where(g => g.Stat == s).Sum(g => g.Amount);
            return o.Challenge is null ? Grown(o.OnSuccess) : (Grown(o.OnSuccess) + Grown(o.OnFailure)) / 2;
        })));

    // How far the ado years pull: the recentred leaning measured along the trait's own direction (its
    // axes as a unit vector — otherwise a trait with a 200 on one axis swings far more than one with a few
    // 35s, and the 0.25 floor turns that swing into a head start), +100 % per LeaningScale points; plus
    // the recentred growth of the stats the trait raises, weighted by its bonus (so a trait that raises
    // every stat — Ambitieux — gets their average, not their sum), GrowthPull per point.
    private const double LeaningScale = 2.5, GrowthPull = 0.5;

    public static double AdultTraitWeight(TraitInfo trait, AdoYears ado)
    {
        var length = Math.Sqrt(trait.Axes.Values.Sum(v => (double)v * v));
        var dot = length == 0
            ? 0
            : trait.Axes.Sum(kv => kv.Value * (ado.Leaning.GetValueOrDefault(kv.Key) - ado.Count * AdoLeaningBaseline[kv.Key])) / length;
        var raised = trait.Stats.Where(kv => kv.Value > 0).ToList();
        var fromGrowth = raised.Count == 0
            ? 0
            : GrowthPull * raised.Sum(kv => kv.Value * (ado.Growth.GetValueOrDefault(kv.Key) - ado.Count * AdoGrowthBaseline[kv.Key]))
              / raised.Sum(kv => kv.Value);
        return Math.Max(0.25, 1 + dot / LeaningScale + fromGrowth);
    }
}
