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

public enum TriggerKind { Adoption, Stage, Draw, Relation, Death, Sick, Break, OldAge }

// Arg: the stage for Stage, the bond for Relation and Death, the level for Break.
public sealed record StoryTrigger(TriggerKind Kind, string? Arg);

public enum PaceKind { NextDay, Now, Hours }

// Hours is 1..48 when Kind is Hours, 0 otherwise.
public sealed record StoryPace(PaceKind Kind, int Hours);

public enum ConditionKind { Trait, OtherTrait, Stage, Flag, Memory, Keepsake, Epithet, Stat, Standing, Relation, Season, Passion, Ended, Chain, Gender }

public enum CompareOp { None, AtLeast, Below }

// Key is the condition's word: a trait key, a stage word, a flag (or "arc.flag"), a stat word, a cast
// key for Standing, "arc.ending" for Ended, "m" or "f" for Gender. Op and Number only for Stat and Standing.
public sealed record StoryCondition(ConditionKind Kind, string Key, bool Negated, CompareOp Op, int Number, StoryPos Pos);

public enum EffectKind { Flag, Unflag, Remember, Forget, Reveal, Stress, Modifier, Grow, Affinity, Cailloux, Item, Lift, Epithet, Keepsake, Unlock, Trait }

// Key is the effect's word (a key, a stat word for Grow, a need word for Lift, the trait gained or
// given up for Trait); To is the trait taken in a swap; Amount for Stress, Affinity, Cailloux, Lift.
// Other: lands on the other Plynling ([other remember …]).
public sealed record StoryEffect(EffectKind Kind, string Key, string? To, double Amount, bool Other, StoryPos Pos);

// One line of prose, shown only when every condition holds.
public sealed record StoryLine(string Text, IReadOnlyList<StoryCondition> When, StoryPos Pos);

public sealed record StoryParagraph(IReadOnlyList<StoryLine> Lines);

public sealed record StoryRoll(PlynlingStat Stat, int Difficulty);

// Next is the scene or ending it leads to; null only on a parse error.
public sealed record StoryOutcome(IReadOnlyList<StoryParagraph> Prose, IReadOnlyList<StoryEffect> Effects, string? Next, StoryPos Pos);

// A plain option has Outcome; an option with a Roll has Success and Failure instead.
public sealed record StoryOption(string Key, string Label, IReadOnlyList<StoryCondition> Needs, string? LockText, bool Secret,
    StoryRoll? Roll, IReadOnlyDictionary<AiAxis, int> Lean, IReadOnlyDictionary<string, int> StressCosts, bool OwnerOnly,
    bool Retired, string? Public, StoryOutcome? Outcome, StoryOutcome? Success, StoryOutcome? Failure, StoryPos Pos);

// A retired scene carries only RetiredTo. A narration scene has no options and a Narration outcome.
public sealed record StoryScene(string Key, string Title, StoryPace Pace, string? Public, bool Bump, bool Handover,
    string? RetiredTo, IReadOnlyList<StoryParagraph> Prose, IReadOnlyList<StoryOption> Options, StoryOutcome? Narration, StoryPos Pos);

public sealed record StoryEnding(string Key, string Title, IReadOnlyList<StoryParagraph> Prose, IReadOnlyList<StoryEffect> Marks, StoryPos Pos);

public sealed record MemoryDecl(string Key, string Holder, bool Public, int Shift, string Wording, StoryPos Pos);

public sealed record EpithetDecl(string Key, string Title, StoryPos Pos);

public sealed record KeepsakeDecl(string Key, string Name, string Story, bool Hurts, StoryPos Pos);

public sealed record StoryWeightBonus(int Bonus, IReadOnlyList<StoryCondition> When);

public sealed record StoryHeader(string Title, StoryFamily Family, IReadOnlyList<PlynlingStage> Stages, StoryTrigger Trigger,
    TimeSpan? Shelf, int Weight, IReadOnlyList<StoryWeightBonus> WeightBonuses, IReadOnlyList<StoryCondition> Requires,
    PlynlingBond? Other, string Public);

/// <summary>One arc. It opens on <c>Scenes[0]</c>.</summary>
public sealed record StoryArc(string Key, StoryPos Pos, StoryHeader Header, IReadOnlyList<MemoryDecl> Memories,
    IReadOnlyList<EpithetDecl> Epithets, IReadOnlyList<KeepsakeDecl> Keepsakes, IReadOnlyList<StoryScene> Scenes,
    IReadOnlyList<StoryEnding> Endings);

/// <summary>A villager from <c>Stories/village.cast</c>: the key memories and standing refer to.</summary>
public sealed record CastMember(string Key, string GivenName, string Byname, StoryPos Pos);
