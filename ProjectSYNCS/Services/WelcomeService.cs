using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// She greets whoever joins the server, the way a member would: as a reply to Discord's own
// « X a rejoint le serveur » line, so there is one welcome, not two, and she follows whatever
// Discord does (membership screening included — the line appears when Discord posts it).
//
// - A newcomer gets WelcomeLines, a real ping narrowed to them (root CLAUDE.md, mentions).
// - Someone coming back (a MemberXp row from before: both XP wipes kept the rows) gets
//   WelcomeBackLines.
// - Past MaxPerHour welcomes in an hour (a raid, an invite wave), one WelcomeRushLines line,
//   then silence until the hour has rolled on.
// - A bot gets no welcome there; as far as we know Discord posts no join line for one added
//   through OAuth, so BotService feeds UserJoined here too, and a new bot gets RivalJoinLines in
//   her main channel (home guild only).
//
// A singleton: the hourly window is in-memory, and resets on restart by design.
internal sealed class WelcomeService
{
    private const int MaxPerHour = 3;
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    private readonly IServiceProvider _services;
    private readonly GuildConfigService _config;
    private readonly ResponsePicker _picker;
    private readonly BreakdownService _breakdown;
    private readonly ILogger<WelcomeService> _logger;

    private readonly object _gate = new();
    private readonly Queue<DateTimeOffset> _recent = new();
    private DateTimeOffset _rushLineAt = DateTimeOffset.MinValue;

    public WelcomeService(
        IServiceProvider services, GuildConfigService config, ResponsePicker picker,
        BreakdownService breakdown, ILogger<WelcomeService> logger)
    {
        _services = services;
        _config = config;
        _picker = picker;
        _breakdown = breakdown;
        _logger = logger;
    }

    /// <summary>Discord's join line: answer it. Every other message is ignored.</summary>
    public async Task HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketSystemMessage { Type: MessageType.GuildMemberJoin } joined) return;
        if (joined.Channel is not SocketTextChannel channel) return;
        if (joined.Author.IsBot) return;
        if (_breakdown.IsActive(channel.Id)) return;

        var now = DateTimeOffset.UtcNow;
        bool welcome, rush;
        lock (_gate)
        {
            while (_recent.Count > 0 && now - _recent.Peek() >= Window) _recent.Dequeue();
            welcome = _recent.Count < MaxPerHour;
            rush = !welcome && now - _rushLineAt >= Window;
            if (welcome) _recent.Enqueue(now);
            if (rush) _rushLineAt = now;
        }

        var reference = new MessageReference(joined.Id, channel.Id, channel.Guild.Id);
        if (rush)
        {
            await BotChat.PostWithTypingAsync(channel, _picker.Pick(BotResponses.WelcomeRushLines),
                _logger, "welcome rush line", AllowedMentions.None, reference);
            return;
        }
        if (!welcome) return;

        bool known;
        try
        {
            await using var scope = _services.CreateAsyncScope();
            known = await scope.ServiceProvider.GetRequiredService<XpService>()
                .IsKnownMemberAsync(channel.Guild.Id, joined.Author.Id);
        }
        catch (Exception ex)
        {
            // A newcomer's welcome is the safe reading when the database can't say.
            _logger.LogWarning(ex, "Failed to look up {UserId} for the welcome.", joined.Author.Id);
            known = false;
        }

        var pool = known ? BotResponses.WelcomeBackLines : BotResponses.WelcomeLines;
        var line = string.Format(_picker.Pick(pool), joined.Author.Mention);
        var allowed = new AllowedMentions { UserIds = new List<ulong> { joined.Author.Id } };
        await BotChat.PostWithTypingAsync(channel, line, _logger, "welcome", allowed, reference);
        _logger.LogInformation("Welcomed {UserId} (back: {Known}).", joined.Author.Id, known);
    }

    /// <summary>A member joined: only a bot is handled here (see the class comment).</summary>
    public async Task HandleUserJoinedAsync(SocketGuildUser user)
    {
        if (!user.IsBot || user.Id == user.Guild.CurrentUser?.Id) return;
        if (user.Guild.Id != HomeGuild.Id) return;

        try
        {
            var channelId = await MorningGreetingService.MainChannelIdAsync(_config);
            if (user.Guild.GetTextChannel(channelId) is not { } channel) return;
            if (_breakdown.IsActive(channel.Id)) return;

            var line = string.Format(_picker.Pick(BotResponses.RivalJoinLines), Format.Sanitize(user.Username));
            await BotChat.PostWithTypingAsync(channel, line, _logger, "rival join line", AllowedMentions.None);
            _logger.LogInformation("New bot {UserId} joined; line posted.", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to greet new bot {UserId}.", user.Id);
        }
    }
}
