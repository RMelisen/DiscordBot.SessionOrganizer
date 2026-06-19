using Discord;
using Discord.Net;
using Discord.WebSocket;
using ProjectSYNCS.Models;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

public static class SessionNotifier
{
    /// <summary>
    /// DMs everyone who was Joined or Maybe that the session has been cancelled.
    /// Mirrors the reminder DM flow, and ignores users who have DMs disabled.
    /// </summary>
    public static async Task NotifyCancelledAsync(
        DiscordSocketClient client, SessionEvent sessionEvent, ILogger? logger = null)
    {
        var recipients = sessionEvent.Participants
            .Where(p => p.Status is ParticipantStatus.Joined or ParticipantStatus.Maybe)
            .ToList();

        var ts = sessionEvent.ScheduledAt.ToUnixTimeSeconds();

        foreach (var participant in recipients)
        {
            try
            {
                var user = await client.GetUserAsync(participant.UserId);
                if (user is null) continue;

                var dm = await user.CreateDMChannelAsync();
                await dm.SendMessageAsync(
                    $"La session **{sessionEvent.Title}** prévue <t:{ts}:F> (<t:{ts}:R>) a été annulée.\n" +
                    $"Mais, c'est pas grave ! (ง ͠ಥ_ಥ)ง");
            }
            catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                logger?.LogWarning("User {UserId} has DMs disabled; skipping cancellation notice.", participant.UserId);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to send cancellation notice to user {UserId}.", participant.UserId);
            }
        }
    }
}
