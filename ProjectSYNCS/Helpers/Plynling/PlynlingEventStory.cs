using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Who an event happened to, as the story shows them. Names are made safe here, once.
public sealed record EventCast(string Name, PlynlingGender Gender, PlynlingSpecies Species, PlynlingStage Stage, ulong OwnerId)
{
    public static EventCast Of(Plynling p, DateTimeOffset now) =>
        new(PlynlingCardUi.SafeName(p.Name), p.Gender, p.Species, PlynlingLife.Stage(p, now), p.OwnerId);

    // As it was when its event resolved: the stored age, or an estimate for older rows.
    public static EventCast Then(Plynling p, long? ageSeconds, DateTimeOffset resolvedAt, DateTimeOffset now) =>
        new(PlynlingCardUi.SafeName(p.Name), p.Gender, p.Species,
            PlynlingLife.StageAt(ageSeconds is { } s ? TimeSpan.FromSeconds(s) : PlynlingLife.AgeAt(p, resolvedAt, now)), p.OwnerId);
}

// Where the answer to an ask stands: still awaited, given, or lost for good (cancelled — the other
// one died or was abandoned — or never asked, because the other one was frozen or gone by then).
public enum ReplyState { Waiting, Answered, Lost }

// SelfFace / TargetFace: the two faces on the last page, the outcome (PlynlingEventFaces); the other
// pages show both calm.
public sealed record EventStory(int InstanceId, int PlynlingId, string Heading, IReadOnlyList<string> Pages, EventCast Self, EventCast? Target, uint Accent,
    PlynlingMood SelfFace = PlynlingMood.Content, PlynlingMood TargetFace = PlynlingMood.Content);

/// <summary>
/// An event told as pages — the scene, the choice, the challenge if any, the outcome — built only
/// from the stored instance and the catalog, so any old card can page it after any restart. Line
/// picks are hashed from the instance id.
/// </summary>
public static class PlynlingEventStory
{
    // When nobody chose: a line, picked by the instance id.
    private static readonly string[] AloneLines =
    {
        "Personne n'est venu trancher. {A} a décidé {a:tout seul|toute seule}.",
        "{A} a attendu un conseil, puis a haussé les épaules et fait à sa façon.",
        "Faute d'avis, {A} a écouté son petit caractère.",
    };

    // parentTitle: the event this one follows from (a response, a follow-up). reply: where the response
    // this one asked for stands — its outcome says it waits, or that it never came. ctx: what a story's
    // conditional prose reads (the run's flags, the Plynling's memories…).
    public static EventStory Build(PlynlingEventInstance inst, EventCast self, EventCast? target, string? parentTitle = null,
        ReplyState reply = ReplyState.Waiting, EventContext? ctx = null)
    {
        var accent = PlynlingCatalog.Info(self.Species).Accent;
        if (PlynlingEvents.ByKey(inst.EventKey) is not { } def
            || def.Options.FirstOrDefault(o => o.Key == inst.OptionKey) is not { } option)
            return new EventStory(inst.Id, inst.PlynlingId, "📜 Une petite aventure",
                new[] { $"**{self.Name}** a vécu une petite aventure. Les détails se sont perdus en route." }, self, target, accent);

        var X = Expander(inst, def, self, target);
        var pages = new List<string> { (parentTitle is null ? "" : $"-# Suite de « {parentTitle} »\n") + X(SceneText(def, ctx)) };
        pages.Add(inst.DecidedAlone
            ? X(StableRoll.Pick(AloneLines, inst.Id, 320, 0))
            : $"<@{self.OwnerId}> a tranché pour **{self.Name}** : **{Label(option, X)}**.");
        if (option.Challenge is { } c && inst.ChancePercent is { } chance)
            pages.Add($"🎲 **{PlynlingStats.Name(c.Stat)}**{(c.VsTarget && target is not null ? $" contre **{target.Name}**" : "")} — {chance} % de chances… " +
                      (inst.ChallengeSucceeded == true ? "**réussi !**" : "**raté.**"));
        pages.Add(OutcomeText(inst, self, target, reply, ctx));
        var (selfFace, targetFace) = PlynlingEventFaces.Outcome(inst, def, option);
        return new EventStory(inst.Id, inst.PlynlingId, $"📜 {def.Title}", pages, self, target, accent, selfFace, targetFace);
    }

    /// <summary>
    /// The scene's text, still templated. A story def renders its prose against the context (a null
    /// context keeps only the unconditional lines); a C# def returns its Scene as written.
    /// </summary>
    public static string SceneText(EventDef def, EventContext? ctx) =>
        def.Story is { } link ? Prose(link.Scene.Prose, ctx) : def.Scene;

