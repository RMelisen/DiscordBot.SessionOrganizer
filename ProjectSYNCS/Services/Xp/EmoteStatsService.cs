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

}
