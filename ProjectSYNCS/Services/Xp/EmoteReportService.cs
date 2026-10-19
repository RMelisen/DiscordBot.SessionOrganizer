using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads the EmoteDailyStat buckets behind /report emotes (Day filtered in SQL) and EmoteStat's
/// markup (where a renamed emote's current name lives, as EmoteStatsService does).
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class EmoteReportService
{
    private readonly AppDbContext _db;

    public EmoteReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<EmoteReportData>> GetAsync(ulong guildId, ReportWindow window, IReadOnlyList<ServerEmote> server)
    {
        var markup = await _db.EmoteStats.AsNoTracking()
            .Where(s => s.GuildId == guildId)
            .ToDictionaryAsync(s => (s.EmoteId, s.Unicode), s => s.Markup);
        async Task<EmoteReportData> ComputeAsync(ReportWindow w) => EmoteReport.Compute(
            await _db.EmoteDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync(),
            server, markup);
        var current = await ComputeAsync(window);
        var previous = window.Previous is { } p ? await ComputeAsync(p) : null;
        return new(current, previous);
    }
}
