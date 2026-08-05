using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

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

    // Records one verdict from one person. FeedbackKind.None is ignored so callers
    // don't have to branch before calling.
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

        if (kind == FeedbackKind.Good) row.GoodCount++;
        else row.BadCount++;

        await _db_context.SaveChangesAsync();
    }

    // How many people have passed verdict at least once in this guild.
    public Task<int> GetCountAsync(ulong guildId) =>
        _db_context.BotFeedbacks.CountAsync(f => f.GuildId == guildId);

    // One page of the leaderboard, ranked by praise. Ordered by GoodCount rather
    // than by a net score: one "bad bot" should not erase a good one, and the two
    // numbers are shown side by side anyway.
    public Task<List<BotFeedback>> GetPageAsync(ulong guildId, int skip, int take) =>
        _db_context.BotFeedbacks
            .Where(f => f.GuildId == guildId)
            .OrderByDescending(f => f.GoodCount)
            .ThenBy(f => f.BadCount)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

    // Guild-wide totals, for the leaderboard footer.
    public async Task<(long Good, long Bad)> GetTotalsAsync(ulong guildId)
    {
        var rows = await _db_context.BotFeedbacks
            .Where(f => f.GuildId == guildId)
            .Select(f => new { f.GoodCount, f.BadCount })
            .ToListAsync();

        return (rows.Sum(r => r.GoodCount), rows.Sum(r => r.BadCount));
    }
}
