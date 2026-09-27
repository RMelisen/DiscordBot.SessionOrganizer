using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// A snapshot of one guild's economy, for /admin stats. Only what is stored — nothing records
// flows over time (cailloux spent per day), so none are shown.
public sealed record EconomyStats(
    long TotalCailloux, int Wallets, IReadOnlyList<(ulong UserId, long Balance)> Richest,
    int Alive, int Frozen, int Graves, IReadOnlyList<(PlynlingSpecies Species, int Count)> LivingBySpecies,
    IReadOnlyList<(CollectionSet Set, int Completions)> Completions, int Discoveries,
    int CosmeticsHeld, int CosmeticsWorn, IReadOnlyList<(CosmeticInfo Cosmetic, int Holders)> PopularCosmetics)
{
    public long AverageCailloux => Wallets == 0 ? 0 : (long)Math.Round(TotalCailloux / (double)Wallets);
}

// Transient, it wraps AppDbContext. Reads only: the Plynlings are settled in memory so a death
// nobody has noticed yet counts as a grave, but nothing is saved — the sweep and the commands
// own writing that.
public class AdminStatsService
{
    public const int RichestShown = 5;
    public const int PopularShown = 3;

    private readonly AppDbContext _db_context;

    public AdminStatsService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    public async Task<EconomyStats> GetAsync(ulong guildId, DateTimeOffset now)
    {
        var wallets = await _db_context.PebbleWallets.AsNoTracking().Where(w => w.GuildId == guildId)
            .Select(w => new { w.UserId, w.Balance }).ToListAsync();
        var richest = wallets.OrderByDescending(w => w.Balance).ThenBy(w => w.UserId).Take(RichestShown)
            .Select(w => (w.UserId, w.Balance)).ToList();

        var plynlings = await _db_context.Plynlings.AsNoTracking().Where(p => p.GuildId == guildId).ToListAsync();
        foreach (var p in plynlings) PlynlingLife.Settle(p, now);          // in memory only: AsNoTracking, never saved
        var living = plynlings.Where(p => p.DiedAt is null).ToList();
        var bySpecies = living.GroupBy(p => p.Species).OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Select(g => (g.Key, g.Count())).ToList();
        var worn = living.Sum(p => Enum.GetValues<CosmeticSlot>().Count(s => CosmeticSlots.Get(p, s) is not null));

        var completions = (await _db_context.CollectionCompletions.AsNoTracking().Where(c => c.GuildId == guildId)
                .Select(c => c.SetKey).ToListAsync())
            .GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count());

        var items = await _db_context.InventoryItems.AsNoTracking().Where(i => i.GuildId == guildId)
            .Select(i => new { i.UserId, i.Key, i.Quantity }).ToListAsync();
        var discoveries = items.Count(i => i.Key.StartsWith("col."));
        var cosmetics = items.Where(i => i.Quantity > 0 && CosmeticCatalog.ByKey(i.Key) is not null).ToList();
        var popular = cosmetics.GroupBy(i => i.Key)
            .Select(g => (Cosmetic: CosmeticCatalog.ByKey(g.Key)!, Holders: g.Select(i => i.UserId).Distinct().Count()))
            .OrderByDescending(x => x.Holders).ThenBy(x => CosmeticCatalog.All.ToList().IndexOf(x.Cosmetic))
            .Take(PopularShown).ToList();

        return new EconomyStats(
            wallets.Sum(w => w.Balance), wallets.Count, richest,
            living.Count, living.Count(p => p.FrozenAt is not null), plynlings.Count - living.Count, bySpecies,
            ItemCatalog.Sets.Select(s => (s, completions.GetValueOrDefault(s.Key))).ToList(), discoveries,
            cosmetics.Sum(i => i.Quantity), worn, popular);
    }
}
