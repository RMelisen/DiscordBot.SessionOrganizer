namespace ProjectSYNCS.Models;

public enum PollKind
{
    // Options are date/time slots, rendered as <t:...> timestamps (/poll).
    DateSlots,
    // Options are free-text labels, e.g. game or movie names (/vote).
    Text
}

public class Poll
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong OrganizerId { get; set; }

    public string Title { get; set; } = string.Empty;
    public PollKind Kind { get; set; } = PollKind.DateSlots;
    public bool IsClosed { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<PollOption> Options { get; set; } = new();
}

public class PollOption
{
    public int Id { get; set; }
    public int PollId { get; set; }

    // Used by DateSlots polls.
    public DateTimeOffset ScheduledAt { get; set; }
    // Used by Text polls.
    public string Label { get; set; } = string.Empty;

    public Poll Poll { get; set; } = null!;
    public List<PollVote> Votes { get; set; } = new();
}

public class PollVote
{
    public int Id { get; set; }
    public int PollOptionId { get; set; }

    public ulong UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTimeOffset VotedAt { get; set; } = DateTimeOffset.UtcNow;

    public PollOption Option { get; set; } = null!;
}
