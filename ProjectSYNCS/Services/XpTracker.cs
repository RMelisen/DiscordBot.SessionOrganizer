using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Grants XP for talking, reacting, and interacting with the bot directly, and
// announces it when someone crosses a level. Runs entirely independently of the
// server's other leveling bot (see Helpers/LevelUpAnnouncement) — same vocabulary
// ("niveau"), no shared state, no cross-reference between the two.
//
// Singleton, like every other gateway-facing tracker: takes IServiceProvider and
// opens _services.CreateAsyncScope() per grant rather than injecting XpService
// directly, since pinning a transient DB service to a singleton would keep one
// AppDbContext alive for the process lifetime.
internal sealed class XpTracker
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly ResponsePicker _picker;
    private readonly ILogger<XpTracker> _logger;

    // Tuning knobs, not load-bearing — easy to retune without touching the logic
    // that uses them.
    private const long MessageXp = 20;
    private const long ReactionXp = 5;
    private const long BotInteractionBonus = 15;
    private const long GoodVerdictBonus = 25;

    // Rewarding "bad bot" with XP would read as paying someone for negativity — Good
    // is the only verdict that grants anything here.
    private const long BadVerdictBonus = 0;

    private static readonly TimeSpan MessageCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ReactionCooldown = TimeSpan.FromSeconds(60);

    // Keyed by (guild, user), not by channel — posting in two channels back-to-back
    // must not double a grant. Two independent gates, deliberately: reacting must
    // never block a message from counting, or vice versa.
    private readonly object _gate = new();
    private readonly Dictionary<(ulong GuildId, ulong UserId), DateTimeOffset> _lastMessageXp = new();
    private readonly Dictionary<(ulong GuildId, ulong UserId), DateTimeOffset> _lastReactionXp = new();

    public XpTracker(
        DiscordSocketClient client,
        IServiceProvider services,
        ResponsePicker picker,
        ILogger<XpTracker> logger)
    {
        _client = client;
        _services = services;
        _picker = picker;
        _logger = logger;
    }

    // Message XP and the bot-interaction bonus ride the same grant, gated by the same
    // cooldown — a reply to her or a mention is just a better version of talking.
    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;
        if (message.Channel is not SocketGuildChannel guildChannel) return;

        var key = (guildChannel.Guild.Id, message.Author.Id);
        if (!TryClaim(_lastMessageXp, key, MessageCooldown)) return;

        var amount = MessageXp;
        bool toHer = message.ReferencedMessage?.Author.Id == _client.CurrentUser.Id
                    || message.MentionedUsers.Any(u => u.Id == _client.CurrentUser.Id);
        if (toHer) amount += BotInteractionBonus;

        await GrantAsync(guildChannel.Guild.Id, message.Author.Id, amount, message.Channel, message.Author);
    }

    // Reaction XP: any message, any emote, its own cooldown. Mirrors EmoteTracker's
    // guard order — cheapest checks first.
    public async Task HandleReactionAddedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction)
    {
        if (reaction.UserId == _client.CurrentUser.Id) return;
        if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;

        var resolved = await channel.GetOrDownloadAsync();
        if (resolved is not IGuildChannel guildChannel) return;

        var key = (guildChannel.GuildId, reaction.UserId);
        if (!TryClaim(_lastReactionXp, key, ReactionCooldown)) return;

        await GrantAsync(guildChannel.GuildId, reaction.UserId, ReactionXp, resolved);
    }

    /// <summary>
    /// Called by BotFeedbackTracker immediately after a Good/Bad verdict is genuinely
    /// recorded — never independently re-detected here, or the bonus would be
    /// farmable by repeating "good bot" with nothing for her to have actually done.
    /// No cooldown of its own: BotFeedbackTracker's own claim already rations this.
    /// </summary>
    public async Task GrantVerdictBonusAsync(ulong guildId, ulong channelId, ulong userId, FeedbackKind verdict)
    {
        var amount = verdict == FeedbackKind.Good ? GoodVerdictBonus : BadVerdictBonus;
        if (amount <= 0) return;

        var channel = _client.GetChannel(channelId) as IMessageChannel;
        await GrantAsync(guildId, userId, amount, channel);
    }

    /// <summary>
    /// Phase 2 entry point — the periodic voice sweep grants through here too, so
    /// announcing and level-up detection stay in one place regardless of signal.
    /// </summary>
    public Task GrantVoiceXpAsync(ulong guildId, ulong userId, long amount)
    {
        var channel = _client.GetGuild(guildId)?.SystemChannel;
        return GrantAsync(guildId, userId, amount, channel);
    }

    // The shared core every signal funnels through: writes the XP, and if it crossed
    // a level, announces the final level reached — never one message per level on a
    // multi-level jump.
    private async Task GrantAsync(
        ulong guildId, ulong userId, long amount, IMessageChannel? channel, IUser? knownUser = null)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var xp = scope.ServiceProvider.GetRequiredService<XpService>();
            var (oldLevel, newLevel) = await xp.AddXpAsync(guildId, userId, amount);

            if (newLevel > oldLevel && channel is not null)
                await AnnounceAsync(channel, userId, newLevel, knownUser);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to grant XP to user {UserId} in guild {GuildId}.", userId, guildId);
        }
    }

    private async Task AnnounceAsync(IMessageChannel channel, ulong userId, int level, IUser? knownUser)
    {
        var name = ResolveName(channel, userId, knownUser);
        // Couldn't resolve the member (rare cache miss) — the level is still
        // recorded, only the celebration is skipped, matching "Discord side effects
        // must never break the flow."
        if (name is null) return;

        var line = string.Format(_picker.Pick(channel.Id, BotResponses.XpLevelUpLines), name, level);
        await BotChat.PostWithTypingAsync(channel, line, _logger, "level-up announcement");
    }

    // Where the caller already has an IUser (the message path), skip the lookup.
    // Otherwise resolve through the guild the channel belongs to.
    private static string? ResolveName(IMessageChannel channel, ulong userId, IUser? knownUser)
    {
        if (knownUser is not null)
            return BotResponses.DisplayNameFor(userId,
                (knownUser as SocketGuildUser)?.Nickname ?? knownUser.GlobalName ?? knownUser.Username);

        var guildUser = (channel as SocketGuildChannel)?.Guild.GetUser(userId);
        return guildUser is null
            ? null
            : BotResponses.DisplayNameFor(userId, guildUser.Nickname ?? guildUser.GlobalName ?? guildUser.Username);
    }

    // Atomically checks one gate's per-(guild,user) cooldown and claims it.
    private bool TryClaim(Dictionary<(ulong, ulong), DateTimeOffset> gate, (ulong, ulong) key, TimeSpan cooldown)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (gate.TryGetValue(key, out var last) && now - last < cooldown) return false;

            gate[key] = now;
            return true;
        }
    }
}
