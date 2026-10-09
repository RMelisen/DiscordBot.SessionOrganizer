using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Says hello in the general channel once a morning, at a random time inside the window
// in Helpers/MorningGreeting. Cosmetic, like PresenceService, and kept apart from every
// other loop: it sleeps until its slot instead of ticking on an interval.
//
// Someone greeting the channel before her slot can draw the hello out early
// (HandleMessageAsync, fed by BotService). Both paths claim the day through TryClaimDay,
// so whichever comes first is the only hello of the morning. Registered as a singleton
// and as the hosted service, so BotService and the host share this one instance.
internal sealed class MorningGreetingService : BackgroundService
{
    // Where she says hello unless the home guild configured another main channel
    // (/config main-channel). A server-specific id, listed in CLAUDE.md's "Hardcoded ids".
    public const ulong DefaultChannelId = 878305034432045079;

    // How often to look again for the channel while the gateway is still coming up.
    private static readonly TimeSpan ChannelPollInterval = TimeSpan.FromSeconds(30);

    // How far back to look for a hello she already said today.
    private const int HistoryDepth = 50;

    // Both pools go through Helpers/DailyRotation, not ResponsePicker: at one line a
    // day, an in-memory history would be wiped by every restart long before it helped.
    // Each pool has its own salt so the two orders are unrelated; changing a salt
    // reshuffles that pool's rotation.
    private const ulong GreetingSalt = 0x5359_4E43_5348_454C; // "SYNCSHEL"
    private const ulong FunFactSalt = 0x5359_4E43_5346_4143;  // "SYNCSFAC"

    // Odds that someone else's greeting draws her hello out before its slot. Rolled
    // once per person per morning, so a chorus of "bonjour" doesn't make it a certainty.
    private const double EarlyHelloChance = 0.3;

    private static readonly HashSet<string> Lines = new(BotResponses.MorningGreetings);

    private readonly DiscordSocketClient _client;
    private readonly GuildConfigService _config;
    private readonly ILogger<MorningGreetingService> _logger;
    private readonly string _statePath;

    // Guards everything below: the loop and the gateway both read and claim the day.
    private readonly object _gate = new();

    // The day key of the last hello claimed, saved to _statePath on every claim so a
    // restart between an early hello and the slot doesn't say it twice. The channel
    // history check in PostHelloAsync stays as a backstop if the file is lost.
    private int _lastDay;

    // The slot the loop is sleeping towards; an early hello only counts before it.
    private DateTimeOffset? _slot;

    // Who has already had their early-hello roll this morning, and which morning.
    private readonly HashSet<ulong> _rolled = new();
    private int _rolledDay;

