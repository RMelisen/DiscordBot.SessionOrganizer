using Discord.WebSocket;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

public readonly record struct HourKey(ulong GuildId, int Day, int Hour);
public readonly record struct ChannelKey(ulong GuildId, ulong ChannelId, int Day);
public readonly record struct CommandKey(ulong GuildId, int Day, string Command);
public readonly record struct EmoteKey(ulong GuildId, int Day, EmoteRef Emote);
public readonly record struct ReactionUsedKey(ulong GuildId, ulong UserId, int Day);
public readonly record struct AnswerKey(ulong GuildId, int Day, string Path, string Kind);

public sealed record HourTally(long Messages, long VoiceMinutes);
public sealed record ChannelTally(long Messages, long VoiceMinutes, int VoicePeak);
public sealed record CommandTally(long Uses, long Failures);
/// <summary>Written occurrences, and reactions added minus removed (so it can be negative).</summary>
public sealed record EmoteTallyDelta(long Written, long Reacted);
/// <summary>A message she found nothing specific to answer, kept for `/debug answers` (no author).</summary>
public sealed record UnansweredEntry(ulong GuildId, DateTimeOffset At, string Path, string Kind, string Text);

/// <summary>What one flush writes: everything counted since the last drain.</summary>
public sealed record ActivityBatch(
    IReadOnlyDictionary<HourKey, HourTally> Hours,
    IReadOnlyDictionary<ChannelKey, ChannelTally> Channels,
    IReadOnlyDictionary<CommandKey, CommandTally> Commands,
    IReadOnlyDictionary<EmoteKey, EmoteTallyDelta> Emotes,
    IReadOnlyDictionary<ReactionUsedKey, long> ReactionsUsed,
    IReadOnlyDictionary<AnswerKey, long> Answers,
    IReadOnlyList<UnansweredEntry> Unanswered)
{
    public bool IsEmpty => Hours.Count == 0 && Channels.Count == 0 && Commands.Count == 0
        && Emotes.Count == 0 && ReactionsUsed.Count == 0 && Answers.Count == 0 && Unanswered.Count == 0;

    public IReadOnlyList<ulong> GuildIds => Hours.Keys.Select(k => k.GuildId)
        .Concat(Channels.Keys.Select(k => k.GuildId))
        .Concat(Commands.Keys.Select(k => k.GuildId))
        .Concat(Emotes.Keys.Select(k => k.GuildId))
        .Concat(ReactionsUsed.Keys.Select(k => k.GuildId))
        .Concat(Answers.Keys.Select(k => k.GuildId))
        .Concat(Unanswered.Select(u => u.GuildId))
        .Distinct().ToList();

    public ActivityBatch For(ulong guildId) => new(
        Hours.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value),
        Channels.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value),
        Commands.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value),
        Emotes.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value),
        ReactionsUsed.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value),
        Answers.Where(x => x.Key.GuildId == guildId).ToDictionary(x => x.Key, x => x.Value),
        Unanswered.Where(u => u.GuildId == guildId).ToList());
}

/// <summary>
/// Everything counted per message or per reaction — messages, voice presence, commands, emotes,
/// reactions used, her chat answers — counted in memory and written every few minutes by
/// ActivityFlushService. A write per message or reaction is what fills the Pi's database with
/// SQLITE_BUSY; this is the one batched counter (root CLAUDE.md).
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
    private Dictionary<EmoteKey, EmoteTallyDelta> _emotes = new();
    private Dictionary<ReactionUsedKey, long> _reactionsUsed = new();
    private Dictionary<AnswerKey, long> _answers = new();
    private List<UnansweredEntry> _unanswered = new();

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

    /// <summary>Emotes written in one message, each with how many times it appeared (EmoteTracker).</summary>
    public void CountEmotesWritten(ulong guildId, IReadOnlyDictionary<EmoteRef, int> counts, DateTimeOffset at)
    {
        var day = ActivityKeys.DayHour(at).Day;
        lock (_lock)
            foreach (var (emote, n) in counts)
                AddEmote(new EmoteKey(guildId, day, emote), new EmoteTallyDelta(n, 0));
    }

    /// <summary>A reaction added (+1) or taken back (−1), counted under its emote (EmoteTracker).</summary>
    public void CountEmoteReacted(ulong guildId, EmoteRef emote, int delta, DateTimeOffset at)
    {
        var day = ActivityKeys.DayHour(at).Day;
        lock (_lock) AddEmote(new EmoteKey(guildId, day, emote), new EmoteTallyDelta(0, delta));
    }

    /// <summary>
    /// A reaction added (+1) or taken back (−1), counted under the person (XpTracker, which alone
    /// decides what counts: after the excluded-channel check).
    /// </summary>
    public void CountReactionUsed(ulong guildId, ulong userId, int delta, DateTimeOffset at)
    {
        var key = new ReactionUsedKey(guildId, userId, ActivityKeys.DayHour(at).Day);
        lock (_lock) AddReactionUsed(key, delta);
    }

    /// <summary>
    /// One chat answer of <paramref name="kind"/> on <paramref name="path"/> (AnswerKinds keys), for
    /// `/debug answers`. <paramref name="unanswered"/>, already cleaned, is what was said when she
    /// found nothing specific to answer.
    /// </summary>
    public void CountAnswer(ulong guildId, string path, string kind, string? unanswered, DateTimeOffset at)
    {
        var key = new AnswerKey(guildId, ActivityKeys.DayHour(at).Day, path, kind);
        lock (_lock)
        {
            AddAnswer(key, 1);
            if (!string.IsNullOrEmpty(unanswered)) _unanswered.Add(new UnansweredEntry(guildId, at, path, kind, unanswered));
        }
    }

    /// <summary>Takes everything counted so far and starts afresh.</summary>
    public ActivityBatch Drain()
    {
        lock (_lock)
        {
            var batch = new ActivityBatch(_hours, _channels, _commands, _emotes, _reactionsUsed, _answers, _unanswered);
            _hours = new();
            _channels = new();
            _commands = new();
            _emotes = new();
            _reactionsUsed = new();
            _answers = new();
            _unanswered = new();
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
            foreach (var (key, tally) in batch.Emotes) AddEmote(key, tally);
            foreach (var (key, delta) in batch.ReactionsUsed) AddReactionUsed(key, delta);
            foreach (var (key, n) in batch.Answers) AddAnswer(key, n);
            _unanswered.AddRange(batch.Unanswered);
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

    private void AddEmote(EmoteKey key, EmoteTallyDelta add) =>
        _emotes[key] = _emotes.TryGetValue(key, out var t)
            ? new EmoteTallyDelta(t.Written + add.Written, t.Reacted + add.Reacted)
            : add;

    private void AddReactionUsed(ReactionUsedKey key, long delta) =>
        _reactionsUsed[key] = _reactionsUsed.GetValueOrDefault(key) + delta;

    private void AddAnswer(AnswerKey key, long n) =>
        _answers[key] = _answers.GetValueOrDefault(key) + n;
}
