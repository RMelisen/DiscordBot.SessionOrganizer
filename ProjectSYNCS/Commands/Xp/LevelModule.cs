using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
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
public class LevelModule : SyncsModuleBase
{
    // Five. Ten was readable but long, and the view switcher makes it impossible
    // anyway: a row with an avatar costs three components (Section + TextDisplay +
    // Thumbnail) against Discord's 40-per-message ceiling, so ten rows plus a second
    // action row of view buttons comes to 42 and throws inside
    // ComponentBuilderV2.Build(). Five lands at 35 with the six views on two rows (checks
    // sections 52 and 68 measure it). Adding anything to a row, or another element to the
    // container, means checking that sum again.
    private const int PageSize = 5;

    // The view the leaderboard opens on. XP is the original ranking and the one that
    // has history behind it; the other two only started counting when they shipped.
    private const LeaderboardView DefaultView = LeaderboardView.Xp;

    // All-time by default, unlike /emotestats' 30 days: only the totals carry the full
    // history here, and a leveling board is a standing rather than recent activity.
    private const StatsPeriod DefaultPeriod = StatsPeriod.AllTime;

    // Bots never earn XP (XpTracker skips them outright), so a bot's card would always
    // be an empty one. One fixed line, deliberately not a ResponsePicker pool: the pools
    // exist so repeated *chatter* doesn't repeat, and this is a command refusal.
    private const string BotRefusal =
        "Les bots ne gagnent pas d'XP. Surtout pas ceux-là. Bien essayé (˶˃ ᵕ ˂˶)";

    private readonly XpService _xp;
    private readonly InventoryService _inventory;
    private readonly MemberBadgeService _badges;
    private readonly PlynlingService _plynlings;

    public LevelModule(XpService xp, InventoryService inventory, MemberBadgeService badges, PlynlingService plynlings)
    {
        _plynlings = plynlings;
        _xp = xp;
        _inventory = inventory;
        _badges = badges;
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
        var (completions, standings) = await GatherBadgesAsync(target.Id);

        await FollowupAsync(
            components: BuildCard(target, rank, completions, standings),
            flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None);
    }

    // The collections and the evaluated badges of one member of this guild.
    private async Task<(IReadOnlyCollection<Models.CollectionCompletion> Completions, IReadOnlyList<BadgeStanding> Standings)>
        GatherBadgesAsync(ulong userId)
    {
        var completions = await _inventory.GetCompletionsAsync(Context.Guild.Id, userId);
        var facts = await _badges.GetFactsAsync(
            Context.Guild.Id, userId, Context.Guild.GetUser(userId)?.JoinedAt, DateTimeOffset.UtcNow);
        return (completions, MemberBadges.Evaluate(facts, DateTimeOffset.UtcNow));
    }

