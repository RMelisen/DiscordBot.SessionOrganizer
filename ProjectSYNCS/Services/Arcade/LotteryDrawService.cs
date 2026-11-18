using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Draws the weekly lottery (/arcade lottery) once its Sunday 21:00 Paris has passed, and announces it
// in the bots channel: the winner, pinged alone, or her gloating when her own ticket won.
//
// Its own 1-minute tick, shared with no other loop: a draw announced long after the time /arcade
// lottery shows reads as broken. Built like GiveawayDrawService: nothing in memory (the entries and
// the draw rows are stored, so a restart resumes and a week due while the bot was down is drawn on
// the next pass), a scope per pass, a try per draw.
//
// A draw is a notice the command set up, so it ignores silence mode, like giveaway draws.
internal sealed class LotteryDrawService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private const ulong WinSalt = 0x5359_4E43_534C_5457;   // "SYNCSLTW"
    private const ulong GloatSalt = 0x5359_4E43_534C_5447; // "SYNCSLTG"

    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly GuildConfigService _config;
    private readonly PlynlingAnnouncer _announcer;
    private readonly ILogger<LotteryDrawService> _logger;

    public LotteryDrawService(IServiceProvider services, DiscordSocketClient client, GuildConfigService config,
        PlynlingAnnouncer announcer, ILogger<LotteryDrawService> logger)
    {
        _services = services;
        _client = client;
        _config = config;
        _announcer = announcer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LotteryDrawService started. Checking every {Interval} minute(s).", CheckInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            // The announcement needs the gateway; the week keeps until the next pass.
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            await SweepAsync();
        }
    }

    private async Task SweepAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var lottery = scope.ServiceProvider.GetRequiredService<LotteryService>();
        var now = DateTimeOffset.UtcNow;

        List<(ulong GuildId, int DrawDay)> due;
        try
        {
            due = await lottery.DueAsync(now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query the lottery draws due.");
            return;
        }

        foreach (var (guildId, drawDay) in due)
        {
            try
            {
                // Decides, pays and records in one save; null if it was already drawn.
                var result = await lottery.DrawAsync(guildId, drawDay, Random.Shared, now);
                if (result is null) continue;
                _logger.LogInformation("Lottery {DrawDay} drawn in guild {GuildId}: winner {Winner}, pot {Pot}, paid {Paid}.",
                    drawDay, guildId, result.Winner?.ToString() ?? "SYNCS", result.Pot, result.Paid);
                await AnnounceAsync(guildId, result);
            }
            catch (Exception ex)
            {
                // One broken week must not stop the others.
                _logger.LogError(ex, "Failed to draw the lottery {DrawDay} in guild {GuildId}.", drawDay, guildId);
            }
        }
    }

    // One step of the rotation per draw: the weeks since day zero, so consecutive Sundays walk the
    // pool's order one line at a time (a day number would jump seven).
    private static int WeekNumber(int drawDay) => DateOnly.FromDateTime(AppTime.FromDayKey(drawDay)).DayNumber / 7;

    private async Task AnnounceAsync(ulong guildId, LotteryResult result)
    {
        if (await BotsChannel.ResolveAsync(_client, _config, _announcer, guildId) is not { } channel)
        {
            _logger.LogWarning("Lottery {DrawDay} drawn in guild {GuildId}, but there is no channel to say so.", result.DrawDay, guildId);
            return;
        }

        if (result.Winner is { } winner)
        {
            var line = string.Format(DailyRotation.Pick(BotResponses.LotteryWinLines, WeekNumber(result.DrawDay), WinSalt),
                $"<@{winner}>", PebbleEconomy.Cailloux(result.Paid));
            // Meant to ping, the winner alone: never roles or everyone.
            await BotChat.PostWithTypingAsync(channel, line, _logger, "lottery draw",
                new AllowedMentions { UserIds = new List<ulong> { winner } });
            return;
        }

        var gloat = string.Format(DailyRotation.Pick(BotResponses.LotteryGloatLines, WeekNumber(result.DrawDay), GloatSalt),
            PebbleEconomy.Cailloux(result.Pot));
        await BotChat.PostWithTypingAsync(channel, gloat, _logger, "lottery draw", AllowedMentions.None);
    }
}
