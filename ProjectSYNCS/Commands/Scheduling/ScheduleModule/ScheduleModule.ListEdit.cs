using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Autocomplete;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;
using System.Globalization;
using System.Text;

namespace ProjectSYNCS.Commands;

public partial class ScheduleModule : InteractionModuleBase<SocketInteractionContext>
{
    // ---- /schedule list ---------------------------------------------------

    [SlashCommand("list", "Lister les sessions actives du serveur")]
    public async Task ScheduleListAsync()
    {
        await DeferAsync(ephemeral: true);

        var events = await _eventService.GetActiveEventsAsync(Context.Guild.Id);
        var series = await _eventService.GetActiveSeriesAsync(Context.Guild.Id);
        if (events.Count == 0 && series.Count == 0)
        {
            await FollowupAsync("Aucune session active pour le moment.", ephemeral: true);
            return;
        }

        var sb = new StringBuilder();
        // Capped like the select below: the list must stay inside one embed description.
        foreach (var e in events.Take(25))
        {
            int joined = e.Participants.Count(p => p.Status == ParticipantStatus.Joined);
            string places = e.MaxPlayers == 0 ? $"{joined}" : $"{joined}/{e.MaxPlayers}";
            var weekly = e.RecurringSessionId is null ? "" : " 🔁";
            sb.AppendLine($"**#{e.Id}** {SessionCategories.Emoji(e.Category)} {Format.Sanitize(e.Title)}{weekly} — <t:{e.ScheduledAt.ToUnixTimeSeconds()}:F> — 👥 {places}");
        }
        if (events.Count > 25) sb.AppendLine($"… et {events.Count - 25} autre(s).");

        if (series.Count > 0)
        {
            sb.AppendLine().AppendLine("**Séries hebdomadaires**");
            foreach (var s in series.Take(10))
                sb.AppendLine($"🔁 {SessionCategories.Emoji(s.Category)} {Format.Sanitize(s.Title)} — chaque {Recurrence.DayName(s.DayOfWeek)} "
                    + $"à {s.MinuteOfDay / 60:D2}:{s.MinuteOfDay % 60:D2} — par <@{s.OrganizerId}>");
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Sessions actives ({events.Count})")
            .WithDescription(sb.ToString())
            .WithColor(Color.Blue)
            .Build();

        if (events.Count == 0)
        {
            await FollowupAsync(embed: embed, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        // Up to 25 options in a select menu.
        var menu = new SelectMenuBuilder()
            .WithCustomId("schedule:republish")
            .WithPlaceholder("Republier une carte dans ce salon");
        foreach (var e in events.Take(25))
        {
            string label = $"#{e.Id} — {e.Title}";
            if (label.Length > 100) label = label[..100];
            menu.AddOption(label, e.Id.ToString());
        }

        var components = new ComponentBuilder().WithSelectMenu(menu).Build();
        await FollowupAsync(embed: embed, components: components, ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    [ComponentInteraction("schedule:republish", ignoreGroupNames: true)]
    public async Task OnRepublishAsync(string[] values)
    {
        await DeferAsync(ephemeral: true);

        if (!int.TryParse(values[0], out int eventId))
        {
            await FollowupAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        var result = await _repost.RepostAsync(Context.Guild, Context.Channel, new CardRef(CardKind.Session, eventId));
        await FollowupAsync(result switch
        {
            RepostResult.NotFound => "Session introuvable.",
            RepostResult.Closed => "Cette session est annulée.",
            _ => $"Session **#{eventId}** republiée ici.",
        }, ephemeral: true);
    }

    // ---- /schedule cancel and /schedule edit ------------------------------

    [SlashCommand("cancel", "Annuler une session que tu as organisée")]
    public async Task ScheduleCancelAsync(
        [Summary("event-id", "La session à annuler"), Autocomplete(typeof(SessionIdAutocompleteHandler))] int eventId)
    {
        await DeferAsync(ephemeral: true);

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

        // The same confirmation as the card's « Annuler » (EventComponentHandler does the rest).
        await FollowupAsync(SessionManage.CancelPrompt(gameEvent), components: SessionManage.CancelConfirm(gameEvent.Id),
            ephemeral: true);
    }

    [SlashCommand("edit", "Modifier une session que tu as organisée")]
    public async Task ScheduleEditAsync(
        [Summary("event-id", "La session à modifier"), Autocomplete(typeof(SessionIdAutocompleteHandler))] int eventId)
    {
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

        await RespondWithModalAsync(BuildEditModal(gameEvent));
    }

    [ModalInteraction("event:editmodal:*", ignoreGroupNames: true)]
    public async Task OnEditModalSubmittedAsync(string eventIdStr, EditSessionModal modal)
    {
        await DeferAsync(ephemeral: true);

        if (!int.TryParse(eventIdStr, out int eventId))
        {
            await FollowupAsync("ID de session invalide.", ephemeral: true);
            return;
        }

        if (!SessionSeats.TryParseCap(modal.MaxPlayers, out var maxPlayers))
        {
            await FollowupAsync(SessionSeats.CapRule, ephemeral: true);
            return;
        }

        var durationInput = modal.Duration.Trim();
        int? duration = durationInput.Length == 0 ? null : SessionTime.ParseDuration(durationInput);
        if (durationInput.Length > 0 && duration is null)
        {
            await FollowupAsync("Durée invalide : « 2h », « 1h30 » ou « 45 min », entre 15 minutes et 6 heures.", ephemeral: true);
            return;
        }

        SessionEvent updated;
        int oldMaxPlayers;
        // Under the session's lock: a join landing during the edit can't slip past the new cap.
        using (await _locks.Sessions.AcquireAsync(eventId))
        {
            var gameEvent = await _eventService.GetEventWithParticipantsAsync(eventId);
            if (gameEvent is null || gameEvent.GuildId != Context.Guild.Id)
            {
                await FollowupAsync("Session introuvable.", ephemeral: true);
                return;
            }

            if (!SessionPermissions.CanManage(Context.User, gameEvent))
            {
                await FollowupAsync("Seul l'organisateur ou un administrateur peut modifier cette session.", ephemeral: true);
                return;
            }

            // Cancelled while the form was open.
            if (gameEvent.IsCancelled)
            {
                await FollowupAsync("Cette session a été annulée entre-temps : rien n'a été modifié.", ephemeral: true);
                return;
            }

            // The field left as it was keeps the time as it was, even for a session already under
            // way (read again, « 01/11 21:00 » once past would mean next year).
            var whenInput = modal.When.Trim();
            DateTimeOffset scheduledAt;
            if (whenInput == WhenInput(gameEvent.ScheduledAt))
                scheduledAt = gameEvent.ScheduledAt;
            else if (SessionTime.Parse(whenInput, DateTimeOffset.UtcNow) is { } at)
                scheduledAt = at;
            else
            {
                await FollowupAsync(WhenRefusal(whenInput), ephemeral: true);
                return;
            }

            int joined = gameEvent.Participants.Count(p => p.Status == ParticipantStatus.Joined);
            if (SessionSeats.CapBelowJoined(maxPlayers, joined))
            {
                await FollowupAsync(
                    $"Il y a déjà **{joined}** inscrits : la limite ne peut pas descendre en dessous. Personne n'est mis dehors.",
                    ephemeral: true);
                return;
            }

            oldMaxPlayers = gameEvent.MaxPlayers;
            await _eventService.UpdateEventAsync(eventId, modal.SessionTitle.Trim(), scheduledAt, maxPlayers,
                duration ?? gameEvent.DurationMinutes, modal.Description.Trim());
            updated = (await _eventService.GetEventWithParticipantsAsync(eventId))!;
            _snapshots.Forget(eventId);
            await _cards.RedrawSessionAsync(Context.Guild, updated);
        }

        // Keep the linked native Discord event in sync with the new details.
        await SessionEventSync.UpdateAsync(Context.Guild, updated, _logger);

        await FollowupAsync("Session mise à jour ✅", ephemeral: true);

        // A raised cap on a full session: the « peut-être » are its waiting list.
        int joinedNow = updated.Participants.Count(p => p.Status == ParticipantStatus.Joined);
        if (SessionSeats.PlaceOpenedByEdit(oldMaxPlayers, updated.MaxPlayers, joinedNow))
            await _waitlist.NotifyPlaceOpenedAsync(updated);
    }

    // The « Quand » field as the edit modal shows it, and reads back: « 01/11 21:00 ».
    public static string WhenInput(DateTimeOffset at) => AppTime.ToZoned(at).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture);

    // Field ids must match EditSessionModal.
    public static Modal BuildEditModal(SessionEvent gameEvent) =>
        new ModalBuilder()
            .WithTitle("Modifier la session")
            .WithCustomId($"event:editmodal:{gameEvent.Id}")
            .AddTextInput("Nom de la session", "title", maxLength: InputCaps.Title,
                value: gameEvent.Title, required: true)
            .AddTextInput("Quand", "when", maxLength: InputCaps.When,
                value: WhenInput(gameEvent.ScheduledAt), placeholder: "ex. samedi 21h, 25/12 20:30", required: true)
            .AddTextInput("Places (vide ou 0 = illimité)", "max_players", maxLength: 3,
                value: gameEvent.MaxPlayers.ToString(), required: false)
            .AddTextInput("Durée", "duration", maxLength: 12,
                value: SessionTime.DurationInput(gameEvent.DurationMinutes), placeholder: "ex. 2h, 1h30, 45 min", required: false)
            .AddTextInput("Description", "description", TextInputStyle.Paragraph, maxLength: InputCaps.Description,
                value: string.IsNullOrEmpty(gameEvent.Description) ? null : gameEvent.Description, required: false)
            .Build();
}
