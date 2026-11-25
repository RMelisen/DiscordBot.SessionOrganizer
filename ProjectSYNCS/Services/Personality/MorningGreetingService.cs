using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Says hello in a server's main channel once a morning, at a random time inside the window in
// Helpers/Personality/MorningGreeting, on every server where the morning feature is on (home by
// default, /config feature). Cosmetic, like PresenceService, and kept apart from every other loop:
// it sleeps until the next server's slot instead of ticking on an interval (capped, so a server
// that switches the feature on is picked up).
//
// Each server has its own slot and its own claim (GuildMorning). Someone greeting the channel
// before the slot can draw the hello out early (HandleMessageAsync, fed by BotService). Both paths
// claim the day through TryClaimDay / TryClaimEarly under one lock, so whichever comes first is
// the only hello of the morning there. Registered as a singleton and as the hosted service, so
// BotService and the host share this one instance.
internal sealed class MorningGreetingService : BackgroundService
{
    // Where she says hello on the home server unless it configured another main channel
    // (/config main-channel). A server-specific id, listed in architecture.md's "Hardcoded ids".
    // Other servers have no default: no main channel, no hello.
    public const ulong DefaultChannelId = 878305034432045079;

    // The longest the loop sleeps: a server that switches the hello on, or sets its main channel,
    // gets a slot drawn within this. Also how often a slot whose channel hasn't arrived is retried.
    private static readonly TimeSpan MaxSleep = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ChannelRetry = TimeSpan.FromSeconds(30);

    // How far back to look for a hello she already said today.
    private const int HistoryDepth = 50;

    // The pools go through Helpers/Core/DailyRotation, not ResponsePicker: at one line a day,
    // an in-memory history would be wiped by every restart long before it helped. Their salts
    // and the composition live in Helpers/Personality/MorningGreeting.ComposeHello. Every server
    // gets the same line on the same day: accepted, they are different rooms.

    // Odds that someone else's greeting draws her hello out before its slot. Rolled
    // once per person per morning, so a chorus of "bonjour" doesn't make it a certainty.
    private const double EarlyHelloChance = 0.3;

    private static readonly HashSet<string> Lines = new(BotResponses.MorningGreetings);

    private readonly DiscordSocketClient _client;
    private readonly GuildConfigService _config;
    private readonly ILogger<MorningGreetingService> _logger;
    private readonly SilenceService _silence;
    private readonly IServiceProvider _services;
    private readonly string _statePath;

    // One server's morning. Guarded by _gate, like the dictionary holding them.
    private sealed class GuildMorning
    {
        // The day key of the last hello claimed here, saved on every claim so a restart between
        // an early hello and the slot doesn't say it twice. The channel history check in
        // PostHelloAsync stays as a backstop if the file is lost.
        public int LastDay;

        // The slot the loop is waiting for here; an early hello only counts before it.
        public DateTimeOffset? Slot;

        // Who has already had their early-hello roll this morning, and which morning.
        public readonly HashSet<ulong> Rolled = new();
        public int RolledDay;
    }

    // Guards everything below: the loop and the gateway both read and claim the day.
    private readonly object _gate = new();
    private readonly Dictionary<ulong, GuildMorning> _mornings = new();

