using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum DebugAffinityOutcome { Shifted, SameOwner, NoFirst, NoSecond }

// What /debug affinity add did: the pair, its affinity before and after, and its bond before and after.
public sealed record DebugAffinityResult(Plynling A, Plynling B, int Before, int After, PlynlingBond BondBefore, PlynlingBond BondAfter);

public partial class PlynlingService
{
    // Reaches the shown Plynling — living, or else the latest grave — because a grave
    // shows its name publicly too, and an offensive one needs fixing there as well.
    // The end of a /plynling play game: the happiness, the counts and — on a win — the
    // player's cailloux land in one save. Null when the Plynling can no longer be played
    // with (it died, was frozen or abandoned mid-game): then nothing is paid.
    // A win may also find an item (ItemCatalog.PlayFindChance), in the same save.
    public Task<(Plynling? Plynling, long Balance, IReadOnlyList<BadgeInfo> Badges, ItemFind? Find)> FinishPlayAsync(
        int plynlingId, ulong ownerId, bool won, long pebbles, DateTimeOffset now, Random? rng = null) =>
        UnitAsync(() => FinishPlayOnceAsync(plynlingId, ownerId, won, pebbles, now, rng));

    private async Task<(Plynling? Plynling, long Balance, IReadOnlyList<BadgeInfo> Badges, ItemFind? Find)> FinishPlayOnceAsync(
        int plynlingId, ulong ownerId, bool won, long pebbles, DateTimeOffset now, Random? rng)
    {
        rng ??= Random.Shared;
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null || plynling.OwnerId != ownerId || plynling.DiedAt is not null || plynling.FrozenAt is not null)
            return (null, 0, NoBadges, null);

