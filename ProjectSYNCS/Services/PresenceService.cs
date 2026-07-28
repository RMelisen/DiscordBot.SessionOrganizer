using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

// Rotates the bot's Discord presence — the status line under its name in the member
// list — through the filler pool in BotResponses. Purely cosmetic: it says nothing
// about the bot's actual state, it just keeps the member list from looking dead.
//
// Kept apart from ReminderService on purpose. That loop's 5-minute interval is tied
// to the reminder window it has to catch; this one is free to change cadence without
// anyone having to think about reminders.
internal sealed class PresenceService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<PresenceService> _logger;

    // Slow on purpose. Nobody sits watching the member list; the effect comes from
    // it saying something different whenever someone happens to look.
    private static readonly TimeSpan RotateInterval = TimeSpan.FromMinutes(5);

    // Bucket key for the picker's no-repeat history. Presence isn't attached to a
    // channel, and 0 is never a real snowflake, so it can't collide with one.
    private const ulong PresenceBucket = 0;

    public PresenceService(
        DiscordSocketClient client,
        ResponsePicker picker,
        ILogger<PresenceService> logger)
    {
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Discord drops the presence on every reconnect, so it has to be re-applied
        // on Ready rather than just once at startup. This also covers the cold start:
        // the loop below sleeps first, so without it the bot would show nothing for
        // the first few minutes after a restart.
        _client.Ready += RotateAsync;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(RotateInterval, stoppingToken);

            // Setting a presence while disconnected achieves nothing; Ready will fire
            // and set one as soon as the gateway is back.
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            await RotateAsync();
        }
    }

    private async Task RotateAsync()
    {
        var (type, text) = _picker.Pick(PresenceBucket, BotResponses.PresenceFillers);

        try
        {
            // A custom status is a different shape on the wire: its text travels in
            // State, not Name, so it needs the dedicated call. SetGameAsync would
            // send the line in the wrong field and render as an empty status.
            if (type == ActivityType.CustomStatus)
                await _client.SetCustomStatusAsync(text);
            else
                await _client.SetGameAsync(text, type: type);
        }
        catch (Exception ex)
        {
            // Cosmetic — never worth disturbing anything else over.
            _logger.LogWarning(ex, "Failed to update the bot's presence.");
        }
    }
}