    public MorningGreetingService(
        DiscordSocketClient client,
        GuildConfigService config,
        IConfiguration configuration,
        ILogger<MorningGreetingService> logger)
    {
        _client = client;
        _config = config;
        _logger = logger;

        // Next to the database, so it lives under /data in production and survives updates.
        var dbPath = Path.GetFullPath(configuration["Database:Path"] ?? "ProjectSYNCS.db");
        _statePath = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "morning-state.json");
        _lastDay = LoadState().LastDay;
    }

    /// <summary>
    /// Her main channel: the home guild's configured one, else the default. Shared with
    /// AmbientService, which lives in the same channel. Cheap enough for every message:
    /// GuildConfigService answers from its cache.
    /// </summary>
    internal static async Task<ulong> MainChannelIdAsync(GuildConfigService config) =>
        MainChannelOf(await config.GetAsync(HomeGuild.Id));

    internal static ulong MainChannelOf(GuildConfig home) =>
        home.MainChannelId != 0 ? home.MainChannelId : DefaultChannelId;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DateTimeOffset slot;
            lock (_gate)
            {
                slot = MorningGreeting.NextSlot(DateTimeOffset.UtcNow, _lastDay == AppTime.TodayKey, Random.Shared);
                _slot = slot;
            }
            _logger.LogInformation("Next morning greeting at {Slot}.", AppTime.ToZoned(slot));

            var wait = slot - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, stoppingToken);

            // Claimed before the attempt: a failed hello is not retried the same morning.
            if (!TryClaimDay(AppTime.DayKey(slot)))
            {
                _logger.LogInformation("Morning greeting slot skipped: already said early.");
                continue;
            }

            try
            {
                await GreetAsync(MorningGreeting.WindowEndFor(slot), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // An exception escaping a hosted loop stops the whole bot.
                _logger.LogWarning(ex, "Morning greeting failed.");
            }
        }
    }

    private async Task GreetAsync(DateTimeOffset deadline, CancellationToken stoppingToken)
    {
        // A slot drawn right after a start can come before the gateway has delivered the
        // guilds, so wait for the channel — but never past the window. In the dev guild
        // the channel never appears and this just runs out.
        IMessageChannel? channel;
        while ((channel = await ResolveChannelAsync()) is null)
        {
            if (DateTimeOffset.UtcNow + ChannelPollInterval >= deadline)
            {
                _logger.LogInformation("Morning greeting skipped: main channel not available.");
                return;
            }
            await Task.Delay(ChannelPollInterval, stoppingToken);
        }

        await PostHelloAsync(channel);
    }

    // Called by BotService for every message. A greeting in her channel, from 7:00 and
    // before her slot, gets one 30% roll per person to bring the hello forward.
    // Anything aimed at her is ChatterService's to answer; a verdict is
    // BotFeedbackTracker's; a hostile "salut" is no greeting (same rule as ReactionService).
    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot || message.Channel.Id != await MainChannelIdAsync(_config)) return;
        if (message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id)) return;
        if (message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id) return;

        var content = message.Content ?? string.Empty;
        if (MessageCues.ReadFeedback(content) != FeedbackKind.None) return;
        var mood = MessageCues.Analyze(content);
        if (!mood.IsGreeting || mood.Emotion == EmotionKind.Mean) return;

        if (!TryClaimEarly(message.Author.Id, DateTimeOffset.UtcNow)) return;

        _logger.LogInformation("Morning greeting brought forward by {UserId}.", message.Author.Id);
        await PostHelloAsync(message.Channel);
    }

    private bool TryClaimDay(int day)
    {
        lock (_gate)
        {
            if (_lastDay == day) return false;
            _lastDay = day;
            SaveState(new MorningState(day));
            return true;
        }
    }

    private bool TryClaimEarly(ulong userId, DateTimeOffset now)
    {
        var today = AppTime.DayKey(now);
        lock (_gate)
        {
            if (_lastDay == today) return false;
            // Only while today's slot is still ahead, and from 7:00 (an hour before her own
            // window): a "bonjour" at midnight or in the afternoon is not a morning.
            if (_slot is not { } slot || AppTime.DayKey(slot) != today || now >= slot) return false;
            if (!MorningGreeting.AcceptsReply(now)) return false;

            if (_rolledDay != today)
            {
                _rolledDay = today;
                _rolled.Clear();
            }
            if (!_rolled.Add(userId)) return false;
            if (Random.Shared.NextDouble() >= EarlyHelloChance) return false;

            _lastDay = today;
            SaveState(new MorningState(today));
            return true;
        }
    }

    private async Task PostHelloAsync(IMessageChannel channel)
    {
        if (await AlreadyGreetedTodayAsync(channel))
        {
            _logger.LogInformation("Morning greeting skipped: already said today.");
            return;
        }

        // The hello comes first, alone on its line, and the fun fact under it:
        // AlreadyGreetedTodayAsync recognises the hello by that first line.
        // Today's lines, so a restart or an early hello says what the slot would have.
        var now = DateTimeOffset.UtcNow;
        var day = AppTime.DayNumber(now);
        // On her birthday the hello is her age alone, no fun fact: the age is the fact.
        var line = MorningGreeting.IsBirthday(now)
            ? BotResponses.BirthdayGreeting(MorningGreeting.Age(now))
            : DailyRotation.Pick(BotResponses.MorningGreetings, day, GreetingSalt)
              + "\n" + DailyRotation.Pick(BotResponses.MorningFunFacts, day, FunFactSalt);

        await BotChat.PostWithTypingAsync(channel, line, _logger, "morning greeting", AllowedMentions.None);
    }

    private async Task<IMessageChannel?> ResolveChannelAsync() =>
        _client.ConnectionState == ConnectionState.Connected
            ? _client.GetChannel(await MainChannelIdAsync(_config)) as IMessageChannel
            : null;

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
                                   && (Lines.Contains(m.Content.Split('\n')[0]) || m.Content == birthday));
        }
        catch (Exception ex)
        {
            // Better a rare double hello than none.
            _logger.LogWarning(ex, "Could not read channel {ChannelId} history before the morning greeting.", channel.Id);
            return false;
        }
    }

    // What a restart must not forget: the day the hello was last claimed.
    private sealed record MorningState(int LastDay);

    private MorningState LoadState()
    {
        try
        {
            if (File.Exists(_statePath))
                return JsonSerializer.Deserialize<MorningState>(File.ReadAllText(_statePath))
                       ?? new MorningState(0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Morning greeting: could not read {Path}; starting fresh.", _statePath);
        }
        return new MorningState(0);
    }

    private void SaveState(MorningState state)
    {
        try
        {
            File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Morning greeting: could not write {Path}.", _statePath);
        }
    }
}
