namespace ProjectSYNCS.Models;

// Points one Plynling earned on one Paris day — the daily buckets behind /leaderboard's Plynlings view
// over 7 and 30 days, next to the Plynling.Score totals. Day is the AppTime.DayKey int, so windows filter
// in SQL; GuildId is here so they filter by guild without a join. One row per (Plynling, day), unique.
// Written in the same unit of work as the total; the buckets started empty when the score shipped (the
// rebuilt history lives only in the totals), so they never sum to them. Gone with an abandoned Plynling.
public class PlynlingScoreDay
{
    public int Id { get; set; }
    public int PlynlingId { get; set; }
    public ulong GuildId { get; set; }
    public int Day { get; set; }
    public long Points { get; set; }
}
