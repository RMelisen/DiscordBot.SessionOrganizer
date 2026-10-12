namespace ProjectSYNCS.Models;

// One day of her CPU's temperature, sampled by PiHealthService. Not per guild: it is her body,
// and there is one of it. Keyed by the Paris day (yyyymmdd, AppTime.DayKey), so a month's report
// filters in SQL. Thousandths of a degree, as the kernel gives them: integers, no rounding drift.
public class PiThermalDay
{
    public int Day { get; set; }

    public int MinMilli { get; set; }
    public int MaxMilli { get; set; }
    // Over Samples readings; the day's average is SumMilli / Samples.
    public long SumMilli { get; set; }
    public int Samples { get; set; }

    // When the day's maximum was read (UTC).
    public DateTimeOffset MaxAt { get; set; }

    // Minutes spent at or above PiHealth.ThrottleMilli, where the Pi 5 slows itself down.
    public int HotMinutes { get; set; }
}
