using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>What a rival bot last did in a channel, and where.</summary>
internal readonly record struct RivalAction(DateTimeOffset At, ulong MessageId, ulong AuthorId);

// She does not like sharing a server. This is the only handler in the fan-out that
// looks at *other* bots' traffic — everything else bails on IsBot — and it does two
// separate jobs with what it sees.
//
// **It remembers who acted last.** BotFeedbackTracker asks, so that a bare
// "good bot" goes to whoever most recently did something rather than always to her.
// That fixes a real mis-attribution: the window only ever knew when *she* acted, so
// praise earned by another bot landed in her column whenever she had spoken in the
// last five minutes.
//
// **It sulks.** A rival posting earns a reaction now and then, and much more rarely
// a muttered line. Rolled per message and rationed by a per-channel cooldown of its
// own — deliberately not ReactionService's, which gates a different trigger
// population entirely, and whose comment says as much.
//
// Webhooks are excluded: they are relentless and they are nobody's rival. The
// level-up bot's *level-up announcements* are excluded too, because ChatterService
// congratulates those and the congratulation is aimed at the person who levelled;
// cheering and sulking at one message would be incoherent. The rest of that bot's
// traffic is fair game.
internal sealed class RivalryService
{
    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<RivalryService> _logger;

    // Odds a rival's message earns a reaction, and the rarer odds it earns a line.
    // Rolled independently, so a message very occasionally gets both.
    private const double ReactChance = 0.15;
    private const double MutterChance = 0.08;

    // Two gates, one per channel each, because the two responses are not the same
    // level of intrusion. A reaction is silent and can be frequent; a muttered line
    // is her *talking in the channel* and has to stay rare, or the sulking becomes
    // the loudest thing in the room. Sharing one gate made them compete: a silent
    // reaction locked out the line for the whole window, and vice versa.
    //
    // These, not the odds, are what govern how often she is actually seen — a chatty
    // rival generates far more rolls than either gate will let through.
    private static readonly TimeSpan ReactCooldown = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan MutterCooldown = TimeSpan.FromMinutes(5);

    // How long a rival's action stays recent enough to have plausibly earned the
    // praise someone just typed. Matches BotFeedbackTracker's own window.
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    // Channels whose last rival action is older than this are forgotten.
    private static readonly TimeSpan Forget = TimeSpan.FromHours(1);

    private readonly object _gate = new();
    private readonly Dictionary<ulong, RivalAction> _lastActions = new();
    private readonly Dictionary<ulong, DateTimeOffset> _lastReaction = new();
    private readonly Dictionary<ulong, DateTimeOffset> _lastMutter = new();

    public RivalryService(
        DiscordSocketClient client,
        ResponsePicker picker,
        ILogger<RivalryService> logger)
    {
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Channel is not SocketGuildChannel) return;
        if (!IsRival(message)) return;

        RecordAction(message.Channel.Id, message.Id, message.Author.Id);

        // Two independent rolls against two independent gates. Each claims its own
        // only after winning its own roll, so a losing roll never burns the other's
        // window and the silent reaction can never mute the line.
        if (Random.Shared.NextDouble() < ReactChance
            && TryClaim(_lastReaction, message.Channel.Id, ReactCooldown))
        {
            await ReactAsync(message);
        }

