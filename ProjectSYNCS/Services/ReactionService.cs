using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

// The bot's second voice: instead of answering in words, it just reacts. Far lower
// noise than a message, and it works on conversations nobody addressed to the bot,
// which is the point — it makes SYNCS feel present rather than summoned.
//
// Two separate behaviours live here:
//
//  * Reacting to a *message*. It qualifies when it reads nice / mean / like a
//    greeting (reusing MessageCues), or when the owner wrote it — he qualifies on
//    anything, which is the favouritism. Qualifying is not enough: a probability roll
//    and a per-channel cooldown keep it occasional, and the emote always comes from a
//    curated pool in BotResponses, so it is a reading of the message rather than a
//    generic acknowledgement.
//
//  * Piling on to someone else's *reaction*, copying the same emote. Odds only, no
//    cooldown — joining in is a per-reaction impulse, and rationing it per channel
//    would make it feel calculated rather than reflexive. The owner's reactions are
//    joined twice as often; nothing is ever piled on to the bot's own messages (a
//    reaction there is someone answering SYNCS, not a verdict to agree with), and
//    hostile emotes on the owner's messages are left alone.
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

    // Odds of copying a reaction someone else just added. Rolled per reaction, and
    // deliberately not subject to Cooldown — see the note at the top of the class.
    // Rodhengard's reactions are worth joining twice as often: the same favouritism
    // the rest of the personality shows him.
    private const double CopyChance = 0.10;
    private const double OwnerCopyChance = 0.20;

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

    // Someone reacted to a message — sometimes SYNCS agrees and adds the same emote.
    // No cooldown, just odds, so a lively message can collect a couple of pile-ons
    // while a quiet channel stays quiet.
    public async Task HandleReactionAddedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction)
    {
        // Never pile on to itself, and don't mirror other bots' bookkeeping marks.
        if (reaction.UserId == _client.CurrentUser.Id) return;
        if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;

        // The bot can only add an emote it shares a guild with. Unicode is always
        // fine; a custom emote pasted from someone else's server would just be
        // rejected. Checked before the roll so the odds apply to copyable reactions
        // rather than being quietly eaten by foreign emotes.
        if (!CanUse(reaction.Emote)) return;

        bool byOwner = reaction.UserId == AvailabilityService.OwnerId;
        if (Random.Shared.NextDouble() >= (byOwner ? OwnerCopyChance : CopyChance)) return;

        try
        {
            if (await channel.GetOrDownloadAsync() is not IGuildChannel) return;

            var resolved = await message.GetOrDownloadAsync();
            if (resolved is null) return;

            // Someone reacting to one of SYNCS's own messages is them answering it;
            // adding the same emote would be the bot applauding itself. Checked here
            // rather than up front because it is the only rule needing the message's
            // author, which costs a fetch.
            if (resolved.Author.Id == _client.CurrentUser.Id) return;

            // Don't help anyone dunk on Rodhengard. A hostile emote on one of his
            // messages is left alone no matter how many people pile on — the same
            // favouritism that gets him compliments instead of roasts.
            if (resolved.Author.Id == AvailabilityService.OwnerId && IsMeanEmote(reaction.Emote))
                return;

            // Already joined in on this one — adding again is a wasted call.
            if (resolved.Reactions.TryGetValue(reaction.Emote, out var meta) && meta.IsMe) return;

            await resolved.AddReactionAsync(reaction.Emote);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to copy a reaction in channel {ChannelId}.", channel.Id);
        }
    }

    // True when the emote is one the bot can actually react with: any unicode emoji,
    // or a custom emote from a guild it is in.
    private bool CanUse(IEmote emote) =>
        emote is not Emote custom || _client.Guilds.Any(g => g.Emotes.Any(e => e.Id == custom.Id));

    // Whether the emote reads as hostile — the very pool SYNCS reaches for when
    // someone writes something nasty, reused here as the definition of "mean". Adding
    // an emote there therefore does two things: the bot may use it, and it won't
    // amplify it against the owner. Compared as parsed emotes rather than as strings,
    // so a custom emote still matches on its id even if it was renamed since.
    private static bool IsMeanEmote(IEmote emote) =>
        BotResponses.MeanReactions
            .Select(ParseEmote)
            .Any(mean => mean is not null && mean.Equals(emote));

    // Which curated pool fits the message, or null to leave it alone. The emotion
    // takes precedence over the greeting for the same reason ChatterService does it:
    // a hostile "salut" is not a greeting worth waving back at.
    private static string[]? ChooseReactionPool(SocketUserMessage message)
    {
        if (message.Author.Id == AvailabilityService.OwnerId)
            return BotResponses.OwnerReactions;

        var mood = MessageCues.Analyze(message.Content ?? string.Empty);

        return mood.Emotion switch
        {
            EmotionKind.Mean => BotResponses.MeanReactions,
            EmotionKind.Nice => BotResponses.NiceReactions,
            _ => mood.IsGreeting ? BotResponses.GreetingReactions : null,
        };
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
