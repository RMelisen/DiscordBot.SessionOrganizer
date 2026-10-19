namespace ProjectSYNCS.Helpers;

/// <summary>
/// When the monthly digest is due, pure so the checks can walk it through a calendar. A month is
/// keyed yyyymm (202610). The digest for a month goes out from 09:00 Paris on the 1st of the next
/// one, and at the next check after that if she was down — never twice, never for a month older
/// than the last one.
/// </summary>
public static class MonthlyDigest
{
    public const int SendHour = 9;

    public static int MonthKey(int year, int month) => year * 100 + month;

    /// <summary>The month before the one <paramref name="now"/> falls in (Paris), as (year, month).</summary>
    public static (int Year, int Month) PreviousMonth(DateTimeOffset now)
    {
        var zoned = AppTime.ToZoned(now);
        var previous = new DateOnly(zoned.Year, zoned.Month, 1).AddMonths(-1);
        return (previous.Year, previous.Month);
    }

    /// <summary>
    /// The month to send now, or null: the previous month, once it's past 09:00 on the 1st, if it
    /// is newer than <paramref name="lastSent"/>.
    /// </summary>
    public static (int Year, int Month)? Due(DateTimeOffset now, int lastSent)
    {
        var zoned = AppTime.ToZoned(now);
        if (zoned.Day == 1 && zoned.Hour < SendHour) return null;
        var (year, month) = PreviousMonth(now);
        return MonthKey(year, month) > lastSent ? (year, month) : null;
    }
}
