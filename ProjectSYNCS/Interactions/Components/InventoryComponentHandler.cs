using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /inventory collection's controls: the category menu and the filter buttons. Anyone may flip
// through someone's book — it is public, like the message — and every click re-reads the
// inventory, so the page shows the book as it is now.
public class InventoryComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly InventoryService _inventory;

    public InventoryComponentHandler(InventoryService inventory)
    {
        _inventory = inventory;
    }

    [ComponentInteraction("col:set:*:*", ignoreGroupNames: true)]
    public Task OnSetAsync(string user, string filter, string[] selected) =>
        ShowAsync(user, selected.FirstOrDefault() ?? InventoryModule.OverviewPage, filter);

    [ComponentInteraction("col:fil:*:*:*", ignoreGroupNames: true)]
    public Task OnFilterAsync(string user, string page, string filter) => ShowAsync(user, page, filter);

    private async Task ShowAsync(string userStr, string page, string filterStr)
    {
        if (!ulong.TryParse(userStr, out var userId)) return;
        if (!Enum.TryParse<CollectionFilter>(filterStr, out var filter)) filter = CollectionFilter.All;
        var held = await _inventory.GetAllAsync(Context.Guild.Id, userId);
        var completions = await _inventory.GetCompletionsAsync(Context.Guild.Id, userId);
        var (embed, components) = InventoryModule.BuildCollectionPage(userId, held, completions, page, filter);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Embed = embed;
            m.Components = components;
            m.AllowedMentions = AllowedMentions.None;
        });
    }
}
