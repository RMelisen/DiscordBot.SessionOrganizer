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

    private readonly BadgeAwarder _badges;

    public InventoryService(AppDbContext db_context, BadgeAwarder badges)
    {
        _db_context = db_context;
        _badges = badges;
    }

    // How many of an item someone holds, in this service's own context.
    public Task<int> CountAsync(ulong guildId, ulong userId, string key) => CountAsync(_db_context, guildId, userId, key);

    public static async Task<InventoryItem?> FindAsync(AppDbContext db, ulong guildId, ulong userId, string key) =>
        db.InventoryItems.Local.FirstOrDefault(i => i.GuildId == guildId && i.UserId == userId && i.Key == key)
        ?? await db.InventoryItems.FirstOrDefaultAsync(i => i.GuildId == guildId && i.UserId == userId && i.Key == key);

    public static async Task<int> CountAsync(AppDbContext db, ulong guildId, ulong userId, string key) =>
        (await FindAsync(db, guildId, userId, key))?.Quantity ?? 0;

    /// <summary>
    /// Takes <paramref name="count"/> of an item, or nothing at all when there are not enough. A
    /// cosmetic that falls to 0 is taken off the person's living Plynling in the same unit of
    /// work, so a card never shows something its owner no longer has; a grave keeps its own.
    /// </summary>
    public static async Task<bool> TakeAsync(AppDbContext db, ulong guildId, ulong userId, string key, int count)
    {
        var row = await FindAsync(db, guildId, userId, key);
        if (row is null || row.Quantity < count) return false;
        row.Quantity -= count;                            // the row stays at 0: still discovered
        if (row.Quantity == 0 && CosmeticCatalog.ByKey(key) is { } cosmetic)
        {
            var wearer = db.Plynlings.Local.FirstOrDefault(p => p.GuildId == guildId && p.OwnerId == userId && p.DiedAt == null)
                         ?? await db.Plynlings.FirstOrDefaultAsync(p => p.GuildId == guildId && p.OwnerId == userId && p.DiedAt == null);
            if (wearer is not null && CosmeticSlots.Get(wearer, cosmetic.Slot) == key)
                CosmeticSlots.Set(wearer, cosmetic.Slot, null);
        }
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

        db.CollectionCompletions.Add(new CollectionCompletion { GuildId = guildId, UserId = userId, SetKey = setKey, CompletedAt = now, Count = 1 });
        await PayCompletionAsync(db, guildId, userId, set, now);
        completed.Add(set);
        return completed;
    }

    // What every completion of a set pays and records — the first (discovery) and each turn-in:
    // the set's reward, the dashboard's counters and today's ranking bucket. Not saved.
    private static async Task<long> PayCompletionAsync(AppDbContext db, ulong guildId, ulong userId, CollectionSet set, DateTimeOffset now)
    {
        var wallet = await PebbleService.GetOrCreateWalletAsync(db, guildId, userId);
        wallet.Balance += set.Reward;
        await EconomyLog.AddAsync(db, guildId, EconomyLog.ItemSet, 1, now);
        await EconomyLog.AddAsync(db, guildId, EconomyLog.EarnCollection, set.Reward, now);

        var day = AppTime.DayKey(now);
        var bucket = db.CollectionDailyStats.Local.FirstOrDefault(b => b.GuildId == guildId && b.UserId == userId && b.Day == day)
                     ?? await db.CollectionDailyStats.FirstOrDefaultAsync(b => b.GuildId == guildId && b.UserId == userId && b.Day == day);
        if (bucket is null)
        {
            bucket = new CollectionDailyStat { GuildId = guildId, UserId = userId, Day = day };
            db.CollectionDailyStats.Add(bucket);
        }
        bucket.Completions++;
        return wallet.Balance;
    }

    public enum TurnInOutcome { TurnedIn, Missing, NotCompleted, UnknownSet }

    public sealed record TurnInResult(
        TurnInOutcome Outcome, CollectionSet? Set, int Count, CollectionTier OldTier, CollectionTier NewTier,
        long Balance, IReadOnlyList<ItemInfo> Missing);

    /// <summary>
    /// A turn-in from the collection book: one of each item of an already completed set is spent,
    /// for another completion — its reward, and maybe the next tier. All of it in one save, or
    /// nothing: two clicks race on the items' quantities and the loser finds them missing.
    /// </summary>
    public Task<TurnInResult> TurnInAsync(ulong guildId, ulong userId, string setKey, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db_context, () => TurnInOnceAsync(guildId, userId, setKey, now));

    private async Task<TurnInResult> TurnInOnceAsync(ulong guildId, ulong userId, string setKey, DateTimeOffset now)
    {
        if (ItemCatalog.Sets.FirstOrDefault(s => s.Key == setKey) is not { } set)
            return new(TurnInOutcome.UnknownSet, null, 0, CollectionTier.None, CollectionTier.None, 0, Array.Empty<ItemInfo>());

        var completion = await _db_context.CollectionCompletions
            .FirstOrDefaultAsync(c => c.GuildId == guildId && c.UserId == userId && c.SetKey == setKey);
        if (completion is null)
            return new(TurnInOutcome.NotCompleted, set, 0, CollectionTier.None, CollectionTier.None, 0, Array.Empty<ItemInfo>());

        var tier = CollectionTiers.TierFor(completion.Count);
        var items = ItemCatalog.InSet(setKey).ToList();
        var missing = new List<ItemInfo>();
        foreach (var item in items)
            if (await CountAsync(_db_context, guildId, userId, item.Key) < 1) missing.Add(item);
        if (missing.Count > 0)
            return new(TurnInOutcome.Missing, set, completion.Count, tier, tier, 0, missing);

        foreach (var item in items)
            await TakeAsync(_db_context, guildId, userId, item.Key, 1);
        completion.Count++;
        var balance = await PayCompletionAsync(_db_context, guildId, userId, set, now);
        await _db_context.SaveChangesAsync();
        return new(TurnInOutcome.TurnedIn, set, completion.Count, tier, CollectionTiers.TierFor(completion.Count), balance, Array.Empty<ItemInfo>());
    }

    /// <summary>One found item into someone's inventory (not saved, like <see cref="AddAsync"/>).</summary>
    public static async Task<ItemFind> GrantAsync(AppDbContext db, ulong guildId, ulong userId, ItemInfo item, DateTimeOffset now)
    {
        await EconomyLog.AddAsync(db, guildId, EconomyLog.ItemFound, 1, now);
        return new(item, await AddAsync(db, guildId, userId, item.Key, 1, now));
    }

    public enum GiveOutcome { Given, NotEnough, UnknownItem, NotSellable }

    public enum ShopOutcome { Bought, TooPoor, UnknownItem }

    /// <summary>/inventory shop: any line of <see cref="ItemCatalog.Shop"/>; the money and the items land together, or neither.</summary>
    public Task<(ShopOutcome Outcome, long Price, long Balance)> BuyAsync(ulong guildId, ulong userId, string key, int quantity, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db_context, () => BuyOnceAsync(guildId, userId, key, quantity, now));

    private async Task<(ShopOutcome Outcome, long Price, long Balance)> BuyOnceAsync(ulong guildId, ulong userId, string key, int quantity, DateTimeOffset now)
    {
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, userId);
        if (ItemCatalog.ShopUnitPrice(key) is not { } unit || quantity < 1) return (ShopOutcome.UnknownItem, 0, wallet.Balance);
        var price = ItemCatalog.BulkPrice(unit, quantity);
        if (wallet.Balance < price) return (ShopOutcome.TooPoor, price, wallet.Balance);
        wallet.Balance -= price;
        await AddAsync(_db_context, guildId, userId, key, quantity, now);
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.SpendShop, price, now);
        await _db_context.SaveChangesAsync();
        return (ShopOutcome.Bought, price, wallet.Balance);
    }

    /// <summary>/inventory give: from one person's inventory to another's, in one save.</summary>
    public async Task<(GiveOutcome Outcome, List<CollectionSet> Completed)> GiveAsync(
        ulong guildId, ulong fromId, ulong toId, string key, int quantity, DateTimeOffset now)
    {
        var result = await EconomySave.RetryAsync(_db_context, () => GiveOnceAsync(guildId, fromId, toId, key, quantity, now));
        // Once, here — never inside the retried unit of work.
        if (result.Outcome == GiveOutcome.Given) _badges.Award(guildId, fromId, MemberBadges.Keys.Generous);
        return result;
    }

    private async Task<(GiveOutcome Outcome, List<CollectionSet> Completed)> GiveOnceAsync(
        ulong guildId, ulong fromId, ulong toId, string key, int quantity, DateTimeOffset now)
    {
        if (ItemCatalog.ByKey(key) is null) return (GiveOutcome.UnknownItem, new());
        if (!await TakeAsync(_db_context, guildId, fromId, key, quantity)) return (GiveOutcome.NotEnough, new());
        var completed = await AddAsync(_db_context, guildId, toId, key, quantity, now);
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.ActGive, 1, now);
        await _db_context.SaveChangesAsync();
        return (GiveOutcome.Given, completed);
    }

    public enum TradeOutcome { Done, OfferorLacks, TargetLacks }

    /// <summary>
    /// An accepted offer: both sides are re-checked, then both items move in one save — or
    /// nothing does. Returns the sets each side completed.
    /// </summary>
    public Task<(TradeOutcome Outcome, List<CollectionSet> FromSets, List<CollectionSet> ToSets)> TradeAsync(TradeOffer offer, DateTimeOffset now) =>
        EconomySave.RetryAsync(_db_context, () => TradeOnceAsync(offer, now));

    private async Task<(TradeOutcome Outcome, List<CollectionSet> FromSets, List<CollectionSet> ToSets)> TradeOnceAsync(TradeOffer offer, DateTimeOffset now)
    {
        var g = offer.GuildId;
        if (await CountAsync(_db_context, g, offer.FromId, offer.GiveKey) < offer.GiveQty) return (TradeOutcome.OfferorLacks, new(), new());
        if (await CountAsync(_db_context, g, offer.ToId, offer.WantKey) < offer.WantQty) return (TradeOutcome.TargetLacks, new(), new());
        await TakeAsync(_db_context, g, offer.FromId, offer.GiveKey, offer.GiveQty);
        await TakeAsync(_db_context, g, offer.ToId, offer.WantKey, offer.WantQty);
        var toSets = await AddAsync(_db_context, g, offer.ToId, offer.GiveKey, offer.GiveQty, now);
        var fromSets = await AddAsync(_db_context, g, offer.FromId, offer.WantKey, offer.WantQty, now);
        await EconomyLog.AddAsync(_db_context, g, EconomyLog.ActTrade, 1, now);
        await _db_context.SaveChangesAsync();
        return (TradeOutcome.Done, fromSets, toSets);
    }

    /// <summary>/inventory sell: items for cailloux at ItemCatalog.SellPrice, in one save. The row stays (discovered).</summary>
    public Task<(GiveOutcome Outcome, long Earned, long Balance)> SellAsync(ulong guildId, ulong userId, string key, int quantity) =>
        EconomySave.RetryAsync(_db_context, () => SellOnceAsync(guildId, userId, key, quantity));

    private async Task<(GiveOutcome Outcome, long Earned, long Balance)> SellOnceAsync(ulong guildId, ulong userId, string key, int quantity)
    {
        if (ItemCatalog.ByKey(key) is not { } item) return (GiveOutcome.UnknownItem, 0, 0);
        // Cosmetics are given and traded, never sold: a buy-back price would invite loops.
        if (item.Kind == ItemKind.Cosmetic) return (GiveOutcome.NotSellable, 0, 0);
        if (!await TakeAsync(_db_context, guildId, userId, key, quantity)) return (GiveOutcome.NotEnough, 0, 0);
        var earned = ItemCatalog.SellPrice(item.Rarity) * quantity;
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, userId);
        wallet.Balance += earned;
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.EarnSale, earned, DateTimeOffset.UtcNow);
        await _db_context.SaveChangesAsync();
        return (GiveOutcome.Given, earned, wallet.Balance);
    }

    public Task<long> BalanceAsync(ulong guildId, ulong userId) =>
        _db_context.PebbleWallets.Where(w => w.GuildId == guildId && w.UserId == userId).Select(w => w.Balance).FirstOrDefaultAsync();

    // Everything someone has ever held here (quantity 0 included: discovered).
    public Task<List<InventoryItem>> GetAllAsync(ulong guildId, ulong userId) =>
        _db_context.InventoryItems.Where(i => i.GuildId == guildId && i.UserId == userId).ToListAsync();

    public Task<List<CollectionCompletion>> GetCompletionsAsync(ulong guildId, ulong userId) =>
        _db_context.CollectionCompletions.Where(c => c.GuildId == guildId && c.UserId == userId).ToListAsync();

    /// <summary>
    /// /leaderboard's Collections view: sets completed per person, most first, ties on ascending
    /// user id. All-time sums the totals (CollectionCompletion.Count); a window sums the daily
    /// buckets, filtered in SQL like XpService's windows. The two never agree and aren't meant to.
    /// </summary>
    public async Task<List<(ulong UserId, long Completions)>> GetCollectorRankingAsync(ulong guildId, StatsPeriod period)
    {
        List<(ulong UserId, long Completions)> tallies;
        if (period == StatsPeriod.AllTime)
        {
            var rows = await _db_context.CollectionCompletions.Where(c => c.GuildId == guildId).ToListAsync();
            tallies = rows.GroupBy(c => c.UserId).Select(g => (g.Key, (long)g.Sum(c => c.Count))).ToList();
        }
        else
        {
            var since = AppTime.KeyDaysAgo(period == StatsPeriod.Week ? 6 : 29);
            var rows = await _db_context.CollectionDailyStats.Where(b => b.GuildId == guildId && b.Day >= since).ToListAsync();
            tallies = rows.GroupBy(b => b.UserId).Select(g => (g.Key, (long)g.Sum(b => b.Completions))).ToList();
        }
        return tallies.Where(t => t.Item2 > 0).OrderByDescending(t => t.Item2).ThenBy(t => t.Item1).ToList();
    }
}
