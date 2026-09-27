using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum CosmeticOutcome { Done, UnknownItem, AlreadyOwned, TooPoor, NotInShop, MissingIngredients, NotOwned, NoPlynling }

// Buying, crafting and wearing cosmetics — transient, it wraps AppDbContext. Each call is one
// save: the cailloux, the items and what is worn move together or not at all.
public class CosmeticService
{
    private readonly AppDbContext _db_context;

    public CosmeticService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    /// <summary>
    /// Buys a cosmetic from the shop of <paramref name="week"/> — the week the shop message was
    /// drawn from, so a select left over from last week cannot buy what is no longer on sale.
    /// </summary>
    public async Task<(CosmeticOutcome Outcome, long Balance)> BuyAsync(ulong guildId, ulong userId, string key, int week, DateTimeOffset now)
    {
        if (CosmeticCatalog.ByKey(key) is not { } cosmetic) return (CosmeticOutcome.UnknownItem, 0);
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, userId);
        if (week != CosmeticCatalog.WeekKey(now) || !CosmeticCatalog.Shop(now).Contains(cosmetic))
            return (CosmeticOutcome.NotInShop, wallet.Balance);
        if (await InventoryService.CountAsync(_db_context, guildId, userId, key) > 0) return (CosmeticOutcome.AlreadyOwned, wallet.Balance);
        if (wallet.Balance < cosmetic.Price) return (CosmeticOutcome.TooPoor, wallet.Balance);

        wallet.Balance -= cosmetic.Price;
        await InventoryService.AddAsync(_db_context, guildId, userId, key, 1, now);
        await _db_context.SaveChangesAsync();
        return (CosmeticOutcome.Done, wallet.Balance);
    }

    /// <summary>Crafts a cosmetic: every ingredient and the cailloux, or nothing at all.</summary>
    public async Task<(CosmeticOutcome Outcome, long Balance)> CraftAsync(ulong guildId, ulong userId, string key, DateTimeOffset now)
    {
        if (CosmeticCatalog.ByKey(key) is not { Source: CosmeticSource.Crafted } cosmetic) return (CosmeticOutcome.UnknownItem, 0);
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, userId);
        if (await InventoryService.CountAsync(_db_context, guildId, userId, key) > 0) return (CosmeticOutcome.AlreadyOwned, wallet.Balance);
        foreach (var (itemKey, count) in cosmetic.Recipe)
            if (await InventoryService.CountAsync(_db_context, guildId, userId, itemKey) < count)
                return (CosmeticOutcome.MissingIngredients, wallet.Balance);
        if (wallet.Balance < cosmetic.Price) return (CosmeticOutcome.TooPoor, wallet.Balance);

        foreach (var (itemKey, count) in cosmetic.Recipe)
            await InventoryService.TakeAsync(_db_context, guildId, userId, itemKey, count);
        wallet.Balance -= cosmetic.Price;
        await InventoryService.AddAsync(_db_context, guildId, userId, key, 1, now);
        await _db_context.SaveChangesAsync();
        return (CosmeticOutcome.Done, wallet.Balance);
    }

    /// <summary>
    /// Puts a cosmetic on the person's living Plynling, in its slot — or takes that slot off when
    /// <paramref name="key"/> is null. Wearing needs at least one in the inventory and uses none.
    /// </summary>
    public async Task<(CosmeticOutcome Outcome, Plynling? Plynling)> WearAsync(ulong guildId, ulong userId, CosmeticSlot slot, string? key, DateTimeOffset now)
    {
        var plynling = await _db_context.Plynlings.FirstOrDefaultAsync(p => p.GuildId == guildId && p.OwnerId == userId && p.DiedAt == null);
        if (plynling is null) return (CosmeticOutcome.NoPlynling, null);
        if (key is not null)
        {
            if (CosmeticCatalog.ByKey(key) is not { } cosmetic || cosmetic.Slot != slot) return (CosmeticOutcome.UnknownItem, plynling);
            if (await InventoryService.CountAsync(_db_context, guildId, userId, key) < 1) return (CosmeticOutcome.NotOwned, plynling);
        }
        CosmeticSlots.Set(plynling, slot, key);
        await _db_context.SaveChangesAsync();
        return (CosmeticOutcome.Done, plynling);
    }

    // The cosmetics someone holds (at least one), in catalog order.
    public async Task<List<CosmeticInfo>> OwnedAsync(ulong guildId, ulong userId)
    {
        var held = (await _db_context.InventoryItems
                .Where(i => i.GuildId == guildId && i.UserId == userId && i.Quantity > 0 && i.Key.StartsWith("cos."))
                .Select(i => i.Key).ToListAsync())
            .ToHashSet();
        return CosmeticCatalog.All.Where(c => held.Contains(c.Key)).ToList();
    }
}
