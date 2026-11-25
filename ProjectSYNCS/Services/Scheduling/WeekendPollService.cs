using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// She starts things herself: every Wednesday evening, a « qui est dispo ce weekend ? » poll in
// her main channel, on every server where the weekend-poll feature is on (home by default), with Friday, Saturday and Sunday at 21:00 (Helpers/Scheduling/WeekendPoll).
// It is an ordinary /when — same card, same buttons, same two-day auto-close — whose organizer
// is her; when it closes ReminderService creates the session itself for a clear winner, or announces
// the result, and whoever voted for a winning slot may turn it into a session (PollModule.OnToSessionAsync).
//
// She never steps on someone else's organising: no poll when a session is already planned for
// that weekend (a weekly series counts), or when someone's date poll is still open. Once a week at most: the claim is the
// poll itself (a poll of hers created in the last six days), so a restart can't post a second.
//
// Its own 15-minute interval, like every loop here (root CLAUDE.md): the slot is drawn to the
// minute, and a quarter of an hour late is invisible for a poll that runs two days.
internal sealed class WeekendPollService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    private const string Title = "Qui est dispo ce weekend ?";

    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly GuildConfigService _config;
    private readonly ResponsePicker _picker;
    private readonly HerMessages _herMessages;
    private readonly ILogger<WeekendPollService> _logger;
    private readonly SilenceService _silence;

    // The Wednesday already decided this run (posted or stood down), per server, so the window isn't
    // re-checked every tick. In memory: a restart re-decides, and the poll itself is the claim. Read
    // and written by the loop only.
    private readonly Dictionary<ulong, DateTime> _decided = new();

    public WeekendPollService(
        IServiceProvider services, DiscordSocketClient client, GuildConfigService config,
        ResponsePicker picker, HerMessages herMessages, ILogger<WeekendPollService> logger, SilenceService silence)
    {
        _silence = silence;
        _services = services;
        _client = client;
        _config = config;
        _picker = picker;
        _herMessages = herMessages;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            if (!WeekendPoll.IsDue(DateTimeOffset.UtcNow, out var wednesday)) continue;

            // One try per server: an exception escaping ExecuteAsync stops the whole host.
            foreach (var guild in _client.Guilds)
            {
                try
                {
                    await TickAsync(guild, wednesday);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Weekend poll tick failed in guild {GuildId}.", guild.Id);
                }
            }
        }
    }

    private async Task TickAsync(SocketGuild guild, DateTime wednesday)
    {
        if (_decided.TryGetValue(guild.Id, out var decided) && decided == wednesday) return;
        var config = await _config.GetAsync(guild.Id);
        if (!config.IsOn(ServerFeatures.WeekendPoll, guild.Id)) return;
        // Silence mode: not decided, so the poll still goes out if silence lifts within its window.
        if (_silence.IsSilent(guild.Id)) return;

        var channelId = MorningGreetingService.MainChannelOf(config, guild.Id);
        // No main channel here (or not delivered yet): nothing to post, nothing to decide.
        if (guild.GetTextChannel(channelId) is not { } channel) return;

        await using var scope = _services.CreateAsyncScope();
        var polls = scope.ServiceProvider.GetRequiredService<PollService>();
        var events = scope.ServiceProvider.GetRequiredService<EventService>();
        var botId = _client.CurrentUser.Id;

        var reason = await StandDownReasonAsync(polls, events, guild.Id, botId, wednesday);
        if (reason is not null)
        {
            _decided[guild.Id] = wednesday;
            _logger.LogInformation("Weekend poll skipped in guild {GuildId}: {Reason}.", guild.Id, reason);
            return;
        }

        // Claimed before the send: a failed send costs this week's poll, never a second one.
        _decided[guild.Id] = wednesday;
        var poll = await polls.CreatePollAsync(guild.Id, channel.Id, botId, Title, WeekendPoll.Slots(wednesday));
        var full = await polls.GetPollWithVotesAsync(poll.Id);
        IUserMessage? line = null;
        try
        {
            // A Components V2 card carries no content: her line goes first, on its own, and is the
            // message replies to her are read against; the card follows it.
            line = await channel.SendMessageAsync(_picker.Pick(BotResponses.WeekendPollLines),
                allowedMentions: AllowedMentions.None);
            _herMessages.Remember(line, ReplyContext.WeekendPoll);
            var card = await PollCards.SendAsync(channel, full!);
            await polls.SetMessageIdAsync(poll.Id, card.Id);
            _logger.LogInformation("Weekend poll {PollId} posted.", poll.Id);
        }
        catch (Exception ex)
        {
            // A poll with no card would sit in /when list and auto-close silently; remove it, and
            // the line announcing it.
            _logger.LogWarning(ex, "Failed to post the weekend poll; removing poll {PollId}.", poll.Id);
            await polls.DeletePollAsync(poll.Id);
            if (line is not null)
            {
                try { await line.DeleteAsync(); }
                catch (Exception deleteEx) { _logger.LogDebug(deleteEx, "Could not delete the weekend poll's line."); }
            }
        }
    }

    private static async Task<string?> StandDownReasonAsync(
        PollService polls, EventService events, ulong guildId, ulong botId, DateTime wednesday)
    {
        if (await polls.HasPollByOrganizerSinceAsync(guildId, botId, DateTimeOffset.UtcNow.AddDays(-6)))
            return "already posted this week";

        var (from, to) = WeekendPoll.Weekend(wednesday);
        if (await events.HasSessionBetweenAsync(guildId, from, to))
            return "a session is already planned that weekend";
        // A weekly series lands that weekend even if its occurrence isn't posted yet (LeadDays).
        if (await events.HasSeriesBetweenAsync(guildId, from, to))
            return "a weekly session falls that weekend";

        var open = await polls.GetActivePollsAsync(guildId, PollKind.DateSlots);
        if (open.Any(p => p.OrganizerId != botId))
            return "someone's date poll is still open";

        return null;
    }
}
