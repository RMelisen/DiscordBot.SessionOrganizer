using System.ComponentModel.DataAnnotations.Schema;

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

    // The DM to the people still on « Peut-être », about an hour before (ReminderService). Set
    // before the DMs go out, reset with ReminderSent when the time moves.
    public bool MaybeNudgeSent { get; set; } = false;

    // Her two lines around a voice session (SessionAttendanceService): the late call at
    // start + LateCallAfter, and the recap once the group has scattered. Each is set before
    // its message goes out, so a crash costs a missing line, never a second one. Reset with
    // ReminderSent when the time moves.
    public bool LateCallSent { get; set; } = false;
    public bool RecapSent { get; set; } = false;

    // Tracks the lifecycle phase already rendered onto the Discord card, so the
    // background service only re-renders the card on an actual transition.
    public SessionPhase RenderedPhase { get; set; } = SessionPhase.Scheduled;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // How long it runs: « en cours » for this long after the start, then finished. Also the native
    // event's end, the roll call's window and the voice channel status.
    public int DurationMinutes { get; set; } = DefaultDurationMinutes;

    // Free text from the organiser, shown on the card. Empty for none.
    public string Description { get; set; } = string.Empty;

    // The voice channel it happens in, when the organiser named one. 0 for none.
    public ulong VoiceChannelId { get; set; }

    // The weekly series it was posted for (RecurringSession), or null for a one-off.
    public int? RecurringSessionId { get; set; }

    public List<Participant> Participants { get; set; } = new();

    public const int DefaultDurationMinutes = 120;

    [NotMapped]
    public TimeSpan Length => TimeSpan.FromMinutes(DurationMinutes > 0 ? DurationMinutes : DefaultDurationMinutes);

    /// <summary>The lifecycle phase at a given instant, ignoring cancellation.</summary>
    public SessionPhase PhaseAt(DateTimeOffset now)
    {
        if (now < ScheduledAt) return SessionPhase.Scheduled;
        if (now < ScheduledAt + Length) return SessionPhase.InProgress;
        return SessionPhase.Finished;
    }
}
