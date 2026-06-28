using Discord;
using Discord.Net;
using ProjectSYNCS.Models;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

// Mirrors a session into a native Discord Guild Scheduled Event (the server
// "Events" tab). Sessions aren't tied to a voice channel, so these are
// "External" events with a free-text location and an explicit end time.
// Every call swallows/logs errors so a missing permission or API hiccup never
// breaks the underlying session flow.
public static class SessionEventSync
{
    // External events run from the session start until start + Duration (2h).
    private static DateTimeOffset EndOf(SessionEvent session) =>
        session.ScheduledAt + SessionEvent.Duration;

    private static string CategoryLabel(SessionCategory category) => category switch
    {
        SessionCategory.Game     => "🎮 Jeu",
        SessionCategory.Activity => "🧑‍🤝‍🧑 Activité",
        SessionCategory.Movie    => "🎬 Film",
        _                        => "✨ Autre"
    };

    /// <summary>
    /// Creates an external scheduled event for the session. Returns its id, or
    /// null if creation failed (e.g. the bot lacks the Manage Events permission).
    /// </summary>
    public static async Task<ulong?> CreateExternalAsync(
        IGuild guild, SessionEvent session, string location, string jumpUrl, ILogger? logger = null)
    {
        try
        {
            var description = $"{CategoryLabel(session.Category)}\nDétails et inscriptions : {jumpUrl}";
            var ev = await guild.CreateEventAsync(
                name: session.Title,
                startTime: session.ScheduledAt,
                type: GuildScheduledEventType.External,
                privacyLevel: GuildScheduledEventPrivacyLevel.Private,
                description: description,
                endTime: EndOf(session),
                location: location);
            return ev.Id;
        }
        catch (HttpException ex)
        {
            logger?.LogWarning(ex,
                "Failed to create native event for session {EventId}: {Code} {Reason}",
                session.Id, ex.DiscordCode, ex.Reason);
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to create native event for session {EventId}.", session.Id);
            return null;
        }
    }

    /// <summary>
    /// Updates the linked native event's name and times to match the session.
    /// No-op if the session has no native event or it can't be found.
    /// </summary>
    public static async Task UpdateExternalAsync(
        IGuild guild, SessionEvent session, ILogger? logger = null)
    {
        if (session.NativeEventId == 0) return;

        try
        {
            var ev = await guild.GetEventAsync(session.NativeEventId);
            if (ev is null) return;

            await ev.ModifyAsync(props =>
            {
                props.Name = session.Title;
                props.StartTime = session.ScheduledAt;
                props.EndTime = EndOf(session);
            });
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to update native event {NativeEventId} for session {EventId}.",
                session.NativeEventId, session.Id);
        }
    }

    /// <summary>
    /// Deletes the linked native event. No-op if it's already gone.
    /// </summary>
    public static async Task DeleteExternalAsync(
        IGuild guild, ulong nativeEventId, ILogger? logger = null)
    {
        if (nativeEventId == 0) return;

        try
        {
            var ev = await guild.GetEventAsync(nativeEventId);
            if (ev is null) return;
            await ev.DeleteAsync();
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to delete native event {NativeEventId}.", nativeEventId);
        }
    }
}
