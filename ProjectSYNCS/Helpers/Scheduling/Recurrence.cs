using System.Globalization;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// When a weekly series' next session falls, and when it is posted. Pure and on Paris wall clock
/// through <see cref="AppTime"/>, so « jeudi 21h » stays 21:00 across both clock changes.
/// </summary>
public static class Recurrence
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    /// <summary>The first « <paramref name="day"/> at <paramref name="minuteOfDay"/> » strictly after <paramref name="afterUtc"/>, in UTC.</summary>
    public static DateTimeOffset NextOccurrence(DayOfWeek day, int minuteOfDay, DateTimeOffset afterUtc)
    {
        var zoned = AppTime.ToZoned(afterUtc).DateTime;
        var ahead = ((int)day - (int)zoned.DayOfWeek + 7) % 7;
        var wall = zoned.Date.AddDays(ahead).AddMinutes(minuteOfDay);
        var at = AppTime.AtWallClock(wall);
        if (at <= afterUtc) at = AppTime.AtWallClock(wall.AddDays(7));
        return at.ToUniversalTime();
    }

    /// <summary>The series' next session: the first one after both its last occurrence and now.</summary>
    public static DateTimeOffset NextOccurrence(RecurringSession series, DateTimeOffset nowUtc) =>
        NextOccurrence(series.DayOfWeek, series.MinuteOfDay,
            series.LastOccurrence > nowUtc ? series.LastOccurrence : nowUtc);

    /// <summary>Whether <paramref name="occurrence"/> is close enough to be posted now.</summary>
    public static bool DueToPost(DateTimeOffset occurrence, DateTimeOffset nowUtc, int leadDays) =>
        occurrence > nowUtc && occurrence - nowUtc <= TimeSpan.FromDays(leadDays);

    /// <summary>The series' day and time, from its first session: (jeudi, 1260) for a Thursday at 21:00.</summary>
    public static (DayOfWeek Day, int MinuteOfDay) SlotOf(DateTimeOffset at)
    {
        var zoned = AppTime.ToZoned(at);
        return (zoned.DayOfWeek, zoned.Hour * 60 + zoned.Minute);
    }

    /// <summary>« jeudi ».</summary>
    public static string DayName(DayOfWeek day) => Fr.DateTimeFormat.GetDayName(day);

    /// <summary>« 🔁 chaque jeudi », on the card.</summary>
    public static string Badge(DateTimeOffset at) => $"🔁 chaque {DayName(AppTime.ToZoned(at).DayOfWeek)}";
}
