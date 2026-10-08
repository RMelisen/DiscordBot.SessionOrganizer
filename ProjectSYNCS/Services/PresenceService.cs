using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

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
        // From 1:00 to 7:00 she "sleeps": the idle moon and a sleepy line. She doesn't really
        // sleep — she does it to be like everyone (docs/syncs-voice.md, "Her nights").
        bool asleep = Ambient.IsSleepHours(DateTimeOffset.UtcNow);
        var line = _picker.Pick(asleep ? BotResponses.NightPresenceFillers : BotResponses.PresenceFillers);

        try
        {
            await _client.SetStatusAsync(asleep ? UserStatus.Idle : UserStatus.Online);
            // Custom status rather than SetGameAsync: it renders the line verbatim,
            // with no verb prepended, and Discord doesn't localise it per viewer. Note
            // the text travels in the wire model's State field rather than Name, which
            // is exactly why this needs the dedicated call — SetGameAsync with
            // ActivityType.CustomStatus would fill the wrong field and render nothing.
            await _client.SetCustomStatusAsync(line);
        }
        catch (Exception ex)
        {
            // Cosmetic — never worth disturbing anything else over.
            _logger.LogWarning(ex, "Failed to update the bot's presence.");
        }
    }
}
