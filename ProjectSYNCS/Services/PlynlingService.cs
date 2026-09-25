using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum AdoptOutcome { Adopted, AlreadyHasOne }

public enum CareOutcome { Done, NoPlynling, Dead, Frozen, Wasted, TooPoor, Asleep, Sulking }

public enum ThawOutcome { Thawed, NoPlynling, Dead, NotFrozen, StaffOnly }

public enum ResurrectOutcome { Resurrected, NoGrave, AlreadyHasOne }

// What an accepted visit did: the scene, the bond before and after, any confession, and the
// happiness both Plynlings got (negative between enemies). Badges per Plynling.
public sealed record VisitOutcome(
    Plynling Visitor, Plynling Host,
    IReadOnlyList<BadgeInfo> VisitorBadges, IReadOnlyList<BadgeInfo> HostBadges,
    bool GoodScene, PlynlingBond Before, PlynlingBond After, Confession Confession, double Happiness);

public sealed record FeedResult(CareOutcome Outcome, Plynling? Plynling, long Price, long Balance,
    IReadOnlyList<BadgeInfo>? Badges = null, double MealFactor = 1.0);

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
        await AddMomentAsync(plynling, JournalKind.Adopted, null, now);  // needs its id: after the first save
        await _db_context.SaveChangesAsync();
        return (AdoptOutcome.Adopted, plynling);
    }

    public async Task<FeedResult> FeedAsync(int plynlingId, ulong actorId, PlynlingFood food, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(food);
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null) return new FeedResult(CareOutcome.NoPlynling, null, info.Price, 0);

        // The feeder pays, from their own wallet — double when it is not their Plynling.
        var price = PlynlingLife.FeedPrice(info, isOwner: plynling.OwnerId == actorId);
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, plynling.GuildId, actorId);
        CareOutcome? refusal =
            plynling.DiedAt is not null ? CareOutcome.Dead
            : plynling.FrozenAt is not null ? CareOutcome.Frozen
            : PlynlingLife.IsSulking(plynling, now) ? CareOutcome.Sulking
            : PlynlingLife.WouldWaste(plynling, info, now) ? CareOutcome.Wasted
            : wallet.Balance < price ? CareOutcome.TooPoor
            : null;
        if (refusal is { } r) return new FeedResult(r, plynling, price, wallet.Balance);

        var wasStarving = PlynlingLife.HungerAt(plynling, now) < PlynlingLife.StarvingBelow;
        var factor = PlynlingLife.MealFactor(plynling, now);
        wallet.Balance -= price;
        PlynlingLife.Feed(plynling, info, now);
        plynling.Meals++;
        if (plynling.Meals == 1) await AddMomentAsync(plynling, JournalKind.FirstMeal, null, now);
        if (plynling.OwnerId != actorId && ++plynling.FedByOthers == 1)
            await AddMomentAsync(plynling, JournalKind.FedByFriend, actorId.ToString(), now);
        var badges = await AwardAsync(plynling, now, wasStarving ? BadgeEvent.SavedFromStarving : BadgeEvent.None);
        await _db_context.SaveChangesAsync();          // the money, the meal and any badge land together
        return new FeedResult(CareOutcome.Done, plynling, price, wallet.Balance, badges, factor);
    }

    public async Task<(CareOutcome Outcome, Plynling? Plynling, IReadOnlyList<BadgeInfo> Badges)> PetAsync(int plynlingId, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null) return (CareOutcome.NoPlynling, null, NoBadges);
        if (plynling.DiedAt is not null) return (CareOutcome.Dead, plynling, NoBadges);
        if (plynling.FrozenAt is not null) return (CareOutcome.Frozen, plynling, NoBadges);
        if (PlynlingLife.IsAsleep(now)) return (CareOutcome.Asleep, plynling, NoBadges);   // feeding still works

        PlynlingLife.Pet(plynling, now);
        plynling.Pets++;
        var badges = await AwardAsync(plynling, now);
        await _db_context.SaveChangesAsync();
        return (CareOutcome.Done, plynling, badges);
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
        await AddMomentAsync(plynling, JournalKind.Frozen, null, now);
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
        await AddMomentAsync(plynling, JournalKind.Thawed, null, now);
        await _db_context.SaveChangesAsync();
        return (ThawOutcome.Thawed, plynling);
    }

    // Reaches the shown Plynling — living, or else the latest grave — because a grave
    // shows its name publicly too, and an offensive one needs fixing there as well.
    // The end of a /plynling play game: the happiness, the counts and — on a win — the
    // player's cailloux land in one save. Null when the Plynling can no longer be played
    // with (it died, was frozen or abandoned mid-game): then nothing is paid.
    public async Task<(Plynling? Plynling, long Balance, IReadOnlyList<BadgeInfo> Badges)> FinishPlayAsync(
        int plynlingId, ulong ownerId, bool won, long pebbles, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null || plynling.OwnerId != ownerId || plynling.DiedAt is not null || plynling.FrozenAt is not null)
            return (null, 0, NoBadges);

        PlynlingLife.Play(plynling, now, won);
        if (won && plynling.PlaysWon == 1) await AddMomentAsync(plynling, JournalKind.FirstWin, null, now);
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, plynling.GuildId, ownerId);
        if (won && pebbles > 0) wallet.Balance += pebbles;
        var badges = await AwardAsync(plynling, now);                   // pays into the same wallet
        await _db_context.SaveChangesAsync();
        return (plynling, wallet.Balance, badges);
    }

    // A /plynling visit accepted: the scene plays out on their relationship — affinity, bond, a
    // confession perhaps — and both are cheered (or not) by what they are to each other, all in
    // one save. Null when either can no longer take part.
    public async Task<VisitOutcome?> VisitAsync(int visitorId, int hostId, DateTimeOffset now, Random? rng = null)
    {
        rng ??= Random.Shared;
        var visitor = await GetByIdAsync(visitorId, now);
        var host = await GetByIdAsync(hostId, now);
        if (visitor is null || host is null || visitor.Id == host.Id) return null;
        if (visitor.DiedAt is not null || host.DiedAt is not null) return null;
        if (visitor.FrozenAt is not null || host.FrozenAt is not null) return null;

        var (lo, hi) = visitor.Id < host.Id ? (visitor.Id, host.Id) : (host.Id, visitor.Id);
        var relation = await _db_context.PlynlingRelations.FirstOrDefaultAsync(r => r.PlynlingAId == lo && r.PlynlingBId == hi);
        if (relation is null)
        {
            relation = new PlynlingRelation { PlynlingAId = lo, PlynlingBId = hi, Bond = PlynlingBond.Acquaintances, Since = now };
            _db_context.PlynlingRelations.Add(relation);
        }

        var before = relation.Bond;
        var compatibility = PlynlingBonds.Compatibility(lo, hi);
        var (good, delta) = PlynlingBonds.RollScene(compatibility, before, rng);
        relation.Affinity = Math.Clamp(relation.Affinity + delta, -100, 100);
        relation.Meetings++;

        var confession = Confession.None;
        if (good && PlynlingBonds.CanConfess(PlynlingBonds.BondFor(relation.Affinity, before), relation.Affinity, visitor, host,
                await InCoupleAsync(visitor.Id, host.Id) || await InCoupleAsync(host.Id, visitor.Id)))
            confession = PlynlingBonds.RollConfession(compatibility, rng);
        if (confession == Confession.Refused)
            relation.Affinity = Math.Clamp(relation.Affinity - PlynlingBonds.HeartbreakLoss, -100, 100);

        var after = confession == Confession.Accepted ? PlynlingBond.Lovers : PlynlingBonds.BondFor(relation.Affinity, before);
        if (after != before)
        {
            relation.Bond = after;
            relation.Since = now;
        }

        var happiness = PlynlingBonds.VisitHappiness(after);
        PlynlingLife.Visit(visitor, now, happiness);
        PlynlingLife.Visit(host, now, happiness);
        await AddMomentAsync(visitor, JournalKind.Visited, host.Name, now);
        await AddMomentAsync(host, JournalKind.Hosted, visitor.Name, now);
        if (confession == Confession.Refused)
        {
            foreach (var (p, other) in new[] { (visitor, host), (host, visitor) })
            {
                PlynlingLife.Sadden(p, now, PlynlingBonds.HeartbreakSadness);
                await AddMomentAsync(p, JournalKind.Heartbroken, other.Name, now);
            }
        }

        var evt = BadgeEvent.None;
        if (after != before && BondMoment(before, after) is { } kind)
        {
            await AddMomentAsync(visitor, kind, host.Name, now);
            await AddMomentAsync(host, kind, visitor.Name, now);
            evt = after switch
            {
                PlynlingBond.Friends => BadgeEvent.BecameFriends,
                PlynlingBond.BestFriends => BadgeEvent.BecameBestFriends,
                PlynlingBond.Lovers => BadgeEvent.BecameLovers,
                _ => BadgeEvent.None,
            };
            if (Closeness(after) > Closeness(before)) evt = BadgeEvent.None;     // drifting apart earns nothing
        }
        var visitorBadges = await AwardAsync(visitor, now, evt);
        var hostBadges = await AwardAsync(host, now, evt);
        await _db_context.SaveChangesAsync();
        return new VisitOutcome(visitor, host, visitorBadges, hostBadges, good, before, after, confession, happiness);
    }

    private static int Closeness(PlynlingBond bond) => PlynlingBonds.Closeness(bond);

    // The journal moment a change of bond writes — getting closer, falling out, breaking up —
    // or none for a quiet drift (best friends back to friends, friends back to acquaintances).
    private static JournalKind? BondMoment(PlynlingBond before, PlynlingBond after) =>
        before == PlynlingBond.Lovers ? JournalKind.BrokeUp
        : after switch
        {
            PlynlingBond.Lovers => JournalKind.BecameLovers,
            PlynlingBond.BestFriends when Closeness(after) < Closeness(before) => JournalKind.BecameBestFriends,
            PlynlingBond.Friends when Closeness(after) < Closeness(before) => JournalKind.BecameFriends,
            PlynlingBond.Rivals when before != PlynlingBond.Enemies => JournalKind.BecameRivals,
            PlynlingBond.Enemies => JournalKind.BecameEnemies,
            _ => null,
        };

    // Whether this Plynling has a living partner other than `except`. A partner who died no longer
    // counts, or the survivor could never love again.
    private async Task<bool> InCoupleAsync(int plynlingId, int except)
    {
        var partners = await _db_context.PlynlingRelations
            .Where(r => r.Bond == PlynlingBond.Lovers && (r.PlynlingAId == plynlingId || r.PlynlingBId == plynlingId))
            .Select(r => r.PlynlingAId == plynlingId ? r.PlynlingBId : r.PlynlingAId)
            .ToListAsync();
        partners.Remove(except);
        return partners.Count > 0
               && await _db_context.Plynlings.AnyAsync(p => partners.Contains(p.Id) && p.DiedAt == null);
    }

    // Its best friends and partner, living, grieve it: happiness down to the grief ceiling, and a
    // moment in their journal. Not saved — the caller's save carries it.
    private async Task GrieveForAsync(Plynling gone, DateTimeOffset now)
    {
        var close = await _db_context.PlynlingRelations
            .Where(r => (r.PlynlingAId == gone.Id || r.PlynlingBId == gone.Id)
                        && (r.Bond == PlynlingBond.BestFriends || r.Bond == PlynlingBond.Lovers))
            .Select(r => r.PlynlingAId == gone.Id ? r.PlynlingBId : r.PlynlingAId)
            .ToListAsync();
        foreach (var id in close)
        {
            var mourner = await GetByIdAsync(id, now);
            if (mourner is null || mourner.DiedAt is not null) continue;
            PlynlingLife.Grieve(mourner, now);
            await AddMomentAsync(mourner, JournalKind.Grieving, gone.Name, now);
        }
    }

    // The card's « en couple avec » line: its living partner, or null — none for a dead Plynling,
    // and a partner who died is no longer shown (one partner at most, so the first living one).
    public async Task<Plynling?> GetPartnerAsync(Plynling p)
    {
        if (p.DiedAt is not null) return null;
        var ids = await _db_context.PlynlingRelations
            .Where(r => r.Bond == PlynlingBond.Lovers && (r.PlynlingAId == p.Id || r.PlynlingBId == p.Id))
            .Select(r => r.PlynlingAId == p.Id ? r.PlynlingBId : r.PlynlingAId)
            .ToListAsync();
        return ids.Count == 0 ? null
            : await _db_context.Plynlings.FirstOrDefaultAsync(x => ids.Contains(x.Id) && x.DiedAt == null);
    }

    // /plynling relations and the journal: everyone it has met, with the other Plynling.
    public async Task<List<(PlynlingRelation Relation, Plynling Other)>> GetRelationsAsync(int plynlingId)
    {
        var rows = await _db_context.PlynlingRelations
            .Where(r => r.PlynlingAId == plynlingId || r.PlynlingBId == plynlingId)
            .ToListAsync();
        var ids = rows.Select(r => r.PlynlingAId == plynlingId ? r.PlynlingBId : r.PlynlingAId).ToList();
        var others = await _db_context.Plynlings.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        return rows
            .Where(r => others.ContainsKey(r.PlynlingAId == plynlingId ? r.PlynlingBId : r.PlynlingAId))
            .Select(r => (r, others[r.PlynlingAId == plynlingId ? r.PlynlingBId : r.PlynlingAId]))
            .ToList();
    }

    // /plynling abandon: the owner's living Plynling leaves for good — the row is deleted,
    // so it never reaches the graveyard and cannot be resurrected. Returns what was removed
    // (for the announcement), or null when there was nothing of theirs to abandon.
    public async Task<Plynling?> AbandonAsync(int plynlingId, ulong ownerId, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null || plynling.OwnerId != ownerId || plynling.DiedAt is not null) return null;

        await GrieveForAsync(plynling, now);          // before the relations go with it
        _db_context.Plynlings.Remove(plynling);
        await _db_context.SaveChangesAsync();
        return plynling;
    }

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
        await AddMomentAsync(grave, JournalKind.Resurrected, null, now);
        await AwardAsync(grave, now, BadgeEvent.Resurrected);
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

    // /plynling list: every living Plynling in the guild, settled first, so one that starved
    // since the last sweep is not listed as alive.
    public async Task<List<Plynling>> GetLivingAsync(ulong guildId, DateTimeOffset now)
    {
        var living = await _db_context.Plynlings.Where(x => x.GuildId == guildId && x.DiedAt == null).ToListAsync();
        var changed = false;
        foreach (var plynling in living)
            changed |= PlynlingLife.Settle(plynling, now);
        if (changed) await _db_context.SaveChangesAsync();
        return living.Where(x => x.DiedAt is null).ToList();
    }

    // Every Plynling the hourly sweep has to look at: the living, and deaths not yet announced.
    public async Task<List<Plynling>> GetSweepBatchAsync() =>
        await _db_context.Plynlings.Where(x => x.DiedAt == null || !x.DeathAnnounced).ToListAsync();

    public Task SaveAsync() => _db_context.SaveChangesAsync();

    // The happy gift, when its owner looks: the day's single draw, spent win or lose and saved
    // with the cailloux it found. Returns what it found (0: nothing, or no draw at all).
    public async Task<long> TryGiftAsync(Plynling p, ulong viewerId, DateTimeOffset now, Random rng)
    {
        if (viewerId != p.OwnerId || !PlynlingLife.CanDrawGift(p, now)) return 0;
        p.LastGiftDay = AppTime.DayKey(now);
        var found = PlynlingLife.GiftDraw(rng);
        if (found > 0)
        {
            var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, p.GuildId, p.OwnerId);
            wallet.Balance += found;
        }
        await _db_context.SaveChangesAsync();
        return found;
    }

    // /plynling journal: its badges and its moments, newest first (ordered in memory — SQLite
    // cannot order by a DateTimeOffset).
    public async Task<(List<PlynlingBadge> Badges, List<PlynlingJournalEntry> Moments)> GetJournalAsync(int plynlingId)
    {
        var badges = await _db_context.PlynlingBadges.Where(b => b.PlynlingId == plynlingId).ToListAsync();
        var moments = (await _db_context.PlynlingJournalEntries.Where(e => e.PlynlingId == plynlingId).ToListAsync())
            .OrderByDescending(e => e.At).ThenByDescending(e => e.Id).ToList();
        return (badges, moments);
    }

    // What only time earns, for the hourly sweep: the « est devenu… » moments — dated when the
    // stage was reached, so a Plynling that already existed when the journal shipped gets its
    // past written in — and any badge it now qualifies for (the age ones, and on ship day the
    // counts). Not saved: the sweep's own save carries it.
    public async Task ProgressAsync(Plynling p, DateTimeOffset now)
    {
        if (p.DiedAt is not null) return;
        var written = (await _db_context.PlynlingJournalEntries
                .Where(e => e.PlynlingId == p.Id && e.Kind == JournalKind.GrewUp)
                .Select(e => e.Detail)
                .ToListAsync())
            .ToHashSet();
        var age = PlynlingLife.Age(p, now);
        foreach (var stage in new[] { PlynlingStage.Teen, PlynlingStage.Adult, PlynlingStage.Elder })
        {
            var start = PlynlingLife.StageStart(stage);
            if (age < start || written.Contains(stage.ToString())) continue;
            // As long ago as it has lived past the threshold — never before it was adopted.
            var at = now - (age - start);
            await AddMomentAsync(p, JournalKind.GrewUp, stage.ToString(), at < p.AdoptedAt ? p.AdoptedAt : at);
        }
        await AwardAsync(p, now);
    }

    // The sweep, announcing a death: the journal's last moment, dated when it died. Not saved.
    // Its best friends and partner grieve it too.
    public async Task JournalDeathAsync(Plynling p)
    {
        if (p.DiedAt is not { } died) return;
        await AddMomentAsync(p, JournalKind.Died, null, died);
        await GrieveForAsync(p, died);
    }

    // ---- badges and the journal. Neither helper saves: what they add rides the caller's save,
    // so an action, its moments, its badges and their cailloux land together or not at all.

    public const int JournalCap = 100;
    private static readonly IReadOnlyList<BadgeInfo> NoBadges = Array.Empty<BadgeInfo>();

    // Adds a moment, then trims this Plynling's journal to JournalCap, oldest first — counting
    // moments added earlier in this same unit of work, so several in one save cannot overshoot.
    private async Task AddMomentAsync(Plynling p, JournalKind kind, string? detail, DateTimeOffset at)
    {
        _db_context.PlynlingJournalEntries.Add(new PlynlingJournalEntry { PlynlingId = p.Id, At = at, Kind = kind, Detail = detail });

        var stored = await _db_context.PlynlingJournalEntries.Where(e => e.PlynlingId == p.Id).ToListAsync();
        var kept = stored.Where(e => _db_context.Entry(e).State != EntityState.Deleted).ToList();
        var pending = _db_context.ChangeTracker.Entries<PlynlingJournalEntry>()
            .Count(e => e.State == EntityState.Added && e.Entity.PlynlingId == p.Id);
        var over = kept.Count + pending - JournalCap;
        if (over > 0) _db_context.PlynlingJournalEntries.RemoveRange(kept.OrderBy(e => e.At).ThenBy(e => e.Id).Take(over));
    }

    // Awards every badge this Plynling now qualifies for and has not earned — each written once
    // (the unique index backs it), journaled, and paid to the owner. Returns what it awarded.
    private async Task<IReadOnlyList<BadgeInfo>> AwardAsync(Plynling p, DateTimeOffset now, BadgeEvent evt = BadgeEvent.None)
    {
        var earned = (await _db_context.PlynlingBadges.Where(b => b.PlynlingId == p.Id).Select(b => b.Key).ToListAsync())
            .Concat(_db_context.PlynlingBadges.Local.Where(b => b.PlynlingId == p.Id).Select(b => b.Key))
            .ToHashSet();
        var fresh = PlynlingBadges.Newly(p, earned, now, evt);
        if (fresh.Count == 0) return NoBadges;

        foreach (var badge in fresh)
        {
            _db_context.PlynlingBadges.Add(new PlynlingBadge { PlynlingId = p.Id, Key = badge.Key, EarnedAt = now });
            await AddMomentAsync(p, JournalKind.Badge, badge.Key, now);
        }
        var reward = fresh.Sum(b => b.Reward);
        if (reward > 0)
        {
            var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, p.GuildId, p.OwnerId);
            wallet.Balance += reward;
        }
        return fresh;
    }

    private async Task<Plynling?> SettledAsync(Plynling? plynling, DateTimeOffset now)
    {
        if (plynling is not null && PlynlingLife.Settle(plynling, now))
            await _db_context.SaveChangesAsync();
        return plynling;
    }
}
