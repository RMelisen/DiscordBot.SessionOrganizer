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

    // Channels that earn nothing, whatever happens in them — spam and the other places
    // where activity says nothing about engagement. Every signal checks this (message,
    // reaction, verdict, voice), so there is no path back in.
    private static readonly HashSet<ulong> ExcludedChannels = new()
    {
        901172450719584356,
        916655216080875551,
        1526335594165440542,
        885475859992035348,
        995433580597624923,
        1010902795207053312,
    };

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
        if (IsExcluded(guildChannel)) return;

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
    //
    // The /leaderboard reaction count is kept here too, rather than in EmoteTracker,
    // so that ExcludedChannels stays in this class alone — the same reason
    // VoiceXpService passes a channel id instead of keeping its own copy of the rule.
    public async Task HandleReactionAddedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction) => await CountAndGrantAsync(channel, reaction, +1);

    // Removing a reaction takes the count back down, so add/remove in a loop nets to
    // zero rather than inflating the ranking. No XP is involved either way: a grant
    // already made is not withdrawn, and the cooldown is deliberately untouched.
    public async Task HandleReactionRemovedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction) => await CountAndGrantAsync(channel, reaction, -1);

    private async Task CountAndGrantAsync(
        Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction, int delta)
    {
        if (reaction.UserId == _client.CurrentUser.Id) return;
        if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;

        var resolved = await channel.GetOrDownloadAsync();
        if (resolved is not IGuildChannel guildChannel) return;
        if (IsExcluded(guildChannel)) return;

        // Counted before the cooldown claim, and regardless of it: every reaction is a
        // reaction used, even the ones too soon after the last to be worth any XP.
        await CountReactionAsync(guildChannel.GuildId, reaction.UserId, delta);

        if (delta <= 0) return;

        var key = (guildChannel.GuildId, reaction.UserId);
        if (!TryClaim(_lastReactionXp, key, ReactionCooldown)) return;

        await GrantAsync(guildChannel.GuildId, reaction.UserId, ReactionXp, resolved);
    }

    private async Task CountReactionAsync(ulong guildId, ulong userId, int delta)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var xp = scope.ServiceProvider.GetRequiredService<XpService>();
            await xp.AddReactionUsedAsync(guildId, userId, delta);
        }
        catch (Exception ex)
        {
            // A leaderboard counter is a nicety; never let it stop the XP grant below.
            _logger.LogWarning(ex, "Failed to count a reaction for user {UserId}.", userId);
        }
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

        var channel = _client.GetChannel(channelId);
        if (IsExcluded(channelId, channel)) return;
        if (!TryClaim(_lastVerdictXp, (guildId, userId), VerdictCooldown)) return;

        await GrantAsync(guildId, userId, amount, channel as IMessageChannel);
    }

    /// <summary>
    /// Phase 2 entry point — the periodic voice sweep grants through here too, so
    /// announcing and level-up detection stay in one place regardless of signal.
    /// <paramref name="voiceChannelId"/> is both where the XP was earned and where the
    /// card is posted: a voice channel carries its own text chat, and that is the
    /// conversation the people who earned it are actually looking at.
    /// </summary>
    /// <remarks>
    /// The system channel remains the fallback, for the case where the voice channel
    /// cannot be resolved from the gateway cache. If it resolves but the bot may not
    /// post in its chat, the send fails and is swallowed and logged like every other
    /// Discord side effect — the XP is already recorded either way, and a celebration
    /// is never worth breaking a grant over.
    /// </remarks>
    public async Task GrantVoiceXpAsync(
        ulong guildId, ulong voiceChannelId, ulong userId, long minutes)
    {
        // Resolved once and reused: the exclusion check needs it, and so does the
        // announcement below.
        var voiceChannel = _client.GetChannel(voiceChannelId);
        if (IsExcluded(voiceChannelId, voiceChannel)) return;

        // Recorded on the same call that pays the XP, so the /leaderboard voice total
        // can never disagree with which minutes were considered eligible — and the same
        // round trip reports how much today's bucket already held, which is what the
        // taper is computed from.
        var earnedToday = await CountVoiceAsync(guildId, userId, minutes);

        // Counting failed, so there is no honest figure to taper on. Skip the payout
        // rather than guessing: this is the anti-abuse path, and the failure is logged.
        if (earnedToday is not { } before) return;

        // Falls from 5/min to 3 to 1 as the day's total grows — see VoiceXpCurve. The
        // minutes above are recorded either way: time spent is a fact, the XP is the
        // reward, and only the reward is rationed.
        var amount = VoiceXpCurve.XpForSpan(before, minutes);
        if (amount <= 0) return;

        // SocketVoiceChannel is an IMessageChannel — text-in-voice — so the card lands
        // in the channel the person is sitting in rather than interrupting #général.
        var channel = voiceChannel as IMessageChannel
                      ?? _client.GetGuild(guildId)?.SystemChannel;

        await GrantAsync(guildId, userId, amount, channel);
    }

    // Returns the minutes already banked today before this tick, or null if the write
    // failed.
    private async Task<long?> CountVoiceAsync(ulong guildId, ulong userId, long minutes)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var xp = scope.ServiceProvider.GetRequiredService<XpService>();
            return await xp.AddVoiceMinutesAsync(guildId, userId, minutes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to count voice minutes for user {UserId}.", userId);
            return null;
        }
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
            ? "SIX SEVEEEN"
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

    /// <summary>
    /// Whether nothing counts in <paramref name="channel"/> — the spam channels, and
    /// any thread inside one.
    /// </summary>
    /// <remarks>
    /// Public so <see cref="ShameTracker"/> can ask rather than keeping a second copy
    /// of the list, the same reason <c>VoiceXpService</c> passes a channel id instead
    /// of holding its own. The list stays in this class alone; only the question is
    /// shared. Note the name is about the channel, not about XP — the wall of shame
    /// honours the same exclusions for the same reason (the spam channels say nothing
    /// about anyone).
    /// </remarks>
    public static bool IsChannelExcluded(IChannel channel) => IsExcluded(channel);

    // Every entry point calls one of these *before* TryClaim, never after: a message in
    // an excluded channel must not burn the person's cooldown, or spamming there would
    // actively block them from earning in a real channel a minute later.
    private static bool IsExcluded(IChannel channel) =>
        IsExcluded(channel.Id, channel);

    private static bool IsExcluded(ulong channelId, IChannel? resolved) =>
        IsExcluded(channelId, (resolved as SocketThreadChannel)?.ParentChannel?.Id);

    // The actual decision, pure and free of gateway types so it can be checked without
    // a connection. A thread inherits its parent's exclusion — otherwise opening a
    // thread inside a spam channel would quietly be a way back in.
    private static bool IsExcluded(ulong channelId, ulong? parentId) =>
        ExcludedChannels.Contains(channelId)
        || (parentId is { } parent && ExcludedChannels.Contains(parent));

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
