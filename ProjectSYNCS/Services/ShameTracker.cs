using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

// Feeds the two earned titles on the wall of shame: "Le Malfaisant" (who is hostile,
// and to how many people) and "Le Perfide" (who keeps turning to other bots). The third
// title, "Le Banni", is voted by people through `/shame user:@…` and never passes
// through here.
//
// A service of its own rather than a branch in ChatterService, for the reason
// BotFeedbackTracker is one too: it draws a conclusion nobody tells it, and it has to
// see *every* message to do that. ChatterService returns early on a dozen branches
// (owner relay, breakdown roll, shutdown threat, mood, …), so a counter living inside
// it would silently miss most of the traffic it is supposed to be counting — and the
// misses would depend on unrelated personality tuning.
//
// **This is the second handler that looks at other bots' traffic**, after
// RivalryService — every other one bails on IsBot. It has to, because the only trace a
// slash command run against another bot leaves on the gateway is *that bot's reply*,
// which carries the invoker's id in its interaction metadata. The rival test itself is
// borrowed from RivalryService rather than rewritten, so the two can never disagree
// about who counts as a rival.
//
// Singleton, like every other gateway-facing tracker: takes IServiceProvider and opens
// _services.CreateAsyncScope() per write rather than injecting the transient
// ShameService, which would pin one AppDbContext for the process lifetime.
internal sealed class ShameTracker
{
    // One perfidy hit per person per channel per minute. Deliberately unlike
    // MeanHits, which is uncapped: hostility is rare and deserves to scale, whereas
    // talking to a bot is mundane and arrives in bursts — an evening of queueing songs
    // is forty replies to a music bot. Uncapped, this title would permanently belong to
    // whoever uses the music bot and stop moving on day two. Rationed, it measures how
    // often someone *turns to* another bot rather than how chatty that bot's UX is.
    private static readonly TimeSpan PerfidyCooldown = TimeSpan.FromSeconds(60);

    // Shouting is rationed for the same reason and on the same terms: it arrives in
    // bursts — an argument is ten shouted messages in two minutes — and uncapped the
    // title would belong permanently to whoever had one bad evening and stop moving.
    // Its own gate, not Perfidy's: they are different acts, and sharing one would let
    // a shout mute a perfidy hit for the whole window.
    private static readonly TimeSpan ShoutCooldown = TimeSpan.FromSeconds(60);

    // Entries older than this are dropped, so the gate cannot grow one key per person
    // per channel forever. Well past the cooldown, so forgetting one is never an early
    // grant. Same shape as XpTracker's and RivalryService's.
    private static readonly TimeSpan Forget = TimeSpan.FromHours(1);

    private readonly DiscordSocketClient _client;
    private readonly RivalryService _rivalry;
    // Asked rather than copied, so the excluded-channel list — hardcoded and
    // configured alike — stays defined in exactly one class.
    private readonly XpTracker _xp;
    private readonly IServiceProvider _services;
    private readonly ILogger<ShameTracker> _logger;

    private readonly object _gate = new();
    private readonly Dictionary<(ulong ChannelId, ulong UserId), DateTimeOffset> _lastPerfidy = new();
    private readonly Dictionary<(ulong ChannelId, ulong UserId), DateTimeOffset> _lastShout = new();

    public ShameTracker(
        DiscordSocketClient client,
        RivalryService rivalry,
        XpTracker xp,
        IServiceProvider services,
        ILogger<ShameTracker> logger)
    {
        _client = client;
        _rivalry = rivalry;
        _xp = xp;
        _services = services;
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Channel is not SocketGuildChannel guildChannel) return;

        var guildId = guildChannel.Guild.Id;

        // The spam channels count for nothing here either. People are hostile there as
        // a bit and try every bot going, and a wall that ranks the joke channel is a
        // wall of noise. Asked of XpTracker so the list of ids stays in one class —
        // which now means the channels an admin configured are honoured here too,
        // without this service knowing they exist.
        if (await _xp.IsChannelExcludedAsync(guildId, guildChannel)) return;

