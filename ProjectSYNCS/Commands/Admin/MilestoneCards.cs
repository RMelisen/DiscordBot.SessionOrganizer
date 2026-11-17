using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

/// <summary>
/// /debug milestones: one channel's message count, the milestones it reached (who, when, a link,
/// announced or not) and the next one, a page at a time. Static and Context-free, so the checks can
/// measure it; the command and <c>MilestoneComponentHandler</c> both render through it.
/// </summary>
internal static class MilestoneCards
{
    // A line runs to ~175 characters with 20-digit ids (the link), so twenty a page stay under the
    // description's 4096 (checks section 49 measures it).
    public const int PageSize = 20;

    /// <summary>How many pages <paramref name="reached"/> milestones take (at least one).</summary>
    public static int PageCount(int reached) => Math.Max(1, (reached + PageSize - 1) / PageSize);

    /// <summary>
    /// The card at <paramref name="page"/>: 0 is the newest milestones, each page further back is
    /// older. Out-of-range pages are clamped.
    /// </summary>
    public static Embed Build(ulong guildId, string channelName, MilestoneView view, int page)
    {
        var total = view.Reached.Count;
        page = Math.Clamp(page, 0, PageCount(total) - 1);
        // view.Reached is oldest first; page 0 ends at the newest.
        var end = total - page * PageSize;
        var start = Math.Max(0, end - PageSize);

        var text = new System.Text.StringBuilder();
        text.Append($"**{ChannelMilestones.Format(view.Count)}** messages");
        text.AppendLine(view.CaughtUp ? "." : " pour l'instant : je lis encore l'historique de ce salon…");
        text.AppendLine();
        text.AppendLine("**Paliers atteints**");
        if (total == 0) text.AppendLine("Aucun pour l'instant.");
        for (var i = start; i < end; i++)
        {
            var m = view.Reached[i];
            text.AppendLine($"• **{ChannelMilestones.Format(m.Milestone)}** — {MentionUtils.MentionUser(m.AuthorId)} · "
                            + $"<t:{m.ReachedAt.ToUnixTimeSeconds()}:D> · "
                            + $"[voir](https://discord.com/channels/{guildId}/{m.ChannelId}/{m.MessageId}) · "
                            + (m.Announced ? "annoncé" : "sans annonce"));
        }
        text.AppendLine();
        text.Append($"Prochain palier : **{ChannelMilestones.Format(view.Next)}**, dans "
                    + $"**{ChannelMilestones.Format(Math.Max(0, view.Next - view.Count))}** messages.");

        var embed = new EmbedBuilder()
            .WithTitle($"Paliers de #{channelName}")
            .WithDescription(text.ToString())
            .WithColor(Color.Purple);
        if (total > PageSize) embed.WithFooter($"Paliers {start + 1} à {end} sur {total}");
        return embed.Build();
    }

    /// <summary>
    /// « Plus anciens » / « Plus récents », or null when everything fits on one page. Each button has
    /// its own verb (Discord refuses two identical custom-ids, disabled ones included) and carries
    /// the page it leads to.
    /// </summary>
    public static MessageComponent? BuildButtons(ulong channelId, int reached, int page)
    {
        var pages = PageCount(reached);
        if (pages == 1) return null;
        page = Math.Clamp(page, 0, pages - 1);
        return new ComponentBuilder()
            .WithButton("◀ Plus anciens", $"milestones:older:{channelId}:{page + 1}", ButtonStyle.Secondary,
                disabled: page >= pages - 1)
            .WithButton("Plus récents ▶", $"milestones:newer:{channelId}:{page - 1}", ButtonStyle.Secondary,
                disabled: page <= 0)
            .Build();
    }
}
