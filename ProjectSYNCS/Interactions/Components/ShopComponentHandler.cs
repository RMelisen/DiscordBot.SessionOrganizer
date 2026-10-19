using Discord;
using Discord.Interactions;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /inventory shop's select and quantity buttons. The shop is an ephemeral message only its owner
// sees, so whoever clicks is whoever it is for; every click re-reads the inventory and redraws it.
// Both read the database before they can answer, so both defer first.
public class ShopComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly InventoryService _inventory;

    public ShopComponentHandler(InventoryService inventory)
    {
        _inventory = inventory;
    }

    [ComponentInteraction("shop:pick", ignoreGroupNames: true)]
    public async Task OnPickAsync(string[] selected)
    {
        await DeferAsync();
        await RedrawAsync(selected.FirstOrDefault(), null);
    }

    [ComponentInteraction("shop:buy:*:*", ignoreGroupNames: true)]
    public async Task OnBuyAsync(string key, string quantityStr)
    {
        await DeferAsync();
        var quantity = int.TryParse(quantityStr, out var q) && ShopCards.Quantities.Contains(q) ? q : 0;
        var (outcome, price, balance) = await _inventory.BuyAsync(Context.Guild.Id, Context.User.Id, key, quantity, DateTimeOffset.UtcNow);
        await RedrawAsync(key, ShopCards.Notice(outcome, ItemCatalog.ByKey(key), quantity, price, balance));
    }

    private async Task RedrawAsync(string? picked, string? notice)
    {
        var held = (await _inventory.GetAllAsync(Context.Guild.Id, Context.User.Id)).ToDictionary(i => i.Key, i => i.Quantity);
        var balance = await _inventory.BalanceAsync(Context.Guild.Id, Context.User.Id);
        var (embed, components) = ShopCards.BuildShop(held, balance, picked, notice);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Embed = embed;
            m.Components = components;
            m.AllowedMentions = AllowedMentions.None;
        });
    }
}
