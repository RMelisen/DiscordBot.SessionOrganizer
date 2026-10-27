using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectSYNCS.Models;

public enum PollKind
{
    // Options are date/time slots, rendered as <t:...> timestamps (/when).
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

    // When it closes on its own. Null on rows from before deadlines existed: those close
    // DefaultLifetime after they were created, as they always did.
    public DateTimeOffset? EndsAt { get; set; }

    // One option per voter: clicking another option moves the vote.
    public bool SingleChoice { get; set; }

    // The card shows counts and bars, never who voted for what.
    public bool Anonymous { get; set; }

    // The session created from this poll (« Créer une session »), or null.
    public int? SessionEventId { get; set; }

    public List<PollOption> Options { get; set; } = new();

    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(2);

    [NotMapped]
    public DateTimeOffset ClosesAt => EndsAt ?? CreatedAt + DefaultLifetime;
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
