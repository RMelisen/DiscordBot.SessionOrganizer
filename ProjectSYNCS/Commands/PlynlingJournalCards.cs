using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Commands;

// /plynling journal — one Components V2 card: who it is, what it has done, its badges and its
// latest moments. Static and Context-free, so it is checkable without a gateway. Sent and
// redrawn with AllowedMentions.None: a moment may name someone (a friend who fed it).
public static class PlynlingJournalCards
{
    public const int MomentsPerPage = 6;

    public static int Pages(int moments) => Math.Max(1, (moments + MomentsPerPage - 1) / MomentsPerPage);

    /// <summary>
    /// The card's whole text, split out so it can be checked as a string. Moments come newest
    /// first; a page past the end shows the last one.
    /// </summary>
    public static string JournalText(Plynling p, IReadOnlyList<PlynlingBadge> badges,
        IReadOnlyList<PlynlingJournalEntry> moments, int page, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(p.Species);
        var name = PlynlingCardUi.SafeName(p.Name);
        var g = p.Gender;
        var age = LevelCardUi.Duration((long)PlynlingLife.Age(p, now).TotalMinutes);

        var header = $"## 📖 Journal de {name}\n" +
                     $"{g.Symbol()} {info.Name} · {PlynlingCardUi.StageLabel(PlynlingLife.Stage(p, now), g)} · " +
                     $"{g.Agree("âgé", "âgée")} de {age}" + (p.DiedAt is null ? "" : $" · 🪦 {g.Agree("mort", "morte")}") + "\n" +
                     $"🍄 {Plural(p.Meals, "repas", "repas")} · 🤲 {Plural(p.Pets, "caresse", "caresses")} · " +
                     $"🎲 {Plural(p.Plays, "partie", "parties")} ({Plural(p.PlaysWon, "gagnée", "gagnées")}) · " +
                     $"🏡 {Plural(p.Visits, "visite", "visites")}";

        // In catalog order, whatever order they were earned in.
        var earned = badges.Select(b => b.Key).ToHashSet();
        var owned = PlynlingBadges.All.Where(b => earned.Contains(b.Key)).ToList();
        var badgeText = $"**Badges** · {owned.Count}/{PlynlingBadges.All.Count}\n" + (owned.Count == 0
            ? "*Aucun badge pour l'instant.*"
            : string.Join(" · ", owned.Select(b => $"{b.Emoji} {b.Name(g)}")));

        var pages = Pages(moments.Count);
        page = Math.Clamp(page, 0, pages - 1);
        var shown = moments.Skip(page * MomentsPerPage).Take(MomentsPerPage)
            .Select(m => $"<t:{m.At.ToUnixTimeSeconds()}:d> · {PlynlingJournalUi.Line(m.Kind, m.Detail, name, g)}")
            .ToList();
        var momentText = "**Moments**\n" + (shown.Count == 0 ? "*Rien encore.*" : string.Join("\n", shown)) +
                         $"\n-# Page {page + 1}/{pages}";

        return header + "\n\n" + badgeText + "\n\n" + momentText;
    }

    public static MessageComponent BuildJournal(Plynling p, IReadOnlyList<PlynlingBadge> badges,
        IReadOnlyList<PlynlingJournalEntry> moments, int page, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(p.Species);
        var pages = Pages(moments.Count);
        page = Math.Clamp(page, 0, pages - 1);
        var picture = p.DiedAt is null
            ? PlynlingArt.Sprite(p.Species, PlynlingLife.Stage(p, now), PlynlingLife.Mood(p, now))
            : PlynlingArt.Memorial(p.Species, PlynlingCatalog.MemorialTier(PlynlingLife.Age(p, now)));

        return new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .WithAccentColor(new Color(info.Accent))
                .AddComponent(new SectionBuilder()
                    .WithAccessory(new ThumbnailBuilder().WithMedia(new UnfurledMediaItemProperties(picture)).WithDescription(info.Name))
                    .AddComponent(new TextDisplayBuilder(JournalText(p, badges, moments, page, now)))))
            // Two verbs: with one, a disabled ◀ on page 0 and a ▶ elsewhere could share an id.
            .AddComponent(new ActionRowBuilder()
                .WithButton("◀", $"plyn:jprev:{p.Id}:{Math.Max(0, page - 1)}", ButtonStyle.Secondary, disabled: page == 0)
                .WithButton("▶", $"plyn:jnext:{p.Id}:{Math.Min(pages - 1, page + 1)}", ButtonStyle.Secondary, disabled: page >= pages - 1))
            .Build();
    }

    private static string Plural(long n, string one, string many) => $"{n} {(n > 1 ? many : one)}";
}
