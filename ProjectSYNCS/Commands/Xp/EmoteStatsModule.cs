using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// Guild-only: this module reads Context.Guild, which is null in a DM, and
// config.yaml ships register_globally: true (a global command is DM-enabled by
// default). Without this it is reachable somewhere it can only throw.
[CommandContextType(InteractionContextType.Guild)]
public class EmoteStatsModule : InteractionModuleBase<SocketInteractionContext>
{
    private const int PageSize = 20;

    // The view the command opens on. Recent activity is what people actually want
    // to see; all-time barely moves once a server has been running a while.
    private const StatsPeriod DefaultPeriod = StatsPeriod.Month;

    private readonly EmoteStatsService _stats;

    public EmoteStatsModule(EmoteStatsService stats)
    {
        _stats = stats;
    }

    [SlashCommand("emotestats", "Classement des emotes les plus utilisées du serveur")]
    public async Task EmoteStatsAsync()
    {
        await DeferAsync();

        var (embed, components) = await BuildPageAsync(DefaultPeriod, 0);
        await FollowupAsync(embed: embed, components: components);
    }

    // Both the page arrows and the period buttons come back here: the custom-id
    // carries the whole view state, since a component handler gets no memory of what
    // was on screen.
    //
    // Two verbs, one behaviour. They cannot share one: the active period's filter
    // button is `{prefix}:{period}:0`, and so is a "◀" pointing at page 0, so from
    // page 2 onwards the message would carry the same custom-id twice and Discord
    // rejects the whole thing with COMPONENT_CUSTOM_ID_DUPLICATED — disabled or not.
    [ComponentInteraction("emotestats:view:*:*", ignoreGroupNames: true)]
    public Task OnPageAsync(string periodStr, string pageStr) => ShowAsync(periodStr, pageStr);

    [ComponentInteraction("emotestats:win:*:*", ignoreGroupNames: true)]
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
        var ranking = await _stats.GetRankingAsync(Context.Guild.Id, period);

        var totalPages = Math.Max(1, (int)Math.Ceiling(ranking.Count / (double)PageSize));
        page = Math.Clamp(page, 0, totalPages - 1);

        var rows = ranking.Skip(page * PageSize).Take(PageSize).ToList();

        var description = rows.Count == 0
            ? EmptyLine(period)
            : string.Join("\n", rows.Select((t, i) =>
            {
                var rank = page * PageSize + i + 1;
                return $"**{rank}.** {t.Markup} — **{t.Total}** ({t.Written} écrites, {t.Reacted} réactions)";
            }));

        var embed = new EmbedBuilder()
            .WithTitle($"Emotes les plus utilisées — {StatsPeriodUi.Label(period)}")
            .WithDescription(description)
            .WithColor(Color.Gold)
            .WithFooter($"Page {page + 1}/{totalPages} — {ranking.Count} emote(s)")
            .Build();

        // Row 0: the filters. Row 1: paging within the current filter, which the id
        // has to carry too.
        var builder = new ComponentBuilder()
            .AddFilterRow("emotestats:win", period)
            .WithButton("◀", $"emotestats:view:{period}:{page - 1}", ButtonStyle.Secondary,
                disabled: page == 0, row: 1)
            .WithButton("▶", $"emotestats:view:{period}:{page + 1}", ButtonStyle.Secondary,
                disabled: page >= totalPages - 1, row: 1);

        return (embed, builder.Build());
    }

    // The month and week views only see data recorded since daily buckets existed,
    // so an empty board is normal at first rather than a sign nothing was counted.
    private static string EmptyLine(StatsPeriod period) => period == StatsPeriod.AllTime
        ? "Aucune emote comptabilisée pour le moment. (˶ᵔ ᵕ ᵔ˶)"
        : "Rien sur cette période. Utilisez des emotes, bande de fainéants ദ്ദി◝ ⩊ ◜.ᐟ";
}
