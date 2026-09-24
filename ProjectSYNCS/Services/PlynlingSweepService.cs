using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Once an hour: settles every living Plynling (deaths land at the instant they happened,
// self-freezes thaw on schedule), announces deaths nobody has announced yet, and sends
// the single warning DM about six hours before a death.
//
// Its own interval, like every sweep here — an hour is precise enough for a 4-day clock,
// and the other loops' intervals are load-bearing for other things. It is a safety net,
// not the source of truth: every read already settles, so a command can find a death
// first; the sweep then announces it.
public sealed class PlynlingSweepService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly PlynlingAnnouncer _announcer;
    private readonly ILogger<PlynlingSweepService> _logger;

    public PlynlingSweepService(
        DiscordSocketClient client,
        IServiceProvider services,
        PlynlingAnnouncer announcer,
        ILogger<PlynlingSweepService> logger)
    {
        _client = client;
        _services = services;
        _announcer = announcer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            // Announcing into a stale gateway cache would fail every send.
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            await SweepAsync();
        }
    }

    private async Task SweepAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var plynlings = scope.ServiceProvider.GetRequiredService<PlynlingService>();

        List<Plynling> batch;
        try
        {
            batch = await plynlings.GetSweepBatchAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Plynlings for the sweep.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var plynling in batch)
        {
            // Per item: one broken Plynling must not stop the others, and an exception
            // escaping ExecuteAsync would stop the whole host.
            try
            {
                PlynlingLife.Settle(plynling, now);

                if (plynling.DiedAt is not null && !plynling.DeathAnnounced)
                {
                    // Marked and saved *before* posting: at most one attempt. A failed post
                    // is logged, never retried every hour into the game channel.
                    plynling.DeathAnnounced = true;
                    await plynlings.SaveAsync();
                    await _announcer.AnnounceDeathAsync(plynling, now);
                }
                else if (PlynlingLife.ShouldWarn(plynling, now))
                {
                    plynling.WarningSent = true;
                    await plynlings.SaveAsync();
                    await _announcer.WarnOwnerAsync(plynling);
                }
                else
                {
                    await plynlings.SaveAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sweep Plynling {PlynlingId}.", plynling.Id);
            }
        }
    }
}
