namespace ProjectSYNCS.Models;

// A channel an admin added to the ones that must all be quiet before SYNCS speaks into
// the silence (AmbientService). One row per (guild, channel); only the home guild's rows
// are read, and /config refuses to write any elsewhere.
//
// Holds only the admin-added ids, like GuildExcludedChannel: the hardcoded ones in
// AmbientService are the floor and are never written here.
public class GuildIdleChannel
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
}
