using Discord;
using Discord.Net;
using Discord.WebSocket;
using ProjectSYNCS.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

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
                    $"Kilou kilou {user.Mention}! Rappel: **{sessionEvent.Title}** va commencer dans " +
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
