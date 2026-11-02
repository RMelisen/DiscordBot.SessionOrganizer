namespace ProjectSYNCS.Models;

// Who used which command on one Paris day, for the reach in /report commands: one row per (guild, day,
// command, user), unique, with nothing else on it, since the report only ever counts different people
// and never names them. Command is CommandPath's key with a shortcut folded into the command it stands
// for (CommandPath.Reached). /debug is never written. Buckets only, from 7.56.0 on; DMs are GuildId 0.
public class CommandMemberDailyStat
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public int Day { get; set; }
    public string Command { get; set; } = string.Empty;
    public ulong UserId { get; set; }
}
