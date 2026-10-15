using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads one person's /recap. Nothing new is stored: every number comes from a table another
/// subsystem already writes. All-time reads the totals; a window sums the daily buckets, filtered
/// in SQL on the int <c>Day</c> — and the two are never substituted for each other (root
/// CLAUDE.md, *Totals + daily buckets*). Sessions and /tonight answers have no buckets: their
/// DateTimeOffset windows are applied in memory, which SQLite needs anyway.
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class RecapService
{
    private readonly AppDbContext _db;
    private readonly XpService _xp;

    public RecapService(AppDbContext db, XpService xp)
    {
        _db = db;
        _xp = xp;
    }

    public async Task<RecapStats> GetAsync(ulong guildId, ulong userId, StatsPeriod period)
    {
        var allTime = period == StatsPeriod.AllTime;
        var since = AppTime.KeyDaysAgo(period == StatsPeriod.Week ? 6 : 29);
        var now = DateTimeOffset.UtcNow;
        var sinceInstant = now.AddDays(period == StatsPeriod.Week ? -7 : -30);

        long xp, reactions, voice, withHer, good, bad, quizWins, quizBest = 0;
        long mean, shout, perfidy, votes, abandons;
        int? level = null, rank = null;

        if (allTime)
        {
            var row = await _db.MemberXps.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);
            xp = row?.TotalXp ?? 0;
            reactions = row?.ReactionsUsed ?? 0;
            voice = row?.VoiceMinutes ?? 0;
            if (await _xp.GetRankAsync(guildId, userId) is { } r)
            {
                level = r.Level;
                rank = r.Rank;
            }

            withHer = (await _db.VoiceCompanionStats.AsNoTracking()
                .FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId))?.Minutes ?? 0;

            var feedback = await _db.BotFeedbacks.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);
            good = feedback?.GoodCount ?? 0;
            bad = feedback?.BadCount ?? 0;

            var quiz = await _db.QuizStats.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);
            quizWins = quiz?.Wins ?? 0;
            quizBest = quiz?.BestMs ?? 0;

            var shame = await _db.ShameRecords.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);
            mean = shame?.MeanHits ?? 0;
            shout = shame?.ShoutHits ?? 0;
            perfidy = shame?.PerfidyHits ?? 0;
            votes = shame?.BanVotes ?? 0;
            abandons = shame?.AbandonHits ?? 0;
        }
        else
        {
            var days = await _db.MemberDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.UserId == userId && x.Day >= since).ToListAsync();
            xp = days.Sum(x => x.XpEarned);
            reactions = days.Sum(x => x.ReactionsUsed);
            voice = days.Sum(x => x.VoiceMinutes);

            withHer = (await _db.VoiceCompanionDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.UserId == userId && x.Day >= since).ToListAsync())
                .Sum(x => x.Minutes);

            var feedback = await _db.BotFeedbackDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.UserId == userId && x.Day >= since).ToListAsync();
            good = feedback.Sum(x => x.GoodCount);
            bad = feedback.Sum(x => x.BadCount);

            quizWins = (await _db.QuizDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.UserId == userId && x.Day >= since).ToListAsync())
                .Sum(x => x.Wins);

            var shame = await _db.ShameDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.UserId == userId && x.Day >= since).ToListAsync();
            mean = shame.Sum(x => x.MeanHits);
            shout = shame.Sum(x => x.ShoutHits);
            perfidy = shame.Sum(x => x.PerfidyHits);
            votes = shame.Sum(x => x.BanVotes);
            abandons = shame.Sum(x => x.AbandonHits);
        }

        // Sessions they said they'd come to, already started, not cancelled. Attended means she
        // saw them in voice; missed only counts sessions whose roll she actually called
        // (RecapSent), so sessions from before attendance existed don't read as no-shows.
        var joined = (await _db.Participants.AsNoTracking()
                .Include(p => p.SessionEvent)
                .Where(p => p.UserId == userId && p.Status == ParticipantStatus.Joined
                            && p.SessionEvent.GuildId == guildId && !p.SessionEvent.IsCancelled)
                .ToListAsync())
            .Where(p => p.SessionEvent.ScheduledAt <= now && (allTime || p.SessionEvent.ScheduledAt >= sinceInstant))
            .ToList();
        var attended = joined.Count(p => p.FirstSeenInVoiceAt is not null);
        var missed = joined.Count(p => p.FirstSeenInVoiceAt is null && p.SessionEvent.RecapSent);

        var tonightIn = (await _db.TonightResponses.AsNoTracking()
                .Include(r => r.TonightCall)
                .Where(r => r.UserId == userId && r.Status == TonightStatus.In && r.TonightCall.GuildId == guildId)
                .ToListAsync())
            .Count(r => allTime || r.AnsweredAt >= sinceInstant);

        var plynlings = await _db.Plynlings.AsNoTracking()
            .Where(p => p.GuildId == guildId && p.OwnerId == userId).ToListAsync();
        var living = plynlings.FirstOrDefault(p => p.DiedAt is null);
        var graves = plynlings.Count(p => p.DiedAt is not null);

        var balance = (await _db.PebbleWallets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId))?.Balance ?? 0;

        return new RecapStats(
            period, xp, level, rank, reactions, voice, withHer, good, bad, quizWins, quizBest,
            mean, shout, perfidy, votes, abandons,
            joined.Count, attended, missed, tonightIn,
            living?.Name, living is null ? null : PlynlingLife.Age(living, now), graves,
            balance);
    }
}
