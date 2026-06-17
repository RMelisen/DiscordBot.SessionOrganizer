using ProjectSYNCS.Data;
using ProjectSYNCS.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectSYNCS.Services;

public class EventService
{
    private readonly AppDbContext _db_context;

    public EventService(AppDbContext db_context)
    {
        _db_context = db_context;
    }

    public async Task<SessionEvent> CreateEventAsync(
        ulong guildId, ulong channelId, ulong organizerId,
        string title, SessionCategory category, DateTimeOffset scheduledAt, int maxPlayers)
    {
        var evt = new SessionEvent
        {
            GuildId = guildId,
            ChannelId = channelId,
            OrganizerId = organizerId,
            Title = title,
            Category = category,
            ScheduledAt = scheduledAt,
            MaxPlayers = maxPlayers
        };
        _db_context.SessionEvents.Add(evt);
        await _db_context.SaveChangesAsync();
        return evt;
    }

    public async Task SetMessageIdAsync(int eventId, ulong messageId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.MessageId = messageId;
        await _db_context.SaveChangesAsync();
    }

    public async Task<SessionEvent?> GetEventWithParticipantsAsync(int eventId)
    {
        return await _db_context.SessionEvents
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == eventId);
    }

    public async Task UpsertParticipantAsync(
        SessionEvent sessionEvent, ulong userId, string username, ParticipantStatus status)
    {
        var existing = sessionEvent.Participants.FirstOrDefault(p => p.UserId == userId);
        if (existing is null)
        {
            sessionEvent.Participants.Add(new Participant
            {
                SessionEventId = sessionEvent.Id,
                UserId = userId,
                Username = username,
                Status = status
            });
        }
        else
        {
            existing.Status = status;
            existing.Username = username;
        }
        await _db_context.SaveChangesAsync();
    }

    public async Task UpdateEventAsync(
        int eventId, string title, DateTimeOffset scheduledAt, int maxPlayers)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;

        // If the time moved, allow the reminder to fire again for the new slot.
        if (evt.ScheduledAt != scheduledAt)
            evt.ReminderSent = false;

        evt.Title = title;
        evt.ScheduledAt = scheduledAt;
        evt.MaxPlayers = maxPlayers;
        await _db_context.SaveChangesAsync();
    }

    public async Task CancelEventAsync(int eventId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.IsCancelled = true;
        await _db_context.SaveChangesAsync();
    }

    public async Task<List<SessionEvent>> GetEventsNeedingReminderAsync()
    {
        var windowStart = DateTimeOffset.UtcNow.AddMinutes(25);
        var windowEnd = DateTimeOffset.UtcNow.AddMinutes(35);

        return await _db_context.SessionEvents
            .Include(e => e.Participants)
            .Where(e =>
                !e.ReminderSent &&
                !e.IsCancelled &&
                e.ScheduledAt >= windowStart &&
                e.ScheduledAt <= windowEnd)
            .ToListAsync();
    }

    public async Task MarkReminderSentAsync(int eventId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.ReminderSent = true;
        await _db_context.SaveChangesAsync();
    }
}
