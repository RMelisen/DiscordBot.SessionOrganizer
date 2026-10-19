namespace ProjectSYNCS.Models;

// How many people joined and left one guild on one Paris day (MemberFlowTracker), for
// /report community and the monthly digest. Bots are not counted.
//
// Daily buckets only, with no totals table: nothing needs an all-time figure that predates the
// buckets, and Discord already shows the current member count. Counting started with 7.5.0;
// earlier days simply have no row. Day is the AppTime.DayKey int, so windows filter in SQL.
// One row per (guild, day), unique.
public class MemberFlowDailyStat
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int Day { get; set; }
    public int Joins { get; set; }
    public int Leaves { get; set; }
}
