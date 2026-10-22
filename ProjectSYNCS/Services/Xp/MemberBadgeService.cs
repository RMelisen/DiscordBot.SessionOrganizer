using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads what the member badges are evaluated from, and writes the stored rows. Callers decide
/// whether a key may be written (BadgeAwarder checks MemberBadges.IsAwardable).
/// </summary>
public class MemberBadgeService
{
    private readonly AppDbContext _db;

    public MemberBadgeService(AppDbContext db) => _db = db;

    public async Task<MemberBadgeFacts> GetFactsAsync(ulong guildId, ulong userId, DateTimeOffset? joinedAt, DateTimeOffset now)
    {
        var xp = await _db.MemberXps.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Select(x => new { x.VoiceMinutes, x.ReactionsUsed })
            .FirstOrDefaultAsync();
        var companion = await _db.VoiceCompanionStats.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Select(x => x.Minutes)
            .FirstOrDefaultAsync();
        var quiz = await _db.QuizStats.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Select(x => new { x.Wins, x.BestMs })
            .FirstOrDefaultAsync();
        var goodBot = await _db.BotFeedbacks.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.UserId == userId)
            .Select(x => x.GoodCount)
            .FirstOrDefaultAsync();
        var giveawaysWon = await _db.GiveawayEntries.AsNoTracking()
            .CountAsync(e => e.UserId == userId && e.IsWinner && e.Giveaway.GuildId == guildId);
        var plynlingBadges = await (
            from b in _db.PlynlingBadges.AsNoTracking()
            join p in _db.Plynlings.AsNoTracking() on b.PlynlingId equals p.Id
            where p.OwnerId == userId && p.GuildId == guildId
            select b.Id).CountAsync();

        // Dates are compared in memory: SQLite cannot translate DateTimeOffset comparisons.
        var organised = await _db.SessionEvents.AsNoTracking()
            .Where(s => s.GuildId == guildId && s.OrganizerId == userId && !s.IsCancelled)
            .Select(s => s.ScheduledAt)
            .ToListAsync();
        var seen = await (
            from p in _db.Participants.AsNoTracking()
            where p.UserId == userId && p.FirstSeenInVoiceAt != null
                && !p.SessionEvent.IsCancelled && p.SessionEvent.GuildId == guildId
            select new { Seen = p.FirstSeenInVoiceAt!.Value, p.SessionEvent.ScheduledAt }).ToListAsync();

        var stored = await _db.MemberBadges.AsNoTracking()
            .Where(b => b.GuildId == guildId && b.UserId == userId)
            .ToListAsync();

        return new MemberBadgeFacts(
            VoiceMinutes: xp?.VoiceMinutes ?? 0,
            CompanionMinutes: companion,
            QuizWins: quiz?.Wins ?? 0,
            QuizBestMs: quiz?.BestMs ?? 0,
            Reactions: xp?.ReactionsUsed ?? 0,
            SessionsAttended: seen.Count,
            SessionsOrganised: organised.Count(at => at < now),
            GiveawaysWon: giveawaysWon,
            GoodBot: goodBot,
            PlynlingBadges: plynlingBadges,
            Punctual: seen.Any(s => s.Seen < s.ScheduledAt),
            JoinedAt: joinedAt,
            Stored: stored.ToDictionary(b => b.Key, b => new StoredBadge(b.Count, b.EarnedAt)));
    }

    /// <summary>Inserts the badge with Count = 1; false if the member already holds it.</summary>
    public async Task<bool> AwardAsync(ulong guildId, ulong userId, string key, DateTimeOffset now)
    {
        if (await _db.MemberBadges.AnyAsync(b => b.GuildId == guildId && b.UserId == userId && b.Key == key))
            return false;
        _db.MemberBadges.Add(new MemberBadge { GuildId = guildId, UserId = userId, Key = key, Count = 1, EarnedAt = now });
        try
        {
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // Lost the race on the unique index: someone else wrote it first.
            _db.ChangeTracker.Clear();
            return false;
        }
    }

    /// <summary>Adds one to the badge's counter, creating the row at 1 if absent.</summary>
    public async Task IncrementAsync(ulong guildId, ulong userId, string key, DateTimeOffset now)
    {
        if (await TryIncrementAsync(guildId, userId, key) > 0) return;
        _db.MemberBadges.Add(new MemberBadge { GuildId = guildId, UserId = userId, Key = key, Count = 1, EarnedAt = now });
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The row appeared between the update and the insert: count on top of it.
            _db.ChangeTracker.Clear();
            await TryIncrementAsync(guildId, userId, key);
        }
    }

    private Task<int> TryIncrementAsync(ulong guildId, ulong userId, string key) =>
        _db.MemberBadges
            .Where(b => b.GuildId == guildId && b.UserId == userId && b.Key == key)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Count, b => b.Count + 1));
}
