using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /inventory shop's select and quantity buttons. The shop is an ephemeral message only its owner
// sees, so whoever clicks is whoever it is for; every click re-reads the inventory and redraws it.
// Both read the database before they can answer, so both defer first. A purchase that went through
// gets its own ephemeral receipt (ShopCards.Receipt) and a clean card; a refusal is the card's notice.
public class ShopComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly InventoryService _inventory;
    private readonly ILogger<ShopComponentHandler> _logger;

    public ShopComponentHandler(InventoryService inventory, ILogger<ShopComponentHandler> logger)
    {
        _inventory = inventory;
        _logger = logger;
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
        var notice = ShopCards.Notice(outcome, ItemCatalog.ByKey(key), quantity, price, balance);
        if (outcome == InventoryService.ShopOutcome.Bought) await ConfirmAsync(key, notice);
        else await RedrawAsync(key, notice);
    }

    // A collection pack, opened at once; the redraw keeps the pack picked so another is one click away.
    [ComponentInteraction("shop:pack:*", ignoreGroupNames: true)]
    public async Task OnPackAsync(string quantityStr)
    {
        await DeferAsync();
        var quantity = int.TryParse(quantityStr, out var q) && ItemCatalog.PackQuantities.Contains(q) ? q : 0;
        var result = await _inventory.OpenPackAsync(Context.Guild.Id, Context.User.Id, quantity, DateTimeOffset.UtcNow, Random.Shared);
        if (result.Outcome == PackOutcome.Opened) await ConfirmAsync(ItemCatalog.PackKey, ShopCards.PackNotice(result));
        else await RedrawAsync(ItemCatalog.PackKey, ShopCards.PackNotice(result));
    }

    // The purchase is saved: redraw the card (new balance, buttons re-checked), then the receipt. A
    // failed receipt is logged and dropped — the purchase stands either way.
    private async Task ConfirmAsync(string picked, string notice)
    {
        await RedrawAsync(picked, null);
        try
        {
            await FollowupAsync(ShopCards.Receipt(notice), ephemeral: true, allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shop receipt failed for {UserId}", Context.User.Id);
        }
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
