using ProjectSYNCS.Data;
using ProjectSYNCS.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectSYNCS.Services;

/// <summary>How a new poll or vote behaves: how long it stays open, one vote or several, names shown or not.</summary>
public sealed record PollSettings(TimeSpan Lifetime, bool SingleChoice, bool Anonymous)
{
    public static readonly PollSettings Default = new(Poll.DefaultLifetime, false, false);
}

public class PollService
{
    private readonly AppDbContext _db_context;

    public PollService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    public async Task<Poll> CreatePollAsync(
        ulong guildId, ulong channelId, ulong organizerId,
        string title, IEnumerable<DateTimeOffset> slots, PollSettings? settings = null)
    {
        var poll = New(guildId, channelId, organizerId, title, PollKind.DateSlots, settings);
        foreach (var slot in slots.OrderBy(s => s))
            poll.Options.Add(new PollOption { ScheduledAt = slot });

        _db_context.Polls.Add(poll);
        await _db_context.SaveChangesAsync();
        return poll;
    }

    public async Task<Poll> CreateTextPollAsync(
        ulong guildId, ulong channelId, ulong organizerId,
        string title, IEnumerable<string> labels, PollSettings? settings = null)
    {
        var poll = New(guildId, channelId, organizerId, title, PollKind.Text, settings);
        // Preserve the order in which the options were entered.
        foreach (var label in labels)
            poll.Options.Add(new PollOption { Label = label });

        _db_context.Polls.Add(poll);
        await _db_context.SaveChangesAsync();
        return poll;
    }

    private static Poll New(ulong guildId, ulong channelId, ulong organizerId, string title, PollKind kind,
        PollSettings? settings)
    {
        var now = DateTimeOffset.UtcNow;
        var s = settings ?? PollSettings.Default;
        return new Poll
        {
            GuildId = guildId,
            ChannelId = channelId,
            OrganizerId = organizerId,
            Title = title,
            Kind = kind,
            CreatedAt = now,
            EndsAt = now + s.Lifetime,
            SingleChoice = s.SingleChoice,
            Anonymous = s.Anonymous,
        };
    }

    public async Task SetMessageIdAsync(int pollId, ulong messageId)
    {
        var poll = await _db_context.Polls.FindAsync(pollId);
        if (poll is null) return;
        poll.MessageId = messageId;
        await _db_context.SaveChangesAsync();
    }

    public async Task<Poll?> GetPollWithVotesAsync(int pollId)
    {
        return await _db_context.Polls
            .Include(p => p.Options)
            .ThenInclude(o => o.Votes)
            .FirstOrDefaultAsync(p => p.Id == pollId);
    }

    public async Task<List<Poll>> GetActivePollsAsync(ulong guildId, PollKind kind)
    {
        var polls = await _db_context.Polls
            .Include(p => p.Options)
            .ThenInclude(o => o.Votes)
            .Where(p => p.GuildId == guildId && p.Kind == kind && !p.IsClosed)
            .ToListAsync();

        // SQLite can't translate DateTimeOffset ordering; sort in memory.
        return polls.OrderByDescending(p => p.CreatedAt).ToList();
    }

    // Used when a poll card is reposted, possibly into a different channel.
    public async Task SetMessageLocationAsync(int pollId, ulong channelId, ulong messageId)
    {
        var poll = await _db_context.Polls.FindAsync(pollId);
        if (poll is null) return;
        poll.ChannelId = channelId;
        poll.MessageId = messageId;
        await _db_context.SaveChangesAsync();
    }

    // Adds the user's vote for the option, or removes it if it was already cast. Multi-select by
    // default: a user may vote for any number of options. A single-choice poll moves the vote
    // instead: the user's other votes on it go in the same save. The caller holds the poll's lock
    // (SchedulingLocks), so a double click can't add two rows; the unique index is the last guard.
    public async Task<Poll?> ToggleVoteAsync(int pollId, int optionId, ulong userId, string username, bool singleChoice = false)
    {
        bool optionExists = await _db_context.PollOptions
            .AnyAsync(o => o.Id == optionId && o.PollId == pollId);
        if (!optionExists) return null;

        var existing = await _db_context.PollVotes
            .FirstOrDefaultAsync(v => v.PollOptionId == optionId && v.UserId == userId);

        if (existing is null)
        {
            if (singleChoice)
            {
                var others = await _db_context.PollVotes
                    .Where(v => v.UserId == userId && v.Option.PollId == pollId)
                    .ToListAsync();
                _db_context.PollVotes.RemoveRange(others);
            }
            _db_context.PollVotes.Add(new PollVote
            {
                PollOptionId = optionId,
                UserId = userId,
                Username = username
            });
        }
        else
        {
            _db_context.PollVotes.Remove(existing);
        }

        await _db_context.SaveChangesAsync();
        _db_context.ChangeTracker.Clear();
        return await GetPollWithVotesAsync(pollId);
    }

