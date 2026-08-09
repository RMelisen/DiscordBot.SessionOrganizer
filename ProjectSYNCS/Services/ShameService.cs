using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>One person's standing in one of the wall's two rankings.</summary>
public readonly record struct ShameTally(ulong UserId, long Count);

/// <summary>What came of someone spending (or trying to spend) their daily vote.</summary>
public enum ShameVoteResult
{
    /// <summary>Counted. The vote is on the record and the announcement should go out.</summary>
    Recorded,
    /// <summary>They already voted today. Nothing was written.</summary>
    AlreadyVotedToday,
}

/// <summary>Both halves of the wall over one window, already ranked and trimmed.</summary>
public readonly record struct ShameWall(
    IReadOnlyList<ShameTally> Malfaisants,
    IReadOnlyList<ShameTally> Bannis);

// Persists the wall of shame. Deliberately dumb in the same way BotFeedbackService is:
// it records what it is told and reads it back. Deciding *whether* a message was mean
// and how many people it was mean to belongs to ShameTracker, which is the piece that
// can see the message.
//
// Transient, because it wraps AppDbContext.
public class ShameService
{
    /// <summary>How many names each title shows on the wall.</summary>
    public const int TopCount = 3;

    private readonly AppDbContext _db_context;

    public ShameService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    /// <summary>
    /// Records that <paramref name="userId"/> was hostile to <paramref name="hits"/>
    /// people in one message. Written to the all-time total and today's bucket in one
    /// call, so every bucketed person necessarily has a totals row.
    /// </summary>
    public async Task AddMeanHitsAsync(ulong guildId, ulong userId, long hits)
    {
        if (hits <= 0) return;

        var row = await GetOrCreateRecordAsync(guildId, userId);
        var bucket = await GetOrCreateDailyAsync(guildId, userId, AppTime.TodayKey);

        row.MeanHits += hits;
        bucket.MeanHits += hits;

        await _db_context.SaveChangesAsync();
    }

    /// <summary>
    /// Spends <paramref name="voterId"/>'s vote for the day on
    /// <paramref name="targetId"/>, if they still have it.
    /// </summary>
    /// <remarks>
    /// The limit check and both writes go out in a single <c>SaveChangesAsync</c>, so a
    /// crash between "marked as voted" and "counted the vote" is not a state this can
    /// end up in. Refusing costs one read and writes nothing.
    /// <para>Self-voting is allowed and deliberately not special-cased here: the voter
    /// and the target resolve to the same row, which takes the day flag and the vote
    /// together. Refusing to shame the bot (or any bot) is the module's job, since it
    /// is the piece holding the <c>IUser</c>.</para>
    /// </remarks>
    public async Task<ShameVoteResult> TryVoteAsync(ulong guildId, ulong voterId, ulong targetId)
    {
        var today = AppTime.TodayKey;

        var voter = await GetOrCreateRecordAsync(guildId, voterId);
        if (voter.LastVoteDay == today) return ShameVoteResult.AlreadyVotedToday;

        // Resolved through the same tracked context, so a self-vote gets the *same*
        // instance rather than a second one that would overwrite the first's changes.
        var target = voterId == targetId
            ? voter
            : await GetOrCreateRecordAsync(guildId, targetId);

        var bucket = await GetOrCreateDailyAsync(guildId, targetId, today);

        voter.LastVoteDay = today;
        target.BanVotes++;
        bucket.BanVotes++;

        await _db_context.SaveChangesAsync();
        return ShameVoteResult.Recorded;
    }

    /// <summary>
    /// The top few of each title over <paramref name="period"/>. Both rankings come
    /// from one read of whichever table the window needs, since they live in the same
    /// row and the wall always shows both.
    /// </summary>
    public async Task<ShameWall> GetWallAsync(ulong guildId, StatsPeriod period)
    {
        if (period == StatsPeriod.AllTime)
        {
            var all = await _db_context.ShameRecords
                .Where(r => r.GuildId == guildId)
                .ToListAsync();

            return new ShameWall(
                Rank(all.Select(r => (r.Id, r.UserId, r.MeanHits))),
                Rank(all.Select(r => (r.Id, r.UserId, r.BanVotes))));
        }

        // Inclusive lower bound: 6 days ago plus today is a week. Day is an int, so
        // unlike every DateTimeOffset window in this project this really is filtered by
        // the database rather than in memory.
        var since = AppTime.KeyDaysAgo(period == StatsPeriod.Week ? 6 : 29);

        var buckets = await _db_context.ShameDailyStats
            .Where(b => b.GuildId == guildId && b.Day >= since)
            .ToListAsync();

        var byUser = buckets.GroupBy(b => b.UserId).ToList();

        return new ShameWall(
            Rank(byUser.Select(g => (g.Min(b => b.Id), g.Key, g.Sum(b => b.MeanHits)))),
            Rank(byUser.Select(g => (g.Min(b => b.Id), g.Key, g.Sum(b => b.BanVotes)))));
    }

    // Ranked highest first, ties broken by the row that appeared first — the earliest
    // Id in the all-time table, or the earliest bucket inside the window. Any stable
    // tie-break would do; what matters is that it *is* stable, or two people level on
    // count would swap places on every re-render and the wall would look broken.
    private static List<ShameTally> Rank(IEnumerable<(int Id, ulong UserId, long Count)> rows) =>
        rows
            .Where(r => r.Count > 0)
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Id)
            .Take(TopCount)
            .Select(r => new ShameTally(r.UserId, r.Count))
            .ToList();

    private async Task<ShameRecord> GetOrCreateRecordAsync(ulong guildId, ulong userId)
    {
        var row = await _db_context.ShameRecords
            .FirstOrDefaultAsync(r => r.GuildId == guildId && r.UserId == userId);

        if (row is null)
        {
            row = new ShameRecord { GuildId = guildId, UserId = userId };
            _db_context.ShameRecords.Add(row);
        }
        return row;
    }

    private async Task<ShameDailyStat> GetOrCreateDailyAsync(ulong guildId, ulong userId, int day)
    {
        var row = await _db_context.ShameDailyStats.FirstOrDefaultAsync(b =>
            b.GuildId == guildId && b.UserId == userId && b.Day == day);

        if (row is null)
        {
            row = new ShameDailyStat { GuildId = guildId, UserId = userId, Day = day };
            _db_context.ShameDailyStats.Add(row);
        }
        return row;
    }
}
