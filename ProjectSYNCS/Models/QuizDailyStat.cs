namespace ProjectSYNCS.Models;

// One day's quiz wins for one person in one guild, so /quiz leaderboard can be scoped to
// a rolling window. As everywhere in this project, the buckets do not sum to the totals.
public class QuizDailyStat
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // The calendar day in Europe/Paris, yyyymmdd (AppTime.DayKey), so a window filters in SQL.
    public int Day { get; set; }

    public long Wins { get; set; }
}