    public MorningGreetingService(
        DiscordSocketClient client,
        GuildConfigService config,
        IConfiguration configuration,
        ILogger<MorningGreetingService> logger, SilenceService silence,
        IServiceProvider services)
    {
        _services = services;
        _silence = silence;
        _client = client;
        _config = config;
        _logger = logger;

        // Next to the database, so it lives under /data in production and survives updates.
        var dbPath = Path.GetFullPath(configuration["Database:Path"] ?? "ProjectSYNCS.db");
        _statePath = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "morning-state.json");
        foreach (var (guildId, day) in LoadState())
            _mornings[guildId] = new GuildMorning { LastDay = day };
    }

    /// <summary>
    /// Her main channel on a server: the configured one, else the default on the home server, else
    /// none (0). Shared with AmbientService, which lives in the same channel. Cheap enough for every
    /// message: GuildConfigService answers from its cache.
    /// </summary>
    internal static async Task<ulong> MainChannelIdAsync(GuildConfigService config, ulong guildId) =>
        MainChannelOf(await config.GetAsync(guildId), guildId);

    internal static ulong MainChannelOf(GuildConfig config, ulong guildId) =>
        config.MainChannelId != 0 ? config.MainChannelId : HomeGuild.Is(guildId) ? DefaultChannelId : 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var wake = now + MaxSleep;

            if (_client.ConnectionState == ConnectionState.Connected)
            {
                // One try per server: an exception escaping a hosted loop stops the whole bot.
                foreach (var guild in _client.Guilds)
                {
                    try
                    {
                        if (await TickAsync(guild, now) is { } next && next < wake) wake = next;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Morning greeting failed in guild {GuildId}.", guild.Id);
                    }
                }
            }

            var wait = wake - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, stoppingToken);
        }
    }

    // One server: draws its slot, posts when the slot has come. Returns when to look again here.
    private async Task<DateTimeOffset?> TickAsync(SocketGuild guild, DateTimeOffset now)
    {
        var config = await _config.GetAsync(guild.Id);
        if (!config.IsOn(ServerFeatures.Morning, guild.Id) || MainChannelOf(config, guild.Id) == 0)
        {
            lock (_gate)
                if (_mornings.TryGetValue(guild.Id, out var off)) off.Slot = null;
            return null;
        }

        DateTimeOffset slot;
        lock (_gate)
        {
            var morning = MorningOf(guild.Id);
            if (morning.Slot is null)
            {
                morning.Slot = MorningGreeting.NextSlot(now, morning.LastDay == AppTime.TodayKey, Random.Shared);
                _logger.LogInformation("Next morning greeting in guild {GuildId} at {Slot}.", guild.Id,
                    AppTime.ToZoned(morning.Slot.Value));
            }
            slot = morning.Slot.Value;
        }
        if (slot > now) return slot;

        // The slot has come. A slot drawn right after a start can come before the gateway has
        // delivered the channel: wait for it, but never past the window.
        if (guild.GetChannel(MainChannelOf(config, guild.Id)) is not IMessageChannel channel)
        {
            if (now + ChannelRetry < MorningGreeting.WindowEndFor(slot)) return now + ChannelRetry;
            _logger.LogInformation("Morning greeting skipped in guild {GuildId}: main channel not available.", guild.Id);
            NextSlot(guild.Id, now, greetedToday: true);
            return null;
        }

        // Claimed before the attempt: a failed hello is not retried the same morning.
        bool claimed = TryClaimDay(guild.Id, AppTime.DayKey(slot));
        NextSlot(guild.Id, now, greetedToday: true);
        if (!claimed)
        {
            _logger.LogInformation("Morning greeting slot skipped in guild {GuildId}: already said early.", guild.Id);
            return null;
        }

        await PostHelloAsync(guild.Id, channel);
        return null;
    }

    // Called by BotService for every message. A greeting in a server's main channel, from 7:00 and
    // before her slot there, gets one 30% roll per person to bring the hello forward.
    // Anything aimed at her is ChatterService's to answer; a verdict is
    // BotFeedbackTracker's; a hostile "salut" is no greeting (same rule as ReactionService).
    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message || message.Author.IsBot) return;
        if (message.Channel is not SocketGuildChannel guildChannel) return;
        var guildId = guildChannel.Guild.Id;
        var config = await _config.GetAsync(guildId);
        if (message.Channel.Id != MainChannelOf(config, guildId) || !config.IsOn(ServerFeatures.Morning, guildId)) return;
        if (message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id)) return;
        if (message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id) return;

        var content = message.Content ?? string.Empty;
        // « Salut syncs » is aimed at her too: ChatterService greets it back.
        if (MessageCues.NamesHerFrom(content, message.Author.Id == AvailabilityService.OwnerId)) return;
        if (MessageCues.ReadFeedback(content) != FeedbackKind.None) return;
        var mood = MessageCues.Analyze(content);
        if (!mood.IsGreeting || mood.Emotion == EmotionKind.Mean) return;

        if (!TryClaimEarly(guildId, message.Author.Id, DateTimeOffset.UtcNow)) return;

        _logger.LogInformation("Morning greeting brought forward by {UserId} in guild {GuildId}.", message.Author.Id, guildId);
        await PostHelloAsync(guildId, message.Channel);
    }

    // Under _gate.
    private GuildMorning MorningOf(ulong guildId)
    {
        if (!_mornings.TryGetValue(guildId, out var morning))
            _mornings[guildId] = morning = new GuildMorning();
        return morning;
    }

    private void NextSlot(ulong guildId, DateTimeOffset now, bool greetedToday)
    {
        lock (_gate)
            MorningOf(guildId).Slot = MorningGreeting.NextSlot(now, greetedToday, Random.Shared);
    }

    private bool TryClaimDay(ulong guildId, int day)
    {
        lock (_gate)
        {
            var morning = MorningOf(guildId);
            if (morning.LastDay == day) return false;
            morning.LastDay = day;
            SaveStateLocked();
            return true;
        }
    }

    private bool TryClaimEarly(ulong guildId, ulong userId, DateTimeOffset now)
    {
        var today = AppTime.DayKey(now);
        lock (_gate)
        {
            var morning = MorningOf(guildId);
            if (morning.LastDay == today) return false;
            // Only while today's slot is still ahead, and from 7:00 (an hour before her own
            // window): a "bonjour" at midnight or in the afternoon is not a morning.
            if (morning.Slot is not { } slot || AppTime.DayKey(slot) != today || now >= slot) return false;
            if (!MorningGreeting.AcceptsReply(now)) return false;

            if (morning.RolledDay != today)
            {
                morning.RolledDay = today;
                morning.Rolled.Clear();
            }
            if (!morning.Rolled.Add(userId)) return false;
            if (Random.Shared.NextDouble() >= EarlyHelloChance) return false;

            morning.LastDay = today;
            SaveStateLocked();
            return true;
        }
    }

    private async Task PostHelloAsync(ulong guildId, IMessageChannel channel)
    {
        if (_silence.IsSilent(guildId))
        {
            _logger.LogInformation("Morning greeting skipped in guild {GuildId}: silence mode.", guildId);
            return;
        }
        if (await AlreadyGreetedTodayAsync(channel))
        {
            _logger.LogInformation("Morning greeting skipped in guild {GuildId}: already said today.", guildId);
            return;
        }

        // The hello comes first, alone on its line, and the fun fact under it:
        // AlreadyGreetedTodayAsync recognises the hello by that first line.
        // Today's lines, so a restart or an early hello says what the slot would have.
        // On her birthday the hello is her age alone, no fun fact: the age is the fact.
        // A won /shoutout replaces the hello line, or ends the message on a credit line (by date, see
        // MorningGreeting.UsesCredit), and pings its winner alone; it is settled only
        // once posted, so a birthday, a silenced day (returned above) or a failed send carries it on.
        var now = DateTimeOffset.UtcNow;
        var due = await NextDedicationAsync(guildId, now);
        var (line, mentions, usesDedication) = MorningGreeting.ComposeHello(now, due?.UserId);

        var posted = await BotChat.PostWithTypingAsync(channel, line, _logger, "morning greeting", mentions);
        if (posted is not null && usesDedication && due is not null)
            await SettleDedicationAsync(due.Id, now);
    }

    // The won dedication this hello should carry, if any. A database hiccup costs the dedication
    // today (it stays due), never the hello.
    private async Task<Models.ShoutoutBid?> NextDedicationAsync(ulong guildId, DateTimeOffset now)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ShoutoutService>().NextDueAsync(guildId, now);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Morning greeting: could not read the dedication; saying the plain hello.");
            return null;
        }
    }

    // Posted: stamp it and log it. A failure here is logged; the dedication
    // stays due, and would be posted again the next morning (better twice than paid for and lost).
    private async Task SettleDedicationAsync(int bidId, DateTimeOffset now)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ShoutoutService>().MarkShownAsync(bidId, now);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Morning greeting: dedication {BidId} posted but not settled.", bidId);
        }
    }

    /// <summary>
    /// Whether one of her messages is a morning hello: its first line is a pool line or a /shoutout
    /// dedication, or it is the birthday hello for its day's age. By text, so it holds across a restart — ChatterService
    /// uses it to answer a reply to the hello as one (ReplyContext.MorningHello).
    /// </summary>
    public static bool IsMorningHello(IMessage message) =>
        IsHelloLine(message.Content.Split('\n')[0])
        || message.Content == BotResponses.BirthdayGreeting(MorningGreeting.Age(message.Timestamp));

    // A pool hello, or a /shoutout dedication in its place.
    private static bool IsHelloLine(string firstLine) =>
        Lines.Contains(firstLine) || MorningGreeting.IsDedicationLine(firstLine);

    // A restart inside the window draws a fresh slot for today; this keeps it from
    // saying hello twice. Matching a first line against the pool's exact text tells her
    // hello apart from anything else she posted this morning.
    private async Task<bool> AlreadyGreetedTodayAsync(IMessageChannel channel)
    {
        try
        {
            var today = AppTime.TodayKey;
            var now = DateTimeOffset.UtcNow;
            // Her birthday hello is not in the pool; it is one fixed line for today's age.
            var birthday = MorningGreeting.IsBirthday(now)
                ? BotResponses.BirthdayGreeting(MorningGreeting.Age(now))
                : null;
            var recent = await channel.GetMessagesAsync(HistoryDepth).FlattenAsync();
            return recent.Any(m => m.Author.Id == _client.CurrentUser.Id
                                   && AppTime.DayKey(m.Timestamp) == today
                                   && (IsHelloLine(m.Content.Split('\n')[0]) || m.Content == birthday));
        }
        catch (Exception ex)
        {
            // Better a rare double hello than none.
            _logger.LogWarning(ex, "Could not read channel {ChannelId} history before the morning greeting.", channel.Id);
            return false;
        }
    }

    private IReadOnlyDictionary<ulong, int> LoadState()
    {
        try
        {
            if (File.Exists(_statePath))
                return MorningStateFile.Parse(File.ReadAllText(_statePath), HomeGuild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Morning greeting: could not read {Path}; starting fresh.", _statePath);
        }
        return new Dictionary<ulong, int>();
    }

    // Under _gate.
    private void SaveStateLocked()
    {
        try
        {
            File.WriteAllText(_statePath,
                MorningStateFile.Write(_mornings.Where(m => m.Value.LastDay != 0).ToDictionary(m => m.Key, m => m.Value.LastDay)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Morning greeting: could not write {Path}.", _statePath);
        }
    }
}

/// <summary>
/// morning-state.json, pure so the checks read both shapes: today's <c>{ "Days": { "guildId": day } }</c>
/// (the day each server's hello was last claimed), and the one-server shape it replaced,
/// <c>{ "LastDay": day }</c>, which was the home server's.
/// </summary>
public static class MorningStateFile
{
    private sealed record State(Dictionary<string, int>? Days, int? LastDay);

    public static IReadOnlyDictionary<ulong, int> Parse(string json, ulong homeGuildId)
    {
        var result = new Dictionary<ulong, int>();
        if (JsonSerializer.Deserialize<State>(json) is not { } state) return result;
        if (state.Days is { } days)
        {
            foreach (var (key, day) in days)
                if (ulong.TryParse(key, out var id)) result[id] = day;
        }
        else if (state.LastDay is { } last && last != 0)
            result[homeGuildId] = last;
        return result;
    }

    public static string Write(IReadOnlyDictionary<ulong, int> days) =>
        JsonSerializer.Serialize(new State(days.ToDictionary(d => d.Key.ToString(), d => d.Value), null));
}
