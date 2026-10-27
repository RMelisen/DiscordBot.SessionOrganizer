using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Writes ActivityCounter's counts: activity (ActivityHourStat, ChannelDailyStat, CommandDailyStat), emotes,
// reactions used and her chat answers. Its own 5-minute loop (root CLAUDE.md: every loop keeps its
// own interval). One scope per guild, each in its own try: a guild whose write fails gets its counts
// put back (ActivityCounter.Restore) for the next pass, and the others are written anyway. StopAsync
// flushes once more, so a clean restart loses nothing.
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

        await WriteEmotesAsync(db, guildId, part.Emotes);
        await WriteReactionsUsedAsync(db, guildId, part.ReactionsUsed);
        await WriteAnswersAsync(db, guildId, part);
    }

    // EmoteStat / EmoteDailyStat (/emotestats): totals + buckets, written together. A reaction taken
    // back lands on the day it was taken back, never below zero (the drift isn't worth a row per
    // reaction); a net removal never creates a row. The key carries the emote's name, so a rename
    // within one batch is two keys on one row, and the last name wins.
    private static async Task WriteEmotesAsync(AppDbContext db, ulong guildId, IReadOnlyDictionary<EmoteKey, EmoteTallyDelta> emotes)
    {
        if (emotes.Count == 0) return;
        var ids = emotes.Keys.Select(k => k.Emote.Id).Distinct().ToList();
        var unicodes = emotes.Keys.Select(k => k.Emote.Unicode).Distinct().ToList();
        var days = emotes.Keys.Select(k => k.Day).Distinct().ToList();

        var totals = (await db.EmoteStats
                .Where(s => s.GuildId == guildId && ids.Contains(s.EmoteId) && unicodes.Contains(s.Unicode))
                .ToListAsync())
            .ToDictionary(s => (s.EmoteId, s.Unicode));
        var buckets = (await db.EmoteDailyStats
                .Where(b => b.GuildId == guildId && days.Contains(b.Day) && ids.Contains(b.EmoteId) && unicodes.Contains(b.Unicode))
                .ToListAsync())
            .ToDictionary(b => (b.EmoteId, b.Unicode, b.Day));

        foreach (var (key, t) in emotes)
        {
            var emote = key.Emote;
            bool adds = t.Written > 0 || t.Reacted > 0;

            if (!totals.TryGetValue((emote.Id, emote.Unicode), out var total) && adds)
            {
                total = new EmoteStat { GuildId = guildId, EmoteId = emote.Id, Unicode = emote.Unicode };
                db.EmoteStats.Add(total);
                totals[(emote.Id, emote.Unicode)] = total;
            }
            if (total is not null)
            {
                total.Name = emote.Name;
                total.IsAnimated = emote.IsAnimated;
                total.WrittenCount += t.Written;
                total.ReactedCount = Math.Max(0, total.ReactedCount + t.Reacted);
            }

            if (!buckets.TryGetValue((emote.Id, emote.Unicode, key.Day), out var bucket) && adds)
            {
                bucket = new EmoteDailyStat { GuildId = guildId, EmoteId = emote.Id, Unicode = emote.Unicode, Day = key.Day };
                db.EmoteDailyStats.Add(bucket);
                buckets[(emote.Id, emote.Unicode, key.Day)] = bucket;
            }
            if (bucket is not null)
            {
                bucket.WrittenCount += t.Written;
                bucket.ReactedCount = Math.Max(0, bucket.ReactedCount + t.Reacted);
            }
        }
    }

    // MemberXp.ReactionsUsed / MemberDailyStat.ReactionsUsed (/leaderboard's reaction view). A fact,
    // not a reward: clamped at zero, and a net removal never creates a row (someone with no row has
    // nothing to take back).
    private static async Task WriteReactionsUsedAsync(AppDbContext db, ulong guildId, IReadOnlyDictionary<ReactionUsedKey, long> reactions)
    {
        if (reactions.Count == 0) return;
        var users = reactions.Keys.Select(k => k.UserId).Distinct().ToList();
        var days = reactions.Keys.Select(k => k.Day).Distinct().ToList();

        var totals = (await db.MemberXps.Where(x => x.GuildId == guildId && users.Contains(x.UserId)).ToListAsync())
            .ToDictionary(x => x.UserId);
        var buckets = (await db.MemberDailyStats
                .Where(b => b.GuildId == guildId && days.Contains(b.Day) && users.Contains(b.UserId))
                .ToListAsync())
            .ToDictionary(b => (b.UserId, b.Day));

        foreach (var (key, delta) in reactions)
        {
            if (delta == 0) continue;
            if (!totals.TryGetValue(key.UserId, out var total))
            {
                if (delta < 0) continue;
                total = new MemberXp { GuildId = guildId, UserId = key.UserId };
                db.MemberXps.Add(total);
                totals[key.UserId] = total;
            }
            total.ReactionsUsed = Math.Max(0, total.ReactionsUsed + delta);

            if (!buckets.TryGetValue((key.UserId, key.Day), out var bucket))
            {
                if (delta < 0) continue;
                bucket = new MemberDailyStat { GuildId = guildId, UserId = key.UserId, Day = key.Day };
                db.MemberDailyStats.Add(bucket);
                buckets[(key.UserId, key.Day)] = bucket;
            }
            bucket.ReactionsUsed = Math.Max(0, bucket.ReactionsUsed + delta);
        }
    }

    // ChatAnswerStat / ChatAnswerDailyStat (`/debug answers`), and the unanswered examples, pruned
    // to their rolling window in the same pass.
    private static async Task WriteAnswersAsync(AppDbContext db, ulong guildId, ActivityBatch part)
    {
        if (part.Answers.Count == 0 && part.Unanswered.Count == 0) return;

        if (part.Unanswered.Count > 0)
        {
            foreach (var u in part.Unanswered)
                db.UnansweredExamples.Add(new UnansweredExample
                {
                    GuildId = guildId, Day = AppTime.DayKey(u.At), At = u.At, Path = u.Path, Kind = u.Kind, Text = u.Text,
                });
            // Rolling window: today plus the days before it. Day is an int, so this runs in SQL.
            var oldest = AppTime.KeyDaysAgo(UnansweredLog.KeptDays - 1);
            await db.UnansweredExamples.Where(x => x.Day < oldest).ExecuteDeleteAsync();
        }

        if (part.Answers.Count == 0) return;
        var days = part.Answers.Keys.Select(k => k.Day).Distinct().ToList();
        var totals = (await db.ChatAnswerStats.Where(x => x.GuildId == guildId).ToListAsync())
            .ToDictionary(x => (x.Path, x.Kind));
        var buckets = (await db.ChatAnswerDailyStats.Where(x => x.GuildId == guildId && days.Contains(x.Day)).ToListAsync())
            .ToDictionary(x => (x.Day, x.Path, x.Kind));

        foreach (var (key, n) in part.Answers)
        {
            if (!totals.TryGetValue((key.Path, key.Kind), out var total))
            {
                total = new ChatAnswerStat { GuildId = guildId, Path = key.Path, Kind = key.Kind };
                db.ChatAnswerStats.Add(total);
                totals[(key.Path, key.Kind)] = total;
            }
            total.Count += n;

            if (!buckets.TryGetValue((key.Day, key.Path, key.Kind), out var bucket))
            {
                bucket = new ChatAnswerDailyStat { GuildId = guildId, Day = key.Day, Path = key.Path, Kind = key.Kind };
                db.ChatAnswerDailyStats.Add(bucket);
                buckets[(key.Day, key.Path, key.Kind)] = bucket;
            }
            bucket.Count += n;
        }
    }
}
