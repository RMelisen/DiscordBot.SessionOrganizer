using Discord;
using Discord.WebSocket;
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
    // Where she says hello. A server-specific id, listed in CLAUDE.md's "Hardcoded ids".
    public const ulong ChannelId = 878305034432045079;

    // How often to look again for the channel while the gateway is still coming up.
    private static readonly TimeSpan ChannelPollInterval = TimeSpan.FromSeconds(30);

    // How far back to look for a hello she already said today.
    private const int HistoryDepth = 50;

    // Each pool's own picker bucket, never the channel's. History is per bucket, and at
    // one pick a day, a day of chatter in the channel would push yesterday's hello out
    // of it; the two pools would also crowd each other. Not snowflakes.
    private const ulong GreetingBucket = 2;
    private const ulong FunFactBucket = 1;

    // Odds that someone else's greeting draws her hello out before its slot. Rolled
    // once per person per morning, so a chorus of "bonjour" doesn't make it a certainty.
    private const double EarlyHelloChance = 0.3;

    private static readonly HashSet<string> Lines = new(BotResponses.MorningGreetings);

    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<MorningGreetingService> _logger;

    // Guards everything below: the loop and the gateway both read and claim the day.
    private readonly object _gate = new();

    // The day key of the last hello this process claimed. In memory, so a restart
    // forgets it — the channel history check in PostHelloAsync covers that case.
    private int _lastDay;

    // The slot the loop is sleeping towards; an early hello only counts before it.
    private DateTimeOffset? _slot;

    // Who has already had their early-hello roll this morning, and which morning.
    private readonly HashSet<ulong> _rolled = new();
    private int _rolledDay;

    public MorningGreetingService(
        DiscordSocketClient client,
        ResponsePicker picker,
        ILogger<MorningGreetingService> logger)
    {
        _client = client;
        _picker = picker;
        _logger = logger;
    }

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
        while ((channel = ResolveChannel()) is null)
        {
            if (DateTimeOffset.UtcNow + ChannelPollInterval >= deadline)
            {
                _logger.LogInformation("Morning greeting skipped: channel {ChannelId} not available.", ChannelId);
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
        if (message.Author.IsBot || message.Channel.Id != ChannelId) return;
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
        var line = _picker.Pick(GreetingBucket, BotResponses.MorningGreetings)
                   + "\n" + _picker.Pick(FunFactBucket, BotResponses.MorningFunFacts);

        await BotChat.PostWithTypingAsync(channel, line, _logger, "morning greeting", AllowedMentions.None);
    }

    private IMessageChannel? ResolveChannel() =>
        _client.ConnectionState == ConnectionState.Connected
            ? _client.GetChannel(ChannelId) as IMessageChannel
            : null;

    // A restart inside the window draws a fresh slot for today; this keeps it from
    // saying hello twice. Matching a first line against the pool's exact text tells her
    // hello apart from anything else she posted this morning.
    private async Task<bool> AlreadyGreetedTodayAsync(IMessageChannel channel)
    {
        try
        {
            var today = AppTime.TodayKey;
            var recent = await channel.GetMessagesAsync(HistoryDepth).FlattenAsync();
            return recent.Any(m => m.Author.Id == _client.CurrentUser.Id
                                   && AppTime.DayKey(m.Timestamp) == today
                                   && Lines.Contains(m.Content.Split('\n')[0]));
        }
        catch (Exception ex)
        {
            // Better a rare double hello than none.
            _logger.LogWarning(ex, "Could not read channel {ChannelId} history before the morning greeting.", ChannelId);
            return false;
        }
    }
}
