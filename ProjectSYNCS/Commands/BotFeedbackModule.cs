using Discord;
using Discord.Interactions;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The "good bot" leaderboard. Deliberately the same shape as EmoteStatsModule —
// paged embed, ◀ ▶ buttons — because it is the same kind of thing and there is no
// reason for two ranking commands to look different.
public class BotFeedbackModule : InteractionModuleBase<SocketInteractionContext>
{
    private const int PageSize = 20;

    private readonly BotFeedbackService _feedback;

    public BotFeedbackModule(BotFeedbackService feedback)
    {
        _feedback = feedback;
    }

    [SlashCommand("goodbot", "Qui dit du bien (ou du mal) de SYNCS")]
    public async Task GoodBotAsync()
    {
        await DeferAsync();

        if (await _feedback.GetCountAsync(Context.Guild.Id) == 0)
        {
            await FollowupAsync(
                "Personne ne m'a encore dit *good bot*. Je ne suis pas vexée. Pas du tout. (ᵕ • ᴗ •)");
            return;
        }

        var (embed, components) = await BuildPageAsync(0);
        await FollowupAsync(embed: embed, components: components);
    }

    [ComponentInteraction("goodbot:page:*", ignoreGroupNames: true)]
    public async Task OnPageAsync(string pageStr)
    {
        int.TryParse(pageStr, out var page);
        var (embed, components) = await BuildPageAsync(page);

        var component = (Discord.WebSocket.SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(m =>
        {
            m.Embed = embed;
            m.Components = components;
        });
    }

    private async Task<(Embed, MessageComponent)> BuildPageAsync(int page)
    {
        var total = await _feedback.GetCountAsync(Context.Guild.Id);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 0, totalPages - 1);

        var rows = await _feedback.GetPageAsync(Context.Guild.Id, page * PageSize, PageSize);
        var (good, bad) = await _feedback.GetTotalsAsync(Context.Guild.Id);

        var lines = rows.Select((f, i) =>
        {
            var rank = page * PageSize + i + 1;
            // Mentions render as a name without pinging: the embed carries no
            // AllowedMentions of its own, and a leaderboard should not notify
            // twenty people every time someone opens it.
            return $"**{rank}.** <@{f.UserId}> — 👍 **{f.GoodCount}** · 👎 **{f.BadCount}**";
        });

        var embed = new EmbedBuilder()
            .WithTitle("Good bot / bad bot")
            .WithDescription(string.Join("\n", lines))
            .WithColor(Color.Teal)
            .WithFooter($"Page {page + 1}/{totalPages} — {good} good bot, {bad} bad bot au total")
            .Build();

        var components = new ComponentBuilder()
            .WithButton("◀", $"goodbot:page:{page - 1}", ButtonStyle.Secondary, disabled: page == 0)
            .WithButton("▶", $"goodbot:page:{page + 1}", ButtonStyle.Secondary, disabled: page >= totalPages - 1)
            .Build();

        return (embed, components);
    }
}
