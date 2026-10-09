using System.Text;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// She organised the session, so she calls the roll. Fed by VoiceXpService's one-minute sweep
// (who is in the server's voice channels right now), like VoiceSpectatorService: no loop and no
// gateway subscription of its own. Two lines per session, in the session card's channel:
//
// - the late call, at start + LateAfter, pinging the confirmed people not seen in voice yet —
//   only once at least one of them is there, or she would ping everyone for a session that
//   happened somewhere else;
// - the recap, once the people who came have scattered (or at the fallback): who came, who was
//   late, and how many confirmed people never showed — a count, never their names.
//
// Game and Movie sessions only: an Activity or Other can happen anywhere, where "not in voice"
// means nothing. Timing rules live in Helpers/SessionAttendance.
//
// What it learns is stored (Participant.FirstSeenInVoiceAt, SessionEvent.LateCallSent /
// RecapSent), so a restart during a session loses nothing but the scatter clock. A singleton
// that scopes per unit of work (root CLAUDE.md).
internal sealed class SessionAttendanceService
{
    // The sessions in their window are re-read this often, so an edit or a cancel is seen.
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _services;
    private readonly ResponsePicker _picker;
    private readonly BreakdownService _breakdown;
    private readonly ILogger<SessionAttendanceService> _logger;

    // Guards both. The sweep is sequential today; the lock keeps it safe if that changes, and
    // VoiceSeatService reads IsInLiveSession from the same sweep.
    private readonly object _gate = new();
    private readonly Dictionary<ulong, (DateTimeOffset At, List<SessionEvent> Sessions)> _cache = new();
    // Per session id: how long fewer than two of the people who came have been in voice.
    private readonly Dictionary<int, TimeSpan> _scattered = new();

    public SessionAttendanceService(
        IServiceProvider services, ResponsePicker picker, BreakdownService breakdown,
        ILogger<SessionAttendanceService> logger)
    {
        _services = services;
        _picker = picker;
        _breakdown = breakdown;
        _logger = logger;
    }

