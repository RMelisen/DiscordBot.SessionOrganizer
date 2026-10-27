using Discord;
using Discord.Interactions;
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
    private readonly ILogger<ScheduleModule> _logger;

    // What /schedule create was given before the picker or a modal took over (title, places,
    // duration, voice, description, weekly), what a failed modal held so « Corriger » can
    // reopen it pre-filled, and the session the review step is setting up. Keyed by user;
    // thirty minutes without a touch and it's gone.
    private static readonly DraftBox<ScheduleDraft> _drafts = new(TimeSpan.FromMinutes(30));

    private sealed class ScheduleDraft
    {
        public string Title { get; set; } = string.Empty;
        // As typed, so a modal can show it back.
        public string Places { get; set; } = string.Empty;
        public string When { get; set; } = string.Empty;
        public int DurationMinutes { get; set; } = SessionEvent.DefaultDurationMinutes;
        public ulong VoiceChannelId { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool Weekly { get; set; }
        // Set once the modal is valid, for the review step.
        public SessionCategory Category { get; set; }
        public DateTimeOffset At { get; set; }
        public int MaxPlayers { get; set; }
        // The closed poll this session comes from (« Créer une session »), linked on publish.
        public int? PollId { get; set; }
    }

    public ScheduleModule(EventService eventService, PollService polls, CardRepost repost, CardUpdater cards,
        SessionWaitlist waitlist, SchedulingLocks locks, ManageSnapshots snapshots, ILogger<ScheduleModule> logger)
    {
        _eventService = eventService;
        _polls = polls;
        _repost = repost;
        _cards = cards;
        _waitlist = waitlist;
        _locks = locks;
        _snapshots = snapshots;
        _logger = logger;
    }

    // ---- /schedule create: one shot with title + when, the picker otherwise ----

    [SlashCommand("create", "Planifier une nouvelle session")]
    public async Task ScheduleCreateAsync(
        [Summary("title", "Le nom de la session"), MaxLength(InputCaps.Title)] string title = "",
        [Summary("when", "Quand : « samedi 21h », « demain », « 25/12 20:30 », « ce soir »"), MaxLength(InputCaps.When),
         Autocomplete(typeof(SessionWhenAutocompleteHandler))] string when = "",
        [Summary("category", "Le type de session"),
         Choice("🎮 Jeu", nameof(SessionCategory.Game)), Choice("🧑‍🤝‍🧑 Activité", nameof(SessionCategory.Activity)),
         Choice("🎬 Film", nameof(SessionCategory.Movie)), Choice("✨ Autre", nameof(SessionCategory.Other))] string category = "",
        [Summary("places", "Places : 0 = illimité, sinon 2 à 67"), MinValue(0), MaxValue(SessionSeats.MaxPlayers)] int places = -1,
        [Summary("duration", "Durée (2 h si rien)"),
         Choice("30 min", 30), Choice("1 h", 60), Choice("1 h 30", 90), Choice("2 h", 120),
         Choice("3 h", 180), Choice("4 h", 240), Choice("6 h", 360)] int duration = SessionEvent.DefaultDurationMinutes,
        [Summary("voice", "Le salon vocal de la session"), ChannelTypes(ChannelType.Voice, ChannelType.Stage)] IGuildChannel? voice = null,
        [Summary("description", "Un mot sur la session, affiché sur la carte"), MaxLength(InputCaps.Description)] string description = "",
        [Summary("weekly", "La reposter chaque semaine, même jour, même heure")] bool weekly = false)
    {
        if (places >= 0 && !SessionSeats.IsValidCap(places))
        {
            await RespondAsync(SessionSeats.CapRule, ephemeral: true);
            return;
        }

        var draft = new ScheduleDraft
        {
            Title = title.Trim(),
            Places = places > 0 ? places.ToString() : string.Empty,
            When = when.Trim(),
            DurationMinutes = duration,
            VoiceChannelId = voice?.Id ?? 0,
            Description = description.Trim(),
            Weekly = weekly,
        };

        DateTimeOffset? at = null;
        if (draft.When.Length > 0)
        {
            at = SessionTime.Parse(draft.When, DateTimeOffset.UtcNow);
            if (at is null)
            {
                await RespondAsync(WhenRefusal(draft.When), ephemeral: true);
                return;
            }
        }
        SessionCategory? picked = Enum.TryParse<SessionCategory>(category, out var c) ? c : null;

        // Everything needed: the session exists in one command.
        if (draft.Title.Length > 0 && at is { } start)
        {
            await DeferAsync(ephemeral: true);
            var (session, error) = await CreateAndPostAsync(picked ?? SessionCategory.Game, start, draft, Math.Max(places, 0));
            await FollowupAsync(error ?? Created(session!), ephemeral: true);
            return;
        }

        // Something missing: the picker opens on the first step it still needs.
        _drafts.Set(Context.User.Id, draft);
        if (at is { } known)
        {
            if (picked is { } cat)
            {
                await RespondWithModalAsync(DetailsModal($"schedule:finalize:{cat}:{known.ToUnixTimeSeconds()}", draft));
                return;
            }
            await RespondAsync($"Session le **{SessionTime.Preview(known)}** — **Quel type de session ?**",
                components: new ComponentBuilder()
                    .WithSelectMenu(SessionCategories.Select($"schedule:catat:{known.ToUnixTimeSeconds()}")).Build(),
                ephemeral: true);
            return;
        }
        if (picked is { } only)
        {
            await RespondAsync("**2/4** — Quel jour ?", components: BuildDayStep(only.ToString()), ephemeral: true);
            return;
        }
        await RespondAsync("**1/4** — Quel type de session ?", components: BuildCategoryStep(), ephemeral: true);
    }

    // The date was given, the category wasn't: straight to the details.
    [ComponentInteraction("schedule:catat:*", ignoreGroupNames: true)]
    public async Task OnCategoryForKnownDateAsync(string unix, string[] values)
    {
        _drafts.TryGet(Context.User.Id, out var draft);
        await RespondWithModalAsync(DetailsModal($"schedule:finalize:{values[0]}:{unix}", draft));
    }

    // ---- Wizard step 2: pick a day ---------------------------------------

    [ComponentInteraction("schedule:cat", ignoreGroupNames: true)]
    public async Task OnCategorySelectedAsync(string[] values)
    {
        var category = values[0];
        var component = (SocketMessageComponent)Context.Interaction;

        await component.UpdateAsync(msg =>
        {
            msg.Content = "**2/4** — Quel jour ?";
            msg.Components = BuildDayStep(category);
        });
    }

    // ---- Wizard step 3: pick an hour, or type the date -------------------

    [ComponentInteraction("schedule:day:*", ignoreGroupNames: true)]
    public async Task OnDaySelectedAsync(string category, string[] values)
    {
        var date = values[0];
        if (date == OtherDate)
        {
            _drafts.TryGet(Context.User.Id, out var draft);
            await RespondWithModalAsync(TypedModal(category, draft));
            return;
        }

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "**3/4** — À quelle heure ?";
            msg.Components = BuildHourStep(category, date);
        });
    }

    // ---- Wizard step 4: pick minutes (optional, defaults to :00) ----------

    [ComponentInteraction("schedule:hour:*:*", ignoreGroupNames: true)]
    public async Task OnHourSelectedAsync(string category, string date, string[] values)
    {
        var hour = values[0];
        var component = (SocketMessageComponent)Context.Interaction;

        await component.UpdateAsync(msg =>
        {
            msg.Content = "**4/4** — À quelle heure ?";
            msg.Components = BuildMinuteStep(category, date, hour);
        });
    }

    // ---- "Retour" navigation: rebuild the previous step ------------------

    [ComponentInteraction("schedule:back:cat", ignoreGroupNames: true)]
    public async Task OnBackToCategoryAsync()
    {
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "**1/4** — Quel type de session ?";
            msg.Components = BuildCategoryStep();
        });
    }

    [ComponentInteraction("schedule:back:day:*", ignoreGroupNames: true)]
    public async Task OnBackToDayAsync(string category)
    {
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "**2/4** — Quel jour ?";
            msg.Components = BuildDayStep(category);
        });
    }

    [ComponentInteraction("schedule:back:hour:*:*", ignoreGroupNames: true)]
    public async Task OnBackToHourAsync(string category, string date)
    {
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "**3/4** — À quelle heure ?";
            msg.Components = BuildHourStep(category, date);
        });
    }

    // ---- Final step: open the modal for the free-text details -------------

    [ComponentInteraction("schedule:min:*:*:*:*", ignoreGroupNames: true)]
    public async Task OnMinuteSelectedAsync(string category, string date, string hour, string minute)
    {
        // The wall clock becomes an instant here, once: from now on the custom-ids carry unix
        // seconds, which have no DST ambiguity and no ':' to trip a wildcard.
        if (!AppTime.TryParseWallClock($"{date}T{hour}:{minute}", "yyyy-MM-ddTHH:mm", out var at))
        {
            await RespondAsync("Date invalide. Relance `/schedule create`.", ephemeral: true);
            return;
        }
        _drafts.TryGet(Context.User.Id, out var draft);
        await RespondWithModalAsync(DetailsModal($"schedule:finalize:{category}:{at.ToUnixTimeSeconds()}", draft));
    }

    // ---- Step component builders (shared by forward + "Retour" paths) -----

    // The day select's last option: the date typed instead, so the picker has no 24-day wall.
    private const string OtherDate = "other";

    private static MessageComponent BuildCategoryStep() =>
        new ComponentBuilder().WithSelectMenu(SessionCategories.Select("schedule:cat")).Build();

    private static MessageComponent BuildDayStep(string category) =>
        new ComponentBuilder()
            .WithSelectMenu(BuildDaySelect(category), row: 0)
            .WithButton("Retour", "schedule:back:cat", ButtonStyle.Secondary, row: 1)
            .Build();

    private static MessageComponent BuildHourStep(string category, string date) =>
        new ComponentBuilder()
            .WithSelectMenu(BuildHourSelect(category, date), row: 0)
            .WithButton("Retour", $"schedule:back:day:{category}", ButtonStyle.Secondary, row: 1)
            .Build();

    private static MessageComponent BuildMinuteStep(string category, string date, string hour)
    {
        var builder = new ComponentBuilder();
        foreach (var m in new[] { "00", "15", "30", "45" })
            builder.WithButton($"{hour}:{m}", $"schedule:min:{category}:{date}:{hour}:{m}", ButtonStyle.Primary, row: 0);
        builder.WithButton("Retour", $"schedule:back:hour:{category}:{date}", ButtonStyle.Secondary, row: 1);
        return builder.Build();
    }

    private static SelectMenuBuilder BuildDaySelect(string category)
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var menu = new SelectMenuBuilder()
            .WithCustomId($"schedule:day:{category}")
            .WithPlaceholder("Choisis le jour");

        var today = AppTime.Now.Date;
        // 25 is the hard cap on options in a Discord select menu: 24 days, then « Autre date… ».
        for (int i = 0; i < 24; i++)
        {
            var date = today.AddDays(i);
            string label = i switch
            {
                0 => "Aujourd'hui",
                1 => "Demain",
                _ => fr.TextInfo.ToTitleCase(date.ToString("dddd dd/MM", fr))
            };
            menu.AddOption(label, date.ToString("yyyy-MM-dd"));
        }
        menu.AddOption("Autre date…", OtherDate, "Tape la date toi-même (jusqu'à 90 jours)", new Emoji("⌨️"));
        return menu;
    }

    private static SelectMenuBuilder BuildHourSelect(string category, string date)
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId($"schedule:hour:{category}:{date}")
            .WithPlaceholder("Choisis l'heure");

        // For today, hide hours already elapsed (past minutes are still guarded
        // when the session is finalized).
        var now = AppTime.Now;
        int startHour = 0;
        if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed) && parsed.Date == now.Date)
            startHour = now.Hour;

        for (int h = startHour; h < 24; h++)
            menu.AddOption($"{h:D2}h", $"{h:D2}");

        return menu;
    }

    // ---- The details modals, built by hand to pre-fill them --------------

    // Field ids must match ScheduleEventModal.
    public static Modal DetailsModal(string customId, string? title = null, string? places = null, string? description = null) =>
        new ModalBuilder()
            .WithTitle("Planifier une session")
            .WithCustomId(customId)
            .AddTextInput("Nom de la session", "title", maxLength: InputCaps.Title,
                placeholder: "ex. Among Us, Gartic, Anime ?", value: title, required: true)
            .AddTextInput("Places (vide = illimité, sinon 2 à 67)", "max_players", maxLength: 3,
                value: places, required: false)
            .AddTextInput("Description (facultatif)", "description", TextInputStyle.Paragraph,
                maxLength: InputCaps.Description, value: description, required: false)
            .Build();

    private static Modal DetailsModal(string customId, ScheduleDraft? draft) =>
        DetailsModal(customId, NullIfEmpty(draft?.Title), NullIfEmpty(draft?.Places), NullIfEmpty(draft?.Description));

    // Field ids must match ScheduleTypedModal.
    private static Modal TypedModal(string category, ScheduleDraft? draft) =>
        new ModalBuilder()
            .WithTitle("Planifier une session")
            .WithCustomId($"schedule:typed:{category}")
            .AddTextInput("Nom de la session", "title", maxLength: InputCaps.Title,
                placeholder: "ex. Among Us, Gartic, Anime ?", value: NullIfEmpty(draft?.Title), required: true)
            .AddTextInput("Quand", "when", maxLength: InputCaps.When,
                placeholder: "ex. samedi 21h, 25/12 20:30, demain", value: NullIfEmpty(draft?.When), required: true)
            .AddTextInput("Places (vide = illimité, sinon 2 à 67)", "max_players", maxLength: 3,
                value: NullIfEmpty(draft?.Places), required: false)
            .AddTextInput("Description (facultatif)", "description", TextInputStyle.Paragraph,
                maxLength: InputCaps.Description, value: NullIfEmpty(draft?.Description), required: false)
            .Build();

    private static string? NullIfEmpty(string? text) => string.IsNullOrEmpty(text) ? null : text;

    // ---- Modal submits: on to the review step -----------------------------

    [ModalInteraction("schedule:finalize:*:*", ignoreGroupNames: true)]
    public Task OnScheduleModalSubmittedAsync(string categoryStr, string unixStr, ScheduleEventModal modal) =>
        FinishAsync(categoryStr, unixStr, modal.SessionTitle, modal.MaxPlayers, modal.Description, pollId: null);

    // A closed poll turned into a session (PollModule.OnCategoryPickedAsync builds this modal).
    [ModalInteraction("schedule:frompoll:*:*:*", ignoreGroupNames: true)]
    public Task OnFromPollSubmittedAsync(string pollIdStr, string categoryStr, string unixStr, ScheduleEventModal modal) =>
        FinishAsync(categoryStr, unixStr, modal.SessionTitle, modal.MaxPlayers, modal.Description,
            int.TryParse(pollIdStr, out var pollId) ? pollId : null);

    [ModalInteraction("schedule:typed:*", ignoreGroupNames: true)]
    public async Task OnTypedModalSubmittedAsync(string categoryStr, ScheduleTypedModal modal)
    {
        var at = SessionTime.Parse(modal.When, DateTimeOffset.UtcNow);
        if (at is null)
        {
            Remember(modal.SessionTitle, modal.MaxPlayers, modal.Description, modal.When);
            await ReplyInWizardAsync($"⚠️ {WhenRefusal(modal.When.Trim())}",
                new ComponentBuilder().WithButton("Corriger", $"schedule:retrytyped:{categoryStr}", ButtonStyle.Primary).Build());
            return;
        }
        await FinishAsync(categoryStr, at.Value.ToUnixTimeSeconds().ToString(), modal.SessionTitle, modal.MaxPlayers,
            modal.Description, pollId: null);
    }

    // Reopens the details modal (pre-filled) after a validation error, without
    // posting anything to the channel.
    [ComponentInteraction("schedule:retry:*:*", ignoreGroupNames: true)]
    public async Task OnRetryAsync(string category, string unix)
    {
        _drafts.TryGet(Context.User.Id, out var draft);
        await RespondWithModalAsync(DetailsModal($"schedule:finalize:{category}:{unix}", draft));
    }

    [ComponentInteraction("schedule:retrytyped:*", ignoreGroupNames: true)]
    public async Task OnRetryTypedAsync(string category)
    {
        _drafts.TryGet(Context.User.Id, out var draft);
        await RespondWithModalAsync(TypedModal(category, draft));
    }

    // The modal's text is checked here; what's left (duration, voice, weekly) is set on the review step.
    private async Task FinishAsync(string categoryStr, string unixStr, string title, string placesInput,
        string description, int? pollId)
    {
        if (!SessionSeats.TryParseCap(placesInput, out var maxPlayers))
        {
            Remember(title, placesInput, description, null);
            var retry = pollId is null
                ? new ComponentBuilder().WithButton("Corriger", $"schedule:retry:{categoryStr}:{unixStr}", ButtonStyle.Primary).Build()
                : new ComponentBuilder().Build();
            await ReplyInWizardAsync($"⚠️ {SessionSeats.CapRule}", retry);
            return;
        }

        if (!Enum.TryParse<SessionCategory>(categoryStr, out var category))
            category = SessionCategory.Other;

        if (!long.TryParse(unixStr, out var unix))
        {
            await ReplyInWizardAsync("⚠️ Date invalide. Relance `/schedule create`.", new ComponentBuilder().Build());
            return;
        }
        var scheduledAt = DateTimeOffset.FromUnixTimeSeconds(unix);
        if (scheduledAt <= DateTimeOffset.UtcNow)
        {
            await ReplyInWizardAsync("⚠️ La session doit être planifiée dans le futur. Relance `/schedule create`.",
                new ComponentBuilder().Build());
            return;
        }

        // What /schedule create was given (duration, voice, weekly) carries over; a poll's session
        // starts from scratch, and « Modifier » on its review keeps what was set there.
        if (!_drafts.TryGet(Context.User.Id, out var draft) || draft.PollId != pollId)
            draft = new ScheduleDraft { PollId = pollId };
        draft.Title = title.Trim();
        draft.Places = placesInput;
        draft.Description = description.Trim();
        draft.Category = category;
        draft.At = scheduledAt;
        draft.MaxPlayers = maxPlayers;
        _drafts.Set(Context.User.Id, draft);

        await ReplyInWizardAsync(ReviewText(draft), ReviewStep(draft));
    }

    // ---- Review step: the choices a modal can't hold, then « Publier » ------
    // Everything lives in the draft, so the custom-ids carry nothing and stay short.

    private static readonly int[] DurationChoices = { 30, 60, 90, 120, 180, 240, 360 };

    private static string ReviewText(ScheduleDraft draft)
    {
        var ts = draft.At.ToUnixTimeSeconds();
        var sb = new StringBuilder();
        sb.AppendLine("**Aperçu** — règle le reste ici, puis **Publier**.");
        sb.AppendLine($"{SessionCategories.Emoji(draft.Category)} **{Format.Sanitize(draft.Title)}** · {SessionCategories.Name(draft.Category)}");
        sb.AppendLine($"📅 <t:{ts}:F> · <t:{ts}:R>");
        sb.Append($"Durée : {SessionTime.DurationLabel(draft.DurationMinutes)} · ");
        sb.Append(draft.MaxPlayers == 0 ? "places illimitées" : $"{draft.MaxPlayers} places");
        sb.AppendLine(draft.VoiceChannelId != 0 ? $" · 🔊 <#{draft.VoiceChannelId}>" : " · pas de salon vocal");
        if (draft.Weekly)
            sb.AppendLine($"🔁 Chaque {Recurrence.DayName(AppTime.ToZoned(draft.At).DayOfWeek)}, "
                + $"postée {RecurringSession.DefaultLeadDays} jours avant");
        if (draft.Description.Length > 0)
            sb.AppendLine(MessageFormat.Quote(Format.Sanitize(draft.Description), InputCaps.Description));
        return sb.ToString();
    }

    private static MessageComponent ReviewStep(ScheduleDraft draft)
    {
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

        return new ComponentBuilder()
            .WithSelectMenu(duration, row: 0)
            .WithSelectMenu(voice, row: 1)
            .WithButton("Publier", "schedule:rv:publish", ButtonStyle.Success, new Emoji("✅"), row: 2)
            .WithButton("Chaque semaine", "schedule:rv:weekly",
                draft.Weekly ? ButtonStyle.Primary : ButtonStyle.Secondary, new Emoji("🔁"), row: 2)
            .WithButton("Modifier", "schedule:rv:edit", ButtonStyle.Secondary, new Emoji("✏️"), row: 2)
            .WithButton("Annuler", "schedule:rv:cancel", ButtonStyle.Danger, row: 2)
            .Build();
    }

    // The draft behind the review step, or an answer saying it's gone.
    private async Task<ScheduleDraft?> ReviewDraftAsync()
    {
        if (_drafts.TryGet(Context.User.Id, out var draft) && draft.At != default) return draft;
        await RespondAsync("Cette session en préparation a expiré. Relance `/schedule create`.", ephemeral: true);
        return null;
    }

    private async Task RedrawReviewAsync(ScheduleDraft draft)
    {
        _drafts.Set(Context.User.Id, draft);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg =>
        {
            msg.Content = ReviewText(draft);
            msg.Components = ReviewStep(draft);
        });
    }

    [ComponentInteraction("schedule:rv:duration", ignoreGroupNames: true)]
    public async Task OnReviewDurationAsync(string[] values)
    {
        if (await ReviewDraftAsync() is not { } draft) return;
        if (values.Length > 0 && int.TryParse(values[0], out var minutes)) draft.DurationMinutes = minutes;
        await RedrawReviewAsync(draft);
    }

    // A channel select's values are the picked ids; none when it was cleared.
    [ComponentInteraction("schedule:rv:voice", ignoreGroupNames: true)]
    public async Task OnReviewVoiceAsync()
    {
        if (await ReviewDraftAsync() is not { } draft) return;
        var values = ((SocketMessageComponent)Context.Interaction).Data.Values;
        draft.VoiceChannelId = values?.FirstOrDefault() is { } id && ulong.TryParse(id, out var channelId) ? channelId : 0;
        await RedrawReviewAsync(draft);
    }

    [ComponentInteraction("schedule:rv:weekly", ignoreGroupNames: true)]
    public async Task OnReviewWeeklyAsync()
    {
        if (await ReviewDraftAsync() is not { } draft) return;
        draft.Weekly = !draft.Weekly;
        await RedrawReviewAsync(draft);
    }

    // Back to the modal, pre-filled; its submit lands on this review again.
    [ComponentInteraction("schedule:rv:edit", ignoreGroupNames: true)]
    public async Task OnReviewEditAsync()
    {
        if (await ReviewDraftAsync() is not { } draft) return;
        var unix = draft.At.ToUnixTimeSeconds();
        await RespondWithModalAsync(DetailsModal(draft.PollId is { } pollId
            ? $"schedule:frompoll:{pollId}:{draft.Category}:{unix}"
            : $"schedule:finalize:{draft.Category}:{unix}", draft));
    }

    [ComponentInteraction("schedule:rv:cancel", ignoreGroupNames: true)]
    public async Task OnReviewCancelAsync()
    {
        _drafts.Remove(Context.User.Id);
        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg =>
        {
            msg.Content = "Session abandonnée, rien n'a été publié.";
            msg.Components = new ComponentBuilder().Build();
        });
    }

    [ComponentInteraction("schedule:rv:publish", ignoreGroupNames: true)]
    public async Task OnReviewPublishAsync()
    {
        // Claimed first: of two clicks on « Publier », one publishes.
        if (!_drafts.TryTake(Context.User.Id, out var draft) || draft.At == default)
        {
            await RespondAsync("Cette session en préparation a expiré. Relance `/schedule create`.", ephemeral: true);
            return;
        }
        if (draft.At <= DateTimeOffset.UtcNow)
        {
            await RespondAsync("⚠️ Ce moment est passé. Relance `/schedule create`.", ephemeral: true);
            return;
        }

        // A write, a send and another write before the review can close: defer first (root
        // CLAUDE.md, reliability).
        var component = (SocketMessageComponent)Context.Interaction;
        await component.DeferAsync();

        var (session, error) = await CreateAndPostAsync(draft.Category, draft.At, draft, draft.MaxPlayers);
        if (session is null)
        {
            _drafts.Set(Context.User.Id, draft);
            await FollowupAsync(error, ephemeral: true);
            return;
        }

        if (draft.PollId is { } id) await LinkPollAsync(id, session);

        await component.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = Created(session);
            msg.Components = new ComponentBuilder().Build();
        });
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
    private async Task<(SessionEvent? Session, string? Error)> CreateAndPostAsync(
        SessionCategory category, DateTimeOffset scheduledAt, ScheduleDraft draft, int maxPlayers)
    {
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
                Category = category,
                DayOfWeek = day,
                MinuteOfDay = minute,
                MaxPlayers = maxPlayers,
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
            category: category,
            scheduledAt: scheduledAt,
            maxPlayers: maxPlayers,
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
        + "Exemples : « samedi 21h », « demain », « 25/12 20:30 », « ce soir ».";

    private void Remember(string title, string places, string description, string? when)
    {
        _drafts.TryGet(Context.User.Id, out var draft);
        draft ??= new ScheduleDraft();
        draft.Title = title;
        draft.Places = places;
        draft.Description = description;
        if (when is not null) draft.When = when;
        _drafts.Set(Context.User.Id, draft);
    }

    // A modal opened from the wizard updates the wizard; one opened straight from the command has
    // no message to update, so it answers on its own.
    private async Task ReplyInWizardAsync(string content, MessageComponent components)
    {
        var modal = (SocketModal)Context.Interaction;
        if (modal.Message is not null)
            await modal.UpdateAsync(msg =>
            {
                msg.Content = content;
                msg.Components = components;
            });
        else
            await RespondAsync(content, components: components, ephemeral: true);
    }

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
