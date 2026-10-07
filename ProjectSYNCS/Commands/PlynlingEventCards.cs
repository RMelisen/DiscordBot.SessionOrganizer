using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Commands;

// The event's two cards: the private choice and the public story. Static and Context-free, so the
// harness measures them; every button carries its own verb (Discord rejects a duplicated id).
public static class PlynlingEventCards
{
    public static string PickId(int instanceId, string optionKey) => $"plev:pick:{instanceId}:{optionKey}";

    // The choice, ephemeral: the scene, one line per option it can see (what it needs, its odds), and
    // one button each. Hidden options are simply absent.
    public static MessageComponent BuildChoice(PlynlingEventInstance inst, EventDef def, EventContext ctx, EventCast self, EventCast? target)
    {
        var X = PlynlingEventStory.Expander(inst, def, self, target);
        var options = def.Options.Where(o => PlynlingEventEngine.Visible(o, ctx)).ToList();
        // Labels may name the other ({B}): expanded, plain (they sit in bold or on a button), and a
        // button's cut to Discord's 80 characters — a long name could pass it.
        var lines = options.Select(o => $"**{PlynlingEventStory.Label(o, X)}**{Details(o, ctx, self.Gender, target)}");
        var row = new ActionRowBuilder();
        foreach (var o in options)
            row.WithButton(Clip(PlynlingEventStory.Label(o, X), 80), PickId(inst.Id, o.Key), ButtonStyle.Primary, ButtonEmoji(o));
        return new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .WithAccentColor(new Color(PlynlingCatalog.Info(self.Species).Accent))
                .AddComponent(new SectionBuilder()
                    .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(
                        PlynlingArt.Sprite(self.Species, self.Stage, PlynlingMood.Content))))
                    .AddComponent(new TextDisplayBuilder($"## ✨ {def.Title}\n{X(def.Scene)}")))
                .AddComponent(new SeparatorBuilder())
                .AddComponent(new TextDisplayBuilder(string.Join("\n", lines) +
                    $"\n-# Sans choix de ta part, **{self.Name}** décidera {self.Gender.Agree("seul", "seule")} <t:{inst.ExpiresAt.ToUnixTimeSeconds()}:R>.")))
            .AddComponent(row)
            .Build();
    }

    private static string Clip(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";

    private static string Details(EventOption o, EventContext ctx, PlynlingGender g, EventCast? target)
    {
        var parts = new List<string>();
        if (o.Gate is TraitGate tg && PlynlingTraits.ByKey(tg.TraitKey) is { } trait) parts.Add($"{trait.Emoji} {trait.Name(g)}");
        if (o.Gate is StatGate sg) parts.Add($"{PlynlingStats.Emoji(sg.Stat)} {PlynlingStats.Name(sg.Stat)} {sg.AtLeast}+");
        if (o.Challenge is { } c)
            parts.Add($"🎲 {PlynlingStats.Name(c.Stat)} : {PlynlingEventEngine.Chance(c, ctx)} %{(c.VsTarget && target is not null ? $" contre **{target.Name}**" : "")}");
        // What choosing it against its nature costs, after its traits' multipliers, and which traits object.
        var cost = PlynlingStress.Scaled(PlynlingEventEngine.StressCost(o, ctx), ctx.Traits);
        if (cost > 0)
            parts.Add($"😣 +{cost} stress ({string.Join(", ", o.StressCosts.Keys.Where(ctx.Has).Select(k => PlynlingTraits.ByKey(k)!.Name(g)))})");
        return parts.Count == 0 ? "" : " · " + string.Join(" · ", parts);
    }

    private static IEmote? ButtonEmoji(EventOption o) =>
        o.Gate is TraitGate tg && PlynlingTraits.ByKey(tg.TraitKey) is { } t ? EmoteMarkup.Parse(t.Emoji)
        : o.Challenge is not null ? new Emoji("🎲")
        : null;

    // After a pick, in place of the choice: the outcome, where the story is told, and the next
    // event if one waits.
    public static MessageComponent BuildResult(string outcome, int pendingLeft, int plynlingId)
    {
        var builder = new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .AddComponent(new TextDisplayBuilder($"{outcome}\n-# L'histoire est racontée dans le salon du jeu.")));
        if (pendingLeft > 0)
            builder.AddComponent(new ActionRowBuilder()
                .WithButton($"Événement suivant ({pendingLeft})", $"plyn:events:{plynlingId}", ButtonStyle.Primary, new Emoji("✨")));
        return builder.Build();
    }

    // One page of the public story: both Plynlings side by side for a social event, else its
    // picture; the page; « 2/4 »; ◀ ▶, « ⏭ Fin » before the last page, « ↺ Début » on it.
    public static MessageComponent BuildStory(EventStory story, int page)
    {
        page = Math.Clamp(page, 0, story.Pages.Count - 1);
        var gallery = new MediaGalleryBuilder()
            .AddItem(PlynlingArt.VisitSprite(story.Self.Species, story.Self.Stage, PlynlingMood.Content), story.Self.Name, false);
        if (story.Target is { } target)
            gallery.AddItem(PlynlingArt.VisitSprite(target.Species, target.Stage, PlynlingMood.Content), target.Name, false);
        var container = new ContainerBuilder()
            .WithAccentColor(new Color(story.Accent))
            .AddComponent(new TextDisplayBuilder($"## {story.Heading}"))
            .AddComponent(gallery)
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder($"{story.Pages[page]}\n-# {page + 1}/{story.Pages.Count}"));

        var builder = new ComponentBuilderV2().AddComponent(container);
        // evs:prev|next|first|last:{instance}:{page} — the verbs PlynlingEventHandler binds.
        if (story.Pages.Count > 1)
            builder.AddComponent(StoryPager.Row(page, story.Pages.Count, verb => $"evs:{verb}:{story.InstanceId}:{page}"));
        return builder.Build();
    }
}
