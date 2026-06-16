using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;
using System.Globalization;

namespace ProjectSYNCS.Commands;

[Group("schedule", "Commandes de planification de sessions")]
public class ScheduleModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly EventService _eventService;

    public ScheduleModule(EventService eventService)
    {
        _eventService = eventService;
    }

    // ---- Wizard step 1: pick a category ----------------------------------

    [SlashCommand("create", "Planifier une nouvelle session")]
    public async Task ScheduleCreateAsync()
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId("schedule:cat")
            .WithPlaceholder("Choisis une catégorie")
            .AddOption("Jeu", nameof(SessionCategory.Game), emote: new Emoji("🎮"))
            .AddOption("Activité", nameof(SessionCategory.Activity), emote: new Emoji("🧑‍🤝‍🧑"))
            .AddOption("Film", nameof(SessionCategory.Movie), emote: new Emoji("🎬"))
            .AddOption("Autre", nameof(SessionCategory.Other), emote: new Emoji("✨"));

        var components = new ComponentBuilder().WithSelectMenu(menu).Build();

        await RespondAsync("**1/5** — Quel type de session ?", components: components, ephemeral: true);
    }

    // ---- Wizard step 2: pick a day ---------------------------------------

    [ComponentInteraction("schedule:cat")]
    public async Task OnCategorySelectedAsync(string[] values)
    {
        var category = values[0];
        var component = (SocketMessageComponent)Context.Interaction;

        await component.UpdateAsync(msg =>
        {
            msg.Content = "**2/5** — Quel jour ?";
            msg.Components = new ComponentBuilder().WithSelectMenu(BuildDaySelect(category)).Build();
        });
    }

    // ---- Wizard step 3: pick an hour -------------------------------------

    [ComponentInteraction("schedule:day:*")]
    public async Task OnDaySelectedAsync(string category, string[] values)
    {
        var date = values[0];
        var component = (SocketMessageComponent)Context.Interaction;

        await component.UpdateAsync(msg =>
        {
            msg.Content = "**3/5** — À quelle heure ?";
            msg.Components = new ComponentBuilder().WithSelectMenu(BuildHourSelect(category, date)).Build();
        });
    }

    // ---- Wizard step 4: pick minutes (optional, defaults to :00) ----------

    [ComponentInteraction("schedule:hour:*:*")]
    public async Task OnHourSelectedAsync(string category, string date, string[] values)
    {
        var hour = values[0];
        var component = (SocketMessageComponent)Context.Interaction;

        var components = new ComponentBuilder()
            .WithSelectMenu(BuildMinuteSelect(category, date, hour))
            .WithButton("Continuer avec :00", $"schedule:skipmin:{category}:{date}:{hour}", ButtonStyle.Secondary)
            .Build();

        await component.UpdateAsync(msg =>
        {
            msg.Content = $"**4/5** — Minutes ? (ou continue directement à {hour}:00)";
            msg.Components = components;
        });
    }

    // ---- Wizard step 5: open the modal for the free-text details ----------

    [ComponentInteraction("schedule:min:*:*:*")]
    public async Task OnMinuteSelectedAsync(string category, string date, string hour, string[] values)
    {
        var minute = values[0];
        await OpenDetailsModalAsync(category, date, hour, minute);
    }

    [ComponentInteraction("schedule:skipmin:*:*:*")]
    public async Task OnSkipMinutesAsync(string category, string date, string hour)
    {
        await OpenDetailsModalAsync(category, date, hour, "00");
    }

    private Task OpenDetailsModalAsync(string category, string date, string hour, string minute)
    {
        // Carry category + chosen datetime through the modal's custom id.
        return RespondWithModalAsync<ScheduleEventModal>($"schedule:finalize:{category}:{date}T{hour}:{minute}");
    }

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

    private static SelectMenuBuilder BuildMinuteSelect(string category, string date, string hour)
    {
        var menu = new SelectMenuBuilder()
            .WithCustomId($"schedule:min:{category}:{date}:{hour}")
            .WithPlaceholder("Choisis les minutes");

        for (int m = 0; m < 60; m += 5)
            menu.AddOption($"{hour}:{m:D2}", $"{m:D2}");

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

        if (gameEvent.OrganizerId != Context.User.Id)
        {
            await FollowupAsync("Seul l'organisateur peut annuler cette session.", ephemeral: true);
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

    [ModalInteraction("schedule:finalize:*:*")]
    public async Task OnScheduleModalSubmittedAsync(string categoryStr, string dateTimeStr, ScheduleEventModal modal)
    {
        await DeferAsync(ephemeral: true);

        int maxPlayers = 0;
        var maxPlayersInput = modal.MaxPlayers.Trim();
        if (maxPlayersInput.Length > 0 &&
            (!int.TryParse(maxPlayersInput, out maxPlayers) || maxPlayers < 2 || maxPlayers > 67))
        {
            await FollowupAsync("Le nombre de joueurs doit être un nombre entre 2 et 67.", ephemeral: true);
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
            await FollowupAsync("Date invalide, réessaie.", ephemeral: true);
            return;
        }

        if (scheduledAt <= DateTimeOffset.Now)
        {
            await FollowupAsync("La session doit être planifiée dans le futur.", ephemeral: true);
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

        await FollowupAsync("Ta session a été planifiée !", ephemeral: true);
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
            .AddField("Places", unlimited ? $"{joined.Count} / ∞" : $"{joined.Count} / {gameEvent.MaxPlayers}", inline: true)
            .WithFooter($"ID de la session : {gameEvent.Id}  •  Heure affichée dans ton fuseau horaire");

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
            .WithButton("Rejoindre", $"event:join:{eventId}",    ButtonStyle.Success, new Emoji("✅"))
            .WithButton("Peut-être", $"event:sub:{eventId}",     ButtonStyle.Primary, new Emoji("🔄"))
            .WithButton("Refuser",   $"event:decline:{eventId}", ButtonStyle.Danger,  new Emoji("❌"))
            .Build();
    }
}
