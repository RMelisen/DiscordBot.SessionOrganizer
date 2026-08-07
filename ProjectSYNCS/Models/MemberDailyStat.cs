namespace ProjectSYNCS.Models;

// One day's activity for one person in one guild — the same three counters as
// MemberXp, bucketed by date so /leaderboard can be scoped to a rolling window.
//
// The third instance of the pattern EmoteStat/EmoteDailyStat and
// BotFeedback/BotFeedbackDailyStat already established, and for the same reason:
// MemberXp is a pure running total with no notion of *when* anything was earned, and
// a date cannot be added to it retroactively because the history it holds has none to
// recover. So the two live side by side and answer different questions — MemberXp
// stays the exact all-time figure including everything counted before buckets existed,
// and these rows cover week and month. **Summing every bucket does not reproduce
// MemberXp, and must not be made to.**
//
// One consequence specific to XP: a *level* is a function of the lifetime total, so it
// belongs only to the all-time view. A windowed ranking shows XP earned in the window
// and no level, because there is no such thing as "the level you were in the last 7
// days".
public class MemberDailyStat
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }

    // The calendar day in Europe/Paris, encoded yyyymmdd by AppTime.DayKey. An int
    // so SQLite can compare a range without the query first being pulled into memory.
    public int Day { get; set; }

    public long XpEarned { get; set; }

    // As on MemberXp, a reaction removal decrements *today's* bucket even when the
    // reaction was added weeks ago — tracking the original day would mean a row per
    // reaction. Clamped at zero, so a window can never go negative.
    public long ReactionsUsed { get; set; }

    public long VoiceMinutes { get; set; }
}
