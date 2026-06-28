using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

public class EventComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EventService _eventService;
    private readonly ILogger<EventComponentHandler> _logger;

    public EventComponentHandler(EventService eventService, ILogger<EventComponentHandler> logger)
    {
        _eventService = eventService;
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

        var gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
        {
            await RespondAsync("Session introuvable.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, gameEvent))
        {
            await RespondAsync("Seul l'organisateur ou un administrateur peut annuler cette session.", ephemeral: true);
            return;
        }

        if (gameEvent.IsCancelled)
        {
            await RespondAsync("Cette session est déjà annulée.", ephemeral: true);
            return;
        }

        await _eventService.CancelEventAsync(eventId);
        gameEvent.IsCancelled = true;

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(props =>
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

        var gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
        {
            await RespondAsync("Session introuvable.", ephemeral: true);
            return;
        }

        if (gameEvent.IsCancelled)
        {
            await RespondAsync("Cette session est annulée.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, gameEvent))
        {
            await RespondAsync("Seul l'organisateur ou un administrateur peut gérer l'événement Discord.", ephemeral: true);
            return;
        }

        if (gameEvent.NativeEventId != 0)
        {
            await RespondAsync("Un événement Discord est déjà lié à cette session.", ephemeral: true);
            return;
        }

        // Precise feedback: most failures here are the bot missing this permission.
        if (!Context.Guild.CurrentUser.GuildPermissions.ManageEvents)
        {
            await RespondAsync(
                "Je n'ai pas la permission « Gérer les événements » sur ce serveur. " +
                "Demande à un admin de me l'accorder, puis réessaie.",
                ephemeral: true);
            return;
        }

        var nativeId = await SessionEventSync.CreateAsync(Context.Guild, gameEvent, _logger);
        if (nativeId is not ulong id)
        {
            await RespondAsync(
                "Impossible de créer l'événement Discord. Réessaie plus tard — le détail de l'erreur est dans mes logs.",
                ephemeral: true);
            return;
        }

        await _eventService.SetNativeEventIdAsync(eventId, id);
        gameEvent.NativeEventId = id;

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(props =>
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

        var gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
        {
            await RespondAsync("Session introuvable.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, gameEvent))
        {
            await RespondAsync("Seul l'organisateur ou un administrateur peut gérer l'événement Discord.", ephemeral: true);
            return;
        }

        if (gameEvent.NativeEventId == 0)
        {
            await RespondAsync("Aucun événement Discord n'est lié à cette session.", ephemeral: true);
            return;
        }

        await SessionEventSync.DeleteAsync(Context.Guild, gameEvent.NativeEventId, _logger);
        await _eventService.SetNativeEventIdAsync(eventId, 0);
        gameEvent.NativeEventId = 0;

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(props =>
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

        var gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
        {
            await RespondAsync("Session introuvable.", ephemeral: true);
            return;
        }

        if (gameEvent.IsCancelled)
        {
            await RespondAsync("Cette session a été annulée.", ephemeral: true);
            return;
        }

        if (newStatus == ParticipantStatus.Joined)
        {
            int joinedCount = gameEvent.Participants.Count(p => p.Status == ParticipantStatus.Joined);
            bool alreadyJoined = gameEvent.Participants.Any(p => p.UserId == Context.User.Id && p.Status == ParticipantStatus.Joined);

            bool unlimited = gameEvent.MaxPlayers == 0;
            if (!unlimited && !alreadyJoined && joinedCount >= gameEvent.MaxPlayers)
            {
                await RespondAsync(
                    $"Cette session est complète ({gameEvent.MaxPlayers}/{gameEvent.MaxPlayers}). " +
                    "Utilise **Peut-être** pour rejoindre la liste d'attente.",
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

        // The button lives on the session message, so update it in place. This
        // acknowledges the interaction without sending a confirmation message.
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(props =>
        {
            props.Embed = ScheduleModule.BuildEventEmbed(updatedEvent!, Context.Guild);
            props.Components = ScheduleModule.BuildEventComponents(updatedEvent!);
        });

        // Reflect the updated participant list in the linked native event, if any.
        // Done after the card update so it never delays the interaction response.
        await SessionEventSync.UpdateAsync(Context.Guild, updatedEvent!, _logger);
    }
}
