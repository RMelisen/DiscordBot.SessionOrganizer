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

[Group("schedule", "Commandes de planification de sessions")]
// Guild-only: this module reads Context.Guild, which is null in a DM, and
// config.yaml ships register_globally: true (a global command is DM-enabled by
// default). Without this it is reachable somewhere it can only throw.
[CommandContextType(InteractionContextType.Guild)]
public class ScheduleModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EventService _eventService;
    private readonly PollService _polls;
    private readonly CardRepost _repost;
    private readonly CardUpdater _cards;
    private readonly SessionWaitlist _waitlist;
    private readonly SchedulingLocks _locks;
    private readonly ManageSnapshots _snapshots;
    private readonly SessionHabits _habits;
    private readonly ILogger<ScheduleModule> _logger;

    // The session being set up, keyed by user: what /schedule create was given, the person's last
    // choices (SessionHabits) for the rest, then whatever the picker and the preview set. Thirty
    // minutes without a touch and it's gone.
    private static readonly DraftBox<ScheduleDraft> _drafts = new(TimeSpan.FromMinutes(30));

    private sealed class ScheduleDraft
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        // As typed in « Autre date… », so « Corriger » can show it back.
        public string When { get; set; } = string.Empty;
        public SessionCategory Category { get; set; } = SessionCategory.Game;
        public int MaxPlayers { get; set; }
        public int DurationMinutes { get; set; } = SessionEvent.DefaultDurationMinutes;
        public ulong VoiceChannelId { get; set; }
        public bool Weekly { get; set; }
        // The quick times' hour: the person's usual one, 21:00 otherwise.
        public int MinuteOfDay { get; set; } = SessionTime.DefaultHour * 60;
        // Null until a time is picked; the preview needs one.
        public DateTimeOffset? At { get; set; }
        // The closed poll this session comes from (« Créer une session »), linked on publish.
        public int? PollId { get; set; }
    }

    public ScheduleModule(EventService eventService, PollService polls, CardRepost repost, CardUpdater cards,
        SessionWaitlist waitlist, SchedulingLocks locks, ManageSnapshots snapshots, SessionHabits habits,
        ILogger<ScheduleModule> logger)
    {
        _eventService = eventService;
        _polls = polls;
        _repost = repost;
        _cards = cards;
        _waitlist = waitlist;
        _locks = locks;
        _snapshots = snapshots;
        _habits = habits;
        _logger = logger;
    }

    // ---- /schedule create: one shot with title + when, the picker otherwise ----
    // The picker: when and name typed in one modal (or only the name, when the time was given) → the
    // preview, where everything else is picked, then « Publier ». « Changer l'heure » goes back to
    // the when step (one click, day → hour → minutes, or typed).
    // Every step is a Components V2 message (root CLAUDE.md), the preview being the real card.

    [SlashCommand("create", "Planifier une nouvelle session")]
    public async Task ScheduleCreateAsync(
        [Summary("title", "Le nom de la session"), MaxLength(InputCaps.Title)] string title = "",
        [Summary("when", "Quand : « samedi 21h », « demain », « 25/12 20:30 », « ce soir »"), MaxLength(InputCaps.When),
         Autocomplete(typeof(SessionWhenAutocompleteHandler))] string when = "",
        [Summary("category", "Le type de session"),
         Choice("🎮 Jeu", nameof(SessionCategory.Game)), Choice("🧑‍🤝‍🧑 Activité", nameof(SessionCategory.Activity)),
         Choice("🎬 Film", nameof(SessionCategory.Movie)), Choice("✨ Autre", nameof(SessionCategory.Other))] string category = "",
        [Summary("places", "Places : 0 = illimité, sinon 2 à 67"), MinValue(0), MaxValue(SessionSeats.MaxPlayers)] int places = -1,
        // 0 = not given: the one-line path takes 2 h, the picker the person's last duration.
        [Summary("duration", "Durée (2 h si rien)"),
         Choice("30 min", 30), Choice("1 h", 60), Choice("1 h 30", 90), Choice("2 h", 120),
         Choice("3 h", 180), Choice("4 h", 240), Choice("6 h", 360)] int duration = 0,
        [Summary("voice", "Le salon vocal de la session"), ChannelTypes(ChannelType.Voice, ChannelType.Stage)] IGuildChannel? voice = null,
        [Summary("description", "Un mot sur la session, affiché sur la carte"), MaxLength(InputCaps.Description)] string description = "",
        [Summary("weekly", "La reposter chaque semaine, même jour, même heure")] bool weekly = false)
    {
        if (places >= 0 && !SessionSeats.IsValidCap(places))
        {
            await RespondAsync(SessionSeats.CapRule, ephemeral: true);
            return;
        }

        DateTimeOffset? at = null;
        if (when.Trim().Length > 0)
        {
            at = SessionTime.Parse(when.Trim(), DateTimeOffset.UtcNow);
            if (at is null)
            {
                await RespondAsync(WhenRefusal(when.Trim()), ephemeral: true);
                return;
            }
        }
        SessionCategory? picked = Enum.TryParse<SessionCategory>(category, out var c) ? c : null;

        // Everything needed: the session exists in one command, with exactly what was typed (no
        // remembered choices: nothing shows them before the card is out).
        if (title.Trim().Length > 0 && at is { } start)
        {
            await DeferAsync(ephemeral: true);
            var oneShot = new ScheduleDraft
            {
                Title = title.Trim(),
                Description = description.Trim(),
                Category = picked ?? SessionCategory.Game,
                MaxPlayers = Math.Max(places, 0),
                DurationMinutes = duration > 0 ? duration : SessionEvent.DefaultDurationMinutes,
                VoiceChannelId = voice?.Id ?? 0,
                Weekly = weekly,
                At = start,
            };
            var (session, error) = await CreateAndPostAsync(oneShot);
            if (session is not null) _habits.Remember(session);
            await FollowupAsync(error ?? Created(session!), ephemeral: true);
            return;
        }

        // The picker starts from the person's last choices; what the command was given wins.
        var draft = NewDraft();
        draft.Title = title.Trim();
        draft.Description = description.Trim();
        draft.Weekly = weekly;
        draft.At = at;
        if (picked is { } cat) draft.Category = cat;
        if (places >= 0) draft.MaxPlayers = places;
        if (duration > 0) draft.DurationMinutes = duration;
        if (voice is not null) draft.VoiceChannelId = voice.Id;
        _drafts.Set(Context.User.Id, draft);

        // The time is known, the name isn't (both known went out above). Otherwise the date is typed,
        // with the name: the buttons (TimeStep) stay for « Changer l'heure » on the preview.
        await RespondWithModalAsync(at is not null ? DetailsModal("schedule:details", draft) : TypedModal(draft));
    }

    private ScheduleDraft NewDraft(int? pollId = null)
    {
        var draft = new ScheduleDraft { PollId = pollId };
        if (_habits.For(Context.Guild.Id, Context.User.Id) is { } habit)
        {
            draft.Category = habit.Category;
            draft.MaxPlayers = habit.MaxPlayers;
            draft.DurationMinutes = habit.DurationMinutes;
            // A channel deleted since is forgotten rather than shown as « #inconnu ».
            draft.VoiceChannelId = Context.Guild.GetChannel(habit.VoiceChannelId) is not null ? habit.VoiceChannelId : 0;
            draft.MinuteOfDay = habit.MinuteOfDay;
        }
        return draft;
    }

    // The draft a step works on, or an answer saying it's gone.
    private async Task<ScheduleDraft?> DraftAsync()
    {
        if (_drafts.TryGet(Context.User.Id, out var draft)) return draft;
        await RespondAsync("Cette session en préparation a expiré. Relance `/schedule create`.", ephemeral: true);
        return null;
    }

    // ---- Step 1: when -------------------------------------------------------

    private static MessageComponent TimeStep(ScheduleDraft draft)
    {
        var heading = draft.Title.Length > 0 ? $"**{Format.Sanitize(draft.Title)}** — quand ?" : "**Nouvelle session** — quand ?";
        var quick = new ActionRowBuilder();
        foreach (var (label, at) in SessionTime.QuickSlots(DateTimeOffset.UtcNow, draft.MinuteOfDay))
            quick.WithButton(label, $"schedule:quick:{at.ToUnixTimeSeconds()}", ButtonStyle.Primary);

        // The quick row is full (four times, five buttons a row): any other time is typed, below.
        var other = new ActionRowBuilder()
            .WithButton("Taper une date", "schedule:typedate", ButtonStyle.Secondary, new Emoji("⌨️"));
        // Coming back from the preview (« Changer l'heure »): a way back without changing anything.
        if (draft.At is not null)
            other.WithButton("Retour à l'aperçu", "schedule:back:preview", ButtonStyle.Secondary);
        return Step(heading, quick, other);
    }

    // Only the date: the name and description were set by the first modal and stay as they are
    // (« Modifier » on the preview changes them). A draft with no name gets the full modal.
    [ComponentInteraction("schedule:typedate", ignoreGroupNames: true)]
    public async Task OnTypeDateAsync()
    {
        _drafts.TryGet(Context.User.Id, out var draft);
        await RespondWithModalAsync(draft is { Title.Length: > 0 } ? WhenModal(draft) : TypedModal(draft));
    }

    [ComponentInteraction("schedule:quick:*", ignoreGroupNames: true)]
    public async Task OnQuickTimeAsync(string unix)
    {
        if (!long.TryParse(unix, out var seconds))
        {
            await RespondAsync("Créneau invalide. Relance `/schedule create`.", ephemeral: true);
            return;
        }
        await TimeChosenAsync(DateTimeOffset.FromUnixTimeSeconds(seconds));
    }

    // A time is set: the name next if it's still missing, the preview otherwise.
    private async Task TimeChosenAsync(DateTimeOffset at)
    {
        if (await DraftAsync() is not { } draft) return;
        // A quick time can go stale on a wizard left open across it.
        if (at <= DateTimeOffset.UtcNow)
        {
            await RespondAsync("Ce moment est déjà passé : choisis-en un autre.", ephemeral: true);
            return;
        }
        draft.At = at;
        _drafts.Set(Context.User.Id, draft);
        if (draft.Title.Length == 0)
            await RespondWithModalAsync(DetailsModal("schedule:details", draft));
        else
            await ShowPreviewAsync(draft);
    }

    [ComponentInteraction("schedule:back:preview", ignoreGroupNames: true)]
    public async Task OnBackToPreviewAsync()
    {
        if (await DraftAsync() is not { } draft) return;
        await ShowPreviewAsync(draft);
    }

    // ---- The modals: name and description, built by hand to pre-fill them ----

    // Field ids must match ScheduleEventModal. A poll's « Créer une session » builds it too.
    public static Modal DetailsModal(string customId, string? title = null, string? description = null) =>
        new ModalBuilder()
            .WithTitle("Planifier une session")
            .WithCustomId(customId)
            .AddTextInput("Nom de la session", "title", maxLength: InputCaps.Title,
                placeholder: "ex. Among Us, Gartic, Anime ?", value: title, required: true)
            .AddTextInput("Description (facultatif)", "description", TextInputStyle.Paragraph,
                maxLength: InputCaps.Description, value: description, required: false)
            .Build();

    private static Modal DetailsModal(string customId, ScheduleDraft? draft) =>
        DetailsModal(customId, NullIfEmpty(draft?.Title), NullIfEmpty(draft?.Description));

    // /schedule create's first step: the date typed, with the name, in one modal. Field ids must
    // match ScheduleTypedModal.
    private static Modal TypedModal(ScheduleDraft? draft) =>
        new ModalBuilder()
            .WithTitle("Planifier une session")
            .WithCustomId("schedule:typed")
            .AddTextInput("Nom de la session", "title", maxLength: InputCaps.Title,
                placeholder: "ex. Among Us, Gartic, Anime ?", value: NullIfEmpty(draft?.Title), required: true)
            .AddTextInput("Quand", "when", maxLength: InputCaps.When,
                placeholder: "ex. samedi 21h, 25/12 20:30, demain", value: NullIfEmpty(draft?.When), required: true)
            .AddTextInput("Description (facultatif)", "description", TextInputStyle.Paragraph,
                maxLength: InputCaps.Description, value: NullIfEmpty(draft?.Description), required: false)
            .Build();

    // « Taper une date » once the name is known: the date alone. Field id must match ScheduleWhenModal.
    private static Modal WhenModal(ScheduleDraft draft) =>
        new ModalBuilder()
            .WithTitle(Truncate($"Quand ? — {draft.Title}", 45))
            .WithCustomId("schedule:typedwhen")
            .AddTextInput("Quand", "when", maxLength: InputCaps.When,
                placeholder: "ex. samedi 21h, 25/12 20:30, demain", value: NullIfEmpty(draft.When), required: true)
            .Build();

    // Discord caps a modal's title at 45 characters.
    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";

    private static string? NullIfEmpty(string? text) => string.IsNullOrEmpty(text) ? null : text;

    [ModalInteraction("schedule:details", ignoreGroupNames: true)]
    public async Task OnDetailsSubmittedAsync(ScheduleEventModal modal)
    {
        if (!_drafts.TryGet(Context.User.Id, out var draft) || draft.At is null)
        {
            await RespondAsync("Cette session en préparation a expiré. Relance `/schedule create`.", ephemeral: true);
            return;
        }
        draft.Title = modal.SessionTitle.Trim();
        draft.Description = modal.Description.Trim();
        await ShowPreviewAsync(draft);
    }

    // A closed poll turned into a session (PollModule.OnCategoryPickedAsync builds this modal): a
    // fresh draft, so options left from an earlier /schedule create never leak into it.
    [ModalInteraction("schedule:frompoll:*:*:*", ignoreGroupNames: true)]
    public async Task OnFromPollSubmittedAsync(string pollIdStr, string categoryStr, string unixStr, ScheduleEventModal modal)
    {
        if (!int.TryParse(pollIdStr, out var pollId) || !long.TryParse(unixStr, out var unix))
        {
            await RespondAsync("Créneau invalide.", ephemeral: true);
            return;
        }
        var draft = NewDraft(pollId);
        if (Enum.TryParse<SessionCategory>(categoryStr, out var category)) draft.Category = category;
        draft.At = DateTimeOffset.FromUnixTimeSeconds(unix);
        draft.Title = modal.SessionTitle.Trim();
        draft.Description = modal.Description.Trim();
        await ShowPreviewAsync(draft);
    }

    [ModalInteraction("schedule:typed", ignoreGroupNames: true)]
    public async Task OnTypedModalSubmittedAsync(ScheduleTypedModal modal)
    {
        _drafts.TryGet(Context.User.Id, out var draft);
        draft ??= NewDraft();
        draft.Title = modal.SessionTitle.Trim();
        draft.Description = modal.Description.Trim();
        draft.When = modal.When.Trim();

        var at = SessionTime.Parse(draft.When, DateTimeOffset.UtcNow);
        if (at is null)
        {
            _drafts.Set(Context.User.Id, draft);
            await RefuseTypedWhenAsync(draft.When, "schedule:retrytyped");
            return;
        }
        draft.At = at;
        await ShowPreviewAsync(draft);
    }

    [ModalInteraction("schedule:typedwhen", ignoreGroupNames: true)]
    public async Task OnWhenModalSubmittedAsync(ScheduleWhenModal modal)
    {
        if (await DraftAsync() is not { } draft) return;
        draft.When = modal.When.Trim();
        _drafts.Set(Context.User.Id, draft);

        var at = SessionTime.Parse(draft.When, DateTimeOffset.UtcNow);
        if (at is null)
        {
            await RefuseTypedWhenAsync(draft.When, "schedule:retrywhen");
            return;
        }
        draft.At = at;
        await ShowPreviewAsync(draft);
    }

    // A typed date it couldn't read: why, and « Corriger » to reopen the same modal pre-filled.
    private async Task RefuseTypedWhenAsync(string when, string retryId)
    {
        var retry = Step($"⚠️ {WhenRefusal(when)}",
            new ActionRowBuilder().WithButton("Corriger", retryId, ButtonStyle.Primary));
        // Opened straight from /schedule create there is no wizard message to edit yet.
        var submitted = (SocketModal)Context.Interaction;
        if (submitted.Message is null)
            await RespondAsync(components: retry, flags: MessageFlags.ComponentsV2, ephemeral: true,
                allowedMentions: AllowedMentions.None);
        else
            await submitted.UpdateAsync(msg => ApplyStep(msg, retry));
    }

    [ComponentInteraction("schedule:retrytyped", ignoreGroupNames: true)]
    public async Task OnRetryTypedAsync()
    {
        _drafts.TryGet(Context.User.Id, out var draft);
        await RespondWithModalAsync(TypedModal(draft));
    }

    [ComponentInteraction("schedule:retrywhen", ignoreGroupNames: true)]
    public async Task OnRetryWhenAsync()
    {
        if (await DraftAsync() is not { } draft) return;
        await RespondWithModalAsync(WhenModal(draft));
    }

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
