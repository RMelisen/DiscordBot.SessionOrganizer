using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /remindme and /reminders. Delivery is PersonalReminderService's (its own 1-minute loop). No
// [CommandContextType]: nothing reads Context.Guild unguarded, so both work in a DM too — the
// reminder then simply has no channel to fall back to.
public class RemindModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PersonalReminderStore _store;
    private readonly ResponsePicker _picker;

    public RemindModule(PersonalReminderStore store, ResponsePicker picker)
    {
        _store = store;
        _picker = picker;
    }

    [SlashCommand("remindme", "Je te rappelle quelque chose plus tard, en message privé")]
    public async Task RemindMeAsync(
        [Summary("when", "« 10m », « dans 2h », « 21h30 », « demain 9h », « samedi 14h », « 25/12 10:00 »")]
        [MaxLength(InputCaps.ReminderWhen)]
        string when,
        [Summary("what", "Ce que je dois te rappeler")]
        [MaxLength(InputCaps.Reminder)]
        string what)
    {
        var now = DateTimeOffset.UtcNow;
        var due = ReminderTime.Parse(when, now);
        if (due is null)
        {
            await RespondAsync(
                "Je n'ai pas compris quand. Essaie par exemple :\n" +
                "`10m` · `dans 2h` · `dans 1h30` · `3j` · `21h30` · `demain 9h` · `samedi 14h` · `25/12 10:00`\n" +
                "-# « 21h » toute seule, c'est l'heure ; pour une durée, dis « dans 2h ».",
                ephemeral: true);
            return;
        }
        if (due.Value - now < ReminderTime.Min)
        {
            await RespondAsync("C'est maintenant, ça. Je ne rappelle pas le présent.", ephemeral: true);
            return;
        }
        if (due.Value - now > ReminderTime.Max)
        {
            await RespondAsync($"Plus de {ReminderTime.Max.TotalDays:0} jours ? Mes logs ne voient pas si loin.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        if (await _store.CountPendingAsync(Context.User.Id) >= PersonalReminderStore.MaxPending)
        {
            await FollowupAsync($"Tu as déjà {PersonalReminderStore.MaxPending} rappels en attente. Annule-en un avec `/reminders`.",
                ephemeral: true);
            return;
        }

        var guildId = Context.Guild?.Id ?? 0;
        await _store.AddAsync(guildId, Context.Channel.Id, Context.User.Id, what.Trim(), due.Value);
        var unix = due.Value.ToUnixTimeSeconds();
        await FollowupAsync($"{_picker.Pick(BotResponses.ReminderSetLines)}\n<t:{unix}:F> (<t:{unix}:R>)\n" +
                            MessageFormat.Quote(what.Trim(), InputCaps.Reminder),
            ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("reminders", "Voir et annuler tes rappels en attente")]
    public async Task RemindersAsync()
    {
        await DeferAsync(ephemeral: true);
        var (embed, components) = await BuildListAsync(_store, Context.User.Id);
        await FollowupAsync(embed: embed, components: components, ephemeral: true);
    }

    // Shared with the cancel handler, which redraws the list in place.
    public static async Task<(Embed Embed, MessageComponent Components)> BuildListAsync(PersonalReminderStore store, ulong userId)
    {
        var pending = await store.GetPendingAsync(userId);
        var embed = new EmbedBuilder().WithTitle("⏰ Tes rappels").WithColor(Color.Gold);
        if (pending.Count == 0)
        {
            embed.WithDescription("Aucun rappel en attente. `/remindme` pour en créer un.");
            return (embed.Build(), new ComponentBuilder().Build());
        }

        // A select holds 25 options, and the description is capped well under 4096.
        var shown = pending.Take(25).ToList();
        embed.WithDescription(string.Join("\n", shown.Select(r =>
            $"<t:{r.DueAt.ToUnixTimeSeconds()}:f> — {Shorten(r.Text, 80)}")));
        if (pending.Count > shown.Count)
            embed.WithFooter($"… et {pending.Count - shown.Count} autre(s)");

        var menu = new SelectMenuBuilder()
            .WithCustomId("rem:cancel")
            .WithPlaceholder("Annuler un rappel…");
        foreach (var r in shown)
            menu.AddOption(Shorten(r.Text, 100), r.Id.ToString(),
                description: AppTime.ToZoned(r.DueAt).ToString("dd/MM HH:mm"));
        return (embed.Build(), new ComponentBuilder().WithSelectMenu(menu).Build());
    }

    private static string Shorten(string text, int max)
    {
        var line = text.ReplaceLineEndings(" ");
        return line.Length <= max ? line : line[..(max - 1)] + "…";
    }
}
