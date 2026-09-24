using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum AdoptOutcome { Adopted, AlreadyHasOne }

public enum CareOutcome { Done, NoPlynling, NotOwner, Dead, Frozen, Wasted, TooPoor }

public enum ThawOutcome { Thawed, NoPlynling, Dead, NotFrozen, StaffOnly }

public enum ResurrectOutcome { Resurrected, NoGrave, AlreadyHasOne }

public sealed record FeedResult(CareOutcome Outcome, Plynling? Plynling, long Price, long Balance);

// EF access for Plynlings — transient. Every read goes through PlynlingLife.Settle
// before returning, so a caller always sees a Plynling as it is *now*: dead if it starved
// since anyone looked, thawed if its self-freeze ran out.
//
// **Feeding touches the wallet too, here, on purpose.** AppDbContext is transient, so
// charging through PebbleService would be a second context and a second SaveChanges — a
// crash between the two would take the cailloux without feeding it. One context, one
// save, the same shape as ShameService.TryVoteAsync.
public class PlynlingService
{
    private readonly AppDbContext _db_context;

    public PlynlingService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    // The owner's living Plynling, brought up to date. If it starved since anyone last
    // looked it comes back dead (and is saved dead), so the caller can say so.
    public async Task<Plynling?> GetCurrentAsync(ulong guildId, ulong ownerId, DateTimeOffset now) =>
        await SettledAsync(await _db_context.Plynlings
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.OwnerId == ownerId && x.DiedAt == null), now);

    // What /plynling view shows: the living one, or else their most recent grave.
    public async Task<Plynling?> GetShownAsync(ulong guildId, ulong ownerId, DateTimeOffset now) =>
        await GetCurrentAsync(guildId, ownerId, now) ?? await GetLatestDeadAsync(guildId, ownerId);

    public async Task<Plynling?> GetByIdAsync(int id, DateTimeOffset now) =>
        await SettledAsync(await _db_context.Plynlings.FirstOrDefaultAsync(x => x.Id == id), now);

    public async Task<Plynling?> GetLatestDeadAsync(ulong guildId, ulong ownerId)
    {
        // Ordered in memory: SQLite cannot translate DateTimeOffset ordering.
        var dead = await _db_context.Plynlings
            .Where(x => x.GuildId == guildId && x.OwnerId == ownerId && x.DiedAt != null)
            .ToListAsync();
        return dead.OrderByDescending(x => x.DiedAt).FirstOrDefault();
    }

    public async Task<(AdoptOutcome Outcome, Plynling? Plynling)> AdoptAsync(
        ulong guildId, ulong ownerId, string name, PlynlingSpecies species, DateTimeOffset now,
        PlynlingGender? gender = null)
    {
        var current = await GetCurrentAsync(guildId, ownerId, now);
        if (current is { DiedAt: null }) return (AdoptOutcome.AlreadyHasOne, current);

        // Rolled here unless given, so PlynlingLife.Create stays pure and the harnesses
        // can pin a gender.
        var plynling = PlynlingLife.Create(guildId, ownerId, name, species, gender ?? PlynlingCatalog.RollGender(), now);
        _db_context.Plynlings.Add(plynling);
        try
        {
            await _db_context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two adoptions raced; the partial unique index let exactly one through.
            return (AdoptOutcome.AlreadyHasOne, null);
        }
        return (AdoptOutcome.Adopted, plynling);
    }

    public async Task<FeedResult> FeedAsync(int plynlingId, ulong actorId, PlynlingFood food, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(food);
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null) return new FeedResult(CareOutcome.NoPlynling, null, info.Price, 0);

        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, plynling.GuildId, actorId);
        CareOutcome? refusal =
            plynling.OwnerId != actorId ? CareOutcome.NotOwner
            : plynling.DiedAt is not null ? CareOutcome.Dead
            : plynling.FrozenAt is not null ? CareOutcome.Frozen
            : PlynlingLife.WouldWaste(plynling, info, now) ? CareOutcome.Wasted
            : wallet.Balance < info.Price ? CareOutcome.TooPoor
            : null;
        if (refusal is { } r) return new FeedResult(r, plynling, info.Price, wallet.Balance);

        wallet.Balance -= info.Price;
        PlynlingLife.Feed(plynling, info, now);
        await _db_context.SaveChangesAsync();          // the money and the meal land together
        return new FeedResult(CareOutcome.Done, plynling, info.Price, wallet.Balance);
    }

    public async Task<(CareOutcome Outcome, Plynling? Plynling)> PetAsync(int plynlingId, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null) return (CareOutcome.NoPlynling, null);
        if (plynling.DiedAt is not null) return (CareOutcome.Dead, plynling);
        if (plynling.FrozenAt is not null) return (CareOutcome.Frozen, plynling);

        PlynlingLife.Pet(plynling, now);
        await _db_context.SaveChangesAsync();
        return (CareOutcome.Done, plynling);
    }

    public async Task<(FreezeOutcome Outcome, Plynling? Plynling)> FreezeAsync(
        ulong guildId, ulong ownerId, bool byStaff, DateTimeOffset now)
    {
        var plynling = await GetCurrentAsync(guildId, ownerId, now);
        if (plynling is null) return (FreezeOutcome.NoPlynling, null);

        // Staff are exempt from the self-freeze rules, but not from reality.
        var blocker = byStaff
            ? (plynling.DiedAt is not null ? FreezeOutcome.Dead
               : plynling.FrozenAt is not null ? FreezeOutcome.AlreadyFrozen
               : (FreezeOutcome?)null)
            : PlynlingLife.SelfFreezeBlocker(plynling, now);
        if (blocker is { } b) return (b, plynling);

        PlynlingLife.Freeze(plynling, now, byStaff);
        await _db_context.SaveChangesAsync();
        return (FreezeOutcome.Frozen, plynling);
    }

    public async Task<(ThawOutcome Outcome, Plynling? Plynling)> ThawAsync(
        ulong guildId, ulong ownerId, bool byStaff, DateTimeOffset now)
    {
        var plynling = await GetCurrentAsync(guildId, ownerId, now);
        if (plynling is null) return (ThawOutcome.NoPlynling, null);
        if (plynling.DiedAt is not null) return (ThawOutcome.Dead, plynling);
        if (plynling.FrozenAt is null) return (ThawOutcome.NotFrozen, plynling);
        // A staff freeze is lifted by staff only; an owner may end their own self-freeze.
        if (plynling.FrozenByStaff && !byStaff) return (ThawOutcome.StaffOnly, plynling);

        PlynlingLife.Thaw(plynling, now);
        await _db_context.SaveChangesAsync();
        return (ThawOutcome.Thawed, plynling);
    }

    // Reaches the shown Plynling — living, or else the latest grave — because a grave
    // shows its name publicly too, and an offensive one needs fixing there as well.
    public async Task<(Plynling? Plynling, string OldName)> RenameAsync(
        ulong guildId, ulong ownerId, string name, DateTimeOffset now)
    {
        var plynling = await GetShownAsync(guildId, ownerId, now);
        if (plynling is null) return (null, string.Empty);

        var oldName = plynling.Name;
        plynling.Name = name;
        await _db_context.SaveChangesAsync();
        return (plynling, oldName);
    }

    public async Task<(ResurrectOutcome Outcome, Plynling? Plynling)> ResurrectAsync(
        ulong guildId, ulong ownerId, DateTimeOffset now)
    {
        var current = await GetCurrentAsync(guildId, ownerId, now);
        if (current is { DiedAt: null }) return (ResurrectOutcome.AlreadyHasOne, current);

        // `current`, if set, is one that just starved on this very read — the latest grave.
        var grave = current ?? await GetLatestDeadAsync(guildId, ownerId);
        if (grave is null) return (ResurrectOutcome.NoGrave, null);

        PlynlingLife.Resurrect(grave, now);
        try
        {
            await _db_context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (ResurrectOutcome.AlreadyHasOne, null);   // an adoption won the race
        }
        return (ResurrectOutcome.Resurrected, grave);
    }

    public async Task<List<Plynling>> GetGraveyardAsync(ulong guildId, ulong? ownerId, DateTimeOffset now)
    {
        // Settle the living first, so anything that starved since the last sweep is
        // already in the ground when someone opens the graveyard.
        var living = await _db_context.Plynlings.Where(x => x.GuildId == guildId && x.DiedAt == null).ToListAsync();
        var changed = false;
        foreach (var plynling in living)
            changed |= PlynlingLife.Settle(plynling, now);
        if (changed) await _db_context.SaveChangesAsync();

        var query = _db_context.Plynlings.Where(x => x.GuildId == guildId && x.DiedAt != null);
        if (ownerId is { } owner) query = query.Where(x => x.OwnerId == owner);
        return await query.ToListAsync();
    }

    // Every Plynling the hourly sweep has to look at: the living, and deaths not yet announced.
    public async Task<List<Plynling>> GetSweepBatchAsync() =>
        await _db_context.Plynlings.Where(x => x.DiedAt == null || !x.DeathAnnounced).ToListAsync();

    public Task SaveAsync() => _db_context.SaveChangesAsync();

    private async Task<Plynling?> SettledAsync(Plynling? plynling, DateTimeOffset now)
    {
        if (plynling is not null && PlynlingLife.Settle(plynling, now))
            await _db_context.SaveChangesAsync();
        return plynling;
    }
}
