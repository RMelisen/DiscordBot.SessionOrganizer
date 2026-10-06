using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum EventPickOutcome { Done, NotOwner, Gone, NotAvailable, Unknown }

public sealed record EventPick(EventPickOutcome Outcome, int PendingLeft = 0);

// Events, in the same class (and so the same AppDbContext) as the rest of the Plynling's state: an
// event's growth, its relation change, its badges and its journal moment land in one save. The rules
// are pure in Helpers/PlynlingEventEngine; this file only loads, applies and stores.
public partial class PlynlingService
{
    public async Task<EventContext> GetEventContextAsync(Plynling p, DateTimeOffset now)
    {
        var traits = await GetTraitsAsync(p);
        return new EventContext(p, traits, PlynlingStats.Compute(p, traits), PlynlingLife.Stage(p, now));
    }

    // Open = neither resolved nor cancelled (pending, or a follow-up not yet available). Dates are
    // compared in memory: SQLite cannot translate DateTimeOffset comparisons.
    private async Task<List<PlynlingEventInstance>> OpenEventsAsync(int plynlingId) =>
        await _db_context.PlynlingEventInstances
            .Where(i => i.PlynlingId == plynlingId && i.ResolvedAt == null && i.CancelledAt == null)
            .OrderBy(i => i.Id)
            .ToListAsync();

    // An event removed from the catalog is never offered (the sweep cancels it within the hour): it
    // would stand first in the queue, unopenable, in front of the real ones.
    public async Task<List<PlynlingEventInstance>> GetPendingEventsAsync(Plynling p, DateTimeOffset now) =>
        (await OpenEventsAsync(p.Id)).Where(i => i.AvailableAt <= now && PlynlingEvents.ByKey(i.EventKey) is not null).ToList();

    public async Task<int> CountPendingEventsAsync(Plynling p, DateTimeOffset now) =>
        p.DiedAt is null ? (await GetPendingEventsAsync(p, now)).Count : 0;

    public Task<PlynlingEventInstance?> GetEventInstanceAsync(int instanceId) =>
        _db_context.PlynlingEventInstances.FirstOrDefaultAsync(i => i.Id == instanceId);

    // The owner's pick. Re-checks everything inside this unit of work: a sweep may have decided it
    // a moment ago, or the option may no longer be shown (a forged or stale click).
    public async Task<EventPick> PickEventAsync(int instanceId, string optionKey, ulong actorId, DateTimeOffset now)
    {
        var inst = await GetEventInstanceAsync(instanceId);
        if (inst is null) return new(EventPickOutcome.Unknown);
        var p = await GetByIdAsync(inst.PlynlingId, now);
        if (p is null) return new(EventPickOutcome.Unknown);
        if (p.OwnerId != actorId) return new(EventPickOutcome.NotOwner);
        if (inst.ResolvedAt is not null || inst.CancelledAt is not null || p.DiedAt is not null || inst.AvailableAt > now)
            return new(EventPickOutcome.Gone);
        if (PlynlingEvents.ByKey(inst.EventKey) is not { } def) return new(EventPickOutcome.Gone);

        var ctx = await GetEventContextAsync(p, now);
        var option = def.Options.FirstOrDefault(o => o.Key == optionKey);
        if (option is null || !PlynlingEventEngine.Visible(option, ctx)) return new(EventPickOutcome.NotAvailable);

        await ApplyEventAsync(p, inst, def, option, ctx, decidedAlone: false, now);
        await FlushMomentsAsync(p);
        try
        {
            await _db_context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(EventPickOutcome.Gone);
        }
        return new(EventPickOutcome.Done, await CountPendingEventsAsync(p, now));
    }

