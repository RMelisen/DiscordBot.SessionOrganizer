namespace ProjectSYNCS.Models;

public class SessionEvent
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong OrganizerId { get; set; }

    public string GameName { get; set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; set; }
    public int MaxPlayers { get; set; }

    public bool ReminderSent { get; set; } = false;
    public bool IsCancelled { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Participant> Participants { get; set; } = new();
}
