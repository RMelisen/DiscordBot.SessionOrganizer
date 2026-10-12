namespace ProjectSYNCS.Models;

// One person's all-time quiz record in one guild. Totals here, per-day buckets in
// QuizDailyStat — the same pattern as ShameRecord / ShameDailyStat, written in the same
// call. **Summing the buckets does not reproduce this row, and must not be made to.**
public class QuizStat
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    public long Wins { get; set; }

    // The fastest winning answer, in milliseconds from the card going up. Zero means no
    // win yet. Shown on the all-time board only: a window has no use for a lifetime record.
    public long BestMs { get; set; }
}
