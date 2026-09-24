using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

public enum GraveSort { Recent, Longest }

// /graveyard — every dead Plynling in the server (or one person's), five per page, each
// with its memorial as the picture. Components V2, like /leaderboard, and for the same
// budget: a row with a picture costs three components; 5 rows + two button rows = 24/40.
//
// Two button rows, two verbs: `grave:sort:` and `grave:page:`. Both encode the same state,
// so sharing a verb would collide by construction (COMPONENT_CUSTOM_ID_DUPLICATED).
[CommandContextType(InteractionContextType.Guild)]
public class GraveyardModule : InteractionModuleBase<SocketInteractionContext>
{
    public const int PageSize = 5;

    private readonly PlynlingService _plynlings;

    public GraveyardModule(PlynlingService plynlings)
    {
        _plynlings = plynlings;
    }

    [SlashCommand("graveyard", "Le cimetière des Plynlings")]
    public async Task GraveyardAsync(
        [Summary("user", "Seulement les tombes de cette personne")] IUser? user = null)
    {
        var now = DateTimeOffset.UtcNow;
        var graves = await _plynlings.GetGraveyardAsync(Context.Guild.Id, user?.Id, now);
        await RespondAsync(components: BuildPage(graves, GraveSort.Recent, user?.Id ?? 0, 0, now),
            flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
    }

    [ComponentInteraction("grave:page:*:*:*", ignoreGroupNames: true)]
    public Task OnPageAsync(string sort, string owner, string page) => ShowAsync(sort, owner, page);

    [ComponentInteraction("grave:sort:*:*:*", ignoreGroupNames: true)]
    public Task OnSortAsync(string sort, string owner, string page) => ShowAsync(sort, owner, page);

    private async Task ShowAsync(string sortStr, string ownerStr, string pageStr)
    {
        if (!Enum.TryParse<GraveSort>(sortStr, out var sort)) sort = GraveSort.Recent;
        ulong.TryParse(ownerStr, out var owner);
        int.TryParse(pageStr, out var page);

        var now = DateTimeOffset.UtcNow;
        var graves = await _plynlings.GetGraveyardAsync(Context.Guild.Id, owner == 0 ? null : owner, now);
        var components = BuildPage(graves, sort, owner, page, now);

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Components = components;
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    // Ties break on id so the order is stable across re-renders.
    public static List<Plynling> Order(IEnumerable<Plynling> graves, GraveSort sort, DateTimeOffset now) =>
        sort == GraveSort.Longest
            ? graves.OrderByDescending(p => PlynlingLife.Age(p, now)).ThenBy(p => p.Id).ToList()
            : graves.OrderByDescending(p => p.DiedAt).ThenBy(p => p.Id).ToList();

    public static GraveSortLabel Label(GraveSort sort) => sort == GraveSort.Longest
        ? new GraveSortLabel("plus longues vies", "⏳ Plus longue vie")
        : new GraveSortLabel("plus récents", "🕯️ Plus récents");

    public static MessageComponent BuildPage(
        IReadOnlyList<Plynling> graves, GraveSort sort, ulong owner, int page, DateTimeOffset now)
    {
        var ordered = Order(graves, sort, now);
        var totalPages = Math.Max(1, (int)Math.Ceiling(ordered.Count / (double)PageSize));
        page = Math.Clamp(page, 0, totalPages - 1);

        var container = new ContainerBuilder()
            .WithAccentColor(new Color(0x5A5E78))
            .AddComponent(new TextDisplayBuilder(PlynlingCardUi.GraveyardTitle(owner, Label(sort))));

        if (ordered.Count == 0)
            container.AddComponent(new TextDisplayBuilder(PlynlingCardUi.EmptyGraveyard(owner)));

        foreach (var plynling in ordered.Skip(page * PageSize).Take(PageSize))
        {
            var tier = PlynlingCatalog.MemorialTier(PlynlingLife.Age(plynling, now));
            container.AddComponent(new SectionBuilder()
                .WithAccessory(new ThumbnailBuilder()
                    .WithMedia(new UnfurledMediaItemProperties(PlynlingArt.Memorial(plynling.Species, tier)))
                    .WithDescription(PlynlingCatalog.MemorialName(tier)))
                .AddComponent(new TextDisplayBuilder(PlynlingCardUi.GraveLine(plynling, now))));
        }

        container.AddComponent(new TextDisplayBuilder($"-# Page {page + 1}/{totalPages} · {ordered.Count} tombe(s)"));

        var sortRow = new ActionRowBuilder();
        foreach (var candidate in new[] { GraveSort.Recent, GraveSort.Longest })
            sortRow.WithButton(Label(candidate).Button, $"grave:sort:{candidate}:{owner}:0",
                candidate == sort ? ButtonStyle.Primary : ButtonStyle.Secondary, disabled: candidate == sort);

        var pageRow = new ActionRowBuilder()
            .WithButton("◀", $"grave:page:{sort}:{owner}:{page - 1}", ButtonStyle.Secondary, disabled: page == 0)
            .WithButton("▶", $"grave:page:{sort}:{owner}:{page + 1}", ButtonStyle.Secondary, disabled: page >= totalPages - 1);

        return new ComponentBuilderV2()
            .AddComponent(container)
            .AddComponent(sortRow)
            .AddComponent(pageRow)
            .Build();
    }
}
