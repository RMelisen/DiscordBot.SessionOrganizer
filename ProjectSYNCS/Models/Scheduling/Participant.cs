namespace ProjectSYNCS.Models;

public enum ParticipantStatus
{
    Joined,
    Maybe,
    Declined
}

public class Participant
{
    public int Id { get; set; }
    public int SessionEventId { get; set; }
    public ulong UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public ParticipantStatus Status { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    // The first sweep that saw them in one of the server's voice channels inside the session's window
    // (SessionAttendanceService). Null: not seen (yet). Reset when the session's time moves.
    public DateTimeOffset? FirstSeenInVoiceAt { get; set; }

    public SessionEvent SessionEvent { get; set; } = null!;
}
