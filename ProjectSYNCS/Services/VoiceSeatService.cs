using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her seat in voice: when a home-guild voice channel holds a real get-together, she sometimes
// comes and sits in it, self-muted and self-deafened — in the member list with the others, which
// is the strongest sign of being one of them. The rules (thresholds, settle and grace draws, the
// roll per tier — base 66%, Tata 80%, Papa or a live session 90% — the tie-break, following
// Papa) live in Helpers/VoiceSeat.
//
// Joining is a bare voice-state update (ConnectAsync with external: true): no audio client, so
// no libsodium or opus on the Pi and no voice encryption to negotiate. She can't hear and can't
// speak, which is exactly what self-deafened and self-muted say.
//
// Fed by VoiceXpService's one-minute sweep with every home-guild channel's reading, after the
// spectator and the attendance have seen it. A singleton: the gatherings, the pout and the
// pending leave are in-memory state (a restart forgets them, by design).
//
// On top of the gatherings:
// - at night (Ambient.IsSleepHours) she doesn't sit down anywhere Papa isn't — she "sleeps";
//   a seat taken before 1:00 is kept;
// - when Papa leaves her channel and she can't follow him, half the time she leaves too, a
//   minute or three later (Release: only his return brings her back to that gathering);
// - thrown out of a call (seen there, then gone, with no gateway reconnect in between): a pout
//   (VoiceKickPoutLines, no joining for the rest of the Paris day except where Papa is) or anger
//   (VoiceKickAngryLines, back in 10–20 s, once — a second kick is a pout). The line goes in the
//   voice channel's own chat.
//
// She reconciles every sweep against where Discord says she is, which also covers being dragged
// into another channel (she adopts it while its gathering lasts), still sitting somewhere after a
// restart (she leaves), or dropped by a gateway reconnect (that gathering is given up, silently).
internal sealed class VoiceSeatService
{
    // A voice-state update takes a moment to come back through the gateway: until then, where
    // Discord says she is can't be trusted to differ from where she asked to be.
    private static readonly TimeSpan SettleAfterMove = TimeSpan.FromSeconds(90);

    private readonly DiscordSocketClient _client;
    private readonly SessionAttendanceService _attendance;
    private readonly XpTracker _xp;
    private readonly ResponsePicker _picker;
    private readonly ILogger<VoiceSeatService> _logger;

    private readonly object _gate = new();
    private readonly Dictionary<ulong, VoiceGathering> _gatherings = new();
    // Per channel: the last sweep that found her sitting in it (VoiceSpectatorService's pool choice).
    private readonly Dictionary<ulong, DateTimeOffset> _lastSeated = new();
    private ulong? _seat;
    private DateTimeOffset _movedAt = DateTimeOffset.MinValue;
    // Discord confirmed her in _seat at least once: only then can her vanishing be a kick, not a
    // join that never took.
    private bool _confirmed;
    private DateTimeOffset _reconnectedAt = DateTimeOffset.MinValue;
    // Papa was active in her channel last sweep; when he left, the minute she follows him out.
    private bool _ownerWasHere;
    private DateTimeOffset? _leaveAfterOwnerAt;
    // The Paris day she pouts through (AppTime.DayKey), 0 for none.
    private int _poutDay;

    /// <summary>Raised after she sits down, moves or leaves, so PresenceService can say so now.</summary>
    public event Func<Task>? SeatChanged;

