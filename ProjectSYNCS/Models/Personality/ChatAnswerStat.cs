namespace ProjectSYNCS.Models;

// How many times she gave one kind of chat answer, on one path, in one guild — all time. Path and
// Kind are stored keys (Helpers/Personality/AnswerKinds): append-only. GuildId 0 is DMs.
// The totals half of the totals + daily buckets pair with ChatAnswerDailyStat.
public class ChatAnswerStat
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    public string Path { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;

    public long Count { get; set; }
}
