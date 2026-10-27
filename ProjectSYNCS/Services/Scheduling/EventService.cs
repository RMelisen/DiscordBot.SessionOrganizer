using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
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
        string title, SessionCategory category, DateTimeOffset scheduledAt, int maxPlayers,
        int durationMinutes = SessionEvent.DefaultDurationMinutes, string description = "",
        ulong voiceChannelId = 0, int? recurringSessionId = null)
    {
        var evt = new SessionEvent
        {
            GuildId = guildId,
            ChannelId = channelId,
            OrganizerId = organizerId,
            Title = title,
            Category = category,
            ScheduledAt = scheduledAt,
            MaxPlayers = maxPlayers,
            DurationMinutes = durationMinutes,
            Description = description,
            VoiceChannelId = voiceChannelId,
            RecurringSessionId = recurringSessionId,
        };
        _db_context.SessionEvents.Add(evt);
        await _db_context.SaveChangesAsync();
        return evt;
    }

    /// <summary>
    /// Removes a session whose card never made it to the channel (no orphan row in /schedule list,
    /// no reminder for a session nobody can see). Its participants go with it (cascade).
    /// </summary>
    public async Task DeleteEventAsync(int eventId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        _db_context.SessionEvents.Remove(evt);
        await _db_context.SaveChangesAsync();
    }

    public async Task SetMessageIdAsync(int eventId, ulong messageId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.MessageId = messageId;
        await _db_context.SaveChangesAsync();
    }

    // Links (or unlinks, with 0) a native Discord scheduled event to a session.
    public async Task SetNativeEventIdAsync(int eventId, ulong nativeEventId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.NativeEventId = nativeEventId;
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

    /// <summary>
    /// The sessions not over yet, the one running first: GetActiveEventsAsync plus a session still
    /// inside its <see cref="SessionEvent.Length"/>. What « c'est quand la prochaine session ? » is
    /// answered from: « on joue ce soir ? » asked at 21:10 should hear that it has started.
    /// </summary>
    public async Task<List<SessionEvent>> GetUpcomingOrRunningAsync(ulong guildId)
    {
        var now = DateTimeOffset.UtcNow;
        var events = await _db_context.SessionEvents
            .Include(e => e.Participants)
            .Where(e => e.GuildId == guildId && !e.IsCancelled)
            .ToListAsync();

        return events
            .Where(e => e.ScheduledAt + e.Length > now)
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
        int eventId, string title, DateTimeOffset scheduledAt, int maxPlayers,
        int durationMinutes, string description)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;

        // If the time moved, allow the reminder to fire again for the new slot, and forget
        // the attendance kept for the old one (late call, recap, who was seen in voice). The
        // card is drawn again as « planned » by the edit itself; RenderedPhase follows, or a
        // finished session moved forward would keep a frozen card (the lifecycle pass skips
        // anything already rendered Finished).
        if (evt.ScheduledAt != scheduledAt || evt.DurationMinutes != durationMinutes)
            evt.RenderedPhase = SessionPhase.Scheduled;
        if (evt.ScheduledAt != scheduledAt)
        {
            evt.ReminderSent = false;
            evt.MaybeNudgeSent = false;
            evt.LateCallSent = false;
            evt.RecapSent = false;
            await _db_context.Participants
                .Where(p => p.SessionEventId == eventId)
                .ExecuteUpdateAsync(u => u.SetProperty(p => p.FirstSeenInVoiceAt, (DateTimeOffset?)null));
        }

        evt.Title = title;
        evt.ScheduledAt = scheduledAt;
        evt.MaxPlayers = maxPlayers;
        evt.DurationMinutes = durationMinutes;
        evt.Description = description;
        await _db_context.SaveChangesAsync();
    }

    public async Task CancelEventAsync(int eventId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.IsCancelled = true;
        await _db_context.SaveChangesAsync();
    }

    // From 35 minutes before the start down to 5: wider than the loop's 5 minutes so none is
    // missed, and wide enough that a reminder missed while she was down still goes out late
    // rather than never. ReminderSent is claimed before the DMs, so none goes out twice.
    public static readonly TimeSpan ReminderLeadMin = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan ReminderLeadMax = TimeSpan.FromMinutes(35);

    public async Task<List<SessionEvent>> GetEventsNeedingReminderAsync()
    {
        var windowStart = DateTimeOffset.UtcNow + ReminderLeadMin;
        var windowEnd = DateTimeOffset.UtcNow + ReminderLeadMax;

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

    /// <summary>
    /// Sessions whose « peut-être » are due their nudge now (SessionSeats.NudgeDue: about an hour
    /// before, with room left). Flags in SQL, the window in memory (SQLite and DateTimeOffset).
    /// </summary>
    public async Task<List<SessionEvent>> GetEventsNeedingMaybeNudgeAsync(DateTimeOffset now)
    {
        var candidates = await _db_context.SessionEvents
            .Include(e => e.Participants)
            .Where(e => !e.MaybeNudgeSent && !e.IsCancelled)
            .ToListAsync();

        return candidates
            .Where(e => e.Participants.Any(p => p.Status == ParticipantStatus.Maybe))
            .Where(e => SessionSeats.NudgeDue(e.ScheduledAt, now, e.MaxPlayers,
                e.Participants.Count(p => p.Status == ParticipantStatus.Joined)))
            .ToList();
    }

    public async Task MarkMaybeNudgeSentAsync(int eventId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.MaybeNudgeSent = true;
        await _db_context.SaveChangesAsync();
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

    // Sessions whose attendance window is open at `now` (SessionAttendanceService): voice
    // categories only, not cancelled, from `before` ahead of the start to `after` past it. Dates compared in memory, as everywhere else here.
    public async Task<List<SessionEvent>> GetEventsForAttendanceAsync(
        DateTimeOffset now, TimeSpan before, TimeSpan after)
    {
        // The window is applied to a light projection first and only the sessions inside it are
        // loaded with their participants. A recapped session stays in: its voice channel status
        // is cleared at the end of its live window, which can come after the recap.
        var candidates = await _db_context.SessionEvents
            .Where(e => !e.IsCancelled
                && (e.Category == SessionCategory.Game || e.Category == SessionCategory.Movie))
            .Select(e => new { e.Id, e.ScheduledAt })
            .ToListAsync();

        var ids = candidates
            .Where(e => now >= e.ScheduledAt - before && now <= e.ScheduledAt + after)
            .Select(e => e.Id)
            .ToList();
        if (ids.Count == 0) return new List<SessionEvent>();

        return await _db_context.SessionEvents
            .Include(e => e.Participants)
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();
    }

    public async Task SetFirstSeenInVoiceAsync(int participantId, DateTimeOffset at)
    {
        var participant = await _db_context.Participants.FindAsync(participantId);
        if (participant is null || participant.FirstSeenInVoiceAt is not null) return;
        participant.FirstSeenInVoiceAt = at;
        await _db_context.SaveChangesAsync();
    }

    public async Task MarkLateCallSentAsync(int eventId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.LateCallSent = true;
        await _db_context.SaveChangesAsync();
    }

    public async Task MarkRecapSentAsync(int eventId)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.RecapSent = true;
        await _db_context.SaveChangesAsync();
    }

    // Any session, not cancelled, starting inside [from, to) in this guild: the weekend
    // poll stands down when the weekend is already organised.
    public async Task<bool> HasSessionBetweenAsync(ulong guildId, DateTimeOffset from, DateTimeOffset to)
    {
        var events = await _db_context.SessionEvents
            .Where(e => e.GuildId == guildId && !e.IsCancelled && e.RenderedPhase != SessionPhase.Finished)
            .ToListAsync();
        return events.Any(e => e.ScheduledAt >= from && e.ScheduledAt < to);
    }

    // ---- Weekly series (RecurringSession) --------------------------------

    public async Task<RecurringSession> CreateSeriesAsync(RecurringSession series)
    {
        _db_context.RecurringSessions.Add(series);
        await _db_context.SaveChangesAsync();
        return series;
    }

    public async Task<RecurringSession?> GetSeriesAsync(int seriesId) =>
        await _db_context.RecurringSessions.FindAsync(seriesId);

    /// <summary>Active series, of one guild or (0) of all.</summary>
    public async Task<List<RecurringSession>> GetActiveSeriesAsync(ulong guildId = 0) =>
        await _db_context.RecurringSessions
            .Where(s => s.Active && (guildId == 0 || s.GuildId == guildId))
            .ToListAsync();

    // A series whose first card failed to post: it never existed.
    public async Task DeleteSeriesAsync(int seriesId)
    {
        var series = await _db_context.RecurringSessions.FindAsync(seriesId);
        if (series is null) return;
        _db_context.RecurringSessions.Remove(series);
        await _db_context.SaveChangesAsync();
    }

    public async Task StopSeriesAsync(int seriesId)
    {
        var series = await _db_context.RecurringSessions.FindAsync(seriesId);
        if (series is null) return;
        series.Active = false;
        await _db_context.SaveChangesAsync();
    }

    /// <summary>
    /// The session for one occurrence of a series, created with the series' LastOccurrence moved
    /// to it in the same save: the claim and the row land together. Null when the series has
    /// stopped meanwhile, or the occurrence already has its session (the unique index).
    /// </summary>
    public async Task<SessionEvent?> CreateOccurrenceAsync(int seriesId, DateTimeOffset at)
    {
        var series = await _db_context.RecurringSessions.FindAsync(seriesId);
        if (series is null || !series.Active || series.LastOccurrence >= at) return null;

        series.LastOccurrence = at;
        var evt = new SessionEvent
        {
            GuildId = series.GuildId,
            ChannelId = series.ChannelId,
            OrganizerId = series.OrganizerId,
            Title = series.Title,
            Category = series.Category,
            ScheduledAt = at,
            MaxPlayers = series.MaxPlayers,
            DurationMinutes = series.DurationMinutes,
            Description = series.Description,
            VoiceChannelId = series.VoiceChannelId,
            RecurringSessionId = series.Id,
        };
        _db_context.SessionEvents.Add(evt);
        try
        {
            await _db_context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Posted already (the unique index on series and slot): nothing to do.
            _db_context.ChangeTracker.Clear();
            return null;
        }
        return evt;
    }

    /// <summary>
    /// Undoes <see cref="CreateOccurrenceAsync"/> after the card failed to post: the session goes,
    /// and the series steps back so the next pass tries again.
    /// </summary>
    public async Task UndoOccurrenceAsync(int seriesId, int eventId, DateTimeOffset previousLast)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is not null) _db_context.SessionEvents.Remove(evt);
        var series = await _db_context.RecurringSessions.FindAsync(seriesId);
        if (series is not null) series.LastOccurrence = previousLast;
        await _db_context.SaveChangesAsync();
    }

    /// <summary>
    /// Whether an active series of this guild has an occurrence inside [from, to): the weekend poll
    /// stands down for those too, though their session may not be posted yet (LeadDays).
    /// </summary>
    public async Task<bool> HasSeriesBetweenAsync(ulong guildId, DateTimeOffset from, DateTimeOffset to)
    {
        var series = await GetActiveSeriesAsync(guildId);
        return series.Any(s => Recurrence.NextOccurrence(s.DayOfWeek, s.MinuteOfDay, from.AddTicks(-1)) < to);
    }

    public async Task SetRenderedPhaseAsync(int eventId, SessionPhase phase)
    {
        var evt = await _db_context.SessionEvents.FindAsync(eventId);
        if (evt is null) return;
        evt.RenderedPhase = phase;
        await _db_context.SaveChangesAsync();
    }
}