        // A rival's own message is only interesting for one thing: whether a human
        // summoned it with a slash command. Nothing else about it is anyone's shame.
        if (message.Author.IsBot)
        {
            await TrackCommandUseAsync(message, guildId);
            return;
        }

        await TrackPerfidyAsync(message, guildId);
        await TrackMeanAsync(message, guildId);
        await TrackShoutingAsync(message, guildId);
    }

    // Shouting: a message long enough to be a sentence, written almost entirely in
    // capitals. Independent of the two above — a shout can also be mean, and both
    // should count, because they are two different things to be ashamed of. It is
    // deliberately *not* short-circuited by MessageCues.ReadFeedback the way hostility
    // is: a verdict belongs to BotFeedbackTracker because that service answers it and
    // keeps the tally, whereas nothing else records how a message was delivered.
    private async Task TrackShoutingAsync(SocketUserMessage message, ulong guildId)
    {
        if (!MessageCues.IsShouting(message.Content ?? string.Empty)) return;
        if (!TryClaim(_lastShout, message.Channel.Id, message.Author.Id, ShoutCooldown)) return;

        try
        {
            await using var scope = _services.CreateAsyncScope();
            var shame = scope.ServiceProvider.GetRequiredService<ShameService>();
            await shame.AddShoutHitAsync(guildId, message.Author.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to record shouting for user {UserId} in guild {GuildId}.",
                message.Author.Id, guildId);
        }
    }

    /// <summary>
    /// Someone ran another bot's slash command. The command itself is never broadcast
    /// on the gateway — the only trace is the rival's *reply*, which carries who
    /// invoked it in <c>InteractionMetadata</c>.
    /// </summary>
    /// <remarks>
    /// Two blind spots are unavoidable and worth knowing about. An **ephemeral**
    /// response produces no message anyone else can see, so those are never counted.
    /// And an old-style prefix command (<c>!play</c>) leaves no metadata tying the
    /// bot's reply back to a person, so those are invisible too — the person's own
    /// <c>!play</c> message mentions nobody.
    /// </remarks>
    private async Task TrackCommandUseAsync(SocketUserMessage message, ulong guildId)
    {
        // The message-aware overload, not IsRival(IUser): a slash-command reply is a
        // webhook call under the hood, so a rival that defers before replying would
        // otherwise never pass the identity check at all — see RivalryService.IsRival's
        // remarks.
        if (!_rivalry.IsRival(message)) return;

        if (message.InteractionMetadata is not { } metadata) return;
        if (metadata.Type != InteractionType.ApplicationCommand) return;

        // A bot invoking another bot's command is not anyone's perfidy.
        if (metadata.User?.IsBot == true) return;

        await ClaimAndRecordAsync(guildId, message.Channel.Id, metadata.UserId);
    }

    // Talking to a rival directly: replying to one of its messages, or naming one
    // outright. One hit however many rivals are involved — turning to another bot is a
    // single act, unlike hostility, which scales with how many people it was aimed at.
    private async Task TrackPerfidyAsync(SocketUserMessage message, ulong guildId)
    {
        // The referenced message goes through the message-aware overload too, for the
        // same reason as above: replying to a rival's deferred slash-command answer must
        // still count, even though that reply is webhook-authored. A plain @mention has
        // no such message to consult — Discord resolves MentionedUsers off the guild's
        // member cache, never off webhook-wrapping — so IsRival(IUser) is the right and
        // only choice there.
        bool consorting =
            (message.ReferencedMessage is { } repliedTo && _rivalry.IsRival(repliedTo))
            || message.MentionedUsers.Any(_rivalry.IsRival);

        if (!consorting) return;

        await ClaimAndRecordAsync(guildId, message.Channel.Id, message.Author.Id);
    }

    private async Task ClaimAndRecordAsync(ulong guildId, ulong channelId, ulong userId)
    {
        if (!TryClaim(_lastPerfidy, channelId, userId, PerfidyCooldown)) return;

        try
        {
            await using var scope = _services.CreateAsyncScope();
            var shame = scope.ServiceProvider.GetRequiredService<ShameService>();
            await shame.AddPerfidyHitAsync(guildId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to record perfidy for user {UserId} in guild {GuildId}.", userId, guildId);
        }
    }

    private async Task TrackMeanAsync(SocketUserMessage message, ulong guildId)
    {
        // A verdict is not a mood — the same short-circuit ChatterService and
        // ReactionService both apply. "Bad bot" is someone rating her, and
        // BotFeedbackTracker already owns the response and the tally; counting it as
        // hostility too would file one message under two systems.
        if (MessageCues.ReadFeedback(message.Content) != FeedbackKind.None) return;

        if (MessageCues.Analyze(message.Content).Emotion != EmotionKind.Mean) return;

        var hits = CountTargets(message);
        if (hits == 0) return;

        try
        {
            await using var scope = _services.CreateAsyncScope();
            var shame = scope.ServiceProvider.GetRequiredService<ShameService>();
            await shame.AddMeanHitsAsync(guildId, message.Author.Id, hits);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to record shame for user {UserId} in guild {GuildId}.",
                message.Author.Id, guildId);
        }
    }

    /// <summary>
    /// How many people one mean message was aimed at. One hit per distinct target, and
    /// deliberately uncapped: being unpleasant to four people at once is four times as
    /// unpleasant, and the wall says so.
    /// </summary>
    /// <remarks>
    /// <para>A target is an explicit <c>@</c> mention or the author of the message this
    /// one replies to. Roles and <c>@everyone</c> are never targets — they are aimed at
    /// a channel, not at anyone, and with per-person scoring one mean <c>@everyone</c>
    /// would otherwise end the ranking permanently in a single message.</para>
    /// <para>Bots are not targets, with exactly one exception: <b>SYNCS herself is</b>.
    /// Being mean to her is the rule this title was built on, and it is still the only
    /// half of it she can vouch for first-hand. Rival bots are skipped — people are
    /// rude to bots constantly, and being rude to one is already Le Perfide's business
    /// rather than Le Malfaisant's.</para>
    /// <para>The author is never their own target, and the set is deduplicated, so
    /// replying to Bob while also tagging Bob is one hit rather than two — Discord puts
    /// the replied-to user in the mention list when the reply ping is on, and leaves
    /// them out when it isn't, so both have to be looked at and neither can be trusted
    /// to be the only source.</para>
    /// </remarks>
    private int CountTargets(SocketUserMessage message)
    {
        var targets = new HashSet<ulong>();

        foreach (var user in message.MentionedUsers)
            if (IsTarget(user, message.Author.Id))
                targets.Add(user.Id);

        if (message.ReferencedMessage?.Author is { } repliedTo && IsTarget(repliedTo, message.Author.Id))
            targets.Add(repliedTo.Id);

        return targets.Count;
    }

    private bool IsTarget(IUser user, ulong authorId) =>
        user.Id != authorId
        && (!user.IsBot || user.Id == _client.CurrentUser.Id);

    // Atomically checks one gate's per-(channel, user) cooldown and claims it. Takes the
    // dictionary rather than owning one, so each rationed behaviour keeps its own window
    // — the same shape XpTracker's TryClaim has, and for the same reason.
    private bool TryClaim(
        Dictionary<(ulong ChannelId, ulong UserId), DateTimeOffset> gate,
        ulong channelId, ulong userId, TimeSpan cooldown)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            var key = (channelId, userId);

            if (gate.TryGetValue(key, out var last) && now - last < cooldown) return false;

            gate[key] = now;
            ForgetStale(gate, now);
            return true;
        }
    }

    // Caller holds _gate. Only sweeps once the gate has grown past a size no real
    // server reaches in an hour, so the common path stays a single dictionary write.
    private static void ForgetStale(
        Dictionary<(ulong ChannelId, ulong UserId), DateTimeOffset> gate, DateTimeOffset now)
    {
        if (gate.Count < 256) return;

        var cutoff = now - Forget;
        foreach (var key in gate.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToList())
            gate.Remove(key);
    }
}
