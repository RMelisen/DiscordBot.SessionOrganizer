using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The line under an event's scene for one option, CK3's tooltip: what it costs, then what it gives
/// (design spec 2026-11-17, « The stakes line »). Pure and Context-free so the checks measure it. It
/// applies to every event, the C# ones included.
/// </summary>
public static class PlynlingEventStakes
{
    public const string MysteryText = "❔ on verra bien";

    /// <summary>
    /// The text after the option's label, parts joined with « · » (empty when there is nothing to say):
    /// the trait or stat it needs, the stress it costs, the odds with what success brings and « sinon »
    /// what failure brings; a padlock and its requirement instead of all that when the option is shown
    /// but cannot be picked; « on verra bien » in place of the rewards of a mystery option.
    /// </summary>
    public static string Line(EventOption o, EventDef def, EventContext ctx, PlynlingGender g, string? otherName)
    {
        if (o.Gate is StoryGate gate && !PlynlingEventEngine.Visible(o, ctx))
            return "🔒 " + LockLine(gate, ctx, g, otherName);

        var parts = new List<string>();
        if (o.Gate is TraitGate tg && PlynlingTraits.ByKey(tg.TraitKey) is { } trait) parts.Add($"{trait.Emoji} {trait.Name(g)}");
        if (o.Gate is StatGate sg) parts.Add($"{PlynlingStats.Emoji(sg.Stat)} {PlynlingStats.Name(sg.Stat)} {sg.AtLeast}+");
        // What choosing it against its nature costs, after its traits' multipliers, and which traits object.
        var cost = PlynlingStress.Scaled(PlynlingEventEngine.StressCost(o, ctx), ctx.Traits);
        if (cost > 0)
            parts.Add($"😣 +{cost} stress ({string.Join(", ", o.StressCosts.Keys.Where(ctx.Has).Select(k => PlynlingTraits.ByKey(k)!.Name(g)))})");

        var win = o.Mystery ? new List<string> { MysteryText } : Gains(o.OnSuccess, o, ctx, g);
        var lose = o.Mystery ? [] : Gains(o.OnFailure, o, ctx, g);
        if (o.Challenge is { } c)
        {
            var odds = $"🎲 {PlynlingStats.Name(c.Stat)} {PlynlingEventEngine.Chance(c, ctx)} %{(c.VsTarget && otherName is not null ? $" contre **{otherName}**" : "")}";
            if (win.Count > 0) odds += " : " + string.Join(" · ", win);
            if (lose.Count > 0 && !lose.SequenceEqual(win)) odds += " · sinon " + string.Join(" · ", lose);
            parts.Add(odds);
        }
        else
            parts.AddRange(win);
        return string.Join(" · ", parts);
    }

    // What an effect list gives, in the order it is written. Marks show only with their ending
    // (they ride behind its EndStory), once per kind.
    private static List<string> Gains(IReadOnlyList<EventEffect> effects, EventOption o, EventContext ctx, PlynlingGender g)
    {
        var gains = new List<string>();
        var ends = effects.OfType<EndStory>().Any();
        foreach (var effect in effects)
        {
            var text = effect switch
            {
                GiveCailloux c => $"🪙 {c.Amount}",
                GiveItem gi when ItemCatalog.ByKey(gi.ItemKey) is { } item => $"🎁 {ItemCatalog.ClearName(item)}",
                Remember r => Remembers(r.MemoryKey),
                ApplyModifier m when PlynlingModifiers.ByKey(m.Key) is { } mod => $"{mod.Emoji} {mod.Name(g)}",
                GrowStat s => $"{PlynlingStats.Emoji(s.Stat)} {PlynlingStats.Name(s.Stat)}",
                GiveMark { Kind: MarkKind.Epithet } when ends => "🏷️ un titre",
                GiveMark { Kind: MarkKind.Keepsake } when ends => "🎁 un souvenir à garder",
                SwapTrait when ends => "✨ un trait change",
                _ => null,
            };
            if (text is not null && !gains.Contains(text)) gains.Add(text);
        }
        return gains;
    }

    // A villager who will remember: ❤️ when the memory is a good one, the plain name when it is not.
    private static string? Remembers(string memoryKey)
    {
        if (!StoryCatalog.Memories.TryGetValue(memoryKey, out var m) || !StoryCatalog.Cast.TryGetValue(m.Holder, out var who)) return null;
        return (m.Shift > 0 ? "❤️ " : "") + $"{who.Byname} s'en souviendra";
    }

