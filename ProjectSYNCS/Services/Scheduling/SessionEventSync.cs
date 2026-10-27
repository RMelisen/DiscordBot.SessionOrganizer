using System.Text;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

// Mirrors a session into a native Discord Guild Scheduled Event (the server
// "Events" tab). Sessions aren't tied to a voice channel, so these are
// "External" events with a fixed location label and an explicit end time; their
// description carries the current participant list. Every call swallows/logs
// errors so a missing permission or API hiccup never breaks the session flow.
public static class SessionEventSync
{
    // External events need a location string: the session's voice channel by name when it has
    // one, a plain label otherwise. Still External either way, so the end time stays ours.
    private const string LocationText = "Salon vocal";

    private static string LocationOf(IGuild guild, SessionEvent session) =>
        session.VoiceChannelId != 0 && guild is SocketGuild g && g.GetVoiceChannel(session.VoiceChannelId) is { } voice
            ? $"🔊 {voice.Name}"
            : LocationText;

    // External events run from the session start until start + its length.
    private static DateTimeOffset EndOf(SessionEvent session) =>
        session.ScheduledAt + session.Length;

    // Discord caps a scheduled-event description at 1000 characters.
    private const int MaxDescriptionLength = 1000;

    // Builds the event description: category, a link back to the session card,
    // and the current participant lists (by stored username, to avoid pinging).
    private static string BuildDescription(SessionEvent session)
    {
        var jumpUrl = $"https://discord.com/channels/{session.GuildId}/{session.ChannelId}/{session.MessageId}";
        var joined = session.Participants.Where(p => p.Status == ParticipantStatus.Joined).Select(p => p.Username).ToList();
        var maybes = session.Participants.Where(p => p.Status == ParticipantStatus.Maybe).Select(p => p.Username).ToList();

        var sb = new StringBuilder();
        sb.AppendLine(SessionCategories.Label(session.Category));
        if (!string.IsNullOrWhiteSpace(session.Description)) sb.AppendLine(session.Description.Trim());
        sb.AppendLine($"Détails et inscriptions : {jumpUrl}");
        sb.AppendLine();
        sb.AppendLine(joined.Count == 0
            ? "Inscrits : personne pour l'instant."
            : $"Inscrits ({joined.Count}) : {string.Join(", ", joined)}");
        if (maybes.Count > 0)
            sb.AppendLine($"Peut-être ({maybes.Count}) : {string.Join(", ", maybes)}");

        var text = sb.ToString().TrimEnd();
        return text.Length <= MaxDescriptionLength ? text : text[..MaxDescriptionLength];
    }

    /// <summary>
    /// Creates an external scheduled event for the session. Returns its id, or
    /// null if creation failed (e.g. the bot lacks the Manage Events permission).
    /// </summary>
    public static async Task<ulong?> CreateAsync(
        IGuild guild, SessionEvent session, ILogger? logger = null)
    {
        try
        {
            var ev = await guild.CreateEventAsync(
                name: session.Title,
                startTime: session.ScheduledAt,
                type: GuildScheduledEventType.External,
                privacyLevel: GuildScheduledEventPrivacyLevel.Private,
                description: BuildDescription(session),
                endTime: EndOf(session),
                location: LocationOf(guild, session));
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
    /// Re-syncs the linked native event's name, start time and description
    /// (including the participant list) to the session. No-op if the session has
    /// no native event or it can't be found. Called on edit and on every RSVP.
    /// </summary>
    public static async Task UpdateAsync(
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
                props.Description = BuildDescription(session);
                props.Location = LocationOf(guild, session);
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
    public static async Task DeleteAsync(
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
