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
public partial class ScheduleModule : InteractionModuleBase<SocketInteractionContext>
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
}