    /// <summary>
    /// An option's outcome prose, still templated: a story option renders its own prose (the failure's
    /// when it failed, else the success's); a C# option returns Outcome / FailOutcome as written. When the
    /// effects that apply end the story, the ending's title (bold) and prose follow.
    /// </summary>
    public static string OutcomeProse(EventDef def, EventOption o, bool success, EventContext? ctx)
    {
        if (def.Story is not { } link || o.Source is not { } src)
            return success ? o.Outcome : o.FailOutcome ?? o.Outcome;
        var outcome = success ? src.Outcome ?? src.Success : src.Failure ?? src.Success ?? src.Outcome;
        var prose = outcome is null ? "" : Prose(outcome.Prose, ctx);
        if ((success ? o.OnSuccess : o.OnFailure).OfType<EndStory>().FirstOrDefault() is { } end
            && link.Arc.Endings.FirstOrDefault(e => e.Key == end.EndingKey) is { } ending)
        {
            var told = Prose(ending.Prose, ctx);
            prose = string.Join("\n\n", new[] { prose, $"**{ending.Title}**", told }.Where(s => s.Length > 0));
        }
        return prose;
    }

    // A title or keepsake name with its gender agreements; the name's own {A} (if any) is the Plynling's.
    public static string Plain(string template, EventCast self) =>
        PlynlingEvents.Expand(template, self.Name, self.Gender).Replace("**", "");

    // The public line that follows a story when its event earned a title: one sentence, the name
    // already safe (EventCast), the title expanded and its article lowered. Null for an unknown key.
    public static string? TitleLine(EventCast self, string epithetKey) =>
        StoryCatalog.Epithets.TryGetValue(epithetKey, out var e)
            ? $"🏷️ **{self.Name}** est désormais *{PlynlingCardUi.LowerArticle(Plain(e.Title, self))}*."
            : null;

    private static string Prose(IReadOnlyList<StoryParagraph>? prose, EventContext? ctx) =>
        prose is null ? "" : ctx is null ? StoryCompiler.Plain(prose) : StoryText.Render(prose, ctx);

    // An option's label as shown: expanded ({B} → the other's name), without the names' bold — it sits
    // inside bold itself, on a button or in the story.
    public static string Label(EventOption option, Func<string, string> expand) => expand(option.Label).Replace("**", "");

    // The event's text expander, shared by the story and the choice card: names, agreements, and for
    // a trait reveal {T} — the new traits stored on the instance ("key,key"), named in its gender.
    public static Func<string, string> Expander(PlynlingEventInstance inst, EventDef def, EventCast self, EventCast? target)
    {
        var traitText = inst.GainedTraitKey is { } keys && IsReveal(def)
            ? string.Join(" et ", keys.Split(',').Select(PlynlingTraits.ByKey).OfType<TraitInfo>().Select(t => t.Name(self.Gender)))
            : null;
        return t => PlynlingEvents.Expand(t, self.Name, self.Gender, target?.Name ?? "quelqu'un", target?.Gender ?? PlynlingGender.Male, traitText);
    }

    private static bool IsReveal(EventDef def) => def.Trigger is OnAction.Adopted or OnAction.BecameTeen or OnAction.BecameAdult;

    // The outcome and what it changed: growth, a new bond if the band moved, stress, modifiers, a
    // coping trait — and for a social event, whether the answer is still awaited or came too late.
    private static string OutcomeText(PlynlingEventInstance inst, EventCast self, EventCast? target, ReplyState reply, EventContext? ctx)
    {
        if (PlynlingEvents.ByKey(inst.EventKey) is not { } def
            || def.Options.FirstOrDefault(o => o.Key == inst.OptionKey) is not { } option)
            return $"**{self.Name}** a vécu une petite aventure.";
        return OutcomeLines(inst, def, option, self, target, reply, ctx);
    }

