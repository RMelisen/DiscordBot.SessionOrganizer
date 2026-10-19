namespace ProjectSYNCS.Models;

// One message she found nothing specific to answer (AnswerKinds.IsUnrecognised), kept for
// `/debug answers` and the owner's monthly digest — 30 rolling days (UnansweredLog.KeptDays), pruned
// as new ones arrive. **No author**, at the owner's request: only what was said, cleaned and capped
// by UnansweredLog.Clean. Day is the AppTime.DayKey int, so windows and pruning filter in SQL.
public class UnansweredExample
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    public int Day { get; set; }
    public DateTimeOffset At { get; set; }

    // AnswerKinds keys: the path (reply, mention, name) and the answer she gave instead.
    public string Path { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
