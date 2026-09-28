using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The window /admin dashboard shows: the last 7 days, the last 30, or everything recorded.
public enum DashboardWindow { Week, Month, All }

/// <summary>
/// One window of the day-by-day economy: every metric's daily series, aligned on <see cref="Days"/>
/// (Paris day keys, oldest first), and — except for « Tout » — each metric's total over the window
/// of the same length just before, for the trend.
/// </summary>
public sealed class DashboardData
{
    private readonly IReadOnlyDictionary<string, long[]> _series;
    private readonly IReadOnlyDictionary<string, long>? _previous;

    public DashboardData(DashboardWindow window, IReadOnlyList<int> days, IReadOnlyDictionary<string, long[]> series,
        IReadOnlyDictionary<string, long>? previous, int? firstDay)
    {
        Window = window;
        Days = days;
        _series = series;
        _previous = previous;
        FirstDay = firstDay;
    }

    public DashboardWindow Window { get; }
    public IReadOnlyList<int> Days { get; }

    // The first day anything was recorded for this guild, or null when nothing ever was.
    public int? FirstDay { get; }

    public long[] Series(string metric) => _series.TryGetValue(metric, out var s) ? s : new long[Days.Count];
    public long Total(string metric) => Series(metric).Sum();
    public long? PreviousTotal(string metric) => _previous is null ? null : _previous.GetValueOrDefault(metric);
}

// Transient, it wraps AppDbContext. Reads only. The day keys are ints, so the window filters in SQL.
public class EconomyDashboardService
{
    private readonly AppDbContext _db_context;

    public EconomyDashboardService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    public async Task<DashboardData> GetAsync(ulong guildId, DashboardWindow window, DateTimeOffset now)
    {
        var today = AppTime.ToZoned(now).Date;
        var firstDay = await _db_context.EconomyDailyStats.Where(r => r.GuildId == guildId)
            .Select(r => (int?)r.Day).MinAsync();

        int length = window switch
        {
            DashboardWindow.Week => 7,
            DashboardWindow.Month => 30,
            _ => firstDay is { } f ? Math.Max(1, (int)(today - FromKey(f)).TotalDays + 1) : 1,
        };
        var days = Enumerable.Range(0, length).Select(i => Key(today.AddDays(i - length + 1))).ToList();
        var previousFrom = window == DashboardWindow.All ? days[0] : Key(today.AddDays(-2 * length + 1));

        var rows = await _db_context.EconomyDailyStats.AsNoTracking()
            .Where(r => r.GuildId == guildId && r.Day >= previousFrom)
            .Select(r => new { r.Day, r.Metric, r.Value }).ToListAsync();

        var index = days.Select((d, i) => (d, i)).ToDictionary(x => x.d, x => x.i);
        var series = new Dictionary<string, long[]>();
        var previous = window == DashboardWindow.All ? null : new Dictionary<string, long>();
        foreach (var r in rows)
        {
            if (index.TryGetValue(r.Day, out var i))
            {
                if (!series.TryGetValue(r.Metric, out var s)) series[r.Metric] = s = new long[length];
                s[i] += r.Value;
            }
            else if (previous is not null && r.Day < days[0])
            {
                previous[r.Metric] = previous.GetValueOrDefault(r.Metric) + r.Value;
            }
        }
        return new DashboardData(window, days, series, previous, firstDay);
    }

    public static int Key(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;

    public static DateTime FromKey(int key) => new(key / 10000, key / 100 % 100, key % 100);
}
