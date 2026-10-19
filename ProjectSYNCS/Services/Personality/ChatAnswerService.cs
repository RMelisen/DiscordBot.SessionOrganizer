using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>One kind of answer on one path, counted over a window (`/debug answers`).</summary>
public readonly record struct ChatAnswerCount(string Path, string Kind, long Count);

/// <summary>
/// The counts behind `/debug answers`: which kinds of chat answer she gives, on which path. The
/// totals + daily buckets pair (root CLAUDE.md), both written in <see cref="AddAsync"/>; a window
/// sums the buckets, filtered in SQL on the int Day, and all time reads the totals.
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class ChatAnswerService
{
    private readonly AppDbContext _db;

    public ChatAnswerService(AppDbContext db) => _db = db;

    /// <summary>
    /// Counts one answer of <paramref name="kind"/> (an AnswerKinds key) on this path. When the
    /// message got no specific answer, <paramref name="unanswered"/> is what was said: it is kept
    /// (no author) in the same save, and anything past <see cref="UnansweredLog.KeptDays"/> is pruned.
    /// </summary>
    public async Task AddAsync(ulong guildId, ChatPath path, string kind, string? unanswered = null)
    {
        if (unanswered is not null && UnansweredLog.Clean(unanswered) is { Length: > 0 } text)
        {
            var now = DateTimeOffset.UtcNow;
            _db.UnansweredExamples.Add(new UnansweredExample
            {
                GuildId = guildId, Day = AppTime.DayKey(now), At = now, Path = AnswerKinds.Key(path), Kind = kind, Text = text,
            });
            // Rolling window: today plus the 29 days before it. Day is an int, so this runs in SQL.
            var oldest = AppTime.KeyDaysAgo(UnansweredLog.KeptDays - 1);
            await _db.UnansweredExamples.Where(x => x.Day < oldest).ExecuteDeleteAsync();
        }

        var pathKey = AnswerKinds.Key(path);

        var total = await _db.ChatAnswerStats
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.Path == pathKey && x.Kind == kind);
        if (total is null)
        {
            total = new ChatAnswerStat { GuildId = guildId, Path = pathKey, Kind = kind };
            _db.ChatAnswerStats.Add(total);
        }

        var day = AppTime.TodayKey;
        var bucket = await _db.ChatAnswerDailyStats
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.Day == day && x.Path == pathKey && x.Kind == kind);
        if (bucket is null)
        {
            bucket = new ChatAnswerDailyStat { GuildId = guildId, Day = day, Path = pathKey, Kind = kind };
            _db.ChatAnswerDailyStats.Add(bucket);
        }

        total.Count++;
        bucket.Count++;
        await _db.SaveChangesAsync();
    }

    /// <summary>Every (path, kind) counted in this guild over <paramref name="period"/>, nonzero only.</summary>
    public async Task<List<ChatAnswerCount>> GetAsync(ulong guildId, StatsPeriod period)
    {
        if (period == StatsPeriod.AllTime)
            return (await _db.ChatAnswerStats.AsNoTracking().Where(x => x.GuildId == guildId).ToListAsync())
                .Where(x => x.Count > 0)
                .Select(x => new ChatAnswerCount(x.Path, x.Kind, x.Count))
                .ToList();

        // Inclusive lower bound: 6 days ago plus today is a week.
        var since = AppTime.KeyDaysAgo(period == StatsPeriod.Week ? 6 : 29);
        return (await _db.ChatAnswerDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.Day >= since).ToListAsync())
            .GroupBy(x => (x.Path, x.Kind))
            .Select(g => new ChatAnswerCount(g.Key.Path, g.Key.Kind, g.Sum(x => x.Count)))
            .Where(x => x.Count > 0)
            .ToList();
    }

    /// <summary>
    /// The messages she found nothing specific to answer over the last <paramref name="days"/> days
    /// (capped at <see cref="UnansweredLog.KeptDays"/>, all that is kept), newest first.
    /// </summary>
    public async Task<List<UnansweredMessage>> GetUnansweredAsync(ulong guildId, int days)
    {
        var since = AppTime.KeyDaysAgo(Math.Min(days, UnansweredLog.KeptDays) - 1);
        // Newest first by Id: rows are only ever appended, and DateTimeOffset can't be ordered in SQLite.
        return (await _db.UnansweredExamples.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.Day >= since)
                .OrderByDescending(x => x.Id)
                .ToListAsync())
            .Select(x => new UnansweredMessage(x.At, x.Path, x.Kind, x.Text))
            .ToList();
    }
}