    // The « Succès » button opens the card member's badges for whoever clicked, ephemeral: the
    // card itself is not edited. Defers first — the reads can outrun Discord's 3 s on the Pi.
    // Its own verbs, apart from the leaderboard's (see above), and each tab its own id.
    [ComponentInteraction("level:badges:*", ignoreGroupNames: true)]
    public async Task OnBadgesAsync(string userId)
    {
        await DeferAsync(ephemeral: true);
        if (!ulong.TryParse(userId, out var id)) return;

        var (completions, standings) = await GatherBadgesAsync(id);
        await FollowupAsync(
            components: MemberBadgeCards.BuildView(id, await DisplayNameAsync(id), BadgeSection.Paliers, completions, standings),
            flags: MessageFlags.ComponentsV2,
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    [ComponentInteraction("level:badgetab:*:*", ignoreGroupNames: true)]
    public async Task OnBadgeTabAsync(string userId, string sectionStr)
    {
        await DeferAsync();
        if (!ulong.TryParse(userId, out var id)) return;
        if (!Enum.TryParse<BadgeSection>(sectionStr, out var section)) section = BadgeSection.Paliers;

        var (completions, standings) = await GatherBadgesAsync(id);
        var view = MemberBadgeCards.BuildView(id, await DisplayNameAsync(id), section, completions, standings);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = view;
            // Re-asserted on every edit, like the leaderboard's.
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    private async Task<string> DisplayNameAsync(ulong id)
    {
        if (Context.Guild.GetUser(id) is { } member) return BotResponses.DisplayNameFor(member);
        try
        {
            return await Context.Client.GetUserAsync(id) is { } user ? BotResponses.DisplayNameFor(user) : "ce membre";
        }
        catch (Exception)
        {
            // A REST failure must not leave the deferred click blank.
            return "ce membre";
        }
    }

    [SlashCommand("leaderboard", "Le classement des niveaux du serveur")]
    public async Task LeaderboardAsync()
    {
        await DeferAsync();

        // Through CardSend: the Plynlings view's rows may carry drawn pictures to attach.
        await Context.Interaction.FollowupCardAsync(await BuildPageAsync(DefaultView, DefaultPeriod, 0));
    }

    // Every button on the board comes back to the same place: the custom-id carries the
    // whole view state, since a component handler gets no memory of what was on screen.
    // Changing either filter resets to page 0 and leaves the other alone — it is a
    // different ranking, so page 3 of the old one means nothing.
    //
    // **One verb per row, and they must stay distinct.** All three rows encode the same
    // triple (view, period, page), so sharing a verb makes ids collide outright: the
    // active view's button and the active period's button are both
    // "current view, current period, page 0", and a "◀" from page 1 is that same string
    // again. Discord rejects any message carrying a duplicated custom-id with
    // COMPONENT_CUSTOM_ID_DUPLICATED, disabled buttons included — this crashed
    // /leaderboard on every single render until the verbs were split.
    //
    // Within a row there is no such risk: each button varies the one segment its row
    // owns. The view segment comes before the period one so the period row can be built
    // by StatsPeriodUi with `level:win:{view}` as its prefix, the same
    // `{prefix}:{period}:0` shape /emotestats and /goodbot use.
    //
    // The card's "Voir le classement" is just "Xp, all-time, page 0" on the paging verb
    // rather than a fourth id. That does mean it replaces the card in place; re-running
    // /level is the way back.
    [ComponentInteraction("level:page:*:*:*", ignoreGroupNames: true)]
    public Task OnPageAsync(string viewStr, string periodStr, string pageStr) =>
        ShowAsync(viewStr, periodStr, pageStr);

    [ComponentInteraction("level:view:*:*:*", ignoreGroupNames: true)]
    public Task OnViewAsync(string viewStr, string periodStr, string pageStr) =>
        ShowAsync(viewStr, periodStr, pageStr);

    [ComponentInteraction("level:win:*:*:*", ignoreGroupNames: true)]
    public Task OnPeriodAsync(string viewStr, string periodStr, string pageStr) =>
        ShowAsync(viewStr, periodStr, pageStr);

    private async Task ShowAsync(string viewStr, string periodStr, string pageStr)
    {
        if (!Enum.TryParse<LeaderboardView>(viewStr, out var view)) view = DefaultView;
        if (!Enum.TryParse<StatsPeriod>(periodStr, out var period)) period = DefaultPeriod;
        int.TryParse(pageStr, out var page);

        // Deferred first: the ranking is read from the database and the Plynlings view may draw
        // up to a page of pictures, which can outrun the 3 s on the Pi. CardSend re-asserts the
        // V2 flag and attaches the drawn pictures (or clears the last page's).
        await DeferAsync();
        await Context.Interaction.ModifyCardAsync(await BuildPageAsync(view, period, page));
    }

    // ---- /level -------------------------------------------------------------

    // The badge line rides along at the bottom — a separator and one TextDisplay, only for
    // someone who has earned something (≈ 11 components of the 40, with the « Succès » button).
    private MessageComponent BuildCard(IUser target, XpRank? rank,
        IReadOnlyCollection<Models.CollectionCompletion> completions, IReadOnlyList<BadgeStanding> standings)
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

        if (MemberBadgeCards.CardLine(completions, standings) is { } badges)
            container
                .AddComponent(new SeparatorBuilder())
                .AddComponent(new TextDisplayBuilder(badges));

        return new ComponentBuilderV2()
            .AddComponent(container)
            .AddComponent(new ActionRowBuilder()
                .WithButton("Voir le classement",
                    $"level:page:{LeaderboardView.Xp}:{StatsPeriod.AllTime}:0", ButtonStyle.Secondary)
                .WithButton("Succès", MemberBadgeCards.OpenId(target.Id), ButtonStyle.Secondary, new Emoji("🏅")))
            .Build();
    }

    // ---- /leaderboard -------------------------------------------------------

    // The Collections view ranks from the inventory's tables rather than XP's, shaped into the
    // same MemberTally so the page, rows and footer stay one path.
    private async Task<List<MemberTally>> RankingAsync(LeaderboardView view, StatsPeriod period) =>
        view == LeaderboardView.Collections
            ? (await _inventory.GetCollectorRankingAsync(Context.Guild.Id, period))
                .Select(t => new MemberTally(t.UserId, 0, 0, 0, t.Completions)).ToList()
            : await _xp.GetRankingAsync(Context.Guild.Id, view, period);

    // The Plynlings view ranks Plynlings rather than people, from PlynlingService, on its own page builder;
    // every other view goes through the member one. Both share Frame.
    private async Task<MessageComponent> BuildPageAsync(LeaderboardView view, StatsPeriod period, int page)
    {
        if (view == LeaderboardView.Plynlings)
        {
            var now = DateTimeOffset.UtcNow;
            return BuildPlynlingPage(await _plynlings.GetScoreRankingAsync(Context.Guild.Id, period, now),
                period, page, Context.User.Id, now);
        }
        return BuildPage(await RankingAsync(view, period), view, period, page, Context.User.Id, id => Context.Guild.GetUser(id));
    }

    // Static and Context-free so the checks can measure the page at its fullest against the
    // 40-component cap. viewerId is whoever is reading (the footer's standing); userOf resolves
    // the avatars.
    public static MessageComponent BuildPage(IReadOnlyList<MemberTally> ranking, LeaderboardView view,
        StatsPeriod period, int page, ulong viewerId, Func<ulong, IUser?> userOf)
    {
        var totalPages = PagesFor(ranking.Count);
        page = Math.Clamp(page, 0, totalPages - 1);
        var rows = ranking.Skip(page * PageSize).Take(PageSize).Select((tally, index) =>
            (Avatar(userOf(tally.UserId)), LevelCardUi.Row(page * PageSize + index + 1, tally.UserId, view, period, tally)));
        return Frame(rows.ToList(), view, period, page, totalPages,
            ranking.Count == 0 ? LevelCardUi.EmptyLine(view, period) : null,
            Footer(ranking, viewerId, view, period, page, totalPages));
    }

    // The Plynlings view: each row shows the Plynling itself (its picture while alive, its tombstone once
    // dead, since the hall of fame keeps them), then its name, its owner and its points. Static and
    // Context-free like BuildPage, so the checks measure it too.
    public static MessageComponent BuildPlynlingPage(IReadOnlyList<PlynlingStanding> ranking, StatsPeriod period,
        int page, ulong viewerId, DateTimeOffset now)
    {
        var totalPages = PagesFor(ranking.Count);
        page = Math.Clamp(page, 0, totalPages - 1);
        var rows = ranking.Skip(page * PageSize).Take(PageSize).Select((standing, index) =>
            (PlynlingPicture(standing.Plynling, now),
             LevelCardUi.PlynlingRow(page * PageSize + index + 1, standing.Plynling, standing.Points)));

        // The viewer's best-placed Plynling, living or not: the list is in rank order.
        var mine = ranking.ToList().FindIndex(s => s.Plynling.OwnerId == viewerId);
        var footer = mine < 0
            ? $"-# Page {page + 1}/{totalPages}"
            : $"-# Page {page + 1}/{totalPages} · ton Plynling : rang #{mine + 1}, {LevelCardUi.Points(ranking[mine].Points)}";
        return Frame(rows.ToList(), LeaderboardView.Plynlings, period, page, totalPages,
            ranking.Count == 0 ? LevelCardUi.EmptyLine(LeaderboardView.Plynlings, period) : null, footer);
    }

    private static int PagesFor(int count) => Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));

