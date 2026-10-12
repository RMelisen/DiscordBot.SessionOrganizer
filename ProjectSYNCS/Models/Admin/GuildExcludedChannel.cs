namespace ProjectSYNCS.Models;

// A channel an admin added to the "earns nothing" list at runtime. One row per
// (guild, channel).
//
// A separate table rather than a column on GuildSettings because it is a *set* — a
// delimited string in a column would need parsing on every read, and the read is on
// the hot path for every message (see GuildConfigService's cache).
//
// Holds only the admin-added ids. The hardcoded ones in XpTracker.ExcludedChannels are
// never written here: they are the floor, they cannot be removed by a command, and
// duplicating them into the database would create a second source of truth that could
// drift from the code.
public class GuildExcludedChannel
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}
