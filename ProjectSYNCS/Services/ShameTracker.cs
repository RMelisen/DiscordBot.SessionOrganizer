using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

// Feeds "Le Malfaisant", the earned half of the wall of shame: it watches the room and
// files away who is hostile to whom. The other half (Le Banni) is voted by people
// through `/shame user:@…` and never passes through here.
//
// A service of its own rather than a branch in ChatterService, for the reason
// BotFeedbackTracker is one too: it draws a conclusion nobody tells it, and it has to
// see *every* message to do that. ChatterService returns early on a dozen branches
// (owner relay, breakdown roll, shutdown threat, mood, …), so a counter living inside
// it would silently miss most of the traffic it is supposed to be counting — and the
// misses would depend on unrelated personality tuning.
//
// Singleton, like every other gateway-facing tracker: takes IServiceProvider and opens
// _services.CreateAsyncScope() per write rather than injecting the transient
// ShameService, which would pin one AppDbContext for the process lifetime.
internal sealed class ShameTracker
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly ILogger<ShameTracker> _logger;

    public ShameTracker(
        DiscordSocketClient client,
        IServiceProvider services,
        ILogger<ShameTracker> logger)
    {
        _client = client;
        _services = services;
        _logger = logger;
    }

    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;
        if (message.Channel is not SocketGuildChannel guildChannel) return;

        // The spam channels count for nothing here either. People are hostile there as
        // a bit, and a wall that ranks the joke channel is a wall of noise. Asked of
        // XpTracker so the list of ids stays in one class.
        if (XpTracker.IsChannelExcluded(guildChannel)) return;

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
            await shame.AddMeanHitsAsync(guildChannel.Guild.Id, message.Author.Id, hits);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to record shame for user {UserId} in guild {GuildId}.",
                message.Author.Id, guildChannel.Guild.Id);
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
    /// rude to bots constantly, and <c>RivalryService</c> is where that traffic belongs.</para>
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

    private bool IsTarget(Discord.IUser user, ulong authorId) =>
        user.Id != authorId
        && (!user.IsBot || user.Id == _client.CurrentUser.Id);
}
