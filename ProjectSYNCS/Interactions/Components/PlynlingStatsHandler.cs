using Discord;
using Discord.Interactions;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /plynling stats' tabs (PlynlingStatsCards): anyone may switch, the card is public like the graveyard's pager.
// Each click defers (a sheet reads a lot), re-reads, and redraws the message in place through CardSend.
public class PlynlingStatsHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PlynlingService _plynlings;

    public PlynlingStatsHandler(PlynlingService plynlings) => _plynlings = plynlings;

    [ComponentInteraction("plst:tab:*:*", ignoreGroupNames: true)]
    public async Task OnTabAsync(string idStr, string tabStr)
    {
        await DeferAsync();
        var now = DateTimeOffset.UtcNow;
        var p = int.TryParse(idStr, out var id) ? await _plynlings.GetByIdAsync(id, now) : null;
        if (p is null)
        {
            await FollowupAsync(PlynlingText.StatsGone, ephemeral: true);
            return;
        }
        var sheet = await _plynlings.GetStatSheetAsync(p, now);
        await Context.Interaction.ModifyCardAsync(PlynlingStatsCards.Build(sheet, Tab(tabStr)));
    }

    [ComponentInteraction("plst:duel:*:*:*", ignoreGroupNames: true)]
    public async Task OnDuelAsync(string aStr, string bStr, string tabStr)
    {
        await DeferAsync();
        var now = DateTimeOffset.UtcNow;
        var a = int.TryParse(aStr, out var aId) ? await _plynlings.GetByIdAsync(aId, now) : null;
        var b = int.TryParse(bStr, out var bId) ? await _plynlings.GetByIdAsync(bId, now) : null;
        if (a is null || b is null)
        {
            await FollowupAsync(PlynlingText.StatsGone, ephemeral: true);
            return;
        }
        var sa = await _plynlings.GetStatSheetAsync(a, now);
        var sb = await _plynlings.GetStatSheetAsync(b, now);
        var pair = await _plynlings.GetStatPairAsync(a, b);
        await Context.Interaction.ModifyCardAsync(PlynlingStatsCards.BuildDuel(sa, sb, Tab(tabStr), pair));
    }

    private static StatsTab Tab(string s) =>
        int.TryParse(s, out var t) && Enum.IsDefined(typeof(StatsTab), t) ? (StatsTab)t : StatsTab.Life;
}
