namespace ProjectSYNCS.Models;

// One counter of the economy for one guild and one Paris day — what /admin dashboard charts.
// Metric is an EconomyLog key (« earn.work », « act.meal »…): stored, so append-only. Day is the
// AppTime.DayKey int, so date windows filter in SQL. One row per (guild, day, metric), unique.
public class EconomyDailyStat
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int Day { get; set; }
    public string Metric { get; set; } = string.Empty;
    public long Value { get; set; }
}
