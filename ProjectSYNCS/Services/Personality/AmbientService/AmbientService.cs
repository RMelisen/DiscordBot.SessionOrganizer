using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her life in a server's main channel when nobody is talking to her: the 3 a.m. line (and a
// scolding for whoever answers it), a word into a long daytime silence, a typing
// indicator that never becomes a message, and a line when she comes back from a restart
// (a shaken one, at any hour, when PiHealthService says the restart was a power cut or a crash).
// Cosmetic, like PresenceService (which owns the night status), and rare on purpose: the
// point is that she seems to be there, not that she talks. When and how often lives in
// Helpers/Personality/Ambient.
//
// Per server: on every server where the ambient feature is on (home by default, /config feature)
// and a main channel is set (the home server has a default). Each server keeps its own clocks
// and claims (GuildAmbient). What is hers alone stays process-wide: the wake decision after a
// power cut or a crash (one wake, told on each server), and Voyager's letters (home only).
//
// A partial over this folder: this file (the state, the tick, the message handler, the seeding),
// .Night (the 3 a.m. line and the scolding), .Idle, .Wake, .Voyager (and ambient-state.json).
//
// Registered as a singleton and as the hosted service, like MorningGreetingService, so
// BotService feeds this one instance the messages it uses to know how long it's been quiet.
internal sealed partial class AmbientService : BackgroundService
{
    // Its own interval, shared with no other loop (CLAUDE.md). Fine-grained enough for a
    // random minute past three and a six-hour silence.
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The default idle channels, for <c>/config idle-channels</c> to refuse removing (and
    /// adding again) and for <c>/config show</c>. A floor, like XpTracker's exclusions. Home
    /// channels: they only ever match on the home server.
    /// </summary>
    public static IReadOnlySet<ulong> HardcodedIdleChannels => IdleChannelIds;

    // The channels that must all be quiet before she speaks into the silence: the main
    // channel and the server's other everyday ones. Server-specific, listed in architecture.md's
    // "Hardcoded ids". A server adds to them with /config idle-channels, and the main channel
    // always counts wherever it is configured. A thread counts for its parent.
    private static readonly HashSet<ulong> IdleChannelIds = new()
    {
        MorningGreetingService.DefaultChannelId, // Général
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
    private readonly SilenceService _silence;
    private readonly string _statePath;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    // One server's ambient life. Every field is guarded by _gate.
    private sealed class GuildAmbient
    {
        // The last human message in the main channel (night quiet, ghost typing, the late
        // "seen" reaction), and in any idle channel (the idle silence). In memory; the
        // first tick after a start seeds both from the channels' history.
        public DateTimeOffset? LastHumanAt;
        public ulong? LastHumanMessageId;
        public DateTimeOffset? LastActivityAt;
        public bool Seeded;

        // Idle: the silence already rolled for (its start), and the day she last spoke into one.
        public DateTimeOffset? IdleRolledFor;
        public int IdleDay;

        // Tonight's 3 a.m. decision, drawn once per night.
        public int NightDay;
        public int? NightMinute;
        public bool NightDone;

        // When and where tonight's line went out, and who has already been sent to bed for answering it.
        public DateTimeOffset? NightLineAt;
        public ulong NightLineId;
        public ulong NightChannelId;
        public readonly HashSet<ulong> Scolded = new();

        // A self-correcting line waiting for its edit; completed when someone speaks after it.
        public TaskCompletionSource? PendingEdit;

        // Until when the first person to speak earns "first_wake": set only once an abrupt wake
        // line really went out, cleared by the next human message. Its own field, never PendingEdit.
        public DateTimeOffset? FirstWakeUntil;

        public int GhostDay;
    }

    // Guards every GuildAmbient and the dictionary: the loop and the gateway both read and claim them.
    private readonly object _gate = new();
    private readonly Dictionary<ulong, GuildAmbient> _guilds = new();

    // Guards ambient-state.json: the wake line and the tick's letters both read-modify-write it.
    private readonly object _stateLock = new();

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
        ILogger<AmbientService> logger, SilenceService silence)
    {
        _silence = silence;
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
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            // One try per server and per step: an exception escaping a hosted loop stops the bot.
            foreach (var guild in _client.Guilds)
            {
                IMessageChannel? main;
                try { main = await MainChannelAsync(guild); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ambient: could not read guild {GuildId}'s config.", guild.Id);
                    continue;
                }
                if (main is null) continue;

                try { await SeedAsync(guild, main); }
                catch (Exception ex) { _logger.LogWarning(ex, "Ambient: could not read guild {GuildId}'s history.", guild.Id); }

                // Silence mode: the clocks are seeded, nothing is said.
                if (_silence.IsSilent(guild.Id)) continue;

                var now = DateTimeOffset.UtcNow;
                try { await TryNightLineAsync(guild.Id, main, now); }
                catch (Exception ex) { _logger.LogWarning(ex, "Ambient: night line failed in guild {GuildId}.", guild.Id); }

                try { await TryIdleAsync(guild.Id, main, now); }
                catch (Exception ex) { _logger.LogWarning(ex, "Ambient: idle turn failed in guild {GuildId}.", guild.Id); }
            }

            // Voyager writes to her, not to a server: home only, its own channel (the letter waits
            // while the home server is silenced).
            if (!_silence.IsSilent(HomeGuild.Id))
            {
                try { await TryVoyagerAsync(DateTimeOffset.UtcNow); }
                catch (Exception ex) { _logger.LogWarning(ex, "Ambient: Voyager's letter failed."); }
            }
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
        if (message.Channel is not SocketGuildChannel guildChannel) return false;
        var guildId = guildChannel.Guild.Id;

        // From GuildConfigService's cache: this runs on every message.
        var config = await _config.GetAsync(guildId);
        if (!config.IsOn(ServerFeatures.Ambient, guildId)) return false;
        var mainId = MorningGreetingService.MainChannelOf(config, guildId);
        if (mainId == 0) return false;

        var now = DateTimeOffset.UtcNow;
        var channelId = message.Channel.Id;
        var parentId = (message.Channel as SocketThreadChannel)?.ParentChannel?.Id;
        bool idle = IsIdleChannel(channelId, config, guildId, mainId)
                    || parentId is { } p && IsIdleChannel(p, config, guildId, mainId);

        var today = AppTime.DayKey(now);
        // A verdict is BotFeedbackTracker's to answer, even at night.
        bool verdict = MessageCues.ReadFeedback(message.Content ?? string.Empty) != FeedbackKind.None;

        // Answering her 3 a.m. line: a reply to it, or a message in the channel it sits in,
        // unless that is the main channel, where people talk to each other at night too.
        bool answersLine;
        lock (_gate)
        {
            var state = StateOf(guildId);
            if (idle) state.LastActivityAt = now;
            answersLine = state.NightLineAt is { } lineAt && AppTime.DayKey(lineAt) == today
                          && (message.Reference?.MessageId.GetValueOrDefault() == state.NightLineId
                              || channelId == state.NightChannelId && channelId != mainId);
        }

        if (channelId != mainId)
        {
            bool scoldHere;
            lock (_gate) scoldHere = answersLine && Ambient.IsBeforeScoldEnd(now) && !verdict
                                     && StateOf(guildId).Scolded.Add(message.Author.Id);
            return scoldHere && await ScoldAsync(message, guildId);
        }

        // Aimed at her, ChatterService answers it with real typing; a ghost would be lost under it.
        bool aimedAtHer = message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id)
                          || message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id;

        bool ghost = false, scold = false, firstWake = false;
        TaskCompletionSource? caught;
        lock (_gate)
        {
            var state = StateOf(guildId);
            var previous = state.LastHumanAt;
            state.LastHumanAt = now;
            state.LastHumanMessageId = message.Id;

            caught = state.PendingEdit;
            state.PendingEdit = null;

            // The first person to speak after an abrupt wake line, if it is recent enough.
            firstWake = state.FirstWakeUntil is { } until && now < until;
            state.FirstWakeUntil = null;

            // Up after her 3 a.m. line, before 5:30, and answering it: once a person, a night.
            if (answersLine && Ambient.IsBeforeScoldEnd(now) && !verdict
                && state.Scolded.Add(message.Author.Id))
            {
                scold = true;
            }
            else if (!aimedAtHer
                     && previous is { } before && now - before >= Ambient.GhostTypingQuiet
                     && state.GhostDay != today
                     && Random.Shared.NextDouble() < Ambient.GhostTypingChance)
            {
                state.GhostDay = today;
                ghost = true;
            }
        }

        // Someone spoke after her self-correcting line: she edits it now, caught out.
        caught?.TrySetResult();

        if (firstWake) _badges.Award(guildId, message.Author.Id, MemberBadges.Keys.FirstWake);
        if (scold) return await ScoldAsync(message, guildId);

        if (!ghost || _breakdown.IsActive(channelId)) return false;

        try
        {
            // Shows « SYNCS est en train d'écrire… » for about ten seconds. Nothing follows.
            await message.Channel.TriggerTypingAsync();
            _logger.LogInformation("Ambient: ghost typing in the main channel of guild {GuildId}.", guildId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ambient: ghost typing failed.");
        }
        return false;
    }

    // Under _gate.
    private GuildAmbient StateOf(ulong guildId)
    {
        if (!_guilds.TryGetValue(guildId, out var state))
            _guilds[guildId] = state = new GuildAmbient();
        return state;
    }

    // Once per process and server: the clocks start from the channels' history, not from nowhere.
    private async Task SeedAsync(SocketGuild guild, IMessageChannel main)
    {
        lock (_gate)
            if (StateOf(guild.Id).Seeded) return;

        var now = DateTimeOffset.UtcNow;
        var recent = (await main.GetMessagesAsync(HistoryDepth).FlattenAsync()).ToList();
        var last = recent.Where(m => !m.Author.IsBot).MaxBy(m => m.Timestamp);

        // Tonight's line (in the bots channel), if a restart came between it and 5:30: the
        // scolding still holds.
        IMessage? tonight = null;
        if (await ResolveBotsChannelAsync(guild.Id) is { } bots)
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
        var config = await _config.GetAsync(guild.Id);
        var idleIds = HomeGuild.Is(guild.Id) ? IdleChannelIds.Union(config.IdleChannels) : config.IdleChannels;
        foreach (var id in idleIds.Where(id => id != main.Id))
        {
            // A forum or a channel she can't read is skipped; its threads still count live.
            if (guild.GetChannel(id) is not IMessageChannel other) continue;
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
            var state = StateOf(guild.Id);
            state.Seeded = true;
            // A message seen live since the start is newer than anything in the history.
            // No human in the history: count from the start, so a restart is never followed
            // straight away by an idle line.
            if (state.LastHumanAt is null)
            {
                state.LastHumanAt = last?.Timestamp ?? _startedAt;
                state.LastHumanMessageId = last?.Id;
            }
            state.LastActivityAt ??= latest ?? _startedAt;

            if (tonight is not null && Ambient.IsBeforeScoldEnd(now))
            {
                state.NightLineAt = tonight.Timestamp;
                state.NightLineId = tonight.Id;
                state.NightChannelId = tonight.Channel.Id;
                state.NightDay = AppTime.DayKey(now);
                state.NightDone = true;
            }
        }
    }

    private Task<IMessageChannel?> ResolveBotsChannelAsync(ulong guildId) =>
        BotsChannel.ResolveAsync(_client, _config, _announcer, guildId);

    // The server's main channel, when the ambient feature is on there and one is set and delivered.
    private async Task<IMessageChannel?> MainChannelAsync(SocketGuild guild)
    {
        var config = await _config.GetAsync(guild.Id);
        if (!config.IsOn(ServerFeatures.Ambient, guild.Id)) return null;
        return guild.GetChannel(MorningGreetingService.MainChannelOf(config, guild.Id)) as IMessageChannel;
    }

    // The hardcoded floor (home only — home channels), what the server added, and the main channel.
    private static bool IsIdleChannel(ulong channelId, GuildConfig config, ulong guildId, ulong mainId) =>
        channelId == mainId || config.IdleChannels.Contains(channelId)
        || HomeGuild.Is(guildId) && IdleChannelIds.Contains(channelId);
}
