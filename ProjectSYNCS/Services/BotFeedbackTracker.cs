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
// A verdict arrives one of three ways. A **reply** to one of her messages and a
// **👍 / 👎 on** one of her messages are both unambiguous and always count — the
// second one physically attached to the very thing being judged. A **plain** message
// counts only if she acted in that channel within Window, otherwise "good bot" aimed
// at one of the server's other bots would land in her column.
//
// All three share one claim, so it is one verdict per person per thing she did:
// holding down Enter after a single joke counts once, and so does thumbing up twenty
// of her old messages in a row.
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

    // The reactions that read as a verdict. Custom emotes are deliberately excluded:
    // she reacts with the server's own emotes herself, and people paste them for all
    // sorts of reasons, whereas nobody adds a thumbs-down to be friendly.
    private const string ThumbsUp = "👍";
    private const string ThumbsDown = "👎";

    // The last thing she did in a channel, and who has already judged it.
    private sealed class LastAction
    {
        public DateTimeOffset At { get; init; }
        public HashSet<ulong> Judged { get; } = new();
    }

    private readonly object _gate = new();
    private readonly Dictionary<ulong, LastAction> _lastActions = new();

    // Messages she has just reacted to in acknowledgement of a verdict. Her own
    // reaction comes back on the gateway indistinguishable from any other, and
    // recording it as a fresh action would clear Judged and hand everyone a free
    // second verdict — praise her, get thanked, praise her again, forever. So the
    // acknowledgement is remembered here and skipped when it echoes back. Bounded
    // FIFO, like every other bit of personality state.
    private const int AcknowledgedCap = 200;
    private readonly HashSet<ulong> _acknowledged = new();
    private readonly Queue<ulong> _acknowledgedOrder = new();

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
        if (!TryClaim(message.Channel.Id, message.Author.Id, unambiguous: isReplyToBot)) return;

        await RecordAsync(guildChannel.Guild.Id, message.Author.Id, verdict);
        await RespondAsync(message, verdict);
    }

    // Two things happen here. Her own reaction is an action people can judge — the
    // other half of "talked or reacted". Everyone else's is a verdict if it is a
    // thumb on one of her messages, and nothing otherwise.
    //
    // Nothing is ever sent back on this path, unlike the typed verdicts. A reaction is
    // a quiet vote, and a 👎 on something she said an hour ago would otherwise fire an
    // indignant comeback into a conversation that had long since moved on.
    public async Task HandleReactionAddedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction)
    {
        if (reaction.UserId == _client.CurrentUser.Id)
        {
            if (!WasAcknowledgement(message.Id)) RecordAction(channel.Id);
            return;
        }

        // Cheapest test first: the emote is on hand, while everything below may cost
        // a fetch, and the overwhelming majority of reactions are not thumbs.
        var verdict = ReadReaction(reaction.Emote);
        if (verdict == FeedbackKind.None) return;

        // Best-effort, like ReactionService: the user is not always populated, and
        // resolving one just to check the flag is not worth a call.
        if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;

        try
        {
            // The tally is guild-scoped, so a reaction in a DM has nowhere to go.
            if (await channel.GetOrDownloadAsync() is not IGuildChannel guildChannel) return;

            var resolved = await message.GetOrDownloadAsync();
            if (resolved is null || resolved.Author.Id != _client.CurrentUser.Id) return;

            // Only the things she *says*. A card is command output, and a 👍 on a
            // session or poll card reads as "I'm in", not as praise. Buttons are what
            // tells the two apart — every card carries them and no line of chatter
            // does. Embeds would be the wrong test: Discord attaches one to any
            // message containing a link, so a chatty message could grow one by itself.
            if (resolved.Components.Count > 0) return;

            // Attached to the message it judges, so it needs no attribution window —
            // unambiguous in exactly the way a reply is. It still goes through the
            // claim, which is what stops someone thumbing their way down her backlog.
            if (!TryClaim(channel.Id, reaction.UserId, unambiguous: true)) return;

            await RecordAsync(guildChannel.GuildId, reaction.UserId, verdict);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read a thumb verdict in channel {ChannelId}.", channel.Id);
        }
    }

    // Which verdict a reaction carries, if any. Skin-tone variants append a modifier
    // to the base code point, so 👍🏽 has to match by prefix rather than by equality.
    private static FeedbackKind ReadReaction(IEmote emote)
    {
        if (emote is not Emoji emoji) return FeedbackKind.None;

        if (emoji.Name.StartsWith(ThumbsUp, StringComparison.Ordinal)) return FeedbackKind.Good;
        if (emoji.Name.StartsWith(ThumbsDown, StringComparison.Ordinal)) return FeedbackKind.Bad;
        return FeedbackKind.None;
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
    //
    // unambiguous: the verdict is attached to one of her messages — a reply to it, or
    // a thumb on it — rather than being a bare line of chat that has to be attributed
    // by timing.
    private bool TryClaim(ulong channelId, ulong userId, bool unambiguous)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;

            if (!_lastActions.TryGetValue(channelId, out var action))
            {
                // Nothing on record — she hasn't acted here, or the process restarted.
                // Something attached to one of her messages is still unambiguous, so
                // seed an action for it; a bare "good bot" gets nothing to attach to.
                if (!unambiguous) return false;

                action = new LastAction { At = now };
                _lastActions[channelId] = action;
            }
            else if (!unambiguous && now - action.At > Window)
            {
                return false;
            }

            return action.Judged.Add(userId);
        }
    }

    // Notes that her next reaction on this message is an acknowledgement rather than
    // an action. Claimed before the reaction is sent, since the gateway echo races it.
    private void MarkAcknowledged(ulong messageId)
    {
        lock (_gate)
        {
            if (!_acknowledged.Add(messageId)) return;

            _acknowledgedOrder.Enqueue(messageId);
            if (_acknowledgedOrder.Count > AcknowledgedCap)
                _acknowledged.Remove(_acknowledgedOrder.Dequeue());
        }
    }

    private bool WasAcknowledgement(ulong messageId)
    {
        lock (_gate) return _acknowledged.Contains(messageId);
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

            // Before sending, not after: the gateway echoes the reaction back and the
            // two race. A mark left behind by a failed reaction costs nothing.
            MarkAcknowledged(message.Id);

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
