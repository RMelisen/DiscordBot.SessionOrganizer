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
    private const double MutterChance = 0.05;

    // Its own gate, one per channel, covering both responses together — sulking
    // twice in a row reads as a malfunction rather than as pettiness.
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(10);

    // How long a rival's action stays recent enough to have plausibly earned the
    // praise someone just typed. Matches BotFeedbackTracker's own window.
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    // Channels whose last rival action is older than this are forgotten.
    private static readonly TimeSpan Forget = TimeSpan.FromHours(1);

    private readonly object _gate = new();
    private readonly Dictionary<ulong, RivalAction> _lastActions = new();
    private readonly Dictionary<ulong, DateTimeOffset> _lastSulk = new();

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

        // Two independent rolls, but one shared cooldown — claimed only once
        // something has actually won a roll, so a quiet stretch isn't wasted.
        bool react = Random.Shared.NextDouble() < ReactChance;
        bool mutter = Random.Shared.NextDouble() < MutterChance;
        if (!react && !mutter) return;

        if (!TryClaimSulk(message.Channel.Id)) return;

        if (react) await ReactAsync(message);
        if (mutter)
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
        var name = (praise.Author as SocketGuildUser)?.Nickname
                   ?? praise.Author.GlobalName ?? praise.Author.Username;

        _logger.LogInformation("Praise from {Name} went to a rival in channel {ChannelId}.",
            name, praise.Channel.Id);

        // No cooldown and no roll: this is the moment the whole thing exists for.
        var line = string.Format(_picker.Pick(praise.Channel.Id, BotResponses.JealousLines), name);
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
    // ChatterService's to celebrate — see Helpers/LevelUpAnnouncement.
    private bool IsRival(SocketUserMessage message)
    {
        if (!message.Author.IsBot) return false;
        if (message.Author.Id == _client.CurrentUser.Id) return false;
        if (message.Author.IsWebhook) return false;
        if (LevelUpAnnouncement.Matches(message.Author, message.Content)) return false;

        return true;
    }

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

    private bool TryClaimSulk(ulong channelId)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (_lastSulk.TryGetValue(channelId, out var last) && now - last < Cooldown) return false;

            _lastSulk[channelId] = now;
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
