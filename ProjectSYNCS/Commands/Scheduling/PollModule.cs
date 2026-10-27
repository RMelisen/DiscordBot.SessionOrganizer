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
using Poll = ProjectSYNCS.Models.Poll;

namespace ProjectSYNCS.Commands;

[Group("when", "Sondages pour choisir un créneau")]
// Guild-only: this module reads Context.Guild, which is null in a DM, and
// config.yaml ships register_globally: true (a global command is DM-enabled by
// default). Without this it is reachable somewhere it can only throw.
[CommandContextType(InteractionContextType.Guild)]
public class PollModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PollService _pollService;
    private readonly EventService _eventService;
    private readonly CardRepost _repost;
    private readonly ILogger<PollModule> _logger;

    // The slots a user is assembling before publishing. The wizard adds them one
    // at a time, so the in-progress poll lives here (keyed by user) rather than
    // being carried through component custom ids. Thirty minutes untouched and it's gone.
    private static readonly DraftBox<PollDraft> _drafts = new(TimeSpan.FromMinutes(30));

    // When converting a closed poll into a session, holds the poll title so the
    // schedule modal can be pre-filled with it (the title is too free-form to
    // carry safely through a component custom id).
    private static readonly DraftBox<string> _toSessionTitles = new(TimeSpan.FromMinutes(30));

    private sealed class PollDraft
    {
        public string Title { get; set; } = string.Empty;
        public List<DateTimeOffset> Slots { get; } = new();
        public PollSettings Settings { get; init; } = PollSettings.Default;
        // The slots came typed with the command: the title modal publishes straight away.
        public bool PublishOnTitle { get; init; }
    }

    public PollModule(PollService pollService, EventService eventService, CardRepost repost, ILogger<PollModule> logger)
    {
        _pollService = pollService;
        _eventService = eventService;
        _repost = repost;
        _logger = logger;
    }

    // ---- Entry: typed slots publish at once, the wizard otherwise --------

    [SlashCommand("create", "Proposer plusieurs créneaux et laisser voter")]
    public async Task PollCreateAsync(
        [Summary("title", "Le titre du sondage"), MaxLength(InputCaps.Title)] string title = "",
        [Summary("slots", "Les créneaux, séparés par des virgules : « ven 21h, sam 21h, dim 18h »"),
         MaxLength(InputCaps.PollSlots)] string slots = "",
        [Summary("ends", "Quand le sondage se termine (2 jours si rien)"),
         Choice("dans 1 h", 1), Choice("dans 3 h", 3), Choice("dans 6 h", 6), Choice("dans 12 h", 12),
         Choice("dans 1 jour", 24), Choice("dans 2 jours", 48), Choice("dans 3 jours", 72), Choice("dans 1 semaine", 168)] int ends = 48,
        [Summary("single", "Un seul créneau par personne")] bool single = false,
        [Summary("anonymous", "Montrer les comptes, jamais les noms")] bool anonymous = false)
    {
        var settings = new PollSettings(TimeSpan.FromHours(ends), single, anonymous);

        if (slots.Trim().Length > 0)
        {
            var (parsed, rejected) = SessionTime.ParseList(slots, DateTimeOffset.UtcNow);
            if (parsed.Count == 0)
            {
                await RespondAsync($"Aucun créneau compris dans « {Format.Sanitize(slots.Trim())} ». "
                    + "Exemple : « ven 21h, sam 21h, dim 18h » (dans les 90 jours).", ephemeral: true);
                return;
            }

            var draft = new PollDraft { Title = title.Trim(), Settings = settings, PublishOnTitle = true };
            draft.Slots.AddRange(parsed);
            if (draft.Title.Length == 0)
            {
                _drafts.Set(Context.User.Id, draft);
                await RespondWithModalAsync<PollModal>("poll:start");
                return;
            }

            await DeferAsync(ephemeral: true);
            var (poll, error) = await PublishAsync(draft);
            await FollowupAsync(error ?? Published(poll!, rejected), ephemeral: true);
            return;
        }

        if (title.Trim().Length > 0)
        {
            _drafts.Set(Context.User.Id, new PollDraft { Title = title.Trim(), Settings = settings });
            await RespondAsync("Ajoute un créneau — **Quel jour ?**", components: BuildDayStep(), ephemeral: true);
            return;
        }

        _drafts.Set(Context.User.Id, new PollDraft { Settings = settings });
        await RespondWithModalAsync<PollModal>("poll:start");
    }

    [ModalInteraction("poll:start", ignoreGroupNames: true)]
    public async Task OnPollStartAsync(PollModal modal)
    {
        if (!_drafts.TryGet(Context.User.Id, out var draft))
            draft = new PollDraft();
        draft.Title = modal.PollTitle.Trim();
        _drafts.Set(Context.User.Id, draft);

        if (draft.PublishOnTitle)
        {
            if (!_drafts.TryTake(Context.User.Id, out draft))
            {
                await RespondAsync("Ce sondage a expiré. Relance `/when create`.", ephemeral: true);
                return;
            }
            await DeferAsync(ephemeral: true);
            var (poll, error) = await PublishAsync(draft);
            if (poll is null) _drafts.Set(Context.User.Id, draft);
            await FollowupAsync(error ?? Published(poll!, new List<string>()), ephemeral: true);
            return;
        }

        await RespondAsync("Ajoute un créneau — **Quel jour ?**",
            components: BuildDayStep(), ephemeral: true);
    }

    // ---- Slot wizard: day -> hour -> minutes, or a typed date ------------

    [ComponentInteraction("poll:day", ignoreGroupNames: true)]
    public async Task OnDaySelectedAsync(string[] values)
    {
        var date = values[0];
        if (date == OtherDate)
        {
            await RespondWithModalAsync<PollSlotModal>("poll:typedslot");
            return;
        }

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "**À quelle heure ?**";
            msg.Components = BuildHourStep(date);
        });
    }

    [ModalInteraction("poll:typedslot", ignoreGroupNames: true)]
    public async Task OnTypedSlotAsync(PollSlotModal modal)
    {
        if (!_drafts.TryGet(Context.User.Id, out var draft))
        {
            await RespondAsync("Ce sondage a expiré. Relance `/when create`.", ephemeral: true);
            return;
        }

        var slot = SessionTime.Parse(modal.When, DateTimeOffset.UtcNow);
        var note = slot is null
            ? $"⚠️ « {Format.Sanitize(modal.When.Trim())} » n'est pas une date à venir que je comprends (ex. {FrenchDate.Examples}).\n"
            : string.Empty;
        if (slot is { } s) AddSlot(draft, s);

        await ((SocketModal)Context.Interaction).UpdateAsync(msg =>
        {
            msg.Content = note + BuildDraftSummary(draft);
            msg.Components = BuildReviewStep(draft);
        });
    }

    [ComponentInteraction("poll:hour:*", ignoreGroupNames: true)]
    public async Task OnHourSelectedAsync(string date, string[] values)
    {
        var hour = values[0];
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "**À quelle heure ?**";
            msg.Components = BuildMinuteStep(date, hour);
        });
    }

    [ComponentInteraction("poll:min:*:*:*", ignoreGroupNames: true)]
    public async Task OnMinuteSelectedAsync(string date, string hour, string minute)
    {
        if (!_drafts.TryGet(Context.User.Id, out var draft))
        {
            await RespondAsync("Ce sondage a expiré. Relance `/when create`.", ephemeral: true);
            return;
        }

        if (AppTime.TryParseWallClock($"{date}T{hour}:{minute}", "yyyy-MM-ddTHH:mm", out var slot)
            && slot > DateTimeOffset.UtcNow)
            AddSlot(draft, slot);

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = BuildDraftSummary(draft);
            msg.Components = BuildReviewStep(draft);
        });
    }

    private static void AddSlot(PollDraft draft, DateTimeOffset slot)
    {
        if (!draft.Slots.Contains(slot) && draft.Slots.Count < SessionTime.MaxSlots)
            draft.Slots.Add(slot);
    }

    // ---- Review step: add another, remove one, finish, or cancel ---------

    [ComponentInteraction("poll:add", ignoreGroupNames: true)]
    public async Task OnAddAnotherAsync()
    {
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "Ajoute un créneau — **Quel jour ?**";
            msg.Components = BuildDayStep();
        });
    }

    [ComponentInteraction("poll:rmslot", ignoreGroupNames: true)]
    public async Task OnRemoveSlotAsync(string[] values)
    {
        if (!_drafts.TryGet(Context.User.Id, out var draft))
        {
            await RespondAsync("Ce sondage a expiré. Relance `/when create`.", ephemeral: true);
            return;
        }
        if (long.TryParse(values[0], out var unix))
            draft.Slots.Remove(DateTimeOffset.FromUnixTimeSeconds(unix));

        await ((SocketMessageComponent)Context.Interaction).UpdateAsync(msg =>
        {
            msg.Content = BuildDraftSummary(draft);
            msg.Components = BuildReviewStep(draft);
        });
    }

    [ComponentInteraction("poll:canceldraft", ignoreGroupNames: true)]
    public async Task OnCancelDraftAsync()
    {
        _drafts.Remove(Context.User.Id);
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "Sondage annulé.";
            msg.Components = new ComponentBuilder().Build();
        });
    }

    [ComponentInteraction("poll:finish", ignoreGroupNames: true)]
    public async Task OnFinishAsync()
    {
        // Claimed first: of two clicks on « Terminer », one publishes.
        if (!_drafts.TryTake(Context.User.Id, out var draft) || draft.Slots.Count == 0)
        {
            if (draft is { Slots.Count: 0 }) _drafts.Set(Context.User.Id, draft);
            await RespondAsync("Ajoute au moins un créneau avant de terminer.", ephemeral: true);
            return;
        }

        // Two writes, a read and a send before the wizard can close: defer first.
        await DeferAsync();

        var (poll, error) = await PublishAsync(draft);
        if (poll is null)
        {
            _drafts.Set(Context.User.Id, draft);
            await FollowupAsync(error, ephemeral: true);
            return;
        }

        await DeleteOriginalResponseAsync();
    }

    // Creates the poll and posts its card here. A card that can't be posted takes the poll with it.
    private async Task<(Poll? Poll, string? Error)> PublishAsync(PollDraft draft)
    {
        var poll = await _pollService.CreatePollAsync(
            Context.Guild.Id, Context.Channel.Id, Context.User.Id, draft.Title, draft.Slots, draft.Settings);
        var full = (await _pollService.GetPollWithVotesAsync(poll.Id))!;
        try
        {
            var message = await PollCards.SendAsync(Context.Channel, full);
            await _pollService.SetMessageIdAsync(poll.Id, message.Id);
            return (full, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post the card of poll {PollId}; removing it.", poll.Id);
            await _pollService.DeletePollAsync(poll.Id);
            return (null, CardPostFailure.Explain(Context.Guild, Context.Channel, "le sondage"));
        }
    }

    private static string Published(Poll poll, List<string> rejected)
    {
        var line = $"Sondage **#{poll.Id}** publié, {poll.Options.Count} créneau(x), fin <t:{poll.ClosesAt.ToUnixTimeSeconds()}:R>.";
        if (rejected.Count > 0)
            line += $"\n⚠️ Pas compris, donc pas ajouté : {string.Join(", ", rejected.Select(r => $"« {Format.Sanitize(r)} »"))}.";
        return line;
    }

    // ---- Listing + republishing active polls -----------------------------

    [SlashCommand("list", "Lister les sondages actifs du serveur")]
    public async Task PollListAsync()
    {
        await DeferAsync(ephemeral: true);

        var polls = await _pollService.GetActivePollsAsync(Context.Guild.Id, PollKind.DateSlots);
        if (polls.Count == 0)
        {
            await FollowupAsync("Aucun sondage actif pour le moment.", ephemeral: true);
            return;
        }

        var sb = new StringBuilder();
        foreach (var p in polls.Take(25))
            sb.AppendLine($"**#{p.Id}** 📊 {Format.Sanitize(p.Title)} — {p.Options.Count} créneaux — 🗳️ {PollRules.VoterCount(p)} votant(s) — fin <t:{p.ClosesAt.ToUnixTimeSeconds()}:R>");

        var embed = new EmbedBuilder()
            .WithTitle($"Sondages actifs ({polls.Count})")
            .WithDescription(sb.ToString())
            .WithColor(Color.Purple)
            .Build();

        // Up to 25 options in a select menu.
        var menu = new SelectMenuBuilder()
            .WithCustomId("poll:republish")
            .WithPlaceholder("Republier un sondage dans ce salon");
        foreach (var p in polls.Take(25))
        {
            string label = $"#{p.Id} — {p.Title}";
            if (label.Length > 100) label = label[..100];
            menu.AddOption(label, p.Id.ToString());
        }

        var components = new ComponentBuilder().WithSelectMenu(menu).Build();
        await FollowupAsync(embed: embed, components: components, ephemeral: true);
    }

    [ComponentInteraction("poll:republish", ignoreGroupNames: true)]
    public async Task OnRepublishAsync(string[] values)
    {
        await DeferAsync(ephemeral: true);

        if (!int.TryParse(values[0], out int pollId))
        {
            await FollowupAsync("ID de sondage invalide.", ephemeral: true);
            return;
        }

        var result = await _repost.RepostAsync(Context.Guild, Context.Channel, new CardRef(CardKind.Poll, pollId));
        await FollowupAsync(result switch
        {
            RepostResult.NotFound => "Sondage introuvable.",
            RepostResult.Closed => "Ce sondage est clôturé.",
            _ => $"Sondage **#{pollId}** republié ici.",
        }, ephemeral: true);
    }

    [SlashCommand("delete", "Supprimer un sondage que tu as créé")]
    public async Task PollDeleteAsync(
        [Summary("poll-id", "Le sondage à supprimer"), Autocomplete(typeof(PollIdAutocompleteHandler))] int pollId)
    {
        await DeferAsync(ephemeral: true);

        var poll = await _pollService.GetPollWithVotesAsync(pollId);
        if (poll is null || poll.GuildId != Context.Guild.Id || poll.Kind != PollKind.DateSlots)
        {
            await FollowupAsync("Sondage introuvable.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, poll))
        {
            await FollowupAsync("Seul l'organisateur ou un administrateur peut supprimer ce sondage.", ephemeral: true);
            return;
        }

        await DeleteCardAsync(Context.Guild, poll);
        await _pollService.DeletePollAsync(pollId);
        await FollowupAsync($"Sondage **#{pollId}** supprimé.", ephemeral: true);
    }

    // Removes the card if it still exists (/when delete, /vote delete).
    public static async Task DeleteCardAsync(SocketGuild guild, Poll poll)
    {
        if (poll.MessageId == 0 || guild.GetTextChannel(poll.ChannelId) is not { } channel) return;
        try
        {
            var msg = await channel.GetMessageAsync(poll.MessageId);
            if (msg is not null) await msg.DeleteAsync();
        }
        catch { /* already gone — ignore */ }
    }

    // ---- Poll -> Session: turn the chosen slot into a real session -------

    [ComponentInteraction("poll:tosession:*", ignoreGroupNames: true)]
    public async Task OnToSessionAsync(string pollIdStr)
    {
        if (!int.TryParse(pollIdStr, out int pollId))
        {
            await RespondAsync("Sondage invalide.", ephemeral: true);
            return;
        }

        // Reads (and, for her poll, the sessions) before the first step can show: defer first.
        await DeferAsync(ephemeral: true);
        var poll = await _pollService.GetPollWithVotesAsync(pollId);
        if (poll is null || poll.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Sondage introuvable.", ephemeral: true);
            return;
        }

        // A session was already made from this poll: say where, rather than make a second.
        if (poll.SessionEventId is { } linkedId
            && await _eventService.GetEventWithParticipantsAsync(linkedId) is { IsCancelled: false } linked)
        {
            await FollowupAsync($"La session de ce sondage existe déjà : {SessionCards.Link(linked)}", ephemeral: true);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var futureSlots = poll.Options.Where(o => o.ScheduledAt > now).ToList();
        int max = futureSlots.Count == 0 ? 0 : futureSlots.Max(o => o.Votes.Count);
        var winners = max > 0
            ? futureSlots.Where(o => o.Votes.Count == max).OrderBy(o => o.ScheduledAt).ToList()
            : futureSlots.OrderBy(o => o.ScheduledAt).ToList();

        // Her own weekend poll (WeekendPollService) has no human organizer, so whoever voted for
        // a winning evening may plan it too, and becomes the session's organizer. Only her polls:
        // everyone else's stay with their organizer and staff.
        bool ownPoll = poll.OrganizerId == Context.Client.CurrentUser.Id;
        bool votedForWinner = ownPoll && max > 0
            && winners.Any(o => o.Votes.Any(v => v.UserId == Context.User.Id));
        if (!SessionPermissions.CanManage(Context.User, poll) && !votedForWinner)
        {
            await FollowupAsync(
                ownPoll
                    ? "Seul quelqu'un qui a voté pour le créneau retenu (ou un administrateur) peut créer cette session."
                    : "Seul l'organisateur ou un administrateur peut créer une session depuis ce sondage.",
                ephemeral: true);
            return;
        }

        if (futureSlots.Count == 0)
        {
            await FollowupAsync("Tous les créneaux de ce sondage sont déjà passés.", ephemeral: true);
            return;
        }

        // Several voters can click the same button on her poll: once one of them has planned a
        // winning evening, the others are told instead of creating a second session.
        if (ownPoll)
        {
            var planned = await _eventService.GetActiveEventsAsync(Context.Guild.Id);
            var taken = winners.FirstOrDefault(o => planned.Any(e => e.ScheduledAt == o.ScheduledAt));
            if (taken is not null)
            {
                await FollowupAsync(
                    $"Une session est déjà prévue à ce créneau (<t:{taken.ScheduledAt.ToUnixTimeSeconds()}:F>).",
                    ephemeral: true);
                return;
            }
        }

        // Pre-fill the future modal's title with the poll's title — not hers, which is a
        // question (« Qui est dispo ce weekend ? »), not the name of what they'll play.
        if (ownPoll)
            _toSessionTitles.Remove(Context.User.Id);
        else
            _toSessionTitles.Set(Context.User.Id, poll.Title);

        // A single clear winner skips straight to category selection.
        if (max > 0 && winners.Count == 1)
        {
            await FollowupAsync(
                $"Créneau retenu : <t:{winners[0].ScheduledAt.ToUnixTimeSeconds()}:F>\n**Quel type de session ?**",
                components: BuildSessionCategoryStep(poll.Id, winners[0].ScheduledAt), ephemeral: true);
            return;
        }

        // Tie (or no votes at all): let the organizer pick which slot to schedule.
        await FollowupAsync(
            max > 0
                ? "Égalité entre plusieurs créneaux — **choisis celui à planifier** :"
                : "Aucun vote — **choisis le créneau à planifier** :",
            components: BuildSlotPickStep(poll.Id, winners), ephemeral: true);
    }

    [ComponentInteraction("poll:slotpick:*", ignoreGroupNames: true)]
    public async Task OnSlotPickAsync(string pollIdStr, string[] values)
    {
        if (!int.TryParse(pollIdStr, out var pollId) || !long.TryParse(values[0], out var unix))
        {
            await RespondAsync("Créneau invalide.", ephemeral: true);
            return;
        }
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "**Quel type de session ?**";
            msg.Components = BuildSessionCategoryStep(pollId, DateTimeOffset.FromUnixTimeSeconds(unix));
        });
    }

    [ComponentInteraction("poll:catpick:*:*", ignoreGroupNames: true)]
    public async Task OnCategoryPickedAsync(string pollIdStr, string unix, string[] values)
    {
        var category = values[0];
        _toSessionTitles.TryTake(Context.User.Id, out var title);

        // ScheduleModule's « schedule:frompoll » handler creates the session, publishes the card in
        // this channel, and links the poll to it.
        await RespondWithModalAsync(ScheduleModule.DetailsModal($"schedule:frompoll:{pollIdStr}:{category}:{unix}", title));
    }

    private static MessageComponent BuildSlotPickStep(int pollId, List<PollOption> slots)
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId($"poll:slotpick:{pollId}")
            .WithPlaceholder("Choisis le créneau");
        // 25 is the hard cap on options in a Discord select menu.
        foreach (var o in slots.Take(25))
        {
            string label = PollCards.ShortLabel(o.ScheduledAt);
            if (o.Votes.Count > 0) label += $" — {o.Votes.Count} vote(s)";
            menu.AddOption(label, o.ScheduledAt.ToUnixTimeSeconds().ToString());
        }
        return new ComponentBuilder().WithSelectMenu(menu).Build();
    }

    private static MessageComponent BuildSessionCategoryStep(int pollId, DateTimeOffset slot) =>
        new ComponentBuilder()
            .WithSelectMenu(SessionCategories.Select($"poll:catpick:{pollId}:{slot.ToUnixTimeSeconds()}"))
            .Build();

    // ---- Wizard step builders --------------------------------------------

    // The day select's last option: the slot typed, past the 24 days the select holds.
    private const string OtherDate = "other";

    private static MessageComponent BuildDayStep() =>
        new ComponentBuilder().WithSelectMenu(BuildDaySelect()).Build();

    private static MessageComponent BuildHourStep(string date) =>
        new ComponentBuilder().WithSelectMenu(BuildHourSelect(date)).Build();

    private static MessageComponent BuildMinuteStep(string date, string hour)
    {
        var builder = new ComponentBuilder();
        foreach (var m in new[] { "00", "15", "30", "45" })
            builder.WithButton($"{hour}:{m}", $"poll:min:{date}:{hour}:{m}", ButtonStyle.Primary, row: 0);
        return builder.Build();
    }

    private static MessageComponent BuildReviewStep(PollDraft draft)
    {
        var builder = new ComponentBuilder();
        if (draft.Slots.Count < SessionTime.MaxSlots)
            builder.WithButton("Ajouter un créneau", "poll:add", ButtonStyle.Secondary, new Emoji("➕"), row: 0);
        if (draft.Slots.Count > 0)
            builder.WithButton("Terminer", "poll:finish", ButtonStyle.Success, new Emoji("✅"), row: 0);
        builder.WithButton("Annuler", "poll:canceldraft", ButtonStyle.Danger, row: 0);

        if (draft.Slots.Count > 0)
        {
            var menu = new SelectMenuBuilder()
                .WithCustomId("poll:rmslot")
                .WithPlaceholder("Retirer un créneau");
            foreach (var s in draft.Slots.OrderBy(s => s))
                menu.AddOption(PollCards.ShortLabel(s), s.ToUnixTimeSeconds().ToString(), emote: new Emoji("➖"));
            builder.WithSelectMenu(menu, row: 1);
        }
        return builder.Build();
    }

    private static SelectMenuBuilder BuildDaySelect()
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var menu = new SelectMenuBuilder()
            .WithCustomId("poll:day")
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
        menu.AddOption("Autre date…", OtherDate, "Tape le créneau toi-même (jusqu'à 90 jours)", new Emoji("⌨️"));
        return menu;
    }

    private static SelectMenuBuilder BuildHourSelect(string date)
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId($"poll:hour:{date}")
            .WithPlaceholder("Choisis l'heure");

        // For today, hide hours already elapsed (past minutes are still guarded
        // when the slot is finalized).
        var now = AppTime.Now;
        int startHour = 0;
        if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed) && parsed.Date == now.Date)
            startHour = now.Hour;

        for (int h = startHour; h < 24; h++)
            menu.AddOption($"{h:D2}h", $"{h:D2}");

        return menu;
    }

    private static string BuildDraftSummary(PollDraft draft)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"**Sondage : {Format.Sanitize(draft.Title)}**");
        sb.AppendLine($"-# {SettingsLine(draft.Settings)}");
        sb.AppendLine($"Créneaux proposés ({draft.Slots.Count}/{SessionTime.MaxSlots}) :");
        if (draft.Slots.Count == 0)
            sb.AppendLine("_(aucun pour l'instant)_");
        else
            foreach (var s in draft.Slots.OrderBy(s => s))
                sb.AppendLine($"• <t:{s.ToUnixTimeSeconds()}:F>");
        return sb.ToString();
    }

    // « Se termine 2 jours après la publication · plusieurs choix · noms visibles »
    public static string SettingsLine(PollSettings settings)
    {
        var hours = (int)settings.Lifetime.TotalHours;
        var ends = hours % 24 == 0 ? $"{hours / 24} jour{(hours / 24 > 1 ? "s" : "")}" : $"{hours} h";
        return $"Se termine {ends} après la publication · "
            + (settings.SingleChoice ? "un seul choix" : "plusieurs choix") + " · "
            + (settings.Anonymous ? "anonyme" : "noms visibles");
    }
}
