using ProjectSYNCS.Data;
using ProjectSYNCS.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectSYNCS.Services;

public class PollService
{
    private readonly AppDbContext _db_context;

    public PollService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    public async Task<Poll> CreatePollAsync(
        ulong guildId, ulong channelId, ulong organizerId,
        string title, IEnumerable<DateTimeOffset> slots)
    {
        var poll = new Poll
        {
            GuildId = guildId,
            ChannelId = channelId,
            OrganizerId = organizerId,
            Title = title
        };
        foreach (var slot in slots.OrderBy(s => s))
            poll.Options.Add(new PollOption { ScheduledAt = slot });

        _db_context.Polls.Add(poll);
        await _db_context.SaveChangesAsync();
        return poll;
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

    // Adds the user's vote for the slot, or removes it if it was already cast
    // (multi-select: a user may vote for any number of slots).
    public async Task<Poll?> ToggleVoteAsync(int pollId, int optionId, ulong userId, string username)
    {
        bool optionExists = await _db_context.PollOptions
            .AnyAsync(o => o.Id == optionId && o.PollId == pollId);
        if (!optionExists) return null;

        var existing = await _db_context.PollVotes
            .FirstOrDefaultAsync(v => v.PollOptionId == optionId && v.UserId == userId);

        if (existing is null)
        {
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
        return await GetPollWithVotesAsync(pollId);
    }

    public async Task ClosePollAsync(int pollId)
    {
        var poll = await _db_context.Polls.FindAsync(pollId);
        if (poll is null) return;
        poll.IsClosed = true;
        await _db_context.SaveChangesAsync();
    }
}
