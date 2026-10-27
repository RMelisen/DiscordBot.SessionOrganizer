namespace ProjectSYNCS.Models;

// A weekly series (/schedule create … weekly:true): the template every occurrence is posted from.
// ReminderService posts each one as an ordinary SessionEvent, LeadDays before it, in Channel.
// Its time is Paris wall clock (DayOfWeek + MinuteOfDay), so 21:00 stays 21:00 across the clock
// changes. DayOfWeek is stored as int: System.DayOfWeek's values never move.
public class RecurringSession
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong OrganizerId { get; set; }

    public string Title { get; set; } = string.Empty;
    public SessionCategory Category { get; set; } = SessionCategory.Game;

    public DayOfWeek DayOfWeek { get; set; }
    // Minutes after midnight, Paris time: 21:00 is 1260.
    public int MinuteOfDay { get; set; }

    public int MaxPlayers { get; set; }
    public int DurationMinutes { get; set; } = SessionEvent.DefaultDurationMinutes;
    public ulong VoiceChannelId { get; set; }
    public string Description { get; set; } = string.Empty;

    // How many days ahead each occurrence is posted.
    public int LeadDays { get; set; } = DefaultLeadDays;

    public bool Active { get; set; } = true;

    // The latest occurrence posted (or skipped): the next one is the first after it. A moved or
    // cancelled occurrence then never brings the original slot back.
    public DateTimeOffset LastOccurrence { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public const int DefaultLeadDays = 3;
}
