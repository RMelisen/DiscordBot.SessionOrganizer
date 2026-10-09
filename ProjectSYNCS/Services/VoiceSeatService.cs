using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her seat in voice: when a home-guild voice channel holds a real get-together, she sometimes
// comes and sits in it, self-muted and self-deafened — in the member list with the others, which
// is the strongest sign of being one of them. The rules (threshold, the 30% roll once per
// gathering, the tie-break between two channels, the grace before leaving) live in
// Helpers/VoiceSeat.
//
// Joining is a bare voice-state update (ConnectAsync with external: true): no audio client, so
// no libsodium or opus on the Pi and no voice encryption to negotiate. She can't hear and can't
// speak, which is exactly what self-deafened and self-muted say.
//
// Fed by VoiceXpService's one-minute sweep with every home-guild channel's reading, after the
// spectator and the attendance have seen it. A singleton: the gatherings are in-memory state (a
// restart forgets them, by design, and a gathering re-rolls after it).
//
// She reconciles every sweep against where Discord says she is, which also covers what nobody
// tells her: kicked out of the call (she stays out of that gathering), dragged into another
// channel (she adopts it while its gathering lasts), still sitting somewhere after a restart
// (she leaves), or dropped by a gateway reconnect (that gathering is given up, like a kick).
internal sealed class VoiceSeatService
{
    // A voice-state update takes a moment to come back through the gateway: until then, where
    // Discord says she is can't be trusted to differ from where she asked to be.
    private static readonly TimeSpan SettleAfterMove = TimeSpan.FromSeconds(90);

    private readonly SessionAttendanceService _attendance;
    private readonly XpTracker _xp;
    private readonly ILogger<VoiceSeatService> _logger;

    private readonly object _gate = new();
    private readonly Dictionary<ulong, VoiceGathering> _gatherings = new();
    // Per channel: the last sweep that found her sitting in it (VoiceSpectatorService's pool choice).
    private readonly Dictionary<ulong, DateTimeOffset> _lastSeated = new();
    private ulong? _seat;
    private DateTimeOffset _movedAt = DateTimeOffset.MinValue;

    public VoiceSeatService(
        SessionAttendanceService attendance, XpTracker xp,
        ILogger<VoiceSeatService> logger)
    {
        _attendance = attendance;
        _xp = xp;
        _logger = logger;
    }

    /// <summary>
    /// Whether she sat in this channel at any point since <paramref name="since"/> — the spectator's
    /// line changes when she was in the call rather than watching the names from outside.
    /// </summary>
    public bool SatInSince(ulong channelId, DateTimeOffset since)
    {
        lock (_gate)
            return _lastSeated.TryGetValue(channelId, out var at) && at >= since;
    }

    /// <summary>
    /// One sweep of the home guild: each voice channel (the AFK one already left out) with its
    /// count of active people, voice XP's rule.
    /// </summary>
    public async Task ObserveAsync(SocketGuild guild, IReadOnlyList<(SocketVoiceChannel Channel, int Active)> readings, TimeSpan tick)
    {
        if (guild.Id != HomeGuild.Id) return;

        // Same exclusions as voice XP and the spectator: a channel the server keeps out of
        // everything never gets her either. The config is cached, so this costs no I/O per sweep.
        var channels = new List<(SocketVoiceChannel Channel, int Active)>(readings.Count);
        foreach (var reading in readings)
            if (!await _xp.IsChannelExcludedAsync(guild.Id, reading.Channel))
                channels.Add(reading);

        var now = DateTimeOffset.UtcNow;
        var actual = guild.CurrentUser?.VoiceChannel?.Id;
        ulong? desired;
        bool settled;

        lock (_gate)
        {
            foreach (var (channel, active) in channels)
            {
                if (!_gatherings.TryGetValue(channel.Id, out var gathering))
                {
                    // Nothing going on: don't keep a row for every empty channel.
                    if (active < VoiceGathering.MinActive) continue;
                    _gatherings[channel.Id] = gathering = new VoiceGathering();
                }
                gathering.Observe(active, tick, now, Random.Shared.NextDouble);
            }

            settled = now - _movedAt >= SettleAfterMove;
            if (settled && actual != _seat)
            {
                if (actual is { } dragged && _gatherings.TryGetValue(dragged, out var there))
                {
                    // Dragged somewhere: she stays while that gathering lasts (if it has started).
                    there.Adopt();
                }
                if (_seat is { } lost && _gatherings.TryGetValue(lost, out var left))
                {
                    // Kicked out of the call, the join refused, or a reconnect dropped her.
                    left.GiveUp();
                }
                _seat = actual;
            }

            if (_seat is { } sitting)
                _lastSeated[sitting] = now;

            var candidates = channels
                .Where(c => _gatherings.TryGetValue(c.Channel.Id, out var g) && g.Started)
                .Select(c =>
                {
                    var g = _gatherings[c.Channel.Id];
                    var live = c.Channel.ConnectedUsers.Any(u => !u.IsBot && _attendance.IsInLiveSession(guild.Id, u.Id, now));
                    return new VoiceSeat.Candidate(c.Channel.Id, c.Active, g.Won, live, g.StartedAt);
                })
                .ToList();
            desired = VoiceSeat.Choose(_seat, candidates);

            // Drop the gatherings that ended, so the dictionary only holds what's going on.
            var present = channels.Where(c => c.Active > 0).Select(c => c.Channel.Id).ToHashSet();
            foreach (var id in _gatherings.Where(kv => !kv.Value.Started && !present.Contains(kv.Key)).Select(kv => kv.Key).ToList())
                _gatherings.Remove(id);
        }

        if (!settled || desired == _seat) return;

        await MoveAsync(guild, desired, now);
    }

    private async Task MoveAsync(SocketGuild guild, ulong? target, DateTimeOffset now)
    {
        try
        {
            if (target is { } id)
            {
                if (guild.GetVoiceChannel(id) is not { } channel) return;
                var perms = guild.CurrentUser.GetPermissions(channel);
                if (!perms.ViewChannel || !perms.Connect)
                {
                    GiveUp(id);
                    return;
                }

                // disconnect: false turns a switch into one move instead of a leave then a join.
                await channel.ConnectAsync(selfDeaf: true, selfMute: true, external: true, disconnect: false);
                _logger.LogInformation("Voice seat: sitting in {ChannelId}.", id);
            }
            else
            {
                if (guild.CurrentUser?.VoiceChannel is not { } current) return;
                await current.DisconnectAsync();
                _logger.LogInformation("Voice seat: left {ChannelId}.", current.Id);
            }

            lock (_gate)
            {
                _seat = target;
                _movedAt = now;
            }
        }
        catch (Exception ex)
        {
            // Swallowed (root CLAUDE.md, reliability): the next sweep sees where she really is.
            _logger.LogWarning(ex, "Voice seat: failed to move to {ChannelId}.", target);
            if (target is { } id) GiveUp(id);
        }
    }

    private void GiveUp(ulong channelId)
    {
        lock (_gate)
            if (_gatherings.TryGetValue(channelId, out var gathering))
                gathering.GiveUp();
    }
}
