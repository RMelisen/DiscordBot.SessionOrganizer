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
// her main channel (home guild), with Friday, Saturday and Sunday at 21:00 (Helpers/WeekendPoll).
// It is an ordinary /poll — same card, same buttons, same two-day auto-close — whose organizer
// is her; ReminderService announces the result when it closes, and whoever voted for the winning
// slot may turn it into a session (PollModule.OnToSessionAsync).
//
// She never steps on someone else's organising: no poll when a session is already planned for
// that weekend, or when someone's date poll is still open. Once a week at most: the claim is the
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
    private readonly ILogger<WeekendPollService> _logger;

    // The Wednesday already decided this run (posted or stood down), so the window isn't
    // re-checked every tick. In memory: a restart re-decides, and the poll itself is the claim.
    private DateTime _decided = DateTime.MinValue;

    public WeekendPollService(
        IServiceProvider services, DiscordSocketClient client, GuildConfigService config,
        ResponsePicker picker, ILogger<WeekendPollService> logger)
    {
        _services = services;
        _client = client;
        _config = config;
        _picker = picker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            // One try around the tick: there is one item, and an exception escaping
            // ExecuteAsync stops the whole host.
            try
            {
                await TickAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weekend poll tick failed.");
            }
        }
    }

    private async Task TickAsync()
    {
        if (!WeekendPoll.IsDue(DateTimeOffset.UtcNow, out var wednesday) || wednesday == _decided) return;

        var guild = _client.GetGuild(HomeGuild.Id);
        var channelId = await MorningGreetingService.MainChannelIdAsync(_config);
        // In the dev guild the channel never resolves: nothing to post, nothing to decide.
        if (guild?.GetTextChannel(channelId) is not { } channel) return;

        await using var scope = _services.CreateAsyncScope();
        var polls = scope.ServiceProvider.GetRequiredService<PollService>();
        var events = scope.ServiceProvider.GetRequiredService<EventService>();
        var botId = _client.CurrentUser.Id;

        var reason = await StandDownReasonAsync(polls, events, guild.Id, botId, wednesday);
        if (reason is not null)
        {
            _decided = wednesday;
            _logger.LogInformation("Weekend poll skipped: {Reason}.", reason);
            return;
        }

        // Claimed before the send: a failed send costs this week's poll, never a second one.
        _decided = wednesday;
        var poll = await polls.CreatePollAsync(guild.Id, channel.Id, botId, Title, WeekendPoll.Slots(wednesday));
        var full = await polls.GetPollWithVotesAsync(poll.Id);
        try
        {
            var message = await channel.SendMessageAsync(
                _picker.Pick(BotResponses.WeekendPollLines),
                embed: PollModule.BuildPollEmbed(full!),
                components: PollModule.BuildPollComponents(full!),
                allowedMentions: AllowedMentions.None);
            await polls.SetMessageIdAsync(poll.Id, message.Id);
            _logger.LogInformation("Weekend poll {PollId} posted.", poll.Id);
        }
        catch (Exception ex)
        {
            // A poll with no card would sit in /poll list and auto-close silently; remove it.
            _logger.LogWarning(ex, "Failed to post the weekend poll; removing poll {PollId}.", poll.Id);
            await polls.DeletePollAsync(poll.Id);
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

        var open = await polls.GetActivePollsAsync(guildId, PollKind.DateSlots);
        if (open.Any(p => p.OrganizerId != botId))
            return "someone's date poll is still open";

        return null;
    }
}
