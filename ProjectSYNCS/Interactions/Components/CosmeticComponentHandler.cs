using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The cosmetics screens' selects. Each screen is an ephemeral message only its owner sees, so
// whoever picks is whoever the screen is about; every pick redraws that message with a notice.
public class CosmeticComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly CosmeticService _cosmetics;
    private readonly InventoryService _inventory;
    private readonly PlynlingService _plynlings;

    public CosmeticComponentHandler(CosmeticService cosmetics, InventoryService inventory, PlynlingService plynlings)
    {
        _cosmetics = cosmetics;
        _inventory = inventory;
        _plynlings = plynlings;
    }

    // The shop's « Acheter… », carrying the week its message was drawn from.
    [ComponentInteraction("cos:buy:*", ignoreGroupNames: true)]
    public async Task OnBuyAsync(string weekStr, string[] selected)
    {
        var now = DateTimeOffset.UtcNow;
        var key = selected.FirstOrDefault() ?? "";
        var week = int.TryParse(weekStr, out var w) ? w : 0;
        var (outcome, _) = await _cosmetics.BuyAsync(Context.Guild.Id, Context.User.Id, key, week, now);
        var cosmetic = CosmeticCatalog.ByKey(key);
        var (held, balance) = await HeldAsync();
        var (embed, components) = CosmeticCards.BuildShop(now, InventoryModule.Owned(held), balance,
            CosmeticCards.Notice(outcome, cosmetic, cosmetic?.Price ?? 0));
        await RedrawAsync(embed, components);
    }

    [ComponentInteraction("cos:craft", ignoreGroupNames: true)]
    public async Task OnCraftAsync(string[] selected)
    {
        var key = selected.FirstOrDefault() ?? "";
        var (outcome, _) = await _cosmetics.CraftAsync(Context.Guild.Id, Context.User.Id, key, DateTimeOffset.UtcNow);
        var cosmetic = CosmeticCatalog.ByKey(key);
        var (held, balance) = await HeldAsync();
        var (embed, components) = CosmeticCards.BuildCraft(held, InventoryModule.Owned(held), balance,
            CosmeticCards.Notice(outcome, cosmetic, cosmetic?.Price ?? 0));
        await RedrawAsync(embed, components);
    }

    // One select per slot; « Aucun » takes the slot off.
    [ComponentInteraction("cos:wear:*", ignoreGroupNames: true)]
    public async Task OnWearAsync(string slotStr, string[] selected)
    {
        var now = DateTimeOffset.UtcNow;
        if (!Enum.TryParse<CosmeticSlot>(slotStr, out var slot)) return;
        var value = selected.FirstOrDefault();
        var key = value is null or CosmeticCards.NoneValue ? null : value;
        var (outcome, plynling) = await _cosmetics.WearAsync(Context.Guild.Id, Context.User.Id, slot, key, now);
        if (plynling is null)
        {
            await RespondAsync(PlynlingText.NoPlynling, ephemeral: true);
            return;
        }
        var notice = outcome == CosmeticOutcome.Done
            ? (key is null ? $"{CosmeticCatalog.SlotLabel(slot)} retiré." : $"✨ {PlynlingCardUi.SafeName(plynling.Name)} porte maintenant ça !")
            : CosmeticCards.Notice(outcome, CosmeticCatalog.ByKey(key));
        var owned = await _cosmetics.OwnedAsync(Context.Guild.Id, Context.User.Id);
        var (embed, components) = CosmeticCards.BuildWardrobe(plynling, owned, notice);
        await RedrawAsync(embed, components);
    }

    private async Task<(Dictionary<string, int> Held, long Balance)> HeldAsync() =>
        ((await _inventory.GetAllAsync(Context.Guild.Id, Context.User.Id)).ToDictionary(i => i.Key, i => i.Quantity),
         await _inventory.BalanceAsync(Context.Guild.Id, Context.User.Id));

    private Task RedrawAsync(Embed embed, MessageComponent components) =>
        ((SocketMessageComponent)Context.Interaction).UpdateAsync(m =>
        {
            m.Embed = embed;
            m.Components = components;
            m.AllowedMentions = AllowedMentions.None;
        });
}
