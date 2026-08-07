using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>One person's standing on the XP leaderboard.</summary>
public readonly record struct XpTally(ulong UserId, long TotalXp)
{
    public int Level => LevelCurve.LevelForXp(TotalXp);
}

/// <summary>Where one person sits in the ranking, and their raw numbers.</summary>
public readonly record struct XpRank(int Rank, long TotalXp, int Level);

// Persists XP. Deliberately dumb, like BotFeedbackService/EmoteStatsService: it
// records what it's told and reads it back. Deciding *whether* XP should be granted —
// cooldowns, which signals count, the bot-interaction bonus — belongs to XpTracker,
// which is the piece that watches the gateway.
//
// Transient, like every other service wrapping AppDbContext.
public class XpService
{
    private readonly AppDbContext _db_context;

    public XpService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    /// <summary>
    /// Adds XP to the (guild, user) row, creating it if needed. Returns the level
    /// before and after the grant, so the caller can detect a crossed threshold
    /// without a second read.
    /// </summary>
    public async Task<(int OldLevel, int NewLevel)> AddXpAsync(ulong guildId, ulong userId, long amount)
    {
        var row = await _db_context.MemberXps
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);

        var oldLevel = LevelCurve.LevelForXp(row?.TotalXp ?? 0);

        if (row is null)
        {
            row = new MemberXp { GuildId = guildId, UserId = userId };
            _db_context.MemberXps.Add(row);
        }

        row.TotalXp += amount;
        await _db_context.SaveChangesAsync();

        return (oldLevel, LevelCurve.LevelForXp(row.TotalXp));
    }

    /// <summary>
    /// The guild's ranking, highest total first. Returns the whole ranking; callers
    /// page it themselves, like EmoteStatsService/BotFeedbackService.
    /// </summary>
    public Task<List<XpTally>> GetRankingAsync(ulong guildId) =>
        _db_context.MemberXps
            .Where(x => x.GuildId == guildId)
            .OrderByDescending(x => x.TotalXp)
            .Select(x => new XpTally(x.UserId, x.TotalXp))
            .ToListAsync();

    /// <summary>
    /// One person's rank, matching the position <see cref="GetRankingAsync"/> would
    /// show them at. Ties are broken by ascending user id in both places, so the
    /// number in a leaderboard footer never disagrees with the row it points at —
    /// a plain "how many people have more XP, plus one" would silently drift apart
    /// from the ranking's own ordering the moment two totals tie exactly.
    /// </summary>
    public async Task<XpRank?> GetRankAsync(ulong guildId, ulong userId)
    {
        var row = await _db_context.MemberXps
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);
        if (row is null) return null;

        var ahead = await _db_context.MemberXps.CountAsync(x =>
            x.GuildId == guildId
            && (x.TotalXp > row.TotalXp || (x.TotalXp == row.TotalXp && x.UserId < row.UserId)));

        return new XpRank(ahead + 1, row.TotalXp, LevelCurve.LevelForXp(row.TotalXp));
    }
}
