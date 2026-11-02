using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads the IncidentDailyStat rows behind /report incidents (Day is an int: filtered in SQL). Not per
/// guild, like the Pi's tables.
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class IncidentReportService
{
    private readonly AppDbContext _db;

    public IncidentReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<IncidentReportData>> GetAsync(ReportWindow window)
    {
        async Task<IncidentReportData> ComputeAsync(ReportWindow w) => IncidentReport.Compute(
            await _db.IncidentDailyStats.AsNoTracking().Where(x => x.Day >= w.FromDay && x.Day <= w.ToDay).ToListAsync());
        var current = await ComputeAsync(window);
        var previous = window.Previous is { } p ? await ComputeAsync(p) : null;
        return new(current, previous, await _db.IncidentDailyStats.MinAsync(x => (int?)x.Day));
    }
}
