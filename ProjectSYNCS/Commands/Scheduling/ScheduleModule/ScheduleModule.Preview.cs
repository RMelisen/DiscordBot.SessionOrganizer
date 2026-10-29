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
    // ---- The preview: the real card, everything a modal can't hold, « Publier » ----
    // Everything lives in the draft, so the custom-ids carry nothing.

    private static readonly int[] PlacesChoices = { 0, 2, 3, 4, 5, 6, 8, 10, 12, 16, 20 };
    private static readonly int[] DurationChoices = { 30, 60, 90, 120, 180, 240, 360 };

    // Most sessions it shares time with that the preview names.
    private const int MaxClashesShown = 2;

    /// <remarks>
    /// Budget: the line 1 + the card without buttons 8 + four select rows 8 + the button row 6 = 23 of
    /// 40. Text: the card's (about 1500 with no one signed up) and two clash lines.
    /// </remarks>
    private MessageComponent Preview(ScheduleDraft draft, IReadOnlyList<SessionEvent> clashes)
    {
        var head = new StringBuilder("**Aperçu** — rien n'est publié avant **Publier**.");
        foreach (var other in clashes.Take(MaxClashesShown))
        {
            var start = other.ScheduledAt.ToUnixTimeSeconds();
            var end = (other.ScheduledAt + other.Length).ToUnixTimeSeconds();
            head.Append($"\n⚠️ En même temps que **{Format.Sanitize(other.Title)}** (<t:{start}:t>–<t:{end}:t>, <t:{start}:D>).");
        }
        if (clashes.Count > MaxClashesShown)
            head.Append($"\n⚠️ Et {clashes.Count - MaxClashesShown} autre(s) session(s) au même moment.");

        var places = new SelectMenuBuilder().WithCustomId("schedule:rv:places");
        foreach (var n in PlacesChoices.Append(draft.MaxPlayers).Distinct().Order())
            places.AddOption(n == 0 ? "Places illimitées" : $"{n} places", n.ToString(), isDefault: n == draft.MaxPlayers);

        var duration = new SelectMenuBuilder().WithCustomId("schedule:rv:duration");
        foreach (var minutes in DurationChoices.Append(draft.DurationMinutes).Distinct().Order())
            duration.AddOption($"Durée : {SessionTime.DurationLabel(minutes)}", minutes.ToString(),
                isDefault: minutes == draft.DurationMinutes);

        var voice = new SelectMenuBuilder()
            .WithType(ComponentType.ChannelSelect)
            .WithCustomId("schedule:rv:voice")
            .WithPlaceholder("Salon vocal (facultatif)")
            .WithChannelTypes(ChannelType.Voice, ChannelType.Stage)
            .WithMinValues(0)
            .WithMaxValues(1);
        if (draft.VoiceChannelId != 0)
            voice.WithDefaultValues(new SelectMenuDefaultValue(draft.VoiceChannelId, SelectDefaultValueType.Channel));

        return new ComponentBuilderV2()
            .AddComponent(new TextDisplayBuilder(head.ToString()))
            .AddComponent(SessionCards.Container(PreviewSession(draft), Context.User, DateTimeOffset.UtcNow))
            .AddComponent(new ActionRowBuilder().WithSelectMenu(SessionCategories.Select("schedule:rv:category", draft.Category)))
            .AddComponent(new ActionRowBuilder().WithSelectMenu(places))
            .AddComponent(new ActionRowBuilder().WithSelectMenu(duration))
            .AddComponent(new ActionRowBuilder().WithSelectMenu(voice))
            .AddComponent(new ActionRowBuilder()
                .WithButton("Publier", "schedule:rv:publish", ButtonStyle.Success, new Emoji("✅"))
                .WithButton("Chaque semaine", "schedule:rv:weekly",
                    draft.Weekly ? ButtonStyle.Primary : ButtonStyle.Secondary, new Emoji("🔁"))
                .WithButton("Changer l'heure", "schedule:rv:time", ButtonStyle.Secondary)
                .WithButton("Modifier", "schedule:rv:edit", ButtonStyle.Secondary)
                .WithButton("Annuler", "schedule:rv:cancel", ButtonStyle.Danger))
            .Build();
    }

    // The session as it would be published; never saved (id 0, no one signed up). A weekly one
    // gets a series id so the card shows its « chaque samedi » badge.
    private SessionEvent PreviewSession(ScheduleDraft draft) => new()
    {
        GuildId = Context.Guild.Id,
        ChannelId = Context.Channel.Id,
        OrganizerId = Context.User.Id,
        Title = draft.Title,
        Category = draft.Category,
        ScheduledAt = draft.At!.Value,
        MaxPlayers = draft.MaxPlayers,
        DurationMinutes = draft.DurationMinutes,
        Description = draft.Description,
        VoiceChannelId = draft.VoiceChannelId,
        RecurringSessionId = draft.Weekly ? 0 : null,
    };

    // Shows (or redraws) the preview. The clash line reads the sessions before anything can show:
    // defer first (root CLAUDE.md, reliability).
    private async Task ShowPreviewAsync(ScheduleDraft draft)
    {
        _drafts.Set(Context.User.Id, draft);
        await DeferAsync(ephemeral: true);

        var at = draft.At!.Value;
        var length = TimeSpan.FromMinutes(draft.DurationMinutes);
        var clashes = (await _eventService.GetUpcomingOrRunningAsync(Context.Guild.Id))
            .Where(e => SessionTime.Overlaps(at, length, e.ScheduledAt, e.Length))
            .ToList();
        await ShowDeferredAsync(Preview(draft, clashes));
    }

    [ComponentInteraction("schedule:rv:category", ignoreGroupNames: true)]
    public async Task OnPreviewCategoryAsync(string[] values)
    {
        if (await PreviewDraftAsync() is not { } draft) return;
        if (values.Length > 0 && Enum.TryParse<SessionCategory>(values[0], out var category)) draft.Category = category;
        await ShowPreviewAsync(draft);
    }

    [ComponentInteraction("schedule:rv:places", ignoreGroupNames: true)]
    public async Task OnPreviewPlacesAsync(string[] values)
    {
        if (await PreviewDraftAsync() is not { } draft) return;
        if (values.Length > 0 && int.TryParse(values[0], out var places) && (places == 0 || SessionSeats.IsValidCap(places)))
            draft.MaxPlayers = places;
        await ShowPreviewAsync(draft);
    }

    [ComponentInteraction("schedule:rv:duration", ignoreGroupNames: true)]
    public async Task OnPreviewDurationAsync(string[] values)
    {
        if (await PreviewDraftAsync() is not { } draft) return;
        if (values.Length > 0 && int.TryParse(values[0], out var minutes)) draft.DurationMinutes = minutes;
        await ShowPreviewAsync(draft);
    }

    // A channel select's values are the picked ids; none when it was cleared.
    [ComponentInteraction("schedule:rv:voice", ignoreGroupNames: true)]
    public async Task OnPreviewVoiceAsync()
    {
        if (await PreviewDraftAsync() is not { } draft) return;
        var values = ((SocketMessageComponent)Context.Interaction).Data.Values;
        draft.VoiceChannelId = values?.FirstOrDefault() is { } id && ulong.TryParse(id, out var channelId) ? channelId : 0;
        await ShowPreviewAsync(draft);
    }

    [ComponentInteraction("schedule:rv:weekly", ignoreGroupNames: true)]
    public async Task OnPreviewWeeklyAsync()
    {
        if (await PreviewDraftAsync() is not { } draft) return;
        draft.Weekly = !draft.Weekly;
        await ShowPreviewAsync(draft);
    }

    // Back to step 1, the rest kept; a time picked there comes straight back here.
    [ComponentInteraction("schedule:rv:time", ignoreGroupNames: true)]
    public async Task OnPreviewTimeAsync()
    {
        if (await PreviewDraftAsync() is not { } draft) return;
        await UpdateStepAsync(TimeStep(draft));
    }

    // Back to the modal, pre-filled; its submit lands on the preview again.
    [ComponentInteraction("schedule:rv:edit", ignoreGroupNames: true)]
    public async Task OnPreviewEditAsync()
    {
        if (await PreviewDraftAsync() is not { } draft) return;
        await RespondWithModalAsync(DetailsModal("schedule:details", draft));
    }

    [ComponentInteraction("schedule:rv:cancel", ignoreGroupNames: true)]
    public async Task OnPreviewCancelAsync()
    {
        _drafts.Remove(Context.User.Id);
        await UpdateStepAsync(Step("Session abandonnée, rien n'a été publié."));
    }

    [ComponentInteraction("schedule:rv:publish", ignoreGroupNames: true)]
    public async Task OnPreviewPublishAsync()
    {
        // Claimed first: of two clicks on « Publier », one publishes.
        if (!_drafts.TryTake(Context.User.Id, out var draft) || draft.At is null || draft.Title.Length == 0)
        {
            await RespondAsync("Cette session en préparation a expiré. Relance `/schedule create`.", ephemeral: true);
            return;
        }
        if (draft.At <= DateTimeOffset.UtcNow)
        {
            _drafts.Set(Context.User.Id, draft);
            await RespondAsync("⚠️ Ce moment est passé : **Changer l'heure** avant de publier.", ephemeral: true);
            return;
        }

        // A write, a send and another write before the preview can close: defer first.
        await DeferAsync(ephemeral: true);
        var (session, error) = await CreateAndPostAsync(draft);
        if (session is null)
        {
            _drafts.Set(Context.User.Id, draft);
            await FollowupAsync(error, ephemeral: true);
            return;
        }

        _habits.Remember(session);
        if (draft.PollId is { } id) await LinkPollAsync(id, session);
        await ShowDeferredAsync(Step(Created(session)));
    }

    // The draft behind the preview, or an answer saying it's gone.
    private async Task<ScheduleDraft?> PreviewDraftAsync()
    {
        if (_drafts.TryGet(Context.User.Id, out var draft) && draft.At is not null && draft.Title.Length > 0) return draft;
        await RespondAsync("Cette session en préparation a expiré. Relance `/schedule create`.", ephemeral: true);
        return null;
    }

    // ---- Showing a step --------------------------------------------------------

    // A text and its rows, as a Components V2 message.
    private static MessageComponent Step(string text, params ActionRowBuilder[] rows)
    {
        var builder = new ComponentBuilderV2().AddComponent(new TextDisplayBuilder(text));
        foreach (var row in rows) builder.AddComponent(row);
        return builder.Build();
    }

    // V2 on every edit (root CLAUDE.md): the flag re-asserted, no content, no embeds, no pings.
    private static void ApplyStep(MessageProperties msg, MessageComponent components)
    {
        msg.Content = string.Empty;
        msg.Embeds = Array.Empty<Embed>();
        msg.Components = components;
        msg.Flags = MessageFlags.ComponentsV2;
        msg.AllowedMentions = AllowedMentions.None;
    }

    // A button or select on the wizard message: that message becomes the next step.
    private async Task UpdateStepAsync(MessageComponent components) =>
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg => ApplyStep(msg, components));

    // After a defer: the wizard message edited in place. A modal opened straight from the command
    // has no message, and a poll's « Créer une session » steps are not V2 (a message may refuse
    // the switch): a new private message then.
    private async Task ShowDeferredAsync(MessageComponent components)
    {
        var hasMessage = Context.Interaction switch
        {
            SocketMessageComponent => true,
            SocketModal modal => modal.Message is not null,
            _ => false,
        };
        if (hasMessage)
        {
            try
            {
                await Context.Interaction.ModifyOriginalResponseAsync(msg => ApplyStep(msg, components));
                return;
            }
            catch (HttpException ex)
            {
                _logger.LogDebug(ex, "The wizard message refused the session preview; sending a new one.");
            }
        }
        await FollowupAsync(components: components, flags: MessageFlags.ComponentsV2, ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    // A poll's « Créer une session » made this one: the poll remembers it, and its card links to it.
    private async Task LinkPollAsync(int pollId, SessionEvent session)
    {
        try
        {
            await _polls.SetSessionAsync(pollId, session.Id);
            if (await _polls.GetPollWithVotesAsync(pollId) is { } poll)
                await _cards.RedrawPollAsync(Context.Guild, poll, SessionCards.Link(session));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to link poll {PollId} to session {EventId}.", pollId, session.Id);
        }
    }

    // Whoever voted for the session's slot is on it as « peut-être » before its card goes out, so the
    // card already lists them. A failure costs the signups, never the session.
    private async Task SignUpVotersAsync(int pollId, SessionEvent session)
    {
        try
        {
            if (await _polls.GetPollWithVotesAsync(pollId) is not { } poll) return;
            var voters = PollRules.VotersAt(poll, session.ScheduledAt).Where(v => v.UserId != Context.User.Id);
            await _eventService.AddMaybesAsync(session, voters);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sign up the voters of poll {PollId} on session {EventId}.", pollId, session.Id);
        }
    }

    /// <summary>
    /// Creates the session, posts its card here and, for a weekly one, its series. A card that
    /// can't be posted takes its row (and series) with it, and the reason comes back instead.
    /// </summary>
    private async Task<(SessionEvent? Session, string? Error)> CreateAndPostAsync(ScheduleDraft draft)
    {
        var scheduledAt = draft.At!.Value;
        RecurringSession? series = null;
        if (draft.Weekly)
        {
            var (day, minute) = Recurrence.SlotOf(scheduledAt);
            series = await _eventService.CreateSeriesAsync(new RecurringSession
            {
                GuildId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                OrganizerId = Context.User.Id,
                Title = draft.Title,
                Category = draft.Category,
                DayOfWeek = day,
                MinuteOfDay = minute,
                MaxPlayers = draft.MaxPlayers,
                DurationMinutes = draft.DurationMinutes,
                VoiceChannelId = draft.VoiceChannelId,
                Description = draft.Description,
                LastOccurrence = scheduledAt,
            });
        }

        var session = await _eventService.CreateEventAsync(
            guildId: Context.Guild.Id,
            channelId: Context.Channel.Id,
            organizerId: Context.User.Id,
            title: draft.Title,
            category: draft.Category,
            scheduledAt: scheduledAt,
            maxPlayers: draft.MaxPlayers,
            durationMinutes: draft.DurationMinutes,
            description: draft.Description,
            voiceChannelId: draft.VoiceChannelId,
            recurringSessionId: series?.Id);

        if (draft.PollId is { } pollId) await SignUpVotersAsync(pollId, session);

        try
        {
            var message = await SessionCards.SendAsync(Context.Channel, session, Context.Guild);
            await _eventService.SetMessageIdAsync(session.Id, message.Id);
            session.MessageId = message.Id;
            return (session, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post the card of session {EventId}; removing it.", session.Id);
            await _eventService.DeleteEventAsync(session.Id);
            if (series is not null) await _eventService.DeleteSeriesAsync(series.Id);
            return (null, CardPostFailure.Explain(Context.Guild, Context.Channel, "la carte de la session"));
        }
    }

    private static string Created(SessionEvent session)
    {
        var line = $"Session **#{session.Id}** créée : {SessionCards.Link(session)}";
        if (session.RecurringSessionId is not null)
            line += $"\n🔁 Elle reviendra chaque {Recurrence.DayName(AppTime.ToZoned(session.ScheduledAt).DayOfWeek)}, "
                + $"postée {RecurringSession.DefaultLeadDays} jours avant. « ⚙️ Gérer » sur la carte pour arrêter la série.";
        return line;
    }

    private static string WhenRefusal(string when) =>
        $"Je n'ai pas compris « {Format.Sanitize(when)} » comme une date à venir (dans les 90 jours). "
        + $"Exemples : {FrenchDate.Examples}.";
}
