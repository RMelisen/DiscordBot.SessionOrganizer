using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Story events' reads and writes (design spec 2026-11-17, "How C works"): what a story knows about a
// Plynling, and the effects only story files give — memories, marks, a trait swap. Same class, so the
// same unit of work as the event that applies them. The pure rules (flags, endings, starts) are in
// Helpers/Plynling/Stories/StoryRuns.
//
// Every write here may run twice: a unit re-run after a conflict starts from a clean context and
// re-reads, and two effects of one option see each other. So every check reads the rows *and* what
// this unit of work added or removed (the DbSet's Local view: tracked rows, Added included, Deleted
// left out) — reading the database alone would add a second row, and the unique index would then
// refuse that save and every later attempt's identical one.
public partial class PlynlingService
{
    // Each arc's first scene key → its arc, for LastStarted (read from PlynlingEvents.All, another class), its
    // old keys (`was:`) included: an arc started before it moved to a story file counts as started.
    private static readonly IReadOnlyDictionary<string, StoryArc> StoryOpeners = StoryRuns.Openers(PlynlingEvents.All, PlynlingEvents.Aliases);
    private static readonly string[] StoryOpenerKeys = StoryOpeners.Keys.ToArray();

    // Everything a story's conditions read about `p`, with `inst`'s run flags (none for a draw). Plain
    // reads, no settle.
    private async Task<StoryKnowledge> StoryKnowledgeAsync(Plynling p, PlynlingEventInstance? inst, DateTimeOffset now)
    {
        var memories = await HeldMemoriesAsync(p.Id);
        var marks = await HeldMarksAsync(p.Id);
        var history = await StoryHistoryAsync(p.Id);
        var (ended, arcFlags) = StoryRuns.Finished(history.Select(i => (i.EventKey, i.EndingKey, i.Flags)));
        var (lastStarted, lastLong) = StoryRuns.Starts(history.Select(i => (i.EventKey, i.CreatedAt)), StoryOpeners);
        var standing = StoryStanding.Compute(memories.Select(m => m.MemoryKey), StoryCatalog.Memories,
            memories.Where(m => m.RevealedAt is not null).Select(m => m.MemoryKey), StoryCatalog.Cast.Keys);
        return new StoryKnowledge(
            StoryRuns.ParseFlags(inst?.Flags),
            memories.Select(m => m.MemoryKey).ToHashSet(),
            marks.Select(m => (m.Kind, m.Key)).ToHashSet(),
            ended, arcFlags, standing, lastStarted, lastLong, ItemCatalog.SeasonAt(now), now);
    }

    // The instances a story's knowledge reads: those that reached an ending, and each arc's first
    // scenes. Tracked, joined by what this unit holds, and filtered on their current values: an ending
    // reached in this unit and not saved yet counts already. Dates are read in memory.
    private async Task<List<PlynlingEventInstance>> StoryHistoryAsync(int plynlingId)
    {
        var stored = await _db_context.PlynlingEventInstances
            .Where(i => i.PlynlingId == plynlingId && (i.EndingKey != null || StoryOpenerKeys.Contains(i.EventKey)))
            .ToListAsync();
        return stored
            .Concat(_db_context.ChangeTracker.Entries<PlynlingEventInstance>()
                .Where(e => e.State is (EntityState.Added or EntityState.Modified or EntityState.Unchanged) && e.Entity.PlynlingId == plynlingId)
                .Select(e => e.Entity))
            .Distinct()
            .Where(i => i.EndingKey is not null || StoryOpeners.ContainsKey(i.EventKey))
            .ToList();
    }

    // Held memories: stored and added in this unit, without those forgotten in it.
    private async Task<List<PlynlingMemory>> HeldMemoriesAsync(int plynlingId)
    {
        await _db_context.PlynlingMemories.Where(m => m.PlynlingId == plynlingId).LoadAsync();
        return _db_context.PlynlingMemories.Local.Where(m => m.PlynlingId == plynlingId).ToList();
    }

    private async Task<List<PlynlingMark>> HeldMarksAsync(int plynlingId)
    {
        await _db_context.PlynlingMarks.Where(m => m.PlynlingId == plynlingId).LoadAsync();
        return _db_context.PlynlingMarks.Local.Where(m => m.PlynlingId == plynlingId).ToList();
    }

    // A story write changes the Plynling's own state, so it moves the Plynling's Version. That does not
    // make two units writing the same memory or mark at once (a click and the sweep, on two of its
    // events) retry: EF orders the INSERT before the UPDATE, so the slower one fails on the unique index
    // with a plain DbUpdateException. The race costs one failed attempt (logged per item in the sweep; on
    // a click the pick fails after the defer and the person clicks again); the next attempt finds the row.
    private void TouchForStory(Plynling p) => _db_context.Entry(p).Property(x => x.Version).IsModified = true;