    // The outcome for a def and option already resolved (public so a hand-built def can be checked).
    public static string OutcomeLines(PlynlingEventInstance inst, EventDef def, EventOption option, EventCast self, EventCast? target,
        ReplyState reply, EventContext? ctx)
    {
        var X = Expander(inst, def, self, target);
        var success = inst.ChallengeSucceeded ?? true;
        var lines = new List<string> { X(OutcomeProse(def, option, success, ctx)) };
        // A lesson: a few days' practice, and rarely the permanent +1. GrewForGood is null on events
        // resolved while every lesson was permanent — told as they happened.
        foreach (var g in (success ? option.OnSuccess : option.OnFailure).OfType<GrowStat>())
        {
            if (inst.GrewForGood is null)
            {
                lines.Add($"-# {PlynlingStats.Emoji(g.Stat)} {PlynlingStats.Name(g.Stat)} +{g.Amount} pour **{self.Name}**");
                continue;
            }
            var practice = PlynlingModifiers.Practice(g.Stat);
            lines.Add($"-# {practice.Emoji} **{practice.Name(self.Gender)}** pour {(int)practice.Duration.TotalDays} jours " +
                      $"({PlynlingStats.Emoji(g.Stat)} {PlynlingStats.Name(g.Stat)} +{practice.Stats[g.Stat]})");
            if (inst.GrewForGood == true)
                lines.Add($"-# 📈 Et ça reste : **{PlynlingStats.Name(g.Stat)} +{g.Amount}** pour de bon, pour **{self.Name}** !");
        }
        if (target is not null && inst.BondBefore is { } before && inst.BondAfter is { } after && before != after)
            lines.Add(PlynlingBonds.ChangeLine(after, self.Name, self.Gender, target.Name, target.Gender));
        if (inst.StressDelta is { } ds && ds != 0)
            lines.Add(ds > 0 ? $"-# 😣 Stress +{ds} pour **{self.Name}**" : $"-# 🌿 Stress −{-ds} pour **{self.Name}**");
        // Only what actually applied: alone, a negative modifier was skipped (outside a break).
        foreach (var m in (success ? option.OnSuccess : option.OnFailure).OfType<ApplyModifier>())
            if (PlynlingModifiers.ByKey(m.Key) is { } mod && PlynlingEventEngine.Applies(m, def, inst.DecidedAlone))
                lines.Add($"-# {mod.Emoji} **{mod.Name(self.Gender)}** pour {(int)mod.Duration.TotalDays} jour{(mod.Duration.TotalDays >= 2 ? "s" : "")}");
        // Rewards: cailloux and items go to the owner (never the mascot's — it has no player), needs to it.
        var paid = self.OwnerId != PlynlingMascot.OwnerId;
        foreach (var effect in success ? option.OnSuccess : option.OnFailure)
            switch (effect)
            {
                case GiveCailloux c when paid:
                    lines.Add($"-# 🪙 **+{c.Amount} cailloux** dans la bourse");
                    break;
                case GiveItem gi when paid && ItemCatalog.ByKey(gi.ItemKey) is { } item:
                    lines.Add($"-# {item.Emoji} **{ItemCatalog.ClearName(item)}** rejoint l'inventaire");
                    break;
                case LiftNeed n:
                    lines.Add(n.Need switch
                    {
                        Need.Hunger => $"-# 🍯 Le ventre de **{self.Name}** est plus plein",
                        Need.Happiness => $"-# 😊 Le moral de **{self.Name}** remonte",
                        _ => $"-# 🫧 **{self.Name}** est plus propre",
                    });
                    break;
            }
        // A break's coping trait. (In a trait reveal the same field holds the {T} traits, already told.)
        var effects = (success ? option.OnSuccess : option.OnFailure).ToList();
        // A swap that really happened: the mascot, or one that no longer held the old trait, swaps silently.
        var swaps = effects.OfType<SwapTrait>().Any(s => inst.GainedTraitKey == s.To);
        if (!IsReveal(def) && !swaps && inst.GainedTraitKey is { } gained && PlynlingTraits.ByKey(gained) is { } trait)
            lines.Add($"-# {trait.Emoji} Nouveau trait : **{trait.Name(self.Gender)}**");
        // A story's marks and trait swap: only what applied (a keepsake that hurts is skipped alone).
        foreach (var effect in effects.Where(e => PlynlingEventEngine.Applies(e, def, inst.DecidedAlone)))
            switch (effect)
            {
                case GiveMark { Kind: MarkKind.Epithet } m when StoryCatalog.Epithets.TryGetValue(m.Key, out var e):
                    lines.Add($"-# 🏷️ **{self.Name}** est désormais *{PlynlingCardUi.LowerArticle(Plain(e.Title, self))}*");
                    break;
                case GiveMark { Kind: MarkKind.Keepsake } m when StoryCatalog.Keepsakes.TryGetValue(m.Key, out var k):
                    lines.Add($"-# 🎁 **{Plain(k.Name, self)}** — gardé");
                    break;
                case SwapTrait s when inst.GainedTraitKey == s.To && PlynlingTraits.ByKey(s.From) is { } from && PlynlingTraits.ByKey(s.To) is { } to:
                    lines.Add($"-# {to.Emoji} {from.Name(self.Gender)} → **{to.Name(self.Gender)}**");
                    break;
            }
        if (effects.OfType<AskTarget>().Any() && target is not null)
            switch (reply)
            {
                case ReplyState.Waiting:
                    lines.Add($"-# ⏳ **{self.Name}** attend la réponse de **{target.Name}**.");
                    break;
                case ReplyState.Lost:
                    lines.Add($"-# ✉️ La réponse de **{target.Name}** n'est jamais arrivée.");
                    break;
            }
        if (effects.OfType<Couple>().Any() && inst.BondAfter != PlynlingBond.Lovers && target is not null)
            lines.Add("-# Hélas, trop tard : l'un des deux a déjà quelqu'un.");
        return string.Join("\n", lines);
    }
}
