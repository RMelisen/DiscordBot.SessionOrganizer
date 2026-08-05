using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectSYNCS.Services;

// A single emote occurrence. Custom: Id != 0, Unicode == "". Unicode emoji:
// Id == 0, Unicode holds the emoji (and Name mirrors it for display).
public readonly record struct EmoteRef(ulong Id, string Name, bool IsAnimated, string Unicode = "")
{
    // Custom emote from a server (has a numeric id).
    public static EmoteRef Custom(ulong id, string name, bool animated) => new(id, name, animated);
    // Standard unicode emoji (no id).
    public static EmoteRef FromUnicode(string emoji) => new(0, emoji, false, emoji);
}

/// <summary>One emote's standing in a ranking, already resolved for display.</summary>
public readonly record struct EmoteTally(string Markup, long Written, long Reacted)
{
    public long Total => Written + Reacted;
}

public class EmoteStatsService
{
    private readonly AppDbContext _db_context;

    public EmoteStatsService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    // Adds written occurrences for a batch of emotes (a message may contain the
    // same emote several times). counts maps each emote to how many times it
    // appeared in the message.
    public async Task AddWrittenAsync(ulong guildId, IReadOnlyDictionary<EmoteRef, int> counts)
    {
        if (counts.Count == 0) return;

        var today = AppTime.TodayKey;
        foreach (var (emote, n) in counts)
        {
            var row = await GetOrCreateAsync(guildId, emote);
            row.WrittenCount += n;

            var bucket = await GetOrCreateDailyAsync(guildId, emote, today);
            bucket.WrittenCount += n;
        }
        await _db_context.SaveChangesAsync();
    }

    // Adjusts the reacted count for one emote by delta (+1 on add, -1 on remove).
    // The stored count never drops below zero.
    //
    // A removal always lands on *today's* bucket, even when the reaction being
    // removed was added weeks ago. Tracking the original day would mean storing a
    // row per reaction; for a leaderboard the drift is not worth that, and the
    // clamp keeps it from going negative.
    public async Task AddReactedAsync(ulong guildId, EmoteRef emote, int delta)
    {
        var row = await GetOrCreateAsync(guildId, emote);
        row.ReactedCount = Math.Max(0, row.ReactedCount + delta);

        var bucket = await GetOrCreateDailyAsync(guildId, emote, AppTime.TodayKey);
        bucket.ReactedCount = Math.Max(0, bucket.ReactedCount + delta);

        await _db_context.SaveChangesAsync();
    }

    /// <summary>
    /// The guild's emotes ranked by total usage over <paramref name="period"/>,
    /// highest first. Returns the whole ranking; callers page it themselves.
    /// </summary>
    public async Task<List<EmoteTally>> GetRankingAsync(ulong guildId, StatsPeriod period)
    {
        if (period == StatsPeriod.AllTime)
        {
            var all = await _db_context.EmoteStats
                .Where(s => s.GuildId == guildId)
                .ToListAsync();

            return all
                .Select(s => new EmoteTally(s.Markup, s.WrittenCount, s.ReactedCount))
                .Where(t => t.Total > 0)
                .OrderByDescending(t => t.Total)
                .ToList();
        }

        // Inclusive lower bound: 6 days ago plus today is a week. Day is an int, so
        // unlike every DateTimeOffset window in this project this really is filtered
        // by the database rather than in memory.
        var since = AppTime.KeyDaysAgo(period == StatsPeriod.Week ? 6 : 29);

        var buckets = await _db_context.EmoteDailyStats
            .Where(b => b.GuildId == guildId && b.Day >= since)
            .ToListAsync();

        if (buckets.Count == 0) return new List<EmoteTally>();

        // Markup lives on EmoteStat so a rename is recorded in one place. Every
        // bucketed emote has a row there — they are written in the same call.
        var names = await _db_context.EmoteStats
            .Where(s => s.GuildId == guildId)
            .ToDictionaryAsync(s => (s.EmoteId, s.Unicode), s => s.Markup);

        return buckets
            .GroupBy(b => (b.EmoteId, b.Unicode))
            .Select(g => new EmoteTally(
                names.TryGetValue(g.Key, out var markup) ? markup : g.Key.Unicode,
                g.Sum(b => b.WrittenCount),
                g.Sum(b => b.ReactedCount)))
            .Where(t => t.Total > 0 && t.Markup.Length > 0)
            .OrderByDescending(t => t.Total)
            .ToList();
    }

    private async Task<EmoteStat> GetOrCreateAsync(ulong guildId, EmoteRef emote)
    {
        var row = await _db_context.EmoteStats.FirstOrDefaultAsync(s =>
            s.GuildId == guildId && s.EmoteId == emote.Id && s.Unicode == emote.Unicode);

        if (row is null)
        {
            row = new EmoteStat
            {
                GuildId = guildId,
                EmoteId = emote.Id,
                Unicode = emote.Unicode,
                Name = emote.Name,
                IsAnimated = emote.IsAnimated
            };
            _db_context.EmoteStats.Add(row);
        }
        else
        {
            // Keep the latest name/animated flag in case the emote was renamed.
            row.Name = emote.Name;
            row.IsAnimated = emote.IsAnimated;
        }
        return row;
    }

    private async Task<EmoteDailyStat> GetOrCreateDailyAsync(ulong guildId, EmoteRef emote, int day)
    {
        var row = await _db_context.EmoteDailyStats.FirstOrDefaultAsync(b =>
            b.GuildId == guildId && b.EmoteId == emote.Id && b.Unicode == emote.Unicode && b.Day == day);

        if (row is null)
        {
            row = new EmoteDailyStat
            {
                GuildId = guildId,
                EmoteId = emote.Id,
                Unicode = emote.Unicode,
                Day = day
            };
            _db_context.EmoteDailyStats.Add(row);
        }
        return row;
    }
}
