using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

/// <summary>
/// The waiting list a full session card promises: « Utilise Peut-être pour rejoindre la liste
/// d'attente ». When a place opens (someone leaves, or the organiser raises the cap), every
/// « peut-être » gets a DM with the card's link; the first click on « Rejoindre » takes it. Nobody
/// is promoted automatically: « peut-être » also means « peut-être ».
/// </summary>
/// <remarks>
/// Singleton: it holds the per-session gate, so someone leaving and rejoining in a loop can't turn
/// it into a DM cannon. A command set this up, so silence mode doesn't apply (root CLAUDE.md).
/// </remarks>
public sealed class SessionWaitlist
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(10);

    private readonly CooldownGate<int> _gate = new(Cooldown, forget: Cooldown);
    private readonly DiscordSocketClient _client;
    private readonly ResponsePicker _picker;
    private readonly ILogger<SessionWaitlist> _logger;

    public SessionWaitlist(DiscordSocketClient client, ResponsePicker picker, ILogger<SessionWaitlist> logger)
    {
        _client = client;
        _picker = picker;
        _logger = logger;
    }

    /// <summary>DMs the session's « peut-être » that a place is free. Never throws.</summary>
    public async Task NotifyPlaceOpenedAsync(SessionEvent session)
    {
        var maybes = session.Participants.Where(p => p.Status == ParticipantStatus.Maybe).ToList();
        if (maybes.Count == 0 || !_gate.TryClaim(session.Id)) return;

        var ts = session.ScheduledAt.ToUnixTimeSeconds();
        var link = $"https://discord.com/channels/{session.GuildId}/{session.ChannelId}/{session.MessageId}";
        var line = string.Format(_picker.Pick(BotResponses.SessionSpotFreedLines),
            Format.Sanitize(session.Title), $"<t:{ts}:F>", link);

        foreach (var participant in maybes)
        {
            try
            {
                var user = await _client.GetUserAsync(participant.UserId);
                if (user is null) continue;
                var dm = await user.CreateDMChannelAsync();
                await dm.SendMessageAsync(line, allowedMentions: AllowedMentions.None);
            }
            catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                _logger.LogInformation("User {UserId} has DMs disabled; no waiting-list notice.", participant.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Waiting-list notice for session {EventId} not sent to {UserId}.", session.Id, participant.UserId);
            }
        }
        _logger.LogInformation("A place opened in session {EventId}; told {Count} « peut-être ».", session.Id, maybes.Count);
    }
}
