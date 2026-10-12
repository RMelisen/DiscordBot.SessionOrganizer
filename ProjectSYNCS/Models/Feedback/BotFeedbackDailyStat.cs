namespace ProjectSYNCS.Models;

// One day's verdicts from one person in one guild — the same counters as
// BotFeedback, but bucketed by date so the leaderboard can be scoped to a rolling
// window.
//
// It exists for the same reason EmoteDailyStat does: BotFeedback is a pure running
// total with no notion of *when* anything was said, and a date cannot be added to it
// retroactively because the history it holds has none to recover. So the two live
// side by side and answer different questions — BotFeedback stays the exact all-time
// figure including everything counted before buckets existed, and these rows cover
// week and month. Summing every bucket therefore does **not** reproduce BotFeedback,
// and is not meant to.
public class BotFeedbackDailyStat
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // The calendar day in Europe/Paris, encoded yyyymmdd by AppTime.DayKey. An int
    // so SQLite can compare a range without the query first being pulled into memory.
    public int Day { get; set; }

    public long GoodCount { get; set; }
    public long BadCount { get; set; }
}
