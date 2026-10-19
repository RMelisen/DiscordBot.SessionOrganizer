using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Reads the rows behind /report plynlings: the guild's Plynlings (settled to now in memory only, as
/// AdminStatsService does: AsNoTracking, never saved) and their events, relations, badges and traits,
/// windowed in memory (their dates are DateTimeOffsets).
/// </summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class PlynlingReportService
{
    private readonly AppDbContext _db;

    public PlynlingReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<PlynlingReportData>> GetAsync(ulong guildId, ReportWindow window, DateTimeOffset now)
    {
        var plynlings = await _db.Plynlings.AsNoTracking().Where(p => p.GuildId == guildId).ToListAsync();
        foreach (var p in plynlings) PlynlingLife.Settle(p, now);   // in memory only: AsNoTracking, never saved
        var ids = plynlings.Select(p => p.Id).ToList();
        var events = await _db.PlynlingEventInstances.AsNoTracking()
            .Where(e => ids.Contains(e.PlynlingId) && e.ResolvedAt != null).ToListAsync();
        var relations = await _db.PlynlingRelations.AsNoTracking().Where(r => ids.Contains(r.PlynlingAId)).ToListAsync();
        var badges = await _db.PlynlingBadges.AsNoTracking().Where(b => ids.Contains(b.PlynlingId)).ToListAsync();
        var traits = await _db.PlynlingTraits.AsNoTracking().Where(t => ids.Contains(t.PlynlingId)).ToListAsync();

        PlynlingReportData Compute(ReportWindow w) => PlynlingReport.Compute(plynlings, events, relations, badges, traits, w, now);
        return new(Compute(window), window.Previous is { } prev ? Compute(prev) : null);
    }
}
