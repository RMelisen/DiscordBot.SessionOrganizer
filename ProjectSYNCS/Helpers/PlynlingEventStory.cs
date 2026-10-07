using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Who an event happened to, as the story shows them. Names are made safe here, once.
public sealed record EventCast(string Name, PlynlingGender Gender, PlynlingSpecies Species, PlynlingStage Stage, ulong OwnerId)
{
    public static EventCast Of(Plynling p, DateTimeOffset now) =>
        new(PlynlingCardUi.SafeName(p.Name), p.Gender, p.Species, PlynlingLife.Stage(p, now), p.OwnerId);
}

public sealed record EventStory(int InstanceId, int PlynlingId, string Heading, IReadOnlyList<string> Pages, EventCast Self, EventCast? Target, uint Accent);

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

    // parentTitle: the event this one follows from (a response, a follow-up). answered: whether the
    // response this one asked for has come — until then its outcome says it waits.
    public static EventStory Build(PlynlingEventInstance inst, EventCast self, EventCast? target, string? parentTitle = null, bool answered = false)
    {
        var accent = PlynlingCatalog.Info(self.Species).Accent;
        if (PlynlingEvents.ByKey(inst.EventKey) is not { } def
            || def.Options.FirstOrDefault(o => o.Key == inst.OptionKey) is not { } option)
            return new EventStory(inst.Id, inst.PlynlingId, "📜 Une petite aventure",
                new[] { $"**{self.Name}** a vécu une petite aventure. Les détails se sont perdus en route." }, self, target, accent);

        var X = Expander(inst, def, self, target);
        var pages = new List<string> { (parentTitle is null ? "" : $"-# Suite de « {parentTitle} »\n") + X(def.Scene) };
        pages.Add(inst.DecidedAlone
            ? X(StableRoll.Pick(AloneLines, inst.Id, 320, 0))
            : $"<@{self.OwnerId}> a tranché pour **{self.Name}** : **{Label(option, X)}**.");
        if (option.Challenge is { } c && inst.ChancePercent is { } chance)
            pages.Add($"🎲 **{PlynlingStats.Name(c.Stat)}**{(c.VsTarget && target is not null ? $" contre **{target.Name}**" : "")} — {chance} % de chances… " +
                      (inst.ChallengeSucceeded == true ? "**réussi !**" : "**raté.**"));
        pages.Add(OutcomeText(inst, self, target, answered));
        return new EventStory(inst.Id, inst.PlynlingId, $"📜 {def.Title}", pages, self, target, accent);
    }

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
    private static string OutcomeText(PlynlingEventInstance inst, EventCast self, EventCast? target, bool answered = false)
    {
        if (PlynlingEvents.ByKey(inst.EventKey) is not { } def
            || def.Options.FirstOrDefault(o => o.Key == inst.OptionKey) is not { } option)
            return $"**{self.Name}** a vécu une petite aventure.";
        var X = Expander(inst, def, self, target);
        var success = inst.ChallengeSucceeded ?? true;
        var lines = new List<string> { X(success ? option.Outcome : option.FailOutcome ?? option.Outcome) };
        foreach (var g in (success ? option.OnSuccess : option.OnFailure).OfType<GrowStat>())
            lines.Add($"-# {PlynlingStats.Emoji(g.Stat)} {PlynlingStats.Name(g.Stat)} +{g.Amount} pour **{self.Name}**");
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
        if (!IsReveal(def) && inst.GainedTraitKey is { } gained && PlynlingTraits.ByKey(gained) is { } trait)
            lines.Add($"-# {trait.Emoji} Nouveau trait : **{trait.Name(self.Gender)}**");
        var effects = (success ? option.OnSuccess : option.OnFailure).ToList();
        if (effects.OfType<AskTarget>().Any() && target is not null && !answered)
            lines.Add($"-# ⏳ **{self.Name}** attend la réponse de **{target.Name}**.");
        if (effects.OfType<Couple>().Any() && inst.BondAfter != PlynlingBond.Lovers && target is not null)
            lines.Add("-# Hélas, trop tard : l'un des deux a déjà quelqu'un.");
        return string.Join("\n", lines);
    }
}
