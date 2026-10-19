using Discord.WebSocket;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

public readonly record struct HourKey(ulong GuildId, int Day, int Hour);
public readonly record struct ChannelKey(ulong GuildId, ulong ChannelId, int Day);
public readonly record struct CommandKey(ulong GuildId, int Day, string Command);

public sealed record HourTally(long Messages, long VoiceMinutes);
public sealed record ChannelTally(long Messages, long VoiceMinutes, int VoicePeak);
public sealed record CommandTally(long Uses, long Failures);

/// <summary>What one flush writes: everything counted since the last drain.</summary>
public sealed record ActivityBatch(
    IReadOnlyDictionary<HourKey, HourTally> Hours,
    IReadOnlyDictionary<ChannelKey, ChannelTally> Channels,
    IReadOnlyDictionary<CommandKey, CommandTally> Commands)
{
    public bool IsEmpty => Hours.Count == 0 && Channels.Count == 0 && Commands.Count == 0;

    public IReadOnlyList<ulong> GuildIds => Hours.Keys.Select(k => k.GuildId)
        .Concat(Channels.Keys.Select(k => k.GuildId))
        .Concat(Commands.Keys.Select(k => k.GuildId))
        .Distinct().ToList();

    public ActivityBatch For(ulong guildId) => new(
        Hours.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value),
        Channels.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value),
        Commands.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value));
}

/// <summary>
/// Messages, voice presence and commands, counted in memory for /report activity and commands, and
/// written every few minutes by ActivityFlushService. A write per message would be the bot's first,
/// on a Pi with no SQLITE_BUSY handling; this is the one batched counter (root CLAUDE.md).
/// </summary>
/// <remarks>
/// Singleton: it holds state. It owns its lock, and every method is synchronous and cheap, safe to
/// call from a gateway handler. What a crash loses is at most one flush interval (in-memory state
/// resets by design); a clean stop flushes.
/// </remarks>
public sealed class ActivityCounter
{
    private readonly object _lock = new();
    private Dictionary<HourKey, HourTally> _hours = new();
    private Dictionary<ChannelKey, ChannelTally> _channels = new();
    private Dictionary<CommandKey, CommandTally> _commands = new();

    /// <summary>
    /// A message in a guild, by a person: bots, webhooks, system messages and DMs are skipped. A
    /// thread counts under its parent channel. Excluded channels count: these are traffic stats, not
    /// rewards.
    /// </summary>
    public void CountMessage(SocketMessage raw)
    {
        if (raw is not SocketUserMessage message || message.Author.IsBot || message.Author.IsWebhook) return;
        if (message.Channel is not SocketGuildChannel channel) return;
        var channelId = message.Channel is SocketThreadChannel { ParentChannel: { } parent } ? parent.Id : channel.Id;
        CountMessage(channel.Guild.Id, channelId, message.Timestamp);
    }

    public void CountMessage(ulong guildId, ulong channelId, DateTimeOffset at)
    {
        var (day, hour) = ActivityKeys.DayHour(at);
        lock (_lock)
        {
            AddHour(new HourKey(guildId, day, hour), new HourTally(1, 0));
            AddChannel(new ChannelKey(guildId, channelId, day), new ChannelTally(1, 0, 0));
        }
    }

    /// <summary>One sweep of a voice room: <paramref name="humans"/> people present for <paramref name="minutes"/>.</summary>
    public void CountVoice(ulong guildId, ulong channelId, int humans, long minutes, DateTimeOffset at)
    {
        if (humans <= 0 || minutes <= 0) return;
        var (day, hour) = ActivityKeys.DayHour(at);
        lock (_lock)
        {
            AddHour(new HourKey(guildId, day, hour), new HourTally(0, humans * minutes));
            AddChannel(new ChannelKey(guildId, channelId, day), new ChannelTally(0, humans * minutes, humans));
        }
    }

    public void CountCommand(ulong guildId, string command, bool success, DateTimeOffset at)
    {
        var day = ActivityKeys.DayHour(at).Day;
        lock (_lock) AddCommand(new CommandKey(guildId, day, command), new CommandTally(1, success ? 0 : 1));
    }

    /// <summary>Takes everything counted so far and starts afresh.</summary>
    public ActivityBatch Drain()
    {
        lock (_lock)
        {
            var batch = new ActivityBatch(_hours, _channels, _commands);
            _hours = new();
            _channels = new();
            _commands = new();
            return batch;
        }
    }

    /// <summary>Puts a batch that could not be written back, to be written with the next one.</summary>
    public void Restore(ActivityBatch batch)
    {
        lock (_lock)
        {
            foreach (var (key, tally) in batch.Hours) AddHour(key, tally);
            foreach (var (key, tally) in batch.Channels) AddChannel(key, tally);
            foreach (var (key, tally) in batch.Commands) AddCommand(key, tally);
        }
    }

    private void AddHour(HourKey key, HourTally add) =>
        _hours[key] = _hours.TryGetValue(key, out var t)
            ? new HourTally(t.Messages + add.Messages, t.VoiceMinutes + add.VoiceMinutes)
            : add;

    private void AddChannel(ChannelKey key, ChannelTally add) =>
        _channels[key] = _channels.TryGetValue(key, out var t)
            ? new ChannelTally(t.Messages + add.Messages, t.VoiceMinutes + add.VoiceMinutes, Math.Max(t.VoicePeak, add.VoicePeak))
            : add;

    private void AddCommand(CommandKey key, CommandTally add) =>
        _commands[key] = _commands.TryGetValue(key, out var t)
            ? new CommandTally(t.Uses + add.Uses, t.Failures + add.Failures)
            : add;
}
