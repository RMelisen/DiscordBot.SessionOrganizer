using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Poll = ProjectSYNCS.Models.Poll;

namespace ProjectSYNCS.Commands;

[Group("poll", "Sondages pour choisir un créneau")]
public class PollModule : InteractionModuleBase<SocketInteractionContext>
{
    private const int MaxOptions = 10;

    private readonly PollService _pollService;

    // The slots a user is assembling before publishing. The wizard adds them one
    // at a time, so the in-progress poll lives here (keyed by user) rather than
    // being carried through component custom ids.
    private static readonly ConcurrentDictionary<ulong, PollDraft> _drafts = new();

    // When converting a closed poll into a session, holds the poll title so the
    // schedule modal can be pre-filled with it (the title is too free-form to
    // carry safely through a component custom id).
    private static readonly ConcurrentDictionary<ulong, string> _toSessionTitles = new();

    private sealed class PollDraft
    {
        public string Title { get; init; } = string.Empty;
        public List<DateTimeOffset> Slots { get; } = new();
    }

    public PollModule(PollService pollService)
    {
        _pollService = pollService;
    }

    // ---- Entry: ask for a title, then start adding slots ------------------

    [SlashCommand("create", "Proposer plusieurs créneaux et laisser voter")]
    public async Task PollCreateAsync()
    {
        await RespondWithModalAsync<PollModal>("poll:start");
    }

    [ModalInteraction("poll:start", ignoreGroupNames: true)]
    public async Task OnPollStartAsync(PollModal modal)
    {
        _drafts[Context.User.Id] = new PollDraft { Title = modal.PollTitle.Trim() };
        await RespondAsync("Ajoute un créneau — **Quel jour ?**",
            components: BuildDayStep(), ephemeral: true);
    }

    // ---- Slot wizard: day -> hour -> minutes -----------------------------