    private static ThumbnailBuilder PlynlingPicture(Plynling p, DateTimeOffset now) => new ThumbnailBuilder()
        .WithMedia(new UnfurledMediaItemProperties(p.DiedAt is null
            ? PlynlingArt.SpriteOf(p, now)
            : PlynlingArt.Memorial(p.Species, PlynlingCatalog.MemorialTier(p))))
        .WithDescription(PlynlingCardUi.SafeName(p.Name));

    // Six views no longer fit Discord's five buttons a row.
    private const int ViewsPerRow = 3;

    // Everything a page has besides its rows: the title, the rows (or the empty line), the footer, then the
    // view rows, the window row and the pager.
    private static MessageComponent Frame(IReadOnlyList<(ThumbnailBuilder Picture, string Text)> rows,
        LeaderboardView view, StatsPeriod period, int page, int totalPages, string? emptyLine, string footer)
    {
        var container = new ContainerBuilder()
            .WithAccentColor(Color.Purple)
            .AddComponent(new TextDisplayBuilder(LevelCardUi.Title(view)))
            .AddComponent(new SeparatorBuilder());

        if (emptyLine is not null)
            container.AddComponent(new TextDisplayBuilder(emptyLine));
        foreach (var (picture, text) in rows)
            container.AddComponent(new SectionBuilder()
                .WithAccessory(picture)
                .AddComponent(new TextDisplayBuilder(text)));

        container
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(footer));

