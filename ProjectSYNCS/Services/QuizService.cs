using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>One person's line on the quiz board.</summary>
public readonly record struct QuizTally(ulong UserId, long Wins, long BestMs);

// EF access for the pop quiz — rounds, wins and the board. Transient, like every service
// wrapping AppDbContext; the behaviour (when to post, who answered) is QuizMasterService's.
//
// SQLite can't compare DateTimeOffsets: every read filters on Closed / GuildId / Day in SQL
// and leaves the instants to the caller, in memory.
public class QuizService
{
    private readonly AppDbContext _db_context;

    public QuizService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    public Task<QuizRound?> GetAsync(int roundId) =>
        _db_context.QuizRounds.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roundId);

    /// <summary>Every round still open, in every guild.</summary>
    public Task<List<QuizRound>> GetOpenAsync() =>
        _db_context.QuizRounds.AsNoTracking().Where(r => !r.Closed).ToListAsync();

    /// <summary>How many rounds this guild has ever had — the step of the question rotation.</summary>
    public Task<int> CountAsync(ulong guildId) =>
        _db_context.QuizRounds.CountAsync(r => r.GuildId == guildId);

    /// <summary>How many rounds were posted today, and when the latest one went up.</summary>
    public async Task<(int Today, DateTimeOffset? LastPostedAt)> GetRecentAsync(ulong guildId, int todayKey)
    {
        var today = await _db_context.QuizRounds.CountAsync(r => r.GuildId == guildId && r.Day == todayKey);
        // Ids grow with time, so the highest is the latest without comparing instants in SQL.
        var last = await _db_context.QuizRounds.AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();
        return (today, last?.PostedAt);
    }

    public async Task<QuizRound> CreateAsync(QuizRound round)
    {
        _db_context.QuizRounds.Add(round);
        await _db_context.SaveChangesAsync();
        return round;
    }

    public Task SetMessageIdAsync(int roundId, ulong messageId) =>
        _db_context.QuizRounds.Where(r => r.Id == roundId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.MessageId, messageId));

    /// <summary>
    /// Closes a round nobody won. False if it was already closed — by a win a moment
    /// earlier, or by a pass that ran first — so only one caller ever announces it.
    /// </summary>
    public async Task<bool> CloseAsync(int roundId) =>
        await _db_context.QuizRounds.Where(r => r.Id == roundId && !r.Closed)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Closed, true)) == 1;

    /// <summary>
    /// Records <paramref name="userId"/> as the winner and pays them, in one transaction.
    /// Null if the round was already won or closed: the conditional update is what makes a
    /// win happen once, whatever QuizMasterService's in-memory claim says. Otherwise the
    /// round as stored after the win.
    /// </summary>
    public async Task<QuizRound?> RecordWinAsync(int roundId, ulong userId, DateTimeOffset now)
    {
        await using var transaction = await _db_context.Database.BeginTransactionAsync();

        var changed = await _db_context.QuizRounds
            .Where(r => r.Id == roundId && r.WinnerId == 0 && !r.Closed)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.WinnerId, userId)
                .SetProperty(r => r.WonAt, now)
                .SetProperty(r => r.Closed, true));
        if (changed == 0) return null;

        var round = await _db_context.QuizRounds.AsNoTracking().FirstAsync(r => r.Id == roundId);
        var elapsedMs = Math.Max(1, (long)(now - round.PostedAt).TotalMilliseconds);

        var wallet = await PebbleService.GetOrCreateWalletAsync(_db_context, round.GuildId, userId);
        wallet.Balance += round.Reward;
        await EconomyLog.AddAsync(_db_context, round.GuildId, EconomyLog.EarnQuiz, round.Reward, now);

        var stat = await _db_context.QuizStats
            .FirstOrDefaultAsync(s => s.GuildId == round.GuildId && s.UserId == userId);
        if (stat is null)
        {
            stat = new QuizStat { GuildId = round.GuildId, UserId = userId };
            _db_context.QuizStats.Add(stat);
        }
        stat.Wins++;
        if (stat.BestMs == 0 || elapsedMs < stat.BestMs) stat.BestMs = elapsedMs;

        var day = AppTime.DayKey(now);
        var bucket = await _db_context.QuizDailyStats
            .FirstOrDefaultAsync(b => b.GuildId == round.GuildId && b.UserId == userId && b.Day == day);
        if (bucket is null)
        {
            bucket = new QuizDailyStat { GuildId = round.GuildId, UserId = userId, Day = day };
            _db_context.QuizDailyStats.Add(bucket);
        }
        bucket.Wins++;

        await _db_context.SaveChangesAsync();
        await transaction.CommitAsync();
        return round;
    }

    /// <summary>
    /// Everyone with a win over <paramref name="period"/>, ranked: most wins first, then
    /// the fastest record (all-time only), then whoever got there first — a stable
    /// tie-break, or two people level on wins would swap places on every re-render.
    /// </summary>
    public async Task<List<QuizTally>> GetBoardAsync(ulong guildId, StatsPeriod period)
    {
        if (period == StatsPeriod.AllTime)
        {
            var all = await _db_context.QuizStats.AsNoTracking()
                .Where(s => s.GuildId == guildId && s.Wins > 0)
                .ToListAsync();
            return all
                .OrderByDescending(s => s.Wins)
                .ThenBy(s => s.BestMs)
                .ThenBy(s => s.Id)
                .Select(s => new QuizTally(s.UserId, s.Wins, s.BestMs))
                .ToList();
        }

        // Inclusive: 6 days ago plus today is a week. Day is an int, so this filters in SQL.
        var since = AppTime.KeyDaysAgo(period == StatsPeriod.Week ? 6 : 29);
        var buckets = await _db_context.QuizDailyStats.AsNoTracking()
            .Where(b => b.GuildId == guildId && b.Day >= since)
            .ToListAsync();
        return buckets
            .GroupBy(b => b.UserId)
            .Select(g => (UserId: g.Key, Wins: g.Sum(b => b.Wins), First: g.Min(b => b.Id)))
            .Where(x => x.Wins > 0)
            .OrderByDescending(x => x.Wins)
            .ThenBy(x => x.First)
            .Select(x => new QuizTally(x.UserId, x.Wins, 0))
            .ToList();
    }
}
