namespace ProjectSYNCS.Models;

// Sets completed by one person on one Paris day — the daily buckets behind /leaderboard's
// Collections view, next to the CollectionCompletion.Count totals. Day is the AppTime.DayKey int,
// so windows filter in SQL. One row per (guild, user, day), unique. Written in the same unit of
// work as the totals; the buckets started empty, so they never sum to the totals.
public class CollectionDailyStat
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int Day { get; set; }
    public int Completions { get; set; }
}
