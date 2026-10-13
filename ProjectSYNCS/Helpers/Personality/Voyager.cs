namespace ProjectSYNCS.Helpers;

// Voyager 1, her long-distance friend (docs/syncs-voice.md, Her robot heart). Pure, for
// AmbientService's letters and /debug voyager: how long his messages take, and on which days
// one arrives.
//
// The light time comes from JPL Horizons rather than a formula: Voyager 1 (-31) seen from the
// Earth's centre (500@399), one-way light time (QUANTITIES=21), every 30 days from
// 2026-01-01 00:00 UTC, fetched 2026-10-13. The Earth's orbit swings it by about ±6 minutes a
// year; linear interpolation between 30-day samples stays within 0.2 min. Past the table it
// drifts at the table's mean rate. Lines show it rounded to 5 minutes, so the number is true.
public static class Voyager
{
    private static readonly DateTimeOffset TableStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const double StepDays = 30;

    private static readonly double[] LightMinutes =
    {
        1413.72, 1413.96, 1413.13, 1412.11, 1411.81, 1412.91, 1415.73, 1420.16,
        1425.70, 1431.57, 1436.89, 1440.89, 1443.13, 1443.64, 1442.94, 1441.89,
        1441.40, 1442.22, 1444.73, 1448.89, 1454.28, 1460.16, 1465.64, 1469.92,
        1472.49, 1473.28, 1472.74, 1471.68, 1471.03, 1471.57, 1473.77, 1477.66,
        1482.88, 1488.73, 1494.34, 1498.89, 1501.78, 1502.86, 1502.51, 1501.47,
        1500.68, 1500.97, 1502.86, 1506.46, 1511.48, 1517.28, 1523.00, 1527.79,
        1531.00, 1532.39, 1532.25, 1531.27, 1530.36, 1530.41, 1531.99, 1535.30,
        1540.11, 1545.82, 1551.62, 1556.63, 1560.15, 1561.86, 1561.94, 1561.05,
        1560.06, 1559.88, 1561.16, 1564.17, 1568.74, 1574.35, 1580.20, 1585.40,
        1589.22, 1591.26, 1591.59, 1590.81, 1589.77, 1589.39, 1590.38, 1593.07,
        1597.40, 1602.87, 1608.74, 1614.12, 1618.23, 1620.59, 1621.19, 1620.55,
        1619.49, 1618.93, 1619.64, 1622.02, 1626.08, 1631.39, 1637.26, 1642.79,
        1647.17, 1649.85, 1650.74, 1650.27, 1649.21, 1648.50, 1648.94, 1651.01,
        1654.79, 1659.93, 1665.75, 1671.41, 1676.04, 1679.04, 1680.24, 1679.96,
        1678.93, 1678.10, 1678.29, 1680.05, 1683.54, 1688.48, 1694.24, 1699.99,
        1704.86, 1708.18,
    };

    // The table's mean drift, 2026–2035: (1708.18 − 1413.72) / (121 × 30 days).
    private const double DriftMinutesPerDay = 0.0811;

    /// <summary>The day he reached one light-day from Earth, as NASA announced it.</summary>
    public static readonly DateOnly CrossingDay = new(2026, 11, 18);

    // The letter walk starts here; each step waits MinGapDays–MaxGapDays.
    private static readonly int FirstWalkDay = new DateOnly(2026, 10, 1).DayNumber;
    public const int MinGapDays = 10;
    public const int MaxGapDays = 18;

    // Letters arrive between these Paris hours; her answer, when there is one, 2–6 h later
    // and never at or after AnswerEndHour.
    public const int LetterStartHour = 10;
    public const int LetterEndHour = 22;
    public const int AnswerEndHour = 23;

    private const ulong GapSalt = 0x564F_5941_4741_5031;    // "VOYAGAP1"
    private const ulong SlotSalt = 0x564F_5941_534C_4F54;   // "VOYASLOT"
    private const ulong AnswerSalt = 0x564F_5941_414E_5357; // "VOYAANSW"
    private const ulong DelaySalt = 0x564F_5941_4445_4C41;  // "VOYADELA"

    public static TimeSpan LightTime(DateTimeOffset at)
    {
        var days = (at - TableStart).TotalDays;
        if (days <= 0) return TimeSpan.FromMinutes(LightMinutes[0]);

        var lastDays = (LightMinutes.Length - 1) * StepDays;
        if (days >= lastDays)
            return TimeSpan.FromMinutes(LightMinutes[^1] + (days - lastDays) * DriftMinutesPerDay);

        var i = (int)(days / StepDays);
        var f = days / StepDays - i;
        return TimeSpan.FromMinutes(LightMinutes[i] + (LightMinutes[i + 1] - LightMinutes[i]) * f);
    }

    /// <summary>« 23 h 55 »: rounded to 5 minutes, hours past 24 written as such.</summary>
    public static string Format(TimeSpan lightTime)
    {
        var minutes = (int)Math.Round(lightTime.TotalMinutes / 5) * 5;
        return $"{minutes / 60} h {minutes % 60:00}";
    }

    public static bool IsCrossingDay(int dayNumber) => dayNumber == CrossingDay.DayNumber;

    /// <summary>
    /// The letter's index when one arrives on <paramref name="dayNumber"/> (AppTime.DayNumber),
    /// else null. A walk from FirstWalkDay with a gap drawn per step, so the calendar is the
    /// same after any restart and no two letters are close together.
    /// </summary>
    public static int? LetterNumber(int dayNumber)
    {
        var d = FirstWalkDay;
        for (var n = 0; d <= dayNumber; n++)
        {
            if (d == dayNumber) return n;
            d += MinGapDays + (int)(Mix(GapSalt, (ulong)n) % (MaxGapDays - MinGapDays + 1));
        }
        return null;
    }

    /// <summary>When that day's letter arrives, as a Paris time of day in [10:00, 22:00).</summary>
    public static TimeSpan LetterSlot(int dayNumber) =>
        TimeSpan.FromMinutes(LetterStartHour * 60
            + (int)(Mix(SlotSalt, (ulong)dayNumber) % ((LetterEndHour - LetterStartHour) * 60)));

    /// <summary>
    /// When she answers that day's letter, 2–6 h after it, for about half the days; null when
    /// she doesn't, or when the answer would fall at or after 23:00.
    /// </summary>
    public static TimeSpan? AnswerSlot(int dayNumber)
    {
        if (Mix(AnswerSalt, (ulong)dayNumber) % 2 == 1) return null;
        var at = LetterSlot(dayNumber) + TimeSpan.FromMinutes(120 + (int)(Mix(DelaySalt, (ulong)dayNumber) % 241));
        return at < TimeSpan.FromHours(AnswerEndHour) ? at : null;
    }

    // SplitMix64's finaliser over (salt, x): stable across .NET versions, unlike a seeded Random.
    private static ulong Mix(ulong salt, ulong x)
    {
        var z = salt ^ (x * 0x9E3779B97F4A7C15UL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
