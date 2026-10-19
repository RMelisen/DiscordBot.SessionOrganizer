namespace ProjectSYNCS.Models;

// How often each command was used in one guild on one Paris day, for /report commands. Command is
// CommandPath's key (« report sessions », « ;work »): a stored string, so append-only. A renamed
// command keeps its old rows under its old name. Failures are uses whose handler reported an error.
// Buckets only, from 7.6.0 on; DMs are GuildId 0. One row per (guild, day, command), unique.
public class CommandDailyStat
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int Day { get; set; }
    public string Command { get; set; } = string.Empty;
    public long Uses { get; set; }
    public long Failures { get; set; }
}