    // The padlock's text: the author's own words, or the needs said plainly.
    private static string LockLine(StoryGate gate, EventContext ctx, PlynlingGender g, string? otherName)
    {
        var self = PlynlingCardUi.SafeName(ctx.Self.Name);
        var otherGender = ctx.Other?.Target.Gender ?? PlynlingGender.Male;
        if (!string.IsNullOrWhiteSpace(gate.LockText))
            return PlynlingEvents.Expand(gate.LockText, self, g, otherName ?? "quelqu'un", otherGender).Replace("**", "");
        // Only what is still missing; every need if (oddly) none is.
        var missing = gate.Needs.Where(c => !StoryConditions.Holds(c, ctx)).ToList();
        return string.Join(", ", (missing.Count > 0 ? missing : gate.Needs).Select(c => Need(c, ctx, g, otherGender)));
    }

    /// <summary>« de » before a name or byname, contracted with its article: « le Fou » → « du Fou », « les X » → « des X »,
    /// « la X » → « de la X », « l'X » → « de l'X », a bare name → « de X ».</summary>
    public static string De(string name)
    {
        if (name.StartsWith("le ", StringComparison.OrdinalIgnoreCase)) return "du " + name[3..];
        if (name.StartsWith("les ", StringComparison.OrdinalIgnoreCase)) return "des " + name[4..];
        return "de " + name;
    }

    // One need in words. Flags and the like are the story's own business: said vaguely.
    private static string Need(StoryCondition c, EventContext ctx, PlynlingGender g, PlynlingGender otherGender)
    {
        var no = c.Negated;
        string Word(string positive, string negative) => no ? negative : positive;
        switch (c.Kind)
        {
            case ConditionKind.Trait when PlynlingTraits.ByKey(c.Key) is { } t:
                return Word(t.Name(g), "sans être " + t.Name(g).ToLowerInvariant());
            case ConditionKind.OtherTrait when PlynlingTraits.ByKey(c.Key) is { } t:
                return Word("l'autre : " + t.Name(otherGender).ToLowerInvariant(), "l'autre sans être " + t.Name(otherGender).ToLowerInvariant());
            case ConditionKind.Stage when StoryVocabulary.TryEnum<PlynlingStage>(c.Key, out var stage):
                return (no ? "pas " : "") + PlynlingCardUi.StageLabel(stage, g).ToLowerInvariant();
            case ConditionKind.Stat when StoryVocabulary.TryEnum<PlynlingStat>(c.Key, out var stat):
                var below = (c.Op == CompareOp.Below) != no;
                return $"{PlynlingStats.Emoji(stat)} {PlynlingStats.Name(stat)} {c.Number}{(below ? "−" : "+")}";
            case ConditionKind.Standing when StoryCatalog.Cast.TryGetValue(c.Key, out var who):
                return ((c.Op == CompareOp.Below) != no ? "mal vu " : "l'estime ") + De(who.Byname);
            case ConditionKind.Relation when StoryVocabulary.TryEnum<PlynlingBond>(c.Key, out var bond):
                return Word(PlynlingBonds.Role(bond, otherGender), "pas " + PlynlingBonds.Role(bond, otherGender));
            case ConditionKind.Season when StoryVocabulary.TryEnum<Season>(c.Key, out var season):
                return Word(ItemCatalog.SeasonLabel(season), "pas en " + ItemCatalog.SeasonLabel(season));
            case ConditionKind.Passion when StoryVocabulary.TryEnum<PlynlingPassion>(c.Key, out var passion):
                return Word(PlynlingPassions.Info(passion).Label, "pas " + PlynlingPassions.Info(passion).Label);
            case ConditionKind.Gender:
                return (c.Key == "f") != no ? "pour une fille" : "pour un garçon";
            case ConditionKind.Epithet when StoryCatalog.Epithets.TryGetValue(c.Key, out var e):
                return Word("un titre : " + PlynlingEvents.Expand(e.Title, "", g), "sans ce titre");
            case ConditionKind.Keepsake when StoryCatalog.Keepsakes.TryGetValue(c.Key, out var k):
                return Word(PlynlingEvents.Expand(k.Name, "", g), "sans ce souvenir");
            case ConditionKind.Memory:
                return Word("un souvenir au village", "sans souvenir au village");
            default:
                return Word("quelque chose à vivre d'abord", "pas après ce qui s'est passé");
        }
    }
}
