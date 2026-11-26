using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Commands;

// /plynling stats — one Plynling's sheet, or a duel of two, one tab at a time (docs/agents/plynling/stats.md).
// Components V2, public, no emoji anywhere (titles, lines, buttons). Budget: the single card is 12 of 40
// (container, a section with its picture and header, the lines, the footer, a row of five tabs); the duel 15
// (the pair's line rides the tab's text, no component of its own). Checks section 74 builds both at their fullest.
//
// Two verbs: `plst:tab:{id}:{tab}` and `plst:duel:{a}:{b}:{tab}`, each card carrying one row of five distinct
// ids. The clicks are handled in PlynlingStatsHandler; this class only draws.
public static class PlynlingStatsCards
{
    public static MessageComponent Build(PlynlingStatSheet s, StatsTab tab)
    {
        var p = s.Plynling;
        var lines = PlynlingStatsUi.Lines(s, tab).Select(l => PlynlingStatsUi.Render(l, s.Partial));
        var container = new ContainerBuilder()
            .WithAccentColor(new Color(PlynlingCatalog.Info(p.Species).Accent))
            .AddComponent(Header(s, $"## {PlynlingCardUi.SafeName(p.Name)}"))
            .AddComponent(new TextDisplayBuilder($"### {PlynlingStatsUi.TabLabel(tab)}\n" + string.Join("\n", lines)));
        if (PlynlingStatsUi.SinceFooter(s, tab) is { } since) container.AddComponent(new TextDisplayBuilder(since));

        return new ComponentBuilderV2()
            .AddComponent(container)
            .AddComponent(Tabs(tab, t => $"plst:tab:{p.Id}:{(int)t}"))
            .Build();
    }

    /// <param name="pair">What the two are to each other (PlynlingService.GetStatPairAsync), null when they never met.</param>
    public static MessageComponent BuildDuel(PlynlingStatSheet a, PlynlingStatSheet b, StatsTab tab, StatPair? pair = null)
    {
        var nameA = PlynlingCardUi.SafeName(a.Plynling.Name);
        var nameB = PlynlingCardUi.SafeName(b.Plynling.Name);
        var lines = PlynlingStatsUi.Duel(a, b, tab);
        var footer = new List<string>();
        if (PlynlingStatsUi.DuelFooter(lines, nameA, nameB) is { } lead) footer.Add(lead);
        if ((a.Partial || b.Partial) && lines.Any(l => l.Label.EndsWith('*'))) footer.Add(PlynlingStatsUi.SinceNote);
        var opening = PlynlingStatsUi.PairLine(pair) is { } together ? together + "\n" : "";

        var container = new ContainerBuilder()
            .WithAccentColor(new Color(PlynlingCatalog.Info(a.Plynling.Species).Accent))
            .AddComponent(Header(a, $"## {nameA}"))
            .AddComponent(Header(b, $"## contre {nameB}"))
            .AddComponent(new TextDisplayBuilder($"{opening}### {PlynlingStatsUi.TabLabel(tab)}\n"
                + string.Join("\n", lines.Select(l => $"{l.Label} — {l.A} · {l.B}"))));
        if (footer.Count > 0) container.AddComponent(new TextDisplayBuilder(string.Join("\n", footer)));

        return new ComponentBuilderV2()
            .AddComponent(container)
            .AddComponent(Tabs(tab, t => $"plst:duel:{a.Plynling.Id}:{b.Plynling.Id}:{(int)t}"))
            .Build();
    }

    // Its picture (alive: as it looks now, dressed; dead: its tombstone) beside its name, species, stage and owner.
    private static SectionBuilder Header(PlynlingStatSheet s, string title)
    {
        var p = s.Plynling;
        var info = PlynlingCatalog.Info(p.Species);
        var picture = p.DiedAt is null
            ? PlynlingArt.SpriteOf(p, s.Now)
            : PlynlingArt.Memorial(p.Species, PlynlingCatalog.MemorialTier(p));
        var stage = PlynlingCardUi.StageLabel(PlynlingLife.Stage(p, s.Now), p.Gender);
        var grave = p.DiedAt is null ? "" : " · au cimetière";
        return new SectionBuilder()
            .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(picture)).WithDescription(info.Name))
            .AddComponent(new TextDisplayBuilder($"{title}\n{info.Name} · {stage} · <@{p.OwnerId}>{grave}"));
    }

    private static ActionRowBuilder Tabs(StatsTab selected, Func<StatsTab, string> id)
    {
        var row = new ActionRowBuilder();
        foreach (var t in Enum.GetValues<StatsTab>())
            row.WithButton(PlynlingStatsUi.TabLabel(t), id(t), t == selected ? ButtonStyle.Primary : ButtonStyle.Secondary);
        return row;
    }
}
