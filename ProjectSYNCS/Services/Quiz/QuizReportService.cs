using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>Reads the QuizRound rows behind /report quiz (Day is an int: filtered in SQL).</summary>
/// <remarks>Transient, like every service wrapping the context.</remarks>
public class QuizReportService
{
    private readonly AppDbContext _db;

    public QuizReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Compared<QuizReportData>> GetAsync(ulong guildId, ReportWindow window)
    {
        async Task<QuizReportData> ComputeAsync(ReportWindow w) => QuizReport.Compute(
            await _db.QuizRounds.AsNoTracking()
                .Where(r => r.GuildId == guildId && r.Day >= w.FromDay && r.Day <= w.ToDay).ToListAsync());
        var current = await ComputeAsync(window);
        var previous = window.Previous is { } p ? await ComputeAsync(p) : null;
        return new(current, previous);
    }
}
