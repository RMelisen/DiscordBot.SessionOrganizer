using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her life in the main channel when nobody is talking to her: the 3 a.m. line, a word
// into a long daytime silence, a typing indicator that never becomes a message, and a
// line when she comes back from a restart. Cosmetic, like PresenceService (which owns
// the night status), and rare on purpose: the point is that she seems to be there, not
// that she talks. When and how often lives in Helpers/Ambient.
//
// Registered as a singleton and as the hosted service, like MorningGreetingService, so
// BotService feeds this one instance the messages it uses to know how long it's been quiet.
internal sealed class AmbientService : BackgroundService
{
    // Its own interval, shared with no other loop (CLAUDE.md). Fine-grained enough for a
    // random minute past three and a six-hour silence.
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(10);

    // How far back to look for the last human message on the first tick after a start,
    // and for a 3 a.m. line already said tonight.
    private const int HistoryDepth = 50;

    // How long to wait for the channel after the first Ready before giving up the wake line.
    private static readonly TimeSpan WakeChannelWait = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ChannelPollInterval = TimeSpan.FromSeconds(10);

    // Every pool here is spent at most once a day, so it goes through DailyRotation (a
    // restart would wipe ResponsePicker's memory long before it helped). One salt each.
    private const ulong NightSalt = 0x5359_4E43_4E49_4748;      // "SYNCNIGH"
    private const ulong IdleSalt = 0x5359_4E43_4944_4C45;       // "SYNCIDLE"
    private const ulong EditSalt = 0x5359_4E43_4544_4954;       // "SYNCEDIT"
    private const ulong SeenSalt = 0x5359_4E43_5345_454E;       // "SYNCSEEN"
    private const ulong WakeSalt = 0x5359_4E43_5741_4B45;       // "SYNCWAKE"
    private const ulong WakeUpdateSalt = 0x5359_4E43_5550_4454; // "SYNCUPDT"

    private static readonly HashSet<string> NightLineSet = new(BotResponses.NightLines);

    private readonly DiscordSocketClient _client;
    private readonly BreakdownService _breakdown;
    private readonly ILogger<AmbientService> _logger;
    private readonly string _statePath;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    // Guards everything below: the loop and the gateway both read and claim it.
    private readonly object _gate = new();

    // The last human message in the main channel. In memory; the first tick after a
    // start seeds it from the channel's history.
    private DateTimeOffset? _lastHumanAt;
    private ulong? _lastHumanMessageId;
    private bool _seeded;

    // Idle: the silence already rolled for (its start), and the day she last spoke into one.
    private DateTimeOffset? _idleRolledFor;
    private int _idleDay;

    // Tonight's 3 a.m. decision, drawn once per night.
    private int _nightDay;
    private int? _nightMinute;
    private bool _nightDone;

    private int _ghostDay;
    private int _woken;

    public AmbientService(
        DiscordSocketClient client,
        BreakdownService breakdown,
        IConfiguration config,
        ILogger<AmbientService> logger)
    {
        _client = client;
        _breakdown = breakdown;
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
            catch (Exception ex) { _logger.LogWarning(ex, "Ambient: could not read the main channel's history."); }

            var now = DateTimeOffset.UtcNow;
            try { await TryNightLineAsync(channel, now); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ambient: night line failed."); }

            try { await TryIdleAsync(channel, now); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ambient: idle turn failed."); }
        }
    }

    // Called by BotService for every message. Keeps the main channel's clock, and now and
    // then answers a message that breaks a silence with a typing indicator and nothing else.
    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot || message.Channel.Id != MorningGreetingService.ChannelId) return;

        var now = DateTimeOffset.UtcNow;
        var today = AppTime.DayKey(now);
        // Aimed at her, ChatterService answers it with real typing; a ghost would be lost under it.
        bool aimedAtHer = message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id)
                          || message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id;

        bool ghost = false;
        lock (_gate)
        {
            var previous = _lastHumanAt;
            _lastHumanAt = now;
            _lastHumanMessageId = message.Id;

            if (!aimedAtHer
                && previous is { } p && now - p >= Ambient.GhostTypingQuiet
                && _ghostDay != today
                && Random.Shared.NextDouble() < Ambient.GhostTypingChance)
            {
                _ghostDay = today;
                ghost = true;
            }
        }

        if (!ghost || _breakdown.IsActive(message.Channel.Id)) return;

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
    }

    // Once per process: the gateway can drop the human clock's history, but not this.
    private async Task SeedAsync(IMessageChannel channel)
    {
        lock (_gate)
            if (_seeded) return;

        var recent = await channel.GetMessagesAsync(HistoryDepth).FlattenAsync();
        var last = recent.Where(m => !m.Author.IsBot).MaxBy(m => m.Timestamp);

        lock (_gate)
        {
            _seeded = true;
            // A message seen live since the start is newer than anything in the history.
            if (_lastHumanAt is null)
            {
                // No human in the recent history: count from the start rather than from
                // nowhere, so a restart is never followed straight away by an idle line.
                _lastHumanAt = last?.Timestamp ?? _startedAt;
                _lastHumanMessageId = last?.Id;
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
        await BotChat.PostWithTypingAsync(channel, line, _logger, "night line", AllowedMentions.None);
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
            if (_lastHumanAt is not { } silenceStart || now - silenceStart < Ambient.IdleQuiet) return;
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
            await PostEditedAsync(channel, DailyRotation.Pick(BotResponses.IdleEditLines, day, EditSalt));
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

    // Says the first version, then thinks better of it: her mid-line self-correction,
    // done with an edit.
    private async Task PostEditedAsync(IMessageChannel channel, (string Before, string After) pair)
    {
        var message = await BotChat.PostWithTypingAsync(channel, pair.Before, _logger, "idle line", AllowedMentions.None);
        if (message is null) return;

        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(3, 7)));
        try
        {
            await message.ModifyAsync(p => p.Content = pair.After);
            _logger.LogInformation("Ambient: self-correcting idle line posted.");
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
            if (!updated && Random.Shared.NextDouble() >= Ambient.WakeChance) return;

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