    public async Task ClosePollAsync(int pollId)
    {
        var poll = await _db_context.Polls.FindAsync(pollId);
        if (poll is null) return;
        poll.IsClosed = true;
        await _db_context.SaveChangesAsync();
    }

    /// <summary>Moves the deadline (Gérer → Prolonger). An old row without one gets one.</summary>
    public async Task SetEndsAtAsync(int pollId, DateTimeOffset endsAt)
    {
        var poll = await _db_context.Polls.FindAsync(pollId);
        if (poll is null) return;
        poll.EndsAt = endsAt;
        await _db_context.SaveChangesAsync();
    }

    public async Task RenameAsync(int pollId, string title)
    {
        var poll = await _db_context.Polls.FindAsync(pollId);
        if (poll is null) return;
        poll.Title = title;
        await _db_context.SaveChangesAsync();
    }

    /// <summary>Adds a slot (date poll) or a label (vote). False when it is already there.</summary>
    public async Task<bool> AddOptionAsync(int pollId, DateTimeOffset? slot, string? label)
    {
        var options = await _db_context.PollOptions.Where(o => o.PollId == pollId).ToListAsync();
        if (slot is { } at)
        {
            if (options.Any(o => o.ScheduledAt == at)) return false;
            _db_context.PollOptions.Add(new PollOption { PollId = pollId, ScheduledAt = at });
        }
        else
        {
            if (options.Any(o => string.Equals(o.Label, label, StringComparison.OrdinalIgnoreCase))) return false;
            _db_context.PollOptions.Add(new PollOption { PollId = pollId, Label = label ?? string.Empty });
        }
        await _db_context.SaveChangesAsync();
        return true;
    }

    /// <summary>Removes an option and (by cascade) its votes.</summary>
    public async Task RemoveOptionAsync(int pollId, int optionId)
    {
        var option = await _db_context.PollOptions.FirstOrDefaultAsync(o => o.Id == optionId && o.PollId == pollId);
        if (option is null) return;
        _db_context.PollOptions.Remove(option);
        await _db_context.SaveChangesAsync();
    }

    /// <summary>The session « Créer une session » made from this poll: the card links to it from then on.</summary>
    public async Task SetSessionAsync(int pollId, int sessionEventId)
    {
        var poll = await _db_context.Polls.FindAsync(pollId);
        if (poll is null) return;
        poll.SessionEventId = sessionEventId;
        await _db_context.SaveChangesAsync();
    }

    // Open polls/votes whose deadline has passed and should auto-close. SQLite can't translate
    // DateTimeOffset comparisons, so the deadline is applied in memory.
    public async Task<List<Poll>> GetPollsToAutoCloseAsync(DateTimeOffset now)
    {
        var open = await _db_context.Polls
            .Include(p => p.Options)
            .ThenInclude(o => o.Votes)
            .Where(p => !p.IsClosed)
            .ToListAsync();

        return open.Where(p => p.ClosesAt <= now).ToList();
    }

    // Whether this organizer created a poll here since `since` (the weekend poll's once-a-week
    // claim, which survives a restart because it is the poll itself). Dates compared in memory.
    public async Task<bool> HasPollByOrganizerSinceAsync(ulong guildId, ulong organizerId, DateTimeOffset since)
    {
        var polls = await _db_context.Polls
            .Where(p => p.GuildId == guildId && p.OrganizerId == organizerId)
            .ToListAsync();
        return polls.Any(p => p.CreatedAt >= since);
    }

    // Removes the poll and (by cascade) its options and votes.
    public async Task DeletePollAsync(int pollId)
    {
        var poll = await _db_context.Polls.FindAsync(pollId);
        if (poll is null) return;
        _db_context.Polls.Remove(poll);
        await _db_context.SaveChangesAsync();
    }
}
