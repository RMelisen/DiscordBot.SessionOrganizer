using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// Every handler that reads the database before it can show anything defers first (root CLAUDE.md,
// reliability): the card is then redrawn with ModifyOriginalResponseAsync, and refusals go out as
// ephemeral follow-ups. « Modifier » is the exception: a modal must be the first response, so it
// opens from the « Gérer » panel's snapshot when it can (ManageSnapshots).
//
// The card carries the answers and « ⚙️ Gérer »; Gérer opens an ephemeral panel (organiser and
// staff only) with Modifier, Annuler, Republier and the rest. Cards posted before the panel still
// carry event:edit / event:cancel / event:removenative: the same handlers serve both.
public class EventComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EventService _eventService;
    private readonly SessionWaitlist _waitlist;
    private readonly SchedulingLocks _locks;
    private readonly ManageSnapshots _snapshots;
    private readonly CardUpdater _cards;
    private readonly CardRepost _repost;
    private readonly ILogger<EventComponentHandler> _logger;

    public EventComponentHandler(EventService eventService, SessionWaitlist waitlist, SchedulingLocks locks,
        ManageSnapshots snapshots, CardUpdater cards, CardRepost repost, ILogger<EventComponentHandler> logger)
    {
        _eventService = eventService;
        _waitlist = waitlist;
        _locks = locks;
        _snapshots = snapshots;
        _cards = cards;
        _repost = repost;
        _logger = logger;
    }

    // The panel and the confirmations are ephemeral messages; the card is not. Same verbs, two places.
    private bool OnEphemeral =>
        Context.Interaction is SocketMessageComponent c && c.Message.Flags?.HasFlag(MessageFlags.Ephemeral) == true;

    [ComponentInteraction("event:join:*")]
    public Task OnJoinAsync(string eventIdStr) =>
        HandleButtonAsync(eventIdStr, ParticipantStatus.Joined);

    [ComponentInteraction("event:sub:*")]
    public Task OnSubAsync(string eventIdStr) =>
        HandleButtonAsync(eventIdStr, ParticipantStatus.Maybe);

    [ComponentInteraction("event:decline:*")]
    public Task OnDeclineAsync(string eventIdStr) =>
        HandleButtonAsync(eventIdStr, ParticipantStatus.Declined);

    // ---- « ⚙️ Gérer » ----------------------------------------------------

    [ComponentInteraction("event:manage:*")]
    public async Task OnManageAsync(string eventIdStr)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var session = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (session is null || session.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Session introuvable.", ephemeral: true);
            return;
        }
        if (!SessionPermissions.CanManage(Context.User, session))
        {
            await FollowupAsync($"Cette session est à <@{session.OrganizerId}> : seul l'organisateur ou un administrateur peut la gérer.",
                ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        if (session.IsCancelled)
        {
            await FollowupAsync("Cette session est annulée.", ephemeral: true);
            return;
        }

        _snapshots.Remember(Context.User.Id, session);
        await FollowupAsync(SessionManage.Panel(session), components: SessionManage.PanelComponents(session), ephemeral: true);
    }

    [ComponentInteraction("event:edit:*")]
    public async Task OnEditAsync(string eventIdStr)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        // From the panel, the snapshot it took: no read on the Pi before the modal. The submit
        // reads the row again and checks everything there.
        if (_snapshots.Session(Context.User.Id, eventId) is { } snapshot)
        {
            await RespondWithModalAsync(ScheduleModule.BuildEditModal(snapshot));
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

    // ---- Cancel: asks first ----------------------------------------------

    [ComponentInteraction("event:cancel:*")]
    public async Task OnCancelAsync(string eventIdStr)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        bool panel = OnEphemeral;
        await DeferAsync(ephemeral: true);
        var session = _snapshots.Session(Context.User.Id, eventId) ?? await _eventService.GetEventWithParticipantsAsync(eventId);
        if (session is null || session.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Session introuvable.", ephemeral: true);
            return;
        }
        if (!SessionPermissions.CanManage(Context.User, session))
        {
            await FollowupAsync("Seul l'organisateur ou un administrateur peut annuler cette session.", ephemeral: true);
            return;
        }

        // From the panel the prompt replaces it; from an old card's button it comes as its own message.
        if (panel)
            await ModifyOriginalResponseAsync(p =>
            {
                p.Content = SessionManage.CancelPrompt(session);
                p.Components = SessionManage.CancelConfirm(eventId);
            });
        else
            await FollowupAsync(SessionManage.CancelPrompt(session), components: SessionManage.CancelConfirm(eventId),
                ephemeral: true);
    }

    [ComponentInteraction("event:cancelno:*")]
    public async Task OnCancelNoAsync(string eventIdStr)
    {
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(p =>
        {
            p.Content = "D'accord, la session est maintenue.";
            p.Components = new ComponentBuilder().Build();
        });
    }

    [ComponentInteraction("event:cancelyes:*")]
    public async Task OnCancelYesAsync(string eventIdStr)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        await DeferAsync();
        SessionEvent? gameEvent;
        using (await _locks.Sessions.AcquireAsync(eventId))
        {
            gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
            if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
            {
                await Done("Session introuvable.");
                return;
            }
            if (!SessionPermissions.CanManage(Context.User, gameEvent))
            {
                await Done("Seul l'organisateur ou un administrateur peut annuler cette session.");
                return;
            }
            if (gameEvent.IsCancelled)
            {
                await Done("Cette session est déjà annulée.");
                return;
            }

            await _eventService.CancelEventAsync(eventId);
            gameEvent.IsCancelled = true;
            _snapshots.Forget(eventId);
        }

        // Each step on its own: a card that can't be edited never costs the DMs or the native
        // event's removal (CardUpdater swallows its failures).
        await _cards.RedrawSessionAsync(Context.Guild, gameEvent);
        await SessionEventSync.DeleteAsync(Context.Guild, gameEvent.NativeEventId, _logger);
        await SessionNotifier.NotifyCancelledAsync(Context.Client, gameEvent, _logger);

        await Done("La session a été annulée. Les participants ont été prévenus.");
    }

    // ---- The rest of the panel -------------------------------------------

    [ComponentInteraction("event:repost:*")]
    public async Task OnRepostAsync(string eventIdStr)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        await DeferAsync();
        var session = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (session is null || session.GuildId != Context.Guild.Id || !SessionPermissions.CanManage(Context.User, session))
        {
            await Done("Session introuvable.");
            return;
        }
        var result = await _repost.RepostAsync(Context.Guild, Context.Channel, new CardRef(CardKind.Session, eventId));
        await Done(result == RepostResult.Reposted ? "Carte republiée ici 📌" : "Cette session est annulée.");
    }

    [ComponentInteraction("event:stopseries:*")]
    public async Task OnStopSeriesAsync(string eventIdStr)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        await DeferAsync();
        var session = await _eventService.GetEventWithParticipantsAsync(eventId);
        if (session is null || session.GuildId != Context.Guild.Id || session.RecurringSessionId is not { } seriesId)
        {
            await Done("Cette session ne fait pas partie d'une série.");
            return;
        }
        if (!SessionPermissions.CanManage(Context.User, session))
        {
            await Done("Seul l'organisateur ou un administrateur peut arrêter cette série.");
            return;
        }
        await _eventService.StopSeriesAsync(seriesId);
        await Done("Série arrêtée : plus aucune session ne sera postée. Celles déjà sur le salon restent, annule-les une par une si besoin.");
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
            await FollowupAsync("Seul l'organisateur ou un administrateur peut créer l'événement Discord.", ephemeral: true);
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
        await RedrawHereAsync(gameEvent);
    }

    [ComponentInteraction("event:removenative:*")]
    public async Task OnRemoveNativeAsync(string eventIdStr)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        bool panel = OnEphemeral;
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

        if (panel)
        {
            await _cards.RedrawSessionAsync(Context.Guild, gameEvent);
            await Done("Événement Discord retiré.");
        }
        else
            await RedrawHereAsync(gameEvent);
    }

    // ---- Rejoindre / Peut-être / Refuser ---------------------------------

    private async Task HandleButtonAsync(string eventIdStr, ParticipantStatus newStatus)
    {
        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await RespondAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        // The most-clicked buttons of the bot: two reads and a write before the card can change.
        await DeferAsync();

        SessionEvent updatedEvent;
        int joinedBefore;
        // Check capacity → write → reload → redraw, one click at a time per session: two people on
        // the last place can't both get it, and the card can't be redrawn out of order.
        using (await _locks.Sessions.AcquireAsync(eventId))
        {
            var gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
            if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
            {
                await FollowupAsync("Session introuvable.", ephemeral: true);
                return;
            }

            // Cancelled, or started: the lifecycle pass takes the buttons off up to five minutes later.
            if (!SessionSeats.CanRespond(gameEvent, DateTimeOffset.UtcNow, out var refusal))
            {
                await FollowupAsync(refusal, ephemeral: true);
                return;
            }

            joinedBefore = gameEvent.Participants.Count(p => p.Status == ParticipantStatus.Joined);
            var mine = gameEvent.Participants.FirstOrDefault(p => p.UserId == Context.User.Id);
            if (mine?.Status == newStatus)
            {
                await FollowupAsync(newStatus switch
                {
                    ParticipantStatus.Joined => "Ton inscription est déjà notée ✅",
                    ParticipantStatus.Maybe => "Tu es déjà en « peut-être ».",
                    _ => "C'est déjà noté : tu ne viens pas.",
                }, ephemeral: true);
                return;
            }

            if (newStatus == ParticipantStatus.Joined
                && gameEvent.MaxPlayers != 0 && joinedBefore >= gameEvent.MaxPlayers)
            {
                await FollowupAsync(
                    $"Cette session est complète ({gameEvent.MaxPlayers}/{gameEvent.MaxPlayers}). " +
                    "Clique **Peut-être** : je préviens les « peut-être » par DM dès qu'une place se libère.",
                    ephemeral: true);
                return;
            }

            try
            {
                await _eventService.UpsertParticipantAsync(
                    gameEvent,
                    userId: Context.User.Id,
                    username: Context.User.Username,
                    status: newStatus);
            }
            catch (DbUpdateException ex)
            {
                // The unique index on (session, person): a click that raced another. Already done.
                _logger.LogInformation(ex, "Duplicate answer on session {EventId} by {UserId}.", eventId, Context.User.Id);
                await FollowupAsync("C'est déjà noté.", ephemeral: true);
                return;
            }

            updatedEvent = (await _eventService.GetEventWithParticipantsAsync(eventId))!;
            // The button lives on the session message, so redraw it in place: no confirmation message.
            await RedrawHereAsync(updatedEvent);
        }

        // Reflect the updated participant list in the linked native event, if any.
        // Done after the card update so it never delays the interaction response.
        await SessionEventSync.UpdateAsync(Context.Guild, updatedEvent, _logger);

        // Someone left a full session: the « peut-être » are its waiting list.
        int joinedAfter = updatedEvent.Participants.Count(p => p.Status == ParticipantStatus.Joined);
        if (SessionSeats.PlaceOpened(updatedEvent.MaxPlayers, joinedBefore, joinedAfter))
            await _waitlist.NotifyPlaceOpenedAsync(updatedEvent);
    }

    // Redraws the card this button is on. A legacy embed card that refuses the V2 edit is reposted
    // instead (CardUpdater's fallback), so the click is never lost.
    private async Task RedrawHereAsync(SessionEvent session)
    {
        try
        {
            await ModifyOriginalResponseAsync(p => SessionCards.ApplyTo(p, session, Context.Guild));
        }
        catch (HttpException ex)
        {
            _logger.LogWarning(ex, "Session card {EventId} refused the edit; reposting it.", session.Id);
            await _repost.RepostAsync(Context.Guild, Context.Channel, new CardRef(CardKind.Session, session.Id));
        }
    }

    // Answers on the ephemeral message the click came from (panel, confirmation).
    private Task Done(string text) =>
        ModifyOriginalResponseAsync(p =>
        {
            p.Content = text;
            p.Components = new ComponentBuilder().Build();
        });
}
