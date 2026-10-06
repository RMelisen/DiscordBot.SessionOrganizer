using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Who an event happened to, as the story shows them. Names are made safe here, once.
public sealed record EventCast(string Name, PlynlingGender Gender, PlynlingSpecies Species, PlynlingStage Stage, ulong OwnerId)
{
    public static EventCast Of(Plynling p, DateTimeOffset now) =>
        new(PlynlingCardUi.SafeName(p.Name), p.Gender, p.Species, PlynlingLife.Stage(p, now), p.OwnerId);
}

public sealed record EventStory(int InstanceId, string Heading, IReadOnlyList<string> Pages, EventCast Self, EventCast? Target, uint Accent);

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

    public static EventStory Build(PlynlingEventInstance inst, EventCast self, EventCast? target)
    {
        var accent = PlynlingCatalog.Info(self.Species).Accent;
        if (PlynlingEvents.ByKey(inst.EventKey) is not { } def
            || def.Options.FirstOrDefault(o => o.Key == inst.OptionKey) is not { } option)
            return new EventStory(inst.Id, "📜 Une petite aventure",
                new[] { $"**{self.Name}** a vécu une petite aventure. Les détails se sont perdus en route." }, self, target, accent);

        string X(string t) => PlynlingEvents.Expand(t, self.Name, self.Gender, target?.Name ?? "quelqu'un", target?.Gender ?? PlynlingGender.Male);
        var pages = new List<string> { X(def.Scene) };
        pages.Add(inst.DecidedAlone
            ? X(AloneLines[(int)(StableRoll.Unit(inst.Id, 320, 0) * AloneLines.Length) % AloneLines.Length])
            : $"<@{self.OwnerId}> a tranché pour **{self.Name}** : **{option.Label}**.");
        if (option.Challenge is { } c && inst.ChancePercent is { } chance)
            pages.Add($"🎲 **{PlynlingStats.Name(c.Stat)}** — {chance} % de chances… " +
                      (inst.ChallengeSucceeded == true ? "**réussi !**" : "**raté.**"));
        pages.Add(OutcomeText(inst, self, target));
        return new EventStory(inst.Id, $"📜 {def.Title}", pages, self, target, accent);
    }

    // The outcome and what it changed: growth, and a new bond if the band moved.
    public static string OutcomeText(PlynlingEventInstance inst, EventCast self, EventCast? target)
    {
        if (PlynlingEvents.ByKey(inst.EventKey) is not { } def
            || def.Options.FirstOrDefault(o => o.Key == inst.OptionKey) is not { } option)
            return $"**{self.Name}** a vécu une petite aventure.";
        string X(string t) => PlynlingEvents.Expand(t, self.Name, self.Gender, target?.Name ?? "quelqu'un", target?.Gender ?? PlynlingGender.Male);
        var success = inst.ChallengeSucceeded ?? true;
        var lines = new List<string> { X(success ? option.Outcome : option.FailOutcome ?? option.Outcome) };
        foreach (var g in (success ? option.OnSuccess : option.OnFailure).OfType<GrowStat>())
            lines.Add($"-# {PlynlingStats.Emoji(g.Stat)} {PlynlingStats.Name(g.Stat)} +{g.Amount} pour **{self.Name}**");
        if (target is not null && inst.BondBefore is { } before && inst.BondAfter is { } after && before != after)
            lines.Add(PlynlingBonds.ChangeLine(after, self.Name, self.Gender, target.Name, target.Gender));
        return string.Join("\n", lines);
    }
}