        PlynlingLife.Play(plynling, now, won);
        await RelieveAsync(plynling, CareAct.Game, now);
        if (won && plynling.PlaysWon == 1) await AddMomentAsync(plynling, JournalKind.FirstWin, null, now);
        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, plynling.GuildId, ownerId);
        if (won && pebbles > 0)
        {
            wallet.Balance += pebbles;
            await EconomyLog.AddAsync(_db_context, plynling.GuildId, EconomyLog.EarnGame, pebbles, now);
        }
        await EconomyLog.AddAsync(_db_context, plynling.GuildId, EconomyLog.ActGame, 1, now);
        await PlynlingScore.AddAsync(_db_context, plynling, PlynlingScore.Play, now);
        var find = won && rng.NextDouble() < ItemCatalog.PlayFindChance
            ? await InventoryService.GrantAsync(_db_context, plynling.GuildId, ownerId, ItemCatalog.DrawCollectible(rng, now), now)
            : null;
        var badges = await AwardAsync(plynling, now);                   // pays into the same wallet
        await _db_context.SaveChangesAsync();
        return (plynling, wallet.Balance, badges, find);
    }

    // A /plynling visit accepted: the scene plays out on their relationship — affinity, bond, the
    // confession if the visitor came to confess — and both are cheered (or not) by what they are to
    // each other, all in one save. `play` is what the two owners chose (VisitPlay). Null when either
    // can no longer take part; TooLate (nothing saved) when the visitor came to confess and the pair
    // no longer may. A good scene may also turn up an item for each owner, drawn from `findRng` —
    // kept apart from the scene's own draws.
    public Task<VisitOutcome?> VisitAsync(int visitorId, int hostId, DateTimeOffset now, VisitPlay play, Random? rng = null, Random? findRng = null) =>
        UnitAsync(() => VisitOnceAsync(visitorId, hostId, now, play, rng ?? Random.Shared, findRng ?? Random.Shared));

    // What starting a visit needs to know about the pair, from one read of their relation (the start
    // answers without a defer, so it reads as little as it can): whether they already met today, as
    // stored; their bond before the visit; and whether it offers « 💌 » — the pair may confess
    // (MayConfessAsync) and today is one of its offer days (PlynlingBonds.ConfessionOffered). The
    // visit's own save checks again whether the pair may confess, but not the offer day: a knock
    // answered after midnight keeps the confession its visitor chose.
    public async Task<VisitTerms> VisitTermsAsync(Plynling visitor, Plynling host, DateTimeOffset now)
    {
        var relation = await FindRelationAsync(visitor.Id, host.Id);
        var offered = relation is not null && PlynlingBonds.ConfessionOffered(visitor.Id, host.Id, AppTime.DayKey(now))
            && await MayConfessAsync(relation, visitor, host, now);
        return new VisitTerms(relation?.LastVisitDay == AppTime.DayKey(now), relation?.Bond ?? PlynlingBond.Acquaintances, offered);
    }

    // Whether the pair may confess right now, the offer day aside: « 💌 » asks again when pressed, so a
    // pair that stopped qualifying since the visit started is told at once, not after the host's reaction.
    public async Task<bool> MayConfessNowAsync(Plynling visitor, Plynling host, DateTimeOffset now) =>
        await FindRelationAsync(visitor.Id, host.Id) is { } relation && await MayConfessAsync(relation, visitor, host, now);

    private async Task<bool> MayConfessAsync(PlynlingRelation relation, Plynling visitor, Plynling host, DateTimeOffset now) =>
        PlynlingBonds.MayConfess(relation.Bond, relation.Affinity, visitor, host,
            await InCoupleAsync(visitor.Id, host.Id) || await InCoupleAsync(host.Id, visitor.Id), relation.LastRefusalAt, now);

    // A Plynling's personality axes, as a visit's choices are judged by them (VisitOdds.Shift).
    public async Task<IReadOnlyDictionary<AiAxis, int>> AxesAsync(Plynling p) => PlynlingPersonality.Axes(await GetTraitsAsync(p));

    // Whether the pair already met today (Paris day), as stored — the claim a restart cannot reopen.
    public async Task<bool> VisitedTodayAsync(int a, int b, DateTimeOffset now) =>
        (await FindRelationAsync(a, b))?.LastVisitDay == AppTime.DayKey(now);

    private async Task<VisitOutcome?> VisitOnceAsync(int visitorId, int hostId, DateTimeOffset now, VisitPlay play, Random rng, Random findRng)
    {
        var visitor = await GetByIdAsync(visitorId, now);
        var host = await GetByIdAsync(hostId, now);
        if (visitor is null || host is null || visitor.Id == host.Id) return null;
        if (visitor.DiedAt is not null || host.DiedAt is not null) return null;
        if (visitor.FrozenAt is not null || host.FrozenAt is not null) return null;
        if (PlynlingLife.IsSick(visitor) || PlynlingLife.IsSick(host)) return null;      // the callers say why

        var (lo, hi) = Pair(visitor.Id, host.Id);
        // The callers check VisitedTodayAsync first; this is the backstop for two visits racing past
        // it (the relation's token makes the slower one re-read and stop here).
        var day = AppTime.DayKey(now);
        var stored = await FindRelationAsync(visitor.Id, host.Id);
        if (stored?.LastVisitDay == day) return null;
        // Offered when the visitor started; someone may have formed a couple, or a refusal the other
        // way started the wait, since. Checked before any change, so nothing is saved.
        if (play.Confess && (stored is null || !await MayConfessAsync(stored, visitor, host, now)))
        {
            var bond = stored?.Bond ?? PlynlingBond.Acquaintances;
            return new VisitOutcome(visitor, host, NoBadges, NoBadges, false, bond, bond, Confession.None, 0, TooLate: true);
        }

        var relation = await RelationForAsync(visitor.Id, host.Id, now);
        relation.LastVisitDay = day;

        var before = relation.Bond;
        var compatibility = PlynlingBonds.Compatibility(lo, hi);
        // A confession visit is a good scene, then the answer; otherwise the scene is rolled with both
        // choices' shift. Never a confession the visitor did not choose.
        var good = play.Confess || rng.NextDouble() < VisitOdds.GoodChance(compatibility, before, relation.Affinity, play.Shift);
        var delta = good ? rng.Next(12, 18) : -rng.Next(12, 19);
        relation.Affinity = Math.Clamp(relation.Affinity + delta, -100, 100);
        relation.Meetings++;

        var confession = !play.Confess ? Confession.None
            : rng.NextDouble() < VisitOdds.AcceptChance(compatibility, play.Shift) ? Confession.Accepted : Confession.Refused;
        if (confession == Confession.Refused)
        {
            relation.Affinity = Math.Clamp(relation.Affinity - PlynlingBonds.HeartbreakLoss, -100, 100);
            relation.LastRefusalAt = now;
        }

        var after = confession == Confession.Accepted ? PlynlingBond.Lovers : PlynlingBonds.BondFor(relation.Affinity, before);
        if (after != before)
        {
            relation.Bond = after;
            relation.Since = now;
        }

        var happiness = PlynlingBonds.VisitHappiness(after);
        PlynlingLife.Visit(visitor, now, happiness);
        PlynlingLife.Visit(host, now, happiness);
        host.Hosted++;
        var close = after is PlynlingBond.Friends or PlynlingBond.BestFriends or PlynlingBond.Lovers;
        await RelieveAsync(visitor, CareAct.Visit, now, close);
        await RelieveAsync(host, CareAct.Visit, now, close);
        if (confession == Confession.Refused)
        {
            foreach (var (p, other) in new[] { (visitor, host), (host, visitor) })
            {
                PlynlingLife.Sadden(p, now, PlynlingBonds.HeartbreakSadness);
                await AddMomentAsync(p, JournalKind.Heartbroken, other.Name, now);
            }
        }

        var evt = await ApplyBondChangeAsync(visitor, host, relation, before, after, now);
        // Her own Plynling finds nothing: an item would land in the bot's inventory, where nobody can use it.
        async Task<ItemFind?> FindFor(Plynling p) => good && !PlynlingMascot.Is(p) && findRng.NextDouble() < ItemCatalog.VisitFindChance
            ? await InventoryService.GrantAsync(_db_context, p.GuildId, p.OwnerId, ItemCatalog.DrawCollectible(findRng, now), now)
            : null;
        var visitorFind = await FindFor(visitor);
        var hostFind = await FindFor(host);
        var visitorBadges = await AwardAsync(visitor, now, evt);
        var hostBadges = await AwardAsync(host, now, evt);
        await EconomyLog.AddAsync(_db_context, visitor.GuildId, EconomyLog.ActVisit, 1, now);
        var visitPoints = PlynlingScore.Visit + (good ? PlynlingScore.VisitGoodScene : 0);
        await PlynlingScore.AddAsync(_db_context, visitor, visitPoints, now);
        await PlynlingScore.AddAsync(_db_context, host, visitPoints, now);
        await _db_context.SaveChangesAsync();
        // Sometimes something is left behind (FlushOnActionsAsync rolls it per Plynling and day).
        QueueOnAction(OnAction.AfterVisit, visitor, host.Id);
        QueueOnAction(OnAction.AfterVisit, host, visitor.Id);
        await FlushOnActionsAsync(now);
        return new VisitOutcome(visitor, host, visitorBadges, hostBadges, good, before, after, confession, happiness,
            visitorFind, hostFind);
    }

    // A change of bond between two Plynlings: both journal it, and getting closer may earn a badge.
    // Shared by visits and events so the two can never word or reward it differently. Never saves.
    //
    // A bond reached (not a breakup) scores both sides, once per pair and bond (relation.ScoredBonds): a pair
    // drifting across a band edge, or a couple making up, adds nothing the second time.
    // `scored: false` (the owner's /debug affinity add) journals the change but scores and badges
    // nothing, and leaves the bond's bit unset so reaching it for real later still scores.
    private async Task<BadgeEvent> ApplyBondChangeAsync(Plynling a, Plynling b, PlynlingRelation relation,
        PlynlingBond before, PlynlingBond after, DateTimeOffset now, bool scored = true)
    {
        if (after == before || BondMoment(before, after) is not { } kind) return BadgeEvent.None;
        await AddMomentAsync(a, kind, b.Name, now, scored);
        await AddMomentAsync(b, kind, a.Name, now, scored);
        if (!scored) return BadgeEvent.None;
        var bit = PlynlingScore.BondBit(after);
        if (kind != JournalKind.BrokeUp && (relation.ScoredBonds & bit) == 0)
        {
            relation.ScoredBonds |= bit;
            await PlynlingScore.AddAsync(_db_context, a, PlynlingScore.Bond(after), now);
            await PlynlingScore.AddAsync(_db_context, b, PlynlingScore.Bond(after), now);
        }
        var evt = after switch
        {
            PlynlingBond.Friends => BadgeEvent.BecameFriends,
            PlynlingBond.BestFriends => BadgeEvent.BecameBestFriends,
            PlynlingBond.Lovers => BadgeEvent.BecameLovers,
            _ => BadgeEvent.None,
        };
        return Closeness(after) > Closeness(before) ? BadgeEvent.None : evt;     // drifting apart earns nothing
    }

    // An event moving two Plynlings' affinity, one-sided: the same relation row, clamp and bands as a
    // visit, but no scene roll and no confession — a couple comes only from an accepted declaration
    // (MakeCoupleAsync). Never saves.
    public async Task<(PlynlingBond Before, PlynlingBond After)> ShiftAffinityAsync(Plynling a, Plynling b, int delta, DateTimeOffset now,
        bool scored = true)
    {
        var relation = await RelationForAsync(a.Id, b.Id, now);
        var before = relation.Bond;
        relation.Affinity = Math.Clamp(relation.Affinity + delta, -100, 100);
        var after = PlynlingBonds.BondFor(relation.Affinity, before);
        if (after != before)
        {
            relation.Bond = after;
            relation.Since = now;
        }
        var evt = await ApplyBondChangeAsync(a, b, relation, before, after, now, scored);
        if (scored)
        {
            await AwardAsync(a, now, evt);
            await AwardAsync(b, now, evt);
        }
        return (before, after);
    }

    // /debug affinity add: moves two living Plynlings' affinity by `delta`, through the same row, clamp
    // and bands as an event (a couple still only breaks up below BreakUpBelow, never forms). A bond it
    // changes is journalled on both sides but scores nothing, like /debug heal's recovery.
    public Task<(DebugAffinityOutcome Outcome, DebugAffinityResult? Result)> DebugShiftAffinityAsync(
        ulong guildId, ulong ownerA, ulong ownerB, int delta, DateTimeOffset now) =>
        UnitAsync(() => DebugShiftAffinityOnceAsync(guildId, ownerA, ownerB, delta, now));

    private async Task<(DebugAffinityOutcome Outcome, DebugAffinityResult? Result)> DebugShiftAffinityOnceAsync(
        ulong guildId, ulong ownerA, ulong ownerB, int delta, DateTimeOffset now)
    {
        if (ownerA == ownerB) return (DebugAffinityOutcome.SameOwner, null);
        var a = await GetCurrentAsync(guildId, ownerA, now);
        if (a is null || a.DiedAt is not null) return (DebugAffinityOutcome.NoFirst, null);
        var b = await GetCurrentAsync(guildId, ownerB, now);
        if (b is null || b.DiedAt is not null) return (DebugAffinityOutcome.NoSecond, null);

        var before = (await FindRelationAsync(a.Id, b.Id))?.Affinity ?? 0;
        var (bondBefore, bondAfter) = await ShiftAffinityAsync(a, b, delta, now, scored: false);
        var after = Math.Clamp(before + delta, -100, 100);      // ShiftAffinityAsync's clamp; a new row isn't queryable before the save
        await _db_context.SaveChangesAsync();
        return (DebugAffinityOutcome.Shifted, new DebugAffinityResult(a, b, before, after, bondBefore, bondAfter));
    }

    // Lifts the pair's affinity to at least `value` (never lowers it), reading the current value from
    // the tracked row — so a shift earlier in the same unit of work counts. Never saves.
    public async Task<(PlynlingBond Before, PlynlingBond After)?> LiftAffinityAsync(Plynling a, Plynling b, int value, DateTimeOffset now)
    {
        var current = (await FindRelationAsync(a.Id, b.Id))?.Affinity ?? 0;
        return value > current ? await ShiftAffinityAsync(a, b, value - current, now) : null;
    }

    // The visit's accepted confession, as an effect: lovers, both journals, the badge. The caller has
    // checked the visit rules (CanConfess), which imply a relation row (best friends). Never saves.
    private async Task<(PlynlingBond Before, PlynlingBond After)> MakeCoupleAsync(Plynling a, Plynling b, DateTimeOffset now)
    {
        var relation = (await FindRelationAsync(a.Id, b.Id))!;
        var before = relation.Bond;
        relation.Bond = PlynlingBond.Lovers;
        relation.Since = now;
        var evt = await ApplyBondChangeAsync(a, b, relation, before, PlynlingBond.Lovers, now);
        await AwardAsync(a, now, evt);
        await AwardAsync(b, now, evt);
        return (before, PlynlingBond.Lovers);
    }

    // A pair's relation row is keyed lower id first (its unique index).
    private static (int Lo, int Hi) Pair(int a, int b) => a < b ? (a, b) : (b, a);

    // The pair's relation row, or null.
    private async Task<PlynlingRelation?> FindRelationAsync(int a, int b)
    {
        var (lo, hi) = Pair(a, b);
        return await _db_context.PlynlingRelations.FirstOrDefaultAsync(r => r.PlynlingAId == lo && r.PlynlingBId == hi);
    }

    // The pair's relation row, created (acquaintances, from now) if they never met. Never saves.
    private async Task<PlynlingRelation> RelationForAsync(int a, int b, DateTimeOffset now)
    {
        if (await FindRelationAsync(a, b) is { } found) return found;
        var (lo, hi) = Pair(a, b);
        var created = new PlynlingRelation { PlynlingAId = lo, PlynlingBId = hi, Bond = PlynlingBond.Acquaintances, Since = now };
        _db_context.PlynlingRelations.Add(created);
        return created;
    }

    // Care eases stress, by its coping traits (PlynlingStress.Relief). The traits are read only when
    // there is stress to ease. Never saves.
    private async Task RelieveAsync(Plynling p, CareAct act, DateTimeOffset now, bool closeBond = false)
    {
        if (p.Stress > 0)
            PlynlingLife.Relieve(p, now, PlynlingStress.Relief(act, await GetTraitsAsync(p), closeBond));
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
    // moment in their journal. Not saved — the caller's save carries it. A death also brings each
    // mourner an event about it (queued; the caller flushes); an abandonment does not — the row is
    // deleted, and no event may point at it.
    private async Task GrieveForAsync(Plynling gone, DateTimeOffset now, bool died = true)
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
            if (died) QueueOnAction(OnAction.Bereaved, mourner, gone.Id);
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

    // The owner's pantry, food by food, for the card's « Nourrir » menu — the owner's rather than
    // whoever pressed a button, since the card is one message everyone sees. Empty on a dead or
    // frozen one, whose card has no menu.
    public async Task<IReadOnlyDictionary<PlynlingFood, int>> GetPantryAsync(Plynling p)
    {
        if (p.DiedAt is not null || p.FrozenAt is not null) return new Dictionary<PlynlingFood, int>();
        var keys = PlynlingCatalog.Foods.ToDictionary(f => ItemCatalog.FoodKey(f.Food), f => f.Food);
        var names = keys.Keys.ToList();
        var rows = await _db_context.InventoryItems
            .Where(i => i.GuildId == p.GuildId && i.UserId == p.OwnerId && names.Contains(i.Key) && i.Quantity > 0)
            .Select(i => new { i.Key, i.Quantity })
            .ToListAsync();
        return rows.ToDictionary(r => keys[r.Key], r => r.Quantity);
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
}
