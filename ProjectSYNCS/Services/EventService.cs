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

    // Used when a card is reposted, possibly into a different channel.
    public async Task SetMessageLocationAsync(int eventId, ulong channelId, ulong messageId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.ChannelId = channelId;
        evt.MessageId = messageId;
        await _db_context.SaveChangesAsync();
    }

    public async Task<List<SessionEvent>> GetActiveEventsAsync(ulong guildId)
    {
        var now = DateTimeOffset.UtcNow;
        // SQLite can't translate DateTimeOffset comparisons, so filter the rest
        // in SQL and compare/sort the dates in memory.
        var events = await _db_context.SessionEvents
            .Include(e => e.Participants)
            .Where(e => e.GuildId == guildId && !e.IsCancelled)
            .ToListAsync();

        return events
            .Where(e => e.ScheduledAt > now)
            .OrderBy(e => e.ScheduledAt)
            .ToList();
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

        // SQLite can't translate DateTimeOffset comparisons; filter flags in SQL
        // and apply the time window in memory.
        var candidates = await _db_context.SessionEvents
            .Include(e => e.Participants)
            .Where(e => !e.ReminderSent && !e.IsCancelled)
            .ToListAsync();

        return candidates
            .Where(e => e.ScheduledAt >= windowStart && e.ScheduledAt <= windowEnd)
            .ToList();
    }

    public async Task MarkReminderSentAsync(int eventId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.ReminderSent = true;
        await _db_context.SaveChangesAsync();
    }

    // Events whose lifecycle phase (Scheduled -> InProgress -> Finished) has moved
    // past what was last rendered on their card. Already-finished and cancelled
    // events are skipped. SQLite can't translate DateTimeOffset comparisons, so the
    // phase is computed in memory.
    public async Task<List<SessionEvent>> GetEventsNeedingLifecycleUpdateAsync()
    {
        var candidates = await _db_context.SessionEvents
            .Include(e => e.Participants)
            .Where(e => !e.IsCancelled && e.RenderedPhase != SessionPhase.Finished)
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        return candidates
            .Where(e => e.PhaseAt(now) != e.RenderedPhase)
            .ToList();
    }

    public async Task SetRenderedPhaseAsync(int eventId, SessionPhase phase)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.RenderedPhase = phase;
        await _db_context.SaveChangesAsync();
    }
}
