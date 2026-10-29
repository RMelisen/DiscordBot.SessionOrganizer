using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum AdoptOutcome { Adopted, AlreadyHasOne }

public enum CureOutcome { Cured, NoPlynling, Dead, NotSick }

public enum CareOutcome { Done, NoPlynling, Dead, Frozen, Wasted, TooPoor, Asleep, Sulking, TooSoon, NotOwner, NotSick, AlreadyTreated }

// The happy gift: cailloux, or an item instead — never both. Nothing when both are empty.
public sealed record GiftResult(long Cailloux, ItemFind? Find)
{
    public static readonly GiftResult None = new(0, null);
    public bool Any => Cailloux > 0 || Find is not null;
}

// /plynling forage: what it brought back, or why it could not go (ReadyAt with TooSoon).
public sealed record ForageResult(CareOutcome Outcome, Plynling? Plynling, ItemFind? Find = null, DateTimeOffset? ReadyAt = null);

public enum ThawOutcome { Thawed, NoPlynling, Dead, NotFrozen, StaffOnly }

public enum ResurrectOutcome { Resurrected, NoGrave, AlreadyHasOne }

public enum TeachOutcome { Taught, Cleared, NoPlynling, Cooldown }

// What an accepted visit did: the scene, the bond before and after, any confession, and the
// happiness both Plynlings got (negative between enemies). Badges per Plynling.
public sealed record VisitOutcome(
    Plynling Visitor, Plynling Host,
    IReadOnlyList<BadgeInfo> VisitorBadges, IReadOnlyList<BadgeInfo> HostBadges,
    bool GoodScene, PlynlingBond Before, PlynlingBond After, Confession Confession, double Happiness,
    ItemFind? VisitorFind = null, ItemFind? HostFind = null);

// PantryUsed: how many of that food came out of the feeder's pantry instead of cailloux (0 when
// they paid), and PantryLeft what remains of it.
public sealed record FeedResult(CareOutcome Outcome, Plynling? Plynling, long Price, long Balance,
    IReadOnlyList<BadgeInfo>? Badges = null, double MealFactor = 1.0, int PantryUsed = 0, int PantryLeft = 0);

// EF access for Plynlings — transient. Every read goes through PlynlingLife.Settle
// before returning, so a caller always sees a Plynling as it is *now*: dead if it starved
// since anyone looked, thawed if its self-freeze ran out.
//
// **Feeding touches the wallet too, here, on purpose.** AppDbContext is transient, so
// charging through PebbleService would be a second context and a second SaveChanges — a
// crash between the two would take the cailloux without feeding it. One context, one
// save, the same shape as ShameService.TryVoteAsync.
public partial class PlynlingService
{
    private readonly AppDbContext _db_context;
    private readonly ILogger<PlynlingService> _logger;
    private readonly PlynlingCooldowns _cooldowns;   // read only, for the card's « Laver » (GetBathSoapAsync)

    private readonly BadgeAwarder _badges;

    public PlynlingService(AppDbContext db_context, ILogger<PlynlingService> logger, PlynlingCooldowns cooldowns, BadgeAwarder badges)
    {
        _db_context = db_context;
        _logger = logger;
        _cooldowns = cooldowns;
        _badges = badges;
    }

