using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

public class EmoteStatsModule : InteractionModuleBase<SocketInteractionContext>
{
    private const int PageSize = 20;

    private readonly EmoteStatsService _stats;

    public EmoteStatsModule(EmoteStatsService stats)
    {
        _stats = stats;
    }

    [SlashCommand("emotestats", "Classement des emotes les plus utilisées du serveur")]
    public async Task EmoteStatsAsync()
    {
        await DeferAsync();

        if (await _stats.GetCountAsync(Context.Guild.Id) == 0)
        {
            await FollowupAsync("Aucune emote comptabilisée pour le moment. (˶ᵔ ᵕ ᵔ˶)");
            return;
        }

        var (embed, components) = await BuildPageAsync(0);
        await FollowupAsync(embed: embed, components: components);
    }

    [ComponentInteraction("emotestats:page:*", ignoreGroupNames: true)]
    public async Task OnPageAsync(string pageStr)
    {
        int.TryParse(pageStr, out var page);
        var (embed, components) = await BuildPageAsync(page);

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(m =>
        {
            m.Embed = embed;
            m.Components = components;
        });
    }

    // Builds the embed + navigation buttons for a given page (clamped to range).
    private async Task<(Embed, MessageComponent)> BuildPageAsync(int page)
    {
        var total = await _stats.GetCountAsync(Context.Guild.Id);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 0, totalPages - 1);

        var rows = await _stats.GetPageAsync(Context.Guild.Id, page * PageSize, PageSize);

        var lines = rows.Select((s, i) =>
        {
            var rank = page * PageSize + i + 1;
            var markup = s.EmoteId != 0
                ? (s.IsAnimated ? $"<a:{s.Name}:{s.EmoteId}>" : $"<:{s.Name}:{s.EmoteId}>")
                : s.Unicode;
            var totalUses = s.WrittenCount + s.ReactedCount;
            return $"**{rank}.** {markup} — **{totalUses}** ({s.WrittenCount} écrites, {s.ReactedCount} réactions)";
        });

        var embed = new EmbedBuilder()
            .WithTitle("Emotes les plus utilisées")
            .WithDescription(string.Join("\n", lines))
            .WithColor(Color.Gold)
            .WithFooter($"Page {page + 1}/{totalPages}")
            .Build();

        var components = new ComponentBuilder()
            .WithButton("◀", $"emotestats:page:{page - 1}", ButtonStyle.Secondary, disabled: page == 0)
            .WithButton("▶", $"emotestats:page:{page + 1}", ButtonStyle.Secondary, disabled: page >= totalPages - 1)
            .Build();

        return (embed, components);
    }
}
