namespace ProjectSYNCS.Models;

public class Poll
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong OrganizerId { get; set; }

    public string Title { get; set; } = string.Empty;
    public bool IsClosed { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<PollOption> Options { get; set; } = new();
}

public class PollOption
{
    public int Id { get; set; }
    public int PollId { get; set; }

    public DateTimeOffset ScheduledAt { get; set; }

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
