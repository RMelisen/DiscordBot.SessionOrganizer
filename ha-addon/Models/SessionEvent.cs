namespace ProjectSYNCS.Models;

public enum SessionCategory
{
    Game,
    Activity,
    Movie,
    Other
}

public class SessionEvent
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong OrganizerId { get; set; }

    public string Title { get; set; } = string.Empty;
    public SessionCategory Category { get; set; } = SessionCategory.Game;
    public DateTimeOffset ScheduledAt { get; set; }
    public int MaxPlayers { get; set; }

    public bool ReminderSent { get; set; } = false;
    public bool IsCancelled { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Participant> Participants { get; set; } = new();
}
