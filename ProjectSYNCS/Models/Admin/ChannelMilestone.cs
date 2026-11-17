namespace ProjectSYNCS.Models;

// A milestone a channel reached (Helpers/Admin/ChannelMilestones: 1 000, 10 000, 100 000, every
// 100 000), and the message that reached it. Written when reached. Announced is false for one
// crossed while she was reading history, which she never announces, or one she kept quiet about
// (silence mode, « chut »). One row per (channel, milestone), unique: reached once, even if
// deletions take the count back under it and it is crossed again. /debug milestones lists them.
public class ChannelMilestone
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public long Milestone { get; set; }
    public ulong MessageId { get; set; }
    public ulong AuthorId { get; set; }
    public DateTimeOffset ReachedAt { get; set; }
    public bool Announced { get; set; }
}
