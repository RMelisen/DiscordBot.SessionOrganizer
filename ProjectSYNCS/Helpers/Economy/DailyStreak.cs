namespace ProjectSYNCS.Helpers;

/// <summary>
/// The /daily rules: one claim per Paris calendar day, a streak of consecutive days that a missed
/// day sends back to one, and a pay and an item chance that grow with it, both capped. Pure, so the
/// checks pin every number.
/// </summary>
/// <remarks>
/// Days are compared as calendar dates (<see cref="AppTime.FromDayKey"/>), never by subtracting
/// 24 hours from an instant: on the 25-hour day the clocks go back, 23:30 minus 24 h is still the
/// same day, and that claim would break a streak it should continue.
/// </remarks>
public static class DailyStreak
{
    public const long MinCailloux = 5;
    public const long MaxCailloux = 100;

    // Day 1 pays 10, each day in a row adds 5: the base reaches the cap on day 19.
    public const long BaseFirstDay = 10;
    public const long BasePerDay = 5;

    // Only the chance of a find grows, never its rarity (the find is ItemCatalog.DrawForage).
    public const double ChanceFirstDay = 0.10;
    public const double ChancePerDay = 0.02;
    public const double ChanceMax = 0.60;

    /// <summary>
    /// The streak a claim today makes, or null when today is already claimed. A stamp later than
    /// today (a clock gone back) refuses too, so it never pays twice. <paramref name="lastDay"/> 0
    /// means never claimed.
    /// </summary>
    public static int? NextStreak(int lastDay, int streak, int today) =>
        NextStreak(lastDay, streak, today, new HashSet<int>());

    /// <summary>
    /// As above, but a gap made only of <paramref name="excused"/> days (she was offline, see
    /// <see cref="ExcusedDays"/>) continues the streak as if nothing was missed.
    /// </summary>
    public static int? NextStreak(int lastDay, int streak, int today, IReadOnlySet<int> excused)
    {
        if (lastDay >= today) return null;
        if (lastDay == 0) return 1;
        var last = AppTime.FromDayKey(lastDay);
        var end = AppTime.FromDayKey(today);
        for (var day = last.AddDays(1); day < end; day = day.AddDays(1))
            if (!excused.Contains(day.Year * 10000 + day.Month * 100 + day.Day)) return 1;
        return streak + 1;
    }

    // A missed day is not the member's fault when she was unreachable this long of it.
    public static readonly TimeSpan ExcuseMin = TimeSpan.FromHours(3);

    /// <summary>
    /// The Paris days on which the given stretches of downtime add up to <see cref="ExcuseMin"/> or
    /// more. Overlapping stretches (a crash inside an outage) are merged first, so time counts once.
    /// </summary>
    public static IReadOnlySet<int> ExcusedDays(IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> downtimes)
    {
        var merged = new List<(DateTimeOffset Start, DateTimeOffset End)>();
        foreach (var (start, end) in downtimes.Where(d => d.End > d.Start).OrderBy(d => d.Start))
        {
            if (merged.Count > 0 && start <= merged[^1].End)
                merged[^1] = (merged[^1].Start, end > merged[^1].End ? end : merged[^1].End);
            else
                merged.Add((start, end));
        }

        var perDay = new Dictionary<int, TimeSpan>();
        foreach (var (start, end) in merged)
        {
            for (var day = AppTime.FromDayKey(AppTime.DayKey(start)); ; day = day.AddDays(1))
            {
                var dayStart = AppTime.AtWallClock(day);
                if (dayStart >= end) break;
                var dayEnd = AppTime.AtWallClock(day.AddDays(1));
                var overlap = (end < dayEnd ? end : dayEnd) - (start > dayStart ? start : dayStart);
                if (overlap <= TimeSpan.Zero) continue;
                var key = day.Year * 10000 + day.Month * 100 + day.Day;
                perDay[key] = perDay.GetValueOrDefault(key) + overlap;
            }
        }
        return perDay.Where(p => p.Value >= ExcuseMin).Select(p => p.Key).ToHashSet();
    }

    public static long Base(int streak) => Math.Min(MaxCailloux, BaseFirstDay + BasePerDay * (Math.Max(1, streak) - 1L));

    // The randomness widens as the pay grows, so a long streak still feels like a roll.
    public static int Spread(int streak) => streak <= 5 ? 5 : streak <= 14 ? 10 : 15;

    public static long RollCailloux(int streak, Random rng)
    {
        var spread = Spread(streak);
        return Math.Clamp(Base(streak) + rng.Next(-spread, spread + 1), MinCailloux, MaxCailloux);
    }

    public static double ItemChance(int streak) => Math.Min(ChanceMax, ChanceFirstDay + ChancePerDay * (Math.Max(1, streak) - 1));

    /// <summary>The next Paris midnight: when tomorrow's claim opens.</summary>
    public static DateTimeOffset NextClaimAt(DateTimeOffset now) =>
        AppTime.AtWallClock(AppTime.FromDayKey(AppTime.DayKey(now)).AddDays(1));

    // "1 jour", "12 jours": 0 and 1 singular in French, like PebbleEconomy.Cailloux.
    public static string Days(int n) => $"{n} {(Math.Abs(n) <= 1 ? "jour" : "jours")}";
}
