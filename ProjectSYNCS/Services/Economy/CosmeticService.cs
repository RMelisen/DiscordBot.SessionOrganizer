using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum CosmeticOutcome { Done, UnknownItem, AlreadyOwned, TooPoor, NotInShop, MissingIngredients, NotOwned, NoPlynling }

// What a buy or a craft did: Worn says it was also put on (« …et porter »), which can fail on its own
// — the Plynling died since the screen was drawn — while the purchase stands.
// Change is what wearing did to the two accessory slots (null for another slot, or when not worn).
public readonly record struct CosmeticResult(CosmeticOutcome Outcome, long Balance, bool Worn = false, AccessoryChange? Change = null);

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
    /// drawn from, so a button left over from last week cannot buy what is no longer on sale. With
    /// <paramref name="wear"/>, it goes on the person's living Plynling in the same save.
    /// </summary>
    public Task<CosmeticResult> BuyAsync(ulong guildId, ulong userId, string key, int week, DateTimeOffset now, bool wear = false) =>
        EconomySave.RetryAsync(_db_context, () => BuyOnceAsync(guildId, userId, key, week, now, wear));

    private async Task<CosmeticResult> BuyOnceAsync(ulong guildId, ulong userId, string key, int week, DateTimeOffset now, bool wear)
    {
        if (CosmeticCatalog.ByKey(key) is not { } cosmetic) return new(CosmeticOutcome.UnknownItem, 0);
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, userId);
        if (week != CosmeticCatalog.WeekKey(now) || !CosmeticCatalog.Shop(now).Contains(cosmetic))
            return new(CosmeticOutcome.NotInShop, wallet.Balance);
        if (await InventoryService.CountAsync(_db_context, guildId, userId, key) > 0) return new(CosmeticOutcome.AlreadyOwned, wallet.Balance);
        if (wallet.Balance < cosmetic.Price) return new(CosmeticOutcome.TooPoor, wallet.Balance);

        wallet.Balance -= cosmetic.Price;
        await InventoryService.AddAsync(_db_context, guildId, userId, key, 1, now);
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.SpendCosmetic, cosmetic.Price, now);
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.CosBought, 1, now);
        var (worn, change) = wear ? await PutOnAsync(guildId, userId, cosmetic) : (Worn: false, Change: (AccessoryChange?)null);
        await _db_context.SaveChangesAsync();
        return new(CosmeticOutcome.Done, wallet.Balance, worn, change);
    }

    /// <summary>
    /// Crafts a cosmetic: every ingredient and the cailloux, or nothing at all. With
    /// <paramref name="wear"/>, it goes on the person's living Plynling in the same save.
    /// </summary>
    public Task<CosmeticResult> CraftAsync(ulong guildId, ulong userId, string key, DateTimeOffset now, bool wear = false) =>
        EconomySave.RetryAsync(_db_context, () => CraftOnceAsync(guildId, userId, key, now, wear));

    private async Task<CosmeticResult> CraftOnceAsync(ulong guildId, ulong userId, string key, DateTimeOffset now, bool wear)
    {
        if (CosmeticCatalog.ByKey(key) is not { Source: CosmeticSource.Crafted } cosmetic) return new(CosmeticOutcome.UnknownItem, 0);
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, userId);
        if (await InventoryService.CountAsync(_db_context, guildId, userId, key) > 0) return new(CosmeticOutcome.AlreadyOwned, wallet.Balance);
        // Every item an ingredient may be (a named one, or a set's communs), counted once.
        var held = new Dictionary<string, int>();
        foreach (var item in cosmetic.Recipe.SelectMany(r => Recipes.Items(r.ItemKey)).Select(i => i.Key).Distinct())
            held[item] = await InventoryService.CountAsync(_db_context, guildId, userId, item);
        if (!Recipes.ItemsReady(cosmetic, held)) return new(CosmeticOutcome.MissingIngredients, wallet.Balance);
        if (wallet.Balance < cosmetic.Price) return new(CosmeticOutcome.TooPoor, wallet.Balance);

        foreach (var (ingredient, count) in cosmetic.Recipe)
            foreach (var (itemKey, n) in Recipes.Take(ingredient, count, held))
            {
                await InventoryService.TakeAsync(_db_context, guildId, userId, itemKey, n);
                held[itemKey] -= n;   // a later ingredient sees what this one took
            }
        wallet.Balance -= cosmetic.Price;
        await InventoryService.AddAsync(_db_context, guildId, userId, key, 1, now);
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.SpendCraft, cosmetic.Price, now);
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.CosCrafted, 1, now);
        var (worn, change) = wear ? await PutOnAsync(guildId, userId, cosmetic) : (Worn: false, Change: (AccessoryChange?)null);
        await _db_context.SaveChangesAsync();
        return new(CosmeticOutcome.Done, wallet.Balance, worn, change);
    }

    // Puts what was just bought or made on the living Plynling, unsaved: the caller's save carries it.
    // An accessory goes where it fits (AccessoryFit.TryOn), in the style the Plynling remembers for it.
    private async Task<(bool Worn, AccessoryChange? Change)> PutOnAsync(ulong guildId, ulong userId, CosmeticInfo cosmetic)
    {
        var plynling = await _db_context.Plynlings.FirstOrDefaultAsync(p => p.GuildId == guildId && p.OwnerId == userId && p.DiedAt == null);
        if (plynling is null) return (false, null);
        if (cosmetic.Slot != CosmeticSlot.Accessory)
        {
            CosmeticSlots.Set(plynling, cosmetic.Slot, cosmetic.Key);
            return (true, null);
        }
        var (first, second) = CosmeticSlots.AccessorySlots(plynling);
        var saved = SavedStyles.Parse(plynling.SavedAccessoryStyles);
        var change = AccessoryFit.TryOn(first, second, new WornAccessory(cosmetic.Key, saved.GetValueOrDefault(cosmetic.Key)));
        CosmeticSlots.Apply(plynling, change);
        return (true, change);
    }

    /// <summary>
    /// Puts a cosmetic on the person's living Plynling, in its slot — or takes that slot off when
    /// <paramref name="key"/> is null. Wearing needs at least one in the inventory and uses none.
    /// For the accessory slot <paramref name="picker"/> (0 or 1) says which of the two pickers
    /// chose, and the third value says what happened to the two slots (null for any other slot).
    /// </summary>
    public async Task<(CosmeticOutcome Outcome, Plynling? Plynling, AccessoryChange? Change)> WearAsync(
        ulong guildId, ulong userId, CosmeticSlot slot, string? key, DateTimeOffset now, int picker = 0)
    {
        var plynling = await _db_context.Plynlings.FirstOrDefaultAsync(p => p.GuildId == guildId && p.OwnerId == userId && p.DiedAt == null);
        if (plynling is null) return (CosmeticOutcome.NoPlynling, null, null);
        if (picker is not (0 or 1)) return (CosmeticOutcome.UnknownItem, plynling, null);
        if (key is not null)
        {
            if (CosmeticCatalog.ByKey(key) is not { } cosmetic || cosmetic.Slot != slot) return (CosmeticOutcome.UnknownItem, plynling, null);
            if (await InventoryService.CountAsync(_db_context, guildId, userId, key) < 1) return (CosmeticOutcome.NotOwned, plynling, null);
        }
        AccessoryChange? change = null;
        if (slot == CosmeticSlot.Accessory)
        {
            var (first, second) = CosmeticSlots.AccessorySlots(plynling);
            var saved = SavedStyles.Parse(plynling.SavedAccessoryStyles);
            change = AccessoryFit.Pick(first, second, picker, key is null ? null : new WornAccessory(key, saved.GetValueOrDefault(key)));
            CosmeticSlots.Apply(plynling, change);
        }
        else CosmeticSlots.Set(plynling, slot, key);
        await _db_context.SaveChangesAsync();
        return (CosmeticOutcome.Done, plynling, change);
    }

    /// <summary>
    /// Sets one choice (colour, position, side) of an accessory the person's living Plynling wears,
    /// in either slot — free, like wearing. Refused (<see cref="CosmeticOutcome.UnknownItem"/>) when
    /// that accessory is no longer worn (a screen left open across a change) or the option isn't one of
    /// its. A side the other accessory had flips that one (the change says which).
    /// </summary>
    public async Task<(CosmeticOutcome Outcome, Plynling? Plynling, AccessoryChange? Change)> StyleAsync(
        ulong guildId, ulong userId, string accessoryKey, int choice, string option)
    {
        var plynling = await _db_context.Plynlings.FirstOrDefaultAsync(p => p.GuildId == guildId && p.OwnerId == userId && p.DiedAt == null);
        if (plynling is null) return (CosmeticOutcome.NoPlynling, null, null);
        var (first, second) = CosmeticSlots.AccessorySlots(plynling);
        var at = first?.Key == accessoryKey ? 0 : second?.Key == accessoryKey ? 1 : -1;
        var choices = AccessoryStyles.Choices(accessoryKey);
        if (at < 0 || choice < 0 || choice >= choices.Count || choices[choice].Options.All(o => o.Key != option))
            return (CosmeticOutcome.UnknownItem, plynling, null);
        var style = AccessoryStyles.With(accessoryKey, at == 0 ? first!.Style : second!.Style, choice, option);
        var change = AccessoryFit.Restyle(first, second, at, style ?? "");
        CosmeticSlots.Apply(plynling, change);
        await _db_context.SaveChangesAsync();
        return (CosmeticOutcome.Done, plynling, change);
    }

    // Everything the person holds, by key (rows at 0 included: the screen reads counts), and their
    // balance — what the cosmetics screen needs besides the Plynling.
    public async Task<(Dictionary<string, int> Held, long Balance)> HeldAsync(ulong guildId, ulong userId)
    {
        var held = await _db_context.InventoryItems
            .Where(i => i.GuildId == guildId && i.UserId == userId)
            .ToDictionaryAsync(i => i.Key, i => i.Quantity);
        var balance = await _db_context.PebbleWallets
            .Where(w => w.GuildId == guildId && w.UserId == userId)
            .Select(w => w.Balance)
            .FirstOrDefaultAsync();
        return (held, balance);
    }
}
