namespace ProjectSYNCS.Helpers;

/// <summary>
/// « Ça faisait longtemps »: whether someone greeting her is back from a long absence, read from
/// their last active day (any XP, reaction or voice minute in <c>MemberDailyStat</c>) before today.
/// Pure, on <c>AppTime.DayKey</c> keys, so it can be checked without a gateway.
/// </summary>
public static class LongAbsence
{
    /// <summary>Three weeks: long enough to be missed, short of a fortnight's holiday being news.</summary>
    public const int MinDays = 21;

    /// <summary>
    /// How many days they were away (from their last active day to today), when it is at least
    /// <see cref="MinDays"/>; null for anyone around recently, or never seen (a newcomer is not
    /// back from anywhere).
    /// </summary>
    public static int? DaysAway(int todayKey, int? lastActiveKey)
    {
        if (lastActiveKey is not { } last) return null;
        var days = (AppTime.FromDayKey(todayKey) - AppTime.FromDayKey(last)).Days;
        return days >= MinDays ? days : null;
    }
}
