namespace ProjectSYNCS.Models;

// One channel's day: messages written there (a thread counts under its parent; a voice room's text
// chat under the room) and, for a voice room, person-minutes and the most people at once. For the
// channel and voice-room lists of /report activity. Excluded channels are counted too (shown as
// such). Buckets only, from 7.6.0 on. One row per (guild, channel, day), unique.
public class ChannelDailyStat
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public int Day { get; set; }
    public long Messages { get; set; }
    public long VoiceMinutes { get; set; }
    public int VoicePeak { get; set; }
}
