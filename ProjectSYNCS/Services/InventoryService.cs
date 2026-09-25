using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// One item found — by foraging, a gift, a game or a visit — and any collection it completed.
public sealed record ItemFind(ItemInfo Item, IReadOnlyList<CollectionSet> Completed);

// The inventory — transient, it wraps AppDbContext. The static helpers take a context so other
// services (feeding, the gift, games, visits) add and take items in **their own** unit of work:
// an action and the items it moves land in one save, the same reason PebbleService's wallet
// helper is static.
public class InventoryService
{
    private readonly AppDbContext _db_context;

    public InventoryService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    public static async Task<InventoryItem?> FindAsync(AppDbContext db, ulong guildId, ulong userId, string key) =>
        db.InventoryItems.Local.FirstOrDefault(i => i.GuildId == guildId && i.UserId == userId && i.Key == key)
        ?? await db.InventoryItems.FirstOrDefaultAsync(i => i.GuildId == guildId && i.UserId == userId && i.Key == key);

    public static async Task<int> CountAsync(AppDbContext db, ulong guildId, ulong userId, string key) =>
        (await FindAsync(db, guildId, userId, key))?.Quantity ?? 0;

    /// <summary>Takes <paramref name="count"/> of an item, or nothing at all when there are not enough.</summary>
    public static async Task<bool> TakeAsync(AppDbContext db, ulong guildId, ulong userId, string key, int count)
    {
        var row = await FindAsync(db, guildId, userId, key);
        if (row is null || row.Quantity < count) return false;
        row.Quantity -= count;                            // the row stays at 0: still discovered
        return true;
    }

    /// <summary>
    /// Adds items (a first one discovers it), then pays any collection set this completes —
    /// once each. Not saved: the caller's save carries it. Returns the sets just completed.
    /// </summary>
    public static async Task<List<CollectionSet>> AddAsync(AppDbContext db, ulong guildId, ulong userId, string key, int count, DateTimeOffset now)
    {
        var row = await FindAsync(db, guildId, userId, key);
        if (row is null)
        {
            row = new InventoryItem { GuildId = guildId, UserId = userId, Key = key, DiscoveredAt = now };
            db.InventoryItems.Add(row);
        }
        row.Quantity += count;

        var completed = new List<CollectionSet>();
        if (ItemCatalog.ByKey(key)?.Set is not { } setKey) return completed;
        var set = ItemCatalog.Sets.First(s => s.Key == setKey);
        var setItems = ItemCatalog.InSet(setKey).Select(i => i.Key).ToList();
        var discovered = (await db.InventoryItems
                .Where(i => i.GuildId == guildId && i.UserId == userId && setItems.Contains(i.Key))
                .Select(i => i.Key).ToListAsync())
            .Concat(db.InventoryItems.Local.Where(i => i.GuildId == guildId && i.UserId == userId && setItems.Contains(i.Key)).Select(i => i.Key))
            .ToHashSet();
        if (!setItems.All(discovered.Contains)) return completed;

        var paid = db.CollectionCompletions.Local.Any(c => c.GuildId == guildId && c.UserId == userId && c.SetKey == setKey)
                   || await db.CollectionCompletions.AnyAsync(c => c.GuildId == guildId && c.UserId == userId && c.SetKey == setKey);
        if (paid) return completed;

        db.CollectionCompletions.Add(new CollectionCompletion { GuildId = guildId, UserId = userId, SetKey = setKey, CompletedAt = now });
        var wallet = await PebbleService.GetOrCreateWalletAsync(db, guildId, userId);
        wallet.Balance += set.Reward;
        completed.Add(set);
        return completed;
    }

    /// <summary>One found item into someone's inventory (not saved, like <see cref="AddAsync"/>).</summary>
    public static async Task<ItemFind> GrantAsync(AppDbContext db, ulong guildId, ulong userId, ItemInfo item, DateTimeOffset now) =>
        new(item, await AddAsync(db, guildId, userId, item.Key, 1, now));

    public enum GiveOutcome { Given, NotEnough, UnknownItem }

    /// <summary>/plynling shop: the money and the food land together, or neither.</summary>
    public async Task<(bool Bought, long Price, long Balance)> BuyAsync(ulong guildId, ulong userId, PlynlingFood food, int quantity, DateTimeOffset now)
    {
        var price = ItemCatalog.ShopPrice(PlynlingCatalog.Info(food), quantity);
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, userId);
        if (wallet.Balance < price) return (false, price, wallet.Balance);
        wallet.Balance -= price;
        await AddAsync(_db_context, guildId, userId, ItemCatalog.FoodKey(food), quantity, now);
        await _db_context.SaveChangesAsync();
        return (true, price, wallet.Balance);
    }

    /// <summary>/plynling give: from one person's inventory to another's, in one save.</summary>
    public async Task<(GiveOutcome Outcome, List<CollectionSet> Completed)> GiveAsync(
        ulong guildId, ulong fromId, ulong toId, string key, int quantity, DateTimeOffset now)
    {
        if (ItemCatalog.ByKey(key) is null) return (GiveOutcome.UnknownItem, new());
        if (!await TakeAsync(_db_context, guildId, fromId, key, quantity)) return (GiveOutcome.NotEnough, new());
        var completed = await AddAsync(_db_context, guildId, toId, key, quantity, now);
        await _db_context.SaveChangesAsync();
        return (GiveOutcome.Given, completed);
    }

    public Task<long> BalanceAsync(ulong guildId, ulong userId) =>
        _db_context.PebbleWallets.Where(w => w.GuildId == guildId && w.UserId == userId).Select(w => w.Balance).FirstOrDefaultAsync();

    // Everything someone has ever held here (quantity 0 included: discovered).
    public Task<List<InventoryItem>> GetAllAsync(ulong guildId, ulong userId) =>
        _db_context.InventoryItems.Where(i => i.GuildId == guildId && i.UserId == userId).ToListAsync();

    public Task<List<CollectionCompletion>> GetCompletionsAsync(ulong guildId, ulong userId) =>
        _db_context.CollectionCompletions.Where(c => c.GuildId == guildId && c.UserId == userId).ToListAsync();
}
