using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /inventory collection's controls (the category menu, the filter buttons, « Déposer ») and /inventory
// view's two pages. Anyone may flip through someone's book — it is public, like the message — and
// every click re-reads the inventory, so the page shows the book as it is now.
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

    // « Déposer » on a set's page: only the book's owner, and only after an ephemeral confirmation
    // naming what it spends — a turn-in can take someone's only Météorite.
    [ComponentInteraction("col:dep:*:*:*", ignoreGroupNames: true)]
    public async Task OnTurnInAsync(string user, string setKey, string filter)
    {
        if (!ulong.TryParse(user, out var userId) || userId != Context.User.Id)
        {
            await RespondAsync("Ce n'est pas ton carnet : seul son propriétaire peut déposer ses collections.", ephemeral: true);
            return;
        }
        if (ItemCatalog.Sets.FirstOrDefault(s => s.Key == setKey) is not { } set) return;
        await DeferAsync(ephemeral: true);
        var held = (await _inventory.GetAllAsync(Context.Guild.Id, userId)).ToDictionary(i => i.Key, i => i.Quantity);
        var embed = TurnInConfirmation(set, held);
        await FollowupAsync(embed: embed, ephemeral: true, components: new ComponentBuilder()
            .WithButton("Confirmer le dépôt", $"col:depok:{set.Key}", ButtonStyle.Success, new Emoji("📥"),
                disabled: !InventoryModule.CanTurnIn(set.Key, held))
            .Build());
    }

    // The confirmation is ephemeral, so whoever clicks it is the book's owner.
    [ComponentInteraction("col:depok:*", ignoreGroupNames: true)]
    public async Task OnTurnInConfirmedAsync(string setKey)
    {
        await DeferAsync();
        var result = await _inventory.TurnInAsync(Context.Guild.Id, Context.User.Id, setKey, DateTimeOffset.UtcNow);
        var notice = result.Outcome switch
        {
            InventoryService.TurnInOutcome.TurnedIn =>
                $"📥 Dépôt fait : **{result.Set!.Emoji} {result.Set.Name}** complétée {result.Count} fois. " +
                $"+{PebbleEconomy.Cailloux(result.Set.Reward)} (solde : {PebbleEconomy.Cailloux(result.Balance)})",
            InventoryService.TurnInOutcome.Missing =>
                "Il te manque déjà quelque chose : " + string.Join(", ", result.Missing.Select(i => $"{i.Emoji} {i.Name}")) + ".",
            InventoryService.TurnInOutcome.NotCompleted => "Cette collection n'est pas encore complète : découvre d'abord chaque objet.",
            _ => "Cette collection n'existe pas.",
        };
        await ModifyOriginalResponseAsync(m =>
        {
            m.Content = notice;
            m.Embed = null;
            m.Components = new ComponentBuilder().Build();
        });
        if (result.Outcome == InventoryService.TurnInOutcome.TurnedIn && result.NewTier != result.OldTier)
            await FollowupAsync(PlynlingText.SetTierUp(Context.User.Id, result.Set!, result.NewTier), allowedMentions: AllowedMentions.None);
    }

    // What a turn-in will spend: one of each, with the last copies called out (an embed, since a
    // 30-item set's names with their icons would overflow a 2000-character message).
    private static Embed TurnInConfirmation(CollectionSet set, IReadOnlyDictionary<string, int> held)
    {
        var items = ItemCatalog.InSet(set.Key).ToList();
        var missing = items.Where(i => held.GetValueOrDefault(i.Key) < 1).ToList();
        var last = items.Where(i => held.GetValueOrDefault(i.Key) == 1).ToList();
        var embed = new EmbedBuilder().WithTitle($"Déposer {set.Name} ?").WithColor(Color.Purple);
        if (missing.Count > 0)
            return embed.WithDescription("Il te manque : " + string.Join(", ", missing.Select(i => $"{i.Emoji} {i.Name}")) +
                                         "\n\nUn dépôt demande un exemplaire de chaque objet.").Build();
        var description = $"Tu donnes **un exemplaire de chacun des {items.Count} objets** contre " +
                          $"**+{PebbleEconomy.Cailloux(set.Reward)}**, et une complétion de plus vers le palier suivant.";
        if (last.Count > 0)
            description += "\n\n⚠️ Tes derniers exemplaires (ils resteront découverts) : " +
                           string.Join(", ", last.Select(i => $"{i.Emoji} {i.Name}"));
        return embed.WithDescription(description).Build();
    }

    // The inventory is ephemeral, so whoever clicks is its owner.
    [ComponentInteraction("inv:page:*", ignoreGroupNames: true)]
    public async Task OnInventoryPageAsync(string pageStr)
    {
        if (!Enum.TryParse<InventoryPage>(pageStr, out var page)) page = InventoryPage.Items;
        var userId = Context.User.Id;
        var held = await _inventory.GetAllAsync(Context.Guild.Id, userId);
        var completions = await _inventory.GetCompletionsAsync(Context.Guild.Id, userId);
        var balance = await _inventory.BalanceAsync(Context.Guild.Id, userId);
        var (embed, components) = InventoryModule.BuildInventoryPage(held, completions.Count, balance, page);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Embed = embed;
            m.Components = components;
        });
    }

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
