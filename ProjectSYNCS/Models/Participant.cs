namespace ProjectSYNCS.Models;

public enum ParticipantStatus
{
    Joined,
    Substitute,
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

    public SessionEvent SessionEvent { get; set; } = null!;
}
