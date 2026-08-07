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
    // How much XP one eligible minute of presence is worth.
    private const long VoiceXpPerMinute = 10;

    // Its own interval — explicitly not ReminderService's 5 minutes (load-bearing
    // there for the reminder window) or PresenceService's 5 minutes (an unrelated
    // rotation). Matches the per-minute payout exactly.
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

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
            foreach (var channel in guild.VoiceChannels)
            {
                var present = channel.ConnectedUsers;

                // Alone (or with only bots) — nobody here earns this tick. Guards the
                // classic solo-AFK-farm case.
                if (present.Count(u => !u.IsBot) < 2) continue;

                foreach (var member in present.Where(u => !u.IsBot))
                {
                    // Only self-mute+deafen together — the other classic farm case —
                    // is excluded. Server (moderator-applied) mute/deafen is
                    // deliberately not checked: someone silenced by a mod for an
                    // unrelated reason shouldn't also lose XP for it.
                    if (member.VoiceState is { IsSelfMuted: true, IsSelfDeafened: true }) continue;

                    // The channel id goes along so XpTracker can apply the same
                    // excluded-channel rule it applies to every other signal — the
                    // list lives there, not here.
                    await _xp.GrantVoiceXpAsync(guild.Id, channel.Id, member.Id, VoiceXpPerMinute);
                }
            }
        }
    }
}
