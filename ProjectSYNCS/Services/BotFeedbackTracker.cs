using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Watches for people passing verdict on the bot — "good bot" / "bad bot" — and
// keeps the tally. Praise earns a wordless reaction; a scolding earns a reply,
// because being told off is worth answering and being praised is not worth a
// paragraph.
//
// It learns what she did by watching **her own** traffic on the gateway:
// MessageReceived and ReactionAdded both fire for the bot itself, so neither
// ChatterService nor ReactionService has to tell it anything. That is the whole
// reason this is a separate service rather than a branch inside either of them.
//
// Attribution has two routes. A **reply** to one of her messages is unambiguous and
// always counts. A **plain** message counts only if she acted in that channel within
// Window — otherwise "good bot" aimed at one of the server's other bots would land
// in her column. Either way it is one verdict per person per thing she did, so
// holding down Enter after a single joke counts once.
//
// Singleton: it holds the per-channel attribution state. Like every other bit of
// personality state that state is in memory and resets on restart, which costs at
// most one re-counted verdict per person per channel.
internal sealed class BotFeedbackTracker
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly ResponsePicker _picker;
    private readonly ILogger<BotFeedbackTracker> _logger;

    // How long after she acts a bare "good bot" still reads as being about her.
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    // Channels whose last action is older than this are forgotten, so a long-lived
    // process doesn't accumulate an entry per channel it has ever spoken in.
    private static readonly TimeSpan Forget = TimeSpan.FromHours(1);

    // The last thing she did in a channel, and who has already judged it.
    private sealed class LastAction
    {
        public DateTimeOffset At { get; init; }
        public HashSet<ulong> Judged { get; } = new();
    }

    private readonly object _gate = new();
    private readonly Dictionary<ulong, LastAction> _lastActions = new();

    public BotFeedbackTracker(
        DiscordSocketClient client,
        IServiceProvider services,
        ResponsePicker picker,
        ILogger<BotFeedbackTracker> logger)
    {
        _client = client;
        _services = services;
        _picker = picker;
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Channel is not SocketGuildChannel guildChannel) return;

        // Her own message: that is an action people can react to. Recorded before
        // the author check below returns, which is the point of watching herself.
        if (message.Author.Id == _client.CurrentUser.Id)
        {
            RecordAction(message.Channel.Id);
            return;
        }

        if (message.Author.IsBot) return;

        var verdict = MessageCues.ReadFeedback(message.Content ?? string.Empty);
        if (verdict == FeedbackKind.None) return;

        bool isReplyToBot = message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id;
        if (!TryClaim(message.Channel.Id, message.Author.Id, isReplyToBot)) return;

        await RecordAsync(guildChannel.Guild.Id, message.Author.Id, verdict);
        await RespondAsync(message, verdict);
    }

    // Her own reaction is an action too — it is the other half of "talked or
    // reacted". Everyone else's reactions are none of this service's business.
    public Task HandleReactionAddedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction)
    {
        if (reaction.UserId == _client.CurrentUser.Id) RecordAction(channel.Id);
        return Task.CompletedTask;
    }

    private void RecordAction(ulong channelId)
    {
        lock (_gate)
        {
            // A fresh action, so everyone gets to judge this one too.
            _lastActions[channelId] = new LastAction { At = DateTimeOffset.UtcNow };
            Forget_Stale();
        }
    }

    // Checks attribution and claims this person's one verdict on the current action,
    // atomically so two messages arriving together can't both count.
    private bool TryClaim(ulong channelId, ulong userId, bool isReplyToBot)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;

            if (!_lastActions.TryGetValue(channelId, out var action))
            {
                // Nothing on record — she hasn't acted here, or the process restarted.
                // A reply to one of her messages is still unambiguous, so seed an
                // action for it; a bare "good bot" gets nothing to attach to.
                if (!isReplyToBot) return false;

                action = new LastAction { At = now };
                _lastActions[channelId] = action;
            }
            else if (!isReplyToBot && now - action.At > Window)
            {
                return false;
            }

            return action.Judged.Add(userId);
        }
    }

    // Caller holds _gate.
    private void Forget_Stale()
    {
        if (_lastActions.Count < 64) return;

        var cutoff = DateTimeOffset.UtcNow - Forget;
        foreach (var id in _lastActions.Where(kv => kv.Value.At < cutoff).Select(kv => kv.Key).ToList())
            _lastActions.Remove(id);
    }

    private async Task RecordAsync(ulong guildId, ulong userId, FeedbackKind verdict)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var feedback = scope.ServiceProvider.GetRequiredService<BotFeedbackService>();
            await feedback.AddAsync(guildId, userId, verdict);

            _logger.LogInformation("{Verdict} bot recorded for user {UserId} in guild {GuildId}.",
                verdict, userId, guildId);
        }
        catch (Exception ex)
        {
            // The tally is a nicety; never let it break the reply below.
            _logger.LogWarning(ex, "Failed to record a {Verdict} bot verdict.", verdict);
        }
    }

    private async Task RespondAsync(SocketUserMessage message, FeedbackKind verdict)
    {
        bool byOwner = message.Author.Id == AvailabilityService.OwnerId;

        if (verdict == FeedbackKind.Good)
        {
            // Wordless on purpose: praise is worth acknowledging, not worth a
            // paragraph. Her creator gets devotion rather than a generic heart.
            var pool = byOwner ? BotResponses.OwnerReactions : BotResponses.NiceReactions;
            var emote = EmoteMarkup.Parse(_picker.Pick(message.Channel.Id, pool));
            if (emote is null) return;

            try
            {
                await message.AddReactionAsync(emote);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to react to a good-bot in channel {ChannelId}.", message.Channel.Id);
            }
            return;
        }

        // Being told off *is* worth answering. Everyone gets indignation; the one
        // person whose opinion she actually cares about gets to hurt her.
        var lines = byOwner ? BotResponses.BadBotRepliesOwner : BotResponses.BadBotReplies;
        var name = (message.Author as SocketGuildUser)?.Nickname
                   ?? message.Author.GlobalName ?? message.Author.Username;

        var line = string.Format(_picker.Pick(message.Channel.Id, lines), name);
        await BotChat.ReplyWithTypingAsync(message, line, _logger, "bad-bot reply");
    }
}