    // The sweep's turn, per Plynling: cancel what a death or the catalog left pending, decide what
    // expired, then the day's pulse. Saves (an instance needs its id before it can be rolled).
    // Returns the instances resolved now, for the caller to tell after the save.
    public async Task<IReadOnlyList<int>> TickEventsAsync(Plynling p, DateTimeOffset now)
    {
        var told = new List<int>();
        var open = await OpenEventsAsync(p.Id);
        if (p.DiedAt is not null)
        {
            foreach (var inst in open) inst.CancelledAt = now;
            await _db_context.SaveChangesAsync();
            return told;
        }
        foreach (var inst in open.Where(i => PlynlingEvents.ByKey(i.EventKey) is null)) inst.CancelledAt = now;
        if (PlynlingLife.IsFrozen(p))
        {
            await _db_context.SaveChangesAsync();
            return told;
        }

        foreach (var inst in open.Where(i => i.CancelledAt is null && i.AvailableAt <= now && i.ExpiresAt <= now))
        {
            await ResolveAloneAsync(p, inst, now);
            told.Add(inst.Id);
        }

        var day = AppTime.DayKey(now);
        if (p.LastPulseDay != day && now >= PlynlingEventEngine.PulseAt(p.Id, day))
        {
            p.LastPulseDay = day;    // drawn or skipped, today's pulse is spent
            var pendingPulses = open.Count(i => i.ResolvedAt is null && i.CancelledAt is null && i.AvailableAt <= now
                                                && PlynlingEvents.ByKey(i.EventKey)?.Type == EventType.Pulse);
            if (pendingPulses < PlynlingEventEngine.MaxPendingPulses)
            {
                var ctx = await GetEventContextAsync(p, now);
                var targets = await TargetCandidatesAsync(p, now);
                var recent = await _db_context.PlynlingEventInstances
                    .Where(i => i.PlynlingId == p.Id && i.ResolvedAt != null)
                    .OrderByDescending(i => i.Id).Take(PlynlingEventEngine.RecentWindow)
                    .Select(i => i.EventKey).ToListAsync();
                var targetable = targets.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToHashSet();
                // Never the same event twice in the queue: what still waits is out of the draw.
                var waiting = open.Where(i => i.ResolvedAt is null && i.CancelledAt is null).Select(i => i.EventKey).ToHashSet();
                var defs = PlynlingEvents.All.Where(d => !waiting.Contains(d.Key));
                if (PlynlingEventEngine.PickPulse(p.Id, day, defs, ctx, recent, targetable) is { } def)
                {
                    var targetId = def.Target == TargetKind.None ? null : PlynlingEventEngine.PickTarget(p.Id, day, targets[def.Target]);
                    var inst = await CreateEventAsync(p, def, targetId, now);
                    if (PlynlingMascot.Is(p))
                    {
                        // She decides at once and never keeps anything waiting.
                        await ResolveAloneAsync(p, inst, now);
                        told.Add(inst.Id);
                    }
                }
            }
        }
        await _db_context.SaveChangesAsync();
        return told;
    }

    // Adds an instance without saving — inside another unit of work (a break queued by an event).
    public Task<PlynlingEventInstance> QueueEventAsync(Plynling p, EventDef def, int? targetId, DateTimeOffset now)
    {
        var inst = new PlynlingEventInstance
        {
            PlynlingId = p.Id, EventKey = def.Key, TargetPlynlingId = targetId,
            CreatedAt = now, AvailableAt = now, ExpiresAt = now + PlynlingEventEngine.Lifetime,
        };
        _db_context.PlynlingEventInstances.Add(inst);
        return Task.FromResult(inst);
    }

    // Saves, so the instance has its id (rolls are hashed from it).
    public async Task<PlynlingEventInstance> CreateEventAsync(Plynling p, EventDef def, int? targetId, DateTimeOffset now)
    {
        var inst = await QueueEventAsync(p, def, targetId, now);
        await _db_context.SaveChangesAsync();
        return inst;
    }

    public async Task ResolveAloneAsync(Plynling p, PlynlingEventInstance inst, DateTimeOffset now)
    {
        var def = PlynlingEvents.ByKey(inst.EventKey)!;
        var ctx = await GetEventContextAsync(p, now);
        await ApplyEventAsync(p, inst, def, PlynlingEventEngine.DecideAlone(def, ctx, inst.Id), ctx, decidedAlone: true, now);
    }

