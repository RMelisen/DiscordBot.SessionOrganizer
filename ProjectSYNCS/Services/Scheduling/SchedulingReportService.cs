using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads the rows behind /report sessions and /report polls; the arithmetic is in
/// <see cref="SessionReport"/> and <see cref="PollReport"/>. Every row carries DateTimeOffsets, which
/// SQLite can't compare, so the guild's rows are read whole and windowed in memory (root CLAUDE.md).
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class SchedulingReportService
{
    private readonly AppDbContext _db;

    public SchedulingReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<SessionReportData>> GetSessionsAsync(ulong guildId, ReportWindow window, DateTimeOffset now)
    {
        var sessions = await _db.SessionEvents.AsNoTracking()
            .Include(s => s.Participants)
            .Where(s => s.GuildId == guildId)
            .ToListAsync();
        return new(
            SessionReport.Compute(sessions, window, now),
            window.Previous is { } previous ? SessionReport.Compute(sessions, previous, now) : null);
    }

    public async Task<Compared<PollReportData>> GetPollsAsync(ulong guildId, ReportWindow window, ulong botId)
    {
        var polls = await _db.Polls.AsNoTracking()
            .Include(p => p.Options).ThenInclude(o => o.Votes)
            .Where(p => p.GuildId == guildId)
            .ToListAsync();
        var calls = await _db.TonightCalls.AsNoTracking()
            .Include(c => c.Responses)
            .Where(c => c.GuildId == guildId)
            .ToListAsync();
        var sessions = await _db.SessionEvents.AsNoTracking()
            .Where(s => s.GuildId == guildId)
            .ToListAsync();
        return new(
            PollReport.Compute(polls, calls, sessions, window, botId),
            window.Previous is { } previous ? PollReport.Compute(polls, calls, sessions, previous, botId) : null);
    }
}
