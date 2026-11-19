namespace ProjectSYNCS.Helpers;

// A parsed story file (docs/superpowers/specs/2026-10-29-plynling-story-file-format-design.md). Every
// key here (arc, scene, option, ending, memory, epithet, keepsake, flag) is stored once the engine
// runs: append-only, never renamed. Pure data, so the checks and, later, the engine read the same
// thing.

/// <summary>Where something was written: the file and its 1-based line.</summary>
public sealed record StoryPos(string File, int Line)
{
    public override string ToString() => $"{File}:{Line}";
}

public sealed record StoryError(StoryPos Pos, string Message)
{
    public override string ToString() => $"{Pos}: {Message}";
}

public enum StoryFamily { Milestone, Friendship, Love, Rivalry, Village, Personal, Hardship }

public enum TriggerKind { Adoption, Stage, Draw, Relation, Death, Sick, Break, OldAge, Recovered, Visit }

// Arg: the stage for Stage, the bond for Relation and Death (null for a bare `death`: any mourner), the level for Break.
public sealed record StoryTrigger(TriggerKind Kind, string? Arg);

// short: a one- or two-scene event; long: an arc with endings (and so once per life).
public enum StorySize { Short, Long }

public enum PaceKind { NextDay, Now, Hours, Range }

// Hours is 1..48 when Kind is Hours (`+Nh`, exact). Range (`Nh`, `Nd`, `Nh-Mh`, `Nd-Md`): Hours is the
// minimum and MaxHours the maximum, both 1..720. 0 otherwise.
public sealed record StoryPace(PaceKind Kind, int Hours, int MaxHours = 0);

public enum ConditionKind { Trait, OtherTrait, Stage, Flag, Memory, Keepsake, Epithet, Stat, Standing, Relation, Season, Passion, Ended, Chain, Gender, Affinity, CanCouple }

public enum CompareOp { None, AtLeast, Below }

// Key is the condition's word: a trait key, a stage word, a flag (or "arc.flag"), a stat word, a cast
// key for Standing, "arc.ending" for Ended, "m" or "f" for Gender, "" for Affinity and CanCouple. Op and
// Number only for Stat, Standing and Affinity (the pair's affinity; Affinity and CanCouple only in `other_if:`).
public sealed record StoryCondition(ConditionKind Kind, string Key, bool Negated, CompareOp Op, int Number, StoryPos Pos);

public enum EffectKind { Flag, Unflag, Remember, Forget, Reveal, Stress, Modifier, Grow, Affinity, Cailloux, Item, Lift, Epithet, Keepsake, Unlock, Trait,
    Coping, Couple, Heartbreak, Bond }

// Key is the effect's word (a key, a stat word for Grow, a need word for Lift, the trait gained or
// given up for Trait, a coping trait or "" for Coping, peace/friends/bestfriends for Bond); To is the
// trait taken in a swap; Amount for Stress, Affinity, Cailloux, Lift.
// Other: lands on the other Plynling ([other remember …]).
public sealed record StoryEffect(EffectKind Kind, string Key, string? To, double Amount, bool Other, StoryPos Pos);

// Who the other Plynling is (`other:`): any of Bonds, any non-hostile one it has met, a rival or enemy, or anyone.
public enum OtherKind { Bonds, Known, Hostile, Anyone }

// Bonds is empty unless Kind is Bonds.
public sealed record StoryOther(OtherKind Kind, IReadOnlyList<PlynlingBond> Bonds);

// One line of prose, shown only when every condition holds.
public sealed record StoryLine(string Text, IReadOnlyList<StoryCondition> When, StoryPos Pos);

public sealed record StoryParagraph(IReadOnlyList<StoryLine> Lines);

// Vs: against the other Plynling's same stat ([roll <stat> vs]); Difficulty is then 0.
public sealed record StoryRoll(PlynlingStat Stat, int Difficulty, bool Vs = false);

// Next is the scene or ending it leads to; End is [end] (the event stops here, Next is null). Both null/false only on a parse error.
public sealed record StoryOutcome(IReadOnlyList<StoryParagraph> Prose, IReadOnlyList<StoryEffect> Effects, string? Next, bool End, StoryPos Pos);

// A plain option has Outcome; an option with a Roll has Success and Failure instead. Stance ([accept],
// [refuse]) leans a handover scene decided alone.
public sealed record StoryOption(string Key, string Label, IReadOnlyList<StoryCondition> Needs, string? LockText, bool Secret,
    StoryRoll? Roll, IReadOnlyDictionary<AiAxis, int> Lean, IReadOnlyDictionary<string, int> StressCosts, bool OwnerOnly,
    bool Retired, bool Mystery, StoryOutcome? Outcome, StoryOutcome? Success, StoryOutcome? Failure, StoryPos Pos,
    Stance Stance = Stance.Neutral);

// A retired scene carries only RetiredTo. A narration scene has no options and a Narration outcome.
// Was: the old event key this scene replaces (`was:`), resolved by PlynlingEvents.ByKey.
public sealed record StoryScene(string Key, string Title, StoryPace Pace, bool Handover,
    string? RetiredTo, IReadOnlyList<StoryParagraph> Prose, IReadOnlyList<StoryOption> Options, StoryOutcome? Narration, StoryPos Pos,
    string? Was = null);

public sealed record StoryEnding(string Key, string Title, IReadOnlyList<StoryParagraph> Prose, IReadOnlyList<StoryEffect> Marks, StoryPos Pos);

public sealed record MemoryDecl(string Key, string Holder, bool Public, int Shift, string Wording, StoryPos Pos);

public sealed record EpithetDecl(string Key, string Title, StoryPos Pos);

public sealed record KeepsakeDecl(string Key, string Name, string Story, bool Hurts, StoryPos Pos);

public sealed record StoryWeightBonus(int Bonus, IReadOnlyList<StoryCondition> When);

public sealed record StoryHeader(string Title, StoryFamily Family, IReadOnlyList<PlynlingStage> Stages, StoryTrigger Trigger,
    TimeSpan? Shelf, int Weight, IReadOnlyList<StoryWeightBonus> WeightBonuses, IReadOnlyList<StoryCondition> Requires,
    StoryOther? Other, StorySize Size, bool Once, TimeSpan? Cooldown, bool Locked, IReadOnlyList<StoryCondition> OtherIf);

/// <summary>One arc. It opens on <c>Scenes[0]</c>.</summary>
public sealed record StoryArc(string Key, StoryPos Pos, StoryHeader Header, IReadOnlyList<MemoryDecl> Memories,
    IReadOnlyList<EpithetDecl> Epithets, IReadOnlyList<KeepsakeDecl> Keepsakes, IReadOnlyList<StoryScene> Scenes,
    IReadOnlyList<StoryEnding> Endings);

/// <summary>A villager from <c>Stories/village.cast</c>: the key memories and standing refer to.</summary>
public sealed record CastMember(string Key, string GivenName, string Byname, StoryPos Pos);
