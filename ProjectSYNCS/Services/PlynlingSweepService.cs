using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Once an hour: settles every living Plynling (deaths land at the instant they happened,
// self-freezes thaw on schedule), announces deaths nobody has announced yet, and sends
// the single warning DM about three hours before a death.
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
        List<int> ids;
        try
        {
            await using var scope = _services.CreateAsyncScope();
            ids = await scope.ServiceProvider.GetRequiredService<PlynlingService>().GetSweepIdsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Plynlings for the sweep.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var id in ids)
        {
            // Per item: one broken Plynling must not stop the others, and an exception
            // escaping ExecuteAsync would stop the whole host. Each in its own scope, so its own
            // AppDbContext: nothing it reads is served stale to a later Plynling (a care click or
            // a pick made meanwhile), and a refused save dies with its scope instead of being
            // retried by the next Plynling's.
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var plynlings = scope.ServiceProvider.GetRequiredService<PlynlingService>();
                if (await plynlings.GetForSweepAsync(id) is not { } plynling) continue;   // abandoned meanwhile

                PlynlingLife.Settle(plynling, now);
                await plynlings.FlushMomentsAsync(plynling);        // fell sick / recovered, if a morning did it
                await plynlings.ProgressAsync(plynling, now);      // time's badges and stage moments
                var told = await plynlings.TickEventsAsync(plynling, now);   // cancels, decides alone, pulses — saves

                if (plynling.DiedAt is not null && !plynling.DeathAnnounced)
                {
                    // Marked and saved *before* posting: at most one attempt. A failed post
                    // is logged, never retried every hour into the game channel.
                    plynling.DeathAnnounced = true;
                    await plynlings.JournalDeathAsync(plynling);
                    await plynlings.SaveAsync();
                    await _announcer.AnnounceDeathAsync(plynling, now);
                }
                else if (PlynlingLife.IsSick(plynling) && !plynling.SickNotified)
                {
                    // Flag saved before the DM, like DeathAnnounced: one attempt, never a repeat.
                    // An onset is always at 05:00, so this never lands in the night.
                    plynling.SickNotified = true;
                    await plynlings.SaveAsync();
                    await _announcer.WarnSickAsync(plynling);
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

                // Told after every save above; a failed post is logged by the announcer, never retried.
                await _announcer.TellAsync(plynlings, told, plynling.GuildId, now);

                // Growing up, falling sick, recovering, a friend's death: their events, created after
                // this Plynling's saves, each on its own (FlushOnActionsAsync swallows and logs). Then
                // whatever the mascot decided at once, here or in a read earlier in this pass.
                await plynlings.FlushOnActionsAsync(now);
                await _announcer.TellAsync(plynlings, plynlings.TakeUntold(), plynling.GuildId, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sweep Plynling {PlynlingId}.", id);
            }
        }
    }
}
