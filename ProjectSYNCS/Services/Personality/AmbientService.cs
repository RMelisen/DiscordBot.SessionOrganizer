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
// indicator that never becomes a message, and a line when she comes back from a restart
// (a shaken one, at any hour, when PiHealthService says the restart was a power cut or a crash).
// Cosmetic, like PresenceService (which owns the night status), and rare on purpose: the
// point is that she seems to be there, not that she talks. When and how often lives in
// Helpers/Personality/Ambient.
//
// Registered as a singleton and as the hosted service, like MorningGreetingService, so
// BotService feeds this one instance the messages it uses to know how long it's been quiet.
internal sealed class AmbientService : BackgroundService
{
    // Its own interval, shared with no other loop (CLAUDE.md). Fine-grained enough for a
    // random minute past three and a six-hour silence.
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The default idle channels, for <c>/config idle-channels</c> to refuse removing (and
    /// adding again) and for <c>/config show</c>. A floor, like XpTracker's exclusions.
    /// </summary>
    public static IReadOnlySet<ulong> HardcodedIdleChannels => IdleChannelIds;

    // The channels that must all be quiet before she speaks into the silence: the main
    // channel and the server's other everyday ones. Server-specific, listed in CLAUDE.md's
    // "Hardcoded ids". The home guild adds to them with /config idle-channels, and the
    // main channel always counts wherever it is configured. A thread counts for its parent.
    private static readonly HashSet<ulong> IdleChannelIds = new()
    {
        MorningGreetingService.DefaultChannelId, // Général
        878306977887957042,               // Média
        1025683260157722704,              // Galerie
        878305034432045080,               // Gaming
        1483863597041062109,              // Musique
        1500799769914773574,              // Nourriture
    };

    // Where Voyager's letters land — their own channel, not the main one. Server-specific, listed
    // in CLAUDE.md's "Hardcoded ids". In the dev guild it never resolves and the letters are skipped.
    internal const ulong VoyagerChannelId = 901172450719584356;

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
    private const ulong PowerCutSalt = 0x5359_4E43_504F_5752;   // "SYNCPOWR"
    private const ulong CrashSalt = 0x5359_4E43_4352_5348;      // "SYNCCRSH"
    private const ulong GlitchSalt = 0x5359_4E43_474C_4954;     // "SYNCGLIT"
    private const ulong VoyagerLetterSalt = 0x5359_4E43_564F_594C; // "SYNCVOYL"
    private const ulong VoyagerAnswerSalt = 0x5359_4E43_564F_5941; // "SYNCVOYA"

    private static readonly HashSet<string> NightLineSet = new(BotResponses.NightLines);

    private readonly DiscordSocketClient _client;
    private readonly GuildConfigService _config;
    private readonly BreakdownService _breakdown;
    private readonly PiHealthService _health;
    private readonly ResponsePicker _picker;
    private readonly HerMessages _herMessages;
    private readonly BadgeAwarder _badges;
    private readonly PlynlingAnnouncer _announcer;
    private readonly ILogger<AmbientService> _logger;
    private readonly string _statePath;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    // Guards everything below: the loop and the gateway both read and claim it.
    private readonly object _gate = new();

    // Guards ambient-state.json: the wake line and the tick's letters both read-modify-write it.
    private readonly object _stateLock = new();

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

    // Until when the first person to speak earns "first_wake": set only once an abrupt wake
    // line really went out, cleared by the next human message. Its own field, never _pendingEdit.
    private DateTimeOffset? _firstWakeUntil;

    private int _ghostDay;
    private int _woken;

    public AmbientService(
        DiscordSocketClient client,
        GuildConfigService guildConfig,
        BreakdownService breakdown,
        PiHealthService health,
        ResponsePicker picker,
        HerMessages herMessages,
        BadgeAwarder badges,
        PlynlingAnnouncer announcer,
        IConfiguration config,
        ILogger<AmbientService> logger)
    {
        _announcer = announcer;
        _badges = badges;
        _client = client;
        _config = guildConfig;
        _breakdown = breakdown;
        _health = health;
        _picker = picker;
        _herMessages = herMessages;
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

            var channel = await ResolveChannelAsync();
            if (channel is null) continue;

            // Each step in its own try: an exception escaping a hosted loop stops the bot.
            try { await SeedAsync(channel); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ambient: could not read the channels' history."); }

            var now = DateTimeOffset.UtcNow;
            try { await TryNightLineAsync(channel, now); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ambient: night line failed."); }

            try { await TryIdleAsync(channel, now); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ambient: idle turn failed."); }

            try { await TryVoyagerAsync(now); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ambient: Voyager's letter failed."); }
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
        // From GuildConfigService's cache: this runs on every message.
        var home = await _config.GetAsync(HomeGuild.Id);
        var mainId = MorningGreetingService.MainChannelOf(home);
        if (IsIdleChannel(channelId, home, mainId) || parentId is { } p && IsIdleChannel(p, home, mainId))
            lock (_gate) _lastActivityAt = now;

        if (channelId != mainId) return false;

        var today = AppTime.DayKey(now);
        // Aimed at her, ChatterService answers it with real typing; a ghost would be lost under it.
        bool aimedAtHer = message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id)
                          || message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id;
        // A verdict is BotFeedbackTracker's to answer, even at night.
        bool verdict = MessageCues.ReadFeedback(message.Content ?? string.Empty) != FeedbackKind.None;

        bool ghost = false, scold = false, firstWake = false;
        TaskCompletionSource? caught;
        lock (_gate)
        {
            var previous = _lastHumanAt;
            _lastHumanAt = now;
            _lastHumanMessageId = message.Id;

            caught = _pendingEdit;
            _pendingEdit = null;

            // The first person to speak after an abrupt wake line, if it is recent enough.
            firstWake = _firstWakeUntil is { } until && now < until;
            _firstWakeUntil = null;

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

        var guildId = (message.Channel as SocketGuildChannel)?.Guild.Id ?? 0;
        if (firstWake) _badges.Award(guildId, message.Author.Id, MemberBadges.Keys.FirstWake);
        // They answered the line, whether or not the scolding goes out.
        if (scold) _badges.Award(guildId, message.Author.Id, MemberBadges.Keys.NightOwl);

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

        // Tonight's line (in the bots channel), if a restart came between it and 5:30: the
        // scolding still holds.
        IMessage? tonight = null;
        if (await ResolveBotsChannelAsync() is { } bots)
        {
            try
            {
                tonight = (await bots.GetMessagesAsync(HistoryDepth).FlattenAsync())
                    .Where(m => m.Author.Id == _client.CurrentUser.Id
                                && AppTime.DayKey(m.Timestamp) == AppTime.DayKey(now)
                                && NightLineSet.Contains(m.Content))
                    .MaxBy(m => m.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ambient: could not read the bots channel's history.");
            }
        }

        var latest = last?.Timestamp;
        var home = await _config.GetAsync(HomeGuild.Id);
        foreach (var id in IdleChannelIds.Union(home.IdleChannels).Where(id => id != main.Id))
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

    // The 3 a.m. line goes to the bots channel; the quiet it waits for is the main channel's
    // (`main`), and the scolding stays there too.
    private async Task TryNightLineAsync(IMessageChannel main, DateTimeOffset now)
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

        if (_breakdown.IsActive(main.Id)) return;
        var channel = await ResolveBotsChannelAsync();
        if (channel is null) return;
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
            _herMessages.Remember(message, ReplyContext.Lonely);
            if (message is not null)
                _ = Task.Run(() => CorrectLaterAsync(message, pair.After, Ambient.EditAfter, "self-correcting idle line"));
            return;
        }

        var line = DailyRotation.Pick(BotResponses.IdleLines, day, IdleSalt);
        _herMessages.Remember(await BotChat.PostWithTypingAsync(channel, line, _logger, "idle line", AllowedMentions.None),
            ReplyContext.Lonely);
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

    // A line she edits later, in the main channel: after `wait`, or two seconds after
    // someone speaks there — she got caught (the idle self-correction) or snapped back (the
    // glitched wake). Runs off the tick loop and the gateway handler. A restart before the edit
    // leaves the first version standing.
    private async Task CorrectLaterAsync(IUserMessage message, string after, TimeSpan wait, string what)
    {
        try
        {
            var caught = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _pendingEdit = caught;

            var first = await Task.WhenAny(Task.Delay(wait), caught.Task);
            lock (_gate)
                if (_pendingEdit == caught) _pendingEdit = null;

            if (first == caught.Task)
                await Task.Delay(Ambient.CaughtEditDelay);

            await message.ModifyAsync(p => p.Content = after);
            _logger.LogInformation("Ambient: {What} edited ({How}).", what,
                first == caught.Task ? "someone spoke" : "timed out");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: could not edit the {What}.", what);
        }
    }

    // Voyager 1 writes to her (docs/agents/personality.md, Ambient life), in VoyagerChannelId: a
    // letter on the days Helpers/Personality/Voyager picks, at its slot; her answer, some of those
    // days, hours later; and on 18 Nov 2026, once ever, the light-day line in place of any letter.
    // Claimed in ambient-state.json before the send, so a restart never says one twice and a
    // failed send isn't retried that day. One line per tick at most.
    private async Task TryVoyagerAsync(DateTimeOffset now)
    {
        if (!Ambient.IsWakeHours(now)) return;
        if (_client.GetChannel(VoyagerChannelId) is not IMessageChannel channel || _breakdown.IsActive(channel.Id)) return;

        var day = AppTime.DayNumber(now);
        var time = AppTime.ToZoned(now).TimeOfDay;
        string? line = null, what = null;
        lock (_stateLock)
        {
            var state = LoadState();
            if (Voyager.IsCrossingDay(day))
            {
                if (!state.CrossingDone && state.LastLetterDay != day && time >= Voyager.LetterSlot(day))
                {
                    line = BotResponses.VoyagerCrossingLine;
                    what = "Voyager crossing line";
                    state = state with { CrossingDone = true, LastLetterDay = day };
                }
            }
            else if (Voyager.LetterNumber(day) is { } number && state.LastLetterDay != day && time >= Voyager.LetterSlot(day))
            {
                line = VoyagerLetter(number, now);
                what = "Voyager letter";
                state = state with { LastLetterDay = day };
            }

            if (line is null && state.LastLetterDay == day && state.LastAnswerDay != day
                && Voyager.AnswerSlot(day) is { } answerAt && time >= answerAt)
            {
                // On the crossing day the letter was the light-day line, which has no number.
                line = VoyagerAnswer(Voyager.LetterNumber(day) ?? day);
                what = "Voyager answer";
                state = state with { LastAnswerDay = day };
            }

            if (line is null) return;
            SaveState(state);
        }

        _logger.LogInformation("Ambient: {What}.", what);
        await BotChat.PostWithTypingAsync(channel, line, _logger, what!, AllowedMentions.None);
    }

    /// <summary>Letter number <paramref name="letterNumber"/>, with his light time at <paramref name="now"/>. Also /debug voyager's.</summary>
    internal static string VoyagerLetter(int letterNumber, DateTimeOffset now) =>
        string.Format(DailyRotation.Pick(BotResponses.VoyagerLetterLines, letterNumber, VoyagerLetterSalt),
            Voyager.Format(Voyager.LightTime(now)));

    /// <summary>Her answer to letter <paramref name="letterNumber"/>. Also /debug voyager's.</summary>
    internal static string VoyagerAnswer(int letterNumber) =>
        DailyRotation.Pick(BotResponses.VoyagerAnswerLines, letterNumber, VoyagerAnswerSalt);

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
            // Only a power cut or a crash gets a wake line, whatever the hour, however many
            // times today. An ordinary restart or update is silent.
            var stop = _health.LastStop;
            if (!PiHealth.IsAbrupt(stop)) return;

            var channel = await WaitForChannelAsync();
            if (channel is null || _breakdown.IsActive(channel.Id)) return;

            var day = AppTime.DayNumber(now);
            if (Random.Shared.NextDouble() < Ambient.GlitchWakeChance)
            {
                await GlitchWakeAsync(channel, day, today);
                return;
            }

            var line = string.Format(stop == StopKind.PowerCut
                    ? DailyRotation.Pick(BotResponses.PowerCutWakeLines, day, PowerCutSalt)
                    : DailyRotation.Pick(BotResponses.CrashWakeLines, day, CrashSalt),
                PiHealth.Duration(_health.Downtime));

            var sent = await BotChat.PostWithTypingAsync(channel, line, _logger, "wake line", AllowedMentions.None);
            _herMessages.Remember(sent, ReplyContext.Wake);
            if (sent is not null)
            {
                lock (_stateLock) SaveState(LoadState() with { LastVersion = AppInfo.Version, LastWakeDay = today });
                ArmFirstWake();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: wake line failed.");
        }
    }

    // Back from a power cut or a crash, corrupted: the glitched line stands for a while — two
    // minutes, or until someone speaks in the main channel and she snaps back — then the same
    // message is edited clean. The glitch strip follows the corrupted line and is deleted as she
    // snaps back, so nothing is left to prove it. It runs beside the wait, not before it, so
    // someone speaking during the upload still catches her. Already off the gateway handler
    // (WakeAsync runs in Task.Run).
    private async Task GlitchWakeAsync(IMessageChannel channel, int day, int today)
    {
        var (glitched, recovered) = DailyRotation.Pick(BotResponses.GlitchWakeLines, day, GlitchSalt);
        var sent = await BotChat.PostWithTypingAsync(channel, glitched, _logger, "glitch wake line", AllowedMentions.None);
        if (sent is null) return;
        _herMessages.Remember(sent, ReplyContext.Wake);
        lock (_stateLock) SaveState(LoadState() with { LastVersion = AppInfo.Version, LastWakeDay = today });
        ArmFirstWake();

        var strip = Task.Run(async () =>
        {
            await Task.Delay(StickerService.Beat);
            return await StickerService.SendAsync(channel, Sticker.GlitchMood, _logger);
        });

        var clean = string.Format(recovered, PiHealth.Duration(_health.Downtime));
        await CorrectLaterAsync(sent, clean, Ambient.GlitchEditAfter, "glitch wake line");

        try
        {
            if (await strip is { } picture) await picture.DeleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: could not delete the glitch wake strip.");
        }
    }

    private void ArmFirstWake()
    {
        lock (_gate) _firstWakeUntil = DateTimeOffset.UtcNow + Ambient.FirstWakeWindow;
    }

    private async Task<IMessageChannel?> WaitForChannelAsync()
    {
        var deadline = DateTimeOffset.UtcNow + WakeChannelWait;
        IMessageChannel? channel;
        while ((channel = await ResolveChannelAsync()) is null)
        {
            // In the dev guild the channel never appears; this just runs out.
            if (DateTimeOffset.UtcNow >= deadline) return null;
            await Task.Delay(ChannelPollInterval);
        }
        return channel;
    }

    private Task<IMessageChannel?> ResolveBotsChannelAsync() =>
        BotsChannel.ResolveAsync(_client, _config, _announcer, HomeGuild.Id);

    private async Task<IMessageChannel?> ResolveChannelAsync() =>
        _client.ConnectionState == ConnectionState.Connected
            ? _client.GetChannel(await MorningGreetingService.MainChannelIdAsync(_config)) as IMessageChannel
            : null;

    // The hardcoded floor, what the home guild added, and the main channel wherever it is.
    private static bool IsIdleChannel(ulong channelId, GuildConfig home, ulong mainId) =>
        channelId == mainId || IdleChannelIds.Contains(channelId) || home.IdleChannels.Contains(channelId);

    // What a restart must not forget: the version she last ran (for the update line), the day
    // she last spoke on waking (one wake line a day, however many deploys) — a DayKey — and
    // Voyager's correspondence: the last letter and answer days (DayNumbers) and whether the
    // light-day line went out. Fields added later default, so an older file still reads.
    private sealed record AmbientState(
        string? LastVersion, int LastWakeDay,
        int LastLetterDay = 0, int LastAnswerDay = 0, bool CrossingDone = false);

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
