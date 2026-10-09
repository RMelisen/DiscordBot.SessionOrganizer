namespace ProjectSYNCS.Models;

// One question she posted, open for an hour (QuizSchedule.OpenFor) or until someone gets
// it right. Stored rather than held in memory so a restart resumes it: the buttons find
// their round by id, and the sweep closes anything that came due while the bot was down.
//
// PostedAt and ClosesAt are UTC DateTimeOffsets, which SQLite cannot compare in a query:
// every read filters on Closed / GuildId / Day in SQL and applies the cutoff in memory.
// See QuizService.
public class QuizRound
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }

    // The card. Zero until the send succeeded: the row is written first, so a click can
    // never reach a round that does not exist.
    public ulong MessageId { get; set; }

    // QuizBank key, stored: keys are append-only (renaming one orphans its rounds).
    public string QuestionKey { get; set; } = string.Empty;

    // How the four choices were shuffled when posted: position i on the card shows
    // Choices[ChoiceOrder[i]], so the right one is wherever '0' sits. Empty for an open
    // question. Stored because the buttons and the reveal must agree after a restart.
    public string ChoiceOrder { get; set; } = string.Empty;

    public DateTimeOffset PostedAt { get; set; }
    public DateTimeOffset ClosesAt { get; set; }

    // The Paris day it was posted on, yyyymmdd (AppTime.DayKey), so "how many today"
    // is counted in SQL.
    public int Day { get; set; }

    // What the winner is paid, fixed at post time from the question's difficulty.
    public long Reward { get; set; }

    // Zero while nobody has won — the same "absent snowflake" convention as
    // GuildSettings. The conditional update on this column is what makes a win
    // happen once.
    public ulong WinnerId { get; set; }
    public DateTimeOffset? WonAt { get; set; }

    // Set by a win or by the timeout. Never reopened.
    public bool Closed { get; set; }
}
