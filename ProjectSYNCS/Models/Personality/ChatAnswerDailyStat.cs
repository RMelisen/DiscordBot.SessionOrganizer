namespace ProjectSYNCS.Models;

// The same count, for one Paris day (Day = AppTime.DayKey, so a window filters in SQL). Written in
// the same call as ChatAnswerStat; the buckets don't sum to the totals and must not be made to.
public class ChatAnswerDailyStat
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    public int Day { get; set; }

    public string Path { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;

    public long Count { get; set; }
}