    // The roll, the effects, the journal. Never saves. A target that is gone (abandoned, dead, frozen)
    // simply skips the affinity: the event still happened to this one. Deciding alone skips what
    // AppliesWhenAlone forbids; each stress level climbed queues its mental break.
    private async Task ApplyEventAsync(Plynling p, PlynlingEventInstance inst, EventDef def, EventOption option,
        EventContext ctx, bool decidedAlone, DateTimeOffset now)
    {
        // Loaded first: reading a Plynling settles it and may save, which must happen before this
        // unit of work starts changing anything.
        var target = inst.TargetPlynlingId is { } tid ? await GetByIdAsync(tid, now) : null;
        var stressBefore = p.Stress;
        var climbed = 0;
        // The owner's own choice may cost stress, scaled by its traits. Alone, the option had no cost.
        if (!decidedAlone)
            climbed += PlynlingLife.AddStress(p, now, PlynlingStress.Scaled(PlynlingEventEngine.StressCost(option, ctx), ctx.Traits));

        var success = true;
        if (option.Challenge is { } challenge)
        {
            inst.ChancePercent = PlynlingEventEngine.Chance(challenge, ctx);
            success = PlynlingEventEngine.Succeeds(inst.Id, inst.ChancePercent.Value);
            inst.ChallengeSucceeded = success;
        }
        foreach (var effect in success ? option.OnSuccess : option.OnFailure)
        {
            if (decidedAlone && !PlynlingEventEngine.AppliesWhenAlone(effect, def)) continue;
            switch (effect)
            {
                case GrowStat g:
                    PlynlingStats.AddGrowth(p, g.Stat, g.Amount);
                    break;
                case AffinityShift a when target is { DiedAt: null, FrozenAt: null }:
                    var (before, after) = await ShiftAffinityAsync(p, target, a.Delta, now);
                    inst.BondBefore ??= before;
                    inst.BondAfter = after;
                    break;
                case StressChange s when s.Amount > 0:
                    climbed += PlynlingLife.AddStress(p, now, PlynlingStress.Scaled(s.Amount, ctx.Traits));
                    break;
                case StressChange s:
                    PlynlingLife.Relieve(p, now, -s.Amount);
                    break;
                case ApplyModifier m when PlynlingModifiers.ByKey(m.Key) is { } modifier:
                    PlynlingLife.AddModifier(p, now, modifier);
                    break;
                case GainCoping:
                    var held = ctx.Traits.Select(t => t.Key).ToList();
                    if (PlynlingTraits.DrawCoping(p.Id, inst.Id, held) is { } coping)
                    {
                        _db_context.PlynlingTraits.Add(new PlynlingTrait { PlynlingId = p.Id, Key = coping.Key, Kind = TraitKind.Coping, AcquiredAt = now });
                        await AddMomentAsync(p, JournalKind.TraitGained, coping.Key, now);
                        inst.GainedTraitKey = coping.Key;
                        await RefreshStressCacheAsync(p);
                    }
                    break;
            }
        }
        inst.StressDelta = p.Stress - stressBefore;
        inst.ResolvedAt = now;
        inst.OptionKey = option.Key;
        inst.DecidedAlone = decidedAlone;
        await AddMomentAsync(p, JournalKind.EventStory, def.Key, now);

        // Each level climbed brings on its mental break, whatever the pulse cap says.
        var level = PlynlingStress.Level(p.Stress);
        for (var l = level - climbed + 1; l <= level; l++)
            if (PlynlingEventEngine.PickBreak(p.Id, inst.Id, l, PlynlingEvents.All) is { } breakDef)
                await QueueEventAsync(p, breakDef, null, now);
    }

    // Who a social event may involve, by kind: living, unfrozen Plynlings of the same guild, not
    // already in a social event with this one in the last 24 h (either direction). Untracked reads: the
    // sweep's context lives for the whole batch, and another Plynling's instance or a relation tracked
    // here would be served stale later in the pass (an owner's pick, a visit) instead of re-read.
    private async Task<Dictionary<TargetKind, IReadOnlyList<int>>> TargetCandidatesAsync(Plynling p, DateTimeOffset now)
    {
        var others = await _db_context.Plynlings
            .Where(x => x.GuildId == p.GuildId && x.Id != p.Id && x.DiedAt == null && x.FrozenAt == null)
            .Select(x => x.Id).ToListAsync();
        var recentPairs = (await _db_context.PlynlingEventInstances.AsNoTracking()
                .Where(i => (i.PlynlingId == p.Id && i.TargetPlynlingId != null) || i.TargetPlynlingId == p.Id)
                .ToListAsync())
            .Where(i => now - i.CreatedAt < TimeSpan.FromHours(24))
            .Select(i => i.PlynlingId == p.Id ? i.TargetPlynlingId!.Value : i.PlynlingId)
            .ToHashSet();
        var relations = (await _db_context.PlynlingRelations.AsNoTracking()
                .Where(r => r.PlynlingAId == p.Id || r.PlynlingBId == p.Id).ToListAsync())
            .ToDictionary(r => r.PlynlingAId == p.Id ? r.PlynlingBId : r.PlynlingAId, r => r.Bond);
        var free = others.Where(id => !recentPairs.Contains(id)).ToList();
        bool Hostile(PlynlingBond b) => b is PlynlingBond.Rivals or PlynlingBond.Enemies;
        return new Dictionary<TargetKind, IReadOnlyList<int>>
        {
            [TargetKind.Known] = free.Where(id => relations.TryGetValue(id, out var b) && !Hostile(b)).ToList(),
            [TargetKind.Hostile] = free.Where(id => relations.TryGetValue(id, out var b) && Hostile(b)).ToList(),
            [TargetKind.Anyone] = free,
        };
    }

