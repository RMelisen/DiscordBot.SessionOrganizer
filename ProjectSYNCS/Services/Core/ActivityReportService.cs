using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads the buckets behind /report activity and /report commands; the arithmetic is in
/// <see cref="ActivityReport"/> and <see cref="CommandReport"/>. Every table has an int Day, so windows filter in SQL.
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class ActivityReportService
{
    private readonly AppDbContext _db;

    public ActivityReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<ActivityReportData>> GetActivityAsync(ulong guildId, ReportWindow window, IReadOnlyList<ChannelInfo> channels)
    {
        async Task<ActivityReportData> ComputeAsync(ReportWindow w)
        {
            var hours = await _db.ActivityHourStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync();
            var perChannel = await _db.ChannelDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync();
            return ActivityReport.Compute(hours, perChannel, channels);
        }
        var current = await ComputeAsync(window);
        var previous = window.Previous is { } p ? await ComputeAsync(p) : null;
        return new(current, previous);
    }

    public async Task<Compared<CommandReportData>> GetCommandsAsync(ulong guildId, ReportWindow window, IReadOnlyCollection<string> registered)
    {
        async Task<CommandReportData> ComputeAsync(ReportWindow w) => CommandReport.Compute(
            await _db.CommandDailyStats.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync(),
            registered);
        var current = await ComputeAsync(window);
        var previous = window.Previous is { } p ? await ComputeAsync(p) : null;
        return new(current, previous);
    }
}
