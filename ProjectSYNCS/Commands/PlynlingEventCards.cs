using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Commands;

// The event's two cards: the private choice and the public story. Static and Context-free, so the
// harness measures them; every button carries its own verb (Discord rejects a duplicated id).
public static class PlynlingEventCards
{
    public static string PickId(int instanceId, string optionKey) => $"plev:pick:{instanceId}:{optionKey}";
    public static string StoryPrevId(int instanceId, int page) => $"evs:prev:{instanceId}:{page}";
    public static string StoryNextId(int instanceId, int page) => $"evs:next:{instanceId}:{page}";
    public static string StoryFirstId(int instanceId, int page) => $"evs:first:{instanceId}:{page}";
    public static string StoryLastId(int instanceId, int page) => $"evs:last:{instanceId}:{page}";

    // The choice, ephemeral: the scene, one line per option it can see (what it needs, its odds), and
    // one button each. Hidden options are simply absent.
    public static MessageComponent BuildChoice(PlynlingEventInstance inst, EventDef def, EventContext ctx, EventCast self, EventCast? target)
    {
        string X(string t) => PlynlingEvents.Expand(t, self.Name, self.Gender, target?.Name ?? "quelqu'un", target?.Gender ?? PlynlingGender.Male);
        var options = def.Options.Where(o => PlynlingEventEngine.Visible(o, ctx)).ToList();
        var lines = options.Select(o => $"**{o.Label}**{Details(o, ctx, self.Gender)}");
        var row = new ActionRowBuilder();
        foreach (var o in options)
            row.WithButton(o.Label, PickId(inst.Id, o.Key), ButtonStyle.Primary, ButtonEmoji(o));
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

    private static string Details(EventOption o, EventContext ctx, PlynlingGender g)
    {
        var parts = new List<string>();
        if (o.Gate is TraitGate tg && PlynlingTraits.ByKey(tg.TraitKey) is { } trait) parts.Add($"{trait.Emoji} {trait.Name(g)}");
        if (o.Gate is StatGate sg) parts.Add($"{PlynlingStats.Emoji(sg.Stat)} {PlynlingStats.Name(sg.Stat)} {sg.AtLeast}+");
        if (o.Challenge is { } c) parts.Add($"🎲 {PlynlingStats.Name(c.Stat)} : {PlynlingEventEngine.Chance(c, ctx)} %");
        // What choosing it against its nature costs, after its traits' multipliers, and which traits object.
        var cost = PlynlingStress.Scaled(PlynlingEventEngine.StressCost(o, ctx), ctx.Traits);
        if (cost > 0)
            parts.Add($"😣 +{cost} stress ({string.Join(", ", o.StressCosts.Keys.Where(ctx.Has).Select(k => PlynlingTraits.ByKey(k)!.Name(g)))})");
        return parts.Count == 0 ? "" : " · " + string.Join(" · ", parts);
    }

    private static IEmote? ButtonEmoji(EventOption o) =>
        o.Gate is TraitGate tg && PlynlingTraits.ByKey(tg.TraitKey) is { } t ? new Emoji(t.Emoji)
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
        if (story.Pages.Count > 1)
        {
            var row = new ActionRowBuilder()
                .WithButton("◀", StoryPrevId(story.InstanceId, page), ButtonStyle.Secondary, disabled: page == 0);
            if (page == story.Pages.Count - 1)
                row.WithButton("↺ Début", StoryFirstId(story.InstanceId, page), ButtonStyle.Secondary);
            else
                row.WithButton("▶", StoryNextId(story.InstanceId, page), ButtonStyle.Secondary)
                   .WithButton("⏭ Fin", StoryLastId(story.InstanceId, page), ButtonStyle.Secondary);
            builder.AddComponent(row);
        }
        return builder.Build();
    }
}