    private async Task RememberAsync(Plynling p, PlynlingEventInstance inst, string key, DateTimeOffset now)
    {
        if ((await HeldMemoriesAsync(p.Id)).Any(m => m.MemoryKey == key)) return;
        TouchForStory(p);
        // Forgotten earlier in this unit: the row stays rather than a second one beside it.
        var forgotten = _db_context.ChangeTracker.Entries<PlynlingMemory>()
            .FirstOrDefault(e => e.State == EntityState.Deleted && e.Entity.PlynlingId == p.Id && e.Entity.MemoryKey == key);
        if (forgotten is not null)
        {
            forgotten.State = EntityState.Modified;
            return;
        }
        _db_context.PlynlingMemories.Add(new PlynlingMemory { PlynlingId = p.Id, MemoryKey = key, AcquiredAt = now, InstanceId = inst.Id });
    }

    private async Task ForgetAsync(Plynling p, string key)
    {
        if ((await HeldMemoriesAsync(p.Id)).FirstOrDefault(m => m.MemoryKey == key) is not { } memory) return;
        TouchForStory(p);
        _db_context.PlynlingMemories.Remove(memory);   // one added in this unit is simply dropped
    }

    // A memory the village now talks about. Only one it holds, and only once.
    private async Task RevealAsync(Plynling p, string key, DateTimeOffset now)
    {
        if ((await HeldMemoriesAsync(p.Id)).FirstOrDefault(m => m.MemoryKey == key) is not { RevealedAt: null } memory) return;
        TouchForStory(p);
        memory.RevealedAt = now;
    }

    // Once per kind and key, ever: a mark already held is no failure, just nothing new.
    private async Task GiveMarkAsync(Plynling p, PlynlingEventInstance inst, GiveMark mark, DateTimeOffset now)
    {
        if ((await HeldMarksAsync(p.Id)).Any(m => m.Kind == mark.Kind && m.Key == mark.Key)) return;
        TouchForStory(p);
        _db_context.PlynlingMarks.Add(new PlynlingMark { PlynlingId = p.Id, Kind = mark.Kind, Key = mark.Key, AcquiredAt = now, InstanceId = inst.Id });
    }

    // A personality trait traded for another of its group (the validator keeps both personality, same
    // group, adulte and ancien arcs only). Never the mascot: her traits are fixed and restored on every
    // Ready. Skipped when it no longer holds From or already holds To — a re-run, or a trait that moved.
    private async Task SwapTraitAsync(Plynling p, PlynlingEventInstance inst, SwapTrait swap, DateTimeOffset now)
    {
        if (PlynlingMascot.Is(p) || PlynlingTraits.ByKey(swap.To) is null) return;
        await _db_context.PlynlingTraits.Where(t => t.PlynlingId == p.Id).LoadAsync();
        var held = _db_context.PlynlingTraits.Local.Where(t => t.PlynlingId == p.Id).ToList();
        if (held.FirstOrDefault(t => t.Key == swap.From) is not { } from || held.Any(t => t.Key == swap.To)) return;
        _db_context.PlynlingTraits.Remove(from);
        _db_context.PlynlingTraits.Add(new PlynlingTrait { PlynlingId = p.Id, Key = swap.To, Kind = TraitKind.Personality, AcquiredAt = now });
        await AddMomentAsync(p, JournalKind.TraitGained, swap.To, now);
        inst.GainedTraitKey = swap.To;
        RefreshStressCache(p, held.Where(t => t != from).Select(t => t.Key).Append(swap.To));
    }

    // The titles this very instance gave: a mark is stored with the instance that earned it, and a mark
    // already held is never given again, so these are exactly the newly earned ones. Plain read.
    public async Task<List<string>> GetTitlesEarnedAsync(int instanceId) =>
        await _db_context.PlynlingMarks.AsNoTracking()
            .Where(m => m.InstanceId == instanceId && m.Kind == MarkKind.Epithet)
            .Select(m => m.Key).ToListAsync();

    // What its stories left on a Plynling, for « Personnalité »: plain reads, nothing tracked.
    public async Task<StoryRecord> GetStoryRecordAsync(Plynling p)
    {
        var marks = await _db_context.PlynlingMarks.AsNoTracking().Where(m => m.PlynlingId == p.Id).ToListAsync();
        var memories = await _db_context.PlynlingMemories.AsNoTracking().Where(m => m.PlynlingId == p.Id).ToListAsync();
        return PlynlingPersonality.StoryRecordOf(p, marks, memories);
    }

    // Its most recent title, for the card's name line (null when it has none).
    public async Task<string?> GetLatestTitleAsync(Plynling p)
    {
        var marks = await _db_context.PlynlingMarks.AsNoTracking().Where(m => m.PlynlingId == p.Id && m.Kind == MarkKind.Epithet).ToListAsync();
        return PlynlingPersonality.StoryRecordOf(p, marks, []).Titles.LastOrDefault();
    }
}
