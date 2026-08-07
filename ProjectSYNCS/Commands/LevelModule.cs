using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The XP leaderboard. Same paged-embed shape as EmoteStatsModule/BotFeedbackModule,
// minus the period filter row — leveling is a single ever-growing ranking, not
// something with a meaningful "this week" view the way an emote count has.
//
// No separate "profile card" render — every ranking feature here is one paged list,
// and a card would be a whole new embed shape for three numbers the list already
// shows. `/level`'s optional `user` param only decides which page the list opens on.
//
// `/leaderboard` is a thin second entry point onto the exact same view: it always
// opens at page 0 (the actual top), where `/level` jumps to whichever page the
// caller (or the queried user) ranks on. Same embed, same ◀/▶ custom-id — paging
// doesn't know or care which command opened the message.
public class LevelModule : InteractionModuleBase<SocketInteractionContext>
{
    private const int PageSize = 20;

    private readonly XpService _xp;

    public LevelModule(XpService xp)
    {
        _xp = xp;
    }

    [SlashCommand("level", "Ton niveau, ton XP, et le classement du serveur")]
    public async Task LevelAsync(
        [Summary("user", "Vérifier le niveau de quelqu'un d'autre (par défaut : toi)")] IUser? user = null)
    {
        await DeferAsync();

        var target = user ?? Context.User;
        var targetRank = await _xp.GetRankAsync(Context.Guild.Id, target.Id);
        var page = targetRank is null ? 0 : (targetRank.Value.Rank - 1) / PageSize;

        var (embed, components) = await BuildPageAsync(page, highlight: target.Id);
        await FollowupAsync(embed: embed, components: components);
    }

    [SlashCommand("leaderboard", "Le top du classement des niveaux")]
    public async Task LeaderboardAsync()
    {
        await DeferAsync();

        var (embed, components) = await BuildPageAsync(0, highlight: null);
        await FollowupAsync(embed: embed, components: components);
    }

    // Both the page arrows come back here: the custom-id carries the page, since a
    // component handler gets no memory of what was on screen. The highlight marker
    // only survives the initial /level call — paging loses it, the same class of
    // accepted simplification EmoteStatsModule/BotFeedbackModule already have around
    // filter/paging state.
    [ComponentInteraction("level:view:*", ignoreGroupNames: true)]
    public async Task OnViewAsync(string pageStr)
    {
        int.TryParse(pageStr, out var page);

        var (embed, components) = await BuildPageAsync(page, highlight: null);

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(m =>
        {
            m.Embed = embed;
            m.Components = components;
        });
    }

    private async Task<(Embed, MessageComponent)> BuildPageAsync(int page, ulong? highlight)
    {
        var ranking = await _xp.GetRankingAsync(Context.Guild.Id);

        var totalPages = Math.Max(1, (int)Math.Ceiling(ranking.Count / (double)PageSize));
        page = Math.Clamp(page, 0, totalPages - 1);

        var rows = ranking.Skip(page * PageSize).Take(PageSize).ToList();

        var description = rows.Count == 0
            ? "Personne n'a encore gagné d'XP. Parlez, réagissez, faites du bruit ദ്ദി◝ ⩊ ◜.ᐟ"
            : string.Join("\n", rows.Select((t, i) =>
            {
                var rank = page * PageSize + i + 1;
                var marker = t.UserId == highlight ? "→ " : "";
                return $"{marker}**{rank}.** <@{t.UserId}> — niveau **{t.Level}** ({t.TotalXp} XP)";
            }));

        // The *current viewer's* own rank, not necessarily the queried target's —
        // this does double duty for free: on the initial /level call it's the
        // invoker's rank, and if someone else later clicks ◀/▶ on this same public
        // message, Context.User on that component interaction is them, so the footer
        // correctly shows their rank too, with no extra state threaded through the
        // custom-id.
        var viewerRank = await _xp.GetRankAsync(Context.Guild.Id, Context.User.Id);
        var footer = viewerRank is { } r
            ? $"Page {page + 1}/{totalPages} — toi : niveau {r.Level}, rang #{r.Rank} ({r.TotalXp} XP)"
            : $"Page {page + 1}/{totalPages}";

        var embed = new EmbedBuilder()
            .WithTitle("Classement des niveaux")
            .WithDescription(description)
            .WithColor(Color.Purple)
            .WithFooter(footer)
            .Build();

        var components = new ComponentBuilder()
            .WithButton("◀", $"level:view:{page - 1}", ButtonStyle.Secondary, disabled: page == 0)
            .WithButton("▶", $"level:view:{page + 1}", ButtonStyle.Secondary, disabled: page >= totalPages - 1)
            .Build();

        return (embed, components);
    }
}
