using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

// The bot's second voice: instead of answering in words, it just reacts. Far lower
// noise than a message, and it works on conversations nobody addressed to the bot,
// which is the point — it makes SYNCS feel present rather than summoned.
//
// A message qualifies when it reads nice / mean / like a greeting (reusing
// MessageCues), or when the owner wrote it — he qualifies on anything, which is the
// favouritism. Qualifying is not enough: a probability roll and a per-channel
// cooldown keep it occasional. The emote always comes from a curated pool in
// BotResponses, so every reaction is a reading of the message rather than a generic
// acknowledgement.
internal sealed class ReactionService
{
    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly BreakdownService _breakdown;
    private readonly ILogger<ReactionService> _logger;

    // At most one reaction per channel per cooldown, and only this often among the
    // messages that qualify. The two together, rather than either alone, are what
    // keep it from either spamming a busy channel or feeling absent from a quiet one.
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(10);
    private const double ReactChance = 0.20;

    private readonly object _gate = new();
    private readonly Dictionary<ulong, DateTimeOffset> _lastReaction = new();

    public ReactionService(
        DiscordSocketClient client,
        ResponsePicker picker,
        BreakdownService breakdown,
        ILogger<ReactionService> logger)
    {
        _client = client;
        _picker = picker;
        _breakdown = breakdown;
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;
        // Reactions are a guild thing; a DM already has the bot's full attention.
        if (message.Channel is not SocketGuildChannel) return;

        // Anything aimed at the bot belongs to ChatterService, which answers it in
        // words. A reaction on top of a comeback is piling on.
        if (message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id)) return;
        if (message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id) return;

        // Never decorate a breakdown in progress.
        if (_breakdown.IsActive(message.Channel.Id)) return;

        var pool = ChooseReactionPool(message);
        if (pool is null) return;

        if (Random.Shared.NextDouble() >= ReactChance) return;

        // Claimed before sending, so two messages arriving at once can't both react.
        // A wasted claim (missing permission, unusable emote) costs one cooldown,
        // which also stops us hammering a channel we can't react in.
        if (!TryClaimChannel(message.Channel.Id)) return;

        var emote = ParseEmote(_picker.Pick(message.Channel.Id, pool));
        if (emote is null) return;

        try
        {
            await message.AddReactionAsync(emote);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to react in channel {ChannelId}.", message.Channel.Id);
        }
    }

    // Which curated pool fits the message, or null to leave it alone. Mean is tested
    // before nice for the same reason ChatterService does it: one nasty word cancels
    // the kind reading.
    private static string[]? ChooseReactionPool(SocketUserMessage message)
    {
        if (message.Author.Id == AvailabilityService.OwnerId)
            return BotResponses.OwnerReactions;

        var content = message.Content ?? string.Empty;

        if (MessageCues.IsMean(content)) return BotResponses.MeanReactions;
        if (MessageCues.IsNice(content)) return BotResponses.NiceReactions;
        if (MessageCues.IsGreeting(content)) return BotResponses.GreetingReactions;

        return null;
    }

    // Custom emotes arrive as markup; everything else is a literal unicode emoji.
    private static IEmote? ParseEmote(string markup)
    {
        if (markup.Length == 0) return null;
        return Emote.TryParse(markup, out var custom) ? custom : new Emoji(markup);
    }

    // Atomically checks the per-channel cooldown and claims it.
    private bool TryClaimChannel(ulong channelId)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (_lastReaction.TryGetValue(channelId, out var last) && now - last < Cooldown)
                return false;

            _lastReaction[channelId] = now;
            return true;
        }
    }
}
