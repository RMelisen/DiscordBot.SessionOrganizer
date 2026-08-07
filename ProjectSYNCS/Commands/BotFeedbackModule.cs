using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The "good bot" leaderboard. Deliberately the same shape as EmoteStatsModule —
// paged embed, period filters, ◀ ▶ buttons — because it is the same kind of thing
// and there is no reason for two ranking commands to look different.
// Guild-only: this module reads Context.Guild, which is null in a DM, and
// config.yaml ships register_globally: true (a global command is DM-enabled by
// default). Without this it is reachable somewhere it can only throw.
[CommandContextType(InteractionContextType.Guild)]
public class BotFeedbackModule : InteractionModuleBase<SocketInteractionContext>
{
    private const int PageSize = 20;

    // Unlike /emotestats, this one opens on all-time. Verdicts are far rarer than
    // emotes, so a rolling window is often empty, and the tally reads as a hall of
    // fame rather than as recent activity.
    private const StatsPeriod DefaultPeriod = StatsPeriod.AllTime;

    private readonly BotFeedbackService _feedback;

    public BotFeedbackModule(BotFeedbackService feedback)
    {
        _feedback = feedback;
    }

    [SlashCommand("goodbot", "Qui dit du bien (ou du mal) de SYNCS")]
    public async Task GoodBotAsync()
    {
        await DeferAsync();

        // "Nobody ever has" is a different answer from "nobody did this week", and
        // only the first one deserves the sulking. The second gets the board, so the
        // filters are there to go looking elsewhere.
        if (!await _feedback.HasAnyAsync(Context.Guild.Id))
        {
            await FollowupAsync(
                "Personne ne m'a encore dit *good bot*. Je ne suis pas vexée. Pas du tout. (ᵕ • ᴗ •)");
            return;
        }

        var (embed, components) = await BuildPageAsync(DefaultPeriod, 0);
        await FollowupAsync(embed: embed, components: components);
    }

    // Both the page arrows and the period buttons come back here: the custom-id
    // carries the whole view state, since a component handler gets no memory of what
    // was on screen.
    //
    // Two verbs, one behaviour, for the same reason EmoteStatsModule has two: the
    // active period's filter button and a "◀" pointing at page 0 would otherwise build
    // the identical custom-id, which Discord rejects outright.
    [ComponentInteraction("goodbot:view:*:*", ignoreGroupNames: true)]
    public Task OnPageAsync(string periodStr, string pageStr) => ShowAsync(periodStr, pageStr);

    [ComponentInteraction("goodbot:win:*:*", ignoreGroupNames: true)]
    public Task OnPeriodAsync(string periodStr, string pageStr) => ShowAsync(periodStr, pageStr);

    private async Task ShowAsync(string periodStr, string pageStr)
    {
        if (!Enum.TryParse<StatsPeriod>(periodStr, out var period)) period = DefaultPeriod;
        int.TryParse(pageStr, out var page);

        var (embed, components) = await BuildPageAsync(period, page);

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(m =>
        {
            m.Embed = embed;
            m.Components = components;
        });
    }

    private async Task<(Embed, MessageComponent)> BuildPageAsync(StatsPeriod period, int page)
    {
        var ranking = await _feedback.GetRankingAsync(Context.Guild.Id, period);

        var totalPages = Math.Max(1, (int)Math.Ceiling(ranking.Count / (double)PageSize));
        page = Math.Clamp(page, 0, totalPages - 1);

        var rows = ranking.Skip(page * PageSize).Take(PageSize).ToList();

        var description = rows.Count == 0
            ? EmptyLine(period)
            : string.Join("\n", rows.Select((f, i) =>
            {
                var rank = page * PageSize + i + 1;
                // Mentions render as a name without pinging: the embed carries no
                // AllowedMentions of its own, and a leaderboard should not notify
                // twenty people every time someone opens it.
                return $"**{rank}.** <@{f.UserId}> — 👍 **{f.Good}** · 👎 **{f.Bad}**";
            }));

        var good = ranking.Sum(f => f.Good);
        var bad = ranking.Sum(f => f.Bad);

        var embed = new EmbedBuilder()
            .WithTitle($"Good bot / bad bot — {StatsPeriodUi.Label(period)}")
            .WithDescription(description)
            .WithColor(Color.Teal)
            .WithFooter($"Page {page + 1}/{totalPages} — {good} good bot, {bad} bad bot sur la période")
            .Build();

        // Row 0: the filters. Row 1: paging within the current filter, which the id
        // has to carry too.
        var builder = new ComponentBuilder()
            .AddFilterRow("goodbot:win", period)
            .WithButton("◀", $"goodbot:view:{period}:{page - 1}", ButtonStyle.Secondary,
                disabled: page == 0, row: 1)
            .WithButton("▶", $"goodbot:view:{period}:{page + 1}", ButtonStyle.Secondary,
                disabled: page >= totalPages - 1, row: 1);

        return (embed, builder.Build());
    }

    // The month and week views only see data recorded since daily buckets existed,
    // so an empty board is normal at first rather than a sign nothing was counted.
    private static string EmptyLine(StatsPeriod period) => period == StatsPeriod.AllTime
        ? "Personne ne m'a encore rien dit. Je ne suis pas vexée. Pas du tout. (ᵕ • ᴗ •)"
        : "Personne n'a rien dit sur cette période. Le silence, ça aussi c'est un avis. (˶ᵕ ᵕ˶)";
}
