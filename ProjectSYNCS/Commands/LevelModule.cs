using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The XP surfaces, both built with Components V2 rather than embeds, so every person
// shown carries their real avatar. Discord fetches those from its own CDN off the url
// we hand it, so nothing here downloads, decodes or draws an image.
//
// Deliberately NOT the same shape as EmoteStatsModule/BotFeedbackModule, which stay
// paged embeds. Those rank emotes and verdicts — things with no face, no level and no
// podium — so the only thing a shared renderer would save is the page arithmetic.
//
// /level and /leaderboard are two different views now, not one list entered at two
// points: /level is a card about one person, /leaderboard the ranked list. That split
// is why /level no longer jumps to the page someone ranks on (and why the old "→"
// marker, which paging lost anyway, is gone).
//
// A ComponentsV2 message carries NO content and NO embeds — the flag makes the whole
// message components, so this is a replacement for the old embed rather than an
// addition to it. Every send also passes AllowedMentions.None: a TextDisplay is real
// message content, so without it every re-render would ping all ten people on the page.
// Guild-only: every query here is scoped to Context.Guild.Id, which is null in a DM.
// config.yaml ships register_globally: true, and a global command is DM-enabled by
// default, so without this the command is reachable somewhere it can only throw.
[CommandContextType(InteractionContextType.Guild)]
public class LevelModule : InteractionModuleBase<SocketInteractionContext>
{
    // Ten, not twenty. A row with an avatar costs three components (Section +
    // TextDisplay + Thumbnail) against Discord's 40-per-message ceiling, and the
    // container, heading, separators and button row take the rest.
    private const int PageSize = 10;

    // Bots never earn XP (XpTracker skips them outright), so a bot's card would always
    // be an empty one. One fixed line, deliberately not a ResponsePicker pool: the pools
    // exist so repeated *chatter* doesn't repeat, and this is a command refusal.
    private const string BotRefusal =
        "Les bots ne gagnent pas d'XP. Surtout pas ceux-là. Bien essayé (˶˃ ᵕ ˂˶)";

    private readonly XpService _xp;

    public LevelModule(XpService xp)
    {
        _xp = xp;
    }

    [SlashCommand("level", "Ton niveau, ton XP et ta progression")]
    public async Task LevelAsync(
        [Summary("user", "Vérifier le niveau de quelqu'un d'autre (par défaut : toi)")] IUser? user = null)
    {
        await DeferAsync();

        var target = user ?? Context.User;
        if (target.IsBot)
        {
            await FollowupAsync(BotRefusal, allowedMentions: AllowedMentions.None);
            return;
        }

        var rank = await _xp.GetRankAsync(Context.Guild.Id, target.Id);

        await FollowupAsync(
            components: BuildCard(target, rank),
            flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("leaderboard", "Le classement des niveaux du serveur")]
    public async Task LeaderboardAsync()
    {
        await DeferAsync();

        await FollowupAsync(
            components: await BuildPageAsync(0),
            flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None);
    }

    // Both page arrows come back here, and so does the card's "Voir le classement"
    // button — it is just page 0, so it reuses this custom-id rather than inventing a
    // second one. That does mean the button replaces the card in place; re-running
    // /level is the way back.
    [ComponentInteraction("level:view:*", ignoreGroupNames: true)]
    public async Task OnViewAsync(string pageStr)
    {
        int.TryParse(pageStr, out var page);

        var components = await BuildPageAsync(page);
        var component = (SocketMessageComponent)Context.Interaction;

        await component.UpdateAsync(m =>
        {
            m.Components = components;
            // Re-asserted on every edit: the flag is a property of the message, and an
            // update that omitted it would be rejected against a ComponentsV2 message.
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    // ---- /level -------------------------------------------------------------

    private MessageComponent BuildCard(IUser target, XpRank? rank)
    {
        var totalXp = rank?.TotalXp ?? 0;
        var level = rank?.Level ?? 0;

        // Both drawn from the same pair, so the bar can never disagree with the numbers
        // printed beneath it.
        var into = LevelCurve.XpIntoLevel(totalXp);
        var span = LevelCurve.XpForLevel(level);

        var container = new ContainerBuilder()
            .WithAccentColor(Color.Purple)
            .AddComponent(new SectionBuilder()
                .WithAccessory(Avatar(target))
                .AddComponent(new TextDisplayBuilder(
                    LevelCardUi.CardHeading(target.Id, level, rank?.Rank))))
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(
                LevelCardUi.CardProgress(into, span, level + 1, totalXp)));

        return new ComponentBuilderV2()
            .AddComponent(container)
            .AddComponent(new ActionRowBuilder()
                .WithButton("Voir le classement", "level:view:0", ButtonStyle.Secondary))
            .Build();
    }

    // ---- /leaderboard -------------------------------------------------------

    private async Task<MessageComponent> BuildPageAsync(int page)
    {
        var ranking = await _xp.GetRankingAsync(Context.Guild.Id);

        var totalPages = Math.Max(1, (int)Math.Ceiling(ranking.Count / (double)PageSize));
        page = Math.Clamp(page, 0, totalPages - 1);

        var container = new ContainerBuilder()
            .WithAccentColor(Color.Purple)
            .AddComponent(new TextDisplayBuilder("## Classement des niveaux"))
            .AddComponent(new SeparatorBuilder());

        if (ranking.Count == 0)
        {
            container.AddComponent(new TextDisplayBuilder(
                "Personne n'a encore gagné d'XP. Parlez, réagissez, faites du bruit ദ്ദി◝ ⩊ ◜.ᐟ"));
        }
        else
        {
            foreach (var (tally, index) in ranking.Skip(page * PageSize).Take(PageSize).Select((t, i) => (t, i)))
            {
                var rank = page * PageSize + index + 1;

                container.AddComponent(new SectionBuilder()
                    .WithAccessory(Avatar(Context.Guild.GetUser(tally.UserId)))
                    .AddComponent(new TextDisplayBuilder(
                        LevelCardUi.Row(rank, tally.UserId, tally.Level, tally.TotalXp))));
            }
        }

        // The *viewer's* rank, not the original caller's: on a component interaction
        // Context.User is whoever clicked, so this stays correct for everyone reading
        // the same public message, with nothing threaded through the custom-id.
        var viewerRank = await _xp.GetRankAsync(Context.Guild.Id, Context.User.Id);
        container
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(viewerRank is { } r
                ? $"-# Page {page + 1}/{totalPages} · toi : niveau {r.Level}, rang #{r.Rank} ({LevelCardUi.Xp(r.TotalXp)} XP)"
                : $"-# Page {page + 1}/{totalPages}"));

        return new ComponentBuilderV2()
            .AddComponent(container)
            .AddComponent(new ActionRowBuilder()
                .WithButton("◀", $"level:view:{page - 1}", ButtonStyle.Secondary, disabled: page == 0)
                .WithButton("▶", $"level:view:{page + 1}", ButtonStyle.Secondary, disabled: page >= totalPages - 1))
            .Build();
    }

    // A member who has since left the guild resolves to null, so fall back to Discord's
    // own default avatar rather than dropping the thumbnail — a Section whose accessory
    // is missing would render lopsided against its neighbours. Their <@id> still renders
    // as a name, since the client resolves that itself.
    private static ThumbnailBuilder Avatar(IUser? user) =>
        new ThumbnailBuilder()
            .WithMedia(new UnfurledMediaItemProperties(
                user?.GetDisplayAvatarUrl(size: 128) ?? CDN.GetDefaultUserAvatarUrl(0)))
            .WithDescription("Avatar");
}