    // A told event, rebuilt from its row. Null while it is still pending or if it no longer exists.
    // Plain reads, no settle: a story shows names and pictures only.
    public async Task<EventStory?> GetEventStoryAsync(int instanceId, DateTimeOffset now)
    {
        var inst = await GetEventInstanceAsync(instanceId);
        if (inst?.ResolvedAt is null) return null;
        var self = await _db_context.Plynlings.FirstOrDefaultAsync(x => x.Id == inst.PlynlingId);
        if (self is null) return null;
        var target = inst.TargetPlynlingId is { } tid ? await _db_context.Plynlings.FirstOrDefaultAsync(x => x.Id == tid) : null;
        return PlynlingEventStory.Build(inst, EventCast.Of(self, now), target is null ? null : EventCast.Of(target, now));
    }

    // /debug event only: any other living Plynling of the guild, for a forced social event.
    public async Task<int?> AnyOtherLivingIdAsync(Plynling p) =>
        await _db_context.Plynlings.Where(x => x.GuildId == p.GuildId && x.Id != p.Id && x.DiedAt == null)
            .Select(x => (int?)x.Id).FirstOrDefaultAsync();

    // /debug stress: set it, and queue the breaks for any level climbed, as play would. Null without a
    // living Plynling.
    public async Task<string?> DebugStressAsync(ulong guildId, ulong ownerId, int value, DateTimeOffset now)
    {
        var p = await GetCurrentAsync(guildId, ownerId, now);
        if (p is null || p.DiedAt is not null) return null;
        var before = PlynlingStress.Level(p.Stress);
        PlynlingLife.SetStress(p, now, value);
        for (var l = before + 1; l <= PlynlingStress.Level(p.Stress); l++)
            if (PlynlingEventEngine.PickBreak(p.Id, (int)(now.ToUnixTimeSeconds() % 100000), l, PlynlingEvents.All) is { } def)
                await QueueEventAsync(p, def, null, now);
        await FlushMomentsAsync(p);
        await _db_context.SaveChangesAsync();
        return $"🔧 Stress {p.Stress} (niveau {PlynlingStress.Level(p.Stress)}).";
    }

    // /debug modifier: apply (or refresh) or remove one. False without a living Plynling.
    public async Task<bool> DebugModifierAsync(ulong guildId, ulong ownerId, ModifierInfo mod, bool remove, DateTimeOffset now)
    {
        var p = await GetCurrentAsync(guildId, ownerId, now);
        if (p is null || p.DiedAt is not null) return false;
        if (remove) PlynlingLife.RemoveModifier(p, now, mod.Key);
        else PlynlingLife.AddModifier(p, now, mod);
        await FlushMomentsAsync(p);
        await _db_context.SaveChangesAsync();
        return true;
    }

    // /debug event only: make a pending event due now, so the next sweep decides it alone.
    public async Task ExpireEventNowAsync(int instanceId, DateTimeOffset now)
    {
        if (await GetEventInstanceAsync(instanceId) is { } inst)
        {
            inst.ExpiresAt = now;
            await _db_context.SaveChangesAsync();
        }
    }

    // The ado years, for the adulte trait: what it chose (or chose alone) between turning ado and now.
    private async Task<Func<TraitInfo, double>?> AdultWeightAsync(Plynling p, DateTimeOffset now)
    {
        var age = PlynlingLife.Age(p, now);
        var teenFrom = now - (age - PlynlingLife.StageStart(PlynlingStage.Teen));
        var resolved = (await _db_context.PlynlingEventInstances
                .Where(i => i.PlynlingId == p.Id && i.ResolvedAt != null && i.OptionKey != null).ToListAsync())
            .Where(i => i.ResolvedAt >= teenFrom)
            .Select(i => (Def: PlynlingEvents.ByKey(i.EventKey), i.OptionKey, Success: i.ChallengeSucceeded ?? true))
            .Where(x => x.Def is not null)
            .Select(x => (Option: x.Def!.Options.FirstOrDefault(o => o.Key == x.OptionKey), x.Success))
            .Where(x => x.Option is not null)
            .Select(x => (x.Option!, x.Success))
            .ToList();
        if (resolved.Count == 0) return null;
        var (leaning, growth) = PlynlingEventEngine.AdoHistory(resolved);
        return t => PlynlingEventEngine.AdultTraitWeight(t, leaning, growth);
    }
}
