using System.Globalization;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

/// <summary>A report for its window, and the same report for the window before (null for « depuis toujours »).</summary>
public sealed record Compared<T>(T Current, T? Previous) where T : class;

/// <summary>
/// The days one /report (or the monthly digest) covers: inclusive Paris day keys
/// (<see cref="AppTime.DayKey"/>), so daily buckets filter <c>Day &gt;= FromDay &amp;&amp; Day &lt;= ToDay</c>
/// in SQL, and DateTimeOffset rows (sessions, polls, outages) filter in memory through
/// <see cref="Contains"/>, which SQLite needs anyway.
/// </summary>
/// <remarks>
/// « Depuis toujours » starts at day 0 and reads the daily buckets, not the totals tables: a report
/// needs the shape of each day, which totals don't have. It therefore covers only what was counted
/// since each table's buckets existed, and says so in its label.
/// </remarks>
public sealed record ReportWindow(int FromDay, int ToDay, string Label, StatsPeriod? Period)
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public bool IsAllTime => FromDay == 0;

    /// <summary>The rolling window a /report period names, ending today (Paris).</summary>
    public static ReportWindow From(StatsPeriod period, DateTimeOffset now)
    {
        var today = AppTime.DayKey(now);
        return period switch
        {
            StatsPeriod.Week => new(AppTime.DayKey(now.AddDays(-6)), today, "7 derniers jours", period),
            StatsPeriod.Month => new(AppTime.DayKey(now.AddDays(-29)), today, "30 derniers jours", period),
            _ => new(0, today, "depuis le début des relevés", period),
        };
    }

    /// <summary>One calendar month, the digest's window: « octobre 2026 ».</summary>
    public static ReportWindow CalendarMonth(int year, int month)
    {
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        return new(Key(first), Key(last), $"{French.DateTimeFormat.GetMonthName(month)} {year}", null);
    }

    /// <summary>
    /// The window just before this one, to draw a trend against: the same number of days for a
    /// rolling window, the month before for a calendar month, none for « depuis toujours ».
    /// </summary>
    public ReportWindow? Previous
    {
        get
        {
            if (IsAllTime) return null;
            var from = FromDate;
            if (Period is null)
            {
                var month = from.AddMonths(-1);
                return CalendarMonth(month.Year, month.Month);
            }
            var length = ToDate.DayNumber - from.DayNumber + 1;
            return new(Key(from.AddDays(-length)), Key(from.AddDays(-1)), "période précédente", Period);
        }
    }

    public DateOnly FromDate => DateOnly.FromDateTime(AppTime.FromDayKey(FromDay));
    public DateOnly ToDate => DateOnly.FromDateTime(AppTime.FromDayKey(ToDay));

    public bool Contains(int dayKey) => dayKey >= FromDay && dayKey <= ToDay;

    public bool Contains(DateTimeOffset instant) => Contains(AppTime.DayKey(instant));

    /// <summary>
    /// Every day key from <paramref name="start"/> (or the window's own start, if later) to the end
    /// of the window, in order: the x-axis of a sparkline. « Depuis toujours » passes its first day
    /// with data, since day 0 is not a date.
    /// </summary>
    public IReadOnlyList<int> Days(int start)
    {
        var from = Math.Max(start, FromDay);
        if (from == 0 || from > ToDay) return Array.Empty<int>();
        var first = DateOnly.FromDateTime(AppTime.FromDayKey(from));
        var days = new List<int>();
        for (var d = first; d <= ToDate; d = d.AddDays(1)) days.Add(Key(d));
        return days;
    }

    private static int Key(DateOnly d) => d.Year * 10000 + d.Month * 100 + d.Day;
}
