namespace ProjectSYNCS.Models;

// One /remindme. Delivered by DM by PersonalReminderService; if the DM is refused, she pings the
// person in the channel where they asked — unless they asked in a DM (GuildId 0).
//
// DueAt is a UTC DateTimeOffset, which SQLite cannot compare in a query: the loop reads the unsent
// rows and applies the cutoff in memory.
public class PersonalReminder
{
    public int Id { get; set; }

    // 0 when it was asked in a DM.
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong UserId { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DueAt { get; set; }

    // Set before sending: a crash costs the reminder, never a second copy. A cancelled reminder
    // is deleted, not flagged.
    public bool Sent { get; set; }
}
