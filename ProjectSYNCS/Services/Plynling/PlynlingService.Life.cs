using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public partial class PlynlingService
{
    // /plynling abandon: the owner's living Plynling leaves for good — the row is deleted,
    // so it never reaches the graveyard and cannot be resurrected. Returns what was removed
    // (for the announcement), or null when there was nothing of theirs to abandon.
    public Task<Plynling?> AbandonAsync(int plynlingId, ulong ownerId, DateTimeOffset now) =>
        UnitAsync(() => AbandonOnceAsync(plynlingId, ownerId, now));

    private async Task<Plynling?> AbandonOnceAsync(int plynlingId, ulong ownerId, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null || plynling.OwnerId != ownerId || plynling.DiedAt is not null) return null;

        await GrieveForAsync(plynling, now, died: false);   // before the relations go with it
        _db_context.Plynlings.Remove(plynling);
        await _db_context.SaveChangesAsync();
        return plynling;
    }

    public Task<(Plynling? Plynling, string OldName)> RenameAsync(
        ulong guildId, ulong ownerId, string name, DateTimeOffset now) =>
        UnitAsync(() => RenameOnceAsync(guildId, ownerId, name, now));

    private async Task<(Plynling? Plynling, string OldName)> RenameOnceAsync(
        ulong guildId, ulong ownerId, string name, DateTimeOffset now)
    {
        var plynling = await GetShownAsync(guildId, ownerId, now);
        if (plynling is null) return (null, string.Empty);

        var oldName = plynling.Name;
        plynling.Name = name;
        await _db_context.SaveChangesAsync();
        return (plynling, oldName);
    }

    // /plynling passion: set (or clear, when text is null) what its owner taught it. The cooldown
    // runs from the last change either way, so set/clear cannot be toggled for spam.
    public Task<(TeachOutcome Outcome, Plynling? Plynling, DateTimeOffset? ReadyAt)> TeachPassionAsync(
        int plynlingId, ulong ownerId, string? text, DateTimeOffset now) =>
        UnitAsync(() => TeachPassionOnceAsync(plynlingId, ownerId, text, now));

    private async Task<(TeachOutcome Outcome, Plynling? Plynling, DateTimeOffset? ReadyAt)> TeachPassionOnceAsync(
        int plynlingId, ulong ownerId, string? text, DateTimeOffset now)
    {
        var plynling = await GetByIdAsync(plynlingId, now);
        if (plynling is null || plynling.OwnerId != ownerId || plynling.DiedAt is not null)
            return (TeachOutcome.NoPlynling, null, null);
        if (plynling.TaughtPassionAt is { } at && at + PlynlingPassions.TeachCooldown > now)
            return (TeachOutcome.Cooldown, plynling, at + PlynlingPassions.TeachCooldown);

        plynling.TaughtPassion = text;
        plynling.TaughtPassionAt = now;
        if (text is not null) await AddMomentAsync(plynling, JournalKind.LearnedPassion, text, now);
        await _db_context.SaveChangesAsync();
        return (text is null ? TeachOutcome.Cleared : TeachOutcome.Taught, plynling, null);
    }

    // Staff moderation, like RenameAsync: reaches the latest grave too, since its card shows it.
    public Task<Plynling?> ResetPassionAsync(ulong guildId, ulong ownerId, DateTimeOffset now) =>
        UnitAsync(() => ResetPassionOnceAsync(guildId, ownerId, now));

    private async Task<Plynling?> ResetPassionOnceAsync(ulong guildId, ulong ownerId, DateTimeOffset now)
    {
        var plynling = await GetShownAsync(guildId, ownerId, now);
        if (plynling is null) return null;
        plynling.TaughtPassion = null;
        plynling.TaughtPassionAt = null;
        await _db_context.SaveChangesAsync();
        return plynling;
    }

    public Task<(ResurrectOutcome Outcome, Plynling? Plynling)> ResurrectAsync(
        ulong guildId, ulong ownerId, DateTimeOffset now) =>
        UnitAsync(() => ResurrectOnceAsync(guildId, ownerId, now));

    private async Task<(ResurrectOutcome Outcome, Plynling? Plynling)> ResurrectOnceAsync(
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
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
        {
            return (ResurrectOutcome.AlreadyHasOne, null);   // an adoption won the race
        }
        return (ResurrectOutcome.Resurrected, grave);
    }

    // The graveyard, with any living one that starved since the last sweep already in the ground.
    // Read-only, like GetLivingAsync: the starved are settled in memory and the sweep stamps them.
    public async Task<List<Plynling>> GetGraveyardAsync(ulong guildId, ulong? ownerId, DateTimeOffset now)
    {
        var starved = (await GetSettledLivingAsync(guildId, now)).Where(x => x.DiedAt is not null);
        var query = _db_context.Plynlings.AsNoTracking().Where(x => x.GuildId == guildId && x.DiedAt != null);
        if (ownerId is { } owner)
        {
            query = query.Where(x => x.OwnerId == owner);
            starved = starved.Where(x => x.OwnerId == owner);
        }
        return (await query.ToListAsync()).Concat(starved).ToList();
    }

    // /plynling list, the race: every living Plynling in the guild, settled to now so one that starved
    // since the last sweep is not listed as alive. Read-only — loaded untracked and settled in memory,
    // never saved: a list must not write a row per Plynling on the Pi, and what it would write (a
    // death, a morning) Settle reproduces identically for the sweep or the next action to store.
    // Display only: never hand these to a writer.
    public async Task<List<Plynling>> GetLivingAsync(ulong guildId, DateTimeOffset now) =>
        (await GetSettledLivingAsync(guildId, now)).Where(x => x.DiedAt is null).ToList();

    private async Task<List<Plynling>> GetSettledLivingAsync(ulong guildId, DateTimeOffset now)
    {
        var living = await _db_context.Plynlings.AsNoTracking().Where(x => x.GuildId == guildId && x.DiedAt == null).ToListAsync();
        foreach (var p in living) PlynlingLife.Settle(p, now);   // in memory only: AsNoTracking, never saved
        return living;
    }

    // Who owns a living Plynling here, for the Plynling role. Read-only on purpose: loaded untracked
    // and settled in memory (like AdminStatsService), so a death nobody has noticed yet already
    // counts as a death, without this read writing it — the sweep stamps and announces it.
    public async Task<HashSet<ulong>> GetLivingOwnerIdsAsync(ulong guildId, DateTimeOffset now)
    {
        var living = await _db_context.Plynlings.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.DiedAt == null)
            .ToListAsync();
        foreach (var p in living) PlynlingLife.Settle(p, now);   // in memory only: AsNoTracking, never saved
        return living.Where(x => x.DiedAt is null).Select(x => x.OwnerId).ToHashSet();
    }

    // The same question for one person (the role hook after adopt / abandon / resurrect).
    public async Task<bool> HasLivingAsync(ulong guildId, ulong ownerId, DateTimeOffset now)
    {
        var p = await _db_context.Plynlings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.OwnerId == ownerId && x.DiedAt == null);
        if (p is null) return false;
        PlynlingLife.Settle(p, now);   // in memory only
        return p.DiedAt is null;
    }

    public Task SaveAsync() => _db_context.SaveChangesAsync();

    // The happy gift, when its owner looks: the day's single draw, spent win or lose and saved
    // with what it found — cailloux, or on half the wins an item instead.
    //
    // It works on the Plynling the caller holds, so it cannot be run again: a conflict (or one left
    // detached by an earlier unit's retry, whose save would drop LastGiftDay but keep the gift) leaves
    // the draw unspent for the next look instead.
    public async Task<GiftResult> TryGiftAsync(Plynling p, ulong viewerId, DateTimeOffset now, Random rng)
    {
        if (viewerId != p.OwnerId || !PlynlingLife.CanDrawGift(p, now)) return GiftResult.None;
        if (_db_context.Entry(p).State == EntityState.Detached) return GiftResult.None;
        try
        {
            return await DrawGiftAsync(p, now, rng);
        }
        catch (DbUpdateConcurrencyException)
        {
            DiscardChanges();
            return GiftResult.None;
        }
    }

    private async Task<GiftResult> DrawGiftAsync(Plynling p, DateTimeOffset now, Random rng)
    {
        p.LastGiftDay = AppTime.DayKey(now);
        var found = PlynlingLife.GiftDraw(rng, PlynlingModifiers.GiftFactor(p));
        var result = GiftResult.None;
        if (found > 0 && rng.NextDouble() >= ItemCatalog.GiftCaillouxShare)
        {
            result = new GiftResult(0, await InventoryService.GrantAsync(_db_context, p.GuildId, p.OwnerId, ItemCatalog.DrawCollectible(rng, now), now));
        }
        else if (found > 0)
        {
            var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, p.GuildId, p.OwnerId);
            wallet.Balance += found;
            await EconomyLog.AddAsync(_db_context, p.GuildId, EconomyLog.EarnGift, found, now);
            result = new GiftResult(found, null);
        }
        await _db_context.SaveChangesAsync();
        return result;
    }

    // /plynling forage: the owner's living Plynling goes out and always brings something back —
    // a collectible, or a food for the pantry. Once every ItemCatalog.ForageCooldown per person
    // (on the wallet row, so a new Plynling does not reset it); never asleep, frozen or sulking.
    public Task<ForageResult> ForageAsync(ulong guildId, ulong ownerId, DateTimeOffset now, Random rng) =>
        UnitAsync(() => ForageOnceAsync(guildId, ownerId, now, rng));

    private async Task<ForageResult> ForageOnceAsync(ulong guildId, ulong ownerId, DateTimeOffset now, Random rng)
    {
        var p = await GetCurrentAsync(guildId, ownerId, now);
        if (p is null) return new ForageResult(CareOutcome.NoPlynling, null);
        CareOutcome? refusal = p.DiedAt is not null ? CareOutcome.Dead : p.FrozenAt is not null ? CareOutcome.Frozen
            : PlynlingLife.IsAsleep(now) ? CareOutcome.Asleep : PlynlingLife.IsSulking(p, now) ? CareOutcome.Sulking : null;
        if (refusal is { } r) return new ForageResult(r, p);

        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, guildId, ownerId);
        if (wallet.LastForageAt is { } last && last + ItemCatalog.ForageCooldown > now)
            return new ForageResult(CareOutcome.TooSoon, p, ReadyAt: last + ItemCatalog.ForageCooldown);

        wallet.LastForageAt = now;
        PlynlingLife.Dirty(p, now, PlynlingLife.ForageDirt);          // an outing in the undergrowth
        var find = await InventoryService.GrantAsync(_db_context, guildId, ownerId, ItemCatalog.DrawForage(rng, now), now);
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.ActForage, 1, now);
        await _db_context.SaveChangesAsync();
        return new ForageResult(CareOutcome.Done, p, find);
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

    // Whatever its stage is owed and it lacks (PlynlingTraits.Draw): the childhood trait from bébé,
    // two personality traits at ado, the fourth trait at adulte. The mascot gets her four chosen ones
    // instead. One path for adoption, the mascot's creation and every sweep — which is also what gives
    // Plynlings that predate traits theirs. Adds and journals; never saves (rides the caller's save).
    // Returns what it added, in order (a trait reveal names them).
    public async Task<IReadOnlyList<TraitInfo>> EnsureTraitsAsync(Plynling p, DateTimeOffset now)
    {
        if (p.DiedAt is not null) return Array.Empty<TraitInfo>();
        var held = await HeldTraitKeysAsync(p.Id);
        var stage = PlynlingLife.Stage(p, now);
        var slot = held.Count(k => PlynlingTraits.ByKey(k)?.Kind == TraitKind.Personality);
        // The ado history is read only when the adulte trait is actually due, not on every sweep.
        var adultWeight = stage >= PlynlingStage.Adult && slot < 3 ? await AdultWeightAsync(p, now) : null;
        var add = PlynlingMascot.Is(p)
            ? PlynlingMascot.TraitKeys.Where(k => !held.Contains(k)).Select(k => PlynlingTraits.ByKey(k)!).ToList()
            : PlynlingTraits.Draw(p.Id, stage, held, adultWeight);
        var age = PlynlingLife.Age(p, now);
        foreach (var trait in add)
        {
            _db_context.PlynlingTraits.Add(new PlynlingTrait { PlynlingId = p.Id, Key = trait.Key, Kind = trait.Kind, AcquiredAt = now });
            // Journaled only when its stage has just begun: a backfill stays out of the journal, and so do
            // the mascot's chosen traits (she does not grow into them).
            if (!PlynlingMascot.Is(p) && PlynlingTraits.JustGained(PlynlingTraits.StageOf(trait.Kind, slot), age))
                await AddMomentAsync(p, JournalKind.TraitGained, trait.Key, now);
            if (trait.Kind == TraitKind.Personality) slot++;
        }
        RefreshStressCache(p, held.Concat(add.Select(t => t.Key)));   // every sweep: also fills rows that predate the cache
        return add;
    }

    // Its traits in the order acquired. A key no longer in the catalog is skipped, never thrown on.
    public async Task<IReadOnlyList<TraitInfo>> GetTraitsAsync(Plynling p) =>
        (await HeldTraitKeysAsync(p.Id)).Select(PlynlingTraits.ByKey).OfType<TraitInfo>().ToList();

    // The keys it holds, stored first, then any added in this unit of work and not saved yet: two
    // mental breaks decided in one sweep must see each other's coping trait, or the second can add the
    // same one again (the unique index refuses the save, and every later sweep makes the same rolls).
    private async Task<List<string>> HeldTraitKeysAsync(int plynlingId)
    {
        var keys = await _db_context.PlynlingTraits.Where(t => t.PlynlingId == plynlingId).OrderBy(t => t.Id).Select(t => t.Key).ToListAsync();
        foreach (var entry in _db_context.ChangeTracker.Entries<PlynlingTrait>())
            if (entry.State == EntityState.Added && entry.Entity.PlynlingId == plynlingId && !keys.Contains(entry.Entity.Key))
                keys.Add(entry.Entity.Key);
        return keys;
    }

    // Its traits' stress-decay multiplier, cached on the row for Settle (which cannot load traits),
    // from the keys it holds now — including any added in this unit of work.
    private static void RefreshStressCache(Plynling p, IEnumerable<string> traitKeys) =>
        p.StressLossBonusPercent = PlynlingStress.LossBonusPercent(traitKeys.Distinct().Select(PlynlingTraits.ByKey).OfType<TraitInfo>());

    // What only time earns, for the hourly sweep: the « est devenu… » moments — dated when the
    // stage was reached, written once while that is recent (PlynlingTraits.JustGained) — any badge
    // it now qualifies for (the age ones, and on ship day the counts), and the traits its stage is
    // owed. Not saved: the sweep's own save carries it.
    public async Task ProgressAsync(Plynling p, DateTimeOffset now)
    {
        if (p.DiedAt is not null) return;
        var written = (await _db_context.PlynlingJournalEntries
                .Where(e => e.PlynlingId == p.Id && e.Kind == JournalKind.GrewUp)
                .Select(e => e.Detail)
                .ToListAsync())
            .ToHashSet();
        var age = PlynlingLife.Age(p, now);
        PlynlingStage? grewTo = null;      // several at once (a backfill) keeps the last
        foreach (var stage in new[] { PlynlingStage.Teen, PlynlingStage.Adult, PlynlingStage.Elder })
        {
            var start = PlynlingLife.StageStart(stage);
            // Past the window, a missing moment is one the journal trimmed: written again it would be
            // the oldest entry and push out the next-oldest — another stage moment the next sweep
            // re-adds, every hour.
            if (!PlynlingTraits.JustGained(stage, age) || written.Contains(stage.ToString())) continue;
            // As long ago as it has lived past the threshold — never before it was adopted.
            var at = now - (age - start);
            await AddMomentAsync(p, JournalKind.GrewUp, stage.ToString(), at < p.AdoptedAt ? p.AdoptedAt : at);
            grewTo = stage;
        }
        var added = await EnsureTraitsAsync(p, now);   // the traits its new stage brings — and any it predates
        await AwardAsync(p, now);
        // Growing up reveals its new traits in an event; the caller flushes after its save. The window
        // above means this happens once per stage, never again for a moment the journal trimmed.
        if (grewTo == PlynlingStage.Teen) QueueOnAction(OnAction.BecameTeen, p, traitText: RevealText(added, TraitKind.Personality));
        if (grewTo == PlynlingStage.Adult) QueueOnAction(OnAction.BecameAdult, p, traitText: RevealText(added, TraitKind.Personality));
    }

    // The sweep, announcing a death: the journal's last moment, dated when it died. Not saved.
    // Its best friends and partner grieve it too.
    public async Task JournalDeathAsync(Plynling p)
    {
        if (p.DiedAt is not { } died) return;
        await AddMomentAsync(p, JournalKind.Died, p.DeathCause == DeathCause.Illness ? "illness" : null, died);
        await GrieveForAsync(p, died);
    }

    // ---- badges and the journal. Neither helper saves: what they add rides the caller's save,
    // so an action, its moments, its badges and their cailloux land together or not at all.

    public const int JournalCap = 500;
    private static readonly IReadOnlyList<BadgeInfo> NoBadges = Array.Empty<BadgeInfo>();

    // Adds a moment, then trims this Plynling's journal to JournalCap, oldest first — counting
    // moments added earlier in this same unit of work, so several in one save cannot overshoot.
    // The new moments are candidates too: a backdated one older than everything kept goes itself
    // rather than pushing out a newer memory (unsaved, its Id is 0 — on a tie, stored ones go first).
    private async Task AddMomentAsync(Plynling p, JournalKind kind, string? detail, DateTimeOffset at)
    {
        _db_context.PlynlingJournalEntries.Add(new PlynlingJournalEntry { PlynlingId = p.Id, At = at, Kind = kind, Detail = detail });

        var stored = await _db_context.PlynlingJournalEntries.Where(e => e.PlynlingId == p.Id).ToListAsync();
        var kept = stored.Where(e => _db_context.Entry(e).State != EntityState.Deleted);
        var pending = _db_context.ChangeTracker.Entries<PlynlingJournalEntry>()
            .Where(e => e.State == EntityState.Added && e.Entity.PlynlingId == p.Id)
            .Select(e => e.Entity);
        var journal = kept.Concat(pending).ToList();
        var over = journal.Count - JournalCap;
        if (over > 0)
            _db_context.PlynlingJournalEntries.RemoveRange(
                journal.OrderBy(e => e.At).ThenBy(e => e.Id == 0).ThenBy(e => e.Id).Take(over));
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
        // The bot's own Plynling earns its badges but is paid nothing: it would open a wallet for
        // the bot and put cailloux from nowhere into /admin dashboard.
        if (reward > 0 && !PlynlingMascot.Is(p))
        {
            var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, p.GuildId, p.OwnerId);
            wallet.Balance += reward;
            await EconomyLog.AddAsync(_db_context, p.GuildId, EconomyLog.EarnBadge, reward, now);
        }
        return fresh;
    }

    // A read that finds it fell sick or recovered (a morning played on this read) creates that
    // on-action's event here, after the save — any read may be the first to see it, not only the sweep.
    private async Task<Plynling?> SettledAsync(Plynling? plynling, DateTimeOffset now)
    {
        if (plynling is not null && PlynlingLife.Settle(plynling, now))
        {
            await FlushMomentsAsync(plynling);
            await _db_context.SaveChangesAsync();
            await FlushOnActionsAsync(now);
        }
        return plynling;
    }

    // Writes the moments a Settle produced (fell sick, recovered) into the journal, and queues their
    // on-actions. Not saved: the caller's save carries them, like every other moment; the caller flushes
    // the on-actions after it.
    public async Task FlushMomentsAsync(Plynling p)
    {
        foreach (var (kind, at) in p.PendingMoments)
        {
            await AddMomentAsync(p, kind, null, at);
            if (kind == JournalKind.FellSick) QueueOnAction(OnAction.FellSick, p);
            if (kind == JournalKind.Recovered) QueueOnAction(OnAction.Recovered, p);
        }
        p.PendingMoments.Clear();
    }

    // The {T} of a trait reveal: the traits of a kind this pass just added, as "key,key". Null without.
    private static string? RevealText(IReadOnlyList<TraitInfo> added, TraitKind kind)
    {
        var keys = added.Where(t => t.Kind == kind).Select(t => t.Key).ToList();
        return keys.Count == 0 ? null : string.Join(",", keys);
    }
}
