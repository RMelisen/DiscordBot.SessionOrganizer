namespace ProjectSYNCS.Models;

// Stored as int: append-only.
public enum TonightStatus
{
    In = 0,
    Maybe = 1,
    Out = 2,
}

// « Qui est chaud ce soir ? » (/tonight): a lighter call than a session. People answer with a
// button; once enough are in, the organizer can turn it into a real SessionEvent. It closes at
// midnight Paris (ReminderService) or when it becomes a session.
//
// Dates are UTC DateTimeOffsets, which SQLite cannot compare in a query: reads filter the open
// rows in SQL and the dates in memory (see TonightService).
public class TonightCall
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }

    // Named like SessionEvent's so SessionPermissions.CanManage treats it the same way.
    public ulong OrganizerId { get; set; }

    // What they'd play; empty for « on verra ».
    public string Game { get; set; } = string.Empty;

    public DateTimeOffset StartsAt { get; set; }
    public int Minimum { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }

    // Set before the ping goes out: a crash costs the line, never a second ping.
    public bool ThresholdAnnounced { get; set; }

    public bool Closed { get; set; }

    // The session it became, 0 if none.
    public int SessionEventId { get; set; }

    public List<TonightResponse> Responses { get; set; } = new();
}

public class TonightResponse
{
    public int Id { get; set; }
    public int TonightCallId { get; set; }

    public ulong UserId { get; set; }
    public TonightStatus Status { get; set; }
    public DateTimeOffset AnsweredAt { get; set; } = DateTimeOffset.UtcNow;

    public TonightCall TonightCall { get; set; } = null!;
}