    /// <summary>
    /// Whether this person confirmed a session of this server that is under way right now (the
    /// seat's tie-break). Reads the cache only.
    /// </summary>
    public bool IsInLiveSession(ulong guildId, ulong userId, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (!_cache.TryGetValue(guildId, out var entry)) return false;
            return entry.Sessions.Any(s =>
                SessionAttendance.IsLive(now, s.ScheduledAt)
                && s.Participants.Any(p => p.UserId == userId && p.Status == ParticipantStatus.Joined));
        }
    }

    /// <summary>
    /// One sweep of one server: <paramref name="inVoice"/> holds every human connected to one of its
    /// voice channels (the AFK one aside), muted or not — someone muted is still there.
    /// </summary>
    public async Task ObserveAsync(SocketGuild guild, IReadOnlySet<ulong> inVoice, TimeSpan tick)
    {
        var now = DateTimeOffset.UtcNow;
        var sessions = await SessionsAsync(guild.Id, now);

        foreach (var session in sessions)
        {
            // Per session, like every sweep here: one bad row must not cost the others their lines.
            try
            {
                await ObserveSessionAsync(guild, session, inVoice, tick, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attendance failed for session {EventId}.", session.Id);
            }
        }
    }

    private async Task<List<SessionEvent>> SessionsAsync(ulong guildId, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(guildId, out var entry) && now - entry.At < RefreshEvery)
                return entry.Sessions;
        }

        List<SessionEvent> sessions;
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var events = scope.ServiceProvider.GetRequiredService<EventService>();
            sessions = (await events.GetEventsForAttendanceAsync(
                    now, SessionAttendance.WatchBefore, SessionAttendance.RecapFallback))
                .Where(e => e.GuildId == guildId)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read sessions for attendance in guild {GuildId}.", guildId);
            return new List<SessionEvent>();
        }

        lock (_gate)
        {
            _cache[guildId] = (now, sessions);
            var live = sessions.Select(s => s.Id).ToHashSet();
            foreach (var id in _scattered.Keys.Where(id => !live.Contains(id)).ToList())
                _scattered.Remove(id);
        }
        return sessions;
    }

    private async Task ObserveSessionAsync(
        SocketGuild guild, SessionEvent session, IReadOnlySet<ulong> inVoice, TimeSpan tick, DateTimeOffset now)
    {
        if (session.RecapSent) return;
        var start = session.ScheduledAt;

        // Joined and Maybe both count once they show up; only Joined people are owed a late call
        // and counted absent. « Peut-être », as she knows, means no.
        if (SessionAttendance.InSightingWindow(now, start))
        {
            foreach (var p in session.Participants)
            {
                if (p.Status == ParticipantStatus.Declined || p.FirstSeenInVoiceAt is not null) continue;
                if (!inVoice.Contains(p.UserId)) continue;

                await using var scope = _services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<EventService>().SetFirstSeenInVoiceAsync(p.Id, now);
                p.FirstSeenInVoiceAt = now;
            }
        }

        var confirmed = session.Participants.Where(p => p.Status == ParticipantStatus.Joined).ToList();
        var came = session.Participants.Where(p => p.FirstSeenInVoiceAt is not null).ToList();

        if (!session.LateCallSent)
        {
            if (SessionAttendance.LateCallExpired(now, start))
            {
                // Missed its slot (a restart, a deploy): skipped, never sent stale.
                await MarkAsync(session, late: true);
            }
            else if (SessionAttendance.LateCallDue(now, start))
            {
                var missing = confirmed.Where(p => p.FirstSeenInVoiceAt is null).ToList();
                if (confirmed.Count < SessionAttendance.MinConfirmed || missing.Count == 0)
                    await MarkAsync(session, late: true);
                // Nobody there yet: wait for the first arrival (until LateCallUntil) rather than
                // ping everyone for a session that may be happening somewhere else.
                else if (confirmed.Any(p => p.FirstSeenInVoiceAt is not null) && !_breakdown.IsActive(session.ChannelId))
                    await SendLateCallAsync(guild, session, missing);
            }
        }

        if (!SessionAttendance.RecapAllowed(now, start)) return;

        // The scatter clock only runs once someone came: before that, a late group still has
        // until the fallback to show up.
        bool scattered = false;
        if (came.Count > 0)
        {
            lock (_gate)
            {
                var together = came.Count(p => inVoice.Contains(p.UserId));
                _scattered.TryGetValue(session.Id, out var apart);
                apart = together >= 2 ? TimeSpan.Zero : apart + tick;
                _scattered[session.Id] = apart;
                scattered = apart >= SessionAttendance.ScatterToRecap;
            }
        }
        if (!scattered && !SessionAttendance.RecapForced(now, start)) return;

        // Nobody came, or too few were ever expected: nothing to say, and nothing to say later.
        if (came.Count == 0 || confirmed.Count < SessionAttendance.MinConfirmed)
        {
            await MarkAsync(session, late: false);
            return;
        }
        if (_breakdown.IsActive(session.ChannelId)) return;

        await SendRecapAsync(guild, session, confirmed, came);
    }

    private async Task SendLateCallAsync(SocketGuild guild, SessionEvent session, List<Participant> missing)
    {
        // Flag first: a crash between the two costs the line, never a second ping.
        await MarkAsync(session, late: true);
        if (guild.GetChannel(session.ChannelId) is not IMessageChannel channel) return;

        var mentions = Names(missing);
        var line = string.Format(_picker.Pick(BotResponses.SessionLateCallLines), mentions, Format.Sanitize(session.Title));
        // Meant to ping, so narrowed to exactly the people it names (root CLAUDE.md, mentions).
        var allowed = new AllowedMentions { UserIds = missing.Select(p => p.UserId).ToList() };
        await BotChat.PostWithTypingAsync(channel, line, _logger, "session late call", allowed);
        _logger.LogInformation("Late call for session {EventId}: {Count} missing.", session.Id, missing.Count);
    }

    private async Task SendRecapAsync(
        SocketGuild guild, SessionEvent session, List<Participant> confirmed, List<Participant> came)
    {
        await MarkAsync(session, late: false);
        if (guild.GetChannel(session.ChannelId) is not IMessageChannel channel) return;

        var start = session.ScheduledAt;
        var absent = confirmed.Count(p => p.FirstSeenInVoiceAt is null);
        var late = came.Where(p => SessionAttendance.IsLate(p.FirstSeenInVoiceAt!.Value, start)).ToList();
        var title = Format.Sanitize(session.Title);

        var intro = absent == 0
            ? string.Format(_picker.Pick(BotResponses.SessionRecapFullLines), title)
            : string.Format(_picker.Pick(BotResponses.SessionRecapLines), title, confirmed.Count - absent, confirmed.Count);

        var sb = new StringBuilder(intro);
        // Headers that gender nobody: « Présents » / « Absents » would.
        sb.Append($"\n> Au rendez-vous : {Names(came)}");
        if (late.Count > 0)
            sb.Append($"\n> En retard : {Names(late)}");
        if (absent > 0)
            sb.Append($"\n> Absences : {absent}");

        // The names show as pills without pinging anyone: a recap is not a summons.
        await BotChat.PostWithTypingAsync(channel, sb.ToString(), _logger, "session recap", AllowedMentions.None);
        _logger.LogInformation("Recap for session {EventId}: {Came} came, {Late} late, {Absent} absent.",
            session.Id, came.Count, late.Count, absent);
    }

    // A session takes up to 67 people; past MaxNames the list stops, so two lists and the intro
    // stay well inside the 2000-character message cap.
    private const int MaxNames = 20;

    private static string Names(List<Participant> people)
    {
        var names = string.Join(", ", people.Take(MaxNames).Select(p => $"<@{p.UserId}>"));
        return people.Count > MaxNames ? $"{names} et {people.Count - MaxNames} autre(s)" : names;
    }

    private async Task MarkAsync(SessionEvent session, bool late)
    {
        await using var scope = _services.CreateAsyncScope();
        var events = scope.ServiceProvider.GetRequiredService<EventService>();
        if (late)
        {
            await events.MarkLateCallSentAsync(session.Id);
            session.LateCallSent = true;
        }
        else
        {
            await events.MarkRecapSentAsync(session.Id);
            session.RecapSent = true;
        }
    }
}
