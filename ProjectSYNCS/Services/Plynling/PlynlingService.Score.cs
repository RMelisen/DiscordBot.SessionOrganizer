using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// One Plynling on /leaderboard's Plynlings view: the row (settled in memory, never saved) and its points for
// the window shown.
public sealed record PlynlingStanding(Plynling Plynling, long Points);

// The hidden score's reads and its one-off rebuild (docs/agents/plynling.md, *Score*). The writes are spread
// over the actions themselves (PlynlingScore.AddAsync, in each action's save).
public partial class PlynlingService
{
    // ---- The ranking ------------------------------------------------------------------------------------------

    /// <summary>
    /// The guild's Plynlings ranked by score, the dead included (the hall of fame), the mascot never. All time
    /// reads the totals; a window sums the daily buckets, filtered by (guild, day) in SQL. Loaded untracked and
    /// settled in memory, so one that starved unseen already shows its tombstone; nothing here writes.
    /// </summary>
    public async Task<List<PlynlingStanding>> GetScoreRankingAsync(ulong guildId, StatsPeriod period, DateTimeOffset now)
    {
        Dictionary<int, long> points;
        if (period == StatsPeriod.AllTime)
        {
            points = await _db_context.Plynlings.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.Score > 0)
                .ToDictionaryAsync(x => x.Id, x => x.Score);
        }
        else
        {
            var since = AppTime.DayKey(now.AddDays(period == StatsPeriod.Week ? -6 : -29));
            var buckets = await _db_context.PlynlingScoreDays.AsNoTracking()
                .Where(b => b.GuildId == guildId && b.Day >= since).ToListAsync();
            points = buckets.GroupBy(b => b.PlynlingId).ToDictionary(g => g.Key, g => g.Sum(b => b.Points));
        }

        var ids = points.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
        var rows = await _db_context.Plynlings.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync();
        foreach (var p in rows) PlynlingLife.Settle(p, now);          // in memory only: AsNoTracking, never saved
        return rows.Where(p => !PlynlingMascot.Is(p))
            .Select(p => new PlynlingStanding(p, points[p.Id]))
            .OrderByDescending(s => s.Points).ThenBy(s => s.Plynling.Id)
            .ToList();
    }

    // ---- The rebuild ------------------------------------------------------------------------------------------

    /// <summary>
    /// The ids of the Plynlings whose score does not count their history yet: every row that existed when the
    /// score shipped (ScoreRebuilt defaults to false there), living or dead.
    /// </summary>
    public async Task<List<int>> GetScoreRebuildIdsAsync() =>
        await _db_context.Plynlings.Where(x => !x.ScoreRebuilt).Select(x => x.Id).ToListAsync();

    /// <summary>
    /// Adds one Plynling's history to its score (PlynlingScoreHistory.Rebuild) and its owner's total, marks the
    /// pair bonds it reached as scored, gives a Plynling already dead its old tombstone as a floor, and marks
    /// it rebuilt — all in one save, so a failure leaves it for the next start and a success never runs twice.
    /// Run before the bot connects (PlynlingScoreRebuild), so no live action can be counted twice. No buckets:
    /// the 7/30-day windows start on release day.
    /// </summary>
    public Task<long?> RebuildScoreAsync(int id, DateTimeOffset now) => UnitAsync(() => RebuildScoreOnceAsync(id, now));

    private async Task<long?> RebuildScoreOnceAsync(int id, DateTimeOffset now)
    {
        var p = await _db_context.Plynlings.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null || p.ScoreRebuilt) return null;

        // Its age and whether it is dead, read from a copy settled in memory: one that starved unseen is dead
        // and aged to its death, without this writing the death (the sweep stamps and announces it).
        var copy = await _db_context.Plynlings.AsNoTracking().FirstAsync(x => x.Id == id);
        PlynlingLife.Settle(copy, now);
        var lived = PlynlingLife.Age(copy, now);
        var dead = copy.DiedAt is not null;

        var history = await HistoryOfAsync(p, lived, copy.DiedAt ?? now);
        var points = PlynlingScoreHistory.Rebuild(history);

        p.ScoreRebuilt = true;
        if (dead) p.TombstoneFloor = PlynlingCatalog.MemorialTier(lived);
        if (points > 0)
        {
            p.Score += points;
            await PlynlingScore.AddToOwnerAsync(_db_context, p.GuildId, p.OwnerId, points);
        }

        // Its current bonds count as scored for the pair, so reaching one again later adds nothing.
        var relations = await _db_context.PlynlingRelations
            .Where(r => r.PlynlingAId == id || r.PlynlingBId == id).ToListAsync();
        foreach (var r in relations.Where(r => r.Bond != PlynlingBond.Acquaintances))
            r.ScoredBonds |= PlynlingScore.BondBit(r.Bond);

        await _db_context.SaveChangesAsync();
        return points;
    }

    // What its stored history holds (see PlynlingHistory for what each count is).
    private async Task<PlynlingHistory> HistoryOfAsync(Plynling p, TimeSpan lived, DateTimeOffset until)
    {
        var badges = await _db_context.PlynlingBadges.Where(b => b.PlynlingId == p.Id).Select(b => b.Key).ToListAsync();
        var journal = await _db_context.PlynlingJournalEntries.Where(e => e.PlynlingId == p.Id)
            .Select(e => new { e.Kind, e.Detail }).ToListAsync();
        var bonds = journal
            .Select(e => (Bond: PlynlingScoreHistory.BondOf(e.Kind), e.Detail))
            .Where(e => e.Bond is not null)
            .Distinct()                                  // one per bond and other Plynling
            .Select(e => e.Bond!.Value).ToList();

        // A run counts once, at its last step: a resolved step its owner chose that queued nothing after it on
        // this Plynling (an answer queued on the other one is that one's run).
        var steps = await _db_context.PlynlingEventInstances
            .Where(i => i.PlynlingId == p.Id && i.ResolvedAt != null)
            .Select(i => new { i.Id, i.EventKey, i.EndingKey, i.DecidedAlone, i.ParentInstanceId })
            .ToListAsync();
        var parents = steps.Where(i => i.ParentInstanceId is not null).Select(i => i.ParentInstanceId!.Value).ToHashSet();
        var pending = await _db_context.PlynlingEventInstances
            .Where(i => i.PlynlingId == p.Id && i.ResolvedAt == null && i.ParentInstanceId != null)
            .Select(i => i.ParentInstanceId!.Value).ToListAsync();
        parents.UnionWith(pending);
        var finished = steps.Where(i => !i.DecidedAlone && !parents.Contains(i.Id)).ToList();
        var longs = finished.Count(i => i.EndingKey is not null
                                        && PlynlingEvents.ByKey(i.EventKey) is { } def && PlynlingEventEngine.IsLong(def));

        // The owner's completions on the days it lived (the daily buckets carry every completion's day).
        var from = AppTime.DayKey(p.AdoptedAt);
        var to = AppTime.DayKey(until);
        var collections = await _db_context.CollectionDailyStats
            .Where(b => b.GuildId == p.GuildId && b.UserId == p.OwnerId && b.Day >= from && b.Day <= to)
            .SumAsync(b => b.Completions);

        return new PlynlingHistory(
            lived, p.Meals, p.Pets, p.Plays, p.Visits, p.Drills, p.RpgXp,
            badges,
            journal.Count(e => e.Kind == JournalKind.TraitGained),
            journal.Count(e => e.Kind == JournalKind.Recovered),
            bonds,
            finished.Count - longs, longs,
            collections);
    }
}
