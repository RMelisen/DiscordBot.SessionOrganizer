using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads the rows behind /report syncs; the arithmetic is in <see cref="SyncsReport"/>. The Pi's
/// tables are global (one Pi) and small, so they are read whole, as PiHealthService.SnapshotAsync
/// does — <c>UptimeEvent</c> has to be anyway, its dates can't be compared in SQL. Everything else
/// has an int <c>Day</c> and filters in SQL.
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class SyncsReportService
{
    private readonly AppDbContext _db;

    public SyncsReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<SyncsReportData>> GetAsync(ulong guildId, ReportWindow window, DateTimeOffset now)
    {
        var thermal = await _db.PiThermalDays.AsNoTracking().ToListAsync();
        var uptime = await _db.UptimeEvents.AsNoTracking().ToListAsync();
        var current = await ComputeAsync(guildId, window, thermal, uptime, now);
        var previous = window.Previous is { } p ? await ComputeAsync(guildId, p, thermal, uptime, now) : null;
        return new(current, previous);
    }

    /// <summary>The hottest day she has ever recorded, for the « record » line: (day key, thousandths of a degree).</summary>
    public async Task<(int Day, int MaxMilli)?> RecordAsync()
    {
        var hottest = await _db.PiThermalDays.AsNoTracking()
            .OrderByDescending(t => t.MaxMilli).FirstOrDefaultAsync();
        return hottest is null ? null : (hottest.Day, hottest.MaxMilli);
    }

    private async Task<SyncsReportData> ComputeAsync(
        ulong guildId, ReportWindow w, IReadOnlyList<Models.PiThermalDay> thermal, IReadOnlyList<Models.UptimeEvent> uptime,
        DateTimeOffset now)
    {
        var answers = await _db.ChatAnswerDailyStats.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync();
        var feedback = await _db.BotFeedbackDailyStats.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync();
        var companions = await _db.VoiceCompanionDailyStats.AsNoTracking()
            .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync();
        return SyncsReport.Compute(thermal, uptime, answers, feedback, companions, w, now);
    }
}
