using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>Which metric the leaderboard is ranking by.</summary>
/// <remarks>
/// Three views over one row rather than three leaderboards: the numbers all live on
/// MemberXp, so switching view is a re-order, not a different query shape.
/// </remarks>
public enum LeaderboardView
{
    /// <summary>Total XP earned — the original ranking.</summary>
    Xp,
    /// <summary>Reactions added.</summary>
    Reactions,
    /// <summary>Eligible minutes spent in voice.</summary>
    Voice,
}

/// <summary>One person's standing, carrying every metric the views rank by.</summary>
public readonly record struct MemberTally(ulong UserId, long TotalXp, long ReactionsUsed, long VoiceMinutes)
{
    public int Level => LevelCurve.LevelForXp(TotalXp);

    /// <summary>The number this row is ranked and rendered by, for a given view.</summary>
    public long ValueFor(LeaderboardView view) => view switch
    {
        LeaderboardView.Reactions => ReactionsUsed,
        LeaderboardView.Voice => VoiceMinutes,
        _ => TotalXp,
    };
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
        var row = await GetOrCreateAsync(guildId, userId);
        var oldLevel = LevelCurve.LevelForXp(row.TotalXp);

        row.TotalXp += amount;
        await _db_context.SaveChangesAsync();

        return (oldLevel, LevelCurve.LevelForXp(row.TotalXp));
    }

    /// <summary>
    /// Adds <paramref name="delta"/> to a person's reaction count, creating the row if
    /// they have none yet. Clamped at zero, since a removal can arrive for a reaction
    /// added before the counter existed.
    /// </summary>
    public async Task AddReactionUsedAsync(ulong guildId, ulong userId, int delta)
    {
        // A removal for someone with no row at all — a reaction added before this
        // counter existed, or in a guild they have since gone quiet in. Nothing to take
        // away, and creating an all-zero row to record that would be worse than noise.
        if (delta <= 0 && !await _db_context.MemberXps
                .AnyAsync(x => x.GuildId == guildId && x.UserId == userId))
            return;

        var row = await GetOrCreateAsync(guildId, userId);
        row.ReactionsUsed = Math.Max(0, row.ReactionsUsed + delta);
        await _db_context.SaveChangesAsync();
    }

    /// <summary>Adds eligible voice minutes to a person's total.</summary>
    public async Task AddVoiceMinutesAsync(ulong guildId, ulong userId, long minutes)
    {
        var row = await GetOrCreateAsync(guildId, userId);
        row.VoiceMinutes += minutes;
        await _db_context.SaveChangesAsync();
    }

    /// <summary>
    /// The guild's ranking for one view, highest first. Returns the whole ranking;
    /// callers page it themselves, like EmoteStatsService/BotFeedbackService.
    /// </summary>
    /// <remarks>
    /// Ordering is applied in memory because the metric is chosen at runtime and the
    /// row count is one per member who has ever earned anything — far below the size
    /// where that matters, and the same shape EmoteStatsService already uses. Rows
    /// scoring zero on the chosen metric are dropped: everyone in this table has XP by
    /// definition, but most of them have never touched a voice channel.
    /// </remarks>
    public async Task<List<MemberTally>> GetRankingAsync(ulong guildId, LeaderboardView view)
    {
        var rows = await _db_context.MemberXps
            .Where(x => x.GuildId == guildId)
            .ToListAsync();

        return rows
            .Select(x => new MemberTally(x.UserId, x.TotalXp, x.ReactionsUsed, x.VoiceMinutes))
            .Where(t => t.ValueFor(view) > 0)
            // Ties break on ascending user id, the same rule GetRankAsync counts by, so
            // a rank shown in one place never disagrees with the row it points at.
            .OrderByDescending(t => t.ValueFor(view))
            .ThenBy(t => t.UserId)
            .ToList();
    }

    private async Task<MemberXp> GetOrCreateAsync(ulong guildId, ulong userId)
    {
        var row = await _db_context.MemberXps
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);

        if (row is null)
        {
            row = new MemberXp { GuildId = guildId, UserId = userId };
            _db_context.MemberXps.Add(row);
        }
        return row;
    }

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