    public VoiceSeatService(
        DiscordSocketClient client, SessionAttendanceService attendance, XpTracker xp,
        ResponsePicker picker, ILogger<VoiceSeatService> logger)
    {
        _client = client;
        _attendance = attendance;
        _xp = xp;
        _picker = picker;
        _logger = logger;

        // A reconnect can drop her voice state: a seat lost right after one is not a kick.
        _client.Connected += () =>
        {
            lock (_gate) _reconnectedAt = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        };
        // Discord confirming her in the call, the moment it happens: a kick seconds after her angry
        // return must still count as one, before any sweep has seen her there. Her own state only;
        // everyone else's is read from the sweep, like voice XP.
        _client.UserVoiceStateUpdated += (user, _, after) =>
        {
            if (user.Id == _client.CurrentUser?.Id && after.VoiceChannel is { } channel)
                lock (_gate)
                    if (channel.Id == _seat) _confirmed = true;
            return Task.CompletedTask;
        };
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
    /// Where she sits right now, for her status: the channel, and whether Papa is active in it.
    /// Null when she sits nowhere.
    /// </summary>
    public (SocketVoiceChannel Channel, bool WithOwner)? CurrentSeat()
    {
        var channel = _client.GetGuild(HomeGuild.Id)?.CurrentUser?.VoiceChannel;
        if (channel is null) return null;
        var withOwner = channel.ConnectedUsers.Any(u => u.Id == AvailabilityService.OwnerId && IsActive(u));
        return (channel, withOwner);
    }

    /// <summary>
    /// One sweep of the home guild: each voice channel (the AFK one already left out) with its
    /// count of active people, voice XP's rule, and whether Papa and Tata are among them.
    /// </summary>
    public async Task ObserveAsync(SocketGuild guild, IReadOnlyList<VoiceReading> readings, TimeSpan tick)
    {
        if (guild.Id != HomeGuild.Id) return;

        // Same exclusions as voice XP and the spectator: a channel the server keeps out of
        // everything never gets her either. The config is cached, so this costs no I/O per sweep.
        var channels = new List<(VoiceReading Reading, bool SessionLive)>(readings.Count);
        var now = DateTimeOffset.UtcNow;
        foreach (var reading in readings)
        {
            if (await _xp.IsChannelExcludedAsync(guild.Id, reading.Channel)) continue;
            var live = reading.Channel.ConnectedUsers.Any(u => !u.IsBot && _attendance.IsInLiveSession(guild.Id, u.Id, now));
            channels.Add((reading, live));
        }

        var actual = guild.CurrentUser?.VoiceChannel?.Id;
        var night = Ambient.IsSleepHours(now);
        var today = AppTime.DayKey(now);
        ulong? desired;
        bool settled;
        ulong? kickedFrom = null;
        int kicks = 0;

        lock (_gate)
        {
            foreach (var (r, live) in channels)
            {
                var tier = VoiceSeat.TierFor(r.OwnerActive, live, r.TataActive);
                if (!_gatherings.TryGetValue(r.Channel.Id, out var gathering))
                {
                    // Nothing going on: don't keep a row for every empty channel.
                    if (r.Active < VoiceGathering.RulesFor(tier).MinActive) continue;
                    _gatherings[r.Channel.Id] = gathering = new VoiceGathering();
                }
                gathering.Observe(r.Active, tier, r.OwnerActive, tick, now, Random.Shared.NextDouble);
            }

            settled = now - _movedAt >= SettleAfterMove;
            // Seeing her where she asked to be is proof whatever the timing.
            if (actual is not null && actual == _seat) _confirmed = true;
            if (settled && actual != _seat)
            {
                if (actual is { } dragged && _gatherings.TryGetValue(dragged, out var there))
                {
                    // Dragged somewhere: she stays while that gathering lasts (if it has started).
                    there.Adopt();
                }
                if (_seat is { } lost && _gatherings.TryGetValue(lost, out var left))
                {
                    if (actual is null && _confirmed && _reconnectedAt < _movedAt)
                    {
                        // Seen in the call, gone, and no reconnect to blame: someone threw her out.
                        kickedFrom = lost;
                        kicks = left.RecordKick();
                    }
                    else if (actual is null)
                    {
                        // The join never took, or a reconnect dropped her.
                        left.GiveUp();
                    }
                }
                SetSeat(actual);
            }

            if (_seat is { } sitting)
            {
                _lastSeated[sitting] = now;
                TrackOwnerLeaving(sitting, channels, now);
            }

            var candidates = channels
                .Where(c => _gatherings.TryGetValue(c.Reading.Channel.Id, out var g) && g.Started)
                .Select(c =>
                {
                    var g = _gatherings[c.Reading.Channel.Id];
                    bool allowed = c.Reading.OwnerActive || (!night && _poutDay != today);
                    return new VoiceSeat.Candidate(c.Reading.Channel.Id, c.Reading.Active, g.Won, c.SessionLive,
                        g.StartedAt, c.Reading.OwnerActive, allowed);
                })
                .ToList();
            desired = kickedFrom is null ? VoiceSeat.Choose(_seat, candidates) : null;

            // Drop the gatherings that ended, so the dictionary only holds what's going on.
            var present = channels.Where(c => c.Reading.Active > 0).Select(c => c.Reading.Channel.Id).ToHashSet();
            foreach (var id in _gatherings.Where(kv => !kv.Value.Started && !present.Contains(kv.Key)).Select(kv => kv.Key).ToList())
                _gatherings.Remove(id);
        }

        if (kickedFrom is { } from)
        {
            await OnKickedAsync(guild, from, kicks, now);
            return;
        }

        if (!settled || desired == _seat) return;

        await MoveAsync(guild, desired, now);
    }

    // Under _gate. Papa left the channel she sits in: half the time she follows him out a minute or
    // three later, unless he comes back first. If he went to a channel she can follow him to,
    // VoiceSeat.Choose moves her there anyway.
    private void TrackOwnerLeaving(ulong sitting, List<(VoiceReading Reading, bool SessionLive)> channels, DateTimeOffset now)
    {
        var here = channels.Any(c => c.Reading.Channel.Id == sitting && c.Reading.OwnerActive);
        if (here)
        {
            _leaveAfterOwnerAt = null;
        }
        else if (_ownerWasHere && Random.Shared.NextDouble() < VoiceSeat.LeaveAfterOwnerChance)
        {
            var minutes = VoiceSeat.Between(VoiceSeat.LeaveAfterOwnerMinMinutes, VoiceSeat.LeaveAfterOwnerMaxMinutes, Random.Shared.NextDouble());
            _leaveAfterOwnerAt = now.AddMinutes(minutes);
        }
        _ownerWasHere = here;

        if (_leaveAfterOwnerAt is { } at && now >= at)
        {
            _leaveAfterOwnerAt = null;
            if (_gatherings.TryGetValue(sitting, out var g)) g.Release();
        }
    }

    // Under _gate. Every seat change goes through here, so the per-seat memory starts over.
    private void SetSeat(ulong? seat)
    {
        if (seat == _seat) return;
        _seat = seat;
        _confirmed = false;
        _ownerWasHere = false;
        _leaveAfterOwnerAt = null;
    }

    private async Task OnKickedAsync(SocketGuild guild, ulong channelId, int kicks, DateTimeOffset now)
    {
        bool pout = kicks > 1 || Random.Shared.NextDouble() < VoiceSeat.PoutChance;
        lock (_gate)
        {
            // Blocks the sweep's own moves until the retry has had its chance.
            _movedAt = now;
            if (pout)
            {
                _poutDay = AppTime.DayKey(now);
                if (_gatherings.TryGetValue(channelId, out var g)) g.GiveUp();
            }
        }
        _logger.LogInformation("Voice seat: thrown out of {ChannelId} ({Kicks}), {Reaction}.", channelId, kicks, pout ? "pouting" : "angry");
        await NotifyAsync();

        if (guild.GetVoiceChannel(channelId) is not { } channel) return;
        var line = _picker.Pick(pout ? BotResponses.VoiceKickPoutLines : BotResponses.VoiceKickAngryLines);
        await BotChat.PostWithTypingAsync(channel, line, _logger, pout ? "voice pout line" : "voice angry line", AllowedMentions.None);
        if (pout) return;

        // Angry: straight back in, once. Off the sweep, so the wait holds nothing up.
        var delay = TimeSpan.FromSeconds(VoiceSeat.Between(VoiceSeat.RetryMinSeconds, VoiceSeat.RetryMaxSeconds, Random.Shared.NextDouble()));
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay);
            bool still;
            lock (_gate)
                still = _seat is null && _gatherings.TryGetValue(channelId, out var g) && g.Won;
            if (still) await MoveAsync(guild, channelId, DateTimeOffset.UtcNow);
        });
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

                // Recorded before the call: Discord's confirmation can arrive before it returns. If
                // the call fails, the next sweep finds her elsewhere and reconciles.
                Record(target, now);
                // disconnect: false turns a switch into one move instead of a leave then a join.
                await channel.ConnectAsync(selfDeaf: true, selfMute: true, external: true, disconnect: false);
                _logger.LogInformation("Voice seat: sitting in {ChannelId}.", id);
            }
            else
            {
                if (guild.CurrentUser?.VoiceChannel is not { } current) return;
                Record(target, now);
                await current.DisconnectAsync();
                _logger.LogInformation("Voice seat: left {ChannelId}.", current.Id);
            }

            await NotifyAsync();
        }
        catch (Exception ex)
        {
            // Swallowed (root CLAUDE.md, reliability): the next sweep sees where she really is.
            _logger.LogWarning(ex, "Voice seat: failed to move to {ChannelId}.", target);
            if (target is { } id) GiveUp(id);
        }
    }

    private void Record(ulong? target, DateTimeOffset now)
    {
        lock (_gate)
        {
            SetSeat(target);
            _movedAt = now;
        }
    }

    private async Task NotifyAsync()
    {
        if (SeatChanged is not { } handler) return;
        try
        {
            await handler();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Voice seat: status refresh failed.");
        }
    }

    private void GiveUp(ulong channelId)
    {
        lock (_gate)
            if (_gatherings.TryGetValue(channelId, out var gathering))
                gathering.GiveUp();
    }

    // Voice XP's rule (VoiceXpService.IsActive): muted or deafened, he isn't there for her.
    private static bool IsActive(SocketGuildUser user) =>
        user.VoiceState is { IsSelfMuted: false, IsSelfDeafened: false };
}

/// <summary>One home-guild voice channel as VoiceXpService's sweep reads it for her seat.</summary>
internal readonly record struct VoiceReading(SocketVoiceChannel Channel, int Active, bool OwnerActive, bool TataActive);