        if (Random.Shared.NextDouble() < MutterChance
            && TryClaim(_lastMutter, message.Channel.Id, MutterCooldown))
        {
            var line = _picker.Pick(message.Channel.Id, BotResponses.RivalMutters);
            await BotChat.ReplyWithTypingAsync(message, line, _logger, "rival mutter");
        }
    }

    /// <summary>
    /// Someone praised a rival in front of her. <paramref name="rivalMessageId"/> is
    /// the message being praised when that is known — a reply names it outright,
    /// otherwise the rival's last message is the best guess.
    /// </summary>
    public async Task OnPraiseStolenAsync(SocketUserMessage praise, ulong? rivalMessageId)
    {
        var name = BotResponses.DisplayNameFor(praise.Author.Id,
            (praise.Author as SocketGuildUser)?.Nickname ?? praise.Author.GlobalName ?? praise.Author.Username);

        bool byOwner = praise.Author.Id == AvailabilityService.OwnerId;

        _logger.LogInformation("Praise from {Name}{Owner} went to a rival in channel {ChannelId}.",
            name, byOwner ? " (the owner)" : "", praise.Channel.Id);

        // No cooldown and no roll: this is the moment the whole thing exists for. Her
        // creator doing it is a different injury from anyone else doing it — everyone
        // else gets wounded pride, he gets betrayal.
        var pool = byOwner ? BotResponses.JealousLinesOwner : BotResponses.JealousLines;
        var line = string.Format(_picker.Pick(praise.Channel.Id, pool), name);
        await BotChat.ReplyWithTypingAsync(praise, line, _logger, "jealous reply");

        var target = rivalMessageId ?? LastAction(praise.Channel.Id)?.MessageId;
        if (target is null) return;

        try
        {
            if (await praise.Channel.GetMessageAsync(target.Value) is IUserMessage rival)
                await MarkAsync(rival);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mark a rival's message in channel {ChannelId}.", praise.Channel.Id);
        }
    }

    /// <summary>The last thing a rival did here, if it is recent enough to matter.</summary>
    public RivalAction? LastAction(ulong channelId)
    {
        lock (_gate)
        {
            if (!_lastActions.TryGetValue(channelId, out var action)) return null;
            return DateTimeOffset.UtcNow - action.At > Window ? null : action;
        }
    }

    // Any bot but herself. Webhooks are not rivals, and a level-up announcement is
    // ChatterService's to answer — see Helpers/LevelUpAnnouncement. It sulks at those
    // too now, but in a single line that also congratulates the person; letting this
    // service add a reaction and a mutter on top would be three responses to one
    // message rather than a mood.
    private bool IsRival(SocketUserMessage message)
    {
        if (!IsRival((IUserMessage)message)) return false;
        if (LevelUpAnnouncement.Matches(message.Author, message.Content)) return false;

        return true;
    }

    /// <summary>
    /// Whether <paramref name="user"/> is a rival bot — any bot but herself, webhooks
    /// excluded (they are relentless and they are nobody's rival).
    /// </summary>
    /// <remarks>
    /// The identity half of the check above, without the message-content carve-out for
    /// level-up announcements. Public so <see cref="ShameTracker"/> can ask rather than
    /// writing a second definition of "rival" that would drift from this one.
    /// <para><b>The carve-out is deliberately not part of this.</b> Sulking at a
    /// level-up announcement would contradict <c>ChatterService</c> congratulating it in
    /// the same breath, which is why the message-level check drops those. Counting
    /// someone who *replies* to one is a different question — she is jealous of the
    /// attention either way — so "Le Perfide" uses this overload and gets no
    /// exception.</para>
    /// <para><b>Blind to interaction responses, and conservatively so.</b> Responding to
    /// an interaction is itself a webhook call under the hood, so Discord builds the
    /// author of an interaction reply — even the bot's own, and especially a deferred
    /// one, which always goes out through the followup webhook — as a webhook user. This
    /// overload cannot tell that apart from a genuine third-party webhook (GitHub,
    /// IFTTT, …), and genuine webhooks carry <c>IsBot</c> too — Discord shows them the
    /// same "BOT" tag — so neither flag alone distinguishes the two. Prefer
    /// <see cref="IsRival(IUserMessage)"/> whenever a message is available; it is the
    /// one that can actually tell.</para>
    /// </remarks>
    public bool IsRival(IUser user) =>
        IsRivalAuthor(user.IsBot, user.Id == _client.CurrentUser.Id, user.IsWebhook, hasInteractionMetadata: false);

    /// <summary>
    /// Whether <paramref name="message"/> was authored by a rival bot. The message-aware
    /// counterpart to <see cref="IsRival(IUser)"/>, and the one to reach for whenever a
    /// message is on hand.
    /// </summary>
    /// <remarks>
    /// <see cref="IUserMessage.InteractionMetadata"/> is the actual signal a plain
    /// <see cref="IUser"/> cannot see: it is present only on a message that was created
    /// in response to an interaction, never on a genuine incoming webhook post. So a
    /// rival's own slash-command reply — webhook-authored though it is — still counts
    /// here, while an unrelated GitHub/IFTTT-style webhook still does not. This is what
    /// was silently excluding every rival that defers before replying: her reaction,
    /// her mutter, and "Le Perfide"'s command-use credit all went missing for exactly
    /// the bots that take long enough to need <c>DeferAsync</c>.
    /// </remarks>
    public bool IsRival(IUserMessage message) =>
        IsRivalAuthor(message.Author.IsBot, message.Author.Id == _client.CurrentUser.Id,
            message.Author.IsWebhook, message.InteractionMetadata is not null);

    /// <summary>
    /// The actual decision, pure and Discord-free so it can be checked without a
    /// gateway. A webhook-flagged author only counts as a rival when interaction
    /// metadata says the "webhook" is really an application answering a slash command;
    /// otherwise it is treated as a genuine third-party webhook (GitHub, IFTTT, …),
    /// which carries the same <c>IsBot</c> flag and would otherwise be indistinguishable
    /// from a real rival.
    /// </summary>
    private static bool IsRivalAuthor(bool isBot, bool isSelf, bool isWebhook, bool hasInteractionMetadata) =>
        isBot && !isSelf && (hasInteractionMetadata || !isWebhook);

    private Task ReactAsync(SocketUserMessage message) => MarkAsync(message);

    private async Task MarkAsync(IUserMessage message)
    {
        var emote = EmoteMarkup.Parse(_picker.Pick(message.Channel.Id, BotResponses.MeanReactions));
        if (emote is null) return;

        try
        {
            await message.AddReactionAsync(emote);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to react to a rival in channel {ChannelId}.", message.Channel.Id);
        }
    }

    private void RecordAction(ulong channelId, ulong messageId, ulong authorId)
    {
        lock (_gate)
        {
            _lastActions[channelId] = new RivalAction(DateTimeOffset.UtcNow, messageId, authorId);
            ForgetStale();
        }
    }

    // Atomically checks one gate's per-channel cooldown and claims it. Both gates
    // share _gate so a single lock still covers all of this service's state.
    private bool TryClaim(Dictionary<ulong, DateTimeOffset> gate, ulong channelId, TimeSpan cooldown)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (gate.TryGetValue(channelId, out var last) && now - last < cooldown) return false;

            gate[channelId] = now;
            return true;
        }
    }

    // Caller holds _gate.
    private void ForgetStale()
    {
        if (_lastActions.Count < 64) return;

        var cutoff = DateTimeOffset.UtcNow - Forget;
        foreach (var id in _lastActions.Where(kv => kv.Value.At < cutoff).Select(kv => kv.Key).ToList())
            _lastActions.Remove(id);
    }
}
