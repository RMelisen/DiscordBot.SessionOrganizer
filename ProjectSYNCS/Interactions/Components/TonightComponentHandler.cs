using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /tonight's buttons. Every one touches the database before it can redraw, so each defers first.
public class TonightComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TonightService _tonight;
    private readonly EventService _events;
    private readonly ResponsePicker _picker;
    private readonly ILogger<TonightComponentHandler> _logger;

    public TonightComponentHandler(TonightService tonight, EventService events, ResponsePicker picker,
        ILogger<TonightComponentHandler> logger)
    {
        _tonight = tonight;
        _events = events;
        _picker = picker;
        _logger = logger;
    }

    private SocketMessageComponent Component => (SocketMessageComponent)Context.Interaction;

    [ComponentInteraction("tonight:in:*")]
    public Task OnInAsync(string id) => AnswerAsync(id, TonightStatus.In);

    [ComponentInteraction("tonight:maybe:*")]
    public Task OnMaybeAsync(string id) => AnswerAsync(id, TonightStatus.Maybe);

    [ComponentInteraction("tonight:out:*")]
    public Task OnOutAsync(string id) => AnswerAsync(id, TonightStatus.Out);

    private async Task AnswerAsync(string idStr, TonightStatus status)
    {
        await DeferAsync();
        if (!int.TryParse(idStr, out var id) || await _tonight.GetAsync(id) is not { } call || call.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Cet appel n'existe plus.", ephemeral: true);
            return;
        }
        if (call.Closed)
        {
            await FollowupAsync("Cet appel est terminé.", ephemeral: true);
            return;
        }

        var changed = await _tonight.AnswerAsync(id, Context.User.Id, status);
        var updated = (await _tonight.GetAsync(id))!;
        await Component.ModifyOriginalResponseAsync(p =>
        {
            p.Embed = TonightModule.BuildEmbed(updated);
            p.Components = TonightModule.BuildComponents(updated);
        });

        if (!changed)
        {
            await FollowupAsync("C'est déjà ce que tu avais répondu.", ephemeral: true);
            return;
        }

        // The minimum just got reached: one ping to the « chauds », once ever (the flag is written
        // before the message, so a crash costs the line, never a second ping).
        if (TonightModule.CountIn(updated) >= updated.Minimum && await _tonight.ClaimThresholdAsync(id))
            await AnnounceAsync(updated);
    }

    private async Task AnnounceAsync(TonightCall call)
    {
        try
        {
            var ids = call.Responses.Where(r => r.Status == TonightStatus.In).Select(r => r.UserId).ToList();
            var line = string.Format(_picker.Pick(BotResponses.TonightThresholdLines), ids.Count);
            await Context.Channel.SendMessageAsync(
                $"{line}\n{string.Join(" ", ids.Select(u => $"<@{u}>"))}\n" +
                $"-# <@{call.OrganizerId}> peut maintenant cliquer sur « Créer la session ».",
                allowedMentions: new AllowedMentions { UserIds = ids },
                messageReference: new MessageReference(call.MessageId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to announce tonight call {CallId}.", call.Id);
        }
    }

    [ComponentInteraction("tonight:session:*")]
    public async Task OnSessionAsync(string idStr)
    {
        if (!int.TryParse(idStr, out var id))
        {
            await RespondAsync("Cet appel n'existe plus.", ephemeral: true);
            return;
        }
        await DeferAsync();

        var call = await _tonight.GetAsync(id);
        if (call is null || call.GuildId != Context.Guild.Id || call.Closed)
        {
            await FollowupAsync("Cet appel est terminé.", ephemeral: true);
            return;
        }
        if (!SessionPermissions.CanManage(Context.User, call))
        {
            await FollowupAsync($"C'est <@{call.OrganizerId}> (ou le staff) qui lance la session.", ephemeral: true,
                allowedMentions: AllowedMentions.None);
            return;
        }
        if (call.StartsAt <= DateTimeOffset.UtcNow)
        {
            await FollowupAsync("L'heure est passée : plus besoin de session, jouez !", ephemeral: true);
            return;
        }
        // One session, whoever clicks first (and never after the midnight sweep).
        if (!await _tonight.ClaimCloseAsync(id))
        {
            await FollowupAsync("La session a déjà été créée.", ephemeral: true);
            return;
        }

        SessionEvent session;
        try
        {
            var title = string.IsNullOrEmpty(call.Game)
                ? $"Soirée du {AppTime.ToZoned(call.StartsAt):dd/MM}"
                : call.Game;
            session = await _events.CreateEventAsync(call.GuildId, call.ChannelId, call.OrganizerId,
                title, SessionCategory.Game, call.StartsAt, 0);
        }
        catch
        {
            // The claim was taken for a session that doesn't exist: give it back.
            await _tonight.ReopenAsync(id);
            throw;
        }

        foreach (var r in call.Responses.Where(r => r.Status != TonightStatus.Out))
        {
            var username = Context.Guild.GetUser(r.UserId)?.Username ?? "";
            var status = r.Status == TonightStatus.In ? ParticipantStatus.Joined : ParticipantStatus.Maybe;
            await _events.UpsertParticipantAsync(session, r.UserId, username, status);
        }
        await _tonight.SetSessionAsync(id, session.Id);

        var full = (await _events.GetEventWithParticipantsAsync(session.Id))!;
        var card = await Context.Channel.SendMessageAsync(
            embed: ScheduleModule.BuildEventEmbed(full, Context.Guild),
            components: ScheduleModule.BuildEventComponents(full));
        await _events.SetMessageIdAsync(session.Id, card.Id);

        call.Closed = true;
        call.SessionEventId = session.Id;
        await Component.ModifyOriginalResponseAsync(p =>
        {
            p.Embed = TonightModule.BuildEmbed(call);
            p.Components = TonightModule.BuildComponents(call);
        });
    }
}
