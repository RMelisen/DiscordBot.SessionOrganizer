using Discord;
using Discord.Net;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Poll = ProjectSYNCS.Models.Poll;

namespace ProjectSYNCS.Services;

public class ReminderService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<ReminderService> _logger;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    public ReminderService(
        IServiceProvider services,
        DiscordSocketClient client,
        ILogger<ReminderService> logger)
    {
        _services = services;
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderService started. Checking every {Interval} minutes.",
            CheckInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);
            await ProcessRemindersAsync();
            await ProcessLifecycleAsync();
            await ProcessPollAutoCloseAsync();
        }
    }

    // Polls and votes auto-close after this long, even without manual closure.
    private static readonly TimeSpan PollLifetime = TimeSpan.FromDays(2);

    private async Task ProcessPollAutoCloseAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var pollService = scope.ServiceProvider.GetRequiredService<PollService>();

        List<Poll> polls;
        try
        {
            polls = await pollService.GetPollsToAutoCloseAsync(PollLifetime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query polls for auto-close.");
            return;
        }

        foreach (var poll in polls)
        {
            await pollService.ClosePollAsync(poll.Id);
            poll.IsClosed = true;
            await UpdatePollCardAsync(poll);
        }
    }

    private async Task UpdatePollCardAsync(Poll poll)
    {
        if (poll.MessageId == 0) return;

        try
        {
            var guild = _client.GetGuild(poll.GuildId);
            var channel = guild?.GetTextChannel(poll.ChannelId);
            if (channel is null) return;

            if (await channel.GetMessageAsync(poll.MessageId) is not IUserMessage message)
                return;

            await message.ModifyAsync(props =>
            {
                props.Embed = PollModule.BuildPollEmbed(poll);
                props.Components = PollModule.BuildPollComponents(poll);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update auto-closed poll card for poll {PollId}.", poll.Id);
        }
    }

    // Advances session cards through their lifecycle (En cours -> Terminée) and
    // disables their buttons once they start.
    private async Task ProcessLifecycleAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var eventService = scope.ServiceProvider.GetRequiredService<EventService>();

        List<SessionEvent> events;
        try
        {
            events = await eventService.GetEventsNeedingLifecycleUpdateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query events for lifecycle updates.");
            return;
        }

        foreach (var sessionEvent in events)
        {
            var phase = sessionEvent.PhaseAt(DateTimeOffset.UtcNow);
            await UpdateCardAsync(sessionEvent);
            await eventService.SetRenderedPhaseAsync(sessionEvent.Id, phase);
        }
    }

    private async Task UpdateCardAsync(SessionEvent sessionEvent)
    {
        if (sessionEvent.MessageId == 0) return;

        try
        {
            var guild = _client.GetGuild(sessionEvent.GuildId);
            var channel = guild?.GetTextChannel(sessionEvent.ChannelId);
            if (channel is null) return;

            if (await channel.GetMessageAsync(sessionEvent.MessageId) is not IUserMessage message)
                return;

            await message.ModifyAsync(props =>
            {
                props.Embed = ScheduleModule.BuildEventEmbed(sessionEvent, guild);
                props.Components = ScheduleModule.BuildEventComponents(sessionEvent);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update lifecycle card for event {EventId}.", sessionEvent.Id);
        }
    }

    private async Task ProcessRemindersAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var eventService = scope.ServiceProvider.GetRequiredService<EventService>();

        List<SessionEvent> events;
        try
        {
            events = await eventService.GetEventsNeedingReminderAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query events for reminders.");
            return;
        }

        foreach (var sessionEvent in events)
        {
            await SendRemindersForEventAsync(sessionEvent, eventService);
        }
    }

    private async Task SendRemindersForEventAsync(SessionEvent sessionEvent, EventService eventService)
    {
        var participants = sessionEvent.Participants
            .Where(p => p.Status == ParticipantStatus.Joined)
            .ToList();

        _logger.LogInformation(
            "Sending reminders for event {EventId} ({Title}) to {Count} participant(s).",
            sessionEvent.Id, sessionEvent.Title, participants.Count);

        foreach (var participant in participants)
        {
            try
            {
                var user = await _client.GetUserAsync(participant.UserId);
                if (user is null)
                {
                    _logger.LogWarning("Could not resolve user {UserId} for reminder.", participant.UserId);
                    continue;
                }

                var dm = await user.CreateDMChannelAsync();
                var ts = sessionEvent.ScheduledAt.ToUnixTimeSeconds();
                var categoryLabel = sessionEvent.Category switch
                {
                    SessionCategory.Game     => "Jeu",
                    SessionCategory.Activity => "Activité",
                    SessionCategory.Movie    => "Film",
                    _                        => "Autre"
                };

                await dm.SendMessageAsync(
                    $"Kilou kilou {user.Mention}! (˶>⩊<˶)\n" +
                    $"Rappel: **{sessionEvent.Title}** va commencer dans " +
                    $"<t:{ts}:R> (<t:{ts}:t>). Tiens toi prêt ! UwU");
            }
            catch (HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                _logger.LogWarning("User {UserId} has DMs disabled; skipping reminder.", participant.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder to user {UserId}.", participant.UserId);
            }
        }

        await eventService.MarkReminderSentAsync(sessionEvent.Id);
    }
}
