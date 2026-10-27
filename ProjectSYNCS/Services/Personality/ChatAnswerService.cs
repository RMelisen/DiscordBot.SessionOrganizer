using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>One kind of answer on one path, counted over a window (`/debug answers`).</summary>
public readonly record struct ChatAnswerCount(string Path, string Kind, long Count);

/// <summary>
/// The counts behind `/debug answers`: which kinds of chat answer she gives, on which path. The
/// totals + daily buckets pair (root CLAUDE.md), both written by ActivityFlushService from
/// ActivityCounter, every few minutes; a window sums the buckets, filtered in SQL on the int Day, and all time reads the totals.
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class ChatAnswerService
{
    private readonly AppDbContext _db;

    public ChatAnswerService(AppDbContext db) => _db = db;

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