    // The owner's living Plynling, brought up to date. If it starved since anyone last
    // looked it comes back dead (and is saved dead), so the caller can say so.
    public Task<Plynling?> GetCurrentAsync(ulong guildId, ulong ownerId, DateTimeOffset now) =>
        UnitAsync(async () => await SettledAsync(await _db_context.Plynlings
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.OwnerId == ownerId && x.DiedAt == null), now));

    // What /plynling view shows: the living one, or else their most recent grave.
    public Task<Plynling?> GetShownAsync(ulong guildId, ulong ownerId, DateTimeOffset now) =>
        UnitAsync(async () => await GetCurrentAsync(guildId, ownerId, now) ?? await GetLatestDeadAsync(guildId, ownerId));

    public Task<Plynling?> GetByIdAsync(int id, DateTimeOffset now) =>
        UnitAsync(async () => await SettledAsync(await _db_context.Plynlings.FirstOrDefaultAsync(x => x.Id == id), now));

    public async Task<Plynling?> GetLatestDeadAsync(ulong guildId, ulong ownerId)
    {
        // Ordered in memory: SQLite cannot translate DateTimeOffset ordering.
        var dead = await _db_context.Plynlings
            .Where(x => x.GuildId == guildId && x.OwnerId == ownerId && x.DiedAt != null)
            .ToListAsync();
        return dead.OrderByDescending(x => x.DiedAt).FirstOrDefault();
    }

    public Task<(AdoptOutcome Outcome, Plynling? Plynling)> AdoptAsync(
        ulong guildId, ulong ownerId, string name, PlynlingSpecies species, DateTimeOffset now,
        PlynlingGender? gender = null, PlynlingPassion? passion = null) =>
        UnitAsync(() => AdoptOnceAsync(guildId, ownerId, name, species, now, gender, passion));

    private async Task<(AdoptOutcome Outcome, Plynling? Plynling)> AdoptOnceAsync(
        ulong guildId, ulong ownerId, string name, PlynlingSpecies species, DateTimeOffset now,
        PlynlingGender? gender, PlynlingPassion? passion)
    {
        var current = await GetCurrentAsync(guildId, ownerId, now);
        if (current is { DiedAt: null }) return (AdoptOutcome.AlreadyHasOne, current);

        // Rolled here unless given, so PlynlingLife.Create stays pure and the harnesses
        // can pin a gender or a passion.
        var plynling = PlynlingLife.Create(guildId, ownerId, name, species, gender ?? PlynlingCatalog.RollGender(), now);
        plynling.Passion = passion ?? PlynlingPassions.RollInnate(Random.Shared);
        _db_context.Plynlings.Add(plynling);
        try
        {
            await _db_context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
        {
            // Two adoptions raced; the partial unique index let exactly one through.
            return (AdoptOutcome.AlreadyHasOne, null);
        }
        await AddMomentAsync(plynling, JournalKind.Adopted, null, now);  // needs its id: after the first save
        var traits = await EnsureTraitsAsync(plynling, now);             // its childhood trait, at once
        await _db_context.SaveChangesAsync();
        QueueOnAction(OnAction.Adopted, plynling, traitText: RevealText(traits, TraitKind.Childhood));
        await FlushOnActionsAsync(now);                                   // « Bienvenue », after the adoption is safe
        return (AdoptOutcome.Adopted, plynling);
    }

    // Ping-Qilin, the bot's own: created once per guild, and never again — any row owned by the bot
    // there, alive or not, counts. Safe to call on every Ready and every join; a raced second
    // insert is turned away by the one-living-per-owner index. A living one that lacks any of her
    // chosen traits gets them here too, at once, rather than at the first hourly sweep.
    public Task<Plynling?> EnsureMascotAsync(ulong guildId, DateTimeOffset now) =>
        UnitAsync(() => EnsureMascotOnceAsync(guildId, now));

    private async Task<Plynling?> EnsureMascotOnceAsync(ulong guildId, DateTimeOffset now)
    {
        if (PlynlingMascot.OwnerId == 0) return null;
        var owner = PlynlingMascot.OwnerId;
        if (await _db_context.Plynlings.FirstOrDefaultAsync(p => p.GuildId == guildId && p.OwnerId == owner && p.DiedAt == null) is { } living)
        {
            await EnsureTraitsAsync(living, now);
            await _db_context.SaveChangesAsync();
            return null;
        }
        if (await _db_context.Plynlings.AnyAsync(p => p.GuildId == guildId && p.OwnerId == owner)) return null;

        var plynling = PlynlingLife.Create(guildId, owner, PlynlingMascot.Name, PlynlingMascot.Species, PlynlingMascot.Gender, now);
        plynling.Passion = PlynlingMascot.Passion;
        _db_context.Plynlings.Add(plynling);
        try
        {
            await _db_context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
        {
            return null;
        }
        await AddMomentAsync(plynling, JournalKind.Adopted, null, now);
        await EnsureTraitsAsync(plynling, now);
        await _db_context.SaveChangesAsync();
        return plynling;
    }

    public Task<FeedResult> FeedAsync(int plynlingId, ulong actorId, PlynlingFood food, DateTimeOffset now) =>
        UnitAsync(() => FeedOnceAsync(plynlingId, actorId, food, now));

    private async Task<FeedResult> FeedOnceAsync(int plynlingId, ulong actorId, PlynlingFood food, DateTimeOffset now)
    {
        var info = PlynlingCatalog.Info(food);
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null) return new FeedResult(CareOutcome.NoPlynling, null, info.Price, 0);

        // The feeder's pantry first — one of that food for their own Plynling, two for someone
        // else's (the same « double » rule) — or else cailloux from their own wallet.
        var isOwner = plynling.OwnerId == actorId;
        var price = PlynlingLife.FeedPrice(info, isOwner);
        var pantryNeeded = isOwner ? 1 : 2;
        var foodKey = ItemCatalog.FoodKey(food);
        var fromPantry = await InventoryService.CountAsync(_db_context, plynling.GuildId, actorId, foodKey) >= pantryNeeded;
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, plynling.GuildId, actorId);
        CareOutcome? refusal =
            plynling.DiedAt is not null ? CareOutcome.Dead
            : plynling.FrozenAt is not null ? CareOutcome.Frozen
            : PlynlingLife.IsSulking(plynling, now) ? CareOutcome.Sulking
            : PlynlingLife.WouldWaste(plynling, info, now) ? CareOutcome.Wasted
            : !fromPantry && wallet.Balance < price ? CareOutcome.TooPoor
            : null;
        if (refusal is { } r) return new FeedResult(r, plynling, price, wallet.Balance);

        var wasStarving = PlynlingLife.HungerAt(plynling, now) < PlynlingLife.StarvingBelow;
        var factor = PlynlingLife.MealFactor(plynling, now);
        if (fromPantry) await InventoryService.TakeAsync(_db_context, plynling.GuildId, actorId, foodKey, pantryNeeded);
        else
        {
            wallet.Balance -= price;
            await EconomyLog.AddAsync(_db_context, plynling.GuildId, isOwner ? EconomyLog.SpendMeal : EconomyLog.SpendMealOther, price, now);
        }
        await EconomyLog.AddAsync(_db_context, plynling.GuildId, EconomyLog.ActMeal, 1, now);
        PlynlingLife.Feed(plynling, info, now);
        await RelieveAsync(plynling, CareAct.Meal, now);
        plynling.Meals++;
        if (plynling.Meals == 1) await AddMomentAsync(plynling, JournalKind.FirstMeal, null, now);
        if (plynling.OwnerId != actorId && ++plynling.FedByOthers == 1)
            await AddMomentAsync(plynling, JournalKind.FedByFriend, actorId.ToString(), now);
        var badges = await AwardAsync(plynling, now, wasStarving ? BadgeEvent.SavedFromStarving : BadgeEvent.None);
        await _db_context.SaveChangesAsync();          // the money, the meal and any badge land together
        if (!isOwner) _badges.Increment(plynling.GuildId, actorId, MemberBadges.Keys.Nanny);   // after the save
        var left = fromPantry ? await InventoryService.CountAsync(_db_context, plynling.GuildId, actorId, foodKey) : 0;
        return new FeedResult(CareOutcome.Done, plynling, fromPantry ? 0 : price, wallet.Balance, badges, factor,
            fromPantry ? pantryNeeded : 0, left);
    }

    public Task<(CareOutcome Outcome, Plynling? Plynling, IReadOnlyList<BadgeInfo> Badges)> PetAsync(int plynlingId, DateTimeOffset now) =>
        UnitAsync(() => PetOnceAsync(plynlingId, now));

    private async Task<(CareOutcome Outcome, Plynling? Plynling, IReadOnlyList<BadgeInfo> Badges)> PetOnceAsync(int plynlingId, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null) return (CareOutcome.NoPlynling, null, NoBadges);
        if (plynling.DiedAt is not null) return (CareOutcome.Dead, plynling, NoBadges);
        if (plynling.FrozenAt is not null) return (CareOutcome.Frozen, plynling, NoBadges);
        if (PlynlingLife.IsAsleep(now)) return (CareOutcome.Asleep, plynling, NoBadges);   // feeding still works

        PlynlingLife.Pet(plynling, now);
        await RelieveAsync(plynling, CareAct.Pet, now);
        plynling.Pets++;
        await EconomyLog.AddAsync(_db_context, plynling.GuildId, EconomyLog.ActPet, 1, now);
        var badges = await AwardAsync(plynling, now);
        await _db_context.SaveChangesAsync();
        return (CareOutcome.Done, plynling, badges);
    }

    // « Laver »: owner only. Refused asleep, frozen, dead, or already clean. Free when the caller
    // claimed the bath cooldown (`free`); otherwise it takes one soap from the owner's pantry, but
    // only if the button said so (`soapOffered`) — a plain « Laver » never spends one unannounced,
    // it answers TooSoon with the soap count instead. Soap is what the owner holds after the bath.
    public Task<(CareOutcome Outcome, Plynling? Plynling, int Soap, bool UsedSoap)> BathAsync(
        int plynlingId, ulong actorId, DateTimeOffset now, bool free, bool soapOffered) =>
        UnitAsync(() => BathOnceAsync(plynlingId, actorId, now, free, soapOffered));

    private async Task<(CareOutcome Outcome, Plynling? Plynling, int Soap, bool UsedSoap)> BathOnceAsync(
        int plynlingId, ulong actorId, DateTimeOffset now, bool free, bool soapOffered)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        CareOutcome? refusal =
            plynling is null ? CareOutcome.NoPlynling
            : plynling.DiedAt is not null ? CareOutcome.Dead
            : plynling.OwnerId != actorId ? CareOutcome.NotOwner
            : plynling.FrozenAt is not null ? CareOutcome.Frozen
            : PlynlingLife.IsAsleep(now) ? CareOutcome.Asleep
            : PlynlingLife.WouldWasteBath(plynling, now) ? CareOutcome.Wasted
            : null;
        if (refusal is { } r) return (r, plynling, 0, false);

        var soap = await InventoryService.CountAsync(_db_context, plynling!.GuildId, actorId, ItemCatalog.SoapKey);
        var useSoap = !free && soapOffered && soap >= 1;
        if (!free && !useSoap) return (CareOutcome.TooSoon, plynling, soap, false);
        if (useSoap)
        {
            await InventoryService.TakeAsync(_db_context, plynling.GuildId, actorId, ItemCatalog.SoapKey, 1);
            soap--;
        }

        PlynlingLife.Bath(plynling, now);
        await RelieveAsync(plynling, CareAct.Bath, now);
        await EconomyLog.AddAsync(_db_context, plynling.GuildId, EconomyLog.ActBath, 1, now);
        await _db_context.SaveChangesAsync();
        return (CareOutcome.Done, plynling, soap, useSoap);
    }

    // What the card's « Laver » offers: the owner's soap while the free bath is on cooldown, else 0
    // (a plain « Laver »). Read at render time, so a card left open can go stale; the handler re-checks.
    public async Task<int> GetBathSoapAsync(Plynling p, DateTimeOffset now)
    {
        if (p.DiedAt is not null || p.FrozenAt is not null || PlynlingLife.IsAsleep(now)) return 0;
        if (_cooldowns.Bath.ReadyAt(p.Id) is null) return 0;
        return await InventoryService.CountAsync(_db_context, p.GuildId, p.OwnerId, ItemCatalog.SoapKey);
    }

    // « Soigner »: owner only, once between two mornings. A dose from the owner's pantry, else
    // MedicinePrice from the wallet — the feeding rule — in one save.
    public Task<(CareOutcome Outcome, Plynling? Plynling, bool FromPantry, long Price, long Balance)> MedicateAsync(
        int plynlingId, ulong actorId, DateTimeOffset now) =>
        UnitAsync(() => MedicateOnceAsync(plynlingId, actorId, now));

    private async Task<(CareOutcome Outcome, Plynling? Plynling, bool FromPantry, long Price, long Balance)> MedicateOnceAsync(
        int plynlingId, ulong actorId, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null) return (CareOutcome.NoPlynling, null, false, 0, 0);
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, plynling.GuildId, actorId);
        var fromPantry = await InventoryService.CountAsync(_db_context, plynling.GuildId, actorId, ItemCatalog.MedicineKey) >= 1;
        var lastMorning = PlynlingLife.MorningAt(plynling.LastMorningDay);
        CareOutcome? refusal =
            plynling.DiedAt is not null ? CareOutcome.Dead
            : plynling.OwnerId != actorId ? CareOutcome.NotOwner
            : plynling.FrozenAt is not null ? CareOutcome.Frozen
            : PlynlingLife.IsAsleep(now) ? CareOutcome.Asleep
            : !PlynlingLife.IsSick(plynling) ? CareOutcome.NotSick
            : plynling.LastMedicineAt is { } dose && dose >= lastMorning ? CareOutcome.AlreadyTreated
            : !fromPantry && wallet.Balance < ItemCatalog.MedicinePrice ? CareOutcome.TooPoor
            : null;
        if (refusal is { } r) return (r, plynling, false, ItemCatalog.MedicinePrice, wallet.Balance);

        if (fromPantry) await InventoryService.TakeAsync(_db_context, plynling.GuildId, actorId, ItemCatalog.MedicineKey, 1);
        else
        {
            wallet.Balance -= ItemCatalog.MedicinePrice;
            await EconomyLog.AddAsync(_db_context, plynling.GuildId, EconomyLog.SpendMedicine, ItemCatalog.MedicinePrice, now);
        }
        plynling.LastMedicineAt = now;
        await EconomyLog.AddAsync(_db_context, plynling.GuildId, EconomyLog.ActMedicine, 1, now);
        await _db_context.SaveChangesAsync();
        return (CareOutcome.Done, plynling, fromPantry, fromPantry ? 0 : ItemCatalog.MedicinePrice, wallet.Balance);
    }

    // /admin plynling cure: cured at once, silently — no DM; the owner sees it on the card.
    public Task<(CureOutcome Outcome, Plynling? Plynling)> CureAsync(ulong guildId, ulong ownerId, DateTimeOffset now) =>
        UnitAsync(() => CureOnceAsync(guildId, ownerId, now));

    private async Task<(CureOutcome Outcome, Plynling? Plynling)> CureOnceAsync(ulong guildId, ulong ownerId, DateTimeOffset now)
    {
        var p = await GetCurrentAsync(guildId, ownerId, now);
        if (p is null) return (CureOutcome.NoPlynling, null);
        if (p.DiedAt is not null) return (CureOutcome.Dead, p);
        if (!PlynlingLife.IsSick(p)) return (CureOutcome.NotSick, p);
        PlynlingLife.Cure(p);
        await AddMomentAsync(p, JournalKind.Recovered, null, now);
        await _db_context.SaveChangesAsync();
        return (CureOutcome.Cured, p);
    }

    // /debug plynling, on the owner's own living Plynling.
    public Task<Plynling?> DebugSetAsync(ulong guildId, ulong ownerId, double? hygiene, bool? sick, DateTimeOffset now) =>
        UnitAsync(() => DebugSetOnceAsync(guildId, ownerId, hygiene, sick, now));

    private async Task<Plynling?> DebugSetOnceAsync(ulong guildId, ulong ownerId, double? hygiene, bool? sick, DateTimeOffset now)
    {
        var p = await GetCurrentAsync(guildId, ownerId, now);
        if (p is null || p.DiedAt is not null) return null;
        PlynlingLife.DebugSet(p, now, hygiene, sick);
        await _db_context.SaveChangesAsync();
        return p;
    }

    public Task<(FreezeOutcome Outcome, Plynling? Plynling)> FreezeAsync(
        ulong guildId, ulong ownerId, bool byStaff, DateTimeOffset now) =>
        UnitAsync(() => FreezeOnceAsync(guildId, ownerId, byStaff, now));

    private async Task<(FreezeOutcome Outcome, Plynling? Plynling)> FreezeOnceAsync(
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

    public Task<(ThawOutcome Outcome, Plynling? Plynling)> ThawAsync(
        ulong guildId, ulong ownerId, bool byStaff, DateTimeOffset now) =>
        UnitAsync(() => ThawOnceAsync(guildId, ownerId, byStaff, now));

    private async Task<(ThawOutcome Outcome, Plynling? Plynling)> ThawOnceAsync(
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
}
