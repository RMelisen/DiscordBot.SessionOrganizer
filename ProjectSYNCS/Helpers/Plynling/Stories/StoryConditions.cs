using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Reads a parsed <see cref="StoryCondition"/> against an <see cref="EventContext"/>. Keys are the
/// vocabulary's words (StoryVocabulary): enums are their lower-cased names.
/// </summary>
public static class StoryConditions
{
    public static bool All(IEnumerable<StoryCondition> conditions, EventContext ctx) => conditions.All(c => Holds(c, ctx));

    public static bool Holds(StoryCondition c, EventContext ctx) => Raw(c, ctx) != c.Negated;

    private static bool Raw(StoryCondition c, EventContext ctx)
    {
        var k = ctx.Knowledge;
        switch (c.Kind)
        {
            case ConditionKind.Trait: return ctx.Has(c.Key);
            case ConditionKind.OtherTrait: return ctx.Other is not null && ctx.OtherTraitKeys?.Contains(c.Key) == true;
            case ConditionKind.Stage: return Word(ctx.Stage) == c.Key;
            case ConditionKind.Flag: return c.Key.Contains('.') ? k.ArcFlags.Contains(c.Key) : k.Flags.Contains(c.Key);
            case ConditionKind.Memory: return k.Memories.Contains(c.Key);
            case ConditionKind.Keepsake: return k.Marks.Contains((MarkKind.Keepsake, c.Key));
            case ConditionKind.Epithet: return k.Marks.Contains((MarkKind.Epithet, c.Key));
            case ConditionKind.Stat:
                return StoryVocabulary.TryEnum<PlynlingStat>(c.Key, out var stat) && Compare(ctx.Stat(stat), c);
            case ConditionKind.Standing: return Compare(k.Standing.GetValueOrDefault(c.Key), c);
            case ConditionKind.Relation: return ctx.Other is { } o && Word(o.Bond) == c.Key;
            case ConditionKind.Season: return Word(k.Season) == c.Key;
            case ConditionKind.Passion: return Word(PlynlingStats.PassionFor(ctx.Self)) == c.Key;
            case ConditionKind.Ended: return k.Ended.Contains(c.Key);
            case ConditionKind.Gender: return (ctx.Self.Gender == PlynlingGender.Female ? "f" : "m") == c.Key;
            case ConditionKind.Chain: return false; // expeditions, later
            default: return false;
        }
    }

    /// <summary>
    /// `other_if:` against one candidate, before an event is drawn: relation (its bond), affinity (the pair's)
    /// and can_couple (the visit rule). Nothing else is read there (the validator holds it).
    /// </summary>
    public static bool OnTarget(IReadOnlyList<StoryCondition> conditions, TargetInfo t) => conditions.All(c => c.Kind switch
    {
        ConditionKind.Relation => Word(t.Bond) == c.Key,
        ConditionKind.Affinity => Compare(t.Affinity, c),
        ConditionKind.CanCouple => t.CanCouple,
        _ => false,
    } != c.Negated);

    private static string Word<T>(T value) where T : struct, Enum => value.ToString().ToLowerInvariant();

    private static bool Compare(int value, StoryCondition c) => c.Op == CompareOp.Below ? value < c.Number : value >= c.Number;
}

public static class StoryText
{
    /// <summary>The lines whose conditions hold, joined with newlines; paragraphs (any left) with a blank line. Callers expand the templates.</summary>
    public static string Render(IReadOnlyList<StoryParagraph> prose, EventContext ctx) =>
        string.Join("\n\n", prose
            .Select(p => string.Join("\n", p.Lines.Where(l => StoryConditions.All(l.When, ctx)).Select(l => l.Text)))
            .Where(p => p.Length > 0));
}
