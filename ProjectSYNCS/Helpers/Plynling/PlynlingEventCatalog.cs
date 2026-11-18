using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Not stored (the instance stores the event's key), but new values go at the end.
public enum EventType { Pulse, Triggered, FollowUp, Response }

// Who a social event involves. Known: a Plynling it has met and is not hostile with. Hostile:
// rivals or enemies. Anyone: any living Plynling of the guild.
public enum TargetKind { None, Known, Hostile, Anyone }

// What brings a triggered event (PlynlingService.FlushOnActionsAsync). Not stored; new values at the end.
public enum OnAction { Adopted, BecameTeen, BecameAdult, AfterVisit, Bereaved, FellSick, Recovered }

// A response option's side, for deciding alone by acceptance.
public enum Stance { Neutral, Accept, Refuse }

// What an option needs to be offered at all. A hidden option is never shown and never picked alone.
public abstract record EventGate;
public sealed record TraitGate(string TraitKey) : EventGate;
public sealed record StatGate(PlynlingStat Stat, int AtLeast) : EventGate;
// A story option's [needs …]: offered when every need holds. LockText is what the locked button says;
// Secret hides it while locked (StoryCompiler).
public sealed record StoryGate(IReadOnlyList<StoryCondition> Needs, string? LockText, bool Secret) : EventGate;
// A story option taken out ([retired]): kept so old stories still find it by key, never shown or picked.
public sealed record RetiredGate : EventGate;

// VsTarget: against the other Plynling's same stat instead of Difficulty (the difficulty stands in
// when no other is known).
public sealed record EventChallenge(PlynlingStat Stat, int Difficulty, bool VsTarget = false);

// What an option does: stats, affinity, stress, modifiers, coping, follow-ups, responses and the
// relation. Another owner's Plynling is touched only through the relation (anti-griefing) — the one
// exception being Heartbreak, which saddens the one who declared.
public abstract record EventEffect;
// A lesson in that stat: its Practice modifier (+1 for a few days), and only rarely the permanent
// +Amount (PlynlingEventEngine.GrowsForGood) — permanent growth on every choice made every challenge
// trivial within a season.
public sealed record GrowStat(PlynlingStat Stat, int Amount = 1) : EventEffect;
public sealed record AffinityShift(int Delta) : EventEffect;
// Positive = gain (owner choices and breaks only), negative = relief (always).
public sealed record StressChange(int Amount) : EventEffect;
// Applies or refreshes a modifier. A negative one never applies when the Plynling decided alone,
// outside a mental break.
public sealed record ApplyModifier(string Key) : EventEffect;
// Mental breaks only: one coping trait it lacks, drawn uniformly; nothing at two.
// With TraitKey, that coping trait (CK3 ties each to its choice: eating → comfort_eater); nothing if it
// already has it or holds two. Without, one it lacks, drawn uniformly.
public sealed record GainCoping(string? TraitKey = null) : EventEffect;
// Brings a later event (same target), between MinHours and MaxHours from now, hashed.
public sealed record FollowUp(string EventKey, double MinHours, double MaxHours) : EventEffect;
// Asks the other Plynling's owner to answer with ResponseKey (a Response event on their card).
public sealed record AskTarget(string ResponseKey) : EventEffect;
// Lifts the pair's affinity to at least Value (never lowers it).
public sealed record SetAffinityAtLeast(int Value) : EventEffect;
// Makes the pair a couple — only if the visit rules still allow it when it resolves.
public sealed record Couple : EventEffect;
// A refused declaration: the one who declared (the event's target, in a response) is saddened and
// the pair loses PlynlingBonds.HeartbreakLoss.
public sealed record Heartbreak : EventEffect;
// CK3's add_gold: cailloux into the owner's wallet (EconomyLog.EarnEvent). Never the mascot's — it has
// no player to pay. Keep it small: a /work shift pays 25–40.
public sealed record GiveCailloux(int Amount) : EventEffect;
// CK3's artifacts: one ItemCatalog item into the owner's inventory (a set it completes pays out in the
// same save). Never the mascot's.
public sealed record GiveItem(string ItemKey) : EventEffect;
// Raises one of its needs by Amount (0–1): a good laugh, a meal, a dip in the river.
public sealed record LiftNeed(Need Need, double Amount) : EventEffect;

// Story effects (StoryCompiler). Flags belong to the run; memories and marks to the Plynling.
public sealed record SetFlag(string Key) : EventEffect;
public sealed record ClearFlag(string Key) : EventEffect;
public sealed record Remember(string MemoryKey) : EventEffect;
public sealed record Forget(string MemoryKey) : EventEffect;
public sealed record RevealMemory(string MemoryKey) : EventEffect;
// An ending's mark. Hurts: a keepsake declared `hurts`, never given when it decided alone.
public sealed record GiveMark(MarkKind Kind, string Key, bool Hurts = false) : EventEffect;
public sealed record SwapTrait(string From, string To) : EventEffect;
// The run reached this ending; the ending's marks follow it in the same list.
public sealed record EndStory(string ArcKey, string EndingKey) : EventEffect;

// Where a compiled event came from: its arc and scene (the story renders from them).
public sealed record StoryLink(StoryArc Arc, StoryScene Scene);

/// <summary>
/// One choice. <see cref="Key"/> is stored and never renamed. <see cref="Outcome"/> is told on
/// success (or always, without a challenge); <see cref="FailOutcome"/> on failure. Stress costs are
/// by trait key; an option with any cost for a trait the Plynling holds is never picked alone, and
/// neither is an <see cref="OwnerOnly"/> one. <see cref="Ai"/> leans the in-character choice (small
/// numbers, about −3…+3); <see cref="Stance"/> leans a response decided alone by acceptance.
/// A story option carries its <see cref="Source"/> (its prose renders from it) and
/// <see cref="Mystery"/> (its reward is not shown).
/// </summary>
public sealed record EventOption(
    string Key, string Label, string Outcome, string? FailOutcome,
    EventGate? Gate, EventChallenge? Challenge,
    IReadOnlyList<EventEffect> OnSuccess, IReadOnlyList<EventEffect> OnFailure,
    IReadOnlyDictionary<string, int> StressCosts, IReadOnlyDictionary<AiAxis, int> Ai,
    bool OwnerOnly = false, Stance Stance = Stance.Neutral, StoryOption? Source = null, bool Mystery = false);

/// <summary>
/// One event. <see cref="Key"/> is stored and never renamed. Text is templated: {A}/{B} names,
/// {a:m|f}/{b:m|f} agreements (PlynlingVisitStory.Expand), {T} the new trait(s) in a trait reveal.
/// Weight 100 is ordinary; <see cref="WeightByTrait"/> and <see cref="WeightByModifier"/> multiply it
/// for a held trait or an active modifier. <see cref="BreakLevel"/> > 0 marks the mental break for
/// that stress level (triggered only). <see cref="Trigger"/> is the on-action that brings it;
/// <see cref="TargetCondition"/> filters a social event's candidates. A compiled story scene carries
/// its <see cref="Story"/>; <see cref="WeightBonus"/> is added to <see cref="Weight"/> before the factors.
/// </summary>
public sealed record EventDef(
    string Key, EventType Type, IReadOnlyList<PlynlingStage> Stages, string Title, string Scene,
    IReadOnlyList<EventOption> Options, int Weight = 100, TargetKind Target = TargetKind.None,
    Func<EventContext, bool>? Condition = null, IReadOnlyDictionary<string, double>? WeightByTrait = null,
    int BreakLevel = 0, IReadOnlyDictionary<string, double>? WeightByModifier = null,
    OnAction? Trigger = null, Func<TargetInfo, bool>? TargetCondition = null,
    StoryLink? Story = null, Func<EventContext, int>? WeightBonus = null);

// Who the other Plynling is to this one, for target conditions and acceptance. CanCouple is the visit
// rule (PlynlingBonds.CanConfess: best friends, close enough, a boy and a girl, neither taken) — the
// one fact a condition on the target alone could not tell.
public sealed record TargetInfo(Plynling Target, PlynlingBond Bond, int Affinity, int Compatibility, bool EitherInCouple,
    bool CanCouple = false);

// What the rules read about the Plynling an event happens to, and the other one if there is one.
public sealed record EventContext(Plynling Self, IReadOnlyList<TraitInfo> Traits, IReadOnlyList<StatLine> Stats, PlynlingStage Stage,
    TargetInfo? Other = null, IReadOnlyList<StatLine>? OtherStats = null, IReadOnlyList<string>? OtherTraitKeys = null,
    StoryKnowledge? Story = null)
{
    // What the story files read: flags, memories, marks, endings, standing, season. Empty when none was given.
    public StoryKnowledge Knowledge => Story ?? StoryKnowledge.Empty(DateTimeOffset.MinValue);

    public bool Has(string traitKey) => Traits.Any(t => t.Key == traitKey);
    public int Stat(PlynlingStat stat) => Stats.Single(l => l.Stat == stat).Total;
}
