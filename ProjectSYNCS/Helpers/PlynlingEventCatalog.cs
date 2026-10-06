using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Not stored (the instance stores the event's key), but new values go at the end.
public enum EventType { Pulse, Triggered, FollowUp, Response }

// Who a social event involves. Known: a Plynling it has met and is not hostile with. Hostile:
// rivals or enemies. Anyone: any living Plynling of the guild.
public enum TargetKind { None, Known, Hostile, Anyone }

// What an option needs to be offered at all. A hidden option is never shown and never picked alone.
public abstract record EventGate;
public sealed record TraitGate(string TraitKey) : EventGate;
public sealed record StatGate(PlynlingStat Stat, int AtLeast) : EventGate;

public sealed record EventChallenge(PlynlingStat Stat, int Difficulty);

// What an option does: stats, affinity, stress, modifiers, coping; follow-ups and responses are
// appended by a later phase. Target-side effects are affinity only (anti-griefing).
public abstract record EventEffect;
public sealed record GrowStat(PlynlingStat Stat, int Amount = 1) : EventEffect;
public sealed record AffinityShift(int Delta) : EventEffect;
// Positive = gain (owner choices and breaks only), negative = relief (always).
public sealed record StressChange(int Amount) : EventEffect;
// Applies or refreshes a modifier. A negative one never applies when the Plynling decided alone,
// outside a mental break.
public sealed record ApplyModifier(string Key) : EventEffect;
// Mental breaks only: one coping trait it lacks, drawn uniformly; nothing at two.
public sealed record GainCoping : EventEffect;

/// <summary>
/// One choice. <see cref="Key"/> is stored and never renamed. <see cref="Outcome"/> is told on
/// success (or always, without a challenge); <see cref="FailOutcome"/> on failure. Stress costs are
/// by trait key; an option with any cost for a trait the Plynling holds is never picked alone.
/// <see cref="Ai"/> leans the in-character choice (small numbers, about −3…+3).
/// </summary>
public sealed record EventOption(
    string Key, string Label, string Outcome, string? FailOutcome,
    EventGate? Gate, EventChallenge? Challenge,
    IReadOnlyList<EventEffect> OnSuccess, IReadOnlyList<EventEffect> OnFailure,
    IReadOnlyDictionary<string, int> StressCosts, IReadOnlyDictionary<AiAxis, int> Ai);

/// <summary>
/// One event. <see cref="Key"/> is stored and never renamed. Text is templated: {A}/{B} names,
/// {a:m|f}/{b:m|f} agreements (PlynlingVisitStory.Expand). Weight 100 is ordinary;
/// <see cref="WeightByTrait"/> and <see cref="WeightByModifier"/> multiply it for a held trait or an
/// active modifier. <see cref="BreakLevel"/> > 0 marks the mental break for that stress level
/// (triggered only).
/// </summary>
public sealed record EventDef(
    string Key, EventType Type, IReadOnlyList<PlynlingStage> Stages, string Title, string Scene,
    IReadOnlyList<EventOption> Options, int Weight = 100, TargetKind Target = TargetKind.None,
    Func<EventContext, bool>? Condition = null, IReadOnlyDictionary<string, double>? WeightByTrait = null,
    int BreakLevel = 0, IReadOnlyDictionary<string, double>? WeightByModifier = null);

// What the rules read about the Plynling an event happens to.
public sealed record EventContext(Plynling Self, IReadOnlyList<TraitInfo> Traits, IReadOnlyList<StatLine> Stats, PlynlingStage Stage)
{
    public bool Has(string traitKey) => Traits.Any(t => t.Key == traitKey);
    public int Stat(PlynlingStat stat) => Stats.Single(l => l.Stat == stat).Total;
}
