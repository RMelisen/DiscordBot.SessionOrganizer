using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Data;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>What /debug milestones shows for one channel.</summary>
public sealed record MilestoneView(long Count, bool CaughtUp, IReadOnlyList<ChannelMilestone> Reached, long Next);

/// <summary>
/// Every text channel's message count, history included, and the milestones it reaches
/// (Helpers/Admin/ChannelMilestones: 1 000, 10 000, 100 000, every 100 000). Discord gives no
/// total, so she reads each channel's history once, oldest first, then counts live.
/// </summary>
/// <remarks>
/// <para><b>Catching up.</b> Each channel is read forward from its saved <c>Cursor</c> (the last
/// message counted), or from its creation the first time, 100 messages a request, one channel at a
/// time. A milestone crossed there is recorded but never announced (the owner's rule: no past
/// milestones). Live messages that arrive while a channel catches up are held; when a page comes
/// back empty the channel is marked caught up under the lock and the held ones newer than the
/// cursor are counted, so the hand-over neither loses nor doubles one.</para>
/// <para><b>Live.</b> BotService feeds every message first thing (before the shortcut return: a
/// shortcut is a message in the channel too). A milestone reached live is written at once (rare)
/// and announced by replying to that message. Deletions she sees take the count back down, for a
/// message at or before the cursor (one already counted).</para>
/// <para><b>Saving.</b> Counts and cursors every 30 minutes, the changed channels only, and on a
/// clean stop: never a write per message. After a crash the catch-up reads on from the saved
/// cursor, so nothing counted is lost; deletions in that window are.</para>
/// <para>Singleton registered twice (AddSingleton + AddHostedService on the same instance) so
/// BotService and /debug reach the same state. Its own 1-minute tick, shared with no other loop.</para>
/// </remarks>
public sealed class ChannelMilestoneService : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SaveEvery = TimeSpan.FromMinutes(30);
    private const int Page = 100;

    // Live messages held while a channel catches up. Only the hand-over needs them (anything older
    // is read by the next page anyway), so the bound only guards memory.
    private const int MaxHeld = 500;

    private sealed class ChannelState(ulong guildId, ulong channelId)
    {
        public ulong GuildId { get; } = guildId;
        public ulong ChannelId { get; } = channelId;
        public long Count;
        public ulong Cursor;
        public bool CaughtUp;
        public bool Dirty;
        public readonly List<IMessage> Held = new();
        public readonly HashSet<long> Reached = new();
    }

    private readonly object _lock = new();
    private readonly Dictionary<ulong, ChannelState> _channels = new();
    private bool _loaded;

    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly GuildConfigService _config;
    private readonly SilenceService _silence;
    private readonly ChatHush _hush;
    private readonly BreakdownService _breakdown;
    private readonly ResponsePicker _picker;
    private readonly ILogger<ChannelMilestoneService> _logger;

    public ChannelMilestoneService(DiscordSocketClient client, IServiceProvider services, GuildConfigService config,
        SilenceService silence, ChatHush hush, BreakdownService breakdown, ResponsePicker picker,
        ILogger<ChannelMilestoneService> logger)
    {
        _client = client;
        _services = services;
        _config = config;
        _silence = silence;
        _hush = hush;
        _breakdown = breakdown;
        _picker = picker;
        _logger = logger;
    }

    // ---- The loop ------------------------------------------------------------------------------

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastSave = DateTimeOffset.UtcNow;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Tick, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                if (await LoadOnceAsync())
                {
                    await TrackChannelsAsync();
                    await CatchUpAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Channel milestones: the pass failed; next tick tries again.");
            }

            if (DateTimeOffset.UtcNow - lastSave >= SaveEvery)
            {
                await SaveAsync();
                lastSave = DateTimeOffset.UtcNow;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await SaveAsync();
    }

    // The saved counts and milestones, once. Until then no channel is tracked: a channel with a saved
    // row must not start over from its creation.
    private async Task<bool> LoadOnceAsync()
    {
        if (_loaded) return true;
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var counts = await db.ChannelMessageCounts.AsNoTracking().ToListAsync();
        var milestones = await db.ChannelMilestones.AsNoTracking().Select(m => new { m.GuildId, m.ChannelId, m.Milestone })
            .ToListAsync();
        lock (_lock)
        {
            foreach (var row in counts)
                _channels[row.ChannelId] = new ChannelState(row.GuildId, row.ChannelId) { Count = row.Count, Cursor = row.Cursor };
            foreach (var m in milestones)
            {
                if (!_channels.TryGetValue(m.ChannelId, out var state))
                    _channels[m.ChannelId] = state = new ChannelState(m.GuildId, m.ChannelId) { Cursor = m.ChannelId };
                state.Reached.Add(m.Milestone);
            }
            _loaded = true;
        }
        _logger.LogInformation("Channel milestones: {Count} channels loaded.", counts.Count);
        return true;
    }

    // Every guild's text channels she can read, minus the XP-excluded ones. A new one starts at its
    // creation, so its whole history is read.
    private async Task TrackChannelsAsync()
    {
        foreach (var guild in _client.Guilds)
        {
            var excluded = (await _config.GetAsync(guild.Id)).ExcludedChannels;
            var me = guild.CurrentUser;
            if (me is null) continue;
            foreach (var channel in guild.TextChannels)
            {
                if (channel is SocketThreadChannel or SocketVoiceChannel) continue;
                if (XpTracker.HardcodedExcludedChannels.Contains(channel.Id) || excluded.Contains(channel.Id)) continue;
                var perms = me.GetPermissions(channel);
                if (!perms.ViewChannel || !perms.ReadMessageHistory) continue;
                lock (_lock)
                    if (!_channels.ContainsKey(channel.Id))
                        _channels[channel.Id] = new ChannelState(guild.Id, channel.Id) { Cursor = channel.Id, Dirty = true };
            }
        }
    }

    private async Task CatchUpAsync(CancellationToken stoppingToken)
    {
        List<ChannelState> behind;
        lock (_lock) behind = _channels.Values.Where(s => !s.CaughtUp).ToList();
        foreach (var state in behind)
        {
            if (stoppingToken.IsCancellationRequested) return;
            try
            {
                await CatchUpChannelAsync(state, stoppingToken);
                // A first catch-up can take a long while: each channel's reading is saved as it
                // ends, not left to the 30-minute save, which waits for this pass.
                await SaveAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Lost access, a deleted channel, Discord failing: this one waits for the next tick.
                _logger.LogWarning(ex, "Channel milestones: could not read channel {ChannelId}.", state.ChannelId);
            }
        }
    }

    private async Task CatchUpChannelAsync(ChannelState state, CancellationToken stoppingToken)
    {
        if (await _client.Rest.GetChannelAsync(state.ChannelId) is not RestTextChannel channel)
        {
            lock (_lock) _channels.Remove(state.ChannelId);
            return;
        }

        var before = state.Count;
        while (!stoppingToken.IsCancellationRequested)
        {
            ulong from;
            lock (_lock) from = state.Cursor;
            var page = (await channel.GetMessagesAsync(from, Direction.After, Page).FlattenAsync())
                .OrderBy(m => m.Id).ToList();

            var reached = new List<ChannelMilestone>();
            lock (_lock)
            {
                if (page.Count == 0)
                {
                    // Caught up: from now on live messages count, and the ones held meanwhile go first.
                    state.CaughtUp = true;
                    foreach (var held in state.Held.OrderBy(m => m.Id)) Count(state, held, reached);
                    state.Held.Clear();
                }
                else
                    foreach (var message in page) Count(state, message, reached);
            }
            // Crossed while reading history: recorded, never announced.
            foreach (var milestone in reached) await RecordAsync(milestone);

            if (page.Count == 0)
            {
                if (state.Count != before)
                    _logger.LogInformation("Channel milestones: #{Channel} caught up at {Count} messages.", channel.Name, state.Count);
                return;
            }
        }
    }

    // One message counted, under the lock: only past the cursor, so nothing counts twice. Adds the
    // milestone it reaches, if any, to `reached`.
    private static void Count(ChannelState state, IMessage message, List<ChannelMilestone> reached)
    {
        if (message.Id <= state.Cursor) return;
        state.Count++;
        state.Cursor = message.Id;
        state.Dirty = true;
        if (ChannelMilestones.IsMilestone(state.Count) && state.Reached.Add(state.Count))
            reached.Add(new ChannelMilestone
            {
                GuildId = state.GuildId, ChannelId = state.ChannelId, Milestone = state.Count, MessageId = message.Id,
                AuthorId = message.Author.Id, ReachedAt = message.Timestamp,
            });
    }

    // ---- Live, from BotService ---------------------------------------------------------------------

    /// <summary>BotService's message fan-out, first thing: one more message in a tracked channel.</summary>
    public async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Channel is not SocketGuildChannel) return;
        var reached = new List<ChannelMilestone>(1);
        lock (_lock)
        {
            if (!_channels.TryGetValue(message.Channel.Id, out var state)) return;
            if (!state.CaughtUp)
            {
                if (state.Held.Count < MaxHeld) state.Held.Add(message);
                return;
            }
            Count(state, message, reached);
        }
        if (reached.Count == 0) return;

        var milestone = reached[0];
        try
        {
            milestone.Announced = await AnnounceAsync(message, milestone.Milestone);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Channel milestones: could not announce {Milestone}.", milestone.Milestone);
        }
        await RecordAsync(milestone);
    }

    /// <summary>BotService's <c>MessageDeleted</c>: one counted message fewer.</summary>
    public Task HandleDeletedAsync(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
    {
        Uncount(channel.Id, [message.Id]);
        return Task.CompletedTask;
    }

    /// <summary>BotService's <c>MessagesBulkDeleted</c>: a purge, uncounted the same way.</summary>
    public Task HandleBulkDeletedAsync(IReadOnlyCollection<Cacheable<IMessage, ulong>> messages,
        Cacheable<IMessageChannel, ulong> channel)
    {
        Uncount(channel.Id, messages.Select(m => m.Id).ToList());
        return Task.CompletedTask;
    }

    // Only what was counted (at or before the cursor) comes off; a message deleted while still held
    // is dropped from the held ones instead. Never below zero.
    private void Uncount(ulong channelId, IReadOnlyCollection<ulong> ids)
    {
        lock (_lock)
        {
            if (!_channels.TryGetValue(channelId, out var state)) return;
            var counted = ids.Count(id => id <= state.Cursor);
            state.Held.RemoveAll(m => ids.Contains(m.Id));
            if (counted == 0) return;
            state.Count = Math.Max(0, state.Count - counted);
            state.Dirty = true;
        }
    }

    // The reply to the message that reached it: the author is the one pinged, a bot never. Not in
    // silence mode, under a « chut », during a breakdown, in a channel excluded since, or on a
    // system message (nothing to reply to). True when it went out.
    private async Task<bool> AnnounceAsync(SocketMessage message, long milestone)
    {
        if (message is not IUserMessage reachedBy || message.Channel is not SocketGuildChannel channel) return false;
        if (_silence.IsSilent || _hush.IsHushed(channel.Id, DateTimeOffset.UtcNow) || _breakdown.IsActive(channel.Id))
            return false;
        if (XpTracker.HardcodedExcludedChannels.Contains(channel.Id)
            || (await _config.GetAsync(channel.Guild.Id)).ExcludedChannels.Contains(channel.Id)) return false;

        var author = message.Author;
        var isSelf = author.Id == _client.CurrentUser.Id;
        var pool = isSelf ? BotResponses.MilestoneSelfLines
            : author.IsBot ? BotResponses.MilestoneBotLines
            : BotResponses.MilestoneLines;
        var line = string.Format(_picker.Pick(pool), BotResponses.DisplayNameFor(author),
            ChannelMilestones.Format(milestone), MentionUtils.MentionChannel(channel.Id));
        var mentions = author.IsBot
            ? AllowedMentions.None
            : new AllowedMentions(AllowedMentionTypes.None) { UserIds = [author.Id], MentionRepliedUser = true };

        var sent = await BotChat.ReplyWithTypingAsync(reachedBy, line, _logger, "milestone", mentions);
        if (sent is not null)
            _logger.LogInformation("Channel milestones: announced {Milestone} in #{Channel}.", milestone, channel.Name);
        return sent is not null;
    }

    // ---- The database ------------------------------------------------------------------------------

    private async Task RecordAsync(ChannelMilestone milestone)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ChannelMilestones.Add(milestone);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Channel milestones: could not record {Milestone} in {ChannelId}.",
                milestone.Milestone, milestone.ChannelId);
        }
    }

    // The changed channels' counts and cursors, one row at a time: a failed row is marked changed
    // again for the next save, and the others are written anyway.
    private async Task SaveAsync()
    {
        List<(ulong GuildId, ulong ChannelId, long Count, ulong Cursor)> changed;
        lock (_lock)
        {
            changed = _channels.Values.Where(s => s.Dirty).Select(s => (s.GuildId, s.ChannelId, s.Count, s.Cursor)).ToList();
            foreach (var state in _channels.Values) state.Dirty = false;
        }
        if (changed.Count == 0) return;

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var (guildId, channelId, count, cursor) in changed)
        {
            try
            {
                var row = await db.ChannelMessageCounts.FirstOrDefaultAsync(r => r.GuildId == guildId && r.ChannelId == channelId);
                if (row is null)
                    db.ChannelMessageCounts.Add(row = new ChannelMessageCount { GuildId = guildId, ChannelId = channelId });
                row.Count = count;
                row.Cursor = cursor;
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                db.ChangeTracker.Clear();
                lock (_lock)
                    if (_channels.TryGetValue(channelId, out var state)) state.Dirty = true;
                _logger.LogWarning(ex, "Channel milestones: could not save channel {ChannelId}.", channelId);
            }
        }
    }

    // ---- /debug milestones ---------------------------------------------------------------------------

    /// <summary>This channel's count and milestones, or null when it isn't counted at all.</summary>
    public async Task<MilestoneView?> ViewAsync(ulong channelId)
    {
        long count;
        bool caughtUp;
        HashSet<long> reachedSet;
        lock (_lock)
        {
            if (!_channels.TryGetValue(channelId, out var state)) return null;
            (count, caughtUp, reachedSet) = (state.Count, state.CaughtUp, new HashSet<long>(state.Reached));
        }

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reached = await db.ChannelMilestones.AsNoTracking().Where(m => m.ChannelId == channelId)
            .OrderBy(m => m.Milestone).ToListAsync();
        return new MilestoneView(count, caughtUp, reached, ChannelMilestones.NextUnreached(count, reachedSet));
    }
}
