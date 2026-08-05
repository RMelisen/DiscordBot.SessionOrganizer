using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>One person's standing in the good-bot leaderboard over some window.</summary>
public readonly record struct FeedbackTally(ulong UserId, long Good, long Bad)
{
    public long Total => Good + Bad;
}

// Persists the "good bot" / "bad bot" tallies. Deliberately dumb: it records what
// it is told and reads it back. Deciding *whether* a verdict counts — attribution
// to something she actually did, and the one-per-person-per-action rationing —
// belongs to BotFeedbackTracker, which is the piece that knows what she did.
//
// Transient, like EmoteStatsService, because it wraps AppDbContext.
public class BotFeedbackService
{
    private readonly AppDbContext _db_context;

    public BotFeedbackService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    // Records one verdict from one person, in the all-time total and in today's
    // bucket. Both are written here so every bucketed user necessarily has a
    // BotFeedback row. FeedbackKind.None is ignored so callers don't have to branch
    // before calling.
    public async Task AddAsync(ulong guildId, ulong userId, FeedbackKind kind)
    {
        if (kind == FeedbackKind.None) return;

        var row = await _db_context.BotFeedbacks
            .FirstOrDefaultAsync(f => f.GuildId == guildId && f.UserId == userId);

        if (row is null)
        {
            row = new BotFeedback { GuildId = guildId, UserId = userId };
            _db_context.BotFeedbacks.Add(row);
        }

        var bucket = await GetOrCreateDailyAsync(guildId, userId, AppTime.TodayKey);

        if (kind == FeedbackKind.Good)
        {
            row.GoodCount++;
            bucket.GoodCount++;
        }
        else
        {
            row.BadCount++;
            bucket.BadCount++;
        }

        await _db_context.SaveChangesAsync();
    }

    // Whether anyone in this guild has ever passed verdict. Asked before the
    // leaderboard is opened, to tell "nobody ever has" apart from "nobody did this
    // week" — the two deserve different answers.
    public Task<bool> HasAnyAsync(ulong guildId) =>
        _db_context.BotFeedbacks.AnyAsync(f => f.GuildId == guildId);

    /// <summary>
    /// The guild's judges ranked over <paramref name="period"/>, most praise first.
    /// Returns the whole ranking; callers page it themselves.
    /// </summary>
    /// <remarks>
    /// Ordered by good verdicts rather than by a net score: one "bad bot" should not
    /// erase a good one, and the two numbers are shown side by side anyway.
    /// </remarks>
    public async Task<List<FeedbackTally>> GetRankingAsync(ulong guildId, StatsPeriod period)
    {
        if (period == StatsPeriod.AllTime)
        {
            var all = await _db_context.BotFeedbacks
                .Where(f => f.GuildId == guildId)
                .ToListAsync();

            return Rank(all.Select(f => new FeedbackTally(f.UserId, f.GoodCount, f.BadCount)));
        }

        // Inclusive lower bound: 6 days ago plus today is a week. Day is an int, so
        // unlike every DateTimeOffset window in this project this really is filtered
        // by the database rather than in memory.
        var since = AppTime.KeyDaysAgo(period == StatsPeriod.Week ? 6 : 29);

        var buckets = await _db_context.BotFeedbackDailyStats
            .Where(b => b.GuildId == guildId && b.Day >= since)
            .ToListAsync();

        return Rank(buckets
            .GroupBy(b => b.UserId)
            .Select(g => new FeedbackTally(g.Key, g.Sum(b => b.GoodCount), g.Sum(b => b.BadCount))));
    }

    private static List<FeedbackTally> Rank(IEnumerable<FeedbackTally> tallies) =>
        tallies
            .Where(t => t.Total > 0)
            .OrderByDescending(t => t.Good)
            .ThenBy(t => t.Bad)
            .ToList();

    private async Task<BotFeedbackDailyStat> GetOrCreateDailyAsync(ulong guildId, ulong userId, int day)
    {
        var row = await _db_context.BotFeedbackDailyStats.FirstOrDefaultAsync(b =>
            b.GuildId == guildId && b.UserId == userId && b.Day == day);

        if (row is null)
        {
            row = new BotFeedbackDailyStat { GuildId = guildId, UserId = userId, Day = day };
            _db_context.BotFeedbackDailyStats.Add(row);
        }
        return row;
    }
}
