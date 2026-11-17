namespace ProjectSYNCS.Models;

// How many messages a text channel holds, as ChannelMilestoneService counts them: its history read
// once oldest first, then every new message, minus the deletions she saw. Cursor is the id of the
// last message counted: a restart reads on from there, so messages counted but not yet saved come
// back without being counted twice. Saved every 30 minutes, never per message. One row per
// (guild, channel), unique.
public class ChannelMessageCount
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public long Count { get; set; }
    public ulong Cursor { get; set; }
}
