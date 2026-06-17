using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;
using System.Collections.Concurrent;
using System.Globalization;

namespace ProjectSYNCS.Commands;

[Group("schedule", "Commandes de planification de sessions")]
public class ScheduleModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EventService _eventService;

    // Holds the title a user typed when their modal failed validation, so the
    // "Corriger" button can reopen the modal pre-filled instead of losing it.
    private static readonly ConcurrentDictionary<ulong, string> _draftTitles = new();

    public ScheduleModule(EventService eventService)
    {
        _eventService = eventService;
    }

    // ---- Wizard step 1: pick a category ----------------------------------

    [SlashCommand("create", "Planifier une nouvelle session")]
    public async Task ScheduleCreateAsync()
    {
        await RespondAsync("**1/4** — Quel type de session ?",
            components: BuildCategoryStep(), ephemeral: true);
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

    // ---- Wizard step 3: pick an hour -------------------------------------

    [ComponentInteraction("schedule:day:*", ignoreGroupNames: true)]
    public async Task OnDaySelectedAsync(string category, string[] values)
    {
        var date = values[0];
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
        // Carry category + chosen datetime through the modal's custom id.
        await RespondWithModalAsync<ScheduleEventModal>($"schedule:finalize:{category}:{date}T{hour}:{minute}");
    }

    // ---- Step component builders (shared by forward + "Retour" paths) -----

    private static MessageComponent BuildCategoryStep() =>
        new ComponentBuilder().WithSelectMenu(BuildCategorySelect()).Build();

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

    private static SelectMenuBuilder BuildCategorySelect() =>
        new SelectMenuBuilder()
            .WithCustomId("schedule:cat")
            .WithPlaceholder("Choisis une catégorie")
            .AddOption("Jeu", nameof(SessionCategory.Game), emote: new Emoji("🎮"))
            .AddOption("Activité", nameof(SessionCategory.Activity), emote: new Emoji("🧑‍🤝‍🧑"))
            .AddOption("Film", nameof(SessionCategory.Movie), emote: new Emoji("🎬"))
            .AddOption("Autre", nameof(SessionCategory.Other), emote: new Emoji("✨"));

    private static SelectMenuBuilder BuildDaySelect(string category)
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var menu = new SelectMenuBuilder()
            .WithCustomId($"schedule:day:{category}")
            .WithPlaceholder("Choisis le jour");

        var today = DateTimeOffset.Now.Date;
        for (int i = 0; i < 15; i++)
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

    private static SelectMenuBuilder BuildHourSelect(string category, string date)
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId($"schedule:hour:{category}:{date}")
            .WithPlaceholder("Choisis l'heure");

        for (int h = 0; h < 24; h++)
            menu.AddOption($"{h:D2}h", $"{h:D2}");

        return menu;
    }

    [SlashCommand("cancel", "Annuler une session que tu as organisée")]
    public async Task ScheduleCancelAsync([Summary("event-id", "L'ID affiché dans le pied de page de la session")] int eventId)
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

        await _eventService.CancelEventAsync(eventId);

        if (gameEvent.MessageId != 0)
        {
            var channel = Context.Guild.GetTextChannel(gameEvent.ChannelId);
            if (channel is not null)
            {
                var message = await channel.GetMessageAsync(gameEvent.MessageId) as IUserMessage;
                if (message is not null)
                {
                    gameEvent.IsCancelled = true;
                    await message.ModifyAsync(props =>
                    {
                        props.Embed = BuildEventEmbed(gameEvent, Context.Guild);
                        props.Components = new ComponentBuilder().Build();
                    });
                }
            }
        }

        await FollowupAsync("La session a été annulée.", ephemeral: true);
    }

    [SlashCommand("edit", "Modifier une session que tu as organisée")]
    public async Task ScheduleEditAsync([Summary("event-id", "L'ID affiché dans le pied de page de la session")] int eventId)
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

        int maxPlayers = 0;
        var maxPlayersInput = modal.MaxPlayers.Trim();
        if (maxPlayersInput.Length > 0 &&
            (!int.TryParse(maxPlayersInput, out maxPlayers) || (maxPlayers != 0 && (maxPlayers < 2 || maxPlayers > 67))))
        {
            await FollowupAsync("Le nombre de participants doit être 0 (illimité) ou un nombre entre 2 et 67.", ephemeral: true);
            return;
        }

        if (!DateTimeOffset.TryParseExact(
                $"{modal.Date.Trim()}T{modal.Time.Trim()}",
                "yyyy-MM-ddTHH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var scheduledAt))
        {
            await FollowupAsync("Date ou heure invalide. Utilise les formats `AAAA-MM-JJ` et `HH:mm`.", ephemeral: true);
            return;
        }

        if (scheduledAt <= DateTimeOffset.Now)
        {
            await FollowupAsync("La session doit être planifiée dans le futur.", ephemeral: true);
            return;
        }

        await _eventService.UpdateEventAsync(eventId, modal.SessionTitle.Trim(), scheduledAt, maxPlayers);
        var updated = await _eventService.GetEventWithParticipantsAsync(eventId);

        if (updated!.MessageId != 0)
        {
            var channel = Context.Guild.GetTextChannel(updated.ChannelId);
            if (channel is not null &&
                await channel.GetMessageAsync(updated.MessageId) is IUserMessage message)
            {
                await message.ModifyAsync(props =>
                {
                    props.Embed = BuildEventEmbed(updated, Context.Guild);
                    props.Components = BuildEventComponents(updated.Id);
                });
            }
        }

        await FollowupAsync("Session mise à jour ✅", ephemeral: true);
    }

    public static Modal BuildEditModal(SessionEvent gameEvent)
    {
        return new ModalBuilder()
            .WithTitle("Modifier la session")
            .WithCustomId($"event:editmodal:{gameEvent.Id}")
            .AddTextInput("Nom de la session", "title", value: gameEvent.Title, required: true)
            .AddTextInput("Date (AAAA-MM-JJ)", "date",
                value: gameEvent.ScheduledAt.ToString("yyyy-MM-dd"), placeholder: "ex. 2026-06-20", required: true)
            .AddTextInput("Heure (HH:mm)", "time",
                value: gameEvent.ScheduledAt.ToString("HH:mm"), placeholder: "ex. 20:30", required: true)
            .AddTextInput("Participants max (0 = illimité)", "max_players",
                value: gameEvent.MaxPlayers.ToString(), required: false)
            .Build();
    }

    [ModalInteraction("schedule:finalize:*:*", ignoreGroupNames: true)]
    public async Task OnScheduleModalSubmittedAsync(string categoryStr, string dateTimeStr, ScheduleEventModal modal)
    {
        var modalInteraction = (SocketModal)Context.Interaction;

        int maxPlayers = 0;
        var maxPlayersInput = modal.MaxPlayers.Trim();
        if (maxPlayersInput.Length > 0 &&
            (!int.TryParse(maxPlayersInput, out maxPlayers) || maxPlayers < 2 || maxPlayers > 67))
        {
            _draftTitles[Context.User.Id] = modal.SessionTitle;
            await modalInteraction.UpdateAsync(msg =>
            {
                msg.Content = "⚠️ Le nombre de participants doit être un nombre entre **2** et **67**.";
                msg.Components = new ComponentBuilder()
                    .WithButton("Corriger", $"schedule:retry:{categoryStr}:{dateTimeStr}", ButtonStyle.Primary)
                    .Build();
            });
            return;
        }

        if (!Enum.TryParse<SessionCategory>(categoryStr, out var category))
            category = SessionCategory.Other;

        if (!DateTimeOffset.TryParseExact(
                dateTimeStr,
                "yyyy-MM-ddTHH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var scheduledAt))
        {
            await modalInteraction.UpdateAsync(msg =>
            {
                msg.Content = "⚠️ Date invalide. Relance `/schedule create`.";
                msg.Components = new ComponentBuilder().Build();
            });
            return;
        }

        if (scheduledAt <= DateTimeOffset.Now)
        {
            await modalInteraction.UpdateAsync(msg =>
            {
                msg.Content = "⚠️ La session doit être planifiée dans le futur. Relance `/schedule create`.";
                msg.Components = new ComponentBuilder().Build();
            });
            return;
        }

        var gameEvent = await _eventService.CreateEventAsync(
            guildId: Context.Guild.Id,
            channelId: Context.Channel.Id,
            organizerId: Context.User.Id,
            title: modal.SessionTitle.Trim(),
            category: category,
            scheduledAt: scheduledAt,
            maxPlayers: maxPlayers);

        var embed = BuildEventEmbed(gameEvent, Context.Guild);
        var components = BuildEventComponents(gameEvent.Id);

        var message = await Context.Channel.SendMessageAsync(embed: embed, components: components);

        await _eventService.SetMessageIdAsync(gameEvent.Id, message.Id);

        // Dismiss the ephemeral wizard message now that the session exists.
        _draftTitles.TryRemove(Context.User.Id, out _);
        await modalInteraction.UpdateAsync(msg =>
        {
            msg.Content = "Session créée.";
            msg.Components = new ComponentBuilder().Build();
        });
        await modalInteraction.DeleteOriginalResponseAsync();
    }

    // Reopens the details modal (pre-filled) after a validation error, without
    // posting anything to the channel.
    [ComponentInteraction("schedule:retry:*:*", ignoreGroupNames: true)]
    public async Task OnRetryAsync(string category, string dateTime)
    {
        _draftTitles.TryGetValue(Context.User.Id, out var title);

        var modal = new ModalBuilder()
            .WithTitle("Planifier une session")
            .WithCustomId($"schedule:finalize:{category}:{dateTime}")
            .AddTextInput("Nom de la session", "title",
                placeholder: "ex. Among Us, Gartic, Anime ?", value: title, required: true)
            .AddTextInput("Nombre de participants max - Optionnel", "max_players", required: false)
            .Build();

        await RespondWithModalAsync(modal);
    }

    public static Embed BuildEventEmbed(SessionEvent gameEvent, IGuild? guild)
    {
        var joined = gameEvent.Participants.Where(p => p.Status == ParticipantStatus.Joined).ToList();
        var maybes = gameEvent.Participants.Where(p => p.Status == ParticipantStatus.Maybe).ToList();
        var declined = gameEvent.Participants.Where(p => p.Status == ParticipantStatus.Declined).ToList();

        bool unlimited = gameEvent.MaxPlayers == 0;
        bool hasFreeSlot = unlimited || joined.Count < gameEvent.MaxPlayers;
        var ts = gameEvent.ScheduledAt.ToUnixTimeSeconds();
        var categoryLabel = gameEvent.Category switch
        {
            SessionCategory.Game     => "🎮 Jeu",
            SessionCategory.Activity => "🧑‍🤝‍🧑 Activité",
            SessionCategory.Movie    => "🎬 Film",
            _                        => "✨ Autre"
        };

        var eb = new EmbedBuilder()
            .WithTitle(gameEvent.IsCancelled ? $"~~{gameEvent.Title}~~ — ANNULÉ" : $"{categoryLabel} : {gameEvent.Title}")
            .WithColor(gameEvent.IsCancelled ? Color.DarkRed : hasFreeSlot ? Color.Green : Color.Orange)
            .AddField("Quand", $"<t:{ts}:F>", inline: true)
            .AddField("Participants", unlimited ? $"{joined.Count}" : $"{joined.Count} / {gameEvent.MaxPlayers}", inline: true)
            .WithFooter($"ID de la session : {gameEvent.Id}");

        if (joined.Count > 0)
            eb.AddField($"Participants ({joined.Count})", string.Join("\n", joined.Select(p => $"<@{p.UserId}>")));

        if (maybes.Count > 0)
            eb.AddField($"Peut-être ({maybes.Count})", string.Join("\n", maybes.Select(p => $"<@{p.UserId}>")));

        if (declined.Count > 0)
            eb.AddField($"Absents ({declined.Count})", string.Join("\n", declined.Select(p => $"<@{p.UserId}>")));

        return eb.Build();
    }

    public static MessageComponent BuildEventComponents(int eventId)
    {
        return new ComponentBuilder()
            .WithButton("Rejoindre", $"event:join:{eventId}",    ButtonStyle.Success, new Emoji("✅"), row: 0)
            .WithButton("Peut-être", $"event:sub:{eventId}",     ButtonStyle.Primary, new Emoji("🔄"), row: 0)
            .WithButton("Refuser",   $"event:decline:{eventId}", ButtonStyle.Danger,  new Emoji("✖️"), row: 0)
            .WithButton("Modifier",  $"event:edit:{eventId}",    ButtonStyle.Secondary, new Emoji("✏️"), row: 1)
            .WithButton("Annuler",   $"event:cancel:{eventId}",  ButtonStyle.Secondary, new Emoji("🗑️"), row: 1)
            .Build();
    }
}