        var builder = new ComponentBuilderV2().AddComponent(container);

        // What is being ranked, in rows of three. Changing it keeps the period. One verb for both rows is
        // safe: each button's id carries its own view, so no two are the same.
        foreach (var chunk in Enum.GetValues<LeaderboardView>().Chunk(ViewsPerRow))
        {
            var views = new ActionRowBuilder();
            foreach (var candidate in chunk)
            {
                views.WithButton(
                    LevelCardUi.ViewLabel(candidate),
                    $"level:view:{candidate}:{period}:0",
                    candidate == view ? ButtonStyle.Primary : ButtonStyle.Secondary,
                    disabled: candidate == view);
            }
            builder.AddComponent(views);
        }

        return builder
            // The window, from the same helper /emotestats and /goodbot use, so
            // the three commands cannot drift into labelling it differently. Its own
            // verb, never the paging one — see the handlers above.
            .AddComponent(new ActionRowBuilder().AddFilterRow($"level:win:{view}", period))
            .AddComponent(new ActionRowBuilder()
                .WithButton("◀", $"level:page:{view}:{period}:{page - 1}", ButtonStyle.Secondary,
                    disabled: page == 0)
                .WithButton("▶", $"level:page:{view}:{period}:{page + 1}", ButtonStyle.Secondary,
                    disabled: page >= totalPages - 1))
            .Build();
    }

    // The *viewer's* standing, not the original caller's: on a component interaction
    // Context.User (viewerId) is whoever clicked, so this stays correct for everyone reading the
    // same public message, with nothing threaded through the custom-id.
    //
    // Read off the ranking already in hand rather than queried separately — it is the
    // list this page was cut from, so the rank shown cannot disagree with the rows
    // above it, and it follows whichever view is on screen for free. Someone absent
    // from the current ranking (no reactions, no voice) simply gets no standing line.
    private static string Footer(IReadOnlyList<MemberTally> ranking, ulong viewerId,
        LeaderboardView view, StatsPeriod period, int page, int totalPages)
    {
        var index = ranking.ToList().FindIndex(t => t.UserId == viewerId);
        if (index < 0) return $"-# Page {page + 1}/{totalPages}";

        var mine = ranking[index];
        var standing = view switch
        {
            LeaderboardView.Reactions => $"{LevelCardUi.Xp(mine.ReactionsUsed)} réaction(s)",
            LeaderboardView.Voice => LevelCardUi.Duration(mine.VoiceMinutes),
            LeaderboardView.Collections => LevelCardUi.Completions(mine.Completions),
            LeaderboardView.Patrons => LevelCardUi.Spent(mine.CaillouxSpent),
            // Same reason the rows drop the level in a window: it is a lifetime figure.
            _ when period != StatsPeriod.AllTime => $"{LevelCardUi.Xp(mine.TotalXp)} XP gagnés",
            _ => $"niveau {mine.Level} ({LevelCardUi.Xp(mine.TotalXp)} XP)",
        };

        return $"-# Page {page + 1}/{totalPages} · toi : rang #{index + 1}, {standing}";
    }

    // Moved to Helpers/Core/AvatarUi once ShameModule needed the same accessory; kept as a
    // local alias so the call sites above read the same as they always did.
    private static ThumbnailBuilder Avatar(IUser? user) => AvatarUi.Thumbnail(user);
}
