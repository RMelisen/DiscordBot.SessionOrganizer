using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Grants XP for talking, reacting, and interacting with the bot directly, and
// announces it when someone crosses a level. 
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

    // Bad still grants something — passing verdict on her at all is engagement — just
    // noticeably less than Good, so praise stays worth more than a complaint.
    private const long BadVerdictBonus = 15;

    private static readonly TimeSpan MessageCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ReactionCooldown = TimeSpan.FromSeconds(60);

    // Own cooldown on top of BotFeedbackTracker's one-per-person-per-action claim:
    // that claim rations *attribution* (does this verdict count at all), not time, so
    // without this a burst of unambiguous verdicts (replies to her, thumbs on several
    // of her messages) in quick succession could each grant the bonus back-to-back.
    private static readonly TimeSpan VerdictCooldown = TimeSpan.FromSeconds(30);

    // Entries older than this are dropped — well past every cooldown above, so
    // forgetting one can never hand anyone an early grant. Without it these grow one
    // entry per person per guild forever, which the other trackers already guard
    // against (RivalryService and BotFeedbackTracker both prune the same way).
    private static readonly TimeSpan Forget = TimeSpan.FromHours(1);

    // Keyed by (guild, user), not by channel — posting in two channels back-to-back
    // must not double a grant. Three independent gates, deliberately: reacting,
    // messaging, and a verdict must never block one another.
    private readonly object _gate = new();
    private readonly Dictionary<(ulong GuildId, ulong UserId), DateTimeOffset> _lastMessageXp = new();
    private readonly Dictionary<(ulong GuildId, ulong UserId), DateTimeOffset> _lastReactionXp = new();
    private readonly Dictionary<(ulong GuildId, ulong UserId), DateTimeOffset> _lastVerdictXp = new();

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
    /// Still gated by its own VerdictCooldown on top of that: BotFeedbackTracker's
    /// claim rations *attribution*, not time, so a burst of unambiguous verdicts in
    /// quick succession (several thumbs, a reply then another) could otherwise each
    /// grant the bonus back-to-back.
    /// </summary>
    public async Task GrantVerdictBonusAsync(ulong guildId, ulong channelId, ulong userId, FeedbackKind verdict)
    {
        var amount = verdict == FeedbackKind.Good ? GoodVerdictBonus : BadVerdictBonus;
        if (amount <= 0) return;
        if (!TryClaim(_lastVerdictXp, (guildId, userId), VerdictCooldown)) return;

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
                await AnnounceAsync(channel, userId, oldLevel, newLevel, knownUser);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to grant XP to user {UserId} in guild {GuildId}.", userId, guildId);
        }
    }

    // The celebration itself: a card rather than a plain line, so a level-up stands
    // out from her ordinary chatter in a busy channel.
    private async Task AnnounceAsync(IMessageChannel channel, ulong userId, int oldLevel, int newLevel, IUser? knownUser)
    {
        var user = ResolveUser(channel, userId, knownUser);
        // Couldn't resolve the member (rare cache miss) — the level is still
        // recorded, only the celebration is skipped, matching "Discord side effects
        // must never break the flow."
        if (user is null) return;

        var name = BotResponses.DisplayNameFor(userId,
            (user as SocketGuildUser)?.Nickname ?? user.GlobalName ?? user.Username);

        // 7 and 67 are a fixed line, not a pool pick — ResponsePicker is skipped
        // entirely, so the egg never burns one of that channel's exclusion slots on a
        // line that isn't actually a pool entry.
        var description = IsSixSeven(newLevel)
            ? "SIX SEVEEEEN"
            : string.Format(_picker.Pick(channel.Id, BotResponses.XpLevelUpLines), name, newLevel);

        var embed = new EmbedBuilder()
            .WithTitle(BuildLevelUpTitle(oldLevel, newLevel))
            .WithDescription(description)
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithColor(Color.Purple)
            .Build();

        await BotChat.PostEmbedWithTypingAsync(channel, embed, description, _logger, "level-up announcement");
    }

    // Shows the span crossed, not just the number landed on: one grant can cross more
    // than one threshold, and "Niveau 5 → 8 !" is the only rendering that stays honest
    // when it does.
    private static string BuildLevelUpTitle(int oldLevel, int newLevel) =>
        $"Niveau {oldLevel} → {newLevel} !";

    private static bool IsSixSeven(int level) => level is 7 or 67;

    // Where the caller already has an IUser (the message path), skip the lookup.
    // Otherwise resolve through the guild the channel belongs to. Returns the user
    // rather than just a name, since the card needs an avatar off the same object.
    private static IUser? ResolveUser(IMessageChannel channel, ulong userId, IUser? knownUser) =>
        knownUser ?? (channel as SocketGuildChannel)?.Guild.GetUser(userId);

    // Atomically checks one gate's per-(guild,user) cooldown and claims it.
    private bool TryClaim(Dictionary<(ulong, ulong), DateTimeOffset> gate, (ulong, ulong) key, TimeSpan cooldown)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (gate.TryGetValue(key, out var last) && now - last < cooldown) return false;

            gate[key] = now;
            ForgetStale(gate, now);
            return true;
        }
    }

    // Caller holds _gate. Only sweeps once a gate has grown past a size no real
    // server reaches in an hour, so the common path stays a single dictionary write.
    private static void ForgetStale(Dictionary<(ulong, ulong), DateTimeOffset> gate, DateTimeOffset now)
    {
        if (gate.Count < 256) return;

        var cutoff = now - Forget;
        foreach (var key in gate.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToList())
            gate.Remove(key);
    }
}
