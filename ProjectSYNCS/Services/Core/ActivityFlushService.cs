using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Writes ActivityCounter's counts to ActivityHourStat, ChannelDailyStat and CommandDailyStat. Its own
// 5-minute loop (root CLAUDE.md: every loop keeps its own interval). One scope per guild, each in
// its own try: a guild whose write fails gets its counts put back (ActivityCounter.Restore) for the
// next pass, and the others are written anyway. StopAsync flushes once more, so a clean restart
// loses nothing.
public sealed class ActivityFlushService : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMinutes(5);

    private readonly ActivityCounter _counter;
    private readonly IServiceProvider _services;
    private readonly ILogger<ActivityFlushService> _logger;

    public ActivityFlushService(ActivityCounter counter, IServiceProvider services, ILogger<ActivityFlushService> logger)
    {
        _counter = counter;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(FlushInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            await FlushAsync();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FlushAsync();
    }

    public async Task FlushAsync()
    {
        var batch = _counter.Drain();
        if (batch.IsEmpty) return;
        foreach (var guildId in batch.GuildIds)
        {
            var part = batch.For(guildId);
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await WriteAsync(db, guildId, part);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _counter.Restore(part);
                _logger.LogWarning(ex, "Activity counts for guild {GuildId} not saved; kept for the next flush.", guildId);
            }
        }
    }

    // Find-or-create every row the batch touches, in three reads (one per table, by the batch's days).
    private static async Task WriteAsync(AppDbContext db, ulong guildId, ActivityBatch part)
    {
        var days = part.Hours.Keys.Select(k => k.Day)
            .Concat(part.Channels.Keys.Select(k => k.Day))
            .Concat(part.Commands.Keys.Select(k => k.Day))
            .Distinct().ToList();

        var hours = (await db.ActivityHourStats.Where(x => x.GuildId == guildId && days.Contains(x.Day)).ToListAsync())
            .ToDictionary(x => (x.Day, x.Hour));
        foreach (var (key, t) in part.Hours)
        {
            if (!hours.TryGetValue((key.Day, key.Hour), out var row))
            {
                row = new ActivityHourStat { GuildId = guildId, Day = key.Day, Hour = key.Hour };
                db.ActivityHourStats.Add(row);
                hours[(key.Day, key.Hour)] = row;
            }
            row.Messages += t.Messages;
            row.VoiceMinutes += t.VoiceMinutes;
        }

        var channels = (await db.ChannelDailyStats.Where(x => x.GuildId == guildId && days.Contains(x.Day)).ToListAsync())
            .ToDictionary(x => (x.ChannelId, x.Day));
        foreach (var (key, t) in part.Channels)
        {
            if (!channels.TryGetValue((key.ChannelId, key.Day), out var row))
            {
                row = new ChannelDailyStat { GuildId = guildId, ChannelId = key.ChannelId, Day = key.Day };
                db.ChannelDailyStats.Add(row);
                channels[(key.ChannelId, key.Day)] = row;
            }
            row.Messages += t.Messages;
            row.VoiceMinutes += t.VoiceMinutes;
            row.VoicePeak = Math.Max(row.VoicePeak, t.VoicePeak);
        }

        var commands = (await db.CommandDailyStats.Where(x => x.GuildId == guildId && days.Contains(x.Day)).ToListAsync())
            .ToDictionary(x => (x.Day, x.Command));
        foreach (var (key, t) in part.Commands)
        {
            if (!commands.TryGetValue((key.Day, key.Command), out var row))
            {
                row = new CommandDailyStat { GuildId = guildId, Day = key.Day, Command = key.Command };
                db.CommandDailyStats.Add(row);
                commands[(key.Day, key.Command)] = row;
            }
            row.Uses += t.Uses;
            row.Failures += t.Failures;
        }
    }
}
