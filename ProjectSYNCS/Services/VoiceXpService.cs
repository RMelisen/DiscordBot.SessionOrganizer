using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

// Grants XP for time spent present in a voice channel — Phase 2 of the XP system,
// the one signal with no event to react to: there is no "voice message received",
// only "how long were they there." A periodic sweep rather than event-driven
// join/leave/mute-toggle tracking, deliberately: the payout unit is already "per
// minute," so a 1-minute sample is exactly as coarse as what it's rewarding, and a
// checkpoint's extra precision would only buy correctness at a grain finer than the
// reward ever uses. See CLAUDE.md for the fuller reasoning.
//
// Fully self-contained: Discord.Net already keeps SocketVoiceChannel.ConnectedUsers
// and each member's VoiceState live from the gateway in its own cache, the same way
// PresenceService's tick reads live state without subscribing to anything — so this
// needs no new BotService.cs subscription and touches nothing else in the fan-out.
internal sealed class VoiceXpService : BackgroundService
{
    // What a minute is worth is no longer a constant here: it tapers with the day's
    // total, and that policy lives in Helpers/VoiceXpCurve, applied by XpTracker. This
    // service's job is only who was present and for how long.

    // Its own interval — explicitly not ReminderService's 5 minutes (load-bearing
    // there for the reminder window) or PresenceService's 5 minutes (an unrelated
    // rotation). Matches the per-minute payout exactly.
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    // What one tick is worth on /leaderboard's voice view. Derived from CheckInterval
    // rather than written as 1, so retuning the interval can't silently make the
    // displayed hours disagree with the time actually spent.
    private static readonly long MinutesPerTick = (long)CheckInterval.TotalMinutes;

    private readonly DiscordSocketClient _client;
    private readonly XpTracker _xp;
    private readonly ILogger<VoiceXpService> _logger;

    public VoiceXpService(DiscordSocketClient client, XpTracker xp, ILogger<VoiceXpService> logger)
    {
        _client = client;
        _xp = xp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            // Sweeping while disconnected would just read stale/empty gateway cache.
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            await SweepAsync();
        }
    }

    private async Task SweepAsync()
    {
        foreach (var guild in _client.Guilds)
        {
            var afkChannelId = guild.AFKChannel?.Id;

            foreach (var channel in guild.VoiceChannels)
            {
                // The AFK channel is where Discord *puts* people for being idle. Two
                // people parked there would otherwise earn indefinitely, which is the
                // one farm the server hands out for free.
                if (channel.Id == afkChannelId) continue;

                // Only people who could actually be taking part count. Someone muted
                // is not company, so being the only unmuted person in a room full of
                // muted ones is being alone — and earns exactly what being alone
                // earns, which is nothing. That is what stops idle or alt accounts
                // being parked in a channel to unlock someone else's XP.
                var active = channel.ConnectedUsers.Where(IsActive).ToList();
                if (active.Count < 2) continue;

                foreach (var member in active)
                {
                    // The channel id goes along so XpTracker can apply the same
                    // excluded-channel rule it applies to every other signal — the
                    // list lives there, not here. One tick is one minute by
                    // construction (CheckInterval), which is what makes the minute
                    // count and the XP payout two views of the same event.
                    await _xp.GrantVoiceXpAsync(
                        guild.Id, channel.Id, member.Id, MinutesPerTick);
                }
            }
        }
    }

    /// <summary>
    /// Whether this member counts — both toward the "someone else is here" threshold
    /// and as someone earning. One predicate rather than two rules, deliberately: if
    /// muting stopped you earning but still let you unlock XP for the person beside
    /// you, parking muted accounts in a channel would be the whole exploit.
    /// </summary>
    /// <remarks>
    /// Self-muted <b>or</b> self-deafened is enough to be out — either one means you
    /// are not in the conversation, and requiring both let someone mute their mic and
    /// idle all day. Server (moderator-applied) mute/deafen is still deliberately not
    /// checked: someone silenced by a mod for an unrelated reason shouldn't also lose
    /// XP for it, and unlike self-muting it isn't something they can use to farm.
    /// </remarks>
    private static bool IsActive(SocketGuildUser member)
    {
        if (member.IsBot) return false;

        // No voice state means the gateway cache hasn't caught up; treat that as not
        // eligible rather than assuming the generous reading.
        if (member.VoiceState is not { } state) return false;

        return !state.IsSelfMuted && !state.IsSelfDeafened;
    }
}
