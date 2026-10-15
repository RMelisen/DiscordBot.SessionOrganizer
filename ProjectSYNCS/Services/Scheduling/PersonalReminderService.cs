using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>
/// Delivers /remindme: its own 1-minute loop (a reminder five minutes late reads as broken, so it
/// can't ride ReminderService's five). By DM, in her voice; a refused DM falls back to a ping in the
/// channel where it was asked. The row is claimed before sending — a crash costs the reminder,
/// never a second copy — and every reminder is guarded on its own.
/// </summary>
public class PersonalReminderService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    // Later than this (she was down), the reminder says so.
    private static readonly TimeSpan LateAfter = TimeSpan.FromMinutes(10);

    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<PersonalReminderService> _logger;

    public PersonalReminderService(IServiceProvider services, DiscordSocketClient client, ResponsePicker picker,
        ILogger<PersonalReminderService> logger)
    {
        _services = services;
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            if (_client.ConnectionState != ConnectionState.Connected) continue;

            List<PersonalReminder> due;
            try
            {
                await using var scope = _services.CreateAsyncScope();
                due = await scope.ServiceProvider.GetRequiredService<PersonalReminderStore>().GetDueAsync(DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read due reminders.");
                continue;
            }

            foreach (var reminder in due)
            {
                try
                {
                    await DeliverAsync(reminder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deliver reminder {ReminderId}.", reminder.Id);
                }
            }
        }
    }

    private async Task DeliverAsync(PersonalReminder reminder)
    {
        await using var scope = _services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<PersonalReminderStore>();
        if (!await store.ClaimSendAsync(reminder.Id)) return;

        var late = DateTimeOffset.UtcNow - reminder.DueAt > LateAfter;
        var line = _picker.Pick(late ? BotResponses.ReminderLateLines : BotResponses.ReminderLines);
        var body = $"{line}\n{MessageFormat.Quote(reminder.Text, InputCaps.Reminder)}\n" +
                   $"-# Demandé <t:{reminder.CreatedAt.ToUnixTimeSeconds()}:R>";

        if (!await TryDmAsync(reminder.UserId, body))
            await PingInChannelAsync(reminder, body);

        await store.DeleteAsync(reminder.Id);
    }

    private async Task<bool> TryDmAsync(ulong userId, string body)
    {
        try
        {
            var user = await _client.GetUserAsync(userId);
            if (user is null) return false;
            await user.SendMessageAsync(body, allowedMentions: AllowedMentions.None);
            return true;
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            return false;
        }
    }

    // Only for a reminder asked in a server: one asked in a DM has nowhere else to go.
    private async Task PingInChannelAsync(PersonalReminder reminder, string body)
    {
        if (reminder.GuildId == 0) return;
        var channel = _client.GetGuild(reminder.GuildId)?.GetTextChannel(reminder.ChannelId);
        if (channel is null) return;
        await channel.SendMessageAsync($"<@{reminder.UserId}> {body}",
            allowedMentions: new AllowedMentions { UserIds = new List<ulong> { reminder.UserId } });
    }
}
