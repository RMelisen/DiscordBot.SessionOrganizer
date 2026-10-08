using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

public enum EventPickOutcome { Done, NotOwner, Gone, NotAvailable, Unknown, Frozen }

// Told: every instance resolved by this pick, for the caller to tell — the picked one, then any
// answer the mascot gave at once. Gender: the Plynling's, for a refusal that names it (Frozen).
public sealed record EventPick(EventPickOutcome Outcome, int PendingLeft = 0, IReadOnlyList<int>? Told = null,
    PlynlingGender Gender = PlynlingGender.Male);

// Events, in the same class (and so the same AppDbContext) as the rest of the Plynling's state: an
// event's growth, its relation change, its badges and its journal moment land in one save. The rules
// are pure in Helpers/PlynlingEventEngine; this file only loads, applies and stores.
public partial class PlynlingService
{
    // With `other`, the context also knows the pair: their relation, hidden compatibility, whether
    // either is taken and whether the visit rules would let them couple, and the other's stats (for a
    // duel).
    public async Task<EventContext> GetEventContextAsync(Plynling p, DateTimeOffset now, Plynling? other = null)
    {
        var traits = await GetTraitsAsync(p);
        var ctx = new EventContext(p, traits, PlynlingStats.Compute(p, traits), PlynlingLife.Stage(p, now));
        if (other is null) return ctx;
        return ctx with { Other = await TargetInfoAsync(p, other), OtherStats = PlynlingStats.Compute(other, await GetTraitsAsync(other)) };
    }

    // Who `other` is to `p`: their relation, hidden compatibility, whether either is taken, and whether
    // the visit rules would let them couple. Enough for a target condition, without either's traits.
    private async Task<TargetInfo> TargetInfoAsync(Plynling p, Plynling other)
    {
        var relation = await FindRelationAsync(p.Id, other.Id);
        var bond = relation?.Bond ?? PlynlingBond.Acquaintances;
        var affinity = relation?.Affinity ?? 0;
        var taken = await InCoupleAsync(p.Id, other.Id) || await InCoupleAsync(other.Id, p.Id);
        var (lo, hi) = Pair(p.Id, other.Id);
        return new TargetInfo(other, bond, affinity, PlynlingBonds.Compatibility(lo, hi), taken,
            PlynlingBonds.CanConfess(bond, affinity, p, other, taken));
    }

    // The other Plynling of an instance, if it has one (settled; may be dead or frozen).
    private async Task<Plynling?> TargetOfAsync(PlynlingEventInstance inst, DateTimeOffset now) =>
        inst.TargetPlynlingId is { } tid ? await GetByIdAsync(tid, now) : null;

    // The responses among `instances` whose asker is gone — abandoned (its id was set null) or dead:
    // nobody is left to answer, so they are cancelled and never offered. A plain read, no settle: an
    // asker that starved unseen is caught once any read has settled it.
    private async Task<HashSet<int>> GoneAskerResponsesAsync(IReadOnlyCollection<PlynlingEventInstance> instances)
    {
        var responses = instances.Where(i => PlynlingEvents.ByKey(i.EventKey)?.Type == EventType.Response).ToList();
        if (responses.Count == 0) return new HashSet<int>();
        var askerIds = responses.Select(i => i.TargetPlynlingId).OfType<int>().Distinct().ToList();
        var dead = (await _db_context.Plynlings.Where(x => askerIds.Contains(x.Id) && x.DiedAt != null).Select(x => x.Id).ToListAsync())
            .ToHashSet();
        return responses.Where(i => i.TargetPlynlingId is not { } asker || dead.Contains(asker)).Select(i => i.Id).ToHashSet();
    }

    // Open = neither resolved nor cancelled (pending, or a follow-up not yet available). Dates are
    // compared in memory: SQLite cannot translate DateTimeOffset comparisons.
    private async Task<List<PlynlingEventInstance>> OpenEventsAsync(int plynlingId) =>
        await _db_context.PlynlingEventInstances
            .Where(i => i.PlynlingId == plynlingId && i.ResolvedAt == null && i.CancelledAt == null)
            .OrderBy(i => i.Id)
            .ToListAsync();

    // An event removed from the catalog is never offered (the sweep cancels it within the hour): it
    // would stand first in the queue, unopenable, in front of the real ones. Nor is a response to an
    // asker who is gone.
    public async Task<List<PlynlingEventInstance>> GetPendingEventsAsync(Plynling p, DateTimeOffset now)
    {
        var open = await OpenEventsAsync(p.Id);
        var gone = await GoneAskerResponsesAsync(open);
        return open.Where(i => i.AvailableAt <= now && PlynlingEvents.ByKey(i.EventKey) is not null && !gone.Contains(i.Id)).ToList();
    }

