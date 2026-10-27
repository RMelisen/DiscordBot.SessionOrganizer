using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// Every handler that reads the database before it can show anything defers first (root CLAUDE.md,
// reliability): the card is then redrawn with ModifyOriginalResponseAsync, and refusals go out as
// ephemeral follow-ups. « Modifier » is the exception: a modal must be the first response.
public class EventComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EventService _eventService;
    private readonly SessionWaitlist _waitlist;
    private readonly ILogger<EventComponentHandler> _logger;

    public EventComponentHandler(EventService eventService, SessionWaitlist waitlist, ILogger<EventComponentHandler> logger)
    {
        _eventService = eventService;
        _waitlist = waitlist;
        _logger = logger;
    }

    [ComponentInteraction("event:join:*")]
    public Task OnJoinAsync(string eventIdStr) =>
        HandleButtonAsync(eventIdStr, ParticipantStatus.Joined);

    [ComponentInteraction("event:sub:*")]
    public Task OnSubAsync(string eventIdStr) =>
        HandleButtonAsync(eventIdStr, ParticipantStatus.Maybe);

    [ComponentInteraction("event:decline:*")]
    public Task OnDeclineAsync(string eventIdStr) =>
        HandleButtonAsync(eventIdStr, ParticipantStatus.Declined);

    [ComponentInteraction("event:cancel:*")]
    public async Task OnCancelAsync(string eventIdStr)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        await DeferAsync();
        var gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Session introuvable.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, gameEvent))
        {
            await FollowupAsync("Seul l'organisateur ou un administrateur peut annuler cette session.", ephemeral: true);
            return;
        }

        if (gameEvent.IsCancelled)
        {
            await FollowupAsync("Cette session est déjà annulée.", ephemeral: true);
            return;
        }

        await _eventService.CancelEventAsync(eventId);
        gameEvent.IsCancelled = true;

        await ModifyOriginalResponseAsync(props =>
        {
            props.Embed = ScheduleModule.BuildEventEmbed(gameEvent, Context.Guild);
            props.Components = new ComponentBuilder().Build();
        });

        // Remove the linked native Discord event, if any.
        await SessionEventSync.DeleteAsync(Context.Guild, gameEvent.NativeEventId, _logger);

        await SessionNotifier.NotifyCancelledAsync(Context.Client, gameEvent);
    }

    [ComponentInteraction("event:edit:*")]
    public async Task OnEditAsync(string eventIdStr)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        var gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
        {
            await RespondAsync("Session introuvable.", ephemeral: true);
            return;
        }

        if (gameEvent.IsCancelled)
        {
            await RespondAsync("Cette session est annulée et ne peut plus être modifiée.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, gameEvent))
        {
            await RespondAsync("Seul l'organisateur ou un administrateur peut modifier cette session.", ephemeral: true);
            return;
        }

        await RespondWithModalAsync(ScheduleModule.BuildEditModal(gameEvent));
    }

    [ComponentInteraction("event:createnative:*")]
    public async Task OnCreateNativeAsync(string eventIdStr)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        await DeferAsync();
        var gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Session introuvable.", ephemeral: true);
            return;
        }

        if (gameEvent.IsCancelled)
        {
            await FollowupAsync("Cette session est annulée.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, gameEvent))
        {
            await FollowupAsync("Seul l'organisateur ou un administrateur peut gérer l'événement Discord.", ephemeral: true);
            return;
        }

        if (gameEvent.NativeEventId != 0)
        {
            await FollowupAsync("Un événement Discord est déjà lié à cette session.", ephemeral: true);
            return;
        }

        // Precise feedback: most failures here are the bot missing this permission.
        if (!Context.Guild.CurrentUser.GuildPermissions.ManageEvents)
        {
            await FollowupAsync(
                "Je n'ai pas la permission « Gérer les événements » sur ce serveur. " +
                "Demande à un admin de me l'accorder, puis réessaie.",
                ephemeral: true);
            return;
        }

        var nativeId = await SessionEventSync.CreateAsync(Context.Guild, gameEvent, _logger);
        if (nativeId is not ulong id)
        {
            await FollowupAsync(
                "Impossible de créer l'événement Discord. Réessaie plus tard — le détail de l'erreur est dans mes logs.",
                ephemeral: true);
            return;
        }

        await _eventService.SetNativeEventIdAsync(eventId, id);
        gameEvent.NativeEventId = id;

        await ModifyOriginalResponseAsync(props =>
        {
            props.Embed = ScheduleModule.BuildEventEmbed(gameEvent, Context.Guild);
            props.Components = ScheduleModule.BuildEventComponents(gameEvent);
        });
    }

    [ComponentInteraction("event:removenative:*")]
    public async Task OnRemoveNativeAsync(string eventIdStr)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        await DeferAsync();
        var gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Session introuvable.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, gameEvent))
        {
            await FollowupAsync("Seul l'organisateur ou un administrateur peut gérer l'événement Discord.", ephemeral: true);
            return;
        }

        if (gameEvent.NativeEventId == 0)
        {
            await FollowupAsync("Aucun événement Discord n'est lié à cette session.", ephemeral: true);
            return;
        }

        await SessionEventSync.DeleteAsync(Context.Guild, gameEvent.NativeEventId, _logger);
        await _eventService.SetNativeEventIdAsync(eventId, 0);
        gameEvent.NativeEventId = 0;

        await ModifyOriginalResponseAsync(props =>
        {
            props.Embed = ScheduleModule.BuildEventEmbed(gameEvent, Context.Guild);
            props.Components = ScheduleModule.BuildEventComponents(gameEvent);
        });
    }

    private async Task HandleButtonAsync(string eventIdStr, ParticipantStatus newStatus)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        // The most-clicked buttons of the bot: two reads and a write before the card can change.
        await DeferAsync();
        var gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Session introuvable.", ephemeral: true);
            return;
        }

        if (gameEvent.IsCancelled)
        {
            await FollowupAsync("Cette session a été annulée.", ephemeral: true);
            return;
        }

        int joinedBefore = gameEvent.Participants.Count(p => p.Status == ParticipantStatus.Joined);
        if (newStatus == ParticipantStatus.Joined)
        {
            bool alreadyJoined = gameEvent.Participants.Any(p => p.UserId == Context.User.Id && p.Status == ParticipantStatus.Joined);

            bool unlimited = gameEvent.MaxPlayers == 0;
            if (!unlimited && !alreadyJoined && joinedBefore >= gameEvent.MaxPlayers)
            {
                await FollowupAsync(
                    $"Cette session est complète ({gameEvent.MaxPlayers}/{gameEvent.MaxPlayers}). " +
                    "Clique **Peut-être** : je préviens les « peut-être » par DM dès qu'une place se libère.",
                    ephemeral: true);
                return;
            }
        }

        await _eventService.UpsertParticipantAsync(
            gameEvent,
            userId: Context.User.Id,
            username: Context.User.Username,
            status: newStatus);

        var updatedEvent = await _eventService.GetEventWithParticipantsAsync(eventId);

        // The button lives on the session message, so redraw it in place: no confirmation message.
        await ModifyOriginalResponseAsync(props =>
        {
            props.Embed = ScheduleModule.BuildEventEmbed(updatedEvent!, Context.Guild);
            props.Components = ScheduleModule.BuildEventComponents(updatedEvent!);
        });

        // Reflect the updated participant list in the linked native event, if any.
        // Done after the card update so it never delays the interaction response.
        await SessionEventSync.UpdateAsync(Context.Guild, updatedEvent!, _logger);

        // Someone left a full session: the « peut-être » are its waiting list.
        int joinedAfter = updatedEvent!.Participants.Count(p => p.Status == ParticipantStatus.Joined);
        if (SessionSeats.PlaceOpened(updatedEvent.MaxPlayers, joinedBefore, joinedAfter))
            await _waitlist.NotifyPlaceOpenedAsync(updatedEvent);
    }
}
