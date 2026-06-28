namespace ProjectSYNCS.Models;

public enum SessionCategory
{
    Game,
    Activity,
    Movie,
    Other
}

public enum SessionPhase
{
    Scheduled,
    InProgress,
    Finished
}

public class SessionEvent
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong OrganizerId { get; set; }

    // The linked native Discord scheduled event, when the organizer opted in.
    // 0 means no native event is attached to this session.
    public ulong NativeEventId { get; set; }

    public string Title { get; set; } = string.Empty;
    public SessionCategory Category { get; set; } = SessionCategory.Game;
    public DateTimeOffset ScheduledAt { get; set; }
    public int MaxPlayers { get; set; }

    public bool ReminderSent { get; set; } = false;
    public bool IsCancelled { get; set; } = false;

    // Tracks the lifecycle phase already rendered onto the Discord card, so the
    // background service only re-renders the card on an actual transition.
    public SessionPhase RenderedPhase { get; set; } = SessionPhase.Scheduled;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Participant> Participants { get; set; } = new();

    // A session is "in progress" for this long after its start time, then it is
    // archived as finished.
    public static readonly TimeSpan Duration = TimeSpan.FromHours(2);

    /// <summary>The lifecycle phase at a given instant, ignoring cancellation.</summary>
    public SessionPhase PhaseAt(DateTimeOffset now)
    {
        if (now < ScheduledAt) return SessionPhase.Scheduled;
        if (now < ScheduledAt + Duration) return SessionPhase.InProgress;
        return SessionPhase.Finished;
    }
}