    // Nothing to offer dead or frozen: nothing happens to a frozen Plynling, its owner's choices included.
    public async Task<int> CountPendingEventsAsync(Plynling p, DateTimeOffset now) =>
        p.DiedAt is null && p.FrozenAt is null ? (await GetPendingEventsAsync(p, now)).Count : 0;

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
        if (p.FrozenAt is not null) return new(EventPickOutcome.Frozen, Gender: p.Gender);
        if (inst.ResolvedAt is not null || inst.CancelledAt is not null || p.DiedAt is not null || inst.AvailableAt > now)
            return new(EventPickOutcome.Gone);
        if (PlynlingEvents.ByKey(inst.EventKey) is not { } def) return new(EventPickOutcome.Gone);
        var target = await TargetOfAsync(inst, now);
        if (def.Type == EventType.Response && target is not { DiedAt: null })
        {
            // Its asker is gone (abandoned, or dead): nobody is left to answer.
            inst.CancelledAt = now;
            await _db_context.SaveChangesAsync();
            return new(EventPickOutcome.Gone);
        }

        var ctx = await GetEventContextAsync(p, now, target);
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
            DiscardChanges();
            return new(EventPickOutcome.Gone);
        }
        var told = new List<int> { instanceId };
        told.AddRange(await AnswerForMascotAsync(told, now));
        told.AddRange(TakeUntold());     // anything the reads above flushed for her
        return new(EventPickOutcome.Done, await CountPendingEventsAsync(p, now), told);
    }

    // The recent-events window counts the daily draws only: follow-ups, responses, breaks and
    // on-actions would otherwise fill its slots and let the same pulses come back sooner.
    private static readonly string[] PulseKeys =
        PlynlingEvents.All.Where(d => d.Type == EventType.Pulse && d.BreakLevel == 0).Select(d => d.Key).ToArray();

    // The sweep's turn, per Plynling: cancel what a death, the catalog or a gone asker left pending,
    // decide what is due (for the mascot, everything available: a follow-up or a break never waits on
    // her card), then the day's pulse. Saves (an instance needs its id before it can be rolled).
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
        var goneAskers = await GoneAskerResponsesAsync(open);
        foreach (var inst in open.Where(i => PlynlingEvents.ByKey(i.EventKey) is null || goneAskers.Contains(i.Id))) inst.CancelledAt = now;
        if (PlynlingLife.IsFrozen(p))
        {
            await _db_context.SaveChangesAsync();
            return told;
        }

        var mascot = PlynlingMascot.Is(p);
        foreach (var inst in open.Where(i => i.CancelledAt is null && i.AvailableAt <= now
                                             && (mascot || PlynlingEventEngine.DueAt(i, p) <= now)))
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
                    .Where(i => i.PlynlingId == p.Id && i.ResolvedAt != null && PulseKeys.Contains(i.EventKey))
                    .OrderByDescending(i => i.Id).Take(PlynlingEventEngine.RecentWindow)
                    .Select(i => i.EventKey).ToListAsync();
                // Never the same event twice in the queue: what still waits is out of the draw.
                var waiting = open.Where(i => i.ResolvedAt is null && i.CancelledAt is null).Select(i => i.EventKey).ToHashSet();
                var defs = PlynlingEvents.All.Where(d => !waiting.Contains(d.Key));
                // Who each candidate is to it, so a social event is drawn only when a candidate meets
                // its target condition (a declaration needs someone it could couple with).
                var infos = new Dictionary<int, TargetInfo>();
                foreach (var id in targets.Values.SelectMany(v => v).Distinct())
                    if (await GetByIdAsync(id, now) is { DiedAt: null, FrozenAt: null } t)
                        infos[id] = await TargetInfoAsync(p, t);
                IReadOnlyList<int> CandidatesFor(EventDef d) =>
                    targets[d.Target].Where(id => infos.TryGetValue(id, out var info) && (d.TargetCondition?.Invoke(info) ?? true)).ToList();
                if (PlynlingEventEngine.PickPulse(p.Id, day, defs, ctx, recent, d => CandidatesFor(d).Count > 0) is { } def)
                {
                    var targetId = def.Target == TargetKind.None ? null : PlynlingEventEngine.PickTarget(p.Id, day, CandidatesFor(def));
                    var inst = await CreateEventAsync(p, def, targetId, now);
                    if (mascot)
                    {
                        // She decides at once and never keeps anything waiting.
                        await ResolveAloneAsync(p, inst, now);
                        told.Add(inst.Id);
                    }
                }
            }
        }
        await _db_context.SaveChangesAsync();
        // Anything it resolved that asked the mascot: she answers now, in character.
        told.AddRange(await AnswerForMascotAsync(told, now));
        return told;
    }

    // Adds an instance without saving — inside another unit of work (a break queued by an event).
    private PlynlingEventInstance QueueEvent(Plynling p, EventDef def, int? targetId, DateTimeOffset now)
    {
        var inst = new PlynlingEventInstance
        {
            PlynlingId = p.Id, EventKey = def.Key, TargetPlynlingId = targetId,
            CreatedAt = now, AvailableAt = now, ExpiresAt = now + PlynlingEventEngine.Lifetime,
        };
        _db_context.PlynlingEventInstances.Add(inst);
        return inst;
    }

    // Saves, so the instance has its id (rolls are hashed from it).
    public async Task<PlynlingEventInstance> CreateEventAsync(Plynling p, EventDef def, int? targetId, DateTimeOffset now)
    {
        var inst = QueueEvent(p, def, targetId, now);
        await _db_context.SaveChangesAsync();
        return inst;
    }

    private static readonly string[] BreakKeys = PlynlingEvents.All.Where(d => d.BreakLevel > 0).Select(d => d.Key).ToArray();

    // The mental break for each stress level above `fromLevel` up to its current one, whatever the
    // pulse cap says — but never a second one for a level whose break still waits: stress can dip
    // below a level overnight and climb back while it does. Never saves.
    private async Task QueueBreaksAsync(Plynling p, int fromLevel, int salt, DateTimeOffset now)
    {
        var level = PlynlingStress.Level(p.Stress);
        if (level <= fromLevel) return;
        // Stored and queued in this unit of work alike; ResolvedAt re-checked in memory, for one resolved
        // in this unit and not saved yet.
        var waiting = (await _db_context.PlynlingEventInstances
                .Where(i => i.PlynlingId == p.Id && i.ResolvedAt == null && i.CancelledAt == null && BreakKeys.Contains(i.EventKey))
                .ToListAsync())
            .Concat(_db_context.ChangeTracker.Entries<PlynlingEventInstance>()
                .Where(e => e.State == EntityState.Added && e.Entity.PlynlingId == p.Id).Select(e => e.Entity))
            .Where(i => i.ResolvedAt is null && i.CancelledAt is null)
            .Select(i => PlynlingEvents.ByKey(i.EventKey)?.BreakLevel ?? 0)
            .ToHashSet();
        for (var l = fromLevel + 1; l <= level; l++)
            if (!waiting.Contains(l) && PlynlingEventEngine.PickBreak(p.Id, salt, l, PlynlingEvents.All) is { } breakDef)
                QueueEvent(p, breakDef, null, now);
    }

    // The bond an event moved, for its story: the first "before" seen and the last "after".
    private static void RecordBond(PlynlingEventInstance inst, (PlynlingBond Before, PlynlingBond After) change)
    {
        inst.BondBefore ??= change.Before;
        inst.BondAfter = change.After;
    }

    // In character: a response by acceptance (affinity, compatibility), anything else by its axes.
    public async Task ResolveAloneAsync(Plynling p, PlynlingEventInstance inst, DateTimeOffset now)
    {
        var def = PlynlingEvents.ByKey(inst.EventKey)!;
        var ctx = await GetEventContextAsync(p, now, await TargetOfAsync(inst, now));
        var option = def.Type == EventType.Response
            ? PlynlingEventEngine.DecideResponse(def, ctx, inst.Id)
            : PlynlingEventEngine.DecideAlone(def, ctx, inst.Id);
        await ApplyEventAsync(p, inst, def, option, ctx, decidedAlone: true, now);
    }

    // Set when an event asked the mascot: she answers right after the save (PickEventAsync, TickEventsAsync).
    private bool _askedMascot;

    // The responses these events queued on the mascot: she answers in character, now. Saves. Takes every
    // parent at once — the flag says only that one of them asked her.
    private async Task<IReadOnlyList<int>> AnswerForMascotAsync(IReadOnlyCollection<int> parentIds, DateTimeOffset now)
    {
        if (!_askedMascot || parentIds.Count == 0) return Array.Empty<int>();
        _askedMascot = false;
        var ids = parentIds.ToList();
        var answered = new List<int>();
        foreach (var reply in await _db_context.PlynlingEventInstances
                     .Where(i => i.ParentInstanceId != null && ids.Contains(i.ParentInstanceId.Value) && i.ResolvedAt == null && i.CancelledAt == null)
                     .ToListAsync())
        {
            var mascot = await GetByIdAsync(reply.PlynlingId, now);
            // Frozen, she answers after the thaw, like anyone (AskTarget does not ask a frozen one anyway).
            if (mascot is null || !PlynlingMascot.Is(mascot) || mascot.FrozenAt is not null || PlynlingEvents.ByKey(reply.EventKey) is null)
                continue;
            await ResolveAloneAsync(mascot, reply, now);
            await FlushMomentsAsync(mascot);
            answered.Add(reply.Id);
        }
        await _db_context.SaveChangesAsync();
        return answered;
    }

    // The roll, the effects, the journal. Never saves. A target that is gone (abandoned, dead, frozen)
    // simply skips the affinity: the event still happened to this one. Deciding alone skips what
    // AppliesWhenAlone forbids; each stress level climbed queues its mental break. The target is the
    // one `ctx` was built with — loaded (and settled, which may save) before anything here changes.
    private async Task ApplyEventAsync(Plynling p, PlynlingEventInstance inst, EventDef def, EventOption option,
        EventContext ctx, bool decidedAlone, DateTimeOffset now)
    {
        var target = ctx.Other?.Target;
        var stressBefore = p.Stress;
        var levelBefore = PlynlingStress.Level(p.Stress);
        // The owner's own choice may cost stress, scaled by its traits. Alone, the option had no cost.
        if (!decidedAlone)
            PlynlingLife.AddStress(p, now, PlynlingStress.Scaled(PlynlingEventEngine.StressCost(option, ctx), ctx.Traits));

        var success = true;
        if (option.Challenge is { } challenge)
        {
            inst.ChancePercent = PlynlingEventEngine.Chance(challenge, ctx);
            success = PlynlingEventEngine.Succeeds(inst.Id, inst.ChancePercent.Value);
            inst.ChallengeSucceeded = success;
        }
        foreach (var effect in success ? option.OnSuccess : option.OnFailure)
        {
            if (!PlynlingEventEngine.Applies(effect, def, decidedAlone)) continue;
            switch (effect)
            {
                case GrowStat g:
                    PlynlingStats.AddGrowth(p, g.Stat, g.Amount);
                    break;
                case AffinityShift a when target is { DiedAt: null, FrozenAt: null }:
                    RecordBond(inst, await ShiftAffinityAsync(p, target, a.Delta, now));
                    break;
                case StressChange s when s.Amount > 0:
                    PlynlingLife.AddStress(p, now, PlynlingStress.Scaled(s.Amount, ctx.Traits));
                    break;
                case StressChange s:
                    PlynlingLife.Relieve(p, now, -s.Amount);
                    break;
                case ApplyModifier m when PlynlingModifiers.ByKey(m.Key) is { } modifier:
                    PlynlingLife.AddModifier(p, now, modifier);
                    break;
                case GiveCailloux c when !PlynlingMascot.Is(p):
                    var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, p.GuildId, p.OwnerId);
                    wallet.Balance += c.Amount;
                    await EconomyLog.AddAsync(_db_context, p.GuildId, EconomyLog.EarnEvent, c.Amount, now);
                    break;
                case GiveItem gi when !PlynlingMascot.Is(p) && ItemCatalog.ByKey(gi.ItemKey) is { } item:
                    await InventoryService.GrantAsync(_db_context, p.GuildId, p.OwnerId, item, now);
                    break;
                case LiftNeed n:
                    PlynlingLife.Lift(p, now, n.Need, n.Amount);
                    break;
                case GainCoping gc:
                    var held = ctx.Traits.Select(t => t.Key).ToList();
                    if ((gc.TraitKey is { } named ? PlynlingTraits.CopingIfOwed(named, held) : PlynlingTraits.DrawCoping(p.Id, inst.Id, held)) is { } coping)
                    {
                        _db_context.PlynlingTraits.Add(new PlynlingTrait { PlynlingId = p.Id, Key = coping.Key, Kind = TraitKind.Coping, AcquiredAt = now });
                        await AddMomentAsync(p, JournalKind.TraitGained, coping.Key, now);
                        inst.GainedTraitKey = coping.Key;
                        RefreshStressCache(p, held.Append(coping.Key));
                    }
                    break;
                case FollowUp f when PlynlingEvents.ByKey(f.EventKey) is { } next:
                    // Comes back later, about the same Plynling; a death cancels it with the rest.
                    var later = QueueEvent(p, next, inst.TargetPlynlingId, now);
                    later.AvailableAt = PlynlingEventEngine.FollowUpAt(inst.Id, f, now);
                    later.ExpiresAt = later.AvailableAt + PlynlingEventEngine.Lifetime;
                    later.ParentInstanceId = inst.Id;
                    break;
                case AskTarget ask when target is { DiedAt: null, FrozenAt: null } && PlynlingEvents.ByKey(ask.ResponseKey) is { } reply:
                    // Queued on the other Plynling, pointing back at this one; its owner answers.
                    QueueEvent(target, reply, p.Id, now).ParentInstanceId = inst.Id;
                    _askedMascot |= PlynlingMascot.Is(target);
                    break;
                case SetAffinityAtLeast lift when target is { DiedAt: null }:
                    if (await LiftAffinityAsync(p, target, lift.Value, now) is { } lifted)
                        RecordBond(inst, lifted);
                    break;
                case Couple when target is { DiedAt: null } && ctx.Other is { CanCouple: true }:
                    // The visit rules, checked again now: someone may have coupled since the ask.
                    // When they no longer allow it, BondAfter stays unset and the story says « trop tard ».
                    RecordBond(inst, await MakeCoupleAsync(p, target, now));
                    break;
                case Heartbreak when target is { DiedAt: null, FrozenAt: null }:
                    // In a response: the one who declared — whose owner chose to — is saddened. Not
                    // while frozen: nothing about a frozen Plynling moves.
                    PlynlingLife.Sadden(target, now, PlynlingBonds.HeartbreakSadness);
                    await AddMomentAsync(target, JournalKind.Heartbroken, p.Name, now);
                    RecordBond(inst, await ShiftAffinityAsync(p, target, -PlynlingBonds.HeartbreakLoss, now));
                    break;
            }
        }
        inst.StressDelta = p.Stress - stressBefore;
        inst.ResolvedAt = now;
        inst.OptionKey = option.Key;
        inst.DecidedAlone = decidedAlone;
        await AddMomentAsync(p, JournalKind.EventStory, def.Key, now);

        // Each level climbed brings on its mental break, whatever the pulse cap says.
        await QueueBreaksAsync(p, levelBefore, inst.Id, now);
    }

    // Who a social event may involve, by kind: living, unfrozen Plynlings of the same guild, not
    // already in a social event with this one in the last 24 h (either direction).
    private async Task<Dictionary<TargetKind, IReadOnlyList<int>>> TargetCandidatesAsync(Plynling p, DateTimeOffset now)
    {
        var others = await _db_context.Plynlings
            .Where(x => x.GuildId == p.GuildId && x.Id != p.Id && x.DiedAt == null && x.FrozenAt == null)
            .Select(x => x.Id).ToListAsync();
        // Only the three columns needed (the history grows without bound); the date window in memory.
        var recentPairs = (await _db_context.PlynlingEventInstances
                .Where(i => (i.PlynlingId == p.Id && i.TargetPlynlingId != null) || i.TargetPlynlingId == p.Id)
                .Select(i => new { i.PlynlingId, i.TargetPlynlingId, i.CreatedAt })
                .ToListAsync())
            .Where(i => now - i.CreatedAt < TimeSpan.FromHours(24))
            .Select(i => i.PlynlingId == p.Id ? i.TargetPlynlingId!.Value : i.PlynlingId)
            .ToHashSet();
        var relations = (await _db_context.PlynlingRelations
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
        var parentTitle = inst.ParentInstanceId is { } pid && await GetEventInstanceAsync(pid) is { } parent
            ? PlynlingEvents.ByKey(parent.EventKey)?.Title : null;
        // Its children include follow-ups; only a response answers an ask. None open and none resolved:
        // the ask was cancelled, or never queued (the other one was frozen or gone).
        var replies = (await _db_context.PlynlingEventInstances.Where(i => i.ParentInstanceId == inst.Id).ToListAsync())
            .Where(i => PlynlingEvents.ByKey(i.EventKey)?.Type == EventType.Response).ToList();
        var reply = replies.Any(i => i.ResolvedAt is not null) ? ReplyState.Answered
            : replies.Any(i => i.CancelledAt is null) ? ReplyState.Waiting
            : ReplyState.Lost;
        return PlynlingEventStory.Build(inst, EventCast.Of(self, now), target is null ? null : EventCast.Of(target, now), parentTitle, reply);
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
        await QueueBreaksAsync(p, before, (int)(now.ToUnixTimeSeconds() % 100000), now);
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
    // Turning ado is dated by its ado traits, stored as it happened: the age leaves out time spent
    // frozen, so "now minus the age past the threshold" would start the ado years late and drop the
    // first choices of anyone frozen meanwhile. The age stands in only when none are stored yet (a
    // backfill drawing every slot at once).
    private async Task<Func<TraitInfo, double>?> AdultWeightAsync(Plynling p, DateTimeOffset now)
    {
        var adoTraitsAt = await _db_context.PlynlingTraits
            .Where(t => t.PlynlingId == p.Id && t.Kind == TraitKind.Personality)
            .Select(t => t.AcquiredAt).ToListAsync();
        var teenFrom = adoTraitsAt.Count > 0
            ? adoTraitsAt.Min()
            : now - (PlynlingLife.Age(p, now) - PlynlingLife.StageStart(PlynlingStage.Teen));
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
        var ado = PlynlingEventEngine.AdoHistory(resolved);
        return t => PlynlingEventEngine.AdultTraitWeight(t, ado);
    }

    // ---- on-actions: life's moments that bring an event --------------------------------------------

    private const int AfterVisitSalt = 350;
    private const double AfterVisitChance = 0.33;

    // On-actions noticed during a unit of work, created after its save (FlushOnActionsAsync): an event
    // must never be what breaks an adoption, a visit or a sweep. TraitText is the {T} of a trait reveal.
    private readonly List<(OnAction Kind, int PlynlingId, int? TargetId, string? TraitText)> _onActions = new();

    private void QueueOnAction(OnAction kind, Plynling p, int? targetId = null, string? traitText = null) =>
        _onActions.Add((kind, p.Id, targetId, traitText));

    // Saved events the mascot decided at once from an on-action, not told yet. Kept here rather than
    // returned, because a flush also runs deep inside any read that settles (SettledAsync), where nobody
    // holds a channel. Whoever does tells them after its own story (TakeUntold): the sweep, a pick, a visit.
    private readonly List<int> _untold = new();

    public IReadOnlyList<int> TakeUntold()
    {
        var ids = _untold.ToList();
        _untold.Clear();
        return ids;
    }

    /// <summary>
    /// Creates the queued on-actions' events, each in its own save and its own try: one that throws is
    /// logged and its half-made changes dropped, never the caller's (which saved before calling). Call
    /// only right after a save. What the mascot decides at once waits in <see cref="TakeUntold"/>.
    /// </summary>
    public async Task FlushOnActionsAsync(DateTimeOffset now)
    {
        while (_onActions.Count > 0)
        {
            // Copied then cleared: reading a Plynling below may settle it and queue more, handled next round.
            var queued = _onActions.ToList();
            _onActions.Clear();
            foreach (var (kind, plynlingId, targetId, traitText) in queued)
            {
                try
                {
                    var p = await GetByIdAsync(plynlingId, now);
                    if (p is null || p.DiedAt is not null || p.FrozenAt is not null) continue;
                    // A visit brings its event only sometimes (hashed per Plynling and day).
                    if (kind == OnAction.AfterVisit && StableRoll.Unit(p.Id, AppTime.DayKey(now), AfterVisitSalt) >= AfterVisitChance) continue;
                    var other = targetId is { } tid ? await GetByIdAsync(tid, now) : null;
                    var ctx = await GetEventContextAsync(p, now, other);
                    if (PlynlingEventEngine.PickTriggered(kind, p.Id, (int)(now.ToUnixTimeSeconds() % 1_000_000), PlynlingEvents.All, ctx) is not { } def)
                        continue;
                    // Never the same event twice in the queue (several visits on a lucky day).
                    if ((await OpenEventsAsync(p.Id)).Any(i => i.EventKey == def.Key)) continue;
                    var inst = QueueEvent(p, def, def.Target == TargetKind.None ? null : targetId, now);
                    inst.GainedTraitKey = traitText;
                    await _db_context.SaveChangesAsync();
                    if (PlynlingMascot.Is(p))
                    {
                        await ResolveAloneAsync(p, inst, now);
                        await FlushMomentsAsync(p);
                        await _db_context.SaveChangesAsync();
                        _untold.Add(inst.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "On-action {Kind} for Plynling {PlynlingId} failed.", kind, plynlingId);
                    ResetTracked();
                }
            }
        }
    }
}