    [ComponentInteraction("poll:day", ignoreGroupNames: true)]
    public async Task OnDaySelectedAsync(string[] values)
    {
        var date = values[0];
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "**À quelle heure ?**";
            msg.Components = BuildHourStep(date);
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
        if (!_drafts.TryGetValue(Context.User.Id, out var draft))
        {
            await RespondAsync("Ce sondage a expiré. Relance `/poll create`.", ephemeral: true);
            return;
        }

        if (AppTime.TryParseWallClock($"{date}T{hour}:{minute}", "yyyy-MM-ddTHH:mm", out var slot)
            && slot > DateTimeOffset.Now
            && !draft.Slots.Contains(slot)
            && draft.Slots.Count < MaxOptions)
        {
            draft.Slots.Add(slot);
        }

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = BuildDraftSummary(draft);
            msg.Components = BuildReviewStep(draft);
        });
    }

    // ---- Review step: add another, finish, or cancel ---------------------

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

    [ComponentInteraction("poll:canceldraft", ignoreGroupNames: true)]
    public async Task OnCancelDraftAsync()
    {
        _drafts.TryRemove(Context.User.Id, out _);
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
        if (!_drafts.TryGetValue(Context.User.Id, out var draft) || draft.Slots.Count == 0)
        {
            await RespondAsync("Ajoute au moins un créneau avant de terminer.", ephemeral: true);
            return;
        }

        var poll = await _pollService.CreatePollAsync(
            Context.Guild.Id, Context.Channel.Id, Context.User.Id, draft.Title, draft.Slots);
        var full = await _pollService.GetPollWithVotesAsync(poll.Id);

        var message = await Context.Channel.SendMessageAsync(
            embed: BuildPollEmbed(full!),
            components: BuildPollComponents(full!));
        await _pollService.SetMessageIdAsync(poll.Id, message.Id);

        _drafts.TryRemove(Context.User.Id, out _);

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "Sondage publié.";
            msg.Components = new ComponentBuilder().Build();
        });
        await component.DeleteOriginalResponseAsync();
    }

    // ---- Listing + republishing active polls -----------------------------

    [SlashCommand("list", "Lister les sondages actifs du serveur")]
    public async Task PollListAsync()
    {
        await DeferAsync(ephemeral: true);

        var polls = await _pollService.GetActivePollsAsync(Context.Guild.Id);
        if (polls.Count == 0)
        {
            await FollowupAsync("Aucun sondage actif pour le moment.", ephemeral: true);
            return;
        }

        var sb = new StringBuilder();
        foreach (var p in polls)
        {
            int voters = p.Options.SelectMany(o => o.Votes).Select(v => v.UserId).Distinct().Count();
            sb.AppendLine($"**#{p.Id}** 📊 {p.Title} — {p.Options.Count} créneaux — 🗳️ {voters} votant(s)");
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Sondages actifs ({polls.Count})")
            .WithDescription(sb.ToString())
            .WithColor(Color.Purple)
            .Build();

        // Up to 25 options in a select menu; the list embed still shows all.
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

        var poll = await _pollService.GetPollWithVotesAsync(pollId);
        if (poll is null || poll.GuildId != Context.Guild.Id)
        {
            await FollowupAsync("Sondage introuvable.", ephemeral: true);
            return;
        }

        if (poll.IsClosed)
        {
            await FollowupAsync("Ce sondage est clôturé.", ephemeral: true);
            return;
        }

        // Remove the previous card if it still exists, to avoid duplicates.
        if (poll.MessageId != 0)
        {
            var oldChannel = Context.Guild.GetTextChannel(poll.ChannelId);
            if (oldChannel is not null)
            {
                try
                {
                    var old = await oldChannel.GetMessageAsync(poll.MessageId);
                    if (old is not null) await old.DeleteAsync();
                }
                catch { /* already gone — ignore */ }
            }
        }

        var message = await Context.Channel.SendMessageAsync(
            embed: BuildPollEmbed(poll),
            components: BuildPollComponents(poll));
        await _pollService.SetMessageLocationAsync(poll.Id, Context.Channel.Id, message.Id);

        await FollowupAsync($"Sondage **#{pollId}** republié ici.", ephemeral: true);
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

        var poll = await _pollService.GetPollWithVotesAsync(pollId);
        if (poll is null || poll.GuildId != Context.Guild.Id)
        {
            await RespondAsync("Sondage introuvable.", ephemeral: true);
            return;
        }

        if (!SessionPermissions.CanManage(Context.User, poll))
        {
            await RespondAsync(
                "Seul l'organisateur ou un administrateur peut créer une session depuis ce sondage.",
                ephemeral: true);
            return;
        }

        var now = DateTimeOffset.Now;
        var futureSlots = poll.Options.Where(o => o.ScheduledAt > now).ToList();
        if (futureSlots.Count == 0)
        {
            await RespondAsync("Tous les créneaux de ce sondage sont déjà passés.", ephemeral: true);
            return;
        }

        // Pre-fill the future modal's title with the poll's title.
        _toSessionTitles[Context.User.Id] = poll.Title;

        int max = futureSlots.Max(o => o.Votes.Count);
        var winners = max > 0
            ? futureSlots.Where(o => o.Votes.Count == max).OrderBy(o => o.ScheduledAt).ToList()
            : futureSlots.OrderBy(o => o.ScheduledAt).ToList();

        // A single clear winner skips straight to category selection.
        if (max > 0 && winners.Count == 1)
        {
            var datetime = AppTime.ToZoned(winners[0].ScheduledAt).ToString("yyyy-MM-ddTHH:mm");
            await RespondAsync(
                $"Créneau retenu : <t:{winners[0].ScheduledAt.ToUnixTimeSeconds()}:F>\n**Quel type de session ?**",
                components: BuildSessionCategoryStep(datetime), ephemeral: true);
            return;
        }

        // Tie (or no votes at all): let the organizer pick which slot to schedule.
        await RespondAsync(
            max > 0
                ? "Égalité entre plusieurs créneaux — **choisis celui à planifier** :"
                : "Aucun vote — **choisis le créneau à planifier** :",
            components: BuildSlotPickStep(winners), ephemeral: true);
    }

    [ComponentInteraction("poll:slotpick", ignoreGroupNames: true)]
    public async Task OnSlotPickAsync(string[] values)
    {
        var datetime = values[0];
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Content = "**Quel type de session ?**";
            msg.Components = BuildSessionCategoryStep(datetime);
        });
    }

    [ComponentInteraction("poll:catpick:*", ignoreGroupNames: true)]
    public async Task OnCategoryPickedAsync(string datetime, string[] values)
    {
        var category = values[0];
        _toSessionTitles.TryRemove(Context.User.Id, out var title);

        // Reuses the existing "schedule:finalize:*:*" modal handler, which creates
        // the session and publishes the card in this channel.
        var modal = new ModalBuilder()
            .WithTitle("Planifier une session")
            .WithCustomId($"schedule:finalize:{category}:{datetime}")
            .AddTextInput("Nom de la session", "title",
                placeholder: "ex. Among Us, Gartic, Anime ?", value: title, required: true)
            .AddTextInput("Nombre de participants max - Optionnel", "max_players", required: false)
            .Build();

        await RespondWithModalAsync(modal);
    }

    private static MessageComponent BuildSlotPickStep(List<PollOption> slots)
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId("poll:slotpick")
            .WithPlaceholder("Choisis le créneau");
        // 25 is the hard cap on options in a Discord select menu.
        foreach (var o in slots.Take(25))
        {
            var datetime = AppTime.ToZoned(o.ScheduledAt).ToString("yyyy-MM-ddTHH:mm");
            string label = ShortLabel(o.ScheduledAt);
            if (o.Votes.Count > 0) label += $" — {o.Votes.Count} vote(s)";
            menu.AddOption(label, datetime);
        }
        return new ComponentBuilder().WithSelectMenu(menu).Build();
    }

    private static MessageComponent BuildSessionCategoryStep(string datetime)
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId($"poll:catpick:{datetime}")
            .WithPlaceholder("Choisis une catégorie")
            .AddOption("Jeu", nameof(SessionCategory.Game), emote: new Emoji("🎮"))
            .AddOption("Activité", nameof(SessionCategory.Activity), emote: new Emoji("🧑‍🤝‍🧑"))
            .AddOption("Film", nameof(SessionCategory.Movie), emote: new Emoji("🎬"))
            .AddOption("Autre", nameof(SessionCategory.Other), emote: new Emoji("✨"));
        return new ComponentBuilder().WithSelectMenu(menu).Build();
    }

    // ---- Wizard step builders --------------------------------------------

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
        if (draft.Slots.Count < MaxOptions)
            builder.WithButton("Ajouter un créneau", "poll:add", ButtonStyle.Secondary, new Emoji("➕"), row: 0);
        if (draft.Slots.Count > 0)
            builder.WithButton("Terminer", "poll:finish", ButtonStyle.Success, new Emoji("✅"), row: 0);
        builder.WithButton("Annuler", "poll:canceldraft", ButtonStyle.Danger, row: 0);
        return builder.Build();
    }

    private static SelectMenuBuilder BuildDaySelect()
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var menu = new SelectMenuBuilder()
            .WithCustomId("poll:day")
            .WithPlaceholder("Choisis le jour");

        var today = AppTime.Now.Date;
        // 25 is the hard cap on options in a Discord select menu.
        for (int i = 0; i < 25; i++)
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
        sb.AppendLine($"**Sondage : {draft.Title}**");
        sb.AppendLine($"Créneaux proposés ({draft.Slots.Count}/{MaxOptions}) :");
        if (draft.Slots.Count == 0)
            sb.AppendLine("_(aucun pour l'instant)_");
        else
            foreach (var s in draft.Slots.OrderBy(s => s))
                sb.AppendLine($"• <t:{s.ToUnixTimeSeconds()}:F>");
        return sb.ToString();
    }

    // ---- Published poll: embed + voting buttons --------------------------

    public static Embed BuildPollEmbed(Poll poll)
    {
        var ordered = poll.Options.OrderBy(o => o.ScheduledAt).ToList();

        var sb = new StringBuilder();
        foreach (var o in ordered)
        {
            int n = o.Votes.Count;
            var ts = o.ScheduledAt.ToUnixTimeSeconds();
            sb.Append($"{(n == 0 ? "🔴" : "🟢")} <t:{ts}:F> — **{n}** ");
            if (n > 0)
                sb.Append("  " + string.Join(" ", o.Votes.Select(v => $"<@{v.UserId}>")));
            sb.AppendLine();
        }

        var eb = new EmbedBuilder()
            .WithTitle($"📊 {poll.Title}")
            .WithDescription(sb.ToString())
            .WithColor(poll.IsClosed ? Color.DarkGrey : Color.Purple)
            .WithFooter(poll.IsClosed
                ? $"Sondage clôturé · ID {poll.Id}"
                : $"Clique tous les créneaux qui te conviennent · ID {poll.Id}");

        if (poll.IsClosed)
        {
            int max = ordered.Count == 0 ? 0 : ordered.Max(o => o.Votes.Count);
            if (max > 0)
            {
                var winners = ordered.Where(o => o.Votes.Count == max).ToList();
                var lines = string.Join("\n",
                    winners.Select(w => $"<t:{w.ScheduledAt.ToUnixTimeSeconds()}:F>"));
                eb.AddField(
                    winners.Count > 1 ? $"Créneaux retenus — égalité ({max} ✅)" : $"Créneau retenu ({max} ✅)",
                    lines);
            }
        }

        return eb.Build();
    }

    public static MessageComponent BuildPollComponents(Poll poll)
    {
        var builder = new ComponentBuilder();
        if (poll.IsClosed)
        {
            // Offer to turn the chosen slot into a real session, as long as at
            // least one proposed slot is still in the future.
            if (poll.Options.Any(o => o.ScheduledAt > DateTimeOffset.Now))
                builder.WithButton("Créer une session", $"poll:tosession:{poll.Id}",
                    ButtonStyle.Success, new Emoji("🗓️"));
            return builder.Build();
        }

        var ordered = poll.Options.OrderBy(o => o.ScheduledAt).ToList();

        // Up to 5 vote buttons per row; MaxOptions (10) fits in rows 0-1.
        int row = 0, inRow = 0;
        foreach (var o in ordered)
        {
            builder.WithButton(ShortLabel(o.ScheduledAt), $"poll:vote:{poll.Id}:{o.Id}", ButtonStyle.Secondary, row: row);
            if (++inRow == 5) { inRow = 0; row++; }
        }

        int closeRow = inRow > 0 ? row + 1 : row;
        builder.WithButton("Clôturer", $"poll:close:{poll.Id}", ButtonStyle.Danger, new Emoji("🔒"), row: closeRow);
        return builder.Build();
    }

    private static string ShortLabel(DateTimeOffset slot)
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var zoned = AppTime.ToZoned(slot);
        return fr.TextInfo.ToTitleCase(zoned.ToString("ddd dd/MM HH:mm", fr));
    }
}
