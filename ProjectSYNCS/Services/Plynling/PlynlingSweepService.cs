using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Once an hour: settles every living Plynling (deaths land at the instant they happened,
// self-freezes thaw on schedule), announces deaths nobody has announced yet, and sends
// the single warning DM about three hours before a death; last, it brings each guild's Plynling
// role in line (PlynlingRoleService).
//
// Its own interval, like every sweep here — an hour is precise enough for a 4-day clock,
// and the other loops' intervals are load-bearing for other things. It is a safety net,
// not the source of truth: every read already settles, so a command can find a death
// first; the sweep then announces it.
//
// The first pass comes a few minutes after start, not an hour: an update or a restart then takes
// effect at once (due events decided, traits owed, deaths announced). A pass is idempotent — a pulse
// is spent once a day, only what is due gets decided — so an extra one changes nothing.
public sealed class PlynlingSweepService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan FirstDelay = TimeSpan.FromMinutes(2);

    // One pass at a time: /debug sweep and the loop must never walk the same Plynlings together.
    private readonly SemaphoreSlim _passGate = new(1, 1);

    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly PlynlingAnnouncer _announcer;
    private readonly PlynlingRoleService _roles;
    private readonly ILogger<PlynlingSweepService> _logger;
    private readonly SilenceService _silence;

    public PlynlingSweepService(
        DiscordSocketClient client,
        IServiceProvider services,
        PlynlingAnnouncer announcer,
        PlynlingRoleService roles,
        ILogger<PlynlingSweepService> logger, SilenceService silence)
    {
        _silence = silence;
        _roles = roles;
        _client = client;
        _services = services;
        _announcer = announcer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = FirstDelay;
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(delay, stoppingToken);
            delay = CheckInterval;

            // Announcing into a stale gateway cache would fail every send.
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            await RunPassAsync(wait: true);
        }
    }

    // One pass now: how many Plynlings it looked at, or null when a pass was already running and
    // `wait` is false (/debug sweep says so rather than queueing a second one behind it).
    public async Task<int?> RunPassAsync(bool wait)
    {
        if (wait) await _passGate.WaitAsync();
        else if (!await _passGate.WaitAsync(0)) return null;
        try
        {
            return await SweepAsync();
        }
        finally
        {
            _passGate.Release();
        }
    }

    private async Task<int> SweepAsync()
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
            return 0;
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
                // Everything stored first, as one unit (run again if a click changed the row meanwhile),
                // with the notice's flag: at most one attempt at each, a failed post logged, never
                // retried every hour.
                if (await plynlings.SweepOneAsync(id, now) is not { } item) continue;   // abandoned meanwhile
                var plynling = item.Plynling;
                bool silent = _silence.IsSilent;

                switch (item.Notice)
                {
                    // Silence mode: the death is journaled and marked, never announced later.
                    case SweepNotice.Death when !silent:
                        await _announcer.AnnounceDeathAsync(plynling, now);
                        break;
                    case SweepNotice.Sick:
                        await _announcer.WarnSickAsync(plynling);
                        break;
                    case SweepNotice.Warning:
                        await _announcer.WarnOwnerAsync(plynling);
                        break;
                }

                // Told after the save; a failed post is logged by the announcer, never retried.
                // Silence mode drops the stories, as a failed post would.
                if (!silent) await _announcer.TellAsync(plynlings, item.Told, plynling.GuildId, now);

                // Growing up, falling sick, recovering, a friend's death: their events, created after
                // this Plynling's saves, each on its own (FlushOnActionsAsync swallows and logs). Then
                // whatever the mascot decided at once, here or in a read earlier in this pass.
                await plynlings.FlushOnActionsAsync(now);
                var untold = plynlings.TakeUntold();
                if (!silent) await _announcer.TellAsync(plynlings, untold, plynling.GuildId, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sweep Plynling {PlynlingId}.", id);
            }
        }

        // Last, after every death above is stamped: a Plynling that died this pass loses its owner
        // the Plynling role in the same pass. Per guild, and SyncGuildAsync never throws — nothing
        // may escape the hosted loop.
        foreach (var guild in _client.Guilds)
        {
            try
            {
                await _roles.SyncGuildAsync(guild);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plynling role sync failed for guild {GuildId}.", guild.Id);
            }
        }
        return ids.Count;
    }
}
