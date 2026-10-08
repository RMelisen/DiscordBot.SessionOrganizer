using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her life in the main channel when nobody is talking to her: the 3 a.m. line (and a
// scolding for whoever answers it), a word into a long daytime silence, a typing
// indicator that never becomes a message, and a line when she comes back from a restart.
// Cosmetic, like PresenceService (which owns the night status), and rare on purpose: the
// point is that she seems to be there, not that she talks. When and how often lives in
// Helpers/Ambient.
//
// Registered as a singleton and as the hosted service, like MorningGreetingService, so
// BotService feeds this one instance the messages it uses to know how long it's been quiet.
internal sealed class AmbientService : BackgroundService
{
    // Its own interval, shared with no other loop (CLAUDE.md). Fine-grained enough for a
    // random minute past three and a six-hour silence.
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(10);

    // The channels that must all be quiet before she speaks into the silence: the main
    // channel and the server's other everyday ones. Server-specific, listed in CLAUDE.md's
    // "Hardcoded ids". A thread counts for its parent channel.
    private static readonly HashSet<ulong> IdleChannelIds = new()
    {
        MorningGreetingService.ChannelId, // Général
        878306977887957042,               // Média
        1025683260157722704,              // Galerie
        878305034432045080,               // Gaming
        1483863597041062109,              // Musique
        1500799769914773574,              // Nourriture
    };

    // How far back to look in the main channel on the first tick after a start (last human
    // message, tonight's line), and in each other channel for its last human message.
    private const int HistoryDepth = 50;
    private const int OtherHistoryDepth = 10;

    // How long to wait for the channel after the first Ready before giving up the wake line.
    private static readonly TimeSpan WakeChannelWait = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ChannelPollInterval = TimeSpan.FromSeconds(10);

    // Every pool here but the scolding is spent at most once a day, so it goes through
    // DailyRotation (a restart would wipe ResponsePicker's memory long before it helped).
    // One salt each.
    private const ulong NightSalt = 0x5359_4E43_4E49_4748;      // "SYNCNIGH"
    private const ulong IdleSalt = 0x5359_4E43_4944_4C45;       // "SYNCIDLE"
    private const ulong EditSalt = 0x5359_4E43_4544_4954;       // "SYNCEDIT"
    private const ulong SeenSalt = 0x5359_4E43_5345_454E;       // "SYNCSEEN"
    private const ulong WakeSalt = 0x5359_4E43_5741_4B45;       // "SYNCWAKE"
    private const ulong WakeUpdateSalt = 0x5359_4E43_5550_4454; // "SYNCUPDT"

    private static readonly HashSet<string> NightLineSet = new(BotResponses.NightLines);

    private readonly DiscordSocketClient _client;
    private readonly BreakdownService _breakdown;
    private readonly ResponsePicker _picker;
    private readonly ILogger<AmbientService> _logger;
    private readonly string _statePath;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    // Guards everything below: the loop and the gateway both read and claim it.
    private readonly object _gate = new();

    // The last human message in the main channel (night quiet, ghost typing, the late
    // "seen" reaction), and in any of IdleChannelIds (the idle silence). In memory; the
    // first tick after a start seeds both from the channels' history.
    private DateTimeOffset? _lastHumanAt;
    private ulong? _lastHumanMessageId;
    private DateTimeOffset? _lastActivityAt;
    private bool _seeded;

    // Idle: the silence already rolled for (its start), and the day she last spoke into one.
    private DateTimeOffset? _idleRolledFor;
    private int _idleDay;

    // Tonight's 3 a.m. decision, drawn once per night.
    private int _nightDay;
    private int? _nightMinute;
    private bool _nightDone;

    // When tonight's line went out, and who has already been sent to bed for answering it.
    private DateTimeOffset? _nightLineAt;
    private readonly HashSet<ulong> _scolded = new();

    // A self-correcting line waiting for its edit; completed when someone speaks after it.
    private TaskCompletionSource? _pendingEdit;

    private int _ghostDay;
    private int _woken;

    public AmbientService(
        DiscordSocketClient client,
        BreakdownService breakdown,
        ResponsePicker picker,
        IConfiguration config,
        ILogger<AmbientService> logger)
    {
        _client = client;
        _breakdown = breakdown;
        _picker = picker;
        _logger = logger;

        // Next to the database, so it lives under /data in production and survives updates.
        var dbPath = Path.GetFullPath(config["Database:Path"] ?? "ProjectSYNCS.db");
        _statePath = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "ambient-state.json");
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Ready += OnReadyAsync;
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Ready -= OnReadyAsync;
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TickInterval, stoppingToken);

            var channel = ResolveChannel();
            if (channel is null) continue;

            // Each step in its own try: an exception escaping a hosted loop stops the bot.
            try { await SeedAsync(channel); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ambient: could not read the channels' history."); }

            var now = DateTimeOffset.UtcNow;
            try { await TryNightLineAsync(channel, now); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ambient: night line failed."); }

            try { await TryIdleAsync(channel, now); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ambient: idle turn failed."); }
        }
    }

    /// <summary>
    /// Called by BotService for every message. Keeps the quiet clocks, catches her out on a
    /// pending self-correction, sends to bed whoever answers the 3 a.m. line, and now and
    /// then meets a message that breaks a silence with a typing indicator and nothing else.
    /// Returns true when she answered the message herself (the scolding), so ChatterService
    /// doesn't answer it a second time.
    /// </summary>
    public async Task<bool> HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message || message.Author.IsBot) return false;

        var now = DateTimeOffset.UtcNow;
        var channelId = message.Channel.Id;
        var parentId = (message.Channel as SocketThreadChannel)?.ParentChannel?.Id;
        if (IdleChannelIds.Contains(channelId) || parentId is { } p && IdleChannelIds.Contains(p))
            lock (_gate) _lastActivityAt = now;

        if (channelId != MorningGreetingService.ChannelId) return false;

        var today = AppTime.DayKey(now);
        // Aimed at her, ChatterService answers it with real typing; a ghost would be lost under it.
        bool aimedAtHer = message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id)
                          || message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id;
        // A verdict is BotFeedbackTracker's to answer, even at night.
        bool verdict = MessageCues.ReadFeedback(message.Content ?? string.Empty) != FeedbackKind.None;

        bool ghost = false, scold = false;
        TaskCompletionSource? caught;
        lock (_gate)
        {
            var previous = _lastHumanAt;
            _lastHumanAt = now;
            _lastHumanMessageId = message.Id;

            caught = _pendingEdit;
            _pendingEdit = null;

            // Up after her 3 a.m. line, before 5:30: once a person, a night.
            if (_nightLineAt is { } lineAt && AppTime.DayKey(lineAt) == today
                && Ambient.IsBeforeScoldEnd(now) && !verdict
                && _scolded.Add(message.Author.Id))
            {
                scold = true;
            }
            else if (!aimedAtHer
                     && previous is { } before && now - before >= Ambient.GhostTypingQuiet
                     && _ghostDay != today
                     && Random.Shared.NextDouble() < Ambient.GhostTypingChance)
            {
                _ghostDay = today;
                ghost = true;
            }
        }

        // Someone spoke after her self-correcting line: she edits it now, caught out.
        caught?.TrySetResult();

        if (_breakdown.IsActive(channelId)) return false;

        if (scold)
        {
            var line = string.Format(_picker.Pick(BotResponses.NightScoldLines),
                BotResponses.DisplayNameFor(message.Author));
            await BotChat.ReplyWithTypingAsync(message, line, _logger, "night scolding");
            _logger.LogInformation("Ambient: sent {UserId} to bed.", message.Author.Id);
            return true;
        }

        if (!ghost) return false;

        try
        {
            // Shows « SYNCS est en train d'écrire… » for about ten seconds. Nothing follows.
            await message.Channel.TriggerTypingAsync();
            _logger.LogInformation("Ambient: ghost typing in the main channel.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: ghost typing failed.");
        }
        return false;
    }

    // Once per process: the clocks start from the channels' history, not from nowhere.
    private async Task SeedAsync(IMessageChannel main)
    {
        lock (_gate)
            if (_seeded) return;

        var now = DateTimeOffset.UtcNow;
        var recent = (await main.GetMessagesAsync(HistoryDepth).FlattenAsync()).ToList();
        var last = recent.Where(m => !m.Author.IsBot).MaxBy(m => m.Timestamp);

        // Tonight's line, if a restart came between it and 5:30: the scolding still holds.
        var tonight = recent
            .Where(m => m.Author.Id == _client.CurrentUser.Id
                        && AppTime.DayKey(m.Timestamp) == AppTime.DayKey(now)
                        && NightLineSet.Contains(m.Content))
            .MaxBy(m => m.Timestamp);

        var latest = last?.Timestamp;
        foreach (var id in IdleChannelIds.Where(id => id != main.Id))
        {
            // A forum or a channel she can't read is skipped; its threads still count live.
            if (_client.GetChannel(id) is not IMessageChannel other) continue;
            try
            {
                var lastThere = (await other.GetMessagesAsync(OtherHistoryDepth).FlattenAsync())
                    .Where(m => !m.Author.IsBot).MaxBy(m => m.Timestamp);
                if (lastThere is not null && (latest is null || lastThere.Timestamp > latest))
                    latest = lastThere.Timestamp;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ambient: could not read channel {ChannelId}'s history.", id);
            }
        }

        lock (_gate)
        {
            _seeded = true;
            // A message seen live since the start is newer than anything in the history.
            // No human in the history: count from the start, so a restart is never followed
            // straight away by an idle line.
            if (_lastHumanAt is null)
            {
                _lastHumanAt = last?.Timestamp ?? _startedAt;
                _lastHumanMessageId = last?.Id;
            }
            _lastActivityAt ??= latest ?? _startedAt;

            if (tonight is not null && Ambient.IsBeforeScoldEnd(now))
            {
                _nightLineAt = tonight.Timestamp;
                _nightDay = AppTime.DayKey(now);
                _nightDone = true;
            }
        }
    }

    private async Task TryNightLineAsync(IMessageChannel channel, DateTimeOffset now)
    {
        if (!Ambient.IsNightLineHour(now)) return;

        var today = AppTime.DayKey(now);
        lock (_gate)
        {
            if (_nightDay != today)
            {
                _nightDay = today;
                _nightMinute = Ambient.NightMinute(Random.Shared);
                _nightDone = false;
                _logger.LogInformation("Ambient: night decision for {Day}: {Minute}.", today,
                    _nightMinute is { } m ? $"3:{m:00}" : "quiet");
            }

            if (_nightDone || _nightMinute is not { } minute || AppTime.ToZoned(now).Minute < minute) return;
            // Not into a conversation: the line is for an empty channel, found in the morning.
            if (_lastHumanAt is not { } last || now - last < Ambient.NightQuiet) return;
            _nightDone = true;
        }

        if (_breakdown.IsActive(channel.Id)) return;
        // A restart inside the hour draws a new decision; this keeps her to one line a night.
        if (await SaidNightLineTodayAsync(channel, today)) return;

        var line = DailyRotation.Pick(BotResponses.NightLines, AppTime.DayNumber(now), NightSalt);
        var sent = await BotChat.PostWithTypingAsync(channel, line, _logger, "night line", AllowedMentions.None);
        if (sent is null) return;

        lock (_gate)
        {
            _nightLineAt = sent.Timestamp;
            _scolded.Clear();
        }
        _logger.LogInformation("Ambient: night line posted.");
    }

    private async Task<bool> SaidNightLineTodayAsync(IMessageChannel channel, int today)
    {
        try
        {
            var recent = await channel.GetMessagesAsync(HistoryDepth).FlattenAsync();
            return recent.Any(m => m.Author.Id == _client.CurrentUser.Id
                                   && AppTime.DayKey(m.Timestamp) == today
                                   && NightLineSet.Contains(m.Content));
        }
        catch (Exception ex)
        {
            // Better a rare second line than none.
            _logger.LogWarning(ex, "Ambient: could not check for tonight's line.");
            return false;
        }
    }

    private async Task TryIdleAsync(IMessageChannel channel, DateTimeOffset now)
    {
        if (!Ambient.IsIdleHours(now)) return;

        var today = AppTime.DayKey(now);
        ulong? target;
        lock (_gate)
        {
            if (_idleDay == today) return;
            // Every everyday channel quiet together, not just this one.
            if (_lastActivityAt is not { } silenceStart || now - silenceStart < Ambient.IdleQuiet) return;
            // One roll per silence: a lost roll keeps her quiet until someone speaks again.
            if (_idleRolledFor == silenceStart) return;
            _idleRolledFor = silenceStart;
            if (Random.Shared.NextDouble() >= Ambient.IdleChance) return;
            _idleDay = today;
            target = _lastHumanMessageId;
        }

        if (_breakdown.IsActive(channel.Id)) return;

        var day = AppTime.DayNumber(now);
        var roll = Random.Shared.NextDouble();

        if (roll < Ambient.SeenChance && target is { } id)
        {
            await ReactSeenAsync(channel, id, day);
            return;
        }

        if (Random.Shared.NextDouble() < Ambient.EditChance)
        {
            var pair = DailyRotation.Pick(BotResponses.IdleEditLines, day, EditSalt);
            var message = await BotChat.PostWithTypingAsync(channel, pair.Before, _logger, "idle line", AllowedMentions.None);
            if (message is not null)
                _ = Task.Run(() => CorrectLaterAsync(message, pair.After));
            return;
        }

        var line = DailyRotation.Pick(BotResponses.IdleLines, day, IdleSalt);
        await BotChat.PostWithTypingAsync(channel, line, _logger, "idle line", AllowedMentions.None);
        _logger.LogInformation("Ambient: idle line posted.");
    }

    // A reaction on the last thing anyone said, hours later, as if she only just read it.
    private async Task ReactSeenAsync(IMessageChannel channel, ulong messageId, int day)
    {
        var markup = DailyRotation.Pick(BotResponses.SeenReactions, day, SeenSalt);
        var emote = EmoteMarkup.Parse(markup);
        if (emote is null)
        {
            _logger.LogWarning("Ambient: seen reaction failed to parse: {Markup}", markup);
            return;
        }

        try
        {
            if (await channel.GetMessageAsync(messageId) is not IUserMessage message) return;
            await message.AddReactionAsync(emote);
            _logger.LogInformation("Ambient: late seen reaction added.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: late seen reaction failed.");
        }
    }

    // Her mid-line self-correction, done with an edit: five minutes after the line, or two
    // seconds after someone speaks after it — she got caught. Runs off the tick loop. A
    // restart before the edit leaves the first version standing.
    private async Task CorrectLaterAsync(IUserMessage message, string after)
    {
        try
        {
            var caught = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _pendingEdit = caught;

            var first = await Task.WhenAny(Task.Delay(Ambient.EditAfter), caught.Task);
            lock (_gate)
                if (_pendingEdit == caught) _pendingEdit = null;

            if (first == caught.Task)
                await Task.Delay(Ambient.CaughtEditDelay);

            await message.ModifyAsync(p => p.Content = after);
            _logger.LogInformation("Ambient: self-correcting idle line edited ({How}).",
                first == caught.Task ? "caught" : "after five minutes");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: could not edit the idle line.");
        }
    }

    // Ready fires on every reconnect; only the first one of the process is a wake-up.
    private Task OnReadyAsync()
    {
        if (Interlocked.Exchange(ref _woken, 1) == 0)
            _ = Task.Run(WakeAsync);
        return Task.CompletedTask;
    }

    private async Task WakeAsync()
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var today = AppTime.DayKey(now);
            var state = LoadState();
            bool updated = state.LastVersion is not null && state.LastVersion != AppInfo.Version;

            // The version she runs is recorded even when she stays quiet, so the next
            // restart compares against this one.
            SaveState(state with { LastVersion = AppInfo.Version });

            if (!Ambient.IsWakeHours(now) || state.LastWakeDay == today) return;

            var channel = await WaitForChannelAsync();
            if (channel is null || _breakdown.IsActive(channel.Id)) return;

            var day = AppTime.DayNumber(now);
            var line = updated
                ? string.Format(DailyRotation.Pick(BotResponses.WakeUpdateLines, day, WakeUpdateSalt), AppInfo.Version)
                : DailyRotation.Pick(BotResponses.WakeLines, day, WakeSalt);

            var sent = await BotChat.PostWithTypingAsync(channel, line, _logger, "wake line", AllowedMentions.None);
            if (sent is not null)
                SaveState(new AmbientState(AppInfo.Version, today));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: wake line failed.");
        }
    }

    private async Task<IMessageChannel?> WaitForChannelAsync()
    {
        var deadline = DateTimeOffset.UtcNow + WakeChannelWait;
        IMessageChannel? channel;
        while ((channel = ResolveChannel()) is null)
        {
            // In the dev guild the channel never appears; this just runs out.
            if (DateTimeOffset.UtcNow >= deadline) return null;
            await Task.Delay(ChannelPollInterval);
        }
        return channel;
    }

    private IMessageChannel? ResolveChannel() =>
        _client.ConnectionState == ConnectionState.Connected
            ? _client.GetChannel(MorningGreetingService.ChannelId) as IMessageChannel
            : null;

    // What a restart must not forget: the version she last ran (for the update line) and
    // the day she last spoke on waking (one wake line a day, however many deploys).
    private sealed record AmbientState(string? LastVersion, int LastWakeDay);

    private AmbientState LoadState()
    {
        try
        {
            if (File.Exists(_statePath))
                return JsonSerializer.Deserialize<AmbientState>(File.ReadAllText(_statePath))
                       ?? new AmbientState(null, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: could not read {Path}; starting fresh.", _statePath);
        }
        return new AmbientState(null, 0);
    }

    private void SaveState(AmbientState state)
    {
        try
        {
            File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: could not write {Path}.", _statePath);
        }
    }
}
